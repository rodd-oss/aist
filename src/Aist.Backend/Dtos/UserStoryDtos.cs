namespace Aist.Backend.Dtos;

public record CreateUserStoryRequest(
    Guid JobId,
    string Title,
    string Who,
    string What,
    string Why,
    int Priority);

public record UserStoryResponse(
    Guid Id,
    Guid JobId,
    string Title,
    string Who,
    string What,
    string Why,
    int Priority,
    bool IsComplete,
    DateTime CreatedAt,
    List<AcceptanceCriteriaResponse>? AcceptanceCriterias,
    List<ProgressLogResponse>? ProgressLogs);

public record UserStorySummaryResponse(Guid Id, string Title, int Priority, bool IsComplete);
public record UserStoryCompleteRequest(bool IsComplete);
