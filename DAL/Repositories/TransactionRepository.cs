using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SecureBank.DAL.Infrastructure;
using SecureBank.DAL.Models;

namespace SecureBank.DAL.Repositories
{
    /// <summary>
    /// Pure ADO.NET repository executing financial transactions and queries via Stored Procedures.
    /// Excludes try/catch blocks to ensure raw SqlException bubbles up directly to BLL for domain handling.
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public TransactionRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<Guid> TransferFundsAsync(
            int fromAccountId,
            int toAccountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = new SqlCommand("dbo.sp_TransferFunds", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@FromAccountId", SqlDbType.Int) { Value = fromAccountId });
                command.Parameters.Add(new SqlParameter("@ToAccountId", SqlDbType.Int) { Value = toAccountId });

                var amountParam = new SqlParameter("@Amount", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = amount
                };
                command.Parameters.Add(amountParam);

                command.Parameters.Add(new SqlParameter("@Remarks", SqlDbType.NVarChar, 255)
                {
                    Value = (object?)remarks ?? DBNull.Value
                });

                command.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.NVarChar, 50)
                {
                    Value = (object?)performedBy ?? "System"
                });

                command.Parameters.Add(new SqlParameter("@IPAddress", SqlDbType.VarChar, 45)
                {
                    Value = (object?)ipAddress ?? DBNull.Value
                });

                var refParam = new SqlParameter("@TransactionReference", SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(refParam);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                return (Guid)refParam.Value;
            }
        }

        public async Task<Guid> DepositFundsAsync(
            int accountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = new SqlCommand("dbo.sp_DepositFunds", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@AccountId", SqlDbType.Int) { Value = accountId });

                var amountParam = new SqlParameter("@Amount", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = amount
                };
                command.Parameters.Add(amountParam);

                command.Parameters.Add(new SqlParameter("@Remarks", SqlDbType.NVarChar, 255)
                {
                    Value = (object?)remarks ?? DBNull.Value
                });

                command.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.NVarChar, 50)
                {
                    Value = (object?)performedBy ?? "System"
                });

                command.Parameters.Add(new SqlParameter("@IPAddress", SqlDbType.VarChar, 45)
                {
                    Value = (object?)ipAddress ?? DBNull.Value
                });

                var refParam = new SqlParameter("@TransactionReference", SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(refParam);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                return (Guid)refParam.Value;
            }
        }

        public async Task<Guid> WithdrawFundsAsync(
            int accountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = new SqlCommand("dbo.sp_WithdrawFunds", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@AccountId", SqlDbType.Int) { Value = accountId });

                var amountParam = new SqlParameter("@Amount", SqlDbType.Decimal)
                {
                    Precision = 18,
                    Scale = 2,
                    Value = amount
                };
                command.Parameters.Add(amountParam);

                command.Parameters.Add(new SqlParameter("@Remarks", SqlDbType.NVarChar, 255)
                {
                    Value = (object?)remarks ?? DBNull.Value
                });

                command.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.NVarChar, 50)
                {
                    Value = (object?)performedBy ?? "System"
                });

                command.Parameters.Add(new SqlParameter("@IPAddress", SqlDbType.VarChar, 45)
                {
                    Value = (object?)ipAddress ?? DBNull.Value
                });

                var refParam = new SqlParameter("@TransactionReference", SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(refParam);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                return (Guid)refParam.Value;
            }
        }

        public async Task<List<Account>> GetAccountsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var accounts = new List<Account>();

            using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = new SqlCommand("dbo.sp_GetUserAccounts", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    int accountIdOrd = reader.GetOrdinal("AccountId");
                    int userIdOrd = reader.GetOrdinal("UserId");
                    int accNumOrd = reader.GetOrdinal("AccountNumber");
                    int accTypeOrd = reader.GetOrdinal("AccountType");
                    int balanceOrd = reader.GetOrdinal("Balance");
                    int currencyOrd = reader.GetOrdinal("Currency");
                    int isActiveOrd = reader.GetOrdinal("IsActive");
                    int createdAtOrd = reader.GetOrdinal("CreatedAt");

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        accounts.Add(new Account
                        {
                            AccountId = reader.GetInt32(accountIdOrd),
                            UserId = reader.GetInt32(userIdOrd),
                            AccountNumber = reader.GetString(accNumOrd),
                            AccountType = reader.GetString(accTypeOrd),
                            Balance = reader.GetDecimal(balanceOrd),
                            Currency = reader.GetString(currencyOrd),
                            IsActive = reader.GetBoolean(isActiveOrd),
                            CreatedAt = reader.GetDateTime(createdAtOrd)
                        });
                    }
                }
            }

            return accounts;
        }

        public async Task<List<TransactionRecord>> GetRecentTransactionsByUserIdAsync(int userId, int limit = 10, CancellationToken cancellationToken = default)
        {
            var transactions = new List<TransactionRecord>();

            using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var command = new SqlCommand("dbo.sp_GetRecentTransactions", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                command.Parameters.Add(new SqlParameter("@Limit", SqlDbType.Int) { Value = limit });

                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    int txIdOrd = reader.GetOrdinal("TransactionId");
                    int txRefOrd = reader.GetOrdinal("TransactionReference");
                    int fromAccOrd = reader.GetOrdinal("FromAccountId");
                    int toAccOrd = reader.GetOrdinal("ToAccountId");
                    int txTypeOrd = reader.GetOrdinal("TransactionType");
                    int amountOrd = reader.GetOrdinal("Amount");
                    int statusOrd = reader.GetOrdinal("Status");
                    int remarksOrd = reader.GetOrdinal("Remarks");
                    int createdAtOrd = reader.GetOrdinal("CreatedAt");

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        transactions.Add(new TransactionRecord
                        {
                            TransactionId = reader.GetInt64(txIdOrd),
                            TransactionReference = reader.GetGuid(txRefOrd),
                            FromAccountId = !reader.IsDBNull(fromAccOrd) ? reader.GetInt32(fromAccOrd) : null,
                            ToAccountId = !reader.IsDBNull(toAccOrd) ? reader.GetInt32(toAccOrd) : null,
                            TransactionType = reader.GetString(txTypeOrd),
                            Amount = reader.GetDecimal(amountOrd),
                            Status = reader.GetString(statusOrd),
                            Remarks = !reader.IsDBNull(remarksOrd) ? reader.GetString(remarksOrd) : null,
                            CreatedAt = reader.GetDateTime(createdAtOrd)
                        });
                    }
                }
            }

            return transactions;
        }
    }
}
