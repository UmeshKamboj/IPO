using IPOClient.Models.Entities;

namespace IPOClient.Repositories.Interfaces
{
    public interface IEmailConfigRepository : IRepository<IPO_EmailConfig>
    {
        Task<IPO_EmailConfig?> GetByUserIdAsync(int userId);
    }
}
