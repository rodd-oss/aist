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
public class AcceptanceCriteriasController : ControllerBase
{
    private readonly AistDbContext _context;

    public AcceptanceCriteriasController(AistDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-story/{storyId}")]
    public async Task<ActionResult<IEnumerable<AcceptanceCriteriaResponse>>> GetCriteriaByStory(Guid storyId)
    {
        var criterias = await _context.AcceptanceCriterias
            .Where(ac => ac.UserStoryId == storyId)
            .Select(ac => new AcceptanceCriteriaResponse(ac.Id, ac.UserStoryId, ac.Description, ac.IsMet))
            .ToListAsync();

        return Ok(criterias);
    }

    [HttpPost]
    public async Task<ActionResult<AcceptanceCriteriaResponse>> CreateAcceptanceCriteria(CreateAcceptanceCriteriaRequest request)
    {
        var criteria = new AcceptanceCriteria
        {
            Id = Guid.NewGuid(),
            UserStoryId = request.UserStoryId,
            Description = request.Description,
            IsMet = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.AcceptanceCriterias.Add(criteria);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCriteriaByStory),
            new { storyId = criteria.UserStoryId },
            new AcceptanceCriteriaResponse(criteria.Id, criteria.UserStoryId, criteria.Description, criteria.IsMet));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAcceptanceCriteria(Guid id, UpdateAcceptanceCriteriaRequest request)
    {
        var criteria = await _context.AcceptanceCriterias.FindAsync(id);

        if (criteria == null)
        {
            return NotFound();
        }

        criteria.IsMet = request.IsMet;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
