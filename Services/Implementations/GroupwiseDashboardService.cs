using IPOClient.Models.Requests.GroupWiseDashboard;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using IPOClient.Services.Interfaces;

namespace IPOClient.Services.Implementations
{
    public class GroupwiseDashboardService: IGroupwiseDashboardService
    {
        private readonly IGroupWiseDashboardRepository _groupwiseDashboardRepository;
        public GroupwiseDashboardService(IGroupWiseDashboardRepository groupwiseDashboardRepository)
        {
            _groupwiseDashboardRepository = groupwiseDashboardRepository;
        }
        
        public async  Task<ReturnData<PagedResult<GroupWiseDashboardGridResponse>>> GetGroupWiseDashboardPagedListAsync(GroupWiseSummaryRequest request, int companyId)
        {
            try
            {
                var flatPaged = await _groupwiseDashboardRepository.GetGroupWiseDashboardSummaryAsync(request, companyId);

                var flatItems = flatPaged.Items ?? new List<GroupIpoSummaryRow>();

                // IPO HEADERS
                var ipos = flatItems
                    .Select(x => new { x.IpoId, x.IpoName })
                    .Distinct()
                    .OrderBy(x => x.IpoName)
                    .Select(x => new IpoHeaderDto
                    {
                        IpoId = x.IpoId,
                        IpoName = x.IpoName
                    })
                    .ToList();

                // GROUP ROWS
                var rows = flatItems
                    .GroupBy(x => new { x.GroupId, x.GroupName })
                    .Select(g =>
                    {
                        var row = new GroupRowDto
                        {
                            GroupId = g.Key.GroupId,
                            GroupName = g.Key.GroupName
                        };

                        foreach (var ipo in ipos)
                        {
                            var cell = g.FirstOrDefault(x => x.IpoId == ipo.IpoId);

                            decimal credit = cell?.Credit ?? 0m;
                            decimal debit = cell?.Debit ?? 0m;
                            decimal total = credit - debit;

                            row.IpoData.Add(new IpoAmount
                            {
                                IpoId = ipo.IpoId,
                                IpoName = ipo.IpoName,
                                Collection = credit,
                                Due = debit - credit,
                                Total = total
                            });

                            row.Collection += credit;
                            row.Due += (debit - credit);
                            row.Total += total;
                        }

                        return row;
                    })
                    .ToList();

                // FOOTER
                var footerIpoTotals = flatItems
                    .GroupBy(x => new { x.IpoId, x.IpoName })
                    .Select(g => new IpoAmount
                    {
                        IpoId = g.Key.IpoId,
                        IpoName = g.Key.IpoName,
                        Collection = g.Sum(x => x.Credit),
                        Due = g.Sum(x => x.Debit - x.Credit),
                        Total = g.Sum(x => x.Credit - x.Debit)
                    })
                    .ToList();

                var footer = new SummaryFooterDto
                {
                    IpoTotals = footerIpoTotals,
                    GrandCollection = footerIpoTotals.Sum(x => x.Collection),
                    GrandDue = footerIpoTotals.Sum(x => x.Due),
                    GrandTotal = footerIpoTotals.Sum(x => x.Total)
                };

                var grid = new GroupWiseDashboardGridResponse
                {
                    //Ipos = ipos,
                    Rows = rows,
                    Footer = footer
                };

                var pagedResult = new PagedResult<GroupWiseDashboardGridResponse>(
                    new List<GroupWiseDashboardGridResponse> { grid },
                    flatPaged.TotalCount,
                    flatPaged.Skip,
                    flatPaged.PageSize
                );
                return ReturnData<PagedResult<GroupWiseDashboardGridResponse>>.SuccessResponse(pagedResult, "Group wise summary retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<GroupWiseDashboardGridResponse>>.ErrorResponse($"Error retrieving group wise summary: {ex.Message}", 500);
            }
            
        }
    }
}
