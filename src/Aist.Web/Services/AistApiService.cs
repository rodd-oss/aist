using System.Net.Http.Json;
using System.Diagnostics.CodeAnalysis;
using Aist.Core;
using Microsoft.Extensions.Logging;

namespace Aist.Web.Services;

[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via DI")]
internal sealed partial class AistApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<AistApiService> _logger;

    public AistApiService(HttpClient http, ILogger<AistApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    private async Task<T?> SafeGetAsync<T>(string url, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        LogFetching(_logger, url);
        return await _http.GetFromJsonAsync(url, typeInfo).ConfigureAwait(false);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string url)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            LogApiError(_logger, url, (int)response.StatusCode, content);
        }
        response.EnsureSuccessStatusCode();
    }

    // Projects
    public async Task<List<ProjectResponse>> GetProjectsAsync()
    {
        return await SafeGetAsync("api/v1/Projects", AistJsonContext.Default.ListProjectResponse).ConfigureAwait(false) ?? new();
    }

    public async Task<ProjectResponse?> GetProjectAsync(Guid id)
    {
        var url = FormattableString.Invariant($"api/v1/Projects/{id}");
        return await SafeGetAsync(url, AistJsonContext.Default.ProjectResponse).ConfigureAwait(false);
    }

    public async Task CreateProjectAsync(CreateProjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        const string url = "api/v1/Projects";
        LogCreatingProject(_logger, request.Title);
        var response = await _http.PostAsJsonAsync(new Uri(url, UriKind.Relative), request, AistJsonContext.Default.CreateProjectRequest).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url).ConfigureAwait(false);
    }

    public async Task DeleteProjectAsync(Guid id)
    {
        var url = FormattableString.Invariant($"api/v1/Projects/{id}");
        var response = await _http.DeleteAsync(new Uri(url, UriKind.Relative)).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url).ConfigureAwait(false);
    }

    // Jobs
    public async Task<List<JobResponse>> GetJobsAsync()
    {
        return await SafeGetAsync("api/v1/Jobs", AistJsonContext.Default.ListJobResponse).ConfigureAwait(false) ?? new();
    }

    public async Task<List<JobResponse>> GetJobsByProjectAsync(Guid projectId)
    {
        var url = FormattableString.Invariant($"api/v1/Jobs?projectId={projectId}");
        return await SafeGetAsync(url, AistJsonContext.Default.ListJobResponse).ConfigureAwait(false) ?? new();
    }

    public async Task<JobResponse?> GetJobAsync(Guid id)
    {
        var url = FormattableString.Invariant($"api/v1/Jobs/{id}");
        return await SafeGetAsync(url, AistJsonContext.Default.JobResponse).ConfigureAwait(false);
    }

    public async Task CreateJobAsync(CreateJobRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        const string url = "api/v1/Jobs";
        var response = await _http.PostAsJsonAsync(new Uri(url, UriKind.Relative), request, AistJsonContext.Default.CreateJobRequest).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url).ConfigureAwait(false);
    }

    public async Task UpdateJobStatusAsync(Guid id, JobStatus status)
    {
        var url = FormattableString.Invariant($"api/v1/Jobs/{id}/status");
        var response = await _http.PatchAsJsonAsync(new Uri(url, UriKind.Relative), new UpdateJobStatusRequest(status), AistJsonContext.Default.UpdateJobStatusRequest).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url).ConfigureAwait(false);
    }

    // User Stories
    public async Task<List<UserStoryResponse>> GetStoriesByJobAsync(Guid jobId)
    {
        var url = FormattableString.Invariant($"api/v1/UserStories/by-job/{jobId}");
        return await SafeGetAsync(url, AistJsonContext.Default.ListUserStoryResponse).ConfigureAwait(false) ?? new();
    }

    public async Task CreateStoryAsync(CreateUserStoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        const string url = "api/v1/UserStories";
        var response = await _http.PostAsJsonAsync(new Uri(url, UriKind.Relative), request, AistJsonContext.Default.CreateUserStoryRequest).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url).ConfigureAwait(false);
    }

    public async Task UpdateStoryCompleteAsync(Guid id, bool isComplete)
    {
        var url = FormattableString.Invariant($"api/v1/UserStories/{id}/complete");
        var response = await _http.PatchAsJsonAsync(new Uri(url, UriKind.Relative), new UpdateUserStoryCompleteRequest(isComplete), AistJsonContext.Default.UpdateUserStoryCompleteRequest).ConfigureAwait(false);
        await EnsureSuccessAsync(response, url).ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "API Error {Url} ({StatusCode}): {Content}")]
    static partial void LogApiError(ILogger logger, string url, int statusCode, string content);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Fetching {Url}")]
    static partial void LogFetching(ILogger logger, string url);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Creating project: {Title}")]
    static partial void LogCreatingProject(ILogger logger, string title);
}
