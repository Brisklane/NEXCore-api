using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;

namespace Accounting.Domain.Entities;

/// <summary>
/// Budget entity - budget amounts by account and fiscal period
/// Enables variance analysis (Actual vs Budget)
/// Critical for forecasting and financial planning
/// </summary>
public class Budget : BaseEntity
{
    /// <summary>
    /// Ledger account reference
    /// The GL account this budget is for
    /// </summary>
    public Guid LedgerAccountId { get; set; }

    /// <summary>
    /// Fiscal period reference
    /// The period this budget applies to
    /// </summary>
    public Guid FiscalPeriodId { get; set; }

    /// <summary>
    /// Budgeted amount for this account and period
    /// Used for variance analysis
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Budget type - differentiates between different versions
    /// Examples: "Annual", "Revised", "Rolling", "Forecast"
    /// </summary>
    public BudgetType BudgetType { get; set; } = BudgetType.Annual;

    /// <summary>
    /// Budget variance percentage threshold
    /// Used to flag variances that exceed acceptable limits
    /// Example: 0.10 = 10% variance threshold
    /// </summary>
    public decimal? VarianceThresholdPercentage { get; set; }

    /// <summary>
    /// Whether this budget amount is final/locked
    /// Locked budgets cannot be modified
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// User who locked the budget (if applicable)
    /// </summary>
    public Guid? LockedByUserId { get; set; }

    /// <summary>
    /// When the budget was locked
    /// </summary>
    public DateTime? LockedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Navigation to ledger account
    /// </summary>
    public LedgerAccount? LedgerAccount { get; set; }

    /// <summary>
    /// Navigation to fiscal period
    /// </summary>
    public FiscalPeriod? FiscalPeriod { get; set; }
}
