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
using PostCore.MainApp.ViewModels.Stockman;
using PostCore.Utils;
using PostCore.ViewUtils;
using Xunit;

namespace PostCore.MainApp.Tests.Controllers
{
    public class StockmanControllerTest
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
                    UserName = "stockman",
                    Email = "stockman@example.com",
                    FirstName = "firstNameStockman",
                    LastName = "lastNameStockman",
                };
                var role = new Role
                {
                    Id = 1,
                    Name = Role.Names.Stockman
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
            mailDaoMock.Setup(m => m.GetAllForStock(
                It.IsAny<Branch>(),
                It.IsAny<bool>(),
                It.IsAny<long?>(),
                It.IsAny<long?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SortOrder>()
                )).
                ReturnsAsync((
                    Branch branch,
                    bool withoutAddressOnly,
                    long? filterSourceBranchId,
                    long? filterDestinationBranchId,
                    string filterPersonFrom,
                    string filterPersonTo,
                    string filterAddressTo,
                    string sortKey,
                    SortOrder sortOrder) =>
                {
                    var mail = context.Mail.AsEnumerable();
                    if (withoutAddressOnly)
                    {
                        mail = mail.Where(p => p.BranchStockAddress == null);
                    }
                    return mail
                        .Where(p => p.SourceBranchId == filterSourceBranchId.Value)
                        .Where(p => p.DestinationBranchId == filterDestinationBranchId.Value)
                        .Where(p => p.PersonFrom.Contains(filterPersonFrom))
                        .Where(p => p.PersonTo.Contains(filterPersonTo))
                        .Where(p => p.AddressTo.Contains(filterAddressTo))
                        .Where(p => p.BranchId == branch.Id)
                        .Where(p => p.State == PostState.Created || p.State == PostState.InBranchStock)
                        .Order(sortKey, sortOrder);
                });
            mailDaoMock.Setup(m => m.StockAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<User>()))
                .Callback((long postId, string address, User user) =>
                {
                    var post = context.Mail.First(p => p.Id == postId);
                    post.State = PostState.InBranchStock;
                    post.BranchStockAddress = address;
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
        public async Task Index_Get()
        {
            bool withoutAddressOnly = true;
            long? sourceBranchId = 0;
            long? destinationBranchId = 1;
            var options = new ListOptions
            {
                Filters = new Dictionary<string, string>
                {
                    { "withoutAddressOnly", withoutAddressOnly.ToString()},
                    { "sourceBranchId", sourceBranchId.Value.ToString()},
                    { "destinationBranchId", destinationBranchId.Value.ToString()},
                    { "personFrom", "2"},
                    { "personTo", "3" },
                    { "addressTo", "4" }
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
                    AddressTo = "addressTo" + random.Next(),
                    SourceBranchId = random.Next() % context.Branches.Count(),
                    DestinationBranchId = random.Next() % context.Branches.Count(),
                    BranchId = context.Branches[myBranchIndex].Id,
                    State = PostState.InBranchStock
                });
            }

            var mailDao = context.MailDaoMock.Object;
            var controller = new StockmanController(
                context.CurrentUserService,
                context.BranchesDao,
                mailDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Index(options) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as IndexViewModel;
            Assert.NotNull(vm);
            var expectedMail = (await mailDao.GetAllForStock(
                branch: context.Branches[myBranchIndex],
                withoutAddressOnly: withoutAddressOnly,
                filterSourceBranchId: sourceBranchId,
                filterDestinationBranchId: destinationBranchId,
                filterPersonFrom: options.Filters["personFrom"],
                filterPersonTo: options.Filters["personTo"],
                filterAddressTo: options.Filters["addressTo"],
                sortKey: options.SortKey,
                sortOrder: options.SortOrder))
                .ToPaginatedList(options.Page, StockmanController.PageSize);
            Assert.Same(context.Branches, vm.AllBranches);
            Assert.Equal(expectedMail, vm.Mail);
            Assert.Same(options, vm.CurrentListOptions);
            Assert.Equal(returnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task StockMail_Post_WrongAddress()
        {
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;
            var context = MakeContext(returnUrlPath, returnUrlQuery);

            var postId = 1;
            context.Mail.Add(new Post
            {
                Id = postId
            });

            var controller = new StockmanController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.StockMail(postId, null, returnUrl) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(returnUrl, r.Url);

            context.MailDaoMock.Verify(m => m.StockAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<User>()),
                Times.Never);

            var message = controller.TempData.Get<MessageViewModel>("message");
            Assert.NotNull(message);
            Assert.Equal(MessageType.Error, message.Type);
        }

        [Fact]
        public async Task StockMail_Post_Success()
        {
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;
            var context = MakeContext(returnUrlPath, returnUrlQuery);

            var postId = 1;
            var address = "address";
            context.Mail.Add(new Post
            {
                Id = postId
            });

            var controller = new StockmanController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.StockMail(postId, address, returnUrl) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(returnUrl, r.Url);

            var post = context.Mail.First();
            Assert.Equal(address, post.BranchStockAddress);
            Assert.Equal(PostState.InBranchStock, post.State);

            var message = controller.TempData.Get<MessageViewModel>("message");
            Assert.NotNull(message);
            Assert.Equal(MessageType.Info, message.Type);
        }
    }
}
