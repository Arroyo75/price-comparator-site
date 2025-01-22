using Microsoft.AspNetCore.Mvc;

namespace price_comparator_site.Controllers
{
    public class GamesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
