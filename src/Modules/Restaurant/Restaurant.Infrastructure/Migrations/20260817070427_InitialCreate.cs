using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "restaurant");

            migrationBuilder.CreateTable(
                name: "combos",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    standard_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_combos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_reasons",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    max_amount_without_approval = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_comp = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_discount_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feedback",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    check_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waiter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    table_id = table.Column<Guid>(type: "uuid", nullable: true),
                    overall_rating = table.Column<int>(type: "integer", nullable: false),
                    food_rating = table.Column<int>(type: "integer", nullable: true),
                    service_rating = table.Column<int>(type: "integer", nullable: true),
                    ambience_rating = table.Column<int>(type: "integer", nullable: true),
                    value_rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    guest_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    resolution_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resolved_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_feedback", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guests",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    birthday = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    anniversary = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dietary_preferences = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    allergies = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    favourite_items = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    preferred_seating = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    visit_count = table.Column<int>(type: "integer", nullable: false),
                    lifetime_spend = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    average_check = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    first_visit_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_visit_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    no_show_count = table.Column<int>(type: "integer", nullable: false),
                    is_vip = table.Column<bool>(type: "boolean", nullable: false),
                    is_blacklisted = table.Column<bool>(type: "boolean", nullable: false),
                    blacklist_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    loyalty_points = table.Column<int>(type: "integer", nullable: false),
                    loyalty_tier = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_guests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "happy_hour_rules",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    active_days = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discount_kind = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applicable_order_types = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
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
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_happy_hour_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_availabilities",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_automatic = table.Column<bool>(type: "boolean", nullable: false),
                    available_again_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    marked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    marked_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_menu_item_availabilities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menus",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    daypart = table.Column<int>(type: "integer", nullable: false),
                    available_from = table.Column<TimeSpan>(type: "interval", nullable: true),
                    available_to = table.Column<TimeSpan>(type: "interval", nullable: true),
                    active_days = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
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
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modifier_groups",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    prompt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    selection_mode = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    min_selections = table.Column<int>(type: "integer", nullable: false),
                    max_selections = table.Column<int>(type: "integer", nullable: false),
                    free_selections = table.Column<int>(type: "integer", nullable: false),
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
                    code = table.Column<string>(type: "text", nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modifier_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outlets",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    service_style = table.Column<int>(type: "integer", nullable: false),
                    cuisine_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    time_zone_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pos_store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_menu_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_tax_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_charge_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    takeaway_tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    seating_capacity = table.Column<int>(type: "integer", nullable: false),
                    average_dining_minutes = table.Column<int>(type: "integer", nullable: false),
                    accepts_reservations = table.Column<bool>(type: "boolean", nullable: false),
                    accepts_delivery = table.Column<bool>(type: "boolean", nullable: false),
                    accepts_takeaway = table.Column<bool>(type: "boolean", nullable: false),
                    has_drive_thru = table.Column<bool>(type: "boolean", nullable: false),
                    qr_ordering_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_temporarily_closed = table.Column<bool>(type: "boolean", nullable: false),
                    closure_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    receipt_footer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_outlets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "printer_profiles",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    target = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    paper_width_mm = table.Column<int>(type: "integer", nullable: false),
                    is_receipt_printer = table.Column<bool>(type: "boolean", nullable: false),
                    is_kitchen_printer = table.Column<bool>(type: "boolean", nullable: false),
                    is_label_printer = table.Column<bool>(type: "boolean", nullable: false),
                    opens_cash_drawer = table.Column<bool>(type: "boolean", nullable: false),
                    copies_per_ticket = table.Column<int>(type: "integer", nullable: false),
                    header_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    footer_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_printer_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_charge_rules",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    basis = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    min_party_size = table.Column<int>(type: "integer", nullable: false),
                    applicable_order_types = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    is_taxable = table.Column<bool>(type: "boolean", nullable: false),
                    tax_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_waivable = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approval_to_waive = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_service_charge_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    cashier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cashier_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    terminal_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opening_float = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    expected_cash = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    expected_card = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    expected_other = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    counted_cash = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    counted_card = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    counted_other = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cash_variance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_blind_close = table.Column<bool>(type: "boolean", nullable: false),
                    total_sales = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_discounts = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_voids = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_refunds = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_tips = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_tax = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_service_charge = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    order_count = table.Column<int>(type: "integer", nullable: false),
                    cover_count = table.Column<int>(type: "integer", nullable: false),
                    check_count = table.Column<int>(type: "integer", nullable: false),
                    x_read_count = table.Column<int>(type: "integer", nullable: false),
                    z_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closing_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    require_waiter_pin = table.Column<bool>(type: "boolean", nullable: false),
                    require_guest_count_on_seat = table.Column<bool>(type: "boolean", nullable: false),
                    require_seat_numbers = table.Column<bool>(type: "boolean", nullable: false),
                    auto_fire_on_send = table.Column<bool>(type: "boolean", nullable: false),
                    seated_attention_minutes = table.Column<int>(type: "integer", nullable: false),
                    served_attention_minutes = table.Column<int>(type: "integer", nullable: false),
                    allow_table_merge = table.Column<bool>(type: "boolean", nullable: false),
                    allow_table_transfer = table.Column<bool>(type: "boolean", nullable: false),
                    allow_split_bill = table.Column<bool>(type: "boolean", nullable: false),
                    prices_include_tax = table.Column<bool>(type: "boolean", nullable: false),
                    tips_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tip_preset_percents = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    tip_pooling_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tip_distribution_basis = table.Column<int>(type: "integer", nullable: false),
                    kitchen_tip_share_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    cash_rounding_increment = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    packaging_charge_per_order = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    void_requires_reason = table.Column<bool>(type: "boolean", nullable: false),
                    discount_requires_reason = table.Column<bool>(type: "boolean", nullable: false),
                    discount_approval_threshold = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    kitchen_display_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    print_kitchen_tickets = table.Column<bool>(type: "boolean", nullable: false),
                    expo_screen_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    kds_warning_minutes = table.Column<int>(type: "integer", nullable: false),
                    auto_bump_on_serve = table.Column<bool>(type: "boolean", nullable: false),
                    show_allergen_warnings = table.Column<bool>(type: "boolean", nullable: false),
                    deplete_stock_on_check_close = table.Column<bool>(type: "boolean", nullable: false),
                    auto86on_zero_stock = table.Column<bool>(type: "boolean", nullable: false),
                    track_wastage = table.Column<bool>(type: "boolean", nullable: false),
                    reservations_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    waitlist_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    reservation_hold_minutes = table.Column<int>(type: "integer", nullable: false),
                    default_reservation_duration = table.Column<int>(type: "integer", nullable: false),
                    require_deposit_for_large_party = table.Column<bool>(type: "boolean", nullable: false),
                    large_party_threshold = table.Column<int>(type: "integer", nullable: false),
                    print_receipt_automatically = table.Column<bool>(type: "boolean", nullable: false),
                    email_receipt_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_header = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    receipt_footer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    show_calories = table.Column<bool>(type: "boolean", nullable: false),
                    feedback_prompt_enabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    role = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pin_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    failed_pin_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    can_take_orders = table.Column<bool>(type: "boolean", nullable: false),
                    can_void_lines = table.Column<bool>(type: "boolean", nullable: false),
                    can_apply_discounts = table.Column<bool>(type: "boolean", nullable: false),
                    can_approve_discounts = table.Column<bool>(type: "boolean", nullable: false),
                    can_open_cash_drawer = table.Column<bool>(type: "boolean", nullable: false),
                    can_close_session = table.Column<bool>(type: "boolean", nullable: false),
                    can_run_reports = table.Column<bool>(type: "boolean", nullable: false),
                    can_edit_menu = table.Column<bool>(type: "boolean", nullable: false),
                    can_manage_tables = table.Column<bool>(type: "boolean", nullable: false),
                    can_serve_alcohol = table.Column<bool>(type: "boolean", nullable: false),
                    default_section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hourly_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    tip_share_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    hired_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code_int = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_clock_entries",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    clocked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    clocked_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_break = table.Column<bool>(type: "boolean", nullable: false),
                    hours = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_adjusted = table.Column<bool>(type: "boolean", nullable: false),
                    adjusted_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_time_clock_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tip_pools",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    basis = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    distributed_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    kitchen_share_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    is_finalised = table.Column<bool>(type: "boolean", nullable: false),
                    finalised_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finalised_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_tip_pools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tips",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waiter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waiter_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tender_type = table.Column<int>(type: "integer", nullable: false),
                    is_declared = table.Column<bool>(type: "boolean", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tip_pool_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_tips", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "void_reasons",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    counts_as_wastage = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_void_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "waitlist",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    quoted_wait_minutes = table.Column<int>(type: "integer", nullable: false),
                    notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    seated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    table_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    preferred_section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pager_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_waitlist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wastage_logs",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_stock_adjusted = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_wastage_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "combo_components",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combo_meal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    min_choices = table.Column<int>(type: "integer", nullable: false),
                    max_choices = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_combo_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_combo_components_combos_combo_meal_id",
                        column: x => x.combo_meal_id,
                        principalSchema: "restaurant",
                        principalTable: "combos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_categories",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    color_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    icon_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_station_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_menu_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_categories_menus_menu_id",
                        column: x => x.menu_id,
                        principalSchema: "restaurant",
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "modifiers",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    price_delta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_delta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumption_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    consumption_uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_removal = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_modifiers", x => x.id);
                    table.ForeignKey(
                        name: "fk_modifiers_modifier_groups_modifier_group_id",
                        column: x => x.modifier_group_id,
                        principalSchema: "restaurant",
                        principalTable: "modifier_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "floors",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    canvas_width = table.Column<int>(type: "integer", nullable: false),
                    canvas_height = table.Column<int>(type: "integer", nullable: false),
                    background_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_floors", x => x.id);
                    table.ForeignKey(
                        name: "fk_floors_outlets_outlet_id",
                        column: x => x.outlet_id,
                        principalSchema: "restaurant",
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outlet_schedules",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    override_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opens_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    closes_at = table.Column<TimeSpan>(type: "interval", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("pk_outlet_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_outlet_schedules_outlets_outlet_id",
                        column: x => x.outlet_id,
                        principalSchema: "restaurant",
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stations",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    station_type = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    color_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    is_expo = table.Column<bool>(type: "boolean", nullable: false),
                    sla_minutes = table.Column<int>(type: "integer", nullable: false),
                    max_concurrent_tickets = table.Column<int>(type: "integer", nullable: false),
                    prints_tickets = table.Column<bool>(type: "boolean", nullable: false),
                    printer_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_stations", x => x.id);
                    table.ForeignKey(
                        name: "fk_stations_outlets_outlet_id",
                        column: x => x.outlet_id,
                        principalSchema: "restaurant",
                        principalTable: "outlets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cash_movements",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_cash_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_movements_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "restaurant",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_shifts",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    scheduled_start = table.Column<TimeSpan>(type: "interval", nullable: false),
                    scheduled_end = table.Column<TimeSpan>(type: "interval", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    actual_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    break_minutes = table.Column<int>(type: "integer", nullable: false),
                    hours_worked = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<int>(type: "integer", nullable: false),
                    sales_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    orders_handled = table.Column<int>(type: "integer", nullable: false),
                    covers_served = table.Column<int>(type: "integer", nullable: false),
                    tips_earned = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_staff_shifts", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_shifts_staff_staff_id",
                        column: x => x.staff_id,
                        principalSchema: "restaurant",
                        principalTable: "staff",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tip_distributions",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tip_pool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staff_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    role = table.Column<int>(type: "integer", nullable: false),
                    hours_worked = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    sales_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    share_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_paid_out = table.Column<bool>(type: "boolean", nullable: false),
                    paid_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_tip_distributions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tip_distributions_tip_pools_tip_pool_id",
                        column: x => x.tip_pool_id,
                        principalSchema: "restaurant",
                        principalTable: "tip_pools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "combo_component_options",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combo_component_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    upcharge_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_combo_component_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_combo_component_options_combo_components_combo_component_id",
                        column: x => x.combo_component_id,
                        principalSchema: "restaurant",
                        principalTable: "combo_components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_items",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    short_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    base_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    standard_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_course = table.Column<int>(type: "integer", nullable: false),
                    prep_time_minutes = table.Column<int>(type: "integer", nullable: false),
                    is_vegetarian = table.Column<bool>(type: "boolean", nullable: false),
                    is_vegan = table.Column<bool>(type: "boolean", nullable: false),
                    is_halal = table.Column<bool>(type: "boolean", nullable: false),
                    is_gluten_free = table.Column<bool>(type: "boolean", nullable: false),
                    contains_nuts = table.Column<bool>(type: "boolean", nullable: false),
                    contains_dairy = table.Column<bool>(type: "boolean", nullable: false),
                    contains_shellfish = table.Column<bool>(type: "boolean", nullable: false),
                    spice_level = table.Column<int>(type: "integer", nullable: false),
                    calories = table.Column<int>(type: "integer", nullable: true),
                    allergens = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_alcohol = table.Column<bool>(type: "boolean", nullable: false),
                    is_sold_by_weight = table.Column<bool>(type: "boolean", nullable: false),
                    is_open_price = table.Column<bool>(type: "boolean", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    is_combo = table.Column<bool>(type: "boolean", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    kitchen_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_menu_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_items_menu_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "restaurant",
                        principalTable: "menu_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fixtures",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    floor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    position_x = table.Column<int>(type: "integer", nullable: false),
                    position_y = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    rotation = table.Column<int>(type: "integer", nullable: false),
                    color_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
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
                    table.PrimaryKey("pk_fixtures", x => x.id);
                    table.ForeignKey(
                        name: "fk_fixtures_floors_floor_id",
                        column: x => x.floor_id,
                        principalSchema: "restaurant",
                        principalTable: "floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sections",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    floor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    color_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    is_smoking = table.Column<bool>(type: "boolean", nullable: false),
                    is_outdoor = table.Column<bool>(type: "boolean", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_spend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
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
                    table.PrimaryKey("pk_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_sections_floors_floor_id",
                        column: x => x.floor_id,
                        principalSchema: "restaurant",
                        principalTable: "floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "routing_rules",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_type = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_type = table.Column<int>(type: "integer", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_additional = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_routing_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_routing_rules_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "restaurant",
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_modifier_groups",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_required_override = table.Column<bool>(type: "boolean", nullable: true),
                    min_selections_override = table.Column<int>(type: "integer", nullable: true),
                    max_selections_override = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_menu_item_modifier_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_item_modifier_groups_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalSchema: "restaurant",
                        principalTable: "menu_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_menu_item_modifier_groups_modifier_groups_modifier_group_id",
                        column: x => x.modifier_group_id,
                        principalSchema: "restaurant",
                        principalTable: "modifier_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_prices",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_menu_item_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_item_prices_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalSchema: "restaurant",
                        principalTable: "menu_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_variants",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    standard_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_menu_item_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_menu_item_variants_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalSchema: "restaurant",
                        principalTable: "menu_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    yield_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    yield_uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_sub_recipe = table.Column<bool>(type: "boolean", nullable: false),
                    output_inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    prep_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    cook_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    plating_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_recipes", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipes_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalSchema: "restaurant",
                        principalTable: "menu_items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tables",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    floor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    table_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    shape = table.Column<int>(type: "integer", nullable: false),
                    seats = table.Column<int>(type: "integer", nullable: false),
                    min_party_size = table.Column<int>(type: "integer", nullable: true),
                    max_party_size = table.Column<int>(type: "integer", nullable: true),
                    position_x = table.Column<int>(type: "integer", nullable: false),
                    position_y = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    rotation = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    current_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_waiter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_guest_count = table.Column<int>(type: "integer", nullable: false),
                    seated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    state_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    merged_into_table_id = table.Column<Guid>(type: "uuid", nullable: true),
                    qr_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_tables", x => x.id);
                    table.ForeignKey(
                        name: "fk_tables_floors_floor_id",
                        column: x => x.floor_id,
                        principalSchema: "restaurant",
                        principalTable: "floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tables_sections_section_id",
                        column: x => x.section_id,
                        principalSchema: "restaurant",
                        principalTable: "sections",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipe_ingredients",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sub_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ingredient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    yield_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    waste_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    line_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_recipe_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_ingredients_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalSchema: "restaurant",
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    token_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    order_type = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    table_id = table.Column<Guid>(type: "uuid", nullable: true),
                    table_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_count = table.Column<int>(type: "integer", nullable: false),
                    waiter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waiter_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    customer_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sub_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    service_charge_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    packaging_charge_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    delivery_fee_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tip_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    rounding_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    cost_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_fired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    served_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    billed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    promised_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    external_source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    external_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    was_offline = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                        name: "fk_orders_tables_table_id",
                        column: x => x.table_id,
                        principalSchema: "restaurant",
                        principalTable: "tables",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    guest_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    reserved_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    table_id = table.Column<Guid>(type: "uuid", nullable: true),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    floor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occasion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    special_requests = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    allergy_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_high_chair_needed = table.Column<bool>(type: "boolean", nullable: false),
                    is_wheelchair_access = table.Column<bool>(type: "boolean", nullable: false),
                    deposit_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_deposit_paid = table.Column<bool>(type: "boolean", nullable: false),
                    deposit_paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    seated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reminder_sent = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_reservations", x => x.id);
                    table.ForeignKey(
                        name: "fk_reservations_tables_table_id",
                        column: x => x.table_id,
                        principalSchema: "restaurant",
                        principalTable: "tables",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "table_state_logs",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waiter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_state = table.Column<int>(type: "integer", nullable: false),
                    to_state = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    seconds_in_previous_state = table.Column<int>(type: "integer", nullable: true),
                    guest_count = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("pk_table_state_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_table_state_logs_tables_table_id",
                        column: x => x.table_id,
                        principalSchema: "restaurant",
                        principalTable: "tables",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "checks",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    split_method = table.Column<int>(type: "integer", nullable: false),
                    split_index = table.Column<int>(type: "integer", nullable: false),
                    split_count = table.Column<int>(type: "integer", nullable: false),
                    seat_numbers = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sub_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    service_charge_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    packaging_charge_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    delivery_fee_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tip_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    rounding_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    change_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cashier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cashier_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    waiter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    printed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    print_count = table.Column<int>(type: "integer", nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiscal_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    void_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                    void_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    voided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_checks", x => x.id);
                    table.ForeignKey(
                        name: "fk_checks_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "restaurant",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deliveries",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    landmark = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    zone_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    delivery_fee = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    distance_km = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    rider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rider_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    rider_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    picked_up_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estimated_arrival_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    delivery_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                        name: "fk_deliveries_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "restaurant",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kitchen_tickets",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    course = table.Column<int>(type: "integer", nullable: false),
                    order_type = table.Column<int>(type: "integer", nullable: false),
                    table_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    waiter_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    guest_count = table.Column<int>(type: "integer", nullable: false),
                    is_priority = table.Column<bool>(type: "boolean", nullable: false),
                    is_remake = table.Column<bool>(type: "boolean", nullable: false),
                    fired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ready_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bumped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    prep_seconds = table.Column<int>(type: "integer", nullable: true),
                    recall_count = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_kitchen_tickets", x => x.id);
                    table.ForeignKey(
                        name: "fk_kitchen_tickets_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "restaurant",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_kitchen_tickets_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "restaurant",
                        principalTable: "stations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    combo_meal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    variant_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    modifier_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_percent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    seat_number = table.Column<int>(type: "integer", nullable: true),
                    course = table.Column<int>(type: "integer", nullable: false),
                    course_sequence = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_held = table.Column<bool>(type: "boolean", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kitchen_ticket_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ready_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    served_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    void_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                    void_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    voided_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    voided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    was_fired_when_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_comped = table.Column<bool>(type: "boolean", nullable: false),
                    discount_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                    special_instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_lines_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "restaurant",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_status_history",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<int>(type: "integer", nullable: false),
                    to_status = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_order_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_status_history_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "restaurant",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "check_discounts",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    promo_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    applied_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_check_discounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_check_discounts_checks_check_id",
                        column: x => x.check_id,
                        principalSchema: "restaurant",
                        principalTable: "checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "check_lines",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    variant_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modifier_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    seat_number = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_check_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_check_lines_checks_check_id",
                        column: x => x.check_id,
                        principalSchema: "restaurant",
                        principalTable: "checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "check_payments",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tender_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tendered_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    change_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    tip_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    card_last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    card_scheme = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    auth_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    gift_card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loyalty_points_used = table.Column<int>(type: "integer", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_refund = table.Column<bool>(type: "boolean", nullable: false),
                    refund_of_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_check_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_check_payments_checks_check_id",
                        column: x => x.check_id,
                        principalSchema: "restaurant",
                        principalTable: "checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kitchen_ticket_lines",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    variant_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    modifier_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    special_instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    allergen_warning = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    seat_number = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    ready_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_kitchen_ticket_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_kitchen_ticket_lines_kitchen_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalSchema: "restaurant",
                        principalTable: "kitchen_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_line_modifiers",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    group_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    price_delta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_delta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_removal = table.Column<bool>(type: "boolean", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumption_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    consumption_uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("pk_order_line_modifiers", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_line_modifiers_order_lines_order_line_id",
                        column: x => x.order_line_id,
                        principalSchema: "restaurant",
                        principalTable: "order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cash_movements_session_id",
                schema: "restaurant",
                table: "cash_movements",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_discounts_check_id",
                schema: "restaurant",
                table: "check_discounts",
                column: "check_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_lines_check_id",
                schema: "restaurant",
                table: "check_lines",
                column: "check_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_payment_outlet_at",
                schema: "restaurant",
                table: "check_payments",
                columns: new[] { "outlet_id", "paid_at" });

            migrationBuilder.CreateIndex(
                name: "ix_check_payment_session",
                schema: "restaurant",
                table: "check_payments",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_payments_check_id",
                schema: "restaurant",
                table: "check_payments",
                column: "check_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_outlet_status",
                schema: "restaurant",
                table: "checks",
                columns: new[] { "outlet_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_check_session",
                schema: "restaurant",
                table: "checks",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_tenant_number",
                schema: "restaurant",
                table: "checks",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "check_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_checks_order_id",
                schema: "restaurant",
                table: "checks",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_combo_component_options_combo_component_id",
                schema: "restaurant",
                table: "combo_component_options",
                column: "combo_component_id");

            migrationBuilder.CreateIndex(
                name: "ix_combo_components_combo_meal_id",
                schema: "restaurant",
                table: "combo_components",
                column: "combo_meal_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_order_id",
                schema: "restaurant",
                table: "deliveries",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_outlet_status",
                schema: "restaurant",
                table: "deliveries",
                columns: new[] { "outlet_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_outlet_at",
                schema: "restaurant",
                table: "feedback",
                columns: new[] { "outlet_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_floor_id",
                schema: "restaurant",
                table: "fixtures",
                column: "floor_id");

            migrationBuilder.CreateIndex(
                name: "ix_floor_outlet_order",
                schema: "restaurant",
                table: "floors",
                columns: new[] { "outlet_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_company_phone",
                schema: "restaurant",
                table: "guests",
                columns: new[] { "company_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_ticket_lines_ticket_id",
                schema: "restaurant",
                table: "kitchen_ticket_lines",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_ticket_outlet_fired",
                schema: "restaurant",
                table: "kitchen_tickets",
                columns: new[] { "outlet_id", "fired_at" });

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_ticket_station_status",
                schema: "restaurant",
                table: "kitchen_tickets",
                columns: new[] { "outlet_id", "station_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_tickets_order_id",
                schema: "restaurant",
                table: "kitchen_tickets",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_tickets_station_id",
                schema: "restaurant",
                table: "kitchen_tickets",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_category_menu_order",
                schema: "restaurant",
                table: "menu_categories",
                columns: new[] { "menu_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_availability_outlet_item",
                schema: "restaurant",
                table: "menu_item_availabilities",
                columns: new[] { "outlet_id", "menu_item_id", "variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_modifier_group_pair",
                schema: "restaurant",
                table: "menu_item_modifier_groups",
                columns: new[] { "menu_item_id", "modifier_group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_modifier_groups_modifier_group_id",
                schema: "restaurant",
                table: "menu_item_modifier_groups",
                column: "modifier_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_price_item_scope",
                schema: "restaurant",
                table: "menu_item_prices",
                columns: new[] { "menu_item_id", "scope", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_variants_menu_item_id",
                schema: "restaurant",
                table: "menu_item_variants",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_barcode",
                schema: "restaurant",
                table: "menu_items",
                column: "barcode");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_category_order",
                schema: "restaurant",
                table: "menu_items",
                columns: new[] { "category_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_company_code",
                schema: "restaurant",
                table: "menu_items",
                columns: new[] { "company_id", "code" });

            migrationBuilder.CreateIndex(
                name: "ix_modifiers_modifier_group_id",
                schema: "restaurant",
                table: "modifiers",
                column: "modifier_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_line_modifiers_order_line_id",
                schema: "restaurant",
                table: "order_line_modifiers",
                column: "order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_line_menu_item",
                schema: "restaurant",
                table: "order_lines",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_line_order_order",
                schema: "restaurant",
                table: "order_lines",
                columns: new[] { "order_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_order_line_order_status",
                schema: "restaurant",
                table: "order_lines",
                columns: new[] { "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_order_status_history_order_id",
                schema: "restaurant",
                table: "order_status_history",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_idempotency",
                schema: "restaurant",
                table: "orders",
                columns: new[] { "company_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_order_outlet_opened",
                schema: "restaurant",
                table: "orders",
                columns: new[] { "outlet_id", "opened_at" });

            migrationBuilder.CreateIndex(
                name: "ix_order_outlet_status",
                schema: "restaurant",
                table: "orders",
                columns: new[] { "outlet_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_order_table",
                schema: "restaurant",
                table: "orders",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_tenant_number",
                schema: "restaurant",
                table: "orders",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outlet_schedule_outlet_day",
                schema: "restaurant",
                table: "outlet_schedules",
                columns: new[] { "outlet_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_outlet_tenant_code",
                schema: "restaurant",
                table: "outlets",
                columns: new[] { "company_id", "branch_id", "business_unit_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_ingredients_recipe_id",
                schema: "restaurant",
                table: "recipe_ingredients",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_item_variant",
                schema: "restaurant",
                table: "recipes",
                columns: new[] { "menu_item_id", "variant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_reservation_outlet_for",
                schema: "restaurant",
                table: "reservations",
                columns: new[] { "outlet_id", "reserved_for" });

            migrationBuilder.CreateIndex(
                name: "ix_reservation_outlet_status",
                schema: "restaurant",
                table: "reservations",
                columns: new[] { "outlet_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_table_id",
                schema: "restaurant",
                table: "reservations",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "ix_routing_rule_outlet_priority",
                schema: "restaurant",
                table: "routing_rules",
                columns: new[] { "outlet_id", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_routing_rules_station_id",
                schema: "restaurant",
                table: "routing_rules",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_sections_floor_id",
                schema: "restaurant",
                table: "sections",
                column: "floor_id");

            migrationBuilder.CreateIndex(
                name: "ix_session_outlet_status",
                schema: "restaurant",
                table: "sessions",
                columns: new[] { "outlet_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_settings_tenant",
                schema: "restaurant",
                table: "settings",
                columns: new[] { "company_id", "branch_id", "business_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staff_outlet_code",
                schema: "restaurant",
                table: "staff",
                columns: new[] { "outlet_id", "code" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_outlet_role",
                schema: "restaurant",
                table: "staff",
                columns: new[] { "outlet_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_shift_outlet_date",
                schema: "restaurant",
                table: "staff_shifts",
                columns: new[] { "outlet_id", "shift_date" });

            migrationBuilder.CreateIndex(
                name: "ix_staff_shifts_staff_id",
                schema: "restaurant",
                table: "staff_shifts",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_station_outlet_order",
                schema: "restaurant",
                table: "stations",
                columns: new[] { "outlet_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_table_state_log_outlet_at",
                schema: "restaurant",
                table: "table_state_logs",
                columns: new[] { "outlet_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_table_state_logs_table_id",
                schema: "restaurant",
                table: "table_state_logs",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "ix_table_outlet_number",
                schema: "restaurant",
                table: "tables",
                columns: new[] { "outlet_id", "table_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_table_outlet_state",
                schema: "restaurant",
                table: "tables",
                columns: new[] { "outlet_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_table_qr_token",
                schema: "restaurant",
                table: "tables",
                column: "qr_token");

            migrationBuilder.CreateIndex(
                name: "ix_tables_floor_id",
                schema: "restaurant",
                table: "tables",
                column: "floor_id");

            migrationBuilder.CreateIndex(
                name: "ix_tables_section_id",
                schema: "restaurant",
                table: "tables",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_clock_staff_at",
                schema: "restaurant",
                table: "time_clock_entries",
                columns: new[] { "outlet_id", "staff_id", "clocked_in_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tip_distributions_tip_pool_id",
                schema: "restaurant",
                table: "tip_distributions",
                column: "tip_pool_id");

            migrationBuilder.CreateIndex(
                name: "ix_tip_outlet_at",
                schema: "restaurant",
                table: "tips",
                columns: new[] { "outlet_id", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_outlet_status",
                schema: "restaurant",
                table: "waitlist",
                columns: new[] { "outlet_id", "status", "joined_at" });

            migrationBuilder.CreateIndex(
                name: "ix_wastage_outlet_at",
                schema: "restaurant",
                table: "wastage_logs",
                columns: new[] { "outlet_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_movements",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "check_discounts",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "check_lines",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "check_payments",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "combo_component_options",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "deliveries",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "discount_reasons",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "feedback",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "fixtures",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "guests",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "happy_hour_rules",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "kitchen_ticket_lines",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "menu_item_availabilities",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "menu_item_modifier_groups",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "menu_item_prices",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "menu_item_variants",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "modifiers",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "order_line_modifiers",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "order_status_history",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "outlet_schedules",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "printer_profiles",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "recipe_ingredients",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "reservations",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "routing_rules",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "service_charge_rules",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "staff_shifts",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "table_state_logs",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "time_clock_entries",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "tip_distributions",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "tips",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "void_reasons",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "waitlist",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "wastage_logs",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "checks",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "combo_components",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "kitchen_tickets",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "modifier_groups",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "order_lines",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "recipes",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "staff",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "tip_pools",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "combos",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "stations",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "menu_items",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "tables",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "menu_categories",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "sections",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "menus",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "floors",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "outlets",
                schema: "restaurant");
        }
    }
}
