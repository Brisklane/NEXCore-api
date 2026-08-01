using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Sales target — the quota assigned to a sales rep, team or territory for a period.
///
/// Used to track attainment and feed commission acceleration tiers (e.g. 120% quota = +1% rate).
///
/// Aligned with:
///   Dynamics 365 Sales — Sales Goal
///   Salesforce         — Forecasting Quota
///   SAP                — Sales quota (InfoStructure S001/S002)
///
/// <para>
/// This is the single system of record for quota. CRM previously carried a second SalesTarget
/// table for forecasting; two records of the same quota meant commission (Sales) and forecast
/// attainment (CRM) could disagree for one rep, so the concept lives here — next to
/// CommissionRule, SalesTeam and SalesTerritory — and CRM reads it. CRM still owns
/// <c>Forecast</c>, which is a genuinely different concept (pipeline vs. quota).
/// </para>
/// </summary>
public class SalesTarget : BaseEntity
{
    // ── Who / What ────────────────────────────────────────────────────────────
    /// <summary>Sales rep this target belongs to. Null for team- or territory-level targets.</summary>
    public Guid? SalesRepId { get; set; }

    /// <summary>Team-level target. Null for rep- or territory-level targets.</summary>
    public Guid? SalesTeamId { get; set; }

    /// <summary>Territory-level target. Null for rep- or team-level targets.</summary>
    public Guid? SalesTerritoryId { get; set; }

    // ── Period ────────────────────────────────────────────────────────────────
    public TargetPeriod Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    /// <summary>Fiscal year the period falls in, for reporting and roll-ups.</summary>
    public int FiscalYear { get; set; }

    /// <summary>1-4, or null when the target is not quarterly.</summary>
    public int? FiscalQuarter { get; set; }

    /// <summary>1-12, or null when the target is not monthly.</summary>
    public int? FiscalMonth { get; set; }

    // ── Target Values ─────────────────────────────────────────────────────────
    public decimal TargetAmount { get; set; }
    public decimal? TargetQuantity { get; set; }

    /// <summary>Deal-count quota, for reps measured on volume rather than value.</summary>
    public int? TargetDealsCount { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    // ── Actuals ───────────────────────────────────────────────────────────────
    public decimal ActualAmount { get; set; }
    public decimal? ActualQuantity { get; set; }

    /// <summary>Deals closed against <see cref="TargetDealsCount"/>.</summary>
    public int? ActualDealsCount { get; set; }

    /// <summary>Attainment % = ActualAmount / TargetAmount � 100.</summary>
    public decimal AttainmentPercentage => TargetAmount > 0
        ? Math.Round(ActualAmount / TargetAmount * 100, 2)
        : 0;

    public string? Notes { get; set; }
}
