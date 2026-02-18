namespace IPOClient.Models.Entities
{
    public class IPO_EmailConfig
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string? Email { get; set; }

        public string? AppPassword { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public IPO_UserMaster? User { get; set; }
    }
}
