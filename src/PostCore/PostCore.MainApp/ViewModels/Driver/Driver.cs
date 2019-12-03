using System.Collections.Generic;
using PostCore.Core.Branches;
using PostCore.Core.Mail;
using PostCore.Utils;

namespace PostCore.MainApp.ViewModels.Driver
{
    public class MailViewModel
    {
        public IEnumerable<Branch> AllBranches { get; set; }
        public PaginatedList<Post> Mail { get; set; }
        public ListOptions CurrentListOptions { get; set; }
        public string ReturnUrl { get; set; }
    }
}
