using CMSApi.Domain;
using Microsoft.EntityFrameworkCore.Storage;

namespace CMSApi.Data.Repository;

public interface ICmsEntityRepository
{
    Task<Dictionary<string, CmsEntity>> GetByIdsAsync(IEnumerable<string> ids);  
    Task<Dictionary<string, CmsEntity>> GetAllAsync();                            
    Task AddAsync(CmsEntity entity);                                              
    Task RemoveAsync(CmsEntity entity);                                        
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}