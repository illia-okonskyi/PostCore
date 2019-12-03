using PostCore.Core.Mail;
using PostCore.Utils;

namespace PostCore.MainApp.ViewModels.Courier
{
    public class MailViewModel
    {
        public PaginatedList<Post> Mail { get; set; }
        public ListOptions CurrentListOptions { get; set; }
        public string ReturnUrl { get; set; }
    }
}
