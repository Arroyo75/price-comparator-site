using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using price_comparator_site.Models;

namespace price_comparator_site.Data
{
    public partial class ApplicationDbContext
    {
        public async Task<List<GamePriceStatistics>> GetGamePriceStatisticsAsync(int? gameId = null)
        {
            var gameIdParameter = new SqlParameter("@GameId",
                gameId.HasValue ? (object)gameId.Value : DBNull.Value);

            return await GamePriceStatistics
                .FromSqlRaw("EXEC GetGamePriceStatistics @GameId", gameIdParameter)
                .ToListAsync();
        }
    }
}
