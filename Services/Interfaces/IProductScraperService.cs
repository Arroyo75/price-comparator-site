using price_comparator_site.Models;

namespace price_comparator_site.Services.Interfaces
{
    public interface IProductScraperService
    {
        Task<Product> ScrapeProductAsync(string url);
        Task<List<Product>> SearchProductsAsync(string searchTerm);
        string StoreName { get; }
    }
}
