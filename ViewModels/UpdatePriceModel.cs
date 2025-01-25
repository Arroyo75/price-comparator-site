namespace price_comparator_site.ViewModels
{
    public class UpdateResult
    {
        public int GameId { get; set; }
        public bool Success { get; set; }
        public decimal NewPrice { get; set; }
    }
}
