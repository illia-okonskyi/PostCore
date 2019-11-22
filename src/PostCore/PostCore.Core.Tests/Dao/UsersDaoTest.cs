using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;
using MockQueryable.Moq;
using PostCore.Core.Dao;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;
using PostCore.Utils;

namespace PostCore.Core.Tests.Dao
{
    public class UsersDaoTest
    {
        class Context
        {
            public List<User> Users { get; set; } = new List<User>();
            public Dictionary<long, List<string>> UserRoles { get; set; } = new Dictionary<long, List<string>>();
            public UserManager<User> UserManager { get; set; }

            public void AddUser(User user, string password)
            {
                user.PasswordHash = password;
                Users.Add(user);
            }

            public void AddUserRole(User user, string roleName)
            {
                if (!UserRoles.ContainsKey(user.Id))
                {
                    UserRoles.Add(user.Id, new List<string>());
                }

                UserRoles[user.Id].Add(roleName);
            }

            public bool IsUserInRole(User user, string roleName)
            {
                if (!UserRoles.ContainsKey(user.Id))
                {
                    return false;
                }

                return UserRoles[user.Id].Contains(roleName);
            }
        }

        static Mock<UserManager<User>> MakeUserManagerMock()
        {
            return new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<User>>().Object,
                new IUserValidator<User>[0],
                new IPasswordValidator<User>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<User>>>().Object);
        }

