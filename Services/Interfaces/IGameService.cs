using price_comparator_site.Models;

namespace price_comparator_site.Services.Interfaces
{
    public interface IGameService
    {
        Task<Game> GetGameAsync(int id);
        Task<IEnumerable<Game>> SearchGamesAsync(string searchTerm);
        Task<Game> GetGameByStoreIdAsync(string storeId, string storeName);
        Task<Game> AddOrUpdateGameAsync(Game game);
    }
}
