namespace IPOClient.Models.Requests.IPOMaster.Response
{
    public class OrderStatusSummaryResponse
    {
        public Dictionary<string, CategoryStatusBlock> Kostak { get; set; } = new();
        public Dictionary<string, CategoryStatusBlock> SubjectTo { get; set; } = new();
        public PremiumStatusBlock Premium { get; set; } = new();
        public List<StrikePriceBlock> StrikePrices { get; set; } = new();
    }
    public class CategoryStatusBlock
    {
        public StatusValueBlock Buy { get; set; } = new();
        public StatusValueBlock Sell { get; set; } = new();
        public StatusValueBlock Net { get; set; } = new();
    }
    public class StrikePriceBlock
    {
        public decimal StrikePrice { get; set; }

        public int Call_TotalShare { get; set; }
        public decimal Call_Avg { get; set; }
        public decimal Call_Amount { get; set; }

        public int Put_TotalShare { get; set; }
        public decimal Put_Avg { get; set; }
        public decimal Put_Amount { get; set; }
    }
    public class StatusValueBlock
    {
        public int Count { get; set; }
        public decimal Avg { get; set; }
        public decimal Amount { get; set; }
    }
    public class PremiumStatusBlock
    {
        public StatusValueBlock Buy { get; set; } = new();
        public StatusValueBlock Sell { get; set; } = new();
        public StatusValueBlock Net { get; set; } = new();
    }
}
