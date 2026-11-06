using System.Collections.Generic;
using SecureBank.DAL.Models;

namespace SecureBank.Models
{
    public class DashboardViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal TotalBalance { get; set; }
        public List<Account> Accounts { get; set; } = new();
        public List<TransactionRecord> RecentTransactions { get; set; } = new();
    }
}
