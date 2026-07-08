using Procurement.Application.DTOs;

namespace Procurement.Application.Services.Interfaces;

/// <summary>Read-only analytics aggregations across procurement documents.</summary>
public interface IProcurementReportsService
{
    Task<PurchaseAnalysisDto> GetPurchaseAnalysisAsync();
    Task<VendorAnalysisDto> GetVendorAnalysisAsync();
    Task<ApAgingDto> GetApAgingAsync();
    Task<ThreeWayMatchDto> GetThreeWayMatchAsync();
    Task<SpendByCategoryDto> GetSpendByCategoryAsync();
}
