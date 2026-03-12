using CMSApi.Domain;
using CMSApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMSApi.Repository
{
    public class CmsEntityVersionRepository(ApplicationDbContext db, ILogger<CmsEntityVersionRepository> logger) : ICmsEntityVersionRepository
    {
        private readonly ApplicationDbContext _db = db;
        private readonly ILogger<CmsEntityVersionRepository> _logger = logger;

        public Task AddAsync(CmsEntityVersion version)
        {
            try
            {
                _db.CmsEntityVersions.Add(version);
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("Version {Version} for entity {EntityId} added to DbContext", version.Version, version.CmsEntityId);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding version {Version} for entity {EntityId}", version.Version, version.CmsEntityId);
                throw;
            }
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
    }
}