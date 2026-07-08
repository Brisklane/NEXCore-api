using Microsoft.EntityFrameworkCore;
using Sales.Domain.Entities;

namespace Sales.Infrastructure.Persistence;

/// <summary>
/// Sales module DbContext - Schema: sales
///
/// Contact (Customer) master is owned by Crm. Sales references contacts
/// by ContactId (cross-module Guid) - no navigation properties.
///
/// Segmentation     : CustomerGroup, SalesTerritory, SalesRepTerritory
/// Pricing          : PriceList, PriceListItem, DiscountScheme
/// Tax Engine       : TaxRate, TaxGroup, TaxGroupRate, TaxRule
/// Approval Engine  : ApprovalPolicy, ApprovalPolicyCondition, ApprovalPolicyStep, ApprovalRequest
/// Commission       : CommissionRule, CommissionEntry, SalesTarget
/// Quotation        : Quotation, QuotationLine, QuotationApproval
/// Sales Order      : SalesOrder, SalesOrderLine, SalesOrderLineAddon
///                    SalesOrderApproval, SalesOrderAttachment, SalesOrderStatusHistory
/// Delivery         : Delivery, DeliveryLine, DeliveryPackage
/// Invoice & Pay    : SalesInvoice, SalesInvoiceLine, SalesPayment
///                    CreditNote, CreditNoteLine
/// Returns          : SalesReturn, SalesReturnLine
/// Agreement        : SalesAgreement, SalesAgreementLine
/// POS              : PosStore, PosTerminal, PosCashier, PosSession
///                    PosTransaction, PosTransactionLine, PosPayment
///                    PosCashMovement, PosCashDrawer, PosCashDrawerEvent
///                    PosReceiptTemplate, PosStoreSchedule, PosStoreHoliday
///                    PosGiftCard, PosGiftCardTransaction
/// Online / App     : StoreMenu, StoreMenuSection, StoreOffer
/// Rider            : Rider, RiderAssignment, RiderLocationLog, RiderShift, RiderRating
///                    DeliveryZone
/// Loyalty          : LoyaltyProgram, LoyaltyAccount, LoyaltyTransaction
/// Promotions       : Promotion, PromotionItem, Coupon, CouponUsage
/// App              : WishlistItem, ProductReview, ProductReviewImage, AppNotification
/// </summary>
public class SalesDbContext : DbContext
{
    private const string Schema = "sales";

    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    // Segmentation
    public DbSet<CustomerGroup> CustomerGroups { get; set; } = null!;
    public DbSet<SalesTerritory> SalesTerritories { get; set; } = null!;
    public DbSet<SalesRepTerritory> SalesRepTerritories { get; set; } = null!;

    // Tax Engine
    /// <summary>
    /// TaxRate (individual rate components) lives in Inventory as TaxDefinition.
    /// Sales owns TaxGroup, TaxGroupRate, and TaxRule only.
    /// TaxGroupRate.TaxDefinitionId is a cross-module Guid - no navigation.
    /// </summary>
    public DbSet<TaxGroup> TaxGroups { get; set; } = null!;
    public DbSet<TaxGroupRate> TaxGroupRates { get; set; } = null!;
    public DbSet<TaxRule> TaxRules { get; set; } = null!;

