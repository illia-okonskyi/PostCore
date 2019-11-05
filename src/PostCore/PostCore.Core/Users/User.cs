using Microsoft.AspNetCore.Identity;

namespace PostCore.Core.Users
{
    public class User : IdentityUser<long>
    {
        public const string UserNameRegex = "^[a-zA-Z0-9-_.]+$";
        public const string UserNameAllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.";
        public const string EmailRegex = "^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
