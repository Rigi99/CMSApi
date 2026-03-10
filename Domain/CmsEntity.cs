namespace CMSApi.Domain;

public class CmsEntity
{
    // Primary key
    public string Id { get; set; } = null!;

    // Entity name
    public string Name { get; set; } = null!;

    // Disabled flag
    public bool IsDisabled { get; set; } = false;

    // Navigation property: versions of this entity
    public ICollection<CmsEntityVersion> Versions { get; set; } = new List<CmsEntityVersion>();
}