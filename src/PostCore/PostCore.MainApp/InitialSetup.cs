using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PostCore.Core.Users;

namespace PostCore.MainApp
{
    public class InitialSetup
    {
        private readonly IApplicationBuilder _app;
        private readonly IConfiguration _configuration;

        public InitialSetup(IApplicationBuilder app, IConfiguration configuration)
        {
            _app = app;
            _configuration = configuration;
        }

        public async Task Run()
        {
            var initialSetup = _configuration.GetValue<bool>("Config:InitialSetup");
            if (!initialSetup)
            {
                return;
            }

            using (var scope = _app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                await Role.InitialSetup(roleManager);
                await User.InitialSetupAdminUser(
                    _configuration["Config:Admin:UserName"],
                    _configuration["Config:Admin:Email"],
                    _configuration["Config:Admin:Password"],
                    userManager,
                    roleManager);
            }
        }
    }
}
