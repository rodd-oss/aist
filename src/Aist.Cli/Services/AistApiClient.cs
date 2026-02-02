using System.Net.Http.Json;
using System.Text.Json;
using Aist.Core;

namespace Aist.Cli.Services;

internal sealed class AistApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private static readonly string BaseUrl = GetBaseUrl();

    private static string GetBaseUrl()
    {
        var url = Environment.GetEnvironmentVariable("AIST_API_URL")
            ?? "http://localhost:5192/api/v1/";
        // Ensure trailing slash for proper URI joining
        if (!url.EndsWith('/'))
        {
            url += "/";
        }
        return url;
    }

    public AistApiClient()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    // Projects
    public async Task<List<ProjectResponse>?> GetProjectsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync("projects", AistJsonContext.Default.ListProjectResponse).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error fetching projects: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<ProjectResponse?> CreateProjectAsync(string title)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("projects", new CreateProjectRequest(title), AistJsonContext.Default.CreateProjectRequest).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(AistJsonContext.Default.ProjectResponse).ConfigureAwait(false);
            }
            await Console.Error.WriteLineAsync($"Error creating project: {response.StatusCode}").ConfigureAwait(false);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error creating project: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<bool> DeleteProjectAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(new Uri($"projects/{id}", UriKind.Relative)).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            await Console.Error.WriteLineAsync($"Error deleting project: {ex.Message}").ConfigureAwait(false);
            return false;
        }
    }

    // Jobs
    public async Task<List<JobResponse>?> GetJobsAsync(string? projectId = null)
    {
        try
        {
            var url = projectId != null ? $"jobs?projectId={projectId}" : "jobs";
            return await _httpClient.GetFromJsonAsync(url, AistJsonContext.Default.ListJobResponse).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error fetching jobs: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<JobResponse?> GetJobAsync(string jobId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync($"jobs/{jobId}", AistJsonContext.Default.JobResponse).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error fetching job: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<JobResponse?> CreateJobAsync(CreateJobRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("jobs", request, AistJsonContext.Default.CreateJobRequest).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(AistJsonContext.Default.JobResponse).ConfigureAwait(false);
            }
            await Console.Error.WriteLineAsync($"Error creating job: {response.StatusCode}").ConfigureAwait(false);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error creating job: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<bool> UpdateJobStatusAsync(string jobId, JobStatus status)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"jobs/{jobId}/status", new UpdateJobStatusRequest(status), AistJsonContext.Default.UpdateJobStatusRequest).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error updating job status: {ex.Message}").ConfigureAwait(false);
            return false;
        }
    }

    public async Task<bool> UpdateJobAsync(string jobId, UpdateJobRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"jobs/{jobId}", request, AistJsonContext.Default.UpdateJobRequest).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error updating job: {ex.Message}").ConfigureAwait(false);
            return false;
        }
    }

    public async Task<bool> DeleteJobAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(new Uri($"jobs/{id}", UriKind.Relative)).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            await Console.Error.WriteLineAsync($"Error deleting job: {ex.Message}").ConfigureAwait(false);
            return false;
        }
    }

    // User Stories
    public async Task<List<UserStoryResponse>?> GetUserStoriesByJobAsync(string jobId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync($"userstories/by-job/{jobId}", AistJsonContext.Default.ListUserStoryResponse).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error fetching user stories: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<UserStoryResponse?> CreateUserStoryAsync(CreateUserStoryRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("userstories", request, AistJsonContext.Default.CreateUserStoryRequest).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(AistJsonContext.Default.UserStoryResponse).ConfigureAwait(false);
            }
            await Console.Error.WriteLineAsync($"Error creating user story: {response.StatusCode}").ConfigureAwait(false);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error creating user story: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<bool> UpdateUserStoryCompleteAsync(string storyId, bool isComplete)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"userstories/{storyId}/complete", new UpdateUserStoryCompleteRequest(isComplete), AistJsonContext.Default.UpdateUserStoryCompleteRequest).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error updating user story: {ex.Message}").ConfigureAwait(false);
            return false;
        }
    }

    // Acceptance Criteria
    public async Task<List<AcceptanceCriteriaResponse>?> GetAcceptanceCriteriaByStoryAsync(string storyId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync($"acceptancecriterias/by-story/{storyId}", AistJsonContext.Default.ListAcceptanceCriteriaResponse).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error fetching acceptance criteria: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<AcceptanceCriteriaResponse?> CreateAcceptanceCriteriaAsync(CreateAcceptanceCriteriaRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("acceptancecriterias", request, AistJsonContext.Default.CreateAcceptanceCriteriaRequest).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(AistJsonContext.Default.AcceptanceCriteriaResponse).ConfigureAwait(false);
            }
            await Console.Error.WriteLineAsync($"Error creating acceptance criteria: {response.StatusCode}").ConfigureAwait(false);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error creating acceptance criteria: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<bool> UpdateAcceptanceCriteriaAsync(string criteriaId, bool isMet)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"acceptancecriterias/{criteriaId}", new UpdateAcceptanceCriteriaRequest(isMet), AistJsonContext.Default.UpdateAcceptanceCriteriaRequest).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error updating acceptance criteria: {ex.Message}").ConfigureAwait(false);
            return false;
        }
    }

    // Progress Logs
    public async Task<List<ProgressLogResponse>?> GetProgressLogsByStoryAsync(string storyId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync($"progresslogs/by-story/{storyId}", AistJsonContext.Default.ListProgressLogResponse).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error fetching progress logs: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public async Task<ProgressLogResponse?> CreateProgressLogAsync(CreateProgressLogRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("progresslogs", request, AistJsonContext.Default.CreateProgressLogRequest).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync(AistJsonContext.Default.ProgressLogResponse).ConfigureAwait(false);
            }
            await Console.Error.WriteLineAsync($"Error creating progress log: {response.StatusCode}").ConfigureAwait(false);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            await Console.Error.WriteLineAsync($"Error creating progress log: {ex.Message}").ConfigureAwait(false);
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
