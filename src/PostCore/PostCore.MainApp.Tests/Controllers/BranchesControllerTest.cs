using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using Xunit;
using Moq;
using PostCore.Utils;
using PostCore.MainApp.Controllers;
using PostCore.MainApp.ViewModels.Branches;
using PostCore.ViewUtils;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using PostCore.Core.Branches;

namespace PostCore.MainApp.Tests.Controllers
{
    public class BranchesControllerTest
    {
        class Context
        {
            public IBranchesDao BranchesDao { get; set; }
            public ITempDataDictionary TempDataDictionary { get; set; }
            public ControllerContext ControllerContext { get; set; }
            public List<Branch> Branches { get; set; } = new List<Branch>();
            public Dictionary<string, object> TempData { get; set; } = new Dictionary<string, object>();
        }

        Context MakeContext(string path = "/", string query = "?query=query")
        {
            var context = new Context();

            var branchesDaoMock = new Mock<IBranchesDao>();
            branchesDaoMock.Setup(m => m.GetAllAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SortOrder>()))
                .ReturnsAsync((
                    string filterName,
                    string filterAddress,
                    string sortKey,
                    SortOrder sortOrder) =>
                {
                    return context.Branches
                        .Where(b => b.Name.Contains(filterName))
                        .Where(b => b.Address.Contains(filterAddress))
                        .Order(sortKey, sortOrder);
                });
            branchesDaoMock.Setup(m => m.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => context.Branches.First(b => b.Id == id));
            branchesDaoMock.Setup(m => m.CreateAsync(It.IsAny<Branch>()))
                .Callback((Branch branch) =>
                {
                    context.Branches.Add(branch);
                });
            branchesDaoMock.Setup(m => m.UpdateAsync(It.IsAny<Branch>(), It.IsAny<Branch>()))
                .Callback((Branch branch, Branch oldBranch) =>
                {
                    var contextBranch = context.Branches.First(b => b.Id == branch.Id);
                    contextBranch.Name = branch.Name;
                    contextBranch.Address = branch.Address;
                });
            branchesDaoMock.Setup(m => m.DeleteAsync(It.IsAny<long>()))
                .Callback((long id) =>
                {
                    var branch = context.Branches.First(b => b.Id == id);
                    context.Branches.Remove(branch);
                });
            context.BranchesDao = branchesDaoMock.Object;

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(m => m.Request.Path)
                .Returns(path);
            httpContextMock.Setup(m => m.Request.QueryString)
                .Returns(new QueryString(query));
            var controllerContext = new ControllerContext
            {
                HttpContext = httpContextMock.Object
            };
            context.ControllerContext = controllerContext;

            var tempDataDictionaryMock = new Mock<ITempDataDictionary>();
            tempDataDictionaryMock.Setup(m => m[It.IsAny<string>()])
                .Returns((string key) => context.TempData[key]);
            tempDataDictionaryMock.SetupSet(m => m[It.IsAny<string>()] = It.IsAny<object>())
                .Callback((string key, object o) => context.TempData[key] = o);
            context.TempDataDictionary = tempDataDictionaryMock.Object;

            return context;
        }

        [Fact]
        public async Task Index_GetAsync()
        {
            var options = new ListOptions
            {
                Filters = new Dictionary<string, string>
                {
                    { "name", "1"},
                    { "address", "2"}
                },
                SortKey = "address",
                SortOrder = SortOrder.Descending
            };
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;

            var context = MakeContext(returnUrlPath, returnUrlQuery);
            var random = new Random();
            for (int i = 0; i < 100; ++i)
            {
                await context.BranchesDao.CreateAsync(new Branch
                {
                    Id = i,
                    Name = "name" + random.Next(),
                    Address = "address" + random.Next()
                });
            }

            var controller = new BranchesController(context.BranchesDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Index(options) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as IndexViewModel;
            Assert.NotNull(vm);
            var expectedBranches = context.Branches
                .Where(b => b.Name.Contains(options.Filters["name"]))
                .Where(b => b.Address.Contains(options.Filters["address"]))
                .Order(options.SortKey, options.SortOrder)
                .ToPaginatedList(options.Page, BranchesController.PageSize);
            Assert.Equal(expectedBranches, vm.Branches);
            Assert.Same(options, vm.CurrentListOptions);
            Assert.Equal(returnUrl, vm.ReturnUrl);
        }

        [Fact]
        public void Create_Get()
        {
            var returnUrl = "/";
            var expectedVm = new EditViewModel
            {
                EditorMode = EditorMode.Create,
                ReturnUrl = returnUrl
            };

            var context = MakeContext();

            var controller = new BranchesController(context.BranchesDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = controller.Create(returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Equal(nameof(controller.Edit), r.ViewName);
            var vm = r.Model as EditViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.EditorMode, vm.EditorMode);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Edit_Get()
        {
            var branch = new Branch
            {
                Id = 1,
                Name = "name",
                Address = "address"
            };
            var returnUrl = "/";
            var expectedVm = new EditViewModel
            {
                EditorMode = EditorMode.Update,
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                ReturnUrl = returnUrl
            };

            var context = MakeContext();
            await context.BranchesDao.CreateAsync(branch);

            var controller = new BranchesController(context.BranchesDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Edit(branch.Id, returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as EditViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.EditorMode, vm.EditorMode);
            Assert.Equal(expectedVm.Id, vm.Id);
            Assert.Equal(expectedVm.Name, vm.Name);
            Assert.Equal(expectedVm.Address, vm.Address);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Edit_Post_CreateBranch()
        {
            var context = MakeContext();

            var controller = new BranchesController(context.BranchesDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };
            var vm = new EditViewModel
            {
                EditorMode = EditorMode.Create,
                Id = 1,
                Name = "name",
                Address = "address",
                ReturnUrl = "/"
            };

            var r = await controller.Edit(vm) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(vm.ReturnUrl, r.Url);
            Assert.Single(context.Branches);
            var branch = context.Branches.First();
            Assert.Equal(vm.Name, branch.Name);
            Assert.Equal(vm.Address, branch.Address);
        }

        [Fact]
        public async Task Edit_Post_UpdateBranch()
        {
            var branch = new Branch
            {
                Id = 1,
                Name = "name",
                Address = "address"
            };

            var context = MakeContext();
            await context.BranchesDao.CreateAsync(branch);

            var controller = new BranchesController(context.BranchesDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };
            var vm = new EditViewModel
            {
                EditorMode = EditorMode.Update,
                Id = 1,
                Name = "name1",
                Address = "address1",
                ReturnUrl = "/"
            };

            var r = await controller.Edit(vm) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(vm.ReturnUrl, r.Url);
            Assert.Single(context.Branches);
            var updatedBranch = context.Branches.First();
            Assert.Equal(vm.Name, updatedBranch.Name);
            Assert.Equal(vm.Address, updatedBranch.Address);
        }

        [Fact]
        public async Task Delete_Post()
        {
            var branch = new Branch
            {
                Id = 1,
                Name = "name",
                Address = "address"
            };
            var returnUrl = "/";

            var context = MakeContext();
            await context.BranchesDao.CreateAsync(branch);

            var controller = new BranchesController(context.BranchesDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Delete(branch.Id, returnUrl) as RedirectResult;
            Assert.NotNull(r);
            Assert.Same(returnUrl, r.Url);
            Assert.Empty(context.Branches);
        }
    }
}
