using System.ComponentModel.DataAnnotations;

namespace PostCore.MainApp.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required]
        [UIHint("EmailAddress")]
        public string Email { get; set; }

        [Required]
        [UIHint("Password")]
        public string Password { get; set; }

        [UIHint("HiddenInput")]
        public string ReturnUrl { get; set; }
    }
}
