using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IIPOBackupService
    {
        Task<ReturnData<FileResponse>> IPOBackupAsync(int ipoId, int companyId, string userName);
        Task<ReturnData<FileResponse>> AllIPOsBackupAsync(int companyId, string userName);

        Task<ReturnData<FileResponse>> IPOAccountingBackupAsync(int companyId, string userName);
    }
}
