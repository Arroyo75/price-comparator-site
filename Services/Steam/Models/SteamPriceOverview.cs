namespace price_comparator_site.Services.Steam.Models
{
    public class SteamPriceOverview
    {
        public string Currency { get; set; } = "";
        public int Initial { get; set; }
        public int Final { get; set; }
        public int Discount_percent { get; set; }
        public string Initial_formatted { get; set; } = "";
        public string Final_formatted { get; set; } = "";
    }
}
