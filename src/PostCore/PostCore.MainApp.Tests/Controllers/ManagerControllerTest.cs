using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PostCore.Core.Activities;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Mail;
using PostCore.Core.Services.Dao;
using PostCore.MainApp.Controllers;
using PostCore.MainApp.ViewModels.Manager;
using PostCore.Utils;
using Xunit;

namespace PostCore.MainApp.Tests.Controllers
{
    public class ManagerControllerTest
    {
        class Context
        {
            public IActivitiesDao ActivitiesDao { get; set; }
            public IBranchesDao BranchesDao { get; set; }
            public ICarsDao CarsDao { get; set; }
            public ControllerContext ControllerContext { get; set; }

            public List<Post> Mail = new List<Post>
            {
                new Post { Id = 1 }, new Post { Id = 2 }
            };
            public List<Activity> Activities { get; set; } = new List<Activity>();
            public List<Branch> Branches { get; set; } = new List<Branch>
            {
                new Branch {Id = 1, Name = "name1", Address = "address1" },
                new Branch {Id = 2, Name = "name2", Address = "address2" }
            };
            public List<Car> Cars { get; set; } = new List<Car>
            {
                new Car { Id = 1, Model = "model1", Number = "number1" },
                new Car { Id = 2, Model = "model2", Number = "number2" }
            };
        }

        Context MakeContext(string path = "/", string query = "?query=query")
        {
            var context = new Context();

            var activitiesDaoMock = new Mock<IActivitiesDao>();
            activitiesDaoMock.Setup(m => m.GetAllAsync(
                It.IsAny<ActivityType?>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<long?>(),
                It.IsAny<long?>()))
                .ReturnsAsync((
                    ActivityType? filterType,
                    string filterMessage,
                    DateTime? filterFrom,
                    DateTime? filterTo,
                    string filterUser,
                    long? filterPostId,
                    long? filterBranchId,
                    long? filterCarId) =>
                {
                    return context.Activities
                        .Where(a => a.Type == filterType.Value)
                        .Where(a => a.Message.Contains(filterMessage))
                        .Where(a => a.DateTime >= filterFrom.Value)
                        .Where(a => a.DateTime <= filterTo.Value)
                        .Where(a => a.User.Contains(filterUser))
                        .Where(a => a.PostId == filterPostId.Value)
                        .Where(a => a.BranchId == filterBranchId.Value)
                        .Where(a => a.CarId == filterCarId.Value)
                        .OrderByDescending((a) => a.DateTime);
                });
            activitiesDaoMock.Setup(m => m.RemoveToDateAsync(It.IsAny<DateTime>()))
                .Callback((DateTime date) =>
                {
                    context.Activities.RemoveAll(a => a.DateTime < date);
                });
            context.ActivitiesDao = activitiesDaoMock.Object;

            var branchesDaoMock = new Mock<IBranchesDao>();
            branchesDaoMock.Setup(m => m.GetAllAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SortOrder>()))
                .ReturnsAsync(context.Branches);
            context.BranchesDao = branchesDaoMock.Object;

            var carsDaoMock = new Mock<ICarsDao>();
            carsDaoMock.Setup(m => m.GetAllAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SortOrder>()))
                .ReturnsAsync(context.Cars);
            context.CarsDao = carsDaoMock.Object;

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

