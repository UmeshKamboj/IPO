using IPOClient.Models.Requests.GroupWiseDashboard;
using IPOClient.Models.Responses;

namespace IPOClient.Repositories.Interfaces
{
    public interface IGroupWiseDashboardRepository
    {
        Task<PagedResult<GroupIpoSummaryRow>> GetGroupWiseDashboardSummaryAsync(GroupWiseSummaryRequest request,int companyId);
    }
}
