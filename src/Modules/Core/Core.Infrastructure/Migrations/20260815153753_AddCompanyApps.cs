using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyApps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_apps",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    app_key = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    is_installed = table.Column<bool>(type: "boolean", nullable: false),
                    installed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    installed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uninstalled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    uninstalled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_apps", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_apps_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "core",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_company_apps_company_id_app_key",
                schema: "core",
                table: "company_apps",
                columns: new[] { "company_id", "app_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_apps",
                schema: "core");
        }
    }
}
