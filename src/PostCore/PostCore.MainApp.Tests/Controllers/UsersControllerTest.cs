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
using PostCore.MainApp.ViewModels.Users;
using PostCore.ViewUtils;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;

namespace PostCore.MainApp.Tests.Controllers
{
    public static class Extensions
    {
        public static IEnumerable<Role> ExceptAdmin(this IEnumerable<Role> roles)
        {
            return roles.Where(r => r.Name != Role.Names.Admin);
        }
    }

    public class UsersControllerTest
    {
        static readonly string DefaultPasswordKey = "Config:Users:DefaultPassword";
        static readonly string DefaultPassword = "1";

        class Context
        {
            public IConfiguration Configuration { get; set; }
            public IRolesDao RolesDao { get; set; }
            public IUsersDao UsersDao { get; set; }
            public ITempDataDictionary TempDataDictionary { get; set; }
            public ControllerContext ControllerContext { get; set; }
            public List<Role> Roles { get; set; } = new List<Role>();
            public List<User> Users { get; set; } = new List<User>();
            public Dictionary<string, object> TempData { get; set; } = new Dictionary<string, object>();
        }

        Context MakeContext(string path = "/", string query = "?query=query")
        {
            var context = new Context();

            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(m => m[DefaultPasswordKey])
                .Returns(DefaultPassword);
            context.Configuration = configurationMock.Object;

            context.Roles.Add(new Role { Id = 1, Name = Role.Names.Admin });
            context.Roles.Add(new Role { Id = 2, Name = Role.Names.Operator });
            context.Roles.Add(new Role { Id = 3, Name = Role.Names.Manager });

            var rolesDaoMock = new Mock<IRolesDao>();
            rolesDaoMock.Setup(m => m.GetAllAsync(It.IsAny<bool>()))
                .ReturnsAsync((bool includeAdmin) => {
                    var roles = context.Roles.AsEnumerable();
                    if (!includeAdmin)
                    {
                        roles = roles.ExceptAdmin();
                    }
                    return roles;
                });
            rolesDaoMock.Setup(m => m.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => context.Roles.First(r => r.Id == id));
            context.RolesDao = rolesDaoMock.Object;

            var usersDaoMock = new Mock<IUsersDao>();
            usersDaoMock.Setup(m => m.GetAllAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SortOrder>()))
                .ReturnsAsync((
                    string filterUserName,
                    string filterEmail,
                    string filterFistName,
                    string filterLastName,
                    string filterRoleName,
                    string sortKey,
                    SortOrder sortOrder) =>
                {
                    return context.Users
                        .Where(u => u.UserName.Contains(filterUserName))
                        .Where(u => u.Email.Contains(filterEmail))
                        .Where(u => u.FirstName.Contains(filterFistName))
                        .Where(u => u.LastName.Contains(filterLastName))
                        .Where(u => u.Role.Name.Contains(filterRoleName))
                        .Order(sortKey, sortOrder);
                });
            usersDaoMock.Setup(m => m.GetByIdWithRoleAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => context.Users.First(u => u.Id == id));
            
