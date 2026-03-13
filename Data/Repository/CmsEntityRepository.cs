using CMSApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CMSApi.Data.Repository;

public class CmsEntityRepository(ApplicationDbContext db, ILogger<CmsEntityRepository> logger) : ICmsEntityRepository
{
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<CmsEntityRepository> _logger = logger;

    public async Task<Dictionary<string, CmsEntity>> GetByIdsAsync(IEnumerable<string> ids)
    {
        try
        {
            return await _db.CmsEntities
                            .Where(e => ids.Contains(e.Id))
                            .ToDictionaryAsync(e => e.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching entities for IDs: {Ids}", string.Join(',', ids));
            throw;
        }
    }

    public async Task<Dictionary<string, CmsEntity>> GetAllAsync()
    {
        try
        {
            return await _db.CmsEntities
                            .Include(e => e.Versions)
                            .ToDictionaryAsync(e => e.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all entities");
            throw;
        }
    }

    public async Task AddAsync(CmsEntity entity)
    {
        try
        {
            await _db.CmsEntities.AddAsync(entity);
            _logger.LogInformation("Entity {EntityId} added to DbContext", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity {EntityId}", entity.Id);
            throw;
        }
    }

    public async Task RemoveAsync(CmsEntity entity)
    {
        try
        {
            _db.CmsEntities.Remove(entity);
            _logger.LogInformation("Entity {EntityId} removed from DbContext", entity.Id);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing entity {EntityId}", entity.Id);
            throw;
        }
    }

    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync() => _db.Database.BeginTransactionAsync();
}