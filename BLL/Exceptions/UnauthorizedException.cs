using System;

namespace SecureBank.BLL.Exceptions
{
    /// <summary>
    /// Thrown when authentication or authorization fails due to invalid credentials or inactive status.
    /// </summary>
    public class UnauthorizedException : BankingException
    {
        public UnauthorizedException(string message) : base(message)
        {
        }

        public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
