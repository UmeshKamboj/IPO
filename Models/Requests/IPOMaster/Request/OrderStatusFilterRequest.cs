namespace IPOClient.Models.Requests.IPOMaster.Request
{
    public class OrderStatusFilterRequest
    {
        public int IPOId { get; set; }
        public int? GroupId { get; set; }          // optional
        public int? OrderCategory { get; set; }    // optional
        public int? InvestorType { get; set; }     // optional
    }
}
