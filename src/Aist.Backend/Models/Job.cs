using Aist.Shared;

namespace Aist.Backend.Models;

public class Job
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ShortSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Todo;
    public JobType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; } // Soft delete
    
    // Navigation
    public Project Project { get; set; } = null!;
    public List<UserStory> UserStories { get; set; } = [];
}
