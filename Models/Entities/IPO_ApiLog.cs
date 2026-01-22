namespace IPOClient.Models.Entities
{
    /// <summary>
    /// Lightweight error log model - only stores failed requests (4xx, 5xx)
    /// </summary>
    public class IPO_ApiLog
    {
        public int Id { get; set; }
        public string? Method { get; set; } // GET, POST, PUT, DELETE
        public string? Path { get; set; } // API endpoint path
        public string? QueryString { get; set; }
        public int? StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int? UserId { get; set; }
        public string? IpAddress { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public long DurationMs { get; set; } // Request duration in milliseconds

        // Note: RequestBody and ResponseBody removed for performance
        // Error-only logging doesn't need full request/response capture
    }
}
