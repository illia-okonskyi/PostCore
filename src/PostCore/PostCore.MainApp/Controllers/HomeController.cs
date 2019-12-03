using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core.Mail;
using PostCore.Core.Services.Dao;
using PostCore.MainApp.ViewModels.Home;

namespace PostCore.MainApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMailDao _mailDao;

        public HomeController(IMailDao mailDao)
        {
            _mailDao = mailDao;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> MyPostStatus(long postId)
        {
            var post = await _mailDao.GetByIdAsync(postId);
            return View(new MyPostStatusViewModel
            {
                PostId = postId,
                HasInformation = post != null,
                PersonFrom = post?.PersonFrom,
                PersonTo = post?.PersonTo,
                AddressTo = post?.AddressTo,
                SourceBranchName = post?.SourceBranch.Name,
                DestinationBranchName = post?.DestinationBranch.Name,
                CurrentBranchName = post?.Branch?.Name,
                PostState = post != null ? PostStateToString(post.State) : null
            });
        }

        string PostStateToString(PostState state)
        {
            switch (state)
            {
                case PostState.Created:
                    return "Created";
                case PostState.InBranchStock:
                    return "In branch stock";
                case PostState.InDeliveryToBranchStock:
                    return "In delivery to branch stock";
                case PostState.InDeviveryToPerson:
                    return "In delivery to person";
                case PostState.Delivered:
                    return "Delivered";

                default:
                    return "- Unknown -";
            }
        }
    }
}
