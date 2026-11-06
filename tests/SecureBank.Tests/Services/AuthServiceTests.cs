using System;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using Moq;
using SecureBank.BLL.Exceptions;
using SecureBank.BLL.Services;
using SecureBank.DAL.Models;
using SecureBank.DAL.Repositories;
using SecureBank.Models;
using Xunit;

namespace SecureBank.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _mockRepo = new Mock<IUserRepository>();
            _service = new AuthService(_mockRepo.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthService(null!));
        }

        #endregion

        #region AuthenticateUserAsync Tests

        [Theory]
        [InlineData(null, "Password123")]
        [InlineData("", "Password123")]
        [InlineData("   ", "Password123")]
        [InlineData("user", null)]
        [InlineData("user", "")]
        public async Task AuthenticateUserAsync_WhenUsernameOrPasswordMissing_ThrowsUnauthorizedException(string? username, string? password)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _service.AuthenticateUserAsync(username!, password!));

            _mockRepo.Verify(r => r.GetUserByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateUserAsync_WhenUserNotFound_ThrowsUnauthorizedException()
        {
            _mockRepo.Setup(r => r.GetUserByUsernameAsync("nonexistent", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _service.AuthenticateUserAsync("nonexistent", "Secret123"));

            Assert.Equal("Invalid username or password.", ex.Message);
        }

        [Fact]
        public async Task AuthenticateUserAsync_WhenUserIsInactive_ThrowsUnauthorizedException()
        {
            var user = new User
            {
                UserId = 1,
                Username = "lockeduser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret123"),
                IsActive = false
            };

            _mockRepo.Setup(r => r.GetUserByUsernameAsync("lockeduser", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(user);

            var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _service.AuthenticateUserAsync("lockeduser", "Secret123"));

            Assert.Contains("inactive or locked", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AuthenticateUserAsync_WhenPasswordDoesNotMatch_ThrowsUnauthorizedException()
        {
            var user = new User
            {
                UserId = 1,
                Username = "activeuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                IsActive = true
            };

            _mockRepo.Setup(r => r.GetUserByUsernameAsync("activeuser", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(user);

            var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _service.AuthenticateUserAsync("activeuser", "WrongPassword"));

            Assert.Equal("Invalid username or password.", ex.Message);
        }

        [Fact]
        public async Task AuthenticateUserAsync_WhenPasswordMatches_ReturnsUser()
        {
            string password = "CorrectPassword123!";
            var user = new User
            {
                UserId = 10,
                Username = "activeuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10),
                Role = "Customer",
                IsActive = true
            };

            _mockRepo.Setup(r => r.GetUserByUsernameAsync("activeuser", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(user);

            var result = await _service.AuthenticateUserAsync("activeuser", password);

            Assert.NotNull(result);
            Assert.Equal(10, result.UserId);
            Assert.Equal("activeuser", result.Username);
        }

        #endregion

        #region RegisterUserAsync Tests

        [Fact]
        public async Task RegisterUserAsync_WhenModelIsNull_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.RegisterUserAsync(null!));
        }

        [Theory]
        [InlineData("", "pass")]
        [InlineData("   ", "pass")]
        [InlineData("user", "")]
        public async Task RegisterUserAsync_WhenUsernameOrPasswordEmpty_ThrowsBankingException(string user, string pass)
        {
            var model = new RegisterViewModel
            {
                Username = user,
                Password = pass,
                ConfirmPassword = pass,
                Email = "test@example.com"
            };

            await Assert.ThrowsAsync<BankingException>(() =>
                _service.RegisterUserAsync(model));
        }

        [Fact]
        public async Task RegisterUserAsync_WhenPasswordMismatch_ThrowsBankingException()
        {
            var model = new RegisterViewModel
            {
                Username = "newuser",
                Password = "PasswordA",
                ConfirmPassword = "PasswordB",
                Email = "test@example.com"
            };

            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.RegisterUserAsync(model));

            Assert.Contains("do not match", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RegisterUserAsync_WhenInitialDepositNegative_ThrowsBankingException()
        {
            var model = new RegisterViewModel
            {
                Username = "newuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Email = "test@example.com",
                InitialDeposit = -50.00m
            };

            var ex = await Assert.ThrowsAsync<BankingException>(() =>
                _service.RegisterUserAsync(model));

            Assert.Contains("cannot be negative", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RegisterUserAsync_WhenValid_HashesWithBCryptAndCallsRepository()
        {
            string plainPassword = "SecurePassword@2026";
            var model = new RegisterViewModel
            {
                Username = "NewCustomer",
                Email = "Customer@Example.Com",
                FullName = "Alice Wonder",
                Password = plainPassword,
                ConfirmPassword = plainPassword,
                InitialDeposit = 250.00m
            };

            string capturedHash = string.Empty;

            _mockRepo.Setup(r => r.RegisterUserAsync(
                "NewCustomer",
                "customer@example.com",
                It.IsAny<string>(),
                "Alice Wonder",
                250.00m,
                It.IsAny<CancellationToken>()))
                .Callback<string, string, string, string, decimal, CancellationToken>((u, e, hash, f, d, ct) => capturedHash = hash)
                .ReturnsAsync((101, 501, "SB-1234567890"));

            var result = await _service.RegisterUserAsync(model);

            Assert.Equal(101, result.UserId);
            Assert.Equal(501, result.AccountId);
            Assert.Equal("SB-1234567890", result.AccountNumber);

            // Verify BCrypt hash validity
            Assert.False(string.IsNullOrWhiteSpace(capturedHash));
            Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, capturedHash));
        }

        #endregion
    }
}
