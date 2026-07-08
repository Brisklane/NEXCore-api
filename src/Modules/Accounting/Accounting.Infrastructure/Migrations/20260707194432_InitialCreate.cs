using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accounting");

            migrationBuilder.CreateTable(
                name: "AccountCategories",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    NormalBalance = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AccountCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dimensions",
                schema: "accounting",
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
                    table.PrimaryKey("PK_Dimensions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DimensionSets",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("PK_DimensionSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalCalendars",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_FiscalCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ledgers",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BaseCurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    FiscalCalendarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_Ledgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DimensionValues",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DimensionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValueName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("PK_DimensionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DimensionValues_Dimensions_DimensionId",
                        column: x => x.DimensionId,
                        principalSchema: "accounting",
                        principalTable: "Dimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiscalPeriods",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalCalendarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_FiscalPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalPeriods_FiscalCalendars_FiscalCalendarId",
                        column: x => x.FiscalCalendarId,
                        principalSchema: "accounting",
                        principalTable: "FiscalCalendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PostingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalDebit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReversalOfJournalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Ledgers_LedgerId",
                        column: x => x.LedgerId,
                        principalSchema: "accounting",
                        principalTable: "Ledgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LedgerAccounts",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPostingAllowed = table.Column<bool>(type: "bit", nullable: false),
                    IsControlAccount = table.Column<bool>(type: "bit", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    IsSubledgerAccount = table.Column<bool>(type: "bit", nullable: false),
                    SubledgerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubledgerMasterAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_LedgerAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerAccounts_AccountCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "accounting",
                        principalTable: "AccountCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LedgerAccounts_LedgerAccounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LedgerAccounts_LedgerAccounts_SubledgerMasterAccountId",
                        column: x => x.SubledgerMasterAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LedgerAccounts_Ledgers_LedgerId",
                        column: x => x.LedgerId,
                        principalSchema: "accounting",
                        principalTable: "Ledgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DimensionSetItems",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DimensionSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DimensionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DimensionValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_DimensionSetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DimensionSetItems_DimensionSets_DimensionSetId",
                        column: x => x.DimensionSetId,
                        principalSchema: "accounting",
                        principalTable: "DimensionSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DimensionSetItems_DimensionValues_DimensionValueId",
                        column: x => x.DimensionValueId,
                        principalSchema: "accounting",
                        principalTable: "DimensionValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DimensionSetItems_Dimensions_DimensionId",
                        column: x => x.DimensionId,
                        principalSchema: "accounting",
                        principalTable: "Dimensions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JournalAudits",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_JournalAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalAudits_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryApprovals",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovingUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovingUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalDecision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalSequence = table.Column<int>(type: "int", nullable: false),
                    RequiredApprovalRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsFinalApproval = table.Column<bool>(type: "bit", nullable: false),
                    RelatedApprovalReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_JournalEntryApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryApprovals_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountBalances",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DebitTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Actual"),
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
                    table.PrimaryKey("PK_AccountBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountBalances_LedgerAccounts_LedgerAccountId",
                        column: x => x.LedgerAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Budgets",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BudgetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Annual"),
                    VarianceThresholdPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Budgets_FiscalPeriods_FiscalPeriodId",
                        column: x => x.FiscalPeriodId,
                        principalSchema: "accounting",
                        principalTable: "FiscalPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Budgets_LedgerAccounts_LedgerAccountId",
                        column: x => x.LedgerAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JournalLines",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BaseDebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseCreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DimensionSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceModuleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentTermsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("PK_JournalLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "accounting",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JournalLines_LedgerAccounts_LedgerAccountId",
                        column: x => x.LedgerAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PostingProfiles",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DebitAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PostingProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingProfiles_LedgerAccounts_CreditAccountId",
                        column: x => x.CreditAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PostingProfiles_LedgerAccounts_DebitAccountId",
                        column: x => x.DebitAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PostingProfiles_LedgerAccounts_TaxAccountId",
                        column: x => x.TaxAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxCodes",
                schema: "accounting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsRecoverable = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TaxCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxCodes_LedgerAccounts_LedgerAccountId",
                        column: x => x.LedgerAccountId,
                        principalSchema: "accounting",
                        principalTable: "LedgerAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalance_Account_Period_Type",
                schema: "accounting",
                table: "AccountBalances",
                columns: new[] { "CompanyId", "LedgerAccountId", "FiscalPeriodId", "BalanceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalances_LedgerAccountId",
                schema: "accounting",
                table: "AccountBalances",
                column: "LedgerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Budget_Account_Period_Type",
                schema: "accounting",
                table: "Budgets",
                columns: new[] { "LedgerAccountId", "FiscalPeriodId", "BudgetType" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_FiscalPeriodId",
                schema: "accounting",
                table: "Budgets",
                column: "FiscalPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Dimension_Company_Code",
                schema: "accounting",
                table: "Dimensions",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimensionSetItems_DimensionId",
                schema: "accounting",
                table: "DimensionSetItems",
                column: "DimensionId");

            migrationBuilder.CreateIndex(
                name: "IX_DimensionSetItems_DimensionSetId",
                schema: "accounting",
                table: "DimensionSetItems",
                column: "DimensionSetId");

            migrationBuilder.CreateIndex(
                name: "IX_DimensionSetItems_DimensionValueId",
                schema: "accounting",
                table: "DimensionSetItems",
                column: "DimensionValueId");

            migrationBuilder.CreateIndex(
                name: "IX_DimensionSet_Company_Hash",
                schema: "accounting",
                table: "DimensionSets",
                columns: new[] { "CompanyId", "HashCode" });

            migrationBuilder.CreateIndex(
                name: "IX_DimensionValue_Dimension_ValueCode",
                schema: "accounting",
                table: "DimensionValues",
                columns: new[] { "DimensionId", "ValueCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriod_Calendar_Dates",
                schema: "accounting",
                table: "FiscalPeriods",
                columns: new[] { "FiscalCalendarId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalAudits_JournalEntryId",
                schema: "accounting",
                table: "JournalAudits",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_Company_JournalNumber",
                schema: "accounting",
                table: "JournalEntries",
                columns: new[] { "CompanyId", "JournalNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_Company_Status",
                schema: "accounting",
                table: "JournalEntries",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_Ledger_PostingDate",
                schema: "accounting",
                table: "JournalEntries",
                columns: new[] { "LedgerId", "PostingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalApproval_Entry_Sequence",
                schema: "accounting",
                table: "JournalEntryApprovals",
                columns: new[] { "JournalEntryId", "ApprovalSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_Customer",
                schema: "accounting",
                table: "JournalLines",
                column: "CustomerId",
                filter: "[CustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_LedgerAccount",
                schema: "accounting",
                table: "JournalLines",
                column: "LedgerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_Source",
                schema: "accounting",
                table: "JournalLines",
                columns: new[] { "SourceEntityType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLine_Vendor",
                schema: "accounting",
                table: "JournalLines",
                column: "VendorId",
                filter: "[VendorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_JournalEntryId",
                schema: "accounting",
                table: "JournalLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccount_Company_AccountNumber",
                schema: "accounting",
                table: "LedgerAccounts",
                columns: new[] { "CompanyId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_CategoryId",
                schema: "accounting",
                table: "LedgerAccounts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_LedgerId",
                schema: "accounting",
                table: "LedgerAccounts",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_ParentAccountId",
                schema: "accounting",
                table: "LedgerAccounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_SubledgerMasterAccountId",
                schema: "accounting",
                table: "LedgerAccounts",
                column: "SubledgerMasterAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingProfiles_CreditAccountId",
                schema: "accounting",
                table: "PostingProfiles",
                column: "CreditAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingProfiles_DebitAccountId",
                schema: "accounting",
                table: "PostingProfiles",
                column: "DebitAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingProfiles_TaxAccountId",
                schema: "accounting",
                table: "PostingProfiles",
                column: "TaxAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCode_Company_Code",
                schema: "accounting",
                table: "TaxCodes",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_LedgerAccountId",
                schema: "accounting",
                table: "TaxCodes",
                column: "LedgerAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountBalances",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "Budgets",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "DimensionSetItems",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "JournalAudits",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "JournalEntryApprovals",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "JournalLines",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "PostingProfiles",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "TaxCodes",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "FiscalPeriods",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "DimensionSets",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "DimensionValues",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "JournalEntries",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "LedgerAccounts",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "FiscalCalendars",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "Dimensions",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "AccountCategories",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "Ledgers",
                schema: "accounting");
        }
    }
}
