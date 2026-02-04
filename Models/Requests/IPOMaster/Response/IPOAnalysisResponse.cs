namespace IPOClient.Models.Requests.IPOMaster.Response
{
    public class IPOAnalysisResponse
    {
        public int AnalysisType { get; set; }
        public decimal IPOPricePerShare { get; set; }

        // Input fields - stored in DB and returned for form pre-fill
        public decimal ExpectedApplications_Retail { get; set; }
        public decimal ExpectedApplications_SHNI { get; set; }
        public decimal ExpectedApplications_BHNI { get; set; }
        public decimal ActualAllottedQty_Total { get; set; }
        public decimal ActualAllottedQty_Retail { get; set; }
        public decimal ActualAllottedQty_SHNI { get; set; }
        public decimal ActualAllottedQty_BHNI { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal SpotPremium { get; set; }
        public decimal? SpotPrice { get; set; }

        // Per investor type subscription data (Tab 1 & 2)
        public Dictionary<string, SubscriptionBlock> Subscriptions { get; set; } = new();

        // Kostak/SubjectTo rates per investor type
        public Dictionary<string, RateBlock> KostakRates { get; set; } = new();
        public Dictionary<string, RateBlock> SubjectToRates { get; set; } = new();

        // Order Count tables (BUY/SELL/NET Count per category per investor type)
        public Dictionary<string, CategoryStatusBlock> KostakCount { get; set; } = new();
        public Dictionary<string, CategoryStatusBlock> SubjectToCount { get; set; } = new();
        public PremiumStatusBlock PremiumCount { get; set; } = new();

        // Share Qty tables
        public Dictionary<string, CategoryStatusBlock> KostakShareQty { get; set; } = new();
        public Dictionary<string, CategoryStatusBlock> SubjectToShareQty { get; set; } = new();
        public PremiumStatusBlock PremiumShareQty { get; set; } = new();

        // Difference Qty (To Hedge)
        public decimal DifferenceQtyToHedge { get; set; }

        // Tab 3 only
        public decimal? ProfitOrLoss { get; set; }
    }

    public class SubscriptionBlock
    {
        public decimal ExpectedApplications { get; set; }
        public int ApplicationForOneTime { get; set; }
        public decimal AvgSharePerApplication { get; set; }
        public decimal IPOSubscription { get; set; }
    }

    public class RateBlock
    {
        public decimal WithoutProfitMargin { get; set; }
        public decimal WithProfitMargin { get; set; }
    }
}
