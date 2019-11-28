using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PostCore.Core.Branches;
using PostCore.Core.Mail;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.Controllers;
using PostCore.MainApp.ViewModels.Message;
using PostCore.MainApp.ViewModels.Operator;
using PostCore.Utils;
using PostCore.ViewUtils;
using Xunit;

namespace PostCore.MainApp.Tests.Controllers
{
    public class OperatorControllerTest
    {
        class Context
        {
            public ICurrentUserService CurrentUserService { get; set; }
            public IBranchesDao BranchesDao { get; set; }
            public Mock<IMailDao> MailDaoMock { get; set; }
            public ITempDataDictionary TempDataDictionary { get; set; }
            public ControllerContext ControllerContext { get; set; }

            public User LoggedInUser { get; set; } = MakeUser();
            public List<Branch> Branches { get; set; } = new List<Branch>
            {
                new Branch {Id = 1, Name = "name1", Address = "address1" },
                new Branch {Id = 2, Name = "name2", Address = "address2" }
            };
            public List<Post> Mail { get; set; } = new List<Post>();

            public Dictionary<string, object> TempData { get; set; } = new Dictionary<string, object>();
            public delegate void TryGetTempDataValueCallback(string key, out object value);

            static User MakeUser()
            {
                var user = new User
                {
                    Id = 1,
                    UserName = "operator",
                    Email = "operator@example.com",
                    FirstName = "firstNameOp",
                    LastName = "lastNameOp",
                };
                var role = new Role
                {
                    Id = 1,
                    Name = Role.Names.Operator
                };
                user.UserRoles = new List<UserRole>
                {
                    new UserRole { UserId = user.Id, User = user, RoleId = role.Id, Role = role}
                };
                return user;
            }
        }

        Context MakeContext(
            string path = "/",
            string query = "?query=query",
            int myBranchIndex = 0)
        {
            var context = new Context();

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(m => m.GetUserAsync())
                .ReturnsAsync(context.LoggedInUser);
            currentUserServiceMock.Setup(m => m.GetBranchAsync())
                .ReturnsAsync(context.Branches[myBranchIndex]);
            context.CurrentUserService = currentUserServiceMock.Object;

            var branchesDaoMock = new Mock<IBranchesDao>();
            branchesDaoMock.Setup(m => m.GetAllAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SortOrder>()))
                .ReturnsAsync(context.Branches);
            context.BranchesDao = branchesDaoMock.Object;

            var mailDaoMock = new Mock<IMailDao>();
            mailDaoMock.Setup(m => m.GetAllAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<long?>(),
                It.IsAny<long?>(),
                It.IsAny<PostState?>(),
                It.IsAny<string>(),
                It.IsAny<SortOrder>()
                )).
                ReturnsAsync((
                    string filterId,
                    string filterPersonFrom,
                    string filterPersonTo,
                    string filterAddressTo,
                    long? filterBranchId,
                    string filterBranchStockAddress,
                    long? filterCarId,
                    long? filterSourceBranchId,
                    long? filterDestinationBranchId,
                    PostState? filterState,
                    string sortKey,
                    SortOrder sortOrder) =>
                {
                    return context.Mail
                        .Where(p => p.Id.ToString().Contains(filterId))
                        .Where(p => p.PersonFrom.Contains(filterPersonFrom))
                        .Where(p => p.PersonTo.Contains(filterPersonTo))
                        .Where(p => p.DestinationBranchId == filterDestinationBranchId.Value)
                        .Where(p => p.State == filterState.Value)
                        .Order(sortKey, sortOrder);
                });
            mailDaoMock.Setup(m => m.CreateAsync(It.IsAny<Post>(), It.IsAny<User>()))
                .Callback((Post post, User user) =>
                {
                    context.Mail.Add(post);
                });
            mailDaoMock.Setup(m => m.DeliverAsync(It.IsAny<long>(), It.IsAny<User>()))
                .Callback((long postId, User user) =>
                {
                    var post = context.Mail.First(p => p.Id == postId);
                    post.BranchId = null;
                    post.BranchStockAddress = null;
                    post.CarId = null;
                    post.State = PostState.Delivered;
                });
            context.MailDaoMock = mailDaoMock;

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
            var tryGetValueCallback = new Context.TryGetTempDataValueCallback((string key, out object value) =>
            {
                context.TempData.TryGetValue(key, out value);
            });
            object dummy;
            tempDataDictionaryMock.Setup(m => m.TryGetValue(It.IsAny<string>(), out dummy))
                .Callback(tryGetValueCallback)
                .Returns(true);
            tempDataDictionaryMock.SetupSet(m => m[It.IsAny<string>()] = It.IsAny<object>())
                .Callback((string key, object o) => context.TempData[key] = o);
            context.TempDataDictionary = tempDataDictionaryMock.Object;

