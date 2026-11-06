using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SecureBank.DAL.Infrastructure
{
    /// <summary>
    /// Thread-safe ADO.NET connection factory that creates and opens resilient SqlConnection instances.
    /// Utilizes SQL Server connection pooling underneath.
    /// </summary>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Database connection string cannot be null or empty.", nameof(connectionString));
            }
            _connectionString = connectionString;
        }

        /// <summary>
        /// Creates and synchronously opens a new SqlConnection.
        /// </summary>
        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Creates and asynchronously opens a new SqlConnection.
        /// </summary>
        public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
    }
}
