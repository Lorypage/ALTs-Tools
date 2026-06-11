using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace RefreshToAccess2.Crypto
{
    /// <summary>
    /// Determines the expiry of a pasted credential. Tries, in order:
    /// 1) a JWT embedded anywhere in the text (reads its <c>exp</c> claim);
    /// 2) a Microsoft login cookie dump in JSON-array, Netscape, or
    ///    Set-Cookie header form (reads the largest auth-cookie expiry).
    /// Ported from the project's token-expiry JS helper.
    /// </summary>
    public static class TokenExpiry
    {
        public sealed record ExpiryInfo(long Exp, DateTime ExpiryLocal, bool Expired, TimeSpan Remaining);

        // Auth cookie names worth preferring, lowest-priority last.
        private static readonly string[] AuthNames =
        {
            "estsauthpersistent", "estsauth", "__host-msaauth",
            "__host-msaauthp", "__secure-1psid", "estsauthlight"
        };

        private static readonly Regex JwtRegex =
            new(@"ey[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]+", RegexOptions.Compiled);

        private static readonly Regex SetCookieRegex = new(
            @"([A-Za-z0-9._\-]+)=[^;]*;\s*expires=([A-Za-z]{3},\s*\d{1,2}[\s-][A-Za-z]{3}[\s-]\d{2,4}\s+\d{2}:\d{2}:\d{2}\s*GMT)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Returns the unix-seconds expiry of <paramref name="code"/>, or null
        /// if neither a JWT nor a recognizable cookie expiry is found.
        /// </summary>
        public static long? ParseTokenExp(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            return ParseJwtExp(code) ?? ParseCookieExp(code);
        }

        /// <summary>Builds a description (absolute time + remaining/elapsed) from a unix-seconds expiry.</summary>
        public static ExpiryInfo? Describe(long? expSec)
        {
            if (expSec is null or <= 0) return null;

            var expiryUtc = DateTimeOffset.FromUnixTimeSeconds(expSec.Value);
            var now = DateTimeOffset.UtcNow;
            return new ExpiryInfo(
                Exp: expSec.Value,
                ExpiryLocal: expiryUtc.LocalDateTime,
                Expired: expiryUtc < now,
                Remaining: expiryUtc - now);
        }

        // ── base64url decode (pad + url-safe → standard) ──
        private static string B64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            s += "===".Substring((s.Length + 3) % 4);
            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }

        // ── JWT: regex-match anywhere, decode middle segment, read exp ──
        private static long? ParseJwtExp(string code)
        {
            var m = JwtRegex.Match(code);
            if (!m.Success) return null;

            try
            {
                string payload = B64UrlDecode(m.Value.Split('.')[1]);
                var obj = JObject.Parse(payload);
                var exp = obj["exp"];
                if (exp != null && exp.Type == JTokenType.Integer)
                    return exp.Value<long>();
            }
            catch { /* malformed payload — fall through */ }

            return null;
        }

        // ── Microsoft login cookie dump → largest relevant expiry ──
        private static long? ParseCookieExp(string code)
        {
            var all = new System.Collections.Generic.List<(string name, long exp)>();
            var auth = new System.Collections.Generic.List<(string name, long exp)>();

            void Add(string? name, double exp)
            {
                name = (name ?? "").ToLowerInvariant();
                if (double.IsNaN(exp) || double.IsInfinity(exp) || exp <= 0) return;
                var entry = (name, (long)Math.Floor(exp));
                all.Add(entry);
                if (Array.IndexOf(AuthNames, name) >= 0) auth.Add(entry);
            }

            string s = code.Trim();

            // Form 1: JSON array [{name, expirationDate, ...}, ...]
            if (s.Length > 0 && s[0] == '[')
            {
                try
                {
                    var arr = JArray.Parse(s);
                    foreach (var c in arr)
                    {
                        if (c is JObject o)
                        {
                            var exp = o["expirationDate"] ?? o["expires"] ?? o["exp"];
                            if (exp != null && (exp.Type == JTokenType.Integer || exp.Type == JTokenType.Float))
                                Add(o["name"]?.ToString(), exp.Value<double>());
                        }
                    }
                }
                catch { /* not valid JSON — try next form */ }
            }

            // Form 2: Netscape tab-separated lines
            if (all.Count == 0)
            {
                foreach (var line in s.Split('\n'))
                {
                    var l = line.TrimEnd('\r');
                    if (l.Length == 0 || l[0] == '#') continue;
                    var cols = l.Split('\t');
                    if (cols.Length >= 7 && double.TryParse(cols[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var e))
                        Add(cols[5], e);
                }
            }

            // Form 3: Set-Cookie header style "name=val; expires=..."
            if (all.Count == 0)
            {
                foreach (Match m in SetCookieRegex.Matches(s))
                {
                    if (DateTime.TryParse(m.Groups[2].Value, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
                    {
                        Add(m.Groups[1].Value, new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeSeconds());
                    }
                }
            }

            if (all.Count == 0) return null;

            // Prefer ESTSAUTHPERSISTENT, then any auth cookie, then any cookie.
            foreach (var c in auth)
                if (c.name == "estsauthpersistent") return c.exp;

            long Max(System.Collections.Generic.List<(string name, long exp)> list)
            {
                long max = long.MinValue;
                foreach (var c in list) if (c.exp > max) max = c.exp;
                return max;
            }

            return auth.Count > 0 ? Max(auth) : Max(all);
        }
    }
}
