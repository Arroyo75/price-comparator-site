namespace price_comparator_site.Models
{
    public class Price
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int? StoreId { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public string CurrencyCode { get; set; } = null!;
        public DateTime LastUpdated { get; set; }
        public bool isAvailable { get; set; }
        public string StoreUrl { get; set; } = null!;

        public virtual Game? Game { get; set; }
        public virtual Store? Store { get; set; }
    }
}
