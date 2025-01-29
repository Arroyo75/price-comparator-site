using Microsoft.AspNetCore.Mvc;
using price_comparator_site.Data;
using price_comparator_site.Models;

namespace price_comparator_site.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(
            ApplicationDbContext context,
            ILogger<StatisticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? gameId = null)
        {
            try
            {
                var statistics = await _context.GetGamePriceStatisticsAsync(gameId);
                return View(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting price statistics");
                TempData["Error"] = "Error occurred while getting statistics.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> GameStats(int id)
        {
            try
            {
                var statistics = await _context.GetGamePriceStatisticsAsync(id);
                if (!statistics.Any())
                {
                    return NotFound();
                }
                return View(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting game statistics for game {GameId}", id);
                TempData["Error"] = "Error occurred while getting game statistics.";
                return RedirectToAction("Index", "Games");
            }
        }
    }
}