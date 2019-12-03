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
using PostCore.MainApp.ViewModels.Driver;
using PostCore.MainApp.ViewModels.Message;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles = Role.Authorize.Driver)]
    public class DriverController : Controller
    {
        public static readonly long PageSize = 25;

        private static readonly ListOptions DefaultListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"sourceBranchId", ""},
                {"destinationBranchId", ""}
            },
            SortKey = "Id",
            SortOrder = SortOrder.Ascending
        };

        private readonly ICurrentUserService _currentUserService;
        private readonly IBranchesDao _branchesDao;
        private readonly IMailDao _mailDao;

        public DriverController(
            ICurrentUserService currentUserService,
            IBranchesDao branchesDao,
            IMailDao mailDao
            )
        {
            _currentUserService = currentUserService;
            _branchesDao = branchesDao;
            _mailDao = mailDao;
        }

        public async Task<IActionResult> StockMail(ListOptions options)
        {
            options = options ?? DefaultListOptions;

            NullableExtensions.TryParse(options.Filters["sourceBranchId"], out long? filterSourceBranchId);
            NullableExtensions.TryParse(options.Filters["destinationBranchId"], out long? filterDestinationBranchId);

            var vm = new MailViewModel
            {
                AllBranches = await _branchesDao.GetAllAsync(),
                Mail = new PaginatedList<Post>(PageSize),
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery()
            };

            var branch = await _currentUserService.GetBranchAsync();
            var car = await _currentUserService.GetCarAsync();
            if (branch == null || car == null)
            {
                TempData.Set("message", MessageViewModel.MakeError("Setup your branch and car"));
                return View(vm);
            }

            var mail = await _mailDao.GetAllAsync(
                filterBranchId: branch.Id,
                filterSourceBranchId: filterSourceBranchId,
                filterDestinationBranchId: filterDestinationBranchId,
                filterState: PostState.InBranchStock,
                sortKey: options.SortKey,
                sortOrder: options.SortOrder);
            vm.Mail = mail.ToPaginatedList(options.Page, PageSize);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> MoveToCar(long postId, string returnUrl)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();
                var car = await _currentUserService.GetCarAsync();
                if (car == null)
                {
                    throw new Exception("Setup your car");
                }

                await _mailDao.MoveToCarAsync(postId, car, false, user);
                TempData.Set("message", MessageViewModel.MakeInfo($"Post #{postId} moved to car"));
            }
            catch (Exception e)
            {
                TempData.Set("message", MessageViewModel.MakeError(e.Message));
            }
            return Redirect(returnUrl);
        }

        public async Task<IActionResult> CarMail(ListOptions options)
        {
            options = options ?? DefaultListOptions;

            NullableExtensions.TryParse(options.Filters["sourceBranchId"], out long? filterSourceBranchId);
            NullableExtensions.TryParse(options.Filters["destinationBranchId"], out long? filterDestinationBranchId);

            var vm = new MailViewModel
            {
                AllBranches = await _branchesDao.GetAllAsync(),
                Mail = new PaginatedList<Post>(PageSize),
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery()
            };

            var branch = await _currentUserService.GetBranchAsync();
            var car = await _currentUserService.GetCarAsync();
            if (branch == null || car == null)
            {
                TempData.Set("message", MessageViewModel.MakeError("Setup your branch and car"));
                return View(vm);
            }

            var mail = await _mailDao.GetAllAsync(
                filterCarId: car.Id,
                filterSourceBranchId: filterSourceBranchId,
                filterDestinationBranchId: filterDestinationBranchId,
                filterState: PostState.InDeliveryToBranchStock,
                sortKey: options.SortKey,
                sortOrder: options.SortOrder);
            vm.Mail = mail.ToPaginatedList(options.Page, PageSize);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> MoveToBranchStock(long postId, string returnUrl)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();
                var branch = await _currentUserService.GetBranchAsync();
                if (branch == null)
                {
                    throw new Exception("Setup your branch");
                }

                await _mailDao.MoveToBranchStockAsync(postId, branch, user);
                TempData.Set("message", MessageViewModel.MakeInfo($"Post #{postId} moved to branch stock"));
            }
            catch (Exception e)
            {
                TempData.Set("message", MessageViewModel.MakeError(e.Message));
            }
            return Redirect(returnUrl);
        }
    }
}
