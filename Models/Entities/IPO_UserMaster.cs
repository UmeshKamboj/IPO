namespace IPOClient.Models.Entities
{
    public class IPO_UserMaster
    {
        public int Id { get; set; }

        public string? FName { get; set; }

        public string? LName { get; set; }

        public string? Email { get; set; } // Used as username

        public string? Password { get; set; } // Encrypted

        public string? Phone { get; set; }

        public bool IsAdmin { get; set; } = false;

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }
    }
}
