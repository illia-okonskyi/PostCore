using Microsoft.EntityFrameworkCore;
using PostCore.Core.Activities;
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
        public virtual DbSet<Activity> Activity { get; set; }

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

            modelBuilder.Entity<Activity>(entity =>
            {
                entity.HasOne(a => a.Post)
                    .WithMany()
                    .HasForeignKey(a => a.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Branch)
                    .WithMany()
                    .HasForeignKey(a => a.BranchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Car)
                    .WithMany()
                    .HasForeignKey(a => a.CarId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
