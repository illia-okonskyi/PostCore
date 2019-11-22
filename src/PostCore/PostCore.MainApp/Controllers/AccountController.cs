using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core.Dao;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels.Message;
using PostCore.MainApp.ViewModels.Account;

namespace PostCore.MainApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly IUsersDao _usersDao;
        public AccountController(
            SignInManager<User> signInManager,
            IUsersDao usersDao)
        {
            _signInManager = signInManager;
            _usersDao = usersDao;
        }

        public IActionResult AccessDenied(string returnUrl)
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

            if (!await Login(vm.Email, vm.Password, vm.RememberMe))
            {
                ModelState.AddModelError(nameof(LoginViewModel.Email), "Invalid user or password");
                return View(vm);
            }

            return Redirect(vm.ReturnUrl ?? "/");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Manage(string returnUrl)
        {
            var user = await _usersDao.GetByUserNameAsync(User.Identity.Name);
            return View(new ManageViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Manage(ManageViewModel vm)
        {
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
            }
            catch (IdentityException e)
            {
                foreach (var error in e.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            TempData["message"] = MessageViewModel.MakeInfo("User info changed");
            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> ChangePassword(string returnUrl)
        {
            var userName = User.Identity.Name;
            var user = await _usersDao.GetByUserNameAsync(userName);
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

            TempData["message"] = MessageViewModel.MakeInfo("Password changed");
            return View(vm);
        }

        async Task<bool> Login(string email, string password, bool rememberMe)
        {
            var user = await _usersDao.GetByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            await _signInManager.SignOutAsync();
            var result = await _signInManager.PasswordSignInAsync(
                user,
                password,
                rememberMe,
                false);

            return result.Succeeded;
        }
    }
}
