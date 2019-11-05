using System.ComponentModel.DataAnnotations;
using PostCore.Core.Users;
using PostCore.ViewUtils;

namespace PostCore.MainApp.ViewModels
{
    public class EditViewModel
    {
        [UIHint("HiddenInput")]
        public EditorMode EditorMode { get; set; }

        [Required]
        [UIHint("HiddenInput")]
        public long Id { get; set; }

        [Required]
        [Display(Name = "User name")]
        [UIHint("Text")]
        [RegularExpression(
            pattern:User.UserNameRegex,
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
    }
}
