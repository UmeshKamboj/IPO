using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOAccountingService
    {
        Task<ReturnData<PaymentAccountingResponse>> GetPagedAccountingListAsync(PaymentListRequest request, int companyId);
        Task<ReturnData<PaymentAccountingResponse>> GetDeletedAccountingListAsync(DeletedAccountingListRequest request, int companyId);
    }
}
