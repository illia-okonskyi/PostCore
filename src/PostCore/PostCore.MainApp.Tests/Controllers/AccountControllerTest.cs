using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.Controllers;
using PostCore.MainApp.ViewModels.Account;
using PostCore.Utils;
using Xunit;

namespace PostCore.MainApp.Tests.Controllers
{
    public class AccountControllerTest
    {
        class Context
        {
            public static string DefaultPassword = "1";

            public Mock<IUsersDao> UsersDaoMock { get; set; }
            public IUsersDao UsersDao { get; set; }
            public IBranchesDao BranchesDao { get; set; }
            public ICarsDao CarsDao { get; set; }
            public Mock<ICurrentUserService> CurrentUserServiceMock { get; set; }
            public ICurrentUserService CurrentUserService { get; set; }
            public ITempDataDictionary TempDataDictionary { get; set; }

            public List<User> Users { get; set; } = MakeUsers();
            public List<Branch> Branches { get; set; } = new List<Branch>
            {
                new Branch { Id = 1, Name = "name1", Address = "address1"},
                new Branch { Id = 2, Name = "name2", Address = "address2"}
            };
            public List<Car> Cars { get; set; } = new List<Car>
            {
                new Car { Id = 1, Model = "model1", Number = "number1" },
                new Car { Id = 2, Model = "model2", Number = "number2" }
            };
            public User LoggedInUser { get; set; }
            public Branch CurrentBranch { get; set; }
            public Car CurrentCar { get; set; }

            public Dictionary<string, object> TempData { get; set; } = new Dictionary<string, object>();

            static List<User> MakeUsers()
            {
                var op = new User
                {
                    Id = 1,
                    UserName = "operator",
                    Email = "operator@example.com",
                    FirstName = "firstNameOp",
                    LastName = "lastNameOp",
                    PasswordHash = DefaultPassword
                };
                var opRole = new Role
                {
                    Id = 1,
                    Name = Role.Names.Operator
                };
                op.UserRoles = new List<UserRole>
                {
                    new UserRole { UserId = op.Id, User = op, RoleId = opRole.Id, Role = opRole}
                };

                var driver = new User
                {
                    Id = 2,
                    UserName = "driver",
                    Email = "driver@example.com",
                    FirstName = "firstNameDriver",
                    LastName = "lastNameDriver",
                    PasswordHash = DefaultPassword
                };
                var driverRole = new Role
                {
                    Id = 2,
                    Name = Role.Names.Driver
                };
                driver.UserRoles = new List<UserRole>
                {
                    new UserRole { UserId = driver.Id, User = driver, RoleId = driverRole.Id, Role = driverRole}
                };

                return new List<User> { op, driver };
            }
        }

