using System.Text.Json.Serialization;

namespace Aist.Shared;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<ProjectResponse>))]
[JsonSerializable(typeof(ProjectResponse))]
[JsonSerializable(typeof(CreateProjectRequest))]
[JsonSerializable(typeof(CreateJobRequest))]
[JsonSerializable(typeof(List<JobResponse>))]
[JsonSerializable(typeof(JobResponse))]
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
public partial class AistJsonContext : JsonSerializerContext
{
}
