using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using PostCore.Core.Branches;
using PostCore.Core.Services;
using PostCore.ViewUtils.ViewComponents;
using Xunit;

namespace PostCore.ViewUtils.Tests.ViewComponents
{
    public class MyBranchTest
    {
        static MyBranch MakeViewComponent(
            bool userLoggedIn = false,
            Branch myBranch = null,
            string path = "/",
            string query = "?query=query")
        {
            var myBranchServiceMock = new Mock<IMyBranchService>();
            myBranchServiceMock.Setup(m => m.GetMyBranchAsync())
                .ReturnsAsync(myBranch);

            var identityMock = new Mock<IIdentity>();
            identityMock.Setup(m => m.IsAuthenticated)
                .Returns(userLoggedIn);
            var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
            claimsPrincipalMock.Setup(m => m.Identity)
                .Returns(identityMock.Object);
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(m => m.User)
                .Returns(claimsPrincipalMock.Object);
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
            var viewComponent = new MyBranch(myBranchServiceMock.Object)
            {
                ViewComponentContext = viewComponentContext
            };
            return viewComponent;
        }

        [Fact]
        public async Task UserNotLoggedIn()
        {
            var vc = MakeViewComponent(
                userLoggedIn: false);

            var vm = (await vc.InvokeAsync()).ExtractViewModel<MyBranchViewModel>();

            Assert.False(vm.Visible);
        }

        [Fact]
        public async Task HasNoMyBranch()
        {
            var path = "/path";
            var query = "?myquery=myquery";
            var currentUrl = path + query;

            var vc = MakeViewComponent(
                userLoggedIn: true,
                path: path,
                query: query);

            var vm = (await vc.InvokeAsync()).ExtractViewModel<MyBranchViewModel>();

            Assert.True(vm.Visible);
            Assert.Equal("<No branch>", vm.MyBranchName);
            Assert.Equal(currentUrl, vm.CurrentUrl);
        }

        [Fact]
        public async Task HasMyBranch()
        {
            var myBranch = new Branch
            {
                Name = "My branch"
            };
            var path = "/path";
            var query = "?myquery=myquery";
            var currentUrl = path + query;

            var vc = MakeViewComponent(
                userLoggedIn: true,
                myBranch: myBranch,
                path: path,
                query: query);

            var vm = (await vc.InvokeAsync()).ExtractViewModel<MyBranchViewModel>();

            Assert.True(vm.Visible);
            Assert.Equal(myBranch.Name, vm.MyBranchName);
            Assert.Equal(currentUrl, vm.CurrentUrl);
        }
    }
}
