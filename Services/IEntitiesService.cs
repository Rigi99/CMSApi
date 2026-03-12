using CMSApi.Domain;

namespace CMSApi.Services
{
    public interface IEntitiesService
    {
        Task<List<CmsEntity>> GetEnabledEntitiesAsync();
        Task<List<CmsEntity>> GetAllEntitiesAsync();
        Task DisableEntityAsync(string id);
    }
}
