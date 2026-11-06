using System;

namespace SecureBank.DAL.Models
{
    public class TransactionRecord
    {
        public long TransactionId { get; set; }
        public Guid TransactionReference { get; set; }
        public int? FromAccountId { get; set; }
        public int? ToAccountId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Completed";
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
