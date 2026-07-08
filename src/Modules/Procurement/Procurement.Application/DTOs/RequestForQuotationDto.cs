using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── RFQ Read ──────────────────────────────────────────────────────────────────

public class RequestForQuotationDto
{
    public Guid Id { get; set; }
    public string RFQNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid? RequisitionId { get; set; }
    public string? RequisitionNumber { get; set; }

    public DateTime IssueDate { get; set; }
    public DateTime SubmissionDeadline { get; set; }
    public DateTime? QuotationValidityDate { get; set; }
    public DateTime? AwardedAt { get; set; }

    public RFQStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public Guid? DeliveryAddressId { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? EvaluationCriteria { get; set; }
    public string? Notes { get; set; }

    public List<RFQLineDto> Lines { get; set; } = [];
    public List<RFQVendorDto> InvitedVendors { get; set; } = [];
    public List<VendorQuotationDto> VendorQuotations { get; set; } = [];
}

public class RFQLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? RequisitionLineId { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public Guid? ProcurementCategoryId { get; set; }
    public string? ProcurementCategoryName { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }
    public string? Specifications { get; set; }
    public string? Notes { get; set; }
}

public class RFQVendorDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public RFQVendorStatus Status { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? Notes { get; set; }
}

public class VendorQuotationDto
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string? VendorQuotationReference { get; set; }

    public Guid RFQId { get; set; }
    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }

    public DateTime SubmissionDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public QuotationStatus Status { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal SubTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public PaymentTerms PaymentTerms { get; set; }
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }
    public int DeliveryLeadTimeDays { get; set; }

    public decimal? TechnicalScore { get; set; }
    public decimal? CommercialScore { get; set; }
    public decimal? OverallScore { get; set; }
    public bool IsRecommended { get; set; }

    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }

    public List<VendorQuotationLineDto> Lines { get; set; } = [];
}

public class VendorQuotationLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid RFQLineId { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public int? LeadTimeDays { get; set; }
    public bool IsAlternative { get; set; }
    public string? Notes { get; set; }
}

// ── RFQ Create ────────────────────────────────────────────────────────────────

public class CreateRFQDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Guid? RequisitionId { get; set; }

    public DateTime SubmissionDeadline { get; set; }
    public DateTime? QuotationValidityDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public Guid? DeliveryAddressId { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? EvaluationCriteria { get; set; }
    public string? Notes { get; set; }

    public List<CreateRFQLineDto> Lines { get; set; } = [];
    public List<Guid> VendorIds { get; set; } = [];
}

public class CreateRFQLineDto
{
    public Guid? RequisitionLineId { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public Guid? ProcurementCategoryId { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }
    public string? Specifications { get; set; }
    public string? Notes { get; set; }
}

public class SubmitVendorQuotationDto
{
    public Guid VendorId { get; set; }
    public string? VendorQuotationReference { get; set; }
    public DateTime ValidUntil { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }
    public int DeliveryLeadTimeDays { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public List<SubmitVendorQuotationLineDto> Lines { get; set; } = [];
}

public class SubmitVendorQuotationLineDto
{
    public Guid RFQLineId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public int? LeadTimeDays { get; set; }
    public bool IsAlternative { get; set; }
    public string? Notes { get; set; }
}

public class EvaluateQuotationDto
{
    public Guid QuotationId { get; set; }
    public decimal? TechnicalScore { get; set; }
    public decimal? CommercialScore { get; set; }
    public bool IsRecommended { get; set; }
}

public class UpdateRFQDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? SubmissionDeadline { get; set; }
    public DateTime? QuotationValidityDate { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? EvaluationCriteria { get; set; }
    public string? Notes { get; set; }
}
