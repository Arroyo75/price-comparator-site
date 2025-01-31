using Microsoft.AspNetCore.Mvc;
using price_comparator_site.Data;
using price_comparator_site.Services.Interfaces;
using price_comparator_site.ViewModels;
using Microsoft.EntityFrameworkCore;

public class PriceUpdateController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<IStoreService> _storeServices;
    private readonly ILogger<PriceUpdateController> _logger;

    public PriceUpdateController(
        ApplicationDbContext context,
        IEnumerable<IStoreService> storeServices,
        ILogger<PriceUpdateController> logger)
    {
        _context = context;
        _storeServices = storeServices;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var gamesWithPrices = await _context.Games
            .Include(g => g.Prices)
                .ThenInclude(p => p.Store)  // Include Store to get store names
            .Where(g => g.StoreId != null)
            .Select(g => new GamePriceViewModel
            {
                Id = g.Id,
                Name = g.Name,
                StoreName = g.Prices
                    .OrderByDescending(p => p.LastUpdated)
                    .Select(p => p.Store.Name)
                    .FirstOrDefault() ?? "Unknown",
                CurrentPrice = g.Prices
                    .OrderByDescending(p => p.LastUpdated)
                    .Select(p => p.CurrentPrice)
                    .FirstOrDefault(),
                LastUpdated = g.Prices
                    .OrderByDescending(p => p.LastUpdated)
                    .Select(p => p.LastUpdated)
                    .FirstOrDefault()
            })
            .ToListAsync();

        foreach (var game in gamesWithPrices)
        {
            if (game.LastUpdated == default(DateTime))
            {
                game.LastUpdated = DateTime.MinValue;
            }
        }

        return View(gamesWithPrices);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("[controller]/UpdatePrice/{gameId}")]
    public async Task<IActionResult> UpdatePrice(int gameId)
    {
        try
        {
            var game = await _context.Games
                .Include(g => g.Prices)
                    .ThenInclude(p => p.Store)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game?.StoreId == null)
            {
                return Json(new { success = false, message = "Game not found" });
            }

            // Find all store services that have prices for this game
            var stores = game.Prices.Select(p => p.Store).ToList();
            var updatedPrices = new List<(decimal price, DateTime updated)>();

            foreach (var store in stores)
            {
                var storeService = _storeServices.FirstOrDefault(s => s.StoreName == store.Name);
                if (storeService == null) continue;

                var existingPrice = game.Prices.FirstOrDefault(p => p.StoreId == store.Id);
                if (existingPrice == null) continue;

                try
                {
                    var newPrice = await storeService.GetGamePriceAsync(game.StoreId, false);
                    if (newPrice != null)
                    {
                        existingPrice.CurrentPrice = newPrice.CurrentPrice;
                        existingPrice.OriginalPrice = newPrice.OriginalPrice;
                        existingPrice.DiscountPercentage = newPrice.DiscountPercentage;
                        existingPrice.LastUpdated = DateTime.UtcNow;
                        existingPrice.StoreUrl = newPrice.StoreUrl;
                        existingPrice.isAvailable = newPrice.isAvailable;

                        _context.Update(existingPrice);
                        updatedPrices.Add((newPrice.CurrentPrice, DateTime.UtcNow));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating price for game {GameId} in store {StoreName}",
                        gameId, store.Name);
                }
            }

            if (updatedPrices.Any())
            {
                await _context.SaveChangesAsync();
                var firstUpdate = updatedPrices.First();
                return Json(new
                {
                    success = true,
                    newPrice = firstUpdate.price,
                    lastUpdated = firstUpdate.updated
                });
            }

            return Json(new { success = false, message = "Could not fetch new prices" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prices for game {GameId}", gameId);
            return Json(new { success = false, message = "Error updating prices" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAllPrices()
    {
        try
        {
            var games = await _context.Games
                .Include(g => g.Prices)
                    .ThenInclude(p => p.Store)
                .Where(g => g.StoreId != null)
                .ToListAsync();

            var results = new List<UpdateResult>();

            foreach (var game in games)
            {
                // Add delay to avoid rate limiting
                await Task.Delay(1000);

                foreach (var price in game.Prices)
                {
                    var storeService = _storeServices.FirstOrDefault(s => s.StoreName == price.Store.Name);
                    if (storeService == null) continue;

                    try
                    {

                        string? storeSpecificId = null;

                        if (price.Store.Name == "Steam")
                        {
                            storeSpecificId = game.StoreId;
                        }
                        else if (price.Store.Name == "GOG")
                        {
                            storeSpecificId = game.GogId;
                        }

                        if (string.IsNullOrEmpty(storeSpecificId))
                        {
                            _logger.LogWarning("No store ID found for game {GameId} in store {StoreName}",
                                game.Id, price.Store.Name);
                            continue;
                        }

                        var newPrice = await storeService.GetGamePriceAsync(storeSpecificId, false);
                        if (newPrice != null)
                        {
                            price.CurrentPrice = newPrice.CurrentPrice;
                            price.OriginalPrice = newPrice.OriginalPrice;
                            price.DiscountPercentage = newPrice.DiscountPercentage;
                            price.LastUpdated = DateTime.UtcNow;
                            price.StoreUrl = newPrice.StoreUrl;
                            price.isAvailable = newPrice.isAvailable;

                            _context.Update(price);
                            await _context.SaveChangesAsync();

                            results.Add(new UpdateResult
                            {
                                GameId = game.Id,
                                Success = true,
                                NewPrice = newPrice.CurrentPrice,
                                StoreName = price.Store.Name
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating price for game {GameId} in store {StoreName}",
                            game.Id, price.Store.Name);
                        results.Add(new UpdateResult
                        {
                            GameId = game.Id,
                            Success = false,
                            StoreName = price.Store.Name
                        });
                    }
                }
            }

            var successfulUpdates = results.Count(r => r.Success);
            var totalPrices = games.Sum(g => g.Prices.Count());

            return Json(new
            {
                success = true,
                message = $"Updated {successfulUpdates} of {totalPrices} prices",
                updates = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk price update");
            return Json(new { success = false, message = "Error updating prices" });
        }
    }
}