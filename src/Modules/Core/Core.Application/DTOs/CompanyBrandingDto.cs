namespace Core.Application.DTOs;

/// <summary>
/// Public, pre-login branding for a company subdomain ({slug}.{BaseDomain}). Returned by the
/// anonymous by-slug endpoint so the SPA can render the tenant's login page before authentication.
/// </summary>
public class CompanyBrandingDto
{
    public required string Slug { get; set; }
    public required string CompanyName { get; set; }
    public bool HasLogo { get; set; }
}

/// <summary>
/// Result of a company-slug availability check used during registration.
/// </summary>
public class SlugAvailabilityDto
{
    public required string Slug { get; set; }
    public bool Available { get; set; }
    /// <summary>null when available; otherwise one of: "invalid-format" | "reserved" | "taken".</summary>
    public string? Reason { get; set; }
}
