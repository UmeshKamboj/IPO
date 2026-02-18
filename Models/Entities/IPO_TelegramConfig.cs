namespace IPOClient.Models.Entities
{
    public class IPO_TelegramConfig
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string? TelegramAPIKey { get; set; }

        public string? MobileNumber { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public IPO_UserMaster? User { get; set; }
    }
}
