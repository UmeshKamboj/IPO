namespace IPOClient.Models.Requests.Group
{
    public class GroupResponse
    {
        public int IPOGroupId { get; set; }
        public string? GroupName { get; set; }
        public string? MobileNo { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Remark { get; set; }
        public int? IPOId { get; set; }
        public string? IPOName { get; set; }
        public int CompanyId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
    }
}
