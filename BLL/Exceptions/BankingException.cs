using System;

namespace SecureBank.BLL.Exceptions
{
    /// <summary>
    /// Base exception class for all domain-level financial and banking rule violations.
    /// </summary>
    public class BankingException : Exception
    {
        public BankingException(string message) : base(message)
        {
        }

        public BankingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
