namespace IPOClient.Models.Requests.IPOMaster.Response
{
    public class GroupWiseBillingResponse
    {
        public string GroupName { get; set; }

        public CategoryBilling Retail { get; set; } = new();
        public CategoryBilling SHNI { get; set; } = new();
        public CategoryBilling BHNI { get; set; } = new();

        public CategoryBilling_SubjectTo SubjectTo_Retail { get; set; } = new();
        public CategoryBilling_SubjectTo SubjectTo_SHNI { get; set; } = new();
        public CategoryBilling_SubjectTo SubjectTo_BHNI { get; set; } = new();

        public PremiumBilling Premium { get; set; } = new();
        public OptionBilling Options { get; set; } = new();

        public int TotalShares { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class CategoryBilling
    {
        public int Count { get; set; }
        public int Alloted { get; set; }
        public decimal Billing { get; set; }
    }

    public class CategoryBilling_SubjectTo : CategoryBilling { }

    public class PremiumBilling
    {
        public int Shares { get; set; }
        public decimal Billing { get; set; }
    }

    public class OptionBilling
    {
        public decimal CallAmount { get; set; }
        public decimal PutAmount { get; set; }
    }
}
