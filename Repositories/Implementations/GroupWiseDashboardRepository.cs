using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Enums;
using IPOClient.Models.Requests.GroupWiseDashboard;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class GroupWiseDashboardRepository:BaseRepository<IPO_PaymentTransaction>, IGroupWiseDashboardRepository
    {
        public GroupWiseDashboardRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<GroupIpoSummaryRow>> GetGroupWiseDashboardSummaryAsync(GroupWiseSummaryRequest request, int companyId)
        {

            // PAGED GROUPS (MASTER)
            var groupQuery = _context.IPO_GroupMaster
                .Where(g => g.CompanyId == companyId && g.IsActive);

            var totalGroups = await groupQuery.CountAsync();

            var pagedGroups = await groupQuery
                .OrderBy(g => g.GroupName)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .Select(g => new { g.IPOGroupId, g.GroupName })
                .ToListAsync();

            var groupIds = pagedGroups.Select(x => x.IPOGroupId).ToList();

            //  ALL IPOS (MASTER)
            var ipos = await _context.IPO_IPOMaster
                .Where(i => i.CompanyId == companyId && i.IsActive)
                .Select(i => new { i.Id, i.IPOName })
                .ToListAsync();

            // TRANSACTION SUMMARY (ONLY EXISTING)
            var summary = await _context.PaymentTransactions
                .Where(x => x.CompanyId == companyId
                         && groupIds.Contains(x.GroupId))
                .GroupBy(x => new { x.GroupId, x.IpoId })
                .Select(g => new
                {
                    g.Key.GroupId,
                    g.Key.IpoId,
                    Debit = g.Where(x => x.AmountType == (int)AmountType.Debit)
                             .Sum(x => x.Amount),
                    Credit = g.Where(x => x.AmountType == (int)AmountType.Credit)
                              .Sum(x => x.Amount)
                })
                .ToListAsync();

            //  MERGE (ZERO FILL)
            var result = new List<GroupIpoSummaryRow>();

            foreach (var g in pagedGroups)
            {
                foreach (var ipo in ipos)
                {
                    var match = summary.FirstOrDefault(x =>
                        x.GroupId == g.IPOGroupId && x.IpoId == ipo.Id);

                    result.Add(new GroupIpoSummaryRow
                    {
                        GroupId = g.IPOGroupId,
                        GroupName = g.GroupName,
                        IpoId = ipo.Id,
                        IpoName = ipo.IPOName,
                        Debit = match?.Debit ?? 0,
                        Credit = match?.Credit ?? 0
                    });
                }
            }

            return new PagedResult<GroupIpoSummaryRow>(result,totalGroups, request.Skip,request.PageSize );

        }
    }
}
