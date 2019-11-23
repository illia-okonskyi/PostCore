using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core;
using PostCore.Core.Services;

namespace PostCore.ViewUtils.ViewComponents
{
    public class MyBranchViewModel
    {
        public bool Visible { get; set; }
        public string MyBranchName { get; set; }
        public string CurrentUrl { get; set; }
    }

    public class MyBranch : ViewComponent
    {
        private readonly IMyBranchService _myBranchService;

        public MyBranch(IMyBranchService myBranchService)
        {
            _myBranchService = myBranchService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userLoggedIn = User.Identity.IsAuthenticated;
            var vm = new MyBranchViewModel
            {
                Visible = userLoggedIn
            };
            if (!userLoggedIn)
            {
                return View(vm);
            }

            vm.MyBranchName = (await _myBranchService.GetMyBranchAsync())?.Name ?? "<No branch>";
            vm.CurrentUrl = Request.PathAndQuery();
            return View(vm);
        }
    }
}
