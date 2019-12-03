using System;
using System.Linq;
using Xunit;
using Moq;
using PostCore.Core.Branches;
using PostCore.Core.Mail;
using PostCore.Core.Services.Dao;
using System.Threading.Tasks;
using PostCore.MainApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using PostCore.MainApp.ViewModels.Home;

namespace PostCore.MainApp.Tests.Controllers
{
    public class HomeControllerTest
    {
        [Fact]
        public async Task MyPostStatus_Get()
        {
            var sourceBranch = new Branch { Id = 1, Name = "sourceBranch" };
            var destinationBranch = new Branch { Id = 1, Name = "destinationBranch" };
            var post = new Post
            {
                Id = 1,
                PersonFrom = "personFrom",
                PersonTo = "personTo",
                AddressTo = "addressTo",
                SourceBranch = sourceBranch,
                DestinationBranch = destinationBranch,
                Branch = sourceBranch,
                State = PostState.Created
            };

            var mailDaoMock = new Mock<IMailDao>();
            mailDaoMock.Setup(m => m.GetByIdAsync(post.Id)).
                ReturnsAsync(post);

            var controller = new HomeController(mailDaoMock.Object);

            var r = await controller.MyPostStatus(post.Id) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);

            var vm = r.Model as MyPostStatusViewModel;
            Assert.NotNull(vm);
            Assert.Equal(post.Id, vm.PostId);
            Assert.Equal(post.PersonFrom, vm.PersonFrom);
            Assert.Equal(post.PersonTo, vm.PersonTo);
            Assert.Equal(post.AddressTo, vm.AddressTo);
            Assert.Equal(post.SourceBranch.Name, vm.SourceBranchName);
            Assert.Equal(post.DestinationBranch.Name, vm.DestinationBranchName);
            Assert.Equal(post.Branch.Name, vm.CurrentBranchName);
            Assert.Equal("Created", vm.PostState);
        }
    }
}
