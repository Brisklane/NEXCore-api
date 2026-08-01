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
                name: "allowances_profiles",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowances_profile_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    allowances_profile_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allowances_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "benefits_plans",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    benefits_plan_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    benefits_plan_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benefits_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grades",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    grade_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    level_no = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grades", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_families",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_family_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    job_family_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_families", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_functions",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_function_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    job_function_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_functions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_locations",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    location_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    state_province = table.Column<string>(type: "text", nullable: true),
                    postal_code = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_types",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    module_name = table.Column<string>(type: "text", nullable: true),
                    entity_name = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lookup_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pay_scales",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pay_scale_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pay_scale_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: true),
                    min_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pay_scales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "screening_questionnaires",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    questionnaire_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    questionnaire_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    questions_json = table.Column<string>(type: "text", nullable: false),
                    version_no = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_screening_questionnaires", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shift_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    time_zone = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shifts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_categories",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "talent_pools",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    talent_pool_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    talent_pool_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    criteria_json = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_talent_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_configs",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_code = table.Column<string>(type: "text", nullable: false),
                    module = table.Column<string>(type: "text", nullable: false),
                    transaction_type = table.Column<string>(type: "text", nullable: false),
                    workflow_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    total_levels = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version_no = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "competency_frameworks",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competency_frameworks", x => x.id);
                    table.ForeignKey(
                        name: "fk_competency_frameworks_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalSchema: "hr",
                        principalTable: "job_families",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interview_feedback_templates",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "text", nullable: false),
                    template_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    interview_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    questions_json = table.Column<string>(type: "text", nullable: false),
                    rating_scale = table.Column<string>(type: "text", nullable: false),
                    version_no = table.Column<int>(type: "integer", nullable: false),
                    competency_framework_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    passing_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    weight_in_overall_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    instructions_for_interviewer = table.Column<string>(type: "text", nullable: true),
                    instructions_for_candidate = table.Column<string>(type: "text", nullable: true),
                    skill_criteria_json = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_feedback_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_interview_feedback_templates_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalSchema: "hr",
                        principalTable: "job_families",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lookup_values",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lookup_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_terminal = table.Column<bool>(type: "boolean", nullable: false),
                    parent_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata_json = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lookup_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_lookup_values_lookup_types_lookup_type_id",
                        column: x => x.lookup_type_id,
                        principalSchema: "hr",
                        principalTable: "lookup_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lookup_values_lookup_values_parent_lookup_value_id",
                        column: x => x.parent_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "workflow_conditions",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "text", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "text", nullable: false),
                    field_value = table.Column<string>(type: "text", nullable: false),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    action_value = table.Column<string>(type: "text", nullable: false),
                    logical_group = table.Column<int>(type: "integer", nullable: false),
                    join_operator = table.Column<string>(type: "text", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_conditions", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_conditions_workflow_configs_workflow_config_id",
                        column: x => x.workflow_config_id,
                        principalSchema: "hr",
                        principalTable: "workflow_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_config_steps",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level_no = table.Column<int>(type: "integer", nullable: false),
                    approver_type = table.Column<string>(type: "text", nullable: false),
                    approver_value = table.Column<string>(type: "text", nullable: true),
                    mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    sla_hours = table.Column<int>(type: "integer", nullable: false),
                    execution_type = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_conditional = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_config_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_config_steps_workflow_configs_workflow_config_id",
                        column: x => x.workflow_config_id,
                        principalSchema: "hr",
                        principalTable: "workflow_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "channel_templates",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_code = table.Column<string>(type: "text", nullable: false),
                    channel_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    channel_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supports_auto_posting = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    api_endpoint = table.Column<string>(type: "text", nullable: true),
                    auth_config_json = table.Column<string>(type: "text", nullable: true),
                    tracking_prefix = table.Column<string>(type: "text", nullable: true),
                    default_status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_channel_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_channel_templates_lookup_values_channel_type_lookup_value_id",
                        column: x => x.channel_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_channel_templates_lookup_values_default_status_lookup_value_",
                        column: x => x.default_status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "communication_templates",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    template_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    template_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    placeholders_json = table.Column<string>(type: "text", nullable: true),
                    language_code = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_system_template = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_communication_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_communication_templates_lookup_values_template_type_lookup_v",
                        column: x => x.template_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    skill_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_core_skill = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skills", x => x.id);
                    table.ForeignKey(
                        name: "fk_skills_lookup_values_skill_type_lookup_value_id",
                        column: x => x.skill_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_skills_skill_categories_skill_category_id",
                        column: x => x.skill_category_id,
                        principalSchema: "hr",
                        principalTable: "skill_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "workflow_escalations",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_config_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    after_hours = table.Column<int>(type: "integer", nullable: false),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    action_target = table.Column<string>(type: "text", nullable: false),
                    reminder_count = table.Column<int>(type: "integer", nullable: false),
                    auto_approve_flag = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_escalations", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_escalations_workflow_config_steps_workflow_config_",
                        column: x => x.workflow_config_step_id,
                        principalSchema: "hr",
                        principalTable: "workflow_config_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competency_framework_items",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_framework_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    minimum_rating = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competency_framework_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_competency_framework_items_competency_frameworks_competency",
                        column: x => x.competency_framework_id,
                        principalSchema: "hr",
                        principalTable: "competency_frameworks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_competency_framework_items_skills_skill_id",
                        column: x => x.skill_id,
                        principalSchema: "hr",
                        principalTable: "skills",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "application_compliances",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    eeo_data_captured = table.Column<bool>(type: "boolean", nullable: false),
                    background_check_status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    background_check_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    drug_test_status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    drug_test_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    right_to_work_verified = table.Column<bool>(type: "boolean", nullable: false),
                    right_to_work_document_url = table.Column<string>(type: "text", nullable: true),
                    visa_sponsorship_required = table.Column<bool>(type: "boolean", nullable: false),
                    data_consent_given = table.Column<bool>(type: "boolean", nullable: false),
                    data_consent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_retention_expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_compliances", x => x.id);
                    table.ForeignKey(
                        name: "fk_application_compliances_lookup_values_background_check_statu",
                        column: x => x.background_check_status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_application_compliances_lookup_values_drug_test_status_looku",
                        column: x => x.drug_test_status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "application_details",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cover_letter_url = table.Column<string>(type: "text", nullable: true),
                    min_qualifications_met = table.Column<string>(type: "text", nullable: true),
                    overall_rating = table.Column<string>(type: "text", nullable: true),
                    resume_parse_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    screening_questionnaire_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    screening_questionnaire_id = table.Column<Guid>(type: "uuid", nullable: true),
                    screening_status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    screening_completed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    screening_completed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_hiring_manager_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hiring_committee_notes = table.Column<string>(type: "text", nullable: true),
                    recruiter_notes = table.Column<string>(type: "text", nullable: true),
                    application_portal_notes = table.Column<string>(type: "text", nullable: true),
                    tags_list = table.Column<string>(type: "text", nullable: true),
                    application_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_follow_up_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    candidate_response_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_contacted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_contacted_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    candidate_satisfaction_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    rejection_reason_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    withdrawal_reason_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_sent_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_notification_sent = table.Column<bool>(type: "boolean", nullable: false),
                    rejected_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    withdrawn_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expected_join_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_join_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    legacy_application_id = table.Column<string>(type: "text", nullable: true),
                    legacy_source_system = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_application_details_lookup_values_rejection_reason_lookup_va",
                        column: x => x.rejection_reason_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_application_details_lookup_values_screening_status_lookup_va",
                        column: x => x.screening_status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_application_details_lookup_values_withdrawal_reason_lookup_v",
                        column: x => x.withdrawal_reason_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_application_details_screening_questionnaires_screening_quest",
                        column: x => x.screening_questionnaire_id,
                        principalSchema: "hr",
                        principalTable: "screening_questionnaires",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "applications",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_posting_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_stage_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_shortlisted = table.Column<bool>(type: "boolean", nullable: false),
                    screening_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    internal_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    assigned_recruiter_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    converted_to_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hired_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    stage_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_applications", x => x.id);
                    table.ForeignKey(
                        name: "fk_applications_lookup_values_current_stage_lookup_value_id",
                        column: x => x.current_stage_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_applications_lookup_values_priority_lookup_value_id",
                        column: x => x.priority_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_applications_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "approval_request_steps",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_config_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_level = table.Column<int>(type: "integer", nullable: false),
                    approver_type = table.Column<string>(type: "text", nullable: false),
                    approver_value = table.Column<string>(type: "text", nullable: true),
                    approver_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    sla_hours = table.Column<int>(type: "integer", nullable: false),
                    execution_type = table.Column<string>(type: "text", nullable: false),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    rejection_category = table.Column<string>(type: "text", nullable: true),
                    action_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delegated_to_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_conditional = table.Column<bool>(type: "boolean", nullable: false),
                    condition_notes = table.Column<string>(type: "text", nullable: true),
                    escalated_flag = table.Column<bool>(type: "boolean", nullable: false),
                    escalated_to_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_request_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_request_steps_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_approval_request_steps_workflow_config_steps_workfl_a435c2a4",
                        column: x => x.workflow_config_step_id,
                        principalSchema: "hr",
                        principalTable: "workflow_config_steps",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "approval_requests",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_request_code = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_level = table.Column<int>(type: "integer", nullable: false),
                    total_levels = table.Column<int>(type: "integer", nullable: false),
                    overall_status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true),
                    reference_notes = table.Column<string>(type: "text", nullable: true),
                    approval_subject_code = table.Column<string>(type: "text", nullable: true),
                    approval_subject_title = table.Column<string>(type: "text", nullable: true),
                    approval_summary = table.Column<string>(type: "text", nullable: true),
                    approval_display_name = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_requests_lookup_values_overall_status_lookup_value_",
                        column: x => x.overall_status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_approval_requests_lookup_values_priority_lookup_value_id",
                        column: x => x.priority_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_approval_requests_workflow_configs_workflow_config_id",
                        column: x => x.workflow_config_id,
                        principalSchema: "hr",
                        principalTable: "workflow_configs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "call_logs",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: true),
                    called_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    call_type = table.Column<string>(type: "text", nullable: false),
                    call_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    outcome = table.Column<string>(type: "text", nullable: true),
                    next_action_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_action_type = table.Column<string>(type: "text", nullable: true),
                    contact_method = table.Column<string>(type: "text", nullable: true),
                    phone_number_used = table.Column<string>(type: "text", nullable: true),
                    call_direction = table.Column<string>(type: "text", nullable: true),
                    recording_url = table.Column<string>(type: "text", nullable: true),
                    transcript_url = table.Column<string>(type: "text", nullable: true),
                    telephony_session_id = table.Column<string>(type: "text", nullable: true),
                    telephony_provider = table.Column<string>(type: "text", nullable: true),
                    sentiment_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_follow_up_done = table.Column<bool>(type: "boolean", nullable: false),
                    follow_up_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    communication_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_call_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_call_logs_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "hr",
                        principalTable: "applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_call_logs_communication_templates_communication_template_id",
                        column: x => x.communication_template_id,
                        principalSchema: "hr",
                        principalTable: "communication_templates",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_addresses",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line1 = table.Column<string>(type: "text", nullable: false),
                    line2 = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    state_province = table.Column<string>(type: "text", nullable: true),
                    postal_code = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_addresses_lookup_values_address_type_lookup_value_",
                        column: x => x.address_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_contacts",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_value = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_contacts_lookup_values_contact_type_looku_ba8c8bc4",
                        column: x => x.contact_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_media_links",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_media_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_media_links_lookup_values_media_type_lookup_value_",
                        column: x => x.media_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_profiles",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    middle_name = table.Column<string>(type: "text", nullable: true),
                    alternate_email = table.Column<string>(type: "text", nullable: true),
                    mobile_phone = table.Column<string>(type: "text", nullable: true),
                    current_company = table.Column<string>(type: "text", nullable: true),
                    current_designation = table.Column<string>(type: "text", nullable: true),
                    referred_by = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: true),
                    nationality = table.Column<string>(type: "text", nullable: true),
                    country_of_residence = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    current_notice_period_days = table.Column<int>(type: "integer", nullable: true),
                    available_from_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    preferred_work_location = table.Column<string>(type: "text", nullable: true),
                    remote_preference = table.Column<string>(type: "text", nullable: true),
                    highest_education_level = table.Column<string>(type: "text", nullable: true),
                    highest_education_field = table.Column<string>(type: "text", nullable: true),
                    portal_registered = table.Column<bool>(type: "boolean", nullable: false),
                    portal_registration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_portal_login_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    profile_completion_percent = table.Column<int>(type: "integer", nullable: true),
                    consent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    talent_pool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    talent_pool_added_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    talent_pool_added_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    diversity_data = table.Column<string>(type: "text", nullable: true),
                    eeo_category = table.Column<string>(type: "text", nullable: true),
                    veteran_status = table.Column<string>(type: "text", nullable: true),
                    disability_status = table.Column<string>(type: "text", nullable: true),
                    recruiter_notes = table.Column<string>(type: "text", nullable: true),
                    blacklist_notes = table.Column<string>(type: "text", nullable: true),
                    blacklist_removed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blacklist_removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    blacklist_removal_reason = table.Column<string>(type: "text", nullable: true),
                    merged_into_candidate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_profiles_talent_pools_talent_pool_id",
                        column: x => x.talent_pool_id,
                        principalSchema: "hr",
                        principalTable: "talent_pools",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_skills",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proficiency_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    years_experience = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_skills", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_skills_lookup_values_proficiency_lookup_value_id",
                        column: x => x.proficiency_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_candidate_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalSchema: "hr",
                        principalTable: "skills",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_stage_histories",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_stage_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_stage_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    days_in_previous_stage = table.Column<int>(type: "integer", nullable: true),
                    is_automated = table.Column<bool>(type: "boolean", nullable: false),
                    trigger_event = table.Column<string>(type: "text", nullable: true),
                    sla_breached = table.Column<bool>(type: "boolean", nullable: false),
                    sla_breach_hours = table.Column<int>(type: "integer", nullable: true),
                    reason_code = table.Column<string>(type: "text", nullable: true),
                    notification_sent = table.Column<bool>(type: "boolean", nullable: false),
                    notification_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_stage_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_stage_histories_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "hr",
                        principalTable: "applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_candidate_stage_histories_lookup_values_from_stage_lookup_va",
                        column: x => x.from_stage_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_candidate_stage_histories_lookup_values_to_stage_lookup_valu",
                        column: x => x.to_stage_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_task_evaluations",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluated_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    result_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    technical_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    quality_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    creativity_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    communication_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    weighted_final_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    recommendation = table.Column<string>(type: "text", nullable: true),
                    feedback_visible_to_candidate = table.Column<bool>(type: "boolean", nullable: false),
                    reviewed_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_task_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_task_evaluations_lookup_values_result_lookup_value",
                        column: x => x.result_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_task_submissions",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    submission_text = table.Column<string>(type: "text", nullable: true),
                    external_link = table.Column<string>(type: "text", nullable: true),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_no = table.Column<int>(type: "integer", nullable: false),
                    is_late_submission = table.Column<bool>(type: "boolean", nullable: false),
                    time_spent_minutes = table.Column<int>(type: "integer", nullable: true),
                    browser_metadata = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    integrity_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    auto_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    parsed_output = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_task_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_task_submissions_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "hr",
                        principalTable: "applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_candidate_task_submissions_lookup_values_status_lookup_value",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidate_tasks",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_code = table.Column<string>(type: "text", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "text", nullable: false),
                    external_provider = table.Column<string>(type: "text", nullable: true),
                    provider_reference_id = table.Column<string>(type: "text", nullable: true),
                    assigned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_method = table.Column<string>(type: "text", nullable: true),
                    max_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    passing_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    attempt_allowed = table.Column<int>(type: "integer", nullable: false),
                    reminder_sent_count = table.Column<int>(type: "integer", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_tasks_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "hr",
                        principalTable: "applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_candidate_tasks_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_candidate_tasks_lookup_values_task_type_lookup_value_id",
                        column: x => x.task_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "candidates",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    resume_url = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    total_experience_years = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    current_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    expected_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    candidate_rating = table.Column<string>(type: "text", nullable: true),
                    is_blacklisted = table.Column<bool>(type: "boolean", nullable: false),
                    blacklist_reason_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blacklisted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    blacklisted_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consent_given = table.Column<bool>(type: "boolean", nullable: false),
                    data_retention_expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duplicate_of_candidate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    merged_into_candidate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    legacy_candidate_id = table.Column<string>(type: "text", nullable: true),
                    legacy_source_system = table.Column<string>(type: "text", nullable: true),
                    current_company = table.Column<string>(type: "text", nullable: true),
                    current_designation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidates", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidates_candidates_duplicate_of_candidate_id",
                        column: x => x.duplicate_of_candidate_id,
                        principalSchema: "hr",
                        principalTable: "candidates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_candidates_candidates_merged_into_candidate_id",
                        column: x => x.merged_into_candidate_id,
                        principalSchema: "hr",
                        principalTable: "candidates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_candidates_lookup_values_blacklist_reason_lookup_value_id",
                        column: x => x.blacklist_reason_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cost_centers",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost_center_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cost_center_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    budget_owner_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cost_centers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    department_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_head_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                    table.ForeignKey(
                        name: "fk_departments_cost_centers_cost_center_id",
                        column: x => x.cost_center_id,
                        principalSchema: "hr",
                        principalTable: "cost_centers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_departments_departments_parent_department_id",
                        column: x => x.parent_department_id,
                        principalSchema: "hr",
                        principalTable: "departments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "designations",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    designation_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    designation_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_function_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_designations", x => x.id);
                    table.ForeignKey(
                        name: "fk_designations_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "hr",
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_designations_grades_grade_id",
                        column: x => x.grade_id,
                        principalSchema: "hr",
                        principalTable: "grades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_designations_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalSchema: "hr",
                        principalTable: "job_families",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_designations_job_functions_job_function_id",
                        column: x => x.job_function_id,
                        principalSchema: "hr",
                        principalTable: "job_functions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "onboarding_task_templates",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "text", nullable: false),
                    task_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    task_category = table.Column<string>(type: "text", nullable: false),
                    default_assignee_role = table.Column<string>(type: "text", nullable: false),
                    default_due_days_from_start = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    applicable_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applicable_designation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applicable_employment_type = table.Column<string>(type: "text", nullable: true),
                    applicable_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    depends_on_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estimated_hours = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    requires_document_upload = table.Column<bool>(type: "boolean", nullable: false),
                    requires_manager_signoff = table.Column<bool>(type: "boolean", nullable: false),
                    notify_employee_on_assign = table.Column<bool>(type: "boolean", nullable: false),
                    notify_assignee_on_create = table.Column<bool>(type: "boolean", nullable: false),
                    escalate_after_days = table.Column<int>(type: "integer", nullable: true),
                    escalate_to_role = table.Column<string>(type: "text", nullable: true),
                    legacy_template_id = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_task_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_task_templates_departments_applicable_department",
                        column: x => x.applicable_department_id,
                        principalSchema: "hr",
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_task_templates_designations_applicable_designati",
                        column: x => x.applicable_designation_id,
                        principalSchema: "hr",
                        principalTable: "designations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_task_templates_job_locations_applicable_location",
                        column: x => x.applicable_location_id,
                        principalSchema: "hr",
                        principalTable: "job_locations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_task_templates_onboarding_task_templates_depends",
                        column: x => x.depends_on_template_id,
                        principalSchema: "hr",
                        principalTable: "onboarding_task_templates",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "positions",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_function_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pay_scale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reports_to_position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_vacant = table.Column<bool>(type: "boolean", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_positions_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "hr",
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_designations_designation_id",
                        column: x => x.designation_id,
                        principalSchema: "hr",
                        principalTable: "designations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_grades_grade_id",
                        column: x => x.grade_id,
                        principalSchema: "hr",
                        principalTable: "grades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalSchema: "hr",
                        principalTable: "job_families",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_job_functions_job_function_id",
                        column: x => x.job_function_id,
                        principalSchema: "hr",
                        principalTable: "job_functions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_pay_scales_pay_scale_id",
                        column: x => x.pay_scale_id,
                        principalSchema: "hr",
                        principalTable: "pay_scales",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_positions_reports_to_position_id",
                        column: x => x.reports_to_position_id,
                        principalSchema: "hr",
                        principalTable: "positions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_positions_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "hr",
                        principalTable: "shifts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporting_manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    join_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_employees_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "hr",
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_employees_designations_designation_id",
                        column: x => x.designation_id,
                        principalSchema: "hr",
                        principalTable: "designations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_employees_employees_reporting_manager_id",
                        column: x => x.reporting_manager_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_employees_job_locations_job_location_id",
                        column: x => x.job_location_id,
                        principalSchema: "hr",
                        principalTable: "job_locations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_employees_positions_position_id",
                        column: x => x.position_id,
                        principalSchema: "hr",
                        principalTable: "positions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_employees_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "hr",
                        principalTable: "shifts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_templates",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "text", nullable: false),
                    template_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    job_title = table.Column<string>(type: "text", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employment_type = table.Column<string>(type: "text", nullable: false),
                    requirements = table.Column<string>(type: "text", nullable: true),
                    required_skills = table.Column<string>(type: "text", nullable: true),
                    responsibilities = table.Column<string>(type: "text", nullable: true),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_function_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    worker_category = table.Column<string>(type: "text", nullable: true),
                    remote_type = table.Column<string>(type: "text", nullable: true),
                    min_experience_years = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_experience_years = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    education_requirements = table.Column<string>(type: "text", nullable: true),
                    preferred_skills = table.Column<string>(type: "text", nullable: true),
                    languages_required = table.Column<string>(type: "text", nullable: true),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    parent_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    legacy_template_id = table.Column<string>(type: "text", nullable: true),
                    legacy_source_system = table.Column<string>(type: "text", nullable: true),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_templates_designations_designation_id",
                        column: x => x.designation_id,
                        principalSchema: "hr",
                        principalTable: "designations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_templates_grades_grade_id",
                        column: x => x.grade_id,
                        principalSchema: "hr",
                        principalTable: "grades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_templates_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalSchema: "hr",
                        principalTable: "job_families",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_templates_job_functions_job_function_id",
                        column: x => x.job_function_id,
                        principalSchema: "hr",
                        principalTable: "job_functions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_templates_positions_position_id",
                        column: x => x.position_id,
                        principalSchema: "hr",
                        principalTable: "positions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_templates_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "hr",
                        principalTable: "shifts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "onboarding_tasks",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_code = table.Column<string>(type: "text", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    onboarding_task_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    task_name = table.Column<string>(type: "text", nullable: false),
                    task_category = table.Column<string>(type: "text", nullable: false),
                    task_description = table.Column<string>(type: "text", nullable: true),
                    assigned_to_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_auto_generated = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    depends_on_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    document_url = table.Column<string>(type: "text", nullable: true),
                    requires_document_upload = table.Column<bool>(type: "boolean", nullable: false),
                    requires_manager_sign_off = table.Column<bool>(type: "boolean", nullable: false),
                    manager_sign_off_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    escalated_flag = table.Column<bool>(type: "boolean", nullable: false),
                    escalated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    escalated_to_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reminder_sent_count = table.Column<int>(type: "integer", nullable: false),
                    last_reminder_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sla_breached = table.Column<bool>(type: "boolean", nullable: false),
                    sla_breach_hours = table.Column<int>(type: "integer", nullable: true),
                    blocked_reason = table.Column<string>(type: "text", nullable: true),
                    cancelled_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_onboarding_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "hr",
                        principalTable: "applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalSchema: "hr",
                        principalTable: "candidates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_employees_assigned_to_employee_id",
                        column: x => x.assigned_to_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_employees_cancelled_by_employee_id",
                        column: x => x.cancelled_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_employees_completed_by_employee_id",
                        column: x => x.completed_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_employees_escalated_to_employee_id",
                        column: x => x.escalated_to_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_employees_verified_by_employee_id",
                        column: x => x.verified_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_onboarding_task_templates_onboardi_73068155",
                        column: x => x.onboarding_task_template_id,
                        principalSchema: "hr",
                        principalTable: "onboarding_task_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_onboarding_tasks_onboarding_tasks_depends_on_task_id",
                        column: x => x.depends_on_task_id,
                        principalSchema: "hr",
                        principalTable: "onboarding_tasks",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    job_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    record_type = table.Column<int>(type: "integer", nullable: false),
                    parent_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    headcount = table.Column<int>(type: "integer", nullable: false),
                    filled_count = table.Column<int>(type: "integer", nullable: false),
                    employment_type = table.Column<int>(type: "integer", nullable: false),
                    salary_range_min = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    salary_range_max = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    target_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    hiring_manager_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recruiter_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posting_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posting_close_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    job_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_jobs_approval_requests_approval_request_id",
                        column: x => x.approval_request_id,
                        principalSchema: "hr",
                        principalTable: "approval_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "hr",
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_designations_designation_id",
                        column: x => x.designation_id,
                        principalSchema: "hr",
                        principalTable: "designations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_employees_closed_by_employee_id",
                        column: x => x.closed_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_employees_hiring_manager_employee_id",
                        column: x => x.hiring_manager_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_employees_recruiter_employee_id",
                        column: x => x.recruiter_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_job_templates_job_template_id",
                        column: x => x.job_template_id,
                        principalSchema: "hr",
                        principalTable: "job_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_jobs_parent_job_id",
                        column: x => x.parent_job_id,
                        principalSchema: "hr",
                        principalTable: "jobs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_lookup_values_priority_lookup_value_id",
                        column: x => x.priority_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_jobs_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interviews",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    interview_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_sequence_no = table.Column<int>(type: "integer", nullable: true),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_feedback_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scheduled_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    format = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: true),
                    video_link = table.Column<string>(type: "text", nullable: true),
                    calendar_provider = table.Column<string>(type: "text", nullable: true),
                    calendar_event_id = table.Column<string>(type: "text", nullable: true),
                    proposed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    proposed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    candidate_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    candidate_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    original_scheduled_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reschedule_count = table.Column<int>(type: "integer", nullable: false),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    actual_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    decision_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    weighted_panel_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    feedback_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    feedback_submitted_count = table.Column<int>(type: "integer", nullable: false),
                    sla_breached = table.Column<bool>(type: "boolean", nullable: false),
                    recording_url = table.Column<string>(type: "text", nullable: true),
                    is_mandatory_round = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_interviews_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "hr",
                        principalTable: "applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interviews_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalSchema: "hr",
                        principalTable: "candidates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interviews_employees_proposed_by_employee_id",
                        column: x => x.proposed_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interviews_interview_feedback_templates_interview_f_e223e41f",
                        column: x => x.interview_feedback_template_id,
                        principalSchema: "hr",
                        principalTable: "interview_feedback_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interviews_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "hr",
                        principalTable: "jobs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interviews_lookup_values_decision_lookup_value_id",
                        column: x => x.decision_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interviews_lookup_values_interview_type_lookup_value_id",
                        column: x => x.interview_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interviews_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_details",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_family_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_function_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pay_scale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    replaced_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporting_manager_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    worker_category = table.Column<string>(type: "text", nullable: true),
                    hiring_type = table.Column<string>(type: "text", nullable: true),
                    vacancy_reason = table.Column<string>(type: "text", nullable: true),
                    requirements = table.Column<string>(type: "text", nullable: true),
                    required_skills = table.Column<string>(type: "text", nullable: true),
                    preferred_skills = table.Column<string>(type: "text", nullable: true),
                    responsibilities = table.Column<string>(type: "text", nullable: true),
                    education_requirements = table.Column<string>(type: "text", nullable: true),
                    languages_required = table.Column<string>(type: "text", nullable: true),
                    min_experience_years = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_experience_years = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    bonus_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    allowances_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    budget_approved_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    forecasted_hire_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    publish_externally_flag = table.Column<bool>(type: "boolean", nullable: false),
                    career_site_visible = table.Column<bool>(type: "boolean", nullable: false),
                    internal_only_flag = table.Column<bool>(type: "boolean", nullable: false),
                    background_check_required = table.Column<bool>(type: "boolean", nullable: false),
                    drug_test_required = table.Column<bool>(type: "boolean", nullable: false),
                    security_clearance_level = table.Column<string>(type: "text", nullable: true),
                    visa_sponsorship_available = table.Column<bool>(type: "boolean", nullable: false),
                    confidential_job_flag = table.Column<bool>(type: "boolean", nullable: false),
                    diversity_target_flag = table.Column<bool>(type: "boolean", nullable: false),
                    eeo_category = table.Column<string>(type: "text", nullable: true),
                    union_role_flag = table.Column<bool>(type: "boolean", nullable: false),
                    travel_required_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    source_campaign_code = table.Column<string>(type: "text", nullable: true),
                    referral_bonus_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    tags = table.Column<string>(type: "text", nullable: true),
                    internal_job_title = table.Column<string>(type: "text", nullable: true),
                    external_job_code = table.Column<string>(type: "text", nullable: true),
                    legacy_source_system = table.Column<string>(type: "text", nullable: true),
                    legacy_record_id = table.Column<string>(type: "text", nullable: true),
                    screening_questionnaire_id = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_details_allowances_profiles_allowances_profile_id",
                        column: x => x.allowances_profile_id,
                        principalSchema: "hr",
                        principalTable: "allowances_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_cost_centers_cost_center_id",
                        column: x => x.cost_center_id,
                        principalSchema: "hr",
                        principalTable: "cost_centers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_employees_closed_by_employee_id",
                        column: x => x.closed_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_employees_replaced_employee_id",
                        column: x => x.replaced_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_employees_reporting_manager_employee_id",
                        column: x => x.reporting_manager_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_grades_grade_id",
                        column: x => x.grade_id,
                        principalSchema: "hr",
                        principalTable: "grades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_job_families_job_family_id",
                        column: x => x.job_family_id,
                        principalSchema: "hr",
                        principalTable: "job_families",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_job_functions_job_function_id",
                        column: x => x.job_function_id,
                        principalSchema: "hr",
                        principalTable: "job_functions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_job_locations_job_location_id",
                        column: x => x.job_location_id,
                        principalSchema: "hr",
                        principalTable: "job_locations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_job_templates_job_template_id",
                        column: x => x.job_template_id,
                        principalSchema: "hr",
                        principalTable: "job_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "hr",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_job_details_pay_scales_pay_scale_id",
                        column: x => x.pay_scale_id,
                        principalSchema: "hr",
                        principalTable: "pay_scales",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_positions_position_id",
                        column: x => x.position_id,
                        principalSchema: "hr",
                        principalTable: "positions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_screening_questionnaires_screening_questionnaire",
                        column: x => x.screening_questionnaire_id,
                        principalSchema: "hr",
                        principalTable: "screening_questionnaires",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_details_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "hr",
                        principalTable: "shifts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_posting_channels",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_name_snapshot = table.Column<string>(type: "text", nullable: false),
                    channel_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_tracking_code = table.Column<string>(type: "text", nullable: false),
                    posting_url = table.Column<string>(type: "text", nullable: true),
                    open_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    close_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applications_received = table.Column<int>(type: "integer", nullable: false),
                    is_sponsored = table.Column<bool>(type: "boolean", nullable: false),
                    sponsored_budget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sponsored_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sponsored_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    agency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agency_fee_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    click_count = table.Column<int>(type: "integer", nullable: false),
                    conversion_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    shortlisted_count = table.Column<int>(type: "integer", nullable: false),
                    hired_count = table.Column<int>(type: "integer", nullable: false),
                    cost_per_application = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    cost_per_hire = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    quality_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    external_posting_id = table.Column<string>(type: "text", nullable: true),
                    legacy_source_system = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_posting_channels", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_posting_channels_channel_templates_channel_template_id",
                        column: x => x.channel_template_id,
                        principalSchema: "hr",
                        principalTable: "channel_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_posting_channels_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "hr",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_job_posting_channels_lookup_values_channel_type_lookup_value",
                        column: x => x.channel_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_posting_channels_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "offer_letters",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reporting_manager_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_package = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    employment_type = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    probation_period_months = table.Column<int>(type: "integer", nullable: false),
                    notice_period_days = table.Column<int>(type: "integer", nullable: false),
                    approval_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    candidate_response_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_via = table.Column<string>(type: "text", nullable: true),
                    response_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoke_reason = table.Column<string>(type: "text", nullable: true),
                    communication_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_letters", x => x.id);
                    table.ForeignKey(
                        name: "fk_offer_letters_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "hr",
                        principalTable: "applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_approval_requests_approval_request_id",
                        column: x => x.approval_request_id,
                        principalSchema: "hr",
                        principalTable: "approval_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalSchema: "hr",
                        principalTable: "candidates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_communication_templates_communication_templat",
                        column: x => x.communication_template_id,
                        principalSchema: "hr",
                        principalTable: "communication_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_employees_reporting_manager_employee_id",
                        column: x => x.reporting_manager_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_employees_revoked_by_employee_id",
                        column: x => x.revoked_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_employees_sent_by_employee_id",
                        column: x => x.sent_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "hr",
                        principalTable: "jobs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_lookup_values_candidate_response_lookup_value",
                        column: x => x.candidate_response_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letters_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interview_panel_members",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interviewer_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alternate_interviewer_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    score_weight = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_lead = table.Column<bool>(type: "boolean", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    invite_status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invite_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    response_comments = table.Column<string>(type: "text", nullable: true),
                    has_conflict_of_interest = table.Column<bool>(type: "boolean", nullable: false),
                    conflict_of_interest_notes = table.Column<string>(type: "text", nullable: true),
                    is_availability_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    feedback_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reminder_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    declined_to_participate = table.Column<bool>(type: "boolean", nullable: false),
                    decline_reason = table.Column<string>(type: "text", nullable: true),
                    attendance_status = table.Column<string>(type: "text", nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    panel_sequence = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_panel_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_interview_panel_members_employees_alternate_interviewer_emp",
                        column: x => x.alternate_interviewer_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_panel_members_employees_interviewer_employee_id",
                        column: x => x.interviewer_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_panel_members_interviews_interview_id",
                        column: x => x.interview_id,
                        principalSchema: "hr",
                        principalTable: "interviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interview_panel_members_lookup_values_invite_status_lookup_v",
                        column: x => x.invite_status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interviewer_availabilities",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interviewer_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    available_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    slot_status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_id = table.Column<Guid>(type: "uuid", nullable: true),
                    time_zone = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    recurrence_pattern = table.Column<string>(type: "text", nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    reason_code = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interviewer_availabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_interviewer_availabilities_employees_interviewer_employee_id",
                        column: x => x.interviewer_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interviewer_availabilities_interviews_interview_id",
                        column: x => x.interview_id,
                        principalSchema: "hr",
                        principalTable: "interviews",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interviewer_availabilities_lookup_values_slot_status_lookup_",
                        column: x => x.slot_status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "offer_letter_details",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_letter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowances_breakdown = table.Column<string>(type: "text", nullable: true),
                    housing_allowance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    transport_allowance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    medical_allowance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    other_allowances = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    bonus_target = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    bonus_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    equity_grant = table.Column<string>(type: "text", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pay_scale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    benefits_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allowances_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    benefits_summary = table.Column<string>(type: "text", nullable: true),
                    terms_document_url = table.Column<string>(type: "text", nullable: true),
                    counter_signed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    counter_signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    digital_signature_url = table.Column<string>(type: "text", nullable: true),
                    is_digitally_signed = table.Column<bool>(type: "boolean", nullable: false),
                    legacy_offer_id = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_letter_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_offer_letter_details_allowances_profiles_allowances_profile",
                        column: x => x.allowances_profile_id,
                        principalSchema: "hr",
                        principalTable: "allowances_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letter_details_benefits_plans_benefits_plan_id",
                        column: x => x.benefits_plan_id,
                        principalSchema: "hr",
                        principalTable: "benefits_plans",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letter_details_employees_counter_signed_by_employee_id",
                        column: x => x.counter_signed_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letter_details_grades_grade_id",
                        column: x => x.grade_id,
                        principalSchema: "hr",
                        principalTable: "grades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_letter_details_offer_letters_offer_letter_id",
                        column: x => x.offer_letter_id,
                        principalSchema: "hr",
                        principalTable: "offer_letters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_offer_letter_details_pay_scales_pay_scale_id",
                        column: x => x.pay_scale_id,
                        principalSchema: "hr",
                        principalTable: "pay_scales",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "offer_negotiations",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    negotiation_round = table.Column<int>(type: "integer", nullable: false),
                    initiated_by = table.Column<string>(type: "text", nullable: false),
                    negotiation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    proposed_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    proposed_terms = table.Column<string>(type: "text", nullable: true),
                    counter_offer_notes = table.Column<string>(type: "text", nullable: true),
                    status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    previous_salary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    salary_difference = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    negotiation_reason_code = table.Column<string>(type: "text", nullable: true),
                    candidate_competing_offer = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    candidate_competing_company = table.Column<string>(type: "text", nullable: true),
                    hr_recommendation = table.Column<string>(type: "text", nullable: true),
                    requires_re_approval = table.Column<bool>(type: "boolean", nullable: false),
                    re_approval_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_negotiations", x => x.id);
                    table.ForeignKey(
                        name: "fk_offer_negotiations_approval_requests_re_approval_request_id",
                        column: x => x.re_approval_request_id,
                        principalSchema: "hr",
                        principalTable: "approval_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_negotiations_employees_response_by_employee_id",
                        column: x => x.response_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_negotiations_lookup_values_status_lookup_value_id",
                        column: x => x.status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_offer_negotiations_offer_letters_offer_id",
                        column: x => x.offer_id,
                        principalSchema: "hr",
                        principalTable: "offer_letters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_feedbacks",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_id = table.Column<Guid>(type: "uuid", nullable: false),
                    panel_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    overall_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    recommendation_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    technical_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    behavioral_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    communication_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    culture_fit_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    weighted_final_score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    strength_notes = table.Column<string>(type: "text", nullable: true),
                    concern_notes = table.Column<string>(type: "text", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true),
                    competency_scores_json = table.Column<string>(type: "text", nullable: false),
                    hire_readiness = table.Column<string>(type: "text", nullable: true),
                    would_rehire = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_late = table.Column<bool>(type: "boolean", nullable: false),
                    reviewed_by_hr = table.Column<bool>(type: "boolean", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confidential_notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "fk_interview_feedbacks_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "hr",
                        principalTable: "applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_feedbacks_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalSchema: "hr",
                        principalTable: "candidates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_feedbacks_employees_submitted_by_employee_id",
                        column: x => x.submitted_by_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_feedbacks_interview_panel_members_panel_member_id",
                        column: x => x.panel_member_id,
                        principalSchema: "hr",
                        principalTable: "interview_panel_members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_feedbacks_interviews_interview_id",
                        column: x => x.interview_id,
                        principalSchema: "hr",
                        principalTable: "interviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interview_feedbacks_lookup_values_decision_lookup_value_id",
                        column: x => x.decision_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_feedbacks_lookup_values_recommendation_lookup_valu",
                        column: x => x.recommendation_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "interview_notifications",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_panel_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivery_status_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_action_lookup_value_id = table.Column<Guid>(type: "uuid", nullable: true),
                    response_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    token = table.Column<string>(type: "text", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    communication_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    is_synced = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_interview_notifications_communication_templates_communicati",
                        column: x => x.communication_template_id,
                        principalSchema: "hr",
                        principalTable: "communication_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_notifications_employees_recipient_employee_id",
                        column: x => x.recipient_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_notifications_interview_panel_members_int_636648d5",
                        column: x => x.interview_panel_member_id,
                        principalSchema: "hr",
                        principalTable: "interview_panel_members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_notifications_interviews_interview_id",
                        column: x => x.interview_id,
                        principalSchema: "hr",
                        principalTable: "interviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interview_notifications_lookup_values_delivery_status_lookup",
                        column: x => x.delivery_status_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_notifications_lookup_values_notification_type_look",
                        column: x => x.notification_type_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_interview_notifications_lookup_values_response_action_lookup",
                        column: x => x.response_action_lookup_value_id,
                        principalSchema: "hr",
                        principalTable: "lookup_values",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_application_compliances_application_id",
                schema: "hr",
                table: "application_compliances",
                column: "application_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_application_compliances_background_check_status_lookup_valu",
                schema: "hr",
                table: "application_compliances",
                column: "background_check_status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_compliances_drug_test_status_lookup_value_id",
                schema: "hr",
                table: "application_compliances",
                column: "drug_test_status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_application_id",
                schema: "hr",
                table: "application_details",
                column: "application_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_application_details_assigned_hiring_manager_employee_id",
                schema: "hr",
                table: "application_details",
                column: "assigned_hiring_manager_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_last_contacted_by_employee_id",
                schema: "hr",
                table: "application_details",
                column: "last_contacted_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_rejection_reason_lookup_value_id",
                schema: "hr",
                table: "application_details",
                column: "rejection_reason_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_rejection_sent_by_employee_id",
                schema: "hr",
                table: "application_details",
                column: "rejection_sent_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_reviewed_by_employee_id",
                schema: "hr",
                table: "application_details",
                column: "reviewed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_screening_completed_by_employee_id",
                schema: "hr",
                table: "application_details",
                column: "screening_completed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_screening_questionnaire_id",
                schema: "hr",
                table: "application_details",
                column: "screening_questionnaire_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_screening_status_lookup_value_id",
                schema: "hr",
                table: "application_details",
                column: "screening_status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_details_withdrawal_reason_lookup_value_id",
                schema: "hr",
                table: "application_details",
                column: "withdrawal_reason_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_assigned_recruiter_employee_id",
                schema: "hr",
                table: "applications",
                column: "assigned_recruiter_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_candidate_id",
                schema: "hr",
                table: "applications",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_converted_to_employee_id",
                schema: "hr",
                table: "applications",
                column: "converted_to_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_current_stage_lookup_value_id",
                schema: "hr",
                table: "applications",
                column: "current_stage_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_job_id",
                schema: "hr",
                table: "applications",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_job_posting_channel_id",
                schema: "hr",
                table: "applications",
                column: "job_posting_channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_priority_lookup_value_id",
                schema: "hr",
                table: "applications",
                column: "priority_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_status_lookup_value_id",
                schema: "hr",
                table: "applications",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_request_steps_approval_request_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "approval_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_request_steps_approver_employee_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "approver_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_request_steps_delegated_to_employee_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "delegated_to_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_request_steps_escalated_to_employee_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "escalated_to_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_request_steps_status_lookup_value_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_request_steps_workflow_config_step_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "workflow_config_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_requests_overall_status_lookup_value_id",
                schema: "hr",
                table: "approval_requests",
                column: "overall_status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_requests_priority_lookup_value_id",
                schema: "hr",
                table: "approval_requests",
                column: "priority_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_requests_requested_by_employee_id",
                schema: "hr",
                table: "approval_requests",
                column: "requested_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_requests_workflow_config_id",
                schema: "hr",
                table: "approval_requests",
                column: "workflow_config_id");

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_application_id",
                schema: "hr",
                table: "call_logs",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_called_by_employee_id",
                schema: "hr",
                table: "call_logs",
                column: "called_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_candidate_id",
                schema: "hr",
                table: "call_logs",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_communication_template_id",
                schema: "hr",
                table: "call_logs",
                column: "communication_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_addresses_address_type_lookup_value_id",
                schema: "hr",
                table: "candidate_addresses",
                column: "address_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_addresses_candidate_id",
                schema: "hr",
                table: "candidate_addresses",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_contacts_candidate_id",
                schema: "hr",
                table: "candidate_contacts",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_contacts_contact_type_lookup_value_id",
                schema: "hr",
                table: "candidate_contacts",
                column: "contact_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_media_links_candidate_id",
                schema: "hr",
                table: "candidate_media_links",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_media_links_media_type_lookup_value_id",
                schema: "hr",
                table: "candidate_media_links",
                column: "media_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_profiles_blacklist_removed_by_employee_id",
                schema: "hr",
                table: "candidate_profiles",
                column: "blacklist_removed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_profiles_candidate_id",
                schema: "hr",
                table: "candidate_profiles",
                column: "candidate_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_candidate_profiles_merged_into_candidate_id",
                schema: "hr",
                table: "candidate_profiles",
                column: "merged_into_candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_profiles_talent_pool_added_by_employee_id",
                schema: "hr",
                table: "candidate_profiles",
                column: "talent_pool_added_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_profiles_talent_pool_id",
                schema: "hr",
                table: "candidate_profiles",
                column: "talent_pool_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_skills_candidate_id",
                schema: "hr",
                table: "candidate_skills",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_skills_proficiency_lookup_value_id",
                schema: "hr",
                table: "candidate_skills",
                column: "proficiency_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_skills_skill_id",
                schema: "hr",
                table: "candidate_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_skills_verified_by_employee_id",
                schema: "hr",
                table: "candidate_skills",
                column: "verified_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_stage_histories_application_id",
                schema: "hr",
                table: "candidate_stage_histories",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_stage_histories_candidate_id",
                schema: "hr",
                table: "candidate_stage_histories",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_stage_histories_changed_by_employee_id",
                schema: "hr",
                table: "candidate_stage_histories",
                column: "changed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_stage_histories_from_stage_lookup_value_id",
                schema: "hr",
                table: "candidate_stage_histories",
                column: "from_stage_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_stage_histories_to_stage_lookup_value_id",
                schema: "hr",
                table: "candidate_stage_histories",
                column: "to_stage_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_task_evaluations_candidate_task_id",
                schema: "hr",
                table: "candidate_task_evaluations",
                column: "candidate_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_task_evaluations_evaluated_by_employee_id",
                schema: "hr",
                table: "candidate_task_evaluations",
                column: "evaluated_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_task_evaluations_result_lookup_value_id",
                schema: "hr",
                table: "candidate_task_evaluations",
                column: "result_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_task_evaluations_submission_id",
                schema: "hr",
                table: "candidate_task_evaluations",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_task_submissions_application_id",
                schema: "hr",
                table: "candidate_task_submissions",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_task_submissions_candidate_id",
                schema: "hr",
                table: "candidate_task_submissions",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_task_submissions_candidate_task_id",
                schema: "hr",
                table: "candidate_task_submissions",
                column: "candidate_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_task_submissions_status_lookup_value_id",
                schema: "hr",
                table: "candidate_task_submissions",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_tasks_application_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_tasks_assigned_by_employee_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "assigned_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_tasks_candidate_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_tasks_job_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_tasks_status_lookup_value_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_tasks_task_type_lookup_value_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "task_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidates_blacklist_reason_lookup_value_id",
                schema: "hr",
                table: "candidates",
                column: "blacklist_reason_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidates_blacklisted_by_employee_id",
                schema: "hr",
                table: "candidates",
                column: "blacklisted_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidates_current_designation_id",
                schema: "hr",
                table: "candidates",
                column: "current_designation_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidates_duplicate_of_candidate_id",
                schema: "hr",
                table: "candidates",
                column: "duplicate_of_candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_candidates_merged_into_candidate_id",
                schema: "hr",
                table: "candidates",
                column: "merged_into_candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_channel_templates_channel_type_lookup_value_id",
                schema: "hr",
                table: "channel_templates",
                column: "channel_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_channel_templates_default_status_lookup_value_id",
                schema: "hr",
                table: "channel_templates",
                column: "default_status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_communication_templates_template_code",
                schema: "hr",
                table: "communication_templates",
                column: "template_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_communication_templates_template_type_lookup_value_id",
                schema: "hr",
                table: "communication_templates",
                column: "template_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_framework_items_competency_framework_id",
                schema: "hr",
                table: "competency_framework_items",
                column: "competency_framework_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_framework_items_skill_id",
                schema: "hr",
                table: "competency_framework_items",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "ix_competency_frameworks_job_family_id",
                schema: "hr",
                table: "competency_frameworks",
                column: "job_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_budget_owner_employee_id",
                schema: "hr",
                table: "cost_centers",
                column: "budget_owner_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_department_id",
                schema: "hr",
                table: "cost_centers",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_departments_cost_center_id",
                schema: "hr",
                table: "departments",
                column: "cost_center_id");

            migrationBuilder.CreateIndex(
                name: "ix_departments_department_head_employee_id",
                schema: "hr",
                table: "departments",
                column: "department_head_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_departments_parent_department_id",
                schema: "hr",
                table: "departments",
                column: "parent_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_designations_department_id",
                schema: "hr",
                table: "designations",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_designations_grade_id",
                schema: "hr",
                table: "designations",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "ix_designations_job_family_id",
                schema: "hr",
                table: "designations",
                column: "job_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_designations_job_function_id",
                schema: "hr",
                table: "designations",
                column: "job_function_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_tenant_code",
                schema: "hr",
                table: "employees",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "employee_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_department_id",
                schema: "hr",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_designation_id",
                schema: "hr",
                table: "employees",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_job_location_id",
                schema: "hr",
                table: "employees",
                column: "job_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_position_id",
                schema: "hr",
                table: "employees",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_reporting_manager_id",
                schema: "hr",
                table: "employees",
                column: "reporting_manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_shift_id",
                schema: "hr",
                table: "employees",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_feedback_templates_job_family_id",
                schema: "hr",
                table: "interview_feedback_templates",
                column: "job_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_feedbacks_application_id",
                schema: "hr",
                table: "interview_feedbacks",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_feedbacks_candidate_id",
                schema: "hr",
                table: "interview_feedbacks",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_feedbacks_decision_lookup_value_id",
                schema: "hr",
                table: "interview_feedbacks",
                column: "decision_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_feedbacks_interview_id",
                schema: "hr",
                table: "interview_feedbacks",
                column: "interview_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_feedbacks_panel_member_id",
                schema: "hr",
                table: "interview_feedbacks",
                column: "panel_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_feedbacks_recommendation_lookup_value_id",
                schema: "hr",
                table: "interview_feedbacks",
                column: "recommendation_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_feedbacks_submitted_by_employee_id",
                schema: "hr",
                table: "interview_feedbacks",
                column: "submitted_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_notifications_communication_template_id",
                schema: "hr",
                table: "interview_notifications",
                column: "communication_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_notifications_delivery_status_lookup_value_id",
                schema: "hr",
                table: "interview_notifications",
                column: "delivery_status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_notifications_interview_id",
                schema: "hr",
                table: "interview_notifications",
                column: "interview_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_notifications_interview_panel_member_id",
                schema: "hr",
                table: "interview_notifications",
                column: "interview_panel_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_notifications_notification_type_lookup_value_id",
                schema: "hr",
                table: "interview_notifications",
                column: "notification_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_notifications_recipient_employee_id",
                schema: "hr",
                table: "interview_notifications",
                column: "recipient_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_notifications_response_action_lookup_value_id",
                schema: "hr",
                table: "interview_notifications",
                column: "response_action_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_panel_members_alternate_interviewer_employee_id",
                schema: "hr",
                table: "interview_panel_members",
                column: "alternate_interviewer_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_panel_members_interview_id",
                schema: "hr",
                table: "interview_panel_members",
                column: "interview_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_panel_members_interviewer_employee_id",
                schema: "hr",
                table: "interview_panel_members",
                column: "interviewer_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_interview_panel_members_invite_status_lookup_value_id",
                schema: "hr",
                table: "interview_panel_members",
                column: "invite_status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviewer_availabilities_interview_id",
                schema: "hr",
                table: "interviewer_availabilities",
                column: "interview_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviewer_availabilities_interviewer_employee_id",
                schema: "hr",
                table: "interviewer_availabilities",
                column: "interviewer_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviewer_availabilities_slot_status_lookup_value_id",
                schema: "hr",
                table: "interviewer_availabilities",
                column: "slot_status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_application_id",
                schema: "hr",
                table: "interviews",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_candidate_id",
                schema: "hr",
                table: "interviews",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_decision_lookup_value_id",
                schema: "hr",
                table: "interviews",
                column: "decision_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_interview_feedback_template_id",
                schema: "hr",
                table: "interviews",
                column: "interview_feedback_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_interview_type_lookup_value_id",
                schema: "hr",
                table: "interviews",
                column: "interview_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_job_id",
                schema: "hr",
                table: "interviews",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_proposed_by_employee_id",
                schema: "hr",
                table: "interviews",
                column: "proposed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_status_lookup_value_id",
                schema: "hr",
                table: "interviews",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_allowances_profile_id",
                schema: "hr",
                table: "job_details",
                column: "allowances_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_closed_by_employee_id",
                schema: "hr",
                table: "job_details",
                column: "closed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_cost_center_id",
                schema: "hr",
                table: "job_details",
                column: "cost_center_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_grade_id",
                schema: "hr",
                table: "job_details",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_job_family_id",
                schema: "hr",
                table: "job_details",
                column: "job_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_job_function_id",
                schema: "hr",
                table: "job_details",
                column: "job_function_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_job_id",
                schema: "hr",
                table: "job_details",
                column: "job_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_details_job_location_id",
                schema: "hr",
                table: "job_details",
                column: "job_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_job_template_id",
                schema: "hr",
                table: "job_details",
                column: "job_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_pay_scale_id",
                schema: "hr",
                table: "job_details",
                column: "pay_scale_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_position_id",
                schema: "hr",
                table: "job_details",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_replaced_employee_id",
                schema: "hr",
                table: "job_details",
                column: "replaced_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_reporting_manager_employee_id",
                schema: "hr",
                table: "job_details",
                column: "reporting_manager_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_screening_questionnaire_id",
                schema: "hr",
                table: "job_details",
                column: "screening_questionnaire_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_details_shift_id",
                schema: "hr",
                table: "job_details",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_posting_channels_channel_template_id",
                schema: "hr",
                table: "job_posting_channels",
                column: "channel_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_posting_channels_channel_type_lookup_value_id",
                schema: "hr",
                table: "job_posting_channels",
                column: "channel_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_posting_channels_job_id",
                schema: "hr",
                table: "job_posting_channels",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_posting_channels_status_lookup_value_id",
                schema: "hr",
                table: "job_posting_channels",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_templates_designation_id",
                schema: "hr",
                table: "job_templates",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_templates_grade_id",
                schema: "hr",
                table: "job_templates",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_templates_job_family_id",
                schema: "hr",
                table: "job_templates",
                column: "job_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_templates_job_function_id",
                schema: "hr",
                table: "job_templates",
                column: "job_function_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_templates_position_id",
                schema: "hr",
                table: "job_templates",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_templates_shift_id",
                schema: "hr",
                table: "job_templates",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_approval_request_id",
                schema: "hr",
                table: "jobs",
                column: "approval_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_closed_by_employee_id",
                schema: "hr",
                table: "jobs",
                column: "closed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_department_id",
                schema: "hr",
                table: "jobs",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_designation_id",
                schema: "hr",
                table: "jobs",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_hiring_manager_employee_id",
                schema: "hr",
                table: "jobs",
                column: "hiring_manager_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_job_template_id",
                schema: "hr",
                table: "jobs",
                column: "job_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_parent_job_id",
                schema: "hr",
                table: "jobs",
                column: "parent_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_priority_lookup_value_id",
                schema: "hr",
                table: "jobs",
                column: "priority_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_recruiter_employee_id",
                schema: "hr",
                table: "jobs",
                column: "recruiter_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_status_lookup_value_id",
                schema: "hr",
                table: "jobs",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_lookup_values_lookup_type_id",
                schema: "hr",
                table: "lookup_values",
                column: "lookup_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_lookup_values_parent_lookup_value_id",
                schema: "hr",
                table: "lookup_values",
                column: "parent_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letter_details_allowances_profile_id",
                schema: "hr",
                table: "offer_letter_details",
                column: "allowances_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letter_details_benefits_plan_id",
                schema: "hr",
                table: "offer_letter_details",
                column: "benefits_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letter_details_counter_signed_by_employee_id",
                schema: "hr",
                table: "offer_letter_details",
                column: "counter_signed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letter_details_grade_id",
                schema: "hr",
                table: "offer_letter_details",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letter_details_offer_letter_id",
                schema: "hr",
                table: "offer_letter_details",
                column: "offer_letter_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offer_letter_details_pay_scale_id",
                schema: "hr",
                table: "offer_letter_details",
                column: "pay_scale_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_application_id",
                schema: "hr",
                table: "offer_letters",
                column: "application_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_approval_request_id",
                schema: "hr",
                table: "offer_letters",
                column: "approval_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_candidate_id",
                schema: "hr",
                table: "offer_letters",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_candidate_response_lookup_value_id",
                schema: "hr",
                table: "offer_letters",
                column: "candidate_response_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_communication_template_id",
                schema: "hr",
                table: "offer_letters",
                column: "communication_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_job_id",
                schema: "hr",
                table: "offer_letters",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_reporting_manager_employee_id",
                schema: "hr",
                table: "offer_letters",
                column: "reporting_manager_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_revoked_by_employee_id",
                schema: "hr",
                table: "offer_letters",
                column: "revoked_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_sent_by_employee_id",
                schema: "hr",
                table: "offer_letters",
                column: "sent_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_letters_status_lookup_value_id",
                schema: "hr",
                table: "offer_letters",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_negotiations_offer_id",
                schema: "hr",
                table: "offer_negotiations",
                column: "offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_negotiations_re_approval_request_id",
                schema: "hr",
                table: "offer_negotiations",
                column: "re_approval_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_negotiations_response_by_employee_id",
                schema: "hr",
                table: "offer_negotiations",
                column: "response_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_negotiations_status_lookup_value_id",
                schema: "hr",
                table: "offer_negotiations",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_templates_applicable_department_id",
                schema: "hr",
                table: "onboarding_task_templates",
                column: "applicable_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_templates_applicable_designation_id",
                schema: "hr",
                table: "onboarding_task_templates",
                column: "applicable_designation_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_templates_applicable_location_id",
                schema: "hr",
                table: "onboarding_task_templates",
                column: "applicable_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_task_templates_depends_on_template_id",
                schema: "hr",
                table: "onboarding_task_templates",
                column: "depends_on_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_application_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_assigned_to_employee_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "assigned_to_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_cancelled_by_employee_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "cancelled_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_candidate_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_completed_by_employee_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "completed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_depends_on_task_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "depends_on_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_employee_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_escalated_to_employee_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "escalated_to_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_onboarding_task_template_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "onboarding_task_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_status_lookup_value_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "status_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_onboarding_tasks_verified_by_employee_id",
                schema: "hr",
                table: "onboarding_tasks",
                column: "verified_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_department_id",
                schema: "hr",
                table: "positions",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_designation_id",
                schema: "hr",
                table: "positions",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_grade_id",
                schema: "hr",
                table: "positions",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_job_family_id",
                schema: "hr",
                table: "positions",
                column: "job_family_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_job_function_id",
                schema: "hr",
                table: "positions",
                column: "job_function_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_pay_scale_id",
                schema: "hr",
                table: "positions",
                column: "pay_scale_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_reports_to_position_id",
                schema: "hr",
                table: "positions",
                column: "reports_to_position_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_shift_id",
                schema: "hr",
                table: "positions",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_skills_skill_category_id",
                schema: "hr",
                table: "skills",
                column: "skill_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_skills_skill_type_lookup_value_id",
                schema: "hr",
                table: "skills",
                column: "skill_type_lookup_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_conditions_workflow_config_id",
                schema: "hr",
                table: "workflow_conditions",
                column: "workflow_config_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_config_steps_workflow_config_id",
                schema: "hr",
                table: "workflow_config_steps",
                column: "workflow_config_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_escalations_workflow_config_step_id",
                schema: "hr",
                table: "workflow_escalations",
                column: "workflow_config_step_id");

            migrationBuilder.AddForeignKey(
                name: "fk_application_compliances_applications_application_id",
                schema: "hr",
                table: "application_compliances",
                column: "application_id",
                principalSchema: "hr",
                principalTable: "applications",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_application_details_applications_application_id",
                schema: "hr",
                table: "application_details",
                column: "application_id",
                principalSchema: "hr",
                principalTable: "applications",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_application_details_employees_assigned_hiring_manager_emplo",
                schema: "hr",
                table: "application_details",
                column: "assigned_hiring_manager_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_application_details_employees_last_contacted_by_employee_id",
                schema: "hr",
                table: "application_details",
                column: "last_contacted_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_application_details_employees_rejection_sent_by_employee_id",
                schema: "hr",
                table: "application_details",
                column: "rejection_sent_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_application_details_employees_reviewed_by_employee_id",
                schema: "hr",
                table: "application_details",
                column: "reviewed_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_application_details_employees_screening_completed_by_employ",
                schema: "hr",
                table: "application_details",
                column: "screening_completed_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_applications_candidates_candidate_id",
                schema: "hr",
                table: "applications",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_applications_employees_assigned_recruiter_employee_id",
                schema: "hr",
                table: "applications",
                column: "assigned_recruiter_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_applications_employees_converted_to_employee_id",
                schema: "hr",
                table: "applications",
                column: "converted_to_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_applications_job_posting_channels_job_posting_channel_id",
                schema: "hr",
                table: "applications",
                column: "job_posting_channel_id",
                principalSchema: "hr",
                principalTable: "job_posting_channels",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_applications_jobs_job_id",
                schema: "hr",
                table: "applications",
                column: "job_id",
                principalSchema: "hr",
                principalTable: "jobs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_approval_request_steps_approval_requests_approval_request_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "approval_request_id",
                principalSchema: "hr",
                principalTable: "approval_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_approval_request_steps_employees_approver_employee_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "approver_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_approval_request_steps_employees_delegated_to_employee_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "delegated_to_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_approval_request_steps_employees_escalated_to_employee_id",
                schema: "hr",
                table: "approval_request_steps",
                column: "escalated_to_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_approval_requests_employees_requested_by_employee_id",
                schema: "hr",
                table: "approval_requests",
                column: "requested_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_call_logs_candidates_candidate_id",
                schema: "hr",
                table: "call_logs",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_call_logs_employees_called_by_employee_id",
                schema: "hr",
                table: "call_logs",
                column: "called_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_addresses_candidates_candidate_id",
                schema: "hr",
                table: "candidate_addresses",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_contacts_candidates_candidate_id",
                schema: "hr",
                table: "candidate_contacts",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_media_links_candidates_candidate_id",
                schema: "hr",
                table: "candidate_media_links",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_profiles_candidates_candidate_id",
                schema: "hr",
                table: "candidate_profiles",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_profiles_candidates_merged_into_candidate_id",
                schema: "hr",
                table: "candidate_profiles",
                column: "merged_into_candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_profiles_employees_blacklist_removed_by_employee_",
                schema: "hr",
                table: "candidate_profiles",
                column: "blacklist_removed_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_profiles_employees_talent_pool_added_by_employee_",
                schema: "hr",
                table: "candidate_profiles",
                column: "talent_pool_added_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_skills_candidates_candidate_id",
                schema: "hr",
                table: "candidate_skills",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_skills_employees_verified_by_employee_id",
                schema: "hr",
                table: "candidate_skills",
                column: "verified_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_stage_histories_candidates_candidate_id",
                schema: "hr",
                table: "candidate_stage_histories",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_stage_histories_employees_changed_by_employee_id",
                schema: "hr",
                table: "candidate_stage_histories",
                column: "changed_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_task_evaluations_candidate_task_submissio_fe4ea1f5",
                schema: "hr",
                table: "candidate_task_evaluations",
                column: "submission_id",
                principalSchema: "hr",
                principalTable: "candidate_task_submissions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_task_evaluations_candidate_tasks_candidate_task_id",
                schema: "hr",
                table: "candidate_task_evaluations",
                column: "candidate_task_id",
                principalSchema: "hr",
                principalTable: "candidate_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_task_evaluations_employees_evaluated_by_employee_",
                schema: "hr",
                table: "candidate_task_evaluations",
                column: "evaluated_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_task_submissions_candidate_tasks_candidate_task_id",
                schema: "hr",
                table: "candidate_task_submissions",
                column: "candidate_task_id",
                principalSchema: "hr",
                principalTable: "candidate_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_task_submissions_candidates_candidate_id",
                schema: "hr",
                table: "candidate_task_submissions",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_tasks_candidates_candidate_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "candidate_id",
                principalSchema: "hr",
                principalTable: "candidates",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_tasks_employees_assigned_by_employee_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "assigned_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_tasks_jobs_job_id",
                schema: "hr",
                table: "candidate_tasks",
                column: "job_id",
                principalSchema: "hr",
                principalTable: "jobs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidates_designations_current_designation_id",
                schema: "hr",
                table: "candidates",
                column: "current_designation_id",
                principalSchema: "hr",
                principalTable: "designations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_candidates_employees_blacklisted_by_employee_id",
                schema: "hr",
                table: "candidates",
                column: "blacklisted_by_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_cost_centers_departments_department_id",
                schema: "hr",
                table: "cost_centers",
                column: "department_id",
                principalSchema: "hr",
                principalTable: "departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_cost_centers_employees_budget_owner_employee_id",
                schema: "hr",
                table: "cost_centers",
                column: "budget_owner_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_departments_employees_department_head_employee_id",
                schema: "hr",
                table: "departments",
                column: "department_head_employee_id",
                principalSchema: "hr",
                principalTable: "employees",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cost_centers_employees_budget_owner_employee_id",
                schema: "hr",
                table: "cost_centers");

            migrationBuilder.DropForeignKey(
                name: "fk_departments_employees_department_head_employee_id",
                schema: "hr",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "fk_cost_centers_departments_department_id",
                schema: "hr",
                table: "cost_centers");

            migrationBuilder.DropTable(
                name: "application_compliances",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "application_details",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "approval_request_steps",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "call_logs",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_addresses",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_contacts",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_media_links",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_profiles",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_skills",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_stage_histories",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_task_evaluations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "competency_framework_items",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "interview_feedbacks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "interview_notifications",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "interviewer_availabilities",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "job_details",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "offer_letter_details",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "offer_negotiations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "onboarding_tasks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "workflow_conditions",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "workflow_escalations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "talent_pools",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_task_submissions",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "competency_frameworks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "interview_panel_members",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "screening_questionnaires",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "allowances_profiles",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "benefits_plans",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "offer_letters",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "onboarding_task_templates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "workflow_config_steps",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidate_tasks",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "skill_categories",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "interviews",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "communication_templates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "applications",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "interview_feedback_templates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "candidates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "job_posting_channels",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "channel_templates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "jobs",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "approval_requests",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "job_templates",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "lookup_values",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "workflow_configs",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "lookup_types",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "job_locations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "positions",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "designations",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "pay_scales",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "shifts",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "grades",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "job_families",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "job_functions",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "cost_centers",
                schema: "hr");
        }
    }
}
