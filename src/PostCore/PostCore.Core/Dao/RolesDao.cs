using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;

namespace PostCore.Core.Db.Dao
{
    public interface IRolesDao
    {
        Task InitialSetup();
    }

    public class RolesDao : IRolesDao
    {
        private readonly DbContext.IdentityDbContext _dbContext;
        private readonly RoleManager<Role> _roleManager;

        public RolesDao(
            DbContext.IdentityDbContext dbContext,
            RoleManager<Role> roleManager)
        {
            _dbContext = dbContext;
            _roleManager = roleManager;
        }

        public async Task InitialSetup()
        {
            foreach (var roleName in Role.Names.All)
            {
                var result = await _roleManager.CreateAsync(new Role(roleName));
                if (!result.Succeeded)
                {
                    throw InitialSetupException.FromIdentityResult(result);
                }
            }
        }
    }
}
