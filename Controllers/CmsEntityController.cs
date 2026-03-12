using CMSApi.Authentication;
using CMSApi.Dtos;
using CMSApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CMSApi.Controllers;

[ApiController]
[Route("cms/events")]
[Authorize(AuthenticationSchemes = "BasicAuthentication")]
public class CmsEntityController(
    ICmsEntityService cmsEventService,
    ILogger<CmsEntityController> logger,
    IOptions<BasicAuthOptions> authOptions) : ControllerBase
{
    private readonly ICmsEntityService _cmsEventService = cmsEventService;
    private readonly ILogger<CmsEntityController> _logger = logger;
    private readonly BasicAuthOptions _authOptions = authOptions.Value;

    // POST: /cms/events
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostEvents([FromBody] IEnumerable<CmsEntityDto> events)
    {
        if (User.Identity?.Name != _authOptions.Username)
        {
            _logger.LogWarning("Unauthorized user {User} tried to post CMS events", User.Identity?.Name);
            return Forbid();
        }

        var eventList = events?.ToList();
        if (eventList == null || eventList.Count == 0)
        {
            _logger.LogWarning("PostEvents called with empty or null events list");
            return BadRequest(new { message = "No events received" });
        }

        try
        {
            await _cmsEventService.ProcessEventsAsync(eventList);
            _logger.LogInformation("Successfully processed {EventCount} CMS events", eventList.Count);
            return Ok(new { message = $"Processed {eventList.Count} events" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error in CMS events");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {EventCount} CMS events", eventList.Count);
            return StatusCode(500, "Internal server error");
        }
    }
}