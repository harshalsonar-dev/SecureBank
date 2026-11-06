using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureBank.BLL.Exceptions;
using SecureBank.BLL.Services;
using SecureBank.Filters;
using SecureBank.Models;

namespace SecureBank.Controllers
{
    /// <summary>
    /// Presentation controller for financial operations (Dashboard, Transfer, Deposit, Withdraw).
    /// Gated by SessionAuthorize filter and protected by Anti-Forgery tokens on all mutating POST requests.
    /// Interacts solely with ITransactionService.
    /// </summary>
    [SessionAuthorize]
    public class TransactionController : Controller
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        }

        #region Dashboard & History

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            string username = HttpContext.Session.GetString("Username") ?? "Valued Customer";
            string role = HttpContext.Session.GetString("Role") ?? "Customer";

            try
            {
                var model = await _transactionService.GetDashboardDataAsync(userId.Value, username, role);
                return View(model);
            }
            catch (BankingException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(new DashboardViewModel { Username = username, Role = role });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Unable to retrieve dashboard balances at this time.";
                return View(new DashboardViewModel { Username = username, Role = role });
            }
        }

        #endregion

        #region Fund Transfer

        [HttpGet]
        public IActionResult Transfer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(int fromAccountId, int toAccountId, decimal amount, string? remarks)
        {
            try
            {
                string performedBy = HttpContext.Session.GetString("Username") ?? "UnknownUser";
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var transactionReference = await _transactionService.TransferFundsAsync(
                    fromAccountId,
                    toAccountId,
                    amount,
                    remarks ?? string.Empty,
                    performedBy,
                    ipAddress);

                TempData["SuccessMessage"] = $"Funds transferred successfully! Reference ID: {transactionReference}";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (BankingException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while processing the transfer. Please try again.";
                return View();
            }
        }

        #endregion

        #region Deposit

        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(int accountId, decimal amount, string? remarks)
        {
            try
            {
                string performedBy = HttpContext.Session.GetString("Username") ?? "UnknownUser";
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var transactionReference = await _transactionService.DepositFundsAsync(
                    accountId,
                    amount,
                    remarks ?? string.Empty,
                    performedBy,
                    ipAddress);

                TempData["SuccessMessage"] = $"Deposit completed successfully! Reference ID: {transactionReference}";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (BankingException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while processing the deposit. Please try again.";
                return View();
            }
        }

        #endregion

        #region Withdrawal

        [HttpGet]
        public IActionResult Withdraw()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int accountId, decimal amount, string? remarks)
        {
            try
            {
                string performedBy = HttpContext.Session.GetString("Username") ?? "UnknownUser";
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var transactionReference = await _transactionService.WithdrawFundsAsync(
                    accountId,
                    amount,
                    remarks ?? string.Empty,
                    performedBy,
                    ipAddress);

                TempData["SuccessMessage"] = $"Withdrawal completed successfully! Reference ID: {transactionReference}";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (BankingException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while processing the withdrawal. Please try again.";
                return View();
            }
        }

        #endregion
    }
}
