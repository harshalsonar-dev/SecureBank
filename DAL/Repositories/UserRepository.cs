using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SecureBank.DAL.Infrastructure;
using SecureBank.DAL.Models;

namespace SecureBank.DAL.Repositories
{
    /// <summary>
    /// Pure ADO.NET repository querying User entities and provisioning new user accounts.
    /// Excludes try/catch blocks so database exceptions bubble up to the BLL.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public UserRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            const string query = @"
                SELECT 
                    UserId, 
                    Username, 
                    Email, 
                    PasswordHash, 
                    FullName, 
                    Role, 
                    IsActive, 
                    FailedLoginAttempts, 
                    LockedUntil, 
                    CreatedAt
                FROM dbo.Users
                WHERE Username = @Username;";

            using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = new SqlCommand(query, connection))
            {
                command.CommandType = CommandType.Text;

                var usernameParam = new SqlParameter("@Username", SqlDbType.NVarChar, 50)
                {
                    Value = string.IsNullOrWhiteSpace(username) ? (object)DBNull.Value : username
                };
                command.Parameters.Add(usernameParam);

                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return MapUserFromReader(reader);
                    }

                    return null;
                }
            }
        }

        public async Task<(int UserId, int AccountId, string AccountNumber)> RegisterUserAsync(
            string username, 
            string email, 
            string passwordHash, 
            string fullName, 
            decimal initialDeposit = 0.00m, 
            CancellationToken cancellationToken = default)
        {
            using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = new SqlCommand("dbo.sp_RegisterUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 50) { Value = username });
                command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 100) { Value = email });
                command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 255) { Value = passwordHash });
                command.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = fullName });

                var initialDepositParam = new SqlParameter("@InitialDeposit", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = initialDeposit
                };
                command.Parameters.Add(initialDepositParam);

                var userIdOut = new SqlParameter("@UserId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var accountIdOut = new SqlParameter("@AccountId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var accountNumOut = new SqlParameter("@AccountNumber", SqlDbType.VarChar, 20) { Direction = ParameterDirection.Output };

                command.Parameters.Add(userIdOut);
                command.Parameters.Add(accountIdOut);
                command.Parameters.Add(accountNumOut);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                return ((int)userIdOut.Value, (int)accountIdOut.Value, (string)accountNumOut.Value);
            }
        }

        private static User MapUserFromReader(SqlDataReader reader)
        {
            int userIdOrdinal = reader.GetOrdinal("UserId");
            int usernameOrdinal = reader.GetOrdinal("Username");
            int emailOrdinal = reader.GetOrdinal("Email");
            int passwordHashOrdinal = reader.GetOrdinal("PasswordHash");
            int fullNameOrdinal = reader.GetOrdinal("FullName");
            int roleOrdinal = reader.GetOrdinal("Role");
            int isActiveOrdinal = reader.GetOrdinal("IsActive");
            int failedAttemptsOrdinal = reader.GetOrdinal("FailedLoginAttempts");
            int lockedUntilOrdinal = reader.GetOrdinal("LockedUntil");
            int createdAtOrdinal = reader.GetOrdinal("CreatedAt");

            return new User
            {
                UserId = reader.GetInt32(userIdOrdinal),
                Username = reader.GetString(usernameOrdinal),
                Email = !reader.IsDBNull(emailOrdinal) ? reader.GetString(emailOrdinal) : string.Empty,
                PasswordHash = reader.GetString(passwordHashOrdinal),
                FullName = !reader.IsDBNull(fullNameOrdinal) ? reader.GetString(fullNameOrdinal) : string.Empty,
                Role = reader.GetString(roleOrdinal),
                IsActive = reader.GetBoolean(isActiveOrdinal),
                FailedLoginAttempts = !reader.IsDBNull(failedAttemptsOrdinal) ? reader.GetInt32(failedAttemptsOrdinal) : 0,
                LockedUntil = !reader.IsDBNull(lockedUntilOrdinal) ? reader.GetDateTime(lockedUntilOrdinal) : null,
                CreatedAt = reader.GetDateTime(createdAtOrdinal)
            };
        }
    }
}
