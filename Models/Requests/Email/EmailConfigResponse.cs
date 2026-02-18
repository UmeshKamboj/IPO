namespace IPOClient.Models.Requests.Email
{
    public class EmailConfigResponse
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string? Email { get; set; }

        public string? AppPassword { get; set; }
    }
}
