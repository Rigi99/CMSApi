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
        var entities = await _entitiesService.GetEnabledEntitiesAsync();
        return Ok(entities);
    }

    [HttpGet("admin")]
    public async Task<IActionResult> GetAllEntities()
    {
        if (!IsAdmin())
            return Forbid();

        var entities = await _entitiesService.GetAllEntitiesAsync();
        return Ok(entities);
    }

    [HttpPatch("{id}/disable")]
    public async Task<IActionResult> DisableEntity(string id)
    {
        if (!IsAdmin())
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

    private bool IsAdmin() => User.Identity?.Name == _authOptions.Username;
}