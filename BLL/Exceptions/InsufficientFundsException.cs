using System;

namespace SecureBank.BLL.Exceptions
{
    /// <summary>
    /// Thrown when an account has inadequate balance to service a withdrawal or transfer request.
    /// </summary>
    public class InsufficientFundsException : BankingException
    {
        public decimal? RequestedAmount { get; }
        public decimal? AvailableBalance { get; }

        public InsufficientFundsException(string message) : base(message)
        {
        }

        public InsufficientFundsException(string message, decimal requestedAmount, decimal availableBalance)
            : base(message)
        {
            RequestedAmount = requestedAmount;
            AvailableBalance = availableBalance;
        }

        public InsufficientFundsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
