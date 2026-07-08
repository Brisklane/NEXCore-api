using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>
/// Procurement lookup tables — returns all enum values as {value, name, label} objects.
/// The UI must call these endpoints to populate dropdowns; nothing is hardcoded on the front-end.
/// </summary>
[ApiController]
[Route("api/v1/procurement/lookups")]
[Produces("application/json")]
[Authorize]
public class ProcurementLookupsController : ControllerBase
{
    private static List<LookupItemDto> ToLookup<TEnum>() where TEnum : struct, Enum
        => Enum.GetValues<TEnum>()
            .Select(e => new LookupItemDto
            {
                Value       = Convert.ToInt32(e),
                Name        = e.ToString(),
                Label       = ToLabel(e.ToString())
            }).ToList();

    private static string ToLabel(string name)
    {
        // "PartiallyReceived" → "Partially Received"
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }

    // ── Vendor ────────────────────────────────────────────────────────────────

    /// <summary>Vendor statuses (PendingApproval, Active, Inactive, Blocked, Archived).</summary>
    [HttpGet("vendor-statuses")]
    public IActionResult VendorStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorStatus>() });

    /// <summary>Vendor types (Company, Individual).</summary>
    [HttpGet("vendor-types")]
    public IActionResult VendorTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorType>() });

    /// <summary>Vendor address types (Billing, Shipping, Both, Registered).</summary>
    [HttpGet("vendor-address-types")]
    public IActionResult VendorAddressTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorAddressType>() });

    /// <summary>Vendor onboarding statuses (Draft → Approved).</summary>
    [HttpGet("vendor-onboarding-statuses")]
    public IActionResult VendorOnboardingStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorOnboardingStatus>() });

    /// <summary>Vendor rating categories (Quality, Delivery, Price, Service, Compliance).</summary>
    [HttpGet("vendor-rating-categories")]
    public IActionResult VendorRatingCategories()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorRatingCategory>() });

    /// <summary>Vendor document types (TradeLicense, VATCertificate, ISO9001, etc.).</summary>
    [HttpGet("vendor-document-types")]
    public IActionResult VendorDocumentTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorDocumentType>() });

    /// <summary>Vendor document statuses (Active, Expired, PendingRenewal, Revoked).</summary>
    [HttpGet("vendor-document-statuses")]
    public IActionResult VendorDocumentStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorDocumentStatus>() });

    // ── Requisition ───────────────────────────────────────────────────────────

    /// <summary>Requisition statuses (Draft → Fulfilled).</summary>
    [HttpGet("requisition-statuses")]
    public IActionResult RequisitionStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<RequisitionStatus>() });

    /// <summary>Requisition priorities (Low, Normal, High, Urgent).</summary>
    [HttpGet("requisition-priorities")]
    public IActionResult RequisitionPriorities()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<RequisitionPriority>() });

    /// <summary>Requisition line statuses (Open, PartiallyFulfilled, Fulfilled, Cancelled).</summary>
    [HttpGet("requisition-line-statuses")]
    public IActionResult RequisitionLineStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<RequisitionLineStatus>() });

    // ── RFQ ───────────────────────────────────────────────────────────────────

    /// <summary>RFQ statuses (Draft, Sent, PartiallyReceived, FullyReceived, Awarded, Cancelled, Closed).</summary>
    [HttpGet("rfq-statuses")]
    public IActionResult RFQStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<RFQStatus>() });

    /// <summary>RFQ vendor statuses (Invited, Acknowledged, Responded, Declined, NoResponse).</summary>
    [HttpGet("rfq-vendor-statuses")]
    public IActionResult RFQVendorStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<RFQVendorStatus>() });

    /// <summary>Vendor quotation statuses (Draft, Submitted, UnderEvaluation, Shortlisted, Accepted, Rejected, Expired).</summary>
    [HttpGet("quotation-statuses")]
    public IActionResult QuotationStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<QuotationStatus>() });

    // ── Purchase Order ────────────────────────────────────────────────────────

    /// <summary>Purchase order statuses (Draft → Closed/Cancelled).</summary>
    [HttpGet("purchase-order-statuses")]
    public IActionResult PurchaseOrderStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<PurchaseOrderStatus>() });

    /// <summary>Purchase order line statuses (Open, PartiallyReceived, FullyReceived, Cancelled, Closed).</summary>
    [HttpGet("purchase-order-line-statuses")]
    public IActionResult PurchaseOrderLineStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<PurchaseOrderLineStatus>() });

    /// <summary>Amendment statuses (Draft, Submitted, Approved, Rejected, Applied).</summary>
    [HttpGet("amendment-statuses")]
    public IActionResult AmendmentStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<AmendmentStatus>() });

    /// <summary>Incoterms 2020 (EXW, FCA, CPT, CIP, DAP, DPU, DDP, FAS, FOB, CFR, CIF).</summary>
    [HttpGet("incoterms")]
    public IActionResult Incoterms()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<Incoterm>() });

    // ── Goods Receipt ─────────────────────────────────────────────────────────

    /// <summary>Goods receipt statuses (Draft, Posted, Cancelled).</summary>
    [HttpGet("goods-receipt-statuses")]
    public IActionResult GoodsReceiptStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<GoodsReceiptStatus>() });

    /// <summary>Receipt types (Standard, Return, Transfer).</summary>
    [HttpGet("receipt-types")]
    public IActionResult ReceiptTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<ReceiptType>() });

    /// <summary>Quality inspection statuses (Pending, Passed, Failed, PartiallyPassed, Waived).</summary>
    [HttpGet("quality-inspection-statuses")]
    public IActionResult QualityInspectionStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<QualityInspectionStatus>() });

    // ── Invoice ───────────────────────────────────────────────────────────────

    /// <summary>Purchase invoice statuses (Draft → FullyPaid/Cancelled).</summary>
    [HttpGet("purchase-invoice-statuses")]
    public IActionResult PurchaseInvoiceStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<PurchaseInvoiceStatus>() });

    /// <summary>Invoice matching statuses (NotMatched, PartiallyMatched, FullyMatched, MatchException).</summary>
    [HttpGet("invoice-matching-statuses")]
    public IActionResult InvoiceMatchingStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<InvoiceMatchingStatus>() });

    /// <summary>Invoice payment statuses (NotPaid, InPayment, PartiallyPaid, FullyPaid, Reversed).</summary>
    [HttpGet("invoice-payment-statuses")]
    public IActionResult InvoicePaymentStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<InvoicePaymentStatus>() });

    /// <summary>Invoice discrepancy types (Quantity, UnitPrice, Quality, Specification, TaxAmount, Other).</summary>
    [HttpGet("discrepancy-types")]
    public IActionResult DiscrepancyTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<DiscrepancyType>() });

    // ── Payment ───────────────────────────────────────────────────────────────

    /// <summary>Vendor payment statuses (Draft, Approved, Processing, Sent, Cleared, Cancelled, Returned).</summary>
    [HttpGet("vendor-payment-statuses")]
    public IActionResult VendorPaymentStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorPaymentStatus>() });

    /// <summary>Vendor payment methods (Cash, BankTransfer, Check, CreditCard, DirectDebit, OnlinePayment, Letter_of_Credit).</summary>
    [HttpGet("vendor-payment-methods")]
    public IActionResult VendorPaymentMethods()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<VendorPaymentMethod>() });

    // ── Contract ──────────────────────────────────────────────────────────────

    /// <summary>Purchase contract statuses (Draft, UnderReview, Active, Suspended, Expired, Terminated).</summary>
    [HttpGet("purchase-contract-statuses")]
    public IActionResult PurchaseContractStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<PurchaseContractStatus>() });

    /// <summary>Purchase contract types (FrameworkAgreement, BlanketOrder, ServiceAgreement, SupplyAgreement, MaintenanceContract).</summary>
    [HttpGet("purchase-contract-types")]
    public IActionResult PurchaseContractTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<PurchaseContractType>() });

    // ── Approval ──────────────────────────────────────────────────────────────

    /// <summary>Procurement approval statuses (Pending, Approved, Rejected, Delegated, Recalled, Escalated).</summary>
    [HttpGet("approval-statuses")]
    public IActionResult ApprovalStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<ProcurementApprovalStatus>() });

    /// <summary>Approval document types — which documents can have workflows configured.</summary>
    [HttpGet("approval-document-types")]
    public IActionResult ApprovalDocumentTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<ApprovalDocumentType>() });

    // ── Returns ───────────────────────────────────────────────────────────────

    /// <summary>Purchase return statuses (Draft, Approved, Posted, Cancelled).</summary>
    [HttpGet("purchase-return-statuses")]
    public IActionResult PurchaseReturnStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<PurchaseReturnStatus>() });

    /// <summary>Purchase return reasons (QualityDefect, WrongItem, ExcessQuantity, Damaged, etc.).</summary>
    [HttpGet("purchase-return-reasons")]
    public IActionResult PurchaseReturnReasons()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<PurchaseReturnReason>() });

    /// <summary>Vendor debit note statuses (Draft, Sent, Acknowledged, PartiallySettled, FullySettled, Cancelled).</summary>
    [HttpGet("debit-note-statuses")]
    public IActionResult DebitNoteStatuses()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<DebitNoteStatus>() });

    // ── Landed Costs ──────────────────────────────────────────────────────────

    /// <summary>Landed cost types (Freight, CustomsDuty, Insurance, Handling, etc.).</summary>
    [HttpGet("landed-cost-types")]
    public IActionResult LandedCostTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<LandedCostType>() });

    /// <summary>Landed cost allocation methods (ByQuantity, ByValue, ByWeight, ByVolume, Equal).</summary>
    [HttpGet("landed-cost-allocation-methods")]
    public IActionResult LandedCostAllocationMethods()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<LandedCostAllocationMethod>() });

    // ── Sequences ─────────────────────────────────────────────────────────────

    /// <summary>Procurement document types — all document types that have auto-number sequences.</summary>
    [HttpGet("document-types")]
    public IActionResult DocumentTypes()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<ProcurementDocumentType>() });

    /// <summary>Sequence reset periods (Never, Yearly, Monthly).</summary>
    [HttpGet("sequence-reset-periods")]
    public IActionResult SequenceResetPeriods()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<SequenceResetPeriod>() });

    // ── Shared / Cross-module ─────────────────────────────────────────────────

    /// <summary>Payment terms (Cash, Net15, Net30, Net45, Net60, Net90, etc.) — shared with Sales and AR.</summary>
    [HttpGet("payment-terms")]
    public IActionResult PaymentTermsLookup()
        => Ok(new ApiResponse<List<LookupItemDto>> { Success = true, Data = ToLookup<PaymentTerms>() });

    // ── Aggregate: all lookups in one call ────────────────────────────────────

    /// <summary>Returns ALL procurement lookup tables in a single response — use on app startup to seed the UI store.</summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, List<LookupItemDto>>>), StatusCodes.Status200OK)]
    public IActionResult All()
    {
        var all = new Dictionary<string, List<LookupItemDto>>
        {
            ["vendorStatuses"]              = ToLookup<VendorStatus>(),
            ["vendorTypes"]                 = ToLookup<VendorType>(),
            ["vendorAddressTypes"]          = ToLookup<VendorAddressType>(),
            ["vendorOnboardingStatuses"]    = ToLookup<VendorOnboardingStatus>(),
            ["vendorRatingCategories"]      = ToLookup<VendorRatingCategory>(),
            ["vendorDocumentTypes"]         = ToLookup<VendorDocumentType>(),
            ["vendorDocumentStatuses"]      = ToLookup<VendorDocumentStatus>(),
            ["requisitionStatuses"]         = ToLookup<RequisitionStatus>(),
            ["requisitionPriorities"]       = ToLookup<RequisitionPriority>(),
            ["requisitionLineStatuses"]     = ToLookup<RequisitionLineStatus>(),
            ["rfqStatuses"]                 = ToLookup<RFQStatus>(),
            ["rfqVendorStatuses"]           = ToLookup<RFQVendorStatus>(),
            ["quotationStatuses"]           = ToLookup<QuotationStatus>(),
            ["purchaseOrderStatuses"]       = ToLookup<PurchaseOrderStatus>(),
            ["purchaseOrderLineStatuses"]   = ToLookup<PurchaseOrderLineStatus>(),
            ["amendmentStatuses"]           = ToLookup<AmendmentStatus>(),
            ["incoterms"]                   = ToLookup<Incoterm>(),
            ["goodsReceiptStatuses"]        = ToLookup<GoodsReceiptStatus>(),
            ["receiptTypes"]                = ToLookup<ReceiptType>(),
            ["qualityInspectionStatuses"]   = ToLookup<QualityInspectionStatus>(),
            ["purchaseInvoiceStatuses"]     = ToLookup<PurchaseInvoiceStatus>(),
            ["invoiceMatchingStatuses"]     = ToLookup<InvoiceMatchingStatus>(),
            ["invoicePaymentStatuses"]      = ToLookup<InvoicePaymentStatus>(),
            ["discrepancyTypes"]            = ToLookup<DiscrepancyType>(),
            ["vendorPaymentStatuses"]       = ToLookup<VendorPaymentStatus>(),
            ["vendorPaymentMethods"]        = ToLookup<VendorPaymentMethod>(),
            ["purchaseContractStatuses"]    = ToLookup<PurchaseContractStatus>(),
            ["purchaseContractTypes"]       = ToLookup<PurchaseContractType>(),
            ["approvalStatuses"]            = ToLookup<ProcurementApprovalStatus>(),
            ["approvalDocumentTypes"]       = ToLookup<ApprovalDocumentType>(),
            ["purchaseReturnStatuses"]      = ToLookup<PurchaseReturnStatus>(),
            ["purchaseReturnReasons"]       = ToLookup<PurchaseReturnReason>(),
            ["debitNoteStatuses"]           = ToLookup<DebitNoteStatus>(),
            ["landedCostTypes"]             = ToLookup<LandedCostType>(),
            ["landedCostAllocationMethods"] = ToLookup<LandedCostAllocationMethod>(),
            ["documentTypes"]               = ToLookup<ProcurementDocumentType>(),
            ["sequenceResetPeriods"]        = ToLookup<SequenceResetPeriod>(),
            ["paymentTerms"]                = ToLookup<PaymentTerms>(),
        };

        return Ok(new ApiResponse<Dictionary<string, List<LookupItemDto>>>
        {
            Success = true,
            Data    = all,
            Message = $"{all.Count} lookup tables"
        });
    }
}

/// <summary>A single lookup entry returned by the Lookups controller.</summary>
public class LookupItemDto
{
    /// <summary>The numeric enum value stored in the database.</summary>
    public int Value { get; set; }
    /// <summary>The enum member name as defined in C# (e.g. "PartiallyReceived").</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Human-readable label with spaces inserted (e.g. "Partially Received").</summary>
    public string Label { get; set; } = string.Empty;
}
