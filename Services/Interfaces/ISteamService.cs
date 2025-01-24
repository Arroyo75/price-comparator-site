using price_comparator_site.Models;

namespace price_comparator_site.Services.Interfaces
{
    public interface ISteamService
    {
        Task<IEnumerable<Game>> SearchGamesAsync(string searchTerm);
        Task<Game> GetGameDetailsAsync(string appId);
        Task<Price> GetGamePriceAsync(string appId);
    }
}
