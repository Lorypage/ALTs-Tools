namespace RefreshToAccess2.Models
{
    public class ProfileData
    {
        public string? IGN       { get; set; }
        public string? RefToken  { get; set; }
        public string? AccToken  { get; set; }
        public string? ClientId  { get; set; }
        public string? UUID      { get; set; }
    }

    public class ProfileDataBlock
    {
        public string?      loginDate      { get; set; }
        public ProfileData? profileData    { get; set; }
        public string?      headSkinBase64 { get; set; }

        // ── Hypixel ban status (nullable so old profiles deserialize cleanly) ──
        /// <summary>"clean" | "temp" | "perm" | "error" | "unknown", or null if never checked.</summary>
        public string?      banStatus      { get; set; }
        /// <summary>Days remaining for a temporary ban.</summary>
        public int?         banDaysLeft    { get; set; }
        /// <summary>ISO-8601 UTC timestamp of the last ban check.</summary>
        public string?      banCheckedUtc  { get; set; }
        /// <summary>Raw disconnect / ban text from the server (for display + debugging parse issues).</summary>
        public string?      banRawMessage  { get; set; }
    }
}
