using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PostCore.Core.Users;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.ViewModels.Users
{
    public class IndexViewModel
    {
        public PaginatedList<User> Users { get; set; }
        public ListOptions CurrentListOptions { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class EditViewModel
    {
        public IEnumerable<Role> AllRoles { get; set; }
        [UIHint("HiddenInput")]
        public bool IsAdminUser { get; set; }

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

        [Required]
        [Display(Name = "User role")]
        public long RoleId { get; set; }

        [Required]
        [UIHint("HiddenInput")]
        public string ReturnUrl { get; set; }
    }
}
