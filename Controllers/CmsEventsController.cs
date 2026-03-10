using CMSApi.Dtos;
using CMSApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMSApi.Controllers;

[ApiController]
[Route("cms/events")]
public class CmsEventsController(ICmsEventProcessor processor) : ControllerBase
{
    private readonly ICmsEventProcessor _processor = processor;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostEvents([FromBody] IEnumerable<CmsEventDto> events)
    {
        if (events == null || !events.Any())
        {
            return BadRequest(new { message = "No events received" });
        }

        Console.WriteLine($"Received {events.Count()} events");

        await _processor.ProcessEventsAsync(events);

        return Ok(new
        {
            message = $"Processed {events.Count()} events"
        });
    }
}