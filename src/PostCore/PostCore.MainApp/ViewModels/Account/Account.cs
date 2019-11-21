using System.ComponentModel.DataAnnotations;
using PostCore.Core.Users;

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

    public class ManageViewModel
    {
        [Required]
        [UIHint("HiddenInput")]
        public long UserId { get; set; }

        [Required]
        [Display(Name = "User name")]
        [UIHint("Text")]
        [RegularExpression(
            pattern: User.UserNameRegex,
            ErrorMessage = "Not valid user name")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "E-mail")]
        [UIHint("EmailAddress")]
        [RegularExpression(
            pattern: User.EmailRegex,
            ErrorMessage = "Not valid email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required]
        [UIHint("Password")]
        public string Password { get; set; }

        [UIHint("HiddenInput")]
        public string ReturnUrl { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [UIHint("HiddenInput")]
        public long UserId { get; set; }

        [Required]
        [Display(Name = "Current password")]
        [UIHint("Password")]
        public string CurrentPassword { get; set; }

        [Required]
        [Display(Name = "New password")]
        [UIHint("Password")]
        public string NewPassword { get; set; }

        [UIHint("Password")]
        [Display(Name = "Repeat new password")]
        [Compare(nameof(NewPassword))]
        public string NewPasswordRepeat { get; set; }

        [UIHint("HiddenInput")]
        public string ReturnUrl { get; set; }
    }
}
