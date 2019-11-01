using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core.Users;

namespace PostCore.MainApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<User> _userManager;

        public HomeController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View(Core.HelloWorld.IndexText as object);
        }

        [Authorize(Roles =
            Role.Names.Admin + "," +
            Role.Names.Operator)]
        public IActionResult SayHello()
        {
            var user = _userManager.FindByNameAsync(HttpContext.User.Identity.Name);
            return View($"Hello, {user.Result.FirstName} {user.Result.LastName}!" as object);
        }
    }
}
