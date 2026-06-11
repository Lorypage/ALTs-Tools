using Newtonsoft.Json.Linq;
using RefreshToAccess2.Localization;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RefreshToAccess2.Services
{
    public static class IGNRenameService
    {
        private static readonly HttpClient _http = new();

        /// <summary>
        /// Renames the Minecraft profile associated with <paramref name="accessToken"/>.
        /// Throws a descriptive <see cref="Exception"/> on failure.
        /// </summary>
        public static async Task RenameAsync(string newName, string accessToken)
        {
            string url = $"https://api.minecraftservices.com/minecraft/profile/name/{newName}";

            var msg = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            msg.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var resp = await _http.SendAsync(msg);
            int code = (int)resp.StatusCode;
            string body = await resp.Content.ReadAsStringAsync();

            switch (code)
            {
                case 200:
                    return; // success

                case 401:
                    throw new Exception(Loc.T("RenameSvc.InvalidToken"));

                case 429:
                    throw new Exception(Loc.T("RenameSvc.TooOften"));

                case 400:
                    throw new Exception(Loc.T("RenameSvc.InvalidFormat"));

                default:
                    if (body.Contains("FORBIDDEN"))
                        throw new Exception(Loc.T("RenameSvc.Wait30Days"));
                    if (body.Contains("DUPLICATE"))
                        throw new Exception(Loc.T("RenameSvc.Taken"));
                    if (body.Contains("NOT_ALLOWED"))
                        throw new Exception(Loc.T("RenameSvc.NotAllowed"));
                    throw new Exception(Loc.T("RenameSvc.Unexpected", code, body));
            }
        }
    }
}
