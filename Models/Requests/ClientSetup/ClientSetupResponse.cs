namespace IPOClient.Models.Requests.ClientSetup
{
    public class ClientSetupResponse
    {
        public int ClientId { get; set; }
        public string PANNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string? GroupName { get; set; }
        public string? ClientDPId { get; set; }
        public int CompanyId { get; set; }
        public bool IsDeleted { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    public class DeleteAllClientsResponse
    {
        public int HistoryId { get; set; }
        public int TotalClientsDeleted { get; set; }
        public DateTime DeletedDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ClientDeleteHistoryResponse
    {
        public int HistoryId { get; set; }
        public DateTime DeletedDate { get; set; }
        public int DeletedBy { get; set; }
        public int CompanyId { get; set; }
        public int TotalClientsDeleted { get; set; }
        public string? Remark { get; set; }
        public List<ClientDeleteHistoryDetailResponse>? Details { get; set; }
    }

    public class ClientDeleteHistoryDetailResponse
    {
        public int DetailId { get; set; }
        public int ClientId { get; set; }
        public string PANNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string? ClientDPId { get; set; }
    }
}
