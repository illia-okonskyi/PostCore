using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Services;
using PostCore.Core.Users;

namespace PostCore.ViewUtils.ViewComponents
{
    public class AccountViewModel
    {
        public bool IsLoggedIn { get; set; }
        public User User { get; set; }
        public bool HasBranch { get; set; } = false;
        public Branch Branch { get; set; }
        public bool HasCar { get; set; } = false;
        public Car Car { get; set; }
        public string CurrentUrl { get; set; }
    }

    public class Account : ViewComponent
    {
        private readonly ICurrentUserService _currentUserService;
        public Account(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var vm = new AccountViewModel
            {
                CurrentUrl = Request.PathAndQuery()
            };
            var isLoggedIn = User.Identity.IsAuthenticated;
            vm.IsLoggedIn = isLoggedIn;
            if (!isLoggedIn)
            {
                return View(vm);
            }
            vm.User = await _currentUserService.GetUserAsync();
            if (vm.User == null)
            {
                vm.IsLoggedIn = false;
                return View(vm);
            }

            var role = await _currentUserService.GetRoleAsync();
            vm.HasBranch = role.HasBranch;
            vm.HasCar = role.HasCar;
            if (vm.HasBranch)
            {
                vm.Branch = await _currentUserService.GetBranchAsync();
            }

            if (vm.HasCar)
            {
                vm.Car = await _currentUserService.GetCarAsync();
            }
            return View(vm);
        }
    }
}
