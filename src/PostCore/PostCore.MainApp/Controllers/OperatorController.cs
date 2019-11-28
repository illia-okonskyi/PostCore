using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core;
using PostCore.Core.Mail;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels.Message;
using PostCore.MainApp.ViewModels.Operator;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles = Role.Authorize.Operator)]
    public class OperatorController : Controller
    {
        public static readonly long PageSize = 25;

        private static readonly ListOptions DefaultDeliverPostListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"id", ""},
                {"personFrom", ""},
                {"personTo", "" }
            },
            SortKey = "Id",
            SortOrder = SortOrder.Ascending
        };

        private readonly ICurrentUserService _currentUserService;
        private readonly IBranchesDao _branchesDao;
        private readonly IMailDao _mailDao;
        public OperatorController(
            ICurrentUserService currentUserService,
            IBranchesDao branchesDao,
            IMailDao mailDao)
        {
            _currentUserService = currentUserService;
            _branchesDao = branchesDao;
            _mailDao = mailDao;
        }

        public async Task<IActionResult> CreatePost()
        {
            return View(new CreatePostViewModel
            {
                AllBranches = await _branchesDao.GetAllAsync()
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(CreatePostViewModel vm)
        {
            vm.AllBranches = await _branchesDao.GetAllAsync();
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                var user = await _currentUserService.GetUserAsync();
                var sourceBranch = await _currentUserService.GetBranchAsync();
                if (sourceBranch == null)
                {
                    throw new Exception("Setup your branch");
                }
                var post = new Post
                {
                    PersonFrom = vm.PersonFrom,
                    PersonTo = vm.PersonTo,
                    AddressTo = vm.AddressTo,
                    BranchId = sourceBranch.Id,
                    SourceBranchId = sourceBranch.Id,
                    DestinationBranchId = vm.DestinationBranchId.Value
                };
                await _mailDao.CreateAsync(post, user);

                vm.PersonFrom = null;
                vm.PersonTo = null;
                vm.DestinationBranchId = null;
                vm.AddressTo = null;
                TempData.Set("message", MessageViewModel.MakeInfo($"Post #{post.Id} created"));
                return View(vm);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View(vm);
            }
        }

        public async Task<IActionResult> DeliverPost(ListOptions options)
        {
            options = options ?? DefaultDeliverPostListOptions;
            var filterId = options.Filters["id"];
            var filterPersonFrom = options.Filters["personFrom"];
            var filterPersonTo = options.Filters["personTo"];

            var vm = new DeliverPostViewModel
            {
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery(),
            };

            var destinationBranch = await _currentUserService.GetBranchAsync();
            if (destinationBranch == null)
            {
                TempData.Set("message", MessageViewModel.MakeError("Setup your branch"));
                vm.Mail = new PaginatedList<Post>(PageSize);
                return View(vm);
            }

            var mail = await _mailDao.GetAllAsync(
                filterId: filterId,
                filterPersonFrom: filterPersonFrom,
                filterPersonTo: filterPersonTo,
                filterDestinationBranchId: destinationBranch.Id,
                filterState: PostState.InBranchStock,
                sortKey: options.SortKey,
                sortOrder: options.SortOrder);
            vm.Mail = mail.ToPaginatedList(options.Page, PageSize);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeliverPost(long postId, string returnUrl)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();
                await _mailDao.DeliverAsync(postId, user);
                TempData.Set("message", MessageViewModel.MakeInfo($"Post #{postId} delivered"));
            }
            catch (Exception e)
            {
                TempData.Set("message", MessageViewModel.MakeError(e.Message));
            }
            return Redirect(returnUrl);
        }
    }
}
