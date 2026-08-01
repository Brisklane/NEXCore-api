using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateTable(
                name: "currencies",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    decimal_places = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "core",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    native_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_rtl = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_languages", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    price_per_month = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    price_per_year = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    max_companies = table.Column<int>(type: "integer", nullable: false),
                    max_users = table.Column<int>(type: "integer", nullable: false),
                    max_branches = table.Column<int>(type: "integer", nullable: false),
                    allowed_modules = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    trial_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "core",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(2)", unicode: false, maxLength: 2, nullable: false),
                    code3 = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    numeric_code = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    official_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sub_region = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: true),
                    default_currency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    phone_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    flag_emoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    postal_code_pattern = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DEFAULT"),
                    state_label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "State / Province"),
                    postal_code_label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.code);
                    table.ForeignKey(
                        name: "fk_countries_currencies_default_currency_id",
                        column: x => x.default_currency_id,
                        principalSchema: "core",
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "currency_translations",
                schema: "core",
                columns: table => new
                {
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currency_translations", x => new { x.currency_id, x.language_code });
                    table.ForeignKey(
                        name: "fk_currency_translations_currencies_currency_id",
                        column: x => x.currency_id,
                        principalSchema: "core",
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_currency_translations_languages_language_code",
                        column: x => x.language_code,
                        principalSchema: "core",
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    base_currency_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    company_street_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    company_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    company_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    company_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    company_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    radius_in_meters = table.Column<int>(type: "integer", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    company_logo = table.Column<byte[]>(type: "bytea", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    fiscal_year_start_month = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                    table.ForeignKey(
                        name: "fk_companies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_subscriptions",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    billing_cycle = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    trial_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    license_key = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_subscriptions_subscription_plans_subscription_plan_id",
                        column: x => x.subscription_plan_id,
                        principalSchema: "core",
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_subscriptions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "country_translations",
                schema: "core",
                columns: table => new
                {
                    country_code = table.Column<string>(type: "character varying(2)", unicode: false, maxLength: 2, nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    official_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_translations", x => new { x.country_code, x.language_code });
                    table.ForeignKey(
                        name: "fk_country_translations_countries_country_code",
                        column: x => x.country_code,
                        principalSchema: "core",
                        principalTable: "countries",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_country_translations_languages_language_code",
                        column: x => x.language_code,
                        principalSchema: "core",
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subdivisions",
                schema: "core",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", unicode: false, maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subdivision_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "State"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subdivisions", x => x.code);
                    table.ForeignKey(
                        name: "fk_subdivisions_countries_country_code",
                        column: x => x.country_code,
                        principalSchema: "core",
                        principalTable: "countries",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    branch_street_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    branch_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    branch_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    branch_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    branch_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    manager_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    branch_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    branch_logo = table.Column<byte[]>(type: "bytea", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_branches_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "core",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    city_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", unicode: false, maxLength: 2, nullable: false),
                    subdivision_code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    population = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                    table.ForeignKey(
                        name: "fk_cities_countries_country_code",
                        column: x => x.country_code,
                        principalSchema: "core",
                        principalTable: "countries",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cities_subdivisions_subdivision_code",
                        column: x => x.subdivision_code,
                        principalSchema: "core",
                        principalTable: "subdivisions",
                        principalColumn: "code");
                });

            migrationBuilder.CreateTable(
                name: "subdivision_translations",
                schema: "core",
                columns: table => new
                {
                    subdivision_code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subdivision_translations", x => new { x.subdivision_code, x.language_code });
                    table.ForeignKey(
                        name: "fk_subdivision_translations_languages_language_code",
                        column: x => x.language_code,
                        principalSchema: "core",
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subdivision_translations_subdivisions_subdivision_code",
                        column: x => x.subdivision_code,
                        principalSchema: "core",
                        principalTable: "subdivisions",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "business_units",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    unit_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    manager_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    manager_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    cost_center_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    profit_center_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_business_units_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "core",
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_business_units_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "core",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "city_translations",
                schema: "core",
                columns: table => new
                {
                    city_id = table.Column<int>(type: "integer", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_city_translations", x => new { x.city_id, x.language_code });
                    table.ForeignKey(
                        name: "fk_city_translations_cities_city_id",
                        column: x => x.city_id,
                        principalSchema: "core",
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_city_translations_languages_language_code",
                        column: x => x.language_code,
                        principalSchema: "core",
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "subscription_plans",
                columns: new[] { "id", "allowed_modules", "code", "created_at", "created_by_user_id", "description", "is_active", "max_branches", "max_companies", "max_users", "name", "price_per_month", "price_per_year", "trial_days", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Core,HR,Accounting,Inventory,Sales", "TRIAL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), "30-day free trial with full access", true, 1, 1, 5, "Trial", 0m, 0m, 30, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Core,HR,Accounting", "STARTER", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), "For small businesses getting started", true, 2, 1, 10, "Starter", 49m, 490m, 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "Core,HR,Accounting,Inventory,Sales", "PROFESSIONAL", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), "For growing businesses needing full ERP capabilities", true, 10, 5, 50, "Professional", 149m, 1490m, 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000004"), null, "ENTERPRISE", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("00000000-0000-0000-0000-000000000000"), "Unlimited scale for large organizations", true, -1, -1, -1, "Enterprise", 499m, 4990m, 0, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_branches_company_id_code",
                schema: "core",
                table: "branches",
                columns: new[] { "company_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_branches_company_id_name",
                schema: "core",
                table: "branches",
                columns: new[] { "company_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_business_units_branch_id_name",
                schema: "core",
                table: "business_units",
                columns: new[] { "branch_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_business_units_company_id_code",
                schema: "core",
                table: "business_units",
                columns: new[] { "company_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cities_country_code",
                schema: "core",
                table: "cities",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_cities_country_code_name",
                schema: "core",
                table: "cities",
                columns: new[] { "country_code", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_cities_subdivision_code",
                schema: "core",
                table: "cities",
                column: "subdivision_code");

            migrationBuilder.CreateIndex(
                name: "ix_city_translations_language_code",
                schema: "core",
                table: "city_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "ix_companies_code",
                schema: "core",
                table: "companies",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_companies_slug",
                schema: "core",
                table: "companies",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_id",
                schema: "core",
                table: "companies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_id_company_name",
                schema: "core",
                table: "companies",
                columns: new[] { "tenant_id", "company_name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_countries_default_currency_id",
                schema: "core",
                table: "countries",
                column: "default_currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_country_translations_language_code",
                schema: "core",
                table: "country_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "ix_currencies_code",
                schema: "core",
                table: "currencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_currency_translations_language_code",
                schema: "core",
                table: "currency_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "ix_subdivision_translations_language_code",
                schema: "core",
                table: "subdivision_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "ix_subdivisions_country_code",
                schema: "core",
                table: "subdivisions",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_code",
                schema: "core",
                table: "subscription_plans",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_subscriptions_license_key",
                schema: "core",
                table: "tenant_subscriptions",
                column: "license_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_subscriptions_subscription_plan_id",
                schema: "core",
                table: "tenant_subscriptions",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_subscriptions_tenant_id_status",
                schema: "core",
                table: "tenant_subscriptions",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_tenants_slug",
                schema: "core",
                table: "tenants",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_units",
                schema: "core");

            migrationBuilder.DropTable(
                name: "city_translations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "country_translations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "currency_translations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "subdivision_translations",
                schema: "core");

            migrationBuilder.DropTable(
                name: "tenant_subscriptions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "branches",
                schema: "core");

            migrationBuilder.DropTable(
                name: "cities",
                schema: "core");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "core");

            migrationBuilder.DropTable(
                name: "subscription_plans",
                schema: "core");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "core");

            migrationBuilder.DropTable(
                name: "subdivisions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "core");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "core");

            migrationBuilder.DropTable(
                name: "currencies",
                schema: "core");
        }
    }
}
