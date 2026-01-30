using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class PaymentTransactionRepository:BaseRepository<IPO_PaymentTransaction> ,IPaymentTransactionRepository
    {
        public PaymentTransactionRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<int> CreateAsync(CreatePaymentRequest request, int userId, int companyId)
        {
            var paymentTransaction = new IPO_PaymentTransaction
            {
                GroupId = request.GroupId,
                IpoId = request.IpoId,
                AmountType = request.AmountType,
                Amount = request.Amount,
                Remark = request.Remark,
                TransactionDate = request.TransactionDate,
                IsJVTransaction = request.IsJV,
                CompanyId = companyId,
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow
            };
            await _dbSet.AddAsync(paymentTransaction);
            await _context.SaveChangesAsync();
            return paymentTransaction.PaymentId;
        }

        public async Task<(int, int)> CreatePaymentIPOtoIPOAsync(CreateIPOToIPOPaymentRequest request, int userId, int companyId)
        {
            if (request.AmountType1 == request.AmountType2)
                throw new Exception("One side must be Debit and other Credit");

            if (request.Amount <= 0)
                throw new Exception("Amount must be greater than zero");
            var payment1 = new IPO_PaymentTransaction
            {
                GroupId = request.GroupId1,
                IpoId = request.IpoId1,
                AmountType = request.AmountType1,
                Amount = request.Amount,
                Remark = request.Remark1,
                TransactionDate = request.TransactionDate,
                CompanyId = companyId,
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow
            };
            var payment2 = new IPO_PaymentTransaction
            {
                GroupId = request.GroupId2,
                IpoId = request.IpoId2,
                AmountType = request.AmountType2,
                Amount = request.Amount,
                Remark = request.Remark2,
                TransactionDate = request.TransactionDate,
                CompanyId = companyId,
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow
            };
            await _dbSet.AddRangeAsync(payment1, payment2);
            await _context.SaveChangesAsync();
            return (payment1.PaymentId,payment2.PaymentId);

        }

        public async Task<IPO_PaymentTransaction> GetByIdAsync(int id, int companyId)
        {
           return await _dbSet
                              .Include(g=>g.Group).Include(i=>i.Ipo)    
                              .FirstOrDefaultAsync(pt => pt.PaymentId == id && pt.CompanyId == companyId);
        }
    }
}
