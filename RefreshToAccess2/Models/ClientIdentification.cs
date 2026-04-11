namespace RefreshToAccess2.Models
{
    public class ClientIdentification
    {
        public string ClientId { get; set; }
        public string Scope    { get; set; }

        public ClientIdentification(string clientId, string scope)
        {
            ClientId = clientId;
            Scope    = scope;
        }

        // ── Presets ────────────────────────────────────────────────────
        public static readonly ClientIdentification Vanilla =
            new("00000000402b5328", "service::user.auth.xboxlive.com::MBI_SSL");

        public static readonly ClientIdentification TziChecker =
            new("00000000402b5328", "service::user.auth.xboxlive.com::MBI_SSL");

        public static readonly ClientIdentification MalChecker =
            new("000000004c12ae6f", "service::user.auth.xboxlive.com::MBI_SSL");

        public static readonly ClientIdentification HMCL =
            new("6a3728d6-27a3-4180-99bb-479895b8f88e", "XboxLive.signin offline_access");

        public static readonly ClientIdentification PCL =
            new("fe72edc2-3a6f-4280-90e8-e2beb64ce7e1", "XboxLive.signin offline_access");

        public static readonly ClientIdentification Essential =
            new("e39cc675-eb52-4475-b5f8-82aaae14eeba", "Xboxlive.signin Xboxlive.offline_access");

        public static readonly ClientIdentification InGameAccountSwitcher =
            new("54fd49e4-2103-4044-9603-2b028c814ec3", "XboxLive.signin offline_access");

        public static readonly ClientIdentification KSYZ_AltManager =
            new("42a60a84-599d-44b2-a7c6-b00cdef1d6a2", "XboxLive.signin offline_access");

        public static readonly ClientIdentification BakaXL =
            new("e847355e-7e50-4859-b062-0e12640b9d8d", "XboxLive.signin offline_access");

        public static readonly ClientIdentification LabyMod =
            new("8058f65d-ce06-4c30-9559-473c9275a65d", "XboxLive.signin offline_access");
    }
}
