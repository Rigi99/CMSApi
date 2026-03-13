using CMSApi.Domain;
using CMSApi.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMSApi.Data.Repository;

public class CmsEntityVersionRepository(ApplicationDbContext db, ILogger<CmsEntityVersionRepository> logger) : ICmsEntityVersionRepository
{
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<CmsEntityVersionRepository> _logger = logger;

    public async Task AddAsync(CmsEntityVersion version)
    {
        try
        {
            await _db.CmsEntityVersions.AddAsync(version);
            _logger.LogInformation("Version {Version} for entity {EntityId} added to DbContext", version.Version, version.CmsEntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding version {Version} for entity {EntityId}", version.Version, version.CmsEntityId);
            throw;
        }
    }

    public async Task AddVersionAsync(CmsEntity entity, CmsEntityDto evt)
    {
        if (entity.Versions.Any(v => v.Version == evt.Version))
            return;

        var version = new CmsEntityVersion
        {
            CmsEntityId = evt.Id,
            Version = evt.Version,
            Timestamp = evt.Timestamp,
            Payload = evt.Payload?.GetRawText()
        };

        await AddAsync(version);
        entity.Versions.Add(version);
    }

    public async Task<List<CmsEntityVersion>> GetByEntityIdAsync(string entityId)
    {
        try
        {
            return await _db.CmsEntityVersions
                            .Where(v => v.CmsEntityId == entityId)
                            .OrderBy(v => v.Version)
                            .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching versions for entity {EntityId}", entityId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string entityId, int version)
    {
        return await _db.CmsEntityVersions
            .AnyAsync(v => v.CmsEntityId == entityId && v.Version == version);
    }
}