using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels.Users;
using PostCore.ViewUtils;
using PostCore.Core.Exceptions;
using PostCore.Utils;
using PostCore.MainApp.ViewModels.Message;
using PostCore.Core;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles=Role.Authorize.Admin)]
    public class UsersController : Controller
    {
        public static readonly long PageSize = 10;

        private static readonly ListOptions DefaultListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"userName", ""},
                {"email", ""},
                {"firstName", ""},
                {"lastName", ""},
                {"roleName", ""}
            },
            SortKey = "Id",
            SortOrder = SortOrder.Ascending
        };

        private readonly IConfiguration _configration;
        private readonly IRolesDao _rolesDao;
        private readonly IUsersDao _usersDao;

        public UsersController(
            IConfiguration configuration,
            IRolesDao rolesDao,
            IUsersDao usersDao)
        {
            _configration = configuration;
            _rolesDao = rolesDao;
            _usersDao = usersDao;
        }

        public async Task<IActionResult> Index(ListOptions options)
        {
            options = options ?? DefaultListOptions;

            var filterUserName = options.Filters["userName"];
            var filterEmail = options.Filters["email"];
            var filterFirstName = options.Filters["firstName"];
            var filterLastName = options.Filters["lastName"];
            var filterRoleName = options.Filters["roleName"];

            var users = await _usersDao.GetAllAsync(
                filterUserName,
                filterEmail,
                filterFirstName,
                filterLastName,
                filterRoleName,
                options.SortKey,
                options.SortOrder);

            return View(new IndexViewModel
            {
                Users = users.ToPaginatedList(options.Page, PageSize),
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery()
            });
        }

        public async Task<IActionResult> Create(string returnUrl)
        {
            return View(
                nameof(Edit),
                new EditViewModel
                {
                    AllRoles = await _rolesDao.GetAllAsync(false),
                    IsAdminUser = false,
                    EditorMode = EditorMode.Create,
                    ReturnUrl = returnUrl
                });
        }

        public async Task<IActionResult> Edit(long id, string returnUrl)
        {
            var user = await _usersDao.GetByIdWithRoleAsync(id);
            return View(
                new EditViewModel
                {
                    AllRoles = await _rolesDao.GetAllAsync(false),
                    IsAdminUser = user.Role.IsAdmin,
                    Id = user.Id,
                    EditorMode = EditorMode.Update,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleId = user.Role.Id,
                    ReturnUrl = returnUrl
                });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllRoles = await _rolesDao.GetAllAsync(false);
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
                    var role = await _rolesDao.GetByIdAsync(vm.RoleId);
                    await _usersDao.CreateAsync(
                        user,
                        _configration["Config:Users:DefaultPassword"],
                        role.Name);
                }
                else
                {
                    await _usersDao.UpdateAsync(user, vm.RoleId);
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
                vm.AllRoles = await _rolesDao.GetAllAsync(false);
                return View(vm);
            }
            else
            {
                return Redirect(vm.ReturnUrl);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(EditViewModel vm)
        {
            try
            {
                await _usersDao.ResetPasswordAsync(vm.Id, _configration["Config:Users:DefaultPassword"]);
                TempData.Set("message", MessageViewModel.MakeInfo("Password reset succeded"));

            }
            catch (Exception e)
            {
                TempData.Set(
                    "message",
                    MessageViewModel.MakeError("Failed to reset password: " + e.Message));
            }

            vm.AllRoles = await _rolesDao.GetAllAsync(false);
            return View(nameof(Edit), vm);
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
