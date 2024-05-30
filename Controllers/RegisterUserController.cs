using DemoIdentityProject.Models.Entity;
using DemoIdentityProject.Models.ViewModels;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.Security.Cryptography;
using System.Text;

namespace DemoIdentityProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RegisterUserController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<UserAuthenticationController> _logger;
        private readonly UserManager<User> _userManager;

        public RegisterUserController(SignInManager<User> signInManager, ILogger<UserAuthenticationController> logger, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Register model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                string randomPassword = GenerateRandomPassword();

                User user = new()
                {
                    Name = model.Name,
                    UserName = model.Email,
                    Email = model.Email,
                    Address = model.Address,
                    IsUsingTemporaryPassword = true
                };

                var result = await _userManager.CreateAsync(user, randomPassword);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation("User created a new account with password.");

                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    var email = new MimeMessage();
                    email.From.Add(new MailboxAddress("Admin", "tuan.tranvan3012@gmail.com"));
                    email.To.Add(new MailboxAddress(user.Name, user.Email));
                    email.Subject = "Confirm your email";
                    var builder = new BodyBuilder();
                    builder.TextBody = $"Hello {user.Name},\n\n" +
                        $"Thank you for registering. Please click the following link to confirm your email and activate your account:\n\n" +
                        $"{Url.Action("ConfirmEmail", "RegisterUser", new { userId = user.Id, token }, Request.Scheme)}\n\n" +
                        $"Your temporary password is: {randomPassword}\n\n" +
                        $"Once confirmed, you can log in using this password and then change it.\n\n" +
                        $"Best regards,\n" +
                        $"Your Application Team";
                    email.Body = builder.ToMessageBody();

                    using var client = new SmtpClient();
                    client.Connect("smtp.gmail.com", 587);
                    client.Authenticate("tuan.tranvan3012@gmail.com", "cglafnccorfdqfce");
                    client.Send(email);
                    client.Disconnect(true);

                    _logger.LogInformation("Email confirmation sent to user.");

                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
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

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("User confirmed email successfully.");
                return View("ConfirmEmail");
            }
            else
            {
                throw new ApplicationException($"Error confirming email for user with ID '{userId}':");
            }
        }

        public string GenerateRandomPassword()
        {
            int length = 12;
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder sb = new StringBuilder();
            byte[] randomBytes = new byte[length * 4]; // Each character requires 4 bytes

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);

                for (int i = 0; i < length; i++)
                {
                    int randomNumber = BitConverter.ToInt32(randomBytes, i * 4);
                    int index = Math.Abs(randomNumber % validChars.Length);
                    sb.Append(validChars[index]);
                }
            }

            return sb.ToString();
        }
    }
}
