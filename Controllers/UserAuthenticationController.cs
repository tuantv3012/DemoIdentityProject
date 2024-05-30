using DemoIdentityProject.Models.Entity;
using DemoIdentityProject.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DemoIdentityProject.Controllers
{
    public class UserAuthenticationController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<UserAuthenticationController> _logger;
        private readonly UserManager<User> _userManager;

        public UserAuthenticationController(SignInManager<User> signInManager, ILogger<UserAuthenticationController> logger, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Username!);
                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password!))
                {
                    if (user.IsUsingTemporaryPassword)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("ChangeTemporaryPassword", "Account");
                    }

                    var result = await _signInManager.PasswordSignInAsync(model.Username!, model.Password!, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User logged out.");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "UserAuthentication");
        }
    }
}
