using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "AppNotifications",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSent = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Channel = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalPolicies",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("PK_ApprovalPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesTerritoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesChannel = table.Column<int>(type: "int", nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Basis = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    MaxCommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecognitionEvent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_CommissionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    MaxUsagePerCustomer = table.Column<int>(type: "int", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ApplicableChannel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetCustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RestrictedToProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RestrictedToCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsFreeDelivery = table.Column<bool>(type: "bit", nullable: false),
                    IsFirstOrderOnly = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    IsBaseCurrency = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerGroups",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AllowCreditSales = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_CustomerGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryZones",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZoneCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZoneName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeoPolygon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CenterLatitude = table.Column<double>(type: "float", nullable: true),
                    CenterLongitude = table.Column<double>(type: "float", nullable: true),
                    RadiusKm = table.Column<double>(type: "float", nullable: true),
                    DeliveryFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreeDeliveryAboveAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EstimatedDeliveryMinutes = table.Column<int>(type: "int", nullable: true),
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
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscountSchemes",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsCumulative = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_DiscountSchemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Separator = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IncludeYear = table.Column<bool>(type: "bit", nullable: false),
                    YearFormat = table.Column<int>(type: "int", nullable: false),
                    IncludeMonth = table.Column<bool>(type: "bit", nullable: false),
                    IncludeDay = table.Column<bool>(type: "bit", nullable: false),
                    SequencePadding = table.Column<int>(type: "int", nullable: false),
                    ResetOn = table.Column<int>(type: "int", nullable: false),
                    NextSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    LastResetYear = table.Column<int>(type: "int", nullable: true),
                    LastResetMonth = table.Column<int>(type: "int", nullable: true),
                    LastResetDay = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_DocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyAccounts",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    PointsBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LifetimePointsEarned = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LifetimePointsRedeemed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LifetimePointsExpired = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TierSpendAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TierExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_LoyaltyAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPrograms",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PointsPerCurrencyUnit = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    MinOrderAmountToEarn = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PointValueInCurrency = table.Column<decimal>(type: "decimal(10,6)", precision: 10, scale: 6, nullable: false),
                    MinPointsToRedeem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxRedemptionPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PointsExpiryDays = table.Column<int>(type: "int", nullable: true),
                    BronzeThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SilverThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GoldThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatinumThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiamondThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_LoyaltyPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosBarcodeLabelTemplates",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LabelWidthMm = table.Column<double>(type: "float", nullable: false),
                    LabelHeightMm = table.Column<double>(type: "float", nullable: false),
                    BarcodeSymbology = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeaderText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowProductName = table.Column<bool>(type: "bit", nullable: false),
                    ShowPrice = table.Column<bool>(type: "bit", nullable: false),
                    ShowSku = table.Column<bool>(type: "bit", nullable: false),
                    ShowBarcodeValue = table.Column<bool>(type: "bit", nullable: false),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductNameFontPt = table.Column<double>(type: "float", nullable: false),
                    PriceFontPt = table.Column<double>(type: "float", nullable: false),
                    BarcodeHeightPt = table.Column<double>(type: "float", nullable: false),
                    ShowBorders = table.Column<bool>(type: "bit", nullable: false),
                    RollPaper = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PosBarcodeLabelTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosCashDrawers",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DrawerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DrawerLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_PosCashDrawers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosReceiptTemplates",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeaderBusinessName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderAddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderAddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderWebsite = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FooterMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowBarcode = table.Column<bool>(type: "bit", nullable: false),
                    ShowQrCode = table.Column<bool>(type: "bit", nullable: false),
                    ShowCashierName = table.Column<bool>(type: "bit", nullable: false),
                    ShowCustomerName = table.Column<bool>(type: "bit", nullable: false),
                    ShowDiscountLine = table.Column<bool>(type: "bit", nullable: false),
                    ShowTaxBreakdown = table.Column<bool>(type: "bit", nullable: false),
                    ShowLoyaltyPoints = table.Column<bool>(type: "bit", nullable: false),
                    ShowSavingsAmount = table.Column<bool>(type: "bit", nullable: false),
                    PaperSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    PrintCopies = table.Column<int>(type: "int", nullable: false),
                    AutoCutPaper = table.Column<bool>(type: "bit", nullable: false),
                    OpenCashDrawer = table.Column<bool>(type: "bit", nullable: false),
                    BarcodeSymbology = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_PosReceiptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosSettings",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequireCashierPin = table.Column<bool>(type: "bit", nullable: false),
                    AutoLockMinutes = table.Column<int>(type: "int", nullable: false),
                    AllowPriceOverride = table.Column<bool>(type: "bit", nullable: false),
                    AllowDiscount = table.Column<bool>(type: "bit", nullable: false),
                    MaxDiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequireCustomer = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AllowNegativeStock = table.Column<bool>(type: "bit", nullable: false),
                    TaxInclusivePricing = table.Column<bool>(type: "bit", nullable: false),
                    AutoApplyTax = table.Column<bool>(type: "bit", nullable: false),
                    DefaultCurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoundingValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RoundingMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptCash = table.Column<bool>(type: "bit", nullable: false),
                    AcceptCard = table.Column<bool>(type: "bit", nullable: false),
                    AcceptMobilePayment = table.Column<bool>(type: "bit", nullable: false),
                    AcceptCreditOnAccount = table.Column<bool>(type: "bit", nullable: false),
                    AllowSplitPayment = table.Column<bool>(type: "bit", nullable: false),
                    AllowPartialPayment = table.Column<bool>(type: "bit", nullable: false),
                    DefaultPaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutoOpenCashDrawer = table.Column<bool>(type: "bit", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CashGlAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankGlAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowProductImages = table.Column<bool>(type: "bit", nullable: false),
                    ShowProductDescription = table.Column<bool>(type: "bit", nullable: false),
                    AllowItemNotes = table.Column<bool>(type: "bit", nullable: false),
                    AllowDecimalQuantity = table.Column<bool>(type: "bit", nullable: false),
                    BarcodeScanSound = table.Column<bool>(type: "bit", nullable: false),
                    ShowStockLevel = table.Column<bool>(type: "bit", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "int", nullable: false),
                    ItemsPerPage = table.Column<int>(type: "int", nullable: false),
                    ShowCategoryFilter = table.Column<bool>(type: "bit", nullable: false),
                    TouchMode = table.Column<bool>(type: "bit", nullable: false),
                    OperatingMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequireOpeningFloat = table.Column<bool>(type: "bit", nullable: false),
                    RequireCashCountOnClose = table.Column<bool>(type: "bit", nullable: false),
                    AutoCloseSession = table.Column<bool>(type: "bit", nullable: false),
                    AutoCloseTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnableLoyaltyPoints = table.Column<bool>(type: "bit", nullable: false),
                    EnablePromotions = table.Column<bool>(type: "bit", nullable: false),
                    EnableCoupons = table.Column<bool>(type: "bit", nullable: false),
                    AutoApplyPromotions = table.Column<bool>(type: "bit", nullable: false),
                    DefaultPaperSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiptCopies = table.Column<int>(type: "int", nullable: false),
                    AutoPrintReceipt = table.Column<bool>(type: "bit", nullable: false),
                    AskToPrintReceipt = table.Column<bool>(type: "bit", nullable: false),
                    SkipReceiptScreen = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowLogo = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowBusinessName = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowAddress = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowContact = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowCashierName = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowCustomerName = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowOrderNumber = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowDateTime = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowItemCodes = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowUnitPrice = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowLineDiscount = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowTaxBreakdown = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowDiscountLine = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowBarcode = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptShowQrCode = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptBarcodeSymbology = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiptAutoCutPaper = table.Column<bool>(type: "bit", nullable: false),
                    ReceiptHeaderNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiptFooterMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiptReturnPolicy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultProductView = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShowNumpad = table.Column<bool>(type: "bit", nullable: false),
                    ShowFavouritesBar = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PosSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceLists",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsTaxInclusive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PromotionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAutoApplied = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ScheduledDays = table.Column<int>(type: "int", nullable: false),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: true),
                    MaxUsagePerCustomer = table.Column<int>(type: "int", nullable: true),
                    CurrentUsageCount = table.Column<int>(type: "int", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    RequiredLoyaltyTier = table.Column<int>(type: "int", nullable: true),
                    RequiredPriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsStackable = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesTargets",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesTerritoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Period = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("PK_SalesTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesTeams",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeamLeaderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeamLeaderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SalesTeams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesTerritories",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentTerritoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_SalesTerritories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesTerritories_SalesTerritories_ParentTerritoryId",
                        column: x => x.ParentTerritoryId,
                        principalSchema: "sales",
                        principalTable: "SalesTerritories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                schema: "sales",
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
                name: "TaxGroups",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("PK_TaxGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VariantName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_WishlistItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalPolicyConditions",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Field = table.Column<int>(type: "int", nullable: false),
                    Operator = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_ApprovalPolicyConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalPolicyConditions_ApprovalPolicies_ApprovalPolicyId",
                        column: x => x.ApprovalPolicyId,
                        principalSchema: "sales",
                        principalTable: "ApprovalPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalPolicySteps",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApproverRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EscalationHours = table.Column<int>(type: "int", nullable: true),
                    EscalationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EscalationRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_ApprovalPolicySteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalPolicySteps_ApprovalPolicies_ApprovalPolicyId",
                        column: x => x.ApprovalPolicyId,
                        principalSchema: "sales",
                        principalTable: "ApprovalPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseCurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RateType = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    RateName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyRates_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "sales",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Riders",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HrEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContractType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HomeBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    VehiclePlateNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "float", nullable: true),
                    CurrentLongitude = table.Column<double>(type: "float", nullable: true),
                    LastLocationUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalDeliveries = table.Column<int>(type: "int", nullable: false),
                    SuccessfulDeliveries = table.Column<int>(type: "int", nullable: false),
                    FailedDeliveries = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Riders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Riders_DeliveryZones_ZoneId",
                        column: x => x.ZoneId,
                        principalSchema: "sales",
                        principalTable: "DeliveryZones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoyaltyAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SourceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceChannel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_LoyaltyAccounts_LoyaltyAccountId",
                        column: x => x.LoyaltyAccountId,
                        principalSchema: "sales",
                        principalTable: "LoyaltyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosStores",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    CountryCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true),
                    NativeLanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StoreType = table.Column<int>(type: "int", nullable: false),
                    StoreFormat = table.Column<int>(type: "int", nullable: false),
                    DefaultWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultPriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcceptsOnlinePickup = table.Column<bool>(type: "bit", nullable: false),
                    HasDelivery = table.Column<bool>(type: "bit", nullable: false),
                    IsOnlineOrderingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    OnlineStatus = table.Column<int>(type: "int", nullable: false),
                    OnlineStatusNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedPrepTimeMinutes = table.Column<int>(type: "int", nullable: true),
                    MinOnlineOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxDeliveryRadiusKm = table.Column<double>(type: "float", nullable: true),
                    OnlineLogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnlineBannerUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    OnboardingStatus = table.Column<int>(type: "int", nullable: true),
                    PosReceiptTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosStores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosStores_PosReceiptTemplates_PosReceiptTemplateId",
                        column: x => x.PosReceiptTemplateId,
                        principalSchema: "sales",
                        principalTable: "PosReceiptTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosStores_PosReceiptTemplates_ReceiptTemplateId",
                        column: x => x.ReceiptTemplateId,
                        principalSchema: "sales",
                        principalTable: "PosReceiptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PosStores_PriceLists_DefaultPriceListId",
                        column: x => x.DefaultPriceListId,
                        principalSchema: "sales",
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MinQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListItems_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "sales",
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuotationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QuotationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    Incoterm = table.Column<int>(type: "int", nullable: true),
                    IncotermLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShipToAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShippingMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesTerritoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CrmDealId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedToSalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerPONumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerPODate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsAndConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotations_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "sales",
                        principalTable: "PriceLists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesAgreements",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AgreementName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    CommittedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReleasedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TermsAndConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SalesAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesAgreements_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "sales",
                        principalTable: "PriceLists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PromotionItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PriceTarget = table.Column<int>(type: "int", nullable: false),
                    IsConditional = table.Column<bool>(type: "bit", nullable: false),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    ConditionQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ConditionAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxDiscountedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BuyQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    GetQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    FreeItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PromotionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionItems_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "sales",
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesTeamMembers",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SalesTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesTeamMembers_SalesTeams_SalesTeamId",
                        column: x => x.SalesTeamId,
                        principalSchema: "sales",
                        principalTable: "SalesTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepTerritories",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesTerritoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedTo = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_SalesRepTerritories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepTerritories_SalesTerritories_SalesTerritoryId",
                        column: x => x.SalesTerritoryId,
                        principalSchema: "sales",
                        principalTable: "SalesTerritories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxGroupRates",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SnapshotRate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    SnapshotTaxType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SnapshotInclusionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsCompound = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TaxGroupRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxGroupRates_TaxGroups_TaxGroupId",
                        column: x => x.TaxGroupId,
                        principalSchema: "sales",
                        principalTable: "TaxGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxRules",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ProductTaxCategory = table.Column<int>(type: "int", nullable: true),
                    CustomerCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    CustomerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SalesChannel = table.Column<int>(type: "int", nullable: true),
                    TaxGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxRules_TaxGroups_TaxGroupId",
                        column: x => x.TaxGroupId,
                        principalSchema: "sales",
                        principalTable: "TaxGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderShifts",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartLatitude = table.Column<double>(type: "float", nullable: true),
                    StartLongitude = table.Column<double>(type: "float", nullable: true),
                    EndLatitude = table.Column<double>(type: "float", nullable: true),
                    EndLongitude = table.Column<double>(type: "float", nullable: true),
                    DeliveriesCompleted = table.Column<int>(type: "int", nullable: false),
                    TotalDistanceKm = table.Column<double>(type: "float", nullable: true),
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
                    table.PrimaryKey("PK_RiderShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderShifts_Riders_RiderId",
                        column: x => x.RiderId,
                        principalSchema: "sales",
                        principalTable: "Riders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryZonePosStore",
                schema: "sales",
                columns: table => new
                {
                    DeliveryZonesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoresId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryZonePosStore", x => new { x.DeliveryZonesId, x.StoresId });
                    table.ForeignKey(
                        name: "FK_DeliveryZonePosStore_DeliveryZones_DeliveryZonesId",
                        column: x => x.DeliveryZonesId,
                        principalSchema: "sales",
                        principalTable: "DeliveryZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryZonePosStore_PosStores_StoresId",
                        column: x => x.StoresId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosCashiers",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PinCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BadgeNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PosStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanApplyManualDiscount = table.Column<bool>(type: "bit", nullable: false),
                    MaxManualDiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CanVoidTransaction = table.Column<bool>(type: "bit", nullable: false),
                    CanIssueRefund = table.Column<bool>(type: "bit", nullable: false),
                    CanOpenDrawer = table.Column<bool>(type: "bit", nullable: false),
                    CanOverridePrices = table.Column<bool>(type: "bit", nullable: false),
                    CanApplyCoupons = table.Column<bool>(type: "bit", nullable: false),
                    CanAccessReports = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PosCashiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosCashiers_PosStores_PosStoreId",
                        column: x => x.PosStoreId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosStoreHolidays",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPartiallyOpen = table.Column<bool>(type: "bit", nullable: false),
                    OpeningTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosingTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PosStoreHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosStoreHolidays_PosStores_PosStoreId",
                        column: x => x.PosStoreId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosStoreSchedules",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    OpeningTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosingTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PosStoreSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosStoreSchedules_PosStores_PosStoreId",
                        column: x => x.PosStoreId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosTerminals",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TerminalCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TerminalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PosStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashDrawerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    CurrentSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PosTerminals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosTerminals_PosCashDrawers_CashDrawerId",
                        column: x => x.CashDrawerId,
                        principalSchema: "sales",
                        principalTable: "PosCashDrawers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosTerminals_PosReceiptTemplates_ReceiptTemplateId",
                        column: x => x.ReceiptTemplateId,
                        principalSchema: "sales",
                        principalTable: "PosReceiptTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosTerminals_PosStores_PosStoreId",
                        column: x => x.PosStoreId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreMenus",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    AvailableFrom = table.Column<TimeOnly>(type: "time", nullable: true),
                    AvailableTo = table.Column<TimeOnly>(type: "time", nullable: true),
                    PosStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_StoreMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreMenus_PosStores_PosStoreId",
                        column: x => x.PosStoreId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StoreOffers",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannerUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BadgeText = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BadgeColor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CallToAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeepLinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_StoreOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreOffers_PosStores_StoreId",
                        column: x => x.StoreId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreOffers_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "sales",
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StoreVendorProfiles",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerCnic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OwnerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OwnerEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CnicFrontDocUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CnicBackDocUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BusinessRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FoodLicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FoodLicenseExpiry = table.Column<DateOnly>(type: "date", nullable: true),
                    FoodLicenseDocUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BankBranch = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AccountTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IbanNumber = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    FacebookUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstagramUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TiktokUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WhatsappNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StoreFrontPhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KitchenPhotosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnboardingStatus = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_StoreVendorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreVendorProfiles_PosStores_StoreId",
                        column: x => x.StoreId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationApprovals",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_QuotationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationApprovals_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "sales",
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxCategory = table.Column<int>(type: "int", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_QuotationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationLines_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "sales",
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesAgreementLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommittedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReleasedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AgreedUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_SalesAgreementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesAgreementLines_SalesAgreements_SalesAgreementId",
                        column: x => x.SalesAgreementId,
                        principalSchema: "sales",
                        principalTable: "SalesAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosSessions",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PosTerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosCashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningFloat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpeningNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpeningDenominationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosingFloat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedClosingFloat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FloatVariance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingDenominationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalSalesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRefundsAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDiscountsAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetSalesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashCollected = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CardCollected = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WalletCollected = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherCollected = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    ClosingNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PosSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosSessions_PosCashiers_PosCashierId",
                        column: x => x.PosCashierId,
                        principalSchema: "sales",
                        principalTable: "PosCashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSessions_PosTerminals_PosTerminalId",
                        column: x => x.PosTerminalId,
                        principalSchema: "sales",
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreMenuSections",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreMenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_StoreMenuSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreMenuSections_StoreMenus_StoreMenuId",
                        column: x => x.StoreMenuId,
                        principalSchema: "sales",
                        principalTable: "StoreMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosCashMovements",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosCashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_PosCashMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosCashMovements_PosCashiers_PosCashierId",
                        column: x => x.PosCashierId,
                        principalSchema: "sales",
                        principalTable: "PosCashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PosCashMovements_PosSessions_PosSessionId",
                        column: x => x.PosSessionId,
                        principalSchema: "sales",
                        principalTable: "PosSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalPolicyStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EscalatedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueBy = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_ApprovalPolicies_ApprovalPolicyId",
                        column: x => x.ApprovalPolicyId,
                        principalSchema: "sales",
                        principalTable: "ApprovalPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_ApprovalPolicySteps_ApprovalPolicyStepId",
                        column: x => x.ApprovalPolicyStepId,
                        principalSchema: "sales",
                        principalTable: "ApprovalPolicySteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommissionEntries",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommissionRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EarnedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayrollEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_CommissionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionEntries_CommissionRules_CommissionRuleId",
                        column: x => x.CommissionRuleId,
                        principalSchema: "sales",
                        principalTable: "CommissionRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CouponUsages",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PosTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiscountApplied = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_CouponUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CouponUsages_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalSchema: "sales",
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditNoteLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxCategory = table.Column<int>(type: "int", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_CreditNoteLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditNoteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deliveries",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PlannedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualShipDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Carrier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShippingMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Incoterm = table.Column<int>(type: "int", nullable: true),
                    IncotermLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientWhatsApp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryLatitude = table.Column<double>(type: "float", nullable: true),
                    DeliveryLongitude = table.Column<double>(type: "float", nullable: true),
                    DeliveryNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedRiderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberOfPackages = table.Column<int>(type: "int", nullable: true),
                    TotalWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WeightUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalVolume = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VolumeUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deliveries_Riders_AssignedRiderId",
                        column: x => x.AssignedRiderId,
                        principalSchema: "sales",
                        principalTable: "Riders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeliveryPackages",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PackageType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WeightUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Length = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Width = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DimensionUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SealNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_DeliveryPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryPackages_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "sales",
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BinLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_DeliveryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryLines_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "sales",
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosCashDrawerEvents",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosCashDrawerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpenReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PosSessionId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PosCashDrawerEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosCashDrawerEvents_PosCashDrawers_PosCashDrawerId",
                        column: x => x.PosCashDrawerId,
                        principalSchema: "sales",
                        principalTable: "PosCashDrawers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosCashDrawerEvents_PosCashiers_CashierId",
                        column: x => x.CashierId,
                        principalSchema: "sales",
                        principalTable: "PosCashiers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosCashDrawerEvents_PosSessions_PosSessionId",
                        column: x => x.PosSessionId,
                        principalSchema: "sales",
                        principalTable: "PosSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosCashDrawerEvents_PosSessions_PosSessionId1",
                        column: x => x.PosSessionId1,
                        principalSchema: "sales",
                        principalTable: "PosSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PosGiftCards",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PinHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedInTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PosGiftCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosGiftCardTransactions",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosGiftCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PosTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_PosGiftCardTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosGiftCardTransactions_PosGiftCards_PosGiftCardId",
                        column: x => x.PosGiftCardId,
                        principalSchema: "sales",
                        principalTable: "PosGiftCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosPayments",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CardScheme = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CardLast4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PosPayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosTransactionLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VariantName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxCategory = table.Column<int>(type: "int", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsRefunded = table.Column<bool>(type: "bit", nullable: false),
                    RefundedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_PosTransactionLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosTransactions",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PosSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosTerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosCashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RoundingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TenderedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CouponCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CouponDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoyaltyPointsEarned = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoyaltyPointsRedeemed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PosTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosTransactions_PosCashiers_PosCashierId",
                        column: x => x.PosCashierId,
                        principalSchema: "sales",
                        principalTable: "PosCashiers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosTransactions_PosSessions_PosSessionId",
                        column: x => x.PosSessionId,
                        principalSchema: "sales",
                        principalTable: "PosSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosTransactions_PosStores_PosStoreId",
                        column: x => x.PosStoreId,
                        principalSchema: "sales",
                        principalTable: "PosStores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosTransactions_PosTerminals_PosTerminalId",
                        column: x => x.PosTerminalId,
                        principalSchema: "sales",
                        principalTable: "PosTerminals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosTransactions_PosTransactions_OriginalTransactionId",
                        column: x => x.OriginalTransactionId,
                        principalSchema: "sales",
                        principalTable: "PosTransactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OfflineOrderNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OrderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerPONumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerPODate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlacedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesChannel = table.Column<int>(type: "int", nullable: false),
                    OriginBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginPosTerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginPosCashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginPosSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosingPosTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CouponCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CouponDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoyaltyPointsRedeemed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoyaltyPointsEarned = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    Incoterm = table.Column<int>(type: "int", nullable: true),
                    IncotermLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceStatus = table.Column<int>(type: "int", nullable: false),
                    InvoicePolicy = table.Column<int>(type: "int", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    FulfillmentType = table.Column<int>(type: "int", nullable: false),
                    BillToName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToStreet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToPostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShipToAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TipAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditCheckPassed = table.Column<bool>(type: "bit", nullable: false),
                    CreditCheckDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesTerritoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsAndConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_PosCashiers_OriginPosCashierId",
                        column: x => x.OriginPosCashierId,
                        principalSchema: "sales",
                        principalTable: "PosCashiers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrders_PosSessions_OriginPosSessionId",
                        column: x => x.OriginPosSessionId,
                        principalSchema: "sales",
                        principalTable: "PosSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrders_PosTerminals_OriginPosTerminalId",
                        column: x => x.OriginPosTerminalId,
                        principalSchema: "sales",
                        principalTable: "PosTerminals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrders_PosTransactions_ClosingPosTransactionId",
                        column: x => x.ClosingPosTransactionId,
                        principalSchema: "sales",
                        principalTable: "PosTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrders_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "sales",
                        principalTable: "PriceLists",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "sales",
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_SalesAgreements_SalesAgreementId",
                        column: x => x.SalesAgreementId,
                        principalSchema: "sales",
                        principalTable: "SalesAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_SalesTerritories_SalesTerritoryId",
                        column: x => x.SalesTerritoryId,
                        principalSchema: "sales",
                        principalTable: "SalesTerritories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductReviews",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerifiedPurchase = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_ProductReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviews_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RiderAssignments",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArrivedAtStoreAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickedUpAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutForDeliveryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickupBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PickupLatitude = table.Column<double>(type: "float", nullable: true),
                    PickupLongitude = table.Column<double>(type: "float", nullable: true),
                    DeliveryLatitude = table.Column<double>(type: "float", nullable: true),
                    DeliveryLongitude = table.Column<double>(type: "float", nullable: true),
                    EstimatedDistanceKm = table.Column<double>(type: "float", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ActualDistanceKm = table.Column<double>(type: "float", nullable: true),
                    ActualDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ProofImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientSignatureUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsReassigned = table.Column<bool>(type: "bit", nullable: false),
                    ReassignedToAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_RiderAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderAssignments_Riders_RiderId",
                        column: x => x.RiderId,
                        principalSchema: "sales",
                        principalTable: "Riders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderAssignments_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RiderRatings",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_RiderRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderRatings_Riders_RiderId",
                        column: x => x.RiderId,
                        principalSchema: "sales",
                        principalTable: "Riders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RiderRatings_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BillToName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToStreet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToPostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    SalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesRepName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecipientBankAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiscalPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutoPost = table.Column<int>(type: "int", nullable: false),
                    LastReminderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    AccountingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "sales",
                        principalTable: "Deliveries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesInvoices_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderApprovals",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_SalesOrderApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderApprovals_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderAttachments",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_SalesOrderAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderAttachments_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VariantName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CancelledQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxCategory = table.Column<int>(type: "int", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MarginAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MarginPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LineStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_SalesOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderStatusHistories",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
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
                    table.PrimaryKey("PK_SalesOrderStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderStatusHistories_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesPayments",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GatewayTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GatewayResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRefunded = table.Column<bool>(type: "bit", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountingReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_SalesPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesPayments_PosTransactions_PosTransactionId",
                        column: x => x.PosTransactionId,
                        principalSchema: "sales",
                        principalTable: "PosTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesPayments_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductReviewImages",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviewImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviewImages_ProductReviews_ProductReviewId",
                        column: x => x.ProductReviewId,
                        principalSchema: "sales",
                        principalTable: "ProductReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiderLocationLogs",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    SpeedKmh = table.Column<double>(type: "float", nullable: true),
                    Bearing = table.Column<double>(type: "float", nullable: true),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_RiderLocationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderLocationLogs_RiderAssignments_RiderAssignmentId",
                        column: x => x.RiderAssignmentId,
                        principalSchema: "sales",
                        principalTable: "RiderAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RiderLocationLogs_Riders_RiderId",
                        column: x => x.RiderId,
                        principalSchema: "sales",
                        principalTable: "Riders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturns",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreditIssuedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RmaNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InspectionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalRefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_SalesReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturns_CreditNotes_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalSchema: "sales",
                        principalTable: "CreditNotes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesReturns_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalSchema: "sales",
                        principalTable: "SalesInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesReturns_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxCategory = table.Column<int>(type: "int", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDownPayment = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SalesInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalSchema: "sales",
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_SalesOrderLines_SalesOrderLineId",
                        column: x => x.SalesOrderLineId,
                        principalSchema: "sales",
                        principalTable: "SalesOrderLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderLineAddons",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddonProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AddonName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_SalesOrderLineAddons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderLineAddons_SalesOrderLines_SalesOrderLineId",
                        column: x => x.SalesOrderLineId,
                        principalSchema: "sales",
                        principalTable: "SalesOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConditionOnReturn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SalesReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_SalesOrderLines_SalesOrderLineId",
                        column: x => x.SalesOrderLineId,
                        principalSchema: "sales",
                        principalTable: "SalesOrderLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_SalesReturns_SalesReturnId",
                        column: x => x.SalesReturnId,
                        principalSchema: "sales",
                        principalTable: "SalesReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicyConditions_ApprovalPolicyId",
                schema: "sales",
                table: "ApprovalPolicyConditions",
                column: "ApprovalPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicySteps_ApprovalPolicyId",
                schema: "sales",
                table: "ApprovalPolicySteps",
                column: "ApprovalPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequest_Order_Step",
                schema: "sales",
                table: "ApprovalRequests",
                columns: new[] { "SalesOrderId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ApprovalPolicyId",
                schema: "sales",
                table: "ApprovalRequests",
                column: "ApprovalPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ApprovalPolicyStepId",
                schema: "sales",
                table: "ApprovalRequests",
                column: "ApprovalPolicyStepId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionEntries_CommissionRuleId",
                schema: "sales",
                table: "CommissionEntries",
                column: "CommissionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionEntries_SalesOrderId",
                schema: "sales",
                table: "CommissionEntries",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionEntry_Rep_Status_Date",
                schema: "sales",
                table: "CommissionEntries",
                columns: new[] { "SalesRepId", "Status", "EarnedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Coupon_Tenant_Code",
                schema: "sales",
                table: "Coupons",
                columns: new[] { "CompanyId", "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId",
                schema: "sales",
                table: "CouponUsages",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_PosTransactionId",
                schema: "sales",
                table: "CouponUsages",
                column: "PosTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_SalesOrderId",
                schema: "sales",
                table: "CouponUsages",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNoteLines_CreditNoteId",
                schema: "sales",
                table: "CreditNoteLines",
                column: "CreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_SalesInvoiceId",
                schema: "sales",
                table: "CreditNotes",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_CurrencyId",
                schema: "sales",
                table: "CurrencyRates",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_AssignedRiderId",
                schema: "sales",
                table: "Deliveries",
                column: "AssignedRiderId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_SalesOrderId",
                schema: "sales",
                table: "Deliveries",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_Tenant_Number",
                schema: "sales",
                table: "Deliveries",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "DeliveryNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryLines_DeliveryId",
                schema: "sales",
                table: "DeliveryLines",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryLines_SalesOrderLineId",
                schema: "sales",
                table: "DeliveryLines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPackages_DeliveryId",
                schema: "sales",
                table: "DeliveryPackages",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZonePosStore_StoresId",
                schema: "sales",
                table: "DeliveryZonePosStore",
                column: "StoresId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSequence_Tenant_Type",
                schema: "sales",
                table: "DocumentSequences",
                columns: new[] { "CompanyId", "BranchId", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_LoyaltyAccountId",
                schema: "sales",
                table: "LoyaltyTransactions",
                column: "LoyaltyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_Invoice",
                schema: "sales",
                table: "PaymentAllocations",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_Payment_Invoice",
                schema: "sales",
                table: "PaymentAllocations",
                columns: new[] { "SalesPaymentId", "SalesInvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosCashDrawerEvent_Drawer_Date",
                schema: "sales",
                table: "PosCashDrawerEvents",
                columns: new[] { "PosCashDrawerId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PosCashDrawerEvents_CashierId",
                schema: "sales",
                table: "PosCashDrawerEvents",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCashDrawerEvents_PosSessionId",
                schema: "sales",
                table: "PosCashDrawerEvents",
                column: "PosSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCashDrawerEvents_PosSessionId1",
                schema: "sales",
                table: "PosCashDrawerEvents",
                column: "PosSessionId1");

            migrationBuilder.CreateIndex(
                name: "IX_PosCashDrawerEvents_PosTransactionId",
                schema: "sales",
                table: "PosCashDrawerEvents",
                column: "PosTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCashiers_PosStoreId",
                schema: "sales",
                table: "PosCashiers",
                column: "PosStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCashMovements_PosCashierId",
                schema: "sales",
                table: "PosCashMovements",
                column: "PosCashierId");

            migrationBuilder.CreateIndex(
                name: "IX_PosCashMovements_PosSessionId",
                schema: "sales",
                table: "PosCashMovements",
                column: "PosSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosGiftCard_Tenant_Number",
                schema: "sales",
                table: "PosGiftCards",
                columns: new[] { "CompanyId", "BranchId", "CardNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosGiftCards_IssuedInTransactionId",
                schema: "sales",
                table: "PosGiftCards",
                column: "IssuedInTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosGiftCardTransactions_PosGiftCardId",
                schema: "sales",
                table: "PosGiftCardTransactions",
                column: "PosGiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_PosGiftCardTransactions_PosTransactionId",
                schema: "sales",
                table: "PosGiftCardTransactions",
                column: "PosTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosPayments_PosTransactionId",
                schema: "sales",
                table: "PosPayments",
                column: "PosTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSessions_PosCashierId",
                schema: "sales",
                table: "PosSessions",
                column: "PosCashierId");

            migrationBuilder.CreateIndex(
                name: "UX_PosSessions_OpenPerTerminal",
                schema: "sales",
                table: "PosSessions",
                column: "PosTerminalId",
                unique: true,
                filter: "[Status] = 0 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PosStoreHolidays_PosStoreId",
                schema: "sales",
                table: "PosStoreHolidays",
                column: "PosStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PosStore_Company_TradingName",
                schema: "sales",
                table: "PosStores",
                columns: new[] { "CompanyId", "TradingName" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [TradingName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PosStores_DefaultPriceListId",
                schema: "sales",
                table: "PosStores",
                column: "DefaultPriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_PosStores_PosReceiptTemplateId",
                schema: "sales",
                table: "PosStores",
                column: "PosReceiptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PosStores_ReceiptTemplateId",
                schema: "sales",
                table: "PosStores",
                column: "ReceiptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PosStoreSchedules_PosStoreId",
                schema: "sales",
                table: "PosStoreSchedules",
                column: "PosStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_CashDrawerId",
                schema: "sales",
                table: "PosTerminals",
                column: "CashDrawerId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_PosStoreId",
                schema: "sales",
                table: "PosTerminals",
                column: "PosStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_ReceiptTemplateId",
                schema: "sales",
                table: "PosTerminals",
                column: "ReceiptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactionLines_PosTransactionId",
                schema: "sales",
                table: "PosTransactionLines",
                column: "PosTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransaction_Tenant_Number",
                schema: "sales",
                table: "PosTransactions",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "TransactionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_OriginalTransactionId",
                schema: "sales",
                table: "PosTransactions",
                column: "OriginalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_PosCashierId",
                schema: "sales",
                table: "PosTransactions",
                column: "PosCashierId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_PosSessionId",
                schema: "sales",
                table: "PosTransactions",
                column: "PosSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_PosStoreId",
                schema: "sales",
                table: "PosTransactions",
                column: "PosStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_PosTerminalId",
                schema: "sales",
                table: "PosTransactions",
                column: "PosTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_SalesOrderId",
                schema: "sales",
                table: "PosTransactions",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_PriceListId",
                schema: "sales",
                table: "PriceListItems",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceList_Tenant_Code",
                schema: "sales",
                table: "PriceLists",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviewImages_ProductReviewId",
                schema: "sales",
                table: "ProductReviewImages",
                column: "ProductReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_SalesOrderId",
                schema: "sales",
                table: "ProductReviews",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionItems_PromotionId",
                schema: "sales",
                table: "PromotionItems",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotion_Tenant_Code",
                schema: "sales",
                table: "Promotions",
                columns: new[] { "CompanyId", "BranchId", "PromotionCode" },
                unique: true,
                filter: "[PromotionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationApprovals_QuotationId",
                schema: "sales",
                table: "QuotationApprovals",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationLines_QuotationId",
                schema: "sales",
                table: "QuotationLines",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_Tenant_Number",
                schema: "sales",
                table: "Quotations",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "QuotationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_PriceListId",
                schema: "sales",
                table: "Quotations",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderAssignments_RiderId",
                schema: "sales",
                table: "RiderAssignments",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderAssignments_SalesOrderId",
                schema: "sales",
                table: "RiderAssignments",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderLocationLogs_RiderAssignmentId",
                schema: "sales",
                table: "RiderLocationLogs",
                column: "RiderAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderLocationLogs_RiderId",
                schema: "sales",
                table: "RiderLocationLogs",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderRatings_RiderId",
                schema: "sales",
                table: "RiderRatings",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderRatings_SalesOrderId",
                schema: "sales",
                table: "RiderRatings",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Riders_ZoneId",
                schema: "sales",
                table: "Riders",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderShifts_RiderId",
                schema: "sales",
                table: "RiderShifts",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesAgreementLines_SalesAgreementId",
                schema: "sales",
                table: "SalesAgreementLines",
                column: "SalesAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesAgreements_PriceListId",
                schema: "sales",
                table: "SalesAgreements",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_SalesInvoiceId",
                schema: "sales",
                table: "SalesInvoiceLines",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_SalesOrderLineId",
                schema: "sales",
                table: "SalesInvoiceLines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoice_Tenant_Number",
                schema: "sales",
                table: "SalesInvoices",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_DeliveryId",
                schema: "sales",
                table: "SalesInvoices",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_SalesOrderId",
                schema: "sales",
                table: "SalesInvoices",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderApprovals_SalesOrderId",
                schema: "sales",
                table: "SalesOrderApprovals",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderAttachments_SalesOrderId",
                schema: "sales",
                table: "SalesOrderAttachments",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLineAddons_SalesOrderLineId",
                schema: "sales",
                table: "SalesOrderLineAddons",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_SalesOrderId",
                schema: "sales",
                table: "SalesOrderLines",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_Branch_Status_PlacedAt",
                schema: "sales",
                table: "SalesOrders",
                columns: new[] { "OriginBranchId", "Status", "PlacedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_Contact_Status",
                schema: "sales",
                table: "SalesOrders",
                columns: new[] { "ContactId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_OfflineOrderNumber",
                schema: "sales",
                table: "SalesOrders",
                column: "OfflineOrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_Tenant_Number",
                schema: "sales",
                table: "SalesOrders",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ClosingPosTransactionId",
                schema: "sales",
                table: "SalesOrders",
                column: "ClosingPosTransactionId",
                unique: true,
                filter: "[ClosingPosTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OriginPosCashierId",
                schema: "sales",
                table: "SalesOrders",
                column: "OriginPosCashierId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OriginPosSessionId",
                schema: "sales",
                table: "SalesOrders",
                column: "OriginPosSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OriginPosTerminalId",
                schema: "sales",
                table: "SalesOrders",
                column: "OriginPosTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_PriceListId",
                schema: "sales",
                table: "SalesOrders",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_QuotationId",
                schema: "sales",
                table: "SalesOrders",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesAgreementId",
                schema: "sales",
                table: "SalesOrders",
                column: "SalesAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesTerritoryId",
                schema: "sales",
                table: "SalesOrders",
                column: "SalesTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderStatusHistory_Order_Date",
                schema: "sales",
                table: "SalesOrderStatusHistories",
                columns: new[] { "SalesOrderId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesPayment_Tenant_Number",
                schema: "sales",
                table: "SalesPayments",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "PaymentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesPayments_PosTransactionId",
                schema: "sales",
                table: "SalesPayments",
                column: "PosTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPayments_SalesOrderId",
                schema: "sales",
                table: "SalesPayments",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepTerritories_SalesTerritoryId",
                schema: "sales",
                table: "SalesRepTerritories",
                column: "SalesTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_SalesOrderLineId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_SalesReturnId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "SalesReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_Tenant_Number",
                schema: "sales",
                table: "SalesReturns",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "ReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CreditNoteId",
                schema: "sales",
                table: "SalesReturns",
                column: "CreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_SalesInvoiceId",
                schema: "sales",
                table: "SalesReturns",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_SalesOrderId",
                schema: "sales",
                table: "SalesReturns",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTarget_Rep_Period",
                schema: "sales",
                table: "SalesTargets",
                columns: new[] { "SalesRepId", "Period", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesTeamMembers_SalesTeamId",
                schema: "sales",
                table: "SalesTeamMembers",
                column: "SalesTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTerritories_ParentTerritoryId",
                schema: "sales",
                table: "SalesTerritories",
                column: "ParentTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFile_Tenant",
                schema: "sales",
                table: "StoredFiles",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreMenus_PosStoreId",
                schema: "sales",
                table: "StoreMenus",
                column: "PosStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreMenuSection_Menu_Category",
                schema: "sales",
                table: "StoreMenuSections",
                columns: new[] { "StoreMenuId", "ItemCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreOffer_Store_Active_Order",
                schema: "sales",
                table: "StoreOffers",
                columns: new[] { "StoreId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreOffers_PromotionId",
                schema: "sales",
                table: "StoreOffers",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreVendorProfile_StoreId",
                schema: "sales",
                table: "StoreVendorProfiles",
                column: "StoreId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxGroupRate_Group_TaxDef",
                schema: "sales",
                table: "TaxGroupRates",
                columns: new[] { "TaxGroupId", "TaxDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxGroup_Tenant_Code",
                schema: "sales",
                table: "TaxGroups",
                columns: new[] { "CompanyId", "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRule_Tenant_Priority",
                schema: "sales",
                table: "TaxRules",
                columns: new[] { "CompanyId", "BranchId", "Priority", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRules_TaxGroupId",
                schema: "sales",
                table: "TaxRules",
                column: "TaxGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_SalesOrders_SalesOrderId",
                schema: "sales",
                table: "ApprovalRequests",
                column: "SalesOrderId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionEntries_SalesOrders_SalesOrderId",
                schema: "sales",
                table: "CommissionEntries",
                column: "SalesOrderId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CouponUsages_PosTransactions_PosTransactionId",
                schema: "sales",
                table: "CouponUsages",
                column: "PosTransactionId",
                principalSchema: "sales",
                principalTable: "PosTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CouponUsages_SalesOrders_SalesOrderId",
                schema: "sales",
                table: "CouponUsages",
                column: "SalesOrderId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditNoteLines_CreditNotes_CreditNoteId",
                schema: "sales",
                table: "CreditNoteLines",
                column: "CreditNoteId",
                principalSchema: "sales",
                principalTable: "CreditNotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditNotes_SalesInvoices_SalesInvoiceId",
                schema: "sales",
                table: "CreditNotes",
                column: "SalesInvoiceId",
                principalSchema: "sales",
                principalTable: "SalesInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_SalesOrders_SalesOrderId",
                schema: "sales",
                table: "Deliveries",
                column: "SalesOrderId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryLines_SalesOrderLines_SalesOrderLineId",
                schema: "sales",
                table: "DeliveryLines",
                column: "SalesOrderLineId",
                principalSchema: "sales",
                principalTable: "SalesOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_SalesInvoices_SalesInvoiceId",
                schema: "sales",
                table: "PaymentAllocations",
                column: "SalesInvoiceId",
                principalSchema: "sales",
                principalTable: "SalesInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAllocations_SalesPayments_SalesPaymentId",
                schema: "sales",
                table: "PaymentAllocations",
                column: "SalesPaymentId",
                principalSchema: "sales",
                principalTable: "SalesPayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PosCashDrawerEvents_PosTransactions_PosTransactionId",
                schema: "sales",
                table: "PosCashDrawerEvents",
                column: "PosTransactionId",
                principalSchema: "sales",
                principalTable: "PosTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PosGiftCards_PosTransactions_IssuedInTransactionId",
                schema: "sales",
                table: "PosGiftCards",
                column: "IssuedInTransactionId",
                principalSchema: "sales",
                principalTable: "PosTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PosGiftCardTransactions_PosTransactions_PosTransactionId",
                schema: "sales",
                table: "PosGiftCardTransactions",
                column: "PosTransactionId",
                principalSchema: "sales",
                principalTable: "PosTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PosPayments_PosTransactions_PosTransactionId",
                schema: "sales",
                table: "PosPayments",
                column: "PosTransactionId",
                principalSchema: "sales",
                principalTable: "PosTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PosTransactionLines_PosTransactions_PosTransactionId",
                schema: "sales",
                table: "PosTransactionLines",
                column: "PosTransactionId",
                principalSchema: "sales",
                principalTable: "PosTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PosTransactions_SalesOrders_SalesOrderId",
                schema: "sales",
                table: "PosTransactions",
                column: "SalesOrderId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PosTransactions_SalesOrders_SalesOrderId",
                schema: "sales",
                table: "PosTransactions");

            migrationBuilder.DropTable(
                name: "AppNotifications",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "ApprovalPolicyConditions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "ApprovalRequests",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "CommissionEntries",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "CouponUsages",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "CreditNoteLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "CurrencyRates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "CustomerGroups",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "DeliveryLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "DeliveryPackages",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "DeliveryZonePosStore",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "DiscountSchemes",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "DocumentSequences",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "LoyaltyPrograms",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PaymentAllocations",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosBarcodeLabelTemplates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosCashDrawerEvents",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosCashMovements",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosGiftCardTransactions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosPayments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosSettings",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosStoreHolidays",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosStoreSchedules",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosTransactionLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PriceListItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "ProductReviewImages",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PromotionItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "QuotationApprovals",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "QuotationLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "RiderLocationLogs",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "RiderRatings",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "RiderShifts",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesAgreementLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesInvoiceLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesOrderApprovals",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesOrderAttachments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesOrderLineAddons",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesOrderStatusHistories",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesRepTerritories",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesReturnLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesTargets",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesTeamMembers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "StoredFiles",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "StoreMenuSections",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "StoreOffers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "StoreVendorProfiles",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "TaxGroupRates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "TaxRules",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "WishlistItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "ApprovalPolicySteps",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "CommissionRules",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Coupons",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "LoyaltyAccounts",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesPayments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosGiftCards",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "ProductReviews",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "RiderAssignments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesOrderLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesReturns",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesTeams",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "StoreMenus",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Promotions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "TaxGroups",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "ApprovalPolicies",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "CreditNotes",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesInvoices",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Deliveries",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Riders",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "DeliveryZones",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesOrders",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosTransactions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Quotations",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesAgreements",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesTerritories",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosSessions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosCashiers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosTerminals",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosCashDrawers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosStores",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PosReceiptTemplates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PriceLists",
                schema: "sales");
        }
    }
}
