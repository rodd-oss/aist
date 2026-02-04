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
internal sealed class ProjectsController : ControllerBase
{
    private readonly AistDbContext _context;

    public ProjectsController(AistDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetProjects()
    {
        var projects = await _context.Projects
            .Select(p => new ProjectResponse(p.Id, p.Title, p.CreatedAt))
            .ToListAsync().ConfigureAwait(false);

        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectResponse>> GetProject(Guid id)
    {
        var project = await _context.Projects.FindAsync(id).ConfigureAwait(false);
        if (project == null)
        {
            return NotFound();
        }
        return Ok(new ProjectResponse(project.Id, project.Title, project.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> CreateProject(CreateProjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return CreatedAtAction(
            nameof(GetProject),
            new { id = project.Id },
            new ProjectResponse(project.Id, project.Title, project.CreatedAt));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var project = await _context.Projects.FindAsync(id).ConfigureAwait(false);
        
        if (project == null)
        {
            return NotFound();
        }

        project.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return NoContent();
    }
}
