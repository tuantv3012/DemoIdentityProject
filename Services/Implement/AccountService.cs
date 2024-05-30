using DemoIdentityProject.Configuration;
using DemoIdentityProject.Models.Entity;
using DemoIdentityProject.Models.ViewModels;
using DemoIdentityProject.Services.Interface;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Security.Cryptography;
using System.Text;

namespace DemoIdentityProject.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AccountService> _logger;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly EmailSettings _emailSettings;

        public AccountService(UserManager<User> userManager, SignInManager<User> signInManager, IHttpContextAccessor httpContextAccessor, ILogger<AccountService> logger, IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor, IOptions<EmailSettings> emailSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
            _emailSettings = emailSettings.Value;
        }

        public async Task<bool> ChangeTemporaryPasswordAsync(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext!.User);
            if (user == null)
            {
                return false;
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword!);
            if (changePasswordResult.Succeeded)
            {
                user.IsUsingTemporaryPassword = false;
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
                return true;
            }

            foreach (var error in changePasswordResult.Errors)
            {
                _logger.LogError(error.Description);
            }

            return false;
        }

        public async Task<bool> LoginWithTemporaryPasswordAsync(Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                if (user.IsUsingTemporaryPassword)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> LoginAsync(Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return true;
                }
            }
            return false;
        }

        public async Task LogoutAsync()
        {
            _logger.LogInformation("User logged out.");
            await _signInManager.SignOutAsync();
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("User confirmed email successfully.");
                return true;
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

        public async Task<string> RegisterUserAsync(Register model, string randomPassword)
        {
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

                var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext!);

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                email.To.Add(new MailboxAddress(user.Name, user.Email));
                email.Subject = "Confirm your email";
                var builder = new BodyBuilder();
                builder.TextBody = $"Hello {user.Name},\n\n" +
                    $"Thank you for registering. Please click the following link to confirm your email and activate your account:\n\n" +
                    $"{urlHelper.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, _actionContextAccessor.ActionContext!.HttpContext.Request.Scheme)}\n\n" +
                    $"Your temporary password is: {randomPassword}\n\n" +
                    $"Once confirmed, you can log in using this password and then change it.\n\n" +
                    $"Best regards,\n" +
                    $"Your Application Team";
                email.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                client.Connect(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
                client.Authenticate(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                client.Send(email);
                client.Disconnect(true);

                _logger.LogInformation("Email confirmation sent to user.");

                return user.Id;
            }
            else
            {
                throw new ApplicationException($"Failed to register user: {result.Errors}");
            }
        }
    }
}
