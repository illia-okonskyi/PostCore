using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;

namespace PostCore.Core.Db.Dao
{
    public interface IUsersDao
    {
        Task InitialSetup(
            string adminUserName,
            string adminEmail,
            string adminPassword);
    }

    public class UsersDao : IUsersDao
    {
        private readonly DbContext.IdentityDbContext _dbContext;
        private readonly UserManager<User> _userManager;

        public UsersDao(
            DbContext.IdentityDbContext dbContext,
            UserManager<User> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task InitialSetup(
            string adminUserName,
            string adminEmail,
            string adminPassword)
        {
            const string admin = "Admin";
            var user = new User
            {
                UserName = adminUserName,
                Email = adminEmail,
                FirstName = admin,
                LastName = admin,
            };

            var result = await _userManager.CreateAsync(user, adminPassword);
            if (!result.Succeeded)
            {
                throw InitialSetupException.FromIdentityResult(result);
            }

            result = await _userManager.AddToRoleAsync(user, Role.Names.Admin);
            if (!result.Succeeded)
            {
                throw InitialSetupException.FromIdentityResult(result);
            }
        }
    }
}
