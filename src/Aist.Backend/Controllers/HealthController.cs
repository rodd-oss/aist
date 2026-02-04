using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Aist.Backend.Controllers;

[ApiController]
[Route("api/health")]
[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via DI")]
internal sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
