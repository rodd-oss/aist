using System.Net.Http.Json;

namespace Aist.Cli.Services;

public class AistApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly string BaseUrl = GetBaseUrl();

    private static string GetBaseUrl()
    {
        var url = Environment.GetEnvironmentVariable("AIST_API_URL")
            ?? "http://localhost:5000/api/";
        // Ensure trailing slash for proper URI joining
        if (!url.EndsWith("/"))
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
            return await _httpClient.GetFromJsonAsync<List<ProjectResponse>>("projects");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching projects: {ex}");
            return null;
        }
    }

    public async Task<ProjectResponse?> CreateProjectAsync(string title)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("projects", new { Title = title });
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProjectResponse>();
            }
            Console.WriteLine($"Error creating project: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating project: {ex}");
            return null;
        }
    }

    public async Task<bool> DeleteProjectAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"projects/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting project: {ex}");
            return false;
        }
    }

    // Jobs
    public async Task<List<JobResponse>?> GetJobsAsync(string? projectId = null)
    {
        try
        {
            var url = projectId != null ? $"jobs?projectId={projectId}" : "jobs";
            return await _httpClient.GetFromJsonAsync<List<JobResponse>>(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching jobs: {ex}");
            return null;
        }
    }

    public async Task<JobResponse?> GetJobAsync(string jobId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<JobResponse>($"jobs/{jobId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching job: {ex}");
            return null;
        }
    }

    public async Task<JobResponse?> CreateJobAsync(CreateJobRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("jobs", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<JobResponse>();
            }
            Console.WriteLine($"Error creating job: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating job: {ex}");
            return null;
        }
    }

    public async Task<bool> UpdateJobStatusAsync(string jobId, JobStatus status)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"jobs/{jobId}/status", new { Status = status });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating job status: {ex}");
            return false;
        }
    }

    public async Task<bool> DeleteJobAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"jobs/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting job: {ex}");
            return false;
        }
    }

    // User Stories
    public async Task<List<UserStoryResponse>?> GetUserStoriesByJobAsync(string jobId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserStoryResponse>>($"userstories/by-job/{jobId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching user stories: {ex}");
            return null;
        }
    }

    public async Task<UserStoryResponse?> CreateUserStoryAsync(CreateUserStoryRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("userstories", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserStoryResponse>();
            }
            Console.WriteLine($"Error creating user story: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating user story: {ex}");
            return null;
        }
    }

    public async Task<bool> UpdateUserStoryCompleteAsync(string storyId, bool isComplete)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"userstories/{storyId}/complete", new { IsComplete = isComplete });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user story: {ex}");
            return false;
        }
    }

    // Acceptance Criteria
    public async Task<List<AcceptanceCriteriaResponse>?> GetAcceptanceCriteriaByStoryAsync(string storyId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<AcceptanceCriteriaResponse>>($"acceptancecriterias/by-story/{storyId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching acceptance criteria: {ex}");
            return null;
        }
    }

    public async Task<AcceptanceCriteriaResponse?> CreateAcceptanceCriteriaAsync(CreateAcceptanceCriteriaRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("acceptancecriterias", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AcceptanceCriteriaResponse>();
            }
            Console.WriteLine($"Error creating acceptance criteria: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating acceptance criteria: {ex}");
            return null;
        }
    }

    public async Task<bool> UpdateAcceptanceCriteriaAsync(string criteriaId, bool isMet)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"acceptancecriterias/{criteriaId}", new { IsMet = isMet });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating acceptance criteria: {ex}");
            return false;
        }
    }

    // Progress Logs
    public async Task<List<ProgressLogResponse>?> GetProgressLogsByStoryAsync(string storyId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ProgressLogResponse>>($"progresslogs/by-story/{storyId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching progress logs: {ex}");
            return null;
        }
    }

    public async Task<ProgressLogResponse?> CreateProgressLogAsync(CreateProgressLogRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("progresslogs", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProgressLogResponse>();
            }
            Console.WriteLine($"Error creating progress log: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating progress log: {ex}");
            return null;
        }
    }
}

// DTOs
public record ProjectResponse(Guid Id, string Title, DateTime CreatedAt);
public record CreateJobRequest(Guid ProjectId, string ShortSlug, string Title, JobType Type, string Description);
public record JobResponse(Guid Id, Guid ProjectId, string ShortSlug, string Title, JobStatus Status, JobType Type, string Description, DateTime CreatedAt, List<UserStorySummaryResponse>? UserStories);
public record UserStorySummaryResponse(Guid Id, string Title, int Priority, bool IsComplete);
public record UserStoryResponse(Guid Id, Guid JobId, string Title, string Who, string What, string Why, int Priority, bool IsComplete, DateTime CreatedAt, List<AcceptanceCriteriaResponse>? AcceptanceCriterias, List<ProgressLogResponse>? ProgressLogs);
public record CreateUserStoryRequest(Guid JobId, string Title, string Who, string What, string Why, int Priority);
public record AcceptanceCriteriaResponse(Guid Id, Guid UserStoryId, string Description, bool IsMet);
public record CreateAcceptanceCriteriaRequest(Guid UserStoryId, string Description);
public record ProgressLogResponse(Guid Id, Guid UserStoryId, string Text, DateTime CreatedAt);
public record CreateProgressLogRequest(Guid UserStoryId, string Text);

public enum JobStatus
{
    Todo,
    InProgress,
    Done
}

public enum JobType
{
    Feature,
    Fix,
    Refactor,
    Chore,
    Fmt,
    Doc
}
