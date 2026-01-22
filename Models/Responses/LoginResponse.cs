namespace IPOClient.Models.Responses
{
    public class LoginResponse
    {
        public string? Token { get; set; } // Access token (15 min)
        public string? RefreshToken { get; set; } // Refresh token (7 days) 
        public UserResponse? User { get; set; } // User details
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsAdmin { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
