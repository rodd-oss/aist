namespace Aist.Backend.Models;

public class UserStory
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Who { get; set; } = string.Empty;
    public string What { get; set; } = string.Empty;
    public string Why { get; set; } = string.Empty;
    public int Priority { get; set; } // Lower is higher, no ceiling
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public Job Job { get; set; } = null!;
    public List<AcceptanceCriteria> AcceptanceCriterias { get; set; } = [];
    public List<ProgressLog> ProgressLogs { get; set; } = [];
}
