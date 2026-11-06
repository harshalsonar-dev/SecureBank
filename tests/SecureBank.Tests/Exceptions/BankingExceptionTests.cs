using System;
using SecureBank.BLL.Exceptions;
using Xunit;

namespace SecureBank.Tests.Exceptions
{
    public class BankingExceptionTests
    {
        [Fact]
        public void BankingException_MessageConstructor_SetsMessageCorrectly()
        {
            var ex = new BankingException("Financial limit exceeded");
            Assert.Equal("Financial limit exceeded", ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void BankingException_InnerExceptionConstructor_PreservesInnerException()
        {
            var inner = new InvalidOperationException("DB timeout");
            var ex = new BankingException("Operation failed", inner);

            Assert.Equal("Operation failed", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void InsufficientFundsException_Constructors_SetPropertiesCorrectly()
        {
            var ex1 = new InsufficientFundsException("Not enough balance");
            Assert.Equal("Not enough balance", ex1.Message);
            Assert.Null(ex1.RequestedAmount);
            Assert.Null(ex1.AvailableBalance);

            var ex2 = new InsufficientFundsException("Overdraft denied", 500.00m, 120.50m);
            Assert.Equal("Overdraft denied", ex2.Message);
            Assert.Equal(500.00m, ex2.RequestedAmount);
            Assert.Equal(120.50m, ex2.AvailableBalance);

            var inner = new Exception("Engine abort");
            var ex3 = new InsufficientFundsException("Engine error", inner);
            Assert.Same(inner, ex3.InnerException);
        }

        [Fact]
        public void AccountNotFoundException_Constructors_SetPropertiesCorrectly()
        {
            var ex1 = new AccountNotFoundException("Account does not exist");
            Assert.Equal("Account does not exist", ex1.Message);
            Assert.Null(ex1.AccountId);

            var ex2 = new AccountNotFoundException("Account inactive", 999);
            Assert.Equal("Account inactive", ex2.Message);
            Assert.Equal(999, ex2.AccountId);

            var inner = new Exception("FK violation");
            var ex3 = new AccountNotFoundException("Not found", inner);
            Assert.Same(inner, ex3.InnerException);
        }

        [Fact]
        public void UnauthorizedException_Constructors_SetPropertiesCorrectly()
        {
            var ex1 = new UnauthorizedException("Invalid credentials");
            Assert.Equal("Invalid credentials", ex1.Message);

            var inner = new Exception("Crypto error");
            var ex2 = new UnauthorizedException("Auth failure", inner);
            Assert.Same(inner, ex2.InnerException);
        }

        [Fact]
        public void AllCustomExceptions_InheritFromBankingException()
        {
            Assert.IsAssignableFrom<BankingException>(new InsufficientFundsException("test"));
            Assert.IsAssignableFrom<BankingException>(new AccountNotFoundException("test"));
            Assert.IsAssignableFrom<BankingException>(new UnauthorizedException("test"));
        }
    }
}
