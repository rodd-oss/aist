using System.CommandLine;
using Aist.Cli.Services;

namespace Aist.Cli;

class Program
{
    private static readonly AistApiClient _apiClient = new();

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Aist - AI-assisted project management CLI");

        rootCommand.AddCommand(CreateProjectCommands());
        rootCommand.AddCommand(CreateJobCommands());
        rootCommand.AddCommand(CreateStoryCommands());
        rootCommand.AddCommand(CreateCriteriaCommands());
        rootCommand.AddCommand(CreateLogCommands());

        return await rootCommand.InvokeAsync(args);
    }

    private static Guid ParseGuid(string input)
    {
        if (Guid.TryParse(input, out var guid))
        {
            return guid;
        }
        return Guid.Empty;
    }

    private static Command CreateProjectCommands()
    {
        var projectCommand = new Command("project", "Manage projects");

        var listCommand = new Command("list", "List all projects");
        listCommand.SetHandler(async () =>
        {
            var projects = await _apiClient.GetProjectsAsync();
            if (projects != null)
            {
                if (projects.Count == 0)
                {
                    Console.WriteLine("No projects found.");
                }
                else
                {
                    foreach (var project in projects)
                    {
                        Console.WriteLine($"{project.Id} - {project.Title} (Created: {project.CreatedAt:yyyy-MM-dd})");
                    }
                }
            }
        });
        projectCommand.AddCommand(listCommand);

        var createCommand = new Command("create", "Create a new project");
        var titleOption = new Option<string>("--title", "Project title") { IsRequired = true };
        createCommand.AddOption(titleOption);
        createCommand.SetHandler(async (string title) =>
        {
            var project = await _apiClient.CreateProjectAsync(title);
            if (project != null)
            {
                Console.WriteLine($"Created project: {project.Id} - {project.Title}");
            }
        }, titleOption);
        projectCommand.AddCommand(createCommand);

        var deleteCommand = new Command("delete", "Delete a project");
        var projectIdOption = new Option<string>("--id", "Project ID") { IsRequired = true };
        deleteCommand.AddOption(projectIdOption);
        deleteCommand.SetHandler(async (string id) =>
        {
            var success = await _apiClient.DeleteProjectAsync(id);
            if (success)
            {
                Console.WriteLine($"Deleted project: {id}");
            }
            else
            {
                Console.WriteLine($"Failed to delete project: {id}");
            }
        }, projectIdOption);
        projectCommand.AddCommand(deleteCommand);

        return projectCommand;
    }

    private static Command CreateJobCommands()
    {
        var jobCommand = new Command("job", "Manage jobs (features, fixes, chores)");

        var listCommand = new Command("list", "List jobs");
        var projectIdOption = new Option<string?>("--project-id", "Filter by project ID");
        listCommand.AddOption(projectIdOption);
        listCommand.SetHandler(async (string? projectId) =>
        {
            var jobs = await _apiClient.GetJobsAsync(projectId);
            if (jobs != null)
            {
                if (jobs.Count == 0)
                {
                    Console.WriteLine("No jobs found.");
                }
                else
                {
                    foreach (var job in jobs)
                    {
                        Console.WriteLine($"{job.Id} [{job.Status}] {job.Type}: {job.Title} (Slug: {job.ShortSlug})");
                    }
                }
            }
        }, projectIdOption);
        jobCommand.AddCommand(listCommand);

        var createCommand = new Command("create", "Create a new job [Manager]");
        var createProjectIdOption = new Option<string>("--project-id", "Project ID") { IsRequired = true };
        var typeOption = new Option<JobType>("--type", "Job type") { IsRequired = true };
        typeOption.FromAmong("feature", "fix", "refactor", "chore", "fmt", "doc");
        var titleOption = new Option<string>("--title", "Job title") { IsRequired = true };
        var descriptionOption = new Option<string>("--description", "Job description") { IsRequired = true };
        var slugOption = new Option<string>("--slug", "Short slug for branch naming") { IsRequired = true };

        createCommand.AddOption(createProjectIdOption);
        createCommand.AddOption(typeOption);
        createCommand.AddOption(titleOption);
        createCommand.AddOption(descriptionOption);
        createCommand.AddOption(slugOption);

        createCommand.SetHandler(async (string projectId, JobType type, string title, string description, string slug) =>
        {
            var projectIdGuid = ParseGuid(projectId);
            if (projectIdGuid == Guid.Empty)
            {
                Console.WriteLine($"Error: Invalid project ID format: '{projectId}'. Expected a valid GUID.");
                Environment.Exit(1);
                return;
            }
            var request = new CreateJobRequest(
                projectIdGuid,
                slug,
                title,
                type,
                description
            );
            var job = await _apiClient.CreateJobAsync(request);
            if (job != null)
            {
                Console.WriteLine($"Created job: {job.Id} - {job.Title} [{job.Type}]");
            }
        }, createProjectIdOption, typeOption, titleOption, descriptionOption, slugOption);
        jobCommand.AddCommand(createCommand);

        var pullCommand = new Command("pull", "Pull job and create git branch [Developer]");
        var pullJobIdOption = new Option<string>("--job-id", "Job ID") { IsRequired = true };
        pullCommand.AddOption(pullJobIdOption);
        pullCommand.SetHandler(async (string jobId) =>
        {
            var job = await _apiClient.GetJobAsync(jobId);
            if (job == null)
            {
                Console.WriteLine($"Job not found: {jobId}");
                return;
            }

            var success = await _apiClient.UpdateJobStatusAsync(jobId, JobStatus.InProgress);
            if (success)
            {
                Console.WriteLine($"Job {jobId} status updated to InProgress");
                Console.WriteLine($"Suggested branch name: {jobId}_{job.ShortSlug}");
                Console.WriteLine("Note: Git branch creation not yet implemented.");
            }
            else
            {
                Console.WriteLine($"Failed to update job status: {jobId}");
            }
        }, pullJobIdOption);
        jobCommand.AddCommand(pullCommand);

        var doneCommand = new Command("done", "Mark job complete and create PR [Developer]");
        var doneJobIdOption = new Option<string>("--job-id", "Job ID") { IsRequired = true };
        var prTitleOption = new Option<string>("--pr-title", "Pull request title");
        var prDescriptionOption = new Option<string>("--pr-description", "Pull request description");
        doneCommand.AddOption(doneJobIdOption);
        doneCommand.AddOption(prTitleOption);
        doneCommand.AddOption(prDescriptionOption);
        doneCommand.SetHandler(async (string jobId, string? prTitle, string? prDescription) =>
        {
            var job = await _apiClient.GetJobAsync(jobId);
            if (job == null)
            {
                Console.WriteLine($"Job not found: {jobId}");
                return;
            }

            var success = await _apiClient.UpdateJobStatusAsync(jobId, JobStatus.Done);
            if (success)
            {
                Console.WriteLine($"Job {jobId} marked as Done");
                Console.WriteLine($"PR Title: {prTitle ?? job.Title}");
                Console.WriteLine("Note: PR creation not yet implemented.");
            }
            else
            {
                Console.WriteLine($"Failed to mark job as done: {jobId}");
            }
        }, doneJobIdOption, prTitleOption, prDescriptionOption);
        jobCommand.AddCommand(doneCommand);

        return jobCommand;
    }

    private static Command CreateStoryCommands()
    {
        var storyCommand = new Command("story", "Manage user stories");

        var listCommand = new Command("list", "List user stories for a job");
        var jobIdOption = new Option<string>("--job-id", "Job ID") { IsRequired = true };
        listCommand.AddOption(jobIdOption);
        listCommand.SetHandler(async (string jobId) =>
        {
            var stories = await _apiClient.GetUserStoriesByJobAsync(jobId);
            if (stories != null)
            {
                if (stories.Count == 0)
                {
                    Console.WriteLine("No stories found for this job.");
                }
                else
                {
                    foreach (var story in stories.OrderBy(s => s.Priority))
                    {
                        var status = story.IsComplete ? "[✓]" : "[ ]";
                        Console.WriteLine($"{status} Priority {story.Priority}: {story.Title} ({story.Id})");
                        Console.WriteLine($"    As a {story.Who}, I want {story.What}, so that {story.Why}");
                    }
                }
            }
        }, jobIdOption);
        storyCommand.AddCommand(listCommand);

        var createCommand = new Command("create", "Create a user story");
        var createJobIdOption = new Option<string>("--job-id", "Job ID") { IsRequired = true };
        var titleOption = new Option<string>("--title", "Story title") { IsRequired = true };
        var whoOption = new Option<string>("--who", "As a...") { IsRequired = true };
        var whatOption = new Option<string>("--what", "I want to...") { IsRequired = true };
        var whyOption = new Option<string>("--why", "So that...") { IsRequired = true };
        var priorityOption = new Option<int>("--priority", "Priority (lower is higher)") { IsRequired = true };

        createCommand.AddOption(createJobIdOption);
        createCommand.AddOption(titleOption);
        createCommand.AddOption(whoOption);
        createCommand.AddOption(whatOption);
        createCommand.AddOption(whyOption);
        createCommand.AddOption(priorityOption);

        createCommand.SetHandler(async (string jobId, string title, string who, string what, string why, int priority) =>
        {
            var jobIdGuid = ParseGuid(jobId);
            if (jobIdGuid == Guid.Empty)
            {
                Console.WriteLine($"Error: Invalid job ID format: '{jobId}'. Expected a valid GUID.");
                Environment.Exit(1);
                return;
            }
            var request = new CreateUserStoryRequest(
                jobIdGuid,
                title,
                who,
                what,
                why,
                priority
            );
            var story = await _apiClient.CreateUserStoryAsync(request);
            if (story != null)
            {
                Console.WriteLine($"Created user story: {story.Id} - {story.Title}");
            }
        }, createJobIdOption, titleOption, whoOption, whatOption, whyOption, priorityOption);
        storyCommand.AddCommand(createCommand);

        var completeCommand = new Command("complete", "Mark user story as complete");
        var storyIdOption = new Option<string>("--story-id", "Story ID") { IsRequired = true };
        completeCommand.AddOption(storyIdOption);
        completeCommand.SetHandler(async (string storyId) =>
        {
            var success = await _apiClient.UpdateUserStoryCompleteAsync(storyId, true);
            if (success)
            {
                Console.WriteLine($"Marked story {storyId} as complete ✓");
            }
            else
            {
                Console.WriteLine($"Failed to mark story as complete: {storyId}");
            }
        }, storyIdOption);
        storyCommand.AddCommand(completeCommand);

        return storyCommand;
    }

    private static Command CreateCriteriaCommands()
    {
        var criteriaCommand = new Command("criteria", "Manage acceptance criteria");

        var listCommand = new Command("list", "List acceptance criteria for a story");
        var storyIdOption = new Option<string>("--story-id", "Story ID") { IsRequired = true };
        listCommand.AddOption(storyIdOption);
        listCommand.SetHandler(async (string storyId) =>
        {
            var criterias = await _apiClient.GetAcceptanceCriteriaByStoryAsync(storyId);
            if (criterias != null)
            {
                if (criterias.Count == 0)
                {
                    Console.WriteLine("No acceptance criteria found for this story.");
                }
                else
                {
                    foreach (var criteria in criterias)
                    {
                        var status = criteria.IsMet ? "[✓]" : "[ ]";
                        Console.WriteLine($"{status} {criteria.Description} ({criteria.Id})");
                    }
                }
            }
        }, storyIdOption);
        criteriaCommand.AddCommand(listCommand);

        var createCommand = new Command("create", "Add acceptance criteria to a story");
        var createStoryIdOption = new Option<string>("--story-id", "Story ID") { IsRequired = true };
        var descriptionOption = new Option<string>("--description", "Criteria description") { IsRequired = true };
        createCommand.AddOption(createStoryIdOption);
        createCommand.AddOption(descriptionOption);
        createCommand.SetHandler(async (string storyId, string description) =>
        {
            var storyIdGuid = ParseGuid(storyId);
            if (storyIdGuid == Guid.Empty)
            {
                Console.WriteLine($"Error: Invalid story ID format: '{storyId}'. Expected a valid GUID.");
                Environment.Exit(1);
                return;
            }
            var request = new CreateAcceptanceCriteriaRequest(
                storyIdGuid,
                description
            );
            var criteria = await _apiClient.CreateAcceptanceCriteriaAsync(request);
            if (criteria != null)
            {
                Console.WriteLine($"Created acceptance criteria: {criteria.Id}");
            }
        }, createStoryIdOption, descriptionOption);
        criteriaCommand.AddCommand(createCommand);

        var checkCommand = new Command("check", "Mark criteria as met");
        var checkCriteriaIdOption = new Option<string>("--criteria-id", "Criteria ID") { IsRequired = true };
        checkCommand.AddOption(checkCriteriaIdOption);
        checkCommand.SetHandler(async (string criteriaId) =>
        {
            var success = await _apiClient.UpdateAcceptanceCriteriaAsync(criteriaId, true);
            if (success)
            {
                Console.WriteLine($"Marked criteria {criteriaId} as met ✓");
            }
            else
            {
                Console.WriteLine($"Failed to mark criteria as met: {criteriaId}");
            }
        }, checkCriteriaIdOption);
        criteriaCommand.AddCommand(checkCommand);

        var uncheckCommand = new Command("uncheck", "Mark criteria as unmet");
        var uncheckCriteriaIdOption = new Option<string>("--criteria-id", "Criteria ID") { IsRequired = true };
        uncheckCommand.AddOption(uncheckCriteriaIdOption);
        uncheckCommand.SetHandler(async (string criteriaId) =>
        {
            var success = await _apiClient.UpdateAcceptanceCriteriaAsync(criteriaId, false);
            if (success)
            {
                Console.WriteLine($"Marked criteria {criteriaId} as unmet ✗");
            }
            else
            {
                Console.WriteLine($"Failed to mark criteria as unmet: {criteriaId}");
            }
        }, uncheckCriteriaIdOption);
        criteriaCommand.AddCommand(uncheckCommand);

        return criteriaCommand;
    }

    private static Command CreateLogCommands()
    {
        var logCommand = new Command("log", "Manage progress logs");

        var listCommand = new Command("list", "List progress logs for a story");
        var storyIdOption = new Option<string>("--story-id", "Story ID") { IsRequired = true };
        listCommand.AddOption(storyIdOption);
        listCommand.SetHandler(async (string storyId) =>
        {
            var logs = await _apiClient.GetProgressLogsByStoryAsync(storyId);
            if (logs != null)
            {
                if (logs.Count == 0)
                {
                    Console.WriteLine("No progress logs found for this story.");
                }
                else
                {
                    foreach (var log in logs.OrderByDescending(l => l.CreatedAt))
                    {
                        Console.WriteLine($"[{log.CreatedAt:yyyy-MM-dd HH:mm}] {log.Text}");
                    }
                }
            }
        }, storyIdOption);
        logCommand.AddCommand(listCommand);

        var addCommand = new Command("add", "Add progress log entry");
        var addStoryIdOption = new Option<string>("--story-id", "Story ID") { IsRequired = true };
        var textOption = new Option<string>("--text", "Log entry text") { IsRequired = true };
        addCommand.AddOption(addStoryIdOption);
        addCommand.AddOption(textOption);
        addCommand.SetHandler(async (string storyId, string text) =>
        {
            var storyIdGuid = ParseGuid(storyId);
            if (storyIdGuid == Guid.Empty)
            {
                Console.WriteLine($"Error: Invalid story ID format: '{storyId}'. Expected a valid GUID.");
                Environment.Exit(1);
                return;
            }
            var request = new CreateProgressLogRequest(
                storyIdGuid,
                text
            );
            var log = await _apiClient.CreateProgressLogAsync(request);
            if (log != null)
            {
                Console.WriteLine($"Added progress log: {log.Id}");
            }
        }, addStoryIdOption, textOption);
        logCommand.AddCommand(addCommand);

        return logCommand;
    }
}
