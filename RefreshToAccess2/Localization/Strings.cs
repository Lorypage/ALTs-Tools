using System.Collections.Generic;

namespace RefreshToAccess2.Localization
{
    /// <summary>
    /// Localized string tables. <see cref="En"/> and <see cref="Zh"/> are
    /// assembled from partial contributions across Strings.*.cs files.
    /// Keys are grouped by feature: Nav.*, Common.*, Converter.*, AltMgr.*,
    /// Skin.*, Rename.*, Inject.*, Settings.*, Msg.*, Status.*.
    /// </summary>
    public static partial class Strings
    {
        public static readonly Dictionary<string, string> En = new();
        public static readonly Dictionary<string, string> Zh = new();

        static Strings()
        {
            AddCommonEn(En);
            AddConverterEn(En);
            AddAltMgrEn(En);
            AddSkinEn(En);
            AddMiscEn(En);

            AddCommonZh(Zh);
            AddConverterZh(Zh);
            AddAltMgrZh(Zh);
            AddSkinZh(Zh);
            AddMiscZh(Zh);
        }
    }
}
