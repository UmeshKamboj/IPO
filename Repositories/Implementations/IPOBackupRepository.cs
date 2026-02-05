using ClosedXML.Excel;
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
            // Get IPO master for pricing data
            var ipoMaster = await _context.IPO_IPOMaster.FirstOrDefaultAsync(x => x.Id == ipoId && x.CompanyId == companyId);
            var ipoPrice = ipoMaster?.IPO_Upper_Price_Band ?? 0;
            var ipoPreOpenPrice = ipoMaster?.OpenIPOPrice ?? 0;

            var children = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Include(c => c.Group)
                .Where(c => c.CompanyId == companyId &&
                           c.IPOOrder.BuyerMaster.IPOId == ipoId &&
                           c.IPOOrder.BuyerMaster.IsActive &&
                           !c.IsDeleted &&
                           !c.IPOOrder.IsDeleted &&
                           !c.IPOOrder.BuyerMaster.IsDeleted)
                .ToListAsync();

            if (!children.Any())
                return null;

            // =========================
            //  GENERATE EXCEL
            // =========================
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Orders");

            // Headers
            var headers = new[] { "Group", "OrderType", "Category", "Investor Type", "Qty", "Rate", "Amount", "Order Date", "Order Time" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data rows
            int row = 2;
            foreach (var child in children)
            {
                var order = child.IPOOrder;
                var allotedQty = child.AllotedQty ?? 0;
                var preOpenPrice = child.PreOpenPrice > 0 ? child.PreOpenPrice : ipoPreOpenPrice;
                var rate = order.Rate;

                // Formula: Amount = (PreOpenPrice - IPOPrice) × AllotedQty - Rate
                // If AllotedQty is 0, Amount should be 0
                var amount = allotedQty == 0 ? 0 : (preOpenPrice - ipoPrice) * allotedQty - rate;
                if (allotedQty != 0 && order.OrderType == (int)IPOOrderType.SELL)
                    amount = -amount;

                worksheet.Cell(row, 1).Value = child.Group?.GroupName ?? "-";
                worksheet.Cell(row, 2).Value = ((IPOOrderType)order.OrderType).ToString();
                worksheet.Cell(row, 3).Value = ((IPOOrderCategory)order.OrderCategory).ToString();
                worksheet.Cell(row, 4).Value = ((IPOInvestorType)order.InvestorType).ToString();
                worksheet.Cell(row, 5).Value = order.Quantity;
                worksheet.Cell(row, 6).Value = rate;
                worksheet.Cell(row, 7).Value = amount;
                worksheet.Cell(row, 8).Value = order.DateTime.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 9).Value = order.DateTime.ToString("HH:mm:ss");
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        public async Task<List<IPOOrderBackupData>> GetAllIPOOrdersForBackupAsync(int companyId)
        {
            var children = await _context.ChildPlaceOrder
                .Include(c => c.IPOOrder)
                    .ThenInclude(o => o.BuyerMaster)
                .Include(c => c.Group)
                .Where(c => c.CompanyId == companyId &&
                           c.IPOOrder.BuyerMaster.IsActive &&
                           !c.IsDeleted &&
                           !c.IPOOrder.IsDeleted &&
                           !c.IPOOrder.BuyerMaster.IsDeleted)
                .ToListAsync();

            return children
                .GroupBy(c => c.IPOOrder.BuyerMaster.IPOId)
                .Select(g => new IPOOrderBackupData
                {
                    IPOId = g.Key,
                    Orders = g.Select(c =>
                    {
                        var order = c.IPOOrder;
                        return new AllOrderExcelRowResponse
                        {
                            GroupName = c.Group?.GroupName ?? "-",
                            OrderType = ((IPOOrderType)order.OrderType).ToString(),
                            OrderTypeId = order.OrderType,
                            OrderCategory = ((IPOOrderCategory)order.OrderCategory).ToString(),
                            InvestorType = ((IPOInvestorType)order.InvestorType).ToString(),
                            Quantity = order.Quantity,
                            AllotedQty = c.AllotedQty ?? 0,
                            PreOpenPrice = c.PreOpenPrice,
                            Rate = order.Rate,
                            Amount = 0,
                            Date = order.DateTime.ToString("dd-MM-yyyy"),
                            Time = order.DateTime.ToString("HH:mm")
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
