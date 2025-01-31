using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using price_comparator_site.Data;
using price_comparator_site.Models;
using price_comparator_site.Services.Interfaces;
using price_comparator_site.Utils.GameMatching;
using price_comparator_site.ViewModels;

namespace price_comparator_site.Controllers
{
    public class SearchController : Controller
    {
        private readonly IEnumerable<IStoreService> _storeServices;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            IEnumerable<IStoreService> storeServices,
            ApplicationDbContext context,
            ILogger<SearchController> logger)
        {
            _storeServices = storeServices;
            _context = context;
            _logger = logger;
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
                var searchTasks = _storeServices.Select(async service =>
                {
                    var games = await service.SearchGamesAsync(searchTerm);
                    return games.Select(game => new GameSearchResult
                    {
                        Game = game,
                        StoreName = service.StoreName
                    });
                });

                var results = await Task.WhenAll(searchTasks);
                var allGames = results.SelectMany(x => x).ToList();

                _logger.LogInformation("Found {Count} games across all stores", allGames.Count);
                return View("SearchResults", allGames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching games");
                TempData["Error"] = "Error occurred while searching games.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddGameToDatabase(string storeId, string storeName)
        {
            try
            {
                //First, get the service for the store where the user found the game
                var primaryStoreService = _storeServices.FirstOrDefault(s =>
                    s.StoreName.Equals(storeName, StringComparison.OrdinalIgnoreCase));

                if (primaryStoreService == null)
                {
                    TempData["Error"] = "Store service not found.";
                    return RedirectToAction("Index");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    //get the initial game details from the primary store
                    var gameDetails = await primaryStoreService.GetGameDetailsAsync(storeId);
                    if (gameDetails == null)
                    {
                        TempData["Error"] = "Could not fetch game details.";
                        return RedirectToAction("Index");
                    }

                    var existingGames = await _context.Games
                        .Include(g => g.Prices)
                        .Select(g => new { g.Id, g.Name, Game = g })
                        .ToListAsync();

                    var existingGame = existingGames
                        .FirstOrDefault(g => GameNameMatcher.AreGamesMatching(g.Name, gameDetails.Name))
                        ?.Game;

                    Game game = existingGame ?? gameDetails;
                    if (existingGame == null)
                    {

                        if (storeName == "Steam")
                        {
                            game.StoreId = storeId;
                            game.GogId = null;
                        }
                        else if (storeName == "GOG")
                        {
                            game.StoreId = null;
                            game.GogId = storeId;
                        }

                        _context.Games.Add(game);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        existingGame.Description = gameDetails.Description;
                        existingGame.ImageUrl = gameDetails.ImageUrl;
                        existingGame.Developer = gameDetails.Developer;
                        existingGame.Publisher = gameDetails.Publisher;
                        _context.Update(existingGame);
                        await _context.SaveChangesAsync();
                    }

                    //Handling prices from other stores
                    foreach (var storeService in _storeServices)
                    {
                        try
                        {
                            var store = await GetOrCreateStore(storeService.StoreName);

                            string? priceStoreId = null;

                            if (storeService.StoreName == storeName)
                            {
                                //For the primary store, use the storeId we already have
                                priceStoreId = storeId;
                            }
                            else
                            {
                                var searchResults = await storeService.SearchGamesAsync(game.Name);
                                var matchingGame = FindBestMatch(searchResults, game.Name);

                                if (matchingGame != null)
                                {
                                    priceStoreId = matchingGame.StoreId;

                                    if (storeService.StoreName == "Steam")
                                    {
                                        game.StoreId = matchingGame.StoreId;
                                    }
                                    else if (storeService.StoreName == "GOG")
                                    {
                                        game.GogId = matchingGame.StoreId;
                                    }

                                    _context.Update(game);
                                    await _context.SaveChangesAsync();
                                }
                            }

                            if (priceStoreId != null)
                            {
                                var price = await storeService.GetGamePriceAsync(priceStoreId, true);
                                if (price != null)
                                {
                                    var existingPrice = game.Prices
                                        .FirstOrDefault(p => p.StoreId == store.Id);

                                    if (existingPrice != null)
                                    {
                                        existingPrice.CurrentPrice = price.CurrentPrice;
                                        existingPrice.OriginalPrice = price.OriginalPrice;
                                        existingPrice.DiscountPercentage = price.DiscountPercentage;
                                        existingPrice.LastUpdated = DateTime.UtcNow;
                                        existingPrice.StoreUrl = price.StoreUrl;
                                        _context.Update(existingPrice);
                                    }
                                    else
                                    {
                                        price.GameId = game.Id;
                                        price.StoreId = store.Id;
                                        _context.Prices.Add(price);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error fetching price from {StoreName}",
                                storeService.StoreName);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Game and prices updated successfully.";
                    return RedirectToAction("Details", "Games", new { id = game.Id });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game to database");
                TempData["Error"] = "Error occurred while adding game.";
                return RedirectToAction("Index");
            }
        }

        private Game? FindBestMatch(IEnumerable<Game> searchResults, string targetName)
        {

            var exactMatch = searchResults.FirstOrDefault(game =>
            string.Equals(game.Name.Trim(), targetName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch;

            return searchResults.FirstOrDefault(game => GameNameMatcher.AreGamesMatching(game.Name, targetName));
        }

        private async Task<Store> GetOrCreateStore(string storeName)
        {
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.Name == storeName);

            if (store == null)
            {
                store = new Store
                {
                    Name = storeName,
                    BaseUrl = storeName == "Steam"
                        ? "https://store.steampowered.com"  
                        : "https://www.gog.com",
                    isActive = true,
                    LogoUrl = $"/images/{storeName.ToLower()}-logo.png",
                    Region = "PL",
                    RequiresAuth = false
                };

                _context.Stores.Add(store);
                await _context.SaveChangesAsync();
            }

            return store;
        }
    }
}