using Microsoft.EntityFrameworkCore;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Mail;

namespace PostCore.Core.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public virtual DbSet<Branch> Branch { get; set; }
        public virtual DbSet<Car> Car { get; set; }
        public virtual DbSet<Post> Post { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasOne(p => p.Branch)
                    .WithMany(b => b.Mail)
                    .HasForeignKey(p => p.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.SourceBranch)
                    .WithMany(b => b.SourceMail)
                    .HasForeignKey(p => p.SourceBranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(p => p.DestinationBranch)
                    .WithMany(b => b.DestinationMail)
                    .HasForeignKey(p => p.DestinationBranchId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(p => p.Car)
                    .WithMany(c => c.Mail)
                    .HasForeignKey(p => p.CarId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
