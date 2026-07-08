using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// POS Cashier profile - links an HR Employee to POS permissions and a home store.
///
/// Cross-module rule: never navigate into Hr.Domain.Employee.
/// Reference by <c>EmployeeId</c> (Guid FK only).
/// The inherited <c>BranchId</c> from <see cref="BaseEntity"/> identifies
/// which branch this cashier belongs to.
/// </summary>
public class PosCashier : BaseEntity
{
    // ? HR Link
    /// <summary>
    /// FK to Hr.Domain.Entities.Employee - referenced by ID only.
    /// Never navigate across module boundaries.
    /// </summary>
    public Guid EmployeeId { get; set; }

    // ? Identity snapshot
    /// <summary>
    /// Denormalized employee name - kept here so POS receipts and reports
    /// work even if HR data is unavailable (offline POS scenario).
    /// Refreshed when the cashier record is created or explicitly synced.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? PinCode { get; set; }        // hashed 4-6 digit PIN for quick login
    public string? BadgeNumber { get; set; }

    // ? Assignment
    /// <summary>Primary store this cashier is assigned to.</summary>
    public Guid PosStoreId { get; set; }
    public PosStore PosStore { get; set; } = null!;

    // ? Permissions
    public bool CanApplyManualDiscount { get; set; }
    public decimal MaxManualDiscountPercentage { get; set; }

    public bool CanVoidTransaction { get; set; }
    public bool CanIssueRefund { get; set; }
    public bool CanOpenDrawer { get; set; }
    public bool CanOverridePrices { get; set; }
    public bool CanApplyCoupons { get; set; }
    public bool CanAccessReports { get; set; }

    // ? Navigation
    public ICollection<PosSession> Sessions { get; set; } = new List<PosSession>();
}
