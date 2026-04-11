using System;

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
        public string?      loginDate   { get; set; }
        public ProfileData? profileData { get; set; }
    }
}
