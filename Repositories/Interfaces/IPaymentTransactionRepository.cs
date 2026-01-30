using IPOClient.Models.Entities;
using IPOClient.Models.Requests.Group;
using IPOClient.Models.Requests.PaymentTransaction;

namespace IPOClient.Repositories.Interfaces
{
    public interface IPaymentTransactionRepository
    {
        Task<int> CreateAsync(CreatePaymentRequest request, int userId, int companyId);
        Task<(int, int)> CreatePaymentIPOtoIPOAsync(CreateIPOToIPOPaymentRequest request, int userId, int companyId);
        Task<IPO_PaymentTransaction> GetByIdAsync(int id, int companyId);
    }
}
