using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureBank.BLL.Exceptions;
using SecureBank.BLL.Services;
using SecureBank.Models;

namespace SecureBank.Controllers
{
    /// <summary>
    /// Handles user authentication, credential submission, registration, session creation, and logout.
    /// Interacts solely with IAuthService (strictly separated from the DAL).
    /// </summary>
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        #region Login

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Dashboard", "Transaction");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Please enter both username and password.";
                return View();
            }

            try
            {
                var user = await _authService.AuthenticateUserAsync(username, password);

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);

                return RedirectToAction("Dashboard", "Transaction");
            }
            catch (UnauthorizedException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred during authentication. Please try again.";
                return View();
            }
        }

        #endregion

        #region Registration

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Dashboard", "Transaction");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (_, _, accountNumber) = await _authService.RegisterUserAsync(model);

                TempData["SuccessMessage"] = $"Registration successful! Your checking account ({accountNumber}) has been opened. Please sign in.";
                return RedirectToAction("Login");
            }
            catch (BankingException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred during registration. Please verify your details and try again.";
                return View(model);
            }
        }

        #endregion

        #region Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        #endregion
    }
}
