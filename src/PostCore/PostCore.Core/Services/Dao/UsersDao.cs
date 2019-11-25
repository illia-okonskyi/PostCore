using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.DbContext;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;
using PostCore.Utils;

namespace PostCore.Core.Services.Dao
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
            string filterRoleName = null,
            string sortKey = null,
            SortOrder sortOrder = SortOrder.Ascending);
        Task<User> GetByIdAsync(long id);
        Task<User> GetByIdWithRoleAsync(long id);
        Task<User> GetByUserNameAsync(string userName);
        Task<User> GetByUserNameWithRoleAsync(string userName);
        Task<User> GetByEmailAsync(string email);
        Task CreateAsync(User user, string password, string roleName);
        Task UpdateAsync(User user, long roleId = 0);
        Task DeleteAsync(long id);
        Task<bool> CheckPasswordAsync(long userId, string password);
        Task ChangePasswordAsync(long userId, string currentPassword, string newPassword);
        Task ResetPasswordAsync(long userId, string newPassword);
        Task<string> GetUserRole(string userName);
    }

    public class UsersDao : IUsersDao
    {
        public static List<string> AcceptableSortKeys { get; private set; } = new List<string>
        {
            nameof(User.Id),
            nameof(User.UserName),
            nameof(User.Email),
            nameof(User.FirstName),
            nameof(User.LastName),
            nameof(User.Role) + "." + nameof(User.Role.Name)
        };

        private readonly UserManager<User> _userManager;
        private readonly IdentityDbContext _dbContext;

        public UsersDao(
            UserManager<User> userManager,
            IdentityDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
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
            string filterRoleName = null,
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
            var users = _userManager.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();
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
            if (!string.IsNullOrEmpty(filterRoleName))
            {
                users = users.Where(u => u.UserRoles.First().Role.Name.Contains(filterRoleName));
            }

            // 3) Query
            var usersList = (await users.ToListAsync()).AsEnumerable();

            // 4) Sort
            usersList = usersList.Order(sortKey, sortOrder);

            return usersList;
        }

        public async Task<User> GetByIdAsync(long id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public async Task<User> GetByIdWithRoleAsync(long id)
        {
            return (await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable()
                .Where(u => u.Id == id)
                .ToListAsync())
                .SingleOrDefault();
        }

        public async Task<User> GetByUserNameAsync(string userName)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task<User> GetByUserNameWithRoleAsync(string userName)
        {
            return (await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable()
                .Where(u => u.UserName == userName)
                .ToListAsync())
                .SingleOrDefault();
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
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

        public async Task UpdateAsync(User user, long roleId = 0)
        {
            var dbUser = await _dbContext.Users
                .Include(u => u.UserRoles)
                .Where(u => u.Id == user.Id)
                .FirstOrDefaultAsync();
            if (dbUser == null)
            {
                throw new ArgumentException("User with such id not found", nameof(user));
            }

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                dbUser.UserName = user.UserName;
                dbUser.Email = user.Email;
                dbUser.FirstName = user.FirstName;
                dbUser.LastName = user.LastName;
                var userRole = dbUser.UserRoles.First();
                //_dbContext.UserRoles.Attach(userRole);
                if (roleId != 0 && userRole.RoleId != roleId)
                {
                    _dbContext.UserRoles.Remove(userRole);
                    await _dbContext.UserRoles.AddAsync(new UserRole
                    {
                        UserId = dbUser.Id,
                        RoleId = roleId
                    });
                }
                await _dbContext.SaveChangesAsync();

                try
                {
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
        }

        public async Task DeleteAsync(long id)
        {
            var user = await GetByIdAsync(id);
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

        public async Task<bool> CheckPasswordAsync(long userId, string password)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User with such id not found", nameof(userId));
            }
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task ChangePasswordAsync(long userId, string currentPassword, string newPassword)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User with such id not found", nameof(userId));
            }

            var r = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!r.Succeeded)
            {
                throw new IdentityException(r);
            }
        }

        public async Task ResetPasswordAsync(long userId, string newPassword)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User with such id not found", nameof(userId));
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var r = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!r.Succeeded)
            {
                throw new IdentityException(r);
            }
        }

        public async Task<string> GetUserRole(string userName)
        {
            var user = await GetByUserNameAsync(userName);
            if (user == null)
            {
                throw new ArgumentException("User with such user name not found", nameof(userName));
            }

            return (await _userManager.GetRolesAsync(user)).FirstOrDefault();
        }
    }
}
