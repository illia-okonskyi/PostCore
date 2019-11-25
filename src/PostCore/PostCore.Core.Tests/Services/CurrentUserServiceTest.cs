using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using Xunit;

namespace PostCore.Core.Tests.Services
{
    public class CurrentUserServiceTest
    {
        class Context
        {
            public IHttpContextAccessor HttpContextAccessor { get; set; }
            public IUsersDao UsersDao { get; set; }
            public IBranchesDao BranchesDao { get; set; }
            public ICarsDao CarsDao { get; set; }

            public bool IsAuthenticated { get; set; }
            public User LoggedInUser { get; set; }
            public List<Branch> Branches { get; set; }
            public List<Car> Cars { get; set; }

            public Dictionary<string, byte[]> SessionObjects { get; set; } = new Dictionary<string, byte[]>();
            public delegate void TryGetValueCallback(string key, out byte[] value);
        }

        Context MakeContenxt(
            bool isAuthenticated = false,
            User loggedInUser = null,
            List<Branch> branches = null,
            List<Car> cars = null)
        {
            var context = new Context
            {
                IsAuthenticated = isAuthenticated,
                LoggedInUser = loggedInUser,
                Branches = branches,
                Cars = cars
            };

            var sessionMock = new Mock<ISession>();
            sessionMock.Setup(m => m.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback((string key, byte[] value) =>
                {
                    context.SessionObjects[key] = value;
                });
            var tryGetValueCallback = new Context.TryGetValueCallback((string key, out byte[] value) =>
            {
                context.SessionObjects.TryGetValue(key, out value);
            });
            byte[] dummy;
            sessionMock.Setup(m => m.TryGetValue(It.IsAny<string>(), out dummy))
                .Callback(tryGetValueCallback)
                .Returns(true);
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(m => m.HttpContext.User.Identity.IsAuthenticated)
                .Returns(context.IsAuthenticated);
            httpContextAccessorMock.Setup(m => m.HttpContext.User.Identity.Name)
                .Returns(context.LoggedInUser?.UserName);
            httpContextAccessorMock.Setup(m => m.HttpContext.Session)
                .Returns(sessionMock.Object);
            context.HttpContextAccessor = httpContextAccessorMock.Object;

            var usersDaoMock = new Mock<IUsersDao>();
            usersDaoMock.Setup(m => m.GetByUserNameWithRoleAsync(It.IsAny<string>()))
                .ReturnsAsync(loggedInUser);
            context.UsersDao = usersDaoMock.Object;

            var branchesDaoMock = new Mock<IBranchesDao>();
            branchesDaoMock.Setup(m => m.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => context.Branches.First(b => b.Id == id));
            context.BranchesDao = branchesDaoMock.Object;

            var carsDaoMock = new Mock<ICarsDao>();
            carsDaoMock.Setup(m => m.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => context.Cars.First(c => c.Id == id));
            context.CarsDao = carsDaoMock.Object;

            return context;
        }

        User MakeUser()
        {
            var user = new User
            {
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName",
            };
            var role = new Role
            {
                Id = 1,
                Name = Role.Names.Admin,
            };
            user.UserRoles = new List<UserRole>
            {
                new UserRole { UserId = user.Id, User = user, RoleId = role.Id, Role = role}
            };

            return user;
        }

        [Fact]
        public async Task UserNotLoggedIn()
        {
            var context = MakeContenxt();

            var service = new CurrentUserService(
                context.HttpContextAccessor,
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao);

            Assert.False(service.IsAuthenticated);
            Assert.Null(await service.GetUserAsync());
            Assert.Null(await service.GetRoleAsync());
            Assert.Null(await service.GetBranchAsync());
            Assert.Null(await service.GetCarAsync());
        }

        [Fact]
        public async Task CurrentUserAndRole()
        {
            var user = MakeUser();
            var context = MakeContenxt(
                isAuthenticated: true, 
                loggedInUser: user);

            var service = new CurrentUserService(
                context.HttpContextAccessor,
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao);

            Assert.True(service.IsAuthenticated);
            Assert.Same(user, await service.GetUserAsync());
            Assert.Same(user.Role, await service.GetRoleAsync());
        }

        [Fact]
        public async Task CurrentBranch()
        {
            var user = MakeUser();
            var branches = new List<Branch>
            {
                new Branch { Id = 1, Name = "branch1", Address = "address1"}
            };
            var context = MakeContenxt(
                isAuthenticated: true,
                loggedInUser: user,
                branches: branches);

            var service = new CurrentUserService(
                context.HttpContextAccessor,
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao);

            Assert.Null(await service.GetBranchAsync());
            Assert.True(await service.SetBranchAsync(branches.First().Id));
            Assert.Same(branches.First(), await service.GetBranchAsync());
        }

        [Fact]
        public async Task CurrentCar()
        {
            var user = MakeUser();
            var cars = new List<Car>
            {
                new Car { Id = 1, Model = "model1", Number = "number1"}
            };
            var context = MakeContenxt(
                isAuthenticated: true,
                loggedInUser: user,
                cars: cars);

            var service = new CurrentUserService(
                context.HttpContextAccessor,
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao);

            Assert.Null(await service.GetCarAsync());
            Assert.True(await service.SetCarAsync(cars.First().Id));
            Assert.Same(cars.First(), await service.GetCarAsync());
        }

        [Fact]
        public async Task Reset()
        {
            var user = MakeUser();
            var branches = new List<Branch>
            {
                new Branch { Id = 1, Name = "branch1", Address = "address1"}
            };
            var cars = new List<Car>
            {
                new Car { Id = 1, Model = "model1", Number = "number1"}
            };
            var context = MakeContenxt(
                isAuthenticated: true,
                loggedInUser: user,
                branches: branches,
                cars: cars);

            var service = new CurrentUserService(
                context.HttpContextAccessor,
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao);

            Assert.True(await service.SetBranchAsync(cars.First().Id));
            Assert.True(await service.SetCarAsync(cars.First().Id));
            Assert.Same(branches.First(), await service.GetBranchAsync());
            Assert.Same(cars.First(), await service.GetCarAsync());

            service.Reset();

            Assert.Null(await service.GetBranchAsync());
            Assert.Null(await service.GetCarAsync());
        }
    }
}
