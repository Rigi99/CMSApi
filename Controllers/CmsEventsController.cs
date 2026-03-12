using CMSApi.Dtos;
using CMSApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMSApi.Controllers;

[ApiController]
[Route("cms/events")]
[Authorize(AuthenticationSchemes = "BasicAuthentication")]

public class CmsEventsController(ICmsEventProcessor processor, ILogger<CmsEventsController> logger, IConfiguration configuration) : ControllerBase
{
    private readonly ICmsEventProcessor _processor = processor;
    private readonly ILogger<CmsEventsController> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    // POST: /cms/events
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostEvents([FromBody] IEnumerable<CmsEventDto> events)
    {
        if (User.Identity?.Name != _configuration["BasicAuth:BasicUsername"])
        {
            _logger.LogWarning("Unauthorized user {User} tried to post CMS events", User.Identity?.Name);
            return Forbid();
        }

        if (events == null || !events.Any())
        {
            _logger.LogWarning("PostEvents called with empty or null events list");
            return BadRequest(new { message = "No events received" });
        }

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Received {EventCount} CMS events", events.Count());

        try
        {
            await _processor.ProcessEventsAsync(events);
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Successfully processed {EventCount} CMS events", events.Count());
            return Ok(new { message = $"Processed {events.Count()} events" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {EventCount} CMS events", events.Count());
            return StatusCode(500, "Internal server error");
        }
    }
}