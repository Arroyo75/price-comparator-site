namespace price_comparator_site.Services.Steam.Models
{
    public class SteamPriceOverview
    {
        public int Initial { get; set; }
        public int Final { get; set; }
        public decimal DiscountPercent { get; set; }
    }
}
