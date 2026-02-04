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
public sealed class ProgressLogsController : ControllerBase
{
    private readonly AistDbContext _context;

    public ProgressLogsController(AistDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-story/{storyId}")]
    public async Task<ActionResult<IEnumerable<ProgressLogResponse>>> GetLogsByStory(Guid storyId)
    {
        var logs = await _context.ProgressLogs
            .Where(pl => pl.UserStoryId == storyId)
            .OrderByDescending(pl => pl.CreatedAt)
            .Select(pl => new ProgressLogResponse(pl.Id, pl.UserStoryId, pl.Text, pl.CreatedAt))
            .ToListAsync().ConfigureAwait(false);

        return Ok(logs);
    }

    [HttpPost]
    public async Task<ActionResult<ProgressLogResponse>> CreateProgressLog(CreateProgressLogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var log = new ProgressLog
        {
            Id = Guid.NewGuid(),
            UserStoryId = request.UserStoryId,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProgressLogs.Add(log);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return CreatedAtAction(
            nameof(GetLogsByStory),
            new { storyId = log.UserStoryId },
            new ProgressLogResponse(log.Id, log.UserStoryId, log.Text, log.CreatedAt));
    }
}
