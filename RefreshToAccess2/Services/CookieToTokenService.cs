using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RefreshToAccess2.Services
{
    /// <summary>
    /// Converts a Microsoft login cookie (specifically <c>__Host-MSAAUTHP</c>)
    /// into a Minecraft access token, using the legacy desktop client OAuth
    /// chain: authorize → token → XBL → XSTS → login_with_xbox.
    /// Ported from the MCC2T flow. Produces an access token only — no refresh
    /// token, no auto-login, no profile persistence.
    /// </summary>
    public static class CookieToTokenService
    {
        private const string ClientId    = "00000000402b5328";
        private const string RedirectUri = "https://login.live.com/oauth20_desktop.srf";
        private const string Scope       = "service::user.auth.xboxlive.com::MBI_SSL";

        // Reused for the JSON Xbox/Minecraft steps (auto-redirect is fine here).
        private static readonly HttpClient _http = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        });

        private static readonly Regex MsaCookieRegex =
            new(@"__Host-MSAAUTHP", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Runs the full cookie→token chain.
        /// </summary>
        /// <returns>[username, uuid, accessToken]</returns>
        public static async Task<string[]> ConvertAsync(
            string cookieDump, IProgress<string>? progress = null)
        {
            string cookieHeader = BuildCookieHeader(cookieDump);
            if (string.IsNullOrWhiteSpace(cookieHeader))
                throw new Exception(Localization.Loc.T("Cookie.Err.NoCookie"));

            progress?.Report(Localization.Loc.T("Cookie.Step.AuthCode"));
            string code = await GetAuthCodeAsync(cookieHeader);

            progress?.Report(Localization.Loc.T("Login.MsToken"));
            string msToken = await GetMsTokenAsync(code);

            progress?.Report(Localization.Loc.T("Login.XblToken"));
            (string xblToken, _) = await XblAuthAsync(msToken);

            progress?.Report(Localization.Loc.T("Login.XstsToken"));
            (string xstsToken, string userHash) = await XstsAuthAsync(xblToken);

            progress?.Report(Localization.Loc.T("Login.AccessToken"));
            string accessToken = await McLoginAsync(userHash, xstsToken);

            progress?.Report(Localization.Loc.T("Login.Profile"));
            (string uuid, string name) = await GetProfileAsync(accessToken);

            return new[] { name, uuid, accessToken };
        }

        // ── Cookie parsing ─────────────────────────────────────────────

        /// <summary>
        /// Extracts a Cookie header value. Prefers a lone <c>__Host-MSAAUTHP</c>;
        /// otherwise assembles all name=value pairs found in the dump.
        /// Supports Netscape tab-separated and plain name=value lines.
        /// </summary>
        private static string BuildCookieHeader(string dump)
        {
            if (string.IsNullOrWhiteSpace(dump)) return "";

            var pairs = new List<(string name, string value)>();

            foreach (var rawLine in dump.Replace("\r", "").Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#") ||
                    line.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Netscape: domain \t flag \t path \t secure \t expiry \t name \t value
                if (line.Contains('\t'))
                {
                    var cols = line.Split('\t');
                    if (cols.Length >= 7)
                        pairs.Add((cols[5].Trim(), cols[6].Trim()));
                    continue;
                }

                // Plain "name=value" (optionally "; "-joined).
                foreach (var part in line.Split(';'))
                {
                    int eq = part.IndexOf('=');
                    if (eq <= 0) continue;
                    string n = part.Substring(0, eq).Trim();
                    string v = part.Substring(eq + 1).Trim();
                    if (n.Length > 0) pairs.Add((n, v));
                }
            }

            // Prefer the single auth cookie the flow actually needs.
            foreach (var (name, value) in pairs)
                if (MsaCookieRegex.IsMatch(name))
                    return $"{name}={value}";

            if (pairs.Count == 0) return "";

            var sb = new StringBuilder();
            foreach (var (name, value) in pairs)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(name).Append('=').Append(value);
            }
            return sb.ToString();
        }

        // ── Step 1: authorization code (manual redirect, cookie per hop) ──

        private static async Task<string> GetAuthCodeAsync(string cookieHeader)
        {
            string url =
                "https://login.live.com/oauth20_authorize.srf" +
                $"?client_id={ClientId}" +
                "&response_type=code" +
                $"&scope={Uri.EscapeDataString(Scope)}" +
                $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}";

            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler);

            for (int hop = 0; hop < 6; hop++)
            {
                var msg = new HttpRequestMessage(HttpMethod.Get, url);
                msg.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

                var resp = await client.SendAsync(msg);

                // Auth code can appear on the current URL once redirected to the desktop srf.
                string? code = TryExtractCode(url);
                if (code != null) return code;

                if ((int)resp.StatusCode is >= 300 and < 400 &&
                    resp.Headers.Location is { } loc)
                {
                    url = loc.IsAbsoluteUri ? loc.ToString()
                                            : new Uri(new Uri(url), loc).ToString();
                    code = TryExtractCode(url);
                    if (code != null) return code;
                    continue;
                }

                break; // no more redirects, no code
            }

            throw new Exception(Localization.Loc.T("Cookie.Err.Expired"));
        }

        private static string? TryExtractCode(string url)
        {
            int q = url.IndexOf('?');
            if (q < 0) return null;

            foreach (var pair in url.Substring(q + 1).Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0) continue;
                if (pair.Substring(0, eq) == "code")
                {
                    string code = Uri.UnescapeDataString(pair.Substring(eq + 1));
                    return string.IsNullOrEmpty(code) ? null : code;
                }
            }
            return null;
        }

        // ── Step 2: MS access token ─────────────────────────────────────

        private static async Task<string> GetMsTokenAsync(string code)
        {
            var form = new Dictionary<string, string>
            {
                ["client_id"]    = ClientId,
                ["code"]         = code,
                ["grant_type"]   = "authorization_code",
                ["redirect_uri"] = RedirectUri
            };

            var resp = await _http.PostAsync(
                "https://login.live.com/oauth20_token.srf",
                new FormUrlEncodedContent(form));

            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return body["access_token"]!.ToString();
        }

        // ── Step 3: XBL ─────────────────────────────────────────────────

        private static async Task<(string token, string uhs)> XblAuthAsync(string msToken)
        {
            var payload = new JObject
            {
                ["Properties"] = new JObject
                {
                    ["AuthMethod"] = "RPS",
                    ["SiteName"]   = "user.auth.xboxlive.com",
                    ["RpsTicket"]  = msToken
                },
                ["RelyingParty"] = "http://auth.xboxlive.com",
                ["TokenType"]    = "JWT"
            };

            var resp = await _http.SendAsync(BuildJson(
                "https://user.auth.xboxlive.com/user/authenticate", payload));
            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return (body["Token"]!.ToString(),
                    body["DisplayClaims"]!["xui"]![0]!["uhs"]!.ToString());
        }

        // ── Step 4: XSTS ────────────────────────────────────────────────

        private static async Task<(string token, string uhs)> XstsAuthAsync(string xblToken)
        {
            var payload = new JObject
            {
                ["Properties"] = new JObject
                {
                    ["SandboxId"]  = "RETAIL",
                    ["UserTokens"] = new JArray(xblToken)
                },
                ["RelyingParty"] = "rp://api.minecraftservices.com/",
                ["TokenType"]    = "JWT"
            };

            var resp = await _http.SendAsync(BuildJson(
                "https://xsts.auth.xboxlive.com/xsts/authorize", payload));
            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return (body["Token"]!.ToString(),
                    body["DisplayClaims"]!["xui"]![0]!["uhs"]!.ToString());
        }

        // ── Step 5: Minecraft login ─────────────────────────────────────

        private static async Task<string> McLoginAsync(string uhs, string xstsToken)
        {
            var payload = new JObject
            {
                ["identityToken"] = $"XBL3.0 x={uhs};{xstsToken}"
            };

            var resp = await _http.SendAsync(BuildJson(
                "https://api.minecraftservices.com/authentication/login_with_xbox", payload));
            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return body["access_token"]!.ToString();
        }

        private static async Task<(string uuid, string name)> GetProfileAsync(string accessToken)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get,
                "https://api.minecraftservices.com/minecraft/profile");
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var resp = await _http.SendAsync(msg);
            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return (body["id"]!.ToString(), body["name"]!.ToString());
        }

        private static HttpRequestMessage BuildJson(string url, JObject body)
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
            };
            msg.Headers.TryAddWithoutValidation("Accept", "application/json");
            return msg;
        }
    }
}
