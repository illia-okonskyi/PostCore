using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core;
using PostCore.Core.Mail;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels.Courier;
using PostCore.MainApp.ViewModels.Message;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles = Role.Authorize.Courier)]
    public class CourierController : Controller
    {
        public static readonly long PageSize = 25;

        private static readonly ListOptions DefaultListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"personTo", ""},
                {"addressTo", ""}
            },
            SortKey = "Id",
            SortOrder = SortOrder.Ascending
        };

        private readonly ICurrentUserService _currentUserService;
        private readonly IMailDao _mailDao;

        public CourierController(
            ICurrentUserService currentUserService,
            IMailDao mailDao
            )
        {
            _currentUserService = currentUserService;
            _mailDao = mailDao;
        }

        public async Task<IActionResult> StockMail(ListOptions options)
        {
            options = options ?? DefaultListOptions;

            var filterPersonTo = options.Filters["personTo"];
            var filterAddressTo = options.Filters["addressTo"];

            var vm = new MailViewModel
            {
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
                filterPersonTo: filterPersonTo,
                filterAddressTo: filterAddressTo,
                filterBranchId: branch.Id,
                filterState: PostState.InBranchStock,
                sortKey: options.SortKey,
                sortOrder: options.SortOrder);
            vm.Mail = mail.ToPaginatedList(options.Page, PageSize);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> MovePostToCar(long postId, string returnUrl)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();
                var car = await _currentUserService.GetCarAsync();
                if (car == null)
                {
                    throw new Exception("Setup your car");
                }

                await _mailDao.MoveToCarAsync(postId, car, true, user);
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

            var filterPersonTo = options.Filters["personTo"];
            var filterAddressTo = options.Filters["addressTo"];

            var vm = new MailViewModel
            {
                Mail = new PaginatedList<Post>(PageSize),
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery()
            };

            var car = await _currentUserService.GetCarAsync();
            if (car == null)
            {
                TempData.Set("message", MessageViewModel.MakeError("Setup your car"));
                return View(vm);
            }

            var mail = await _mailDao.GetAllAsync(
                filterPersonTo: filterPersonTo,
                filterAddressTo: filterAddressTo,
                filterCarId: car.Id,
                filterState: PostState.InDeviveryToPerson,
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
