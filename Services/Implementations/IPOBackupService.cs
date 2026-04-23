using ClosedXML.Excel;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Implementations;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;
using System.IO.Compression;

namespace IPOClient.Services.Implementations
{
    public class IPOBackupService : IIPOBackupService
    {
        private readonly IIPOBackupRepository _ipoBackupRepository;
        private readonly IIPORepository _ipoRepository;
        public IPOBackupService(IIPOBackupRepository ipoBackupRepository, IIPORepository ipoRepository)
        {
            _ipoBackupRepository = ipoBackupRepository;
            _ipoRepository = ipoRepository;
        }
      
        public async Task<ReturnData<FileResponse>> IPOBackupAsync(int ipoId, int companyId,string userName)
        {
            try
            {
                byte[]? bytes = await _ipoBackupRepository.GetIPOOrdersForBackupAsync(ipoId, companyId);
                var ipo = await _ipoRepository.GetByIdAsync(ipoId, companyId);
                if (bytes != null)
                {
                    var file = new FileResponse
                    {
                        Bytes = bytes,
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        FileName = $"{userName}-{ipo?.IPOName ?? ""}-{DateTime.Now:dd-MM-yyyy-HH-mm}.xlsx"
                    };
                    return ReturnData<FileResponse>.SuccessResponse(file, "IPO backup successfully", 200);
                }
                else
                {
                    return ReturnData<FileResponse>.ErrorResponse("IPO backup not found", 404);
                }
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"An error occurred while retrieving IPO backup: {ex.Message}", 500);
            }
           
         
        }
        public async Task<ReturnData<FileResponse>> AllIPOsBackupAsync(int companyId, string userName)
        {
            try
            {
                var ipoDataList = await _ipoBackupRepository.GetAllIPOOrdersForBackupAsync(companyId);
                if (!ipoDataList.Any())
                    return ReturnData<FileResponse>.ErrorResponse("IPO backup not found", 404);

                using var zipStream = new MemoryStream();
                using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (var ipo in ipoDataList)
                    {
                        var ipoData = await _ipoRepository.GetByIdAsync(ipo.IPOId, companyId);
                        var safeName = ipoData?.IPOName ?? "";
                        var ipoPrice = ipoData?.IPO_Upper_Price_Band ?? 0;
                        var ipoPreOpenPrice = ipoData?.OpenIPOPrice ?? 0;

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
                        foreach (var orderRow in ipo.Orders)
                        {
                            // Get PreOpenPrice: Use child's PreOpenPrice, fallback to IPO's if child has 0
                            var preOpenPrice = orderRow.PreOpenPrice > 0 ? orderRow.PreOpenPrice : ipoPreOpenPrice;

                            var orderCategory = orderRow.OrderCategoryId;
                            bool isPremOpt = orderCategory == (int)IPOOrderCategory.Premium ||
                                             orderCategory == (int)IPOOrderCategory.CALL ||
                                             orderCategory == (int)IPOOrderCategory.PUT;
                            var rate = isPremOpt && orderRow.EffectiveRate.HasValue
                                ? orderRow.EffectiveRate.Value
                                : orderRow.Rate;

                            decimal amount;
                            if (orderCategory == (int)IPOOrderCategory.CALL)
                            {
                                amount = -rate * orderRow.AllotedQty;
                            }
                            else if (orderCategory == (int)IPOOrderCategory.PUT)
                            {
                                decimal putSp = 0;
                                if (!string.IsNullOrEmpty(orderRow.PremiumStrikePrice)
                                    && decimal.TryParse(orderRow.PremiumStrikePrice, out var parsedSp))
                                    putSp = parsedSp;
                                amount = (ipoPrice - preOpenPrice - rate + putSp) * orderRow.AllotedQty;
                            }
                            else if (isPremOpt) // Premium
                            {
                                amount = (preOpenPrice - ipoPrice - rate) * orderRow.AllotedQty;
                            }
                            else // Kostak / SubjectTo
                            {
                                amount = orderRow.AllotedQty == 0 ? 0 : (preOpenPrice - ipoPrice) * orderRow.AllotedQty - orderRow.Rate;
                            }

                            if (orderRow.OrderTypeId == 2) // SELL
                                amount = -amount;

                            worksheet.Cell(row, 1).Value = orderRow.GroupName;
                            worksheet.Cell(row, 2).Value = orderRow.OrderType;
                            worksheet.Cell(row, 3).Value = orderRow.OrderCategory;
                            worksheet.Cell(row, 4).Value = orderRow.InvestorType;
                            worksheet.Cell(row, 5).Value = orderRow.Quantity;
                            worksheet.Cell(row, 6).Value = orderRow.Rate;
                            worksheet.Cell(row, 7).Value = amount;
                            worksheet.Cell(row, 8).Value = orderRow.Date;
                            worksheet.Cell(row, 9).Value = orderRow.Time;
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();

                        var entry = zip.CreateEntry($"{userName}-{safeName}-{DateTime.Now:dd-MM-yyyy-HH-mm}.xlsx");
                        using var entryStream = entry.Open();
                        workbook.SaveAs(entryStream);
                    }
                }
                var file = new FileResponse
                {
                    Bytes = zipStream.ToArray(),
                    ContentType = "application/zip",
                    FileName = $"backup files-{DateTime.Now:dd-MM-yyyy-HH-mm}.zip"
                };
                return ReturnData<FileResponse>.SuccessResponse(file, "IPO backup successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"An error occurred while retrieving all IPOs backup: {ex.Message}", 500);
            }
        }

        public async Task<ReturnData<FileResponse>> IPOAccountingBackupAsync(int companyId, string userName)
        {
            try
            {
                byte[]? bytes = await _ipoBackupRepository.GetAllPaymentTransactionBackup(companyId);
                if (bytes != null)
                {
                    var file = new FileResponse
                    {
                        Bytes = bytes,
                        ContentType = "text/csv",
                        FileName = $"{userName}-Accounting-Backup {DateTime.Now:dd-MM-yyyy-HH-mm}.csv"
                    };
                    return ReturnData<FileResponse>.SuccessResponse(file, "IPO backup  accounting successfully", 200);
                }
                else
                {
                    return ReturnData<FileResponse>.ErrorResponse("IPO accounting backup not found", 404);
                }
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"An error occurred while retrieving IPO accounting backup: {ex.Message}", 500);
            }

        }
    }
}
