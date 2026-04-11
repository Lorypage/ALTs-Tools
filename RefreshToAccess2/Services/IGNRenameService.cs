using Newtonsoft.Json.Linq;
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
                    throw new Exception("Invalid or expired access token.");

                case 429:
                    throw new Exception(
                        "You are changing your name too often – wait a moment and try again.");

                case 400:
                    throw new Exception("Invalid name format.");

                default:
                    if (body.Contains("FORBIDDEN"))
                        throw new Exception(
                            "You must wait 30 days before changing your name again.");
                    if (body.Contains("DUPLICATE"))
                        throw new Exception("That name is already taken.");
                    if (body.Contains("NOT_ALLOWED"))
                        throw new Exception("That name is not allowed.");
                    throw new Exception($"Unexpected response ({code}): {body}");
            }
        }
    }
}
