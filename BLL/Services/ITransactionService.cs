using System;
using System.Threading;
using System.Threading.Tasks;
using SecureBank.Models;

namespace SecureBank.BLL.Services
{
    /// <summary>
    /// Service contract defining banking operations including transfers, deposits, withdrawals, and dashboard aggregation.
    /// </summary>
    public interface ITransactionService
    {
        Task<Guid> TransferFundsAsync(
            int fromAccountId,
            int toAccountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default);

        Task<Guid> DepositFundsAsync(
            int accountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default);

        Task<Guid> WithdrawFundsAsync(
            int accountId,
            decimal amount,
            string remarks,
            string performedBy,
            string ipAddress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Aggregates user accounts, computes total net balance, and retrieves recent transaction history.
        /// </summary>
        Task<DashboardViewModel> GetDashboardDataAsync(
            int userId, 
            string username, 
            string role, 
            CancellationToken cancellationToken = default);
    }
}
