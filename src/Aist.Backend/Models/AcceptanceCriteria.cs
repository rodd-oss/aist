namespace Aist.Backend.Models;

public sealed class AcceptanceCriteria
{
    public Guid Id { get; set; }
    public Guid UserStoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsMet { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public UserStory UserStory { get; set; } = null!;
}
