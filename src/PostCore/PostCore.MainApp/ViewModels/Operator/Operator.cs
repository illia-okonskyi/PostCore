using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PostCore.Core.Branches;
using PostCore.Core.Mail;
using PostCore.Utils;

namespace PostCore.MainApp.ViewModels.Operator
{
    public class CreatePostViewModel
    {
        public IEnumerable<Branch> AllBranches;

        [Required]
        [Display(Name = "Person from")]
        public string PersonFrom { get; set; }

        [Required]
        [Display(Name = "Person to")]
        public string PersonTo { get; set; }

        [Required]
        [Display(Name = "Destination branch")]
        public long? DestinationBranchId { get; set; }

        [Required]
        [Display(Name = "Address to")]
        public string AddressTo { get; set; }
    }

    public class DeliverPostViewModel
    {
        public PaginatedList<Post> Mail;
        public ListOptions CurrentListOptions { get; set; }
        public string ReturnUrl { get; set; }
    }
}
