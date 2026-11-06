using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SecureBank.BLL.Exceptions;
using SecureBank.BLL.Services;
using SecureBank.DAL.Models;
using SecureBank.DAL.Repositories;
using Xunit;

namespace SecureBank.Tests.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _mockRepo;
        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            _mockRepo = new Mock<ITransactionRepository>();
            _service = new TransactionService(_mockRepo.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TransactionService(null!));
        }

        #endregion

        #region TransferFundsAsync Tests

        [Theory]
        [InlineData(0.00)]
        [InlineData(-0.01)]
        [InlineData(-500.00)]
        public async Task TransferFundsAsync_WhenAmountNonPositive_ThrowsBankingException(double amount)
        {
            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.TransferFundsAsync(1, 2, (decimal)amount, "Test", "User", "127.0.0.1"));

            Assert.Contains("strictly greater than zero", ex.Message, StringComparison.OrdinalIgnoreCase);
            _mockRepo.Verify(r => r.TransferFundsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TransferFundsAsync_WhenFromAndToAccountsAreSame_ThrowsBankingException()
        {
            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.TransferFundsAsync(5, 5, 100.00m, "Test", "User", "127.0.0.1"));

            Assert.Contains("cannot be identical", ex.Message, StringComparison.OrdinalIgnoreCase);
            _mockRepo.Verify(r => r.TransferFundsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(-1, 2)]
        [InlineData(1, 0)]
        [InlineData(1, -5)]
        public async Task TransferFundsAsync_WhenInvalidAccountIds_ThrowsBankingException(int fromId, int toId)
        {
            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.TransferFundsAsync(fromId, toId, 100.00m, "Test", "User", "127.0.0.1"));

            Assert.Contains("Invalid source or destination", ex.Message, StringComparison.OrdinalIgnoreCase);
            _mockRepo.Verify(r => r.TransferFundsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TransferFundsAsync_WhenValidInputs_CallsRepositoryAndReturnsGuid()
        {
            var expectedGuid = Guid.NewGuid();
            _mockRepo.Setup(r => r.TransferFundsAsync(10, 20, 250.75m, "Settlement", "Admin", "192.168.1.1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedGuid);

            var result = await _service.TransferFundsAsync(10, 20, 250.75m, "Settlement", "Admin", "192.168.1.1");

            Assert.Equal(expectedGuid, result);
            _mockRepo.Verify(r => r.TransferFundsAsync(10, 20, 250.75m, "Settlement", "Admin", "192.168.1.1", It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region DepositFundsAsync Tests

        [Theory]
        [InlineData(0.00)]
        [InlineData(-10.00)]
        public async Task DepositFundsAsync_WhenAmountNonPositive_ThrowsBankingException(double amount)
        {
            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.DepositFundsAsync(1, (decimal)amount, "Deposit", "Teller", "127.0.0.1"));

            Assert.Contains("strictly greater than zero", ex.Message, StringComparison.OrdinalIgnoreCase);
            _mockRepo.Verify(r => r.DepositFundsAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task DepositFundsAsync_WhenInvalidAccountId_ThrowsBankingException(int accountId)
        {
            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.DepositFundsAsync(accountId, 100.00m, "Deposit", "Teller", "127.0.0.1"));

            Assert.Contains("Invalid account ID", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DepositFundsAsync_WhenValidInputs_CallsRepositoryAndReturnsGuid()
        {
            var expectedGuid = Guid.NewGuid();
            _mockRepo.Setup(r => r.DepositFundsAsync(15, 500.00m, "Cash Deposit", "Teller1", "10.0.0.1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedGuid);

            var result = await _service.DepositFundsAsync(15, 500.00m, "Cash Deposit", "Teller1", "10.0.0.1");

            Assert.Equal(expectedGuid, result);
            _mockRepo.Verify(r => r.DepositFundsAsync(15, 500.00m, "Cash Deposit", "Teller1", "10.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region WithdrawFundsAsync Tests

        [Theory]
        [InlineData(0.00)]
        [InlineData(-5.00)]
        public async Task WithdrawFundsAsync_WhenAmountNonPositive_ThrowsBankingException(double amount)
        {
            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.WithdrawFundsAsync(1, (decimal)amount, "ATM", "Customer", "127.0.0.1"));

            Assert.Contains("strictly greater than zero", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public async Task WithdrawFundsAsync_WhenInvalidAccountId_ThrowsBankingException(int accountId)
        {
            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.WithdrawFundsAsync(accountId, 50.00m, "ATM", "Customer", "127.0.0.1"));

            Assert.Contains("Invalid account ID", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task WithdrawFundsAsync_WhenValidInputs_CallsRepositoryAndReturnsGuid()
        {
            var expectedGuid = Guid.NewGuid();
            _mockRepo.Setup(r => r.WithdrawFundsAsync(8, 200.00m, "ATM Cash", "User", "127.0.0.1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedGuid);

            var result = await _service.WithdrawFundsAsync(8, 200.00m, "ATM Cash", "User", "127.0.0.1");

            Assert.Equal(expectedGuid, result);
            _mockRepo.Verify(r => r.WithdrawFundsAsync(8, 200.00m, "ATM Cash", "User", "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetDashboardDataAsync Tests

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task GetDashboardDataAsync_WhenInvalidUserId_ThrowsBankingException(int userId)
        {
            await Assert.ThrowsAsync<BankingException>(() =>
                _service.GetDashboardDataAsync(userId, "test", "Customer"));
        }

        [Fact]
        public async Task GetDashboardDataAsync_ComputesTotalBalance_AcrossOnlyActiveAccounts()
        {
            var accounts = new List<Account>
            {
                new() { AccountId = 1, Balance = 500m, IsActive = true },
                new() { AccountId = 2, Balance = 1500m, IsActive = true },
                new() { AccountId = 3, Balance = 3000m, IsActive = false } // Inactive account should not count
            };

            var transactions = new List<TransactionRecord>
            {
                new() { TransactionId = 1, Amount = 100m, TransactionType = "Deposit" }
            };

            _mockRepo.Setup(r => r.GetAccountsByUserIdAsync(42, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(accounts);
            _mockRepo.Setup(r => r.GetRecentTransactionsByUserIdAsync(42, 10, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(transactions);

            var result = await _service.GetDashboardDataAsync(42, "alex", "Customer");

            Assert.NotNull(result);
            Assert.Equal("alex", result.Username);
            Assert.Equal("Customer", result.Role);
            Assert.Equal(2000m, result.TotalBalance); // 500 + 1500 (ignoring 3000 inactive)
            Assert.Equal(3, result.Accounts.Count);
            Assert.Single(result.RecentTransactions);
        }

        #endregion
    }
}