            usersDaoMock.Setup(m => m.CreateAsync(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Callback((User user, string password, string roleName) =>
                {
                    user.PasswordHash = password;
                    user.UserRoles = new List<UserRole>
                    {
                        new UserRole
                        {
                            User = user,
                            Role = new Role
                            {
                                Name = roleName
                            }
                        }
                    };
                    context.Users.Add(user);
                });
            usersDaoMock.Setup(m => m.UpdateAsync(It.IsAny<User>(), It.IsAny<long>()))
                .Callback((User user, long roleId) =>
                {
                    var contextUser = context.Users.First(u => u.Id == user.Id);
                    contextUser.UserName = user.UserName;
                    contextUser.Email = user.Email;
                    contextUser.FirstName = user.FirstName;
                    contextUser.LastName = user.LastName;
                    contextUser.UserRoles.First().RoleId = roleId;
                });
            usersDaoMock.Setup(m => m.ResetPasswordAsync(It.IsAny<long>(), It.IsAny<string>()))
                .Callback((long userId, string password) =>
                {
                    var user = context.Users.First(u => u.Id == userId);
                    user.PasswordHash = password;
                });
            usersDaoMock.Setup(m => m.DeleteAsync(It.IsAny<long>()))
                .Callback((long userId) =>
                {
                    var user = context.Users.First(u => u.Id == userId);
                    context.Users.Remove(user);
                });
            context.UsersDao = usersDaoMock.Object;

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
        public async Task Index_Get()
        {
            var options = new ListOptions
            {
                Filters = new Dictionary<string, string>
                {
                    { "userName", "1"},
                    { "email", "2"},
                    { "firstName", "3"},
                    { "lastName", "4"},
                    { "roleName", "5"},
                },
                SortKey = "FirstName",
                SortOrder = SortOrder.Ascending
            };
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;

            var context = MakeContext(returnUrlPath, returnUrlQuery);
            var random = new Random();
            for (int i = 0; i < 100; ++i)
            {
                await context.UsersDao.CreateAsync(new User
                {
                    Id = i,
                    UserName = "userName" + random.Next(),
                    Email = "email@example.com" + random.Next(),
                    FirstName = "firstName" + random.Next(),
                    LastName = "lastName" + random.Next(),
                },
                DefaultPassword,
                "operator" + random.Next());
            }

            var controller = new UsersController(
                context.Configuration,
                context.RolesDao,
                context.UsersDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Index(options) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as IndexViewModel;
            Assert.NotNull(vm);
            var expectedUsers = context.Users
                .Where(u => u.UserName.Contains(options.Filters["userName"]))
                .Where(u => u.Email.Contains(options.Filters["email"]))
                .Where(u => u.FirstName.Contains(options.Filters["firstName"]))
                .Where(u => u.LastName.Contains(options.Filters["lastName"]))
                .Where(u => u.Role.Name.Contains(options.Filters["roleName"]))
                .Order(options.SortKey, options.SortOrder)
                .ToPaginatedList(options.Page, UsersController.PageSize);
            Assert.Equal(expectedUsers, vm.Users);
            Assert.Same(options, vm.CurrentListOptions);
            Assert.Equal(returnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Create_Get()
        {
            var context = MakeContext();

            var returnUrl = "/";
            var expectedVm = new EditViewModel
            {
                AllRoles = context.Roles.ExceptAdmin(),
                IsAdminUser = false,
                EditorMode = EditorMode.Create,
                ReturnUrl = returnUrl
            };

            var controller = new UsersController(
                context.Configuration,
                context.RolesDao,
                context.UsersDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = (await controller.Create(returnUrl)) as ViewResult;
            Assert.NotNull(r);
            Assert.Equal(nameof(controller.Edit), r.ViewName);
            var vm = r.Model as EditViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.AllRoles, vm.AllRoles);
            Assert.Equal(expectedVm.IsAdminUser, vm.IsAdminUser);
            Assert.Equal(expectedVm.EditorMode, vm.EditorMode);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Edit_Get()
        {
            var context = MakeContext();

            var user = new User
            {
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName"
            };
            var returnUrl = "/";
            var expectedVm = new EditViewModel
            {
                AllRoles = context.Roles.ExceptAdmin(),
                IsAdminUser = false,
                EditorMode = EditorMode.Update,
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = 0,
                ReturnUrl = returnUrl
            };
            var role = context.Roles[1];

            await context.UsersDao.CreateAsync(user, DefaultPassword, role.Name);

            var controller = new UsersController(
                context.Configuration,
                context.RolesDao,
                context.UsersDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Edit(user.Id, returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as EditViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.AllRoles, vm.AllRoles);
            Assert.Equal(expectedVm.EditorMode, vm.EditorMode);
            Assert.Equal(expectedVm.Id, vm.Id);
            Assert.Equal(expectedVm.UserName, vm.UserName);
            Assert.Equal(expectedVm.Email, vm.Email);
            Assert.Equal(expectedVm.FirstName, vm.FirstName);
            Assert.Equal(expectedVm.LastName, vm.LastName);
            Assert.Equal(expectedVm.RoleId, vm.RoleId);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Edit_Get_AdminUser()
        {
            var context = MakeContext();

            var user = new User
            {
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName"
            };
            var returnUrl = "/";
            var expectedVm = new EditViewModel
            {
                IsAdminUser = true,
                EditorMode = EditorMode.Update,
                ReturnUrl = returnUrl
            };
            var role = context.Roles[0];

            await context.UsersDao.CreateAsync(user, DefaultPassword, role.Name);

            var controller = new UsersController(
                context.Configuration,
                context.RolesDao,
                context.UsersDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Edit(user.Id, returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as EditViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.IsAdminUser, vm.IsAdminUser);
            Assert.Equal(expectedVm.EditorMode, vm.EditorMode);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Edit_Post_CreateUser()
        {
            var context = MakeContext();

            var controller = new UsersController(
                context.Configuration,
                context.RolesDao,
                context.UsersDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };
            var role = context.Roles[0];
            var vm = new EditViewModel
            {
                EditorMode = EditorMode.Create,
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName",
                RoleId = role.Id,
                ReturnUrl = "/"
            };

            var r = await controller.Edit(vm) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(vm.ReturnUrl, r.Url);
            Assert.Single(context.Users);
            var user = context.Users.First();
            Assert.Equal(vm.UserName, user.UserName);
            Assert.Equal(vm.Email, user.Email);
            Assert.Equal(vm.FirstName, user.FirstName);
            Assert.Equal(vm.LastName, user.LastName);
            Assert.Equal(DefaultPassword, user.PasswordHash);
            Assert.Equal(role.Name, user.Role.Name);
        }

        [Fact]
        public async Task Edit_Post_UpdateUser()
        {
            var context = MakeContext();

            var user = new User
            {
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName",
            };
            var oldRole = context.Roles[1];
            var newRole = context.Roles[2];

            await context.UsersDao.CreateAsync(user, DefaultPassword, oldRole.Name);

            var controller = new UsersController(
                context.Configuration,
                context.RolesDao,
                context.UsersDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };
            var vm = new EditViewModel
            {
                EditorMode = EditorMode.Update,
                Id = 1,
                UserName = "userName1",
                Email = "email@example.com1",
                FirstName = "firstName1",
                LastName = "lastName1",
                RoleId = newRole.Id,
                ReturnUrl = "/"
            };

            var r = await controller.Edit(vm) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(vm.ReturnUrl, r.Url);
            Assert.Single(context.Users);
            var updatedUser = context.Users.First();
            Assert.Equal(vm.UserName, updatedUser.UserName);
            Assert.Equal(vm.Email, updatedUser.Email);
            Assert.Equal(vm.FirstName, updatedUser.FirstName);
            Assert.Equal(vm.LastName, updatedUser.LastName);
            Assert.Equal(vm.RoleId, updatedUser.UserRoles.First().RoleId);
        }

        [Fact]
        public async Task ResetPassword_Post()
        {
            var user = new User
            {
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName"
            };

            var context = MakeContext();
            await context.UsersDao.CreateAsync(user, "123", context.Roles[0].Name);

            var controller = new UsersController(
                context.Configuration,
                context.RolesDao,
                context.UsersDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };
            var vm = new EditViewModel
            {
                EditorMode = EditorMode.Update,
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName",
                RoleId = 1,
                ReturnUrl = "/"
            };

            var r = await controller.ResetPassword(vm) as ViewResult;
            Assert.NotNull(r);
            Assert.Same(vm, r.Model);
            Assert.Equal(DefaultPassword, context.Users.First().PasswordHash);
            Assert.NotNull(controller.TempData["message"]);
        }

        [Fact]
        public async Task Delete_Post()
        {
            var user = new User
            {
                Id = 1,
                UserName = "userName",
                Email = "email@example.com",
                FirstName = "firstName",
                LastName = "lastName"
            };
            var returnUrl = "/";

            var context = MakeContext();
            await context.UsersDao.CreateAsync(user, DefaultPassword, context.Roles[0].Name);

            var controller = new UsersController(
                context.Configuration,
                context.RolesDao,
                context.UsersDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Delete(user.Id, returnUrl) as RedirectResult;
            Assert.NotNull(r);
            Assert.Same(returnUrl, r.Url);
            Assert.Empty(context.Users);
        }
    }
}
