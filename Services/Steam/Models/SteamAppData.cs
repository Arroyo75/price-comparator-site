namespace price_comparator_site.Services.Steam.Models
{
    public class SteamAppData
    {
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string HeaderImage { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
        public SteamReleaseDate ReleaseDate { get; set; }
        public SteamPriceOverview PriceOverview { get; set; }
    }
}
