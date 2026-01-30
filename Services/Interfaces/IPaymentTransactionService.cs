using IPOClient.Models.Requests.Group;
using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IPaymentTransactionService
    {
        Task<ReturnData<PaymentTransactionResponse>>CreatePaymentAsync(CreatePaymentRequest request, int userId, int companyId);
        Task<ReturnData<IPOtoIPOPaymentResponse>>CreateIPOtoIPOPaymentAsync(CreateIPOToIPOPaymentRequest request, int userId, int companyId);
        Task<ReturnData<PaymentTransactionResponse>> GetPaymentTransactionByIdAsync(int id, int companyId);
    }
}
