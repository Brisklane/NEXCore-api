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
                name: "approval_workflows",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    document_type = table.Column<int>(type: "integer", nullable: false),
                    minimum_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    maximum_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_approval_workflows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_sequences",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    suffix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    separator = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    include_year = table.Column<bool>(type: "boolean", nullable: false),
                    year_format = table.Column<int>(type: "integer", nullable: false),
                    include_month = table.Column<bool>(type: "boolean", nullable: false),
                    sequence_padding = table.Column<int>(type: "integer", nullable: false),
                    reset_on = table.Column<int>(type: "integer", nullable: false),
                    next_sequence_number = table.Column<int>(type: "integer", nullable: false),
                    last_reset_year = table.Column<int>(type: "integer", nullable: true),
                    last_reset_month = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_document_sequences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "procurement_categories",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_ledger_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_tax_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    require_requisition = table.Column<bool>(type: "boolean", nullable: false),
                    rfq_threshold_amount = table.Column<decimal>(type: "numeric", nullable: true),
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
                    table.PrimaryKey("pk_procurement_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_procurement_categories_procurement_categories_parent_catego",
                        column: x => x.parent_category_id,
                        principalSchema: "procurement",
                        principalTable: "procurement_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_categories",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_vendor_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_categories_vendor_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalSchema: "procurement",
                        principalTable: "vendor_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "approval_workflow_steps",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_number = table.Column<int>(type: "integer", nullable: false),
                    step_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    approver_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approver_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    amount_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_parallel_step = table.Column<bool>(type: "boolean", nullable: false),
                    required_approvals = table.Column<int>(type: "integer", nullable: false),
                    escalation_after_days = table.Column<int>(type: "integer", nullable: false),
                    escalation_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    instructions = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_approval_workflow_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_workflow_steps_approval_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalSchema: "procurement",
                        principalTable: "approval_workflows",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "procurement_settings",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    require_requisition_for_po = table.Column<bool>(type: "boolean", nullable: false),
                    rfq_mandatory_above_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    enable3way_matching = table.Column<bool>(type: "boolean", nullable: false),
                    enforce_invoice_po_tolerance = table.Column<bool>(type: "boolean", nullable: false),
                    invoice_po_tolerance_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    default_payment_terms = table.Column<int>(type: "integer", nullable: false),
                    default_currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    default_lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    po_approval_workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requisition_approval_workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_approval_workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    require_vendor_approval = table.Column<bool>(type: "boolean", nullable: false),
                    require_vendor_bank_verification = table.Column<bool>(type: "boolean", nullable: false),
                    send_po_to_vendor_by_email = table.Column<bool>(type: "boolean", nullable: false),
                    send_rfq_to_vendor_by_email = table.Column<bool>(type: "boolean", nullable: false),
                    po_approval_reminder_days = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_procurement_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_procurement_settings_approval_workflows_invoice_approval_wo",
                        column: x => x.invoice_approval_workflow_id,
                        principalSchema: "procurement",
                        principalTable: "approval_workflows",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_procurement_settings_approval_workflows_po_approval_workflo",
                        column: x => x.po_approval_workflow_id,
                        principalSchema: "procurement",
                        principalTable: "approval_workflows",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_procurement_settings_approval_workflows_requisition_approva",
                        column: x => x.requisition_approval_workflow_id,
                        principalSchema: "procurement",
                        principalTable: "approval_workflows",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    short_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    onboarding_status = table.Column<int>(type: "integer", nullable: false),
                    tax_registration_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    company_registration_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    vat_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    vendor_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    primary_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    primary_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    primary_mobile = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_preferred_vendor = table.Column<bool>(type: "boolean", nullable: false),
                    ap_ledger_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subledger_type = table.Column<int>(type: "integer", nullable: false),
                    overall_rating = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    on_time_delivery_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    quality_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    block_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    blocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    blocked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_vendors", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendors_vendor_categories_vendor_category_id",
                        column: x => x.vendor_category_id,
                        principalSchema: "procurement",
                        principalTable: "vendor_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "approved_vendor_lists",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "text", nullable: true),
                    item_description = table.Column<string>(type: "text", nullable: true),
                    procurement_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_preferred = table.Column<bool>(type: "boolean", nullable: false),
                    is_exclusive = table.Column<bool>(type: "boolean", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    block_reason = table.Column<string>(type: "text", nullable: true),
                    default_unit_price = table.Column<decimal>(type: "numeric", nullable: true),
                    currency_code = table.Column<string>(type: "text", nullable: true),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    minimum_order_quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requires_quality_inspection = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_approved_vendor_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_approved_vendor_lists_procurement_categories_procurement_cat",
                        column: x => x.procurement_category_id,
                        principalSchema: "procurement",
                        principalTable: "procurement_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_approved_vendor_lists_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "landed_costs",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    landed_cost_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    document_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    total_landed_cost_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    accounting_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_landed_costs", x => x.id);
                    table.ForeignKey(
                        name: "fk_landed_costs_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_contracts",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contract_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terminated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    auto_renew = table.Column<bool>(type: "boolean", nullable: false),
                    renewal_notice_days = table.Column<int>(type: "integer", nullable: false),
                    renewal_duration_months = table.Column<int>(type: "integer", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    maximum_contract_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    committed_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    used_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    incoterm = table.Column<int>(type: "integer", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    termination_reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_purchase_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_contracts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisitions",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisition_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    required_by_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    suggested_vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    estimated_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_budget_checked = table.Column<bool>(type: "boolean", nullable: false),
                    is_budget_available = table.Column<bool>(type: "boolean", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_purchase_requisitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_requisitions_vendors_suggested_vendor_id",
                        column: x => x.suggested_vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_addresses",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type = table.Column<int>(type: "integer", nullable: false),
                    street = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    street2 = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_vendor_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_addresses_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_bank_accounts",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    bank_code = table.Column<string>(type: "text", nullable: true),
                    branch_code = table.Column<string>(type: "text", nullable: true),
                    branch_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    account_holder_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    iban = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    swift_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    routing_number = table.Column<string>(type: "text", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_vendor_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_bank_accounts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_contacts",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    job_title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    mobile = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_vendor_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_contacts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_documents",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<int>(type: "integer", nullable: false),
                    document_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    document_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    issued_by = table.Column<string>(type: "text", nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    never_expires = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reminder_days = table.Column<int>(type: "integer", nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    file_content_type = table.Column<string>(type: "text", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verification_notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_vendor_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_documents_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_performances",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    on_time_delivery_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    quality_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    price_compliance_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    responsiveness_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    document_accuracy_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    overall_rating = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    total_orders = table.Column<int>(type: "integer", nullable: false),
                    late_deliveries = table.Column<int>(type: "integer", nullable: false),
                    quality_rejections = table.Column<int>(type: "integer", nullable: false),
                    invoice_discrepancies = table.Column<int>(type: "integer", nullable: false),
                    total_purchase_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    evaluated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_vendor_performances", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_performances_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_pricelists",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_vendor_pricelists", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_pricelists_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "landed_cost_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    landed_cost_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    cost_type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    allocation_method = table.Column<int>(type: "integer", nullable: false),
                    ledger_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_landed_cost_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_landed_cost_lines_landed_costs_landed_cost_id",
                        column: x => x.landed_cost_id,
                        principalSchema: "procurement",
                        principalTable: "landed_costs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_contract_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    procurement_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    minimum_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    maximum_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    committed_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ordered_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_purchase_contract_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_contract_lines_procurement_categories_procurement_",
                        column: x => x.procurement_category_id,
                        principalSchema: "procurement",
                        principalTable: "procurement_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_contract_lines_purchase_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_contracts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "procurement_approvals",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<int>(type: "integer", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workflow_step_id = table.Column<Guid>(type: "uuid", nullable: true),
                    step_number = table.Column<int>(type: "integer", nullable: false),
                    step_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    action_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    delegated_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delegated_to_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    delegation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_escalated = table.Column<bool>(type: "boolean", nullable: false),
                    escalated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_procurement_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_procurement_approvals_approval_workflow_steps_workflow_step",
                        column: x => x.workflow_step_id,
                        principalSchema: "procurement",
                        principalTable: "approval_workflow_steps",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_procurement_approvals_approval_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalSchema: "procurement",
                        principalTable: "approval_workflows",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_procurement_approvals_purchase_requisitions_document_id",
                        column: x => x.document_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_requisitions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    estimated_unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    procurement_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_by_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivery_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_status = table.Column<int>(type: "integer", nullable: false),
                    quantity_ordered = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    suggested_vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_purchase_requisition_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_requisition_lines_procurement_categories_procureme",
                        column: x => x.procurement_category_id,
                        principalSchema: "procurement",
                        principalTable: "procurement_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_requisition_lines_purchase_requisitions_requisitio",
                        column: x => x.requisition_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_requisitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_requisition_lines_vendors_suggested_vendor_id",
                        column: x => x.suggested_vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "request_for_quotations",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    requisition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submission_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    quotation_validity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    awarded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    delivery_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    evaluation_criteria = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_request_for_quotations", x => x.id);
                    table.ForeignKey(
                        name: "fk_request_for_quotations_purchase_requisitions_requisition_id",
                        column: x => x.requisition_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_requisitions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_payments",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    value_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cleared_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    payment_method = table.Column<int>(type: "integer", nullable: false),
                    company_bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendor_bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unallocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bank_reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    check_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    transaction_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    accounting_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiscal_period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    withholding_tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    withholding_tax_ledger_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_vendor_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_payments_vendor_bank_accounts_vendor_bank_account_id",
                        column: x => x.vendor_bank_account_id,
                        principalSchema: "procurement",
                        principalTable: "vendor_bank_accounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vendor_payments_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_pricelist_items",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricelist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "text", nullable: true),
                    item_description = table.Column<string>(type: "text", nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "text", nullable: true),
                    minimum_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric", nullable: true),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_vendor_pricelist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_pricelist_items_vendor_pricelists_pricelist_id",
                        column: x => x.pricelist_id,
                        principalSchema: "procurement",
                        principalTable: "vendor_pricelists",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "rfq_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    requisition_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    estimated_unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    procurement_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    specifications = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_rfq_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfq_lines_procurement_categories_procurement_category_id",
                        column: x => x.procurement_category_id,
                        principalSchema: "procurement",
                        principalTable: "procurement_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_rfq_lines_purchase_requisition_lines_requisition_line_id",
                        column: x => x.requisition_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_rfq_lines_request_for_quotations_rfq_id",
                        column: x => x.rfq_id,
                        principalSchema: "procurement",
                        principalTable: "request_for_quotations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "rfq_vendors",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    response_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    decline_reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_rfq_vendors", x => x.id);
                    table.ForeignKey(
                        name: "fk_rfq_vendors_request_for_quotations_rfq_id",
                        column: x => x.rfq_id,
                        principalSchema: "procurement",
                        principalTable: "request_for_quotations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_rfq_vendors_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_quotations",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vendor_quotation_reference = table.Column<string>(type: "text", nullable: true),
                    rfq_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    sub_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    incoterm = table.Column<int>(type: "integer", nullable: true),
                    incoterm_location = table.Column<string>(type: "text", nullable: true),
                    delivery_lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    technical_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    commercial_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    overall_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    is_recommended = table.Column<bool>(type: "boolean", nullable: false),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_vendor_quotations", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_quotations_request_for_quotations_rfq_id",
                        column: x => x.rfq_id,
                        principalSchema: "procurement",
                        principalTable: "request_for_quotations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vendor_quotations_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    vendor_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requisition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expected_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_to_vendor_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    delivery_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delivery_street = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    delivery_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    delivery_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    delivery_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    delivery_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    incoterm = table.Column<int>(type: "integer", nullable: true),
                    incoterm_location = table.Column<string>(type: "text", nullable: true),
                    sub_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    invoiced_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    outstanding_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accounting_commitment_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_purchase_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_contracts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_orders_purchase_requisitions_requisition_id",
                        column: x => x.requisition_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_requisitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_orders_vendor_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalSchema: "procurement",
                        principalTable: "vendor_quotations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_orders_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_quotation_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    promised_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lead_time_days = table.Column<int>(type: "integer", nullable: true),
                    is_alternative = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_vendor_quotation_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_quotation_lines_rfq_lines_rfq_line_id",
                        column: x => x.rfq_line_id,
                        principalSchema: "procurement",
                        principalTable: "rfq_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vendor_quotation_lines_vendor_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalSchema: "procurement",
                        principalTable: "vendor_quotations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "goods_receipts",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vendor_delivery_note_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    receipt_type = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accounting_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiscal_period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_goods_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_goods_receipts_goods_receipts_original_receipt_id",
                        column: x => x.original_receipt_id,
                        principalSchema: "procurement",
                        principalTable: "goods_receipts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_goods_receipts_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_goods_receipts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoices",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vendor_invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posting_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    matching_status = table.Column<int>(type: "integer", nullable: false),
                    payment_status = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    sub_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    outstanding_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    recoverable_tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    non_recoverable_tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    accounting_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiscal_period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dimension_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hold_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    dispute_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_purchase_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_invoices_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_invoices_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_amendments",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amendment_number = table.Column<int>(type: "integer", nullable: false),
                    amendment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    previous_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    requires_re_approval = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_purchase_order_amendments", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_amendments_purchase_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_approvals",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_number = table.Column<int>(type: "integer", nullable: false),
                    step_name = table.Column<string>(type: "text", nullable: true),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: true),
                    action_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    delegated_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_escalated = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_purchase_order_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_approvals_purchase_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    requisition_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    procurement_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ledger_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dimension_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expected_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivery_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_accepted = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_rejected = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_returned = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_invoiced = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    line_status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_purchase_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_procurement_categories_procurement_cat",
                        column: x => x.procurement_category_id,
                        principalSchema: "procurement",
                        principalTable: "procurement_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_purchase_contract_lines_contract_line_",
                        column: x => x.contract_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_contract_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_purchase_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_purchase_requisition_lines_req_f05a0a51",
                        column: x => x.requisition_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "landed_cost_goods_receipts",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    landed_cost_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_landed_cost_goods_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_landed_cost_goods_receipts_goods_receipts_goods_receipt_id",
                        column: x => x.goods_receipt_id,
                        principalSchema: "procurement",
                        principalTable: "goods_receipts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_landed_cost_goods_receipts_landed_costs_landed_cost_id",
                        column: x => x.landed_cost_id,
                        principalSchema: "procurement",
                        principalTable: "landed_costs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_returns",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    return_reason = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    total_return_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    accounting_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiscal_period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendor_return_authorisation_number = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_purchase_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_returns_goods_receipts_goods_receipt_id",
                        column: x => x.goods_receipt_id,
                        principalSchema: "procurement",
                        principalTable: "goods_receipts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_returns_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_returns_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_payment_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    invoice_outstanding_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_taken = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    write_off_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fx_gain_loss_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fx_gain_loss_ledger_account_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_vendor_payment_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_payment_lines_purchase_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_invoices",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vendor_payment_lines_vendor_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "procurement",
                        principalTable: "vendor_payments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity_ordered = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_accepted = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_rejected = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lot_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    manufacturer_batch_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    storage_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storage_location_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    quality_status = table.Column<int>(type: "integer", nullable: false),
                    quality_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    inspected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inspected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_goods_receipt_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_goods_receipt_lines_goods_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalSchema: "procurement",
                        principalTable: "goods_receipts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_goods_receipt_lines_purchase_order_lines_purchase_o_a4722671",
                        column: x => x.purchase_order_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_order_lines",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_debit_notes",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit_note_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purchase_return_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_name = table.Column<string>(type: "text", nullable: true),
                    original_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    debit_note_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    sub_total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    settled_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    outstanding_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    accounting_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_vendor_debit_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_debit_notes_purchase_invoices_original_invoice_id",
                        column: x => x.original_invoice_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_invoices",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vendor_debit_notes_purchase_returns_purchase_return_id",
                        column: x => x.purchase_return_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_returns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vendor_debit_notes_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "procurement",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "landed_cost_allocations",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    landed_cost_id = table.Column<Guid>(type: "uuid", nullable: false),
                    landed_cost_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_method = table.Column<int>(type: "integer", nullable: false),
                    allocation_basis_value = table.Column<decimal>(type: "numeric", nullable: false),
                    total_basis_value = table.Column<decimal>(type: "numeric", nullable: false),
                    allocation_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_amount_per_unit = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("pk_landed_cost_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_landed_cost_allocations_goods_receipt_lines_goods_receipt_l",
                        column: x => x.goods_receipt_line_id,
                        principalSchema: "procurement",
                        principalTable: "goods_receipt_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_landed_cost_allocations_landed_cost_lines_landed_co_37a6bcca",
                        column: x => x.landed_cost_line_id,
                        principalSchema: "procurement",
                        principalTable: "landed_cost_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_landed_cost_allocations_landed_costs_landed_cost_id",
                        column: x => x.landed_cost_id,
                        principalSchema: "procurement",
                        principalTable: "landed_costs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoice_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    goods_receipt_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    recoverable_tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    non_recoverable_tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    procurement_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ledger_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dimension_set_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_purchase_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_goods_receipt_lines_goods_receipt_li",
                        column: x => x.goods_receipt_line_id,
                        principalSchema: "procurement",
                        principalTable: "goods_receipt_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_procurement_categories_procurement_c",
                        column: x => x.procurement_category_id,
                        principalSchema: "procurement",
                        principalTable: "procurement_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_purchase_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_invoices",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_purchase_order_lines_purchas_1bdde5e8",
                        column: x => x.purchase_order_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_order_lines",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_return_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_return_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    goods_receipt_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "text", nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_returned = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_of_measure_name = table.Column<string>(type: "text", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_return_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    return_reason = table.Column<int>(type: "integer", nullable: false),
                    quality_issue_description = table.Column<string>(type: "text", nullable: true),
                    lot_number = table.Column<string>(type: "text", nullable: true),
                    return_storage_location_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_purchase_return_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_return_lines_goods_receipt_lines_goods_receipt_lin",
                        column: x => x.goods_receipt_line_id,
                        principalSchema: "procurement",
                        principalTable: "goods_receipt_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_return_lines_purchase_order_lines_purchase_order_l",
                        column: x => x.purchase_order_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_order_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_purchase_return_lines_purchase_returns_purchase_return_id",
                        column: x => x.purchase_return_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_returns",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "three_way_match_records",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    goods_receipt_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    match_status = table.Column<int>(type: "integer", nullable: false),
                    has_discrepancy = table.Column<bool>(type: "boolean", nullable: false),
                    discrepancy_type = table.Column<int>(type: "integer", nullable: true),
                    po_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    invoiced_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_variance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    po_unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    invoice_unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    price_variance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_variance_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    matched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    matched_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_three_way_match_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_three_way_match_records_goods_receipt_lines_goods_receipt_l",
                        column: x => x.goods_receipt_line_id,
                        principalSchema: "procurement",
                        principalTable: "goods_receipt_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_three_way_match_records_purchase_invoice_lines_invoice_line",
                        column: x => x.invoice_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_invoice_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_three_way_match_records_purchase_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_invoices",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_three_way_match_records_purchase_order_lines_purchase_order",
                        column: x => x.purchase_order_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_order_lines",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vendor_debit_note_lines",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    return_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_code = table.Column<string>(type: "text", nullable: true),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ledger_account_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_vendor_debit_note_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_debit_note_lines_purchase_return_lines_return_line_id",
                        column: x => x.return_line_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_return_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vendor_debit_note_lines_vendor_debit_notes_debit_note_id",
                        column: x => x.debit_note_id,
                        principalSchema: "procurement",
                        principalTable: "vendor_debit_notes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_approval_workflow_steps_workflow_id",
                schema: "procurement",
                table: "approval_workflow_steps",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_approved_vendor_lists_procurement_category_id",
                schema: "procurement",
                table: "approved_vendor_lists",
                column: "procurement_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_approved_vendor_lists_vendor_id",
                schema: "procurement",
                table: "approved_vendor_lists",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_sequence_company_type",
                schema: "procurement",
                table: "document_sequences",
                columns: new[] { "company_id", "document_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_lines_purchase_order_line_id",
                schema: "procurement",
                table: "goods_receipt_lines",
                column: "purchase_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_lines_receipt_id",
                schema: "procurement",
                table: "goods_receipt_lines",
                column: "receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_company_number",
                schema: "procurement",
                table: "goods_receipts",
                columns: new[] { "company_id", "receipt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_original_receipt_id",
                schema: "procurement",
                table: "goods_receipts",
                column: "original_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_purchase_order_id",
                schema: "procurement",
                table: "goods_receipts",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_vendor_id",
                schema: "procurement",
                table: "goods_receipts",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_landed_cost_allocations_goods_receipt_line_id",
                schema: "procurement",
                table: "landed_cost_allocations",
                column: "goods_receipt_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_landed_cost_allocations_landed_cost_id",
                schema: "procurement",
                table: "landed_cost_allocations",
                column: "landed_cost_id");

            migrationBuilder.CreateIndex(
                name: "ix_landed_cost_allocations_landed_cost_line_id",
                schema: "procurement",
                table: "landed_cost_allocations",
                column: "landed_cost_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_landed_cost_goods_receipts_goods_receipt_id",
                schema: "procurement",
                table: "landed_cost_goods_receipts",
                column: "goods_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_landed_cost_goods_receipts_landed_cost_id",
                schema: "procurement",
                table: "landed_cost_goods_receipts",
                column: "landed_cost_id");

            migrationBuilder.CreateIndex(
                name: "ix_landed_cost_lines_landed_cost_id",
                schema: "procurement",
                table: "landed_cost_lines",
                column: "landed_cost_id");

            migrationBuilder.CreateIndex(
                name: "ix_landed_cost_company_number",
                schema: "procurement",
                table: "landed_costs",
                columns: new[] { "company_id", "landed_cost_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_landed_costs_vendor_id",
                schema: "procurement",
                table: "landed_costs",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_procurement_approval_document",
                schema: "procurement",
                table: "procurement_approvals",
                columns: new[] { "document_type", "document_id" });

            migrationBuilder.CreateIndex(
                name: "ix_procurement_approvals_document_id",
                schema: "procurement",
                table: "procurement_approvals",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_procurement_approvals_workflow_id",
                schema: "procurement",
                table: "procurement_approvals",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_procurement_approvals_workflow_step_id",
                schema: "procurement",
                table: "procurement_approvals",
                column: "workflow_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_procurement_categories_parent_category_id",
                schema: "procurement",
                table: "procurement_categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_procurement_category_company_code",
                schema: "procurement",
                table: "procurement_categories",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_procurement_settings_invoice_approval_workflow_id",
                schema: "procurement",
                table: "procurement_settings",
                column: "invoice_approval_workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_procurement_settings_po_approval_workflow_id",
                schema: "procurement",
                table: "procurement_settings",
                column: "po_approval_workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_procurement_settings_requisition_approval_workflow_id",
                schema: "procurement",
                table: "procurement_settings",
                column: "requisition_approval_workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_contract_lines_contract_id",
                schema: "procurement",
                table: "purchase_contract_lines",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_contract_lines_procurement_category_id",
                schema: "procurement",
                table: "purchase_contract_lines",
                column: "procurement_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_contract_company_number",
                schema: "procurement",
                table: "purchase_contracts",
                columns: new[] { "company_id", "contract_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_contracts_vendor_id",
                schema: "procurement",
                table: "purchase_contracts",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_lines_goods_receipt_line_id",
                schema: "procurement",
                table: "purchase_invoice_lines",
                column: "goods_receipt_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_lines_invoice_id",
                schema: "procurement",
                table: "purchase_invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_lines_procurement_category_id",
                schema: "procurement",
                table: "purchase_invoice_lines",
                column: "procurement_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_lines_purchase_order_line_id",
                schema: "procurement",
                table: "purchase_invoice_lines",
                column: "purchase_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_company_number",
                schema: "procurement",
                table: "purchase_invoices",
                columns: new[] { "company_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_vendor_vendor_number",
                schema: "procurement",
                table: "purchase_invoices",
                columns: new[] { "company_id", "vendor_id", "vendor_invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_purchase_order_id",
                schema: "procurement",
                table: "purchase_invoices",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_vendor_id",
                schema: "procurement",
                table: "purchase_invoices",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_amendments_order_id",
                schema: "procurement",
                table: "purchase_order_amendments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_approvals_order_id",
                schema: "procurement",
                table: "purchase_order_approvals",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_contract_line_id",
                schema: "procurement",
                table: "purchase_order_lines",
                column: "contract_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_order_id",
                schema: "procurement",
                table: "purchase_order_lines",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_procurement_category_id",
                schema: "procurement",
                table: "purchase_order_lines",
                column: "procurement_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_requisition_line_id",
                schema: "procurement",
                table: "purchase_order_lines",
                column: "requisition_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_company_number",
                schema: "procurement",
                table: "purchase_orders",
                columns: new[] { "company_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_company_status",
                schema: "procurement",
                table: "purchase_orders",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_contract_id",
                schema: "procurement",
                table: "purchase_orders",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_quotation_id",
                schema: "procurement",
                table: "purchase_orders",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_requisition_id",
                schema: "procurement",
                table: "purchase_orders",
                column: "requisition_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_vendor_id",
                schema: "procurement",
                table: "purchase_orders",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_lines_procurement_category_id",
                schema: "procurement",
                table: "purchase_requisition_lines",
                column: "procurement_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_lines_requisition_id",
                schema: "procurement",
                table: "purchase_requisition_lines",
                column: "requisition_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_lines_suggested_vendor_id",
                schema: "procurement",
                table: "purchase_requisition_lines",
                column: "suggested_vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_company_number",
                schema: "procurement",
                table: "purchase_requisitions",
                columns: new[] { "company_id", "requisition_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_company_status",
                schema: "procurement",
                table: "purchase_requisitions",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisitions_suggested_vendor_id",
                schema: "procurement",
                table: "purchase_requisitions",
                column: "suggested_vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_lines_goods_receipt_line_id",
                schema: "procurement",
                table: "purchase_return_lines",
                column: "goods_receipt_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_lines_purchase_order_line_id",
                schema: "procurement",
                table: "purchase_return_lines",
                column: "purchase_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_lines_purchase_return_id",
                schema: "procurement",
                table: "purchase_return_lines",
                column: "purchase_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_company_number",
                schema: "procurement",
                table: "purchase_returns",
                columns: new[] { "company_id", "return_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_goods_receipt_id",
                schema: "procurement",
                table: "purchase_returns",
                column: "goods_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_purchase_order_id",
                schema: "procurement",
                table: "purchase_returns",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_vendor_id",
                schema: "procurement",
                table: "purchase_returns",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_request_for_quotation_company_number",
                schema: "procurement",
                table: "request_for_quotations",
                columns: new[] { "company_id", "rfq_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_request_for_quotations_requisition_id",
                schema: "procurement",
                table: "request_for_quotations",
                column: "requisition_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfq_lines_procurement_category_id",
                schema: "procurement",
                table: "rfq_lines",
                column: "procurement_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfq_lines_requisition_line_id",
                schema: "procurement",
                table: "rfq_lines",
                column: "requisition_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfq_lines_rfq_id",
                schema: "procurement",
                table: "rfq_lines",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfq_vendor_rfq_vendor",
                schema: "procurement",
                table: "rfq_vendors",
                columns: new[] { "rfq_id", "vendor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rfq_vendors_vendor_id",
                schema: "procurement",
                table: "rfq_vendors",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_three_way_match_records_goods_receipt_line_id",
                schema: "procurement",
                table: "three_way_match_records",
                column: "goods_receipt_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_three_way_match_records_invoice_id",
                schema: "procurement",
                table: "three_way_match_records",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_three_way_match_records_invoice_line_id",
                schema: "procurement",
                table: "three_way_match_records",
                column: "invoice_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_three_way_match_records_purchase_order_line_id",
                schema: "procurement",
                table: "three_way_match_records",
                column: "purchase_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_addresses_vendor_id",
                schema: "procurement",
                table: "vendor_addresses",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bank_accounts_vendor_id",
                schema: "procurement",
                table: "vendor_bank_accounts",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_categories_parent_category_id",
                schema: "procurement",
                table: "vendor_categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_category_company_code",
                schema: "procurement",
                table: "vendor_categories",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_contacts_vendor_id",
                schema: "procurement",
                table: "vendor_contacts",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_debit_note_lines_debit_note_id",
                schema: "procurement",
                table: "vendor_debit_note_lines",
                column: "debit_note_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_debit_note_lines_return_line_id",
                schema: "procurement",
                table: "vendor_debit_note_lines",
                column: "return_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_debit_note_company_number",
                schema: "procurement",
                table: "vendor_debit_notes",
                columns: new[] { "company_id", "debit_note_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_debit_notes_original_invoice_id",
                schema: "procurement",
                table: "vendor_debit_notes",
                column: "original_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_debit_notes_purchase_return_id",
                schema: "procurement",
                table: "vendor_debit_notes",
                column: "purchase_return_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_debit_notes_vendor_id",
                schema: "procurement",
                table: "vendor_debit_notes",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_documents_vendor_id",
                schema: "procurement",
                table: "vendor_documents",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payment_line_payment_invoice",
                schema: "procurement",
                table: "vendor_payment_lines",
                columns: new[] { "payment_id", "invoice_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payment_lines_invoice_id",
                schema: "procurement",
                table: "vendor_payment_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payment_company_number",
                schema: "procurement",
                table: "vendor_payments",
                columns: new[] { "company_id", "payment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payments_vendor_bank_account_id",
                schema: "procurement",
                table: "vendor_payments",
                column: "vendor_bank_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payments_vendor_id",
                schema: "procurement",
                table: "vendor_payments",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_performance_vendor_period",
                schema: "procurement",
                table: "vendor_performances",
                columns: new[] { "vendor_id", "period_from" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_pricelist_items_pricelist_id",
                schema: "procurement",
                table: "vendor_pricelist_items",
                column: "pricelist_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_pricelists_vendor_id",
                schema: "procurement",
                table: "vendor_pricelists",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_quotation_lines_quotation_id",
                schema: "procurement",
                table: "vendor_quotation_lines",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_quotation_lines_rfq_line_id",
                schema: "procurement",
                table: "vendor_quotation_lines",
                column: "rfq_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_quotation_company_number",
                schema: "procurement",
                table: "vendor_quotations",
                columns: new[] { "company_id", "quotation_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_quotations_rfq_id",
                schema: "procurement",
                table: "vendor_quotations",
                column: "rfq_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_quotations_vendor_id",
                schema: "procurement",
                table: "vendor_quotations",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_company_number",
                schema: "procurement",
                table: "vendors",
                columns: new[] { "company_id", "vendor_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendors_vendor_category_id",
                schema: "procurement",
                table: "vendors",
                column: "vendor_category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approved_vendor_lists",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "document_sequences",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "landed_cost_allocations",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "landed_cost_goods_receipts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "procurement_approvals",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "procurement_settings",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_order_amendments",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_order_approvals",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "rfq_vendors",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "three_way_match_records",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_addresses",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_contacts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_debit_note_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_documents",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_payment_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_performances",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_pricelist_items",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_quotation_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "landed_cost_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "approval_workflow_steps",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_invoice_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_return_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_debit_notes",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_payments",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_pricelists",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "rfq_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "landed_costs",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "approval_workflows",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "goods_receipt_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_invoices",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_returns",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_bank_accounts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_order_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "goods_receipts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_contract_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_requisition_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "procurement_categories",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_contracts",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_quotations",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "request_for_quotations",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_requisitions",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendors",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_categories",
                schema: "procurement");
        }
    }
}
