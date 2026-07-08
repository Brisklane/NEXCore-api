using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Company-level procurement configuration.
/// One record per CompanyId. Controls numbering, policies, and workflow defaults.
/// Aligned with SAP Purchasing Organization settings, Odoo Purchase Settings.
/// </summary>
public class ProcurementSettings : BaseEntity
{
    // ─── Purchase Policies ─────────────────────────────────────────────────────
    /// <summary>When true, a Purchase Requisition must exist before a PO can be created.</summary>
    public bool RequireRequisitionForPO { get; set; } = false;

    /// <summary>Amount above which an RFQ is mandatory before issuing a PO.</summary>
    public decimal? RFQMandatoryAboveAmount { get; set; }

    /// <summary>Enable three-way matching (PO + GR + Invoice) before payment is allowed.</summary>
    public bool Enable3WayMatching { get; set; } = true;

    /// <summary>When true, invoices cannot be posted if they exceed PO amount + tolerance.</summary>
    public bool EnforceInvoicePOTolerance { get; set; } = true;
    public decimal InvoicePOTolerancePercent { get; set; } = 5;

    // ─── Default Values ────────────────────────────────────────────────────────
    public PaymentTerms DefaultPaymentTerms { get; set; } = PaymentTerms.Net30;
    public string DefaultCurrencyCode { get; set; } = "PKR";
    public int DefaultLeadTimeDays { get; set; } = 7;

    // ─── Approval Workflows ────────────────────────────────────────────────────
    public Guid? POApprovalWorkflowId { get; set; }
    public ApprovalWorkflow? POApprovalWorkflow { get; set; }

    public Guid? RequisitionApprovalWorkflowId { get; set; }
    public ApprovalWorkflow? RequisitionApprovalWorkflow { get; set; }

    public Guid? InvoiceApprovalWorkflowId { get; set; }
    public ApprovalWorkflow? InvoiceApprovalWorkflow { get; set; }

    // ─── Vendor Onboarding ─────────────────────────────────────────────────────
    public bool RequireVendorApproval { get; set; } = true;
    public bool RequireVendorBankVerification { get; set; } = true;

    // ─── Notifications ─────────────────────────────────────────────────────────
    public bool SendPOToVendorByEmail { get; set; } = true;
    public bool SendRFQToVendorByEmail { get; set; } = true;
    public int POApprovalReminderDays { get; set; } = 2;
}
