﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;
using MockQueryable.Moq;
using PostCore.Core.Db.Dao;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;

namespace PostCore.Core.Tests
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
            mock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .Returns(failedResult);
            mock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .Returns(failedResult);
            mock.Setup(m => m.UpdateAsync(It.IsAny<User>()))
                .Returns(failedResult);
            mock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
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

            var allUsers = await dao.GetAllAsync();
            Assert.Equal(2, allUsers.Count());

            var userById = await dao.GetByIdAsync(users[0].Id);
            Assert.Equal(users[0].Id, userById.Id);
            Assert.Equal(users[0].UserName, userById.UserName);

            var userByUserName = await dao.GetByUserNameAsync(users[1].UserName);
            Assert.Equal(users[1].Id, userByUserName.Id);
            Assert.Equal(users[1].UserName, userByUserName.UserName);

            var errorContext = MakeErrorContext(false);
            var errorDao = new UsersDao(errorContext.UserManager);
            Assert.Null(await errorDao.GetByIdAsync(users[0].Id));
            Assert.Null(await errorDao.GetByUserNameAsync(users[1].UserName));
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
    }
}
