using System.ComponentModel.DataAnnotations;
using PostCore.Core.Branches;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.ViewModels.Branches
{
    public class IndexViewModel
    {
        public PaginatedList<Branch> Branches { get; set; }
        public ListOptions CurrentListOptions { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class EditViewModel
    {
        [UIHint("HiddenInput")]
        public EditorMode EditorMode { get; set; }

        [Required]
        [UIHint("HiddenInput")]
        public long Id { get; set; }

        [Required]
        [Display(Name = "Branch name")]
        [UIHint("Text")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Branch address")]
        [UIHint("Text")]
        public string Address { get; set; }

        [Required]
        [UIHint("HiddenInput")]
        public string ReturnUrl { get; set; }
    }
}
