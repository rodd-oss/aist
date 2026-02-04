namespace Aist.Backend.Models;

public sealed class Project
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; } // Soft delete
    
    // Navigation
    public ICollection<Job> Jobs { get; } = new List<Job>();
}
