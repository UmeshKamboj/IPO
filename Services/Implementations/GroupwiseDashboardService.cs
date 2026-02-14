using ClosedXML.Excel;
using IPOClient.Models.Requests.GroupWiseDashboard;
using IPOClient.Models.Requests.IPOMaster.Response;
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
                var search = request.SearchValue?.Trim() ?? "";
                bool isNumericSearch = !string.IsNullOrEmpty(search) && IsNumericSearchTerm(search);

                // If numeric search, we need computed values so fetch all and filter in-memory
                // If text search or no search, use DB-level pagination (more efficient)
                if (isNumericSearch)
                {
                    return await GetDashboardWithNumericSearch(request, companyId, search);
                }
                else
                {
                    return await GetDashboardWithDbPagination(request, companyId, search);
                }
            }
            catch (Exception ex)
            {
                return ReturnData<PagedResult<GroupWiseDashboardGridResponse>>.ErrorResponse($"Error retrieving group wise summary: {ex.Message}", 500);
            }
        }

        private async Task<ReturnData<PagedResult<GroupWiseDashboardGridResponse>>> GetDashboardWithDbPagination(GroupWiseSummaryRequest request, int companyId, string search)
        {
            var flatPaged = await _groupwiseDashboardRepository.GetGroupWiseDashboardSummaryAsync(request, companyId);
            var flatItems = flatPaged.Items ?? new List<GroupIpoSummaryRow>();

            var groupIds = flatItems.Select(x => x.GroupId).Distinct().ToList();
            var billingData = await _groupwiseDashboardRepository.GetOrderBillingByGroupAsync(groupIds, companyId);

            var ipos = flatItems
                .Select(x => new { x.IpoId, x.IpoName })
                .Distinct()
                .OrderBy(x => x.IpoName)
                .Select(x => new IpoHeaderDto { IpoId = x.IpoId, IpoName = x.IpoName })
                .ToList();

            var rows = BuildGroupRows(flatItems, ipos, billingData);

            var footerIpoTotals = BuildFooterIpoTotals(flatItems, billingData, null);

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

            var grid = new GroupWiseDashboardGridResponse { Rows = rows, Footer = footer };
            var pagedResult = new PagedResult<GroupWiseDashboardGridResponse>(
                new List<GroupWiseDashboardGridResponse> { grid },
                flatPaged.TotalCount,
                flatPaged.Skip,
                flatPaged.PageSize
            );
            return ReturnData<PagedResult<GroupWiseDashboardGridResponse>>.SuccessResponse(pagedResult, "Group wise summary retrieved successfully", 200);
        }

        private async Task<ReturnData<PagedResult<GroupWiseDashboardGridResponse>>> GetDashboardWithNumericSearch(GroupWiseSummaryRequest request, int companyId, string search)
        {
            var flatItems = await _groupwiseDashboardRepository.GetAllGroupWiseDashboardSummaryAsync(companyId);
            var allGroupIds = flatItems.Select(x => x.GroupId).Distinct().ToList();
            var billingData = await _groupwiseDashboardRepository.GetOrderBillingByGroupAsync(allGroupIds, companyId);

            var ipos = flatItems
                .Select(x => new { x.IpoId, x.IpoName })
                .Distinct()
                .OrderBy(x => x.IpoName)
                .Select(x => new IpoHeaderDto { IpoId = x.IpoId, IpoName = x.IpoName })
                .ToList();

            var allRows = BuildGroupRows(flatItems, ipos, billingData);

            // Filter rows where any numeric field matches the search term
            var filteredRows = allRows.Where(row =>
                row.GroupName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                MatchesDecimal(row.JV, search) ||
                MatchesDecimal(row.Total, search) ||
                MatchesDecimal(row.OldCollection, search) ||
                MatchesDecimal(row.NewCollection, search) ||
                MatchesDecimal(row.DueAmount, search) ||
                row.IpoData.Any(ipo =>
                    MatchesDecimal(ipo.Total, search) ||
                    MatchesDecimal(ipo.Collection, search) ||
                    MatchesDecimal(ipo.Due, search))
            ).ToList();

            var totalCount = filteredRows.Count;
            var pagedRows = filteredRows.Skip(request.Skip).Take(request.PageSize).ToList();

            var filteredGroupIds = filteredRows.Select(r => r.GroupId).ToHashSet();
            var filteredFlatItems = flatItems.Where(x => filteredGroupIds.Contains(x.GroupId)).ToList();
            var footerIpoTotals = BuildFooterIpoTotals(filteredFlatItems, billingData, filteredGroupIds);

            var footer = new SummaryFooterDto
            {
                IpoTotals = footerIpoTotals,
                GrandJV = filteredFlatItems.Sum(x => x.JV),
                GrandTotal = footerIpoTotals.Sum(x => x.Total),
                GrandOldCollection = footerIpoTotals.Sum(x => x.Collection),
                GrandNewCollection = footerIpoTotals.Sum(x => x.Collection),
                GrandDueAmount = footerIpoTotals.Sum(x => x.Due),
                GrandCollection = footerIpoTotals.Sum(x => x.Collection),
                GrandDue = footerIpoTotals.Sum(x => x.Due)
            };

            var grid = new GroupWiseDashboardGridResponse { Rows = pagedRows, Footer = footer };
            var pagedResult = new PagedResult<GroupWiseDashboardGridResponse>(
                new List<GroupWiseDashboardGridResponse> { grid },
                totalCount,
                request.Skip,
                request.PageSize
            );
            return ReturnData<PagedResult<GroupWiseDashboardGridResponse>>.SuccessResponse(pagedResult, "Group wise summary retrieved successfully", 200);
        }

        private static List<GroupRowDto> BuildGroupRows(List<GroupIpoSummaryRow> flatItems, List<IpoHeaderDto> ipos, List<GroupIpoBillingRow> billingData)
        {
            return flatItems
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
                        decimal jv = cell?.JV ?? 0m;
                        decimal billingTotal = billing?.BillingTotal ?? 0m;
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
                        row.Collection += credit;
                        row.Due += (total - credit);
                    }

                    return row;
                })
                .OrderBy(r => r.GroupName)
                .ToList();
        }

        private static List<IpoAmount> BuildFooterIpoTotals(List<GroupIpoSummaryRow> flatItems, List<GroupIpoBillingRow> billingData, HashSet<int>? filteredGroupIds)
        {
            return flatItems
                .GroupBy(x => new { x.IpoId, x.IpoName })
                .Select(g =>
                {
                    var billingQuery = billingData.Where(b => b.IpoId == g.Key.IpoId);
                    if (filteredGroupIds != null)
                        billingQuery = billingQuery.Where(b => filteredGroupIds.Contains(b.GroupId));

                    var billing = billingQuery.Sum(b => b.BillingTotal);

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
        }

        private static bool IsNumericSearchTerm(string search)
        {
            // Contains any digit or starts with minus followed by digit
            return search.Any(char.IsDigit);
        }

        private static bool MatchesDecimal(decimal value, string search)
        {
            var valueStr = value.ToString("G");
            if (valueStr.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            var valueStr2 = value.ToString("0.0#####");
            return valueStr2.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ReturnData<FileResponse>> DownloadGroupWiseDashboardExcelAsync(int companyId)
        {
            try
            {
                // Fetch ALL groups (no pagination)
                var flatItems = await _groupwiseDashboardRepository.GetAllGroupWiseDashboardSummaryAsync(companyId);

                var groupIds = flatItems.Select(x => x.GroupId).Distinct().ToList();
                var billingData = await _groupwiseDashboardRepository.GetOrderBillingByGroupAsync(groupIds, companyId);

                // IPO HEADERS
                var ipos = flatItems
                    .Select(x => new { x.IpoId, x.IpoName })
                    .Distinct()
                    .OrderBy(x => x.IpoName)
                    .ToList();

                // GROUP ROWS
                var groupRows = flatItems
                    .GroupBy(x => new { x.GroupId, x.GroupName })
                    .OrderBy(g => g.Key.GroupName)
                    .ToList();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Group Wise Dashboard");

                // === HEADER ROW ===
                int col = 1;
                worksheet.Cell(1, col).Value = "Group Name";
                col++;

                // Dynamic IPO columns
                foreach (var ipo in ipos)
                {
                    worksheet.Cell(1, col).Value = ipo.IpoName;
                    col++;
                }

                // Summary columns
                worksheet.Cell(1, col).Value = "JV"; col++;
                worksheet.Cell(1, col).Value = "Total"; col++;
                worksheet.Cell(1, col).Value = "Old Collection"; col++;
                worksheet.Cell(1, col).Value = "New Collection"; col++;
                worksheet.Cell(1, col).Value = "Due Amount"; col++;

                int totalColumns = col - 1;

                // Style header row
                var headerRange = worksheet.Range(1, 1, 1, totalColumns);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

                // === DATA ROWS ===
                int row = 2;
                decimal grandJV = 0, grandTotal = 0, grandOldCollection = 0, grandNewCollection = 0, grandDueAmount = 0;

                foreach (var group in groupRows)
                {
                    col = 1;
                    worksheet.Cell(row, col).Value = group.Key.GroupName;
                    col++;

                    decimal rowJV = 0, rowTotal = 0, rowOldCollection = 0, rowNewCollection = 0, rowDueAmount = 0;

                    foreach (var ipo in ipos)
                    {
                        var cell = group.FirstOrDefault(x => x.IpoId == ipo.IpoId);
                        var billing = billingData.FirstOrDefault(b =>
                            b.GroupId == group.Key.GroupId && b.IpoId == ipo.IpoId);

                        decimal credit = cell?.Credit ?? 0m;
                        decimal jv = cell?.JV ?? 0m;
                        decimal billingTotal = billing?.BillingTotal ?? 0m;
                        decimal due = billingTotal - credit;

                        worksheet.Cell(row, col).Value = $"Total: {billingTotal}\nCollection: {credit}\nDue: {due}";
                        worksheet.Cell(row, col).Style.Alignment.WrapText = true;
                        col++;

                        rowJV += jv;
                        rowTotal += billingTotal;
                        rowOldCollection += credit;
                        rowNewCollection += credit;
                        rowDueAmount += due;
                    }

                    worksheet.Cell(row, col).Value = rowJV; col++;
                    worksheet.Cell(row, col).Value = rowTotal; col++;
                    worksheet.Cell(row, col).Value = rowOldCollection; col++;
                    worksheet.Cell(row, col).Value = rowNewCollection; col++;
                    worksheet.Cell(row, col).Value = rowDueAmount; col++;

                    grandJV += rowJV;
                    grandTotal += rowTotal;
                    grandOldCollection += rowOldCollection;
                    grandNewCollection += rowNewCollection;
                    grandDueAmount += rowDueAmount;

                    row++;
                }

                // === FOOTER ROW ===
                col = 1;
                worksheet.Cell(row, col).Value = "Total";
                worksheet.Cell(row, col).Style.Font.Bold = true;
                col++;

                // Footer IPO totals
                foreach (var ipo in ipos)
                {
                    var ipoItems = flatItems.Where(x => x.IpoId == ipo.IpoId).ToList();
                    var ipoBilling = billingData.Where(b => b.IpoId == ipo.IpoId).Sum(b => b.BillingTotal);
                    var ipoCredit = ipoItems.Sum(x => x.Credit);
                    var ipoDue = ipoBilling - ipoCredit;

                    worksheet.Cell(row, col).Value = $"Total: {ipoBilling}\nCollection: {ipoCredit}\nDue: {ipoDue}";
                    worksheet.Cell(row, col).Style.Alignment.WrapText = true;
                    col++;
                }

                worksheet.Cell(row, col).Value = grandJV; col++;
                worksheet.Cell(row, col).Value = grandTotal; col++;
                worksheet.Cell(row, col).Value = grandOldCollection; col++;
                worksheet.Cell(row, col).Value = grandNewCollection; col++;
                worksheet.Cell(row, col).Value = grandDueAmount; col++;

                // Style footer row
                var footerRange = worksheet.Range(row, 1, row, totalColumns);
                footerRange.Style.Font.Bold = true;

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                var file = new FileResponse
                {
                    Bytes = stream.ToArray(),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"Group Wise Dashboard-{DateTime.Now:dd-MM-yyyy-HH-mm}.xlsx"
                };

                return ReturnData<FileResponse>.SuccessResponse(file, "Group wise dashboard exported successfully", 200);
            }
            catch (Exception ex)
            {
                return ReturnData<FileResponse>.ErrorResponse($"Error exporting group wise dashboard: {ex.Message}", 500);
            }
        }
    }
}
