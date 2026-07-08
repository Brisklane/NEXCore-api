using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hr.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hr");

            migrationBuilder.CreateTable(
                name: "AllowancesProfiles",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowancesProfileCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllowancesProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_AllowancesProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitsPlans",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BenefitsPlanCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BenefitsPlanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_BenefitsPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrencyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GradeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LevelNo = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Grades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobFamilies",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobFamilyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobFamilyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_JobFamilies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobFunctions",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobFunctionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobFunctionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_JobFunctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobLocations",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateProvince = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_JobLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupTypes",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ModuleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_LookupTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScreeningQuestionnaires",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionnaireCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuestionnaireName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QuestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ScreeningQuestionnaires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                schema: "hr",
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
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TalentPools",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TalentPoolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TalentPoolName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_TalentPools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowConfigs",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkflowName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TotalLevels = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayScales",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayScaleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayScaleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_PayScales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayScales_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "hr",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompetencyFrameworks",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    JobFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_CompetencyFrameworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyFrameworks_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalSchema: "hr",
                        principalTable: "JobFamilies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InterviewFeedbackTemplates",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InterviewTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RatingScale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    CompetencyFrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    PassingScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WeightInOverallScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InstructionsForInterviewer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstructionsForCandidate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkillCriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_InterviewFeedbackTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbackTemplates_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalSchema: "hr",
                        principalTable: "JobFamilies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LookupValues",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LookupTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    ParentLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_LookupValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupValues_LookupTypes_LookupTypeId",
                        column: x => x.LookupTypeId,
                        principalSchema: "hr",
                        principalTable: "LookupTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LookupValues_LookupValues_ParentLookupValueId",
                        column: x => x.ParentLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowConditions",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogicalGroup = table.Column<int>(type: "int", nullable: false),
                    JoinOperator = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowConditions_WorkflowConfigs_WorkflowConfigId",
                        column: x => x.WorkflowConfigId,
                        principalSchema: "hr",
                        principalTable: "WorkflowConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowConfigSteps",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevelNo = table.Column<int>(type: "int", nullable: false),
                    ApproverType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mandatory = table.Column<bool>(type: "bit", nullable: false),
                    SLAHours = table.Column<int>(type: "int", nullable: false),
                    ExecutionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsConditional = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowConfigSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowConfigSteps_WorkflowConfigs_WorkflowConfigId",
                        column: x => x.WorkflowConfigId,
                        principalSchema: "hr",
                        principalTable: "WorkflowConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelTemplates",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChannelName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChannelTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupportsAutoPosting = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrackingPrefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultStatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ChannelTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelTemplates_LookupValues_ChannelTypeLookupValueId",
                        column: x => x.ChannelTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChannelTemplates_LookupValues_DefaultStatusLookupValueId",
                        column: x => x.DefaultStatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommunicationTemplates",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommunicationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TemplateTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlaceholdersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanguageCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_CommunicationTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationTemplates_LookupValues_TemplateTypeLookupValueId",
                        column: x => x.TemplateTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SkillTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCoreSkill = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skills_LookupValues_SkillTypeLookupValueId",
                        column: x => x.SkillTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skills_SkillCategories_SkillCategoryId",
                        column: x => x.SkillCategoryId,
                        principalSchema: "hr",
                        principalTable: "SkillCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowEscalations",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowConfigStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AfterHours = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionTarget = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    AutoApproveFlag = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowEscalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowEscalations_WorkflowConfigSteps_WorkflowConfigStepId",
                        column: x => x.WorkflowConfigStepId,
                        principalSchema: "hr",
                        principalTable: "WorkflowConfigSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyFrameworkItems",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetencyFrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumRating = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CompetencyFrameworkItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyFrameworkItems_CompetencyFrameworks_CompetencyFrameworkId",
                        column: x => x.CompetencyFrameworkId,
                        principalSchema: "hr",
                        principalTable: "CompetencyFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetencyFrameworkItems_Skills_SkillId",
                        column: x => x.SkillId,
                        principalSchema: "hr",
                        principalTable: "Skills",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationCompliances",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EEODataCaptured = table.Column<bool>(type: "bit", nullable: false),
                    BackgroundCheckStatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BackgroundCheckDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DrugTestStatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DrugTestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RightToWorkVerified = table.Column<bool>(type: "bit", nullable: false),
                    RightToWorkDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisaSponsorshipRequired = table.Column<bool>(type: "bit", nullable: false),
                    DataConsentGiven = table.Column<bool>(type: "bit", nullable: false),
                    DataConsentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataRetentionExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ApplicationCompliances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationCompliances_LookupValues_BackgroundCheckStatusLookupValueId",
                        column: x => x.BackgroundCheckStatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationCompliances_LookupValues_DrugTestStatusLookupValueId",
                        column: x => x.DrugTestStatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationDetails",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoverLetterUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinQualificationsMet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallRating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResumeParseScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ScreeningQuestionnaireScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ScreeningQuestionnaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScreeningStatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScreeningCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScreeningCompletedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedHiringManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HiringCommitteeNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecruiterNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationPortalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsList = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextFollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CandidateResponseDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastContactedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastContactedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CandidateSatisfactionScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RejectionReasonLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WithdrawalReasonLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionSentByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionNotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    RejectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WithdrawnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedJoinDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualJoinDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LegacyApplicationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacySourceSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ApplicationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationDetails_LookupValues_RejectionReasonLookupValueId",
                        column: x => x.RejectionReasonLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationDetails_LookupValues_ScreeningStatusLookupValueId",
                        column: x => x.ScreeningStatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationDetails_LookupValues_WithdrawalReasonLookupValueId",
                        column: x => x.WithdrawalReasonLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationDetails_ScreeningQuestionnaires_ScreeningQuestionnaireId",
                        column: x => x.ScreeningQuestionnaireId,
                        principalSchema: "hr",
                        principalTable: "ScreeningQuestionnaires",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobPostingChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentStageLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriorityLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsShortlisted = table.Column<bool>(type: "bit", nullable: false),
                    ScreeningScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    InternalScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    AssignedRecruiterEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedToEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HiredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StageChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_LookupValues_CurrentStageLookupValueId",
                        column: x => x.CurrentStageLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applications_LookupValues_PriorityLookupValueId",
                        column: x => x.PriorityLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applications_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentLevel = table.Column<int>(type: "int", nullable: false),
                    TotalLevels = table.Column<int>(type: "int", nullable: false),
                    OverallStatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriorityLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalSubjectCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalSubjectTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                        name: "FK_ApprovalRequests_LookupValues_OverallStatusLookupValueId",
                        column: x => x.OverallStatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_LookupValues_PriorityLookupValueId",
                        column: x => x.PriorityLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_WorkflowConfigs_WorkflowConfigId",
                        column: x => x.WorkflowConfigId,
                        principalSchema: "hr",
                        principalTable: "WorkflowConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequestSteps",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowConfigStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepLevel = table.Column<int>(type: "int", nullable: false),
                    ApproverType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApproverValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproverEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Mandatory = table.Column<bool>(type: "bit", nullable: false),
                    SLAHours = table.Column<int>(type: "int", nullable: false),
                    ExecutionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DelegatedToEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsConditional = table.Column<bool>(type: "bit", nullable: false),
                    ConditionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EscalatedFlag = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedToEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ApprovalRequestSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequestSteps_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalSchema: "hr",
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalRequestSteps_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalRequestSteps_WorkflowConfigSteps_WorkflowConfigStepId",
                        column: x => x.WorkflowConfigStepId,
                        principalSchema: "hr",
                        principalTable: "WorkflowConfigSteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CallLogs",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CalledByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CallDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextActionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextActionType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberUsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CallDirection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordingUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TranscriptUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TelephonySessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TelephonyProvider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentimentScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsFollowUpDone = table.Column<bool>(type: "bit", nullable: false),
                    FollowUpCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommunicationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommunicationTemplateId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_CallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallLogs_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "hr",
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CallLogs_CommunicationTemplates_CommunicationTemplateId",
                        column: x => x.CommunicationTemplateId,
                        principalSchema: "hr",
                        principalTable: "CommunicationTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CallLogs_CommunicationTemplates_CommunicationTemplateId1",
                        column: x => x.CommunicationTemplateId1,
                        principalSchema: "hr",
                        principalTable: "CommunicationTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateAddresses",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Line1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Line2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StateProvince = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_CandidateAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateAddresses_LookupValues_AddressTypeLookupValueId",
                        column: x => x.AddressTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateContacts",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_CandidateContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateContacts_LookupValues_ContactTypeLookupValueId",
                        column: x => x.ContactTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateMediaLinks",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CandidateMediaLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateMediaLinks_LookupValues_MediaTypeLookupValueId",
                        column: x => x.MediaTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateProfiles",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlternateEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobilePhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentDesignation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferredBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountryOfResidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentNoticePeriodDays = table.Column<int>(type: "int", nullable: true),
                    AvailableFromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreferredWorkLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemotePreference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HighestEducationLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HighestEducationField = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PortalRegistered = table.Column<bool>(type: "bit", nullable: false),
                    PortalRegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPortalLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProfileCompletionPercent = table.Column<int>(type: "int", nullable: true),
                    ConsentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TalentPoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TalentPoolAddedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TalentPoolAddedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiversityData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EEOCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VeteranStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisabilityStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecruiterNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlacklistNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlacklistRemovedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BlacklistRemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlacklistRemovalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MergedIntoCandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TalentPoolId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_CandidateProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateProfiles_TalentPools_TalentPoolId",
                        column: x => x.TalentPoolId,
                        principalSchema: "hr",
                        principalTable: "TalentPools",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CandidateProfiles_TalentPools_TalentPoolId1",
                        column: x => x.TalentPoolId1,
                        principalSchema: "hr",
                        principalTable: "TalentPools",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Candidates",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResumeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalExperienceYears = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CurrentSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpectedSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CandidateRating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBlacklisted = table.Column<bool>(type: "bit", nullable: false),
                    BlacklistReasonLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BlacklistedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlacklistedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsentGiven = table.Column<bool>(type: "bit", nullable: false),
                    DataRetentionExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuplicateOfCandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MergedIntoCandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LegacyCandidateId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacySourceSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentDesignationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidates_Candidates_DuplicateOfCandidateId",
                        column: x => x.DuplicateOfCandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Candidates_Candidates_MergedIntoCandidateId",
                        column: x => x.MergedIntoCandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Candidates_LookupValues_BlacklistReasonLookupValueId",
                        column: x => x.BlacklistReasonLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateSkills",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProficiencyLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    YearsExperience = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_CandidateSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateSkills_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateSkills_LookupValues_ProficiencyLookupValueId",
                        column: x => x.ProficiencyLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CandidateSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalSchema: "hr",
                        principalTable: "Skills",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateStageHistories",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStageLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToStageLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DaysInPreviousStage = table.Column<int>(type: "int", nullable: true),
                    IsAutomated = table.Column<bool>(type: "bit", nullable: false),
                    TriggerEvent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SLABreached = table.Column<bool>(type: "bit", nullable: false),
                    SLABreachHours = table.Column<int>(type: "int", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_CandidateStageHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateStageHistories_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "hr",
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CandidateStageHistories_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateStageHistories_LookupValues_FromStageLookupValueId",
                        column: x => x.FromStageLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CandidateStageHistories_LookupValues_ToStageLookupValueId",
                        column: x => x.ToStageLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateTaskEvaluations",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ResultLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TechnicalScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QualityScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreativityScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CommunicationScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WeightedFinalScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackVisibleToCandidate = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedDurationMinutes = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_CandidateTaskEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateTaskEvaluations_LookupValues_ResultLookupValueId",
                        column: x => x.ResultLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateTasks",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalProvider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PassingScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AttemptAllowed = table.Column<int>(type: "int", nullable: false),
                    ReminderSentCount = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_CandidateTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateTasks_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "hr",
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateTasks_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CandidateTasks_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CandidateTasks_LookupValues_TaskTypeLookupValueId",
                        column: x => x.TaskTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CandidateTaskSubmissions",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmissionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    IsLateSubmission = table.Column<bool>(type: "bit", nullable: false),
                    TimeSpentMinutes = table.Column<int>(type: "int", nullable: true),
                    BrowserMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntegrityScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AutoScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ParsedOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_CandidateTaskSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateTaskSubmissions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "hr",
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CandidateTaskSubmissions_CandidateTasks_CandidateTaskId",
                        column: x => x.CandidateTaskId,
                        principalSchema: "hr",
                        principalTable: "CandidateTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateTaskSubmissions_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CandidateTaskSubmissions_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CostCenters",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCenterCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CostCenterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BudgetOwnerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentHeadEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalSchema: "hr",
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalSchema: "hr",
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Designations",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesignationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DesignationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobFunctionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Designations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Designations_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "hr",
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Designations_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "hr",
                        principalTable: "Grades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Designations_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalSchema: "hr",
                        principalTable: "JobFamilies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Designations_JobFunctions_JobFunctionId",
                        column: x => x.JobFunctionId,
                        principalSchema: "hr",
                        principalTable: "JobFunctions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTaskTemplates",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaskCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultAssigneeRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultDueDaysFromStart = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ApplicableDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicableDesignationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicableEmploymentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicableLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DependsOnTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RequiresDocumentUpload = table.Column<bool>(type: "bit", nullable: false),
                    RequiresManagerSignoff = table.Column<bool>(type: "bit", nullable: false),
                    NotifyEmployeeOnAssign = table.Column<bool>(type: "bit", nullable: false),
                    NotifyAssigneeOnCreate = table.Column<bool>(type: "bit", nullable: false),
                    EscalateAfterDays = table.Column<int>(type: "int", nullable: true),
                    EscalateToRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacyTemplateId = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_OnboardingTaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTaskTemplates_Departments_ApplicableDepartmentId",
                        column: x => x.ApplicableDepartmentId,
                        principalSchema: "hr",
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTaskTemplates_Designations_ApplicableDesignationId",
                        column: x => x.ApplicableDesignationId,
                        principalSchema: "hr",
                        principalTable: "Designations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTaskTemplates_JobLocations_ApplicableLocationId",
                        column: x => x.ApplicableLocationId,
                        principalSchema: "hr",
                        principalTable: "JobLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTaskTemplates_OnboardingTaskTemplates_DependsOnTemplateId",
                        column: x => x.DependsOnTemplateId,
                        principalSchema: "hr",
                        principalTable: "OnboardingTaskTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesignationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobFunctionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportsToPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsVacant = table.Column<bool>(type: "bit", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "hr",
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_Designations_DesignationId",
                        column: x => x.DesignationId,
                        principalSchema: "hr",
                        principalTable: "Designations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "hr",
                        principalTable: "Grades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalSchema: "hr",
                        principalTable: "JobFamilies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_JobFunctions_JobFunctionId",
                        column: x => x.JobFunctionId,
                        principalSchema: "hr",
                        principalTable: "JobFunctions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_PayScales_PayScaleId",
                        column: x => x.PayScaleId,
                        principalSchema: "hr",
                        principalTable: "PayScales",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_Positions_ReportsToPositionId",
                        column: x => x.ReportsToPositionId,
                        principalSchema: "hr",
                        principalTable: "Positions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "hr",
                        principalTable: "Shifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DesignationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportingManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "hr",
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Designations_DesignationId",
                        column: x => x.DesignationId,
                        principalSchema: "hr",
                        principalTable: "Designations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Employees_ReportingManagerId",
                        column: x => x.ReportingManagerId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_JobLocations_JobLocationId",
                        column: x => x.JobLocationId,
                        principalSchema: "hr",
                        principalTable: "JobLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "hr",
                        principalTable: "Positions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "hr",
                        principalTable: "Shifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobTemplates",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DesignationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmploymentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredSkills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Responsibilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobFunctionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkerCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemoteType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinExperienceYears = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxExperienceYears = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EducationRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredSkills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanguagesRequired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ParentTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LegacyTemplateId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacySourceSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_JobTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTemplates_Designations_DesignationId",
                        column: x => x.DesignationId,
                        principalSchema: "hr",
                        principalTable: "Designations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobTemplates_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "hr",
                        principalTable: "Grades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobTemplates_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalSchema: "hr",
                        principalTable: "JobFamilies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobTemplates_JobFunctions_JobFunctionId",
                        column: x => x.JobFunctionId,
                        principalSchema: "hr",
                        principalTable: "JobFunctions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobTemplates_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "hr",
                        principalTable: "Positions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobTemplates_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "hr",
                        principalTable: "Shifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTasks",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OnboardingTaskTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAutoGenerated = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DependsOnTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresDocumentUpload = table.Column<bool>(type: "bit", nullable: false),
                    RequiresManagerSignOff = table.Column<bool>(type: "bit", nullable: false),
                    ManagerSignOffDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EscalatedFlag = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EscalatedToEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReminderSentCount = table.Column<int>(type: "int", nullable: false),
                    LastReminderSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SLABreached = table.Column<bool>(type: "bit", nullable: false),
                    SLABreachHours = table.Column<int>(type: "int", nullable: true),
                    BlockedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancelledByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_OnboardingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "hr",
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Employees_AssignedToEmployeeId",
                        column: x => x.AssignedToEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Employees_CancelledByEmployeeId",
                        column: x => x.CancelledByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Employees_CompletedByEmployeeId",
                        column: x => x.CompletedByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Employees_EscalatedToEmployeeId",
                        column: x => x.EscalatedToEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Employees_VerifiedByEmployeeId",
                        column: x => x.VerifiedByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_OnboardingTaskTemplates_OnboardingTaskTemplateId",
                        column: x => x.OnboardingTaskTemplateId,
                        principalSchema: "hr",
                        principalTable: "OnboardingTaskTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_OnboardingTasks_DependsOnTaskId",
                        column: x => x.DependsOnTaskId,
                        principalSchema: "hr",
                        principalTable: "OnboardingTasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RecordType = table.Column<int>(type: "int", nullable: false),
                    ParentJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesignationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Headcount = table.Column<int>(type: "int", nullable: false),
                    FilledCount = table.Column<int>(type: "int", nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    SalaryRangeMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalaryRangeMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PriorityLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HiringManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecruiterEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostingStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostingCloseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalSchema: "hr",
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "hr",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "hr",
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_Designations_DesignationId",
                        column: x => x.DesignationId,
                        principalSchema: "hr",
                        principalTable: "Designations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_Employees_ClosedByEmployeeId",
                        column: x => x.ClosedByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_Employees_HiringManagerEmployeeId",
                        column: x => x.HiringManagerEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_Employees_RecruiterEmployeeId",
                        column: x => x.RecruiterEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_JobTemplates_JobTemplateId",
                        column: x => x.JobTemplateId,
                        principalSchema: "hr",
                        principalTable: "JobTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_Jobs_ParentJobId",
                        column: x => x.ParentJobId,
                        principalSchema: "hr",
                        principalTable: "Jobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_LookupValues_PriorityLookupValueId",
                        column: x => x.PriorityLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Jobs_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Interviews",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InterviewTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewSequenceNo = table.Column<int>(type: "int", nullable: true),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewFeedbackTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Format = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalendarProvider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalendarEventId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProposedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CandidateConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CandidateConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginalScheduledStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RescheduleCount = table.Column<int>(type: "int", nullable: false),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    DecisionLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WeightedPanelScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    FeedbackDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FeedbackSubmittedCount = table.Column<int>(type: "int", nullable: false),
                    SLABreached = table.Column<bool>(type: "bit", nullable: false),
                    RecordingUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMandatoryRound = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Interviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interviews_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "hr",
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Interviews_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Interviews_Employees_ProposedByEmployeeId",
                        column: x => x.ProposedByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Interviews_InterviewFeedbackTemplates_InterviewFeedbackTemplateId",
                        column: x => x.InterviewFeedbackTemplateId,
                        principalSchema: "hr",
                        principalTable: "InterviewFeedbackTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Interviews_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "hr",
                        principalTable: "Jobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Interviews_LookupValues_DecisionLookupValueId",
                        column: x => x.DecisionLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Interviews_LookupValues_InterviewTypeLookupValueId",
                        column: x => x.InterviewTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Interviews_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobDetails",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobFunctionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplacedEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportingManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkerCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HiringType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VacancyReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredSkills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredSkills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Responsibilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EducationRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanguagesRequired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinExperienceYears = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxExperienceYears = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BonusEligible = table.Column<bool>(type: "bit", nullable: false),
                    AllowancesProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BudgetApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ForecastedHireCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PublishExternallyFlag = table.Column<bool>(type: "bit", nullable: false),
                    CareerSiteVisible = table.Column<bool>(type: "bit", nullable: false),
                    InternalOnlyFlag = table.Column<bool>(type: "bit", nullable: false),
                    BackgroundCheckRequired = table.Column<bool>(type: "bit", nullable: false),
                    DrugTestRequired = table.Column<bool>(type: "bit", nullable: false),
                    SecurityClearanceLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisaSponsorshipAvailable = table.Column<bool>(type: "bit", nullable: false),
                    ConfidentialJobFlag = table.Column<bool>(type: "bit", nullable: false),
                    DiversityTargetFlag = table.Column<bool>(type: "bit", nullable: false),
                    EEOCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnionRoleFlag = table.Column<bool>(type: "bit", nullable: false),
                    TravelRequiredPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SourceCampaignCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferralBonusEligible = table.Column<bool>(type: "bit", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InternalJobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalJobCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacySourceSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacyRecordId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScreeningQuestionnaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_JobDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobDetails_AllowancesProfiles_AllowancesProfileId",
                        column: x => x.AllowancesProfileId,
                        principalSchema: "hr",
                        principalTable: "AllowancesProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalSchema: "hr",
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_Employees_ClosedByEmployeeId",
                        column: x => x.ClosedByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_Employees_ReplacedEmployeeId",
                        column: x => x.ReplacedEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_Employees_ReportingManagerEmployeeId",
                        column: x => x.ReportingManagerEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "hr",
                        principalTable: "Grades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalSchema: "hr",
                        principalTable: "JobFamilies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_JobFunctions_JobFunctionId",
                        column: x => x.JobFunctionId,
                        principalSchema: "hr",
                        principalTable: "JobFunctions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_JobLocations_JobLocationId",
                        column: x => x.JobLocationId,
                        principalSchema: "hr",
                        principalTable: "JobLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_JobTemplates_JobTemplateId",
                        column: x => x.JobTemplateId,
                        principalSchema: "hr",
                        principalTable: "JobTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "hr",
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobDetails_PayScales_PayScaleId",
                        column: x => x.PayScaleId,
                        principalSchema: "hr",
                        principalTable: "PayScales",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "hr",
                        principalTable: "Positions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_ScreeningQuestionnaires_ScreeningQuestionnaireId",
                        column: x => x.ScreeningQuestionnaireId,
                        principalSchema: "hr",
                        principalTable: "ScreeningQuestionnaires",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobDetails_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "hr",
                        principalTable: "Shifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobPostingChannels",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelNameSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChannelTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceTrackingCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostingUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CloseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationsReceived = table.Column<int>(type: "int", nullable: false),
                    IsSponsored = table.Column<bool>(type: "bit", nullable: false),
                    SponsoredBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SponsoredStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SponsoredEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgencyFeePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ShortlistedCount = table.Column<int>(type: "int", nullable: false),
                    HiredCount = table.Column<int>(type: "int", nullable: false),
                    CostPerApplication = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CostPerHire = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QualityScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExternalPostingId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacySourceSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_JobPostingChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPostingChannels_ChannelTemplates_ChannelTemplateId",
                        column: x => x.ChannelTemplateId,
                        principalSchema: "hr",
                        principalTable: "ChannelTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobPostingChannels_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "hr",
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobPostingChannels_LookupValues_ChannelTypeLookupValueId",
                        column: x => x.ChannelTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobPostingChannels_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OfferLetters",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ReportingManagerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPackage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProbationPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    NoticePeriodDays = table.Column<int>(type: "int", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CandidateResponseLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentVia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevokeReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommunicationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_OfferLetters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferLetters_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "hr",
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_ApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalSchema: "hr",
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_CommunicationTemplates_CommunicationTemplateId",
                        column: x => x.CommunicationTemplateId,
                        principalSchema: "hr",
                        principalTable: "CommunicationTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "hr",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_Employees_ReportingManagerEmployeeId",
                        column: x => x.ReportingManagerEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_Employees_RevokedByEmployeeId",
                        column: x => x.RevokedByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_Employees_SentByEmployeeId",
                        column: x => x.SentByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "hr",
                        principalTable: "Jobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_LookupValues_CandidateResponseLookupValueId",
                        column: x => x.CandidateResponseLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetters_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InterviewerAvailabilities",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SlotStatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_InterviewerAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewerAvailabilities_Employees_InterviewerEmployeeId",
                        column: x => x.InterviewerEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewerAvailabilities_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalSchema: "hr",
                        principalTable: "Interviews",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewerAvailabilities_LookupValues_SlotStatusLookupValueId",
                        column: x => x.SlotStatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InterviewPanelMembers",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlternateInterviewerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoreWeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsLead = table.Column<bool>(type: "bit", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    InviteStatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InviteSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasConflictOfInterest = table.Column<bool>(type: "bit", nullable: false),
                    ConflictOfInterestNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAvailabilityConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    FeedbackDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeclinedToParticipate = table.Column<bool>(type: "bit", nullable: false),
                    DeclineReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttendanceStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PanelSequence = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_InterviewPanelMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewPanelMembers_Employees_AlternateInterviewerEmployeeId",
                        column: x => x.AlternateInterviewerEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewPanelMembers_Employees_InterviewerEmployeeId",
                        column: x => x.InterviewerEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewPanelMembers_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalSchema: "hr",
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewPanelMembers_LookupValues_InviteStatusLookupValueId",
                        column: x => x.InviteStatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OfferLetterDetails",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferLetterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowancesBreakdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HousingAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TransportAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MedicalAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OtherAllowances = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BonusTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BonusPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EquityGrant = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BenefitsPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AllowancesProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BenefitsSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CounterSignedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CounterSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DigitalSignatureUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDigitallySigned = table.Column<bool>(type: "bit", nullable: false),
                    LegacyOfferId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_OfferLetterDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferLetterDetails_AllowancesProfiles_AllowancesProfileId",
                        column: x => x.AllowancesProfileId,
                        principalSchema: "hr",
                        principalTable: "AllowancesProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetterDetails_BenefitsPlans_BenefitsPlanId",
                        column: x => x.BenefitsPlanId,
                        principalSchema: "hr",
                        principalTable: "BenefitsPlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetterDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "hr",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetterDetails_Employees_CounterSignedByEmployeeId",
                        column: x => x.CounterSignedByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetterDetails_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "hr",
                        principalTable: "Grades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferLetterDetails_OfferLetters_OfferLetterId",
                        column: x => x.OfferLetterId,
                        principalSchema: "hr",
                        principalTable: "OfferLetters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferLetterDetails_PayScales_PayScaleId",
                        column: x => x.PayScaleId,
                        principalSchema: "hr",
                        principalTable: "PayScales",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OfferNegotiations",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegotiationRound = table.Column<int>(type: "int", nullable: false),
                    InitiatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NegotiationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProposedSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProposedTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CounterOfferNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SalaryDifference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NegotiationReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CandidateCompetingOffer = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CandidateCompetingCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HRRecommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresReApproval = table.Column<bool>(type: "bit", nullable: false),
                    ReApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_OfferNegotiations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferNegotiations_ApprovalRequests_ReApprovalRequestId",
                        column: x => x.ReApprovalRequestId,
                        principalSchema: "hr",
                        principalTable: "ApprovalRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferNegotiations_Employees_ResponseByEmployeeId",
                        column: x => x.ResponseByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferNegotiations_LookupValues_StatusLookupValueId",
                        column: x => x.StatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OfferNegotiations_OfferLetters_OfferId",
                        column: x => x.OfferId,
                        principalSchema: "hr",
                        principalTable: "OfferLetters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewFeedbacks",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PanelMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    RecommendationLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecisionLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TechnicalScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BehavioralScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CommunicationScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CultureFitScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WeightedFinalScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StrengthNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcernNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompetencyScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HireReadiness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WouldRehire = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedLate = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedByHR = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfidentialNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_InterviewFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "hr",
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "hr",
                        principalTable: "Candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_Employees_SubmittedByEmployeeId",
                        column: x => x.SubmittedByEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_InterviewPanelMembers_PanelMemberId",
                        column: x => x.PanelMemberId,
                        principalSchema: "hr",
                        principalTable: "InterviewPanelMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalSchema: "hr",
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_LookupValues_DecisionLookupValueId",
                        column: x => x.DecisionLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_LookupValues_RecommendationLookupValueId",
                        column: x => x.RecommendationLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InterviewNotifications",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewPanelMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationTypeLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryStatusLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseActionLookupValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommunicationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommunicationTemplateId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_InterviewNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewNotifications_CommunicationTemplates_CommunicationTemplateId",
                        column: x => x.CommunicationTemplateId,
                        principalSchema: "hr",
                        principalTable: "CommunicationTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewNotifications_CommunicationTemplates_CommunicationTemplateId1",
                        column: x => x.CommunicationTemplateId1,
                        principalSchema: "hr",
                        principalTable: "CommunicationTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewNotifications_Employees_RecipientEmployeeId",
                        column: x => x.RecipientEmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewNotifications_InterviewPanelMembers_InterviewPanelMemberId",
                        column: x => x.InterviewPanelMemberId,
                        principalSchema: "hr",
                        principalTable: "InterviewPanelMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewNotifications_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalSchema: "hr",
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewNotifications_LookupValues_DeliveryStatusLookupValueId",
                        column: x => x.DeliveryStatusLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewNotifications_LookupValues_NotificationTypeLookupValueId",
                        column: x => x.NotificationTypeLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewNotifications_LookupValues_ResponseActionLookupValueId",
                        column: x => x.ResponseActionLookupValueId,
                        principalSchema: "hr",
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationCompliances_ApplicationId",
                schema: "hr",
                table: "ApplicationCompliances",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationCompliances_BackgroundCheckStatusLookupValueId",
                schema: "hr",
                table: "ApplicationCompliances",
                column: "BackgroundCheckStatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationCompliances_DrugTestStatusLookupValueId",
                schema: "hr",
                table: "ApplicationCompliances",
                column: "DrugTestStatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_ApplicationId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_AssignedHiringManagerEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "AssignedHiringManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_LastContactedByEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "LastContactedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_RejectionReasonLookupValueId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "RejectionReasonLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_RejectionSentByEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "RejectionSentByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_ReviewedByEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "ReviewedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_ScreeningCompletedByEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "ScreeningCompletedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_ScreeningQuestionnaireId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "ScreeningQuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_ScreeningStatusLookupValueId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "ScreeningStatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDetails_WithdrawalReasonLookupValueId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "WithdrawalReasonLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AssignedRecruiterEmployeeId",
                schema: "hr",
                table: "Applications",
                column: "AssignedRecruiterEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CandidateId",
                schema: "hr",
                table: "Applications",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ConvertedToEmployeeId",
                schema: "hr",
                table: "Applications",
                column: "ConvertedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CurrentStageLookupValueId",
                schema: "hr",
                table: "Applications",
                column: "CurrentStageLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobId",
                schema: "hr",
                table: "Applications",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobPostingChannelId",
                schema: "hr",
                table: "Applications",
                column: "JobPostingChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_PriorityLookupValueId",
                schema: "hr",
                table: "Applications",
                column: "PriorityLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_StatusLookupValueId",
                schema: "hr",
                table: "Applications",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_OverallStatusLookupValueId",
                schema: "hr",
                table: "ApprovalRequests",
                column: "OverallStatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_PriorityLookupValueId",
                schema: "hr",
                table: "ApprovalRequests",
                column: "PriorityLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedByEmployeeId",
                schema: "hr",
                table: "ApprovalRequests",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_WorkflowConfigId",
                schema: "hr",
                table: "ApprovalRequests",
                column: "WorkflowConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestSteps_ApprovalRequestId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestSteps_ApproverEmployeeId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "ApproverEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestSteps_DelegatedToEmployeeId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "DelegatedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestSteps_EscalatedToEmployeeId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "EscalatedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestSteps_StatusLookupValueId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestSteps_WorkflowConfigStepId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "WorkflowConfigStepId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_ApplicationId",
                schema: "hr",
                table: "CallLogs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_CalledByEmployeeId",
                schema: "hr",
                table: "CallLogs",
                column: "CalledByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_CandidateId",
                schema: "hr",
                table: "CallLogs",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_CommunicationTemplateId",
                schema: "hr",
                table: "CallLogs",
                column: "CommunicationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_CommunicationTemplateId1",
                schema: "hr",
                table: "CallLogs",
                column: "CommunicationTemplateId1");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAddresses_AddressTypeLookupValueId",
                schema: "hr",
                table: "CandidateAddresses",
                column: "AddressTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAddresses_CandidateId",
                schema: "hr",
                table: "CandidateAddresses",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateContacts_CandidateId",
                schema: "hr",
                table: "CandidateContacts",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateContacts_ContactTypeLookupValueId",
                schema: "hr",
                table: "CandidateContacts",
                column: "ContactTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateMediaLinks_CandidateId",
                schema: "hr",
                table: "CandidateMediaLinks",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateMediaLinks_MediaTypeLookupValueId",
                schema: "hr",
                table: "CandidateMediaLinks",
                column: "MediaTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_BlacklistRemovedByEmployeeId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "BlacklistRemovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_CandidateId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "CandidateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_MergedIntoCandidateId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "MergedIntoCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_TalentPoolAddedByEmployeeId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "TalentPoolAddedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_TalentPoolId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "TalentPoolId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_TalentPoolId1",
                schema: "hr",
                table: "CandidateProfiles",
                column: "TalentPoolId1");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_BlacklistedByEmployeeId",
                schema: "hr",
                table: "Candidates",
                column: "BlacklistedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_BlacklistReasonLookupValueId",
                schema: "hr",
                table: "Candidates",
                column: "BlacklistReasonLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_CurrentDesignationId",
                schema: "hr",
                table: "Candidates",
                column: "CurrentDesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_DuplicateOfCandidateId",
                schema: "hr",
                table: "Candidates",
                column: "DuplicateOfCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_MergedIntoCandidateId",
                schema: "hr",
                table: "Candidates",
                column: "MergedIntoCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSkills_CandidateId",
                schema: "hr",
                table: "CandidateSkills",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSkills_ProficiencyLookupValueId",
                schema: "hr",
                table: "CandidateSkills",
                column: "ProficiencyLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSkills_SkillId",
                schema: "hr",
                table: "CandidateSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSkills_VerifiedByEmployeeId",
                schema: "hr",
                table: "CandidateSkills",
                column: "VerifiedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateStageHistories_ApplicationId",
                schema: "hr",
                table: "CandidateStageHistories",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateStageHistories_CandidateId",
                schema: "hr",
                table: "CandidateStageHistories",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateStageHistories_ChangedByEmployeeId",
                schema: "hr",
                table: "CandidateStageHistories",
                column: "ChangedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateStageHistories_FromStageLookupValueId",
                schema: "hr",
                table: "CandidateStageHistories",
                column: "FromStageLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateStageHistories_ToStageLookupValueId",
                schema: "hr",
                table: "CandidateStageHistories",
                column: "ToStageLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTaskEvaluations_CandidateTaskId",
                schema: "hr",
                table: "CandidateTaskEvaluations",
                column: "CandidateTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTaskEvaluations_EvaluatedByEmployeeId",
                schema: "hr",
                table: "CandidateTaskEvaluations",
                column: "EvaluatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTaskEvaluations_ResultLookupValueId",
                schema: "hr",
                table: "CandidateTaskEvaluations",
                column: "ResultLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTaskEvaluations_SubmissionId",
                schema: "hr",
                table: "CandidateTaskEvaluations",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTasks_ApplicationId",
                schema: "hr",
                table: "CandidateTasks",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTasks_AssignedByEmployeeId",
                schema: "hr",
                table: "CandidateTasks",
                column: "AssignedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTasks_CandidateId",
                schema: "hr",
                table: "CandidateTasks",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTasks_JobId",
                schema: "hr",
                table: "CandidateTasks",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTasks_StatusLookupValueId",
                schema: "hr",
                table: "CandidateTasks",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTasks_TaskTypeLookupValueId",
                schema: "hr",
                table: "CandidateTasks",
                column: "TaskTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTaskSubmissions_ApplicationId",
                schema: "hr",
                table: "CandidateTaskSubmissions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTaskSubmissions_CandidateId",
                schema: "hr",
                table: "CandidateTaskSubmissions",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTaskSubmissions_CandidateTaskId",
                schema: "hr",
                table: "CandidateTaskSubmissions",
                column: "CandidateTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateTaskSubmissions_StatusLookupValueId",
                schema: "hr",
                table: "CandidateTaskSubmissions",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelTemplates_ChannelTypeLookupValueId",
                schema: "hr",
                table: "ChannelTemplates",
                column: "ChannelTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelTemplates_DefaultStatusLookupValueId",
                schema: "hr",
                table: "ChannelTemplates",
                column: "DefaultStatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTemplates_TemplateCode",
                schema: "hr",
                table: "CommunicationTemplates",
                column: "TemplateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationTemplates_TemplateTypeLookupValueId",
                schema: "hr",
                table: "CommunicationTemplates",
                column: "TemplateTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyFrameworkItems_CompetencyFrameworkId",
                schema: "hr",
                table: "CompetencyFrameworkItems",
                column: "CompetencyFrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyFrameworkItems_SkillId",
                schema: "hr",
                table: "CompetencyFrameworkItems",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyFrameworks_JobFamilyId",
                schema: "hr",
                table: "CompetencyFrameworks",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_BudgetOwnerEmployeeId",
                schema: "hr",
                table: "CostCenters",
                column: "BudgetOwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_DepartmentId",
                schema: "hr",
                table: "CostCenters",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_CurrencyCode",
                schema: "hr",
                table: "Currencies",
                column: "CurrencyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CostCenterId",
                schema: "hr",
                table: "Departments",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentHeadEmployeeId",
                schema: "hr",
                table: "Departments",
                column: "DepartmentHeadEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                schema: "hr",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Designations_DepartmentId",
                schema: "hr",
                table: "Designations",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Designations_GradeId",
                schema: "hr",
                table: "Designations",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Designations_JobFamilyId",
                schema: "hr",
                table: "Designations",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Designations_JobFunctionId",
                schema: "hr",
                table: "Designations",
                column: "JobFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Tenant_Code",
                schema: "hr",
                table: "Employees",
                columns: new[] { "CompanyId", "BranchId", "BusinessUnitId", "EmployeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                schema: "hr",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DesignationId",
                schema: "hr",
                table: "Employees",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_JobLocationId",
                schema: "hr",
                table: "Employees",
                column: "JobLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                schema: "hr",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ReportingManagerId",
                schema: "hr",
                table: "Employees",
                column: "ReportingManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ShiftId",
                schema: "hr",
                table: "Employees",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewerAvailabilities_InterviewerEmployeeId",
                schema: "hr",
                table: "InterviewerAvailabilities",
                column: "InterviewerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewerAvailabilities_InterviewId",
                schema: "hr",
                table: "InterviewerAvailabilities",
                column: "InterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewerAvailabilities_SlotStatusLookupValueId",
                schema: "hr",
                table: "InterviewerAvailabilities",
                column: "SlotStatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_ApplicationId",
                schema: "hr",
                table: "InterviewFeedbacks",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_CandidateId",
                schema: "hr",
                table: "InterviewFeedbacks",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_DecisionLookupValueId",
                schema: "hr",
                table: "InterviewFeedbacks",
                column: "DecisionLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_InterviewId",
                schema: "hr",
                table: "InterviewFeedbacks",
                column: "InterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_PanelMemberId",
                schema: "hr",
                table: "InterviewFeedbacks",
                column: "PanelMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_RecommendationLookupValueId",
                schema: "hr",
                table: "InterviewFeedbacks",
                column: "RecommendationLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_SubmittedByEmployeeId",
                schema: "hr",
                table: "InterviewFeedbacks",
                column: "SubmittedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbackTemplates_JobFamilyId",
                schema: "hr",
                table: "InterviewFeedbackTemplates",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewNotifications_CommunicationTemplateId",
                schema: "hr",
                table: "InterviewNotifications",
                column: "CommunicationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewNotifications_CommunicationTemplateId1",
                schema: "hr",
                table: "InterviewNotifications",
                column: "CommunicationTemplateId1");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewNotifications_DeliveryStatusLookupValueId",
                schema: "hr",
                table: "InterviewNotifications",
                column: "DeliveryStatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewNotifications_InterviewId",
                schema: "hr",
                table: "InterviewNotifications",
                column: "InterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewNotifications_InterviewPanelMemberId",
                schema: "hr",
                table: "InterviewNotifications",
                column: "InterviewPanelMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewNotifications_NotificationTypeLookupValueId",
                schema: "hr",
                table: "InterviewNotifications",
                column: "NotificationTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewNotifications_RecipientEmployeeId",
                schema: "hr",
                table: "InterviewNotifications",
                column: "RecipientEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewNotifications_ResponseActionLookupValueId",
                schema: "hr",
                table: "InterviewNotifications",
                column: "ResponseActionLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPanelMembers_AlternateInterviewerEmployeeId",
                schema: "hr",
                table: "InterviewPanelMembers",
                column: "AlternateInterviewerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPanelMembers_InterviewerEmployeeId",
                schema: "hr",
                table: "InterviewPanelMembers",
                column: "InterviewerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPanelMembers_InterviewId",
                schema: "hr",
                table: "InterviewPanelMembers",
                column: "InterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPanelMembers_InviteStatusLookupValueId",
                schema: "hr",
                table: "InterviewPanelMembers",
                column: "InviteStatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_ApplicationId",
                schema: "hr",
                table: "Interviews",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_CandidateId",
                schema: "hr",
                table: "Interviews",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_DecisionLookupValueId",
                schema: "hr",
                table: "Interviews",
                column: "DecisionLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_InterviewFeedbackTemplateId",
                schema: "hr",
                table: "Interviews",
                column: "InterviewFeedbackTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_InterviewTypeLookupValueId",
                schema: "hr",
                table: "Interviews",
                column: "InterviewTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_JobId",
                schema: "hr",
                table: "Interviews",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_ProposedByEmployeeId",
                schema: "hr",
                table: "Interviews",
                column: "ProposedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_StatusLookupValueId",
                schema: "hr",
                table: "Interviews",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_AllowancesProfileId",
                schema: "hr",
                table: "JobDetails",
                column: "AllowancesProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_ClosedByEmployeeId",
                schema: "hr",
                table: "JobDetails",
                column: "ClosedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_CostCenterId",
                schema: "hr",
                table: "JobDetails",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_GradeId",
                schema: "hr",
                table: "JobDetails",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_JobFamilyId",
                schema: "hr",
                table: "JobDetails",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_JobFunctionId",
                schema: "hr",
                table: "JobDetails",
                column: "JobFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_JobId",
                schema: "hr",
                table: "JobDetails",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_JobLocationId",
                schema: "hr",
                table: "JobDetails",
                column: "JobLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_JobTemplateId",
                schema: "hr",
                table: "JobDetails",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_PayScaleId",
                schema: "hr",
                table: "JobDetails",
                column: "PayScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_PositionId",
                schema: "hr",
                table: "JobDetails",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_ReplacedEmployeeId",
                schema: "hr",
                table: "JobDetails",
                column: "ReplacedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_ReportingManagerEmployeeId",
                schema: "hr",
                table: "JobDetails",
                column: "ReportingManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_ScreeningQuestionnaireId",
                schema: "hr",
                table: "JobDetails",
                column: "ScreeningQuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDetails_ShiftId",
                schema: "hr",
                table: "JobDetails",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostingChannels_ChannelTemplateId",
                schema: "hr",
                table: "JobPostingChannels",
                column: "ChannelTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostingChannels_ChannelTypeLookupValueId",
                schema: "hr",
                table: "JobPostingChannels",
                column: "ChannelTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostingChannels_JobId",
                schema: "hr",
                table: "JobPostingChannels",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostingChannels_StatusLookupValueId",
                schema: "hr",
                table: "JobPostingChannels",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ApprovalRequestId",
                schema: "hr",
                table: "Jobs",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ClosedByEmployeeId",
                schema: "hr",
                table: "Jobs",
                column: "ClosedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CurrencyId",
                schema: "hr",
                table: "Jobs",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DepartmentId",
                schema: "hr",
                table: "Jobs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_DesignationId",
                schema: "hr",
                table: "Jobs",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_HiringManagerEmployeeId",
                schema: "hr",
                table: "Jobs",
                column: "HiringManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobTemplateId",
                schema: "hr",
                table: "Jobs",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ParentJobId",
                schema: "hr",
                table: "Jobs",
                column: "ParentJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_PriorityLookupValueId",
                schema: "hr",
                table: "Jobs",
                column: "PriorityLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RecruiterEmployeeId",
                schema: "hr",
                table: "Jobs",
                column: "RecruiterEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_StatusLookupValueId",
                schema: "hr",
                table: "Jobs",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_DesignationId",
                schema: "hr",
                table: "JobTemplates",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_GradeId",
                schema: "hr",
                table: "JobTemplates",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_JobFamilyId",
                schema: "hr",
                table: "JobTemplates",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_JobFunctionId",
                schema: "hr",
                table: "JobTemplates",
                column: "JobFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_PositionId",
                schema: "hr",
                table: "JobTemplates",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_ShiftId",
                schema: "hr",
                table: "JobTemplates",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_LookupTypeId",
                schema: "hr",
                table: "LookupValues",
                column: "LookupTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_ParentLookupValueId",
                schema: "hr",
                table: "LookupValues",
                column: "ParentLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetterDetails_AllowancesProfileId",
                schema: "hr",
                table: "OfferLetterDetails",
                column: "AllowancesProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetterDetails_BenefitsPlanId",
                schema: "hr",
                table: "OfferLetterDetails",
                column: "BenefitsPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetterDetails_CounterSignedByEmployeeId",
                schema: "hr",
                table: "OfferLetterDetails",
                column: "CounterSignedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetterDetails_CurrencyId",
                schema: "hr",
                table: "OfferLetterDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetterDetails_GradeId",
                schema: "hr",
                table: "OfferLetterDetails",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetterDetails_OfferLetterId",
                schema: "hr",
                table: "OfferLetterDetails",
                column: "OfferLetterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetterDetails_PayScaleId",
                schema: "hr",
                table: "OfferLetterDetails",
                column: "PayScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_ApplicationId",
                schema: "hr",
                table: "OfferLetters",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_ApprovalRequestId",
                schema: "hr",
                table: "OfferLetters",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_CandidateId",
                schema: "hr",
                table: "OfferLetters",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_CandidateResponseLookupValueId",
                schema: "hr",
                table: "OfferLetters",
                column: "CandidateResponseLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_CommunicationTemplateId",
                schema: "hr",
                table: "OfferLetters",
                column: "CommunicationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_CurrencyId",
                schema: "hr",
                table: "OfferLetters",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_JobId",
                schema: "hr",
                table: "OfferLetters",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_ReportingManagerEmployeeId",
                schema: "hr",
                table: "OfferLetters",
                column: "ReportingManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_RevokedByEmployeeId",
                schema: "hr",
                table: "OfferLetters",
                column: "RevokedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_SentByEmployeeId",
                schema: "hr",
                table: "OfferLetters",
                column: "SentByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_StatusLookupValueId",
                schema: "hr",
                table: "OfferLetters",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferNegotiations_OfferId",
                schema: "hr",
                table: "OfferNegotiations",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferNegotiations_ReApprovalRequestId",
                schema: "hr",
                table: "OfferNegotiations",
                column: "ReApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferNegotiations_ResponseByEmployeeId",
                schema: "hr",
                table: "OfferNegotiations",
                column: "ResponseByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferNegotiations_StatusLookupValueId",
                schema: "hr",
                table: "OfferNegotiations",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_ApplicationId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_AssignedToEmployeeId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_CancelledByEmployeeId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "CancelledByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_CandidateId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_CompletedByEmployeeId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "CompletedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_DependsOnTaskId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "DependsOnTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_EmployeeId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_EscalatedToEmployeeId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "EscalatedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_OnboardingTaskTemplateId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "OnboardingTaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_StatusLookupValueId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "StatusLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_VerifiedByEmployeeId",
                schema: "hr",
                table: "OnboardingTasks",
                column: "VerifiedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTaskTemplates_ApplicableDepartmentId",
                schema: "hr",
                table: "OnboardingTaskTemplates",
                column: "ApplicableDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTaskTemplates_ApplicableDesignationId",
                schema: "hr",
                table: "OnboardingTaskTemplates",
                column: "ApplicableDesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTaskTemplates_ApplicableLocationId",
                schema: "hr",
                table: "OnboardingTaskTemplates",
                column: "ApplicableLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTaskTemplates_DependsOnTemplateId",
                schema: "hr",
                table: "OnboardingTaskTemplates",
                column: "DependsOnTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PayScales_CurrencyId",
                schema: "hr",
                table: "PayScales",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DepartmentId",
                schema: "hr",
                table: "Positions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DesignationId",
                schema: "hr",
                table: "Positions",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_GradeId",
                schema: "hr",
                table: "Positions",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_JobFamilyId",
                schema: "hr",
                table: "Positions",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_JobFunctionId",
                schema: "hr",
                table: "Positions",
                column: "JobFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PayScaleId",
                schema: "hr",
                table: "Positions",
                column: "PayScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_ReportsToPositionId",
                schema: "hr",
                table: "Positions",
                column: "ReportsToPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_ShiftId",
                schema: "hr",
                table: "Positions",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_SkillCategoryId",
                schema: "hr",
                table: "Skills",
                column: "SkillCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_SkillTypeLookupValueId",
                schema: "hr",
                table: "Skills",
                column: "SkillTypeLookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConditions_WorkflowConfigId",
                schema: "hr",
                table: "WorkflowConditions",
                column: "WorkflowConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigSteps_WorkflowConfigId",
                schema: "hr",
                table: "WorkflowConfigSteps",
                column: "WorkflowConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEscalations_WorkflowConfigStepId",
                schema: "hr",
                table: "WorkflowEscalations",
                column: "WorkflowConfigStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationCompliances_Applications_ApplicationId",
                schema: "hr",
                table: "ApplicationCompliances",
                column: "ApplicationId",
                principalSchema: "hr",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDetails_Applications_ApplicationId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "ApplicationId",
                principalSchema: "hr",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDetails_Employees_AssignedHiringManagerEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "AssignedHiringManagerEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDetails_Employees_LastContactedByEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "LastContactedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDetails_Employees_RejectionSentByEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "RejectionSentByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDetails_Employees_ReviewedByEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "ReviewedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDetails_Employees_ScreeningCompletedByEmployeeId",
                schema: "hr",
                table: "ApplicationDetails",
                column: "ScreeningCompletedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Candidates_CandidateId",
                schema: "hr",
                table: "Applications",
                column: "CandidateId",
                principalSchema: "hr",
                principalTable: "Candidates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Employees_AssignedRecruiterEmployeeId",
                schema: "hr",
                table: "Applications",
                column: "AssignedRecruiterEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Employees_ConvertedToEmployeeId",
                schema: "hr",
                table: "Applications",
                column: "ConvertedToEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_JobPostingChannels_JobPostingChannelId",
                schema: "hr",
                table: "Applications",
                column: "JobPostingChannelId",
                principalSchema: "hr",
                principalTable: "JobPostingChannels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Jobs_JobId",
                schema: "hr",
                table: "Applications",
                column: "JobId",
                principalSchema: "hr",
                principalTable: "Jobs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Employees_RequestedByEmployeeId",
                schema: "hr",
                table: "ApprovalRequests",
                column: "RequestedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequestSteps_Employees_ApproverEmployeeId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "ApproverEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequestSteps_Employees_DelegatedToEmployeeId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "DelegatedToEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequestSteps_Employees_EscalatedToEmployeeId",
                schema: "hr",
                table: "ApprovalRequestSteps",
                column: "EscalatedToEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_Candidates_CandidateId",
                schema: "hr",
                table: "CallLogs",
                column: "CandidateId",
                principalSchema: "hr",
                principalTable: "Candidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_Employees_CalledByEmployeeId",
                schema: "hr",
                table: "CallLogs",
                column: "CalledByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateAddresses_Candidates_CandidateId",
                schema: "hr",
                table: "CandidateAddresses",
                column: "CandidateId",
                principalSchema: "hr",
                principalTable: "Candidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateContacts_Candidates_CandidateId",
                schema: "hr",
                table: "CandidateContacts",
                column: "CandidateId",
                principalSchema: "hr",
                principalTable: "Candidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateMediaLinks_Candidates_CandidateId",
                schema: "hr",
                table: "CandidateMediaLinks",
                column: "CandidateId",
                principalSchema: "hr",
                principalTable: "Candidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateProfiles_Candidates_CandidateId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "CandidateId",
                principalSchema: "hr",
                principalTable: "Candidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateProfiles_Candidates_MergedIntoCandidateId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "MergedIntoCandidateId",
                principalSchema: "hr",
                principalTable: "Candidates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateProfiles_Employees_BlacklistRemovedByEmployeeId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "BlacklistRemovedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateProfiles_Employees_TalentPoolAddedByEmployeeId",
                schema: "hr",
                table: "CandidateProfiles",
                column: "TalentPoolAddedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Designations_CurrentDesignationId",
                schema: "hr",
                table: "Candidates",
                column: "CurrentDesignationId",
                principalSchema: "hr",
                principalTable: "Designations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Employees_BlacklistedByEmployeeId",
                schema: "hr",
                table: "Candidates",
                column: "BlacklistedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateSkills_Employees_VerifiedByEmployeeId",
                schema: "hr",
                table: "CandidateSkills",
                column: "VerifiedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateStageHistories_Employees_ChangedByEmployeeId",
                schema: "hr",
                table: "CandidateStageHistories",
                column: "ChangedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateTaskEvaluations_CandidateTaskSubmissions_SubmissionId",
                schema: "hr",
                table: "CandidateTaskEvaluations",
                column: "SubmissionId",
                principalSchema: "hr",
                principalTable: "CandidateTaskSubmissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateTaskEvaluations_CandidateTasks_CandidateTaskId",
                schema: "hr",
                table: "CandidateTaskEvaluations",
                column: "CandidateTaskId",
                principalSchema: "hr",
                principalTable: "CandidateTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateTaskEvaluations_Employees_EvaluatedByEmployeeId",
                schema: "hr",
                table: "CandidateTaskEvaluations",
                column: "EvaluatedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateTasks_Employees_AssignedByEmployeeId",
                schema: "hr",
                table: "CandidateTasks",
                column: "AssignedByEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateTasks_Jobs_JobId",
                schema: "hr",
                table: "CandidateTasks",
                column: "JobId",
                principalSchema: "hr",
                principalTable: "Jobs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CostCenters_Departments_DepartmentId",
                schema: "hr",
                table: "CostCenters",
                column: "DepartmentId",
                principalSchema: "hr",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CostCenters_Employees_BudgetOwnerEmployeeId",
                schema: "hr",
                table: "CostCenters",
                column: "BudgetOwnerEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_DepartmentHeadEmployeeId",
                schema: "hr",
                table: "Departments",
                column: "DepartmentHeadEmployeeId",
                principalSchema: "hr",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostCenters_Employees_BudgetOwnerEmployeeId",
                schema: "hr",
                table: "CostCenters");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_DepartmentHeadEmployeeId",
                schema: "hr",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_CostCenters_Departments_DepartmentId",
                schema: "hr",
                table: "CostCenters");

            migrationBuilder.DropTable(
                name: "ApplicationCompliances",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "ApplicationDetails",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "ApprovalRequestSteps",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CallLogs",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateAddresses",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateContacts",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateMediaLinks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateProfiles",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateSkills",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateStageHistories",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateTaskEvaluations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CompetencyFrameworkItems",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "InterviewerAvailabilities",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "InterviewFeedbacks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "InterviewNotifications",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "JobDetails",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "OfferLetterDetails",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "OfferNegotiations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "OnboardingTasks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "WorkflowConditions",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "WorkflowEscalations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "TalentPools",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateTaskSubmissions",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CompetencyFrameworks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Skills",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "InterviewPanelMembers",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "ScreeningQuestionnaires",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "AllowancesProfiles",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "BenefitsPlans",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "OfferLetters",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "OnboardingTaskTemplates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "WorkflowConfigSteps",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CandidateTasks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "SkillCategories",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Interviews",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CommunicationTemplates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "InterviewFeedbackTemplates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Candidates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "JobPostingChannels",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "ChannelTemplates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Jobs",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "ApprovalRequests",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "JobTemplates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "LookupValues",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "WorkflowConfigs",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "LookupTypes",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "JobLocations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Positions",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Designations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "PayScales",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Shifts",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Grades",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "JobFamilies",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "JobFunctions",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CostCenters",
                schema: "hr");
        }
    }
}
