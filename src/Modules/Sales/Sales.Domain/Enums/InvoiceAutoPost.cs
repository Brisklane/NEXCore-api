namespace Sales.Domain.Enums;

/// <summary>
/// Controls whether and when a draft invoice is automatically posted (confirmed).
/// Mirrors Odoo's account.move.auto_post field.
/// </summary>
public enum InvoiceAutoPost
{
    /// <summary>Manual posting only — user must click Confirm.</summary>
    No = 0,

    /// <summary>Post automatically on the Invoice Date (accounting date).</summary>
    AtDate = 1,

    /// <summary>Post immediately upon creation.</summary>
    Immediately = 2,

    /// <summary>Recurring: auto-post monthly and create the next draft.</summary>
    Monthly = 3,

    /// <summary>Recurring: auto-post quarterly and create the next draft.</summary>
    Quarterly = 4,

    /// <summary>Recurring: auto-post yearly and create the next draft.</summary>
    Yearly = 5,
}
