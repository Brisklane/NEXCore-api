using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Manufacturing.Domain.Entities;

namespace Manufacturing.Infrastructure.Persistence;

/// <summary>
/// Manufacturing module DbContext
/// Default Schema: mfg (Manufacturing)
///
/// Covers all manufacturing entities aligned with SAP PP / Oracle Manufacturing Cloud:
///
/// Master Data:
///   - WorkCenter            : Production stations / machines / labor units
///   - WorkCenterShift       : Shift-based capacity per work center
///   - BillOfMaterial        : Product recipe / structure
///   - BOMItem               : Individual BOM component lines
///   - BOMByProduct          : Co-products, by-products, scrap outputs
///   - Routing               : Sequence of manufacturing operations per product
///   - RoutingOperation      : Individual operation steps on a routing
///
/// Planning:
///   - PlannedOrder          : MRP-generated production demand
///   - ProductionSchedule    : Capacity scheduling of orders to work center time slots
///
/// Execution:
///   - ProductionOrder       : Main manufacturing transaction (work order)
///   - ProductionOrderOperation   : Order-specific copy of routing operations (actual vs planned)
///   - ProductionOrderComponent   : Order-specific copy of BOM components (actual vs planned)
///   - MaterialIssue         : Raw material consumption / goods issue
///   - WorkInProgress        : WIP quantity tracking
///   - SubContractOrder      : Outsourced operation tracking (outside processing)
///
/// Quality:
///   - Inspection            : Quality checkpoint records
///   - InspectionCharacteristic : Measured/attribute quality results with tolerances
///
/// Completion:
///   - FinishedGoodsReceipt  : Finished product receipt to inventory
///   - ProductionBatch       : Batch/lot master for traceability
///
/// Costing:
///   - CostEntry             : Production cost posting (material, labor, machine, overhead)
///   - ProductionVariance    : Standard vs actual cost variance analysis
///
/// Maintenance:
///   - MachineDowntime       : Work center stoppages and OEE tracking
/// </summary>
public class ManufacturingDbContext : DbContext
{
    private const string DefaultSchema = "manufacturing";

    public ManufacturingDbContext(DbContextOptions<ManufacturingDbContext> options) : base(options)
    {
    }

    // ===================== MASTER DATA =====================

    /// <summary>Work centers (machines, stations, labor units)</summary>
    public DbSet<WorkCenter> WorkCenters { get; set; } = null!;

    /// <summary>Shift-based availability per work center</summary>
    public DbSet<WorkCenterShift> WorkCenterShifts { get; set; } = null!;

    /// <summary>Bill of Materials headers</summary>
    public DbSet<BillOfMaterial> BillsOfMaterial { get; set; } = null!;

    /// <summary>BOM component lines</summary>
    public DbSet<BOMItem> BOMItems { get; set; } = null!;

    /// <summary>BOM co-products, by-products and scrap outputs</summary>
    public DbSet<BOMByProduct> BOMByProducts { get; set; } = null!;

    /// <summary>Routing headers (sequence of operations per product)</summary>
    public DbSet<Routing> Routings { get; set; } = null!;

    /// <summary>Individual routing operation steps</summary>
    public DbSet<RoutingOperation> RoutingOperations { get; set; } = null!;

    // ===================== PLANNING =====================

    /// <summary>MRP-generated planned production demands</summary>
    public DbSet<PlannedOrder> PlannedOrders { get; set; } = null!;

    /// <summary>Production schedule slots assigned to work centers</summary>
    public DbSet<ProductionSchedule> ProductionSchedules { get; set; } = null!;

    // ===================== EXECUTION =====================

    /// <summary>Production orders (main manufacturing transactions)</summary>
    public DbSet<ProductionOrder> ProductionOrders { get; set; } = null!;

    /// <summary>Order-specific operation copies (actual vs planned hours/yield)</summary>
    public DbSet<ProductionOrderOperation> ProductionOrderOperations { get; set; } = null!;

    /// <summary>Order-specific BOM component copies (actual vs planned quantities)</summary>
    public DbSet<ProductionOrderComponent> ProductionOrderComponents { get; set; } = null!;

    /// <summary>Material issues / goods issue against production orders</summary>
    public DbSet<MaterialIssue> MaterialIssues { get; set; } = null!;

    /// <summary>Work-in-progress quantity tracking</summary>
    public DbSet<WorkInProgress> WorkInProgress { get; set; } = null!;

