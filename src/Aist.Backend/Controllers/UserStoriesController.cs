using System.Diagnostics.CodeAnalysis;
using Aist.Backend.Data;
using Aist.Backend.Models;
using Aist.Core;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aist.Backend.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via DI")]
internal sealed class UserStoriesController : ControllerBase
{
    private readonly AistDbContext _context;

    public UserStoriesController(AistDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-job/{jobId}")]
    public async Task<ActionResult<IEnumerable<UserStoryResponse>>> GetStoriesByJob(Guid jobId)
    {
        var stories = await _context.UserStories
            .Where(us => us.JobId == jobId)
            .OrderBy(us => us.Priority)
            .Include(us => us.AcceptanceCriterias)
            .Include(us => us.ProgressLogs)
            .Select(us => new UserStoryResponse(
                us.Id,
                us.JobId,
                us.Title,
                us.Who,
                us.What,
                us.Why,
                us.Priority,
                us.IsComplete,
                us.CreatedAt,
                us.AcceptanceCriterias.Select(ac => new AcceptanceCriteriaResponse(ac.Id, ac.UserStoryId, ac.Description, ac.IsMet)).ToList(),
                us.ProgressLogs.Select(pl => new ProgressLogResponse(pl.Id, pl.UserStoryId, pl.Text, pl.CreatedAt)).ToList()))
            .ToListAsync().ConfigureAwait(false);

        return Ok(stories);
    }

    [HttpPost]
    public async Task<ActionResult<UserStoryResponse>> CreateUserStory(CreateUserStoryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var story = new UserStory
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            Title = request.Title,
            Who = request.Who,
            What = request.What,
            Why = request.Why,
            Priority = request.Priority,
            IsComplete = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserStories.Add(story);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return CreatedAtAction(
            nameof(GetStoriesByJob),
            new { jobId = story.JobId },
            new UserStoryResponse(
                story.Id,
                story.JobId,
                story.Title,
                story.Who,
                story.What,
                story.Why,
                story.Priority,
                story.IsComplete,
                story.CreatedAt,
                null,
                null));
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> UpdateStoryCompleteStatus(Guid id, UpdateUserStoryCompleteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var story = await _context.UserStories.FindAsync(id).ConfigureAwait(false);

        if (story == null)
        {
            return NotFound();
        }

        story.IsComplete = request.IsComplete;
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return NoContent();
    }
}
