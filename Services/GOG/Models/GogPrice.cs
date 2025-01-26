namespace price_comparator_site.Services.GOG.Models
{
    public class GogPrice
    {
        public string Amount { get; set; } = "";
        public string BaseAmount { get; set; } = "";
        public int DiscountPercentage { get; set; }
        public string Currency { get; set; } = "PLN";
        public bool IsFinal { get; set; }
        public bool IsDiscounted => DiscountPercentage > 0;
    }
}
