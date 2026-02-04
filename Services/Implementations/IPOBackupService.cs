using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Implementations;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;
using System.IO.Compression;
using System.Text;

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
      
        public async Task<ReturnData<FileResponse>> IPOBackupAsync(int ipoId, int companyId)
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
                        ContentType = "text/csv",
                        FileName = $"{ipo?.IPOName ?? ""} {DateTime.Now:dd-MM-yyyy-HH-mm}.csv"
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
        public async Task<ReturnData<FileResponse>> AllIPOsBackupAsync(int companyId)
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
                        var sb = new StringBuilder();
                        sb.AppendLine("Group,Order Type,Order Category,Investor Type,Qty,Rate,Amount,Date,Time");

                        foreach (var row in ipo.Orders)
                        {
                            sb.AppendLine(
                                $"{row.GroupName},{row.OrderType},{row.OrderCategory}," +
                                $"{row.InvestorType},{row.Quantity},{row.Rate}," +
                                $"{row.Amount},{row.Date},{row.Time}"
                            );
                        }

                        var safeName = ipoData?.IPOName ?? "";
                        var entry = zip.CreateEntry($"{safeName} {DateTime.Now:dd-MM-yyyy-HH-mm}.csv");

                        using var entryStream = entry.Open();
                        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                        entryStream.Write(bytes, 0, bytes.Length);
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

        public async Task<ReturnData<FileResponse>> IPOAccountingBackupAsync(int companyId)
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
                        FileName = $"Accounting-Backup {DateTime.Now:dd-MM-yyyy-HH-mm}.csv"
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
