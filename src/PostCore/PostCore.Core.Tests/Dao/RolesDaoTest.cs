using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using MockQueryable.Moq;
using PostCore.Core.Users;
using PostCore.Core.Db.Dao;
using PostCore.Core.Exceptions;

namespace PostCore.Core.Tests.Dao
{
    public class RolesDaoTest
    {
        class Context
        {
            public List<Role> Roles { get; set; } = new List<Role>();
            public RoleManager<Role> RoleManager { get; set; }
        }

        static Mock<RoleManager<Role>> MakeRoleManagerMock()
        {
            return new Mock<RoleManager<Role>>(
                new Mock<IRoleStore<Role>>().Object,
                new IRoleValidator<Role>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<ILogger<RoleManager<Role>>>().Object
                );
        }

        Context MakeContext()
        {
            var context = new Context();

            var mock = MakeRoleManagerMock();

            var successResult = Task.FromResult(IdentityResult.Success);

            mock.Setup(m => m.Roles)
                .Returns(() =>
                {
                    var m = context.Roles.AsQueryable().BuildMock();
                    return m.Object;
                });
            mock.Setup(m => m.CreateAsync(It.IsAny<Role>()))
                .Callback((Role role) => context.Roles.Add(role))
                .Returns(successResult);

            context.RoleManager = mock.Object;
            return context;
        }

        Context MakeErrorContext()
        {
            var context = new Context();

            var mock = new Mock<RoleManager<Role>>(
                    new Mock<IRoleStore<Role>>().Object,
                    new IRoleValidator<Role>[0],
                    new Mock<ILookupNormalizer>().Object,
                    new Mock<IdentityErrorDescriber>().Object,
                    new Mock<ILogger<RoleManager<Role>>>().Object
                    );


            var failedResult = Task.FromResult(
                IdentityResult.Failed(
                    new IdentityError()
                    {
                        Code = "ErrorCode",
                        Description = "ErrorDescription"
                    })
                );

            mock.Setup(m => m.Roles)
                .Returns(() =>
                {
                    var m = context.Roles.AsQueryable().BuildMock();
                    return m.Object;
                });
            mock.Setup(m => m.CreateAsync(It.IsAny<Role>()))
                .Returns(failedResult);

            context.RoleManager = mock.Object;
            return context;
        }

        [Fact]
        async Task Create()
        {
            var roleName = "roleName";

            var context = MakeContext();
            var dao = new RolesDao(context.RoleManager);

            await dao.CreateAsync(roleName);

            Assert.Single(context.Roles);
            Assert.Equal(roleName, context.Roles[0].Name);

            var errorContext = MakeErrorContext();
            var errorDao = new RolesDao(errorContext.RoleManager);

            await Assert.ThrowsAsync<IdentityException>(async () =>
                await errorDao.CreateAsync(roleName));
        }

        [Fact]
        async Task InitialSetupAsync()
        {
            var roles = new Role[]
            {
                new Role(Role.Names.Admin),
                new Role(Role.Names.Operator)
            }.OrderBy(r => r.Name).ToList();

            var context = MakeContext();
            var dao = new RolesDao(context.RoleManager);

            await dao.InitialSetupAsync();
            context.Roles.OrderBy(r => r.Name);

            Assert.Equal(roles.Count(), context.Roles.Count());
            for (int i = 0; i < roles.Count(); ++i)
            {
                Assert.Equal(roles[i].Name, context.Roles[i].Name);
            }

            var errorContext = MakeErrorContext();
            var errorDao = new RolesDao(errorContext.RoleManager);

            await Assert.ThrowsAsync<InitialSetupException>(async () =>
                await errorDao.InitialSetupAsync());
        }
    }
}
