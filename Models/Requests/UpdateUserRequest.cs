namespace IPOClient.Models.Requests
{
    public class UpdateUserRequest
    {
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string? Phone { get; set; }
        public bool? IsAdmin { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
