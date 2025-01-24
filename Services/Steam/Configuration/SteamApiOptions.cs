using Microsoft.IdentityModel.Tokens;

namespace price_comparator_site.Services.Steam.Configuration
{
    public class SteamApiOptions
    {
        public string Key { get; set; }
        public string BaseUrl { get; set; }
        public string Region { get; set; }
    }
}
