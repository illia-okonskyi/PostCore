using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using PostCore.Core.Dao;
using PostCore.Core.Users;
using PostCore.ViewUtils.ViewComponents;
using Xunit;

namespace PostCore.ViewUtils.Tests.ViewComponents
{
    public class AccountTest
    {
        static Account MakeAccountViewComponent(
            bool hasAuthenticatedUser,
            User authenticatedUser = null)
        {
            var usersDaoMock = new Mock<IUsersDao>();
            usersDaoMock.Setup(m => m.GetByUserNameAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(authenticatedUser));

            var identityMock = new Mock<IIdentity>();
            identityMock.Setup(m => m.IsAuthenticated)
                .Returns(hasAuthenticatedUser);
            identityMock.Setup(m => m.Name)
                .Returns(authenticatedUser?.UserName);
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(m => m.Identity)
                .Returns(identityMock.Object);
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(m => m.User)
                .Returns(claimsPrincipalMock.Object);

            var viewContext = new ViewContext
            {
                HttpContext = httpContextMock.Object
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };
            var viewComponent = new Account(usersDaoMock.Object)
            {
                ViewComponentContext = viewComponentContext
            };
            return viewComponent;
        }

        [Fact]
        public async Task NoLoggedInUser()
        {
            var vc = MakeAccountViewComponent(false);

            var vm = (await vc.InvokeAsync()).ExtractViewModel<AccountViewModel>();

            Assert.False(vm.IsLoggedIn);
            Assert.Null(vm.User);
        }

        [Fact]
        public async Task LoggedInUser()
        {
            var user = new User
            {
                Id = 1,
                UserName = "userName"
            };

            var vc = MakeAccountViewComponent(true, user);

            var vm = (await vc.InvokeAsync()).ExtractViewModel<AccountViewModel>();

            Assert.True(vm.IsLoggedIn);
            Assert.Equal(user.Id, vm.User.Id);
            Assert.Equal(user.UserName, vm.User.UserName);
        }
    }
}
