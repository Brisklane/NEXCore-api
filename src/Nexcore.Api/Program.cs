using Auth.Infrastructure;
using Core.Infrastructure;
using Procurement.Infrastructure;
using Crm.Infrastructure;
using Accounting.Infrastructure;
using Hr.Infrastructure;
using Inventory.Infrastructure;
using Manufacturing.Infrastructure;
using Restaurant.Infrastructure;
using Distribution.Infrastructure;
using Sales.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Nexcore.SharedKernel.Helpers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text;
using Nexcore.SharedKernel.Events;
using Sales.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Auth.Api.Controllers.AuthController).Assembly)
    .AddApplicationPart(typeof(Core.Api.Controllers.CompanyController).Assembly)
    .AddApplicationPart(typeof(Accounting.Api.Controllers.LedgerController).Assembly)
    .AddApplicationPart(typeof(Inventory.Api.Controllers.ItemController).Assembly)
    .AddApplicationPart(typeof(Manufacturing.Api.Controllers.ProductionOrderController).Assembly)
    .AddApplicationPart(typeof(Hr.Api.Controllers.CandidateController).Assembly)
    .AddApplicationPart(typeof(Crm.Api.Controllers.AccountsController).Assembly)
    .AddApplicationPart(typeof(Sales.Api.Controllers.SalesOrderController).Assembly)
    .AddApplicationPart(typeof(Restaurant.Api.Controllers.OrderController).Assembly)
    .AddApplicationPart(typeof(Distribution.Api.Controllers.OrderController).Assembly)
    .AddApplicationPart(typeof(Procurement.Api.Controllers.VendorController).Assembly);

// ? ADD THIS: Register IHttpContextAccessor for TenantAwareRepository
builder.Services.AddHttpContextAccessor();

