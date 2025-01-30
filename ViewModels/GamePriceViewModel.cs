namespace price_comparator_site.ViewModels
{
    public class GamePriceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string StoreName { get; set; } = "";
        public decimal CurrentPrice { get; set; }
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedFormatted =>
            LastUpdated == DateTime.MinValue
                ? "Never"
                : $"{LastUpdated:g} ({(DateTime.Now - LastUpdated).Days} days ago)";
    }
}
