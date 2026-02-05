using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.Group;
using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class IPOAccountingService : IIPOAccountingService
    {
        private readonly IIPOAccountingRepository _ipoAccountingRepository;
        public IPOAccountingService(IIPOAccountingRepository ipoAccountingRepository)
        {
            _ipoAccountingRepository = ipoAccountingRepository;
        }
        public async Task<ReturnData<PaymentAccountingResponse>> GetPagedAccountingListAsync(PaymentListRequest request, int companyId)
        {
            try
            {
                var pageddata = await _ipoAccountingRepository.GetPagedPaymentListAsync(request, companyId);
                var responses = pageddata.PagedResult.Items.Select(MapToResponse).ToList();
                var pagedResult = new PagedResult<PaymentTransactionResponse>(responses, pageddata.PagedResult.TotalCount, request.Skip, request.PageSize);
                var response = new PaymentAccountingResponse
                {
                    PagedResult = pagedResult,
                    TotalCredit = pageddata.TotalCredit,
                    TotalDebit = pageddata.TotalDebit,
                    TotalAmount = pageddata.TotalAmount
                };
                return ReturnData<PaymentAccountingResponse>.SuccessResponse(response, "Payment transactions retrieved successfully.", 200);
                
            }
            catch (Exception ex)
            {
                return ReturnData<PaymentAccountingResponse>.ErrorResponse($"An error occurred while retrieving payment transactions: {ex.Message}");
            }
           
        }

        public async Task<ReturnData<PaymentAccountingResponse>> GetDeletedAccountingListAsync(DeletedAccountingListRequest request, int companyId)
        {
            try
            {
                var pageddata = await _ipoAccountingRepository.GetDeletedAccountingListAsync(request, companyId);
                var responses = pageddata.PagedResult.Items.Select(MapToResponse).ToList();
                var pagedResult = new PagedResult<PaymentTransactionResponse>(responses, pageddata.PagedResult.TotalCount, request.Skip, request.PageSize);
                var response = new PaymentAccountingResponse
                {
                    PagedResult = pagedResult,
                    TotalCredit = pageddata.TotalCredit,
                    TotalDebit = pageddata.TotalDebit,
                    TotalAmount = pageddata.TotalAmount
                };
                return ReturnData<PaymentAccountingResponse>.SuccessResponse(response, "Deleted accounting transactions retrieved successfully.", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PaymentAccountingResponse>.ErrorResponse($"An error occurred while retrieving deleted accounting transactions: {ex.Message}");
            }
        }

        private PaymentTransactionResponse MapToResponse(IPO_PaymentTransaction payment)
        {
            return new PaymentTransactionResponse
            {
                PaymentTransactionId = payment.PaymentId,
                IpoId = payment.IpoId ?? 0,
                IPOName = payment.Ipo?.IPOName ?? string.Empty,
                GroupId = payment.GroupId,
                GroupName = payment.Group?.GroupName ?? string.Empty,
                TransactionDate = payment.TransactionDate,
                Amount = payment.Amount,
                Remark = payment.Remark,
                AmountType = payment.AmountType,
                AmountTypeName = ((AmountType)payment.AmountType).ToString(),
            };
        }
    }
}
