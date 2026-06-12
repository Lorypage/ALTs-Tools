using Newtonsoft.Json.Linq;
using RefreshToAccess2.Localization;
using RefreshToAccess2.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RefreshToAccess2.Services
{
    /// <summary>
    /// Stateless Microsoft / Xbox / Minecraft authentication flow.
    /// Progress is reported through the <see cref="IProgress{T}"/> overload.
    /// </summary>
    public static class MSLoginService
    {
        // One shared client – thread-safe for concurrent GET/POST.
        private static readonly HttpClient _http = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        });

        // ── OAuth endpoints ────────────────────────────────────────────

        /// <summary>Desktop redirect registered for the legacy MSA clients.</summary>
        public const string RedirectUri =
            "https://login.live.com/oauth20_desktop.srf";

        private const string AuthorizeEndpoint =
            "https://login.live.com/oauth20_authorize.srf";

        private const string TokenEndpoint =
            "https://login.live.com/oauth20_token.srf";

        /// <summary>
        /// Builds the Microsoft authorization URL the login window navigates to.
        /// On success Microsoft redirects to <see cref="RedirectUri"/> with a
        /// <c>?code=</c> query parameter that <see cref="ExchangeCodeForRefreshTokenAsync"/>
        /// trades for a refresh token.
        /// </summary>
        public static string BuildAuthorizeUrl(ClientIdentification client)
        {
            var query = new Dictionary<string, string>
            {
                ["client_id"]     = client.ClientId,
                ["response_type"] = "code",
                ["redirect_uri"]  = RedirectUri,
                ["scope"]         = client.Scope,
                // Always show the account chooser so a different account can be
                // added even when the embedded browser already has a session.
                ["prompt"]        = "select_account"
            };

            var sb = new StringBuilder(AuthorizeEndpoint);
            sb.Append('?');
            bool first = true;
            foreach (var kv in query)
            {
                if (!first) sb.Append('&');
                first = false;
                sb.Append(Uri.EscapeDataString(kv.Key))
                  .Append('=')
                  .Append(Uri.EscapeDataString(kv.Value));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Exchanges an authorization <paramref name="code"/> (captured from the
        /// login redirect) for a Microsoft refresh token, using the same client
        /// the authorize URL was built with.
        /// </summary>
        public static async Task<string> ExchangeCodeForRefreshTokenAsync(
            string code, ClientIdentification client)
        {
            var form = new Dictionary<string, string>
            {
                ["client_id"]    = client.ClientId,
                ["code"]         = code,
                ["grant_type"]   = "authorization_code",
                ["redirect_uri"] = RedirectUri,
                ["scope"]        = client.Scope
            };

            var resp = await _http.PostAsync(
                TokenEndpoint, new FormUrlEncodedContent(form));

            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return body["refresh_token"]!.ToString();
        }

        // ── Public entry point ─────────────────────────────────────────

        /// <returns>[username, uuid, accessToken]</returns>
        public static async Task<string[]> RequestTokenAsync(
            string refreshToken,
            ClientIdentification client,
            IProgress<string>? progress = null)
        {
            progress?.Report(Loc.T("Login.MsToken"));
            string msToken = await GetMicrosoftTokenAsync(refreshToken, client);

            progress?.Report(Loc.T("Login.XblToken"));
            bool useDPrefix = client.Scope != ClientIdentification.Vanilla.Scope;
            string xblToken = await GetXboxLiveTokenAsync(msToken, useDPrefix);

            progress?.Report(Loc.T("Login.XstsToken"));
            (string xstsToken, string userHash) = await GetXstsTokenAsync(xblToken);

            progress?.Report(Loc.T("Login.AccessToken"));
            string accessToken = await GetAccessTokenAsync(userHash, xstsToken);

            progress?.Report(Loc.T("Login.Profile"));
            (string uuid, string name) = await GetProfileAsync(accessToken);

            return new[] { name, uuid, accessToken };
        }

        // ── Steps ──────────────────────────────────────────────────────

        private static async Task<string> GetMicrosoftTokenAsync(
            string refreshToken, ClientIdentification client)
        {
            var form = new Dictionary<string, string>
            {
                ["client_id"]     = client.ClientId,
                ["refresh_token"] = refreshToken,
                ["grant_type"]    = "refresh_token",
                ["redirect_uri"]  = "https://login.live.com/oauth20_desktop.srf",
                ["scope"]         = client.Scope
            };

            var resp = await _http.PostAsync(
                "https://login.live.com/oauth20_token.srf",
                new FormUrlEncodedContent(form));

            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return body["access_token"]!.ToString();
        }

        private static async Task<string> GetXboxLiveTokenAsync(string msToken, bool useDPrefix)
        {
            var payload = new JObject
            {
                ["Properties"] = new JObject
                {
                    ["AuthMethod"] = "RPS",
                    ["SiteName"]   = "user.auth.xboxlive.com",
                    ["RpsTicket"]  = (useDPrefix ? "d=" : "") + msToken
                },
                ["RelyingParty"] = "http://auth.xboxlive.com",
                ["TokenType"]    = "JWT"
            };

            var msg = BuildJson(HttpMethod.Post,
                "https://user.auth.xboxlive.com/user/authenticate", payload);

            var resp = await _http.SendAsync(msg);
            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return body["Token"]!.ToString();
        }

        private static async Task<(string xstsToken, string userHash)> GetXstsTokenAsync(
            string xblToken)
        {
            var payload = new JObject
            {
                ["Properties"] = new JObject
                {
                    ["UserTokens"] = new JArray(xblToken),
                    ["SandboxId"]  = "RETAIL"
                },
                ["RelyingParty"] = "rp://api.minecraftservices.com/",
                ["TokenType"]    = "JWT"
            };

            var msg = BuildJson(HttpMethod.Post,
                "https://xsts.auth.xboxlive.com/xsts/authorize", payload);

            var resp = await _http.SendAsync(msg);
            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());

            string token    = body["Token"]!.ToString();
            string userHash = body["DisplayClaims"]!["xui"]![0]!["uhs"]!.ToString();
            return (token, userHash);
        }

        private static async Task<string> GetAccessTokenAsync(
            string userHash, string xstsToken)
        {
            var payload = new JObject
            {
                ["identityToken"] = $"XBL3.0 x={userHash};{xstsToken}"
            };

            var msg = BuildJson(HttpMethod.Post,
                "https://api.minecraftservices.com/authentication/login_with_xbox", payload);

            var resp = await _http.SendAsync(msg);
            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return body["access_token"]!.ToString();
        }

        private static async Task<(string uuid, string name)> GetProfileAsync(
            string accessToken)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get,
                "https://api.minecraftservices.com/minecraft/profile");
            msg.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var resp = await _http.SendAsync(msg);
            resp.EnsureSuccessStatusCode();
            var body = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return (body["id"]!.ToString(), body["name"]!.ToString());
        }

        // ── Factory helpers ────────────────────────────────────────────

        private static HttpRequestMessage BuildJson(
            HttpMethod method, string url, JObject body)
        {
            var msg = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(
                    body.ToString(), Encoding.UTF8, "application/json")
            };
            msg.Headers.TryAddWithoutValidation("Accept", "application/json");
            return msg;
        }
    }
}
