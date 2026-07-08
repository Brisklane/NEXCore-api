using Crm.Domain.Enums;
using Nexcore.SharedKernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Lead : BaseEntity
    {
        // ─── Name ──────────────────────────────────────────────────────
        public string? Salutation { get; set; }        // Mr., Mrs., Dr., etc.
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // ─── Company Info ──────────────────────────────────────────────
        public string Company { get; set; } = string.Empty;  // Required
        public string? Title { get; set; }                   // Job title
        public string? Website { get; set; }

        // ─── Contact ───────────────────────────────────────────────────
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // ─── Address ───────────────────────────────────────────────────
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // ─── Segment ───────────────────────────────────────────────────
        public int? NumberOfEmployees { get; set; }
        public decimal? AnnualRevenue { get; set; }
        public string? LeadSource { get; set; }     // Dropdown: Advertisement, Employee Referral, External Referral, Partner, Public Relations, Seminar - Internal, Seminar - Partner, Trade Show, Web, Word of mouth, Other
        public string? Industry { get; set; }       // Long dropdown list

        // ─── Management ────────────────────────────────────────────────
        public string Status { get; set; } = "New"; // Required dropdown: New, Contacted, Nurturing, Unqualified, Converted
        public Guid? OwnerId { get; set; }          // FK to User (external)
        public Guid? AssignedEmployeeId { get; set; } // FK to HR Employee

        public bool EmailOptOut { get; set; }

        // ─── Conversion ────────────────────────────────────────────────
        public bool IsConverted { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public Guid? ConvertedAccountId { get; set; }
        public Guid? ConvertedContactId { get; set; }
        public Guid? ConvertedDealId { get; set; }

        // ─── Navigation ────────────────────────────────────────────────
        public Account? ConvertedAccount { get; set; }
        public Contact? ConvertedContact { get; set; }
        public Deal? ConvertedDeal { get; set; }

        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
