using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels;

namespace PostCore.MainApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

            if (!await Core.Users.User.Login(
                vm.Email,
                vm.Password,
                _userManager,
                _signInManager))
            {
                ModelState.AddModelError(nameof(LoginViewModel.Email), "Invalid user or password");
                return View(vm);
            }

            return Redirect(vm.ReturnUrl ?? "/");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await Core.Users.User.Logout(_signInManager);
            return RedirectToAction("Index", "Home");
        }
    }
}
