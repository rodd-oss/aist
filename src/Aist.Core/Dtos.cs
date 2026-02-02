namespace Aist.Core;

public record ProjectResponse(Guid Id, string Title, DateTime CreatedAt);
public record CreateProjectRequest(string Title);

public record CreateJobRequest(
    Guid ProjectId, 
    string ShortSlug, 
    string Title, 
    JobType Type, 
    string Description);

public record JobResponse(
    Guid Id,
    Guid ProjectId,
    string ShortSlug,
    string Title,
    JobStatus Status,
    JobType Type,
    string Description,
    DateTime CreatedAt,
    IReadOnlyCollection<UserStorySummaryResponse>? UserStories);

public record UpdateJobStatusRequest(JobStatus Status);

public record UpdateJobRequest(
    string ShortSlug,
    string Title,
    JobType Type,
    string Description);

public record UserStorySummaryResponse(Guid Id, string Title, int Priority, bool IsComplete);

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
    IReadOnlyCollection<AcceptanceCriteriaResponse>? AcceptanceCriterias, 
    IReadOnlyCollection<ProgressLogResponse>? ProgressLogs);

public record CreateUserStoryRequest(
    Guid JobId, 
    string Title, 
    string Who, 
    string What, 
    string Why, 
    int Priority);

public record UpdateUserStoryCompleteRequest(bool IsComplete);

public record AcceptanceCriteriaResponse(Guid Id, Guid UserStoryId, string Description, bool IsMet);
public record CreateAcceptanceCriteriaRequest(Guid UserStoryId, string Description);
public record UpdateAcceptanceCriteriaRequest(bool IsMet);

public record ProgressLogResponse(Guid Id, Guid UserStoryId, string Text, DateTime CreatedAt);
public record CreateProgressLogRequest(Guid UserStoryId, string Text);
