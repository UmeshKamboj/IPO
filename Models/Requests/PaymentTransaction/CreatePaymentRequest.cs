using IPOClient.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace IPOClient.Models.Requests.PaymentTransaction
{
    public class CreatePaymentRequest
    {
        [Required(ErrorMessage = "Group is required.")]
        public int GroupId { get; set; }


        public int IpoId { get; set; } = 0; // Optional, default to 0 for non-IPO transactions

        [Required(ErrorMessage = "Amount type is required.")]
        public AmountType AmountType { get; set; } // Amount type Enum use - accepts both int (1,2) and string ("Debit","Credit")

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        public string? Remark { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime TransactionDate { get; set; }

        public bool IsJV { get; set; } = false;
    }
    public class PaymentListRequest: PaginationRequest
    {
       public int? IpoId { get; set; }
       public int? GroupId { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }

        public DateTime? GetFromDate()
        {
            return DateTime.TryParse(FromDate, out var dt) ? dt : (DateTime?)null;
        }

        public DateTime? GetToDate()
        {
            return DateTime.TryParse(ToDate, out var dt) ? dt : (DateTime?)null;
        }


    }

    public class DeletedAccountingListRequest : PaginationRequest
    {
        public int? IpoId { get; set; }
        public int? GroupId { get; set; }

        /// <summary>
        /// Date range as single string e.g. "10-02-2026 to 14-02-2026"
        /// </summary>
        public string? DateRange { get; set; }

        public bool IsDeleted { get; set; } = true;

        private static readonly string[] DateFormats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };

        public DateTime? GetFromDate()
        {
            if (string.IsNullOrWhiteSpace(DateRange)) return null;
            var parts = DateRange.Split("to", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1) return null;
            return DateTime.TryParseExact(parts[0], DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt
                : (DateTime?)null;
        }

        public DateTime? GetToDate()
        {
            if (string.IsNullOrWhiteSpace(DateRange)) return null;
            var parts = DateRange.Split("to", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;
            return DateTime.TryParseExact(parts[1], DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt
                : (DateTime?)null;
        }
    }
}
