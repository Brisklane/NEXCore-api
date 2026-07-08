using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "AttributeDefinitions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Text"),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AllowedValues = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsVariant = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AttributeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Colors",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HexCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    R = table.Column<byte>(type: "tinyint", nullable: true),
                    G = table.Column<byte>(type: "tinyint", nullable: true),
                    B = table.Column<byte>(type: "tinyint", nullable: true),
                    ColorFamily = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SwatchImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Colors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemCategories",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InventoryAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CogsAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OnlineImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnlineDisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCategories_ItemCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "inventory",
                        principalTable: "ItemCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sizes",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeChart = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Apparel"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Sizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxDefinitions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsPercentage = table.Column<bool>(type: "bit", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InclusionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxPayableAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxRecoverableAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplyOnSales = table.Column<bool>(type: "bit", nullable: false),
                    ApplyOnPurchases = table.Column<bool>(type: "bit", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_TaxDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WarehouseType = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Inventory"),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayColorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaseUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "New"),
                    AgeRestriction = table.Column<int>(type: "int", nullable: false),
                    InventoryAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CogsAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostingMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "MovingAverage"),
                    TrackingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "None"),
                    IsBatchTracked = table.Column<bool>(type: "bit", nullable: false),
                    IsSerialTracked = table.Column<bool>(type: "bit", nullable: false),
                    HasVariants = table.Column<bool>(type: "bit", nullable: false),
                    IsComponent = table.Column<bool>(type: "bit", nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxStockLevel = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EconomicOrderQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AlertOnLowStock = table.Column<bool>(type: "bit", nullable: false),
                    AlertOnExcessStock = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "inventory",
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Items_Colors_DisplayColorId",
                        column: x => x.DisplayColorId,
                        principalSchema: "inventory",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Items_ItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "inventory",
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Items_Units_BaseUnitId",
                        column: x => x.BaseUnitId,
                        principalSchema: "inventory",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bins",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aisle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rack = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Capacity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    table.PrimaryKey("PK_Bins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bins_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryDocuments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_InventoryDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryDocuments_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryDocuments_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryValuations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValuationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValuationMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "MovingAverage"),
                    PeriodReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_InventoryValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryValuations_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryValuations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemAttributes",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_ItemAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemAttributes_AttributeDefinitions_AttributeDefinitionId",
                        column: x => x.AttributeDefinitionId,
                        principalSchema: "inventory",
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemAttributes_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemBarcodes",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BarcodeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "EAN13"),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ItemBarcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemBarcodes_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemBarcodes_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inventory",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemBundles",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsIncludedInCost = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ItemBundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemBundles_Items_BundleItemId",
                        column: x => x.BundleItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemBundles_Items_ComponentItemId",
                        column: x => x.ComponentItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemBundles_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inventory",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemChannelListings",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ListingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    ChannelTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChannelDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ChannelPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ChannelDiscount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DiscountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExternalProductId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ListingUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ListedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutoSyncStock = table.Column<bool>(type: "bit", nullable: false),
                    AutoSyncPrice = table.Column<bool>(type: "bit", nullable: false),
                    IsSoldOut = table.Column<bool>(type: "bit", nullable: false),
                    MaxQuantityPerOrder = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ItemChannelListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemChannelListings_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemComments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Internal"),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ItemComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemComments_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemDiscounts",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DiscountType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Percentage"),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BuyQuantity = table.Column<int>(type: "int", nullable: true),
                    GetQuantity = table.Column<int>(type: "int", nullable: true),
                    MinQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ApplicableChannels = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ItemDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemDiscounts_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemImages",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Original"),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemImages_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemPrices",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceList = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Default"),
                    SalePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MinSalePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SalePriceExcludingTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SaleTaxAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SalePriceIncludingTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PurchasePriceExcludingTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PurchaseTaxAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PurchasePriceIncludingTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsTaxInclusive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ItemPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemPrices_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemPrices_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inventory",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemSeos",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaKeywords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_ItemSeos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSeos_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemShippings",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LengthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WidthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VolumetricWeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CountryOfOrigin = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    HsCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UnitsPerCarton = table.Column<int>(type: "int", nullable: true),
                    CartonsPerPallet = table.Column<int>(type: "int", nullable: true),
                    CartonWeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CartonLengthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CartonWidthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CartonHeightCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RequiresSpecialHandling = table.Column<bool>(type: "bit", nullable: false),
                    HandlingNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsHazmat = table.Column<bool>(type: "bit", nullable: false),
                    IsShippableInternational = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ItemShippings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemShippings_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemSubstitutions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubstituteItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsBidirectional = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ItemSubstitutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSubstitutions_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemSubstitutions_Items_SubstituteItemId",
                        column: x => x.SubstituteItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemSuppliers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupplierItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastPurchasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    MinOrderQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    LastPurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ItemSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSuppliers_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemTaxes",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverrideRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EffectiveRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_ItemTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemTaxes_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemTaxes_TaxDefinitions_TaxDefinitionId",
                        column: x => x.TaxDefinitionId,
                        principalSchema: "inventory",
                        principalTable: "TaxDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemUomConversions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_ItemUomConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemUomConversions_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemUomConversions_Units_FromUnitId",
                        column: x => x.FromUnitId,
                        principalSchema: "inventory",
                        principalTable: "Units",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemUomConversions_Units_ToUnitId",
                        column: x => x.ToUnitId,
                        principalSchema: "inventory",
                        principalTable: "Units",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItemVariants",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ColorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtraDimension = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SalePriceOverride = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PurchasePriceOverride = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ItemVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemVariants_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "inventory",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ItemVariants_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemVariants_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "inventory",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItemWarranties",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarrantyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Seller"),
                    DurationMonths = table.Column<int>(type: "int", nullable: false),
                    PolicyDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProviderName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProviderContact = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_ItemWarranties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemWarranties_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryDocumentLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ReferenceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_InventoryDocumentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryDocumentLines_Bins_BinId",
                        column: x => x.BinId,
                        principalSchema: "inventory",
                        principalTable: "Bins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryDocumentLines_InventoryDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "inventory",
                        principalTable: "InventoryDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryDocumentLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryDocumentLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inventory",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryDocumentLines_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemColors",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemBarcodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ItemColors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemColors_Colors_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "inventory",
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemColors_ItemBarcodes_ItemBarcodeId",
                        column: x => x.ItemBarcodeId,
                        principalSchema: "inventory",
                        principalTable: "ItemBarcodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemColors_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemSizes",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemBarcodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ItemSizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSizes_ItemBarcodes_ItemBarcodeId",
                        column: x => x.ItemBarcodeId,
                        principalSchema: "inventory",
                        principalTable: "ItemBarcodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemSizes_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemSizes_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalSchema: "inventory",
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBalances",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityAvailable = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostingMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "MovingAverage"),
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
                    table.PrimaryKey("PK_InventoryBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Bins_BinId",
                        column: x => x.BinId,
                        principalSchema: "inventory",
                        principalTable: "Bins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryBalances_ItemVariants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "inventory",
                        principalTable: "ItemVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemBatches",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
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
                    table.PrimaryKey("PK_ItemBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemBatches_ItemVariants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "inventory",
                        principalTable: "ItemVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemBatches_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemSerials",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Imei = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Imei2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MacAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "InStock"),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceiptDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarrantyStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarrantyEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoldDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SoldDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SalesReference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
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
                    table.PrimaryKey("PK_ItemSerials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSerials_Bins_BinId",
                        column: x => x.BinId,
                        principalSchema: "inventory",
                        principalTable: "Bins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemSerials_ItemVariants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "inventory",
                        principalTable: "ItemVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemSerials_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemSerials_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryDocumentLineSerials",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Imei = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Imei2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MacAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_InventoryDocumentLineSerials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryDocumentLineSerials_InventoryDocumentLines_DocumentLineId",
                        column: x => x.DocumentLineId,
                        principalSchema: "inventory",
                        principalTable: "InventoryDocumentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemLotStocks",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_ItemLotStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemLotStocks_Bins_BinId",
                        column: x => x.BinId,
                        principalSchema: "inventory",
                        principalTable: "Bins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemLotStocks_ItemBatches_ItemBatchId",
                        column: x => x.ItemBatchId,
                        principalSchema: "inventory",
                        principalTable: "ItemBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemLotStocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemSerialHistories",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemSerialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_ItemSerialHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSerialHistories_ItemSerials_ItemSerialId",
                        column: x => x.ItemSerialId,
                        principalSchema: "inventory",
                        principalTable: "ItemSerials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostLayers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsExhausted = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_InventoryCostLayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostLayers_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCostLayers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemSerialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Bins_BinId",
                        column: x => x.BinId,
                        principalSchema: "inventory",
                        principalTable: "Bins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InventoryCostLayers_CostLayerId",
                        column: x => x.CostLayerId,
                        principalSchema: "inventory",
                        principalTable: "InventoryCostLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InventoryDocumentLines_DocumentLineId",
                        column: x => x.DocumentLineId,
                        principalSchema: "inventory",
                        principalTable: "InventoryDocumentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InventoryDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "inventory",
                        principalTable: "InventoryDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_ItemBatches_ItemBatchId",
                        column: x => x.ItemBatchId,
                        principalSchema: "inventory",
                        principalTable: "ItemBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_ItemSerials_ItemSerialId",
                        column: x => x.ItemSerialId,
                        principalSchema: "inventory",
                        principalTable: "ItemSerials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "inventory",
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inventory",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinition_Tenant_Code",
                schema: "inventory",
                table: "AttributeDefinitions",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bins_WarehouseId",
                schema: "inventory",
                table: "Bins",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Brand_Tenant_Code",
                schema: "inventory",
                table: "Brands",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Color_Family",
                schema: "inventory",
                table: "Colors",
                column: "ColorFamily");

            migrationBuilder.CreateIndex(
                name: "IX_Color_Tenant_Code",
                schema: "inventory",
                table: "Colors",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalance_Item_Warehouse_Bin_Variant",
                schema: "inventory",
                table: "InventoryBalances",
                columns: new[] { "ItemId", "WarehouseId", "BinId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalance_QuantityOnHand",
                schema: "inventory",
                table: "InventoryBalances",
                column: "QuantityOnHand");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_BinId",
                schema: "inventory",
                table: "InventoryBalances",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_VariantId",
                schema: "inventory",
                table: "InventoryBalances",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_WarehouseId",
                schema: "inventory",
                table: "InventoryBalances",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayer_Item_Warehouse_Status",
                schema: "inventory",
                table: "InventoryCostLayers",
                columns: new[] { "ItemId", "WarehouseId", "IsExhausted", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_ReceiptTransactionId",
                schema: "inventory",
                table: "InventoryCostLayers",
                column: "ReceiptTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_WarehouseId",
                schema: "inventory",
                table: "InventoryCostLayers",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocumentLine_Document_LineNumber",
                schema: "inventory",
                table: "InventoryDocumentLines",
                columns: new[] { "DocumentId", "LineNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocumentLines_BinId",
                schema: "inventory",
                table: "InventoryDocumentLines",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocumentLines_ItemId",
                schema: "inventory",
                table: "InventoryDocumentLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocumentLines_UnitId",
                schema: "inventory",
                table: "InventoryDocumentLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocumentLines_WarehouseId",
                schema: "inventory",
                table: "InventoryDocumentLines",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocumentLineSerial_Line",
                schema: "inventory",
                table: "InventoryDocumentLineSerials",
                column: "DocumentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocument_Date",
                schema: "inventory",
                table: "InventoryDocuments",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocument_Status",
                schema: "inventory",
                table: "InventoryDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocument_Tenant_Number_Type",
                schema: "inventory",
                table: "InventoryDocuments",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "DocumentNumber", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocuments_FromWarehouseId",
                schema: "inventory",
                table: "InventoryDocuments",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocuments_ToWarehouseId",
                schema: "inventory",
                table: "InventoryDocuments",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_Date",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_Document",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_Item_Warehouse",
                schema: "inventory",
                table: "InventoryTransactions",
                columns: new[] { "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_BinId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CostLayerId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "CostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_DocumentLineId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "DocumentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ItemBatchId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "ItemBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ItemSerialId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "ItemSerialId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_UnitId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_WarehouseId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuation_Date_Period",
                schema: "inventory",
                table: "InventoryValuations",
                columns: new[] { "ValuationDate", "PeriodReference" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuations_ItemId",
                schema: "inventory",
                table: "InventoryValuations",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuations_WarehouseId",
                schema: "inventory",
                table: "InventoryValuations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemAttribute_Item_Attribute",
                schema: "inventory",
                table: "ItemAttributes",
                columns: new[] { "ItemId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemAttributes_AttributeDefinitionId",
                schema: "inventory",
                table: "ItemAttributes",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBarcode_Item_Primary",
                schema: "inventory",
                table: "ItemBarcodes",
                columns: new[] { "ItemId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBarcode_TenantBarcode",
                schema: "inventory",
                table: "ItemBarcodes",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemBarcodes_UnitId",
                schema: "inventory",
                table: "ItemBarcodes",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBatch_Expiry",
                schema: "inventory",
                table: "ItemBatches",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBatch_Item_Status",
                schema: "inventory",
                table: "ItemBatches",
                columns: new[] { "ItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemBatch_Tenant_Item_Batch",
                schema: "inventory",
                table: "ItemBatches",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "ItemId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemBatches_VariantId",
                schema: "inventory",
                table: "ItemBatches",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBundle_Bundle_Component",
                schema: "inventory",
                table: "ItemBundles",
                columns: new[] { "BundleItemId", "ComponentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemBundles_ComponentItemId",
                schema: "inventory",
                table: "ItemBundles",
                column: "ComponentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBundles_UnitId",
                schema: "inventory",
                table: "ItemBundles",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_ParentCategoryId",
                schema: "inventory",
                table: "ItemCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemChannelListing_Channel_Status",
                schema: "inventory",
                table: "ItemChannelListings",
                columns: new[] { "Channel", "ListingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemChannelListing_Item_Channel",
                schema: "inventory",
                table: "ItemChannelListings",
                columns: new[] { "ItemId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemColor_Item_Color",
                schema: "inventory",
                table: "ItemColors",
                columns: new[] { "ItemId", "ColorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemColor_Item_Default",
                schema: "inventory",
                table: "ItemColors",
                columns: new[] { "ItemId", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ItemColors_ColorId",
                schema: "inventory",
                table: "ItemColors",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemColors_ItemBarcodeId",
                schema: "inventory",
                table: "ItemColors",
                column: "ItemBarcodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemComment_Item_Pinned",
                schema: "inventory",
                table: "ItemComments",
                columns: new[] { "ItemId", "IsPinned" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemDiscount_Item_Active_Period",
                schema: "inventory",
                table: "ItemDiscounts",
                columns: new[] { "ItemId", "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemImage_Item_Primary",
                schema: "inventory",
                table: "ItemImages",
                columns: new[] { "ItemId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ItemImage_Item_Resolution",
                schema: "inventory",
                table: "ItemImages",
                columns: new[] { "ItemId", "Resolution" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemLotStock_Batch_Warehouse_Bin",
                schema: "inventory",
                table: "ItemLotStocks",
                columns: new[] { "ItemBatchId", "WarehouseId", "BinId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemLotStocks_BinId",
                schema: "inventory",
                table: "ItemLotStocks",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLotStocks_WarehouseId",
                schema: "inventory",
                table: "ItemLotStocks",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrice_Item_PriceList",
                schema: "inventory",
                table: "ItemPrices",
                columns: new[] { "ItemId", "PriceList" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrice_Item_Unit_PriceList_Currency",
                schema: "inventory",
                table: "ItemPrices",
                columns: new[] { "ItemId", "UnitId", "PriceList", "CurrencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrices_UnitId",
                schema: "inventory",
                table: "ItemPrices",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_Status",
                schema: "inventory",
                table: "Items",
                columns: new[] { "IsActive", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_Item_Tenant_Code",
                schema: "inventory",
                table: "Items",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Item_Type",
                schema: "inventory",
                table: "Items",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_Items_BaseUnitId",
                schema: "inventory",
                table: "Items",
                column: "BaseUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_BrandId",
                schema: "inventory",
                table: "Items",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                schema: "inventory",
                table: "Items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_DisplayColorId",
                schema: "inventory",
                table: "Items",
                column: "DisplayColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSeo_Slug",
                schema: "inventory",
                table: "ItemSeos",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSeos_ItemId",
                schema: "inventory",
                table: "ItemSeos",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSerialHistory_Serial_Date",
                schema: "inventory",
                table: "ItemSerialHistories",
                columns: new[] { "ItemSerialId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSerial_Item_Status",
                schema: "inventory",
                table: "ItemSerials",
                columns: new[] { "ItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSerial_Tenant_Imei",
                schema: "inventory",
                table: "ItemSerials",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Imei" },
                filter: "[Imei] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSerial_Tenant_Item_Serial",
                schema: "inventory",
                table: "ItemSerials",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "ItemId", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSerial_Warehouse_Status",
                schema: "inventory",
                table: "ItemSerials",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSerials_BinId",
                schema: "inventory",
                table: "ItemSerials",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSerials_VariantId",
                schema: "inventory",
                table: "ItemSerials",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemShippings_ItemId",
                schema: "inventory",
                table: "ItemShippings",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSize_Item_Default",
                schema: "inventory",
                table: "ItemSizes",
                columns: new[] { "ItemId", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSize_Item_Size",
                schema: "inventory",
                table: "ItemSizes",
                columns: new[] { "ItemId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSizes_ItemBarcodeId",
                schema: "inventory",
                table: "ItemSizes",
                column: "ItemBarcodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSizes_SizeId",
                schema: "inventory",
                table: "ItemSizes",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSubstitution_Item_Substitute",
                schema: "inventory",
                table: "ItemSubstitutions",
                columns: new[] { "ItemId", "SubstituteItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSubstitutions_SubstituteItemId",
                schema: "inventory",
                table: "ItemSubstitutions",
                column: "SubstituteItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSupplier_Item_Primary",
                schema: "inventory",
                table: "ItemSuppliers",
                columns: new[] { "ItemId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSupplier_Item_Supplier",
                schema: "inventory",
                table: "ItemSuppliers",
                columns: new[] { "ItemId", "SupplierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemTax_Item_Tax",
                schema: "inventory",
                table: "ItemTaxes",
                columns: new[] { "ItemId", "TaxDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemTaxes_TaxDefinitionId",
                schema: "inventory",
                table: "ItemTaxes",
                column: "TaxDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemUomConversions_FromUnitId",
                schema: "inventory",
                table: "ItemUomConversions",
                column: "FromUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemUomConversions_ItemId",
                schema: "inventory",
                table: "ItemUomConversions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemUomConversions_ToUnitId",
                schema: "inventory",
                table: "ItemUomConversions",
                column: "ToUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariant_Code",
                schema: "inventory",
                table: "ItemVariants",
                column: "VariantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariant_Item_Color_Size",
                schema: "inventory",
                table: "ItemVariants",
                columns: new[] { "ItemId", "ColorId", "SizeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariant_Tenant_Code",
                schema: "inventory",
                table: "ItemVariants",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "VariantCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariants_ColorId",
                schema: "inventory",
                table: "ItemVariants",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariants_SizeId",
                schema: "inventory",
                table: "ItemVariants",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemWarranties_ItemId",
                schema: "inventory",
                table: "ItemWarranties",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Size_Tenant_Code_Chart",
                schema: "inventory",
                table: "Sizes",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code", "SizeChart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFile_Tenant",
                schema: "inventory",
                table: "StoredFiles",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDefinition_Tenant_Code",
                schema: "inventory",
                table: "TaxDefinitions",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryCostLayers_InventoryTransactions_ReceiptTransactionId",
                schema: "inventory",
                table: "InventoryCostLayers",
                column: "ReceiptTransactionId",
                principalSchema: "inventory",
                principalTable: "InventoryTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bins_Warehouses_WarehouseId",
                schema: "inventory",
                table: "Bins");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryCostLayers_Warehouses_WarehouseId",
                schema: "inventory",
                table: "InventoryCostLayers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryDocumentLines_Warehouses_WarehouseId",
                schema: "inventory",
                table: "InventoryDocumentLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryDocuments_Warehouses_FromWarehouseId",
                schema: "inventory",
                table: "InventoryDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryDocuments_Warehouses_ToWarehouseId",
                schema: "inventory",
                table: "InventoryDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemSerials_Warehouses_WarehouseId",
                schema: "inventory",
                table: "ItemSerials");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryDocumentLines_Bins_BinId",
                schema: "inventory",
                table: "InventoryDocumentLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Bins_BinId",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemSerials_Bins_BinId",
                schema: "inventory",
                table: "ItemSerials");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemBatches_ItemVariants_VariantId",
                schema: "inventory",
                table: "ItemBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemSerials_ItemVariants_VariantId",
                schema: "inventory",
                table: "ItemSerials");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryCostLayers_Items_ItemId",
                schema: "inventory",
                table: "InventoryCostLayers");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryDocumentLines_Items_ItemId",
                schema: "inventory",
                table: "InventoryDocumentLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Items_ItemId",
                schema: "inventory",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemBatches_Items_ItemId",
                schema: "inventory",
                table: "ItemBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemSerials_Items_ItemId",
                schema: "inventory",
                table: "ItemSerials");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryCostLayers_InventoryTransactions_ReceiptTransactionId",
                schema: "inventory",
                table: "InventoryCostLayers");

            migrationBuilder.DropTable(
                name: "InventoryBalances",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryDocumentLineSerials",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryValuations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemAttributes",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemBundles",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemChannelListings",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemColors",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemComments",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemDiscounts",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemImages",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemLotStocks",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemPrices",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemSeos",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemSerialHistories",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemShippings",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemSizes",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemSubstitutions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemSuppliers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemTaxes",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemUomConversions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemWarranties",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StoredFiles",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "AttributeDefinitions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemBarcodes",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "TaxDefinitions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Bins",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemVariants",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Sizes",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Items",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Brands",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Colors",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemCategories",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryTransactions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryCostLayers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryDocumentLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemBatches",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ItemSerials",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryDocuments",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Units",
                schema: "inventory");
        }
    }
}
