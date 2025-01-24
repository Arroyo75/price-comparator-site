using price_comparator_site.Services.Interfaces;
using price_comparator_site.Services.Steam.Models;
using price_comparator_site.Models;

namespace price_comparator_site.Services.Steam
{
    public class SteamService : ISteamService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SteamService> _logger;
        private readonly string _apiKey;

        public SteamService(HttpClient httpClient, IConfiguration configuraion, ILogger<SteamService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuraion;
            _logger = logger;
            _apiKey = _configuration["SteamApi:Key"];
        }

        public async Task<IEnumerable<Game>> SearchGamesAsync(string searchTerm)
        {
            try
            {
                var url = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(searchTerm)}&l=english&cc=PL";
                var response = await _httpClient.GetFromJsonAsync<SteamSearchResponse>(url);

                return response?.Items?.Select(item => new Game
                {
                    Name = item.Name,
                    StoreId = item.Id.ToString(),
                    ImageUrl = item.Tiny_Image,
                    Description = item.Name,
                    Developer = "",
                    Publisher = "",
                    ReleaseDate = DateTime.Now
                }) ?? new List<Game>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Steam games");
                return new List<Game>();
            }
        }

        public async Task<Game> GetGameDetailsAsync(string appId)
        {
            try
            {
                var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=PL";
                var response = await _httpClient.GetFromJsonAsync<Dictionary<string, SteamAppDetails>>(url);

                if (response?.TryGetValue(appId, out var details) == true && details.Success)
                {
                    return new Game
                    {
                        Name = details.Data.Name,
                        StoreId = appId,
                        Description = details.Data.HeaderImage,
                        Developer = details.Data.Developers?.FirstOrDefault() ?? "",
                        Publisher = details.Data.Publishers?.FirstOrDefault() ?? "",
                        ReleaseDate = DateTime.Parse(details.Data.ReleaseDate.Date)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Steam game details");
                return null;
            }
        }
        public async Task<Price> GetGamePriceAsync(string appId)
        {
            try
            {
                var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=PL&filters=price_overview";
                var response = await _httpClient.GetFromJsonAsync<Dictionary<string, SteamAppDetails>>(url);

                if (response?.TryGetValue(appId, out var details) == true && details.Success && details.Data.PriceOverview != null)
                {
                    return new Price
                    {
                        CurrentPrice = details.Data.PriceOverview.Final / 100m,
                        OriginalPrice = details.Data.PriceOverview.Initial / 100m,
                        DiscountPercentage = details.Data.PriceOverview.DiscountPercent,
                        CurrencyCode = "PLN",
                        LastUpdated = DateTime.Now,
                        isAvailable = true,
                        StoreUrl = $"https://store.steampowered.com/app/{appId}"
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Steam game price");
                return null;
            }
        }
    }
}
