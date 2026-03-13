namespace CMSApi.Authentication;

public class BasicAuthOptions
{
    public string BasicUsername { get; set; } = string.Empty;
    public string BasicPassword { get; set; } = string.Empty;
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}