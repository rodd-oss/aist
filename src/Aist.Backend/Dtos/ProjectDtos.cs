namespace Aist.Backend.Dtos;

public record CreateProjectRequest(string Title);
public record ProjectResponse(Guid Id, string Title, DateTime CreatedAt);
