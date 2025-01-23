namespace price_comparator_site.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? StoreId { get; set; }
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string Developer { get; set; } = "";
        public string? Publisher { get; set; }
        public DateTime ReleaseDate { get; set; }
        public virtual ICollection<Price>? Prices { get; set; }
    }
}
