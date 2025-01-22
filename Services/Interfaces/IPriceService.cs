using price_comparator_site.Models;

namespace price_comparator_site.Services.Interfaces
{
    public interface IPriceService
    {
        Task<IEnumerable<Price>> GetPricesForGameAsync(int gameId);
        Task<Price> GetLatestPriceAsync(int gameId, int storeId);
        Task<Price> AddOrUpdatePriceAsync(Price price);
        Task UpdatePricesAsync();
    }
}
