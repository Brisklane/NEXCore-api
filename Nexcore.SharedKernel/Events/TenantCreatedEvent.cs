namespace Nexcore.SharedKernel.Events;

/// <summary>
/// Published when a new tenant is created (always before company creation).
/// Allows modules to perform tenant-level initialization if needed.
/// </summary>
public class TenantCreatedEvent
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
