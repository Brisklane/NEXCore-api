using Crm.Domain.Enums;
using Nexcore.SharedKernel;

namespace Crm.Domain.Entities;

/// <summary>
/// Unified person master for CRM and all commerce channels.
///
/// In CRM context: a contact linked to an Account (B2B person, lead convert).
/// In Sales/POS context: the customer on every SalesOrder, Invoice, and Payment.
///
/// ┌─────────────────────────────────────────────────────────────────────┐
/// │  CustomerType   │ Channel          │ Key fields used                │
/// ├─────────────────────────────────────────────────────────────────────┤
/// │  WalkIn         │ POS counter      │ Name, Phone (optional)         │
/// │  AppConsumer    │ Mobile/Web app   │ Phone, Email, Loyalty, Addresses│
/// │  B2B            │ Field/Portal/POS │ CreditLimit, PaymentTerms      │
/// │  Wholesale      │ Portal/Field     │ PriceList, SalesAgreement      │
/// │  Internal       │ Warehouse        │ internal transfers             │
/// └─────────────────────────────────────────────────────────────────────┘
/// </summary>
public class Contact : BaseEntity
{
    // ─── CRM Identity ──────────────────────────────────────────────────────
    public string? Salutation { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Full display name (B2B company name or "First Last").</summary>
    public string? FullName { get; set; }

    public string? ShortName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    // ─── Account Link (B2B / CRM) ──────────────────────────────────────────
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }

    // ─── Professional ──────────────────────────────────────────────────────
    public string? Title { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }

    // ─── Reporting Hierarchy ───────────────────────────────────────────────
    public Guid? ReportsToId { get; set; }
    public Contact? ReportsTo { get; set; }

    // ─── Contact Info ──────────────────────────────────────────────────────
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // ─── Commerce Identity ─────────────────────────────────────────────────
    /// <summary>Auto-generated customer number (e.g., CUS-000001). Set when used in sales.</summary>
    public string? CustomerNumber { get; set; }

    /// <summary>How this contact purchases — drives POS, app, and B2B behaviour.</summary>
    public CustomerType CustomerType { get; set; } = CustomerType.WalkIn;

    public string? TaxRegistrationNumber { get; set; }
    public string? NationalId { get; set; }

    // ─── App / Consumer ────────────────────────────────────────────────────
    /// <summary>Phone verified via OTP (AppConsumer only).</summary>
    public bool IsVerified { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? PushToken { get; set; }
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool MarketingOptIn { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredCurrencyCode { get; set; }

    // ─── Mailing / Billing Address ─────────────────────────────────────────
    public string? MailingStreet { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingState { get; set; }
    public string? MailingPostalCode { get; set; }
    public string? MailingCountry { get; set; }

    // ─── Financial Defaults (B2B / Wholesale) ──────────────────────────────
    /// <summary>ISO 4217 default currency, e.g., "USD", "PKR".</summary>
    public string DefaultCurrencyCode { get; set; } = "USD";

    /// <summary>Payment terms string, e.g., "Net30", "Net60", "Immediate".</summary>
    public string? DefaultPaymentTerms { get; set; }

    /// <summary>FK to Sales.PriceList — cross-module Guid, no navigation.</summary>
    public Guid? DefaultPriceListId { get; set; }

    // ─── Credit Management (B2B / Wholesale) ───────────────────────────────
    public decimal CreditLimit { get; set; }
    public decimal OutstandingBalance { get; set; }
    public CustomerCreditStatus CreditStatus { get; set; } = CustomerCreditStatus.Good;
    public bool AllowCreditSales { get; set; } = true;

    // ─── Loyalty ───────────────────────────────────────────────────────────
    /// <summary>FK to Sales.LoyaltyAccount — cross-module Guid, no navigation.</summary>
    public Guid? LoyaltyAccountId { get; set; }

    // ─── Preferences ───────────────────────────────────────────────────────
    public bool EmailOptOut { get; set; }

    // ─── Status / Flags ────────────────────────────────────────────────────
    public bool IsAnonymous { get; set; }
    public bool IsTaxExempt { get; set; }

    // ─── Ownership ─────────────────────────────────────────────────────────
    public Guid? OwnerId { get; set; }

    // ─── Conversion Link ───────────────────────────────────────────────────
    public Guid? ConvertedFromLeadId { get; set; }
    public Lead? ConvertedFromLead { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────
    public ICollection<Contact> DirectReports { get; set; } = new List<Contact>();
    public ICollection<ContactAddress> Addresses { get; set; } = new List<ContactAddress>();
    public ICollection<Deal> Deals { get; set; } = new List<Deal>();
    public ICollection<Case> Cases { get; set; } = new List<Case>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}