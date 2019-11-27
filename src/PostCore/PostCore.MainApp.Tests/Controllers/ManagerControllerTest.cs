using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        Context MakeContext()
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

            return context;
        }

        [Fact]
        public async Task Index_Get()
        {
            var context = MakeContext();

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
                context.CarsDao);

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
        }
    }
}
