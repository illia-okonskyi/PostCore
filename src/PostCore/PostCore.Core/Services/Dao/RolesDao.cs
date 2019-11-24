using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.Exceptions;
using PostCore.Core.Users;

namespace PostCore.Core.Services.Dao
{
    public interface IRolesDao
    {
        Task InitialSetupAsync();

        Task<IEnumerable<Role>> GetAllAsync(bool includeAdmin = true);
        Task CreateAsync(string roleName);
        Task<Role> GetByIdAsync(long id);
    }

    public class RolesDao : IRolesDao
    {
        private readonly RoleManager<Role> _roleManager;

        public RolesDao(RoleManager<Role> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task InitialSetupAsync()
        {
            foreach (var roleName in Role.Names.All)
            {
                try
                {
                    await CreateAsync(roleName);
                }
                catch (Exception e)
                {
                    throw new InitialSetupException($"Failed to create role {roleName}", e);
                }
            }
        }

        public async Task<IEnumerable<Role>> GetAllAsync(bool includeAdmin = true)
        {
            var roles = _roleManager.Roles;
            if (!includeAdmin)
            {
                roles = roles.Where(r => r.Name != Role.Names.Admin);
            }
            return await roles.ToListAsync();
        }

        public async Task CreateAsync(string roleName)
        {
            var r = await _roleManager.CreateAsync(new Role(roleName));
            if (!r.Succeeded)
            {
                throw new IdentityException(r);
            }
        }

        public async Task<Role> GetByIdAsync(long id)
        {
            return await _roleManager.FindByIdAsync(id.ToString());
        }
    }
}
