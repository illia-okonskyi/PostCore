using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core;
using PostCore.Core.Dao;
using PostCore.Core.Users;

namespace PostCore.ViewUtils.ViewComponents
{
    public class AccountViewModel
    {
        public bool IsLoggedIn { get; set; }
        public User User { get; set; }
        public string CurrentUrl { get; set; }
    }

    public class Account : ViewComponent
    {
        private readonly IUsersDao _usersDao;
        public Account(IUsersDao usersDao)
        {
            _usersDao = usersDao;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var vm = new AccountViewModel
            {
                CurrentUrl = Request.PathAndQuery()
            };
            var isLoggedIn = User.Identity.IsAuthenticated;
            vm.IsLoggedIn = isLoggedIn;
            if (isLoggedIn)
            {
                vm.User = await _usersDao.GetByUserNameAsync(User.Identity.Name);
                if (vm.User == null)
                {
                    vm.IsLoggedIn = false;
                }
            }
            return View(vm);
        }
    }
}
