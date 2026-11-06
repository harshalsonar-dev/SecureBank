using System;
using System.Collections.Generic;
using SecureBank.DAL.Models;
using SecureBank.Models;
using Xunit;

namespace SecureBank.Tests.Models
{
    public class DomainModelTests
    {
        #region User Model Tests

        [Fact]
        public void User_DefaultValues_AreProperlyInitialized()
        {
            var user = new User();

            Assert.Equal(0, user.UserId);
            Assert.Equal(string.Empty, user.Username);
            Assert.Equal(string.Empty, user.Email);
            Assert.Equal(string.Empty, user.PasswordHash);
            Assert.Equal(string.Empty, user.FullName);
            Assert.Equal("Customer", user.Role);
            Assert.True(user.IsActive);
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockedUntil);
            Assert.True((DateTime.UtcNow - user.CreatedAt).TotalSeconds < 5);
        }

        [Fact]
        public void User_PropertyMutations_StoreAndRetrieveAccurately()
        {
            var lockTime = DateTime.UtcNow.AddMinutes(30);
            var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var user = new User
            {
                UserId = 42,
                Username = "bankteller1",
                Email = "teller@securebank.com",
                PasswordHash = "$2a$12$e8x...",
                FullName = "Bob Teller",
                Role = "BankTeller",
                IsActive = false,
                FailedLoginAttempts = 3,
                LockedUntil = lockTime,
                CreatedAt = created
            };

            Assert.Equal(42, user.UserId);
            Assert.Equal("bankteller1", user.Username);
            Assert.Equal("teller@securebank.com", user.Email);
            Assert.Equal("$2a$12$e8x...", user.PasswordHash);
            Assert.Equal("Bob Teller", user.FullName);
            Assert.Equal("BankTeller", user.Role);
            Assert.False(user.IsActive);
            Assert.Equal(3, user.FailedLoginAttempts);
            Assert.Equal(lockTime, user.LockedUntil);
            Assert.Equal(created, user.CreatedAt);
        }

        #endregion

        #region Account Model Tests

        [Fact]
        public void Account_DefaultValues_AreProperlyInitialized()
        {
            var account = new Account();

            Assert.Equal(0, account.AccountId);
            Assert.Equal(0, account.UserId);
            Assert.Equal(string.Empty, account.AccountNumber);
            Assert.Equal("Checking", account.AccountType);
            Assert.Equal(0.00m, account.Balance);
            Assert.Equal("USD", account.Currency);
            Assert.True(account.IsActive);
            Assert.True((DateTime.UtcNow - account.CreatedAt).TotalSeconds < 5);
        }

        [Fact]
        public void Account_PropertyMutations_StoreAndRetrieveAccurately()
        {
            var created = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
            var account = new Account
            {
                AccountId = 101,
                UserId = 7,
                AccountNumber = "SB-9876543210",
                AccountType = "Savings",
                Balance = 15420.75m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = created
            };

            Assert.Equal(101, account.AccountId);
            Assert.Equal(7, account.UserId);
            Assert.Equal("SB-9876543210", account.AccountNumber);
            Assert.Equal("Savings", account.AccountType);
            Assert.Equal(15420.75m, account.Balance);
            Assert.Equal("USD", account.Currency);
            Assert.True(account.IsActive);
            Assert.Equal(created, account.CreatedAt);
        }

        #endregion

        #region TransactionRecord Model Tests

        [Fact]
        public void TransactionRecord_DefaultValues_AreProperlyInitialized()
        {
            var tx = new TransactionRecord();

            Assert.Equal(0L, tx.TransactionId);
            Assert.Equal(Guid.Empty, tx.TransactionReference);
            Assert.Null(tx.FromAccountId);
            Assert.Null(tx.ToAccountId);
            Assert.Equal(string.Empty, tx.TransactionType);
            Assert.Equal(0.00m, tx.Amount);
            Assert.Equal("Completed", tx.Status);
            Assert.Null(tx.Remarks);
        }

        [Fact]
        public void TransactionRecord_PropertyMutations_StoreAndRetrieveAccurately()
        {
            var refGuid = Guid.NewGuid();
            var time = DateTime.UtcNow;

            var tx = new TransactionRecord
            {
                TransactionId = 5001L,
                TransactionReference = refGuid,
                FromAccountId = 10,
                ToAccountId = 20,
                TransactionType = "Transfer",
                Amount = 750.50m,
                Status = "Completed",
                Remarks = "Wire Settlement",
                CreatedAt = time
            };

            Assert.Equal(5001L, tx.TransactionId);
            Assert.Equal(refGuid, tx.TransactionReference);
            Assert.Equal(10, tx.FromAccountId);
            Assert.Equal(20, tx.ToAccountId);
            Assert.Equal("Transfer", tx.TransactionType);
            Assert.Equal(750.50m, tx.Amount);
            Assert.Equal("Completed", tx.Status);
            Assert.Equal("Wire Settlement", tx.Remarks);
            Assert.Equal(time, tx.CreatedAt);
        }

        #endregion

        #region DashboardViewModel Tests

        [Fact]
        public void DashboardViewModel_Collections_AreInitializedNonNull()
        {
            var model = new DashboardViewModel();

            Assert.NotNull(model.Accounts);
            Assert.Empty(model.Accounts);
            Assert.NotNull(model.RecentTransactions);
            Assert.Empty(model.RecentTransactions);
            Assert.Equal(0.00m, model.TotalBalance);
        }

        [Fact]
        public void DashboardViewModel_AssigningAccountsAndTransactions_MaintainsIntegrity()
        {
            var accounts = new List<Account>
            {
                new() { AccountId = 1, Balance = 200m, IsActive = true },
                new() { AccountId = 2, Balance = 500m, IsActive = true }
            };

            var transactions = new List<TransactionRecord>
            {
                new() { TransactionId = 1, Amount = 100m, TransactionType = "Deposit" }
            };

            var model = new DashboardViewModel
            {
                Username = "janesmith",
                FullName = "Jane Smith",
                Role = "Customer",
                TotalBalance = 700m,
                Accounts = accounts,
                RecentTransactions = transactions
            };

            Assert.Equal("janesmith", model.Username);
            Assert.Equal("Jane Smith", model.FullName);
            Assert.Equal("Customer", model.Role);
            Assert.Equal(700m, model.TotalBalance);
            Assert.Equal(2, model.Accounts.Count);
            Assert.Single(model.RecentTransactions);
        }

        #endregion

        #region ErrorViewModel Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ErrorViewModel_ShowRequestId_WhenNullOrEmpty_ReturnsFalse(string? requestId)
        {
            var model = new ErrorViewModel { RequestId = requestId };
            Assert.False(model.ShowRequestId);
        }

        [Fact]
        public void ErrorViewModel_ShowRequestId_WhenHasValue_ReturnsTrue()
        {
            var model = new ErrorViewModel { RequestId = "req-12345" };
            Assert.True(model.ShowRequestId);
        }

        [Fact]
        public void ErrorViewModel_DefaultMessage_IsNonEmptyUserFriendlyText()
        {
            var model = new ErrorViewModel();
            Assert.False(string.IsNullOrWhiteSpace(model.Message));
            Assert.Contains("unexpected error", model.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
