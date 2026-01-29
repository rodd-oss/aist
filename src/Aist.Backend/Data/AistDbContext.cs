using Aist.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Aist.Backend.Data;

public class AistDbContext : DbContext
{
    public AistDbContext(DbContextOptions<AistDbContext> options) : base(options) { }
    
    public DbSet<Project> Projects { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<UserStory> UserStories { get; set; }
    public DbSet<AcceptanceCriteria> AcceptanceCriterias { get; set; }
    public DbSet<ProgressLog> ProgressLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global query filter for soft deletes
        modelBuilder.Entity<Project>().HasQueryFilter(p => p.DeletedAt == null);
        modelBuilder.Entity<Job>().HasQueryFilter(j => j.DeletedAt == null);
        
        // Indexes for better query performance
        modelBuilder.Entity<Job>()
            .HasIndex(j => new { j.ProjectId, j.Status });
        
        modelBuilder.Entity<UserStory>()
            .HasIndex(us => new { us.JobId, us.Priority });
        
        modelBuilder.Entity<AcceptanceCriteria>()
            .HasIndex(ac => ac.UserStoryId);
        
        modelBuilder.Entity<ProgressLog>()
            .HasIndex(pl => pl.UserStoryId);
    }
}
