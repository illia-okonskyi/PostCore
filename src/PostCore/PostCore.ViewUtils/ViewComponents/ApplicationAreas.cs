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
        public string Action { get; set; } = "Index";
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
                DisplayName = "Admin - Manage users",
                Controller = "Users",
                ForRoles = new List<string>
                {
                    Role.Names.Admin
                }
            },
            new AreaInfo
            {
                DisplayName = "Admin - Manage branches",
                Controller = "Branches",
                ForRoles = new List<string>
                {
                    Role.Names.Admin
                }
            },
            new AreaInfo
            {
                DisplayName = "Admin - Manage cars",
                Controller = "Cars",
                ForRoles = new List<string>
                {
                    Role.Names.Admin
                }
            },
            new AreaInfo
            {
                DisplayName = "Manager - Activities",
                Controller = "Manager",
                ForRoles = new List<string>
                {
                    Role.Names.Admin, Role.Names.Manager
                }
            },
            new AreaInfo
            {
                DisplayName = "Operator - Create post",
                Controller = "Operator",
                Action = "CreatePost",
                ForRoles = new List<string>
                {
                    Role.Names.Admin, Role.Names.Operator
                }
            },
            new AreaInfo
            {
                DisplayName = "Operator - Deliver post",
                Controller = "Operator",
                Action = "DeliverPost",
                ForRoles = new List<string>
                {
                    Role.Names.Admin, Role.Names.Operator
                }
            },
            new AreaInfo
            {
                DisplayName = "Stockman - Stock",
                Controller = "Stockman",
                ForRoles = new List<string>
                {
                    Role.Names.Admin, Role.Names.Stockman
                }
            },
            new AreaInfo
            {
                DisplayName = "Driver - Stock mail",
                Controller = "Driver",
                Action = "StockMail",
                ForRoles = new List<string>
                {
                    Role.Names.Admin, Role.Names.Driver
                }
            },
            new AreaInfo
            {
                DisplayName = "Driver - Car mail",
                Controller = "Driver",
                Action = "CarMail",
                ForRoles = new List<string>
                {
                    Role.Names.Admin, Role.Names.Driver
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
