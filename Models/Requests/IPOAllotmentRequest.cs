namespace IPOClient.Models.Requests
{
    public class IPOAllotmentCheckRequest
    {
        public string Registrar { get; set; } = string.Empty; // Linkin, Kfintech, BigShare, etc.
        public string CompanyCode { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for bulk allotment check from the IPO Allotment Check dialog
    /// </summary>
    public class BulkAllotmentCheckRequest
    {
        public string Registrar { get; set; } = string.Empty;   // Linkin, Kfintech, BigShare, etc.
        public string CompanyCode { get; set; } = string.Empty;  // IPO company code from registrar
        public int IpoId { get; set; }                           // IPO ID in our system
        public string PanFilter { get; set; } = "all";           // "all" = All PAN Records (Whole Allotment), "pending" = Pending PAN Records (Pending Blank Allotment)
    }

    /// <summary>
    /// Request for Firm Allotment dialog
    /// </summary>
    public class FirmAllotmentRequest
    {
        public int IpoId { get; set; }
        public int GroupId { get; set; }
        /// <summary>
        /// Investor type filter: 0 or null = All, 1 = Retail, 2 = SHNI, 3 = BHNI
        /// Filters Kostak + SubjectTo orders for the selected investor type
        /// </summary>
        public int? InvestorType { get; set; }
        public int AllotedQty { get; set; }
    }
}