            return context;
        }

        [Fact]
        public async Task Index_Get()
        {
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;

            var context = MakeContext(returnUrlPath, returnUrlQuery);

            ActivityType? filterType = ActivityType.PostCreated;
            DateTime? filterFrom = DateTime.Today;
            DateTime? filterTo = filterFrom.Value.AddMinutes(75);
            long? filterPostId = context.Mail[0].Id;
            long? filterBranchId = context.Branches[0].Id;
            long? filterCarId = context.Cars[0].Id;
            var options = new ListOptions
            {
                Filters = new Dictionary<string, string>
                {
                    {"type", filterType.Value.ToString()},
                    {"message", "1"},
                    {"from", filterFrom.Value.ToString("g")},
                    {"to", filterTo.Value.ToString("g")},
                    {"user", "2"},
                    {"postId", filterPostId.Value.ToString()},
                    {"branchId", filterBranchId.Value.ToString()},
                    {"carId", filterCarId.Value.ToString()}
                }
            };
            var random = new Random();
            for (int i = 0; i < 100; ++i)
            {
                context.Activities.Add(new Activity
                {
                    Id = i,
                    Type = (ActivityType)(random.Next() % (int)(ActivityType.PostDelivered + 1)),
                    Message = "message" + random.Next(),
                    DateTime = filterFrom.Value.AddMinutes(random.Next() % 100),
                    User = "user" + random.Next(),
                    PostId = random.Next() % context.Mail.Count(),
                    BranchId = random.Next() % context.Branches.Count(),
                    CarId = random.Next() % context.Cars.Count(),
                });
            }

            var controller = new ManagerController(
                context.ActivitiesDao,
                context.BranchesDao,
                context.CarsDao)
            {
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Index(options) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as IndexViewModel;
            Assert.NotNull(vm);
            var expectedActivities = context.Activities
                        .Where(a => a.Type == filterType.Value)
                        .Where(a => a.Message.Contains(options.Filters["message"]))
                        .Where(a => a.DateTime >= filterFrom.Value)
                        .Where(a => a.DateTime <= filterTo.Value)
                        .Where(a => a.User.Contains(options.Filters["user"]))
                        .Where(a => a.PostId == filterPostId.Value)
                        .Where(a => a.BranchId == filterBranchId.Value)
                        .Where(a => a.CarId == filterCarId.Value)
                        .OrderByDescending((a) => a.DateTime)
                        .ToPaginatedList(options.Page, ManagerController.PageSize);

            Assert.Same(Activity.AllTypes, vm.AllActivityTypes);
            Assert.Same(context.Branches, vm.AllBranches);
            Assert.Same(context.Cars, vm.AllCars);
            Assert.Equal(expectedActivities, vm.Activities);
            Assert.Same(options, vm.CurrentListOptions);
            Assert.Equal(returnUrl, vm.ReturnUrl);
        }

        [Fact]
        public void RemoveActivities_Get()
        {
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;

            var context = MakeContext(returnUrlPath, returnUrlQuery);

            var controller = new ManagerController(
                context.ActivitiesDao,
                context.BranchesDao,
                context.CarsDao)
            {
                ControllerContext = context.ControllerContext
            };

            var r = controller.RemoveActivities(returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as RemoveActivitiesViewModel;
            Assert.NotNull(vm);

            Assert.Equal(returnUrl, vm.ReturnUrl);
            Assert.True(vm.ToDate > DateTime.Now.AddHours(-1));
            Assert.True(vm.ToDate < DateTime.Now.AddHours(1));
        }

        [Fact]
        public async void RemoveActivities_Post()
        {
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;

            var context = MakeContext(returnUrlPath, returnUrlQuery);

            DateTime startDate = DateTime.Now;
            DateTime endDate = startDate.AddMinutes(120);
            DateTime removeToDate = startDate.AddMinutes(60);

            var random = new Random();
            for (int i = 0; i < 100; ++i)
            {
                context.Activities.Add(new Activity
                {
                    Id = i,
                    Type = ActivityType.PostCreated,
                    DateTime = startDate.AddMinutes(random.Next() % 120),
                });
            }

            var controller = new ManagerController(
                context.ActivitiesDao,
                context.BranchesDao,
                context.CarsDao)
            {
                ControllerContext = context.ControllerContext
            };

            var expectedActivities = context.Activities.ToList();
            expectedActivities.RemoveAll(a => a.DateTime < removeToDate);

            var vm = new RemoveActivitiesViewModel
            {
                ToDate = removeToDate,
                ReturnUrl = returnUrl
            };

            var r = await controller.RemoveActivities(vm) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(returnUrl, r.Url);
            Assert.Equal(expectedActivities, context.Activities);
        }
    }
}
