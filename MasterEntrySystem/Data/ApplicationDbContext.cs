using MasterEntrySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace MasterEntrySystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure one-to-many relationship
            modelBuilder.Entity<TaskAssignment>()
                .HasOne(t => t.Worker)
                .WithMany()
                .HasForeignKey(t => t.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
