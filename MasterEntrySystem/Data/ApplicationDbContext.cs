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
        public DbSet<TaskSubmission> TaskSubmissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure one-to-many relationship (keep assignments cascading when a worker is deleted)
            modelBuilder.Entity<TaskAssignment>()
                .HasOne(t => t.Worker)
                .WithMany()
                .HasForeignKey(t => t.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Avoid multiple-cascade-path cycles for worker submissions:
            // TaskAssignment -> Worker (cascade) + TaskSubmission -> Worker (cascade) + TaskSubmission -> TaskAssignment (cascade)
            // can introduce cycles/multiple cascade paths in SQL Server.
            modelBuilder.Entity<TaskSubmission>()
                .HasOne(s => s.Worker)
                .WithMany()
                .HasForeignKey(s => s.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskSubmission>()
                .HasOne(s => s.TaskAssignment)
                .WithMany()
                .HasForeignKey(s => s.TaskAssignmentId)
                .OnDelete(DeleteBehavior.NoAction);

            // Also set Worker FK to NoAction to eliminate SQL Server cascade-path detection.
            modelBuilder.Entity<TaskSubmission>()
                .HasOne(s => s.Worker)
                .WithMany()
                .HasForeignKey(s => s.WorkerId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
