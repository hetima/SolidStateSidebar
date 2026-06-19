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

        public override bool IsNumeric => false;

        public override void Update() { }

        public void SetText(string? value) => Text = value;

        /// <summary>ラベル（左側テキスト）を動的に変更する</summary>
        public void SetLabel(string value)
        {
            CustomLabel = value;
        }
    }

    public class CodexMonitor : BaseMonitor
    {
        private static readonly string AuthPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex", "auth.json");

        private const string UsageEndpoint = "https://chatgpt.com/backend-api/wham/usage";
        private const string CreditsEndpoint = "https://chatgpt.com/backend-api/wham/rate-limit-reset-credits";

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        private Timer? _timer;
        private ResetTimeDisplay _shortDisplay = ResetTimeDisplay.Countdown;
        private ResetTimeDisplay _longDisplay  = ResetTimeDisplay.Countdown;
        public CodexMonitor(bool showResetCredits) : base("codex", "Codex", false)
        {

            var metrics = new System.Collections.Generic.List<iMetric>
            {
                new CodexUsageMetric(MetricKey.Codex5h, "5h"),
                new CodexUsageMetric(MetricKey.Codex1w, "1w"),
            };
            if (showResetCredits)
                metrics.Add(new CodexUsageMetric(MetricKey.CodexCredits, "-"));

            Metrics = [.. metrics];
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
            var metricCredits = (Metrics.Length >= 3) ? Metrics[2] as CodexUsageMetric : null;

            metric5h.SetText("Loading...");
            metric1w.SetText("Loading...");
            metricCredits?.SetText("Loading...");

            Task.Run(async () =>
            {
                try
                {
                    var token = TryGetAccessToken();
                    if (string.IsNullOrEmpty(token)) { metric5h.SetText("Error"); metric1w.SetText("Error"); metricCredits?.SetText("Error"); return; }

                    var request = new HttpRequestMessage(HttpMethod.Get, UsageEndpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await _http.SendAsync(request);
                    if (!response.IsSuccessStatusCode) { metric5h.SetText("Error"); metric1w.SetText("Error"); metricCredits?.SetText("Error"); return; }

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("rate_limit", out var rl)) { metric5h.SetText("Error"); metric1w.SetText("Error"); metricCredits?.SetText("Error"); return; }

                    metric5h.SetText(FormatWindow(rl, "primary_window",   isWeekly: false, shortDisplay));
                    metric1w.SetText(FormatWindow(rl, "secondary_window", isWeekly: true,  longDisplay));

                    if (metricCredits != null)
                        await RefreshResetCreditsAsync(token, metricCredits);
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

        /// <summary>クレジット有効期限の残り時間を表示する。24h以上は日数、未満は時:分</summary>
        private static string FormatCreditsExpiry(DateTime expiresAt)
        {
            var remaining = expiresAt - DateTime.Now;
            if (remaining <= TimeSpan.Zero) return "0:00";
            if (remaining.TotalHours >= 24)
                return $"{(int)remaining.TotalDays}d";
            return $"{(int)remaining.TotalHours}:{remaining.Minutes:00}";
        }

        /// <summary>
        /// rate-limit-reset-credits API を叩き、available な codex_rate_limits クレジットの件数と
        /// 直近の expires_at をメトリクスに反映する。
        /// </summary>
        private static async Task RefreshResetCreditsAsync(string token, CodexUsageMetric metric)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, CreditsEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) { metric.SetLabel("-"); metric.SetText("Error"); return; }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("credits", out var credits)) { metric.SetLabel("-"); metric.SetText("-"); return; }

                // status:available かつ reset_type:codex_rate_limits のものを抽出
                DateTimeOffset? nearest = null;
                int count = 0;
                foreach (var c in credits.EnumerateArray())
                {
                    if (c.TryGetProperty("status", out var s) && s.GetString() != "available") continue;
                    if (c.TryGetProperty("reset_type", out var r) && r.GetString() != "codex_rate_limits") continue;

                    count++;
                    if (c.TryGetProperty("expires_at", out var exp) && exp.ValueKind == JsonValueKind.String)
                    {
                        if (DateTimeOffset.TryParse(exp.GetString(), out var dto))
                        {
                            if (nearest == null || dto < nearest)
                                nearest = dto;
                        }
                    }
                }

                metric.SetLabel("x" + count.ToString());
                metric.SetText(nearest.HasValue ? FormatCreditsExpiry(nearest.Value.LocalDateTime) : "-");
            }
            catch
            {
                metric.SetLabel("-");
                metric.SetText("Error");
            }
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

        public static iMonitor[] GetInstances(bool showResetCredits = false)
            => [new CodexMonitor(showResetCredits)];
    }
}
