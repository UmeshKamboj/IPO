using IPOClient.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPOClient.Models.Requests.PaymentTransaction
{
    public class CreateIPOToIPOPaymentRequest
    {
        // Side A
        [Required(ErrorMessage = "Group is required.")]
        public int GroupId1 { get; set; }
        [Required(ErrorMessage = "IPO is required.")]
        public int IpoId1 { get; set; }
        [Required(ErrorMessage = "Amount type is required.")]
        public int AmountType1 { get; set; }   // Debit / Credit

        [Required(ErrorMessage = "Remark is required.")]
        public string Remark1 { get; set; }

        // Side B
        [Required(ErrorMessage = "Group is required.")]
        public int GroupId2 { get; set; }
        [Required(ErrorMessage = "IPO is required.")]
        public int IpoId2 { get; set; }
        [Required(ErrorMessage = "Amount type is required.")]
        public int AmountType2 { get; set; }   // Debit / Credit
        [Required(ErrorMessage = "Remark is required.")]
        public string Remark2 { get; set; }
        [Required(ErrorMessage = "Amount is required")]
        public decimal Amount { get; set; }
        [Required(ErrorMessage = "Date is required")]
        public DateTime TransactionDate { get; set; }
    }
}
