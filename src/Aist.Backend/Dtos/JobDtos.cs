using Aist.Backend.Models;

namespace Aist.Backend.Dtos;

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
    List<UserStorySummaryResponse>? UserStories);

public record JobStatusUpdateRequest(JobStatus Status);
