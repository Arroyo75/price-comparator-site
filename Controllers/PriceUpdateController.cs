using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using price_comparator_site.Data;
using price_comparator_site.Services.Interfaces;
using price_comparator_site.ViewModels;

namespace price_comparator_site.Controllers
{
    public class PriceUpdateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStoreService _steamService;
        private readonly ILogger<PriceUpdateController> _logger;
        
        public PriceUpdateController(ApplicationDbContext context, IStoreService steamService, ILogger<PriceUpdateController> logger)
        {
            _context = context;
            _steamService = steamService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var gamesWithPrices = await _context.Games
                .Include(g => g.Prices)
                .Where(g => g.StoreId != null)
                .Select(g => new GamePriceViewModel
                {
                    Id = g.Id,
                    Name = g.Name,
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
                var game = await _context.Games.Include(g => g.Prices).FirstOrDefaultAsync(g => g.Id == gameId);

                if (game?.StoreId == null)
                {
                    return Json(new { success = false, message = "Game not found" });
                }

                var newPrice = await _steamService.GetGamePriceAsync(game.StoreId);
                if (newPrice != null)
                {
                    // Update existing price or add new one
                    var existingPrice = game.Prices
                        .FirstOrDefault(p => p.StoreUrl.Contains("steampowered.com"));

                    if (existingPrice != null)
                    {
                        existingPrice.CurrentPrice = newPrice.CurrentPrice;
                        existingPrice.OriginalPrice = newPrice.OriginalPrice;
                        existingPrice.DiscountPercentage = newPrice.DiscountPercentage;
                        existingPrice.LastUpdated = DateTime.UtcNow;
                        _context.Update(existingPrice);
                    }
                    else
                    {
                        newPrice.GameId = gameId;
                        _context.Prices.Add(newPrice);
                    }

                    await _context.SaveChangesAsync();
                    return Json(new
                    {
                        success = true,
                        newPrice = newPrice.CurrentPrice,
                        lastUpdated = DateTime.UtcNow
                    });
                }

                return Json(new { success = false, message = "Could not fetch new price" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating price for game {GameId}", gameId);
                return Json(new { success = false, message = "Error updating price" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAllPrices()
        {
            try
            {
                var games = await _context.Games
                    .Where(g => g.StoreId != null)
                    .ToListAsync();

                var results = new List<UpdateResult>();

                foreach (var game in games)
                {
                    try
                    {
                        await Task.Delay(1000);

                        var newPrice = await _steamService.GetGamePriceAsync(game.StoreId);
                        if (newPrice != null)
                        {
                            var existingPrice = await _context.Prices
                                .FirstOrDefaultAsync(p => p.GameId == game.Id &&
                                                        p.StoreUrl.Contains("steampowered.com"));

                            if (existingPrice != null)
                            {
                                existingPrice.CurrentPrice = newPrice.CurrentPrice;
                                existingPrice.OriginalPrice = newPrice.OriginalPrice;
                                existingPrice.DiscountPercentage = newPrice.DiscountPercentage;
                                existingPrice.LastUpdated = DateTime.UtcNow;
                                _context.Update(existingPrice);
                            }
                            else
                            {
                                var steamStore = await _context.Stores
                                    .FirstOrDefaultAsync(s => s.Name == "Steam");
                                if (steamStore != null)
                                {
                                    newPrice.GameId = game.Id;
                                    newPrice.StoreId = steamStore.Id;
                                    _context.Prices.Add(newPrice);
                                }
                            }

                            await _context.SaveChangesAsync();
                            results.Add(new UpdateResult
                            {
                                GameId = game.Id,
                                Success = true,
                                NewPrice = newPrice.CurrentPrice
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating price for game {GameId}", game.Id);
                        results.Add(new UpdateResult
                        {
                            GameId = game.Id,
                            Success = false
                        });
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"Updated {results.Count(r => r.Success)} of {games.Count} games",
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
}
