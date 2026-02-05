using IPOClient.Models.Entities;
using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Models.Responses;

namespace IPOClient.Repositories.Interfaces
{
    public interface IIPOAccountingRepository
    {
        Task<PaymentPagedResponse> GetPagedPaymentListAsync(PaymentListRequest request,int companyId);
        Task<PaymentPagedResponse> GetDeletedAccountingListAsync(DeletedAccountingListRequest request, int companyId);
    }
}
