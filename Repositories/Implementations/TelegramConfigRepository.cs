using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class TelegramConfigRepository : BaseRepository<IPO_TelegramConfig>, ITelegramConfigRepository
    {
        public TelegramConfigRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<IPO_TelegramConfig?> GetByUserIdAsync(int userId)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.UserId == userId);
        }
    }
}