    /// <summary>Subcontract / outside processing orders</summary>
    public DbSet<SubContractOrder> SubContractOrders { get; set; } = null!;

    // ===================== QUALITY =====================

    /// <summary>Quality inspection records</summary>
    public DbSet<Inspection> Inspections { get; set; } = null!;

    /// <summary>Individual quality characteristic measurements and results</summary>
    public DbSet<InspectionCharacteristic> InspectionCharacteristics { get; set; } = null!;

    // ===================== COMPLETION =====================

    /// <summary>Finished goods receipts (goods receipt from production)</summary>
    public DbSet<FinishedGoodsReceipt> FinishedGoodsReceipts { get; set; } = null!;

    /// <summary>Batch/lot master for manufactured goods traceability</summary>
    public DbSet<ProductionBatch> ProductionBatches { get; set; } = null!;

    // ===================== COSTING =====================

    /// <summary>Production cost postings per order</summary>
    public DbSet<CostEntry> CostEntries { get; set; } = null!;

    /// <summary>Standard vs actual cost variance per order</summary>
    public DbSet<ProductionVariance> ProductionVariances { get; set; } = null!;

    // ===================== MAINTENANCE =====================

    /// <summary>Machine/work center downtime records for OEE tracking</summary>
    public DbSet<MachineDowntime> MachineDowntimes { get; set; } = null!;

    // ===================== REWORK =====================
    /// <summary>Rework orders for reprocessing rejected items</summary>
    public DbSet<ReworkOrder> ReworkOrders { get; set; } = null!;

    // ===================== PLANNING - MRP =====================
    /// <summary>MRP planning parameters per product</summary>
    public DbSet<MaterialPlanningData> MaterialPlanningData { get; set; } = null!;

    /// <summary>Demand records from sales orders or forecasts used by MRP</summary>
    public DbSet<Demand> Demands { get; set; } = null!;

    // ===================== COSTING - STANDARDS =====================
    /// <summary>Standard cost estimates per product</summary>
    public DbSet<StandardCost> StandardCosts { get; set; } = null!;

    /// <summary>Overhead allocation rules per work center or globally</summary>
    public DbSet<OverheadRule> OverheadRules { get; set; } = null!;

    // ===================== CAPACITY =====================
    /// <summary>Daily capacity load records per work center</summary>
    public DbSet<CapacityLoad> CapacityLoads { get; set; } = null!;

    // ===================== INVENTORY (manufacturing movements) =====================
    /// <summary>Unified ledger of manufacturing stock movements</summary>
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;

    // ============================================================

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        // ===================== WORK CENTER =====================
        modelBuilder.Entity<WorkCenter>(entity =>
        {
            entity.ToTable("WorkCenters", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CapacityPerHour).HasPrecision(18, 4);
            entity.Property(e => e.HourlyMachineCost).HasPrecision(18, 4);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Code })
                .IsUnique().HasDatabaseName("IX_WorkCenter_Tenant_Code");