            return context;
        }

        [Fact]
        public async Task CreatePost_Get()
        {
            var context = MakeContext();

            var controller = new OperatorController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                ControllerContext = context.ControllerContext,
                TempData = context.TempDataDictionary
            };

            var r = await controller.CreatePost() as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as CreatePostViewModel;
            Assert.NotNull(vm);

            Assert.Same(context.Branches, vm.AllBranches);
        }

        [Fact]
        public async Task CreatePost_Post_ModelError()
        {
            var context = MakeContext();

            var controller = new OperatorController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                ControllerContext = context.ControllerContext,
                TempData = context.TempDataDictionary
            };

            var destinationBranch = context.Branches[0];
            var vm = new CreatePostViewModel
            {
                PersonFrom = "personFrom",
                PersonTo = "personTo",
                DestinationBranchId = destinationBranch.Id,
                AddressTo = "addressTo"
            };

            controller.ModelState.AddModelError("", "error");
            var r = await controller.CreatePost(vm) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var returnedVm = r.Model as CreatePostViewModel;
            Assert.NotNull(returnedVm);
            Assert.Equal(context.Branches, returnedVm.AllBranches);
            context.MailDaoMock.Verify(
                m => m.CreateAsync(It.IsAny<Post>(), It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task CreatePost_Post_Success()
        {
            var myBranchIndex = 0;
            var context = MakeContext(myBranchIndex: myBranchIndex);

            var controller = new OperatorController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                ControllerContext = context.ControllerContext,
                TempData = context.TempDataDictionary
            };

            var destinationBranch = context.Branches[0];
            var personFrom = "personFrom";
            var personTo = "personTo";
            var addressTo = "addressFrom";
            var destinationBranchId = destinationBranch.Id;
            var vm = new CreatePostViewModel
            {
                PersonFrom = personFrom,
                PersonTo = personTo,
                DestinationBranchId = destinationBranchId,
                AddressTo = addressTo
            };

            var r = await controller.CreatePost(vm) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var returnedVm = r.Model as CreatePostViewModel;
            Assert.NotNull(returnedVm);
            Assert.Equal(context.Branches, returnedVm.AllBranches);
            Assert.Single(context.Mail);
            var post = context.Mail.First();
            Assert.Equal(personFrom, post.PersonFrom);
            Assert.Equal(personTo, post.PersonTo);
            Assert.Equal(context.Branches[myBranchIndex].Id, post.BranchId);
            Assert.Equal(context.Branches[myBranchIndex].Id, post.SourceBranchId);
            Assert.Equal(destinationBranchId, post.DestinationBranchId);
            Assert.Equal(addressTo, post.AddressTo);
            Assert.Equal(PostState.Created, post.State);
            Assert.Null(returnedVm.PersonFrom);
            Assert.Null(returnedVm.PersonTo);
            Assert.Null(returnedVm.DestinationBranchId);
            Assert.Null(returnedVm.AddressTo);
            Assert.NotNull(context.TempDataDictionary.Get<MessageViewModel>("message"));
        }

        [Fact]
        public async Task DeliverPost_Get()
        {
            var options = new ListOptions
            {
                Filters = new Dictionary<string, string>
                {
                    { "id", "1"},
                    { "personFrom", "2"},
                    { "personTo", "3"}
                },
                SortKey = "PersonFrom",
                SortOrder = SortOrder.Descending
            };
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;
            var myBranchIndex = 0;

            var context = MakeContext(returnUrlPath, returnUrlQuery, myBranchIndex);
            var random = new Random();
            for (int i = 0; i < 100; ++i)
            {
                context.Mail.Add(new Post
                {
                    Id = random.Next(),
                    PersonFrom = "personFrom" + random.Next(),
                    PersonTo = "personTo" + random.Next(),
                    DestinationBranchId = context.Branches[myBranchIndex].Id,
                    State = PostState.InBranchStock
                });
            }

            var controller = new OperatorController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.DeliverPost(options) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as DeliverPostViewModel;
            Assert.NotNull(vm);
            var expectedMail = context.Mail
                .Where(p => p.Id.ToString().Contains(options.Filters["id"]))
                .Where(p => p.PersonFrom.Contains(options.Filters["personFrom"]))
                .Where(p => p.PersonTo.Contains(options.Filters["personTo"]))
                .Where(p => p.DestinationBranchId == context.Branches[myBranchIndex].Id)
                .Where(p => p.State == PostState.InBranchStock)
                .Order(options.SortKey, options.SortOrder)
                .ToPaginatedList(options.Page, OperatorController.PageSize);
            Assert.Equal(expectedMail, vm.Mail);
            Assert.Same(options, vm.CurrentListOptions);
            Assert.Equal(returnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task DeliverPost_Post()
        {
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;
            var myBranchIndex = 0;

            var context = MakeContext(returnUrlPath, returnUrlQuery, myBranchIndex);

            var postId = 1;
            context.Mail.Add(new Post
            {
                Id = postId,
                BranchId = context.Branches[myBranchIndex].Id,
                DestinationBranchId = context.Branches[myBranchIndex].Id,
            });

            var controller = new OperatorController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.DeliverPost(postId, returnUrl) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(returnUrl, r.Url);

            var post = context.Mail.First();
            Assert.Null(post.BranchId);
            Assert.Null(post.BranchStockAddress);
            Assert.Null(post.CarId);
            Assert.Equal(PostState.Delivered, post.State);
        }
    }
}
