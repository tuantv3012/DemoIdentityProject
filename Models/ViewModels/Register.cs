using System.ComponentModel.DataAnnotations;

namespace DemoIdentityProject.Models.ViewModels
{
    public class Register
    {
        [Required]
        public string? Name { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Address { get; set; }
    }
}
