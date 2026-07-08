namespace Nexcore.SharedKernel;

/// <summary>
/// Application-wide constants shared across modules — default values and the field-length
/// limits that keep validation attributes and EF column sizes in agreement.
/// </summary>
public static class AppConstants
{
    /// <summary>Currency assumed when a company/document does not specify one.</summary>
    public const string DefaultCurrency = "USD";

    // ── Maximum field lengths (mirror these in EF configuration and validators) ──
    public const int MaxCodeLength = 50;
    public const int MaxNameLength = 255;
    public const int MaxEmailLength = 255;
    public const int MaxDescriptionLength = 2000;

    // ── Password policy ──
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 255;

    /// <summary>
    /// All-zeros GUID used as the audit stamp for system/seed writes that have no
    /// signed-in user (migrations, background jobs, reference-data seeding).
    /// </summary>
    public const string CreatedUserId = "00000000-0000-0000-0000-000000000000";
}
