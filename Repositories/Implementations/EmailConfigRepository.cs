using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class EmailConfigRepository : BaseRepository<IPO_EmailConfig>, IEmailConfigRepository
    {
        public EmailConfigRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<IPO_EmailConfig?> GetByUserIdAsync(int userId)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.UserId == userId);
        }
    }
}
