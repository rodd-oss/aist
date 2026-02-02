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
public class JobsController : ControllerBase
{
    private readonly AistDbContext _context;

    public JobsController(AistDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobs([FromQuery] Guid? projectId)
    {
        var query = _context.Jobs.AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(j => j.ProjectId == projectId.Value);
        }

        var jobs = await query
            .Select(j => new JobResponse(
                j.Id,
                j.ProjectId,
                j.ShortSlug,
                j.Title,
                j.Status,
                j.Type,
                j.Description,
                j.CreatedAt,
                null))
            .ToListAsync();

        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobResponse>> GetJob(Guid id)
    {
        var job = await _context.Jobs
            .Include(j => j.UserStories)
                .ThenInclude(us => us.AcceptanceCriterias)
            .Include(j => j.UserStories)
                .ThenInclude(us => us.ProgressLogs)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound();
        }

        var response = new JobResponse(
            job.Id,
            job.ProjectId,
            job.ShortSlug,
            job.Title,
            job.Status,
            job.Type,
            job.Description,
            job.CreatedAt,
            job.UserStories.Select(us => new UserStorySummaryResponse(us.Id, us.Title, us.Priority, us.IsComplete)).ToList());

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJob(CreateJobRequest request)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            ShortSlug = request.ShortSlug,
            Title = request.Title,
            Type = request.Type,
            Description = request.Description,
            Status = JobStatus.Todo,
            CreatedAt = DateTime.UtcNow
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetJob),
            new { id = job.Id },
            new JobResponse(
                job.Id,
                job.ProjectId,
                job.ShortSlug,
                job.Title,
                job.Status,
                job.Type,
                job.Description,
                job.CreatedAt,
                null));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateJobStatus(Guid id, UpdateJobStatusRequest request)
    {
        var job = await _context.Jobs.FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }

        job.Status = request.Status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(Guid id, UpdateJobRequest request)
    {
        var job = await _context.Jobs.FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }

        job.Title = request.Title;
        job.Description = request.Description;
        job.ShortSlug = request.ShortSlug;
        job.Type = request.Type;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var job = await _context.Jobs.FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }

        job.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
