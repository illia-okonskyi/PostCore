using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;
using PostCore.Utils;

namespace PostCore.Core.Db.Dao
{
    public interface IUsersDao
    {
        Task InitialSetupAsync(
            string adminUserName,
            string adminEmail,
            string adminPassword);

        Task<IEnumerable<User>> GetAllAsync(
            string filterUserName = null,
            string filterEmail = null,
            string filterFirstName = null,
            string filterLastName = null,
            string sortKey = null,
            SortOrder sortOrder = SortOrder.Ascending);
        Task<User> GetByIdAsync(long id);
        Task<User> GetByUserNameAsync(string userName);
        Task CreateAsync(User user, string password, string roleName);
        Task UpdateAsync(User user);
        Task DeleteAsync(long id);
    }

    public class UsersDao : IUsersDao
    {
        public static List<string> AcceptableSortKeys { get; private set; } = new List<string>
        {
            nameof(User.Id),
            nameof(User.UserName),
            nameof(User.Email),
            nameof(User.FirstName),
            nameof(User.LastName)
        };


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

        public async Task<IEnumerable<User>> GetAllAsync(
            string filterUserName = null,
            string filterEmail = null,
            string filterFirstName = null,
            string filterLastName = null,
            string sortKey = null,
            SortOrder sortOrder = SortOrder.Ascending)
        {
            // 1) Check sortKey
            if (string.IsNullOrEmpty(sortKey))
            {
                sortKey = AcceptableSortKeys.First();
            }
            if (!AcceptableSortKeys.Contains(sortKey))
            {
                throw new ArgumentException("Must be one of AcceptableSortKeys", nameof(sortKey));
            }

            // 2) Filter
            var users = _userManager.Users;
            if (!string.IsNullOrEmpty(filterUserName))
            {
                users = users.Where(u => u.UserName.Contains(filterUserName));
            }
            if (!string.IsNullOrEmpty(filterEmail))
            {
                users = users.Where(u => u.Email.Contains(filterEmail));
            }
            if (!string.IsNullOrEmpty(filterFirstName))
            {
                users = users.Where(u => u.FirstName.Contains(filterFirstName));
            }
            if (!string.IsNullOrEmpty(filterLastName))
            {
                users = users.Where(u => u.LastName.Contains(filterLastName));
            }

            // 3) Sort
            users = users.Order(sortKey, sortOrder);

            return await users.ToListAsync();
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
            if (userMangerUser == null)
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
