using CMSApi.Domain;
using CMSApi.Dtos;
using CMSApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CMSApi.Services;

public class CmsEventProcessor(ApplicationDbContext db) : ICmsEventProcessor
{
    private readonly ApplicationDbContext _db = db;

    public async Task ProcessEventsAsync(IEnumerable<CmsEventDto> events)
    {
        foreach (var evt in events)
        {
            var entity = _db.CmsEntities.Local.FirstOrDefault(e => e.Id == evt.Id)
                         ?? await _db.CmsEntities
                                     .Include(e => e.Versions)
                                     .FirstOrDefaultAsync(e => e.Id == evt.Id);

            switch (evt.Type.ToLower())
            {
                case "publish":
                case "update":
                    if (entity == null)
                    {
                        entity = new CmsEntity
                        {
                            Id = evt.Id,
                            Name = evt.Payload?.Name ?? evt.Id
                        };
                        _db.CmsEntities.Add(entity);
                    }
                    else
                    {
                        entity.Name = evt.Payload?.Name ?? entity.Name;
                        entity.IsDisabled = false;
                    }

                    var version = new CmsEntityVersion
                    {
                        CmsEntity = entity,
                        Version = evt.Version,
                        Timestamp = evt.Timestamp,
                        Payload = evt.Payload == null ? "{}" : JsonSerializer.Serialize(evt.Payload)
                    };
                    _db.CmsEntityVersions.Add(version);
                    break;

                case "unpublish":
                    if (entity != null)
                    {
                        entity.IsDisabled = true;
                        if (evt.Payload != null)
                            entity.Name = evt.Payload.Name ?? entity.Name;
                    }
                    break;

                case "delete":
                    if (entity != null)
                        _db.CmsEntities.Remove(entity);
                    break;
            }
        }

        await _db.SaveChangesAsync();
    }
}