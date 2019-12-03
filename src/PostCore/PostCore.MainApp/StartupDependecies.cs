using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace PostCore.MainApp
{
    static class StartupDependecies
    {
        static readonly IList<Assembly> _assemblies = new List<Assembly>()
        {
            Assembly.GetAssembly(typeof(ViewUtils.ViewComponents.Account))
        };

        public static void PopulateDependencyViews(this IServiceCollection services)
        {
            services.Configure<RazorViewEngineOptions>(options =>
            {
                foreach (var a in _assemblies)
                {
                    options.FileProviders.Add(new EmbeddedFileProvider(a));
                }
            });
        }

        public static IMvcBuilder PopulateDependencyObjects(this IMvcBuilder mvcBuilder)
        {
            mvcBuilder
                .ConfigureApplicationPartManager(apm =>
                {
                    foreach (var a in _assemblies)
                    {
                        apm.ApplicationParts.Add(new AssemblyPart(a));
                    }
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            return mvcBuilder;
        }
    }
}
