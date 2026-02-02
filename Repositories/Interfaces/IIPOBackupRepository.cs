using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOBackup;

namespace IPOClient.Repositories.Interfaces
{
    public interface IIPOBackupRepository
    {
        Task<byte[]?> GetIPOOrdersForBackupAsync(int ipoId, int companyId);
        Task<List<IPOOrderBackupData>> GetAllIPOOrdersForBackupAsync(int companyId);

        Task<byte[]?> GetAllPaymentTransactionBackup(int companyId);
    }
}
