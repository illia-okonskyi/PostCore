using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PostCore.Core.Dao;

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
                var rolesDao = scope.ServiceProvider.GetRequiredService<IRolesDao>();
                var usersDao = scope.ServiceProvider.GetRequiredService<IUsersDao>();
                await rolesDao.InitialSetupAsync();
                await usersDao.InitialSetupAsync(
                    _configuration["Config:Admin:UserName"],
                    _configuration["Config:Admin:Email"],
                    _configuration["Config:Admin:Password"]
                    );
            }
        }
    }
}