        Context MakeContext()
        {
            var context = new Context();

            var usersDaoMock = new Mock<IUsersDao>();
            usersDaoMock.Setup(m => m.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((string email, string password, bool rememberMe) =>
                {
                    var user = context.Users.FirstOrDefault(u => u.Email == email);
                    if (user == null)
                    {
                        return false;
                    }
                    if (user.PasswordHash != password)
                    {
                        return false;
                    }

                    context.LoggedInUser = user;
                    return true;
                });
            usersDaoMock.Setup(m => m.LogoutAsync())
                .Callback(() => context.LoggedInUser = null);

            usersDaoMock.Setup(m => m.CheckPasswordAsync(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync((long id, string password) =>
                {
                    var user = context.Users.First(u => u.Id == id);
                    return user.PasswordHash == password;
                });
            usersDaoMock.Setup(m => m.UpdateAsync(It.IsAny<User>(), It.IsAny<long>()))
                .Callback((User user, long roleId) =>
                {
                    var contextUser = context.Users.First(u => u.Id == user.Id);
                    contextUser.UserName = user.UserName;
                    contextUser.Email = user.Email;
                    contextUser.FirstName = user.FirstName;
                    contextUser.LastName = user.LastName;
                });
            usersDaoMock.Setup(m => m.ChangePasswordAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((long userId, string currentPassword, string newPassword) =>
                {
                    var user = context.Users.First(u => u.Id == userId);
                    user.PasswordHash = newPassword;
                });
            context.UsersDaoMock = usersDaoMock;
            context.UsersDao = usersDaoMock.Object;

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

            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(m => m.GetUserAsync())
                .ReturnsAsync(() =>
                {
                    return context.LoggedInUser;
                });
            currentUserServiceMock.Setup(m => m.GetRoleAsync())
                .ReturnsAsync(() =>
                {
                    return context.LoggedInUser?.Role;
                });
            currentUserServiceMock.Setup(m => m.SetBranchAsync(It.IsAny<long>()))
                .Callback((long branchId) =>
                {
                    var branch = context.Branches.First(b => b.Id == branchId);
                    context.CurrentBranch = branch;
                })
                .ReturnsAsync(true);
            currentUserServiceMock.Setup(m => m.SetCarAsync(It.IsAny<long>()))
                .Callback((long carId) =>
                {
                    var car = context.Cars.First(b => b.Id == carId);
                    context.CurrentCar = car;
                })
                .ReturnsAsync(true);
            currentUserServiceMock.Setup(m => m.Reset())
                .Callback(() =>
                {
                    context.CurrentBranch = null;
                    context.CurrentCar = null;
                });
            context.CurrentUserServiceMock = currentUserServiceMock;
            context.CurrentUserService = currentUserServiceMock.Object;

            var tempDataDictionaryMock = new Mock<ITempDataDictionary>();
            tempDataDictionaryMock.Setup(m => m[It.IsAny<string>()])
                .Returns((string key) => context.TempData[key]);
            tempDataDictionaryMock.SetupSet(m => m[It.IsAny<string>()] = It.IsAny<object>())
                .Callback((string key, object o) => context.TempData[key] = o);
            context.TempDataDictionary = tempDataDictionaryMock.Object;

            return context;
        }

        [Fact]
        public void AccessDenied_Get()
        {
            var context = MakeContext();

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = controller.AccessDenied() as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
        }

        [Fact]
        public void Login_Get()
        {
            var returnUrl = "/";
            var context = MakeContext();
            var expectedVm = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = controller.Login(returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);

            var vm = r.Model as LoginViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Login_Post_Failed()
        {
            var context = MakeContext();
            var vm = new LoginViewModel
            {
                Email = "otherEmail@example.com",
                Password = "123"
            };

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = await controller.Login(vm) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);

            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(vm.Email)));
        }

        [Fact]
        public async Task Login_Post_Success()
        {
            var context = MakeContext();
            var user = context.Users[0];
            var vm = new LoginViewModel
            {
                Email = user.Email,
                Password = user.PasswordHash,
                ReturnUrl = "/"
            };

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = await controller.Login(vm) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(vm.ReturnUrl, r.Url);
            Assert.Same(user, context.LoggedInUser);
        }

        [Fact]
        public async Task Logout_Get()
        {
            var context = MakeContext();
            var user = context.Users[0];
            context.LoggedInUser = user;

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };


            var r = await controller.Logout() as RedirectToActionResult;
            Assert.NotNull(r);
            Assert.Equal("Home", r.ControllerName);
            Assert.Equal("Index", r.ActionName);
            Assert.Null(context.LoggedInUser);
        }

        [Fact]
        public async Task Manage_Get()
        {
            var returnUrl = "/";
            var context = MakeContext();
            var user = context.Users[0];
            context.LoggedInUser = user;

            var expectedVm = new ManageViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                HasBranch = user.Role.HasBranch,
                AllBranches = user.Role.HasBranch ? context.Branches : null,
                HasCar = user.Role.HasCar,
                AllCars = user.Role.HasCar ? context.Cars : null,
                ReturnUrl = returnUrl
            };

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = await controller.Manage(returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);

            var vm = r.Model as ManageViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.UserId, vm.UserId);
            Assert.Equal(expectedVm.UserName, vm.UserName);
            Assert.Equal(expectedVm.Email, vm.Email);
            Assert.Equal(expectedVm.FirstName, vm.FirstName);
            Assert.Equal(expectedVm.LastName, vm.LastName);
            Assert.Equal(expectedVm.HasBranch, vm.HasBranch);
            Assert.Equal(expectedVm.AllBranches, vm.AllBranches);
            Assert.Equal(expectedVm.AllCars, vm.AllCars);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Manage_Post_CheckPasswordFailed()
        {
            var returnUrl = "/";
            var context = MakeContext();
            var user = context.Users[1];
            context.LoggedInUser = user;

            var branch = context.Branches[0];
            var car = context.Cars[0];
            var vm = new ManageViewModel
            {
                UserId = user.Id,
                UserName = user.UserName + "1",
                Email = user.Email + "1",
                FirstName = user.FirstName + "1",
                LastName = user.LastName + "1",
                HasBranch = user.Role.HasBranch,
                BranchId = branch.Id,
                HasCar = user.Role.HasCar,
                CarId = car.Id,
                Password = "123",
                ReturnUrl = returnUrl
            };

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = await controller.Manage(vm) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            Assert.True(controller.ModelState.ContainsKey(nameof(vm.Password)));

            var returnedVm = r.Model as ManageViewModel;
            Assert.NotNull(returnedVm);
            var expectedAllBranches = vm.HasBranch ? context.Branches : null;
            var expectedAllCars = vm.HasCar ? context.Cars : null;
            Assert.Equal(expectedAllBranches, returnedVm.AllBranches);
            Assert.Equal(expectedAllCars, returnedVm.AllCars);
            Assert.Equal(vm.ReturnUrl, returnedVm.ReturnUrl);
            context.UsersDaoMock.Verify(
                m => m.UpdateAsync(It.IsAny<User>(), It.IsAny<long>()),
                Times.Never);
            context.CurrentUserServiceMock.Verify(
                m => m.SetBranchAsync(It.IsAny<long>()),
                Times.Never);
            context.CurrentUserServiceMock.Verify(
                m => m.SetCarAsync(It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task Manage_Post_Success()
        {
            var returnUrl = "/";
            var context = MakeContext();
            var user = context.Users[1];
            context.LoggedInUser = user;

            var branch = context.Branches[0];
            var car = context.Cars[0];
            var vm = new ManageViewModel
            {
                UserId = user.Id,
                UserName = user.UserName + "1",
                Email = user.Email + "1",
                FirstName = user.FirstName + "1",
                LastName = user.LastName + "1",
                HasBranch = user.Role.HasBranch,
                BranchId = branch.Id,
                HasCar = user.Role.HasCar,
                CarId = car.Id,
                Password = user.PasswordHash,
                ReturnUrl = returnUrl
            };

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = await controller.Manage(vm) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);

            var returnedVm = r.Model as ManageViewModel;
            Assert.NotNull(returnedVm);
            var expectedAllBranches = vm.HasBranch ? context.Branches : null;
            var expectedAllCars = vm.HasCar ? context.Cars : null;
            Assert.Equal(expectedAllBranches, returnedVm.AllBranches);
            Assert.Equal(expectedAllCars, returnedVm.AllCars);
            Assert.Equal(vm.ReturnUrl, returnedVm.ReturnUrl);
            Assert.Equal(vm.UserName, context.LoggedInUser.UserName);
            Assert.Equal(vm.Email, context.LoggedInUser.Email);
            Assert.Equal(vm.FirstName, context.LoggedInUser.FirstName);
            Assert.Equal(vm.LastName, context.LoggedInUser.LastName);
            if (vm.HasBranch)
            {
                Assert.Same(branch, context.CurrentBranch);
            }
            else
            {
                context.CurrentUserServiceMock.Verify(
                    m => m.SetBranchAsync(It.IsAny<long>()),
                    Times.Never);
            }
            if (vm.HasCar)
            {
                Assert.Same(car, context.CurrentCar);
            }
            else
            {
                context.CurrentUserServiceMock.Verify(
                    m => m.SetCarAsync(It.IsAny<long>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task ChangePassword_Get()
        {
            var returnUrl = "/";
            var context = MakeContext();
            var user = context.Users[0];
            context.LoggedInUser = user;

            var expectedVm = new ChangePasswordViewModel
            {
                UserId = user.Id,
                ReturnUrl = returnUrl
            };

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = await controller.ChangePassword(returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);

            var vm = r.Model as ChangePasswordViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.UserId, vm.UserId);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task ChangePassword_Post()
        {
            var returnUrl = "/";
            var context = MakeContext();
            var user = context.Users[1];
            context.LoggedInUser = user;

            var vm = new ChangePasswordViewModel
            {
                UserId = user.Id,
                CurrentPassword = user.PasswordHash,
                NewPassword = "123",
                NewPasswordRepeat = "123",
                ReturnUrl = returnUrl
            };

            var controller = new AccountController(
                context.UsersDao,
                context.BranchesDao,
                context.CarsDao,
                context.CurrentUserService)
            {
                TempData = context.TempDataDictionary
            };

            var r = await controller.ChangePassword(vm) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);

            var returnedVm = r.Model as ChangePasswordViewModel;
            Assert.NotNull(returnedVm);
            Assert.Equal(vm.UserId, returnedVm.UserId);
            Assert.Equal(vm.ReturnUrl, returnedVm.ReturnUrl);
            Assert.Equal(vm.NewPassword, user.PasswordHash);
            Assert.NotNull(controller.TempData["message"]);
        }
    }
}
