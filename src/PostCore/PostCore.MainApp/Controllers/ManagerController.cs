using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core;
using PostCore.Core.Activities;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels.Manager;
using PostCore.Utils;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles = Role.Authorize.Manager)]
    public class ManagerController : Controller
    {
        public static readonly long PageSize = 25;
        private static readonly ListOptions DefaultListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"type", ""},
                {"message", ""},
                {"from", ""},
                {"to", ""},
                {"user", ""},
                {"postId", ""},
                {"branchId", ""},
                {"carId", ""}
            }
        };

        private readonly IActivitiesDao _activitiesDao;
        private readonly IBranchesDao _branchesDao;
        private readonly ICarsDao _carsDao;
        public ManagerController(
            IActivitiesDao activitiesDao,
            IBranchesDao branchesDao,
            ICarsDao carsDao)
        {
            _activitiesDao = activitiesDao;
            _branchesDao = branchesDao;
            _carsDao = carsDao;
        }

        public async Task<IActionResult> Index(ListOptions options)
        {
            if (options == null)
            {
                options = DefaultListOptions;
                options.Filters["from"] = DateTime.Now.AddHours(-1).ToString("g");
                options.Filters["to"] = DateTime.Now.ToString("g");
            }

            NullableExtensions.TryParse(options.Filters["type"], out ActivityType? filterType);
            var filterMessage = options.Filters["message"];
            NullableExtensions.TryParse(options.Filters["from"], out DateTime? filterFrom);
            NullableExtensions.TryParse(options.Filters["to"], out DateTime? filterTo);
            var filterUser = options.Filters["user"];
            NullableExtensions.TryParse(options.Filters["postId"], out long? filterPostId);
            NullableExtensions.TryParse(options.Filters["branchId"], out long? filterBranchId);
            NullableExtensions.TryParse(options.Filters["carId"], out long? filterCarId);

            var activities = await _activitiesDao.GetAllAsync(
                filterType,
                filterMessage,
                filterFrom,
                filterTo,
                filterUser,
                filterPostId,
                filterBranchId,
                filterCarId);

            return View(new IndexViewModel
            {
                AllActivityTypes = Activity.AllTypes,
                AllBranches = await _branchesDao.GetAllAsync(),
                AllCars = await _carsDao.GetAllAsync(),
                Activities = activities.ToPaginatedList(options.Page, PageSize),
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery()
            });
        }

        public IActionResult RemoveActivities(string returnUrl)
        {
            return View(new RemoveActivitiesViewModel
            {
                ToDate = DateTime.Now,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveActivities(RemoveActivitiesViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            await _activitiesDao.RemoveToDateAsync(vm.ToDate);
            return Redirect(vm.ReturnUrl);
        }
    }
}
