namespace IPOClient.Models.Requests.IPOMaster.Request
{
    public class GroupWiseBillingRequest
    {
        public string SearchValue { get; set; } = string.Empty;
        public int Skip { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }
}
