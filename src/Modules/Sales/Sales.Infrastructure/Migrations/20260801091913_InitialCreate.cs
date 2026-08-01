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
                name: "app_notifications",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    action_payload = table.Column<string>(type: "text", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_sent = table.Column<bool>(type: "boolean", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    channel = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_app_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approval_policies",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("pk_approval_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "commission_rules",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    sales_rep_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_territory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_channel = table.Column<int>(type: "integer", nullable: true),
                    min_order_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    basis = table.Column<int>(type: "integer", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    max_commission_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recognition_event = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("pk_commission_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "coupons",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    max_discount_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_usage_count = table.Column<int>(type: "integer", nullable: true),
                    usage_count = table.Column<int>(type: "integer", nullable: false),
                    max_usage_per_customer = table.Column<int>(type: "integer", nullable: false),
                    min_order_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    applicable_channel = table.Column<string>(type: "text", nullable: true),
                    target_customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    restricted_to_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    restricted_to_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_free_delivery = table.Column<bool>(type: "boolean", nullable: false),
                    is_first_order_only = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_coupons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    symbol = table.Column<string>(type: "text", nullable: false),
                    decimal_places = table.Column<int>(type: "integer", nullable: false),
                    is_base_currency = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_groups",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    allow_credit_sales = table.Column<bool>(type: "boolean", nullable: false),
                    default_credit_limit = table.Column<decimal>(type: "numeric", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_customer_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_zones",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_code = table.Column<string>(type: "text", nullable: false),
                    zone_name = table.Column<string>(type: "text", nullable: false),
                    geo_polygon = table.Column<string>(type: "text", nullable: true),
                    center_latitude = table.Column<double>(type: "double precision", nullable: true),
                    center_longitude = table.Column<double>(type: "double precision", nullable: true),
                    radius_km = table.Column<double>(type: "double precision", nullable: true),
                    delivery_fee = table.Column<decimal>(type: "numeric", nullable: false),
                    free_delivery_above_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    estimated_delivery_minutes = table.Column<int>(type: "integer", nullable: true),
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
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_schemes",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    min_order_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    min_quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    is_cumulative = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_discount_schemes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_sequences",
                schema: "sales",
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
                    include_day = table.Column<bool>(type: "boolean", nullable: false),
                    sequence_padding = table.Column<int>(type: "integer", nullable: false),
                    reset_on = table.Column<int>(type: "integer", nullable: false),
                    next_sequence_number = table.Column<int>(type: "integer", nullable: false),
                    last_reset_year = table.Column<int>(type: "integer", nullable: true),
                    last_reset_month = table.Column<int>(type: "integer", nullable: true),
                    last_reset_day = table.Column<int>(type: "integer", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "loyalty_accounts",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    points_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    lifetime_points_earned = table.Column<decimal>(type: "numeric", nullable: false),
                    lifetime_points_redeemed = table.Column<decimal>(type: "numeric", nullable: false),
                    lifetime_points_expired = table.Column<decimal>(type: "numeric", nullable: false),
                    tier_spend_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tier_expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_loyalty_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_programs",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_name = table.Column<string>(type: "text", nullable: false),
                    points_per_currency_unit = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    min_order_amount_to_earn = table.Column<decimal>(type: "numeric", nullable: true),
                    point_value_in_currency = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    min_points_to_redeem = table.Column<decimal>(type: "numeric", nullable: false),
                    max_redemption_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    points_expiry_days = table.Column<int>(type: "integer", nullable: true),
                    bronze_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    silver_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    gold_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    platinum_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    diamond_threshold = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("pk_loyalty_programs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_barcode_label_templates",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_name = table.Column<string>(type: "text", nullable: false),
                    label_width_mm = table.Column<double>(type: "double precision", nullable: false),
                    label_height_mm = table.Column<double>(type: "double precision", nullable: false),
                    barcode_symbology = table.Column<string>(type: "text", nullable: false),
                    header_text = table.Column<string>(type: "text", nullable: true),
                    show_product_name = table.Column<bool>(type: "boolean", nullable: false),
                    show_price = table.Column<bool>(type: "boolean", nullable: false),
                    show_sku = table.Column<bool>(type: "boolean", nullable: false),
                    show_barcode_value = table.Column<bool>(type: "boolean", nullable: false),
                    currency_symbol = table.Column<string>(type: "text", nullable: true),
                    product_name_font_pt = table.Column<double>(type: "double precision", nullable: false),
                    price_font_pt = table.Column<double>(type: "double precision", nullable: false),
                    barcode_height_pt = table.Column<double>(type: "double precision", nullable: false),
                    show_borders = table.Column<bool>(type: "boolean", nullable: false),
                    roll_paper = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_pos_barcode_label_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_cash_drawers",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    drawer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    drawer_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_pos_cash_drawers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_receipt_templates",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_name = table.Column<string>(type: "text", nullable: false),
                    header_business_name = table.Column<string>(type: "text", nullable: true),
                    header_address_line1 = table.Column<string>(type: "text", nullable: true),
                    header_address_line2 = table.Column<string>(type: "text", nullable: true),
                    header_phone = table.Column<string>(type: "text", nullable: true),
                    header_email = table.Column<string>(type: "text", nullable: true),
                    header_website = table.Column<string>(type: "text", nullable: true),
                    tax_registration_number = table.Column<string>(type: "text", nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    header_message = table.Column<string>(type: "text", nullable: true),
                    footer_message = table.Column<string>(type: "text", nullable: true),
                    return_policy = table.Column<string>(type: "text", nullable: true),
                    show_barcode = table.Column<bool>(type: "boolean", nullable: false),
                    show_qr_code = table.Column<bool>(type: "boolean", nullable: false),
                    show_cashier_name = table.Column<bool>(type: "boolean", nullable: false),
                    show_customer_name = table.Column<bool>(type: "boolean", nullable: false),
                    show_discount_line = table.Column<bool>(type: "boolean", nullable: false),
                    show_tax_breakdown = table.Column<bool>(type: "boolean", nullable: false),
                    show_loyalty_points = table.Column<bool>(type: "boolean", nullable: false),
                    show_savings_amount = table.Column<bool>(type: "boolean", nullable: false),
                    paper_size = table.Column<string>(type: "text", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    print_copies = table.Column<int>(type: "integer", nullable: false),
                    auto_cut_paper = table.Column<bool>(type: "boolean", nullable: false),
                    open_cash_drawer = table.Column<bool>(type: "boolean", nullable: false),
                    barcode_symbology = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_pos_receipt_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_settings",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    require_cashier_pin = table.Column<bool>(type: "boolean", nullable: false),
                    auto_lock_minutes = table.Column<int>(type: "integer", nullable: false),
                    allow_price_override = table.Column<bool>(type: "boolean", nullable: false),
                    allow_discount = table.Column<bool>(type: "boolean", nullable: false),
                    max_discount_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    require_customer = table.Column<bool>(type: "boolean", nullable: false),
                    default_customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allow_negative_stock = table.Column<bool>(type: "boolean", nullable: false),
                    tax_inclusive_pricing = table.Column<bool>(type: "boolean", nullable: false),
                    auto_apply_tax = table.Column<bool>(type: "boolean", nullable: false),
                    default_currency_code = table.Column<string>(type: "text", nullable: true),
                    rounding_value = table.Column<decimal>(type: "numeric", nullable: false),
                    rounding_mode = table.Column<string>(type: "text", nullable: false),
                    accept_cash = table.Column<bool>(type: "boolean", nullable: false),
                    accept_card = table.Column<bool>(type: "boolean", nullable: false),
                    accept_mobile_payment = table.Column<bool>(type: "boolean", nullable: false),
                    accept_credit_on_account = table.Column<bool>(type: "boolean", nullable: false),
                    allow_split_payment = table.Column<bool>(type: "boolean", nullable: false),
                    allow_partial_payment = table.Column<bool>(type: "boolean", nullable: false),
                    default_payment_method = table.Column<string>(type: "text", nullable: true),
                    auto_open_cash_drawer = table.Column<bool>(type: "boolean", nullable: false),
                    min_order_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    max_order_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    cash_gl_account_number = table.Column<string>(type: "text", nullable: true),
                    bank_gl_account_number = table.Column<string>(type: "text", nullable: true),
                    show_product_images = table.Column<bool>(type: "boolean", nullable: false),
                    show_product_description = table.Column<bool>(type: "boolean", nullable: false),
                    allow_item_notes = table.Column<bool>(type: "boolean", nullable: false),
                    allow_decimal_quantity = table.Column<bool>(type: "boolean", nullable: false),
                    barcode_scan_sound = table.Column<bool>(type: "boolean", nullable: false),
                    show_stock_level = table.Column<bool>(type: "boolean", nullable: false),
                    low_stock_threshold = table.Column<int>(type: "integer", nullable: false),
                    items_per_page = table.Column<int>(type: "integer", nullable: false),
                    show_category_filter = table.Column<bool>(type: "boolean", nullable: false),
                    touch_mode = table.Column<bool>(type: "boolean", nullable: false),
                    operating_mode = table.Column<string>(type: "text", nullable: false),
                    require_opening_float = table.Column<bool>(type: "boolean", nullable: false),
                    require_cash_count_on_close = table.Column<bool>(type: "boolean", nullable: false),
                    auto_close_session = table.Column<bool>(type: "boolean", nullable: false),
                    auto_close_time = table.Column<string>(type: "text", nullable: true),
                    enable_loyalty_points = table.Column<bool>(type: "boolean", nullable: false),
                    enable_promotions = table.Column<bool>(type: "boolean", nullable: false),
                    enable_coupons = table.Column<bool>(type: "boolean", nullable: false),
                    auto_apply_promotions = table.Column<bool>(type: "boolean", nullable: false),
                    default_paper_size = table.Column<string>(type: "text", nullable: false),
                    receipt_copies = table.Column<int>(type: "integer", nullable: false),
                    auto_print_receipt = table.Column<bool>(type: "boolean", nullable: false),
                    ask_to_print_receipt = table.Column<bool>(type: "boolean", nullable: false),
                    skip_receipt_screen = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_logo = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_business_name = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_address = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_contact = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_cashier_name = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_customer_name = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_order_number = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_date_time = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_item_codes = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_unit_price = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_line_discount = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_tax_breakdown = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_discount_line = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_barcode = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_show_qr_code = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_barcode_symbology = table.Column<string>(type: "text", nullable: false),
                    receipt_auto_cut_paper = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_header_note = table.Column<string>(type: "text", nullable: true),
                    receipt_footer_message = table.Column<string>(type: "text", nullable: true),
                    receipt_return_policy = table.Column<string>(type: "text", nullable: true),
                    theme = table.Column<string>(type: "text", nullable: false),
                    primary_color = table.Column<string>(type: "text", nullable: true),
                    default_product_view = table.Column<string>(type: "text", nullable: false),
                    show_numpad = table.Column<bool>(type: "boolean", nullable: false),
                    show_favourites_bar = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_pos_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_lists",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_tax_inclusive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_price_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    promotion_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_auto_applied = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    scheduled_days = table.Column<int>(type: "integer", nullable: false),
                    max_usage_count = table.Column<int>(type: "integer", nullable: true),
                    max_usage_per_customer = table.Column<int>(type: "integer", nullable: true),
                    current_usage_count = table.Column<int>(type: "integer", nullable: false),
                    min_order_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    target_type = table.Column<int>(type: "integer", nullable: false),
                    required_loyalty_tier = table.Column<int>(type: "integer", nullable: true),
                    required_price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_stackable = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_targets",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_rep_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_territory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    period = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    fiscal_quarter = table.Column<int>(type: "integer", nullable: true),
                    fiscal_month = table.Column<int>(type: "integer", nullable: true),
                    target_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    target_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    target_deals_count = table.Column<int>(type: "integer", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false, defaultValue: "USD"),
                    actual_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_deals_count = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_sales_targets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_teams",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    alias = table.Column<string>(type: "text", nullable: true),
                    team_leader_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team_leader_name = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_sales_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_territories",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    region = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_territory_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_sales_territories", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_territories_sales_territories_parent_territory_id",
                        column: x => x.parent_territory_id,
                        principalSchema: "sales",
                        principalTable: "sales_territories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stored_files",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stored_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tax_groups",
                schema: "sales",
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
                    table.PrimaryKey("pk_tax_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_items",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant_name = table.Column<string>(type: "text", nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_wishlist_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approval_policy_conditions",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field = table.Column<int>(type: "integer", nullable: false),
                    @operator = table.Column<int>(name: "operator", type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("pk_approval_policy_conditions", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_policy_conditions_approval_policies_approval_polic",
                        column: x => x.approval_policy_id,
                        principalSchema: "sales",
                        principalTable: "approval_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_policy_steps",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    step_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    approver_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    approver_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    escalation_hours = table.Column<int>(type: "integer", nullable: true),
                    escalation_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    escalation_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_approval_policy_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_policy_steps_approval_policies_approval_policy_id",
                        column: x => x.approval_policy_id,
                        principalSchema: "sales",
                        principalTable: "approval_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "currency_rates",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    base_currency_code = table.Column<string>(type: "text", nullable: false),
                    rate = table.Column<decimal>(type: "numeric", nullable: false),
                    rate_type = table.Column<int>(type: "integer", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    rate_name = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_currency_rates", x => x.id);
                    table.ForeignKey(
                        name: "fk_currency_rates_currencies_currency_id",
                        column: x => x.currency_id,
                        principalSchema: "sales",
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "riders",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rider_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    profile_image_url = table.Column<string>(type: "text", nullable: true),
                    national_id = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hr_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_type = table.Column<string>(type: "text", nullable: false),
                    home_branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_type = table.Column<int>(type: "integer", nullable: false),
                    vehicle_plate_number = table.Column<string>(type: "text", nullable: true),
                    vehicle_model = table.Column<string>(type: "text", nullable: true),
                    vehicle_color = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    current_latitude = table.Column<double>(type: "double precision", nullable: true),
                    current_longitude = table.Column<double>(type: "double precision", nullable: true),
                    last_location_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_deliveries = table.Column<int>(type: "integer", nullable: false),
                    successful_deliveries = table.Column<int>(type: "integer", nullable: false),
                    failed_deliveries = table.Column<int>(type: "integer", nullable: false),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_riders", x => x.id);
                    table.ForeignKey(
                        name: "fk_riders_delivery_zones_zone_id",
                        column: x => x.zone_id,
                        principalSchema: "sales",
                        principalTable: "delivery_zones",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "loyalty_transactions",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loyalty_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    points = table.Column<decimal>(type: "numeric", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric", nullable: false),
                    source_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_channel = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_loyalty_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_loyalty_transactions_loyalty_accounts_loyalty_account_id",
                        column: x => x.loyalty_account_id,
                        principalSchema: "sales",
                        principalTable: "loyalty_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pos_stores",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city_id = table.Column<int>(type: "integer", nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", unicode: false, maxLength: 2, nullable: true),
                    native_language_id = table.Column<Guid>(type: "uuid", nullable: true),
                    store_type = table.Column<int>(type: "integer", nullable: false),
                    store_format = table.Column<int>(type: "integer", nullable: false),
                    default_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receipt_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepts_online_pickup = table.Column<bool>(type: "boolean", nullable: false),
                    has_delivery = table.Column<bool>(type: "boolean", nullable: false),
                    is_online_ordering_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    online_status = table.Column<int>(type: "integer", nullable: false),
                    online_status_note = table.Column<string>(type: "text", nullable: true),
                    estimated_prep_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    min_online_order_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_delivery_radius_km = table.Column<double>(type: "double precision", nullable: true),
                    online_logo_url = table.Column<string>(type: "text", nullable: true),
                    online_banner_url = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    onboarding_status = table.Column<int>(type: "integer", nullable: true),
                    pos_receipt_template_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pos_stores", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_stores_pos_receipt_templates_pos_receipt_template_id",
                        column: x => x.pos_receipt_template_id,
                        principalSchema: "sales",
                        principalTable: "pos_receipt_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_stores_pos_receipt_templates_receipt_template_id",
                        column: x => x.receipt_template_id,
                        principalSchema: "sales",
                        principalTable: "pos_receipt_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_pos_stores_price_lists_default_price_list_id",
                        column: x => x.default_price_list_id,
                        principalSchema: "sales",
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "price_list_items",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    min_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    max_quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_price_list_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_list_items_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalSchema: "sales",
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quotation_name = table.Column<string>(type: "text", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    quotation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    incoterm = table.Column<int>(type: "integer", nullable: true),
                    incoterm_location = table.Column<string>(type: "text", nullable: true),
                    requested_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ship_to_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shipping_method = table.Column<string>(type: "text", nullable: true),
                    subtotal_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    shipping_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sales_rep_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_territory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crm_deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    converted_to_sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_po_number = table.Column<string>(type: "text", nullable: true),
                    customer_po_date = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_quotations", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotations_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalSchema: "sales",
                        principalTable: "price_lists",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sales_agreements",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    agreement_name = table.Column<string>(type: "text", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    committed_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    released_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    remaining_amount = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("pk_sales_agreements", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_agreements_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalSchema: "sales",
                        principalTable: "price_lists",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "promotion_items",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    price_target = table.Column<int>(type: "integer", nullable: false),
                    is_conditional = table.Column<bool>(type: "boolean", nullable: false),
                    condition_type = table.Column<int>(type: "integer", nullable: false),
                    condition_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    condition_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    max_discounted_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    buy_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    get_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    free_item_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_promotion_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_items_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "sales",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_team_members",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_sales_team_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_team_members_sales_teams_sales_team_id",
                        column: x => x.sales_team_id,
                        principalSchema: "sales",
                        principalTable: "sales_teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_rep_territories",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_territory_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_rep_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_sales_rep_territories", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_rep_territories_sales_territories_sales_territory_id",
                        column: x => x.sales_territory_id,
                        principalSchema: "sales",
                        principalTable: "sales_territories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tax_group_rates",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    snapshot_rate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    snapshot_tax_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    snapshot_inclusion_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    is_compound = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_tax_group_rates", x => x.id);
                    table.ForeignKey(
                        name: "fk_tax_group_rates_tax_groups_tax_group_id",
                        column: x => x.tax_group_id,
                        principalSchema: "sales",
                        principalTable: "tax_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tax_rules",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    product_tax_category = table.Column<int>(type: "integer", nullable: true),
                    customer_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    customer_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sales_channel = table.Column<int>(type: "integer", nullable: true),
                    tax_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_tax_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_tax_rules_tax_groups_tax_group_id",
                        column: x => x.tax_group_id,
                        principalSchema: "sales",
                        principalTable: "tax_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rider_shifts",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shift_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    start_latitude = table.Column<double>(type: "double precision", nullable: true),
                    start_longitude = table.Column<double>(type: "double precision", nullable: true),
                    end_latitude = table.Column<double>(type: "double precision", nullable: true),
                    end_longitude = table.Column<double>(type: "double precision", nullable: true),
                    deliveries_completed = table.Column<int>(type: "integer", nullable: false),
                    total_distance_km = table.Column<double>(type: "double precision", nullable: true),
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
                    table.PrimaryKey("pk_rider_shifts", x => x.id);
                    table.ForeignKey(
                        name: "fk_rider_shifts_riders_rider_id",
                        column: x => x.rider_id,
                        principalSchema: "sales",
                        principalTable: "riders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delivery_zone_pos_store",
                schema: "sales",
                columns: table => new
                {
                    delivery_zones_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stores_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_zone_pos_store", x => new { x.delivery_zones_id, x.stores_id });
                    table.ForeignKey(
                        name: "fk_delivery_zone_pos_store_delivery_zones_delivery_zones_id",
                        column: x => x.delivery_zones_id,
                        principalSchema: "sales",
                        principalTable: "delivery_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_delivery_zone_pos_store_pos_stores_stores_id",
                        column: x => x.stores_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pos_cashiers",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pin_code = table.Column<string>(type: "text", nullable: true),
                    badge_number = table.Column<string>(type: "text", nullable: true),
                    pos_store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    can_apply_manual_discount = table.Column<bool>(type: "boolean", nullable: false),
                    max_manual_discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    can_void_transaction = table.Column<bool>(type: "boolean", nullable: false),
                    can_issue_refund = table.Column<bool>(type: "boolean", nullable: false),
                    can_open_drawer = table.Column<bool>(type: "boolean", nullable: false),
                    can_override_prices = table.Column<bool>(type: "boolean", nullable: false),
                    can_apply_coupons = table.Column<bool>(type: "boolean", nullable: false),
                    can_access_reports = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_pos_cashiers", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_cashiers_pos_stores_pos_store_id",
                        column: x => x.pos_store_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pos_store_holidays",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_partially_open = table.Column<bool>(type: "boolean", nullable: false),
                    opening_time = table.Column<string>(type: "text", nullable: true),
                    closing_time = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_pos_store_holidays", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_store_holidays_pos_stores_pos_store_id",
                        column: x => x.pos_store_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pos_store_schedules",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    opening_time = table.Column<string>(type: "text", nullable: false),
                    closing_time = table.Column<string>(type: "text", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_pos_store_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_store_schedules_pos_stores_pos_store_id",
                        column: x => x.pos_store_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pos_terminals",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    terminal_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    terminal_name = table.Column<string>(type: "text", nullable: false),
                    pos_store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_identifier = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    cash_drawer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receipt_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    current_session_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_pos_terminals", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_terminals_pos_cash_drawers_cash_drawer_id",
                        column: x => x.cash_drawer_id,
                        principalSchema: "sales",
                        principalTable: "pos_cash_drawers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_terminals_pos_receipt_templates_receipt_template_id",
                        column: x => x.receipt_template_id,
                        principalSchema: "sales",
                        principalTable: "pos_receipt_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_terminals_pos_stores_pos_store_id",
                        column: x => x.pos_store_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "store_menus",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    available_from = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    available_to = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    pos_store_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_store_menus", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_menus_pos_stores_pos_store_id",
                        column: x => x.pos_store_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "store_offers",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_type = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    banner_url = table.Column<string>(type: "text", nullable: true),
                    badge_text = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    badge_color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    call_to_action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    deep_link_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_category_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_offers_pos_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_store_offers_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "sales",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "store_vendor_profiles",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner_cnic = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    owner_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    owner_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cnic_front_doc_url = table.Column<string>(type: "text", nullable: true),
                    cnic_back_doc_url = table.Column<string>(type: "text", nullable: true),
                    business_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    business_registration_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    food_license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    food_license_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    food_license_doc_url = table.Column<string>(type: "text", nullable: true),
                    business_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    bank_branch = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    account_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    iban_number = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    facebook_url = table.Column<string>(type: "text", nullable: true),
                    instagram_url = table.Column<string>(type: "text", nullable: true),
                    tiktok_url = table.Column<string>(type: "text", nullable: true),
                    whatsapp_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    store_front_photo_url = table.Column<string>(type: "text", nullable: true),
                    kitchen_photos_json = table.Column<string>(type: "text", nullable: true),
                    onboarding_status = table.Column<int>(type: "integer", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_store_vendor_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_vendor_profiles_pos_stores_store_id",
                        column: x => x.store_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_approvals",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_level = table.Column<int>(type: "integer", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    decision_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_quotation_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotation_approvals_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalSchema: "sales",
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    product_description = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    line_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_category = table.Column<int>(type: "integer", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    requested_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_quotation_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotation_lines_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalSchema: "sales",
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_agreement_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: false),
                    committed_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    released_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    remaining_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    agreed_unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("pk_sales_agreement_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_agreement_lines_sales_agreements_sales_agreement_id",
                        column: x => x.sales_agreement_id,
                        principalSchema: "sales",
                        principalTable: "sales_agreements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pos_sessions",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pos_terminal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_cashier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opening_float = table.Column<decimal>(type: "numeric", nullable: false),
                    opening_notes = table.Column<string>(type: "text", nullable: true),
                    opening_denominations_json = table.Column<string>(type: "text", nullable: true),
                    closing_float = table.Column<decimal>(type: "numeric", nullable: false),
                    expected_closing_float = table.Column<decimal>(type: "numeric", nullable: false),
                    float_variance = table.Column<decimal>(type: "numeric", nullable: false),
                    closing_denominations_json = table.Column<string>(type: "text", nullable: true),
                    total_sales_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_refunds_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_discounts_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    net_sales_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    cash_collected = table.Column<decimal>(type: "numeric", nullable: false),
                    card_collected = table.Column<decimal>(type: "numeric", nullable: false),
                    wallet_collected = table.Column<decimal>(type: "numeric", nullable: false),
                    other_collected = table.Column<decimal>(type: "numeric", nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false),
                    closing_notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_pos_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_sessions_pos_cashiers_pos_cashier_id",
                        column: x => x.pos_cashier_id,
                        principalSchema: "sales",
                        principalTable: "pos_cashiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pos_sessions_pos_terminals_pos_terminal_id",
                        column: x => x.pos_terminal_id,
                        principalSchema: "sales",
                        principalTable: "pos_terminals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "store_menu_sections",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_store_menu_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_menu_sections_store_menus_store_menu_id",
                        column: x => x.store_menu_id,
                        principalSchema: "sales",
                        principalTable: "store_menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pos_cash_movements",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_cashier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_pos_cash_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_cash_movements_pos_cashiers_pos_cashier_id",
                        column: x => x.pos_cashier_id,
                        principalSchema: "sales",
                        principalTable: "pos_cashiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pos_cash_movements_pos_sessions_pos_session_id",
                        column: x => x.pos_session_id,
                        principalSchema: "sales",
                        principalTable: "pos_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_requests",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_policy_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    decision = table.Column<int>(type: "integer", nullable: false),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_escalated = table.Column<bool>(type: "boolean", nullable: false),
                    escalated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    escalated_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_by = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                        name: "fk_approval_requests_approval_policies_approval_policy_id",
                        column: x => x.approval_policy_id,
                        principalSchema: "sales",
                        principalTable: "approval_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approval_requests_approval_policy_steps_approval_policy_ste",
                        column: x => x.approval_policy_step_id,
                        principalSchema: "sales",
                        principalTable: "approval_policy_steps",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "commission_entries",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commission_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_rep_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    status = table.Column<int>(type: "integer", nullable: false),
                    earned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payroll_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_commission_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_commission_entries_commission_rules_commission_rule_id",
                        column: x => x.commission_rule_id,
                        principalSchema: "sales",
                        principalTable: "commission_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "coupon_usages",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pos_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_applied = table.Column<decimal>(type: "numeric", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_coupon_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_coupon_usages_coupons_coupon_id",
                        column: x => x.coupon_id,
                        principalSchema: "sales",
                        principalTable: "coupons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credit_note_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    line_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_category = table.Column<int>(type: "integer", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_credit_note_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credit_notes",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_note_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    credit_note_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    accounting_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_credit_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deliveries",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    planned_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actual_ship_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    carrier = table.Column<string>(type: "text", nullable: true),
                    shipping_method = table.Column<string>(type: "text", nullable: true),
                    tracking_number = table.Column<string>(type: "text", nullable: true),
                    incoterm = table.Column<int>(type: "integer", nullable: true),
                    incoterm_location = table.Column<string>(type: "text", nullable: true),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_name = table.Column<string>(type: "text", nullable: true),
                    recipient_phone = table.Column<string>(type: "text", nullable: true),
                    recipient_whats_app = table.Column<string>(type: "text", nullable: true),
                    street = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<string>(type: "text", nullable: true),
                    postal_code = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    delivery_latitude = table.Column<double>(type: "double precision", nullable: true),
                    delivery_longitude = table.Column<double>(type: "double precision", nullable: true),
                    delivery_notes = table.Column<string>(type: "text", nullable: true),
                    assigned_rider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rider_name = table.Column<string>(type: "text", nullable: true),
                    number_of_packages = table.Column<int>(type: "integer", nullable: true),
                    total_weight = table.Column<decimal>(type: "numeric", nullable: true),
                    weight_unit = table.Column<string>(type: "text", nullable: true),
                    total_volume = table.Column<decimal>(type: "numeric", nullable: true),
                    volume_unit = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_deliveries_riders_assigned_rider_id",
                        column: x => x.assigned_rider_id,
                        principalSchema: "sales",
                        principalTable: "riders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "delivery_packages",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_number = table.Column<string>(type: "text", nullable: false),
                    package_type = table.Column<string>(type: "text", nullable: true),
                    weight = table.Column<decimal>(type: "numeric", nullable: true),
                    weight_unit = table.Column<string>(type: "text", nullable: true),
                    length = table.Column<decimal>(type: "numeric", nullable: true),
                    width = table.Column<decimal>(type: "numeric", nullable: true),
                    height = table.Column<decimal>(type: "numeric", nullable: true),
                    dimension_unit = table.Column<string>(type: "text", nullable: true),
                    tracking_number = table.Column<string>(type: "text", nullable: true),
                    seal_number = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_delivery_packages", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_packages_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "sales",
                        principalTable: "deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delivery_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivered_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    bin_location = table.Column<string>(type: "text", nullable: true),
                    lot_number = table.Column<string>(type: "text", nullable: true),
                    serial_number = table.Column<string>(type: "text", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_delivery_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_lines_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "sales",
                        principalTable: "deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allocated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_payment_allocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_cash_drawer_events",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_cash_drawer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cashier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    open_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_pos_cash_drawer_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_cash_drawer_events_pos_cash_drawers_pos_cash_drawer_id",
                        column: x => x.pos_cash_drawer_id,
                        principalSchema: "sales",
                        principalTable: "pos_cash_drawers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_cash_drawer_events_pos_cashiers_cashier_id",
                        column: x => x.cashier_id,
                        principalSchema: "sales",
                        principalTable: "pos_cashiers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_cash_drawer_events_pos_sessions_pos_session_id",
                        column: x => x.pos_session_id,
                        principalSchema: "sales",
                        principalTable: "pos_sessions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pos_gift_card_transactions",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_gift_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    pos_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_pos_gift_card_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_gift_cards",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pin_hash = table.Column<string>(type: "text", nullable: true),
                    original_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    current_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    status = table.Column<int>(type: "integer", nullable: false),
                    issued_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issued_in_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_name = table.Column<string>(type: "text", nullable: true),
                    recipient_email = table.Column<string>(type: "text", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_pos_gift_cards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_payments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tender_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    reference_number = table.Column<string>(type: "text", nullable: true),
                    authorization_code = table.Column<string>(type: "text", nullable: true),
                    card_scheme = table.Column<string>(type: "text", nullable: true),
                    card_last4 = table.Column<string>(type: "text", nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_pos_payments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_transaction_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    barcode = table.Column<string>(type: "text", nullable: true),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant_name = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    net_unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    line_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_category = table.Column<int>(type: "integer", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    is_refunded = table.Column<bool>(type: "boolean", nullable: false),
                    refunded_quantity = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("pk_pos_transaction_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pos_transactions",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    receipt_number = table.Column<string>(type: "text", nullable: true),
                    pos_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_terminal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_cashier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    rounding_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tendered_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    change_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    coupon_code = table.Column<string>(type: "text", nullable: true),
                    coupon_discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    loyalty_points_earned = table.Column<decimal>(type: "numeric", nullable: false),
                    loyalty_points_redeemed = table.Column<decimal>(type: "numeric", nullable: false),
                    original_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_pos_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_pos_transactions_pos_cashiers_pos_cashier_id",
                        column: x => x.pos_cashier_id,
                        principalSchema: "sales",
                        principalTable: "pos_cashiers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_transactions_pos_sessions_pos_session_id",
                        column: x => x.pos_session_id,
                        principalSchema: "sales",
                        principalTable: "pos_sessions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_transactions_pos_stores_pos_store_id",
                        column: x => x.pos_store_id,
                        principalSchema: "sales",
                        principalTable: "pos_stores",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_transactions_pos_terminals_pos_terminal_id",
                        column: x => x.pos_terminal_id,
                        principalSchema: "sales",
                        principalTable: "pos_terminals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pos_transactions_pos_transactions_original_transaction_id",
                        column: x => x.original_transaction_id,
                        principalSchema: "sales",
                        principalTable: "pos_transactions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sales_orders",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    offline_order_number = table.Column<string>(type: "text", nullable: true),
                    order_name = table.Column<string>(type: "text", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    customer_po_number = table.Column<string>(type: "text", nullable: true),
                    customer_po_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    placed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    requested_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_agreement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_channel = table.Column<int>(type: "integer", nullable: false),
                    origin_branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin_pos_terminal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin_pos_cashier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin_pos_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    closing_pos_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    coupon_code = table.Column<string>(type: "text", nullable: true),
                    coupon_discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    loyalty_points_redeemed = table.Column<decimal>(type: "numeric", nullable: false),
                    loyalty_points_earned = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    incoterm = table.Column<int>(type: "integer", nullable: true),
                    incoterm_location = table.Column<string>(type: "text", nullable: true),
                    invoice_status = table.Column<int>(type: "integer", nullable: false),
                    invoice_policy = table.Column<int>(type: "integer", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    fulfillment_type = table.Column<int>(type: "integer", nullable: false),
                    bill_to_name = table.Column<string>(type: "text", nullable: true),
                    bill_to_street = table.Column<string>(type: "text", nullable: true),
                    bill_to_city = table.Column<string>(type: "text", nullable: true),
                    bill_to_state = table.Column<string>(type: "text", nullable: true),
                    bill_to_postal_code = table.Column<string>(type: "text", nullable: true),
                    bill_to_country = table.Column<string>(type: "text", nullable: true),
                    ship_to_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subtotal_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tip_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_check_passed = table.Column<bool>(type: "boolean", nullable: false),
                    credit_check_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sales_rep_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_territory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
                    terms_and_conditions = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_sales_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_orders_pos_cashiers_origin_pos_cashier_id",
                        column: x => x.origin_pos_cashier_id,
                        principalSchema: "sales",
                        principalTable: "pos_cashiers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_orders_pos_sessions_origin_pos_session_id",
                        column: x => x.origin_pos_session_id,
                        principalSchema: "sales",
                        principalTable: "pos_sessions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_orders_pos_terminals_origin_pos_terminal_id",
                        column: x => x.origin_pos_terminal_id,
                        principalSchema: "sales",
                        principalTable: "pos_terminals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_orders_pos_transactions_closing_pos_transaction_id",
                        column: x => x.closing_pos_transaction_id,
                        principalSchema: "sales",
                        principalTable: "pos_transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_orders_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalSchema: "sales",
                        principalTable: "price_lists",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_orders_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalSchema: "sales",
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_sales_orders_sales_agreements_sales_agreement_id",
                        column: x => x.sales_agreement_id,
                        principalSchema: "sales",
                        principalTable: "sales_agreements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_sales_orders_sales_territories_sales_territory_id",
                        column: x => x.sales_territory_id,
                        principalSchema: "sales",
                        principalTable: "sales_territories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_reviews",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    is_verified_purchase = table.Column<bool>(type: "boolean", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    review_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_product_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_reviews_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "rider_assignments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    arrived_at_store_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    picked_up_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    out_for_delivery_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pickup_branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pickup_latitude = table.Column<double>(type: "double precision", nullable: true),
                    pickup_longitude = table.Column<double>(type: "double precision", nullable: true),
                    delivery_latitude = table.Column<double>(type: "double precision", nullable: true),
                    delivery_longitude = table.Column<double>(type: "double precision", nullable: true),
                    estimated_distance_km = table.Column<double>(type: "double precision", nullable: true),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    actual_distance_km = table.Column<double>(type: "double precision", nullable: true),
                    actual_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    proof_image_url = table.Column<string>(type: "text", nullable: true),
                    recipient_name = table.Column<string>(type: "text", nullable: true),
                    recipient_signature_url = table.Column<string>(type: "text", nullable: true),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    is_reassigned = table.Column<bool>(type: "boolean", nullable: false),
                    reassigned_to_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_rider_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_rider_assignments_riders_rider_id",
                        column: x => x.rider_id,
                        principalSchema: "sales",
                        principalTable: "riders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rider_assignments_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "rider_ratings",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    rated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_rider_ratings", x => x.id);
                    table.ForeignKey(
                        name: "fk_rider_ratings_riders_rider_id",
                        column: x => x.rider_id,
                        principalSchema: "sales",
                        principalTable: "riders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rider_ratings_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoices",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    invoice_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    shipping_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_due = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bill_to_name = table.Column<string>(type: "text", nullable: true),
                    bill_to_street = table.Column<string>(type: "text", nullable: true),
                    bill_to_city = table.Column<string>(type: "text", nullable: true),
                    bill_to_state = table.Column<string>(type: "text", nullable: true),
                    bill_to_postal_code = table.Column<string>(type: "text", nullable: true),
                    bill_to_country = table.Column<string>(type: "text", nullable: true),
                    payment_terms = table.Column<int>(type: "integer", nullable: false),
                    sales_rep_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_rep_name = table.Column<string>(type: "text", nullable: true),
                    payment_reference = table.Column<string>(type: "text", nullable: true),
                    recipient_bank_account = table.Column<string>(type: "text", nullable: true),
                    delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fiscal_position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_method = table.Column<string>(type: "text", nullable: true),
                    auto_post = table.Column<int>(type: "integer", nullable: false),
                    last_reminder_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reminder_count = table.Column<int>(type: "integer", nullable: false),
                    accounting_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_status = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    customer_reference = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_sales_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_invoices_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "sales",
                        principalTable: "deliveries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_invoices_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_approvals",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_level = table.Column<int>(type: "integer", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    decision_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_sales_order_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_order_approvals_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_attachments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    content_type = table.Column<string>(type: "text", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_sales_order_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_order_attachments_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    product_description = table.Column<string>(type: "text", nullable: true),
                    product_image_url = table.Column<string>(type: "text", nullable: true),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant_name = table.Column<string>(type: "text", nullable: true),
                    ordered_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    delivered_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    invoiced_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    cancelled_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    line_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_category = table.Column<int>(type: "integer", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cost_price = table.Column<decimal>(type: "numeric", nullable: true),
                    margin_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    margin_percentage = table.Column<decimal>(type: "numeric", nullable: true),
                    requested_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    line_status = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_sales_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_order_lines_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_status_histories",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
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
                    table.PrimaryKey("pk_sales_order_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_order_status_histories_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_payments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    exchange_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_number = table.Column<string>(type: "text", nullable: true),
                    bank_name = table.Column<string>(type: "text", nullable: true),
                    gateway_transaction_id = table.Column<string>(type: "text", nullable: true),
                    gateway_response = table.Column<string>(type: "text", nullable: true),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_refunded = table.Column<bool>(type: "boolean", nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    accounting_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_sales_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_payments_pos_transactions_pos_transaction_id",
                        column: x => x.pos_transaction_id,
                        principalSchema: "sales",
                        principalTable: "pos_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_sales_payments_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_review_images",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
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
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_review_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_review_images_product_reviews_product_review_id",
                        column: x => x.product_review_id,
                        principalSchema: "sales",
                        principalTable: "product_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rider_location_logs",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rider_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    speed_kmh = table.Column<double>(type: "double precision", nullable: true),
                    bearing = table.Column<double>(type: "double precision", nullable: true),
                    logged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_rider_location_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_rider_location_logs_rider_assignments_rider_assignment_id",
                        column: x => x.rider_assignment_id,
                        principalSchema: "sales",
                        principalTable: "rider_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rider_location_logs_riders_rider_id",
                        column: x => x.rider_id,
                        principalSchema: "sales",
                        principalTable: "riders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_returns",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    request_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    credit_issued_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rma_number = table.Column<string>(type: "text", nullable: true),
                    return_reason = table.Column<string>(type: "text", nullable: true),
                    inspection_notes = table.Column<string>(type: "text", nullable: true),
                    total_refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    credit_note_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_sales_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_returns_credit_notes_credit_note_id",
                        column: x => x.credit_note_id,
                        principalSchema: "sales",
                        principalTable: "credit_notes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_returns_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalSchema: "sales",
                        principalTable: "sales_invoices",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_returns_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sales_invoice_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    line_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_category = table.Column<int>(type: "integer", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_down_payment = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_sales_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_invoice_lines_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalSchema: "sales",
                        principalTable: "sales_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sales_invoice_lines_sales_order_lines_sales_order_line_id",
                        column: x => x.sales_order_line_id,
                        principalSchema: "sales",
                        principalTable: "sales_order_lines",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sales_order_line_addons",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    addon_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    addon_name = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("pk_sales_order_line_addons", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_order_line_addons_sales_order_lines_sales_order_line_",
                        column: x => x.sales_order_line_id,
                        principalSchema: "sales",
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_return_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_return_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "text", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    returned_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_of_measure = table.Column<string>(type: "text", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    condition_on_return = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_sales_return_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_return_lines_sales_order_lines_sales_order_line_id",
                        column: x => x.sales_order_line_id,
                        principalSchema: "sales",
                        principalTable: "sales_order_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_return_lines_sales_returns_sales_return_id",
                        column: x => x.sales_return_id,
                        principalSchema: "sales",
                        principalTable: "sales_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_approval_policy_conditions_approval_policy_id",
                schema: "sales",
                table: "approval_policy_conditions",
                column: "approval_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_policy_steps_approval_policy_id",
                schema: "sales",
                table: "approval_policy_steps",
                column: "approval_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_request_order_step",
                schema: "sales",
                table: "approval_requests",
                columns: new[] { "sales_order_id", "step_order" });

            migrationBuilder.CreateIndex(
                name: "ix_approval_requests_approval_policy_id",
                schema: "sales",
                table: "approval_requests",
                column: "approval_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_requests_approval_policy_step_id",
                schema: "sales",
                table: "approval_requests",
                column: "approval_policy_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_commission_entries_commission_rule_id",
                schema: "sales",
                table: "commission_entries",
                column: "commission_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_commission_entries_sales_order_id",
                schema: "sales",
                table: "commission_entries",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_commission_entry_rep_status_date",
                schema: "sales",
                table: "commission_entries",
                columns: new[] { "sales_rep_id", "status", "earned_date" });

            migrationBuilder.CreateIndex(
                name: "ix_coupon_usages_coupon_id",
                schema: "sales",
                table: "coupon_usages",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_usages_pos_transaction_id",
                schema: "sales",
                table: "coupon_usages",
                column: "pos_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_usages_sales_order_id",
                schema: "sales",
                table: "coupon_usages",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_tenant_code",
                schema: "sales",
                table: "coupons",
                columns: new[] { "company_id", "branch_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credit_note_lines_credit_note_id",
                schema: "sales",
                table: "credit_note_lines",
                column: "credit_note_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_notes_sales_invoice_id",
                schema: "sales",
                table: "credit_notes",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_currency_rates_currency_id",
                schema: "sales",
                table: "currency_rates",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_assigned_rider_id",
                schema: "sales",
                table: "deliveries",
                column: "assigned_rider_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_sales_order_id",
                schema: "sales",
                table: "deliveries",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_tenant_number",
                schema: "sales",
                table: "deliveries",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "delivery_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_lines_delivery_id",
                schema: "sales",
                table: "delivery_lines",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_lines_sales_order_line_id",
                schema: "sales",
                table: "delivery_lines",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_packages_delivery_id",
                schema: "sales",
                table: "delivery_packages",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_zone_pos_store_stores_id",
                schema: "sales",
                table: "delivery_zone_pos_store",
                column: "stores_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_sequence_tenant_type",
                schema: "sales",
                table: "document_sequences",
                columns: new[] { "company_id", "branch_id", "document_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_transactions_loyalty_account_id",
                schema: "sales",
                table: "loyalty_transactions",
                column: "loyalty_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocation_invoice",
                schema: "sales",
                table: "payment_allocations",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocation_payment_invoice",
                schema: "sales",
                table: "payment_allocations",
                columns: new[] { "sales_payment_id", "sales_invoice_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pos_cash_drawer_event_drawer_date",
                schema: "sales",
                table: "pos_cash_drawer_events",
                columns: new[] { "pos_cash_drawer_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pos_cash_drawer_events_cashier_id",
                schema: "sales",
                table: "pos_cash_drawer_events",
                column: "cashier_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_cash_drawer_events_pos_session_id",
                schema: "sales",
                table: "pos_cash_drawer_events",
                column: "pos_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_cash_drawer_events_pos_transaction_id",
                schema: "sales",
                table: "pos_cash_drawer_events",
                column: "pos_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_cash_movements_pos_cashier_id",
                schema: "sales",
                table: "pos_cash_movements",
                column: "pos_cashier_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_cash_movements_pos_session_id",
                schema: "sales",
                table: "pos_cash_movements",
                column: "pos_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_cashiers_pos_store_id",
                schema: "sales",
                table: "pos_cashiers",
                column: "pos_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_gift_card_transactions_pos_gift_card_id",
                schema: "sales",
                table: "pos_gift_card_transactions",
                column: "pos_gift_card_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_gift_card_transactions_pos_transaction_id",
                schema: "sales",
                table: "pos_gift_card_transactions",
                column: "pos_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_gift_card_tenant_number",
                schema: "sales",
                table: "pos_gift_cards",
                columns: new[] { "company_id", "branch_id", "card_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pos_gift_cards_issued_in_transaction_id",
                schema: "sales",
                table: "pos_gift_cards",
                column: "issued_in_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_payments_pos_transaction_id",
                schema: "sales",
                table: "pos_payments",
                column: "pos_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_sessions_pos_cashier_id",
                schema: "sales",
                table: "pos_sessions",
                column: "pos_cashier_id");

            migrationBuilder.CreateIndex(
                name: "ux_pos_sessions_open_per_terminal",
                schema: "sales",
                table: "pos_sessions",
                column: "pos_terminal_id",
                unique: true,
                filter: "status = 0 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_pos_store_holidays_pos_store_id",
                schema: "sales",
                table: "pos_store_holidays",
                column: "pos_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_store_schedules_pos_store_id",
                schema: "sales",
                table: "pos_store_schedules",
                column: "pos_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_store_company_trading_name",
                schema: "sales",
                table: "pos_stores",
                columns: new[] { "company_id", "trading_name" },
                unique: true,
                filter: "is_deleted = false AND trading_name IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pos_stores_default_price_list_id",
                schema: "sales",
                table: "pos_stores",
                column: "default_price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_stores_pos_receipt_template_id",
                schema: "sales",
                table: "pos_stores",
                column: "pos_receipt_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_stores_receipt_template_id",
                schema: "sales",
                table: "pos_stores",
                column: "receipt_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_cash_drawer_id",
                schema: "sales",
                table: "pos_terminals",
                column: "cash_drawer_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_pos_store_id",
                schema: "sales",
                table: "pos_terminals",
                column: "pos_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_receipt_template_id",
                schema: "sales",
                table: "pos_terminals",
                column: "receipt_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_transaction_lines_pos_transaction_id",
                schema: "sales",
                table: "pos_transaction_lines",
                column: "pos_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_transaction_tenant_number",
                schema: "sales",
                table: "pos_transactions",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "transaction_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pos_transactions_original_transaction_id",
                schema: "sales",
                table: "pos_transactions",
                column: "original_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_transactions_pos_cashier_id",
                schema: "sales",
                table: "pos_transactions",
                column: "pos_cashier_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_transactions_pos_session_id",
                schema: "sales",
                table: "pos_transactions",
                column: "pos_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_transactions_pos_store_id",
                schema: "sales",
                table: "pos_transactions",
                column: "pos_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_transactions_pos_terminal_id",
                schema: "sales",
                table: "pos_transactions",
                column: "pos_terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_transactions_sales_order_id",
                schema: "sales",
                table: "pos_transactions",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_list_items_price_list_id",
                schema: "sales",
                table: "price_list_items",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_list_tenant_code",
                schema: "sales",
                table: "price_lists",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_review_images_product_review_id",
                schema: "sales",
                table: "product_review_images",
                column: "product_review_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_sales_order_id",
                schema: "sales",
                table: "product_reviews",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_items_promotion_id",
                schema: "sales",
                table: "promotion_items",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_tenant_code",
                schema: "sales",
                table: "promotions",
                columns: new[] { "company_id", "branch_id", "promotion_code" },
                unique: true,
                filter: "promotion_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_approvals_quotation_id",
                schema: "sales",
                table: "quotation_approvals",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_lines_quotation_id",
                schema: "sales",
                table: "quotation_lines",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_tenant_number",
                schema: "sales",
                table: "quotations",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "quotation_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotations_price_list_id",
                schema: "sales",
                table: "quotations",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_rider_assignments_rider_id",
                schema: "sales",
                table: "rider_assignments",
                column: "rider_id");

            migrationBuilder.CreateIndex(
                name: "ix_rider_assignments_sales_order_id",
                schema: "sales",
                table: "rider_assignments",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_rider_location_logs_rider_assignment_id",
                schema: "sales",
                table: "rider_location_logs",
                column: "rider_assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_rider_location_logs_rider_id",
                schema: "sales",
                table: "rider_location_logs",
                column: "rider_id");

            migrationBuilder.CreateIndex(
                name: "ix_rider_ratings_rider_id",
                schema: "sales",
                table: "rider_ratings",
                column: "rider_id");

            migrationBuilder.CreateIndex(
                name: "ix_rider_ratings_sales_order_id",
                schema: "sales",
                table: "rider_ratings",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_rider_shifts_rider_id",
                schema: "sales",
                table: "rider_shifts",
                column: "rider_id");

            migrationBuilder.CreateIndex(
                name: "ix_riders_zone_id",
                schema: "sales",
                table: "riders",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_agreement_lines_sales_agreement_id",
                schema: "sales",
                table: "sales_agreement_lines",
                column: "sales_agreement_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_agreements_price_list_id",
                schema: "sales",
                table: "sales_agreements",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoice_lines_sales_invoice_id",
                schema: "sales",
                table: "sales_invoice_lines",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoice_lines_sales_order_line_id",
                schema: "sales",
                table: "sales_invoice_lines",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoice_tenant_number",
                schema: "sales",
                table: "sales_invoices",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_delivery_id",
                schema: "sales",
                table: "sales_invoices",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_sales_order_id",
                schema: "sales",
                table: "sales_invoices",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_approvals_sales_order_id",
                schema: "sales",
                table: "sales_order_approvals",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_attachments_sales_order_id",
                schema: "sales",
                table: "sales_order_attachments",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_line_addons_sales_order_line_id",
                schema: "sales",
                table: "sales_order_line_addons",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_lines_sales_order_id",
                schema: "sales",
                table: "sales_order_lines",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_status_history_order_date",
                schema: "sales",
                table: "sales_order_status_histories",
                columns: new[] { "sales_order_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_branch_status_placed_at",
                schema: "sales",
                table: "sales_orders",
                columns: new[] { "origin_branch_id", "status", "placed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_contact_status",
                schema: "sales",
                table: "sales_orders",
                columns: new[] { "contact_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_offline_order_number",
                schema: "sales",
                table: "sales_orders",
                column: "offline_order_number");

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_tenant_number",
                schema: "sales",
                table: "sales_orders",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_closing_pos_transaction_id",
                schema: "sales",
                table: "sales_orders",
                column: "closing_pos_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_origin_pos_cashier_id",
                schema: "sales",
                table: "sales_orders",
                column: "origin_pos_cashier_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_origin_pos_session_id",
                schema: "sales",
                table: "sales_orders",
                column: "origin_pos_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_origin_pos_terminal_id",
                schema: "sales",
                table: "sales_orders",
                column: "origin_pos_terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_price_list_id",
                schema: "sales",
                table: "sales_orders",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_quotation_id",
                schema: "sales",
                table: "sales_orders",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_sales_agreement_id",
                schema: "sales",
                table: "sales_orders",
                column: "sales_agreement_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_sales_territory_id",
                schema: "sales",
                table: "sales_orders",
                column: "sales_territory_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_payment_tenant_number",
                schema: "sales",
                table: "sales_payments",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "payment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_payments_pos_transaction_id",
                schema: "sales",
                table: "sales_payments",
                column: "pos_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_payments_sales_order_id",
                schema: "sales",
                table: "sales_payments",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_rep_territories_sales_territory_id",
                schema: "sales",
                table: "sales_rep_territories",
                column: "sales_territory_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_lines_sales_order_line_id",
                schema: "sales",
                table: "sales_return_lines",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_lines_sales_return_id",
                schema: "sales",
                table: "sales_return_lines",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_tenant_number",
                schema: "sales",
                table: "sales_returns",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "return_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_credit_note_id",
                schema: "sales",
                table: "sales_returns",
                column: "credit_note_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_sales_invoice_id",
                schema: "sales",
                table: "sales_returns",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_sales_order_id",
                schema: "sales",
                table: "sales_returns",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_target_rep_period",
                schema: "sales",
                table: "sales_targets",
                columns: new[] { "sales_rep_id", "period", "period_start" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_target_team_fiscal_year",
                schema: "sales",
                table: "sales_targets",
                columns: new[] { "sales_team_id", "fiscal_year" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_target_territory_fiscal_year",
                schema: "sales",
                table: "sales_targets",
                columns: new[] { "sales_territory_id", "fiscal_year" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_team_members_sales_team_id",
                schema: "sales",
                table: "sales_team_members",
                column: "sales_team_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_territories_parent_territory_id",
                schema: "sales",
                table: "sales_territories",
                column: "parent_territory_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_menu_section_menu_category",
                schema: "sales",
                table: "store_menu_sections",
                columns: new[] { "store_menu_id", "item_category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_store_menus_pos_store_id",
                schema: "sales",
                table: "store_menus",
                column: "pos_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_offer_store_active_order",
                schema: "sales",
                table: "store_offers",
                columns: new[] { "store_id", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_store_offers_promotion_id",
                schema: "sales",
                table: "store_offers",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_vendor_profile_store_id",
                schema: "sales",
                table: "store_vendor_profiles",
                column: "store_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stored_file_tenant",
                schema: "sales",
                table: "stored_files",
                columns: new[] { "company_id", "branch_id", "business_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tax_group_rate_group_tax_def",
                schema: "sales",
                table: "tax_group_rates",
                columns: new[] { "tax_group_id", "tax_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tax_group_tenant_code",
                schema: "sales",
                table: "tax_groups",
                columns: new[] { "company_id", "branch_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tax_rule_tenant_priority",
                schema: "sales",
                table: "tax_rules",
                columns: new[] { "company_id", "branch_id", "priority", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_tax_group_id",
                schema: "sales",
                table: "tax_rules",
                column: "tax_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_approval_requests_sales_orders_sales_order_id",
                schema: "sales",
                table: "approval_requests",
                column: "sales_order_id",
                principalSchema: "sales",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_commission_entries_sales_orders_sales_order_id",
                schema: "sales",
                table: "commission_entries",
                column: "sales_order_id",
                principalSchema: "sales",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_coupon_usages_pos_transactions_pos_transaction_id",
                schema: "sales",
                table: "coupon_usages",
                column: "pos_transaction_id",
                principalSchema: "sales",
                principalTable: "pos_transactions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_coupon_usages_sales_orders_sales_order_id",
                schema: "sales",
                table: "coupon_usages",
                column: "sales_order_id",
                principalSchema: "sales",
                principalTable: "sales_orders",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_credit_note_lines_credit_notes_credit_note_id",
                schema: "sales",
                table: "credit_note_lines",
                column: "credit_note_id",
                principalSchema: "sales",
                principalTable: "credit_notes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_credit_notes_sales_invoices_sales_invoice_id",
                schema: "sales",
                table: "credit_notes",
                column: "sales_invoice_id",
                principalSchema: "sales",
                principalTable: "sales_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_deliveries_sales_orders_sales_order_id",
                schema: "sales",
                table: "deliveries",
                column: "sales_order_id",
                principalSchema: "sales",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_delivery_lines_sales_order_lines_sales_order_line_id",
                schema: "sales",
                table: "delivery_lines",
                column: "sales_order_line_id",
                principalSchema: "sales",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_allocations_sales_invoices_sales_invoice_id",
                schema: "sales",
                table: "payment_allocations",
                column: "sales_invoice_id",
                principalSchema: "sales",
                principalTable: "sales_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_allocations_sales_payments_sales_payment_id",
                schema: "sales",
                table: "payment_allocations",
                column: "sales_payment_id",
                principalSchema: "sales",
                principalTable: "sales_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_cash_drawer_events_pos_transactions_pos_transaction_id",
                schema: "sales",
                table: "pos_cash_drawer_events",
                column: "pos_transaction_id",
                principalSchema: "sales",
                principalTable: "pos_transactions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_pos_gift_card_transactions_pos_gift_cards_pos_gift_card_id",
                schema: "sales",
                table: "pos_gift_card_transactions",
                column: "pos_gift_card_id",
                principalSchema: "sales",
                principalTable: "pos_gift_cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_gift_card_transactions_pos_transactions_pos_transaction_",
                schema: "sales",
                table: "pos_gift_card_transactions",
                column: "pos_transaction_id",
                principalSchema: "sales",
                principalTable: "pos_transactions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_pos_gift_cards_pos_transactions_issued_in_transaction_id",
                schema: "sales",
                table: "pos_gift_cards",
                column: "issued_in_transaction_id",
                principalSchema: "sales",
                principalTable: "pos_transactions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_pos_payments_pos_transactions_pos_transaction_id",
                schema: "sales",
                table: "pos_payments",
                column: "pos_transaction_id",
                principalSchema: "sales",
                principalTable: "pos_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_transaction_lines_pos_transactions_pos_transaction_id",
                schema: "sales",
                table: "pos_transaction_lines",
                column: "pos_transaction_id",
                principalSchema: "sales",
                principalTable: "pos_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_transactions_sales_orders_sales_order_id",
                schema: "sales",
                table: "pos_transactions",
                column: "sales_order_id",
                principalSchema: "sales",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pos_transactions_sales_orders_sales_order_id",
                schema: "sales",
                table: "pos_transactions");

            migrationBuilder.DropTable(
                name: "app_notifications",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "approval_policy_conditions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "approval_requests",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "commission_entries",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "coupon_usages",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "credit_note_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "currency_rates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "customer_groups",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "delivery_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "delivery_packages",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "delivery_zone_pos_store",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "discount_schemes",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "document_sequences",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "loyalty_programs",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "loyalty_transactions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "payment_allocations",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_barcode_label_templates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_cash_drawer_events",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_cash_movements",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_gift_card_transactions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_payments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_settings",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_store_holidays",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_store_schedules",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_transaction_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "price_list_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "product_review_images",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "promotion_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "quotation_approvals",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "quotation_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "rider_location_logs",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "rider_ratings",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "rider_shifts",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_agreement_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_invoice_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_order_approvals",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_order_attachments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_order_line_addons",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_order_status_histories",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_rep_territories",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_return_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_targets",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_team_members",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "store_menu_sections",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "store_offers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "store_vendor_profiles",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "stored_files",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "tax_group_rates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "tax_rules",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "wishlist_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "approval_policy_steps",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "commission_rules",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "coupons",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "currencies",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "loyalty_accounts",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_payments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_gift_cards",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "product_reviews",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "rider_assignments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_order_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_returns",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_teams",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "store_menus",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "promotions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "tax_groups",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "approval_policies",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "credit_notes",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_invoices",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "deliveries",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "riders",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "delivery_zones",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_orders",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_transactions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "quotations",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_agreements",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_territories",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_sessions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_cashiers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_terminals",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_cash_drawers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_stores",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "pos_receipt_templates",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "price_lists",
                schema: "sales");
        }
    }
}
