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

                // Get order billing data for these groups
                var groupIds = flatItems.Select(x => x.GroupId).Distinct().ToList();
                var billingData = await _groupwiseDashboardRepository.GetOrderBillingByGroupAsync(groupIds, companyId);

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
                            var billing = billingData.FirstOrDefault(b =>
                                b.GroupId == g.Key.GroupId && b.IpoId == ipo.IpoId);

                            decimal credit = cell?.Credit ?? 0m;
                            decimal debit = cell?.Debit ?? 0m;
                            decimal jv = cell?.JV ?? 0m;
                            decimal billingTotal = billing?.BillingTotal ?? 0m;

                            // Total = order billing net amount
                            decimal total = billingTotal;

                            row.IpoData.Add(new IpoAmount
                            {
                                IpoId = ipo.IpoId,
                                IpoName = ipo.IpoName,
                                Collection = credit,
                                Due = total - credit,
                                Total = total
                            });

                            row.JV += jv;
                            row.Total += total;
                            row.OldCollection += credit;
                            row.NewCollection += credit;
                            row.DueAmount += (total - credit);

                            // Backward compatible
                            row.Collection += credit;
                            row.Due += (total - credit);
                        }

                        return row;
                    })
                    .ToList();

                // FOOTER
                var footerIpoTotals = flatItems
                    .GroupBy(x => new { x.IpoId, x.IpoName })
                    .Select(g =>
                    {
                        var billing = billingData
                            .Where(b => b.IpoId == g.Key.IpoId)
                            .Sum(b => b.BillingTotal);

                        return new IpoAmount
                        {
                            IpoId = g.Key.IpoId,
                            IpoName = g.Key.IpoName,
                            Collection = g.Sum(x => x.Credit),
                            Total = billing,
                            Due = billing - g.Sum(x => x.Credit)
                        };
                    })
                    .ToList();

                var footer = new SummaryFooterDto
                {
                    IpoTotals = footerIpoTotals,
                    GrandJV = flatItems.Sum(x => x.JV),
                    GrandTotal = footerIpoTotals.Sum(x => x.Total),
                    GrandOldCollection = footerIpoTotals.Sum(x => x.Collection),
                    GrandNewCollection = footerIpoTotals.Sum(x => x.Collection),
                    GrandDueAmount = footerIpoTotals.Sum(x => x.Due),
                    GrandCollection = footerIpoTotals.Sum(x => x.Collection),
                    GrandDue = footerIpoTotals.Sum(x => x.Due)
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
