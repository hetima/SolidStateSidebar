using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using SSS.Core;

namespace SSS.Module.ClaudeMonitor
{
    public class ClaudeUsageMetric : BaseMetric
    {
        public ClaudeUsageMetric(MetricKey key, string label)
            : base(key, DataType.Dynamic, label)
        {
            Text = "-";
        }


        public override bool IsNumeric => false;

        public override void Update() { }

        public void SetText(string? value) => Text = value;
    }

    public class ClaudeMonitor : BaseMonitor
    {
        private static readonly string CredentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", ".credentials.json");

        private const string UsageEndpoint = "https://api.anthropic.com/api/oauth/usage";
        private const string TokenUrl = "https://platform.claude.com/v1/oauth/token";
        private const string ClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
        private const string BetaHeader = "oauth-2025-04-20";

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly SemaphoreSlim _lock = new(1, 1);

        private Timer? _timer;
        private ResetTimeDisplay _shortDisplay = ResetTimeDisplay.Countdown;
        private ResetTimeDisplay _longDisplay  = ResetTimeDisplay.Countdown;

        public ClaudeMonitor() : base("claude", "Claude", false)
        {
            Metrics =
            [
                new ClaudeUsageMetric(MetricKey.Claude5h, "5h"),
                new ClaudeUsageMetric(MetricKey.Claude1w, "1w"),
            ];
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

            // 初回は即時実行
            ManualRefresh(shortDisplay, longDisplay);

            if (ms != Timeout.Infinite)
                _timer = new Timer(_ => ManualRefresh(_shortDisplay, _longDisplay), null, ms, ms);
        }

        /// <summary>
        /// 手動更新：Claude API から使用量を取得してメトリクスを更新する。
        /// </summary>
        public void ManualRefresh(ResetTimeDisplay shortDisplay, ResetTimeDisplay longDisplay)
        {
            if (Metrics is not { Length: >= 2 }) return;
            if (Metrics[0] is not ClaudeUsageMetric metric5h) return;
            if (Metrics[1] is not ClaudeUsageMetric metric1w) return;

            metric5h.SetText("Loading...");
            metric1w.SetText("Loading...");

            Task.Run(async () =>
            {
                try
                {
                    var token = await GetValidAccessTokenAsync();
                    if (string.IsNullOrEmpty(token)) { metric5h.SetText("Error"); metric1w.SetText("Error"); return; }

                    var request = new HttpRequestMessage(HttpMethod.Get, UsageEndpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Headers.Add("anthropic-beta", BetaHeader);

                    var response = await _http.SendAsync(request);
                    if (!response.IsSuccessStatusCode) { metric5h.SetText("Error"); metric1w.SetText("Error"); return; }

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    metric5h.SetText(FormatWindow(root, "five_hour", isWeekly: false, shortDisplay));
                    metric1w.SetText(FormatWindow(root, "seven_day", isWeekly: true,  longDisplay));
                }
                catch
                {
                    metric5h.SetText("Error");
                    metric1w.SetText("Error");
                }
            });
        }

        /// <summary>
        /// utilization と resets_at を整形する。例: "50% (2h30m)" / "50% (6d)"
        /// </summary>
        private static string FormatWindow(JsonElement root, string key, bool isWeekly, ResetTimeDisplay display)
        {
            if (!root.TryGetProperty(key, out var win) || win.ValueKind == JsonValueKind.Null)
                return "-";

            double util = win.TryGetProperty("utilization", out var u) ? u.GetDouble() : 0;
            string reset = "";
            if (win.TryGetProperty("resets_at", out var ra) &&
                DateTimeOffset.TryParse(ra.GetString(), out var dt))
            {
                var local = dt.LocalDateTime;
                reset = display == ResetTimeDisplay.Absolute
                    ? FormatAbsolute(local, isWeekly)
                    : FormatCountdown(local, isWeekly);
                reset = $" ({reset})";
            }

            return $"{util:0}%{reset}";
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

        /// <summary>
        /// 有効なアクセストークンを返す。期限切れなら自動リフレッシュする。
        /// </summary>
        private static async Task<string?> GetValidAccessTokenAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (!File.Exists(CredentialsPath)) return null;

                string raw;
                string? accessToken, refreshToken;
                long expiresAt;
                try
                {
                    raw = File.ReadAllText(CredentialsPath);
                    using var doc = JsonDocument.Parse(raw);
                    var oauthEl = doc.RootElement.GetProperty("claudeAiOauth");
                    accessToken  = oauthEl.TryGetProperty("accessToken",  out var at) ? at.GetString() : null;
                    refreshToken = oauthEl.TryGetProperty("refreshToken", out var rt) ? rt.GetString() : null;
                    expiresAt    = oauthEl.TryGetProperty("expiresAt",    out var ea) ? ea.GetInt64()  : 0;
                }
                catch { return null; }

                if (accessToken is null) return null;

                // 60秒バッファ付きで有効期限チェック
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < expiresAt - 60_000)
                    return accessToken;

                // リフレッシュ試行
                var newToken = await TryRefreshAsync(refreshToken);
                if (newToken is null) return accessToken;

                // credentials.json に書き戻す（既存フィールドを保持）
                try
                {
                    var rawNode = JsonNode.Parse(File.ReadAllText(CredentialsPath))!;
                    rawNode["claudeAiOauth"]!["accessToken"] = newToken.AccessToken;
                    rawNode["claudeAiOauth"]!["expiresAt"]   = newToken.ExpiresAt;
                    if (!string.IsNullOrEmpty(newToken.RefreshToken))
                        rawNode["claudeAiOauth"]!["refreshToken"] = newToken.RefreshToken;
                    File.WriteAllText(CredentialsPath,
                        rawNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }
                catch { /* 書き込み失敗は無視 */ }

                return newToken.AccessToken;
            }
            finally
            {
                _lock.Release();
            }
        }

        private static async Task<RefreshResult?> TryRefreshAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) return null;
            try
            {
                var body = JsonSerializer.Serialize(new
                {
                    grant_type    = "refresh_token",
                    refresh_token = refreshToken,
                    client_id     = ClientId
                });
                var response = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Post, TokenUrl)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
                if (!response.IsSuccessStatusCode) return null;

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                if (!root.TryGetProperty("access_token", out var at)) return null;

                long expiresAt = root.TryGetProperty("expires_in", out var ei)
                    ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ei.GetInt64() * 1000
                    : DateTimeOffset.UtcNow.AddHours(5).ToUnixTimeMilliseconds();

                string? newRefresh = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
                return new RefreshResult(at.GetString()!, expiresAt, newRefresh);
            }
            catch { return null; }
        }

        public static iMonitor[] GetInstances()
            => [new ClaudeMonitor()];

        private record RefreshResult(string AccessToken, long ExpiresAt, string? RefreshToken);
    }
}
