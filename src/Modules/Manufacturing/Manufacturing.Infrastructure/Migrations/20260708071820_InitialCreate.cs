using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manufacturing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "manufacturing");

            migrationBuilder.CreateTable(
                name: "BillsOfMaterial",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinishedProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillsOfMaterial", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Demands",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FulfilledQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialPlanningData",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SafetyStock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaximumStockLevel = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LotSize = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    PlanningHorizonDays = table.Column<int>(type: "int", nullable: false),
                    ScrapPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ProcurementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MRPType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialPlanningData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlannedOrders",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RequiredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Routings",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandardCosts",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MachineCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OverheadCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardCosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenters",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CapacityPerHour = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    HourlyMachineCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BOMByProducts",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillOfMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CostAllocationPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOMByProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BOMByProducts_BillsOfMaterial_BillOfMaterialId",
                        column: x => x.BillOfMaterialId,
                        principalSchema: "manufacturing",
                        principalTable: "BillsOfMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BOMItems",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillOfMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityRequired = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ScrapPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOMItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BOMItems_BillsOfMaterial_BillOfMaterialId",
                        column: x => x.BillOfMaterialId,
                        principalSchema: "manufacturing",
                        principalTable: "BillsOfMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillOfMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantityPlanned = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityProduced = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_BillsOfMaterial_BillOfMaterialId",
                        column: x => x.BillOfMaterialId,
                        principalSchema: "manufacturing",
                        principalTable: "BillsOfMaterial",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrders_PlannedOrders_PlannedOrderId",
                        column: x => x.PlannedOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "PlannedOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Routings_RoutingId",
                        column: x => x.RoutingId,
                        principalSchema: "manufacturing",
                        principalTable: "Routings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OverheadRules",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AppliesTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverheadRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverheadRules_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalSchema: "manufacturing",
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutingOperations",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    SetupHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    LaborHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    MachineHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutingOperations_Routings_RoutingId",
                        column: x => x.RoutingId,
                        principalSchema: "manufacturing",
                        principalTable: "Routings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutingOperations_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalSchema: "manufacturing",
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkCenterShifts",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    AvailableHours = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CapacityUtilizationPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    WorkingDays = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenterShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCenterShifts_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalSchema: "manufacturing",
                        principalTable: "WorkCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEntries",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MachineCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    OverheadCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ScrapCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEntries_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinishedGoodsReceipts",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedGoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedGoodsReceipts_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inspections",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectedQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PassedQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RejectedQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InspectedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspections_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MachineDowntimes",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RootCause = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReportedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolvedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineDowntimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineDowntimes_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MachineDowntimes_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalSchema: "manufacturing",
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialIssues",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityIssued = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReturned = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialIssues_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderComponents",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BOMItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IssuedQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReturnedQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScrapPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsSubstituted = table.Column<bool>(type: "bit", nullable: false),
                    OriginalMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsManuallyAdded = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderComponents_BOMItems_BOMItemId",
                        column: x => x.BOMItemId,
                        principalSchema: "manufacturing",
                        principalTable: "BOMItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderComponents_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkInProgress",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityInProgress = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityCompleted = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkInProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkInProgress_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderOperations",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutingOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PlannedSetupHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    PlannedLaborHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    PlannedMachineHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    ActualSetupHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    ActualLaborHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    ActualMachineHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    ConfirmedQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ScrapQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderOperations_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderOperations_RoutingOperations_RoutingOperationId",
                        column: x => x.RoutingOperationId,
                        principalSchema: "manufacturing",
                        principalTable: "RoutingOperations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionOrderOperations_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalSchema: "manufacturing",
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CapacityLoads",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiredHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    AvailableHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    LoadPercentage = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    ProductionOrderCount = table.Column<int>(type: "int", nullable: false),
                    IsOverloaded = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityLoads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapacityLoads_WorkCenterShifts_WorkCenterShiftId",
                        column: x => x.WorkCenterShiftId,
                        principalSchema: "manufacturing",
                        principalTable: "WorkCenterShifts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CapacityLoads_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalSchema: "manufacturing",
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionVariances",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardMaterialCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualMaterialCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StandardLaborCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualLaborCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StandardMachineCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualMachineCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StandardOverheadCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualOverheadCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VarianceCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsSettled = table.Column<bool>(type: "bit", nullable: false),
                    SettlementJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionVariances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionVariances_CostEntries_CostEntryId",
                        column: x => x.CostEntryId,
                        principalSchema: "manufacturing",
                        principalTable: "CostEntries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionVariances_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionCharacteristics",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacteristicName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InspectionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    UpperTolerance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    LowerTolerance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ActualValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    QualitativeResult = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    SampleSize = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionCharacteristics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionCharacteristics_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "manufacturing",
                        principalTable: "Inspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionBatches",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ManufacturingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReTestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CertificateOfAnalysis = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    VendorBatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QualityApproved = table.Column<bool>(type: "bit", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionBatches_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "manufacturing",
                        principalTable: "Inspections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionBatches_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReworkOrders",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReworkRoutingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityCompleted = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScheduledStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReworkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReworkOrders_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "manufacturing",
                        principalTable: "Inspections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReworkOrders_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReworkOrders_Routings_ReworkRoutingId",
                        column: x => x.ReworkRoutingId,
                        principalSchema: "manufacturing",
                        principalTable: "Routings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionSchedules",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CapacityRequiredHours = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HasCapacityConflict = table.Column<bool>(type: "bit", nullable: false),
                    ConflictDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionSchedules_ProductionOrderOperations_ProductionOrderOperationId",
                        column: x => x.ProductionOrderOperationId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrderOperations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionSchedules_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionSchedules_WorkCenters_WorkCenterId",
                        column: x => x.WorkCenterId,
                        principalSchema: "manufacturing",
                        principalTable: "WorkCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubContractOrders",
                schema: "manufacturing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductionOrderOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantitySent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubContractOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubContractOrders_ProductionOrderOperations_ProductionOrderOperationId",
                        column: x => x.ProductionOrderOperationId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrderOperations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubContractOrders_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalSchema: "manufacturing",
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BOM_Product_Version",
                schema: "manufacturing",
                table: "BillsOfMaterial",
                columns: new[] { "FinishedProductId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_BOMByProducts_BillOfMaterialId",
                schema: "manufacturing",
                table: "BOMByProducts",
                column: "BillOfMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_BOMItems_BillOfMaterialId",
                schema: "manufacturing",
                table: "BOMItems",
                column: "BillOfMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityLoad_WorkCenter_Date",
                schema: "manufacturing",
                table: "CapacityLoads",
                columns: new[] { "WorkCenterId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CapacityLoads_WorkCenterShiftId",
                schema: "manufacturing",
                table: "CapacityLoads",
                column: "WorkCenterShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEntries_ProductionOrderId",
                schema: "manufacturing",
                table: "CostEntries",
                column: "ProductionOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Demand_Product_Status_DueDate",
                schema: "manufacturing",
                table: "Demands",
                columns: new[] { "ProductId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinishedGoodsReceipts_ProductionOrderId",
                schema: "manufacturing",
                table: "FinishedGoodsReceipts",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionCharacteristics_InspectionId",
                schema: "manufacturing",
                table: "InspectionCharacteristics",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_ProductionOrderId",
                schema: "manufacturing",
                table: "Inspections",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_Product_Date",
                schema: "manufacturing",
                table: "InventoryTransactions",
                columns: new[] { "ProductId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineDowntime_WorkCenter_StartTime",
                schema: "manufacturing",
                table: "MachineDowntimes",
                columns: new[] { "WorkCenterId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineDowntimes_ProductionOrderId",
                schema: "manufacturing",
                table: "MachineDowntimes",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialIssues_ProductionOrderId",
                schema: "manufacturing",
                table: "MaterialIssues",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPlanningData_ProductId",
                schema: "manufacturing",
                table: "MaterialPlanningData",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OverheadRule_Tenant_Code",
                schema: "manufacturing",
                table: "OverheadRules",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OverheadRules_WorkCenterId",
                schema: "manufacturing",
                table: "OverheadRules",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedOrder_Product_Status",
                schema: "manufacturing",
                table: "PlannedOrders",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatch_Product_Status",
                schema: "manufacturing",
                table: "ProductionBatches",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatch_Tenant_BatchNumber",
                schema: "manufacturing",
                table: "ProductionBatches",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_InspectionId",
                schema: "manufacturing",
                table: "ProductionBatches",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_ProductionOrderId",
                schema: "manufacturing",
                table: "ProductionBatches",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderComponents_BOMItemId",
                schema: "manufacturing",
                table: "ProductionOrderComponents",
                column: "BOMItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderComponents_ProductionOrderId",
                schema: "manufacturing",
                table: "ProductionOrderComponents",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderOperations_ProductionOrderId",
                schema: "manufacturing",
                table: "ProductionOrderOperations",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderOperations_RoutingOperationId",
                schema: "manufacturing",
                table: "ProductionOrderOperations",
                column: "RoutingOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderOperations_WorkCenterId",
                schema: "manufacturing",
                table: "ProductionOrderOperations",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_Product_Status",
                schema: "manufacturing",
                table: "ProductionOrders",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_Tenant_OrderNumber",
                schema: "manufacturing",
                table: "ProductionOrders",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_BillOfMaterialId",
                schema: "manufacturing",
                table: "ProductionOrders",
                column: "BillOfMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_PlannedOrderId",
                schema: "manufacturing",
                table: "ProductionOrders",
                column: "PlannedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_RoutingId",
                schema: "manufacturing",
                table: "ProductionOrders",
                column: "RoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedule_WorkCenter_Dates",
                schema: "manufacturing",
                table: "ProductionSchedules",
                columns: new[] { "WorkCenterId", "ScheduledStartDate", "ScheduledEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedules_ProductionOrderId",
                schema: "manufacturing",
                table: "ProductionSchedules",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedules_ProductionOrderOperationId",
                schema: "manufacturing",
                table: "ProductionSchedules",
                column: "ProductionOrderOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionVariances_CostEntryId",
                schema: "manufacturing",
                table: "ProductionVariances",
                column: "CostEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionVariances_ProductionOrderId",
                schema: "manufacturing",
                table: "ProductionVariances",
                column: "ProductionOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReworkOrder_Order_Status",
                schema: "manufacturing",
                table: "ReworkOrders",
                columns: new[] { "ProductionOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReworkOrders_InspectionId",
                schema: "manufacturing",
                table: "ReworkOrders",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReworkOrders_ReworkRoutingId",
                schema: "manufacturing",
                table: "ReworkOrders",
                column: "ReworkRoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperations_RoutingId",
                schema: "manufacturing",
                table: "RoutingOperations",
                column: "RoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperations_WorkCenterId",
                schema: "manufacturing",
                table: "RoutingOperations",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Routing_Product_Version",
                schema: "manufacturing",
                table: "Routings",
                columns: new[] { "ProductId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_StandardCost_Product_Version",
                schema: "manufacturing",
                table: "StandardCosts",
                columns: new[] { "ProductId", "Version", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SubContractOrders_ProductionOrderId",
                schema: "manufacturing",
                table: "SubContractOrders",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SubContractOrders_ProductionOrderOperationId",
                schema: "manufacturing",
                table: "SubContractOrders",
                column: "ProductionOrderOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenter_Tenant_Code",
                schema: "manufacturing",
                table: "WorkCenters",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterShifts_WorkCenterId",
                schema: "manufacturing",
                table: "WorkCenterShifts",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkInProgress_ProductionOrderId",
                schema: "manufacturing",
                table: "WorkInProgress",
                column: "ProductionOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkInProgress_Tenant_ProductionOrder",
                schema: "manufacturing",
                table: "WorkInProgress",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "ProductionOrderId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BOMByProducts",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "CapacityLoads",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "Demands",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "FinishedGoodsReceipts",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "InspectionCharacteristics",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "InventoryTransactions",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "MachineDowntimes",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "MaterialIssues",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "MaterialPlanningData",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "OverheadRules",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "ProductionBatches",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "ProductionOrderComponents",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "ProductionSchedules",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "ProductionVariances",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "ReworkOrders",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "StandardCosts",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "SubContractOrders",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "WorkInProgress",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "WorkCenterShifts",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "BOMItems",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "CostEntries",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "Inspections",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "ProductionOrderOperations",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "ProductionOrders",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "RoutingOperations",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "BillsOfMaterial",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "PlannedOrders",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "Routings",
                schema: "manufacturing");

            migrationBuilder.DropTable(
                name: "WorkCenters",
                schema: "manufacturing");
        }
    }
}
