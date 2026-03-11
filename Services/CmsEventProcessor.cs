using CMSApi.Domain;
using CMSApi.Dtos;
using CMSApi.Repository;

namespace CMSApi.Services
{
    public class CmsEventProcessor(ICmsEntityRepository repo, ILogger<CmsEventProcessor> logger) : ICmsEventProcessor
    {
        private readonly ICmsEntityRepository _repo = repo;
        private readonly ILogger<CmsEventProcessor> _logger = logger;

        public async Task ProcessEventsAsync(IEnumerable<CmsEventDto> events)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Starting to process {EventCount} CMS events", events.Count());

            await using var transaction = await _repo.BeginTransactionAsync();

            try
            {
                var eventList = events
                    .OrderBy(e => e.Timestamp)
                    .ThenBy(e => e.Version)
                    .ToList();
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("Ordered {EventCount} events by timestamp and version", eventList.Count);

                var ids = eventList.Select(e => e.Id).Distinct().ToList();
                var entities = await _repo.GetByIdsWithVersionsAsync(ids);

                _logger.LogInformation("Fetched {EntityCount} existing entities from repository", entities.Count);

                foreach (var evt in eventList)
                {
                    entities.TryGetValue(evt.Id, out var entity);

                    switch (evt.Type.ToLowerInvariant())
                    {
                        case "publish":
                        case "update":
                            if (entity == null)
                            {
                                entity = new CmsEntity
                                {
                                    Id = evt.Id,
                                    LatestVersion = evt.Version
                                };

                                await _repo.AddEntityAsync(entity);
                                entities[evt.Id] = entity;
                                if (_logger.IsEnabled(LogLevel.Information))
                                    _logger.LogInformation("Created new entity {EntityId} with version {Version}", evt.Id, evt.Version);
                            }
                            else
                            {
                                entity.IsDisabled = false;
                                entity.LatestVersion = Math.Max(entity.LatestVersion, evt.Version);
                                if (_logger.IsEnabled(LogLevel.Information))
                                    _logger.LogInformation("Updated entity {EntityId} to version {Version}", evt.Id, evt.Version);
                            }

                            if (!entity.Versions.Any(v => v.Version == evt.Version))
                            {
                                var version = new CmsEntityVersion
                                {
                                    CmsEntityId = evt.Id,
                                    Version = evt.Version,
                                    Timestamp = evt.Timestamp,
                                    Payload = evt.Payload?.GetRawText()
                                };

                                await _repo.AddVersionAsync(version);
                                entity.Versions.Add(version);

                                if (_logger.IsEnabled(LogLevel.Information))
                                    _logger.LogInformation("Added version {Version} to entity {EntityId}", evt.Version, evt.Id);
                            }

                            break;
                        case "unpublish":
                            if (entity != null)
                            {
                                if (!entity.Versions.Any(v => v.Version == evt.Version))
                                {
                                    var version = new CmsEntityVersion
                                    {
                                        CmsEntityId = evt.Id,
                                        Version = evt.Version,
                                        Timestamp = evt.Timestamp,
                                        Payload = evt.Payload?.GetRawText()
                                    };
                                    await _repo.AddVersionAsync(version);
                                    entity.Versions.Add(version);

                                    if (_logger.IsEnabled(LogLevel.Information))
                                        _logger.LogInformation("Added version {Version} for unpublish to entity {EntityId}", evt.Version, evt.Id);
                                }

                                entity.IsDisabled = true;
                                if (_logger.IsEnabled(LogLevel.Information))
                                    _logger.LogInformation("Entity {EntityId} marked as disabled (unpublish)", evt.Id);
                            }
                            break;

                        case "delete":
                            if (entity != null)
                            {
                                await _repo.RemoveEntityAsync(entity);
                                entities.Remove(evt.Id);

                                if (_logger.IsEnabled(LogLevel.Information))
                                    _logger.LogInformation("Entity {EntityId} deleted", evt.Id);
                            }
                            break;
                        default:
                            _logger.LogWarning("Unknown event type '{EventType}' for entity {EntityId}", evt.Type, evt.Id);
                            break;
                    }
                }


                await _repo.SaveChangesAsync();
                await transaction.CommitAsync();

                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("Successfully processed {EventCount} CMS events", events.Count());
            }
            catch (Exception ex)
            {

                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing CMS events, transaction rolled back");
                throw;

            }
        }
    }
}