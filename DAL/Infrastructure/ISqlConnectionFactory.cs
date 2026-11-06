using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SecureBank.DAL.Infrastructure
{
    /// <summary>
    /// Factory abstraction for creating and opening database connections.
    /// Decouples repository classes from concrete ADO.NET connection construction.
    /// </summary>
    public interface ISqlConnectionFactory
    {
        /// <summary>
        /// Creates and opens a new database connection synchronously.
        /// </summary>
        IDbConnection CreateConnection();

        /// <summary>
        /// Creates and opens a new SQL Server connection asynchronously.
        /// </summary>
        Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
    }
}
