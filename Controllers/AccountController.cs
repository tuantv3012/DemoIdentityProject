using DemoIdentityProject.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DemoIdentityProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword!);
            if (changePasswordResult.Succeeded)
            {
                user.IsUsingTemporaryPassword = false;
                await _userManager.UpdateAsync(user);

                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
