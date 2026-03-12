using CMSApi.Dtos;

namespace CMSApi.Services;

public interface ICmsEntityService
{
    Task ProcessEventsAsync(IEnumerable<CmsEntityDto> events);
}