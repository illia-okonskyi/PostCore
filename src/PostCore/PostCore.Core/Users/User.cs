using Microsoft.AspNetCore.Identity;

namespace PostCore.Core.Users
{
    public class User : IdentityUser<long>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
