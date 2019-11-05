using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;

namespace PostCore.Core.Db.Dao
{
    public interface IUsersDao
    {
        Task InitialSetupAsync(
            string adminUserName,
            string adminEmail,
            string adminPassword);

        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByIdAsync(long id);
        Task<User> GetByUserNameAsync(string userName);
        Task CreateAsync(User user, string password, string roleName);
        Task UpdateAsync(User user);
        Task DeleteAsync(long id);
    }

    public class UsersDao : IUsersDao
    {
        private readonly UserManager<User> _userManager;

        public UsersDao(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task InitialSetupAsync(
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

            try
            {
                await CreateAsync(user, adminPassword, Role.Names.Admin);
            }
            catch (Exception e)
            {
                throw new InitialSetupException("Failed to create admin user", e);
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<User> GetByIdAsync(long id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public async Task<User> GetByUserNameAsync(string userName)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task CreateAsync(User user, string password, string roleName)
        {
            user.Id = 0;
            var r = await _userManager.CreateAsync(user, password);
            if (!r.Succeeded)
            {
                throw new IdentityException(r);
            }

            r = await _userManager.AddToRoleAsync(user, roleName);
            if (!r.Succeeded)
            {
                throw new IdentityException(r);
            }
        }

        public async Task UpdateAsync(User user)
        {
            var userMangerUser = await _userManager.FindByIdAsync(user.Id.ToString());
            if (user == null)
            {
                throw new ArgumentException("User with such id not found", nameof(User));
            }

            userMangerUser.UserName = user.UserName;
            userMangerUser.Email = user.Email;
            userMangerUser.FirstName = user.FirstName;
            userMangerUser.LastName = user.LastName;

            var r = await _userManager.UpdateAsync(userMangerUser);
            if (!r.Succeeded)
            {
                throw new IdentityException(r);
            }
        }

        public async Task DeleteAsync(long id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                throw new ArgumentException("User with such id not found", nameof(id));
            }

            var r = await _userManager.DeleteAsync(user);
            if (!r.Succeeded)
            {
                throw new IdentityException(r);
            }
        }
    }
}