    // Approval Engine
    public DbSet<ApprovalPolicy> ApprovalPolicies { get; set; } = null!;
    public DbSet<ApprovalPolicyCondition> ApprovalPolicyConditions { get; set; } = null!;
    public DbSet<ApprovalPolicyStep> ApprovalPolicySteps { get; set; } = null!;
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; } = null!;

    // Commission
    public DbSet<CommissionRule> CommissionRules { get; set; } = null!;
    public DbSet<CommissionEntry> CommissionEntries { get; set; } = null!;
    public DbSet<SalesTarget> SalesTargets { get; set; } = null!;

    // Pricing
    public DbSet<PriceList> PriceLists { get; set; } = null!;
    public DbSet<PriceListItem> PriceListItems { get; set; } = null!;
    public DbSet<DiscountScheme> DiscountSchemes { get; set; } = null!;

    // Quotation
    public DbSet<Quotation> Quotations { get; set; } = null!;
    public DbSet<QuotationLine> QuotationLines { get; set; } = null!;
    public DbSet<QuotationApproval> QuotationApprovals { get; set; } = null!;

    // Sales Order
    public DbSet<SalesOrder> SalesOrders { get; set; } = null!;
    public DbSet<SalesOrderLine> SalesOrderLines { get; set; } = null!;
    public DbSet<SalesOrderLineAddon> SalesOrderLineAddons { get; set; } = null!;
    public DbSet<SalesOrderApproval> SalesOrderApprovals { get; set; } = null!;
    public DbSet<SalesOrderAttachment> SalesOrderAttachments { get; set; } = null!;
    public DbSet<SalesOrderStatusHistory> SalesOrderStatusHistories { get; set; } = null!;

    // Delivery
    public DbSet<Delivery> Deliveries { get; set; } = null!;
    public DbSet<DeliveryLine> DeliveryLines { get; set; } = null!;
    public DbSet<DeliveryPackage> DeliveryPackages { get; set; } = null!;

    // Document Sequences
    public DbSet<DocumentSequence> DocumentSequences { get; set; } = null!;

    // Currency & Exchange Rates
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<CurrencyRate> CurrencyRates { get; set; } = null!;

    // Sales Teams
    public DbSet<SalesTeam> SalesTeams { get; set; } = null!;
    public DbSet<SalesTeamMember> SalesTeamMembers { get; set; } = null!;

    // Invoice & Payment
    public DbSet<SalesInvoice> SalesInvoices { get; set; } = null!;
    public DbSet<SalesInvoiceLine> SalesInvoiceLines { get; set; } = null!;
    public DbSet<SalesPayment> SalesPayments { get; set; } = null!;
    public DbSet<PaymentAllocation> PaymentAllocations { get; set; } = null!;
    public DbSet<CreditNote> CreditNotes { get; set; } = null!;
    public DbSet<CreditNoteLine> CreditNoteLines { get; set; } = null!;

    // Returns
    public DbSet<SalesReturn> SalesReturns { get; set; } = null!;
    public DbSet<SalesReturnLine> SalesReturnLines { get; set; } = null!;

    // Agreement
    public DbSet<SalesAgreement> SalesAgreements { get; set; } = null!;
    public DbSet<SalesAgreementLine> SalesAgreementLines { get; set; } = null!;

    // POS
    public DbSet<PosSettings> PosSettings { get; set; } = null!;
    public DbSet<PosStore> PosStores { get; set; } = null!;
    public DbSet<PosTerminal> PosTerminals { get; set; } = null!;
    public DbSet<PosCashier> PosCashiers { get; set; } = null!;
    public DbSet<PosSession> PosSessions { get; set; } = null!;
    public DbSet<PosTransaction> PosTransactions { get; set; } = null!;
    public DbSet<PosTransactionLine> PosTransactionLines { get; set; } = null!;
    public DbSet<PosPayment> PosPayments { get; set; } = null!;
    public DbSet<PosCashMovement> PosCashMovements { get; set; } = null!;
    public DbSet<PosCashDrawer> PosCashDrawers { get; set; } = null!;
    public DbSet<PosCashDrawerEvent> PosCashDrawerEvents { get; set; } = null!;
    public DbSet<PosReceiptTemplate> PosReceiptTemplates { get; set; } = null!;
    public DbSet<PosBarcodeLabelTemplate> PosBarcodeLabelTemplates { get; set; } = null!;
    public DbSet<PosStoreSchedule> PosStoreSchedules { get; set; } = null!;
    public DbSet<PosStoreHoliday> PosStoreHolidays { get; set; } = null!;
    public DbSet<PosGiftCard> PosGiftCards { get; set; } = null!;
    public DbSet<PosGiftCardTransaction> PosGiftCardTransactions { get; set; } = null!;

    // Online / App
    public DbSet<StoreMenu> StoreMenus { get; set; } = null!;
    public DbSet<StoreMenuSection> StoreMenuSections { get; set; } = null!;
    public DbSet<StoreOffer> StoreOffers { get; set; } = null!;
    public DbSet<StoreVendorProfile> StoreVendorProfiles { get; set; } = null!;

    // Rider
    public DbSet<Rider> Riders { get; set; } = null!;
    public DbSet<RiderAssignment> RiderAssignments { get; set; } = null!;
    public DbSet<RiderLocationLog> RiderLocationLogs { get; set; } = null!;
    public DbSet<RiderShift> RiderShifts { get; set; } = null!;
    public DbSet<RiderRating> RiderRatings { get; set; } = null!;
    public DbSet<DeliveryZone> DeliveryZones { get; set; } = null!;

    // Loyalty
    public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; } = null!;
    public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; } = null!;
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; } = null!;

    // Promotions
    public DbSet<Promotion> Promotions { get; set; } = null!;
    public DbSet<PromotionItem> PromotionItems { get; set; } = null!;
    public DbSet<Coupon> Coupons { get; set; } = null!;
    public DbSet<CouponUsage> CouponUsages { get; set; } = null!;

    // App
    public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
    public DbSet<ProductReview> ProductReviews { get; set; } = null!;
    public DbSet<ProductReviewImage> ProductReviewImages { get; set; } = null!;
    public DbSet<AppNotification> AppNotifications { get; set; } = null!;

    // Stored files (SQL-backed image storage)
    public DbSet<StoredFile> StoredFiles { get; set; } = null!;

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

        ApplyUtcDateTimeConverters(modelBuilder);

        // Stored File (SQL-backed image bytes)
        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("StoredFiles", Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Content).IsRequired().HasColumnType("varbinary(max)");
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId })
                .HasDatabaseName("IX_StoredFile_Tenant");
        });

        // CustomerGroup
        modelBuilder.Entity<CustomerGroup>(e =>
        {
            e.ToTable("CustomerGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        // ?? PosStore - trading name unique per company (active, named stores) ?
        modelBuilder.Entity<PosStore>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.TradingName })
             .IsUnique()
             .HasDatabaseName("IX_PosStore_Company_TradingName")
             .HasFilter("[IsDeleted] = 0 AND [TradingName] IS NOT NULL");
        });

        // SalesTerritory
        modelBuilder.Entity<SalesTerritory>(e =>
        {
            e.ToTable("SalesTerritories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.HasOne(x => x.ParentTerritory).WithMany(x => x.SubTerritories)
             .HasForeignKey(x => x.ParentTerritoryId).OnDelete(DeleteBehavior.NoAction);
        });

        // Tax Engine
        modelBuilder.Entity<TaxGroup>(e =>
        {
            e.ToTable("TaxGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.Code })
             .IsUnique().HasDatabaseName("IX_TaxGroup_Tenant_Code");
            e.HasMany(x => x.Rates).WithOne(r => r.TaxGroup).HasForeignKey(r => r.TaxGroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Rules).WithOne(r => r.TaxGroup).HasForeignKey(r => r.TaxGroupId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaxGroupRate>(e =>
        {
            e.ToTable("TaxGroupRates");
            e.HasKey(x => x.Id);
            e.Property(x => x.SnapshotCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.SnapshotRate).HasPrecision(8, 4);
            e.Property(x => x.SnapshotTaxType).IsRequired().HasMaxLength(50);
            e.Property(x => x.SnapshotInclusionType).IsRequired().HasMaxLength(50);
            // TaxDefinitionId is a cross-module Guid - no EF FK or navigation
            e.HasIndex(x => new { x.TaxGroupId, x.TaxDefinitionId })
             .IsUnique().HasDatabaseName("IX_TaxGroupRate_Group_TaxDef");
        });

        modelBuilder.Entity<TaxRule>(e =>
        {
            e.ToTable("TaxRules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.CustomerCountryCode).HasMaxLength(2);
            e.Property(x => x.CustomerType).HasMaxLength(50);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.Priority, x.IsActive })
             .HasDatabaseName("IX_TaxRule_Tenant_Priority");
        });

        // Approval Engine
        modelBuilder.Entity<ApprovalPolicy>(e =>
        {
            e.ToTable("ApprovalPolicies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.HasMany(x => x.Conditions).WithOne(c => c.ApprovalPolicy).HasForeignKey(c => c.ApprovalPolicyId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Steps).WithOne(s => s.ApprovalPolicy).HasForeignKey(s => s.ApprovalPolicyId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Requests).WithOne(r => r.ApprovalPolicy).HasForeignKey(r => r.ApprovalPolicyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalPolicyCondition>(e =>
        {
            e.ToTable("ApprovalPolicyConditions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ApprovalPolicyStep>(e =>
        {
            e.ToTable("ApprovalPolicySteps");
            e.HasKey(x => x.Id);
            e.Property(x => x.StepName).IsRequired().HasMaxLength(255);
            e.Property(x => x.ApproverRole).HasMaxLength(100);
            e.Property(x => x.EscalationRole).HasMaxLength(100);
        });

        modelBuilder.Entity<ApprovalRequest>(e =>
        {
            e.ToTable("ApprovalRequests");
            e.HasKey(x => x.Id);
            e.Property(x => x.AssignedRole).HasMaxLength(100);
            e.Property(x => x.Comments).HasMaxLength(1000);
            e.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ApprovalPolicyStep).WithMany().HasForeignKey(x => x.ApprovalPolicyStepId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.SalesOrderId, x.StepOrder })
             .HasDatabaseName("IX_ApprovalRequest_Order_Step");
        });

        // Commission
        modelBuilder.Entity<CommissionRule>(e =>
        {
            e.ToTable("CommissionRules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.Rate).HasPrecision(8, 4);
            e.Property(x => x.MaxCommissionAmount).HasPrecision(18, 2);
            e.Property(x => x.RecognitionEvent).HasMaxLength(50);
        });

        modelBuilder.Entity<CommissionEntry>(e =>
        {
            e.ToTable("CommissionEntries");
            e.HasKey(x => x.Id);
            e.Property(x => x.BaseAmount).HasPrecision(18, 2);
            e.Property(x => x.CommissionRate).HasPrecision(8, 4);
            e.Property(x => x.CommissionAmount).HasPrecision(18, 2);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CommissionRule).WithMany().HasForeignKey(x => x.CommissionRuleId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SalesRepId, x.Status, x.EarnedDate })
             .HasDatabaseName("IX_CommissionEntry_Rep_Status_Date");
        });

        modelBuilder.Entity<SalesTarget>(e =>
        {
            e.ToTable("SalesTargets");
            e.HasKey(x => x.Id);
            e.Property(x => x.TargetAmount).HasPrecision(18, 2);
            e.Property(x => x.TargetQuantity).HasPrecision(18, 4);
            e.Property(x => x.ActualAmount).HasPrecision(18, 2);
            e.Property(x => x.ActualQuantity).HasPrecision(18, 4);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Ignore(x => x.AttainmentPercentage);
            e.HasIndex(x => new { x.SalesRepId, x.Period, x.PeriodStart })
             .HasDatabaseName("IX_SalesTarget_Rep_Period");
        });

        // PriceList
        modelBuilder.Entity<PriceList>(e =>
        {
            e.ToTable("PriceLists");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.Code })
             .IsUnique().HasDatabaseName("IX_PriceList_Tenant_Code");
            e.HasMany(x => x.Items).WithOne(i => i.PriceList).HasForeignKey(i => i.PriceListId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PriceListItem>(e =>
        {
            e.ToTable("PriceListItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.MinQuantity).HasPrecision(18, 4);
        });

        // Quotation
        modelBuilder.Entity<Quotation>(e =>
        {
            e.ToTable("Quotations");
            e.HasKey(x => x.Id);
            e.Property(x => x.QuotationNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.QuotationNumber })
             .IsUnique().HasDatabaseName("IX_Quotation_Tenant_Number");
            e.HasMany(x => x.Lines).WithOne(l => l.Quotation).HasForeignKey(l => l.QuotationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Approvals).WithOne(a => a.Quotation).HasForeignKey(a => a.QuotationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuotationLine>(e =>
        {
            e.ToTable("QuotationLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        // SalesOrder
        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.ToTable("SalesOrders");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.ShippingAmount).HasPrecision(18, 2);
            e.Property(x => x.TipAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.BalanceDue).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.OrderNumber })
             .IsUnique().HasDatabaseName("IX_SalesOrder_Tenant_Number");
            e.HasIndex(x => new { x.OriginBranchId, x.Status, x.PlacedAt })
             .HasDatabaseName("IX_SalesOrder_Branch_Status_PlacedAt");
            e.HasIndex(x => new { x.OfflineOrderNumber })
             .HasDatabaseName("IX_SalesOrder_OfflineOrderNumber");
            e.HasIndex(x => new { x.ContactId, x.Status })
             .HasDatabaseName("IX_SalesOrder_Contact_Status");
            e.HasMany(x => x.Lines).WithOne(l => l.SalesOrder).HasForeignKey(l => l.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payments).WithOne(p => p.SalesOrder).HasForeignKey(p => p.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Invoices).WithOne(i => i.SalesOrder).HasForeignKey(i => i.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Deliveries).WithOne(d => d.SalesOrder).HasForeignKey(d => d.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.StatusHistory).WithOne(h => h.SalesOrder).HasForeignKey(h => h.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.OriginPosTerminal).WithMany()
             .HasForeignKey(x => x.OriginPosTerminalId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.OriginPosCashier).WithMany()
             .HasForeignKey(x => x.OriginPosCashierId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.OriginPosSession).WithMany()
             .HasForeignKey(x => x.OriginPosSessionId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Quotation).WithMany().HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SalesAgreement).WithMany(a => a.ReleasedOrders).HasForeignKey(x => x.SalesAgreementId).OnDelete(DeleteBehavior.SetNull);

            // Separate 1:1 relationship
            // SalesOrder.ClosingPosTransactionId is the FK on the dependent side.
            // PosTransaction.SalesOrder is a DIFFERENT many:1 relationship - configured on PosTransaction below.
            e.HasOne(x => x.ClosingPosTransaction).WithOne()
             .HasForeignKey<SalesOrder>(x => x.ClosingPosTransactionId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SalesOrderLine>(e =>
        {
            e.ToTable("SalesOrderLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasMany(x => x.Addons).WithOne(a => a.SalesOrderLine).HasForeignKey(a => a.SalesOrderLineId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.DeliveryLines).WithOne(dl => dl.SalesOrderLine).HasForeignKey(dl => dl.SalesOrderLineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrderLineAddon>(e =>
        {
            e.ToTable("SalesOrderLineAddons");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
        });

        modelBuilder.Entity<SalesOrderStatusHistory>(e =>
        {
            e.ToTable("SalesOrderStatusHistories");
            e.HasKey(x => x.Id);
            e.Property(x => x.ChangedBy).HasMaxLength(100);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.SalesOrderId, x.ChangedAt })
             .HasDatabaseName("IX_SalesOrderStatusHistory_Order_Date");
        });

        // Delivery
        modelBuilder.Entity<Delivery>(e =>
        {
            e.ToTable("Deliveries");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeliveryNumber).IsRequired().HasMaxLength(50);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.DeliveryNumber })
             .IsUnique().HasDatabaseName("IX_Delivery_Tenant_Number");
            e.HasMany(x => x.Lines).WithOne(l => l.Delivery).HasForeignKey(l => l.DeliveryId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Packages).WithOne(p => p.Delivery).HasForeignKey(p => p.DeliveryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeliveryLine>(e =>
        {
            e.ToTable("DeliveryLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeliveredQuantity).HasPrecision(18, 4);
        });

        // Invoice & Payment
        modelBuilder.Entity<SalesInvoice>(e =>
        {
            e.ToTable("SalesInvoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.BalanceDue).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.InvoiceNumber })
             .IsUnique().HasDatabaseName("IX_SalesInvoice_Tenant_Number");
            e.HasMany(x => x.Lines).WithOne(l => l.SalesInvoice).HasForeignKey(l => l.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Allocations).WithOne(a => a.Invoice).HasForeignKey(a => a.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.CreditNotes).WithOne(cn => cn.SalesInvoice).HasForeignKey(cn => cn.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesInvoiceLine>(e =>
        {
            e.ToTable("SalesInvoiceLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SalesPayment>(e =>
        {
            e.ToTable("SalesPayments");
            e.HasKey(x => x.Id);
            e.Property(x => x.PaymentNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RefundedAmount).HasPrecision(18, 2);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.PaymentMethod).HasMaxLength(50);
            e.HasOne(x => x.PosTransaction).WithMany().HasForeignKey(x => x.PosTransactionId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Allocations).WithOne(a => a.Payment).HasForeignKey(a => a.SalesPaymentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.PaymentNumber })
             .IsUnique().HasDatabaseName("IX_SalesPayment_Tenant_Number");
        });

        modelBuilder.Entity<PaymentAllocation>(e =>
        {
            e.ToTable("PaymentAllocations");
            e.HasKey(x => x.Id);
            e.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.SalesPaymentId, x.SalesInvoiceId })
             .IsUnique().HasDatabaseName("IX_PaymentAllocation_Payment_Invoice");
            e.HasIndex(x => x.SalesInvoiceId).HasDatabaseName("IX_PaymentAllocation_Invoice");
        });

        modelBuilder.Entity<CreditNote>(e =>
        {
            e.ToTable("CreditNotes");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreditNoteNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasMany(x => x.Lines).WithOne(l => l.CreditNote).HasForeignKey(l => l.CreditNoteId).OnDelete(DeleteBehavior.Cascade);
        });

        // SalesReturn
        // SalesReturnLine is reachable from SalesOrder via TWO paths:
        //   Path 1: SalesOrder ?(Cascade)? SalesReturn ?(Cascade)? SalesReturnLine
        //   Path 2: SalesOrder ?(Cascade)? SalesOrderLine ?(?)? SalesReturnLine
        // Both non-owning FKs must be NoAction to satisfy SQL Server.
        modelBuilder.Entity<SalesReturn>(e =>
        {
            e.ToTable("SalesReturns");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReturnNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.TotalRefundAmount).HasPrecision(18, 2);
            e.HasMany(x => x.Lines).WithOne(l => l.SalesReturn)
             .HasForeignKey(l => l.SalesReturnId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SalesOrder).WithMany()
             .HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.SalesInvoice).WithMany()
             .HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.CreditNote).WithMany()
             .HasForeignKey(x => x.CreditNoteId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.ReturnNumber })
             .IsUnique().HasDatabaseName("IX_SalesReturn_Tenant_Number");
        });

        modelBuilder.Entity<SalesReturnLine>(e =>
        {
            e.ToTable("SalesReturnLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.RefundAmount).HasPrecision(18, 2);
            // NoAction: second cascade path from SalesOrder ? SalesOrderLine ? SalesReturnLine
            e.HasOne(x => x.SalesOrderLine).WithMany()
             .HasForeignKey(x => x.SalesOrderLineId).OnDelete(DeleteBehavior.NoAction);
        });

        // SalesOrderApproval / SalesOrderAttachment
        modelBuilder.Entity<SalesOrderApproval>(e =>
        {
            e.ToTable("SalesOrderApprovals");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<SalesOrderAttachment>(e =>
        {
            e.ToTable("SalesOrderAttachments");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(255);
        });

        // PosGiftCard
        modelBuilder.Entity<PosGiftCard>(e =>
        {
            e.ToTable("PosGiftCards");
            e.HasKey(x => x.Id);
            e.Property(x => x.CardNumber).IsRequired().HasMaxLength(100);
            e.Property(x => x.CurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.OriginalBalance).HasPrecision(18, 2);
            e.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.CardNumber })
             .IsUnique().HasDatabaseName("IX_PosGiftCard_Tenant_Number");
            e.HasMany(x => x.Transactions).WithOne(t => t.PosGiftCard)
             .HasForeignKey(t => t.PosGiftCardId).OnDelete(DeleteBehavior.Cascade);
            // NoAction: PosTransaction is inside the PosStore cascade tree
            e.HasOne(x => x.IssuedInTransaction).WithMany()
             .HasForeignKey(x => x.IssuedInTransactionId).OnDelete(DeleteBehavior.NoAction);
        });

        // PosGiftCardTransaction
        modelBuilder.Entity<PosGiftCardTransaction>(e =>
        {
            e.ToTable("PosGiftCardTransactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.TransactionType).IsRequired().HasMaxLength(50);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            // NoAction: PosTransaction is inside the PosStore cascade tree
            e.HasOne(x => x.PosTransaction).WithMany()
             .HasForeignKey(x => x.PosTransactionId).OnDelete(DeleteBehavior.NoAction);
        });

        // RiderAssignment (SalesOrder FK)
        modelBuilder.Entity<RiderAssignment>(e =>
        {
            e.ToTable("RiderAssignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.AssignmentNumber).IsRequired().HasMaxLength(50);
            e.HasMany(x => x.LocationLogs).WithOne(l => l.RiderAssignment)
             .HasForeignKey(l => l.RiderAssignmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SalesOrder).WithMany()
             .HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PosStore>(e =>
        {
            e.ToTable("PosStores", "sales");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.CountryCode).HasMaxLength(2).IsUnicode(false);
            e.Property(x => x.TradingName).HasMaxLength(300);
            e.Property(x => x.MinOnlineOrderAmount).HasPrecision(18, 2);
            e.HasOne(x => x.DefaultPriceList).WithMany()
             .HasForeignKey(x => x.DefaultPriceListId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ReceiptTemplate).WithMany()
             .HasForeignKey(x => x.ReceiptTemplateId).OnDelete(DeleteBehavior.SetNull);
            e.OwnsOne(x => x.Location, g =>
            {
                g.Property(p => p.Latitude).HasColumnName("Latitude").HasPrecision(10, 7);
                g.Property(p => p.Longitude).HasColumnName("Longitude").HasPrecision(10, 7);
            });
        });

        modelBuilder.Entity<PosTerminal>(e =>
        {
            e.ToTable("PosTerminals");
            e.HasKey(x => x.Id);
            e.Property(x => x.TerminalCode).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.PosStore).WithMany(s => s.Terminals).HasForeignKey(x => x.PosStoreId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Sessions).WithOne(s => s.PosTerminal).HasForeignKey(s => s.PosTerminalId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PosCashier>(e =>
        {
            e.ToTable("PosCashiers");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
            e.HasMany(x => x.Sessions).WithOne(s => s.PosCashier).HasForeignKey(s => s.PosCashierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PosSession>(e =>
        {
            e.ToTable("PosSessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.SessionNumber).IsRequired().HasMaxLength(50);
            e.HasMany(x => x.Transactions).WithOne(t => t.PosSession).HasForeignKey(t => t.PosSessionId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.CashMovements).WithOne(cm => cm.PosSession).HasForeignKey(cm => cm.PosSessionId).OnDelete(DeleteBehavior.Cascade);
            // One open session per terminal (one terminal = one cash drawer). Filtered unique index
            // so closed/deleted sessions don't count — the DB enforces this even under a check-in race.
            e.HasIndex(x => x.PosTerminalId)
                .IsUnique()
                .HasFilter("[Status] = 0 AND [IsDeleted] = 0")
                .HasDatabaseName("UX_PosSessions_OpenPerTerminal");
        });

        modelBuilder.Entity<PosTransaction>(e =>
        {
            e.ToTable("PosTransactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.TransactionNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.TransactionNumber })
             .IsUnique().HasDatabaseName("IX_PosTransaction_Tenant_Number");
            e.HasMany(x => x.Lines).WithOne(l => l.PosTransaction).HasForeignKey(l => l.PosTransactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payments).WithOne(p => p.PosTransaction).HasForeignKey(p => p.PosTransactionId).OnDelete(DeleteBehavior.Cascade);

            // NoAction on all four POS origin FKs - PosStore cascades to PosTerminal, PosCashier,
            // and PosSession, so SQL Server detects multiple cascade paths to SalesOrders
            // if any of these remain SetNull/Cascade.
            e.HasOne(x => x.PosStore).WithMany()
             .HasForeignKey(x => x.PosStoreId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PosTerminal).WithMany()
             .HasForeignKey(x => x.PosTerminalId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PosCashier).WithMany()
             .HasForeignKey(x => x.PosCashierId).OnDelete(DeleteBehavior.NoAction);

            // PosSession already owns the Restrict delete on its Transactions collection -
            // align here so both sides agree.
            e.HasOne(x => x.PosSession).WithMany(s => s.Transactions)
             .HasForeignKey(x => x.PosSessionId).OnDelete(DeleteBehavior.NoAction);

            // Self-referencing refund/void link
            e.HasOne(x => x.OriginalTransaction).WithMany()
             .HasForeignKey(x => x.OriginalTransactionId).OnDelete(DeleteBehavior.NoAction);

            // Many:1 to SalesOrder (separate from SalesOrder.ClosingPosTransaction 1:1)
            e.HasOne(x => x.SalesOrder).WithMany()
             .HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.SetNull);
        });

        // PosCashDrawer
        modelBuilder.Entity<PosCashDrawer>(e =>
        {
            e.ToTable("PosCashDrawers");
            e.HasKey(x => x.Id);
            e.Property(x => x.DrawerCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.DrawerLabel).HasMaxLength(100);
            e.HasMany(x => x.Events).WithOne(ev => ev.PosCashDrawer)
             .HasForeignKey(ev => ev.PosCashDrawerId).OnDelete(DeleteBehavior.Cascade);
        });

        // PosCashDrawerEvent
        // Audit log: all FKs use NoAction to eliminate SQL Server cascade cycle errors.
        // Cascade paths PosStore?PosCashier and PosStore?PosSession both reach this table.
        modelBuilder.Entity<PosCashDrawerEvent>(e =>
        {
            e.ToTable("PosCashDrawerEvents");
            e.HasKey(x => x.Id);
            e.Property(x => x.OpenReason).IsRequired().HasMaxLength(100);
            // PosCashDrawer already owns the Cascade delete - no second cascade needed here
            e.HasOne(x => x.PosCashDrawer).WithMany(d => d.Events)
             .HasForeignKey(x => x.PosCashDrawerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PosSession).WithMany()
             .HasForeignKey(x => x.PosSessionId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Cashier).WithMany()
             .HasForeignKey(x => x.CashierId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PosTransaction).WithMany()
             .HasForeignKey(x => x.PosTransactionId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.PosCashDrawerId, x.OpenedAt })
             .HasDatabaseName("IX_PosCashDrawerEvent_Drawer_Date");
        });

        // Rider
        modelBuilder.Entity<Rider>(e =>
        {
            e.ToTable("Riders");
            e.HasKey(x => x.Id);
            e.Property(x => x.RiderCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.Phone).IsRequired().HasMaxLength(50);
            e.Property(x => x.AverageRating).HasPrecision(3, 2);
            e.HasMany(x => x.Assignments).WithOne(a => a.Rider).HasForeignKey(a => a.RiderId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Shifts).WithOne(s => s.Rider).HasForeignKey(s => s.RiderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Ratings).WithOne(r => r.Rider).HasForeignKey(r => r.RiderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RiderAssignment>(e =>
        {
            e.ToTable("RiderAssignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.AssignmentNumber).IsRequired().HasMaxLength(50);
            e.HasMany(x => x.LocationLogs).WithOne(l => l.RiderAssignment).HasForeignKey(l => l.RiderAssignmentId).OnDelete(DeleteBehavior.Cascade);
        });

        // Store Menu
        modelBuilder.Entity<StoreMenu>(e =>
        {
            e.ToTable("StoreMenus");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.HasMany(x => x.Sections).WithOne(s => s.StoreMenu).HasForeignKey(s => s.StoreMenuId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoreMenuSection>(e =>
        {
            e.ToTable("StoreMenuSections");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.StoreMenuId, x.ItemCategoryId })
             .IsUnique().HasDatabaseName("IX_StoreMenuSection_Menu_Category");
        });

        modelBuilder.Entity<StoreVendorProfile>(e =>
        {
            e.ToTable("StoreVendorProfiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.OwnerName).IsRequired().HasMaxLength(200);
            e.Property(x => x.OwnerCnic).HasMaxLength(20);
            e.Property(x => x.OwnerPhone).HasMaxLength(20);
            e.Property(x => x.OwnerEmail).HasMaxLength(200);
            e.Property(x => x.BusinessName).HasMaxLength(300);
            e.Property(x => x.BusinessRegistrationNumber).HasMaxLength(100);
            e.Property(x => x.FoodLicenseNumber).HasMaxLength(100);
            e.Property(x => x.BusinessDescription).HasMaxLength(2000);
            e.Property(x => x.BankName).HasMaxLength(150);
            e.Property(x => x.BankBranch).HasMaxLength(150);
            e.Property(x => x.AccountTitle).HasMaxLength(200);
            e.Property(x => x.AccountNumber).HasMaxLength(50);
            e.Property(x => x.IbanNumber).HasMaxLength(34);
            e.Property(x => x.WhatsappNumber).HasMaxLength(20);
            e.Property(x => x.ReviewNotes).HasMaxLength(1000);
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.HasOne(x => x.Store).WithOne(s => s.VendorProfile)
             .HasForeignKey<StoreVendorProfile>(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.StoreId).IsUnique().HasDatabaseName("IX_StoreVendorProfile_StoreId");
        });

        modelBuilder.Entity<StoreOffer>(e =>
        {
            e.ToTable("StoreOffers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Subtitle).HasMaxLength(300);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.BadgeText).HasMaxLength(30);
            e.Property(x => x.BadgeColor).HasMaxLength(10);
            e.Property(x => x.CallToAction).HasMaxLength(50);
            e.Property(x => x.DeepLinkUrl).HasMaxLength(500);
            e.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Promotion).WithMany().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.StoreId, x.IsActive, x.DisplayOrder })
             .HasDatabaseName("IX_StoreOffer_Store_Active_Order");
        });

        // Loyalty
        modelBuilder.Entity<LoyaltyProgram>(e =>
        {
            e.ToTable("LoyaltyPrograms");
            e.HasKey(x => x.Id);
            e.Property(x => x.PointsPerCurrencyUnit).HasPrecision(10, 4);
            e.Property(x => x.PointValueInCurrency).HasPrecision(10, 6);
        });

        modelBuilder.Entity<LoyaltyAccount>(e =>
        {
            e.ToTable("LoyaltyAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.PointsBalance).HasPrecision(18, 2);
            e.HasMany(x => x.Transactions).WithOne(t => t.LoyaltyAccount).HasForeignKey(t => t.LoyaltyAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        // Promotion
        modelBuilder.Entity<Promotion>(e =>
        {
            e.ToTable("Promotions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.PromotionCode).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.MinOrderAmount).HasPrecision(18, 4);
            e.Property(x => x.ScheduledDays).HasConversion<int>();
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.PromotionCode })
             .HasFilter("[PromotionCode] IS NOT NULL")
             .IsUnique().HasDatabaseName("IX_Promotion_Tenant_Code");
            e.HasMany(x => x.Items).WithOne(i => i.Promotion)
             .HasForeignKey(i => i.PromotionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromotionItem>(e =>
        {
            e.ToTable("PromotionItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.Property(x => x.ConditionQuantity).HasPrecision(18, 4);
            e.Property(x => x.ConditionAmount).HasPrecision(18, 4);
            e.Property(x => x.MaxDiscountedQuantity).HasPrecision(18, 4);
            e.Property(x => x.BuyQuantity).HasPrecision(18, 4);
            e.Property(x => x.GetQuantity).HasPrecision(18, 4);
        });

        // Coupon
        modelBuilder.Entity<Coupon>(e =>
        {
            e.ToTable("Coupons");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(100);
            e.Property(x => x.DiscountValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.Code })
             .IsUnique().HasDatabaseName("IX_Coupon_Tenant_Code");
            e.HasMany(x => x.Usages).WithOne(u => u.Coupon).HasForeignKey(u => u.CouponId).OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentSequence
        modelBuilder.Entity<DocumentSequence>(e =>
        {
            e.ToTable("DocumentSequences");
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentType).HasConversion<string>().IsRequired().HasMaxLength(50);
            e.Property(x => x.Prefix).IsRequired().HasMaxLength(20);
            e.Property(x => x.Suffix).HasMaxLength(20);
            e.Property(x => x.Separator).IsRequired().HasMaxLength(5);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.DocumentType })
             .IsUnique().HasDatabaseName("IX_DocumentSequence_Tenant_Type");
        });

        // SalesAgreement
        modelBuilder.Entity<SalesAgreement>(e =>
        {
            e.ToTable("SalesAgreements");
            e.HasKey(x => x.Id);
            e.Property(x => x.AgreementNumber).IsRequired().HasMaxLength(50);
            e.HasMany(x => x.Lines).WithOne(l => l.SalesAgreement).HasForeignKey(l => l.SalesAgreementId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    protected static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        var utc = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcN = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
        foreach (var e in modelBuilder.Model.GetEntityTypes())
            foreach (var p in e.GetProperties())
            {
                if (p.ClrType == typeof(DateTime))  p.SetValueConverter(utc);
                if (p.ClrType == typeof(DateTime?)) p.SetValueConverter(utcN);
            }
    }
}
