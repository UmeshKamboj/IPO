using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.PaymentTransaction;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Xml.Linq;

namespace IPOClient.Repositories.Implementations
{
    public class IPOAccountingRepository:BaseRepository<IPO_PaymentTransaction>,IIPOAccountingRepository
    {
      public IPOAccountingRepository(IPOClientDbContext context) : base(context)
      {

      }

        public async Task<PaymentPagedResponse> GetPagedPaymentListAsync(PaymentListRequest request, int companyId)
        {
            var query=_dbSet.AsQueryable();
            query=query.Where(x=>x.CompanyId==companyId);
            if(request.IpoId.HasValue && request.IpoId.Value > 0)
            {
                query=query.Where(x=>x.IpoId==request.IpoId.Value);
            }
            if(request.GroupId.HasValue && request.GroupId.Value > 0)
            {
                query=query.Where(x=>x.GroupId==request.GroupId.Value);
            }
            var fromDate = request.GetFromDate();
            var toDate = request.GetToDate();

            if (fromDate.HasValue)
            {
                query=query.Where(x=>x.TransactionDate>= fromDate.Value);
            }
            if(toDate.HasValue)
            {
                query=query.Where(x=>x.TransactionDate<= toDate.Value);
            }
            // Apply global search if provided
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue.Trim().ToLower();

                int? amountTypeMatch = Enum.GetValues(typeof(AmountType))
                    .Cast<AmountType>()
                    .Where(e => e.ToString().ToLower().Contains(search))
                    .Select(e => (int?)e)
                    .FirstOrDefault();

                query = query.Where(o =>
                (amountTypeMatch.HasValue && o.AmountType == amountTypeMatch.Value) || (o.Ipo.IPOName !=null&& o.Ipo.IPOName.ToLower().Contains(search))
                ||(o.Group.GroupName!=null&& o.Group.GroupName.ToLower().Contains(search))
                ||(o.Amount!=null&&o.Amount.ToString().Contains(search))
                ||(o.Remark!=null&&o.Remark.ToString().Contains(search))
                );
            }
            // Total count before pagination
            var totalCount = await query.CountAsync();
            var totalCredit = await query.Where(x => x.AmountType == (int)AmountType.Credit).SumAsync(x => (decimal?)x.Amount) ?? 0;

            var totalDebit = await query .Where(x => x.AmountType == (int)AmountType.Debit).SumAsync(x => (decimal?)x.Amount) ?? 0;

            var totalAmount = totalCredit - totalDebit;
            // Apply offset-based pagination
            var payment = await query
                .Include(g => g.Ipo).Include(g=>g.Group)
                .OrderByDescending(x => x.TransactionDate)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            return new PaymentPagedResponse
            {
                PagedResult = new PagedResult<IPO_PaymentTransaction>(payment,totalCount,request.Skip, request.PageSize),
                TotalCredit = totalCredit,
                TotalDebit = totalDebit,
                TotalAmount = totalAmount
            };
        }

        public async Task<PaymentPagedResponse> GetDeletedAccountingListAsync(DeletedAccountingListRequest request, int companyId)
        {
            var query = _dbSet.AsQueryable();
            query = query.Where(x => x.CompanyId == companyId);

            // Filter by IsDeleted: when true, show data where IPO or Group is deleted (IsActive == false)
            // By default (IsDeleted = true), show deleted IPO and deleted Group data
            if (request.IsDeleted)
            {
                query = query.Where(x => x.Ipo.IsActive == false || x.Group.IsActive == false);
            }
            else
            {
                query = query.Where(x => x.Ipo.IsActive == true && x.Group.IsActive == true);
            }

            if (request.IpoId.HasValue && request.IpoId.Value > 0)
            {
                query = query.Where(x => x.IpoId == request.IpoId.Value);
            }
            if (request.GroupId.HasValue && request.GroupId.Value > 0)
            {
                query = query.Where(x => x.GroupId == request.GroupId.Value);
            }

            // Apply date range filter (DateRange = "10-02-2026 to 14-02-2026")
            var fromDate = request.GetFromDate();
            var toDate = request.GetToDate();

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.TransactionDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                query = query.Where(x => x.TransactionDate <= toDate.Value);
            }

            // Apply global search if provided
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue.Trim().ToLower();

                int? amountTypeMatch = Enum.GetValues(typeof(AmountType))
                    .Cast<AmountType>()
                    .Where(e => e.ToString().ToLower().Contains(search))
                    .Select(e => (int?)e)
                    .FirstOrDefault();

                query = query.Where(o =>
                (amountTypeMatch.HasValue && o.AmountType == amountTypeMatch.Value) ||
                (o.Ipo.IPOName != null && o.Ipo.IPOName.ToLower().Contains(search)) ||
                (o.Group.GroupName != null && o.Group.GroupName.ToLower().Contains(search)) ||
                (o.Amount.ToString().Contains(search)) ||
                (o.Remark != null && o.Remark.ToLower().Contains(search))
                );
            }

            // Total count before pagination
            var totalCount = await query.CountAsync();
            var totalCredit = await query.Where(x => x.AmountType == (int)AmountType.Credit).SumAsync(x => (decimal?)x.Amount) ?? 0;
            var totalDebit = await query.Where(x => x.AmountType == (int)AmountType.Debit).SumAsync(x => (decimal?)x.Amount) ?? 0;
            var totalAmount = totalCredit - totalDebit;

            // Apply offset-based pagination
            var payment = await query
                .Include(g => g.Ipo)
                .Include(g => g.Group)
                .OrderByDescending(x => x.TransactionDate)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            return new PaymentPagedResponse
            {
                PagedResult = new PagedResult<IPO_PaymentTransaction>(payment, totalCount, request.Skip, request.PageSize),
                TotalCredit = totalCredit,
                TotalDebit = totalDebit,
                TotalAmount = totalAmount
            };
        }
    }
}

