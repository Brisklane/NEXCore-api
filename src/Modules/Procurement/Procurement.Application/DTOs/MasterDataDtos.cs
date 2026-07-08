using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Vendor Category ───────────────────────────────────────────────────────────

public class VendorCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateVendorCategoryDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateVendorCategoryDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

// ── Procurement Category ──────────────────────────────────────────────────────

public class ProcurementCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProcurementCategoryDto> Children { get; set; } = [];
}

public class CreateProcurementCategoryDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
}

public class UpdateProcurementCategoryDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool? IsActive { get; set; }
}

// ── Vendor Document ───────────────────────────────────────────────────────────

public class VendorDocumentDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public VendorDocumentType DocumentType { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool NeverExpires { get; set; }
    public VendorDocumentStatus Status { get; set; }
    public int ReminderDays { get; set; } = 30;
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysUntilExpiry { get; set; }
}

public class CreateVendorDocumentDto
{
    public VendorDocumentType DocumentType { get; set; }
    public required string DocumentName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool NeverExpires { get; set; }
    public int ReminderDays { get; set; } = 30;
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? Notes { get; set; }
}

public class UpdateVendorDocumentDto
{
    public string? DocumentName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool? NeverExpires { get; set; }
    public VendorDocumentStatus? Status { get; set; }
    public int? ReminderDays { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? Notes { get; set; }
}

// ── Vendor Pricelist ──────────────────────────────────────────────────────────

public class VendorPricelistDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public List<VendorPricelistItemDto> Items { get; set; } = [];
}

public class VendorPricelistItemDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemDescription { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? MinimumQuantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class CreateVendorPricelistDto
{
    public required string Name { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public List<CreateVendorPricelistItemDto> Items { get; set; } = [];
}

public class CreateVendorPricelistItemDto
{
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemDescription { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? MinimumQuantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

// ── Approval Workflow ─────────────────────────────────────────────────────────

public class ApprovalWorkflowDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ApprovalDocumentType DocumentType { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaximumAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int Priority { get; set; }
    public List<ApprovalWorkflowStepDto> Steps { get; set; } = [];
}

public class ApprovalWorkflowStepDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public Guid? ApproverUserId { get; set; }
    public string? ApproverRole { get; set; }
    public decimal? AmountThreshold { get; set; }
    public bool IsParallelStep { get; set; }
    public int RequiredApprovals { get; set; } = 1;
    public int EscalationAfterDays { get; set; } = 3;
    public Guid? EscalationUserId { get; set; }
    public bool IsOptional { get; set; }
    public string? Instructions { get; set; }
}

public class CreateApprovalWorkflowDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ApprovalDocumentType DocumentType { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaximumAmount { get; set; }
    public bool IsDefault { get; set; }
    public int Priority { get; set; } = 1;
    public List<ApprovalWorkflowStepDto> Steps { get; set; } = [];
}

// ── Document Sequence ─────────────────────────────────────────────────────────

public class DocumentSequenceDto
{
    public Guid Id { get; set; }
    public ProcurementDocumentType DocumentType { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public string? Suffix { get; set; }
    public string Separator { get; set; } = "-";
    public bool IncludeYear { get; set; } = true;
    public bool IncludeMonth { get; set; }
    public int SequencePadding { get; set; } = 5;
    public SequenceResetPeriod ResetOn { get; set; }
    public int NextSequenceNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateDocumentSequenceDto
{
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string? Separator { get; set; }
    public bool? IncludeYear { get; set; }
    public bool? IncludeMonth { get; set; }
    public int? SequencePadding { get; set; }
    public SequenceResetPeriod? ResetOn { get; set; }
    public bool? IsActive { get; set; }
}

// ── Procurement Settings ──────────────────────────────────────────────────────

public class ProcurementSettingsDto
{
    public Guid Id { get; set; }
    public bool RequireRequisitionForPO { get; set; }
    public decimal? RFQMandatoryAboveAmount { get; set; }
    public bool Enable3WayMatching { get; set; }
    public bool EnforceInvoicePOTolerance { get; set; }
    public decimal InvoicePOTolerancePercent { get; set; }
    public Nexcore.SharedKernel.Enums.PaymentTerms DefaultPaymentTerms { get; set; }
    public string DefaultCurrencyCode { get; set; } = "USD";
    public int DefaultLeadTimeDays { get; set; }
    public Guid? POApprovalWorkflowId { get; set; }
    public Guid? RequisitionApprovalWorkflowId { get; set; }
    public Guid? InvoiceApprovalWorkflowId { get; set; }
    public bool RequireVendorApproval { get; set; }
    public bool RequireVendorBankVerification { get; set; }
    public bool SendPOToVendorByEmail { get; set; }
    public bool SendRFQToVendorByEmail { get; set; }
    public int POApprovalReminderDays { get; set; }
}

public class UpdateProcurementSettingsDto
{
    public bool? RequireRequisitionForPO { get; set; }
    public decimal? RFQMandatoryAboveAmount { get; set; }
    public bool? Enable3WayMatching { get; set; }
    public bool? EnforceInvoicePOTolerance { get; set; }
    public decimal? InvoicePOTolerancePercent { get; set; }
    public Nexcore.SharedKernel.Enums.PaymentTerms? DefaultPaymentTerms { get; set; }
    public string? DefaultCurrencyCode { get; set; }
    public int? DefaultLeadTimeDays { get; set; }
    public Guid? POApprovalWorkflowId { get; set; }
    public Guid? RequisitionApprovalWorkflowId { get; set; }
    public Guid? InvoiceApprovalWorkflowId { get; set; }
    public bool? RequireVendorApproval { get; set; }
    public bool? RequireVendorBankVerification { get; set; }
    public bool? SendPOToVendorByEmail { get; set; }
    public bool? SendRFQToVendorByEmail { get; set; }
    public int? POApprovalReminderDays { get; set; }
}
