using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodSafetyAndDeliveryZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checklists",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    active_days = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    due_at = table.Column<TimeSpan>(type: "interval", nullable: true),
                    assigned_role = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_checklists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_zones",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    delivery_fee = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    minimum_order_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    radius_km = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                    free_delivery_threshold = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    covered_areas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    color_hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
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
                    table.PrimaryKey("pk_delivery_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prep_batches",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    menu_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prepared_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    use_by_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    prepared_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prepared_by_staff_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    supplier_batch_refs = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_discarded = table.Column<bool>(type: "boolean", nullable: false),
                    discarded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discard_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    storage_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("pk_prep_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "temperature_checkpoints",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    min_safe_celsius = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    max_safe_celsius = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    check_interval_hours = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    station_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_temperature_checkpoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "checklist_items",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    answer_type = table.Column<int>(type: "integer", nullable: false),
                    min_value = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    max_value = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_critical = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    guidance = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklist_items_checklists_checklist_id",
                        column: x => x.checklist_id,
                        principalSchema: "restaurant",
                        principalTable: "checklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checklist_runs",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_by_staff_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    pass_count = table.Column<int>(type: "integer", nullable: false),
                    fail_count = table.Column<int>(type: "integer", nullable: false),
                    has_critical_failure = table.Column<bool>(type: "boolean", nullable: false),
                    corrective_action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_checklist_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklist_runs_checklists_checklist_id",
                        column: x => x.checklist_id,
                        principalSchema: "restaurant",
                        principalTable: "checklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "temperature_logs",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reading_celsius = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    is_out_of_range = table.Column<bool>(type: "boolean", nullable: false),
                    corrective_action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_temperature_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_temperature_logs_temperature_checkpoints_checkpoint_id",
                        column: x => x.checkpoint_id,
                        principalSchema: "restaurant",
                        principalTable: "temperature_checkpoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checklist_answers",
                schema: "restaurant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    yes_no_value = table.Column<bool>(type: "boolean", nullable: true),
                    numeric_value = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    text_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_pass = table.Column<bool>(type: "boolean", nullable: false),
                    is_critical = table.Column<bool>(type: "boolean", nullable: false),
                    corrective_action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_checklist_answers", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklist_answers_checklist_runs_run_id",
                        column: x => x.run_id,
                        principalSchema: "restaurant",
                        principalTable: "checklist_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_checklist_answers_run_id",
                schema: "restaurant",
                table: "checklist_answers",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_items_checklist_id",
                schema: "restaurant",
                table: "checklist_items",
                column: "checklist_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_run_outlet_due",
                schema: "restaurant",
                table: "checklist_runs",
                columns: new[] { "outlet_id", "due_on" });

            migrationBuilder.CreateIndex(
                name: "ix_checklist_runs_checklist_id",
                schema: "restaurant",
                table: "checklist_runs",
                column: "checklist_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_zone_outlet_order",
                schema: "restaurant",
                table: "delivery_zones",
                columns: new[] { "outlet_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_prep_batch_company_code",
                schema: "restaurant",
                table: "prep_batches",
                columns: new[] { "company_id", "batch_code" });

            migrationBuilder.CreateIndex(
                name: "ix_prep_batch_outlet_use_by",
                schema: "restaurant",
                table: "prep_batches",
                columns: new[] { "outlet_id", "use_by_at" });

            migrationBuilder.CreateIndex(
                name: "ix_temp_checkpoint_outlet_order",
                schema: "restaurant",
                table: "temperature_checkpoints",
                columns: new[] { "outlet_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_temp_log_outlet_at",
                schema: "restaurant",
                table: "temperature_logs",
                columns: new[] { "outlet_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_temp_log_outlet_breach",
                schema: "restaurant",
                table: "temperature_logs",
                columns: new[] { "outlet_id", "is_out_of_range", "is_resolved" });

            migrationBuilder.CreateIndex(
                name: "ix_temperature_logs_checkpoint_id",
                schema: "restaurant",
                table: "temperature_logs",
                column: "checkpoint_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checklist_answers",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "checklist_items",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "delivery_zones",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "prep_batches",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "temperature_logs",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "checklist_runs",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "temperature_checkpoints",
                schema: "restaurant");

            migrationBuilder.DropTable(
                name: "checklists",
                schema: "restaurant");
        }
    }
}
