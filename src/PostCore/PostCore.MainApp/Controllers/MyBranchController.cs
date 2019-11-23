using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.MainApp.ViewModels.MyBranch;

namespace PostCore.MainApp.Controllers
{
    [Authorize]
    public class MyBranchController : Controller
    {
        private readonly IBranchesDao _branhesDao;
        private readonly IMyBranchService _myBranchService;

        public MyBranchController(
            IBranchesDao branhesDao,
            IMyBranchService myBranchService)
        {
            _branhesDao = branhesDao;
            _myBranchService = myBranchService;
        }

        public async Task<IActionResult> Index(string returnUrl)
        {
            return View(new IndexViewModel
            {
                MyBranch = await _myBranchService.GetMyBranchAsync(),
                Branches = (await _branhesDao.GetAllAsync()).ToList(),
                ReturnUrl = returnUrl ?? HttpContext.Request.PathAndQuery()
            });
        }

        [HttpPost]
        public IActionResult SetMyBranch(long id, string returnUrl)
        {
            _myBranchService.SetMyBranch(id);
            return Redirect(returnUrl);
        }
    }
}
