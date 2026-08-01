using Sales.Domain.Enums;

namespace Sales.Application.DTOs;

/// <summary>
/// Sales quota for a rep, team or territory. Moved here from CRM so quota has one owner —
/// see <c>Sales.Domain.Entities.SalesTarget</c> for why.
/// </summary>
public class SalesTargetDto
{
    public Guid Id { get; set; }

    public Guid? SalesRepId { get; set; }
    public Guid? SalesTeamId { get; set; }
    public Guid? SalesTerritoryId { get; set; }

    public TargetPeriod Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int FiscalYear { get; set; }
    public int? FiscalQuarter { get; set; }
    public int? FiscalMonth { get; set; }

    public decimal TargetAmount { get; set; }
    public decimal? TargetQuantity { get; set; }
    public int? TargetDealsCount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal ActualAmount { get; set; }
    public decimal? ActualQuantity { get; set; }
    public int? ActualDealsCount { get; set; }

    /// <summary>Computed by the domain: ActualAmount / TargetAmount × 100.</summary>
    public decimal AttainmentPercentage { get; set; }

    public string? Notes { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSalesTargetDto
{
    public Guid? SalesRepId { get; set; }
    public Guid? SalesTeamId { get; set; }
    public Guid? SalesTerritoryId { get; set; }

    public TargetPeriod Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int FiscalYear { get; set; }
    public int? FiscalQuarter { get; set; }
    public int? FiscalMonth { get; set; }

    public decimal TargetAmount { get; set; }
    public decimal? TargetQuantity { get; set; }
    public int? TargetDealsCount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string? Notes { get; set; }
    public string? Description { get; set; }
}

public class UpdateSalesTargetDto : CreateSalesTargetDto { }
