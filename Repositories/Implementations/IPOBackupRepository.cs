using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOBackup;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;

namespace IPOClient.Repositories.Implementations
{
    public class IPOBackupRepository: BaseRepository<IPO_BuyerPlaceOrderMaster>, IIPOBackupRepository
    {
        public IPOBackupRepository(IPOClientDbContext context) : base(context)
        {
        }

      

        public async Task<byte[]?> GetIPOOrdersForBackupAsync(int ipoId, int companyId)
        {
            var masters = await _context.BuyerPlaceOrderMasters
               .Include(m => m.Orders)
                   .ThenInclude(o => o.OrderChild).ThenInclude(c => c.Group)
               .Where(x => x.CompanyId == companyId && x.IPOId == ipoId && !x.IsDeleted).ToListAsync();
            if (!masters.Any())
                return null;
            var excelRows = new List<AllOrderExcelRowResponse>();
            foreach (var master in masters)
            {
                foreach (var order in master.Orders.Where(o => !o.IsDeleted))
                {
                    var firstChild = order.OrderChild?.FirstOrDefault();
                    excelRows.Add(new AllOrderExcelRowResponse
                    {
                        GroupName = firstChild?.Group?.GroupName ?? "-",
                        OrderCategory = ((IPOOrderCategory)order.OrderCategory).ToString(),
                        InvestorType = ((IPOInvestorType)order.InvestorType).ToString(),
                        OrderType = ((IPOOrderType)order.OrderType).ToString(),
                        Quantity = order.Quantity,
                        Rate = order.Rate,
                        Amount=0,
                        Date = order.DateTime.ToString("yyyy-MM-dd"),
                        Time = order.DateTime.ToString("HH:mm:ss")
                    });
                }
            }
            // =========================
            //  GENERATE EXCEL
            // =========================
            var sb = new StringBuilder();
            sb.AppendLine("Group,Order Type,Order Category,Investor Type,Qty,Rate,Amount,Date,Time");

            foreach (var r in excelRows)
            {
                sb.AppendLine(
                    $"{r.GroupName},{r.OrderType},{r.OrderCategory},{r.InvestorType},{r.Quantity}," +
                    $"{r.Rate},{r.Amount},{r.Date},{r.Time}");
            }
            byte[] bytes = !string.IsNullOrEmpty(sb.ToString()) ? Encoding.UTF8.GetBytes(sb.ToString()) : Array.Empty<byte>();
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        public async Task<List<IPOOrderBackupData>> GetAllIPOOrdersForBackupAsync(int companyId)
        {
            var masters = await _context.BuyerPlaceOrderMasters
                .Include(m => m.Orders)
                    .ThenInclude(o => o.OrderChild).ThenInclude(c => c.Group)
                .Where(x => x.CompanyId == companyId && !x.IsDeleted).ToListAsync();

            return masters
              .GroupBy(m => m.IPOId)
              .Select(g => new IPOOrderBackupData
              {
                  IPOId = g.Key,
                  Orders = g.SelectMany(m => m.Orders)
                      .Where(o => !o.IsDeleted)
                      .Select(o =>
                      {
                          var firstChild = o.OrderChild.FirstOrDefault();
                          return new AllOrderExcelRowResponse
                          {
                              GroupName = firstChild?.Group?.GroupName ?? "-",
                              OrderType = ((IPOOrderType)o.OrderType).ToString(),
                              OrderCategory = ((IPOOrderCategory)o.OrderCategory).ToString(),
                              InvestorType = ((IPOInvestorType)o.InvestorType).ToString(),
                              Quantity = o.Quantity,
                              Rate = o.Rate,
                              Amount = 0,
                              Date = o.DateTime.ToString("dd-MM-yyyy"),
                              Time = o.DateTime.ToString("HH:mm")
                          };
                      }).ToList()
              }).ToList();
        }

        public async Task<byte[]?> GetAllPaymentTransactionBackup(int companyId)
        {
            var paymentList = await _context.PaymentTransactions.Include(x => x.Group).Include(x => x.Ipo).Where(x => x.CompanyId == companyId).ToListAsync();
            // =========================
            //  GENERATE EXCEL
            // =========================
            var sb = new StringBuilder();
            sb.AppendLine("IPOName,GroupName,AmountType,Amount,Remark,DateTime");
            foreach (var r in paymentList)
            {
                sb.AppendLine(
                   $"{r?.Ipo?.IPOName??"-"},{r?.Group?.GroupName??"-"},{(AmountType)r.AmountType},{r.Amount},{r.Remark},{r.TransactionDate.ToString("dd-MM-yyyy HH:MM")}");
            }

            byte[] bytes = !string.IsNullOrEmpty(sb.ToString()) ? Encoding.UTF8.GetBytes(sb.ToString()) : Array.Empty<byte>();
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
