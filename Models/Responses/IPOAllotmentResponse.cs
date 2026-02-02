namespace IPOClient.Models.Responses
{
    public class IPOAllotmentCompany
    {
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
    }

    public class IPOAllotmentResult
    {
        public string Status { get; set; } = string.Empty; // Allotted / Not Allotted
        public string ApplicationNumber { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public int AllottedShares { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Registrar { get; set; } = string.Empty;
        public string? DematNumber { get; set; }
    }

    /// <summary>
    /// Response for bulk allotment check
    /// </summary>
    public class BulkAllotmentCheckResponse
    {
        public int TotalPANs { get; set; }
        public int Processed { get; set; }
        public int Allotted { get; set; }
        public int NotAllotted { get; set; }
        public int Failed { get; set; }
        public int Updated { get; set; }
        public List<AllotmentPanResult> Results { get; set; } = new();
    }

    public class AllotmentPanResult
    {
        public int POChildId { get; set; }
        public string PanNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Allotted / Not Allotted / Error
        public int AllottedShares { get; set; }
        public string? DematNumber { get; set; }
        public string? ApplicationNo { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
