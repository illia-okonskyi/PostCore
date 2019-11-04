using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core.Users;

namespace PostCore.ViewUtils.ViewComponents
{
    public class AccountViewModel
    {
        public bool IsLoggedIn { get; set; }
        public User User { get; set; }
    }

    public class Account : ViewComponent
    {
        private readonly UserManager<User> _userManager;
        public Account(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var vm = new AccountViewModel();
            var userNameMvc = User.Identity.Name;
            var isLoggedIn = userNameMvc != null;
            vm.IsLoggedIn = isLoggedIn;
            if (isLoggedIn)
            {
                vm.User = await _userManager.FindByNameAsync(userNameMvc);
                if (vm.User == null)
                {
                    vm.IsLoggedIn = false;
                }
            }
            return View(vm);
        }
    }
}