        Context MakeContext()
        {
            var context = new Context();
            var mock = MakeUserManagerMock();
            
            var successResult = Task.FromResult(IdentityResult.Success);

            mock.Setup(m => m.Users)
                .Returns(() =>
                {
                    var m = context.Users.AsQueryable().BuildMock();
                    return m.Object;
                });
            mock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .Returns((string id) =>
                   Task.FromResult(
                       context.Users.First((User user) => user.Id == long.Parse(id))));
            mock.Setup(m => m.FindByNameAsync(It.IsAny<string>()))
                .Returns((string userName) =>
                   Task.FromResult(
                       context.Users.First((User user) => user.UserName == userName)));
            mock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .Returns((string email) =>
                   Task.FromResult(
                       context.Users.First((User user) => user.Email == email)));
            mock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .Callback((User user, string password) => context.AddUser(user, password))
                .Returns(successResult);
            mock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .Callback((User user, string roleName) => context.AddUserRole(user, roleName))
                .Returns(successResult);
            mock.Setup(m => m.UpdateAsync(It.IsAny<User>()))
                .Callback((User user) =>
                {
                    var oldUser = context.Users.First(u => u.Id == user.Id);
                    oldUser = user;
                })
                .Returns(successResult);
            mock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
                .Callback((User user) => context.Users.Remove(user))
                .Returns(successResult);
            mock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync((User user, string password) => {
                    var foundUser = context.Users.First(u => u.Id == user.Id);
                    return foundUser.PasswordHash == password;
                });
            mock.Setup(m => m.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((User user, string currentPassword, string newPassword) => {
                    var foundUser = context.Users.First(u => u.Id == user.Id);
                    foundUser.PasswordHash = newPassword;
                })
                .Returns(successResult);
            mock.Setup(m => m.GeneratePasswordResetTokenAsync(It.IsAny<User>()))
                .ReturnsAsync("token");
            mock.Setup(m => m.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((User user, string token, string newPassword) => {
                    var foundUser = context.Users.First(u => u.Id == user.Id);
                    foundUser.PasswordHash = newPassword;
                })
                .Returns(successResult);

            mock.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
                .Returns((User user) => Task.FromResult<IList<string>>(context.UserRoles[user.Id]));

            context.UserManager = mock.Object;
            return context;
        }

        Context MakeErrorContext(bool findByIdSuccess)
        {
            var context = new Context();
            var mock = MakeUserManagerMock();

            var nullUser = Task.FromResult<User>(null);
            var failedResult = Task.FromResult(
                IdentityResult.Failed(
                    new IdentityError()
                    {
                        Code = "ErrorCode",
                        Description = "ErrorDescription"
                    })
                );

            mock.Setup(m => m.Users)
                .Returns(() =>
                {
                    var m = context.Users.AsQueryable().BuildMock();
                    return m.Object;
                });

            if (findByIdSuccess)
            {
                mock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                    .Returns((string id) =>
                        Task.FromResult(context.Users.First((User user) => user.Id == long.Parse(id))));
            }
            else
            {
                mock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                    .Returns(nullUser);
            }

            mock.Setup(m => m.FindByNameAsync(It.IsAny<string>()))
                .Returns(nullUser);
            mock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .Returns(nullUser);
            mock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .Returns(failedResult);
            mock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .Returns(failedResult);
            mock.Setup(m => m.UpdateAsync(It.IsAny<User>()))
                .Returns(failedResult);
            mock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
                .Returns(failedResult);
            mock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(false);
            mock.Setup(m => m.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(failedResult);
            mock.Setup(m => m.GeneratePasswordResetTokenAsync(It.IsAny<User>()))
                .ReturnsAsync("token");
            mock.Setup(m => m.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(failedResult);

            context.UserManager = mock.Object;
            return context;
        }

        [Fact]
        public async Task Create()
        {
            var user = new User
            {
                Id = 1,
                UserName = "UserName",
                Email = "email@example.com",
                FirstName = "FirstName",
                LastName = "LastName"
            };
            var password = "password";
            var roleName = "operator";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);
            await dao.CreateAsync(user, password, roleName);

            Assert.Single(context.Users);
            var createdUser = context.Users.First();
            Assert.Equal(user.Id, createdUser.Id);
            Assert.Equal(user.UserName, createdUser.UserName);
            Assert.Equal(user.Email, createdUser.Email);
            Assert.Equal(user.FirstName, createdUser.FirstName);
            Assert.Equal(user.LastName, createdUser.LastName);
            Assert.Equal(password, createdUser.PasswordHash);
            Assert.True(context.IsUserInRole(createdUser, roleName));

            var errorContext = MakeErrorContext(false);
            var errorDao = new UsersDao(errorContext.UserManager);
            await Assert.ThrowsAsync<IdentityException>(async () =>
                await errorDao.CreateAsync(user, password, roleName)
            );
        }

        [Fact]
        public async Task InitialSetup()
        {
            var admin = "Admin";
            var adminUserName = "admin";
            var adminEmail = "admin@example.com";
            var adminPassword = "password";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);
            await dao.InitialSetupAsync(adminUserName, adminEmail, adminPassword);

            Assert.Single(context.Users);
            var createdUser = context.Users.First();
            Assert.Equal(adminUserName, createdUser.UserName);
            Assert.Equal(adminEmail, createdUser.Email);
            Assert.Equal(admin, createdUser.FirstName);
            Assert.Equal(admin, createdUser.LastName);
            Assert.Equal(adminPassword, createdUser.PasswordHash);
            Assert.True(context.IsUserInRole(createdUser, Role.Names.Admin));

            var errorContext = MakeErrorContext(false);
            var errorDao = new UsersDao(errorContext.UserManager);
            await Assert.ThrowsAsync<InitialSetupException>(async () =>
                await errorDao.InitialSetupAsync(adminUserName, adminEmail, adminPassword)
            );
        }

        [Fact]
        public async Task Get()
        {
            var users = new User[]
            {
                new User
                {
                    Id = 1,
                    UserName = "UserName1",
                    Email = "username1@example.com"
                },
                new User
                {
                    Id = 2,
                    UserName = "UserName2",
                    Email = "username2@example.com"
                },
                new User
                {
                    Id = 3,
                    UserName = "UserName3",
                    Email = "username3@example.com"
                }
            };
            var password = "password";
            var roleName = "operator";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            await dao.CreateAsync(users[0], password, roleName);
            await dao.CreateAsync(users[1], password, roleName);
            await dao.CreateAsync(users[2], password, roleName);

            var allUsers = await dao.GetAllAsync();
            Assert.Equal(3, allUsers.Count());

            var userById = await dao.GetByIdAsync(users[0].Id);
            Assert.Equal(users[0].Id, userById.Id);
            Assert.Equal(users[0].UserName, userById.UserName);
            Assert.Equal(users[0].Email, userById.Email);

            var userByUserName = await dao.GetByUserNameAsync(users[1].UserName);
            Assert.Equal(users[1].Id, userByUserName.Id);
            Assert.Equal(users[1].UserName, userByUserName.UserName);
            Assert.Equal(users[1].Email, userByUserName.Email);

            var userByEmail = await dao.GetByEmailAsync(users[2].Email);
            Assert.Equal(users[2].Id, userByEmail.Id);
            Assert.Equal(users[2].UserName, userByEmail.UserName);
            Assert.Equal(users[2].Email, userByEmail.Email);

            var errorContext = MakeErrorContext(false);
            var errorDao = new UsersDao(errorContext.UserManager);
            Assert.Null(await errorDao.GetByIdAsync(users[0].Id));
            Assert.Null(await errorDao.GetByUserNameAsync(users[1].UserName));
            Assert.Null(await errorDao.GetByEmailAsync(users[2].UserName));
        }

        [Fact]
        public async Task GetFiltered()
        {
            var usersCount = 10;
            var users = new List<User>();
            for (int i = 0; i < usersCount; ++i) {
                users.Add(new User
                {
                    Id = i,
                    UserName = "UserName" + i % 2,
                    Email = "email" + i % 3+ "@example.com",
                    FirstName = "John" + i % 4,
                    LastName = "Smith" + i % 5
                });
            }
            var password = "password";
            var roleName = "operator";
            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            for (int i = 0; i < usersCount; ++i)
            {
                await dao.CreateAsync(users[i], password, roleName);
            }

            // filtered user
            var filtered = await dao.GetAllAsync(
                filterUserName: "1",
                filterEmail: "1",
                filterFirstName: "3",
                filterLastName: "2");
            Assert.Single(filtered);
            Assert.Equal(users[7].Id, filtered.First().Id);
        }

        [Fact]
        public async Task GetOrdered()
        {
            var random = new Random();
            var usersCount = 10;
            var users = new List<User>();
            for (int i = 0; i < usersCount; ++i)
            {
                users.Add(new User
                {
                    Id = i,
                    UserName = "UserName" + random.Next(),
                    Email = "email" + random.Next() + "@example.com",
                    FirstName = "John" + random.Next(),
                    LastName = "Smith" + random.Next()
                });
            }
            var password = "password";
            var roleName = "operator";
            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            for (int i = 0; i < usersCount; ++i)
            {
                await dao.CreateAsync(users[i], password, roleName);
            }

            // wrong key
            await Assert.ThrowsAsync<ArgumentException>("sortKey", () => dao.GetAllAsync(sortKey: "afefe"));

            // defaults
            users = users.AsEnumerable().Order("Id", SortOrder.Ascending).ToList();
            var ordered = (await dao.GetAllAsync()).ToList();
            for (int i = 0; i < usersCount; ++i)
            {
                Assert.Equal(users[i].Id, ordered[i].Id);
            }

            // order by first name desc
            users = users.AsEnumerable().Order("FirstName", SortOrder.Descending).ToList();
            ordered = (await dao.GetAllAsync(sortKey: "FirstName", sortOrder: SortOrder.Descending)).ToList();
            for (int i = 0; i < usersCount; ++i)
            {
                Assert.Equal(users[i].Id, ordered[i].Id);
            }
        }

        [Fact]
        public async Task Update()
        {
            var user = new User
            {
                Id = 1,
                UserName = "UserName1",
            };
            var password = "password";
            var roleName = "operator";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            await dao.CreateAsync(user, password, roleName);

            Assert.Equal(user.Id, context.Users[0].Id);
            Assert.Equal(user.UserName, context.Users[0].UserName);

            user.UserName = "UserName2";
            await dao.UpdateAsync(user);

            Assert.Equal(user.UserName, context.Users[0].UserName);

            var errorContextFindFailed = MakeErrorContext(false);
            var errorDaoFindFailed = new UsersDao(errorContextFindFailed.UserManager);
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await errorDaoFindFailed.UpdateAsync(user));


            var errorContextUpdateFailed = MakeErrorContext(true);
            var errorDaoUpdateFailed = new UsersDao(errorContextUpdateFailed.UserManager);
            errorContextUpdateFailed.Users = context.Users;
            user.UserName = "UserName3";
            await Assert.ThrowsAsync<IdentityException>(async () =>
                await errorDaoUpdateFailed.UpdateAsync(user));
        }

        [Fact]
        public async Task Delete()
        {
            var users = new User[]
            {
                new User
                {
                    Id = 1,
                    UserName = "UserName1",
                },
                new User
                {
                    Id = 2,
                    UserName = "UserName2",
                }
            };
            var password = "password";
            var roleName = "operator";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            await dao.CreateAsync(users[0], password, roleName);
            await dao.CreateAsync(users[1], password, roleName);

            Assert.Equal(2, (await dao.GetAllAsync()).Count());

            await dao.DeleteAsync(users[0].Id);
            Assert.Single(await dao.GetAllAsync());

            var errorContextFindFailed = MakeErrorContext(false);
            var errorDaoFindFailed = new UsersDao(errorContextFindFailed.UserManager);
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await errorDaoFindFailed.DeleteAsync(users[0].Id));

            var errorContextDeleteFailed = MakeErrorContext(true);
            var errorDaoDeleteFailed = new UsersDao(errorContextDeleteFailed.UserManager);
            errorContextDeleteFailed.Users = context.Users;
            await Assert.ThrowsAsync<IdentityException>(async () =>
                await errorDaoDeleteFailed.DeleteAsync(users[0].Id));
        }

