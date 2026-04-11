using RefreshToAccess2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RefreshToAccess2.Services
{
    public sealed class NamedPlayerSkinLookupResult
    {
        public string Name { get; init; } = string.Empty;
        public string Id { get; init; } = string.Empty;
        public string SkinUrl { get; init; } = string.Empty;
        public MinecraftSkinVariant Variant { get; init; } = MinecraftSkinVariant.Classic;
    }

    public sealed class MinecraftSkinService
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        static MinecraftSkinService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("TokenTools/1.0");
        }

        public async Task<MinecraftProfileResponse> GetProfileAsync(string accessToken, CancellationToken ct = default)
        {
            using HttpRequestMessage req =
                new(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage resp = await _http.SendAsync(req, ct);
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw BuildApiException(resp, body);

            MinecraftProfileResponse? profile =
                JsonSerializer.Deserialize<MinecraftProfileResponse>(body, _jsonOptions);

            return profile ?? new MinecraftProfileResponse();
        }

        public async Task SetSkinFromUrlAsync(
            string accessToken,
            string imageUrl,
            MinecraftSkinVariant variant,
            CancellationToken ct = default)
        {
            var payload = new
            {
                variant = variant.ToApiString(),
                url = imageUrl
            };

            using HttpRequestMessage req =
                new(HttpMethod.Post, "https://api.minecraftservices.com/minecraft/profile/skins");

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage resp = await _http.SendAsync(req, ct);
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw BuildApiException(resp, body);
        }

        public async Task SetSkinFromFileAsync(
            string accessToken,
            string filePath,
            MinecraftSkinVariant variant,
            CancellationToken ct = default)
        {
            await using FileStream fs = File.OpenRead(filePath);

            using MultipartFormDataContent form = new();
            form.Add(new StringContent(variant.ToApiString()), "variant");

            StreamContent fileContent = new(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            using HttpRequestMessage req =
                new(HttpMethod.Put, "https://api.minecraftservices.com/minecraft/profile/skins");

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            req.Content = form;

            using HttpResponseMessage resp = await _http.SendAsync(req, ct);
            string body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw BuildApiException(resp, body);
        }

        public Task<byte[]> DownloadSkinByUrlAsync(string url, CancellationToken ct = default)
            => _http.GetByteArrayAsync(url, ct);

        public async Task<NamedPlayerSkinLookupResult> LookupPlayerSkinByNameAsync(
            string playerName,
            CancellationToken ct = default)
        {
            playerName = playerName?.Trim() ?? string.Empty;

            if (playerName.Length == 0)
                throw new InvalidOperationException("Enter a Minecraft player name.");

            using HttpRequestMessage nameReq = new(
                HttpMethod.Get,
                $"https://api.mojang.com/users/profiles/minecraft/{Uri.EscapeDataString(playerName)}");

            using HttpResponseMessage nameResp = await _http.SendAsync(nameReq, ct);

            if (nameResp.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
                throw new InvalidOperationException($"Player '{playerName}' was not found.");

            string nameBody = await nameResp.Content.ReadAsStringAsync();

            if (!nameResp.IsSuccessStatusCode)
                throw BuildApiException(nameResp, nameBody);

            MojangNameLookupResponse? lookedUp =
                JsonSerializer.Deserialize<MojangNameLookupResponse>(nameBody, _jsonOptions);

            if (lookedUp == null || string.IsNullOrWhiteSpace(lookedUp.Id))
                throw new InvalidOperationException($"Player '{playerName}' was not found.");

            string resolvedName = string.IsNullOrWhiteSpace(lookedUp.Name)
                ? playerName
                : lookedUp.Name;

            string resolvedId = lookedUp.Id;

            try
            {
                using HttpRequestMessage sessionReq = new(
                    HttpMethod.Get,
                    $"https://sessionserver.mojang.com/session/minecraft/profile/{resolvedId}");

                using HttpResponseMessage sessionResp = await _http.SendAsync(sessionReq, ct);

                if (!sessionResp.IsSuccessStatusCode)
                {
                    return new NamedPlayerSkinLookupResult
                    {
                        Name = resolvedName,
                        Id = resolvedId,
                        SkinUrl = $"https://crafatar.com/skins/{resolvedId}",
                        Variant = MinecraftSkinVariant.Classic
                    };
                }

                string sessionBody = await sessionResp.Content.ReadAsStringAsync();

                SessionProfileResponse? session =
                    JsonSerializer.Deserialize<SessionProfileResponse>(sessionBody, _jsonOptions);

                string skinUrl = $"https://crafatar.com/skins/{resolvedId}";
                MinecraftSkinVariant variant = MinecraftSkinVariant.Classic;

                if (session?.Properties != null)
                {
                    foreach (SessionProperty property in session.Properties)
                    {
                        if (!property.Name.Equals("textures", StringComparison.OrdinalIgnoreCase) ||
                            string.IsNullOrWhiteSpace(property.Value))
                        {
                            continue;
                        }

                        (string parsedUrl, MinecraftSkinVariant parsedVariant) =
                            ParseTexturesProperty(property.Value, resolvedId);

                        if (!string.IsNullOrWhiteSpace(parsedUrl))
                            skinUrl = parsedUrl;

                        variant = parsedVariant;
                        break;
                    }
                }

                return new NamedPlayerSkinLookupResult
                {
                    Name = resolvedName,
                    Id = resolvedId,
                    SkinUrl = skinUrl,
                    Variant = variant
                };
            }
            catch
            {
                return new NamedPlayerSkinLookupResult
                {
                    Name = resolvedName,
                    Id = resolvedId,
                    SkinUrl = $"https://crafatar.com/skins/{resolvedId}",
                    Variant = MinecraftSkinVariant.Classic
                };
            }
        }

        private static (string SkinUrl, MinecraftSkinVariant Variant) ParseTexturesProperty(
            string base64Value,
            string fallbackId)
        {
            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Value));
                using JsonDocument doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("textures", out JsonElement textures))
                    return ($"https://crafatar.com/skins/{fallbackId}", MinecraftSkinVariant.Classic);

                if (!textures.TryGetProperty("SKIN", out JsonElement skin))
                    return ($"https://crafatar.com/skins/{fallbackId}", MinecraftSkinVariant.Classic);

                string skinUrl = $"https://crafatar.com/skins/{fallbackId}";
                MinecraftSkinVariant variant = MinecraftSkinVariant.Classic;

                if (skin.TryGetProperty("url", out JsonElement urlProp) &&
                    urlProp.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(urlProp.GetString()))
                {
                    skinUrl = urlProp.GetString()!;
                }

                if (skin.TryGetProperty("metadata", out JsonElement metadata) &&
                    metadata.TryGetProperty("model", out JsonElement modelProp) &&
                    modelProp.ValueKind == JsonValueKind.String &&
                    string.Equals(modelProp.GetString(), "slim", StringComparison.OrdinalIgnoreCase))
                {
                    variant = MinecraftSkinVariant.Slim;
                }

                return (skinUrl, variant);
            }
            catch
            {
                return ($"https://crafatar.com/skins/{fallbackId}", MinecraftSkinVariant.Classic);
            }
        }

        private static Exception BuildApiException(HttpResponseMessage resp, string body)
        {
            return new InvalidOperationException(
                $"Minecraft Services API error {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        private sealed class MojangNameLookupResponse
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        private sealed class SessionProfileResponse
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public List<SessionProperty> Properties { get; set; } = new();
        }

        private sealed class SessionProperty
        {
            public string Name { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }
}
