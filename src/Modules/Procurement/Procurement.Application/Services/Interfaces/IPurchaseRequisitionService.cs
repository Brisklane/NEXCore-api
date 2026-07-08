using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

public interface IPurchaseRequisitionService
{
    Task<PaginatedResponse<PurchaseRequisitionDto>> GetAllAsync(PaginationParams pagination);
    Task<PurchaseRequisitionDto?> GetByIdAsync(Guid id);
    Task<PurchaseRequisitionDto?> GetByNumberAsync(string requisitionNumber);
    Task<List<PurchaseRequisitionDto>> GetByStatusAsync(RequisitionStatus status);
    Task<List<PurchaseRequisitionDto>> GetByRequesterAsync(Guid requestedByUserId);
    Task<List<PurchaseRequisitionDto>> GetByDepartmentAsync(Guid departmentId);
    Task<List<PurchaseRequisitionDto>> GetPendingApprovalAsync();
    Task<PurchaseRequisitionDto> CreateAsync(CreatePurchaseRequisitionDto dto);
    Task<PurchaseRequisitionDto> UpdateAsync(Guid id, UpdatePurchaseRequisitionDto dto);
    Task<PurchaseRequisitionDto> SubmitAsync(Guid id);
    Task<PurchaseRequisitionDto> ApproveAsync(Guid id, Guid approvedByUserId);
    Task<PurchaseRequisitionDto> RejectAsync(Guid id, RejectRequisitionDto dto, Guid rejectedByUserId);
    Task<PurchaseRequisitionDto> CancelAsync(Guid id);

    /// <summary>Converts an approved requisition directly into a draft Purchase Order and returns it.</summary>
    Task<PurchaseOrderDto> ConvertToPurchaseOrderAsync(Guid id);

    Task DeleteAsync(Guid id);
}
