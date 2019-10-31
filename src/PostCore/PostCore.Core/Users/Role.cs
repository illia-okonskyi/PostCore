using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PostCore.Core.Exceptions;

namespace PostCore.Core.Users
{
    public class Role : IdentityRole<long>
    {
        public Role()
            : base()
        { }

        public Role(string roleName)
            : base(roleName)
        { }

        public static class Names
        {
            public const string Admin = "Admin";
            public const string Operator = "Operator";

            public static IList<string> All => new List<string> { Admin, Operator };
        }

        public static async Task InitialSetup(RoleManager<Role> roleManager)
        {
            foreach (var roleName in Names.All)
            {
                var result = await roleManager.CreateAsync(new Role(roleName));
                if (!result.Succeeded)
                {
                    throw InitialSetupException.FromIdentityResult(result);
                }
            }
        }
    }
}
