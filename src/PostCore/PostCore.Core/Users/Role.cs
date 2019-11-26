using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace PostCore.Core.Users
{
    public class Role : IdentityRole<long>
    {
        private static readonly List<string> HasBranchRoleNames = new List<string>
        {
            Names.Admin, Names.Operator, Names.Stockman, Names.Courier
        };
        private static readonly List<string> HasCarRoleNames = new List<string>
        {
            Names.Admin, Names.Driver, Names.Courier
        };

        public Role()
            : base()
        { }

        public Role(string roleName)
            : base(roleName)
        { }

        public ICollection<UserRole> UserRoles { get; set; }

        public bool IsAdmin => Name == Names.Admin;
        public bool HasBranch => HasBranchRoleNames.Contains(Name);
        public bool HasCar => HasCarRoleNames.Contains(Name);

        public static class Names
        {
            public const string Admin = "Admin";
            public const string Operator = "Operator";
            public const string Manager = "Manager";
            public const string Stockman = "Stockman";
            public const string Driver = "Driver";
            public const string Courier = "Courier";

            public static IList<string> All => new List<string>
            {
                Admin,
                Operator,
                Manager,
                Stockman,
                Driver,
                Courier
            };
        }

        public static class Authorize
        {
            public const string Admin = Names.Admin;
            public const string Operator = Names.Admin + "," + Names.Operator;
            public const string Manager = Names.Admin + "," + Names.Manager;
            public const string Stockman = Names.Admin + "," + Names.Stockman;
            public const string Driver = Names.Admin + "," + Names.Driver;
            public const string Courier = Names.Admin + "," + Names.Courier;
        }
    }
}
