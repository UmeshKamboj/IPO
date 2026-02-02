using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class PaymentTransactionService:IPaymentTransactionService
    {
        private readonly IPaymentTransactionRepository _paymentRepository;
        public PaymentTransactionService(IPaymentTransactionRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

      

        public async Task<ReturnData<PaymentTransactionResponse>> CreatePaymentAsync(CreatePaymentRequest request, int userId, int companyId)
        {
            try
            {
                var id=await _paymentRepository.CreateAsync(request, userId, companyId);
                var payment=await _paymentRepository.GetByIdAsync(id, companyId);
                if (payment == null)
                {
                    return ReturnData<PaymentTransactionResponse>.ErrorResponse("Payment transaction created but failed to retrieve.",500);
                }
               var response = MapToResponse(payment);
                return ReturnData<PaymentTransactionResponse>.SuccessResponse(response, "Payment transaction created successfully.", 201);
            }
            catch (Exception ex)
            {
                return ReturnData<PaymentTransactionResponse>.ErrorResponse($"Error creating payment: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<PaymentTransactionResponse>> GetPaymentTransactionByIdAsync(int id, int companyId)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(id, companyId);
                if (payment == null)
                {
                    return ReturnData<PaymentTransactionResponse>.ErrorResponse("Payment transaction not found.", 404);
                }
                var response = MapToResponse(payment);
                return ReturnData<PaymentTransactionResponse>.SuccessResponse(response, "Payment transaction retrieved successfully.", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PaymentTransactionResponse>.ErrorResponse($"Error retrieving payment transaction: {ex.Message}", 500);
            }
        }
        public async Task<ReturnData<IPOtoIPOPaymentResponse>> CreateIPOtoIPOPaymentAsync(CreateIPOToIPOPaymentRequest request, int userId, int companyId)
        {
            try
            {
                var (id1,id2) = await _paymentRepository.CreatePaymentIPOtoIPOAsync(request, userId, companyId);
                var payment1 = await _paymentRepository.GetByIdAsync(id1, companyId);
                var payment2 = await _paymentRepository.GetByIdAsync(id2, companyId);
                if (payment1 == null || payment2==null)
                {
                    return ReturnData<IPOtoIPOPaymentResponse>.ErrorResponse("Payment transaction created but failed to retrieve.", 500);
                }
                var response = MapIPOToIPOPaymentResponse(payment1, payment2);
                return ReturnData<IPOtoIPOPaymentResponse>.SuccessResponse(response, "Payment transaction created successfully.", 201);
            }
            catch (Exception ex)
            {
                return ReturnData<IPOtoIPOPaymentResponse>.ErrorResponse($"Error creating payment: {ex.Message}", 500);
            }
        }
        private PaymentTransactionResponse MapToResponse(IPO_PaymentTransaction payment)
        {
            return new PaymentTransactionResponse
            {
                PaymentTransactionId = payment.PaymentId,
                GroupId = payment.GroupId,
                GroupName = payment.Group?.GroupName,
                IpoId = payment.IpoId,
                IPOName = payment.Ipo?.IPOName,
                AmountType = payment.AmountType,
                AmountTypeName = ((AmountType)payment.AmountType).ToString(),
                Amount = payment.Amount,
                Remark = payment.Remark,
                TransactionDate = payment.TransactionDate,
                IsJV = payment.IsJVTransaction
            };
        }
        private IPOtoIPOPaymentResponse MapIPOToIPOPaymentResponse(IPO_PaymentTransaction payment1, IPO_PaymentTransaction payment2)
        {
            return new IPOtoIPOPaymentResponse
            {
                PaymentTransactionId1 = payment1.PaymentId,
                GroupId1 = payment1.GroupId,
                GroupName1 = payment1.Group?.GroupName,
                IpoId1 = payment1.IpoId,
                IPOName1 = payment1.Ipo?.IPOName,
                AmountType1 = payment1.AmountType,
                AmountTypeName1 = ((AmountType)payment1.AmountType).ToString(),
                Amount = payment1.Amount,
                Remark1 = payment1.Remark,
                PaymentTransactionId2 = payment2.PaymentId,
                GroupId2 = payment2.GroupId,
                GroupName2= payment2.Group?.GroupName,
                IpoId2 = payment2.IpoId,
                IPOName2 = payment2.Ipo?.IPOName,
                AmountType2 = payment2.AmountType,
                AmountTypeName2 = ((AmountType)payment2.AmountType).ToString(),
                Remark2 = payment2.Remark,
                TransactionDate = payment1.TransactionDate

            };
        }
    }
}
