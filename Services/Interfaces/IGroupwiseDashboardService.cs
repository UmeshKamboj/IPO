using IPOClient.Models.Requests.GroupWiseDashboard;
using IPOClient.Models.Responses;

namespace IPOClient.Services.Interfaces
{
    public interface IGroupwiseDashboardService
    {
        Task<ReturnData<PagedResult<GroupWiseDashboardGridResponse>>> GetGroupWiseDashboardPagedListAsync(GroupWiseSummaryRequest request, int companyId);
    }
}
