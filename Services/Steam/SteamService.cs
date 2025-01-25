﻿using price_comparator_site.Services.Interfaces;
using price_comparator_site.Services.Steam.Models;
using price_comparator_site.Models;
using System.Text.Json;

namespace price_comparator_site.Services.Steam
{
    public class SteamService : IStoreService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SteamService> _logger;
        private readonly string _apiKey;

        public string StoreName => "Steam";

        public SteamService(HttpClient httpClient, IConfiguration configuraion, ILogger<SteamService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuraion;
            _logger = logger;
            _apiKey = _configuration["SteamApi:Key"] ?? throw new InvalidOperationException("Steam API key not found in configuration"); ;
        }

        public async Task<IEnumerable<Game>> SearchGamesAsync(string searchTerm)
        {
            try
            {
                var url = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(searchTerm)}&l=english&cc=PL";
                var response = await _httpClient.GetFromJsonAsync<SteamSearchResponse>(url);

                if (response.Items == null)
                {
                    _logger.LogWarning("No items found in Steam search response");
                    return Enumerable.Empty<Game>();
                }

                return response.Items.Select(item => new Game
                {
                    Name = item.Name ?? "Unknown",
                    StoreId = item.Id.ToString(),
                    ImageUrl = item.Tiny_Image ?? "",
                    Description = item.Name ?? "Unknown",
                    Developer = "",
                    Publisher = "",
                    ReleaseDate = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Steam games");
                return Enumerable.Empty<Game>();
            }
        }

        public async Task<Game?> GetGameDetailsAsync(string appId)
        {
            try
            {
                var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=PL";
                var responseString = await _httpClient.GetStringAsync(url);

                _logger.LogInformation("Raw Steam response: {Response}", responseString);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var response = JsonSerializer.Deserialize<Dictionary<string, SteamAppDetails>>(responseString, options);

                if (!response.TryGetValue(appId, out var details) || !details.Success || details.Data == null)
                {
                    _logger.LogWarning("Failed to get details for Steam game: {AppId}", appId);
                    return null;
                }

                var description = details.Data.Short_description ?? details.Data.Detailed_description ?? "No description available";
                if (description.Length > 500)
                {
                    description = description.Substring(0, 497) + "...";
                }

                return new Game
                {
                    Name = details.Data.Name,
                    StoreId = appId,
                    Description = description,
                    ImageUrl = details.Data.Header_image ??
                              $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg",
                    Developer = details.Data.Developers?.FirstOrDefault() ?? "",
                    Publisher = details.Data.Publishers?.FirstOrDefault() ?? "",
                    ReleaseDate = details.Data.Release_date?.Date != null
                        ? DateTime.TryParse(details.Data.Release_date.Date, out var date) ? date : DateTime.Now
                        : DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Steam game details");
                return null;
            }
        }
        public async Task<Price?> GetGamePriceAsync(string appId)
        {
            try
            {
                var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=PL&filters=price_overview";
                var responseString = await _httpClient.GetStringAsync(url);

                _logger.LogInformation("Raw Steam price response: {Response}", responseString);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var response = JsonSerializer.Deserialize<Dictionary<string, SteamAppDetails>>(responseString, options);

                if (!response.TryGetValue(appId, out var details) || !details.Success)
                {
                    return new Price
                    {
                        isAvailable = false,
                        LastUpdated = DateTime.UtcNow,
                        StoreUrl = $"https://store.steampowered.com/app/{appId}"
                    };
                }

                if (details.Data?.Price_overview == null)
                {
                    return new Price
                    {
                        CurrentPrice = 0,
                        OriginalPrice = 0,
                        DiscountPercentage = 0,
                        CurrencyCode = "PLN",
                        LastUpdated = DateTime.UtcNow,
                        isAvailable = !details.Data.Is_free,
                        StoreUrl = $"https://store.steampowered.com/app/{appId}"
                    };
                }

                var priceOverview = details.Data.Price_overview;

                _logger.LogInformation(
                    "Processing price for game {AppId}: Initial={Initial}, Final={Final}, Discount={Discount}",
                    appId,
                    priceOverview.Initial,
                    priceOverview.Final,
                    priceOverview.Discount_percent
                );

                return new Price
                {
                    CurrentPrice = priceOverview.Final / 100m,
                    OriginalPrice = priceOverview.Initial / 100m,
                    DiscountPercentage = priceOverview.Discount_percent,
                    CurrencyCode = priceOverview.Currency ?? "PLN",
                    LastUpdated = DateTime.UtcNow,
                    isAvailable = true,
                    StoreUrl = $"https://store.steampowered.com/app/{appId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Steam game price: {Message}", ex.Message);
                return null;
            }
        }
    }
}
