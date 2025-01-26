using System.Text.Json.Serialization;

namespace price_comparator_site.Services.GOG.Models
{
    public class GogPriceResponse
    {
        [JsonPropertyName("_embedded")]
        public GogPriceEmbedded? Embedded { get; set; }
    }

    public class GogPriceEmbedded
    {
        public List<GogPriceInfo> Prices { get; set; } = new();
    }

    public class GogPriceInfo
    {
        public GogCurrency Currency { get; set; } = new();
        public string BasePrice { get; set; } = "";
        public string FinalPrice { get; set; } = "";
        public string BonusWalletFunds { get; set; } = "";
    }

    public class GogCurrency
    {
        public string Code { get; set; } = "";
    }
}
