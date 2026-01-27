namespace IPOClient.Models.Requests.IPOMaster.Response
{
    public class OrderRemarkResponse
    {
        public int RemarkId { get; set; }
        public int IPOId { get; set; }
        public string Remark { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public bool IsActive { get; set; }
    }
    public class OrderRemarkDTOResponse
    {
        public int RemarkId { get; set; }
        public string Remark { get; set; } = string.Empty;
    }
}
