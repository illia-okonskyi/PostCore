using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

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

        public static class Authorize
        {
            public const string Operator = Names.Admin + "," + Names.Operator;
        }
    }
}
