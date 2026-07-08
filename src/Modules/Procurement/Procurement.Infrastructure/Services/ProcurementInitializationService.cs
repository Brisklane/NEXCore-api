using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;

namespace Procurement.Infrastructure.Services;

/// <summary>
/// Seeds essential procurement master data for a newly onboarded company.
/// Seeding order respects FK constraints:
///   1. Document Sequences
///   2. Vendor Categories
///   3. Procurement Categories
///   4. Approval Workflows + Steps
///   5. Procurement Settings
/// </summary>
public class ProcurementInitializationService : IProcurementInitializationService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<ProcurementInitializationService> _logger;

    public ProcurementInitializationService(
        ProcurementDbContext ctx,
        ILogger<ProcurementInitializationService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task InitializeAsync(Guid companyId)
    {
        if (await _ctx.DocumentSequences.AnyAsync(s => s.CompanyId == companyId))
        {
            _logger.LogWarning("Procurement data already initialized for Company:{CompanyId}", companyId);
            return;
        }

        _logger.LogInformation("Initializing Procurement data for Company:{CompanyId}", companyId);

        await using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var T = companyId;

            // 1. Document Sequences
            var sequences = SeedDocumentSequences(T);
            _ctx.DocumentSequences.AddRange(sequences);
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded {N} document sequences", sequences.Length);

            // 2. Vendor Categories
            var vendorCategories = SeedVendorCategories(T);
            _ctx.VendorCategories.AddRange(vendorCategories);
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded {N} vendor categories", vendorCategories.Length);

            // 3. Procurement Categories
            var procCategories = SeedProcurementCategories(T);
            _ctx.ProcurementCategories.AddRange(procCategories);
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded {N} procurement categories", procCategories.Length);

            // 4. Approval Workflows
            var (workflows, steps) = SeedApprovalWorkflows(T);
            _ctx.ApprovalWorkflows.AddRange(workflows);
            await _ctx.SaveChangesAsync();
            _ctx.ApprovalWorkflowSteps.AddRange(steps);
            await _ctx.SaveChangesAsync();
            _logger.LogInformation("Seeded {N} approval workflows", workflows.Length);

            // 5. Procurement Settings
            var settings = SeedSettings(T, workflows);
            _ctx.ProcurementSettings.Add(settings);
            await _ctx.SaveChangesAsync();

            await tx.CommitAsync();
            _logger.LogInformation("Procurement initialization complete for Company:{CompanyId}", companyId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Failed to initialize Procurement for Company:{CompanyId}", companyId);
            throw;
        }
    }

    // ── Seed Helpers ──────────────────────────────────────────────────────────

    private static DocumentSequence[] SeedDocumentSequences(Guid companyId) =>
    [
        Seq(companyId, ProcurementDocumentType.Vendor,               "V",    "Vendor Number"),
        Seq(companyId, ProcurementDocumentType.PurchaseRequisition,  "PR",   "Purchase Requisition"),
        Seq(companyId, ProcurementDocumentType.RequestForQuotation,  "RFQ",  "Request for Quotation"),
        Seq(companyId, ProcurementDocumentType.PurchaseOrder,        "PO",   "Purchase Order"),
        Seq(companyId, ProcurementDocumentType.GoodsReceipt,         "GRN",  "Goods Receipt Note"),
        Seq(companyId, ProcurementDocumentType.PurchaseInvoice,      "BILL", "Vendor Bill"),
        Seq(companyId, ProcurementDocumentType.PurchaseContract,     "PC",   "Purchase Contract"),
        Seq(companyId, ProcurementDocumentType.VendorPayment,        "PAY",  "Vendor Payment"),
        Seq(companyId, ProcurementDocumentType.PurchaseReturn,       "PRN",  "Purchase Return Note"),
        Seq(companyId, ProcurementDocumentType.VendorDebitNote,      "DN",   "Vendor Debit Note"),
    ];

    private static DocumentSequence Seq(
        Guid companyId, ProcurementDocumentType type, string prefix, string description) =>
        new()
        {
            CompanyId           = companyId,
            DocumentType        = type,
            Prefix              = prefix,
            Separator           = "-",
            IncludeYear         = true,
            YearFormat          = SequenceYearFormat.Full,
            IncludeMonth        = false,
            SequencePadding     = 5,
            ResetOn             = SequenceResetPeriod.Yearly,
            NextSequenceNumber  = 1,
            IsActive            = true,
            Description         = description
        };

    private static VendorCategory[] SeedVendorCategories(Guid companyId) =>
    [
        Cat(companyId, "GOODS",   "Goods Supplier"),
        Cat(companyId, "SERVICE", "Service Provider"),
        Cat(companyId, "RAW",     "Raw Material Supplier"),
        Cat(companyId, "IT",      "IT & Technology"),
        Cat(companyId, "MAINT",   "Maintenance & Repair"),
        Cat(companyId, "CONSULT", "Consultancy"),
        Cat(companyId, "UTIL",    "Utilities"),
        Cat(companyId, "OTHER",   "Other"),
    ];

    private static VendorCategory Cat(Guid companyId, string code, string name) =>
        new() { CompanyId = companyId, Code = code, Name = name };

    private static ProcurementCategory[] SeedProcurementCategories(Guid companyId) =>
    [
        ProcCat(companyId, "DIRECT",   "Direct Materials"),
        ProcCat(companyId, "INDIRECT", "Indirect Procurement"),
        ProcCat(companyId, "IT-HW",    "IT Hardware"),
        ProcCat(companyId, "IT-SW",    "IT Software & Licenses"),
        ProcCat(companyId, "FACILITY", "Facilities & Office"),
        ProcCat(companyId, "TRAVEL",   "Travel & Accommodation"),
        ProcCat(companyId, "MKTG",     "Marketing & Events"),
        ProcCat(companyId, "HR",       "Human Resources"),
        ProcCat(companyId, "CAPEX",    "Capital Expenditure"),
        ProcCat(companyId, "SERVICES", "Professional Services"),
    ];

    private static ProcurementCategory ProcCat(Guid companyId, string code, string name) =>
        new() { CompanyId = companyId, Code = code, Name = name };

    private static (ApprovalWorkflow[] workflows, ApprovalWorkflowStep[] steps) SeedApprovalWorkflows(Guid companyId)
    {
        var poWorkflow = new ApprovalWorkflow
        {
            CompanyId    = companyId,
            Name         = "Purchase Order Approval",
            DocumentType = ApprovalDocumentType.PurchaseOrder,
            IsActive     = true,
            IsDefault    = true,
            Priority     = 1
        };

        var reqWorkflow = new ApprovalWorkflow
        {
            CompanyId    = companyId,
            Name         = "Requisition Approval",
            DocumentType = ApprovalDocumentType.PurchaseRequisition,
            IsActive     = true,
            IsDefault    = true,
            Priority     = 1
        };

        var workflows = new[] { poWorkflow, reqWorkflow };

        var steps = new[]
        {
            new ApprovalWorkflowStep
            {
                CompanyId         = companyId,
                WorkflowId        = poWorkflow.Id,
                StepNumber        = 1,
                StepName          = "Department Manager Review",
                ApproverRole      = "PurchaseManager",
                EscalationAfterDays = 2,
                RequiredApprovals = 1
            },
            new ApprovalWorkflowStep
            {
                CompanyId         = companyId,
                WorkflowId        = poWorkflow.Id,
                StepNumber        = 2,
                StepName          = "Finance Director Approval",
                ApproverRole      = "FinanceDirector",
                EscalationAfterDays = 3,
                RequiredApprovals = 1
            },
            new ApprovalWorkflowStep
            {
                CompanyId         = companyId,
                WorkflowId        = reqWorkflow.Id,
                StepNumber        = 1,
                StepName          = "Department Head Approval",
                ApproverRole      = "DepartmentHead",
                EscalationAfterDays = 2,
                RequiredApprovals = 1
            }
        };

        return (workflows, steps);
    }

    private static ProcurementSettings SeedSettings(Guid companyId, ApprovalWorkflow[] workflows)
    {
        var poWorkflow  = workflows.First(w => w.DocumentType == ApprovalDocumentType.PurchaseOrder);
        var reqWorkflow = workflows.First(w => w.DocumentType == ApprovalDocumentType.PurchaseRequisition);

        return new ProcurementSettings
        {
            CompanyId                       = companyId,
            RequireRequisitionForPO         = false,
            RFQMandatoryAboveAmount         = 10_000,
            Enable3WayMatching              = true,
            EnforceInvoicePOTolerance       = true,
            InvoicePOTolerancePercent       = 5,
            DefaultPaymentTerms             = Nexcore.SharedKernel.Enums.PaymentTerms.Net30,
            DefaultCurrencyCode             = "PKR",
            DefaultLeadTimeDays             = 7,
            POApprovalWorkflowId            = poWorkflow.Id,
            RequisitionApprovalWorkflowId   = reqWorkflow.Id,
            RequireVendorApproval           = true,
            RequireVendorBankVerification   = true,
            SendPOToVendorByEmail           = true,
            SendRFQToVendorByEmail          = true,
            POApprovalReminderDays          = 2
        };
    }
}
