using CMSApi.Data.Repository;
using CMSApi.Domain;
using CMSApi.Dtos;
using System.Text.RegularExpressions;

namespace CMSApi.Services
{
    public partial class CmsEventService(ICmsEntityRepository entityRepo,
                            ICmsEntityVersionRepository entityVersionRepo,
                            ILogger<CmsEventService> logger) : ICmsEventService
    {
        private readonly ICmsEntityRepository _entityRepo = entityRepo;
        private readonly ICmsEntityVersionRepository _entityVersionRepo = entityVersionRepo;
        private readonly ILogger<CmsEventService> _logger = logger;

        public async Task ProcessEventsAsync(IEnumerable<CmsEventDto> events)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Starting to process {EventCount} CMS events", events.Count());

            await using var transaction = await _entityRepo.BeginTransactionAsync();

            try
            {
                var eventList = events
                    .OrderBy(e => e.Timestamp)
                    .ThenBy(e => e.Version)
                    .ToList();

                var ids = eventList.Select(e => e.Id).Distinct().ToList();
                var entities = await _entityRepo.GetByIdsAsync(ids);
                foreach (var evt in eventList)
                {
                    if (!IsValidEvent(evt, out var error))
                        throw new InvalidOperationException($"Invalid event {evt.Id}: {error}");

                    entities.TryGetValue(evt.Id, out var entity);

                    switch (evt.Type.ToLowerInvariant())
                    {
                        case "publish":
                        case "update":
                            if (entity == null)
                            {
                                entity = new CmsEntity { Id = evt.Id, LatestVersion = evt.Version };
                                await _entityRepo.AddAsync(entity);
                                entities[evt.Id] = entity;
                            }
                            else
                            {
                                entity.IsDisabled = false;
                                entity.LatestVersion = Math.Max(entity.LatestVersion, evt.Version);
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
                                await _entityVersionRepo.AddAsync(version);
                                entity.Versions.Add(version);
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
                                    await _entityVersionRepo.AddAsync(version);
                                    entity.Versions.Add(version);
                                }
                                entity.IsDisabled = true;
                            }
                            break;

                        case "delete":
                            if (entity != null)
                            {
                                await _entityRepo.RemoveAsync(entity);
                                entities.Remove(evt.Id);
                            }
                            break;

                        default:
                            _logger.LogWarning("Unknown event type '{EventType}' for entity {EntityId}", evt.Type, evt.Id);
                            break;
                    }
                }

                await _entityRepo.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static bool IsValidEvent(CmsEventDto evt, out string error)
        {
            error = string.Empty;
            var allowedTypes = new[] { "publish", "update", "unpublish", "delete" };

            if (string.IsNullOrWhiteSpace(evt.Id))
            {
                error = "Id cannot be empty";
                return false;
            }

            if (!IdFormatRegex().IsMatch(evt.Id))
            {
                error = "Id format invalid";
                return false;
            }

            if (!allowedTypes.Contains(evt.Type?.ToLowerInvariant()))
            {
                error = $"Invalid type {evt.Type}";
                return false;
            }

            if (evt.Version < 1 && !string.Equals(evt.Type, "delete", StringComparison.OrdinalIgnoreCase))
            {
                error = $"Invalid version {evt.Version}";
                return false;
            }

            if (evt.Timestamp == default || evt.Timestamp > DateTime.UtcNow.AddMinutes(5))
            {
                error = "Invalid timestamp";
                return false;
            }

            if (evt.Payload.HasValue && evt.Payload.Value.GetRawText().Length > 1_000_000)
            {
                error = "Payload too large";
                return false;
            }

            return true;
        }

        [GeneratedRegex(@"^[a-zA-Z0-9\-]{1,50}$")]
        private static partial Regex IdFormatRegex();
    }
}