using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
namespace IPOClient.Repositories.Interfaces
{
    public interface IIPOOrderRemarkRepository:IRepository<IPO_Order_Remark>
    {
        Task<IPO_Order_Remark?> GetByIdAsync(int id, int companyId);
        Task<int> CreateAsync(CreateOrderRemarkRequest request, int userId, int companyId);

        Task<List<IPO_Order_Remark>> GetRemarkByCompanyAsync(int companyId, int? ipoId); //get ipo remarks by ipo id and company id
    }
}