            entity.HasMany(e => e.RoutingOperations)
                .WithOne(o => o.WorkCenter)
                .HasForeignKey(o => o.WorkCenterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(e => e.Shifts)
                .WithOne(s => s.WorkCenter)
                .HasForeignKey(s => s.WorkCenterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Downtimes)
                .WithOne(d => d.WorkCenter)
                .HasForeignKey(d => d.WorkCenterId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== WORK CENTER SHIFT =====================
        modelBuilder.Entity<WorkCenterShift>(entity =>
        {
            entity.ToTable("WorkCenterShifts", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShiftName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WorkingDays).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AvailableHours).HasPrecision(8, 2);
            entity.Property(e => e.CapacityUtilizationPercent).HasPrecision(5, 2);

            entity.HasOne(e => e.WorkCenter)
                .WithMany(w => w.Shifts)
                .HasForeignKey(e => e.WorkCenterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== BILL OF MATERIAL =====================
        modelBuilder.Entity<BillOfMaterial>(entity =>
        {
            entity.ToTable("BillsOfMaterial", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            entity.HasIndex(e => new { e.FinishedProductId, e.Version })
                .HasDatabaseName("IX_BOM_Product_Version");

            entity.HasMany(e => e.Items)
                .WithOne(i => i.BillOfMaterial)
                .HasForeignKey(i => i.BillOfMaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ByProducts)
                .WithOne(b => b.BillOfMaterial)
                .HasForeignKey(b => b.BillOfMaterialId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== BOM ITEM =====================
        modelBuilder.Entity<BOMItem>(entity =>
        {
            entity.ToTable("BOMItems", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.QuantityRequired).HasPrecision(18, 6);
            entity.Property(e => e.ScrapPercentage).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.BillOfMaterial)
                .WithMany(b => b.Items)
                .HasForeignKey(e => e.BillOfMaterialId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== BOM BY-PRODUCT =====================
        modelBuilder.Entity<BOMByProduct>(entity =>
        {
            entity.ToTable("BOMByProducts", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Quantity).HasPrecision(18, 6);
            entity.Property(e => e.CostAllocationPercent).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.BillOfMaterial)
                .WithMany(b => b.ByProducts)
                .HasForeignKey(e => e.BillOfMaterialId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== ROUTING =====================
        modelBuilder.Entity<Routing>(entity =>
        {
            entity.ToTable("Routings", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ProductId, e.Version })
                .HasDatabaseName("IX_Routing_Product_Version");

            entity.HasMany(e => e.Operations)
                .WithOne(o => o.Routing)
                .HasForeignKey(o => o.RoutingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== ROUTING OPERATION =====================
        modelBuilder.Entity<RoutingOperation>(entity =>
        {
            entity.ToTable("RoutingOperations", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StandardHours).HasPrecision(10, 4);
            entity.Property(e => e.SetupHours).HasPrecision(10, 4);
            entity.Property(e => e.LaborHours).HasPrecision(10, 4);
            entity.Property(e => e.MachineHours).HasPrecision(10, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Routing)
                .WithMany(r => r.Operations)
                .HasForeignKey(e => e.RoutingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.WorkCenter)
                .WithMany(w => w.RoutingOperations)
                .HasForeignKey(e => e.WorkCenterId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== PLANNED ORDER =====================
        modelBuilder.Entity<PlannedOrder>(entity =>
        {
            entity.ToTable("PlannedOrders", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PlannedQty).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ProductId, e.Status })
                .HasDatabaseName("IX_PlannedOrder_Product_Status");

            entity.HasMany(e => e.ProductionOrders)
                .WithOne(po => po.PlannedOrder)
                .HasForeignKey(po => po.PlannedOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== PRODUCTION ORDER =====================
        modelBuilder.Entity<ProductionOrder>(entity =>
        {
            entity.ToTable("ProductionOrders", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QuantityPlanned).HasPrecision(18, 4);
            entity.Property(e => e.QuantityProduced).HasPrecision(18, 4);
            entity.Property(e => e.QuantityRejected).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            // Order number is unique per tenant (not globally - two companies may reuse the same number).
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.OrderNumber })
                .IsUnique().HasDatabaseName("IX_ProductionOrder_Tenant_OrderNumber");
            entity.HasIndex(e => new { e.ProductId, e.Status })
                .HasDatabaseName("IX_ProductionOrder_Product_Status");

            entity.HasOne(e => e.BillOfMaterial)
                .WithMany()
                .HasForeignKey(e => e.BillOfMaterialId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Routing)
                .WithMany()
                .HasForeignKey(e => e.RoutingId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.PlannedOrder)
                .WithMany(p => p.ProductionOrders)
                .HasForeignKey(e => e.PlannedOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(e => e.MaterialIssues)
                .WithOne(m => m.ProductionOrder)
                .HasForeignKey(m => m.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.WorkInProgress)
                .WithOne(w => w.ProductionOrder)
                .HasForeignKey<WorkInProgress>(w => w.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Inspections)
                .WithOne(i => i.ProductionOrder)
                .HasForeignKey(i => i.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.FinishedGoodsReceipts)
                .WithOne(f => f.ProductionOrder)
                .HasForeignKey(f => f.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CostEntry)
                .WithOne(c => c.ProductionOrder)
                .HasForeignKey<CostEntry>(c => c.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Operations)
                .WithOne(o => o.ProductionOrder)
                .HasForeignKey(o => o.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Components)
                .WithOne(c => c.ProductionOrder)
                .HasForeignKey(c => c.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Schedules)
                .WithOne(s => s.ProductionOrder)
                .HasForeignKey(s => s.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.SubContractOrders)
                .WithOne(s => s.ProductionOrder)
                .HasForeignKey(s => s.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Variance)
                .WithOne(v => v.ProductionOrder)
                .HasForeignKey<ProductionVariance>(v => v.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Batches)
                .WithOne(b => b.ProductionOrder)
                .HasForeignKey(b => b.ProductionOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(e => e.MachineDowntimes)
                .WithOne(d => d.ProductionOrder)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== PRODUCTION ORDER OPERATION =====================
        modelBuilder.Entity<ProductionOrderOperation>(entity =>
        {
            entity.ToTable("ProductionOrderOperations", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PlannedSetupHours).HasPrecision(10, 4);
            entity.Property(e => e.PlannedLaborHours).HasPrecision(10, 4);
            entity.Property(e => e.PlannedMachineHours).HasPrecision(10, 4);
            entity.Property(e => e.ActualSetupHours).HasPrecision(10, 4);
            entity.Property(e => e.ActualLaborHours).HasPrecision(10, 4);
            entity.Property(e => e.ActualMachineHours).HasPrecision(10, 4);
            entity.Property(e => e.ConfirmedQty).HasPrecision(18, 4);
            entity.Property(e => e.ScrapQty).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.Operations)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RoutingOperation)
                .WithMany()
                .HasForeignKey(e => e.RoutingOperationId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.WorkCenter)
                .WithMany()
                .HasForeignKey(e => e.WorkCenterId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== PRODUCTION ORDER COMPONENT =====================
        modelBuilder.Entity<ProductionOrderComponent>(entity =>
        {
            entity.ToTable("ProductionOrderComponents", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PlannedQty).HasPrecision(18, 6);
            entity.Property(e => e.IssuedQty).HasPrecision(18, 6);
            entity.Property(e => e.ReturnedQty).HasPrecision(18, 6);
            entity.Property(e => e.ScrapPercentage).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.Components)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.BOMItem)
                .WithMany()
                .HasForeignKey(e => e.BOMItemId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== MATERIAL ISSUE =====================
        modelBuilder.Entity<MaterialIssue>(entity =>
        {
            entity.ToTable("MaterialIssues", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityIssued).HasPrecision(18, 4);
            entity.Property(e => e.QuantityReturned).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.MaterialIssues)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== WORK IN PROGRESS =====================
        modelBuilder.Entity<WorkInProgress>(entity =>
        {
            entity.ToTable("WorkInProgress", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityInProgress).HasPrecision(18, 4);
            entity.Property(e => e.QuantityCompleted).HasPrecision(18, 4);
            entity.Property(e => e.QuantityRejected).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Tenant-scoped unique index for active records only
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.ProductionOrderId })
                .HasDatabaseName("IX_WorkInProgress_Tenant_ProductionOrder")
                .IsUnique()
                .HasFilter("is_deleted = false");

            entity.HasOne(e => e.ProductionOrder)
                .WithOne(p => p.WorkInProgress)
                .HasForeignKey<WorkInProgress>(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== SUBCONTRACT ORDER =====================
        modelBuilder.Entity<SubContractOrder>(entity =>
        {
            entity.ToTable("SubContractOrders", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QuantitySent).HasPrecision(18, 4);
            entity.Property(e => e.QuantityReceived).HasPrecision(18, 4);
            entity.Property(e => e.QuantityRejected).HasPrecision(18, 4);
            entity.Property(e => e.UnitCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalCost).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.SubContractOrders)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProductionOrderOperation)
                .WithMany()
                .HasForeignKey(e => e.ProductionOrderOperationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== PRODUCTION SCHEDULE =====================
        modelBuilder.Entity<ProductionSchedule>(entity =>
        {
            entity.ToTable("ProductionSchedules", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ScheduleType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CapacityRequiredHours).HasPrecision(10, 4);
            entity.Property(e => e.ConflictDescription).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.WorkCenterId, e.ScheduledStartDate, e.ScheduledEndDate })
                .HasDatabaseName("IX_ProductionSchedule_WorkCenter_Dates");

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.Schedules)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.WorkCenter)
                .WithMany()
                .HasForeignKey(e => e.WorkCenterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ProductionOrderOperation)
                .WithMany()
                .HasForeignKey(e => e.ProductionOrderOperationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== INSPECTION =====================
        modelBuilder.Entity<Inspection>(entity =>
        {
            entity.ToTable("Inspections", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InspectedQty).HasPrecision(18, 4);
            entity.Property(e => e.PassedQty).HasPrecision(18, 4);
            entity.Property(e => e.RejectedQty).HasPrecision(18, 4);
            entity.Property(e => e.Remarks).HasMaxLength(2000);

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.Inspections)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Characteristics)
                .WithOne(c => c.Inspection)
                .HasForeignKey(c => c.InspectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Batches)
                .WithOne(b => b.Inspection)
                .HasForeignKey(b => b.InspectionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== INSPECTION CHARACTERISTIC =====================
        modelBuilder.Entity<InspectionCharacteristic>(entity =>
        {
            entity.ToTable("InspectionCharacteristics", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CharacteristicName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.InspectionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(20);
            entity.Property(e => e.Result).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QualitativeResult).HasMaxLength(100);
            entity.Property(e => e.TargetValue).HasPrecision(18, 6);
            entity.Property(e => e.UpperTolerance).HasPrecision(18, 6);
            entity.Property(e => e.LowerTolerance).HasPrecision(18, 6);
            entity.Property(e => e.ActualValue).HasPrecision(18, 6);
            entity.Property(e => e.Remarks).HasMaxLength(2000);

            entity.HasOne(e => e.Inspection)
                .WithMany(i => i.Characteristics)
                .HasForeignKey(e => e.InspectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== FINISHED GOODS RECEIPT =====================
        modelBuilder.Entity<FinishedGoodsReceipt>(entity =>
        {
            entity.ToTable("FinishedGoodsReceipts", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityReceived).HasPrecision(18, 4);
            entity.Property(e => e.BatchNo).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.FinishedGoodsReceipts)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== PRODUCTION BATCH =====================
        modelBuilder.Entity<ProductionBatch>(entity =>
        {
            entity.ToTable("ProductionBatches", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.CertificateOfAnalysis).HasMaxLength(255);
            entity.Property(e => e.VendorBatchNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.BatchNumber })
                .IsUnique().HasDatabaseName("IX_ProductionBatch_Tenant_BatchNumber");
            entity.HasIndex(e => new { e.ProductId, e.Status }).HasDatabaseName("IX_ProductionBatch_Product_Status");

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.Batches)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Inspection)
                .WithMany(i => i.Batches)
                .HasForeignKey(e => e.InspectionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== COST ENTRY =====================
        modelBuilder.Entity<CostEntry>(entity =>
        {
            entity.ToTable("CostEntries", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaterialCost).HasPrecision(18, 4);
            entity.Property(e => e.LaborCost).HasPrecision(18, 4);
            entity.Property(e => e.MachineCost).HasPrecision(18, 4);
            entity.Property(e => e.OverheadCost).HasPrecision(18, 4);
            entity.Property(e => e.ScrapCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalCost).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.ProductionOrder)
                .WithOne(p => p.CostEntry)
                .HasForeignKey<CostEntry>(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== PRODUCTION VARIANCE =====================
        modelBuilder.Entity<ProductionVariance>(entity =>
        {
            entity.ToTable("ProductionVariances", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VarianceCategory).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StandardMaterialCost).HasPrecision(18, 4);
            entity.Property(e => e.ActualMaterialCost).HasPrecision(18, 4);
            entity.Property(e => e.StandardLaborCost).HasPrecision(18, 4);
            entity.Property(e => e.ActualLaborCost).HasPrecision(18, 4);
            entity.Property(e => e.StandardMachineCost).HasPrecision(18, 4);
            entity.Property(e => e.ActualMachineCost).HasPrecision(18, 4);
            entity.Property(e => e.StandardOverheadCost).HasPrecision(18, 4);
            entity.Property(e => e.ActualOverheadCost).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Computed properties - not mapped to DB columns
            entity.Ignore(e => e.MaterialVariance);
            entity.Ignore(e => e.LaborVariance);
            entity.Ignore(e => e.MachineVariance);
            entity.Ignore(e => e.OverheadVariance);
            entity.Ignore(e => e.TotalVariance);

            entity.HasOne(e => e.ProductionOrder)
                .WithOne(p => p.Variance)
                .HasForeignKey<ProductionVariance>(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CostEntry)
                .WithMany()
                .HasForeignKey(e => e.CostEntryId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== MACHINE DOWNTIME =====================
        modelBuilder.Entity<MachineDowntime>(entity =>
        {
            entity.ToTable("MachineDowntimes", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RootCause).HasMaxLength(1000);
            entity.Property(e => e.Resolution).HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DurationHours).HasPrecision(10, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.WorkCenterId, e.StartTime })
                .HasDatabaseName("IX_MachineDowntime_WorkCenter_StartTime");

            entity.HasOne(e => e.WorkCenter)
                .WithMany(w => w.Downtimes)
                .HasForeignKey(e => e.WorkCenterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.MachineDowntimes)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== REWORK ORDER =====================
        modelBuilder.Entity<ReworkOrder>(entity =>
        {
            entity.ToTable("ReworkOrders", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UnitOfMeasure).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.QuantityCompleted).HasPrecision(18, 4);
            entity.Property(e => e.QuantityRejected).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            entity.HasIndex(e => new { e.ProductionOrderId, e.Status })
                .HasDatabaseName("IX_ReworkOrder_Order_Status");

            entity.HasOne(e => e.ProductionOrder)
                .WithMany(p => p.ReworkOrders)
                .HasForeignKey(e => e.ProductionOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Inspection)
                .WithMany()
                .HasForeignKey(e => e.InspectionId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ReworkRouting)
                .WithMany()
                .HasForeignKey(e => e.ReworkRoutingId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== MATERIAL PLANNING DATA =====================
        modelBuilder.Entity<MaterialPlanningData>(entity =>
        {
            entity.ToTable("MaterialPlanningData", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcurementType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MRPType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SafetyStock).HasPrecision(18, 4);
            entity.Property(e => e.ReorderPoint).HasPrecision(18, 4);
            entity.Property(e => e.MaximumStockLevel).HasPrecision(18, 4);
            entity.Property(e => e.LotSize).HasPrecision(18, 4);
            entity.Property(e => e.ScrapPercentage).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("IX_MaterialPlanningData_ProductId");
        });

        // ===================== DEMAND =====================
        modelBuilder.Entity<Demand>(entity =>
        {
            entity.ToTable("Demands", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.FulfilledQty).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ProductId, e.Status, e.DueDate })
                .HasDatabaseName("IX_Demand_Product_Status_DueDate");
        });

        // ===================== STANDARD COST =====================
        modelBuilder.Entity<StandardCost>(entity =>
        {
            entity.ToTable("StandardCosts", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
            entity.Property(e => e.MaterialCost).HasPrecision(18, 4);
            entity.Property(e => e.LaborCost).HasPrecision(18, 4);
            entity.Property(e => e.MachineCost).HasPrecision(18, 4);
            entity.Property(e => e.OverheadCost).HasPrecision(18, 4);
            entity.Property(e => e.TotalCost).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ProductId, e.Version, e.IsActive })
                .HasDatabaseName("IX_StandardCost_Product_Version");
        });

        // ===================== OVERHEAD RULE =====================
        modelBuilder.Entity<OverheadRule>(entity =>
        {
            entity.ToTable("OverheadRules", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RateType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AppliesTo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Value).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.Code })
                .IsUnique().HasDatabaseName("IX_OverheadRule_Tenant_Code");

            entity.HasOne(e => e.WorkCenter)
                .WithMany(w => w.OverheadRules)
                .HasForeignKey(e => e.WorkCenterId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== CAPACITY LOAD =====================
        modelBuilder.Entity<CapacityLoad>(entity =>
        {
            entity.ToTable("CapacityLoads", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequiredHours).HasPrecision(10, 4);
            entity.Property(e => e.AvailableHours).HasPrecision(10, 4);
            entity.Property(e => e.LoadPercentage).HasPrecision(7, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.WorkCenterId, e.Date })
                .HasDatabaseName("IX_CapacityLoad_WorkCenter_Date");

            entity.HasOne(e => e.WorkCenter)
                .WithMany(w => w.CapacityLoads)
                .HasForeignKey(e => e.WorkCenterId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.WorkCenterShift)
                .WithMany()
                .HasForeignKey(e => e.WorkCenterShiftId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ===================== INVENTORY TRANSACTION =====================
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ReferenceType).HasMaxLength(100);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasIndex(e => new { e.ProductId, e.TransactionDate })
                .HasDatabaseName("IX_InventoryTransaction_Product_Date");
        });

        // Cross-cutting rules shared by every module: UTC normalisation for all
        // DateTime properties and the xmin optimistic-concurrency token. Must stay
        // last so it sees owned-type and DbSet-less properties configured above.
        modelBuilder.ApplyNexcoreConventions();
    }
}
