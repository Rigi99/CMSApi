using CMSApi.Domain;

namespace CMSApi.Services
{
    public interface IEntityService
    {
        Task<List<CmsEntity>> GetEnabledEntitiesAsync();
        Task<List<CmsEntity>> GetAllEntitiesAsync();
        Task DisableEntityAsync(string id);
    }
}
