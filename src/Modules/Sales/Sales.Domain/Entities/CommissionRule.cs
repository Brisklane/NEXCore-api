using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Commission Rule - defines how a sales rep earns commission.
///
/// Rules are evaluated per order line. The first matching rule wins.
///
/// Aligned with:
///   Dynamics 365 Sales - Sales Commission
///   SAP   - Rebate / Commission Condition Records
///   Odoo  - Sale Commission (OCA add-on / enterprise)
/// </summary>
public class CommissionRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Lower number = evaluated first.</summary>
    public int Priority { get; set; } = 10;

    // ? Match Criteria
    /// <summary>Specific sales rep user this rule applies to. Null = all reps.</summary>
    public Guid? SalesRepId { get; set; }

    /// <summary>Sales territory restriction. Null = all territories.</summary>
    public Guid? SalesTerritoryId { get; set; }

    /// <summary>Customer group restriction. Null = all groups.</summary>
    public Guid? CustomerGroupId { get; set; }

    /// <summary>Product category restriction (cross-module Guid). Null = all categories.</summary>
    public Guid? ProductCategoryId { get; set; }

    /// <summary>Specific product restriction. Null = all products.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Sales channel restriction. Null = all channels.</summary>
    public SalesChannel? SalesChannel { get; set; }

    /// <summary>Minimum order total to qualify for this rule.</summary>
    public decimal? MinOrderAmount { get; set; }

    // ? Rate
    public CommissionBasis Basis { get; set; } = CommissionBasis.PercentageOfNet;

    /// <summary>
    /// Rate value - percentage (e.g., 3.5 for 3.5%) or fixed amount depending on Basis.
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>Cap the maximum commission per order. Null = no cap.</summary>
    public decimal? MaxCommissionAmount { get; set; }

    // ? Validity
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// When commission is recognized: OnOrderPlace, OnInvoice, OnPayment, OnDelivery.
    /// </summary>
    public string RecognitionEvent { get; set; } = "OnInvoice";
}
