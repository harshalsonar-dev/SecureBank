using System.Threading;
using System.Threading.Tasks;
using SecureBank.DAL.Models;

namespace SecureBank.DAL.Repositories
{
    /// <summary>
    /// Repository contract for querying user identities and registering accounts.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by their unique username. Returns null if not found.
        /// </summary>
        Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically provisions a new user record and creates their default checking account.
        /// </summary>
        Task<(int UserId, int AccountId, string AccountNumber)> RegisterUserAsync(
            string username, 
            string email, 
            string passwordHash, 
            string fullName, 
            decimal initialDeposit = 0.00m, 
            CancellationToken cancellationToken = default);
    }
}
