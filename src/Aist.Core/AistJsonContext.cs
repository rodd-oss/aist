using System.Text.Json.Serialization;

namespace Aist.Core;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(List<ProjectResponse>))]
[JsonSerializable(typeof(ProjectResponse))]
[JsonSerializable(typeof(CreateProjectRequest))]
[JsonSerializable(typeof(CreateJobRequest))]
[JsonSerializable(typeof(List<JobResponse>))]
[JsonSerializable(typeof(JobResponse))]
[JsonSerializable(typeof(List<UserStorySummaryResponse>))]
[JsonSerializable(typeof(UserStorySummaryResponse))]
[JsonSerializable(typeof(UpdateJobStatusRequest))]
[JsonSerializable(typeof(UpdateJobRequest))]
[JsonSerializable(typeof(List<UserStoryResponse>))]
[JsonSerializable(typeof(UserStoryResponse))]
[JsonSerializable(typeof(CreateUserStoryRequest))]
[JsonSerializable(typeof(UpdateUserStoryCompleteRequest))]
[JsonSerializable(typeof(List<AcceptanceCriteriaResponse>))]
[JsonSerializable(typeof(AcceptanceCriteriaResponse))]
[JsonSerializable(typeof(CreateAcceptanceCriteriaRequest))]
[JsonSerializable(typeof(UpdateAcceptanceCriteriaRequest))]
[JsonSerializable(typeof(List<ProgressLogResponse>))]
[JsonSerializable(typeof(ProgressLogResponse))]
[JsonSerializable(typeof(CreateProgressLogRequest))]
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(IReadOnlyCollection<GitHubAsset>))]
public partial class AistJsonContext : JsonSerializerContext
{
}