        [Fact]
        public async Task CheckPassword()
        {
            var user = new User
            {
                Id = 1,
            };
            var password = "password";
            var roleName = "operator";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            await dao.CreateAsync(user, password, roleName);

            Assert.True(await dao.CheckPasswordAsync(user.Id, password));
            Assert.False(await dao.CheckPasswordAsync(user.Id, password + "1"));
        }

        [Fact]
        public async Task ChangePassword()
        {
            var user = new User
            {
                Id = 1,
            };
            var password = "password";
            var newPassword = "newPassword";
            var roleName = "operator";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            await dao.CreateAsync(user, password, roleName);

            await dao.ChangePasswordAsync(user.Id, password, newPassword);
            Assert.Equal(newPassword, context.Users.First().PasswordHash);

            var errorContext = MakeErrorContext(true);
            var errorDao = new UsersDao(errorContext.UserManager);
            errorContext.Users = context.Users;
            await Assert.ThrowsAsync<IdentityException>(async () =>
                await errorDao.ChangePasswordAsync(user.Id, newPassword, "error"));
        }

        [Fact]
        public async Task ResetPassword()
        {
            var user = new User
            {
                Id = 1,
            };
            var password = "password";
            var newPassword = "newPassword";
            var roleName = "operator";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            await dao.CreateAsync(user, password, roleName);

            await dao.ResetPassword(user.Id, newPassword);
            Assert.Equal(newPassword, context.Users.First().PasswordHash);

            var errorContext = MakeErrorContext(true);
            var errorDao = new UsersDao(errorContext.UserManager);
            errorContext.Users = context.Users;
            await Assert.ThrowsAsync<IdentityException>(async () =>
                await errorDao.ChangePasswordAsync(user.Id, newPassword, "error"));
        }


        [Fact]
        public async Task GetUserRole()
        {
            var user = new User
            {
                Id = 1,
                UserName = "userName"
            };
            var password = "password";
            var roleName = "operator";

            var context = MakeContext();
            var dao = new UsersDao(context.UserManager);

            await dao.CreateAsync(user, password, roleName);

            Assert.Equal(roleName, await dao.GetUserRole(user.UserName));
        }
    }
}
