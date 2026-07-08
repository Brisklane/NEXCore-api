using Nexcore.SharedKernel;
using System.Net.Mail;

namespace Crm.Domain.Entities
{
    public class Case : BaseEntity
    {
        // ─── Basic Info ────────────────────────────────────────────────
        public string CaseNumber { get; set; } = string.Empty;  // Auto-generated

        // ─── Classification ────────────────────────────────────────────
        public string Status { get; set; } = "New";  // Dropdown: New, Working, Waiting on Customer, Escalated, Closed
        public string? CaseOrigin { get; set; }  // Dropdown: --None--, Phone, Email, Web, Chat
        public string Priority { get; set; } = "Medium";  // Dropdown: Low, Medium, High, Critical

        // ─── Contact Info ──────────────────────────────────────────────
        public Guid? ContactId { get; set; }  // Search Contacts
        public Contact? Contact { get; set; }

        public Guid? AccountId { get; set; }  // Search Accounts
        public Account? Account { get; set; }

        // ─── Entitlement ───────────────────────────────────────────────
        public Guid? EntitlementId { get; set; }
        public Entitlement? Entitlement { get; set; }

        // ─── Description ───────────────────────────────────────────────
        public string? Subject { get; set; }

        // ─── Notification ──────────────────────────────────────────────
        public bool SendNotificationEmail { get; set; }

        // ─── Ownership ─────────────────────────────────────────────────
        public Guid? OwnerId { get; set; }

        // ─── Navigation ────────────────────────────────────────────────
        public ICollection<CaseComment> Comments { get; set; } = new List<CaseComment>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}