// Add Swagger/Swashbuckle
builder.Services.AddSwaggerGen(options =>
{
    // ── One doc per module — keeps each spec small and fast to load ───────────
    options.SwaggerDoc("auth", new OpenApiInfo { Version = "v1", Title = "Auth" });
    options.SwaggerDoc("core", new OpenApiInfo { Version = "v1", Title = "Core" });
    options.SwaggerDoc("accounting", new OpenApiInfo { Version = "v1", Title = "Accounting" });
    options.SwaggerDoc("inventory", new OpenApiInfo { Version = "v1", Title = "Inventory" });
    options.SwaggerDoc("manufacturing", new OpenApiInfo { Version = "v1", Title = "Manufacturing" });
    options.SwaggerDoc("hr", new OpenApiInfo { Version = "v1", Title = "HR" });
    options.SwaggerDoc("crm", new OpenApiInfo { Version = "v1", Title = "CRM" });
    options.SwaggerDoc("sales", new OpenApiInfo { Version = "v1", Title = "Sales" });
    options.SwaggerDoc("restaurant", new OpenApiInfo { Version = "v1", Title = "Restaurant" });
    options.SwaggerDoc("distribution", new OpenApiInfo { Version = "v1", Title = "Distribution" });
    options.SwaggerDoc("procurement", new OpenApiInfo { Version = "v1", Title = "Procurement" });

    // ── Route each controller into its own module doc ─────────────────────────
    options.DocInclusionPredicate((docName, api) =>
    {
        if (api.ActionDescriptor is not Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad)
            return false;
        var ns = cad.ControllerTypeInfo.Namespace ?? "";
        return docName switch
        {
            "auth" => ns.StartsWith("Auth.Api"),
            "core" => ns.StartsWith("Core.Api"),
            "accounting" => ns.StartsWith("Accounting.Api"),
            "inventory" => ns.StartsWith("Inventory.Api"),
            "manufacturing" => ns.StartsWith("Manufacturing.Api"),
            "hr" => ns.StartsWith("Hr.Api"),
            "crm" => ns.StartsWith("Crm.Api"),
            "sales" => ns.StartsWith("Sales.Api"),
            "restaurant" => ns.StartsWith("Restaurant.Api"),
            "distribution" => ns.StartsWith("Distribution.Api"),
            "procurement" => ns.StartsWith("Procurement.Api"),
            _ => false
        };
    });

    // ── Tag by controller name only (module is already the doc) ──────────────
    options.TagActionsBy(api =>
    {
        if (api.ActionDescriptor is not Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad)
            return [api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Other"];
        return [cad.ControllerName];
    });

    // ── JWT security ──────────────────────────────────────────────────────────
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // ── XML comments ──────────────────────────────────────────────────────────
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.CustomSchemaIds(type => type.FullName);

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

// Register Event Publisher as Singleton — it holds IServiceScopeFactory (singleton itself)
// and creates a new child scope per handler, so no lifetime issues.
builder.Services.AddSingleton<IEventPublisher, InProcessEventPublisher>();

// Add Core Infrastructure (Company, Branch, BusinessUnit) + Event Publisher
builder.Services.AddCoreInfrastructure(builder.Configuration);

// Add Auth Infrastructure (User, Role, Permission)
builder.Services.AddAuthInfrastructure(builder.Configuration);

// Add Accounting Infrastructure (Ledgers, Accounts, Reports, etc.)
// Includes: Repositories, FinancialReportService, EventHandlers
builder.Services.AddAccountingInfrastructure(builder.Configuration);

// Add Inventory Infrastructure (Items, Warehouses, Documents, etc.)
// Includes: Repositories, Document Services
builder.Services.AddInventoryInfrastructure(builder.Configuration);

// Add Manufacturing Infrastructure (Production, BOM, Routing, WIP, Quality, Costing, etc.)
builder.Services.AddManufacturingInfrastructure(builder.Configuration);

// Add HR Infrastructure (Jobs, Candidates, Applications, Interviews, Offers, Lookups)
builder.Services.AddHrInfrastructure(builder.Configuration);

// Add CRM Infrastructure (Accounts, Contacts, Leads, Deals, etc.)
builder.Services.AddCrmInfrastructure(builder.Configuration);

// Add Sales Infrastructure (Orders, Invoices, Payments, POS, Riders, Loyalty, etc.)
builder.Services.AddSalesInfrastructure(builder.Configuration);

// ── Real-time POS order notifications (SignalR) ───────────────────────────────
// Hub pushes online-order arrivals + pending-count changes to each store's POS screen.
// The handler bridges in-process order events to the hub.
builder.Services.AddSignalR();
builder.Services.AddScoped<IEventHandler<OnlineOrderPlacedEvent>, PosOrderNotificationHandler>();
builder.Services.AddScoped<IEventHandler<OnlineOrderQueueChangedEvent>, PosOrderNotificationHandler>();

// Add Restaurant Infrastructure (outlets, floor plan, menus, kitchen, orders, checks, recipes)
builder.Services.AddRestaurantInfrastructure(builder.Configuration);

// Live sync for the floor plan, order pad and kitchen display.
builder.Services.AddScoped<Restaurant.Api.Hubs.IRestaurantNotifier, Restaurant.Api.Hubs.RestaurantNotifier>();

// Add Distribution Infrastructure (channel network, routes, field force, van sales, trade schemes,
// claims, settlement and the secondary-sales layer)
builder.Services.AddDistributionInfrastructure(builder.Configuration);

// Live sync for the dispatch desk, trip board and settlement queue.
builder.Services.AddScoped<Distribution.Api.Hubs.IDistributionNotifier, Distribution.Api.Hubs.DistributionNotifier>();

// Add Procurement Infrastructure (Vendors, POs, GRNs, AP Invoices, Payments, Contracts)
builder.Services.AddProcurementInfrastructure(builder.Configuration);

// Local-dev only: auto-migrate + seed on startup. Gated by RUN_MIGRATIONS=true, which is set
// in launchSettings.json but on NO Azure tier — so on Dev/QA/Prod this service is never registered
// and the app never touches the schema or seed data on startup. There, the pipeline owns migrations
// (and seeding). (Testing also never sets RUN_MIGRATIONS, so test hosts skip it too.)
if (Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true")
    builder.Services.AddHostedService<Nexcore.Api.DatabaseMigrationService>();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured");

// ── Subdomain tenancy ({slug}.{BaseDomain}) config ──────────────────────────
var appBaseDomain = builder.Configuration["App:BaseDomain"] ?? "";
var appReservedSubdomains = builder.Configuration.GetSection("App:ReservedSubdomains").Get<string[]>() ?? Array.Empty<string>();
// Default OFF: enable only once the app sits behind Front Door so the real {slug}.{BaseDomain}
// host (via X-Forwarded-Host) reaches here — otherwise every tenant request would be rejected.
var enforceHostTenantMatch = builder.Configuration.GetValue<bool>("App:EnforceHostTenantMatch");

// Behind Azure Front Door the original tenant host arrives in X-Forwarded-Host; honor it so the
// optional Host↔CompanySlug check (and any Request.Host use) sees {slug}.{BaseDomain}.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto
                             | ForwardedHeaders.XForwardedHost;
    // Front Door's egress IPs aren't enumerable here and the edge is the only ingress, so trust it.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // SignalR WebSockets can't send an Authorization header — accept the JWT from
        // the access_token query string on hub requests so [Authorize] hubs work.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },

            // Optional defense (App:EnforceHostTenantMatch): reject a company-scoped token whose
            // CompanySlug claim does not match the {slug}.{BaseDomain} host it was presented on, so
            // a token minted for company A cannot be replayed against company B's subdomain. Skipped
            // for tokens without a CompanySlug claim (pre-auth/handoff/legacy/integration) and for
            // non-tenant hosts (apex, reserved/platform subdomains, azurewebsites.net, localhost).
            OnTokenValidated = context =>
            {
                // Single-purpose tokens (handoff/pre-auth) are signed JWTs but must NEVER be usable as
                // access tokens against the API — reject them outright if presented as a bearer.
                var tokenType = context.Principal?.FindFirst("token_type")?.Value;
                if (tokenType is "handoff" or "pre-auth")
                {
                    context.Fail("This token type cannot be used to access the API.");
                    return Task.CompletedTask;
                }

                if (!enforceHostTenantMatch || string.IsNullOrEmpty(appBaseDomain))
                    return Task.CompletedTask;

                var slugClaim = context.Principal?.FindFirst("CompanySlug")?.Value;
                if (string.IsNullOrEmpty(slugClaim))
                    return Task.CompletedTask;

                var host = context.HttpContext.Request.Host.Host;
                if (!host.EndsWith("." + appBaseDomain, StringComparison.OrdinalIgnoreCase))
                    return Task.CompletedTask; // not a tenant host under the base domain

                var label = host[..(host.Length - appBaseDomain.Length - 1)];
                var lastDot = label.LastIndexOf('.');
                if (lastDot >= 0)
                    label = label[(lastDot + 1)..]; // left-most label only

                if (SubdomainRules.IsReserved(label, appReservedSubdomains))
                    return Task.CompletedTask; // platform/reserved host

                if (!string.Equals(label, slugClaim, StringComparison.OrdinalIgnoreCase))
                    context.Fail("Token is not valid for this company workspace.");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddHttpClient();

// Add Authorization with permission-based policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("COMPANY_CREATE", policy => policy.RequireClaim("permission", "COMPANY_CREATE"))
    .AddPolicy("COMPANY_EDIT", policy => policy.RequireClaim("permission", "COMPANY_EDIT"))
    .AddPolicy("COMPANY_DELETE", policy => policy.RequireClaim("permission", "COMPANY_DELETE"))
    .AddPolicy("COMPANY_VIEW", policy => policy.RequireClaim("permission", "COMPANY_VIEW"))
    .AddPolicy("USER_CREATE", policy => policy.RequireClaim("permission", "USER_CREATE"))
    .AddPolicy("USER_EDIT", policy => policy.RequireClaim("permission", "USER_EDIT"))
    .AddPolicy("USER_DELETE", policy => policy.RequireClaim("permission", "USER_DELETE"))
    .AddPolicy("USER_VIEW", policy => policy.RequireClaim("permission", "USER_VIEW"))
    .AddPolicy("ROLE_CREATE", policy => policy.RequireClaim("permission", "ROLE_CREATE"))
    .AddPolicy("ROLE_EDIT", policy => policy.RequireClaim("permission", "ROLE_EDIT"))
    .AddPolicy("ROLE_DELETE", policy => policy.RequireClaim("permission", "ROLE_DELETE"))
    .AddPolicy("ROLE_VIEW", policy => policy.RequireClaim("permission", "ROLE_VIEW"))
    .AddPolicy("AUDIT_VIEW", policy => policy.RequireClaim("permission", "AUDIT_VIEW"));

// CORS — permissive in Development for local tooling/Swagger; locked down everywhere else.
// Production origins come from App:AllowedOrigins (array). If none are configured, fall back to
// any subdomain of App:BaseDomain ({slug}.{BaseDomain}); if that is empty too, no cross-origin
// request is allowed. Auth is bearer-token (no cookies), so AllowCredentials is intentionally unset.
var allowedCorsOrigins = builder.Configuration.GetSection("App:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.AllowAnyMethod().AllowAnyHeader();

        if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin();
        else if (allowedCorsOrigins.Length > 0)
            policy.WithOrigins(allowedCorsOrigins);
        else if (!string.IsNullOrEmpty(appBaseDomain))
            policy.SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var u) &&
                (u.Host.Equals(appBaseDomain, StringComparison.OrdinalIgnoreCase) ||
                 u.Host.EndsWith("." + appBaseDomain, StringComparison.OrdinalIgnoreCase)));
    });
});

