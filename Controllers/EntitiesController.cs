using CMSApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSApi.Controllers;

[ApiController]
[Route("api/entities")]
public class EntitiesController(ApplicationDbContext db) : ControllerBase
{
    private readonly ApplicationDbContext _db = db;

    // GET: api/entities
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntities()
    {
        var entities = await _db.CmsEntities
            .AsNoTracking()
            .Include(e => e.Versions)
            .Where(e => !e.IsDisabled)
            .ToListAsync();

        return Ok(entities);
    }

    // GET: api/entities/admin
    [HttpGet("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEntities()
    {
        var entities = await _db.CmsEntities
            .AsNoTracking()
            .Include(e => e.Versions)
            .ToListAsync();

        return Ok(entities);
    }

    // PATCH: api/entities/{id}/disable
    [HttpPatch("{id}/disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableEntity(string id)
    {
        var entity = await _db.CmsEntities.FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null)
            return NotFound(new { message = "Entity not found" });

        entity.IsDisabled = true;

        await _db.SaveChangesAsync();

        return Ok(new { message = $"Entity {id} disabled" });
    }
}