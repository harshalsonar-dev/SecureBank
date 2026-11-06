using System;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.Data.SqlClient;
using SecureBank.BLL.Exceptions;
using SecureBank.DAL.Models;
using SecureBank.DAL.Repositories;
using SecureBank.Models;

namespace SecureBank.BLL.Services
{
    /// <summary>
    /// Service responsible for validating user credentials and provisioning user accounts
    /// with BCrypt.Net-Next password hashing.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<User> AuthenticateUserAsync(string username, string plainTextPassword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(plainTextPassword))
            {
                throw new UnauthorizedException("Invalid username or password.");
            }

            var user = await _userRepository.GetUserByUsernameAsync(username.Trim(), cancellationToken).ConfigureAwait(false);

            if (user == null)
            {
                throw new UnauthorizedException("Invalid username or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedException("User account is inactive or locked. Please contact bank administration.");
            }

            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(plainTextPassword, user.PasswordHash);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedException("Authentication failed due to password verification error.", ex);
            }

            if (!isPasswordValid)
            {
                throw new UnauthorizedException("Invalid username or password.");
            }

            return user;
        }

        public async Task<(int UserId, int AccountId, string AccountNumber)> RegisterUserAsync(RegisterViewModel model, CancellationToken cancellationToken = default)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                throw new BankingException("Username and Password are required.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                throw new BankingException("Password and confirmation password do not match.");
            }

            if (model.InitialDeposit < 0)
            {
                throw new BankingException("Initial deposit cannot be negative.");
            }

            // Cryptographically hash password using BCrypt with work factor 12
            string passwordHash;
            try
            {
                passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12);
            }
            catch (Exception ex)
            {
                throw new BankingException("Password hashing security routine failed.", ex);
            }

            try
            {
                return await _userRepository.RegisterUserAsync(
                    model.Username.Trim(),
                    model.Email.Trim().ToLowerInvariant(),
                    passwordHash,
                    model.FullName.Trim(),
                    model.InitialDeposit,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("already taken", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BankingException("The chosen username is already taken. Please choose another.");
                }
                if (ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BankingException("An account is already registered with this email address.");
                }

                throw new BankingException($"Registration failed: {ex.Message}", ex);
            }
        }
    }
}
