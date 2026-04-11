using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RefreshToAccess2.Models
{
    public sealed class MinecraftProfileResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("skins")]
        public List<MinecraftProfileSkinResponse> Skins { get; set; } = new();
    }

    public sealed class MinecraftProfileSkinResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("variant")]
        public string Variant { get; set; } = string.Empty;

        [JsonIgnore]
        public MinecraftSkinVariant VariantKind
            => MinecraftSkinVariantExtensions.FromApi(Variant);
    }
}
