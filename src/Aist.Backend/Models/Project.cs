namespace Aist.Backend.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; } // Soft delete
    
    // Navigation
    public List<Job> Jobs { get; set; } = new();
}
