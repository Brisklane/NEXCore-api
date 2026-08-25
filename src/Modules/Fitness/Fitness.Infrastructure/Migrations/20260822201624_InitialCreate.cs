using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fitness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fitness");

            migrationBuilder.CreateTable(
                name: "AccessEvents",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    door_id = table.Column<Guid>(type: "uuid", nullable: true),
                    controller_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credential_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    method = table.Column<int>(type: "integer", nullable: false),
                    decision = table.Column<int>(type: "integer", nullable: false),
                    denial_reason = table.Column<int>(type: "integer", nullable: false),
                    decision_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    was_offline_decision = table.Column<bool>(type: "boolean", nullable: false),
                    replayed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    overridden_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    check_in_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_ms = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AccessRules",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    balance_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    requires_waiver = table.Column<bool>(type: "boolean", nullable: false),
                    requires_medical_clearance = table.Column<bool>(type: "boolean", nullable: false),
                    respects_occupancy_cap = table.Column<bool>(type: "boolean", nullable: false),
                    anti_passback = table.Column<int>(type: "integer", nullable: false),
                    minimum_age = table.Column<int>(type: "integer", nullable: true),
                    requires_guardian = table.Column<bool>(type: "boolean", nullable: false),
                    max_visits_per_period = table.Column<int>(type: "integer", nullable: false),
                    visit_limit_basis = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AgreementTemplates",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    requires_guardian_signature = table.Column<bool>(type: "boolean", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agreement_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AllowanceUsage",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entitlement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    allowance = table.Column<int>(type: "integer", nullable: false),
                    used = table.Column<int>(type: "integer", nullable: false),
                    overage = table.Column<int>(type: "integer", nullable: false),
                    overage_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allowance_usage", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    show_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    show_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    show_on_kiosk = table.Column<bool>(type: "boolean", nullable: false),
                    show_in_app = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_club_screens = table.Column<bool>(type: "boolean", nullable: false),
                    is_urgent = table.Column<bool>(type: "boolean", nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentSeries",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    series_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    series_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    repeat_every_weeks = table.Column<int>(type: "integer", nullable: false),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointment_series", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentTemplates",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    recommended_interval_days = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_system_template = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessment_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Audit",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    change_summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    is_sensitive_access = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Badges",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blurb = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    icon_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    criteria_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criteria_expression = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    points_awarded = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_automatic = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_badges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "BillingRuns",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    billing_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_preview = table.Column<bool>(type: "boolean", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_scheduled = table.Column<int>(type: "integer", nullable: false),
                    invoices_created = table.Column<int>(type: "integer", nullable: false),
                    payments_collected = table.Column<int>(type: "integer", nullable: false),
                    payments_failed = table.Column<int>(type: "integer", nullable: false),
                    skipped = table.Column<int>(type: "integer", nullable: false),
                    errors = table.Column<int>(type: "integer", nullable: false),
                    total_billed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_collected = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_failed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    error_summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    run_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_automatic = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "BookingPolicies",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_opens_days_before = table.Column<int>(type: "integer", nullable: false),
                    booking_closes_minutes_before = table.Column<int>(type: "integer", nullable: false),
                    max_concurrent_bookings = table.Column<int>(type: "integer", nullable: false),
                    max_bookings_per_day = table.Column<int>(type: "integer", nullable: false),
                    max_bookings_per_week = table.Column<int>(type: "integer", nullable: false),
                    waitlist_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    max_waitlist_length = table.Column<int>(type: "integer", nullable: false),
                    hold_credit_on_waitlist = table.Column<bool>(type: "boolean", nullable: false),
                    waitlist_confirm_minutes = table.Column<int>(type: "integer", nullable: false),
                    prevent_duplicate_same_day = table.Column<bool>(type: "boolean", nullable: false),
                    requires_payment_up_front = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    message_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recipient_count = table.Column<int>(type: "integer", nullable: false),
                    sent_count = table.Column<int>(type: "integer", nullable: false),
                    delivered_count = table.Column<int>(type: "integer", nullable: false),
                    opened_count = table.Column<int>(type: "integer", nullable: false),
                    clicked_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    suppressed_count = table.Column<int>(type: "integer", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    leads_generated = table.Column<int>(type: "integer", nullable: false),
                    joins_attributed = table.Column<int>(type: "integer", nullable: false),
                    revenue_attributed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    promotion_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_sent = table.Column<bool>(type: "boolean", nullable: false),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CancellationPolicies",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    free_cancel_hours = table.Column<int>(type: "integer", nullable: false),
                    late_cancel_outcome = table.Column<int>(type: "integer", nullable: false),
                    late_cancel_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    no_show_outcome = table.Column<int>(type: "integer", nullable: false),
                    no_show_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    no_show_grace_minutes = table.Column<int>(type: "integer", nullable: false),
                    strike_threshold = table.Column<int>(type: "integer", nullable: false),
                    strike_window_days = table.Column<int>(type: "integer", nullable: false),
                    booking_ban_days = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cancellation_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CashSessions",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opening_float = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cash_sales = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    card_sales = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    other_sales = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refunds = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_in = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_out = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    drops = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expected_cash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    counted_cash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    variance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    was_blind_count = table.Column<bool>(type: "boolean", nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false),
                    variance_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blurb = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    metric = table.Column<int>(type: "integer", nullable: false),
                    custom_metric_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    is_team_based = table.Column<bool>(type: "boolean", nullable: false),
                    is_open_to_all = table.Column<bool>(type: "boolean", nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entry_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    prize = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    points_for_completion = table.Column<int>(type: "integer", nullable: false),
                    participant_count = table.Column<int>(type: "integer", nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_challenges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ClassTypes",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    discipline = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    marketing_blurb = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    default_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    default_capacity = table.Column<int>(type: "integer", nullable: false),
                    intensity = table.Column<int>(type: "integer", nullable: false),
                    equipment_needed = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    minimum_age = table.Column<int>(type: "integer", nullable: true),
                    maximum_age = table.Column<int>(type: "integer", nullable: true),
                    requires_skill_clearance = table.Column<bool>(type: "boolean", nullable: false),
                    required_skill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allows_drop_in = table.Column<bool>(type: "boolean", nullable: false),
                    drop_in_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_cost = table.Column<int>(type: "integer", nullable: false),
                    booking_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    available_to_marketplace = table.Column<bool>(type: "boolean", nullable: false),
                    is_bookable = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Clubs",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_type = table.Column<int>(type: "integer", nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    post_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    unit_system = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pos_store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_tax_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    soft_capacity = table.Column<int>(type: "integer", nullable: true),
                    hard_capacity = table.Column<int>(type: "integer", nullable: true),
                    current_occupancy = table.Column<int>(type: "integer", nullable: false),
                    default_booking_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_cancellation_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_dunning_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    access_balance_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    anti_passback = table.Column<int>(type: "integer", nullable: false),
                    anti_passback_minutes = table.Column<int>(type: "integer", nullable: false),
                    offline_policy = table.Column<int>(type: "integer", nullable: false),
                    minimum_age = table.Column<int>(type: "integer", nullable: false),
                    guardian_required_below_age = table.Column<int>(type: "integer", nullable: false),
                    requires_waiver = table.Column<bool>(type: "boolean", nullable: false),
                    requires_health_screening = table.Column<bool>(type: "boolean", nullable: false),
                    allows_cross_club_visits = table.Column<bool>(type: "boolean", nullable: false),
                    cross_club_visit_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_temporarily_closed = table.Column<bool>(type: "boolean", nullable: false),
                    closure_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    receipt_footer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    brand_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clubs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applies_to_role = table.Column<int>(type: "integer", nullable: true),
                    basis = table.Column<int>(type: "integer", nullable: false),
                    rate_per_unit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    accelerated_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    period_cap = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Controllers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    vendor = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    firmware_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    api_key_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    offline_policy = table.Column<int>(type: "integer", nullable: false),
                    cache_seconds = table.Column<int>(type: "integer", nullable: false),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pending_event_count = table.Column<int>(type: "integer", nullable: false),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    heartbeat_timeout_minutes = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_controllers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CorporateAccounts",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crm_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tax_registration_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    billing_model = table.Column<int>(type: "integer", nullable: false),
                    negotiated_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    discount_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    subsidy_per_member = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subsidy_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    default_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    contract_ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_members = table.Column<int>(type: "integer", nullable: false),
                    current_member_count = table.Column<int>(type: "integer", nullable: false),
                    invoice_day_of_month = table.Column<int>(type: "integer", nullable: false),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false),
                    receives_usage_report = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_note_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    issued_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    applied_to_balance = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DayPasses",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pass_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visitor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    visitor_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    visitor_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    visitor_date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    max_entries = table.Column<int>(type: "integer", nullable: false),
                    entries_used = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waiver_signed = table.Column<bool>(type: "boolean", nullable: false),
                    waiver_signature_id = table.Column<Guid>(type: "uuid", nullable: true),
                    temporary_credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_lead_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_trial = table.Column<bool>(type: "boolean", nullable: false),
                    issued_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_day_passes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DeferredRevenue",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    basis = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    recognised_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    service_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    service_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_units = table.Column<int>(type: "integer", nullable: false),
                    consumed_units = table.Column<int>(type: "integer", nullable: false),
                    revenue_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deferred_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    closed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deferred_revenue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DunningPolicies",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    write_off_after_days = table.Column<int>(type: "integer", nullable: false),
                    suspend_access_after_days = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dunning_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    model = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    asset_tag = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    purchased_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    purchase_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    warranty_ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    service_contract_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    service_contract_ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    installed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retired_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disposal_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    usage_hours = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    usage_read_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_serviced_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_service_due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_maintenance_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_downtime_hours = table.Column<int>(type: "integer", nullable: false),
                    qr_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    out_of_service_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    out_of_service_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    muscle_groups = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    equipment = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    video_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    scaling_options = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tracks_personal_record = table.Column<bool>(type: "boolean", nullable: false),
                    pr_score_type = table.Column<int>(type: "integer", nullable: true),
                    is_system_exercise = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercises", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "FacilityChecks",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: false),
                    due_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    times_per_day = table.Column<int>(type: "integer", nullable: false),
                    default_assignee_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alert_on_missed = table.Column<bool>(type: "boolean", nullable: false),
                    missed_after_minutes = table.Column<int>(type: "integer", nullable: false),
                    requires_signature = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facility_checks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "FaultReports",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fault_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reported_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reported_by_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    photo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    taken_out_of_service = table.Column<bool>(type: "boolean", nullable: false),
                    sign_printed = table.Column<bool>(type: "boolean", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fault_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    channel = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    is_actioned = table.Column<bool>(type: "boolean", nullable: false),
                    action_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    actioned_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feedback", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "GiftCards",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initial_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    issued_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    purchased_by_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_redeemed = table.Column<bool>(type: "boolean", nullable: false),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gift_cards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "GradingEvents",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_ladder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    held_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    examiner_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_examiner_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    candidate_count = table.Column<int>(type: "integer", nullable: false),
                    pass_count = table.Column<int>(type: "integer", nullable: false),
                    fee_per_candidate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grading_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Handovers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    from_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    outstanding_items = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    has_urgent_items = table.Column<bool>(type: "boolean", nullable: false),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_handovers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Households",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    primary_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    post_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    any_adult_may_check_in_children = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_households", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    involved_person_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    involved_person_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reported_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    detail = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    witness_names = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    witness_statements = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    first_aid_given = table.Column<bool>(type: "boolean", nullable: false),
                    first_aider_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    aed_used = table.Column<bool>(type: "boolean", nullable: false),
                    ambulance_called = table.Column<bool>(type: "boolean", nullable: false),
                    hospital_attended = table.Column<bool>(type: "boolean", nullable: false),
                    immediate_action = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    photo_urls = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    owner_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    root_cause = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    preventive_action = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_reportable = table.Column<bool>(type: "boolean", nullable: false),
                    was_reported = table.Column<bool>(type: "boolean", nullable: false),
                    reported_to_authority_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    authority_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    insurer_notified = table.Column<bool>(type: "boolean", nullable: false),
                    insurer_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    estimated_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incidents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Journeys",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trigger = table.Column<int>(type: "integer", nullable: false),
                    trigger_threshold_days = table.Column<int>(type: "integer", nullable: true),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prevent_re_enrolment = table.Column<bool>(type: "boolean", nullable: false),
                    re_enrolment_cooldown_days = table.Column<int>(type: "integer", nullable: false),
                    enrolled_count = table.Column<int>(type: "integer", nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: false),
                    success_metric = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    success_count = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journeys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Leaderboard",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_occurrence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: true),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    member_photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    score_display = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    was_scaled = table.Column<bool>(type: "boolean", nullable: false),
                    division = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    workout_result_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leaderboard", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "LeadSources",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    monthly_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tracking_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lead_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "LockerBanks",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_lockers = table.Column<int>(type: "integer", nullable: false),
                    rented_count = table.Column<int>(type: "integer", nullable: false),
                    out_of_order_count = table.Column<int>(type: "integer", nullable: false),
                    supports_rental = table.Column<bool>(type: "boolean", nullable: false),
                    supports_day_use = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locker_banks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "LossReasons",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    requires_note = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loss_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "LostProperty",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    found_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    found_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    found_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    claimed_by_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    claimed_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    claimed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dispose_after = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disposed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disposal_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lost_property", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTiers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    points_required = table.Column<int>(type: "integer", nullable: false),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    badge_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    earn_multiplier = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    benefits = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retention_months = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_tiers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Mandates",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method_ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mandate_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scheme_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    signed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    first_collection_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    advance_notice_days = table.Column<int>(type: "integer", nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mandates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceChannels",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rate_per_booking = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    revenue_share_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    off_peak_only = table.Column<bool>(type: "boolean", nullable: false),
                    default_capacity_per_class = table.Column<int>(type: "integer", nullable: false),
                    api_endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    api_key_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_marketplace_channels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "MessageLog",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    message_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    journey_enrolment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dunning_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    body_preview = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    clicked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    plain_text_body = table.Column<string>(type: "text", nullable: true),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    purpose = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    is_transactional = table.Column<bool>(type: "boolean", nullable: false),
                    is_system_template = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OccupancySnapshots",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    taken_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    occupancy = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    check_ins_in_interval = table.Column<int>(type: "integer", nullable: false),
                    check_outs_in_interval = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_occupancy_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Payers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payer_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false),
                    agreed_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    requires_authorisation_number = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PlanChangePaths",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_immediately = table.Column<bool>(type: "boolean", nullable: false),
                    proration = table.Column<int>(type: "integer", nullable: false),
                    change_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    restarts_minimum_term = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_change_paths", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    marketing_blurb = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    price_includes_tax = table.Column<bool>(type: "boolean", nullable: false),
                    billing_period = table.Column<int>(type: "integer", nullable: false),
                    billing_anchor = table.Column<int>(type: "integer", nullable: false),
                    join_proration = table.Column<int>(type: "integer", nullable: false),
                    cancel_proration = table.Column<int>(type: "integer", nullable: false),
                    joining_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    admin_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    card_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    annual_maintenance_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    annual_fee_month = table.Column<int>(type: "integer", nullable: true),
                    minimum_term_months = table.Column<int>(type: "integer", nullable: false),
                    duration_months = table.Column<int>(type: "integer", nullable: true),
                    notice_period_days = table.Column<int>(type: "integer", nullable: false),
                    auto_renews = table.Column<bool>(type: "boolean", nullable: false),
                    early_termination_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    early_termination_percent_of_remaining = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    credit_count = table.Column<int>(type: "integer", nullable: false),
                    valid_for_days = table.Column<int>(type: "integer", nullable: false),
                    credits_transferable = table.Column<bool>(type: "boolean", nullable: false),
                    credits_refundable = table.Column<bool>(type: "boolean", nullable: false),
                    restricted_to_club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allows_cross_club_access = table.Column<bool>(type: "boolean", nullable: false),
                    visits_per_period = table.Column<int>(type: "integer", nullable: false),
                    visit_limit_basis = table.Column<int>(type: "integer", nullable: false),
                    guest_passes_per_period = table.Column<int>(type: "integer", nullable: false),
                    booking_window_days = table.Column<int>(type: "integer", nullable: false),
                    max_concurrent_bookings = table.Column<int>(type: "integer", nullable: false),
                    sellable_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sellable_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sellable_at_desk = table.Column<bool>(type: "boolean", nullable: false),
                    sellable_online = table.Column<bool>(type: "boolean", nullable: false),
                    sellable_in_app = table.Column<bool>(type: "boolean", nullable: false),
                    sellable_at_kiosk = table.Column<bool>(type: "boolean", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_age = table.Column<int>(type: "integer", nullable: true),
                    maximum_age = table.Column<int>(type: "integer", nullable: true),
                    required_proof = table.Column<int>(type: "integer", nullable: false),
                    waiver_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agreement_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requires_health_screening = table.Column<bool>(type: "boolean", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recognition_basis = table.Column<int>(type: "integer", nullable: false),
                    revenue_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deferred_revenue_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramTracks",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_tracks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_kind = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    period_count = table.Column<int>(type: "integer", nullable: false),
                    active_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    active_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    new_members_only = table.Column<bool>(type: "boolean", nullable: false),
                    max_redemptions = table.Column<int>(type: "integer", nullable: false),
                    redemption_count = table.Column<int>(type: "integer", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "RankLadders",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discipline = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rank_ladders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    requested_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    to_original_method = table.Column<bool>(type: "boolean", nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refunds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    slot_minutes = table.Column<int>(type: "integer", nullable: false),
                    buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    member_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    non_member_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    peak_surcharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    booking_window_days = table.Column<int>(type: "integer", nullable: false),
                    max_concurrent_bookings_per_member = table.Column<int>(type: "integer", nullable: false),
                    free_cancel_hours = table.Column<int>(type: "integer", nullable: false),
                    late_cancel_outcome = table.Column<int>(type: "integer", nullable: false),
                    no_show_outcome = table.Column<int>(type: "integer", nullable: false),
                    linked_door_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bookable_online = table.Column<bool>(type: "boolean", nullable: false),
                    is_out_of_service = table.Column<bool>(type: "boolean", nullable: false),
                    out_of_service_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SalesTargets",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metric_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    achievement_percent = table.Column<int>(type: "integer", nullable: false),
                    bonus_on_achievement = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_achieved = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_targets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Segments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    definition_json = table.Column<string>(type: "text", nullable: false),
                    last_count = table.Column<int>(type: "integer", nullable: false),
                    last_counted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_system_segment = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_segments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    required_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    free_cancel_hours = table.Column<int>(type: "integer", nullable: false),
                    late_cancel_outcome = table.Column<int>(type: "integer", nullable: false),
                    no_show_outcome = table.Column<int>(type: "integer", nullable: false),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    bookable_online = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_number_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    default_notice_period_days = table.Column<int>(type: "integer", nullable: false),
                    default_cooling_off_days = table.Column<int>(type: "integer", nullable: false),
                    max_freeze_days_per_year = table.Column<int>(type: "integer", nullable: false),
                    default_freeze_fee_per_month = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    default_billing_anchor = table.Column<int>(type: "integer", nullable: false),
                    fixed_billing_day_of_month = table.Column<int>(type: "integer", nullable: false),
                    default_proration = table.Column<int>(type: "integer", nullable: false),
                    invoice_grace_days = table.Column<int>(type: "integer", nullable: false),
                    default_late_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    auto_run_billing = table.Column<bool>(type: "boolean", nullable: false),
                    billing_run_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    access_cache_seconds = table.Column<int>(type: "integer", nullable: false),
                    capture_image_on_denial = table.Column<bool>(type: "boolean", nullable: false),
                    absence_risk_days = table.Column<int>(type: "integer", nullable: false),
                    critical_absence_days = table.Column<int>(type: "integer", nullable: false),
                    auto_score_churn = table.Column<bool>(type: "boolean", nullable: false),
                    quiet_hours_from = table.Column<TimeSpan>(type: "interval", nullable: false),
                    quiet_hours_to = table.Column<TimeSpan>(type: "interval", nullable: false),
                    respect_quiet_hours = table.Column<bool>(type: "boolean", nullable: false),
                    from_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    from_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sms_sender_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    lead_response_sla_minutes = table.Column<int>(type: "integer", nullable: false),
                    discount_approval_threshold_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    refund_approval_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    write_off_approval_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    require_pin_for_overrides = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    position = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    break_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    required_headcount = table.Column<int>(type: "integer", nullable: false),
                    assigned_headcount = table.Column<int>(type: "integer", nullable: false),
                    required_certification = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shifts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StaffRoles",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_kind = table.Column<int>(type: "integer", nullable: false),
                    permissions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    discount_limit_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    refund_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    write_off_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    can_override_policies = table.Column<bool>(type: "boolean", nullable: false),
                    can_view_medical_data = table.Column<bool>(type: "boolean", nullable: false),
                    can_export_member_data = table.Column<bool>(type: "boolean", nullable: false),
                    is_system_role = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StaffTargets",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metric_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    achievement_percent = table.Column<int>(type: "integer", nullable: false),
                    bonus = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_achieved = table.Column<bool>(type: "boolean", nullable: false),
                    bonus_paid = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_targets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TimeOff",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_all_day = table.Column<bool>(type: "boolean", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_off", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "VendingRevenue",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revenue_source = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    machine_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    gross_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    commission_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: true),
                    entered_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vending_revenue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WaiverTemplates",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    class_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    activity_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    consent_clauses_json = table.Column<string>(type: "text", nullable: true),
                    requires_guardian_signature = table.Column<bool>(type: "boolean", nullable: false),
                    guardian_required_below_age = table.Column<int>(type: "integer", nullable: true),
                    valid_for_days = table.Column<int>(type: "integer", nullable: false),
                    blocks_access = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    requires_resign_on_new_version = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waiver_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Workouts",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    coach_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    score_type = table.Column<int>(type: "integer", nullable: true),
                    score_unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    time_cap_seconds = table.Column<int>(type: "integer", nullable: true),
                    is_benchmark = table.Column<bool>(type: "boolean", nullable: false),
                    benchmark_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: true),
                    is_template = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workouts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WriteOffs",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dunning_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    written_off_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    was_recovered = table.Column<bool>(type: "boolean", nullable: false),
                    recovered_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recovered_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_write_offs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AccessRuleWindows",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ends_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_rule_windows", x => x.id);
                    table.ForeignKey(
                        name: "fk_access_rule_windows_access_rules_access_rule_id",
                        column: x => x.access_rule_id,
                        principalSchema: "fitness",
                        principalTable: "AccessRules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentMeasures",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    measure_type = table.Column<int>(type: "integer", nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    grouping = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    min_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    max_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    normal_low = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    normal_high = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_calculated = table.Column<bool>(type: "boolean", nullable: false),
                    calculation_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_device_imported = table.Column<bool>(type: "boolean", nullable: false),
                    device_field_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessment_measures", x => x.id);
                    table.ForeignKey(
                        name: "fk_assessment_measures_assessment_templates_assessment_template_",
                        column: x => x.assessment_template_id,
                        principalSchema: "fitness",
                        principalTable: "AssessmentTemplates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingRunLines",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    billing_schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    failure_reason = table.Column<int>(type: "integer", nullable: true),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_run_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_billing_run_lines_billing_runs_billing_run_id",
                        column: x => x.billing_run_id,
                        principalSchema: "fitness",
                        principalTable: "BillingRuns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashMovements",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_movements_cash_sessions_cash_session_id",
                        column: x => x.cash_session_id,
                        principalSchema: "fitness",
                        principalTable: "CashSessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Areas",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    current_occupancy = table.Column<int>(type: "integer", nullable: false),
                    requires_entitlement = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_age = table.Column<int>(type: "integer", nullable: true),
                    max_participants_per_staff = table.Column<int>(type: "integer", nullable: true),
                    is_out_of_service = table.Column<bool>(type: "boolean", nullable: false),
                    out_of_service_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_areas", x => x.id);
                    table.ForeignKey(
                        name: "fk_areas_clubs_club_id",
                        column: x => x.club_id,
                        principalSchema: "fitness",
                        principalTable: "Clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubClosures",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    member_notice = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancels_classes = table.Column<bool>(type: "boolean", nullable: false),
                    extends_agreements = table.Column<bool>(type: "boolean", nullable: false),
                    blocks_access = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_club_closures", x => x.id);
                    table.ForeignKey(
                        name: "fk_club_closures_clubs_club_id",
                        column: x => x.club_id,
                        principalSchema: "fitness",
                        principalTable: "Clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubSchedules",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    override_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opens_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    closes_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    staffed_from = table.Column<TimeSpan>(type: "interval", nullable: true),
                    staffed_to = table.Column<TimeSpan>(type: "interval", nullable: true),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_club_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_club_schedules_clubs_club_id",
                        column: x => x.club_id,
                        principalSchema: "fitness",
                        principalTable: "Clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    role_kind = table.Column<int>(type: "integer", nullable: false),
                    pin_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    started_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    left_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_contractor = table.Column<bool>(type: "boolean", nullable: false),
                    access_credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    access_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    can_sell = table.Column<bool>(type: "boolean", nullable: false),
                    can_train = table.Column<bool>(type: "boolean", nullable: false),
                    can_teach = table.Column<bool>(type: "boolean", nullable: false),
                    can_approve_overrides = table.Column<bool>(type: "boolean", nullable: false),
                    is_bookable = table.Column<bool>(type: "boolean", nullable: false),
                    additional_club_ids = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_clubs_club_id",
                        column: x => x.club_id,
                        principalSchema: "fitness",
                        principalTable: "Clubs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "CorporateInvoices",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    corporate_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    issued_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    member_count = table.Column<int>(type: "integer", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    breakdown_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    document_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    purchase_order_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_invoices_corporate_accounts_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalSchema: "fitness",
                        principalTable: "CorporateAccounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityRules",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    corporate_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proof = table.Column<int>(type: "integer", nullable: false),
                    match_value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    requires_manual_approval = table.Column<bool>(type: "boolean", nullable: false),
                    revalidate_every_days = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eligibility_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_eligibility_rules_corporate_accounts_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalSchema: "fitness",
                        principalTable: "CorporateAccounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeferredRevenueEntries",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recognised_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    trigger = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    units_consumed = table.Column<int>(type: "integer", nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deferred_revenue_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_deferred_revenue_entries_deferred_revenue_schedule_id",
                        column: x => x.schedule_id,
                        principalSchema: "fitness",
                        principalTable: "DeferredRevenue",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DunningSteps",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dunning_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_number = table.Column<int>(type: "integer", nullable: false),
                    delay_days = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: true),
                    message_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    skip_on_technical_failure = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dunning_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_dunning_steps_dunning_policies_dunning_policy_id",
                        column: x => x.dunning_policy_id,
                        principalSchema: "fitness",
                        principalTable: "DunningPolicies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentUsage",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cumulative_hours = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    hours_since_last_read = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    distance_km = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    session_count = table.Column<int>(type: "integer", nullable: true),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_usage", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipment_usage_equipment_equipment_asset_id",
                        column: x => x.equipment_asset_id,
                        principalSchema: "fitness",
                        principalTable: "Equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSchedules",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applies_to_category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    task_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    trigger = table.Column<int>(type: "integer", nullable: false),
                    interval_days = table.Column<int>(type: "integer", nullable: false),
                    interval_usage_hours = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    last_performed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                    default_assignee_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    auto_create_work_order = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenance_schedules_equipment_equipment_asset_id",
                        column: x => x.equipment_asset_id,
                        principalSchema: "fitness",
                        principalTable: "Equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    maintenance_schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fault_report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    detail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    raised_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contractor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contractor_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    labour_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    parts_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    parts_used = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    downtime_hours = table.Column<int>(type: "integer", nullable: false),
                    resolution_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    photo_urls = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    purchase_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_orders_equipment_equipment_asset_id",
                        column: x => x.equipment_asset_id,
                        principalSchema: "fitness",
                        principalTable: "Equipment",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "FacilityCheckItems",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    answer_kind = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    acceptable_low = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    acceptable_high = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    is_critical = table.Column<bool>(type: "boolean", nullable: false),
                    requires_photo = table.Column<bool>(type: "boolean", nullable: false),
                    last_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_completed_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_passed = table.Column<bool>(type: "boolean", nullable: true),
                    last_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    last_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facility_check_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_facility_check_items_facility_checks_facility_check_id",
                        column: x => x.facility_check_id,
                        principalSchema: "fitness",
                        principalTable: "FacilityChecks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftCardTransactions",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gift_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gift_card_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_gift_card_transactions_gift_cards_gift_card_id",
                        column: x => x.gift_card_id,
                        principalSchema: "fitness",
                        principalTable: "GiftCards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    preferred_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    national_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    occupation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    alternate_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    post_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    preferred_channel = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    home_club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    left_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    first_joined_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    household_id = table.Column<Guid>(type: "uuid", nullable: true),
                    corporate_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_coach_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lead_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    referred_by_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    last_visit_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_visits = table.Column<int>(type: "integer", nullable: false),
                    visits_this_month = table.Column<int>(type: "integer", nullable: false),
                    visit_frequency_baseline = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    next_billing_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    risk_band = table.Column<int>(type: "integer", nullable: false),
                    current_streak_days = table.Column<int>(type: "integer", nullable: false),
                    loyalty_points = table.Column<int>(type: "integer", nullable: false),
                    waiver_signed = table.Column<bool>(type: "boolean", nullable: false),
                    waiver_signed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    waiver_template_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    medical_clearance = table.Column<int>(type: "integer", nullable: false),
                    medical_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false),
                    ban_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ban_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    banned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    photo_consent = table.Column<bool>(type: "boolean", nullable: false),
                    leaderboard_opt_in = table.Column<bool>(type: "boolean", nullable: false),
                    is_anonymised = table.Column<bool>(type: "boolean", nullable: false),
                    anonymised_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_members_clubs_home_club_id",
                        column: x => x.home_club_id,
                        principalSchema: "fitness",
                        principalTable: "Clubs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_members_households_household_id",
                        column: x => x.household_id,
                        principalSchema: "fitness",
                        principalTable: "Households",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "IncidentActions",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    assigned_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raised_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completion_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_overdue = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incident_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_incident_actions_incidents_incident_id",
                        column: x => x.incident_id,
                        principalSchema: "fitness",
                        principalTable: "Incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JourneySteps",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_number = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    delay_hours = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: true),
                    message_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condition_expression = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    on_false_step_number = table.Column<int>(type: "integer", nullable: true),
                    tag_to_apply = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    offer_promotion_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loyalty_points_to_grant = table.Column<int>(type: "integer", nullable: false),
                    task_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    task_assign_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journey_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_journey_steps_journeys_engagement_journey_id",
                        column: x => x.engagement_journey_id,
                        principalSchema: "fitness",
                        principalTable: "Journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    lead_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    referred_by_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    promo_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    goal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    interested_in_plan_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_contacted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    response_minutes = table.Column<int>(type: "integer", nullable: true),
                    sla_breached = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_follow_up_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contact_attempts = table.Column<int>(type: "integer", nullable: false),
                    tour_booked_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    toured_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_started_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trial_ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    won_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resulting_agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    won_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    lost_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    loss_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loss_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    attributed_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leads", x => x.id);
                    table.ForeignKey(
                        name: "fk_leads_lead_sources_lead_source_id",
                        column: x => x.lead_source_id,
                        principalSchema: "fitness",
                        principalTable: "LeadSources",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Lockers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    locker_bank_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    size = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    lock_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    key_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    monthly_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    annual_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    deposit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    current_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    out_of_order_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_cleaned_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lockers", x => x.id);
                    table.ForeignKey(
                        name: "fk_lockers_locker_banks_locker_bank_id",
                        column: x => x.locker_bank_id,
                        principalSchema: "fitness",
                        principalTable: "LockerBanks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceBookings",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_occurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    attendee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    attendee_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    booked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attended = table.Column<bool>(type: "boolean", nullable: false),
                    amount_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_reconciled = table.Column<bool>(type: "boolean", nullable: false),
                    reconciled_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_marketplace_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_marketplace_bookings_marketplace_channels_marketplace_channel",
                        column: x => x.marketplace_channel_id,
                        principalSchema: "fitness",
                        principalTable: "MarketplaceChannels",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "PlanEntitlements",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    limit = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    overage_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allow_overage = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_entitlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_entitlements_plans_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "fitness",
                        principalTable: "Plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanPrices",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    joining_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_prices_plans_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "fitness",
                        principalTable: "Plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_text = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    max_uses = table.Column<int>(type: "integer", nullable: false),
                    use_count = table.Column<int>(type: "integer", nullable: false),
                    one_per_member = table.Column<bool>(type: "boolean", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issued_to_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promo_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_promo_codes_promotions_promotion_rule_id",
                        column: x => x.promotion_rule_id,
                        principalSchema: "fitness",
                        principalTable: "Promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RankLevels",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_ladder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    badge_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    required_attendances = table.Column<int>(type: "integer", nullable: false),
                    minimum_months_at_previous = table.Column<int>(type: "integer", nullable: false),
                    requirements_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    grading_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    minimum_age = table.Column<int>(type: "integer", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rank_levels", x => x.id);
                    table.ForeignKey(
                        name: "fk_rank_levels_rank_ladders_rank_ladder_id",
                        column: x => x.rank_ladder_id,
                        principalSchema: "fitness",
                        principalTable: "RankLadders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceSlotRules",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bookable_resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ends_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    is_peak = table.Column<bool>(type: "boolean", nullable: false),
                    rate_override = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    block_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_slot_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_resource_slot_rules_resources_bookable_resource_id",
                        column: x => x.bookable_resource_id,
                        principalSchema: "fitness",
                        principalTable: "Resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramDays",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    publish_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    coach_brief = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_program_days_program_tracks_program_track_id",
                        column: x => x.program_track_id,
                        principalSchema: "fitness",
                        principalTable: "ProgramTracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_program_days_workouts_workout_id",
                        column: x => x.workout_id,
                        principalSchema: "fitness",
                        principalTable: "Workouts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSections",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    rounds = table.Column<int>(type: "integer", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    rest_seconds = table.Column<int>(type: "integer", nullable: true),
                    score_type = table.Column<int>(type: "integer", nullable: true),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_workout_sections_workouts_workout_id",
                        column: x => x.workout_id,
                        principalSchema: "fitness",
                        principalTable: "Workouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doors",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    controller_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reader_address = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    hardware_kind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    counts_occupancy = table.Column<bool>(type: "boolean", nullable: false),
                    requires_class_booking = table.Column<bool>(type: "boolean", nullable: false),
                    class_booking_window_minutes = table.Column<int>(type: "integer", nullable: false),
                    staff_only = table.Column<bool>(type: "boolean", nullable: false),
                    anti_passback_override = table.Column<int>(type: "integer", nullable: true),
                    is_held_open = table.Column<bool>(type: "boolean", nullable: false),
                    held_open_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doors", x => x.id);
                    table.ForeignKey(
                        name: "fk_doors_areas_area_id",
                        column: x => x.area_id,
                        principalSchema: "fitness",
                        principalTable: "Areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_doors_clubs_club_id",
                        column: x => x.club_id,
                        principalSchema: "fitness",
                        principalTable: "Clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_doors_controllers_controller_id",
                        column: x => x.controller_id,
                        principalSchema: "fitness",
                        principalTable: "Controllers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    has_spot_map = table.Column<bool>(type: "boolean", nullable: false),
                    grid_columns = table.Column<int>(type: "integer", nullable: false),
                    grid_rows = table.Column<int>(type: "integer", nullable: false),
                    equipment_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_out_of_service = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rooms", x => x.id);
                    table.ForeignKey(
                        name: "fk_rooms_areas_area_id",
                        column: x => x.area_id,
                        principalSchema: "fitness",
                        principalTable: "Areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_rooms_clubs_club_id",
                        column: x => x.club_id,
                        principalSchema: "fitness",
                        principalTable: "Clubs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookableStaff",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    specialities = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    service_ids = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    hourly_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    bookable_online = table.Column<bool>(type: "boolean", nullable: false),
                    default_buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    booking_window_days = table.Column<int>(type: "integer", nullable: false),
                    is_contractor = table.Column<bool>(type: "boolean", nullable: false),
                    max_clients_per_day = table.Column<int>(type: "integer", nullable: false),
                    accepting_new_clients = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookable_staff", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookable_staff_staff_staff_id",
                        column: x => x.staff_id,
                        principalSchema: "fitness",
                        principalTable: "Staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Certifications",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    issuing_body = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reference_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    issued_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    document_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    blocks_work_on_expiry = table.Column<bool>(type: "boolean", nullable: false),
                    reminder_sent = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_certifications_staff_staff_id",
                        column: x => x.staff_id,
                        principalSchema: "fitness",
                        principalTable: "Staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommissionAccruals",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commission_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    commission_statement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    basis = table.Column<int>(type: "integer", nullable: false),
                    earned_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    base_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    narrative = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_reversed = table.Column<bool>(type: "boolean", nullable: false),
                    reversal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_accruals", x => x.id);
                    table.ForeignKey(
                        name: "fk_commission_accruals_staff_staff_id",
                        column: x => x.staff_id,
                        principalSchema: "fitness",
                        principalTable: "Staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "CommissionStatements",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    session_commission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    class_commission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sales_commission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    retail_commission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bonus = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    adjustments = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    sessions_delivered = table.Column<int>(type: "integer", nullable: false),
                    classes_taught = table.Column<int>(type: "integer", nullable: false),
                    memberships_sold = table.Column<int>(type: "integer", nullable: false),
                    packages_sold = table.Column<int>(type: "integer", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    exported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payroll_reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_statements", x => x.id);
                    table.ForeignKey(
                        name: "fk_commission_statements_staff_staff_id",
                        column: x => x.staff_id,
                        principalSchema: "fitness",
                        principalTable: "Staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ShiftAssignments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    clocked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    clocked_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    was_no_show = table.Column<bool>(type: "boolean", nullable: false),
                    was_late = table.Column<bool>(type: "boolean", nullable: false),
                    late_minutes = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_assignments_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "fitness",
                        principalTable: "Shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shift_assignments_staff_staff_id",
                        column: x => x.staff_id,
                        principalSchema: "fitness",
                        principalTable: "Staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "TimeClock",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    clocked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    clocked_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    break_minutes = table.Column<int>(type: "integer", nullable: false),
                    worked_minutes = table.Column<int>(type: "integer", nullable: true),
                    device = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    geofence_passed = table.Column<bool>(type: "boolean", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    was_edited = table.Column<bool>(type: "boolean", nullable: false),
                    edit_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_clock", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_clock_staff_staff_id",
                        column: x => x.staff_id,
                        principalSchema: "fitness",
                        principalTable: "Staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Agreements",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version = table.Column<int>(type: "integer", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    minimum_term_ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    signed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_effective_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cooling_off_ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    billing_period = table.Column<int>(type: "integer", nullable: false),
                    billing_anchor = table.Column<int>(type: "integer", nullable: false),
                    billing_day_of_month = table.Column<int>(type: "integer", nullable: true),
                    notice_period_days = table.Column<int>(type: "integer", nullable: false),
                    auto_renews = table.Column<bool>(type: "boolean", nullable: false),
                    price_locked = table.Column<bool>(type: "boolean", nullable: false),
                    promotion_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    promo_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    promotional_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    promotional_periods_remaining = table.Column<int>(type: "integer", nullable: false),
                    credits_granted = table.Column<int>(type: "integer", nullable: false),
                    credits_remaining = table.Column<int>(type: "integer", nullable: false),
                    credits_expire_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_billing_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_billed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    periods_billed = table.Column<int>(type: "integer", nullable: false),
                    total_instalments = table.Column<int>(type: "integer", nullable: false),
                    payment_method_ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payer_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    corporate_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    third_party_payer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leave_reason = table.Column<int>(type: "integer", nullable: true),
                    leave_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    early_termination_fee_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    supersedes_agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    superseded_by_agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sold_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signature_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    document_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agreements", x => x.id);
                    table.ForeignKey(
                        name: "fk_agreements_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_agreements_plans_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "fitness",
                        principalTable: "Plans",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    payment_kind = table.Column<int>(type: "integer", nullable: false),
                    session_package_purchase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_credit_movement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credits_used = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    penalty_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rescheduled_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    plan_for_next_session = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_first_session = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointments", x => x.id);
                    table.ForeignKey(
                        name: "fk_appointments_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_appointments_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "fitness",
                        principalTable: "Services",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    recommendations = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    device_source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    device_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    next_due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shared_with_member = table.Column<bool>(type: "boolean", nullable: false),
                    report_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_assessments_assessment_templates_assessment_template_id",
                        column: x => x.assessment_template_id,
                        principalSchema: "fitness",
                        principalTable: "AssessmentTemplates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_assessments_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Authorisations",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    third_party_payer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authorisation_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_units = table.Column<int>(type: "integer", nullable: false),
                    used_units = table.Column<int>(type: "integer", nullable: false),
                    rate_per_unit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    approved_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    invoiced_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    referrer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_exhausted = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authorisations", x => x.id);
                    table.ForeignKey(
                        name: "fk_authorisations_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_authorisations_payers_third_party_payer_id",
                        column: x => x.third_party_payer_id,
                        principalSchema: "fitness",
                        principalTable: "Payers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeParticipants",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: true),
                    team_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    has_completed = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    points_awarded = table.Column<bool>(type: "boolean", nullable: false),
                    last_progress_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_challenge_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_challenge_participants_challenges_challenge_id",
                        column: x => x.challenge_id,
                        principalSchema: "fitness",
                        principalTable: "Challenges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_challenge_participants_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheckIns",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    checked_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    auto_closed = table.Column<bool>(type: "boolean", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    door_id = table.Column<Guid>(type: "uuid", nullable: true),
                    area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    host_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    day_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    was_manual_entry = table.Column<bool>(type: "boolean", nullable: false),
                    checked_in_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumed_visit_allowance = table.Column<bool>(type: "boolean", nullable: false),
                    fee_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_check_ins", x => x.id);
                    table.ForeignKey(
                        name: "fk_check_ins_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ChurnScores",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    computed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    band = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    previous_band = table.Column<int>(type: "integer", nullable: true),
                    previous_score = table.Column<int>(type: "integer", nullable: true),
                    band_worsened = table.Column<bool>(type: "boolean", nullable: false),
                    days_since_last_visit = table.Column<int>(type: "integer", nullable: false),
                    visits_per_week_now = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    visits_per_week_baseline = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    tenure_days = table.Column<int>(type: "integer", nullable: false),
                    has_upcoming_booking = table.Column<bool>(type: "boolean", nullable: false),
                    has_outstanding_balance = table.Column<bool>(type: "boolean", nullable: false),
                    has_failed_payment = table.Column<bool>(type: "boolean", nullable: false),
                    days_to_contract_end = table.Column<int>(type: "integer", nullable: false),
                    owner_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_actioned = table.Column<bool>(type: "boolean", nullable: false),
                    actioned_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_churn_scores", x => x.id);
                    table.ForeignKey(
                        name: "fk_churn_scores_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clearances",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    health_screening_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    requested_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submitted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    practitioner_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    practitioner_registration = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    practice_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    restrictions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    blocks_participation = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clearances", x => x.id);
                    table.ForeignKey(
                        name: "fk_clearances_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachAssignments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coach_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_assignments_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachCheckIns",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    member_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    member_response = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    energy_rating = table.Column<int>(type: "integer", nullable: true),
                    sleep_rating = table.Column<int>(type: "integer", nullable: true),
                    stress_rating = table.Column<int>(type: "integer", nullable: true),
                    adherence_rating = table.Column<int>(type: "integer", nullable: true),
                    coach_replied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    coach_response = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    adjustments_made = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    was_missed = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coach_check_ins", x => x.id);
                    table.ForeignKey(
                        name: "fk_coach_check_ins_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Complaints",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    complainant_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    complainant_contact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    detail = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    raised_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acknowledged_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    target_resolution_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    compensation_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    compensation_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    complainant_satisfied = table.Column<bool>(type: "boolean", nullable: true),
                    channel = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaints", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaints_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Consents",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    purpose = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    granted = table.Column<bool>(type: "boolean", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consent_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    captured_via = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_consents_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CorporateMembers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    corporate_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    department = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    joined_scheme_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_scheme_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    eligibility_verified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    eligibility_expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    proof_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employer_contribution = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    employee_contribution = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    leave_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_corporate_members_corporate_accounts_corporate_account_id",
                        column: x => x.corporate_account_id,
                        principalSchema: "fitness",
                        principalTable: "CorporateAccounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_corporate_members_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrolments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_sessions = table.Column<int>(type: "integer", nullable: false),
                    sessions_attended = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    is_withdrawn = table.Column<bool>(type: "boolean", nullable: false),
                    withdrawal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_enrolments", x => x.id);
                    table.ForeignKey(
                        name: "fk_course_enrolments_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    issued_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivation_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    replacement_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    replaces_credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issued_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credentials", x => x.id);
                    table.ForeignKey(
                        name: "fk_credentials_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditBalances",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_movement_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_credit_balances_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DunningCases",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dunning_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    opened_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    amount_outstanding = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_recovered = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    late_fees_added = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    initial_failure_reason = table.Column<int>(type: "integer", nullable: false),
                    current_step = table.Column<int>(type: "integer", nullable: false),
                    next_step_due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_retry_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    pause_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    assigned_to_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dunning_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_dunning_cases_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "EffortSessions",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_occurrence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    check_in_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    effort_points = table.Column<int>(type: "integer", nullable: false),
                    grey_minutes = table.Column<int>(type: "integer", nullable: false),
                    blue_minutes = table.Column<int>(type: "integer", nullable: false),
                    green_minutes = table.Column<int>(type: "integer", nullable: false),
                    yellow_minutes = table.Column<int>(type: "integer", nullable: false),
                    red_minutes = table.Column<int>(type: "integer", nullable: false),
                    average_heart_rate = table.Column<int>(type: "integer", nullable: true),
                    peak_heart_rate = table.Column<int>(type: "integer", nullable: true),
                    calories_burned = table.Column<int>(type: "integer", nullable: true),
                    peak_zone = table.Column<int>(type: "integer", nullable: false),
                    device_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    external_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_effort_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_effort_sessions_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    relationship = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    alternate_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emergency_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_emergency_contacts_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    measure_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    start_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    target_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    current_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    set_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    target_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    achieved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    progress_percent = table.Column<int>(type: "integer", nullable: false),
                    set_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    why_it_matters = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goals", x => x.id);
                    table.ForeignKey(
                        name: "fk_goals_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestVisits",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    guest_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    guest_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    guest_date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    visited_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    waiver_signed = table.Column<bool>(type: "boolean", nullable: false),
                    waiver_signature_id = table.Column<Guid>(type: "uuid", nullable: true),
                    used_host_allowance = table.Column<bool>(type: "boolean", nullable: false),
                    fee_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    check_in_id = table.Column<Guid>(type: "uuid", nullable: true),
                    temporary_credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_lead_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_visits", x => x.id);
                    table.ForeignKey(
                        name: "fk_guest_visits_members_host_member_id",
                        column: x => x.host_member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Habits",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutrition_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    habit_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    daily_target = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    longest_streak = table.Column<int>(type: "integer", nullable: false),
                    adherence_percent = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_habits", x => x.id);
                    table.ForeignKey(
                        name: "fk_habits_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthScreenings",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    template_version = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    requires_clearance = table.Column<bool>(type: "boolean", nullable: false),
                    clearance_status = table.Column<int>(type: "integer", nullable: false),
                    risk_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    captured_via = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_screenings", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_screenings_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseAccountCharges",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    charged_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    charge_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    settled_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_settled = table.Column<bool>(type: "boolean", nullable: false),
                    settled_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    authorised_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_house_account_charges", x => x.id);
                    table.ForeignKey(
                        name: "fk_house_account_charges_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "HouseholdMembers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    may_collect_children = table.Column<bool>(type: "boolean", nullable: false),
                    ages_out_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_household_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_household_members_households_household_id",
                        column: x => x.household_id,
                        principalSchema: "fitness",
                        principalTable: "Households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_household_members_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    corporate_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    third_party_payer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payer_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    issued_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_refunded = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    billing_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dunning_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reminders_suppressed = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoices_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "JourneyEnrolments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrolled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_step = table.Column<int>(type: "integer", nullable: false),
                    next_step_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    exit_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    was_successful = table.Column<bool>(type: "boolean", nullable: false),
                    success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journey_enrolments", x => x.id);
                    table.ForeignKey(
                        name: "fk_journey_enrolments_journeys_engagement_journey_id",
                        column: x => x.engagement_journey_id,
                        principalSchema: "fitness",
                        principalTable: "Journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_journey_enrolments_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ledger",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    entry_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_note_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: true),
                    write_off_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger", x => x.id);
                    table.ForeignKey(
                        name: "fk_ledger_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyAccounts",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points_balance = table.Column<int>(type: "integer", nullable: false),
                    lifetime_points = table.Column<int>(type: "integer", nullable: false),
                    points_redeemed = table.Column<int>(type: "integer", nullable: false),
                    points_expired = table.Column<int>(type: "integer", nullable: false),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tier_achieved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    points_to_next_tier = table.Column<int>(type: "integer", nullable: false),
                    next_expiry_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    points_expiring_soon = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_loyalty_accounts_loyalty_tiers_tier_id",
                        column: x => x.tier_id,
                        principalSchema: "fitness",
                        principalTable: "LoyaltyTiers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_loyalty_accounts_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalFlags",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    visible_to_instructors = table.Column<bool>(type: "boolean", nullable: false),
                    review_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medical_flags", x => x.id);
                    table.ForeignKey(
                        name: "fk_medical_flags_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberAlerts",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    action_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    action_route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    blocks_access = table.Column<bool>(type: "boolean", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_member_alerts_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberBadges",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    earned_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    context = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    times_earned = table.Column<int>(type: "integer", nullable: false),
                    notification_sent = table.Column<bool>(type: "boolean", nullable: false),
                    awarded_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_badges", x => x.id);
                    table.ForeignKey(
                        name: "fk_member_badges_badges_badge_id",
                        column: x => x.badge_id,
                        principalSchema: "fitness",
                        principalTable: "Badges",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_member_badges_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberDocuments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_member_documents_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberNotes",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_member_notes_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberStatusHistory",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<int>(type: "integer", nullable: false),
                    to_status = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_member_status_history_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberTags",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    is_system_tag = table.Column<bool>(type: "boolean", nullable: false),
                    colour_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_member_tags_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NpsResponses",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    band = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    trigger = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    class_occurrence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    followed_up = table.Column<bool>(type: "boolean", nullable: false),
                    followed_up_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    followed_up_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    follow_up_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nps_responses", x => x.id);
                    table.ForeignKey(
                        name: "fk_nps_responses_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionPlans",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    daily_calories = table.Column<int>(type: "integer", nullable: true),
                    protein_grams = table.Column<int>(type: "integer", nullable: true),
                    carb_grams = table.Column<int>(type: "integer", nullable: true),
                    fat_grams = table.Column<int>(type: "integer", nullable: true),
                    fibre_grams = table.Column<int>(type: "integer", nullable: true),
                    water_millilitres = table.Column<int>(type: "integer", nullable: true),
                    meal_guidance = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    restrictions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    supplement_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    disclaimer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nutrition_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_nutrition_plans_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackagePurchases",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchased_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sessions_purchased = table.Column<int>(type: "integer", nullable: false),
                    sessions_used = table.Column<int>(type: "integer", nullable: false),
                    sessions_remaining = table.Column<int>(type: "integer", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_per_session = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_expired = table.Column<bool>(type: "boolean", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deferred_revenue_schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sold_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_transferable = table.Column<bool>(type: "boolean", nullable: false),
                    is_refundable = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_purchases", x => x.id);
                    table.ForeignKey(
                        name: "fk_package_purchases_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    provider_token = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    provider_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    card_brand = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    card_last_four = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    expiry_month = table.Column<int>(type: "integer", nullable: true),
                    expiry_year = table.Column<int>(type: "integer", nullable: true),
                    bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    account_last_four = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    account_holder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_expiring_soon = table.Column<bool>(type: "boolean", nullable: false),
                    last_failed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_methods_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalRecords",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    record_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    score_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    rep_max = table.Column<int>(type: "integer", nullable: true),
                    achieved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    workout_result_id = table.Column<Guid>(type: "uuid", nullable: true),
                    previous_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    previous_achieved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personal_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_personal_records_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Preferences",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    units = table.Column<int>(type: "integer", nullable: false),
                    class_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    class_reminder_minutes_before = table.Column<int>(type: "integer", nullable: false),
                    billing_reminders = table.Column<bool>(type: "boolean", nullable: false),
                    marketing_messages = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_leaderboards = table.Column<bool>(type: "boolean", nullable: false),
                    interests_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_preferences_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressPhotos",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    taken_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    consent_given = table.Column<bool>(type: "boolean", nullable: false),
                    consent_given_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    may_use_in_marketing = table.Column<bool>(type: "boolean", nullable: false),
                    taken_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progress_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_progress_photos_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    referrer_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referred_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    referred_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    referred_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: true),
                    referred_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    referred_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    referral_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    converted = table.Column<bool>(type: "boolean", nullable: false),
                    converted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    referrer_reward_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referrer_reward_points = table.Column<int>(type: "integer", nullable: false),
                    referrer_rewarded = table.Column<bool>(type: "boolean", nullable: false),
                    referrer_rewarded_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    referred_reward_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referred_rewarded = table.Column<bool>(type: "boolean", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referrals", x => x.id);
                    table.ForeignKey(
                        name: "fk_referrals_members_referrer_member_id",
                        column: x => x.referrer_member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceBookings",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    bookable_resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    guest_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    participant_count = table.Column<int>(type: "integer", nullable: false),
                    participant_member_ids = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    penalty_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    booked_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_resource_bookings_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_resource_bookings_resources_bookable_resource_id",
                        column: x => x.bookable_resource_id,
                        principalSchema: "fitness",
                        principalTable: "Resources",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "RetentionTasks",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    trigger = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    churn_score_id = table.Column<Guid>(type: "uuid", nullable: true),
                    journey_enrolment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outcome = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_dismissed = table.Column<bool>(type: "boolean", nullable: false),
                    dismiss_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_retention_tasks_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sold_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_method = table.Column<int>(type: "integer", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_house_account_charge = table.Column<bool>(type: "boolean", nullable: false),
                    cash_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sold_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    returns_sale_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_return = table.Column<bool>(type: "boolean", nullable: false),
                    return_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    stock_depleted = table.Column<bool>(type: "boolean", nullable: false),
                    discount_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    discount_approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "SessionCredits",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_package_purchase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    granted = table.Column<int>(type: "integer", nullable: false),
                    used = table.Column<int>(type: "integer", nullable: false),
                    remaining = table.Column<int>(type: "integer", nullable: false),
                    held = table.Column<int>(type: "integer", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_expired = table.Column<bool>(type: "boolean", nullable: false),
                    unit_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_session_credits", x => x.id);
                    table.ForeignKey(
                        name: "fk_session_credits_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillClearances",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cleared_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cleared_by_staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_clearances", x => x.id);
                    table.ForeignKey(
                        name: "fk_skill_clearances_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Streaks",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cadence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_count = table.Column<int>(type: "integer", nullable: false),
                    longest_count = table.Column<int>(type: "integer", nullable: false),
                    started_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_qualifying_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    broken_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    required_per_period = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streaks", x => x.id);
                    table.ForeignKey(
                        name: "fk_streaks_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Strikes",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    was_no_show = table.Column<bool>(type: "boolean", nullable: false),
                    class_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fee_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_waived = table.Column<bool>(type: "boolean", nullable: false),
                    waived_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    waived_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strikes", x => x.id);
                    table.ForeignKey(
                        name: "fk_strikes_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Suspensions",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lifted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    reason_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    continues_billing = table.Column<bool>(type: "boolean", nullable: false),
                    auto_lifts_when_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    imposed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lifted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suspensions", x => x.id);
                    table.ForeignKey(
                        name: "fk_suspensions_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaiverSignatures",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    waiver_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version = table.Column<int>(type: "integer", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    signer_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    signer_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    signer_date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    guest_visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    day_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    guardian_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    guardian_relationship = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    guardian_signature_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    signature_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    document_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    captured_via = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    consent_answers_json = table.Column<string>(type: "text", nullable: true),
                    remote_token = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    remote_token_expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waiver_signatures", x => x.id);
                    table.ForeignKey(
                        name: "fk_waiver_signatures_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_waiver_signatures_waiver_templates_waiver_template_id",
                        column: x => x.waiver_template_id,
                        principalSchema: "fitness",
                        principalTable: "WaiverTemplates",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutResults",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_occurrence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    program_track_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    score_type = table.Column<int>(type: "integer", nullable: false),
                    time_seconds = table.Column<int>(type: "integer", nullable: true),
                    rounds = table.Column<int>(type: "integer", nullable: true),
                    reps = table.Column<int>(type: "integer", nullable: true),
                    load_kg = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    distance_metres = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    calories = table.Column<int>(type: "integer", nullable: true),
                    points = table.Column<int>(type: "integer", nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: true),
                    normalised_score = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    was_scaled = table.Column<bool>(type: "boolean", nullable: false),
                    scaling_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    did_not_finish = table.Column<bool>(type: "boolean", nullable: false),
                    member_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    coach_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_personal_record = table.Column<bool>(type: "boolean", nullable: false),
                    entered_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_workout_results_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workout_results_workouts_workout_id",
                        column: x => x.workout_id,
                        principalSchema: "fitness",
                        principalTable: "Workouts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "LeadActivities",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    outcome = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    from_status = table.Column<int>(type: "integer", nullable: true),
                    to_status = table.Column<int>(type: "integer", nullable: true),
                    was_successful_contact = table.Column<bool>(type: "boolean", nullable: false),
                    message_log_id = table.Column<Guid>(type: "uuid", nullable: true),
                    follow_up_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lead_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_lead_activities_leads_lead_id",
                        column: x => x.lead_id,
                        principalSchema: "fitness",
                        principalTable: "Leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tours",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    arrived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    was_no_show = table.Column<bool>(type: "boolean", nullable: false),
                    was_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    converted_on_day = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reminder_sent = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tours", x => x.id);
                    table.ForeignKey(
                        name: "fk_tours_leads_lead_id",
                        column: x => x.lead_id,
                        principalSchema: "fitness",
                        principalTable: "Leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trials",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    visits_allowed = table.Column<int>(type: "integer", nullable: false),
                    visits_used = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    day_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    converted = table.Column<bool>(type: "boolean", nullable: false),
                    converted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resulting_agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversion_sequence_started = table.Column<bool>(type: "boolean", nullable: false),
                    issued_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trials", x => x.id);
                    table.ForeignKey(
                        name: "fk_trials_leads_lead_id",
                        column: x => x.lead_id,
                        principalSchema: "fitness",
                        principalTable: "Leads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LockerAssignments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    locker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_day_use = table.Column<bool>(type: "boolean", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    deposit_held = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    deposit_returned = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    auto_renews = table.Column<bool>(type: "boolean", nullable: false),
                    next_billing_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiry_notice_sent = table.Column<bool>(type: "boolean", nullable: false),
                    expiry_notice_sent_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    was_reclaimed = table.Column<bool>(type: "boolean", nullable: false),
                    reclaim_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    key_issued = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    key_returned = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locker_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_locker_assignments_lockers_locker_id",
                        column: x => x.locker_id,
                        principalSchema: "fitness",
                        principalTable: "Lockers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_locker_assignments_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "AccessTimeBands",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entitlement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ends_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_time_bands", x => x.id);
                    table.ForeignKey(
                        name: "fk_access_time_bands_plan_entitlements_entitlement_id",
                        column: x => x.entitlement_id,
                        principalSchema: "fitness",
                        principalTable: "PlanEntitlements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberRanks",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_ladder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    awarded_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    awarded_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grading_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    attendances_at_rank = table.Column<int>(type: "integer", nullable: false),
                    certificate_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_ranks", x => x.id);
                    table.ForeignKey(
                        name: "fk_member_ranks_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_member_ranks_rank_levels_rank_level_id",
                        column: x => x.rank_level_id,
                        principalSchema: "fitness",
                        principalTable: "RankLevels",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutMovements",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    movement_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    sets = table.Column<int>(type: "integer", nullable: true),
                    reps = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    load_kg = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    load_percent_of_max = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    distance_metres = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    calories = table.Column<int>(type: "integer", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    rest_seconds = table.Column<int>(type: "integer", nullable: true),
                    tempo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    scaling_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_workout_movements_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalSchema: "fitness",
                        principalTable: "Exercises",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_workout_movements_workout_sections_workout_section_id",
                        column: x => x.workout_section_id,
                        principalSchema: "fitness",
                        principalTable: "WorkoutSections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassOccurrences",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instructor_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    substitute_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    booked_count = table.Column<int>(type: "integer", nullable: false),
                    waitlist_count = table.Column<int>(type: "integer", nullable: false),
                    attended_count = table.Column<int>(type: "integer", nullable: false),
                    no_show_count = table.Column<int>(type: "integer", nullable: false),
                    marketplace_capacity = table.Column<int>(type: "integer", nullable: false),
                    marketplace_booked_count = table.Column<int>(type: "integer", nullable: false),
                    booking_opens_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    booking_closes_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    spot_release_minutes = table.Column<int>(type: "integer", nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_notified = table.Column<bool>(type: "boolean", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "fk_class_occurrences_class_types_class_type_id",
                        column: x => x.class_type_id,
                        principalSchema: "fitness",
                        principalTable: "ClassTypes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_class_occurrences_rooms_room_id",
                        column: x => x.room_id,
                        principalSchema: "fitness",
                        principalTable: "Rooms",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ClassSchedules",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instructor_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    days_of_week_mask = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    marketplace_capacity = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    repeat_every_weeks = table.Column<int>(type: "integer", nullable: false),
                    generate_ahead_days = table.Column<int>(type: "integer", nullable: false),
                    generated_through = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    season_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_class_schedules_class_types_class_type_id",
                        column: x => x.class_type_id,
                        principalSchema: "fitness",
                        principalTable: "ClassTypes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_class_schedules_rooms_room_id",
                        column: x => x.room_id,
                        principalSchema: "fitness",
                        principalTable: "Rooms",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "RoomSpots",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grid_column = table.Column<int>(type: "integer", nullable: false),
                    grid_row = table.Column<int>(type: "integer", nullable: false),
                    equipment_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_reserved = table.Column<bool>(type: "boolean", nullable: false),
                    reserved_note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_out_of_service = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_spots", x => x.id);
                    table.ForeignKey(
                        name: "fk_room_spots_rooms_room_id",
                        column: x => x.room_id,
                        principalSchema: "fitness",
                        principalTable: "Rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Availability",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bookable_staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: true),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    starts_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ends_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    break_starts_at = table.Column<TimeSpan>(type: "interval", nullable: true),
                    break_ends_at = table.Column<TimeSpan>(type: "interval", nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availability", x => x.id);
                    table.ForeignKey(
                        name: "fk_availability_bookable_staff_bookable_staff_id",
                        column: x => x.bookable_staff_id,
                        principalSchema: "fitness",
                        principalTable: "BookableStaff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SwapRequests",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offered_to_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_swap_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_swap_requests_shift_assignments_shift_assignment_id",
                        column: x => x.shift_assignment_id,
                        principalSchema: "fitness",
                        principalTable: "ShiftAssignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgreementSignatures",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_version = table.Column<int>(type: "integer", nullable: false),
                    signer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    guardian_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    guardian_relationship = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    signature_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    captured_via = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    remote_token = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    remote_token_expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agreement_signatures", x => x.id);
                    table.ForeignKey(
                        name: "fk_agreement_signatures_agreements_agreement_id",
                        column: x => x.agreement_id,
                        principalSchema: "fitness",
                        principalTable: "Agreements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Amendments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    effective_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    previous_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    new_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    previous_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    change_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    proration_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_amendments", x => x.id);
                    table.ForeignKey(
                        name: "fk_amendments_agreements_agreement_id",
                        column: x => x.agreement_id,
                        principalSchema: "fitness",
                        principalTable: "Agreements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingSchedules",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_number = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    charge_kind = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_billed = table.Column<bool>(type: "boolean", nullable: false),
                    is_skipped = table.Column<bool>(type: "boolean", nullable: false),
                    skip_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    original_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    adjustment_note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_billing_schedules_agreements_agreement_id",
                        column: x => x.agreement_id,
                        principalSchema: "fitness",
                        principalTable: "Agreements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CancellationRequests",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    reason_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    channel = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    early_termination_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refund_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    outstanding_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    was_saved = table.Column<bool>(type: "boolean", nullable: false),
                    saved_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    processed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    win_back_task_created = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cancellation_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_cancellation_requests_agreements_agreement_id",
                        column: x => x.agreement_id,
                        principalSchema: "fitness",
                        principalTable: "Agreements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Freezes",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actually_ended_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    reason_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fee_per_period = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_fee_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    days_extended = table.Column<int>(type: "integer", nullable: false),
                    supporting_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_medical = table.Column<bool>(type: "boolean", nullable: false),
                    counts_against_allowance = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_released = table.Column<bool>(type: "boolean", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_freezes", x => x.id);
                    table.ForeignKey(
                        name: "fk_freezes_agreements_agreement_id",
                        column: x => x.agreement_id,
                        principalSchema: "fitness",
                        principalTable: "Agreements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentParticipants",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    credits_used = table.Column<int>(type: "integer", nullable: false),
                    session_credit_movement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    penalty_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointment_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_appointment_participants_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "fitness",
                        principalTable: "Appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_appointment_participants_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "SignOffs",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signed_off_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    member_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    member_signature_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    credits_consumed = table.Column<int>(type: "integer", nullable: false),
                    session_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    commission_accrued = table.Column<bool>(type: "boolean", nullable: false),
                    commission_accrual_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sign_offs", x => x.id);
                    table.ForeignKey(
                        name: "fk_sign_offs_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "fitness",
                        principalTable: "Appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentValues",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    measure_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    measure_type = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    numeric_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    text_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    previous_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    change = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    change_percent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    norm_band = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    percentile = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessment_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_assessment_values_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalSchema: "fitness",
                        principalTable: "Assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChurnFactors",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    churn_score_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    suggested_action = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_churn_factors", x => x.id);
                    table.ForeignKey(
                        name: "fk_churn_factors_churn_scores_churn_score_id",
                        column: x => x.churn_score_id,
                        principalSchema: "fitness",
                        principalTable: "ChurnScores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DunningEvents",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dunning_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_number = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    amount_collected = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_log_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dunning_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_dunning_events_dunning_cases_dunning_case_id",
                        column: x => x.dunning_case_id,
                        principalSchema: "fitness",
                        principalTable: "DunningCases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HabitEntries",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    habit_tracker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    for_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    completed = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    logged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_habit_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_habit_entries_habits_habit_tracker_id",
                        column: x => x.habit_tracker_id,
                        principalSchema: "fitness",
                        principalTable: "Habits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScreeningAnswers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    health_screening_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_number = table.Column<int>(type: "integer", nullable: false),
                    question_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    answer_kind = table.Column<int>(type: "integer", nullable: false),
                    boolean_answer = table.Column<bool>(type: "boolean", nullable: true),
                    text_answer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    numeric_answer = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    date_answer = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_gating_question = table.Column<bool>(type: "boolean", nullable: false),
                    follow_up_answer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_screening_answers", x => x.id);
                    table.ForeignKey(
                        name: "fk_screening_answers_health_screenings_health_screening_id",
                        column: x => x.health_screening_id,
                        principalSchema: "fitness",
                        principalTable: "HealthScreenings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_kind = table.Column<int>(type: "integer", nullable: false),
                    line_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    proration_explanation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    revenue_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_lines_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "fitness",
                        principalTable: "Invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    club_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    received_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    settled_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    authorisation_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    card_brand = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    card_last_four = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    mandate_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_reason = table.Column<int>(type: "integer", nullable: true),
                    failure_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    cash_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    taken_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    billing_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_method_ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "fitness",
                        principalTable: "Invoices",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loyalty_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    redemption_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    awarded_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_loyalty_transactions_loyalty_accounts_loyalty_account_id",
                        column: x => x.loyalty_account_id,
                        principalSchema: "fitness",
                        principalTable: "LoyaltyAccounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleLines",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    barcode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    modifiers = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_lines_sales_sale_id",
                        column: x => x.sale_id,
                        principalSchema: "fitness",
                        principalTable: "Sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditMovements",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_credit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    was_held = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    performed_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_credit_movements_session_credits_session_credit_id",
                        column: x => x.session_credit_id,
                        principalSchema: "fitness",
                        principalTable: "SessionCredits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassBookings",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_occurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    guest_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    guest_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    payment_kind = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    booked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    spot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    spot_label = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_credit_movement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credits_used = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    penalty_charged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_forfeited = table.Column<bool>(type: "boolean", nullable: false),
                    strike_issued = table.Column<bool>(type: "boolean", nullable: false),
                    marketplace_channel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    marketplace_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    waitlist_position = table.Column<int>(type: "integer", nullable: true),
                    promoted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    booked_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_class_bookings_class_occurrences_class_occurrence_id",
                        column: x => x.class_occurrence_id,
                        principalSchema: "fitness",
                        principalTable: "ClassOccurrences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_class_bookings_members_member_id",
                        column: x => x.member_id,
                        principalSchema: "fitness",
                        principalTable: "Members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "SaveOffers",
                schema: "fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancellation_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    period_count = table.Column<int>(type: "integer", nullable: true),
                    alternative_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    offered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    offered_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    was_accepted = table.Column<bool>(type: "boolean", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decline_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_save_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_save_offers_cancellation_requests_cancellation_request_id",
                        column: x => x.cancellation_request_id,
                        principalSchema: "fitness",
                        principalTable: "CancellationRequests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_events_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "AccessEvents",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_access_events_member_id_occurred_at",
                schema: "fitness",
                table: "AccessEvents",
                columns: new[] { "member_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvent_Club_Time",
                schema: "fitness",
                table: "AccessEvents",
                columns: new[] { "company_id", "club_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvent_Denials",
                schema: "fitness",
                table: "AccessEvents",
                columns: new[] { "club_id", "decision", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_access_rules_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "AccessRules",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_access_rule_windows_access_rule_id",
                schema: "fitness",
                table: "AccessRuleWindows",
                column: "access_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_rule_windows_company_id_branch_id_business_unit_id_is_",
                schema: "fitness",
                table: "AccessRuleWindows",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_access_time_bands_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "AccessTimeBands",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_access_time_bands_entitlement_id",
                schema: "fitness",
                table: "AccessTimeBands",
                column: "entitlement_id");

            migrationBuilder.CreateIndex(
                name: "IX_Agreement_Billing_Due",
                schema: "fitness",
                table: "Agreements",
                columns: new[] { "company_id", "next_billing_on", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Agreement_Tenant_Number",
                schema: "fitness",
                table: "Agreements",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "agreement_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agreements_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Agreements",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_agreements_company_id_club_id_status",
                schema: "fitness",
                table: "Agreements",
                columns: new[] { "company_id", "club_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_agreements_member_id_status",
                schema: "fitness",
                table: "Agreements",
                columns: new[] { "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_agreements_plan_id",
                schema: "fitness",
                table: "Agreements",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_signatures_agreement_id",
                schema: "fitness",
                table: "AgreementSignatures",
                column: "agreement_id");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_signatures_company_id_branch_id_business_unit_id_i",
                schema: "fitness",
                table: "AgreementSignatures",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_agreement_signatures_remote_token",
                schema: "fitness",
                table: "AgreementSignatures",
                column: "remote_token");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_templates_company_id_branch_id_business_unit_id_is",
                schema: "fitness",
                table: "AgreementTemplates",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_agreement_templates_company_id_name_version",
                schema: "fitness",
                table: "AgreementTemplates",
                columns: new[] { "company_id", "name", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Allowance_Lookup",
                schema: "fitness",
                table: "AllowanceUsage",
                columns: new[] { "member_id", "agreement_id", "kind", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ix_allowance_usage_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "AllowanceUsage",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_amendments_agreement_id",
                schema: "fitness",
                table: "Amendments",
                column: "agreement_id");

            migrationBuilder.CreateIndex(
                name: "ix_amendments_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Amendments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_announcements_club_id_is_published_show_from",
                schema: "fitness",
                table: "Announcements",
                columns: new[] { "club_id", "is_published", "show_from" });

            migrationBuilder.CreateIndex(
                name: "ix_announcements_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "Announcements",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_appointment_participants_appointment_id_member_id",
                schema: "fitness",
                table: "AppointmentParticipants",
                columns: new[] { "appointment_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointment_participants_company_id_branch_id_business_unit_",
                schema: "fitness",
                table: "AppointmentParticipants",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_appointment_participants_member_id",
                schema: "fitness",
                table: "AppointmentParticipants",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_Staff_Time",
                schema: "fitness",
                table: "Appointments",
                columns: new[] { "staff_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_Tenant_Number",
                schema: "fitness",
                table: "Appointments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "appointment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointments_club_id_starts_at_status",
                schema: "fitness",
                table: "Appointments",
                columns: new[] { "club_id", "starts_at", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "Appointments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_member_id_starts_at",
                schema: "fitness",
                table: "Appointments",
                columns: new[] { "member_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_service_id",
                schema: "fitness",
                table: "Appointments",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_series_company_id_branch_id_business_unit_id_is_",
                schema: "fitness",
                table: "AppointmentSeries",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_areas_club_id_display_order",
                schema: "fitness",
                table: "Areas",
                columns: new[] { "club_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_areas_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Areas",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_assessment_measures_assessment_template_id",
                schema: "fitness",
                table: "AssessmentMeasures",
                column: "assessment_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_assessment_measures_company_id_branch_id_business_unit_id_is",
                schema: "fitness",
                table: "AssessmentMeasures",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_assessment_template_id",
                schema: "fitness",
                table: "Assessments",
                column: "assessment_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_assessments_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Assessments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_member_id_performed_on",
                schema: "fitness",
                table: "Assessments",
                columns: new[] { "member_id", "performed_on" });

            migrationBuilder.CreateIndex(
                name: "ix_assessment_templates_company_id_branch_id_business_unit_id_i",
                schema: "fitness",
                table: "AssessmentTemplates",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_assessment_values_assessment_id_display_order",
                schema: "fitness",
                table: "AssessmentValues",
                columns: new[] { "assessment_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_assessment_values_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "AssessmentValues",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Audit",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_company_id_occurred_at",
                schema: "fitness",
                table: "Audit",
                columns: new[] { "company_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_member_id_occurred_at",
                schema: "fitness",
                table: "Audit",
                columns: new[] { "member_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Sensitive",
                schema: "fitness",
                table: "Audit",
                columns: new[] { "company_id", "is_sensitive_access", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_authorisations_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "Authorisations",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_authorisations_member_id",
                schema: "fitness",
                table: "Authorisations",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_authorisations_third_party_payer_id_authorisation_number",
                schema: "fitness",
                table: "Authorisations",
                columns: new[] { "third_party_payer_id", "authorisation_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_availability_bookable_staff_id_day_of_week",
                schema: "fitness",
                table: "Availability",
                columns: new[] { "bookable_staff_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_availability_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "Availability",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_badges_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Badges",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_run_lines_billing_run_id_outcome",
                schema: "fitness",
                table: "BillingRunLines",
                columns: new[] { "billing_run_id", "outcome" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_run_lines_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "BillingRunLines",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_runs_company_id_billing_date",
                schema: "fitness",
                table: "BillingRuns",
                columns: new[] { "company_id", "billing_date" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_runs_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "BillingRuns",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_schedules_agreement_id_period_number",
                schema: "fitness",
                table: "BillingSchedules",
                columns: new[] { "agreement_id", "period_number" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_schedules_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "BillingSchedules",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_Due",
                schema: "fitness",
                table: "BillingSchedules",
                columns: new[] { "company_id", "due_on", "is_billed", "is_skipped" });

            migrationBuilder.CreateIndex(
                name: "ix_bookable_staff_club_id_staff_id",
                schema: "fitness",
                table: "BookableStaff",
                columns: new[] { "club_id", "staff_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookable_staff_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "BookableStaff",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_bookable_staff_staff_id",
                schema: "fitness",
                table: "BookableStaff",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_policies_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "BookingPolicies",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Campaigns",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_company_id_sent_at",
                schema: "fitness",
                table: "Campaigns",
                columns: new[] { "company_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_policies_company_id_branch_id_business_unit_id_",
                schema: "fitness",
                table: "CancellationPolicies",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Cancellation_Due",
                schema: "fitness",
                table: "CancellationRequests",
                columns: new[] { "company_id", "effective_on", "is_processed" });

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_requests_agreement_id",
                schema: "fitness",
                table: "CancellationRequests",
                column: "agreement_id");

            migrationBuilder.CreateIndex(
                name: "ix_cancellation_requests_company_id_branch_id_business_unit_id_",
                schema: "fitness",
                table: "CancellationRequests",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_movements_cash_session_id",
                schema: "fitness",
                table: "CashMovements",
                column: "cash_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_movements_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "CashMovements",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_sessions_club_id_status_opened_at",
                schema: "fitness",
                table: "CashSessions",
                columns: new[] { "club_id", "status", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_sessions_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "CashSessions",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CashSession_Number",
                schema: "fitness",
                table: "CashSessions",
                columns: new[] { "company_id", "session_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cert_Expiry",
                schema: "fitness",
                table: "Certifications",
                columns: new[] { "company_id", "expires_on", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_certifications_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "Certifications",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_certifications_staff_id",
                schema: "fitness",
                table: "Certifications",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_challenge_participants_challenge_id_member_id",
                schema: "fitness",
                table: "ChallengeParticipants",
                columns: new[] { "challenge_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_challenge_participants_company_id_branch_id_business_unit_id",
                schema: "fitness",
                table: "ChallengeParticipants",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_challenge_participants_member_id",
                schema: "fitness",
                table: "ChallengeParticipants",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_challenges_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Challenges",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_challenges_company_id_starts_on_ends_on",
                schema: "fitness",
                table: "Challenges",
                columns: new[] { "company_id", "starts_on", "ends_on" });

            migrationBuilder.CreateIndex(
                name: "ix_check_ins_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "CheckIns",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIn_Club_Time",
                schema: "fitness",
                table: "CheckIns",
                columns: new[] { "company_id", "club_id", "checked_in_at" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIn_Member_Time",
                schema: "fitness",
                table: "CheckIns",
                columns: new[] { "member_id", "checked_in_at" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIn_Open",
                schema: "fitness",
                table: "CheckIns",
                columns: new[] { "club_id", "checked_out_at" });

            migrationBuilder.CreateIndex(
                name: "ix_churn_factors_churn_score_id",
                schema: "fitness",
                table: "ChurnFactors",
                column: "churn_score_id");

            migrationBuilder.CreateIndex(
                name: "ix_churn_factors_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "ChurnFactors",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Churn_Board",
                schema: "fitness",
                table: "ChurnScores",
                columns: new[] { "company_id", "club_id", "band", "score" });

            migrationBuilder.CreateIndex(
                name: "IX_Churn_Member",
                schema: "fitness",
                table: "ChurnScores",
                column: "member_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_churn_scores_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "ChurnScores",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Booking_One_Live_Per_Member",
                schema: "fitness",
                table: "ClassBookings",
                columns: new[] { "class_occurrence_id", "member_id" },
                unique: true,
                filter: "\"Status\" IN (1,2,3,4) AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_Spot_Unique",
                schema: "fitness",
                table: "ClassBookings",
                columns: new[] { "class_occurrence_id", "spot_id" },
                unique: true,
                filter: "\"SpotId\" IS NOT NULL AND \"Status\" IN (1,3,4) AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_class_bookings_class_occurrence_id_status",
                schema: "fitness",
                table: "ClassBookings",
                columns: new[] { "class_occurrence_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_class_bookings_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "ClassBookings",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_class_bookings_member_id_booked_at",
                schema: "fitness",
                table: "ClassBookings",
                columns: new[] { "member_id", "booked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_class_occurrences_class_type_id",
                schema: "fitness",
                table: "ClassOccurrences",
                column: "class_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_class_occurrences_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "ClassOccurrences",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_class_occurrences_instructor_staff_id_starts_at",
                schema: "fitness",
                table: "ClassOccurrences",
                columns: new[] { "instructor_staff_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_class_occurrences_room_id_starts_at",
                schema: "fitness",
                table: "ClassOccurrences",
                columns: new[] { "room_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_Club_Start",
                schema: "fitness",
                table: "ClassOccurrences",
                columns: new[] { "company_id", "club_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_Finishing",
                schema: "fitness",
                table: "ClassOccurrences",
                columns: new[] { "company_id", "status", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "ix_class_schedules_class_type_id",
                schema: "fitness",
                table: "ClassSchedules",
                column: "class_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_class_schedules_club_id_is_published",
                schema: "fitness",
                table: "ClassSchedules",
                columns: new[] { "club_id", "is_published" });

            migrationBuilder.CreateIndex(
                name: "ix_class_schedules_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "ClassSchedules",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_class_schedules_room_id",
                schema: "fitness",
                table: "ClassSchedules",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_class_types_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "ClassTypes",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_class_types_company_id_is_active_display_order",
                schema: "fitness",
                table: "ClassTypes",
                columns: new[] { "company_id", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_clearances_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Clearances",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_clearances_member_id_status",
                schema: "fitness",
                table: "Clearances",
                columns: new[] { "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_club_closures_club_id_starts_on_ends_on",
                schema: "fitness",
                table: "ClubClosures",
                columns: new[] { "club_id", "starts_on", "ends_on" });

            migrationBuilder.CreateIndex(
                name: "ix_club_closures_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "ClubClosures",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Club_Tenant_Code",
                schema: "fitness",
                table: "Clubs",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clubs_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Clubs",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_club_schedules_club_id_day_of_week_override_date",
                schema: "fitness",
                table: "ClubSchedules",
                columns: new[] { "club_id", "day_of_week", "override_date" });

            migrationBuilder.CreateIndex(
                name: "ix_club_schedules_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "ClubSchedules",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_coach_assignments_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "CoachAssignments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_coach_assignments_member_id_is_primary",
                schema: "fitness",
                table: "CoachAssignments",
                columns: new[] { "member_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "ix_coach_assignments_staff_id_ended_on",
                schema: "fitness",
                table: "CoachAssignments",
                columns: new[] { "staff_id", "ended_on" });

            migrationBuilder.CreateIndex(
                name: "ix_coach_check_ins_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "CoachCheckIns",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_coach_check_ins_member_id",
                schema: "fitness",
                table: "CoachCheckIns",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_coach_check_ins_staff_id_due_on_is_complete",
                schema: "fitness",
                table: "CoachCheckIns",
                columns: new[] { "staff_id", "due_on", "is_complete" });

            migrationBuilder.CreateIndex(
                name: "IX_Accrual_Staff_Period",
                schema: "fitness",
                table: "CommissionAccruals",
                columns: new[] { "staff_id", "earned_on" });

            migrationBuilder.CreateIndex(
                name: "ix_commission_accruals_commission_statement_id",
                schema: "fitness",
                table: "CommissionAccruals",
                column: "commission_statement_id");

            migrationBuilder.CreateIndex(
                name: "ix_commission_accruals_company_id_branch_id_business_unit_id_is",
                schema: "fitness",
                table: "CommissionAccruals",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_commission_rules_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "CommissionRules",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_commission_rules_company_id_club_id_basis_is_active",
                schema: "fitness",
                table: "CommissionRules",
                columns: new[] { "company_id", "club_id", "basis", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_commission_statements_company_id_branch_id_business_unit_id_",
                schema: "fitness",
                table: "CommissionStatements",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_commission_statements_staff_id_period_start",
                schema: "fitness",
                table: "CommissionStatements",
                columns: new[] { "staff_id", "period_start" });

            migrationBuilder.CreateIndex(
                name: "IX_Statement_Tenant_Number",
                schema: "fitness",
                table: "CommissionStatements",
                columns: new[] { "company_id", "statement_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complaint_Number",
                schema: "fitness",
                table: "Complaints",
                columns: new[] { "company_id", "complaint_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaints_club_id_status",
                schema: "fitness",
                table: "Complaints",
                columns: new[] { "club_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_complaints_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Complaints",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_complaints_member_id",
                schema: "fitness",
                table: "Complaints",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_consents_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Consents",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_consents_member_id_channel_purpose",
                schema: "fitness",
                table: "Consents",
                columns: new[] { "member_id", "channel", "purpose" });

            migrationBuilder.CreateIndex(
                name: "ix_controllers_club_id",
                schema: "fitness",
                table: "Controllers",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "ix_controllers_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Controllers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_corporate_accounts_company_id_branch_id_business_unit_id_is_",
                schema: "fitness",
                table: "CorporateAccounts",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Corporate_Tenant_Code",
                schema: "fitness",
                table: "CorporateAccounts",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorpInvoice_Number",
                schema: "fitness",
                table: "CorporateInvoices",
                columns: new[] { "company_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invoices_company_id_branch_id_business_unit_id_is_",
                schema: "fitness",
                table: "CorporateInvoices",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_corporate_invoices_corporate_account_id",
                schema: "fitness",
                table: "CorporateInvoices",
                column: "corporate_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_corporate_members_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "CorporateMembers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_corporate_members_corporate_account_id_member_id",
                schema: "fitness",
                table: "CorporateMembers",
                columns: new[] { "corporate_account_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_corporate_members_member_id",
                schema: "fitness",
                table: "CorporateMembers",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_course_enrolments_class_schedule_id_member_id",
                schema: "fitness",
                table: "CourseEnrolments",
                columns: new[] { "class_schedule_id", "member_id" });

            migrationBuilder.CreateIndex(
                name: "ix_course_enrolments_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "CourseEnrolments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_course_enrolments_member_id",
                schema: "fitness",
                table: "CourseEnrolments",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_Credential_Active_Identifier",
                schema: "fitness",
                table: "Credentials",
                columns: new[] { "company_id", "identifier" },
                unique: true,
                filter: "\"Status\" = 1 AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Credential_Lookup",
                schema: "fitness",
                table: "Credentials",
                columns: new[] { "company_id", "identifier", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Credentials",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_member_id",
                schema: "fitness",
                table: "Credentials",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_balances_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "CreditBalances",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_credit_balances_member_id",
                schema: "fitness",
                table: "CreditBalances",
                column: "member_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credit_movements_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "CreditMovements",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_credit_movements_member_id_occurred_at",
                schema: "fitness",
                table: "CreditMovements",
                columns: new[] { "member_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_credit_movements_session_credit_id",
                schema: "fitness",
                table: "CreditMovements",
                column: "session_credit_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_notes_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "CreditNotes",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNote_Tenant_Number",
                schema: "fitness",
                table: "CreditNotes",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "credit_note_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_day_passes_club_id_valid_from_valid_to",
                schema: "fitness",
                table: "DayPasses",
                columns: new[] { "club_id", "valid_from", "valid_to" });

            migrationBuilder.CreateIndex(
                name: "ix_day_passes_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "DayPasses",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DayPass_Tenant_Number",
                schema: "fitness",
                table: "DayPasses",
                columns: new[] { "company_id", "pass_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deferred_Open",
                schema: "fitness",
                table: "DeferredRevenue",
                columns: new[] { "company_id", "is_closed", "service_end" });

            migrationBuilder.CreateIndex(
                name: "ix_deferred_revenue_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "DeferredRevenue",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_deferred_revenue_entries_company_id_branch_id_business_unit_i",
                schema: "fitness",
                table: "DeferredRevenueEntries",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_deferred_revenue_entries_schedule_id_recognised_on",
                schema: "fitness",
                table: "DeferredRevenueEntries",
                columns: new[] { "schedule_id", "recognised_on" });

            migrationBuilder.CreateIndex(
                name: "ix_doors_area_id",
                schema: "fitness",
                table: "Doors",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "ix_doors_club_id",
                schema: "fitness",
                table: "Doors",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "ix_doors_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Doors",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_doors_controller_id",
                schema: "fitness",
                table: "Doors",
                column: "controller_id");

            migrationBuilder.CreateIndex(
                name: "ix_dunning_cases_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "DunningCases",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_dunning_cases_member_id_status",
                schema: "fitness",
                table: "DunningCases",
                columns: new[] { "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Dunning_Due",
                schema: "fitness",
                table: "DunningCases",
                columns: new[] { "company_id", "status", "next_step_due_on" });

            migrationBuilder.CreateIndex(
                name: "ix_dunning_events_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "DunningEvents",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_dunning_events_dunning_case_id",
                schema: "fitness",
                table: "DunningEvents",
                column: "dunning_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_dunning_policies_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "DunningPolicies",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_dunning_steps_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "DunningSteps",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_dunning_steps_dunning_policy_id_step_number",
                schema: "fitness",
                table: "DunningSteps",
                columns: new[] { "dunning_policy_id", "step_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_effort_sessions_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "EffortSessions",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_effort_sessions_company_id_external_reference",
                schema: "fitness",
                table: "EffortSessions",
                columns: new[] { "company_id", "external_reference" });

            migrationBuilder.CreateIndex(
                name: "ix_effort_sessions_member_id_started_at",
                schema: "fitness",
                table: "EffortSessions",
                columns: new[] { "member_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_eligibility_rules_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "EligibilityRules",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_eligibility_rules_corporate_account_id",
                schema: "fitness",
                table: "EligibilityRules",
                column: "corporate_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contacts_company_id_branch_id_business_unit_id_is_",
                schema: "fitness",
                table: "EmergencyContacts",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contacts_member_id",
                schema: "fitness",
                table: "EmergencyContacts",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_club_id_status",
                schema: "fitness",
                table: "Equipment",
                columns: new[] { "club_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Equipment",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Qr",
                schema: "fitness",
                table: "Equipment",
                columns: new[] { "company_id", "qr_code" },
                unique: true,
                filter: "\"QrCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_usage_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "EquipmentUsage",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_usage_equipment_asset_id_read_on",
                schema: "fitness",
                table: "EquipmentUsage",
                columns: new[] { "equipment_asset_id", "read_on" });

            migrationBuilder.CreateIndex(
                name: "ix_exercises_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Exercises",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_exercises_company_id_category",
                schema: "fitness",
                table: "Exercises",
                columns: new[] { "company_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_exercises_company_id_name",
                schema: "fitness",
                table: "Exercises",
                columns: new[] { "company_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_facility_check_items_company_id_branch_id_business_unit_id_is",
                schema: "fitness",
                table: "FacilityCheckItems",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_facility_check_items_facility_check_id",
                schema: "fitness",
                table: "FacilityCheckItems",
                column: "facility_check_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_checks_club_id_is_active",
                schema: "fitness",
                table: "FacilityChecks",
                columns: new[] { "club_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_facility_checks_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "FacilityChecks",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_club_id_is_resolved_reported_at",
                schema: "fitness",
                table: "FaultReports",
                columns: new[] { "club_id", "is_resolved", "reported_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fault_reports_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "FaultReports",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_club_id_is_actioned_submitted_at",
                schema: "fitness",
                table: "Feedback",
                columns: new[] { "club_id", "is_actioned", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Feedback",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Freeze_Release_Due",
                schema: "fitness",
                table: "Freezes",
                columns: new[] { "company_id", "ends_on", "is_released" });

            migrationBuilder.CreateIndex(
                name: "ix_freezes_agreement_id",
                schema: "fitness",
                table: "Freezes",
                column: "agreement_id");

            migrationBuilder.CreateIndex(
                name: "ix_freezes_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Freezes",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_freezes_member_id_starts_on",
                schema: "fitness",
                table: "Freezes",
                columns: new[] { "member_id", "starts_on" });

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "GiftCards",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_GiftCard_Tenant_Number",
                schema: "fitness",
                table: "GiftCards",
                columns: new[] { "company_id", "card_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gift_card_transactions_company_id_branch_id_business_unit_id_",
                schema: "fitness",
                table: "GiftCardTransactions",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_gift_card_transactions_gift_card_id",
                schema: "fitness",
                table: "GiftCardTransactions",
                column: "gift_card_id");

            migrationBuilder.CreateIndex(
                name: "ix_goals_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Goals",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_goals_member_id_status",
                schema: "fitness",
                table: "Goals",
                columns: new[] { "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_grading_events_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "GradingEvents",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_visits_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "GuestVisits",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_visits_host_member_id_visited_on",
                schema: "fitness",
                table: "GuestVisits",
                columns: new[] { "host_member_id", "visited_on" });

            migrationBuilder.CreateIndex(
                name: "ix_habit_entries_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "HabitEntries",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_habit_entries_habit_tracker_id_for_date",
                schema: "fitness",
                table: "HabitEntries",
                columns: new[] { "habit_tracker_id", "for_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_habits_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Habits",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_habits_member_id",
                schema: "fitness",
                table: "Habits",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_handovers_club_id_shift_ended_at",
                schema: "fitness",
                table: "Handovers",
                columns: new[] { "club_id", "shift_ended_at" });

            migrationBuilder.CreateIndex(
                name: "ix_handovers_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Handovers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_health_screenings_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "HealthScreenings",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_health_screenings_member_id_completed_at",
                schema: "fitness",
                table: "HealthScreenings",
                columns: new[] { "member_id", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_house_account_charges_company_id_branch_id_business_unit_id_i",
                schema: "fitness",
                table: "HouseAccountCharges",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_house_account_charges_member_id_is_settled",
                schema: "fitness",
                table: "HouseAccountCharges",
                columns: new[] { "member_id", "is_settled" });

            migrationBuilder.CreateIndex(
                name: "ix_household_members_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "HouseholdMembers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_household_members_household_id_member_id",
                schema: "fitness",
                table: "HouseholdMembers",
                columns: new[] { "household_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_household_members_member_id",
                schema: "fitness",
                table: "HouseholdMembers",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_households_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Households",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_incident_actions_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "IncidentActions",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_incident_actions_incident_id",
                schema: "fitness",
                table: "IncidentActions",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Number",
                schema: "fitness",
                table: "Incidents",
                columns: new[] { "company_id", "incident_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_incidents_club_id_status_occurred_at",
                schema: "fitness",
                table: "Incidents",
                columns: new[] { "club_id", "status", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Incidents",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "InvoiceLines",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_invoice_id",
                schema: "fitness",
                table: "InvoiceLines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Arrears",
                schema: "fitness",
                table: "Invoices",
                columns: new[] { "company_id", "club_id", "status", "due_on" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Tenant_Number",
                schema: "fitness",
                table: "Invoices",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Invoices",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_member_id_status",
                schema: "fitness",
                table: "Invoices",
                columns: new[] { "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrolment_Due",
                schema: "fitness",
                table: "JourneyEnrolments",
                columns: new[] { "company_id", "next_step_due_at", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_enrolments_company_id_branch_id_business_unit_id_is_",
                schema: "fitness",
                table: "JourneyEnrolments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_enrolments_engagement_journey_id_member_id",
                schema: "fitness",
                table: "JourneyEnrolments",
                columns: new[] { "engagement_journey_id", "member_id" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_enrolments_member_id",
                schema: "fitness",
                table: "JourneyEnrolments",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_journeys_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Journeys",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_journeys_company_id_trigger_is_active",
                schema: "fitness",
                table: "Journeys",
                columns: new[] { "company_id", "trigger", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_steps_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "JourneySteps",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_journey_steps_engagement_journey_id_step_number",
                schema: "fitness",
                table: "JourneySteps",
                columns: new[] { "engagement_journey_id", "step_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lead_activities_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "LeadActivities",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_lead_activities_lead_id_occurred_at",
                schema: "fitness",
                table: "LeadActivities",
                columns: new[] { "lead_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_challenge_id_rank",
                schema: "fitness",
                table: "Leaderboard",
                columns: new[] { "challenge_id", "rank" });

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Leaderboard",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_workout_id_division_rank",
                schema: "fitness",
                table: "Leaderboard",
                columns: new[] { "workout_id", "division", "rank" });

            migrationBuilder.CreateIndex(
                name: "IX_Lead_Club_Status",
                schema: "fitness",
                table: "Leads",
                columns: new[] { "company_id", "club_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Lead_Sla",
                schema: "fitness",
                table: "Leads",
                columns: new[] { "company_id", "first_contacted_at", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_leads_assigned_staff_id_status",
                schema: "fitness",
                table: "Leads",
                columns: new[] { "assigned_staff_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_leads_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Leads",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_leads_lead_source_id",
                schema: "fitness",
                table: "Leads",
                column: "lead_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_lead_sources_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "LeadSources",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_lead_sources_company_id_tracking_code",
                schema: "fitness",
                table: "LeadSources",
                columns: new[] { "company_id", "tracking_code" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Ledger",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Ledger_Member_Time",
                schema: "fitness",
                table: "Ledger",
                columns: new[] { "member_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_locker_assignments_company_id_branch_id_business_unit_id_is_",
                schema: "fitness",
                table: "LockerAssignments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_locker_assignments_locker_id",
                schema: "fitness",
                table: "LockerAssignments",
                column: "locker_id");

            migrationBuilder.CreateIndex(
                name: "ix_locker_assignments_member_id_released_on",
                schema: "fitness",
                table: "LockerAssignments",
                columns: new[] { "member_id", "released_on" });

            migrationBuilder.CreateIndex(
                name: "IX_Locker_Expiry",
                schema: "fitness",
                table: "LockerAssignments",
                columns: new[] { "company_id", "ends_on", "released_on" });

            migrationBuilder.CreateIndex(
                name: "ix_locker_banks_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "LockerBanks",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_lockers_club_id_status",
                schema: "fitness",
                table: "Lockers",
                columns: new[] { "club_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_lockers_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Lockers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_lockers_locker_bank_id_number",
                schema: "fitness",
                table: "Lockers",
                columns: new[] { "locker_bank_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loss_reasons_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "LossReasons",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_lost_property_club_id_status_found_on",
                schema: "fitness",
                table: "LostProperty",
                columns: new[] { "club_id", "status", "found_on" });

            migrationBuilder.CreateIndex(
                name: "ix_lost_property_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "LostProperty",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_accounts_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "LoyaltyAccounts",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_accounts_member_id",
                schema: "fitness",
                table: "LoyaltyAccounts",
                column: "member_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_accounts_tier_id",
                schema: "fitness",
                table: "LoyaltyAccounts",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_tiers_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "LoyaltyTiers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Loyalty_Expiry",
                schema: "fitness",
                table: "LoyaltyTransactions",
                columns: new[] { "company_id", "expires_on" });

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_transactions_company_id_branch_id_business_unit_id_i",
                schema: "fitness",
                table: "LoyaltyTransactions",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_transactions_loyalty_account_id",
                schema: "fitness",
                table: "LoyaltyTransactions",
                column: "loyalty_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_transactions_member_id_occurred_at",
                schema: "fitness",
                table: "LoyaltyTransactions",
                columns: new[] { "member_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Maintenance_Due",
                schema: "fitness",
                table: "MaintenanceSchedules",
                columns: new[] { "company_id", "next_due_on", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_company_id_branch_id_business_unit_id_",
                schema: "fitness",
                table: "MaintenanceSchedules",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_equipment_asset_id",
                schema: "fitness",
                table: "MaintenanceSchedules",
                column: "equipment_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_mandates_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Mandates",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_mandates_company_id_mandate_reference",
                schema: "fitness",
                table: "Mandates",
                columns: new[] { "company_id", "mandate_reference" });

            migrationBuilder.CreateIndex(
                name: "ix_marketplace_bookings_company_id_branch_id_business_unit_id_i",
                schema: "fitness",
                table: "MarketplaceBookings",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_marketplace_bookings_marketplace_channel_id_external_referen",
                schema: "fitness",
                table: "MarketplaceBookings",
                columns: new[] { "marketplace_channel_id", "external_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_marketplace_channels_company_id_branch_id_business_unit_id_i",
                schema: "fitness",
                table: "MarketplaceChannels",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_medical_flags_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "MedicalFlags",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_medical_flags_member_id",
                schema: "fitness",
                table: "MedicalFlags",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_alerts_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "MemberAlerts",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberAlert_Member_Blocking",
                schema: "fitness",
                table: "MemberAlerts",
                columns: new[] { "member_id", "blocks_access" });

            migrationBuilder.CreateIndex(
                name: "ix_member_badges_badge_id",
                schema: "fitness",
                table: "MemberBadges",
                column: "badge_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_badges_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "MemberBadges",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_member_badges_member_id_badge_id",
                schema: "fitness",
                table: "MemberBadges",
                columns: new[] { "member_id", "badge_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_member_documents_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "MemberDocuments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_member_documents_member_id_kind",
                schema: "fitness",
                table: "MemberDocuments",
                columns: new[] { "member_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_member_notes_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "MemberNotes",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberNote_Member_Time",
                schema: "fitness",
                table: "MemberNotes",
                columns: new[] { "member_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_member_ranks_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "MemberRanks",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_member_ranks_member_id_rank_ladder_id_is_current",
                schema: "fitness",
                table: "MemberRanks",
                columns: new[] { "member_id", "rank_ladder_id", "is_current" });

            migrationBuilder.CreateIndex(
                name: "ix_member_ranks_rank_level_id",
                schema: "fitness",
                table: "MemberRanks",
                column: "rank_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_Member_Club_Status",
                schema: "fitness",
                table: "Members",
                columns: new[] { "company_id", "home_club_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Member_Name",
                schema: "fitness",
                table: "Members",
                columns: new[] { "company_id", "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "IX_Member_Phone",
                schema: "fitness",
                table: "Members",
                columns: new[] { "company_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "IX_Member_Risk",
                schema: "fitness",
                table: "Members",
                columns: new[] { "company_id", "risk_band" });

            migrationBuilder.CreateIndex(
                name: "IX_Member_Tenant_Number",
                schema: "fitness",
                table: "Members",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "member_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Members",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_members_home_club_id",
                schema: "fitness",
                table: "Members",
                column: "home_club_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_household_id",
                schema: "fitness",
                table: "Members",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_status_history_company_id_branch_id_business_unit_id_i",
                schema: "fitness",
                table: "MemberStatusHistory",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_member_status_history_member_id_changed_at",
                schema: "fitness",
                table: "MemberStatusHistory",
                columns: new[] { "member_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_member_tags_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "MemberTags",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_member_tags_member_id_tag",
                schema: "fitness",
                table: "MemberTags",
                columns: new[] { "member_id", "tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberTag_Lookup",
                schema: "fitness",
                table: "MemberTags",
                columns: new[] { "company_id", "tag" });

            migrationBuilder.CreateIndex(
                name: "ix_message_log_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "MessageLog",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_message_log_company_id_club_id_queued_at",
                schema: "fitness",
                table: "MessageLog",
                columns: new[] { "company_id", "club_id", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_message_log_member_id_queued_at",
                schema: "fitness",
                table: "MessageLog",
                columns: new[] { "member_id", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_message_templates_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "MessageTemplates",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_message_templates_company_id_channel_purpose",
                schema: "fitness",
                table: "MessageTemplates",
                columns: new[] { "company_id", "channel", "purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_Nps_Detractors",
                schema: "fitness",
                table: "NpsResponses",
                columns: new[] { "club_id", "band", "followed_up" });

            migrationBuilder.CreateIndex(
                name: "ix_nps_responses_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "NpsResponses",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_nps_responses_company_id_club_id_responded_at",
                schema: "fitness",
                table: "NpsResponses",
                columns: new[] { "company_id", "club_id", "responded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_nps_responses_member_id",
                schema: "fitness",
                table: "NpsResponses",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_nutrition_plans_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "NutritionPlans",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_nutrition_plans_member_id",
                schema: "fitness",
                table: "NutritionPlans",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_Occupancy_Club_Time",
                schema: "fitness",
                table: "OccupancySnapshots",
                columns: new[] { "club_id", "taken_at" });

            migrationBuilder.CreateIndex(
                name: "ix_occupancy_snapshots_company_id_branch_id_business_unit_id_is",
                schema: "fitness",
                table: "OccupancySnapshots",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Package_Expiry",
                schema: "fitness",
                table: "PackagePurchases",
                columns: new[] { "company_id", "expires_on", "is_expired" });

            migrationBuilder.CreateIndex(
                name: "ix_package_purchases_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "PackagePurchases",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_package_purchases_member_id_is_expired",
                schema: "fitness",
                table: "PackagePurchases",
                columns: new[] { "member_id", "is_expired" });

            migrationBuilder.CreateIndex(
                name: "ix_payers_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Payers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "PaymentMethods",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_member_id_is_default",
                schema: "fitness",
                table: "PaymentMethods",
                columns: new[] { "member_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethod_Expiry",
                schema: "fitness",
                table: "PaymentMethods",
                columns: new[] { "company_id", "expiry_year", "expiry_month" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Idempotency",
                schema: "fitness",
                table: "Payments",
                columns: new[] { "company_id", "idempotency_key" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Tenant_Number",
                schema: "fitness",
                table: "Payments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "payment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Payments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_company_id_club_id_received_on",
                schema: "fitness",
                table: "Payments",
                columns: new[] { "company_id", "club_id", "received_on" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_invoice_id",
                schema: "fitness",
                table: "Payments",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_member_id_received_on",
                schema: "fitness",
                table: "Payments",
                columns: new[] { "member_id", "received_on" });

            migrationBuilder.CreateIndex(
                name: "ix_personal_records_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "PersonalRecords",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Pr_Member_Record",
                schema: "fitness",
                table: "PersonalRecords",
                columns: new[] { "member_id", "record_name", "rep_max" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plan_change_paths_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "PlanChangePaths",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_plan_change_paths_from_plan_id_to_plan_id",
                schema: "fitness",
                table: "PlanChangePaths",
                columns: new[] { "from_plan_id", "to_plan_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plan_entitlements_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "PlanEntitlements",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_plan_entitlements_plan_id_kind",
                schema: "fitness",
                table: "PlanEntitlements",
                columns: new[] { "plan_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_plan_prices_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "PlanPrices",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_plan_prices_plan_id_club_id",
                schema: "fitness",
                table: "PlanPrices",
                columns: new[] { "plan_id", "club_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Plan_Tenant_Code",
                schema: "fitness",
                table: "Plans",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plans_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Plans",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_plans_company_id_kind_is_active",
                schema: "fitness",
                table: "Plans",
                columns: new[] { "company_id", "kind", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_preferences_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Preferences",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_preferences_member_id",
                schema: "fitness",
                table: "Preferences",
                column: "member_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_program_days_club_id_scheduled_on_is_published",
                schema: "fitness",
                table: "ProgramDays",
                columns: new[] { "club_id", "scheduled_on", "is_published" });

            migrationBuilder.CreateIndex(
                name: "ix_program_days_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "ProgramDays",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_program_days_program_track_id_scheduled_on",
                schema: "fitness",
                table: "ProgramDays",
                columns: new[] { "program_track_id", "scheduled_on" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_program_days_workout_id",
                schema: "fitness",
                table: "ProgramDays",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "ix_program_tracks_company_id_branch_id_business_unit_id_is_dele",
                schema: "fitness",
                table: "ProgramTracks",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_progress_photos_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "ProgressPhotos",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_progress_photos_member_id_taken_on",
                schema: "fitness",
                table: "ProgressPhotos",
                columns: new[] { "member_id", "taken_on" });

            migrationBuilder.CreateIndex(
                name: "ix_promo_codes_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "PromoCodes",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_promo_codes_promotion_rule_id",
                schema: "fitness",
                table: "PromoCodes",
                column: "promotion_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCode_Tenant_Code",
                schema: "fitness",
                table: "PromoCodes",
                columns: new[] { "company_id", "code_text" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotions_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Promotions",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_promotions_company_id_plan_id_is_active",
                schema: "fitness",
                table: "Promotions",
                columns: new[] { "company_id", "plan_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_rank_ladders_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "RankLadders",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_rank_levels_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "RankLevels",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_rank_levels_rank_ladder_id_ordinal",
                schema: "fitness",
                table: "RankLevels",
                columns: new[] { "rank_ladder_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_referrals_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Referrals",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_referrals_company_id_referral_code",
                schema: "fitness",
                table: "Referrals",
                columns: new[] { "company_id", "referral_code" });

            migrationBuilder.CreateIndex(
                name: "ix_referrals_referrer_member_id_converted",
                schema: "fitness",
                table: "Referrals",
                columns: new[] { "referrer_member_id", "converted" });

            migrationBuilder.CreateIndex(
                name: "IX_Refund_Tenant_Number",
                schema: "fitness",
                table: "Refunds",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "refund_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refunds_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Refunds",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_resource_bookings_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "ResourceBookings",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_resource_bookings_member_id_starts_at",
                schema: "fitness",
                table: "ResourceBookings",
                columns: new[] { "member_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBooking_Number",
                schema: "fitness",
                table: "ResourceBookings",
                columns: new[] { "company_id", "booking_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBooking_Slot",
                schema: "fitness",
                table: "ResourceBookings",
                columns: new[] { "bookable_resource_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_resources_club_id_kind_display_order",
                schema: "fitness",
                table: "Resources",
                columns: new[] { "club_id", "kind", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_resources_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Resources",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_resource_slot_rules_bookable_resource_id",
                schema: "fitness",
                table: "ResourceSlotRules",
                column: "bookable_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_resource_slot_rules_company_id_branch_id_business_unit_id_is_",
                schema: "fitness",
                table: "ResourceSlotRules",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_retention_tasks_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "RetentionTasks",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_retention_tasks_member_id",
                schema: "fitness",
                table: "RetentionTasks",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_Task_Board",
                schema: "fitness",
                table: "RetentionTasks",
                columns: new[] { "company_id", "assigned_staff_id", "completed_at", "due_on" });

            migrationBuilder.CreateIndex(
                name: "ix_rooms_area_id",
                schema: "fitness",
                table: "Rooms",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_club_id_display_order",
                schema: "fitness",
                table: "Rooms",
                columns: new[] { "club_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_rooms_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Rooms",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_room_spots_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "RoomSpots",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomSpot_Room_Label",
                schema: "fitness",
                table: "RoomSpots",
                columns: new[] { "room_id", "label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_lines_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "SaleLines",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_lines_sale_id",
                schema: "fitness",
                table: "SaleLines",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "IX_Sale_Tenant_Number",
                schema: "fitness",
                table: "Sales",
                columns: new[] { "company_id", "sale_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_club_id_sold_at",
                schema: "fitness",
                table: "Sales",
                columns: new[] { "club_id", "sold_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Sales",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_member_id",
                schema: "fitness",
                table: "Sales",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_targets_club_id_staff_id_period_start",
                schema: "fitness",
                table: "SalesTargets",
                columns: new[] { "club_id", "staff_id", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_targets_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "SalesTargets",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_save_offers_cancellation_request_id",
                schema: "fitness",
                table: "SaveOffers",
                column: "cancellation_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_save_offers_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "SaveOffers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_screening_answers_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "ScreeningAnswers",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_screening_answers_health_screening_id",
                schema: "fitness",
                table: "ScreeningAnswers",
                column: "health_screening_id");

            migrationBuilder.CreateIndex(
                name: "ix_segments_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Segments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_services_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Services",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_services_company_id_kind_is_active",
                schema: "fitness",
                table: "Services",
                columns: new[] { "company_id", "kind", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_Credit_Expiry",
                schema: "fitness",
                table: "SessionCredits",
                columns: new[] { "company_id", "expires_on", "is_expired" });

            migrationBuilder.CreateIndex(
                name: "IX_Credit_Member_Kind",
                schema: "fitness",
                table: "SessionCredits",
                columns: new[] { "member_id", "kind", "is_expired" });

            migrationBuilder.CreateIndex(
                name: "ix_session_credits_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "SessionCredits",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_settings_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Settings",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Tenant",
                schema: "fitness",
                table: "Settings",
                columns: new[] { "company_id", "branch_id", "business_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_assignments_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "ShiftAssignments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_assignments_shift_id_staff_id",
                schema: "fitness",
                table: "ShiftAssignments",
                columns: new[] { "shift_id", "staff_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_assignments_staff_id",
                schema: "fitness",
                table: "ShiftAssignments",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_shifts_club_id_starts_at",
                schema: "fitness",
                table: "Shifts",
                columns: new[] { "club_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_shifts_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Shifts",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_sign_offs_appointment_id",
                schema: "fitness",
                table: "SignOffs",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "ix_sign_offs_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "SignOffs",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_sign_offs_staff_id_signed_off_at",
                schema: "fitness",
                table: "SignOffs",
                columns: new[] { "staff_id", "signed_off_at" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_clearances_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "SkillClearances",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_clearances_member_id_skill_name",
                schema: "fitness",
                table: "SkillClearances",
                columns: new[] { "member_id", "skill_name" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_club_id",
                schema: "fitness",
                table: "Staff",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Staff",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_company_id_club_id_is_active",
                schema: "fitness",
                table: "Staff",
                columns: new[] { "company_id", "club_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_user_id",
                schema: "fitness",
                table: "Staff",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_roles_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "StaffRoles",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_targets_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "StaffTargets",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_targets_staff_id_period_start",
                schema: "fitness",
                table: "StaffTargets",
                columns: new[] { "staff_id", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ix_streaks_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Streaks",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_streaks_member_id_cadence",
                schema: "fitness",
                table: "Streaks",
                columns: new[] { "member_id", "cadence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Strike_Active",
                schema: "fitness",
                table: "Strikes",
                columns: new[] { "member_id", "expires_on", "is_waived" });

            migrationBuilder.CreateIndex(
                name: "ix_strikes_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Strikes",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_suspensions_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Suspensions",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_suspensions_member_id_lifted_on",
                schema: "fitness",
                table: "Suspensions",
                columns: new[] { "member_id", "lifted_on" });

            migrationBuilder.CreateIndex(
                name: "ix_swap_requests_company_id_branch_id_business_unit_id_is_delet",
                schema: "fitness",
                table: "SwapRequests",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_swap_requests_shift_assignment_id",
                schema: "fitness",
                table: "SwapRequests",
                column: "shift_assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_Clock_Open",
                schema: "fitness",
                table: "TimeClock",
                columns: new[] { "club_id", "clocked_out_at" });

            migrationBuilder.CreateIndex(
                name: "ix_time_clock_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "TimeClock",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_time_clock_staff_id_clocked_in_at",
                schema: "fitness",
                table: "TimeClock",
                columns: new[] { "staff_id", "clocked_in_at" });

            migrationBuilder.CreateIndex(
                name: "ix_time_off_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "TimeOff",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_time_off_staff_id_starts_at_ends_at",
                schema: "fitness",
                table: "TimeOff",
                columns: new[] { "staff_id", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tours_club_id_scheduled_for",
                schema: "fitness",
                table: "Tours",
                columns: new[] { "club_id", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_tours_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Tours",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_tours_lead_id",
                schema: "fitness",
                table: "Tours",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "IX_Trial_Expiring",
                schema: "fitness",
                table: "Trials",
                columns: new[] { "club_id", "ends_on", "converted" });

            migrationBuilder.CreateIndex(
                name: "ix_trials_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Trials",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_trials_lead_id",
                schema: "fitness",
                table: "Trials",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_vending_revenue_club_id_period_start",
                schema: "fitness",
                table: "VendingRevenue",
                columns: new[] { "club_id", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ix_vending_revenue_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "VendingRevenue",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_waiver_signatures_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "WaiverSignatures",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_waiver_signatures_member_id_status",
                schema: "fitness",
                table: "WaiverSignatures",
                columns: new[] { "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_waiver_signatures_waiver_template_id",
                schema: "fitness",
                table: "WaiverSignatures",
                column: "waiver_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_waiver_templates_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "WaiverTemplates",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_waiver_templates_company_id_name_version",
                schema: "fitness",
                table: "WaiverTemplates",
                columns: new[] { "company_id", "name", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_club_id_status_priority",
                schema: "fitness",
                table: "WorkOrders",
                columns: new[] { "club_id", "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "WorkOrders",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_equipment_asset_id",
                schema: "fitness",
                table: "WorkOrders",
                column: "equipment_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_Number",
                schema: "fitness",
                table: "WorkOrders",
                columns: new[] { "company_id", "work_order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workout_movements_company_id_branch_id_business_unit_id_is_d",
                schema: "fitness",
                table: "WorkoutMovements",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_workout_movements_exercise_id",
                schema: "fitness",
                table: "WorkoutMovements",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_workout_movements_workout_section_id",
                schema: "fitness",
                table: "WorkoutMovements",
                column: "workout_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_Result_Leaderboard",
                schema: "fitness",
                table: "WorkoutResults",
                columns: new[] { "workout_id", "normalised_score" });

            migrationBuilder.CreateIndex(
                name: "ix_workout_results_company_id_branch_id_business_unit_id_is_del",
                schema: "fitness",
                table: "WorkoutResults",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_workout_results_member_id_performed_on",
                schema: "fitness",
                table: "WorkoutResults",
                columns: new[] { "member_id", "performed_on" });

            migrationBuilder.CreateIndex(
                name: "ix_workouts_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "Workouts",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_workouts_company_id_is_benchmark",
                schema: "fitness",
                table: "Workouts",
                columns: new[] { "company_id", "is_benchmark" });

            migrationBuilder.CreateIndex(
                name: "ix_workout_sections_company_id_branch_id_business_unit_id_is_de",
                schema: "fitness",
                table: "WorkoutSections",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_workout_sections_workout_id",
                schema: "fitness",
                table: "WorkoutSections",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "ix_write_offs_company_id_branch_id_business_unit_id_is_deleted",
                schema: "fitness",
                table: "WriteOffs",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_write_offs_company_id_written_off_on",
                schema: "fitness",
                table: "WriteOffs",
                columns: new[] { "company_id", "written_off_on" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessEvents",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AccessRuleWindows",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AccessTimeBands",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AgreementSignatures",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AgreementTemplates",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AllowanceUsage",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Amendments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Announcements",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AppointmentParticipants",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AppointmentSeries",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AssessmentMeasures",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AssessmentValues",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Audit",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Authorisations",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Availability",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "BillingRunLines",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "BillingSchedules",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "BookingPolicies",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Campaigns",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CancellationPolicies",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CashMovements",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Certifications",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ChallengeParticipants",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CheckIns",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ChurnFactors",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ClassBookings",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ClassSchedules",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Clearances",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ClubClosures",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ClubSchedules",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CoachAssignments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CoachCheckIns",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CommissionAccruals",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CommissionRules",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CommissionStatements",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Complaints",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Consents",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CorporateInvoices",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CorporateMembers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CourseEnrolments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Credentials",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CreditBalances",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CreditMovements",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CreditNotes",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "DayPasses",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "DeferredRevenueEntries",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Doors",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "DunningEvents",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "DunningSteps",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "EffortSessions",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "EligibilityRules",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "EmergencyContacts",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "EquipmentUsage",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "FacilityCheckItems",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "FaultReports",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Feedback",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Freezes",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "GiftCardTransactions",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Goals",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "GradingEvents",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "GuestVisits",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "HabitEntries",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Handovers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "HouseAccountCharges",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "HouseholdMembers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "IncidentActions",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "InvoiceLines",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "JourneyEnrolments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "JourneySteps",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LeadActivities",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Leaderboard",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Ledger",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LockerAssignments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LossReasons",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LostProperty",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MaintenanceSchedules",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Mandates",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MarketplaceBookings",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MedicalFlags",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MemberAlerts",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MemberBadges",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MemberDocuments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MemberNotes",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MemberRanks",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MemberStatusHistory",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MemberTags",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MessageLog",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MessageTemplates",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "NpsResponses",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "NutritionPlans",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "OccupancySnapshots",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "PackagePurchases",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "PaymentMethods",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "PersonalRecords",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "PlanChangePaths",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "PlanPrices",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Preferences",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ProgramDays",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ProgressPhotos",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "PromoCodes",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Referrals",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Refunds",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ResourceBookings",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ResourceSlotRules",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "RetentionTasks",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "RoomSpots",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "SaleLines",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "SalesTargets",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "SaveOffers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ScreeningAnswers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Segments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "SignOffs",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "SkillClearances",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "StaffRoles",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "StaffTargets",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Streaks",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Strikes",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Suspensions",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "SwapRequests",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "TimeClock",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "TimeOff",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Tours",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Trials",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "VendingRevenue",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "WaiverSignatures",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "WorkOrders",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "WorkoutMovements",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "WorkoutResults",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "WriteOffs",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AccessRules",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "PlanEntitlements",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Assessments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Payers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "BookableStaff",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "BillingRuns",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CashSessions",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Challenges",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ChurnScores",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ClassOccurrences",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "SessionCredits",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "DeferredRevenue",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Controllers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "DunningCases",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "DunningPolicies",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CorporateAccounts",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "FacilityChecks",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "GiftCards",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Habits",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Incidents",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Journeys",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Lockers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LoyaltyAccounts",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "MarketplaceChannels",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Badges",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "RankLevels",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ProgramTracks",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Promotions",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Resources",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Sales",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "CancellationRequests",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "HealthScreenings",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ShiftAssignments",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Leads",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "WaiverTemplates",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Equipment",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Exercises",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "WorkoutSections",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "AssessmentTemplates",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "ClassTypes",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Rooms",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LockerBanks",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LoyaltyTiers",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "RankLadders",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Agreements",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Shifts",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Staff",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "LeadSources",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Workouts",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Areas",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Members",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Clubs",
                schema: "fitness");

            migrationBuilder.DropTable(
                name: "Households",
                schema: "fitness");
        }
    }
}
