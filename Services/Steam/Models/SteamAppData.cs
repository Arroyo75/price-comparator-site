using System.Text.Json.Serialization;

namespace price_comparator_site.Services.Steam.Models
{
    public class SteamAppData
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public int Steam_appid { get; set; }
        public int Required_age { get; set; }
        public bool Is_free { get; set; }
        public string? Controller_support { get; set; }
        public List<int>? Dlc { get; set; }
        [JsonPropertyName("detailed_description")]
        public string Detailed_description { get; set; } = "";
        [JsonPropertyName("short_description")]
        public string Short_description { get; set; } = "";
        [JsonPropertyName("header_image")]
        public string Header_image { get; set; } = "";
        public List<string>? Developers { get; set; }
        public List<string>? Publishers { get; set; }
        [JsonPropertyName("release_date")]
        public SteamReleaseDate? Release_date { get; set; }
        [JsonPropertyName("price_overview")]
        public SteamPriceOverview? Price_overview { get; set; }
    }
}
