namespace Aist.Backend.Models;

public sealed class ProgressLog
{
    public Guid Id { get; set; }
    public Guid UserStoryId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public UserStory UserStory { get; set; } = null!;
}
