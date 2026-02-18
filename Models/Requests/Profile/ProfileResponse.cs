namespace IPOClient.Models.Requests.Profile
{
    public class ProfileResponse
    {
        public int Id { get; set; }

        public string? FName { get; set; }

        public string? LName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public bool IsAdmin { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
