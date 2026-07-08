using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// KYC and onboarding data for a marketplace / home-chef seller.
/// Linked 1:1 with <see cref="PosStore"/> via <see cref="StoreId"/>.
/// Admin-created stores do not need this record.
/// </summary>
public class StoreVendorProfile : BaseEntity
{
    public Guid StoreId { get; set; }

    // ─── Owner / Seller Identity ─────────────────────────────────────────────
    public string OwnerName { get; set; } = string.Empty;
    /// <summary>Pakistani CNIC — 13 digits without dashes.</summary>
    public string? OwnerCnic { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerEmail { get; set; }
    /// <summary>URL to uploaded front-side CNIC scan.</summary>
    public string? CnicFrontDocUrl { get; set; }
    /// <summary>URL to uploaded back-side CNIC scan.</summary>
    public string? CnicBackDocUrl { get; set; }

    // ─── Business Details ────────────────────────────────────────────────────
    /// <summary>Legal or trading business name (may differ from store trading name).</summary>
    public string? BusinessName { get; set; }
    /// <summary>SECP / FBR registration number for formal businesses. Optional for home chefs.</summary>
    public string? BusinessRegistrationNumber { get; set; }
    /// <summary>Local authority food / safety license number.</summary>
    public string? FoodLicenseNumber { get; set; }
    public DateOnly? FoodLicenseExpiry { get; set; }
    /// <summary>URL to uploaded food license document.</summary>
    public string? FoodLicenseDocUrl { get; set; }
    /// <summary>Brief description of what the seller offers (shown in onboarding review).</summary>
    public string? BusinessDescription { get; set; }

    // ─── Banking / Payout ────────────────────────────────────────────────────
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? AccountTitle { get; set; }
    public string? AccountNumber { get; set; }
    /// <summary>IBAN (24-character PK format).</summary>
    public string? IbanNumber { get; set; }

    // ─── Social Media ────────────────────────────────────────────────────────
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TiktokUrl { get; set; }
    /// <summary>WhatsApp number used for customer contact.</summary>
    public string? WhatsappNumber { get; set; }

    // ─── Kitchen / Store Photos ──────────────────────────────────────────────
    /// <summary>URL to the main storefront / entrance photo.</summary>
    public string? StoreFrontPhotoUrl { get; set; }
    /// <summary>JSON array of kitchen photo URLs, e.g. ["https://...","https://..."].</summary>
    public string? KitchenPhotosJson { get; set; }

    // ─── Onboarding Review ───────────────────────────────────────────────────
    public VendorOnboardingStatus OnboardingStatus { get; set; } = VendorOnboardingStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    /// <summary>UserId of the platform reviewer who approved/rejected the application.</summary>
    public Guid? ReviewedByUserId { get; set; }
    /// <summary>Internal notes from the reviewer (not shown to seller).</summary>
    public string? ReviewNotes { get; set; }
    /// <summary>Rejection reason shown to the seller so they can correct and reapply.</summary>
    public string? RejectionReason { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────────
    public PosStore? Store { get; set; }
}
