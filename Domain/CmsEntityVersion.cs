namespace CMSApi.Domain;

public class CmsEntityVersion
{
    // Primary key
    public int Id { get; set; }

    // Foreign key pointing to CmsEntity
    public string CmsEntityId { get; set; } = null!;

    // Navigation property
    public CmsEntity CmsEntity { get; set; } = null!;

    // Version number
    public int Version { get; set; }

    // Timestamp of the version
    public DateTime Timestamp { get; set; }

    // JSON serialized payload
    public string? Payload { get; set; }
}