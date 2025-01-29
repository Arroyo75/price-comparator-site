using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using price_comparator_site.Models;
using price_comparator_site.Services.GOG.Models;
using price_comparator_site.Services.Interfaces;

namespace price_comparator_site.Services.GOG
{
    public class GogService : IStoreService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GogService> _logger;

        public string StoreName => "GOG";

        public GogService(HttpClient httpClient, ILogger<GogService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://www.gog.com/");
        }

        public async Task<IEnumerable<Game>> SearchGamesAsync(string searchTerm)
        {
            try
            {
                // Construct the search URL with proper encoding
                var url = $"https://www.gog.com/games/ajax/filtered?mediaType=game&search={Uri.EscapeDataString(searchTerm)}&language=en-US&country=PL";
                var responseString = await _httpClient.GetStringAsync(url);

                _logger.LogInformation("GOG search response received for term: {SearchTerm}", searchTerm);
                _logger.LogDebug("Response content: {Response}", responseString);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<GogProductResponse>(responseString, options);

                if (response?.Products == null || !response.Products.Any())
                {
                    _logger.LogWarning("No games found on GOG for search term: {SearchTerm}", searchTerm);
                    return Enumerable.Empty<Game>();
                }

                var games = new List<Game>();
                foreach (var product in response.Products)
                {
                    try
                    {
                        // Convert the nullable timestamp to a DateTime
                        var releaseDate = product.ReleaseDateTimestamp.HasValue
                            ? DateTimeOffset.FromUnixTimeSeconds(product.ReleaseDateTimestamp.Value).DateTime
                            : DateTime.Now;

                        var game = new Game
                        {
                            Name = product.Title?.Trim() ?? "Unknown Title",
                            StoreId = product.Id.ToString(),
                            Description = product.Title?.Trim() ?? "No description available",
                            ImageUrl = FixImageUrl(product.Image),
                            Developer = product.Developers?.FirstOrDefault()?.Trim() ?? "",
                            Publisher = product.Publishers?.FirstOrDefault()?.Trim(),
                            ReleaseDate = releaseDate,
                            Prices = new List<Price>()
                        };

                        games.Add(game);
                        _logger.LogInformation("Successfully processed game: {Title}", game.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing GOG product: {Title}", product.Title);
                        // Continue with next product
                        continue;
                    }
                }

                return games;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching GOG games: {Message}", ex.Message);
                return Enumerable.Empty<Game>();
            }
        }

        public async Task<Game?> GetGameDetailsAsync(string storeId)
        {
            try
            {
                if (!int.TryParse(storeId, out var gogId))
                {
                    _logger.LogWarning("Invalid GOG ID format: {StoreId}", storeId);
                    return null;
                }

                // Get game details from the products endpoint
                var productUrl = $"https://api.gog.com/products/{gogId}";
                var productResponse = await _httpClient.GetStringAsync(productUrl);
                _logger.LogInformation("GOG product response received for ID: {StoreId}", storeId);
                _logger.LogDebug("Response content: {Response}", productResponse);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                // Deserialize directly to GogApiProduct since the API returns a single product
                var product = JsonSerializer.Deserialize<GogApiProduct>(productResponse, options);

                if (product == null)
                {
                    _logger.LogWarning("Could not deserialize GOG game with ID: {StoreId}", storeId);
                    return null;
                }

                // Try to parse the release date
                DateTime releaseDate;
                if (!DateTime.TryParse(product.ReleaseDate, out releaseDate))
                {
                    releaseDate = DateTime.Now;
                }

                // Create our Game model from the API response
                return new Game
                {
                    Name = product.Title.Trim(),
                    StoreId = product.Id.ToString(),
                    Description = $"{product.Title.Trim()} - {(product.IsPreOrder ? "Pre-order" : "Available")}",
                    ImageUrl = FixImageUrl(product.Images.Background),
                    Developer = "",
                    Publisher = "",
                    ReleaseDate = releaseDate,
                    Prices = new List<Price>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GOG game details: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<Price?> GetGamePriceAsync(string storeId, bool? isNewGame)
        {
            try
            {
                if (!int.TryParse(storeId, out var gogId))
                {
                    _logger.LogWarning("Invalid GOG ID for price lookup: {StoreId}", storeId);
                    return null;
                }

                var url = $"https://api.gog.com/products/{gogId}/prices?countryCode=PL";
                var responseString = await _httpClient.GetStringAsync(url);

                _logger.LogInformation("GOG Price response received for ID: {StoreId}", storeId);
                _logger.LogInformation("Response content: {Response}", responseString);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<GogPriceResponse>(responseString, options);

                if (response?.Embedded?.Prices == null || !response.Embedded.Prices.Any())
                {
                    return new Price
                    {
                        isAvailable = false,
                        LastUpdated = DateTime.UtcNow,
                        CurrencyCode = "PLN"  // Set default currency even when unavailable
                    };
                }

                var priceInfo = response.Embedded.Prices.First();

                // Parse the price strings - they come in format "3999 PLN"
                if (!TryParseGogPrice(priceInfo.FinalPrice, out decimal finalPrice) ||
                    !TryParseGogPrice(priceInfo.BasePrice, out decimal basePrice))
                {
                    _logger.LogWarning("Failed to parse price values for game {StoreId}", storeId);
                    return null;
                }

                // Calculate discount percentage
                var discountPercentage = 0;
                if (basePrice > 0)
                {
                    discountPercentage = (int)Math.Round((1 - (finalPrice / basePrice)) * 100);
                }

                var price = new Price
                {
                    CurrentPrice = finalPrice,
                    OriginalPrice = basePrice,
                    DiscountPercentage = discountPercentage,
                    CurrencyCode = "PLN",  // Always use PLN for consistency
                    LastUpdated = DateTime.UtcNow,
                    isAvailable = true
                };

                if(isNewGame == true)
                {
                    var detailsUrl = $"https://api.gog.com/products/{gogId}";
                    var detailsResponse = await _httpClient.GetStringAsync(detailsUrl);

                    var details = JsonSerializer.Deserialize<GogApiProduct>(detailsResponse, options);

                    if (details != null)
                    {
                        price.StoreUrl = $"https://www.gog.com/game/{details.Slug}";
                    }
                    else
                    {
                        _logger.LogWarning("Could not get game details for store URL: {StoreId}", storeId);
                        price.StoreUrl = $"https://www.gog.com/game/{storeId}";
                    }
                } else
                {
                    price.StoreUrl = $"https://www.gog.com/game/{storeId}";
                }

                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GOG game price: {Message}", ex.Message);
                return null;
            }
        }

        private bool TryParseGogPrice(string priceString, out decimal price)
        {
            // GOG returns prices in format "3999 PLN" where 3999 means 39.99 PLN
            price = 0m;

            if (string.IsNullOrEmpty(priceString))
                return false;

            // Extract just the number part
            var parts = priceString.Split(' ');
            if (parts.Length == 0)
                return false;

            // Parse the number and convert from smallest currency unit (groszy) to PLN
            if (int.TryParse(parts[0], out int smallestUnit))
            {
                price = smallestUnit / 100m;  // Convert to decimal zloty
                return true;
            }

            return false;
        }

        private string FixImageUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return "";
            return imageUrl.StartsWith("//") ? $"https:{imageUrl}" : imageUrl;
        }
    }
}