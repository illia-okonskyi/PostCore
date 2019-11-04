using System.ComponentModel.DataAnnotations;
using PostCore.ViewUtils;

namespace PostCore.MainApp.ViewModels
{
    public class EditViewModel
    {
        public EditorMode EditorMode { get; set; }

        [Required]
        [UIHint("HiddenInput")]
        public long Id { get; set; }

        [Required]
        [Display(Name = "E-mail")]
        [UIHint("EmailAddress")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; }
    }
}
