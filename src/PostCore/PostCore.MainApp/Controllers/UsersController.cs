using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PostCore.Core.Db.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels;
using PostCore.ViewUtils;
using PostCore.Core.Exceptions;
using System;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles=Role.Authorize.Admin)]
    public class UsersController : Controller
    {
        private readonly IConfiguration _configration;
        private readonly IUsersDao _usersDao;

        public UsersController(
            IConfiguration configuration,
            IUsersDao usersDao)
        {
            _configration = configuration;
            _usersDao = usersDao;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _usersDao.GetAllAsync());
        }

        public IActionResult Create()
        {
            return View(
                nameof(Edit),
                new EditViewModel
                {
                    EditorMode = EditorMode.Create,
                });
        }

        public async Task<IActionResult> Edit(long id)
        {
            var user = await _usersDao.GetByIdAsync(id);
            return View(
                new EditViewModel
                {
                    Id = user.Id,
                    EditorMode = EditorMode.Update,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName
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
                UserName = vm.FirstName.ToLower() + "." + vm.LastName.ToLower(),
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
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _usersDao.DeleteAsync(id);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
