using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    billing_street = table.Column<string>(type: "text", nullable: true),
                    billing_city = table.Column<string>(type: "text", nullable: true),
                    billing_state = table.Column<string>(type: "text", nullable: true),
                    billing_postal_code = table.Column<string>(type: "text", nullable: true),
                    billing_country = table.Column<string>(type: "text", nullable: true),
                    shipping_street = table.Column<string>(type: "text", nullable: true),
                    shipping_city = table.Column<string>(type: "text", nullable: true),
                    shipping_state = table.Column<string>(type: "text", nullable: true),
                    shipping_postal_code = table.Column<string>(type: "text", nullable: true),
                    shipping_country = table.Column<string>(type: "text", nullable: true),
                    parent_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounts_accounts_parent_account_id",
                        column: x => x.parent_account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_name = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    http_method = table.Column<string>(type: "text", nullable: true),
                    endpoint = table.Column<string>(type: "text", nullable: true),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_username = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "text", nullable: true),
                    severity = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
                    parent_campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expected_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    budgeted_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    actual_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    num_sent = table.Column<int>(type: "integer", nullable: true),
                    expected_response_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "fk_campaigns_campaigns_parent_campaign_id",
                        column: x => x.parent_campaign_id,
                        principalSchema: "crm",
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contact_lists",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    list_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_dynamic = table.Column<bool>(type: "boolean", nullable: false),
                    filter_criteria = table.Column<string>(type: "text", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_contact_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_messages",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    html_body = table.Column<string>(type: "text", nullable: true),
                    text_body = table.Column<string>(type: "text", nullable: true),
                    from_address = table.Column<string>(type: "text", nullable: true),
                    from_name = table.Column<string>(type: "text", nullable: true),
                    to_address = table.Column<string>(type: "text", nullable: true),
                    cc_address = table.Column<string>(type: "text", nullable: true),
                    bcc_address = table.Column<string>(type: "text", nullable: true),
                    incoming = table.Column<bool>(type: "boolean", nullable: false),
                    message_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    has_attachment = table.Column<bool>(type: "boolean", nullable: false),
                    is_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    open_count = table.Column<int>(type: "integer", nullable: false),
                    first_open_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    click_count = table.Column<int>(type: "integer", nullable: false),
                    first_click_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    related_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_to_type = table.Column<string>(type: "text", nullable: true),
                    message_id = table.Column<string>(type: "text", nullable: true),
                    in_reply_to_id = table.Column<string>(type: "text", nullable: true),
                    thread_identifier = table.Column<string>(type: "text", nullable: true),
                    sent_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_email_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "forecasts",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    fiscal_quarter = table.Column<int>(type: "integer", nullable: false),
                    fiscal_month = table.Column<int>(type: "integer", nullable: true),
                    period_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pipeline_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    best_case_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    commit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    closed_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    adjusted_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    quota_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_manager_adjusted = table.Column<bool>(type: "boolean", nullable: false),
                    is_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_forecasts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_articles",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    url_name = table.Column<string>(type: "text", nullable: true),
                    article_type = table.Column<string>(type: "text", nullable: true),
                    category_group = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    is_visible_in_app = table.Column<bool>(type: "boolean", nullable: false),
                    is_visible_in_csp = table.Column<bool>(type: "boolean", nullable: false),
                    is_visible_in_pkb = table.Column<bool>(type: "boolean", nullable: false),
                    published_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_knowledge_articles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pipelines",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pipeline_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_pipelines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pricebooks",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricebook_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_standard = table.Column<bool>(type: "boolean", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
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
                    table.PrimaryKey("pk_pricebooks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    product_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_family = table.Column<string>(type: "text", nullable: true),
                    quantity_unit = table.Column<string>(type: "text", nullable: true),
                    quantity_unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "territories",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    territory_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    parent_territory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_territories", x => x.id);
                    table.ForeignKey(
                        name: "fk_territories_territories_parent_territory_id",
                        column: x => x.parent_territory_id,
                        principalSchema: "crm",
                        principalTable: "territories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_stages",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pipeline_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    probability_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 18, scale: 2, nullable: false),
                    is_won = table.Column<bool>(type: "boolean", nullable: false),
                    is_lost = table.Column<bool>(type: "boolean", nullable: false),
                    forecast_category = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_pipeline_stages", x => x.id);
                    table.ForeignKey(
                        name: "fk_pipeline_stages_pipelines_pipeline_id",
                        column: x => x.pipeline_id,
                        principalSchema: "crm",
                        principalTable: "pipelines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricebook_entries",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricebook_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 4, nullable: false),
                    use_standard_price = table.Column<bool>(type: "boolean", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
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
                    table.PrimaryKey("pk_pricebook_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_pricebook_entries_pricebooks_pricebook_id",
                        column: x => x.pricebook_id,
                        principalSchema: "crm",
                        principalTable: "pricebooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pricebook_entries_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "crm",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entity_tags",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("pk_entity_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_entity_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "crm",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    account_access_level = table.Column<string>(type: "text", nullable: true),
                    opportunity_access_level = table.Column<string>(type: "text", nullable: true),
                    case_access_level = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_team_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_members_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "crm",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "territory_accounts",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    territory_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_territory_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_territory_accounts_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_territory_accounts_territories_territory_id",
                        column: x => x.territory_id,
                        principalSchema: "crm",
                        principalTable: "territories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    start_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<string>(type: "text", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    call_type = table.Column<string>(type: "text", nullable: true),
                    call_purpose = table.Column<string>(type: "text", nullable: true),
                    call_result = table.Column<string>(type: "text", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true),
                    related_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_to_type = table.Column<string>(type: "text", nullable: true),
                    name_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name_type = table.Column<string>(type: "text", nullable: true),
                    assigned_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_activity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaign_activity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_activity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_activity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deal_activity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_activities_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_extension = table.Column<string>(type: "text", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: true),
                    storage_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    knowledge_article_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_attachments_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_attachments_email_messages_email_message_id",
                        column: x => x.email_message_id,
                        principalSchema: "crm",
                        principalTable: "email_messages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_attachments_knowledge_articles_knowledge_article_id",
                        column: x => x.knowledge_article_id,
                        principalSchema: "crm",
                        principalTable: "knowledge_articles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "campaign_members",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    first_responded_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_campaign_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_campaign_members_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalSchema: "crm",
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "case_comments",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comment_body = table.Column<string>(type: "text", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_customer_comment = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_case_comments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cases",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    case_origin = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entitlement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject = table.Column<string>(type: "text", nullable: true),
                    send_notification_email = table.Column<bool>(type: "boolean", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_cases_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contact_addresses",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Shipping"),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    street2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    whats_app = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    delivery_instructions = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_contact_addresses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contact_list_members",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: true),
                    member_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_contact_list_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_contact_list_members_contact_lists_contact_list_id",
                        column: x => x.contact_list_id,
                        principalSchema: "crm",
                        principalTable: "contact_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contacts",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    salutation = table.Column<string>(type: "text", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    short_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    profile_image_url = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    job_title = table.Column<string>(type: "text", nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    reports_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    mobile = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fax = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    website = table.Column<string>(type: "text", nullable: true),
                    customer_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    customer_type = table.Column<int>(type: "integer", nullable: false),
                    tax_registration_number = table.Column<string>(type: "text", nullable: true),
                    national_id = table.Column<string>(type: "text", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    push_token = table.Column<string>(type: "text", nullable: true),
                    push_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    marketing_opt_in = table.Column<bool>(type: "boolean", nullable: false),
                    preferred_language = table.Column<string>(type: "text", nullable: true),
                    preferred_currency_code = table.Column<string>(type: "text", nullable: true),
                    mailing_street = table.Column<string>(type: "text", nullable: true),
                    mailing_city = table.Column<string>(type: "text", nullable: true),
                    mailing_state = table.Column<string>(type: "text", nullable: true),
                    mailing_postal_code = table.Column<string>(type: "text", nullable: true),
                    mailing_country = table.Column<string>(type: "text", nullable: true),
                    default_currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    default_payment_terms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    default_price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    outstanding_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_status = table.Column<int>(type: "integer", nullable: false),
                    allow_credit_sales = table.Column<bool>(type: "boolean", nullable: false),
                    loyalty_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email_opt_out = table.Column<bool>(type: "boolean", nullable: false),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    is_tax_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    converted_from_lead_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_contacts_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contacts_contacts_reports_to_id",
                        column: x => x.reports_to_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contract_term_months = table.Column<int>(type: "integer", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contract_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_code = table.Column<string>(type: "text", nullable: true),
                    billing_contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    special_terms = table.Column<string>(type: "text", nullable: true),
                    activated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_contracts_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_contacts_billing_contact_id",
                        column: x => x.billing_contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deals",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    close_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    stage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    forecast_category = table.Column<string>(type: "text", nullable: true),
                    pipeline_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pipeline_stage_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_deals", x => x.id);
                    table.ForeignKey(
                        name: "fk_deals_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deals_contacts_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_deals_pipeline_stages_pipeline_stage_id",
                        column: x => x.pipeline_stage_id,
                        principalSchema: "crm",
                        principalTable: "pipeline_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deals_pipelines_pipeline_id",
                        column: x => x.pipeline_id,
                        principalSchema: "crm",
                        principalTable: "pipelines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entitlements",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entitlement_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_level_name = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "text", nullable: true),
                    is_per_incident = table.Column<bool>(type: "boolean", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cases_per_entitlement = table.Column<int>(type: "integer", nullable: true),
                    cases_used = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_entitlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_entitlements_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_entitlements_contacts_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deal_contacts",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_deal_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_deal_contacts_contacts_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deal_contacts_deals_deal_id",
                        column: x => x.deal_id,
                        principalSchema: "crm",
                        principalTable: "deals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deal_products",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricebook_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 4, nullable: false),
                    list_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 4, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(5,2)", precision: 18, scale: 2, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_deal_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_deal_products_deals_deal_id",
                        column: x => x.deal_id,
                        principalSchema: "crm",
                        principalTable: "deals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_deal_products_pricebook_entries_pricebook_entry_id",
                        column: x => x.pricebook_entry_id,
                        principalSchema: "crm",
                        principalTable: "pricebook_entries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_deal_products_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "crm",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    salutation = table.Column<string>(type: "text", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    website = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    street = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<string>(type: "text", nullable: true),
                    postal_code = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    number_of_employees = table.Column<int>(type: "integer", nullable: true),
                    annual_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    lead_source = table.Column<string>(type: "text", nullable: true),
                    industry = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email_opt_out = table.Column<bool>(type: "boolean", nullable: false),
                    is_converted = table.Column<bool>(type: "boolean", nullable: false),
                    converted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    converted_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    converted_contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    converted_deal_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_leads", x => x.id);
                    table.ForeignKey(
                        name: "fk_leads_accounts_converted_account_id",
                        column: x => x.converted_account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leads_contacts_converted_contact_id",
                        column: x => x.converted_contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leads_deals_converted_deal_id",
                        column: x => x.converted_deal_id,
                        principalSchema: "crm",
                        principalTable: "deals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quote_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricebook_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_syncing = table.Column<bool>(type: "boolean", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(5,2)", precision: 18, scale: 2, nullable: true),
                    tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    shipping_and_handling = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    grand_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    billing_street = table.Column<string>(type: "text", nullable: true),
                    billing_city = table.Column<string>(type: "text", nullable: true),
                    billing_state = table.Column<string>(type: "text", nullable: true),
                    billing_postal_code = table.Column<string>(type: "text", nullable: true),
                    billing_country = table.Column<string>(type: "text", nullable: true),
                    shipping_street = table.Column<string>(type: "text", nullable: true),
                    shipping_city = table.Column<string>(type: "text", nullable: true),
                    shipping_state = table.Column<string>(type: "text", nullable: true),
                    shipping_postal_code = table.Column<string>(type: "text", nullable: true),
                    shipping_country = table.Column<string>(type: "text", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_quotes", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotes_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quotes_contacts_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quotes_deals_deal_id",
                        column: x => x.deal_id,
                        principalSchema: "crm",
                        principalTable: "deals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quotes_pricebooks_pricebook_id",
                        column: x => x.pricebook_id,
                        principalSchema: "crm",
                        principalTable: "pricebooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lead_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_notes_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notes_cases_case_id",
                        column: x => x.case_id,
                        principalSchema: "crm",
                        principalTable: "cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notes_contacts_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notes_deals_deal_id",
                        column: x => x.deal_id,
                        principalSchema: "crm",
                        principalTable: "deals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_notes_leads_lead_id",
                        column: x => x.lead_id,
                        principalSchema: "crm",
                        principalTable: "leads",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_name = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pricebook_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    order_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    activated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shipped_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    shipping_and_handling = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    grand_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    billing_street = table.Column<string>(type: "text", nullable: true),
                    billing_city = table.Column<string>(type: "text", nullable: true),
                    billing_state = table.Column<string>(type: "text", nullable: true),
                    billing_postal_code = table.Column<string>(type: "text", nullable: true),
                    billing_country = table.Column<string>(type: "text", nullable: true),
                    shipping_street = table.Column<string>(type: "text", nullable: true),
                    shipping_city = table.Column<string>(type: "text", nullable: true),
                    shipping_state = table.Column<string>(type: "text", nullable: true),
                    shipping_postal_code = table.Column<string>(type: "text", nullable: true),
                    shipping_country = table.Column<string>(type: "text", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "crm",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "crm",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_pricebooks_pricebook_id",
                        column: x => x.pricebook_id,
                        principalSchema: "crm",
                        principalTable: "pricebooks",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_orders_quotes_quote_id",
                        column: x => x.quote_id,
                        principalSchema: "crm",
                        principalTable: "quotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_line_items",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricebook_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 4, nullable: false),
                    list_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 4, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(5,2)", precision: 18, scale: 2, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_quote_line_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_quote_line_items_pricebook_entries_pricebook_entry_id",
                        column: x => x.pricebook_entry_id,
                        principalSchema: "crm",
                        principalTable: "pricebook_entries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_quote_line_items_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "crm",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quote_line_items_quotes_quote_id",
                        column: x => x.quote_id,
                        principalSchema: "crm",
                        principalTable: "quotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_line_items",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricebook_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 4, nullable: false),
                    list_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 4, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(5,2)", precision: 18, scale: 2, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: true),
                    quantity_shipped = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    quantity_returned = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_order_line_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_line_items_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "crm",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_line_items_pricebook_entries_pricebook_entry_id",
                        column: x => x.pricebook_entry_id,
                        principalSchema: "crm",
                        principalTable: "pricebook_entries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_order_line_items_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "crm",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounts_parent_account_id",
                schema: "crm",
                table: "accounts",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_activities_account_id",
                schema: "crm",
                table: "activities",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_activities_case_id",
                schema: "crm",
                table: "activities",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_activities_contact_id",
                schema: "crm",
                table: "activities",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_activities_deal_id",
                schema: "crm",
                table: "activities",
                column: "deal_id");

            migrationBuilder.CreateIndex(
                name: "ix_activities_lead_id",
                schema: "crm",
                table: "activities",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_account_id",
                schema: "crm",
                table: "attachments",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_case_id",
                schema: "crm",
                table: "attachments",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_contact_id",
                schema: "crm",
                table: "attachments",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_contract_id",
                schema: "crm",
                table: "attachments",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_deal_id",
                schema: "crm",
                table: "attachments",
                column: "deal_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_email_message_id",
                schema: "crm",
                table: "attachments",
                column: "email_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_knowledge_article_id",
                schema: "crm",
                table: "attachments",
                column: "knowledge_article_id");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_lead_id",
                schema: "crm",
                table: "attachments",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_members_campaign_id",
                schema: "crm",
                table: "campaign_members",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_members_contact_id",
                schema: "crm",
                table: "campaign_members",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_members_lead_id",
                schema: "crm",
                table: "campaign_members",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_parent_campaign_id",
                schema: "crm",
                table: "campaigns",
                column: "parent_campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_case_comments_case_id",
                schema: "crm",
                table: "case_comments",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_cases_account_id",
                schema: "crm",
                table: "cases",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_cases_contact_id",
                schema: "crm",
                table: "cases",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_cases_entitlement_id",
                schema: "crm",
                table: "cases",
                column: "entitlement_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_address_contact",
                schema: "crm",
                table: "contact_addresses",
                columns: new[] { "company_id", "contact_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contact_address_contact_default",
                schema: "crm",
                table: "contact_addresses",
                columns: new[] { "company_id", "contact_id", "is_default" },
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "ix_contact_addresses_contact_id",
                schema: "crm",
                table: "contact_addresses",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_list_members_contact_id",
                schema: "crm",
                table: "contact_list_members",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_list_members_contact_list_id",
                schema: "crm",
                table: "contact_list_members",
                column: "contact_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_list_members_lead_id",
                schema: "crm",
                table: "contact_list_members",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_tenant_customer_number",
                schema: "crm",
                table: "contacts",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "customer_number" },
                unique: true,
                filter: "customer_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_contacts_account_id",
                schema: "crm",
                table: "contacts",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_contacts_converted_from_lead_id",
                schema: "crm",
                table: "contacts",
                column: "converted_from_lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_contacts_reports_to_id",
                schema: "crm",
                table: "contacts",
                column: "reports_to_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_account_id",
                schema: "crm",
                table: "contracts",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_billing_contact_id",
                schema: "crm",
                table: "contracts",
                column: "billing_contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_deal_contacts_contact_id",
                schema: "crm",
                table: "deal_contacts",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_deal_contacts_deal_id",
                schema: "crm",
                table: "deal_contacts",
                column: "deal_id");

            migrationBuilder.CreateIndex(
                name: "ix_deal_products_deal_id",
                schema: "crm",
                table: "deal_products",
                column: "deal_id");

            migrationBuilder.CreateIndex(
                name: "ix_deal_products_pricebook_entry_id",
                schema: "crm",
                table: "deal_products",
                column: "pricebook_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_deal_products_product_id",
                schema: "crm",
                table: "deal_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_deals_account_id",
                schema: "crm",
                table: "deals",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_deals_contact_id",
                schema: "crm",
                table: "deals",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_deals_pipeline_id",
                schema: "crm",
                table: "deals",
                column: "pipeline_id");

            migrationBuilder.CreateIndex(
                name: "ix_deals_pipeline_stage_id",
                schema: "crm",
                table: "deals",
                column: "pipeline_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_entitlements_account_id",
                schema: "crm",
                table: "entitlements",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_entitlements_contact_id",
                schema: "crm",
                table: "entitlements",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_entity_tags_tag_id_entity_id_entity_type",
                schema: "crm",
                table: "entity_tags",
                columns: new[] { "tag_id", "entity_id", "entity_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leads_converted_account_id",
                schema: "crm",
                table: "leads",
                column: "converted_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_converted_contact_id",
                schema: "crm",
                table: "leads",
                column: "converted_contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_converted_deal_id",
                schema: "crm",
                table: "leads",
                column: "converted_deal_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_account_id",
                schema: "crm",
                table: "notes",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_case_id",
                schema: "crm",
                table: "notes",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_contact_id",
                schema: "crm",
                table: "notes",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_deal_id",
                schema: "crm",
                table: "notes",
                column: "deal_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_lead_id",
                schema: "crm",
                table: "notes",
                column: "lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_line_items_order_id",
                schema: "crm",
                table: "order_line_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_line_items_pricebook_entry_id",
                schema: "crm",
                table: "order_line_items",
                column: "pricebook_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_line_items_product_id",
                schema: "crm",
                table: "order_line_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_account_id",
                schema: "crm",
                table: "orders",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_contract_id",
                schema: "crm",
                table: "orders",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_pricebook_id",
                schema: "crm",
                table: "orders",
                column: "pricebook_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_quote_id",
                schema: "crm",
                table: "orders",
                column: "quote_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_stages_pipeline_id",
                schema: "crm",
                table: "pipeline_stages",
                column: "pipeline_id");

            migrationBuilder.CreateIndex(
                name: "ix_pricebook_entries_pricebook_id",
                schema: "crm",
                table: "pricebook_entries",
                column: "pricebook_id");

            migrationBuilder.CreateIndex(
                name: "ix_pricebook_entries_product_id",
                schema: "crm",
                table: "pricebook_entries",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_line_items_pricebook_entry_id",
                schema: "crm",
                table: "quote_line_items",
                column: "pricebook_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_line_items_product_id",
                schema: "crm",
                table: "quote_line_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_line_items_quote_id",
                schema: "crm",
                table: "quote_line_items",
                column: "quote_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_account_id",
                schema: "crm",
                table: "quotes",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_contact_id",
                schema: "crm",
                table: "quotes",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_deal_id",
                schema: "crm",
                table: "quotes",
                column: "deal_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_pricebook_id",
                schema: "crm",
                table: "quotes",
                column: "pricebook_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_company_id_tag_name",
                schema: "crm",
                table: "tags",
                columns: new[] { "company_id", "tag_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_team_members_team_id",
                schema: "crm",
                table: "team_members",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_territories_parent_territory_id",
                schema: "crm",
                table: "territories",
                column: "parent_territory_id");

            migrationBuilder.CreateIndex(
                name: "ix_territory_accounts_account_id",
                schema: "crm",
                table: "territory_accounts",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_territory_accounts_territory_id_account_id",
                schema: "crm",
                table: "territory_accounts",
                columns: new[] { "territory_id", "account_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_activities_cases_case_id",
                schema: "crm",
                table: "activities",
                column: "case_id",
                principalSchema: "crm",
                principalTable: "cases",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_activities_contacts_contact_id",
                schema: "crm",
                table: "activities",
                column: "contact_id",
                principalSchema: "crm",
                principalTable: "contacts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_activities_deals_deal_id",
                schema: "crm",
                table: "activities",
                column: "deal_id",
                principalSchema: "crm",
                principalTable: "deals",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_activities_leads_lead_id",
                schema: "crm",
                table: "activities",
                column: "lead_id",
                principalSchema: "crm",
                principalTable: "leads",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_attachments_cases_case_id",
                schema: "crm",
                table: "attachments",
                column: "case_id",
                principalSchema: "crm",
                principalTable: "cases",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_attachments_contacts_contact_id",
                schema: "crm",
                table: "attachments",
                column: "contact_id",
                principalSchema: "crm",
                principalTable: "contacts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_attachments_contracts_contract_id",
                schema: "crm",
                table: "attachments",
                column: "contract_id",
                principalSchema: "crm",
                principalTable: "contracts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_attachments_deals_deal_id",
                schema: "crm",
                table: "attachments",
                column: "deal_id",
                principalSchema: "crm",
                principalTable: "deals",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_attachments_leads_lead_id",
                schema: "crm",
                table: "attachments",
                column: "lead_id",
                principalSchema: "crm",
                principalTable: "leads",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_members_contacts_contact_id",
                schema: "crm",
                table: "campaign_members",
                column: "contact_id",
                principalSchema: "crm",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_campaign_members_leads_lead_id",
                schema: "crm",
                table: "campaign_members",
                column: "lead_id",
                principalSchema: "crm",
                principalTable: "leads",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_case_comments_cases_case_id",
                schema: "crm",
                table: "case_comments",
                column: "case_id",
                principalSchema: "crm",
                principalTable: "cases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_cases_contacts_contact_id",
                schema: "crm",
                table: "cases",
                column: "contact_id",
                principalSchema: "crm",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cases_entitlements_entitlement_id",
                schema: "crm",
                table: "cases",
                column: "entitlement_id",
                principalSchema: "crm",
                principalTable: "entitlements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contact_addresses_contacts_contact_id",
                schema: "crm",
                table: "contact_addresses",
                column: "contact_id",
                principalSchema: "crm",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_contact_list_members_contacts_contact_id",
                schema: "crm",
                table: "contact_list_members",
                column: "contact_id",
                principalSchema: "crm",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contact_list_members_leads_lead_id",
                schema: "crm",
                table: "contact_list_members",
                column: "lead_id",
                principalSchema: "crm",
                principalTable: "leads",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_contacts_leads_converted_from_lead_id",
                schema: "crm",
                table: "contacts",
                column: "converted_from_lead_id",
                principalSchema: "crm",
                principalTable: "leads",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contacts_accounts_account_id",
                schema: "crm",
                table: "contacts");

            migrationBuilder.DropForeignKey(
                name: "fk_deals_accounts_account_id",
                schema: "crm",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "fk_leads_accounts_converted_account_id",
                schema: "crm",
                table: "leads");

            migrationBuilder.DropForeignKey(
                name: "fk_deals_contacts_contact_id",
                schema: "crm",
                table: "deals");

            migrationBuilder.DropForeignKey(
                name: "fk_leads_contacts_converted_contact_id",
                schema: "crm",
                table: "leads");

            migrationBuilder.DropTable(
                name: "activities",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "campaign_members",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "case_comments",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "contact_addresses",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "contact_list_members",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "deal_contacts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "deal_products",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "entity_tags",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "forecasts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "order_line_items",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "quote_line_items",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "team_members",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "territory_accounts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "email_messages",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "knowledge_articles",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "campaigns",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "contact_lists",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "cases",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "pricebook_entries",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "territories",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "entitlements",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "quotes",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "products",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "pricebooks",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "contacts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "leads",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "deals",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "pipeline_stages",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "pipelines",
                schema: "crm");
        }
    }
}
