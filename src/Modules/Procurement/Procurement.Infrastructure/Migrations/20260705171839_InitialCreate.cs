using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "procurement");

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflows",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaximumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ApprovalWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                schema: "procurement",
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
                    SequencePadding = table.Column<int>(type: "int", nullable: false),
                    ResetOn = table.Column<int>(type: "int", nullable: false),
                    NextSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    LastResetYear = table.Column<int>(type: "int", nullable: true),
                    LastResetMonth = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_DocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcurementCategories",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultLedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultTaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequireRequisition = table.Column<bool>(type: "bit", nullable: false),
                    RFQThresholdAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    table.PrimaryKey("PK_ProcurementCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcurementCategories_ProcurementCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "procurement",
                        principalTable: "ProcurementCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorCategories",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_VendorCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorCategories_VendorCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "procurement",
                        principalTable: "VendorCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowSteps",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApproverRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AmountThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsParallelStep = table.Column<bool>(type: "bit", nullable: false),
                    RequiredApprovals = table.Column<int>(type: "int", nullable: false),
                    EscalationAfterDays = table.Column<int>(type: "int", nullable: false),
                    EscalationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ApprovalWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowSteps_ApprovalWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalSchema: "procurement",
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProcurementSettings",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequireRequisitionForPO = table.Column<bool>(type: "bit", nullable: false),
                    RFQMandatoryAboveAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Enable3WayMatching = table.Column<bool>(type: "bit", nullable: false),
                    EnforceInvoicePOTolerance = table.Column<bool>(type: "bit", nullable: false),
                    InvoicePOTolerancePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DefaultPaymentTerms = table.Column<int>(type: "int", nullable: false),
                    DefaultCurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    DefaultLeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    POApprovalWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequisitionApprovalWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceApprovalWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequireVendorApproval = table.Column<bool>(type: "bit", nullable: false),
                    RequireVendorBankVerification = table.Column<bool>(type: "bit", nullable: false),
                    SendPOToVendorByEmail = table.Column<bool>(type: "bit", nullable: false),
                    SendRFQToVendorByEmail = table.Column<bool>(type: "bit", nullable: false),
                    POApprovalReminderDays = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ProcurementSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcurementSettings_ApprovalWorkflows_InvoiceApprovalWorkflowId",
                        column: x => x.InvoiceApprovalWorkflowId,
                        principalSchema: "procurement",
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProcurementSettings_ApprovalWorkflows_POApprovalWorkflowId",
                        column: x => x.POApprovalWorkflowId,
                        principalSchema: "procurement",
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProcurementSettings_ApprovalWorkflows_RequisitionApprovalWorkflowId",
                        column: x => x.RequisitionApprovalWorkflowId,
                        principalSchema: "procurement",
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OnboardingStatus = table.Column<int>(type: "int", nullable: false),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VATNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VendorCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrimaryEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PrimaryPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrimaryMobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsPreferredVendor = table.Column<bool>(type: "bit", nullable: false),
                    ApLedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubledgerType = table.Column<int>(type: "int", nullable: false),
                    OverallRating = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    OnTimeDeliveryRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    QualityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlockedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorCategories_VendorCategoryId",
                        column: x => x.VendorCategoryId,
                        principalSchema: "procurement",
                        principalTable: "VendorCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovedVendorLists",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcurementCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false),
                    IsExclusive = table.Column<bool>(type: "bit", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    MinimumOrderQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequiresQualityInspection = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ApprovedVendorLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedVendorLists_ProcurementCategories_ProcurementCategoryId",
                        column: x => x.ProcurementCategoryId,
                        principalSchema: "procurement",
                        principalTable: "ProcurementCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovedVendorLists_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LandedCosts",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandedCostNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLandedCostAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AccountingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_LandedCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandedCosts_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseContracts",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContractType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false),
                    RenewalNoticeDays = table.Column<int>(type: "int", nullable: false),
                    RenewalDurationMonths = table.Column<int>(type: "int", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    MaximumContractValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CommittedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UsedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    Incoterm = table.Column<int>(type: "int", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TermsAndConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TerminationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseContracts_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitions",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiredByDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SuggestedVendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    EstimatedTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsBudgetChecked = table.Column<bool>(type: "bit", nullable: false),
                    IsBudgetAvailable = table.Column<bool>(type: "bit", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitions_Vendors_SuggestedVendorId",
                        column: x => x.SuggestedVendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorAddresses",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressType = table.Column<int>(type: "int", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Street2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_VendorAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorAddresses_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorBankAccounts",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AccountHolderName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IBAN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SWIFTCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RoutingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_VendorBankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBankAccounts_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorContacts",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_VendorContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorContacts_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorDocuments",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NeverExpires = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReminderDays = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerificationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_VendorDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorDocuments_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorPerformances",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnTimeDeliveryRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    QualityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PriceComplianceRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ResponsivenessScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DocumentAccuracyScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OverallRating = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    LateDeliveries = table.Column<int>(type: "int", nullable: false),
                    QualityRejections = table.Column<int>(type: "int", nullable: false),
                    InvoiceDiscrepancies = table.Column<int>(type: "int", nullable: false),
                    TotalPurchaseValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EvaluatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_VendorPerformances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPerformances_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorPricelists",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_VendorPricelists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPricelists_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LandedCostLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandedCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    CostType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllocationMethod = table.Column<int>(type: "int", nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_LandedCostLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandedCostLines_LandedCosts_LandedCostId",
                        column: x => x.LandedCostId,
                        principalSchema: "procurement",
                        principalTable: "LandedCosts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseContractLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProcurementCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MinimumQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaximumQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CommittedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseContractLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseContractLines_ProcurementCategories_ProcurementCategoryId",
                        column: x => x.ProcurementCategoryId,
                        principalSchema: "procurement",
                        principalTable: "ProcurementCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseContractLines_PurchaseContracts_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProcurementApprovals",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DelegatedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DelegatedToName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DelegationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ProcurementApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcurementApprovals_ApprovalWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalSchema: "procurement",
                        principalTable: "ApprovalWorkflowSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProcurementApprovals_ApprovalWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalSchema: "procurement",
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProcurementApprovals_PurchaseRequisitions_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitionLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstimatedUnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EstimatedTotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProcurementCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequiredByDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineStatus = table.Column<int>(type: "int", nullable: false),
                    QuantityOrdered = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SuggestedVendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseRequisitionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionLines_ProcurementCategories_ProcurementCategoryId",
                        column: x => x.ProcurementCategoryId,
                        principalSchema: "procurement",
                        principalTable: "ProcurementCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionLines_PurchaseRequisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionLines_Vendors_SuggestedVendorId",
                        column: x => x.SuggestedVendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequestForQuotations",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RFQNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmissionDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotationValidityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    DeliveryAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequiredDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TermsAndConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluationCriteria = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_RequestForQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestForQuotations_PurchaseRequisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorPayments",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClearedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    CompanyBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnallocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BankReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AccountingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WithholdingTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WithholdingTaxLedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_VendorPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPayments_VendorBankAccounts_VendorBankAccountId",
                        column: x => x.VendorBankAccountId,
                        principalSchema: "procurement",
                        principalTable: "VendorBankAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorPayments_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorPricelistItems",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PricelistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinimumQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_VendorPricelistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPricelistItems_VendorPricelists_PricelistId",
                        column: x => x.PricelistId,
                        principalSchema: "procurement",
                        principalTable: "VendorPricelists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RFQLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RFQId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    RequisitionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstimatedUnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ProcurementCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequiredDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_RFQLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RFQLines_ProcurementCategories_ProcurementCategoryId",
                        column: x => x.ProcurementCategoryId,
                        principalSchema: "procurement",
                        principalTable: "ProcurementCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RFQLines_PurchaseRequisitionLines_RequisitionLineId",
                        column: x => x.RequisitionLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseRequisitionLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RFQLines_RequestForQuotations_RFQId",
                        column: x => x.RFQId,
                        principalSchema: "procurement",
                        principalTable: "RequestForQuotations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RFQVendors",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RFQId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeclineReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_RFQVendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RFQVendors_RequestForQuotations_RFQId",
                        column: x => x.RFQId,
                        principalSchema: "procurement",
                        principalTable: "RequestForQuotations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RFQVendors_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorQuotations",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorQuotationReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RFQId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    Incoterm = table.Column<int>(type: "int", nullable: true),
                    IncotermLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryLeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    TechnicalScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CommercialScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsRecommended = table.Column<bool>(type: "bit", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_VendorQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorQuotations_RequestForQuotations_RFQId",
                        column: x => x.RFQId,
                        principalSchema: "procurement",
                        principalTable: "RequestForQuotations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorQuotations_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    VendorReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentToVendorAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeliveryAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryStreet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeliveryState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeliveryPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DeliveryCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    Incoterm = table.Column<int>(type: "int", nullable: true),
                    IncotermLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoicedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountingCommitmentEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TermsAndConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_PurchaseContracts_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_PurchaseRequisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_VendorQuotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "procurement",
                        principalTable: "VendorQuotations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorQuotationLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RFQLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PromisedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    IsAlternative = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_VendorQuotationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorQuotationLines_RFQLines_RFQLineId",
                        column: x => x.RFQLineId,
                        principalSchema: "procurement",
                        principalTable: "RFQLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorQuotationLines_VendorQuotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "procurement",
                        principalTable: "VendorQuotations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorDeliveryNoteNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceiptType = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_GoodsReceipts_OriginalReceiptId",
                        column: x => x.OriginalReceiptId,
                        principalSchema: "procurement",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoices",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VendorInvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MatchingStatus = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecoverableTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NonRecoverableTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentTerms = table.Column<int>(type: "int", nullable: false),
                    AccountingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DimensionSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HoldReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisputeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderAmendments",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmendmentNumber = table.Column<int>(type: "int", nullable: false),
                    AmendmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreviousValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresReApproval = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseOrderAmendments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderAmendments_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderApprovals",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DelegatedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PurchaseOrderApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderApprovals_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    RequisitionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContractLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProcurementCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DimensionSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityAccepted = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReturned = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityInvoiced = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineStatus = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_ProcurementCategories_ProcurementCategoryId",
                        column: x => x.ProcurementCategoryId,
                        principalSchema: "procurement",
                        principalTable: "ProcurementCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseContractLines_ContractLineId",
                        column: x => x.ContractLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseContractLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseRequisitionLines_RequisitionLineId",
                        column: x => x.RequisitionLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseRequisitionLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LandedCostGoodsReceipts",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandedCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_LandedCostGoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandedCostGoodsReceipts_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "procurement",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LandedCostGoodsReceipts_LandedCosts_LandedCostId",
                        column: x => x.LandedCostId,
                        principalSchema: "procurement",
                        principalTable: "LandedCosts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturns",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReturnReason = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalReturnAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccountingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FiscalPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VendorReturnAuthorisationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "procurement",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorPaymentLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceOutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountTaken = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WriteOffAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FXGainLossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FXGainLossLedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_VendorPaymentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorPaymentLines_PurchaseInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorPaymentLines_VendorPayments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "procurement",
                        principalTable: "VendorPayments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuantityOrdered = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityAccepted = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManufacturerBatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StorageLocationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    QualityStatus = table.Column<int>(type: "int", nullable: false),
                    QualityNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InspectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_GoodsReceiptLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_GoodsReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "procurement",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorDebitNotes",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebitNoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DebitNoteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SettledAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccountingJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_VendorDebitNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorDebitNotes_PurchaseInvoices_OriginalInvoiceId",
                        column: x => x.OriginalInvoiceId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorDebitNotes_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorDebitNotes_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "procurement",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LandedCostAllocations",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandedCostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandedCostLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocationMethod = table.Column<int>(type: "int", nullable: false),
                    AllocationBasisValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalBasisValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllocationPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAmountPerUnit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_LandedCostAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandedCostAllocations_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalSchema: "procurement",
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LandedCostAllocations_LandedCostLines_LandedCostLineId",
                        column: x => x.LandedCostLineId,
                        principalSchema: "procurement",
                        principalTable: "LandedCostLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LandedCostAllocations_LandedCosts_LandedCostId",
                        column: x => x.LandedCostId,
                        principalSchema: "procurement",
                        principalTable: "LandedCosts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoiceLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecoverableTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NonRecoverableTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProcurementCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DimensionSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalSchema: "procurement",
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_ProcurementCategories_ProcurementCategoryId",
                        column: x => x.ProcurementCategoryId,
                        principalSchema: "procurement",
                        principalTable: "ProcurementCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_PurchaseInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturnLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReturned = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitOfMeasureName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalReturnAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnReason = table.Column<int>(type: "int", nullable: false),
                    QualityIssueDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnStorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalSchema: "procurement",
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ThreeWayMatchRecords",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchStatus = table.Column<int>(type: "int", nullable: false),
                    HasDiscrepancy = table.Column<bool>(type: "bit", nullable: false),
                    DiscrepancyType = table.Column<int>(type: "int", nullable: true),
                    POQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityVariance = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    POUnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoiceUnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PriceVariance = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalVarianceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MatchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MatchedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ThreeWayMatchRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreeWayMatchRecords_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalSchema: "procurement",
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThreeWayMatchRecords_PurchaseInvoiceLines_InvoiceLineId",
                        column: x => x.InvoiceLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseInvoiceLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThreeWayMatchRecords_PurchaseInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseInvoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThreeWayMatchRecords_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorDebitNoteLines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebitNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ReturnLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_VendorDebitNoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorDebitNoteLines_PurchaseReturnLines_ReturnLineId",
                        column: x => x.ReturnLineId,
                        principalSchema: "procurement",
                        principalTable: "PurchaseReturnLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorDebitNoteLines_VendorDebitNotes_DebitNoteId",
                        column: x => x.DebitNoteId,
                        principalSchema: "procurement",
                        principalTable: "VendorDebitNotes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowSteps_WorkflowId",
                schema: "procurement",
                table: "ApprovalWorkflowSteps",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorLists_ProcurementCategoryId",
                schema: "procurement",
                table: "ApprovedVendorLists",
                column: "ProcurementCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedVendorLists_VendorId",
                schema: "procurement",
                table: "ApprovedVendorLists",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSequence_Company_Type",
                schema: "procurement",
                table: "DocumentSequences",
                columns: new[] { "CompanyId", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_PurchaseOrderLineId",
                schema: "procurement",
                table: "GoodsReceiptLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_ReceiptId",
                schema: "procurement",
                table: "GoodsReceiptLines",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_Company_Number",
                schema: "procurement",
                table: "GoodsReceipts",
                columns: new[] { "CompanyId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_OriginalReceiptId",
                schema: "procurement",
                table: "GoodsReceipts",
                column: "OriginalReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                schema: "procurement",
                table: "GoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_VendorId",
                schema: "procurement",
                table: "GoodsReceipts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocations_GoodsReceiptLineId",
                schema: "procurement",
                table: "LandedCostAllocations",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocations_LandedCostId",
                schema: "procurement",
                table: "LandedCostAllocations",
                column: "LandedCostId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocations_LandedCostLineId",
                schema: "procurement",
                table: "LandedCostAllocations",
                column: "LandedCostLineId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostGoodsReceipts_GoodsReceiptId",
                schema: "procurement",
                table: "LandedCostGoodsReceipts",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostGoodsReceipts_LandedCostId",
                schema: "procurement",
                table: "LandedCostGoodsReceipts",
                column: "LandedCostId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostLines_LandedCostId",
                schema: "procurement",
                table: "LandedCostLines",
                column: "LandedCostId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCost_Company_Number",
                schema: "procurement",
                table: "LandedCosts",
                columns: new[] { "CompanyId", "LandedCostNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LandedCosts_VendorId",
                schema: "procurement",
                table: "LandedCosts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementApproval_Document",
                schema: "procurement",
                table: "ProcurementApprovals",
                columns: new[] { "DocumentType", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementApprovals_DocumentId",
                schema: "procurement",
                table: "ProcurementApprovals",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementApprovals_WorkflowId",
                schema: "procurement",
                table: "ProcurementApprovals",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementApprovals_WorkflowStepId",
                schema: "procurement",
                table: "ProcurementApprovals",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementCategories_ParentCategoryId",
                schema: "procurement",
                table: "ProcurementCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementCategory_Company_Code",
                schema: "procurement",
                table: "ProcurementCategories",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementSettings_InvoiceApprovalWorkflowId",
                schema: "procurement",
                table: "ProcurementSettings",
                column: "InvoiceApprovalWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementSettings_POApprovalWorkflowId",
                schema: "procurement",
                table: "ProcurementSettings",
                column: "POApprovalWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementSettings_RequisitionApprovalWorkflowId",
                schema: "procurement",
                table: "ProcurementSettings",
                column: "RequisitionApprovalWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContractLines_ContractId",
                schema: "procurement",
                table: "PurchaseContractLines",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContractLines_ProcurementCategoryId",
                schema: "procurement",
                table: "PurchaseContractLines",
                column: "ProcurementCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContract_Company_Number",
                schema: "procurement",
                table: "PurchaseContracts",
                columns: new[] { "CompanyId", "ContractNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseContracts_VendorId",
                schema: "procurement",
                table: "PurchaseContracts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_GoodsReceiptLineId",
                schema: "procurement",
                table: "PurchaseInvoiceLines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_InvoiceId",
                schema: "procurement",
                table: "PurchaseInvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_ProcurementCategoryId",
                schema: "procurement",
                table: "PurchaseInvoiceLines",
                column: "ProcurementCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_PurchaseOrderLineId",
                schema: "procurement",
                table: "PurchaseInvoiceLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoice_Company_Number",
                schema: "procurement",
                table: "PurchaseInvoices",
                columns: new[] { "CompanyId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoice_Vendor_VendorNumber",
                schema: "procurement",
                table: "PurchaseInvoices",
                columns: new[] { "CompanyId", "VendorId", "VendorInvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_PurchaseOrderId",
                schema: "procurement",
                table: "PurchaseInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_VendorId",
                schema: "procurement",
                table: "PurchaseInvoices",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderAmendments_OrderId",
                schema: "procurement",
                table: "PurchaseOrderAmendments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderApprovals_OrderId",
                schema: "procurement",
                table: "PurchaseOrderApprovals",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_ContractLineId",
                schema: "procurement",
                table: "PurchaseOrderLines",
                column: "ContractLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_OrderId",
                schema: "procurement",
                table: "PurchaseOrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_ProcurementCategoryId",
                schema: "procurement",
                table: "PurchaseOrderLines",
                column: "ProcurementCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_RequisitionLineId",
                schema: "procurement",
                table: "PurchaseOrderLines",
                column: "RequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_Company_Number",
                schema: "procurement",
                table: "PurchaseOrders",
                columns: new[] { "CompanyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_Company_Status",
                schema: "procurement",
                table: "PurchaseOrders",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ContractId",
                schema: "procurement",
                table: "PurchaseOrders",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_QuotationId",
                schema: "procurement",
                table: "PurchaseOrders",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_RequisitionId",
                schema: "procurement",
                table: "PurchaseOrders",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_VendorId",
                schema: "procurement",
                table: "PurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionLines_ProcurementCategoryId",
                schema: "procurement",
                table: "PurchaseRequisitionLines",
                column: "ProcurementCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionLines_RequisitionId",
                schema: "procurement",
                table: "PurchaseRequisitionLines",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionLines_SuggestedVendorId",
                schema: "procurement",
                table: "PurchaseRequisitionLines",
                column: "SuggestedVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisition_Company_Number",
                schema: "procurement",
                table: "PurchaseRequisitions",
                columns: new[] { "CompanyId", "RequisitionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisition_Company_Status",
                schema: "procurement",
                table: "PurchaseRequisitions",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_SuggestedVendorId",
                schema: "procurement",
                table: "PurchaseRequisitions",
                column: "SuggestedVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_GoodsReceiptLineId",
                schema: "procurement",
                table: "PurchaseReturnLines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_PurchaseOrderLineId",
                schema: "procurement",
                table: "PurchaseReturnLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_PurchaseReturnId",
                schema: "procurement",
                table: "PurchaseReturnLines",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_Company_Number",
                schema: "procurement",
                table: "PurchaseReturns",
                columns: new[] { "CompanyId", "ReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_GoodsReceiptId",
                schema: "procurement",
                table: "PurchaseReturns",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_PurchaseOrderId",
                schema: "procurement",
                table: "PurchaseReturns",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_VendorId",
                schema: "procurement",
                table: "PurchaseReturns",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotation_Company_Number",
                schema: "procurement",
                table: "RequestForQuotations",
                columns: new[] { "CompanyId", "RFQNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotations_RequisitionId",
                schema: "procurement",
                table: "RequestForQuotations",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RFQLines_ProcurementCategoryId",
                schema: "procurement",
                table: "RFQLines",
                column: "ProcurementCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RFQLines_RequisitionLineId",
                schema: "procurement",
                table: "RFQLines",
                column: "RequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_RFQLines_RFQId",
                schema: "procurement",
                table: "RFQLines",
                column: "RFQId");

            migrationBuilder.CreateIndex(
                name: "IX_RFQVendor_RFQ_Vendor",
                schema: "procurement",
                table: "RFQVendors",
                columns: new[] { "RFQId", "VendorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RFQVendors_VendorId",
                schema: "procurement",
                table: "RFQVendors",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreeWayMatchRecords_GoodsReceiptLineId",
                schema: "procurement",
                table: "ThreeWayMatchRecords",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreeWayMatchRecords_InvoiceId",
                schema: "procurement",
                table: "ThreeWayMatchRecords",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreeWayMatchRecords_InvoiceLineId",
                schema: "procurement",
                table: "ThreeWayMatchRecords",
                column: "InvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreeWayMatchRecords_PurchaseOrderLineId",
                schema: "procurement",
                table: "ThreeWayMatchRecords",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorAddresses_VendorId",
                schema: "procurement",
                table: "VendorAddresses",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBankAccounts_VendorId",
                schema: "procurement",
                table: "VendorBankAccounts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCategories_ParentCategoryId",
                schema: "procurement",
                table: "VendorCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCategory_Company_Code",
                schema: "procurement",
                table: "VendorCategories",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorContacts_VendorId",
                schema: "procurement",
                table: "VendorContacts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebitNoteLines_DebitNoteId",
                schema: "procurement",
                table: "VendorDebitNoteLines",
                column: "DebitNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebitNoteLines_ReturnLineId",
                schema: "procurement",
                table: "VendorDebitNoteLines",
                column: "ReturnLineId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebitNote_Company_Number",
                schema: "procurement",
                table: "VendorDebitNotes",
                columns: new[] { "CompanyId", "DebitNoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebitNotes_OriginalInvoiceId",
                schema: "procurement",
                table: "VendorDebitNotes",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebitNotes_PurchaseReturnId",
                schema: "procurement",
                table: "VendorDebitNotes",
                column: "PurchaseReturnId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorDebitNotes_VendorId",
                schema: "procurement",
                table: "VendorDebitNotes",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorDocuments_VendorId",
                schema: "procurement",
                table: "VendorDocuments",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPaymentLine_Payment_Invoice",
                schema: "procurement",
                table: "VendorPaymentLines",
                columns: new[] { "PaymentId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorPaymentLines_InvoiceId",
                schema: "procurement",
                table: "VendorPaymentLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayment_Company_Number",
                schema: "procurement",
                table: "VendorPayments",
                columns: new[] { "CompanyId", "PaymentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayments_VendorBankAccountId",
                schema: "procurement",
                table: "VendorPayments",
                column: "VendorBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPayments_VendorId",
                schema: "procurement",
                table: "VendorPayments",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPerformance_Vendor_Period",
                schema: "procurement",
                table: "VendorPerformances",
                columns: new[] { "VendorId", "PeriodFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorPricelistItems_PricelistId",
                schema: "procurement",
                table: "VendorPricelistItems",
                column: "PricelistId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorPricelists_VendorId",
                schema: "procurement",
                table: "VendorPricelists",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationLines_QuotationId",
                schema: "procurement",
                table: "VendorQuotationLines",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotationLines_RFQLineId",
                schema: "procurement",
                table: "VendorQuotationLines",
                column: "RFQLineId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotation_Company_Number",
                schema: "procurement",
                table: "VendorQuotations",
                columns: new[] { "CompanyId", "QuotationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotations_RFQId",
                schema: "procurement",
                table: "VendorQuotations",
                column: "RFQId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorQuotations_VendorId",
                schema: "procurement",
                table: "VendorQuotations",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendor_Company_Number",
                schema: "procurement",
                table: "Vendors",
                columns: new[] { "CompanyId", "VendorNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorCategoryId",
                schema: "procurement",
                table: "Vendors",
                column: "VendorCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovedVendorLists",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "DocumentSequences",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "LandedCostAllocations",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "LandedCostGoodsReceipts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "ProcurementApprovals",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "ProcurementSettings",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseOrderAmendments",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseOrderApprovals",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "RFQVendors",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "ThreeWayMatchRecords",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorAddresses",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorContacts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorDebitNoteLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorDocuments",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorPaymentLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorPerformances",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorPricelistItems",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorQuotationLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "LandedCostLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowSteps",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseInvoiceLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseReturnLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorDebitNotes",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorPayments",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorPricelists",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "RFQLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "LandedCosts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflows",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseInvoices",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseReturns",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorBankAccounts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "GoodsReceipts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseContractLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitionLines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "ProcurementCategories",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseContracts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorQuotations",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "RequestForQuotations",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitions",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "Vendors",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "VendorCategories",
                schema: "procurement");
        }
    }
}
