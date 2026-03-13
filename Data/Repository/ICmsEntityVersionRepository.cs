using CMSApi.Domain;
using CMSApi.Dtos;

namespace CMSApi.Data.Repository
{
    public interface ICmsEntityVersionRepository
    {
        Task AddAsync(CmsEntityVersion version);
        Task AddVersionAsync(CmsEntity entity, CmsEntityDto evt);
        Task<List<CmsEntityVersion>> GetByEntityIdAsync(string entityId);
        Task<bool> ExistsAsync(string entityId, int version);
    }
}
