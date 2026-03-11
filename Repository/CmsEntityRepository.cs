using CMSApi.Domain;
using CMSApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CMSApi.Repository
{
    public class CmsEntityRepository(ApplicationDbContext db, ILogger<CmsEntityRepository> logger) : ICmsEntityRepository
    {
        private readonly ApplicationDbContext _db = db;
        private readonly ILogger<CmsEntityRepository> _logger = logger;

        public async Task<Dictionary<string, CmsEntity>> GetByIdsWithVersionsAsync(IEnumerable<string> ids)
        {
            try
            {
                return await _db.CmsEntities
                                .Include(e => e.Versions)
                                .Where(e => ids.Contains(e.Id))
                                .ToDictionaryAsync(e => e.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching entities for IDs: {Ids}", string.Join(",", ids));
                throw;
            }
        }

        public Task AddEntityAsync(CmsEntity entity)
        {
            try
            {
                _db.CmsEntities.Add(entity);
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("Entity {EntityId} added to DbContext", entity.Id);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity {EntityId}", entity.Id);
                throw;
            }
        }

        public Task AddVersionAsync(CmsEntityVersion version)
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

        public Task RemoveEntityAsync(CmsEntity entity)
        {
            try
            {
                _db.CmsEntities.Remove(entity);
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("Entity {EntityId} removed from DbContext", entity.Id);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing entity {EntityId}", entity.Id);
                throw;
            }
        }

        public Task<int> SaveChangesAsync()
        {
            try
            {
                return _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to DbContext");
                throw;
            }
        }

        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            try
            {
                return _db.Database.BeginTransactionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting DB transaction");
                throw;
            }
        }
    }
}