using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SecureBank.DAL.Models;

namespace SecureBank.DAL.Repositories
{
    /// <summary>
    /// Repository contract for executing financial transactions and querying ledgers using ADO.NET.
    /// </summary>
    public interface ITransactionRepository
    {
        /// <summary>
        /// Atomically transfers funds between two bank accounts via dbo.sp_TransferFunds.
        /// </summary>
        Task<Guid> TransferFundsAsync(
            int fromAccountId,
            int toAccountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically deposits funds into a bank account via dbo.sp_DepositFunds.
        /// </summary>
        Task<Guid> DepositFundsAsync(
            int accountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically withdraws funds from a bank account via dbo.sp_WithdrawFunds.
        /// </summary>
        Task<Guid> WithdrawFundsAsync(
            int accountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all bank accounts belonging to a user.
        /// </summary>
        Task<List<Account>> GetAccountsByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the most recent financial transactions associated with a user's accounts.
        /// </summary>
        Task<List<TransactionRecord>> GetRecentTransactionsByUserIdAsync(int userId, int limit = 10, CancellationToken cancellationToken = default);
    }
}
