using IPOClient.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Requests.PaymentTransaction
{
    public class CreatePaymentRequest
    {
        [Required(ErrorMessage = "Group is required.")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "IPO is required.")]
        public int IpoId { get; set; }

        [Required(ErrorMessage = "Amount type is required.")]
        public int AmountType { get; set; } // Amount type Enum use

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        public string? Remark { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime TransactionDate { get; set; }

        public bool IsJV { get; set; } = false;
    }
}
