using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using price_comparator_site.Data;
using price_comparator_site.Models;
using price_comparator_site.Services.Interfaces;

namespace price_comparator_site.Controllers
{
    public class SearchController : Controller
    {
        private readonly IStoreService _steamService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchController> _logger;
        private readonly IConfiguration _configuration;

        public SearchController(IStoreService steamService, ApplicationDbContext context, ILogger<SearchController> logger, IConfiguration configuration)
        {
            _steamService = steamService;
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchGames(string searchTerm)
        {
            try
            {
                var steamGames = await _steamService.SearchGamesAsync(searchTerm);
                return View("SearchResults", steamGames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching games");
                TempData["Error"] = "Error occurred while searching games.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddGameToDatabase(string storeId)
        {
            try
            {
                var steamStore = await EnsureSteamStoreExists();

                var existingGame = await _context.Games
                    .Include(g => g.Prices)
                    .FirstOrDefaultAsync(g => g.StoreId == storeId);

                if (existingGame != null)
                {
                    if (!existingGame.Prices.Any())
                    {
                        var newPrice = await _steamService.GetGamePriceAsync(storeId);
                        if (newPrice != null)
                        {
                            newPrice.GameId = existingGame.Id;
                            newPrice.StoreId = steamStore.Id;
                            _context.Prices.Add(newPrice);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return RedirectToAction("Details", "Games", new { id = existingGame.Id });
                }

                var gameDetailsTask = _steamService.GetGameDetailsAsync(storeId);
                var gamePriceTask = _steamService.GetGamePriceAsync(storeId);

                await Task.WhenAll(gameDetailsTask, gamePriceTask);

                var gameDetails = await gameDetailsTask;
                var price = await gamePriceTask;

                if (gameDetails == null)
                {
                    TempData["Error"] = "Could not fetch game details.";
                    return RedirectToAction("Index");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Games.Add(gameDetails);
                    await _context.SaveChangesAsync();

                    if (price != null)
                    {
                        price.GameId = gameDetails.Id;
                        price.StoreId = steamStore.Id;
                        _context.Prices.Add(price);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    TempData["Success"] = "Game added successfully.";
                    return RedirectToAction("Details", "Games", new { id = gameDetails.Id });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game with storeId: {StoreId}", storeId);
                TempData["Error"] = "Error occurred while adding game.";
                return RedirectToAction("Index");
            }
        }

        private async Task<Store> EnsureSteamStoreExists()
        {
            var steamStore = await _context.Stores
                .FirstOrDefaultAsync(s => s.Name == "Steam");

            if (steamStore == null)
            {
                steamStore = new Store
                {
                    Name = "Steam",
                    BaseUrl = "https://store.steampowered.com",
                    isActive = true,
                    LogoUrl = "/logos/steamlogo.png",
                    Region = "PL",
                    RequiresAuth = false,
                    ApiKey = _configuration["SteamApi:Key"] ?? ""
                };
                _context.Stores.Add(steamStore);
                await _context.SaveChangesAsync();
            }

            return steamStore;
        }
    }
}
