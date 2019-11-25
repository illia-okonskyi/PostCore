using System.ComponentModel.DataAnnotations;
using PostCore.Core.Cars;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.ViewModels.Cars
{
    public class IndexViewModel
    {
        public PaginatedList<Car> Cars { get; set; }
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
        [Display(Name = "Car model")]
        [UIHint("Text")]
        public string Model { get; set; }

        [Required]
        [Display(Name = "Car number")]
        [UIHint("Text")]
        public string Number { get; set; }

        [Required]
        [UIHint("HiddenInput")]
        public string ReturnUrl { get; set; }
    }
}
