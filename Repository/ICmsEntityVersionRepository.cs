using CMSApi.Domain;

namespace CMSApi.Repository
{
    public interface ICmsEntityVersionRepository
    {
        Task AddAsync(CmsEntityVersion version);
        Task<List<CmsEntityVersion>> GetByEntityIdAsync(string entityId);
    }
}
