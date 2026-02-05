namespace IPOClient.Models.Requests.IPOBackup
{
    public class AllOrderExcelRowResponse
    {
        public string GroupName { get; set; }
        public string OrderType { get; set; }
        public int OrderTypeId { get; set; }
        public string OrderCategory { get; set; }
        public string InvestorType { get; set; }
        public int Quantity { get; set; }
        public int AllotedQty { get; set; }
        public decimal PreOpenPrice { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
    }
    public class IPOOrderBackupData
    {
        public int IPOId { get; set; }
        public List<AllOrderExcelRowResponse> Orders { get; set; } = new();
    }
}
