using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Procurement.Domain.Entities;

namespace Procurement.Infrastructure.Persistence;

/// <summary>
/// Procurement module DbContext — Schema: procurement
///
/// Vendor Master     : Vendor, VendorContact, VendorAddress, VendorBankAccount,
///                     VendorCategory, VendorPerformance, VendorDocument, VendorPricelist,
///                     VendorPricelistItem, ApprovedVendorList
/// Sourcing          : PurchaseRequisition, PurchaseRequisitionLine,
///                     RequestForQuotation, RFQLine, RFQVendor,
///                     VendorQuotation, VendorQuotationLine
/// Contracting       : PurchaseContract, PurchaseContractLine
/// Ordering          : PurchaseOrder, PurchaseOrderLine, PurchaseOrderApproval,
///                     PurchaseOrderAmendment
/// Receipt           : GoodsReceipt, GoodsReceiptLine
/// AP Invoice        : PurchaseInvoice, PurchaseInvoiceLine, ThreeWayMatchRecord
/// Payment           : VendorPayment, VendorPaymentLine
/// Returns           : PurchaseReturn, PurchaseReturnLine, VendorDebitNote, VendorDebitNoteLine
/// Landed Costs      : LandedCost, LandedCostLine, LandedCostGoodsReceipt, LandedCostAllocation
/// Approval Engine   : ApprovalWorkflow, ApprovalWorkflowStep, ProcurementApproval
/// Config            : DocumentSequence, ProcurementSettings, ProcurementCategory
/// </summary>
public class ProcurementDbContext : DbContext
{
    private const string Schema = "procurement";

    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : base(options) { }

    // ── Vendor Master ─────────────────────────────────────────────────────────
    public DbSet<Vendor> Vendors { get; set; } = null!;
    public DbSet<VendorContact> VendorContacts { get; set; } = null!;
    public DbSet<VendorAddress> VendorAddresses { get; set; } = null!;
    public DbSet<VendorBankAccount> VendorBankAccounts { get; set; } = null!;
    public DbSet<VendorCategory> VendorCategories { get; set; } = null!;
    public DbSet<VendorPerformance> VendorPerformances { get; set; } = null!;
    public DbSet<VendorDocument> VendorDocuments { get; set; } = null!;
    public DbSet<VendorPricelist> VendorPricelists { get; set; } = null!;
    public DbSet<VendorPricelistItem> VendorPricelistItems { get; set; } = null!;
    public DbSet<ApprovedVendorList> ApprovedVendorLists { get; set; } = null!;

    // ── Sourcing ──────────────────────────────────────────────────────────────
    public DbSet<PurchaseRequisition> PurchaseRequisitions { get; set; } = null!;
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines { get; set; } = null!;
    public DbSet<RequestForQuotation> RequestForQuotations { get; set; } = null!;
    public DbSet<RFQLine> RFQLines { get; set; } = null!;
    public DbSet<RFQVendor> RFQVendors { get; set; } = null!;
    public DbSet<VendorQuotation> VendorQuotations { get; set; } = null!;
    public DbSet<VendorQuotationLine> VendorQuotationLines { get; set; } = null!;

    // ── Contracting ───────────────────────────────────────────────────────────
    public DbSet<PurchaseContract> PurchaseContracts { get; set; } = null!;
    public DbSet<PurchaseContractLine> PurchaseContractLines { get; set; } = null!;

    // ── Ordering ──────────────────────────────────────────────────────────────
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = null!;
    public DbSet<PurchaseOrderApproval> PurchaseOrderApprovals { get; set; } = null!;
    public DbSet<PurchaseOrderAmendment> PurchaseOrderAmendments { get; set; } = null!;

