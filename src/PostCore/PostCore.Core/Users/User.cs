using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PostCore.Core.Exceptions;

namespace PostCore.Core.Users
{
    public class User : IdentityUser<long>
    {
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string MiddleName { get; set; }

        public static async Task InitialSetupAdminUser(
            string userName,
            string email,
            string password,
            UserManager<User> userManager,
            RoleManager<Role> roleManager)
        {
            const string admin = "Admin";
            var user = new User
            {
                UserName = userName,
                Email = email,
                FirstName = admin,
                SecondName = admin,
                MiddleName = admin
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw InitialSetupException.FromIdentityResult(result);
            }

            result = await userManager.AddToRoleAsync(user, Role.Names.Admin);
            if (!result.Succeeded)
            {
                throw InitialSetupException.FromIdentityResult(result);
            }
        }

        public static async Task<bool> Login(
            string email,
            string password,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            await signInManager.SignOutAsync();
            var result = await signInManager.PasswordSignInAsync(user, password, false, false);

            return result.Succeeded;
        }

        public static async Task Logout(SignInManager<User> signInManager)
        {
            await signInManager.SignOutAsync();
        }
    }
}
