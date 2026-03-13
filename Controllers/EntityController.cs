using CMSApi.Authentication;
using CMSApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CMSApi.Controllers;

[ApiController]
[Route("api/entities")]
[Authorize(AuthenticationSchemes = "BasicAuthentication")]
public class EntityController(
    IEntityService entitiesService,
    ILogger<EntityController> logger,
    IOptions<BasicAuthOptions> authOptions) : ControllerBase
{
    private readonly IEntityService _entitiesService = entitiesService;
    private readonly ILogger<EntityController> _logger = logger;
    private readonly BasicAuthOptions _authOptions = authOptions.Value;

    [HttpGet]
    public async Task<IActionResult> GetEntities()
    {
        _logger.LogInformation("User {User} requested enabled entities", User.Identity?.Name);

        try
        {
            var entities = await _entitiesService.GetEnabledEntitiesAsync();
            _logger.LogInformation("Returned {Count} enabled entities to {User}", entities.Count(), User.Identity?.Name);
            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching enabled entities for user {User}", User.Identity?.Name);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("admin")]
    public async Task<IActionResult> GetAllEntities()
    {
        _logger.LogInformation("User {User} requested all entities (admin endpoint)", User.Identity?.Name);

        if (!IsAdmin())
        {
            _logger.LogWarning("Unauthorized access attempt to admin endpoint by user {User}", User.Identity?.Name);
            return Forbid();
        }

        try
        {
            var entities = await _entitiesService.GetAllEntitiesAsync();
            _logger.LogInformation("Returned {Count} entities to admin {User}", entities.Count(), User.Identity?.Name);
            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all entities for admin {User}", User.Identity?.Name);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPatch("{id}/disable")]
    public async Task<IActionResult> DisableEntity(string id)
    {
        _logger.LogInformation("User {User} requested to disable entity {EntityId}", User.Identity?.Name, id);

        if (!IsAdmin())
        {
            _logger.LogWarning("Unauthorized disable attempt on entity {EntityId} by user {User}", id, User.Identity?.Name);
            return Forbid();
        }

        try
        {
            await _entitiesService.DisableEntityAsync(id);
            _logger.LogInformation("Entity {EntityId} disabled by admin {User}", id, User.Identity?.Name);
            return Ok(new { message = $"Entity {id} disabled" });
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Entity {EntityId} not found when disable was attempted by admin {User}", id, User.Identity?.Name);
            return NotFound(new { message = "Entity not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling entity {EntityId} by admin {User}", id, User.Identity?.Name);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private bool IsAdmin() => User.Identity?.Name == _authOptions.AdminUsername;
}