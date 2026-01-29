namespace Aist.Backend.Dtos;

public record CreateProgressLogRequest(Guid UserStoryId, string Text);
public record ProgressLogResponse(Guid Id, Guid UserStoryId, string Text, DateTime CreatedAt);
