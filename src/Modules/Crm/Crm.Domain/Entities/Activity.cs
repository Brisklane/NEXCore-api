using Crm.Domain.Enums;
using Nexcore.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Activity : BaseEntity
    {
        // ─── Type ──────────────────────────────────────────────────────
        public string Type { get; set; } = "Task";  // Task, Event, Call, Email, Log a Call

        // ─── Subject ───────────────────────────────────────────────────
        public string Subject { get; set; } = string.Empty;

        // ─── Dates ─────────────────────────────────────────────────────
        public DateTime? DueDate { get; set; }      // For Task
        public DateTime? StartDateTime { get; set; } // For Event/Call
        public DateTime? EndDateTime { get; set; }   // For Event/Call

        // ─── Status ────────────────────────────────────────────────────
        public string? Status { get; set; }  // Not Started, In Progress, Completed, Waiting, Deferred
        public string? Priority { get; set; } // Low, Normal, High

        // ─── Call Details ──────────────────────────────────────────────
        public int? DurationMinutes { get; set; }
        public string? CallType { get; set; }     // Inbound, Outbound
        public string? CallPurpose { get; set; }
        public string? CallResult { get; set; }

        // ─── Description ───────────────────────────────────────────────
        public string? Comments { get; set; }

        // ─── Linked To (Related To) ────────────────────────────────────
        public Guid? RelatedToId { get; set; }     // Account, Deal, Case, Campaign
        public string? RelatedToType { get; set; } // "Account", "Deal", "Case", "Campaign"

        // ─── Name (Contact or Lead) ────────────────────────────────────
        public Guid? NameId { get; set; }     // Contact or Lead
        public string? NameType { get; set; } // "Contact" or "Lead"

        // ─── Ownership ─────────────────────────────────────────────────
        public Guid? AssignedToId { get; set; }  // FK to User

        // ─── Navigation (helpers for EF Core) ──────────────────────────
        public Lead? Lead { get; set; }
        public Contact? Contact { get; set; }
        public Account? Account { get; set; }
        public Deal? Deal { get; set; }
        public Case? Case { get; set; }
        public Campaign? Campaign { get; set; }
    }
}
