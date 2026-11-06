using System.Threading;
using System.Threading.Tasks;
using SecureBank.DAL.Models;
using SecureBank.Models;

namespace SecureBank.BLL.Services
{
    /// <summary>
    /// Service contract for user authentication, registration, and credential security.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user using secure BCrypt password verification.
        /// </summary>
        Task<User> AuthenticateUserAsync(string username, string plainTextPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates registration details, hashes password via BCrypt, and provisions user + checking account.
        /// </summary>
        Task<(int UserId, int AccountId, string AccountNumber)> RegisterUserAsync(RegisterViewModel model, CancellationToken cancellationToken = default);
    }
}
