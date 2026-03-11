using CMSApi.Domain;
using CMSApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CMSApi.Repository
{
    public class CmsEntityRepository(ApplicationDbContext db) : ICmsEntityRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<Dictionary<string, CmsEntity>> GetByIdsWithVersionsAsync(IEnumerable<string> ids)
        {
            return await _db.CmsEntities
                            .Include(e => e.Versions)
                            .Where(e => ids.Contains(e.Id))
                            .ToDictionaryAsync(e => e.Id);
        }

        public Task AddEntityAsync(CmsEntity entity)
        {
            _db.CmsEntities.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddVersionAsync(CmsEntityVersion version)
        {
            _db.CmsEntityVersions.Add(version);
            return Task.CompletedTask;
        }

        public Task RemoveEntityAsync(CmsEntity entity)
        {
            _db.CmsEntities.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();

        public Task<IDbContextTransaction> BeginTransactionAsync() => _db.Database.BeginTransactionAsync();
    }
}