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
using PostCore.MainApp.ViewModels.Stockman;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles = Role.Authorize.Stockman)]
    public class StockmanController : Controller
    {
        public static readonly long PageSize = 25;

        private static readonly ListOptions DefaultStockMailListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"withoutAddressOnly", "false"},
                {"sourceBranchId", ""},
                {"destinationBranchId", ""},
                {"personFrom", ""},
                {"personTo", "" },
                {"addressTo", "" }
            },
            SortKey = "Id",
            SortOrder = SortOrder.Ascending
        };

        private readonly ICurrentUserService _currentUserService;
        private readonly IBranchesDao _branchesDao;
        private readonly IMailDao _mailDao;

        public StockmanController(
            ICurrentUserService currentUserService,
            IBranchesDao branchesDao,
            IMailDao mailDao)
        {
            _currentUserService = currentUserService;
            _branchesDao = branchesDao;
            _mailDao = mailDao;
        }

        public async Task<IActionResult> Index(ListOptions options)
        {
            options = options ?? DefaultStockMailListOptions;

            var vm = new IndexViewModel
            {
                AllBranches = await _branchesDao.GetAllAsync(),
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery()
            };

            if (!bool.TryParse(options.Filters["withoutAddressOnly"], out bool withoutAddresOnly))
            {
                withoutAddresOnly = false;
            }
            NullableExtensions.TryParse(options.Filters["sourceBranchId"], out long? filterSourceBranchId);
            NullableExtensions.TryParse(options.Filters["destinationBranchId"], out long? filterDestinationBranchId);
            var filterPersonFrom = options.Filters["personFrom"];
            var filterPersonTo = options.Filters["personTo"];
            var filterAddressTo = options.Filters["addressTo"];

            var branch = await _currentUserService.GetBranchAsync();
            if (branch == null)
            {
                TempData.Set("message", MessageViewModel.MakeError("Setup your branch"));
                vm.Mail = new PaginatedList<Post>(PageSize);
                return View(vm);
            }
            var mail = await _mailDao.GetAllForStock(
                branch: branch,
                withoutAddressOnly: withoutAddresOnly,
                filterSourceBranchId: filterSourceBranchId,
                filterDestinationBranchId: filterDestinationBranchId,
                filterPersonFrom: filterPersonFrom,
                filterPersonTo: filterPersonTo,
                filterAddressTo: filterAddressTo,
                sortKey: options.SortKey,
                sortOrder: options.SortOrder);
            vm.Mail = mail.ToPaginatedList(options.Page, PageSize);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> StockMail(long postId, string address, string returnUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(address))
                {
                    throw new Exception("Address required");
                }
                var user = await _currentUserService.GetUserAsync();
                await _mailDao.StockAsync(postId, address, user);
                TempData.Set("message", MessageViewModel.MakeInfo($"Post #{postId} stocked"));
            }
            catch (Exception e)
            {
                TempData.Set("message", MessageViewModel.MakeError(e.Message));
            }
            return Redirect(returnUrl);
        }
    }
}
