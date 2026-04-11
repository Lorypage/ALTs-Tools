using Newtonsoft.Json;
using RefreshToAccess2.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RefreshToAccess2.Crypto;

namespace RefreshToAccess2.Services
{
    public static class ProfileService
    {
        private const int    CryptoKey  = 8964;
        private const bool   LegacyMode = false;

        // ── Registry persistence ───────────────────────────────────────

        public static void Save(List<ProfileDataBlock> profiles)
        {
            try
            {
                string json   = JsonConvert.SerializeObject(profiles);
                string packed = DataPacker.PackData(LegacyMode, json, CryptoKey);
                RegistryService.Write("ProfileDataList", packed);
            }
            catch { /* non-fatal */ }
        }

        /// <summary>
        /// Loads profiles from the registry.
        /// Attempts the current encryption first; if that fails it tries the
        /// legacy format (LegacyMode = true) and re-saves in the new format,
        /// matching the migration logic from the original program.
        /// </summary>
        public static List<ProfileDataBlock>? Load()
        {
            string raw = RegistryService.Read("ProfileDataList");
            if (string.IsNullOrEmpty(raw)) return null;

            // ── Try current format ─────────────────────────────────────
            try
            {
                string json = DataPacker.UnpackData(LegacyMode, raw, CryptoKey);
                return JsonConvert.DeserializeObject<List<ProfileDataBlock>>(json);
            }
            catch { /* fall through to legacy attempt */ }

            // ── Try legacy format and migrate ──────────────────────────
            try
            {
                string json = DataPacker.UnpackData(true, raw, CryptoKey);
                var profiles = JsonConvert.DeserializeObject<List<ProfileDataBlock>>(json);

                if (profiles != null)
                    Save(profiles); // re-save in current format

                return profiles;
            }
            catch
            {
                return null;
            }
        }

        // ── File export ────────────────────────────────────────────────

        /// <summary>
        /// Packs <paramref name="profiles"/> into the binary .tapf format
        /// used by the original program:
        ///   JSON → DataPacker.PackData → Convert.FromBase64String → raw bytes
        /// </summary>
        public static byte[] ExportToBytes(List<ProfileDataBlock> profiles)
        {
            string json   = JsonConvert.SerializeObject(profiles);
            string packed = DataPacker.PackData(LegacyMode, json, CryptoKey);
            return Convert.FromBase64String(packed);
        }

        // ── File import ────────────────────────────────────────────────

        /// <summary>
        /// Reverses <see cref="ExportToBytes"/>:
        ///   raw bytes → Convert.ToBase64String → DataPacker.UnpackData → JSON
        /// </summary>
        public static List<ProfileDataBlock>? ImportFromBytes(byte[] bytes)
        {
            string packed = Convert.ToBase64String(bytes);
            string json   = DataPacker.UnpackData(LegacyMode, packed, CryptoKey);
            return JsonConvert.DeserializeObject<List<ProfileDataBlock>>(json);
        }

        // ── Duplicate removal ──────────────────────────────────────────

        public static List<ProfileDataBlock> RemoveDuplicates(
            List<ProfileDataBlock> source)
        {
            var best = new Dictionary<string, ProfileDataBlock>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var block in source)
            {
                string key = block.profileData?.IGN ?? string.Empty;

                if (!best.TryGetValue(key, out var existing))
                {
                    best[key] = block;
                }
                else
                {
                    if (ParseDate(block.loginDate) > ParseDate(existing.loginDate))
                        best[key] = block;
                }
            }

            return new List<ProfileDataBlock>(best.Values);
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static DateTime ParseDate(string? raw)
        {
            if (DateTime.TryParseExact(
                    raw,
                    @"yyyy/MM/dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime dt))
                return dt;

            return DateTime.MinValue;
        }
    }
}
