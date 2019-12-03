using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Mail;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.Controllers;
using PostCore.MainApp.ViewModels.Driver;
using PostCore.MainApp.ViewModels.Message;
using PostCore.Utils;
using PostCore.ViewUtils;
using Xunit;

namespace PostCore.MainApp.Tests.Controllers
{
    public class DriverControllerTest
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
            public Car MyCar { get; set; } = new Car { Id = 1, Model = "model", Number = "number" };
            public List<Post> Mail { get; set; } = new List<Post>();

            public Dictionary<string, object> TempData { get; set; } = new Dictionary<string, object>();
            public delegate void TryGetTempDataValueCallback(string key, out object value);

            static User MakeUser()
            {
                var user = new User
                {
                    Id = 1,
                    UserName = "driver",
                    Email = "driver@example.com",
                    FirstName = "firstNameDriver",
                    LastName = "lastNameDriver",
                };
                var role = new Role
                {
                    Id = 1,
                    Name = Role.Names.Driver
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
            currentUserServiceMock.Setup(m => m.GetCarAsync())
                .ReturnsAsync(context.MyCar);
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
                    var mail = context.Mail.AsEnumerable();
                    if (filterBranchId.HasValue)
                    {
                        mail = mail.Where(p => p.BranchId == filterBranchId.Value);
                    }
                    if (filterCarId.HasValue)
                    {
                        mail = mail.Where(p => p.CarId == filterCarId.Value);
                    }
                    return mail
                        .Where(p => p.SourceBranchId == filterSourceBranchId.Value)
                        .Where(p => p.DestinationBranchId == filterDestinationBranchId.Value)
                        .Where(p => p.State == filterState.Value)
                        .Order(sortKey, sortOrder);
                });
            mailDaoMock.Setup(m => m.MoveToCarAsync(
                It.IsAny<long>(),
                It.IsAny<Car>(),
                It.IsAny<bool>(),
                It.IsAny<User>()))
                .Callback((long postId, Car car, bool courierDelivery, User user) =>
                {
                    var post = context.Mail.First(p => p.Id == postId);
                    post.State = PostState.InDeliveryToBranchStock;
                    post.BranchId = null;
                    post.BranchStockAddress = null;
                    post.CarId = car.Id;
                });
            mailDaoMock.Setup(m => m.MoveToBranchStockAsync(
                It.IsAny<long>(),
                It.IsAny<Branch>(),
                It.IsAny<User>()))
                .Callback((long postId, Branch branch, User user) =>
                {
                    var post = context.Mail.First(p => p.Id == postId);
                    post.State = PostState.InBranchStock;
                    post.BranchId = branch.Id;
                    post.BranchStockAddress = null;
                    post.CarId = null;
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
        public async Task StockMail_Get()
        {
            long? sourceBranchId = 0;
            long? destinationBranchId = 1;
            var options = new ListOptions
            {
                Filters = new Dictionary<string, string>
                {
                    { "sourceBranchId", sourceBranchId.Value.ToString()},
                    { "destinationBranchId", destinationBranchId.Value.ToString()}
                },
                SortKey = "SourceBranchId",
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
                    Id = i,
                    SourceBranchId = random.Next() % context.Branches.Count(),
                    DestinationBranchId = random.Next() % context.Branches.Count(),
                    BranchId = random.Next() % 2 == 0 ? context.Branches[myBranchIndex].Id : default(long),
                    State = random.Next() % 5 == 0 ? PostState.InBranchStock : PostState.Created
                });
            }

            var mailDao = context.MailDaoMock.Object;
            var controller = new DriverController(
                context.CurrentUserService,
                context.BranchesDao,
                mailDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.StockMail(options) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as MailViewModel;
            Assert.NotNull(vm);
            var expectedMail = (await mailDao.GetAllAsync(
                filterBranchId: context.Branches[myBranchIndex].Id,
                filterSourceBranchId: sourceBranchId,
                filterDestinationBranchId: destinationBranchId,
                filterState: PostState.InBranchStock,
                sortKey: options.SortKey,
                sortOrder: options.SortOrder))
                .ToPaginatedList(options.Page, DriverController.PageSize);
            Assert.Same(context.Branches, vm.AllBranches);
            Assert.Equal(expectedMail, vm.Mail);
            Assert.Same(options, vm.CurrentListOptions);
            Assert.Equal(returnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task MovePostToCar_Post()
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

            var controller = new DriverController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.MovePostToCar(postId, returnUrl) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(returnUrl, r.Url);

            var post = context.Mail.First();
            Assert.Null(post.BranchId);
            Assert.Null(post.BranchStockAddress);
            Assert.Equal(context.MyCar.Id, post.CarId);
            Assert.Equal(PostState.InDeliveryToBranchStock, post.State);

            var message = controller.TempData.Get<MessageViewModel>("message");
            Assert.NotNull(message);
            Assert.Equal(MessageType.Info, message.Type);
        }

        [Fact]
        public async Task CarMail_Get()
        {
            long? sourceBranchId = 0;
            long? destinationBranchId = 1;
            var options = new ListOptions
            {
                Filters = new Dictionary<string, string>
                {
                    { "sourceBranchId", sourceBranchId.Value.ToString()},
                    { "destinationBranchId", destinationBranchId.Value.ToString()}
                },
                SortKey = "SourceBranchId",
                SortOrder = SortOrder.Descending
            };
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;

            var context = MakeContext(returnUrlPath, returnUrlQuery);
            var random = new Random();
            for (int i = 0; i < 100; ++i)
            {
                context.Mail.Add(new Post
                {
                    Id = i,
                    SourceBranchId = random.Next() % context.Branches.Count(),
                    DestinationBranchId = random.Next() % context.Branches.Count(),
                    CarId = random.Next() % 2 == 0 ? context.MyCar.Id : default(long),
                    State = random.Next() % 5 == 0 ? PostState.InDeliveryToBranchStock : PostState.Created
                });
            }

            var mailDao = context.MailDaoMock.Object;
            var controller = new DriverController(
                context.CurrentUserService,
                context.BranchesDao,
                mailDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.CarMail(options) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as MailViewModel;
            Assert.NotNull(vm);
            var expectedMail = (await mailDao.GetAllAsync(
                filterCarId: context.MyCar.Id,
                filterSourceBranchId: sourceBranchId,
                filterDestinationBranchId: destinationBranchId,
                filterState: PostState.InDeliveryToBranchStock,
                sortKey: options.SortKey,
                sortOrder: options.SortOrder))
                .ToPaginatedList(options.Page, DriverController.PageSize);
            Assert.Same(context.Branches, vm.AllBranches);
            Assert.Equal(expectedMail, vm.Mail);
            Assert.Same(options, vm.CurrentListOptions);
            Assert.Equal(returnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task MovePostToBranchStock_Post()
        {
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;
            var myBranchIndex = 1;
            var context = MakeContext(returnUrlPath, returnUrlQuery, myBranchIndex);

            var postId = 1;
            context.Mail.Add(new Post
            {
                Id = postId
            });

            var controller = new DriverController(
                context.CurrentUserService,
                context.BranchesDao,
                context.MailDaoMock.Object)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.MovePostToBranchStock(postId, returnUrl) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(returnUrl, r.Url);

            var post = context.Mail.First();
            Assert.Null(post.CarId);
            Assert.Null(post.BranchStockAddress);
            Assert.Equal(context.Branches[myBranchIndex].Id, post.BranchId);
            Assert.Equal(PostState.InBranchStock, post.State);

            var message = controller.TempData.Get<MessageViewModel>("message");
            Assert.NotNull(message);
            Assert.Equal(MessageType.Info, message.Type);
        }
    }
}
