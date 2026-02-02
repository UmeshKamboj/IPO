namespace IPOClient.Models.Requests.PaymentTransaction
{
    public class IPOtoIPOPaymentResponse
    {
        public int PaymentTransactionId1 { get; set; }
        public int PaymentTransactionId2 { get; set; }
        public int GroupId1 { get; set; }
        public int GroupId2 { get; set; }
        public string? GroupName1 { get; set; }
        public string? GroupName2 { get; set; }
        public int IpoId1 { get; set; }
        public int IpoId2 { get; set; }
        public string? IPOName1 { get; set; }
        public string? IPOName2 { get; set; }

        public int AmountType1 { get; set; } // Amount type Enum use
        public int AmountType2 { get; set; } // Amount type Enum use

        public string? AmountTypeName1 { get; set; }
        public string? AmountTypeName2 { get; set; }

        public decimal Amount { get; set; }

        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }

        public DateTime TransactionDate { get; set; }

    }
}
