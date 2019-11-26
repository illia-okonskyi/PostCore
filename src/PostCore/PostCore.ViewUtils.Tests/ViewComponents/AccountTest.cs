using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Services;
using PostCore.Core.Users;
using PostCore.ViewUtils.ViewComponents;
using Xunit;

namespace PostCore.ViewUtils.Tests.ViewComponents
{
    public class AccountTest
    {
        class Context
        {
            public Mock<ICurrentUserService> CurrentUserServiceMock { get; set; }
            public ViewComponentContext ViewComponentContext { get; set; }
            public User LoggedInUser { get; set; }
            public Branch CurrentBranch { get; } = new Branch { Id = 1, Name = "name", Address = "address" };
            public Car CurrentCar { get; } = new Car { Id = 1, Model = "model", Number = "number" };
        }

        static User MakeLoggedInUser()
        {
            var role = new Role
            {
                Id = 1,
                Name = Role.Names.Admin
            };
            var user = new User
            {
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName",
            };
            user.UserRoles = new List<UserRole>
            {
                new UserRole { UserId = user.Id, User = user, RoleId = role.Id, Role = role }
            };
            return user;
        }

        Context MakeContext(
            bool hasLoggedInUser = false,
            string path = "/",
            string query = "?query=query")
        {
            var context = new Context();
            if (hasLoggedInUser)
            {
                context.LoggedInUser = MakeLoggedInUser();
            }

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(m => m.GetUserAsync())
                .ReturnsAsync(context.LoggedInUser);
            currentUserServiceMock.Setup(m => m.GetRoleAsync())
                .ReturnsAsync(context.LoggedInUser?.Role);
            currentUserServiceMock.Setup(m => m.GetBranchAsync())
                .ReturnsAsync(context.CurrentBranch);
            currentUserServiceMock.Setup(m => m.GetCarAsync())
                .ReturnsAsync(context.CurrentCar);
            context.CurrentUserServiceMock = currentUserServiceMock;

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(m => m.User.Identity.IsAuthenticated)
                .Returns(hasLoggedInUser);
            httpContextMock.Setup(m => m.Request.Path)
                .Returns(path);
            httpContextMock.Setup(m => m.Request.QueryString)
                .Returns(new QueryString(query));
            var viewContext = new ViewContext
            {
                HttpContext = httpContextMock.Object
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };
            context.ViewComponentContext = viewComponentContext;

            return context;
        }

        [Fact]
        public async Task NoLoggedInUser()
        {
            var path = "/path";
            var query = "?myquery=myquery";
            var currentUrl = path + query;

            var context = MakeContext(
                hasLoggedInUser: false,
                path: path,
                query: query);
            var vc = new Account(context.CurrentUserServiceMock.Object)
            {
                ViewComponentContext = context.ViewComponentContext
            };

            var vm = (await vc.InvokeAsync()).ExtractViewModel<AccountViewModel>();

            Assert.False(vm.IsLoggedIn);
            Assert.Null(vm.User);
            Assert.False(vm.HasBranch);
            Assert.Null(vm.Branch);
            Assert.False(vm.HasCar);
            Assert.Null(vm.Car);
            Assert.Equal(currentUrl, vm.CurrentUrl);
            context.CurrentUserServiceMock.Verify(m => m.GetBranchAsync(), Times.Never);
            context.CurrentUserServiceMock.Verify(m => m.GetCarAsync(), Times.Never);
        }

        [Fact]
        public async Task LoggedInUser()
        {
            var path = "/path";
            var query = "?myquery=myquery";
            var currentUrl = path + query;

            var context = MakeContext(
                hasLoggedInUser: true,
                path: path,
                query: query);
            var vc = new Account(context.CurrentUserServiceMock.Object)
            {
                ViewComponentContext = context.ViewComponentContext
            };

            var vm = (await vc.InvokeAsync()).ExtractViewModel<AccountViewModel>();

            Assert.True(vm.IsLoggedIn);
            Assert.Same(context.LoggedInUser, vm.User);
            Assert.Equal(context.LoggedInUser.Role.HasBranch, vm.HasBranch);
            if (context.LoggedInUser.Role.HasBranch)
            {
                Assert.Same(context.CurrentBranch, vm.Branch);
            }
            else
            {
                context.CurrentUserServiceMock.Verify(m => m.GetBranchAsync(), Times.Never);
            }
            Assert.Equal(context.LoggedInUser.Role.HasCar, vm.HasCar);
            if (context.LoggedInUser.Role.HasCar)
            {
                Assert.Same(context.CurrentCar, vm.Car);
            }
            else
            {
                context.CurrentUserServiceMock.Verify(m => m.GetCarAsync(), Times.Never);
            }
            Assert.Equal(currentUrl, vm.CurrentUrl);
        }
    }
}
