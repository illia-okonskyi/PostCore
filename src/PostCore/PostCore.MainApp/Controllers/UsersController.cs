using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PostCore.Core.Db.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels.Users;
using PostCore.ViewUtils;
using PostCore.Core.Exceptions;
using PostCore.Utils;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles=Role.Authorize.Admin)]
    public class UsersController : Controller
    {
        private static readonly ListOptions DefaultListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"userName", ""},
                {"email", ""},
                {"firstName", ""},
                {"lastName", ""}
            },
            SortKey = "Id",
            SortOrder = SortOrder.Ascending
        };
        private static readonly long PageSize = 10;

        private readonly IConfiguration _configration;
        private readonly IUsersDao _usersDao;

        public UsersController(
            IConfiguration configuration,
            IUsersDao usersDao)
        {
            _configration = configuration;
            _usersDao = usersDao;
        }

        public async Task<IActionResult> Index(ListOptions options)
        {
            options = options ?? DefaultListOptions;

            var filterUserName = options.Filters["userName"];
            var filterEmail = options.Filters["email"];
            var filterFirstName = options.Filters["firstName"];
            var filterLastName = options.Filters["lastName"];

            var users = await _usersDao.GetAllAsync(
                filterUserName,
                filterEmail,
                filterFirstName,
                filterLastName,
                options.SortKey,
                options.SortOrder);

            return View(new IndexViewModel
            {
                Users = users.ToPaginatedList(options.Page, PageSize),
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery()
            });
        }

        public IActionResult Create(string returnUrl)
        {
            return View(
                nameof(Edit),
                new EditViewModel
                {
                    EditorMode = EditorMode.Create,
                    ReturnUrl = returnUrl
                });
        }

        public async Task<IActionResult> Edit(long id, string returnUrl)
        {
            var user = await _usersDao.GetByIdAsync(id);
            return View(
                new EditViewModel
                {
                    Id = user.Id,
                    EditorMode = EditorMode.Update,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ReturnUrl = returnUrl
                });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var user = new User
            {
                Id = vm.Id,
                UserName = vm.UserName,
                Email = vm.Email,
                FirstName = vm.FirstName,
                LastName = vm.LastName
            };

            try
            {
                if (vm.EditorMode == EditorMode.Create)
                {
                    await _usersDao.CreateAsync(
                        user,
                        _configration["Config:Users:DefaultPassword"],
                        Role.Names.Operator);
                }
                else
                {
                    await _usersDao.UpdateAsync(user);
                }
            }
            catch (IdentityException e)
            {
                foreach (var error in e.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            if (ModelState.ErrorCount != 0)
            {
                return View(vm);
            }
            else
            {
                return Redirect(vm.ReturnUrl);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id, string returnUrl)
        {
            try
            {
                await _usersDao.DeleteAsync(id);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Redirect(returnUrl);
        }
    }
}
