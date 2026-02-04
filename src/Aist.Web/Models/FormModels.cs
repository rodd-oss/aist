using System.ComponentModel.DataAnnotations;
using Aist.Core;

namespace Aist.Web.Models;

internal sealed class ProjectForm
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = "";

    public CreateProjectRequest ToRequest() => new(Title);
}

internal sealed class JobForm
{
    public Guid ProjectId { get; set; }
    
    [Required]
    [StringLength(20)]
    public string ShortSlug { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = "";

    public JobType Type { get; set; } = JobType.Feature;

    [Required]
    public string Description { get; set; } = "";

    public CreateJobRequest ToRequest() => new(ProjectId, ShortSlug, Title, Type, Description);
}

internal sealed class StoryForm
{
    public Guid JobId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = "";

    [Required]
    public string Who { get; set; } = "";

    [Required]
    public string What { get; set; } = "";

    [Required]
    public string Why { get; set; } = "";

    [Range(1, 5)]
    public int Priority { get; set; } = 1;

    public CreateUserStoryRequest ToRequest() => new(JobId, Title, Who, What, Why, Priority);
}
