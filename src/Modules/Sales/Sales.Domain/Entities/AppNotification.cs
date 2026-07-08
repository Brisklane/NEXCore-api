using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Push / in-app notification sent to a customer.
/// Covers order status updates, promotions, loyalty alerts, and delivery tracking.
/// Works for all CustomerTypes — AppConsumer (push/in-app), B2B (email/SMS), WalkIn (SMS).
/// </summary>
public class AppNotification : BaseEntity
{
    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid ContactId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Optional deep-link payload (e.g., order ID, product ID).</summary>
    public string? ActionPayload { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }

    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
}
