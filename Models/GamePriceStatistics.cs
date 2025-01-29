namespace price_comparator_site.Models
{
    public class GamePriceStatistics
    {
        public string GameName { get; set; } = "";
        public string StoreName { get; set; } = "";
        public decimal PriceUpdatesCount { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal LowestPrice { get; set; }
        public decimal HighestPrice { get; set; }
        public decimal? AverageDiscount { get; set; }
        public decimal MaxDiscount { get; set; }
        public decimal TimesOnSale { get; set; }
    }
}
