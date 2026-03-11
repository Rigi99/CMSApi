using CMSApi.Domain;
using Microsoft.EntityFrameworkCore.Storage;

namespace CMSApi.Repository
{
    public interface ICmsEntityRepository
    {
        Task<Dictionary<string, CmsEntity>> GetByIdsWithVersionsAsync(IEnumerable<string> ids);
        Task AddEntityAsync(CmsEntity entity);
        Task AddVersionAsync(CmsEntityVersion version);
        Task RemoveEntityAsync(CmsEntity entity);
        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}