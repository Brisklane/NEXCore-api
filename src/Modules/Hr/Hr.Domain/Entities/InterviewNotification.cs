
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class InterviewNotification : BaseEntity
{
    public Guid InterviewId { get; set; }
    public Guid? InterviewPanelMemberId { get; set; }
    public Guid RecipientEmployeeId { get; set; }
    public Guid NotificationTypeLookupValueId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public Guid DeliveryStatusLookupValueId { get; set; }
    public Guid? ResponseActionLookupValueId { get; set; }
    public DateTime? ResponseAt { get; set; }
    public string? Token { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public Interview? Interview { get; set; }
    public InterviewPanelMember? PanelMember { get; set; }
    public Employee? RecipientEmployee { get; set; }
    public LookupValue? NotificationTypeLookupValue { get; set; }
    public LookupValue? DeliveryStatusLookupValue { get; set; }
    public LookupValue? ResponseActionLookupValue { get; set; }
    public Guid? CommunicationTemplateId { get; set; }
    public CommunicationTemplate? CommunicationTemplate { get; set; }
}
