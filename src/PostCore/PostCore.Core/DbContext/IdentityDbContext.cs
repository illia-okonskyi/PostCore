using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.Users;

namespace PostCore.Core.DbContext
{
    public class IdentityDbContext : IdentityDbContext<User, Role, long>
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
            : base(options)
        {}
    }
}
