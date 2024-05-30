using DemoIdentityProject.Models.ViewModels;
using DemoIdentityProject.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoIdentityProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [Authorize]
        public IActionResult ChangeTemporaryPassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangeTemporaryPassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _accountService.ChangeTemporaryPasswordAsync(model);
            if (result)
            {
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "An error occurred while changing the password.");
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Register(Register model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                string randomPassword = _accountService.GenerateRandomPassword();

                try
                {
                    var userId = await _accountService.RegisterUserAsync(model, randomPassword);
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Failed to register user: {ex.Message}");
                }
            }
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            bool isConfirmed = await _accountService.ConfirmEmailAsync(userId, token);

            if (isConfirmed)
            {
                return View("ConfirmEmail");
            }
            else
            {
                throw new ApplicationException($"Error confirming email for user with ID '{userId}':");
            }
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            if (ModelState.IsValid)
            {
                if (await _accountService.LoginWithTemporaryPasswordAsync(model))
                {
                    return RedirectToAction("ChangeTemporaryPassword");
                }

                if (await _accountService.LoginAsync(model))
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            return RedirectToAction("Login");
        }
    }
}
