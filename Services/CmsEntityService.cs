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

    private static readonly HashSet<string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "publish", "update", "unpublish", "delete"
        };

    public async Task ProcessEventsAsync(IEnumerable<CmsEntityDto> events)
    {
        var eventList = events
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.Version)
            .ToList();

        _logger.LogInformation("Starting to process {EventCount} CMS events", eventList.Count);

        await using var transaction = await _entityRepo.BeginTransactionAsync();

        try
        {
            var ids = eventList.Select(e => e.Id).Distinct().ToList();
            var entities = await _entityRepo.GetByIdsAsync(ids);

            foreach (var evt in eventList)
            {
                if (!IsValidEvent(evt, out var error))
                    throw new InvalidOperationException($"Invalid event {evt.Id}: {error}");

                var entity = entities.GetValueOrDefault(evt.Id);

                switch (evt.Type?.ToLowerInvariant())
                {
                    case "publish":
                    case "update":
                        entity = await HandlePublishOrUpdate(evt, entity, entities);
                        break;

                    case "unpublish":
                        await HandleUnpublish(evt, entity);
                        break;

                    case "delete":
                        await HandleDelete(evt, entity, entities);
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

        if (string.IsNullOrWhiteSpace(evt.Type) || !AllowedTypes.Contains(evt.Type))
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

        await _entityVersionRepo.AddVersionAsync(entity, evt);
        return entity;
    }

    private async Task HandleUnpublish(CmsEntityDto evt, CmsEntity? entity)
    {
        if (entity == null) return;

        await _entityVersionRepo.AddVersionAsync(entity, evt);
        entity.IsDisabled = true;
    }

    private async Task HandleDelete(CmsEntityDto evt, CmsEntity? entity, Dictionary<string, CmsEntity> entities)
    {
        if (entity == null) return;

        await _entityRepo.RemoveAsync(entity);
        entities.Remove(evt.Id);
    }
}