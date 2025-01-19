namespace price_comparator_site.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set;  }
        public string ProductUrl { get; set; }
        public string ImageUrl { get; set; }
        public string StoreSource { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool isAvaliable { get; set; }
        public string Category { get; set; }
        public string Specification { get; set; }
    }
}
