namespace RefreshToAccess2.Models
{
    public enum MinecraftSkinVariant
    {
        Classic,
        Slim
    }

    public static class MinecraftSkinVariantExtensions
    {
        public static string ToApiString(this MinecraftSkinVariant variant)
            => variant == MinecraftSkinVariant.Slim ? "slim" : "classic";

        public static MinecraftSkinVariant FromApi(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return MinecraftSkinVariant.Classic;

            return raw.Equals("slim", System.StringComparison.OrdinalIgnoreCase) ||
                   raw.Equals("alex", System.StringComparison.OrdinalIgnoreCase)
                ? MinecraftSkinVariant.Slim
                : MinecraftSkinVariant.Classic;
        }
    }

    public enum PreviewBackgroundMode
    {
        Bright,
        Moody,
        Dark,
        Panorama
    }
}
