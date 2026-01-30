namespace IPOClient.Models.Requests.PaymentTransaction
{
    public class PaymentTransactionResponse
    {
        public int PaymentTransactionId { get; set; }
        public int GroupId { get; set; }
        public string? GroupName { get; set; }
        public int IpoId { get; set; }
        public string? IPOName { get; set; }

        public int AmountType { get; set; } // Amount type Enum use

        public string? AmountTypeName { get; set; }

        public decimal Amount { get; set; }

        public string? Remark { get; set; }

        public DateTime TransactionDate { get; set; }

        public bool IsJV { get; set; } = false;
    }
}
