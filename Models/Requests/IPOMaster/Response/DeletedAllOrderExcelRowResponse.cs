namespace IPOClient.Models.Requests.IPOMaster.Response
{
    public class DeletedAllOrderExcelRowResponse
    {
        public string GroupName { get; set; }
        public string OrderType { get; set; }
        public string OrderCategory { get; set; }
        public string InvestorType { get; set; }
        public string PremiumStrikePrice { get; set; }
        public int Quantity { get; set; }
        public decimal Rate { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Remarks { get; set; }
    }
}
