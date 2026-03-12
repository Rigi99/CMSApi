using CMSApi.Domain;
using CMSApi.Data.Repository;

namespace CMSApi.Services
{
    public class EntityService(ICmsEntityRepository entityRepo, ILogger<EntityService> logger) : IEntityService
    {
        private readonly ICmsEntityRepository _entityRepo = entityRepo;
        private readonly ILogger<EntityService> _logger = logger;

        public async Task<List<CmsEntity>> GetEnabledEntitiesAsync()
        {
            var allEntities = await _entityRepo.GetAllAsync();
            var enabled = allEntities.Values.Where(e => !e.IsDisabled).ToList();

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Fetched {Count} enabled entities", enabled.Count);

            return enabled;
        }

        public async Task<List<CmsEntity>> GetAllEntitiesAsync()
        {
            var allEntities = await _entityRepo.GetAllAsync();

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Fetched {Count} total entities", allEntities.Count);

            return [.. allEntities.Values];
        }

        public async Task DisableEntityAsync(string id)
        {
            var entities = await _entityRepo.GetByIdsAsync([id]);

            if (!entities.TryGetValue(id, out var entity))
            {
                _logger.LogWarning("Entity {EntityId} not found", id);
                throw new KeyNotFoundException($"Entity {id} not found");
            }

            entity.IsDisabled = true;
            await _entityRepo.SaveChangesAsync();

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Entity {EntityId} disabled", id);
        }
    }
}