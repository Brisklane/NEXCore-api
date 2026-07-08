using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

// ── Vendor Category ───────────────────────────────────────────────────────────

public interface IVendorCategoryService
{
    Task<List<VendorCategoryDto>> GetAllAsync();
    Task<List<VendorCategoryDto>> GetActiveAsync();
    Task<VendorCategoryDto?> GetByIdAsync(Guid id);
    Task<VendorCategoryDto> CreateAsync(CreateVendorCategoryDto dto);
    Task<VendorCategoryDto> UpdateAsync(Guid id, UpdateVendorCategoryDto dto);
    Task DeleteAsync(Guid id);
}

// ── Procurement Category ──────────────────────────────────────────────────────

public interface IProcurementCategoryService
{
    Task<List<ProcurementCategoryDto>> GetAllAsync();
    Task<List<ProcurementCategoryDto>> GetActiveAsync();
    Task<List<ProcurementCategoryDto>> GetTreeAsync();
    Task<ProcurementCategoryDto?> GetByIdAsync(Guid id);
    Task<ProcurementCategoryDto> CreateAsync(CreateProcurementCategoryDto dto);
    Task<ProcurementCategoryDto> UpdateAsync(Guid id, UpdateProcurementCategoryDto dto);
    Task DeleteAsync(Guid id);
}

// ── Vendor Document ───────────────────────────────────────────────────────────

public interface IVendorDocumentService
{
    Task<List<VendorDocumentDto>> GetByVendorAsync(Guid vendorId);
    Task<List<VendorDocumentDto>> GetExpiringAsync(int withinDays = 30);
    Task<VendorDocumentDto?> GetByIdAsync(Guid id);
    Task<VendorDocumentDto> CreateAsync(Guid vendorId, CreateVendorDocumentDto dto);
    Task<VendorDocumentDto> UpdateAsync(Guid id, UpdateVendorDocumentDto dto);
    Task<VendorDocumentDto> VerifyAsync(Guid id, Guid verifiedByUserId);
    Task DeleteAsync(Guid id);
}

// ── Vendor Pricelist ──────────────────────────────────────────────────────────

public interface IVendorPricelistService
{
    Task<List<VendorPricelistDto>> GetByVendorAsync(Guid vendorId);
    Task<VendorPricelistDto?> GetByIdAsync(Guid id);
    Task<VendorPricelistDto> CreateAsync(Guid vendorId, CreateVendorPricelistDto dto);
    Task<VendorPricelistDto> UpdateAsync(Guid id, CreateVendorPricelistDto dto);
    Task DeleteAsync(Guid id);
}

// ── Approval Workflow ─────────────────────────────────────────────────────────

public interface IApprovalWorkflowService
{
    Task<List<ApprovalWorkflowDto>> GetAllAsync();
    Task<List<ApprovalWorkflowDto>> GetByDocumentTypeAsync(ApprovalDocumentType documentType);
    Task<ApprovalWorkflowDto?> GetByIdAsync(Guid id);
    Task<ApprovalWorkflowDto> CreateAsync(CreateApprovalWorkflowDto dto);
    Task<ApprovalWorkflowDto> UpdateAsync(Guid id, CreateApprovalWorkflowDto dto);
    Task DeleteAsync(Guid id);
}

// ── Document Sequence ─────────────────────────────────────────────────────────

public interface IDocumentSequenceManagementService
{
    Task<List<DocumentSequenceDto>> GetAllAsync();
    Task<DocumentSequenceDto?> GetByDocumentTypeAsync(ProcurementDocumentType documentType);
    Task<DocumentSequenceDto> UpdateAsync(Guid id, UpdateDocumentSequenceDto dto);
}

// ── Procurement Settings ──────────────────────────────────────────────────────

public interface IProcurementSettingsService
{
    Task<ProcurementSettingsDto?> GetAsync();
    Task<ProcurementSettingsDto> UpdateAsync(UpdateProcurementSettingsDto dto);
}
