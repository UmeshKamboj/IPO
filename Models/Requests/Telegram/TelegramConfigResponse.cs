namespace IPOClient.Models.Requests.Telegram
{
    public class TelegramConfigResponse
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string? TelegramAPIKey { get; set; }

        public string? MobileNumber { get; set; }
    }
}
