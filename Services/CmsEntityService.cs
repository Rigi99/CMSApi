using CMSApi.Data.Repository;
using CMSApi.Domain;
using CMSApi.Dtos;
using System.Text.RegularExpressions;

namespace CMSApi.Services;

public partial class CmsEntityService(
    ICmsEntityRepository entityRepo,
    ICmsEntityVersionRepository entityVersionRepo,
    ILogger<CmsEntityService> logger) : ICmsEntityService
{
    private readonly ICmsEntityRepository _entityRepo = entityRepo;
    private readonly ICmsEntityVersionRepository _entityVersionRepo = entityVersionRepo;
    private readonly ILogger<CmsEntityService> _logger = logger;

    public async Task ProcessEventsAsync(IEnumerable<CmsEntityDto> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.Where(e => e != null).ToList();

        if (eventList.Count == 0)
            throw new InvalidOperationException("No events received");

        _logger.LogInformation("Starting to process {EventCount} CMS events", eventList.Count);

        await using var transaction = await _entityRepo.BeginTransactionAsync();

        try
        {
            var ids = eventList
    .Select(e => e.Id)
    .Where(id => !string.IsNullOrWhiteSpace(id))
    .Distinct()
    .ToList();

            var entities = await _entityRepo.GetByIdsAsync(ids) ?? new Dictionary<string, CmsEntity>();

            foreach (var evt in eventList)
            {
                if (!IsValidEvent(evt, out var error))
                    throw new InvalidOperationException($"Invalid event {evt.Id}: {error}");

                entities.TryGetValue(evt.Id, out var entity);

                if (string.IsNullOrWhiteSpace(evt.Type))
                    throw new InvalidOperationException($"Invalid event type for {evt.Id}");

                var eventType = evt.Type.ToLowerInvariant();

                switch (eventType)
                {
                    case "publish":
                    case "update":
                        entity = await HandlePublishOrUpdate(evt, entity, entities);
                        break;

                    case "unpublish":
                        if (entity != null)
                            await HandleUnpublish(evt, entity);
                        else
                            _logger.LogWarning("Unpublish event for missing entity {Id}", evt.Id);
                        break;

                    case "delete":
                        if (entity != null)
                            await HandleDelete(evt, entity, entities);
                        else
                            _logger.LogWarning("Delete event for missing entity {Id}", evt.Id);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown event type '{evt.Type}' for {evt.Id}");
                }
            }

            await _entityRepo.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CMS events");
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static bool IsValidEvent(CmsEntityDto evt, out string error)
    {
        error = string.Empty;

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

        if (string.IsNullOrWhiteSpace(evt.Type))
        {
            error = $"Invalid type {evt.Type}";
            return false;
        }

        if (evt.Version < 1 && !string.Equals(evt.Type, "delete", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Invalid version {evt.Version}";
            return false;
        }

        var now = DateTime.UtcNow;
        if (evt.Timestamp == default || evt.Timestamp > now.AddMinutes(5))
        {
            error = "Invalid timestamp";
            return false;
        }

        if (evt.Payload?.GetRawText().Length > 1_000_000)
        {
            error = "Payload too large";
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\-]{1,50}$")]
    private static partial Regex IdFormatRegex();

    private async Task<CmsEntity> HandlePublishOrUpdate(
    CmsEntityDto evt,
    CmsEntity? entity,
    Dictionary<string, CmsEntity> entities)
    {
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

        var exists = await _entityVersionRepo.ExistsAsync(entity.Id, evt.Version);
        if (!exists)
            await _entityVersionRepo.AddVersionAsync(entity, evt);

        return entity;
    }

    private async Task HandleUnpublish(CmsEntityDto evt, CmsEntity? entity)
    {
        if (entity == null) return;

        var exists = await _entityVersionRepo.ExistsAsync(entity.Id, evt.Version);
        if (!exists)
            await _entityVersionRepo.AddVersionAsync(entity, evt);

        entity.IsDisabled = true;
    }

    private async Task HandleDelete(CmsEntityDto evt, CmsEntity? entity, Dictionary<string, CmsEntity>? entities)
    {
        if (entity == null || entities == null) return;

        await _entityRepo.RemoveAsync(entity);
        entities.Remove(evt.Id);
    }
}