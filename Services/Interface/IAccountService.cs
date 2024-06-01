using DemoIdentityProject.Models.ViewModels;
using System.Threading.Tasks;

namespace DemoIdentityProject.Services.Interface
{
    public interface IAccountService
    {
        Task<bool> ChangeTemporaryPasswordAsync(ChangePasswordViewModel model);
        Task<string> LoginAsync(Login model);
        Task LogoutAsync();
        Task<string> RegisterUserAsync(Register model, string randomPassword);
        Task<bool> ConfirmEmailAsync(string userId, string token);
        string GenerateRandomPassword();
    }
}
