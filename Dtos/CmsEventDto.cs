using System.Text.Json;

namespace CMSApi.Dtos;

public class CmsEventDto
{
    public string Type { get; set; } = string.Empty; // publish, update, delete, unPublish
    public string Id { get; set; } = string.Empty;
    public JsonElement? Payload { get; set; } // can be null for delete
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
}