    // ── Receipt ───────────────────────────────────────────────────────────────
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; } = null!;
    public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; } = null!;

    // ── AP Invoice ────────────────────────────────────────────────────────────
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; } = null!;
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines { get; set; } = null!;
    public DbSet<ThreeWayMatchRecord> ThreeWayMatchRecords { get; set; } = null!;

    // ── Payment ───────────────────────────────────────────────────────────────
    public DbSet<VendorPayment> VendorPayments { get; set; } = null!;
    public DbSet<VendorPaymentLine> VendorPaymentLines { get; set; } = null!;

    // ── Returns ───────────────────────────────────────────────────────────────
    public DbSet<PurchaseReturn> PurchaseReturns { get; set; } = null!;
    public DbSet<PurchaseReturnLine> PurchaseReturnLines { get; set; } = null!;
    public DbSet<VendorDebitNote> VendorDebitNotes { get; set; } = null!;
    public DbSet<VendorDebitNoteLine> VendorDebitNoteLines { get; set; } = null!;

    // ── Landed Costs ──────────────────────────────────────────────────────────
    public DbSet<LandedCost> LandedCosts { get; set; } = null!;
    public DbSet<LandedCostLine> LandedCostLines { get; set; } = null!;
    public DbSet<LandedCostGoodsReceipt> LandedCostGoodsReceipts { get; set; } = null!;
    public DbSet<LandedCostAllocation> LandedCostAllocations { get; set; } = null!;

    // ── Approval Engine ───────────────────────────────────────────────────────
    public DbSet<ApprovalWorkflow> ApprovalWorkflows { get; set; } = null!;
    public DbSet<ApprovalWorkflowStep> ApprovalWorkflowSteps { get; set; } = null!;
    public DbSet<ProcurementApproval> ProcurementApprovals { get; set; } = null!;

    // ── Config ────────────────────────────────────────────────────────────────
    public DbSet<DocumentSequence> DocumentSequences { get; set; } = null!;
    public DbSet<ProcurementSettings> ProcurementSettings { get; set; } = null!;
    public DbSet<ProcurementCategory> ProcurementCategories { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        // ── VendorCategory ────────────────────────────────────────────────────
        modelBuilder.Entity<VendorCategory>(e =>
        {
            e.ToTable("VendorCategories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.HasIndex(x => new { x.CompanyId, x.Code })
             .IsUnique().HasDatabaseName("IX_VendorCategory_Company_Code");
        });

        // ── Vendor ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Vendor>(e =>
        {
            e.ToTable("Vendors");
            e.HasKey(x => x.Id);
            e.Property(x => x.VendorNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.ShortName).HasMaxLength(100);
            e.Property(x => x.TaxRegistrationNumber).HasMaxLength(100);
            e.Property(x => x.CompanyRegistrationNumber).HasMaxLength(100);
            e.Property(x => x.VATNumber).HasMaxLength(50);
            e.Property(x => x.Website).HasMaxLength(500);
            e.Property(x => x.PrimaryEmail).HasMaxLength(255);
            e.Property(x => x.PrimaryPhone).HasMaxLength(50);
            e.Property(x => x.PrimaryMobile).HasMaxLength(50);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.CreditLimit).HasPrecision(18, 2);
            e.Property(x => x.OverallRating).HasPrecision(5, 2);
            e.Property(x => x.OnTimeDeliveryRate).HasPrecision(5, 2);
            e.Property(x => x.QualityScore).HasPrecision(5, 2);
            e.Property(x => x.BlockReason).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.VendorNumber })
             .IsUnique().HasDatabaseName("IX_Vendor_Company_Number");
            e.HasOne(x => x.VendorCategory).WithMany(c => c.Vendors)
             .HasForeignKey(x => x.VendorCategoryId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Contacts).WithOne(c => c.Vendor)
             .HasForeignKey(c => c.VendorId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Addresses).WithOne(a => a.Vendor)
             .HasForeignKey(a => a.VendorId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.BankAccounts).WithOne(b => b.Vendor)
             .HasForeignKey(b => b.VendorId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PerformanceRecords).WithOne(p => p.Vendor)
             .HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Pricelists).WithOne(p => p.Vendor)
             .HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── VendorContact ─────────────────────────────────────────────────────
        modelBuilder.Entity<VendorContact>(e =>
        {
            e.ToTable("VendorContacts");
            e.HasKey(x => x.Id);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.JobTitle).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Mobile).HasMaxLength(50);
        });

        // ── VendorAddress ─────────────────────────────────────────────────────
        modelBuilder.Entity<VendorAddress>(e =>
        {
            e.ToTable("VendorAddresses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Street).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.State).HasMaxLength(100);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.Country).HasMaxLength(100);
        });

        // ── VendorBankAccount ─────────────────────────────────────────────────
        modelBuilder.Entity<VendorBankAccount>(e =>
        {
            e.ToTable("VendorBankAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountHolderName).IsRequired().HasMaxLength(255);
            e.Property(x => x.AccountNumber).IsRequired().HasMaxLength(100);
            e.Property(x => x.BankName).IsRequired().HasMaxLength(255);
            e.Property(x => x.BranchName).HasMaxLength(255);
            e.Property(x => x.IBAN).HasMaxLength(50);
            e.Property(x => x.SWIFTCode).HasMaxLength(20);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
        });

        // ── VendorPerformance ─────────────────────────────────────────────────
        modelBuilder.Entity<VendorPerformance>(e =>
        {
            e.ToTable("VendorPerformances");
            e.HasKey(x => x.Id);
            e.Property(x => x.OnTimeDeliveryRate).HasPrecision(5, 2);
            e.Property(x => x.QualityScore).HasPrecision(5, 2);
            e.Property(x => x.PriceComplianceRate).HasPrecision(5, 2);
            e.Property(x => x.ResponsivenessScore).HasPrecision(5, 2);
            e.Property(x => x.DocumentAccuracyScore).HasPrecision(5, 2);
            e.Property(x => x.OverallRating).HasPrecision(5, 2);
            e.Property(x => x.TotalPurchaseValue).HasPrecision(18, 2);
            e.HasIndex(x => new { x.VendorId, x.PeriodFrom })
             .HasDatabaseName("IX_VendorPerformance_Vendor_Period");
        });

        // ── VendorDocument ────────────────────────────────────────────────────
        modelBuilder.Entity<VendorDocument>(e =>
        {
            e.ToTable("VendorDocuments");
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentName).IsRequired().HasMaxLength(255);
            e.Property(x => x.DocumentNumber).HasMaxLength(100);
            e.Property(x => x.FilePath).HasMaxLength(1000);
            e.HasOne(x => x.Vendor).WithMany()
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── VendorPricelist ───────────────────────────────────────────────────
        modelBuilder.Entity<VendorPricelist>(e =>
        {
            e.ToTable("VendorPricelists");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.HasMany(x => x.Items).WithOne(i => i.Pricelist)
             .HasForeignKey(i => i.PricelistId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── VendorPricelistItem ───────────────────────────────────────────────
        modelBuilder.Entity<VendorPricelistItem>(e =>
        {
            e.ToTable("VendorPricelistItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.MinimumQuantity).HasPrecision(18, 4);
        });

        // ── ApprovedVendorList ────────────────────────────────────────────────
        modelBuilder.Entity<ApprovedVendorList>(e =>
        {
            e.ToTable("ApprovedVendorLists");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Vendor).WithMany()
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ProcurementCategory).WithMany()
             .HasForeignKey(x => x.ProcurementCategoryId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── ProcurementCategory ───────────────────────────────────────────────
        modelBuilder.Entity<ProcurementCategory>(e =>
        {
            e.ToTable("ProcurementCategories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.HasIndex(x => new { x.CompanyId, x.Code })
             .IsUnique().HasDatabaseName("IX_ProcurementCategory_Company_Code");
            e.HasOne(x => x.ParentCategory).WithMany(x => x.SubCategories)
             .HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── PurchaseRequisition ───────────────────────────────────────────────
        modelBuilder.Entity<PurchaseRequisition>(e =>
        {
            e.ToTable("PurchaseRequisitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.RequisitionNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.RequestedByName).HasMaxLength(255);
            e.Property(x => x.DepartmentName).HasMaxLength(255);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.EstimatedTotalAmount).HasPrecision(18, 2);
            e.Property(x => x.RejectionReason).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.RequisitionNumber })
             .IsUnique().HasDatabaseName("IX_PurchaseRequisition_Company_Number");
            e.HasIndex(x => new { x.CompanyId, x.Status })
             .HasDatabaseName("IX_PurchaseRequisition_Company_Status");
            e.HasOne(x => x.SuggestedVendor).WithMany()
             .HasForeignKey(x => x.SuggestedVendorId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Lines).WithOne(l => l.Requisition)
             .HasForeignKey(l => l.RequisitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Approvals).WithOne(a => a.Requisition)
             .HasForeignKey(a => a.DocumentId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── PurchaseRequisitionLine ───────────────────────────────────────────
        modelBuilder.Entity<PurchaseRequisitionLine>(e =>
        {
            e.ToTable("PurchaseRequisitionLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.ItemCode).HasMaxLength(100);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.QuantityOrdered).HasPrecision(18, 4);
            e.Property(x => x.EstimatedUnitPrice).HasPrecision(18, 4);
            e.Property(x => x.EstimatedTotalPrice).HasPrecision(18, 2);
            e.Property(x => x.UnitOfMeasureName).HasMaxLength(50);
            e.Ignore(x => x.QuantityRemaining);
            e.HasOne(x => x.SuggestedVendor).WithMany()
             .HasForeignKey(x => x.SuggestedVendorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ProcurementCategory).WithMany()
             .HasForeignKey(x => x.ProcurementCategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── RequestForQuotation ───────────────────────────────────────────────
        modelBuilder.Entity<RequestForQuotation>(e =>
        {
            e.ToTable("RequestForQuotations");
            e.HasKey(x => x.Id);
            e.Property(x => x.RFQNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.HasIndex(x => new { x.CompanyId, x.RFQNumber })
             .IsUnique().HasDatabaseName("IX_RequestForQuotation_Company_Number");
            e.HasOne(x => x.Requisition).WithMany()
             .HasForeignKey(x => x.RequisitionId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Lines).WithOne(l => l.RFQ)
             .HasForeignKey(l => l.RFQId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.InvitedVendors).WithOne(v => v.RFQ)
             .HasForeignKey(v => v.RFQId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.VendorQuotations).WithOne(q => q.RFQ)
             .HasForeignKey(q => q.RFQId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── RFQLine ───────────────────────────────────────────────────────────
        modelBuilder.Entity<RFQLine>(e =>
        {
            e.ToTable("RFQLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.ItemCode).HasMaxLength(100);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.EstimatedUnitPrice).HasPrecision(18, 4);
            e.Property(x => x.UnitOfMeasureName).HasMaxLength(50);
            e.HasOne(x => x.RequisitionLine).WithMany()
             .HasForeignKey(x => x.RequisitionLineId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ProcurementCategory).WithMany()
             .HasForeignKey(x => x.ProcurementCategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── RFQVendor ─────────────────────────────────────────────────────────
        modelBuilder.Entity<RFQVendor>(e =>
        {
            e.ToTable("RFQVendors");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Vendor).WithMany()
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.RFQId, x.VendorId })
             .IsUnique().HasDatabaseName("IX_RFQVendor_RFQ_Vendor");
        });

        // ── VendorQuotation ───────────────────────────────────────────────────
        modelBuilder.Entity<VendorQuotation>(e =>
        {
            e.ToTable("VendorQuotations");
            e.HasKey(x => x.Id);
            e.Property(x => x.QuotationNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.SubTotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TechnicalScore).HasPrecision(5, 2);
            e.Property(x => x.CommercialScore).HasPrecision(5, 2);
            e.Property(x => x.OverallScore).HasPrecision(5, 2);
            e.HasIndex(x => new { x.CompanyId, x.QuotationNumber })
             .IsUnique().HasDatabaseName("IX_VendorQuotation_Company_Number");
            e.HasOne(x => x.Vendor).WithMany()
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(l => l.Quotation)
             .HasForeignKey(l => l.QuotationId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── VendorQuotationLine ───────────────────────────────────────────────
        modelBuilder.Entity<VendorQuotationLine>(e =>
        {
            e.ToTable("VendorQuotationLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.ItemCode).HasMaxLength(100);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(5, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);
            e.Property(x => x.UnitOfMeasureName).HasMaxLength(50);
            e.HasOne(x => x.RFQLine).WithMany(l => l.QuotationLines)
             .HasForeignKey(x => x.RFQLineId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── PurchaseContract ──────────────────────────────────────────────────
        modelBuilder.Entity<PurchaseContract>(e =>
        {
            e.ToTable("PurchaseContracts");
            e.HasKey(x => x.Id);
            e.Property(x => x.ContractNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.VendorName).HasMaxLength(255);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.MaximumContractValue).HasPrecision(18, 2);
            e.Property(x => x.CommittedValue).HasPrecision(18, 2);
            e.Property(x => x.UsedValue).HasPrecision(18, 2);
            e.Ignore(x => x.RemainingValue);
            e.HasIndex(x => new { x.CompanyId, x.ContractNumber })
             .IsUnique().HasDatabaseName("IX_PurchaseContract_Company_Number");
            e.HasOne(x => x.Vendor).WithMany(v => v.Contracts)
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(l => l.Contract)
             .HasForeignKey(l => l.ContractId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── PurchaseContractLine ──────────────────────────────────────────────
        modelBuilder.Entity<PurchaseContractLine>(e =>
        {
            e.ToTable("PurchaseContractLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.ItemCode).HasMaxLength(100);
            e.Property(x => x.UnitOfMeasureName).HasMaxLength(50);
            e.Property(x => x.MinimumQuantity).HasPrecision(18, 4);
            e.Property(x => x.MaximumQuantity).HasPrecision(18, 4);
            e.Property(x => x.CommittedQuantity).HasPrecision(18, 4);
            e.Property(x => x.OrderedQuantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Ignore(x => x.RemainingQuantity);
            e.HasOne(x => x.ProcurementCategory).WithMany()
             .HasForeignKey(x => x.ProcurementCategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── PurchaseOrder ─────────────────────────────────────────────────────
        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.ToTable("PurchaseOrders");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.VendorName).HasMaxLength(255);
            e.Property(x => x.VendorReference).HasMaxLength(100);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.SubTotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.ShippingAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.InvoicedAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
            e.Property(x => x.DeliveryStreet).HasMaxLength(500);
            e.Property(x => x.DeliveryCity).HasMaxLength(100);
            e.Property(x => x.DeliveryState).HasMaxLength(100);
            e.Property(x => x.DeliveryPostalCode).HasMaxLength(20);
            e.Property(x => x.DeliveryCountry).HasMaxLength(100);
            e.HasIndex(x => new { x.CompanyId, x.OrderNumber })
             .IsUnique().HasDatabaseName("IX_PurchaseOrder_Company_Number");
            e.HasIndex(x => new { x.CompanyId, x.Status })
             .HasDatabaseName("IX_PurchaseOrder_Company_Status");
            e.HasOne(x => x.Vendor).WithMany(v => v.PurchaseOrders)
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Quotation).WithMany()
             .HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Requisition).WithMany()
             .HasForeignKey(x => x.RequisitionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Contract).WithMany(c => c.PurchaseOrders)
             .HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Lines).WithOne(l => l.Order)
             .HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Approvals).WithOne(a => a.Order)
             .HasForeignKey(a => a.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Amendments).WithOne(a => a.Order)
             .HasForeignKey(a => a.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── PurchaseOrderLine ─────────────────────────────────────────────────
        modelBuilder.Entity<PurchaseOrderLine>(e =>
        {
            e.ToTable("PurchaseOrderLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.ItemCode).HasMaxLength(100);
            e.Property(x => x.UnitOfMeasureName).HasMaxLength(50);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(5, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);
            e.Property(x => x.QuantityReceived).HasPrecision(18, 4);
            e.Property(x => x.QuantityAccepted).HasPrecision(18, 4);
            e.Property(x => x.QuantityRejected).HasPrecision(18, 4);
            e.Property(x => x.QuantityReturned).HasPrecision(18, 4);
            e.Property(x => x.QuantityInvoiced).HasPrecision(18, 4);
            e.Ignore(x => x.QuantityRemaining);
            e.Ignore(x => x.QuantityToInvoice);
            e.HasOne(x => x.RequisitionLine).WithMany()
             .HasForeignKey(x => x.RequisitionLineId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ContractLine).WithMany(c => c.OrderLines)
             .HasForeignKey(x => x.ContractLineId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ProcurementCategory).WithMany()
             .HasForeignKey(x => x.ProcurementCategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── PurchaseOrderApproval ─────────────────────────────────────────────
        modelBuilder.Entity<PurchaseOrderApproval>(e =>
        {
            e.ToTable("PurchaseOrderApprovals");
            e.HasKey(x => x.Id);
            e.Property(x => x.ApproverName).HasMaxLength(255);
            e.Property(x => x.Comments).HasMaxLength(1000);
        });

        // ── PurchaseOrderAmendment ────────────────────────────────────────────
        modelBuilder.Entity<PurchaseOrderAmendment>(e =>
        {
            e.ToTable("PurchaseOrderAmendments");
            e.HasKey(x => x.Id);
            e.Property(x => x.AmendmentNumber).IsRequired();
            e.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        });

        // ── GoodsReceipt ──────────────────────────────────────────────────────
        modelBuilder.Entity<GoodsReceipt>(e =>
        {
            e.ToTable("GoodsReceipts");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.VendorDeliveryNoteNumber).HasMaxLength(100);
            e.HasIndex(x => new { x.CompanyId, x.ReceiptNumber })
             .IsUnique().HasDatabaseName("IX_GoodsReceipt_Company_Number");
            e.HasOne(x => x.PurchaseOrder).WithMany(po => po.GoodsReceipts)
             .HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Vendor).WithMany()
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OriginalReceipt).WithMany()
             .HasForeignKey(x => x.OriginalReceiptId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Receipt)
             .HasForeignKey(l => l.ReceiptId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── GoodsReceiptLine ──────────────────────────────────────────────────
        modelBuilder.Entity<GoodsReceiptLine>(e =>
        {
            e.ToTable("GoodsReceiptLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.ItemCode).HasMaxLength(100);
            e.Property(x => x.QuantityOrdered).HasPrecision(18, 4);
            e.Property(x => x.QuantityReceived).HasPrecision(18, 4);
            e.Property(x => x.QuantityAccepted).HasPrecision(18, 4);
            e.Property(x => x.QuantityRejected).HasPrecision(18, 4);
            e.Property(x => x.UnitOfMeasureName).HasMaxLength(50);
            e.Property(x => x.LotNumber).HasMaxLength(100);
            e.Property(x => x.SerialNumber).HasMaxLength(100);
            e.Property(x => x.ManufacturerBatchNumber).HasMaxLength(100);
            e.Property(x => x.StorageLocationName).HasMaxLength(255);
            e.Property(x => x.QualityNotes).HasMaxLength(1000);
            // NoAction: PurchaseOrder → PurchaseOrderLine → GoodsReceiptLine would create
            // a second cascade path via GoodsReceipt (PurchaseOrder → GoodsReceipt → Lines)
            e.HasOne(x => x.PurchaseOrderLine).WithMany(l => l.ReceiptLines)
             .HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── PurchaseInvoice ───────────────────────────────────────────────────
        modelBuilder.Entity<PurchaseInvoice>(e =>
        {
            e.ToTable("PurchaseInvoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.VendorInvoiceNumber).IsRequired().HasMaxLength(100);
            e.Property(x => x.VendorName).HasMaxLength(255);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.SubTotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
            e.Property(x => x.RecoverableTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.NonRecoverableTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.HoldReason).HasMaxLength(500);
            e.Property(x => x.DisputeReason).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.InvoiceNumber })
             .IsUnique().HasDatabaseName("IX_PurchaseInvoice_Company_Number");
            e.HasIndex(x => new { x.CompanyId, x.VendorId, x.VendorInvoiceNumber })
             .IsUnique().HasDatabaseName("IX_PurchaseInvoice_Vendor_VendorNumber");
            e.HasOne(x => x.Vendor).WithMany(v => v.PurchaseInvoices)
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PurchaseOrder).WithMany(po => po.PurchaseInvoices)
             .HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Lines).WithOne(l => l.Invoice)
             .HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.MatchRecords).WithOne(m => m.Invoice)
             .HasForeignKey(m => m.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PaymentAllocations).WithOne(p => p.Invoice)
             .HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── PurchaseInvoiceLine ───────────────────────────────────────────────
        modelBuilder.Entity<PurchaseInvoiceLine>(e =>
        {
            e.ToTable("PurchaseInvoiceLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.ItemCode).HasMaxLength(100);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitOfMeasureName).HasMaxLength(50);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(5, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.RecoverableTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.NonRecoverableTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);
            // NoAction: multiple cascade paths from PurchaseOrder through PurchaseOrderLine
            e.HasOne(x => x.PurchaseOrderLine).WithMany(l => l.InvoiceLines)
             .HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.GoodsReceiptLine).WithMany()
             .HasForeignKey(x => x.GoodsReceiptLineId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.ProcurementCategory).WithMany()
             .HasForeignKey(x => x.ProcurementCategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── ThreeWayMatchRecord ───────────────────────────────────────────────
        modelBuilder.Entity<ThreeWayMatchRecord>(e =>
        {
            e.ToTable("ThreeWayMatchRecords");
            e.HasKey(x => x.Id);
            e.Property(x => x.POQuantity).HasPrecision(18, 4);
            e.Property(x => x.ReceivedQuantity).HasPrecision(18, 4);
            e.Property(x => x.InvoicedQuantity).HasPrecision(18, 4);
            e.Property(x => x.QuantityVariance).HasPrecision(18, 4);
            e.Property(x => x.POUnitPrice).HasPrecision(18, 4);
            e.Property(x => x.InvoiceUnitPrice).HasPrecision(18, 4);
            e.Property(x => x.PriceVariance).HasPrecision(18, 4);
            e.Property(x => x.TotalVarianceAmount).HasPrecision(18, 2);
            e.Property(x => x.Resolution).HasMaxLength(1000);
            // NoAction on all secondary FKs to avoid cascade cycles through PurchaseInvoice
            e.HasOne(x => x.InvoiceLine).WithMany()
             .HasForeignKey(x => x.InvoiceLineId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PurchaseOrderLine).WithMany()
             .HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.GoodsReceiptLine).WithMany()
             .HasForeignKey(x => x.GoodsReceiptLineId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── VendorPayment ─────────────────────────────────────────────────────
        modelBuilder.Entity<VendorPayment>(e =>
        {
            e.ToTable("VendorPayments");
            e.HasKey(x => x.Id);
            e.Property(x => x.PaymentNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.VendorName).HasMaxLength(255);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            e.Property(x => x.UnallocatedAmount).HasPrecision(18, 2);
            e.Property(x => x.WithholdingTaxAmount).HasPrecision(18, 2);
            e.Property(x => x.BankReferenceNumber).HasMaxLength(100);
            e.Property(x => x.CheckNumber).HasMaxLength(100);
            e.Property(x => x.TransactionReference).HasMaxLength(200);
            e.HasIndex(x => new { x.CompanyId, x.PaymentNumber })
             .IsUnique().HasDatabaseName("IX_VendorPayment_Company_Number");
            e.HasOne(x => x.Vendor).WithMany(v => v.Payments)
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VendorBankAccount).WithMany()
             .HasForeignKey(x => x.VendorBankAccountId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Allocations).WithOne(a => a.Payment)
             .HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── VendorPaymentLine ─────────────────────────────────────────────────
        modelBuilder.Entity<VendorPaymentLine>(e =>
        {
            e.ToTable("VendorPaymentLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceTotalAmount).HasPrecision(18, 2);
            e.Property(x => x.InvoiceOutstandingAmount).HasPrecision(18, 2);
            e.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountTaken).HasPrecision(18, 2);
            e.Property(x => x.WriteOffAmount).HasPrecision(18, 2);
            e.Property(x => x.FXGainLossAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.PaymentId, x.InvoiceId })
             .IsUnique().HasDatabaseName("IX_VendorPaymentLine_Payment_Invoice");
            // NoAction: Vendor → VendorPayment (Restrict) and Vendor → PurchaseInvoice (Restrict)
            // both reach this table — use NoAction on the invoice side
            e.HasOne(x => x.Invoice).WithMany(i => i.PaymentAllocations)
             .HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── PurchaseReturn ────────────────────────────────────────────────────
        modelBuilder.Entity<PurchaseReturn>(e =>
        {
            e.ToTable("PurchaseReturns");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReturnNumber).IsRequired().HasMaxLength(50);
            e.HasIndex(x => new { x.CompanyId, x.ReturnNumber })
             .IsUnique().HasDatabaseName("IX_PurchaseReturn_Company_Number");
            e.HasOne(x => x.PurchaseOrder).WithMany()
             .HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.GoodsReceipt).WithMany()
             .HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.PurchaseReturn)
             .HasForeignKey(l => l.PurchaseReturnId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── PurchaseReturnLine ────────────────────────────────────────────────
        modelBuilder.Entity<PurchaseReturnLine>(e =>
        {
            e.ToTable("PurchaseReturnLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.QuantityReturned).HasPrecision(18, 4);
            e.Property(x => x.QuantityReceived).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalReturnAmount).HasPrecision(18, 2);
            e.HasOne(x => x.GoodsReceiptLine).WithMany()
             .HasForeignKey(x => x.GoodsReceiptLineId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PurchaseOrderLine).WithMany()
             .HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── VendorDebitNote ───────────────────────────────────────────────────
        modelBuilder.Entity<VendorDebitNote>(e =>
        {
            e.ToTable("VendorDebitNotes");
            e.HasKey(x => x.Id);
            e.Property(x => x.DebitNoteNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.DebitNoteNumber })
             .IsUnique().HasDatabaseName("IX_VendorDebitNote_Company_Number");
            e.HasOne(x => x.Vendor).WithMany()
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne(l => l.DebitNote)
             .HasForeignKey(l => l.DebitNoteId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── VendorDebitNoteLine ───────────────────────────────────────────────
        modelBuilder.Entity<VendorDebitNoteLine>(e =>
        {
            e.ToTable("VendorDebitNoteLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasOne(x => x.ReturnLine).WithMany()
             .HasForeignKey(x => x.ReturnLineId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── LandedCost ────────────────────────────────────────────────────────
        modelBuilder.Entity<LandedCost>(e =>
        {
            e.ToTable("LandedCosts");
            e.HasKey(x => x.Id);
            e.Property(x => x.LandedCostNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.TotalLandedCostAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.LandedCostNumber })
             .IsUnique().HasDatabaseName("IX_LandedCost_Company_Number");
            e.HasMany(x => x.CostLines).WithOne(l => l.LandedCost)
             .HasForeignKey(l => l.LandedCostId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.GoodsReceipts).WithOne(g => g.LandedCost)
             .HasForeignKey(g => g.LandedCostId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Allocations).WithOne(a => a.LandedCost)
             .HasForeignKey(a => a.LandedCostId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── LandedCostLine ────────────────────────────────────────────────────
        modelBuilder.Entity<LandedCostLine>(e =>
        {
            e.ToTable("LandedCostLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });

        // ── LandedCostGoodsReceipt ────────────────────────────────────────────
        modelBuilder.Entity<LandedCostGoodsReceipt>(e =>
        {
            e.ToTable("LandedCostGoodsReceipts");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.GoodsReceipt).WithMany()
             .HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── LandedCostAllocation ──────────────────────────────────────────────
        modelBuilder.Entity<LandedCostAllocation>(e =>
        {
            e.ToTable("LandedCostAllocations");
            e.HasKey(x => x.Id);
            e.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            e.HasOne(x => x.GoodsReceiptLine).WithMany()
             .HasForeignKey(x => x.GoodsReceiptLineId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── ApprovalWorkflow ──────────────────────────────────────────────────
        modelBuilder.Entity<ApprovalWorkflow>(e =>
        {
            e.ToTable("ApprovalWorkflows");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.MinimumAmount).HasPrecision(18, 2);
            e.Property(x => x.MaximumAmount).HasPrecision(18, 2);
            e.HasMany(x => x.Steps).WithOne(s => s.Workflow)
             .HasForeignKey(s => s.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── ApprovalWorkflowStep ──────────────────────────────────────────────
        modelBuilder.Entity<ApprovalWorkflowStep>(e =>
        {
            e.ToTable("ApprovalWorkflowSteps");
            e.HasKey(x => x.Id);
            e.Property(x => x.StepName).IsRequired().HasMaxLength(255);
            e.Property(x => x.ApproverRole).HasMaxLength(100);
            e.Property(x => x.AmountThreshold).HasPrecision(18, 2);
        });

        // ── ProcurementApproval ───────────────────────────────────────────────
        modelBuilder.Entity<ProcurementApproval>(e =>
        {
            e.ToTable("ProcurementApprovals");
            e.HasKey(x => x.Id);
            e.Property(x => x.StepName).HasMaxLength(255);
            e.Property(x => x.ApproverName).HasMaxLength(255);
            e.Property(x => x.Comments).HasMaxLength(1000);
            e.Property(x => x.DelegatedToName).HasMaxLength(255);
            e.Property(x => x.DelegationReason).HasMaxLength(500);
            e.HasIndex(x => new { x.DocumentType, x.DocumentId })
             .HasDatabaseName("IX_ProcurementApproval_Document");
            e.HasOne(x => x.Workflow).WithMany()
             .HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.WorkflowStep).WithMany()
             .HasForeignKey(x => x.WorkflowStepId).OnDelete(DeleteBehavior.NoAction);
            // Requisition nav is optional — approval may belong to PO, invoice, etc.
            e.HasOne(x => x.Requisition).WithMany(r => r.Approvals)
             .HasForeignKey(x => x.DocumentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ── DocumentSequence ──────────────────────────────────────────────────
        modelBuilder.Entity<DocumentSequence>(e =>
        {
            e.ToTable("DocumentSequences");
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentType).HasConversion<string>().IsRequired().HasMaxLength(50);
            e.Property(x => x.Prefix).IsRequired().HasMaxLength(20);
            e.Property(x => x.Suffix).HasMaxLength(20);
            e.Property(x => x.Separator).IsRequired().HasMaxLength(5);
            e.HasIndex(x => new { x.CompanyId, x.DocumentType })
             .IsUnique().HasDatabaseName("IX_DocumentSequence_Company_Type");
        });

        // ── ProcurementSettings ───────────────────────────────────────────────
        modelBuilder.Entity<ProcurementSettings>(e =>
        {
            e.ToTable("ProcurementSettings");
            e.HasKey(x => x.Id);
            e.Property(x => x.RFQMandatoryAboveAmount).HasPrecision(18, 2);
            e.Property(x => x.InvoicePOTolerancePercent).HasPrecision(5, 2);
            e.Property(x => x.DefaultCurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            // Three nullable FKs to the same ApprovalWorkflows table — SQL Server rejects
            // multiple SET NULL cascade paths (error 1785), so use NoAction. Cleanup is
            // handled in application logic (soft-delete), so DB-level cascade is unnecessary.
            e.HasOne(x => x.POApprovalWorkflow).WithMany()
             .HasForeignKey(x => x.POApprovalWorkflowId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.RequisitionApprovalWorkflow).WithMany()
             .HasForeignKey(x => x.RequisitionApprovalWorkflowId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.InvoiceApprovalWorkflow).WithMany()
             .HasForeignKey(x => x.InvoiceApprovalWorkflowId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Global: neutralise DB-level cascade. This originally worked around SQL Server's
        // "multiple cascade paths / cycles" restriction (error 1785), which PostgreSQL does
        // not have — but the behaviour is kept deliberately: this module soft-deletes
        // everywhere, so database cascade is unnecessary, and changing it now would alter
        // delete semantics rather than just the provider. ClientCascade/ClientSetNull keep
        // EF's in-memory orphan handling working (e.g. replacing pricelist lines) while the
        // generated FKs use ON DELETE NO ACTION.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            if (fk.DeleteBehavior == DeleteBehavior.Cascade)
                fk.DeleteBehavior = DeleteBehavior.ClientCascade;
            else if (fk.DeleteBehavior == DeleteBehavior.SetNull)
                fk.DeleteBehavior = DeleteBehavior.ClientSetNull;
        }

        // Cross-cutting rules shared by every module: UTC normalisation for all
        // DateTime properties and the xmin optimistic-concurrency token. Must stay
        // last so it sees owned-type and DbSet-less properties configured above.
        modelBuilder.ApplyNexcoreConventions();
    }
}
