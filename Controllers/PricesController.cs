using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using price_comparator_site.Data;
using price_comparator_site.Models;

namespace price_comparator_site.Controllers
{
    public class PricesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PricesController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _context.Prices
                .Include(p => p.Game)
                .Include(p => p.Store)
                .ToListAsync());
        }
        public async Task<IActionResult> Create()
        {
            ViewBag.Games = new SelectList(await _context.Games.ToListAsync(), "Id", "Name");
            ViewBag.Stores = new SelectList(await _context.Stores.ToListAsync(), "Id", "Name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GameId,StoreId,CurrentPrice,OriginalPrice,DiscountPercentage,CurrencyCode,StoreUrl")] Price price)
        {
            if(ModelState.IsValid)
            {
                price.LastUpdated = DateTime.Now;
                _context.Add(price);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Games = new SelectList(await _context.Games.ToListAsync(), "Id", "Name");
            ViewBag.Stores = new SelectList(await _context.Stores.ToListAsync(), "Id", "Name");
            return View(price);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }
            var price = await _context.Prices.FindAsync(id);
            if(price == null)
            {
                return NotFound();
            }

            ViewBag.Games = new SelectList(await _context.Games.ToListAsync(), "Id", "Name", price.GameId);
            ViewBag.Stores = new SelectList(await _context.Stores.ToListAsync(), "Id", "Name", price.StoreId);
            return View(price);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,GameId,StoreId,CurrentPrice,OriginalPrice,DiscountPercentage,CurrencyCode,StoreUrl")] Price price)
        {
            if(id != price.Id)
            {
                return NotFound();
            }
            if(ModelState.IsValid)
            {
                price.LastUpdated = DateTime.Now;
                _context.Update(price);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Games = new SelectList(await _context.Games.ToListAsync(), "Id", "Name", price.GameId);
            ViewBag.Stores = new SelectList(await _context.Stores.ToListAsync(), "Id", "Name", price.StoreId);
            return View(price);
        }
    }
}
