using Microsoft.EntityFrameworkCore;
using PostCore.Core.Branches;

namespace PostCore.Core.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public virtual DbSet<Branch> Branch { get; set; }
    }
}
