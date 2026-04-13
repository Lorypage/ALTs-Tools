using RefreshToAccess2.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RefreshToAccess2.Services
{
    /// <summary>
    /// Fetches Minecraft head skins from Mojang and stores them as
    /// base64 PNG directly in the ProfileDataBlock. No file-based cache.
    /// </summary>
    public static class HeadSkinCacheService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly SemaphoreSlim _gate = new(3);

        static HeadSkinCacheService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("TokenTools/2.2");
        }

        /// <summary>
        /// Returns a frozen BitmapImage of the player's head.
        /// Reads from block.headSkinBase64 if cached, otherwise fetches
        /// from Mojang and writes back into the block.
        /// </summary>
        public static async Task<BitmapImage?> GetHeadAsync(
            ProfileDataBlock block, bool force = false, CancellationToken ct = default)
        {
            // ── Try stored base64 first ────────────────────────────
            if (!force && !string.IsNullOrEmpty(block.headSkinBase64))
            {
                var cached = await Task.Run(() => DecodeBase64(block.headSkinBase64), ct);
                if (cached != null) return cached;
            }

            string? uuid = block.profileData?.UUID;
            if (string.IsNullOrWhiteSpace(uuid)) return null;
            string clean = uuid.Replace("-", "");

            await _gate.WaitAsync(ct);
            try
            {
                // Double-check after acquiring gate (another task may have filled it)
                if (!force && !string.IsNullOrEmpty(block.headSkinBase64))
                {
                    var cached = await Task.Run(() => DecodeBase64(block.headSkinBase64), ct);
                    if (cached != null) return cached;
                }

                string skinUrl = await ResolveSkinUrlAsync(clean, ct);
                if (string.IsNullOrEmpty(skinUrl)) return null;

                byte[] skinBytes = await _http.GetByteArrayAsync(skinUrl, ct);
                byte[]? headPng = await Task.Run(() => CropHead(skinBytes), ct);
                if (headPng == null) return null;

                // Store as base64 in the profile block
                block.headSkinBase64 = Convert.ToBase64String(headPng);

                return await Task.Run(() => DecodeBase64(block.headSkinBase64), ct);
            }
            catch { return null; }
            finally { _gate.Release(); }
        }

        /// <summary>
        /// Clears the cached head so the next GetHeadAsync re-fetches.
        /// </summary>
        public static void Invalidate(ProfileDataBlock block)
        {
            block.headSkinBase64 = null;
        }

        // ── Base64 → frozen BitmapImage ────────────────────────────

        private static BitmapImage? DecodeBase64(string b64)
        {
            try
            {
                byte[] data = Convert.FromBase64String(b64);
                var bi = new BitmapImage();
                using (var ms = new MemoryStream(data))
                {
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.DecodePixelWidth = 64;
                    bi.DecodePixelHeight = 64;
                    bi.EndInit();
                }
                bi.Freeze();
                return bi;
            }
            catch { return null; }
        }

        // ── Mojang session server → skin URL ───────────────────────

        private static async Task<string> ResolveSkinUrlAsync(
            string uuid, CancellationToken ct)
        {
            var resp = await _http.GetAsync(
                $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}", ct);
            if (!resp.IsSuccessStatusCode) return "";

            string json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("properties", out var props))
                return "";

            foreach (var p in props.EnumerateArray())
            {
                if (p.TryGetProperty("name", out var n) &&
                    n.GetString() == "textures" &&
                    p.TryGetProperty("value", out var v))
                {
                    string decoded = Encoding.UTF8.GetString(
                        Convert.FromBase64String(v.GetString()!));
                    using var td = JsonDocument.Parse(decoded);

                    if (td.RootElement.TryGetProperty("textures", out var tex) &&
                        tex.TryGetProperty("SKIN", out var skin) &&
                        skin.TryGetProperty("url", out var url) &&
                        url.ValueKind == JsonValueKind.String)
                    {
                        return url.GetString() ?? "";
                    }
                }
            }
            return "";
        }

        // ── 8×8 face + hat overlay → 64×64 PNG bytes ──────────────

        private static byte[]? CropHead(byte[] skinPng)
        {
            try
            {
                BitmapImage src;
                using (var ms = new MemoryStream(skinPng))
                {
                    src = new BitmapImage();
                    src.BeginInit();
                    src.StreamSource = ms;
                    src.CacheOption = BitmapCacheOption.OnLoad;
                    src.EndInit();
                    src.Freeze();
                }

                var conv = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
                conv.Freeze();

                int w = conv.PixelWidth, h = conv.PixelHeight;
                int stride = w * 4;
                byte[] px = new byte[h * stride];
                conv.CopyPixels(px, stride, 0);

                const int S = 8;
                byte[] face = new byte[S * S * 4];

                // Face region (8,8)→(15,15)
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                        Buffer.BlockCopy(px, ((y + 8) * w + x + 8) * 4,
                                         face, (y * S + x) * 4, 4);

                // Hat overlay (40,8)→(47,15) — alpha blended
                if (w >= 48 && h >= 16)
                {
                    for (int y = 0; y < S; y++)
                        for (int x = 0; x < S; x++)
                        {
                            int si = ((y + 8) * w + x + 40) * 4;
                            int di = (y * S + x) * 4;
                            byte a = px[si + 3];
                            if (a == 0) continue;
                            if (a == 255)
                            {
                                Buffer.BlockCopy(px, si, face, di, 4);
                                continue;
                            }
                            float f = a / 255f, inv = 1f - f;
                            face[di + 0] = (byte)(px[si + 0] * f + face[di + 0] * inv);
                            face[di + 1] = (byte)(px[si + 1] * f + face[di + 1] * inv);
                            face[di + 2] = (byte)(px[si + 2] * f + face[di + 2] * inv);
                            face[di + 3] = 255;
                        }
                }

                // Nearest-neighbour scale 8 → 64
                const int O = 64;
                byte[] big = new byte[O * O * 4];
                for (int y = 0; y < O; y++)
                {
                    int sy = y * S / O;
                    for (int x = 0; x < O; x++)
                    {
                        int sx = x * S / O;
                        Buffer.BlockCopy(face, (sy * S + sx) * 4,
                                         big, (y * O + x) * 4, 4);
                    }
                }

                var bmp = BitmapSource.Create(
                    O, O, 96, 96, PixelFormats.Bgra32, null, big, O * 4);
                bmp.Freeze();

                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bmp));
                using var output = new MemoryStream();
                enc.Save(output);
                return output.ToArray();
            }
            catch { return null; }
        }
    }
}
