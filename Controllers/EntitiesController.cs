using CMSApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSApi.Controllers;

[ApiController]
[Route("api/entities")]
public class EntitiesController(ApplicationDbContext db, ILogger<EntitiesController> logger) : ControllerBase
{
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<EntitiesController> _logger = logger;

    // GET: api/entities
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntities()
    {
        _logger.LogInformation("Fetching enabled CMS entities at {Time}", DateTime.UtcNow);

        var entities = await _db.CmsEntities
            .AsNoTracking()
            .Include(e => e.Versions)
            .Where(e => !e.IsDisabled)
            .ToListAsync();

        _logger.LogInformation("Returned {Count} enabled CMS entities", entities.Count);
        return Ok(entities);
    }

    // GET: api/entities/admin
    [HttpGet("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEntities()
    {
        _logger.LogInformation("Fetching all CMS entities (admin) at {Time}", DateTime.UtcNow);

        var entities = await _db.CmsEntities
            .AsNoTracking()
            .Include(e => e.Versions)
            .ToListAsync();

        _logger.LogInformation("Returned {Count} total CMS entities", entities.Count);
        return Ok(entities);
    }

    // PATCH: api/entities/{id}/disable
    [HttpPatch("{id}/disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableEntity(string id)
    {
        _logger.LogInformation("DisableEntity called for ID {EntityId}", id);

        var entity = await _db.CmsEntities.FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null)
        {
            _logger.LogWarning("Entity with ID {EntityId} not found", id);
            return NotFound(new { message = "Entity not found" });
        }

        entity.IsDisabled = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Entity {EntityId} disabled successfully", id);
        return Ok(new { message = $"Entity {id} disabled" });
    }
}