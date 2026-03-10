using CMSApi.Dtos;

namespace CMSApi.Services;

public interface ICmsEventProcessor
{
    Task ProcessEventsAsync(IEnumerable<CmsEventDto> events);
}