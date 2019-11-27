using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PostCore.Core.DbContext;
using PostCore.Core.Services;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;

namespace PostCore.MainApp
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        ILogger _logger;

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _logger = loggerFactory.CreateLogger<Startup>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddDbContext<IdentityDbContext>(options =>
            {
                var connectionString = Configuration["Config:ConnectionStrings:Identity"];
                options.UseSqlServer(connectionString, builder =>
                    builder.MigrationsAssembly(typeof(Startup).Assembly.FullName)
                );
            });
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = Configuration["Config:ConnectionStrings:Application"];
                options.UseSqlServer(connectionString, builder =>
                    builder.MigrationsAssembly(typeof(Startup).Assembly.FullName)
                );
            });

            services
                .AddIdentity<User, Role>(options => {
                    options.User.RequireUniqueEmail = true;
                    options.User.AllowedUserNameCharacters = User.UserNameAllowedChars;
                    options.Password.RequiredLength = 1;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireDigit = false;
                })
                .AddEntityFrameworkStores<IdentityDbContext>()
                .AddDefaultTokenProviders();

            // NOTE: When not authorized user tries to access restricted actions the automatic
            //       redirect result is generated. Default redirection link is
            //       /Account/Login?ReturnUrl=<URL> and it can be changed via 
            //       IServiceCollection.ConfigureApplicationCookie extenstion method
            services.ConfigureApplicationCookie(options => {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IRolesDao, RolesDao>();
            services.AddScoped<IUsersDao, UsersDao>();
            services.AddScoped<IBranchesDao, BranchesDao>();
            services.AddScoped<ICarsDao, CarsDao>();
            services.AddScoped<IActivitiesDao, ActivitiesDao>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.PopulateDependencyViews();
            services.AddMvc()
                .PopulateDependencyObjects();
            services.AddMemoryCache();
            services.AddSession();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseStatusCodePages();
            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();

            try
            {
                new InitialSetup(app, Configuration).Run().Wait();
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Exception during initial setup: {e.ToString()}");
            }
        }
    }
}
