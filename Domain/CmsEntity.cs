namespace CMSApi.Domain;

public class CmsEntity
{
    // Primary key
    public string Id { get; set; } = null!;

    // Disabled flag
    public bool IsDisabled { get; set; } = false;

    // Latest version
    public int LatestVersion { get; set; }

    // Navigation property: versions of this entity
    public ICollection<CmsEntityVersion> Versions { get; set; } = [];
}