var app = builder.Build();

// Must run first so X-Forwarded-Host/Proto from Front Door are applied before anything reads Host.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI
    app.UseSwagger(options =>
    {
        options.SerializeAsV2 = false;
    });

    app.UseSwaggerUI(options =>
    {
        // One dropdown entry per module — each loads only its own spec
        options.SwaggerEndpoint("/swagger/auth/swagger.json", "Auth");
        options.SwaggerEndpoint("/swagger/core/swagger.json", "Core");
        options.SwaggerEndpoint("/swagger/accounting/swagger.json", "Accounting");
        options.SwaggerEndpoint("/swagger/inventory/swagger.json", "Inventory");
        options.SwaggerEndpoint("/swagger/manufacturing/swagger.json", "Manufacturing");
        options.SwaggerEndpoint("/swagger/hr/swagger.json", "HR");
        options.SwaggerEndpoint("/swagger/crm/swagger.json", "CRM");
        options.SwaggerEndpoint("/swagger/sales/swagger.json", "Sales");
        options.SwaggerEndpoint("/swagger/restaurant/swagger.json", "Restaurant");
        options.SwaggerEndpoint("/swagger/distribution/swagger.json", "Distribution");
        options.SwaggerEndpoint("/swagger/procurement/swagger.json", "Procurement");

        options.RoutePrefix = "swagger";

        // Collapse everything — nothing is pre-rendered on page load
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        options.DefaultModelsExpandDepth(-1); // hide schemas section by default
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles(); // serve wwwroot/images/products/ for seed placeholder images

app.UseCors("DefaultCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Real-time POS order notifications
app.MapHub<PosOrderHub>("/hubs/pos-orders");

// Real-time restaurant floor / kitchen sync
app.MapHub<Restaurant.Api.Hubs.RestaurantHub>("/hubs/restaurant");

// Real-time dispatch, trip and settlement sync
app.MapHub<Distribution.Api.Hubs.DistributionHub>("/hubs/distribution");

app.Run();

/// <summary>
/// Custom operation filter to show authorization requirement in Swagger
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Any() ?? false;

        var methodHasAuthorize = context.MethodInfo
            .GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Any();

        if (hasAuthorize || methodHasAuthorize)
        {
            operation.Summary ??= "";
            if (!operation.Summary.Contains("Requires authentication"))
            {
                operation.Summary = $"[Auth Required] {operation.Summary}";
            }
        }
    }
}

// Make Program accessible for testing
public partial class Program { }
