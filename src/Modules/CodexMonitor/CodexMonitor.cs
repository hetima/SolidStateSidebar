using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SSS.Core;

namespace SSS.Module.CodexMonitor
{
    public class CodexUsageMetric : BaseMetric
    {
        public CodexUsageMetric(MetricKey key, string label)
            : base(key, DataType.Dynamic, label)
        {
            Text = "-";
        }

        ~CodexUsageMetric()
        {
            Dispose(false);
        }

        public override bool IsNumeric => false;

        public override void Update() { }

        public void SetText(string? value) => Text = value;
    }

    public class CodexMonitor : BaseMonitor
    {
        private static readonly string AuthPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex", "auth.json");

        private const string UsageEndpoint = "https://chatgpt.com/backend-api/wham/usage";

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        private Timer? _timer;
        private ResetTimeDisplay _shortDisplay = ResetTimeDisplay.Countdown;
        private ResetTimeDisplay _longDisplay  = ResetTimeDisplay.Countdown;

        public CodexMonitor() : base("codex", "Codex", false)
        {
            Metrics =
            [
                new CodexUsageMetric(MetricKey.Codex5h, "5h"),
                new CodexUsageMetric(MetricKey.Codex1w, "1w"),
            ];
        }

        ~CodexMonitor()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// ロード時に呼ぶ。初回即時実行 + 定期タイマーを設定する。
        /// </summary>
        public void StartAutoRefresh(AutoRefreshInterval interval, ResetTimeDisplay shortDisplay, ResetTimeDisplay longDisplay)
        {
            _shortDisplay = shortDisplay;
            _longDisplay  = longDisplay;

            _timer?.Dispose();

            int ms = interval switch
            {
                AutoRefreshInterval.OneMin  => 60_000,
                AutoRefreshInterval.FiveMin => 300_000,
                AutoRefreshInterval.TenMin  => 600_000,
                _                           => Timeout.Infinite
            };

            ManualRefresh(shortDisplay, longDisplay);

            if (ms != Timeout.Infinite)
                _timer = new Timer(_ => ManualRefresh(_shortDisplay, _longDisplay), null, ms, ms);
        }

        /// <summary>
        /// 手動更新：Codex API から使用量を取得してメトリクスを更新する。
        /// </summary>
        public void ManualRefresh(ResetTimeDisplay shortDisplay, ResetTimeDisplay longDisplay)
        {
            if (Metrics is not { Length: >= 2 }) return;
            if (Metrics[0] is not CodexUsageMetric metric5h) return;
            if (Metrics[1] is not CodexUsageMetric metric1w) return;

            metric5h.SetText("Loading...");
            metric1w.SetText("Loading...");

            Task.Run(async () =>
            {
                try
                {
                    var token = TryGetAccessToken();
                    if (string.IsNullOrEmpty(token)) { metric5h.SetText("Error"); metric1w.SetText("Error"); return; }

                    var request = new HttpRequestMessage(HttpMethod.Get, UsageEndpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await _http.SendAsync(request);
                    if (!response.IsSuccessStatusCode) { metric5h.SetText("Error"); metric1w.SetText("Error"); return; }

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("rate_limit", out var rl)) { metric5h.SetText("Error"); metric1w.SetText("Error"); return; }

                    metric5h.SetText(FormatWindow(rl, "primary_window",   isWeekly: false, shortDisplay));
                    metric1w.SetText(FormatWindow(rl, "secondary_window", isWeekly: true,  longDisplay));
                }
                catch
                {
                    metric5h.SetText("Error");
                    metric1w.SetText("Error");
                }
            });
        }

        /// <summary>
        /// used_percent と reset_at を整形する。例: "45% (2h30m)" / "45% (6d)"
        /// </summary>
        private static string FormatWindow(JsonElement rl, string key, bool isWeekly, ResetTimeDisplay display)
        {
            if (!rl.TryGetProperty(key, out var win) || win.ValueKind == JsonValueKind.Null)
                return "-";

            double pct = win.TryGetProperty("used_percent", out var u) ? u.GetDouble() : 0;
            string reset = "";
            if (win.TryGetProperty("reset_at", out var ra) && ra.TryGetInt64(out var epoch))
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(epoch).LocalDateTime;
                reset = display == ResetTimeDisplay.Absolute
                    ? FormatAbsolute(dt, isWeekly)
                    : FormatCountdown(dt, isWeekly);
                reset = $" ({reset})";
            }

            return $"{pct:0}%{reset}";
        }

        private static string FormatAbsolute(DateTime resetAt, bool isWeekly)
        {
            var remaining = resetAt - DateTime.Now;
            return isWeekly && remaining.TotalHours >= 24
                ? $"{resetAt:M/d}"
                : $"{resetAt:HH:mm}";
        }

        private static string FormatCountdown(DateTime resetAt, bool isWeekly)
        {
            var remaining = resetAt - DateTime.Now;
            if (remaining <= TimeSpan.Zero) return "0:00";
            if (isWeekly && remaining.TotalHours >= 24)
                return $"{(int)remaining.TotalDays}d";
            return $"{(int)remaining.TotalHours}:{remaining.Minutes:00}";
        }

        private static string? TryGetAccessToken()
        {
            try
            {
                if (!File.Exists(AuthPath)) return null;
                var json = File.ReadAllText(AuthPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                // auth.json 構造: { "tokens": { "access_token": "..." } }
                return root.TryGetProperty("tokens", out var tokens) &&
                       tokens.TryGetProperty("access_token", out var t)
                    ? t.GetString() : null;
            }
            catch { return null; }
        }

        public static iMonitor[] GetInstances()
            => [new CodexMonitor()];
    }
}
