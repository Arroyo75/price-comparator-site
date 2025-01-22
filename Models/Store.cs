namespace price_comparator_site.Models
{
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public bool isActive { get; set; }
        public string ApiKey { get; set; }
    }
}
