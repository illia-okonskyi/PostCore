using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core.Services;
using PostCore.Core.Users;

namespace PostCore.ViewUtils.ViewComponents
{
    public class AreaInfo
    {
        public string DisplayName { get; set; }
        public string Controller { get; set; }
        public List<string> ForRoles { get; set; }
    }

    public class ApplicationAreasViewModel
    {
        public List<AreaInfo> AreasForUser { get; set; } = new List<AreaInfo>();
    }

    public class ApplicationAreas : ViewComponent
    {
        private static readonly List<AreaInfo> AllAreas = new List<AreaInfo>
        {
            new AreaInfo
            {
                DisplayName = "Manage users",
                Controller = "Users",
                ForRoles = new List<string>
                {
                    Role.Names.Admin
                }
            },
            new AreaInfo
            {
                DisplayName = "Manage branches",
                Controller = "Branches",
                ForRoles = new List<string>
                {
                    Role.Names.Admin
                }
            },
            new AreaInfo
            {
                DisplayName = "Manage cars",
                Controller = "Cars",
                ForRoles = new List<string>
                {
                    Role.Names.Admin
                }
            }
        };

        private readonly ICurrentUserService _currentUserService;
        public ApplicationAreas(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var isLoggedIn = User.Identity.IsAuthenticated;
            if (!isLoggedIn)
            {
                return View(new ApplicationAreasViewModel());
            }

            var role = await _currentUserService.GetRoleAsync();
            return View(new ApplicationAreasViewModel
            {
                AreasForUser = AllAreas.Where(ai => ai.ForRoles.Contains(role.Name)).ToList()
            });
        }
    }
}
