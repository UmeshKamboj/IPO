namespace IPOClient.Models.Requests.GroupWiseDashboard
{
    public class GroupIpoSummaryRow
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = "";

        public int IpoId { get; set; }
        public string IpoName { get; set; } = "";

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal JV { get; set; }
    }

    /// <summary>
    /// Order billing total per group per IPO (from BUY/SELL orders)
    /// </summary>
    public class GroupIpoBillingRow
    {
        public int GroupId { get; set; }
        public int IpoId { get; set; }
        public decimal BillingTotal { get; set; }
    }
}
