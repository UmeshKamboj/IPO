using System.ComponentModel.DataAnnotations;

namespace IPOClient.Models.Requests.IPOMaster.Request
{
    public class EditIPOOrderRequest
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Group is required")]
        public int GroupId { get; set; }
        public int OrderType { get; set; }      // BUY / SELL
        public int OrderCategory { get; set; }  // Retail / SHNI / BHNI / Premium / Call / Put / Subject To
        public int InvestorType { get; set; }   // OPTIONS / PREMIUM / BHNI / SHNI
        public string? PremiumStrikePrice { get; set; }
        public bool ApplicateRate { get; set; } = false; // If true: Premium, If false: Application
        public int Quantity { get; set; }
        public decimal Rate { get; set; }
        public string? RemarksIds { get; set; } //comma separated remark IDs

        [Required(ErrorMessage = "Date and time is required")]
        public DateTime DateTime { get; set; }
    }
}
