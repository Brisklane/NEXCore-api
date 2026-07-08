using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                schema: "core",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NativeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsRtl = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PricePerMonth = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerYear = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxCompanies = table.Column<int>(type: "int", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    MaxBranches = table.Column<int>(type: "int", nullable: false),
                    AllowedModules = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrialDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                schema: "core",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Code3 = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    NumericCode = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OfficialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubRegion = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: true),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    DefaultCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PhoneCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FlagEmoji = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PostalCodePattern = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "DEFAULT"),
                    StateLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "State / Province"),
                    PostalCodeLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Countries_Currencies_DefaultCurrencyId",
                        column: x => x.DefaultCurrencyId,
                        principalSchema: "core",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyTranslations",
                schema: "core",
                columns: table => new
                {
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyTranslations", x => new { x.CurrencyId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_CurrencyTranslations_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "core",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrencyTranslations_Languages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalSchema: "core",
                        principalTable: "Languages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseCurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Company_StreetAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Company_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Company_State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Company_PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Company_Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RadiusInMeters = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    CompanyLogo = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "core",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscriptions",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BillingCycle = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LicenseKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalSchema: "core",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "core",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CountryTranslations",
                schema: "core",
                columns: table => new
                {
                    CountryCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OfficialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryTranslations", x => new { x.CountryCode, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_CountryTranslations_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "core",
                        principalTable: "Countries",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CountryTranslations_Languages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalSchema: "core",
                        principalTable: "Languages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subdivisions",
                schema: "core",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CountryCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubdivisionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "State"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subdivisions", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Subdivisions_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "core",
                        principalTable: "Countries",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Branch_StreetAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Branch_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Branch_State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Branch_PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Branch_Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BranchType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BranchLogo = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "core",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CityCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: true),
                    CountryCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    SubdivisionCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Population = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "core",
                        principalTable: "Countries",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cities_Subdivisions_SubdivisionCode",
                        column: x => x.SubdivisionCode,
                        principalSchema: "core",
                        principalTable: "Subdivisions",
                        principalColumn: "Code");
                });

            migrationBuilder.CreateTable(
                name: "SubdivisionTranslations",
                schema: "core",
                columns: table => new
                {
                    SubdivisionCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubdivisionTranslations", x => new { x.SubdivisionCode, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_SubdivisionTranslations_Languages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalSchema: "core",
                        principalTable: "Languages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubdivisionTranslations_Subdivisions_SubdivisionCode",
                        column: x => x.SubdivisionCode,
                        principalSchema: "core",
                        principalTable: "Subdivisions",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUnits",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ManagerEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CostCenterCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProfitCenterCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessUnits_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "core",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessUnits_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "core",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CityTranslations",
                schema: "core",
                columns: table => new
                {
                    CityId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityTranslations", x => new { x.CityId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_CityTranslations_Cities_CityId",
                        column: x => x.CityId,
                        principalSchema: "core",
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CityTranslations_Languages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalSchema: "core",
                        principalTable: "Languages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "SubscriptionPlans",
                columns: new[] { "Id", "AllowedModules", "Code", "CreatedAt", "CreatedByUserId", "Description", "IsActive", "MaxBranches", "MaxCompanies", "MaxUsers", "Name", "PricePerMonth", "PricePerYear", "TrialDays", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Core,HR,Accounting,Inventory,Sales", "TRIAL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), "30-day free trial with full access", true, 1, 1, 5, "Trial", 0m, 0m, 30, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Core,HR,Accounting", "STARTER", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), "For small businesses getting started", true, 2, 1, 10, "Starter", 49m, 490m, 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "Core,HR,Accounting,Inventory,Sales", "PROFESSIONAL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), "For growing businesses needing full ERP capabilities", true, 10, 5, 50, "Professional", 149m, 1490m, 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000004"), null, "ENTERPRISE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), "Unlimited scale for large organizations", true, -1, -1, -1, "Enterprise", 499m, 4990m, 0, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId_Code",
                schema: "core",
                table: "Branches",
                columns: new[] { "CompanyId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId_Name",
                schema: "core",
                table: "Branches",
                columns: new[] { "CompanyId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_BranchId_Name",
                schema: "core",
                table: "BusinessUnits",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnits_CompanyId_Code",
                schema: "core",
                table: "BusinessUnits",
                columns: new[] { "CompanyId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CountryCode",
                schema: "core",
                table: "Cities",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CountryCode_Name",
                schema: "core",
                table: "Cities",
                columns: new[] { "CountryCode", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_SubdivisionCode",
                schema: "core",
                table: "Cities",
                column: "SubdivisionCode");

            migrationBuilder.CreateIndex(
                name: "IX_CityTranslations_LanguageCode",
                schema: "core",
                table: "CityTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                schema: "core",
                table: "Companies",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Slug",
                schema: "core",
                table: "Companies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId",
                schema: "core",
                table: "Companies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId_CompanyName",
                schema: "core",
                table: "Companies",
                columns: new[] { "TenantId", "CompanyName" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_DefaultCurrencyId",
                schema: "core",
                table: "Countries",
                column: "DefaultCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CountryTranslations_LanguageCode",
                schema: "core",
                table: "CountryTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                schema: "core",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyTranslations_LanguageCode",
                schema: "core",
                table: "CurrencyTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_Subdivisions_CountryCode",
                schema: "core",
                table: "Subdivisions",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_SubdivisionTranslations_LanguageCode",
                schema: "core",
                table: "SubdivisionTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                schema: "core",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                schema: "core",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_LicenseKey",
                schema: "core",
                table: "TenantSubscriptions",
                column: "LicenseKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_SubscriptionPlanId",
                schema: "core",
                table: "TenantSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId_Status",
                schema: "core",
                table: "TenantSubscriptions",
                columns: new[] { "TenantId", "Status" });

            // ── Seed ISO geo reference data (currencies, languages, countries, subdivisions,
            //    cities) from the embedded, idempotent SeedGeoReferenceData.sql. Carried over
            //    from the former standalone SeedGeoReferenceData migration during the squash. ──
            var geoAssembly = typeof(InitialCreate).Assembly;
            var geoResource = geoAssembly.GetManifestResourceNames()
                .Single(n => n.EndsWith("SeedGeoReferenceData.sql", StringComparison.Ordinal));
            using (var geoStream = geoAssembly.GetManifestResourceStream(geoResource))
            using (var geoReader = new StreamReader(geoStream))
            {
                migrationBuilder.Sql(geoReader.ReadToEnd());
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessUnits",
                schema: "core");

            migrationBuilder.DropTable(
                name: "CityTranslations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "CountryTranslations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "CurrencyTranslations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "SubdivisionTranslations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "TenantSubscriptions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Branches",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Cities",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Languages",
                schema: "core");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Subdivisions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "core");
        }
    }
}
