using CMSApi.Domain;
using CMSApi.Data.Repository;

namespace CMSApi.Services;

public class EntityService(ICmsEntityRepository entityRepo, ILogger<EntityService> logger) : IEntityService
{
    private readonly ICmsEntityRepository _entityRepo = entityRepo;
    private readonly ILogger<EntityService> _logger = logger;

    public async Task<List<CmsEntity>> GetEnabledEntitiesAsync()
    {
        var allEntities = await _entityRepo.GetAllAsync();
        var enabled = allEntities.Values.Where(e => !e.IsDisabled).ToList();

        _logger.LogInformation("Fetched {Count} enabled entities", enabled.Count);
        return enabled;
    }

    public async Task<List<CmsEntity>> GetAllEntitiesAsync()
    {
        var allEntities = await _entityRepo.GetAllAsync();
        var entityList = allEntities.Values.ToList();

        _logger.LogInformation("Fetched {Count} total entities", entityList.Count);
        return entityList;
    }

    public async Task DisableEntityAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id), "Id cannot be null or whitespace");

        var entities = await _entityRepo.GetByIdsAsync([id]) ?? [];

        if (!entities.TryGetValue(id, out var entity) || entity == null)
        {
            _logger.LogWarning("Entity {EntityId} not found", id);
            throw new KeyNotFoundException($"Entity {id} not found");
        }

        if (entity.IsDisabled)
        {
            _logger.LogInformation("Entity {EntityId} is already disabled", id);
            return;
        }

        entity.IsDisabled = true;
        await _entityRepo.SaveChangesAsync();

        _logger.LogInformation("Entity {EntityId} disabled", id);
    }
}