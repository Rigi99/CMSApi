using CMSApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMSApi.Controllers;

[ApiController]
[Route("api/entities")]
[Authorize(AuthenticationSchemes = "BasicAuthentication")]

public class EntityController(IEntityService entitiesService,
                                ILogger<EntityController> logger,
                                IConfiguration configuration) : ControllerBase
{
    private readonly IEntityService _entitiesService = entitiesService;
    private readonly ILogger<EntityController> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    [HttpGet]
    public async Task<IActionResult> GetEntities()
    {
        var entities = await _entitiesService.GetEnabledEntitiesAsync();
        return Ok(entities);
    }

    [HttpGet("admin")]
    public async Task<IActionResult> GetAllEntities()
    {
        if (User.Identity?.Name != _configuration["BasicAuth:AdminUsername"])
            return Forbid();

        var entities = await _entitiesService.GetAllEntitiesAsync();
        return Ok(entities);
    }

    [HttpPatch("{id}/disable")]
    public async Task<IActionResult> DisableEntity(string id)
    {
        if (User.Identity?.Name != _configuration["BasicAuth:AdminUsername"])
            return Forbid();

        try
        {
            await _entitiesService.DisableEntityAsync(id);
            return Ok(new { message = $"Entity {id} disabled" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Entity not found" });
        }
    }
}