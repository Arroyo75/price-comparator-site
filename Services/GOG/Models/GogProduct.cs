using System.Text.Json;
using System.Text.Json.Serialization;

namespace price_comparator_site.Services.GOG.Models
{
    // Custom converter to handle various release date formats
    public class ReleaseDateConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                // Handle different possible JSON value types
                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                        return null;
                    case JsonTokenType.Number:
                        return reader.GetInt64();
                    case JsonTokenType.String:
                        var dateStr = reader.GetString();
                        if (string.IsNullOrEmpty(dateStr))
                            return null;
                        if (long.TryParse(dateStr, out long result))
                            return result;
                        return null;
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }

    public class GogApiProduct
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        [JsonPropertyName("purchase_link")]
        public string PurchaseLink { get; set; } = "";
        public string Slug { get; set; } = "";
        [JsonPropertyName("content_system_compatibility")]
        public GogSystemCompatibility SystemCompatibility { get; set; } = new();
        public Dictionary<string, string> Languages { get; set; } = new();
        [JsonPropertyName("in_development")]
        public GogDevelopmentInfo InDevelopment { get; set; } = new();
        [JsonPropertyName("is_secret")]
        public bool IsSecret { get; set; }
        [JsonPropertyName("is_installable")]
        public bool IsInstallable { get; set; }
        [JsonPropertyName("game_type")]
        public string GameType { get; set; } = "";
        [JsonPropertyName("is_pre_order")]
        public bool IsPreOrder { get; set; }
        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; } = "";
        public GogImages Images { get; set; } = new();
        public List<object> Dlcs { get; set; } = new();
    }

    public class GogSystemCompatibility
    {
        public bool Windows { get; set; }
        public bool Osx { get; set; }
        public bool Linux { get; set; }
    }

    public class GogDevelopmentInfo
    {
        public bool Active { get; set; }
        public string? Until { get; set; }
    }

    public class GogImages
    {
        public string Background { get; set; } = "";
        public string Logo { get; set; } = "";
        [JsonPropertyName("logo2x")]
        public string Logo2x { get; set; } = "";
        public string Icon { get; set; } = "";
        [JsonPropertyName("sidebarIcon")]
        public string SidebarIcon { get; set; } = "";
        [JsonPropertyName("sidebarIcon2x")]
        public string SidebarIcon2x { get; set; } = "";
        [JsonPropertyName("menuNotificationAv")]
        public string MenuNotificationAv { get; set; } = "";
        [JsonPropertyName("menuNotificationAv2")]
        public string MenuNotificationAv2 { get; set; } = "";
    }

    // Keep this for the search endpoint
    public class GogProductResponse
    {
        public List<GogProduct> Products { get; set; } = new();
        public int TotalProducts { get; set; }
    }

    // Keep existing GogProduct for search results
    public class GogProduct
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Image { get; set; } = "";
        public List<string> Developers { get; set; } = new();
        public List<string> Publishers { get; set; } = new();
        [JsonPropertyName("globalReleaseDate")]
        [JsonConverter(typeof(ReleaseDateConverter))]
        public long? ReleaseDateTimestamp { get; set; }
        public List<string> Genres { get; set; } = new();
        public string Description { get; set; } = "";
        public bool IsDiscounted { get; set; }
        public bool IsInDevelopment { get; set; }
        public GogPrice Price { get; set; } = new();
    }
}
