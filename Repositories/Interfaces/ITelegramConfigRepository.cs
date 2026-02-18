using IPOClient.Models.Entities;

namespace IPOClient.Repositories.Interfaces
{
    public interface ITelegramConfigRepository : IRepository<IPO_TelegramConfig>
    {
        Task<IPO_TelegramConfig?> GetByUserIdAsync(int userId);
    }
}
