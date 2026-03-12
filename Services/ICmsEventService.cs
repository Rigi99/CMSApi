using CMSApi.Dtos;

namespace CMSApi.Services;

public interface ICmsEventService
{
    Task ProcessEventsAsync(IEnumerable<CmsEventDto> events);
}