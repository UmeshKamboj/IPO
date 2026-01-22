namespace IPOClient.Models.Requests
{
    public class CreateUserRequest
    {
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public bool IsAdmin { get; set; } = false;
        public DateTime? ExpiryDate { get; set; }
    }
}
