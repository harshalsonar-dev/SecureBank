using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SecureBank.BLL.Exceptions;
using SecureBank.DAL.Repositories;
using SecureBank.Models;

namespace SecureBank.BLL.Services
{
    /// <summary>
    /// Implements financial business logic, domain validations, translation of
    /// low-level SQL exceptions, and dashboard data aggregation.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
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
            if (amount <= 0.00m)
            {
                throw new BankingException("Transfer amount must be strictly greater than zero.");
            }

            if (fromAccountId == toAccountId)
            {
                throw new BankingException("Source and destination accounts cannot be identical.");
            }

            if (fromAccountId <= 0 || toAccountId <= 0)
            {
                throw new BankingException("Invalid source or destination account ID specified.");
            }

            try
            {
                return await _transactionRepository.TransferFundsAsync(
                    fromAccountId,
                    toAccountId,
                    amount,
                    remarks,
                    performedBy,
                    ipAddress,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                throw MapSqlExceptionToBankingException(ex);
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
            if (amount <= 0.00m)
            {
                throw new BankingException("Deposit amount must be strictly greater than zero.");
            }

            if (accountId <= 0)
            {
                throw new BankingException("Invalid account ID specified for deposit.");
            }

            try
            {
                return await _transactionRepository.DepositFundsAsync(
                    accountId,
                    amount,
                    remarks,
                    performedBy,
                    ipAddress,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                throw MapSqlExceptionToBankingException(ex);
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
            if (amount <= 0.00m)
            {
                throw new BankingException("Withdrawal amount must be strictly greater than zero.");
            }

            if (accountId <= 0)
            {
                throw new BankingException("Invalid account ID specified for withdrawal.");
            }

            try
            {
                return await _transactionRepository.WithdrawFundsAsync(
                    accountId,
                    amount,
                    remarks,
                    performedBy,
                    ipAddress,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                throw MapSqlExceptionToBankingException(ex);
            }
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(
            int userId, 
            string username, 
            string role, 
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new BankingException("Invalid user ID provided for dashboard summary.");
            }

            try
            {
                var accounts = await _transactionRepository.GetAccountsByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
                var recentTransactions = await _transactionRepository.GetRecentTransactionsByUserIdAsync(userId, 10, cancellationToken).ConfigureAwait(false);

                decimal totalBalance = accounts.Where(a => a.IsActive).Sum(a => a.Balance);

                return new DashboardViewModel
                {
                    Username = username,
                    Role = role,
                    TotalBalance = totalBalance,
                    Accounts = accounts,
                    RecentTransactions = recentTransactions
                };
            }
            catch (SqlException ex)
            {
                throw MapSqlExceptionToBankingException(ex);
            }
        }

        private static BankingException MapSqlExceptionToBankingException(SqlException ex)
        {
            string message = ex.Message;

            if (message.Contains("Insufficient funds", StringComparison.OrdinalIgnoreCase))
            {
                return new InsufficientFundsException(message, ex);
            }

            if (message.Contains("does not exist or is inactive", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("invalid or inactive", StringComparison.OrdinalIgnoreCase))
            {
                return new AccountNotFoundException(message, ex);
            }

            return new BankingException($"Banking transaction error: {message}", ex);
        }
    }
}
