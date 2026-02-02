namespace IPOClient.Models.Requests.GroupWiseDashboard
{
    public class GroupWiseDashboardGridResponse
    {
        //public List<IpoHeaderDto> Ipos { get; set; } = new();
        public List<GroupRowDto> Rows { get; set; } = new();
        public SummaryFooterDto Footer { get; set; } = new();
    }
    public class IpoHeaderDto
    {
        public int IpoId { get; set; }
        public string IpoName { get; set; } = "";
    }
    public class GroupRowDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = "";

        public List<IpoAmount> IpoData { get; set; } = new();

        public decimal Total { get; set; }
        public decimal Collection { get; set; }
        public decimal Due { get; set; }
    }
    public class IpoAmount
    {
        public int IpoId { get; set; }
        public string IpoName { get; set; } = "";
        public decimal Total { get; set; }
        public decimal Collection { get; set; }
        public decimal Due { get; set; }
    }
    
    public class SummaryFooterDto
    {
        public List<IpoAmount> IpoTotals { get; set; } = new();
        public decimal GrandTotal { get; set; }
        public decimal GrandCollection { get; set; }
        public decimal GrandDue { get; set; }
    }
}
