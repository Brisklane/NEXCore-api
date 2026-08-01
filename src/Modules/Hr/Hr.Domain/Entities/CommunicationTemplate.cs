using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CommunicationTemplate : BaseEntity
{
    // Note: the primary key is BaseEntity.Id. A separate CommunicationTemplateId column used to
    // sit here, but nothing read it and the seeder filled it with a throwaway GUID, so every
    // foreign key already pointed at Id. Removed rather than left as a second, meaningless key.

    /// <summary>
    /// Unique template code (EMAIL_OFFER, SMS_INTERVIEW_REMINDER)
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Email / SMS / WhatsApp / CallScript
    /// </summary>
    public Guid TemplateTypeLookupValueId { get; set; }

    /// <summary>
    /// Subject (used for email)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Body content (HTML/Text with placeholders)
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// JSON list of placeholders (for validation/UI)
    /// Example: ["CandidateName", "JobTitle", "InterviewDate"]
    /// </summary>
    public string? PlaceholdersJson { get; set; }

    /// <summary>
    /// Language support
    /// </summary>
    public string? LanguageCode { get; set; }

    /// <summary>
    /// Versioning support
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Is system template (locked)
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    // Navigation
    public LookupValue? TemplateTypeLookupValue { get; set; }

    public ICollection<InterviewNotification>? InterviewNotifications { get; set; }
    public ICollection<CallLog>? CallLogs { get; set; }
    public ICollection<OfferLetter>? OfferLetters { get; set; }
}
