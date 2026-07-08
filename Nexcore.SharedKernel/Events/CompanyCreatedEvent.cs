namespace Nexcore.SharedKernel.Events;

/// <summary>
/// Domain event published when a new company is created
/// Allows other modules (like Accounting) to subscribe and initialize their data independently
/// This maintains loose coupling between modules
/// </summary>
public class CompanyCreatedEvent
{
    /// <summary>
    /// The newly created company ID
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// The first/default branch ID for the company
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// The first/default business unit ID for the company
    /// </summary>
    public Guid BusinessUnitId { get; set; }

    /// <summary>
    /// The user ID who created the company (for audit trail)
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Company name for logging/reference
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// When true, modules also seed sample/demo data (orders, invoices, contacts, items, etc.).
    /// When false (default) only essential master/config data is seeded (chart of accounts,
    /// templates, price lists, tax, sequences, POS store/terminals/cashiers…).
    /// </summary>
    public bool IncludeSampleData { get; set; }

    /// <summary>
    /// When the event was published
    /// </summary>
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
