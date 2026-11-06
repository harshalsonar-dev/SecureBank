using System;

namespace SecureBank.BLL.Exceptions
{
    /// <summary>
    /// Thrown when an account does not exist or has been flagged inactive.
    /// </summary>
    public class AccountNotFoundException : BankingException
    {
        public int? AccountId { get; }

        public AccountNotFoundException(string message) : base(message)
        {
        }

        public AccountNotFoundException(string message, int accountId) : base(message)
        {
            AccountId = accountId;
        }

        public AccountNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
