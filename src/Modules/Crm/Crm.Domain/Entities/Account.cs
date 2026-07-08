using Nexcore.SharedKernel;
using Nexcore.SharedKernel.ValueObjects;
using System.Net.Mail;

namespace Crm.Domain.Entities
{
    public class Account : BaseEntity
    {
        // ─── Basic Info ────────────────────────────────────────────────
        public string AccountName { get; set; } = string.Empty;  // Required
        public string? Website { get; set; }
        public string? Phone { get; set; }
        public string? Type { get; set; }  // Dropdown: --None--, Analyst, Competitor, Customer, Integrator, Investor, Partner, Press, Prospect, Reseller, Other
        public string? Industry { get; set; }

        // ─── Billing Address ───────────────────────────────────────────
        public string? BillingStreet { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingState { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCountry { get; set; }

        // ─── Shipping Address ──────────────────────────────────────────
        public string? ShippingStreet { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCountry { get; set; }

        // ─── Hierarchy ─────────────────────────────────────────────────
        public Guid? ParentAccountId { get; set; }
        public Account? ParentAccount { get; set; }

        // ─── Ownership ─────────────────────────────────────────────────
        public Guid? OwnerId { get; set; }  // FK to User (external)

        // ─── Navigation ────────────────────────────────────────────────
        public ICollection<Account> ChildAccounts { get; set; } = new List<Account>();
        public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public ICollection<Deal> Deals { get; set; } = new List<Deal>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}