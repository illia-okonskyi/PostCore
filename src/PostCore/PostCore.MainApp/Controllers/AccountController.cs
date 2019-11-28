using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels.Message;
using PostCore.MainApp.ViewModels.Account;
using PostCore.ViewUtils;
using System;

namespace PostCore.MainApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUsersDao _usersDao;
        private readonly IBranchesDao _branchesDao;
        private readonly ICarsDao _carsDao;
        private readonly ICurrentUserService _currentUserService;
        public AccountController(
            IUsersDao usersDao,
            IBranchesDao branchesDao,
            ICarsDao carsDao,
            ICurrentUserService currentUserService)
        {
            _usersDao = usersDao;
            _branchesDao = branchesDao;
            _carsDao = carsDao;
            _currentUserService = currentUserService;
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel() { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            if (!await _usersDao.LoginAsync(vm.Email, vm.Password, vm.RememberMe))
            {
                ModelState.AddModelError(nameof(LoginViewModel.Email), "Invalid user or password");
                return View(vm);
            }

            return Redirect(vm.ReturnUrl ?? "/");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _usersDao.LogoutAsync();
            _currentUserService.Reset();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Manage(string returnUrl)
        {
            var user = await _currentUserService.GetUserAsync();
            var role = await _currentUserService.GetRoleAsync();

            var vm = new ManageViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                HasBranch = role.HasBranch,
                HasCar = role.HasCar,
                ReturnUrl = returnUrl
            };

            if (vm.HasBranch)
            {
                vm.AllBranches = await _branchesDao.GetAllAsync();
            }
            if (vm.HasCar)
            {
                vm.AllCars = await _carsDao.GetAllAsync();
            }

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Manage(ManageViewModel vm)
        {
            if (vm.HasBranch)
            {
                vm.AllBranches = await _branchesDao.GetAllAsync();
            }
            if (vm.HasCar)
            {
                vm.AllCars = await _carsDao.GetAllAsync();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var user = new User
            {
                Id = vm.UserId,
                UserName = vm.UserName,
                Email = vm.Email,
                FirstName = vm.FirstName,
                LastName = vm.LastName
            };

            if (!await _usersDao.CheckPasswordAsync(user.Id, vm.Password))
            {
                ModelState.AddModelError(nameof(vm.Password), "Wrong password");
                return View(vm);
            }

            try
            {
                await _usersDao.UpdateAsync(user);
                if (vm.HasBranch)
                {
                    if (!await _currentUserService.SetBranchAsync(vm.BranchId))
                    {
                        throw new Exception("Failed to set current branch");
                    }
                }

                if (vm.HasCar)
                {
                    if (!await _currentUserService.SetCarAsync(vm.CarId))
                    {
                        throw new Exception("Failed to set current car");
                    }
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

            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> ChangePassword(string returnUrl)
        {
            var user = await _currentUserService.GetUserAsync();
            return View(new ChangePasswordViewModel
            {
                UserId = user.Id,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                await _usersDao.ChangePasswordAsync(
                    vm.UserId,
                    vm.CurrentPassword,
                    vm.NewPassword);
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

            TempData.Set("message", MessageViewModel.MakeInfo("Password changed"));
            return View(vm);
        }
    }
}
