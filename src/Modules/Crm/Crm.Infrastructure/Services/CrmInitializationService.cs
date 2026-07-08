using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel;

namespace Crm.Infrastructure.Services;

/// <summary>
/// Seeds a rich, realistic set of CRM master and sample data for a newly created company.
/// Called automatically when a CompanyCreatedEvent is published.
/// Seeded in dependency order inside a single transaction:
///   Pipelines ? Products ? Pricebook ? Tags ? Territories ? Teams
///   ? Accounts ? Contacts ? Leads ? Deals ? Campaigns
///   ? Cases ? KnowledgeArticles ? Activities ? Notes
/// </summary>
public class CrmInitializationService : ICrmInitializationService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<CrmInitializationService> _logger;

    public CrmInitializationService(CrmDbContext db, ILogger<CrmInitializationService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ?
    public async Task<Result> InitializeCrmForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        try
        {
            if (await CrmDataExistsAsync(companyId))
            {
                _logger.LogWarning("CRM data already exists for Company:{CompanyId}", companyId);
                return Result.Ok("CRM data already initialized for this company.");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var ctx = new SeedCtx(companyId, branchId, businessUnitId, userId, DateTime.UtcNow);

                // 1. Pipelines & Stages
                var pipelines = CreatePipelines(ctx);
                _db.Pipelines.AddRange(pipelines);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} pipelines seeded", pipelines.Length);

                var stages = CreatePipelineStages(ctx, pipelines);
                _db.PipelineStages.AddRange(stages);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} pipeline stages seeded", stages.Length);

                // 2. Products & Pricebooks
                var products = CreateProducts(ctx);
                _db.Products.AddRange(products);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} products seeded", products.Length);

                var pricebooks = CreatePricebooks(ctx);
                _db.Pricebooks.AddRange(pricebooks);
                await _db.SaveChangesAsync();

                var entries = CreatePricebookEntries(ctx, products, pricebooks);
                _db.PricebookEntries.AddRange(entries);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} pricebook entries seeded", entries.Length);

                // 3. Tags
                var tags = CreateTags(ctx);
                _db.Tags.AddRange(tags);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} tags seeded", tags.Length);

                // 4. Territories
                var territories = CreateTerritories(ctx);
                _db.Territories.AddRange(territories);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} territories seeded", territories.Length);

                // 5. Teams
                var teams = CreateTeams(ctx);
                _db.Teams.AddRange(teams);
                await _db.SaveChangesAsync();

                var teamMembers = CreateTeamMembers(ctx, teams);
                _db.TeamMembers.AddRange(teamMembers);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} teams + {M} members seeded", teams.Length, teamMembers.Length);

                // 6. Accounts
                var accounts = CreateAccounts(ctx);
                _db.Accounts.AddRange(accounts);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} accounts seeded", accounts.Length);

                // 7. Territory ? Account assignments
                var terrAccounts = CreateTerritoryAccounts(ctx, territories, accounts);
                _db.TerritoryAccounts.AddRange(terrAccounts);
                await _db.SaveChangesAsync();

                // 8. Contacts
                var contacts = CreateContacts(ctx, accounts);
                _db.Contacts.AddRange(contacts);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} contacts seeded", contacts.Length);

                // 9. Leads
                var leads = CreateLeads(ctx);
                _db.Leads.AddRange(leads);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} leads seeded", leads.Length);

                // 10. Deals
                var deals = CreateDeals(ctx, accounts, pipelines, stages);
                _db.Deals.AddRange(deals);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} deals seeded", deals.Length);

                // 11. Campaigns
                var campaigns = CreateCampaigns(ctx);
                _db.Campaigns.AddRange(campaigns);
                await _db.SaveChangesAsync();

                var campaignMembers = CreateCampaignMembers(ctx, campaigns, leads, contacts);
                _db.CampaignMembers.AddRange(campaignMembers);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} campaigns seeded", campaigns.Length);

                // 12. Contact Lists
                var contactLists = CreateContactLists(ctx);
                _db.ContactLists.AddRange(contactLists);
                await _db.SaveChangesAsync();

                // 13. Entitlements
                var entitlements = CreateEntitlements(ctx, accounts, contacts);
                _db.Entitlements.AddRange(entitlements);
                await _db.SaveChangesAsync();

                // 14. Cases
                var cases = CreateCases(ctx, accounts, contacts, entitlements);
                _db.Cases.AddRange(cases);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} cases seeded", cases.Length);

                // 15. Knowledge Articles
                var articles = CreateKnowledgeArticles(ctx);
                _db.KnowledgeArticles.AddRange(articles);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} knowledge articles seeded", articles.Length);

                // 16. Activities
                var activities = CreateActivities(ctx, accounts, contacts, leads, deals, cases);
                _db.Activities.AddRange(activities);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} activities seeded", activities.Length);

                // 17. Notes
                var notes = CreateNotes(ctx, accounts, contacts, leads, deals, cases);
                _db.Notes.AddRange(notes);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} notes seeded", notes.Length);

                // 18. Entity Tags
                var entityTags = CreateEntityTags(ctx, tags, accounts, contacts, leads, deals);
                _db.EntityTags.AddRange(entityTags);
                await _db.SaveChangesAsync();

                // 19. Sales Targets
                var targets = CreateSalesTargets(ctx, teams, territories);
                _db.SalesTargets.AddRange(targets);
                await _db.SaveChangesAsync();
                _logger.LogInformation("CRM: {N} sales targets seeded", targets.Length);

                // 20. Forecasts
                var forecasts = CreateForecasts(ctx);
                _db.Forecasts.AddRange(forecasts);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                _logger.LogInformation("CRM initialization complete for Company:{CompanyId}", companyId);
                return Result.Ok("CRM data initialized successfully.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize CRM data for Company:{CompanyId}", companyId);
            return Result.Fail($"CRM initialization failed: {ex.Message}");
        }
    }

    public async Task<bool> CrmDataExistsAsync(Guid companyId)
        => await _db.Accounts.AnyAsync(a => a.CompanyId == companyId);

    public async Task EnsureWalkInCustomersAsync()
    {
        // Find all companies that have CRM contacts but no walk-in customer.
        var companiesWithContacts = await _db.Contacts
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.CompanyId, c.BranchId, c.BusinessUnitId, c.CreatedByUserId })
            .Distinct()
            .ToListAsync();

        var companiesWithWalkIn = await _db.Contacts
            .Where(c => c.CustomerType == Crm.Domain.Enums.CustomerType.WalkIn && c.IsAnonymous && !c.IsDeleted)
            .Select(c => c.CompanyId)
            .ToListAsync();

        var missing = companiesWithContacts
            .GroupBy(c => c.CompanyId)
            .Where(g => !companiesWithWalkIn.Contains(g.Key))
            .Select(g => g.First())
            .ToList();

        if (missing.Count == 0) return;

        foreach (var m in missing)
        {
            var walkIn = new Crm.Domain.Entities.Contact
            {
                FirstName      = "Walk-in",
                LastName       = "Customer",
                FullName       = "Walk-in Customer",
                ShortName      = "Walk-in",
                CustomerType   = Crm.Domain.Enums.CustomerType.WalkIn,
                CustomerNumber = "CUS-000000",
                IsAnonymous    = true,
                IsActive       = true,
                EmailOptOut    = true,
                OwnerId        = m.CreatedByUserId,
                Description    = "Default anonymous walk-in customer for POS / counter sales",
                CompanyId       = m.CompanyId,
                BranchId        = m.BranchId,
                BusinessUnitId  = m.BusinessUnitId,
                CreatedByUserId = m.CreatedByUserId,
                CreatedAt       = DateTime.UtcNow,
            };
            _db.Contacts.Add(walkIn);
            _logger.LogInformation("CRM: Walk-in Customer seeded for Company:{CompanyId}", m.CompanyId);
        }

        await _db.SaveChangesAsync();
    }

    // ?
    // Seed context - avoids repeating tenant params in every factory call
    // ?
    private sealed class SeedCtx
    {
        public Guid CompanyId      { get; }
        public Guid BranchId       { get; }
        public Guid BusinessUnitId { get; }
        public Guid UserId         { get; }
        public DateTime Now        { get; }

        public SeedCtx(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now)
        {
            CompanyId = companyId; BranchId = branchId;
            BusinessUnitId = businessUnitId; UserId = userId; Now = now;
        }
    }

    /// <summary>Applies tenant + audit fields shared by every entity.</summary>
    private void SetBase(BaseEntity e, SeedCtx ctx)
    {
        e.CompanyId       = ctx.CompanyId;
        e.BranchId        = ctx.BranchId;
        e.BusinessUnitId  = ctx.BusinessUnitId;
        e.CreatedByUserId = ctx.UserId;
        e.CreatedAt       = ctx.Now;
    }

    // ?
    // 1. Pipelines
    // ?
    private Pipeline[] CreatePipelines(SeedCtx ctx)
    {
        Pipeline P(string name, string desc, bool isDefault)
        {
            var p = new Pipeline { PipelineName = name, Description = desc, IsDefault = isDefault, IsActive = true };
            SetBase(p, ctx);
            return p;
        }

        return
        [
            P("Sales Pipeline",         "Standard B2B sales process",                  true),
            P("Enterprise Pipeline",    "Long-cycle enterprise sales",                 false),
            P("SMB Pipeline",           "Small & medium business fast-close process",  false),
            P("Renewal Pipeline",       "Customer renewal and upsell opportunities",   false),
            P("Partner Pipeline",       "Channel and reseller-driven opportunities",   false),
        ];
    }

    // ?
    // 2. Pipeline Stages
    // ?
    private PipelineStage[] CreatePipelineStages(SeedCtx ctx, Pipeline[] pipelines)
    {
        PipelineStage S(Guid pipelineId, string name, int order, decimal prob,
                        string forecast, bool won = false, bool lost = false)
        {
            var s = new PipelineStage
            {
                PipelineId         = pipelineId,
                StageName          = name,
                DisplayOrder       = order,
                ProbabilityPercent = prob,
                ForecastCategory   = forecast,
                IsWon              = won,
                IsLost             = lost
            };
            SetBase(s, ctx);
            return s;
        }

        var sales      = pipelines[0].Id;
        var enterprise = pipelines[1].Id;
        var smb        = pipelines[2].Id;
        var renewal    = pipelines[3].Id;
        var partner    = pipelines[4].Id;

        return
        [
            // Sales Pipeline
            S(sales, "Prospecting",            1, 10,  "Pipeline"),
            S(sales, "Qualification",          2, 20,  "Pipeline"),
            S(sales, "Needs Analysis",         3, 30,  "Pipeline"),
            S(sales, "Value Proposition",      4, 50,  "BestCase"),
            S(sales, "Decision Makers",        5, 60,  "BestCase"),
            S(sales, "Perception Analysis",    6, 70,  "Commit"),
            S(sales, "Proposal / Price Quote", 7, 75,  "Commit"),
            S(sales, "Negotiation / Review",   8, 85,  "Commit"),
            S(sales, "Closed Won",             9, 100, "Closed", won: true),
            S(sales, "Closed Lost",           10, 0,   "Omitted", lost: true),

            // Enterprise Pipeline
            S(enterprise, "Initial Outreach",  1, 5,   "Pipeline"),
            S(enterprise, "Discovery Call",    2, 15,  "Pipeline"),
            S(enterprise, "Demo Delivered",    3, 25,  "Pipeline"),
            S(enterprise, "POC / Pilot",       4, 40,  "BestCase"),
            S(enterprise, "Business Case",     5, 55,  "BestCase"),
            S(enterprise, "Procurement",       6, 70,  "Commit"),
            S(enterprise, "Contract Review",   7, 80,  "Commit"),
            S(enterprise, "Closed Won",        8, 100, "Closed", won: true),
            S(enterprise, "Closed Lost",       9, 0,   "Omitted", lost: true),

            // SMB Pipeline
            S(smb, "New Lead",    1, 20,  "Pipeline"),
            S(smb, "Demo",        2, 40,  "BestCase"),
            S(smb, "Trial",       3, 60,  "Commit"),
            S(smb, "Closed Won",  4, 100, "Closed", won: true),
            S(smb, "Closed Lost", 5, 0,   "Omitted", lost: true),

            // Renewal Pipeline
            S(renewal, "Renewal Due",      1, 70,  "Commit"),
            S(renewal, "Upsell Review",    2, 75,  "Commit"),
            S(renewal, "Contract Sent",    3, 85,  "Commit"),
            S(renewal, "Renewed Won",      4, 100, "Closed", won: true),
            S(renewal, "Churned",          5, 0,   "Omitted", lost: true),

            // Partner Pipeline
            S(partner, "Registered",      1, 15,  "Pipeline"),
            S(partner, "Partner Demo",    2, 30,  "BestCase"),
            S(partner, "Joint Proposal",  3, 55,  "Commit"),
            S(partner, "Closed Won",      4, 100, "Closed", won: true),
            S(partner, "Closed Lost",     5, 0,   "Omitted", lost: true),
        ];
    }

    // ?
    // 3. Products
    // ?
    private Product[] CreateProducts(SeedCtx ctx)
    {
        Product P(string name, string code, string family, string unit, decimal price, string desc)
        {
            var p = new Product
            {
                ProductName       = name,
                ProductCode       = code,
                ProductFamily     = family,
                QuantityUnit      = unit,
                QuantityUnitPrice = price,
                Description       = desc,
                IsActive          = true
            };
            SetBase(p, ctx);
            return p;
        }

        return
        [
            // Software
            P("CRM Starter License",       "SW-CRM-STR", "Software",   "License", 49m,    "CRM for small teams up to 5 users"),
            P("CRM Professional License",  "SW-CRM-PRO", "Software",   "License", 99m,    "CRM professional with automation"),
            P("CRM Enterprise License",    "SW-CRM-ENT", "Software",   "License", 199m,   "Full-featured enterprise CRM"),
            P("ERP Core Module",           "SW-ERP-COR", "Software",   "License", 299m,   "Core ERP with GL, AR, AP"),
            P("ERP Manufacturing Add-on",  "SW-ERP-MFG", "Software",   "License", 149m,   "Manufacturing and BOM add-on"),
            P("ERP Inventory Add-on",      "SW-ERP-INV", "Software",   "License", 129m,   "Inventory and warehousing add-on"),
            P("Analytics Dashboard",       "SW-ANA-DSH", "Software",   "License", 79m,    "Business intelligence dashboards"),
            P("Mobile App License",        "SW-MOB-APP", "Software",   "License", 29m,    "Mobile CRM app per user"),

            // Services
            P("Implementation Service",    "SV-IMP-STD", "Services",   "Hour",    150m,   "Standard implementation hours"),
            P("Premium Implementation",    "SV-IMP-PRM", "Services",   "Hour",    200m,   "Senior consultant implementation"),
            P("Training - Standard",       "SV-TRN-STD", "Services",   "Day",     800m,   "On-site standard user training"),
            P("Training - Advanced",       "SV-TRN-ADV", "Services",   "Day",     1200m,  "Advanced administrator training"),
            P("Data Migration Service",    "SV-DAT-MIG", "Services",   "Hour",    175m,   "Legacy data migration and cleansing"),
            P("Custom Development",        "SV-DEV-CST", "Services",   "Hour",    225m,   "Custom feature development"),
            P("API Integration Service",   "SV-INT-API", "Services",   "Hour",    190m,   "Third-party API integration"),

            // Support
            P("Support - Bronze",          "SP-SUP-BRZ", "Support",    "Month",   199m,   "8-5 email support SLA"),
            P("Support - Silver",          "SP-SUP-SLV", "Support",    "Month",   399m,   "12-5 phone & email support"),
            P("Support - Gold",            "SP-SUP-GLD", "Support",    "Month",   799m,   "24-7 priority support with CSM"),
            P("Support - Platinum",        "SP-SUP-PLT", "Support",    "Month",   1499m,  "24-7 dedicated support team"),

            // Hardware
            P("Barcode Scanner",           "HW-BAR-SCN", "Hardware",   "Each",    249m,   "Bluetooth barcode scanner"),
            P("Label Printer",             "HW-LBL-PRT", "Hardware",   "Each",    349m,   "Thermal label printer"),
            P("Server Appliance",          "HW-SRV-APP", "Hardware",   "Each",    2499m,  "On-premise server appliance"),
        ];
    }

    // ?
    // 4. Pricebooks & Entries
    // ?
    private Pricebook[] CreatePricebooks(SeedCtx ctx)
    {
        Pricebook B(string name, string currency, bool isStandard, string desc)
        {
            var b = new Pricebook
            {
                PricebookName = name, CurrencyCode = currency,
                IsStandard = isStandard, IsActive = true, Description = desc
            };
            SetBase(b, ctx);
            return b;
        }

        return
        [
            B("Standard Price Book",   "USD", true,  "Default list prices"),
            B("Partner Price Book",    "USD", false, "Discounted prices for resellers (15% off)"),
            B("Enterprise Price Book", "USD", false, "Volume pricing for enterprise clients"),
            B("GBP Price Book",        "GBP", false, "UK market pricing in GBP"),
            B("EUR Price Book",        "EUR", false, "European market pricing in EUR"),
        ];
    }

    private PricebookEntry[] CreatePricebookEntries(SeedCtx ctx, Product[] products, Pricebook[] books)
    {
        var entries    = new List<PricebookEntry>();
        var standard   = books[0];
        var partner    = books[1];
        var enterprise = books[2];

        foreach (var p in products)
        {
            var list = p.QuantityUnitPrice ?? 0m;

            PricebookEntry E(Guid bookId, decimal price, string currency)
            {
                var e = new PricebookEntry
                {
                    PricebookId      = bookId, ProductId = p.Id,
                    UnitPrice        = price,  IsActive  = true,
                    CurrencyCode     = currency,
                    UseStandardPrice = false
                };
                SetBase(e, ctx);
                return e;
            }

            entries.Add(E(standard.Id,   list,                          "USD")); // list price
            entries.Add(E(partner.Id,    Math.Round(list * 0.85m, 2),   "USD")); // 15% off
            entries.Add(E(enterprise.Id, Math.Round(list * 0.75m, 2),   "USD")); // 25% off
        }

        return [.. entries];
    }

    // ?
    // 5. Tags
    // ?
    private Tag[] CreateTags(SeedCtx ctx)
    {
        Tag T(string name, string color, string desc)
        {
            var t = new Tag { TagName = name, Color = color, Description = desc };
            SetBase(t, ctx);
            return t;
        }

        return
        [
            T("VIP",            "#FFD700", "High-value customer or prospect"),
            T("Hot Lead",       "#FF4500", "Lead with immediate buying intent"),
            T("Cold",           "#4682B4", "Low activity or long-term prospect"),
            T("Upsell Target",  "#32CD32", "Existing customer with expansion potential"),
            T("At Risk",        "#DC143C", "Customer at churn risk"),
            T("New Logo",       "#9370DB", "Net-new customer acquisition target"),
            T("Renewal",        "#20B2AA", "Upcoming contract renewal"),
            T("Escalated",      "#FF6347", "Escalated issue or support case"),
            T("Strategic",      "#1E90FF", "Strategic account requiring executive attention"),
            T("Partner",        "#FFA500", "Channel partner or reseller"),
            T("Prospect",       "#6A5ACD", "Potential customer in evaluation phase"),
            T("Champion",       "#2E8B57", "Internal champion driving the deal"),
        ];
    }

    // ?
    // 6. Territories
    // ?
    private Territory[] CreateTerritories(SeedCtx ctx)
    {
        Territory T(string name, string desc, Guid? parentId = null)
        {
            var t = new Territory { TerritoryName = name, Description = desc, OwnerId = ctx.UserId, ParentTerritoryId = parentId };
            SetBase(t, ctx);
            return t;
        }

        // Regions (parents)
        var na   = T("North America",  "NA region covering US, Canada, Mexico");
        var emea = T("EMEA",           "Europe, Middle East, Africa region");
        var apac = T("APAC",           "Asia Pacific region");
        var latam= T("LATAM",          "Latin America region");

        // Sub-territories
        return
        [
            na, emea, apac, latam,
            T("US East",      "Eastern United States",             na.Id),
            T("US West",      "Western United States",             na.Id),
            T("Canada",       "Canada territory",                  na.Id),
            T("UK & Ireland", "United Kingdom and Ireland",        emea.Id),
            T("DACH",         "Germany, Austria, Switzerland",     emea.Id),
            T("Nordics",      "Denmark, Norway, Sweden, Finland",  emea.Id),
            T("ANZ",          "Australia and New Zealand",         apac.Id),
            T("SE Asia",      "Southeast Asia",                    apac.Id),
        ];
    }

    // ?
    // 7. Teams
    // ?
    private Team[] CreateTeams(SeedCtx ctx)
    {
        Team T(string name, string desc)
        {
            var t = new Team { TeamName = name, Description = desc, ManagerId = ctx.UserId };
            SetBase(t, ctx);
            return t;
        }

        return
        [
            T("NA Sales Team",          "North America new business sales"),
            T("EMEA Sales Team",        "EMEA new business and expansion"),
            T("Enterprise Sales Team",  "Global enterprise accounts (>1000 employees)"),
            T("SMB Sales Team",         "Small and medium business accounts"),
            T("Customer Success",       "Post-sale customer health and retention"),
            T("Support Team",           "Technical and product support"),
            T("Partner Sales Team",     "Channel and partner-driven revenue"),
        ];
    }

    private TeamMember[] CreateTeamMembers(SeedCtx ctx, Team[] teams)
    {
        TeamMember M(Guid teamId, string role, string accAccess, string oppAccess, string caseAccess)
        {
            var m = new TeamMember
            {
                TeamId                 = teamId,
                UserId                 = ctx.UserId,
                TeamRole               = role,
                AccountAccessLevel     = accAccess,
                OpportunityAccessLevel = oppAccess,
                CaseAccessLevel        = caseAccess
            };
            SetBase(m, ctx);
            return m;
        }

        return
        [
            M(teams[0].Id, "Sales Rep",           "Read/Write", "Read/Write", "Read Only"),
            M(teams[1].Id, "Sales Rep",           "Read/Write", "Read/Write", "Read Only"),
            M(teams[2].Id, "Enterprise AE",       "Read/Write", "Read/Write", "Read/Write"),
            M(teams[3].Id, "SMB Account Manager", "Read/Write", "Read/Write", "Read Only"),
            M(teams[4].Id, "CSM",                 "Read/Write", "Read Only",  "Read/Write"),
            M(teams[5].Id, "Support Engineer",    "Read Only",  "Read Only",  "Read/Write"),
            M(teams[6].Id, "Partner Manager",     "Read/Write", "Read/Write", "Read Only"),
        ];
    }

    // ?
    // 8. Accounts
    // ?
    private Account[] CreateAccounts(SeedCtx ctx)
    {
        Account A(string name, string type, string industry, string phone, string website,
                  string street, string city, string state, string zip, string country,
                  string? desc = null)
        {
            var a = new Account
            {
                AccountName     = name,   Type     = type,    Industry         = industry,
                Phone           = phone,  Website  = website,
                BillingStreet   = street, BillingCity = city, BillingState     = state,
                BillingPostalCode = zip,  BillingCountry = country,
                OwnerId         = ctx.UserId,
                Description     = desc
            };
            SetBase(a, ctx);
            return a;
        }

        return
        [
            A("Apex Technologies",       "Customer",  "Technology",         "+1-415-555-0100", "https://apextechnologies.com",   "101 Market St",      "San Francisco", "CA", "94105", "USA",     "Mid-market software company"),
            A("Global Dynamics Corp",    "Customer",  "Manufacturing",      "+1-312-555-0200", "https://globaldynamics.com",     "233 S Wacker Dr",    "Chicago",       "IL", "60606", "USA",     "Large manufacturing conglomerate"),
            A("Northwind Trading",       "Prospect",  "Retail",             "+1-212-555-0300", "https://northwindtrading.com",   "1 World Trade Ctr",  "New York",      "NY", "10007", "USA",     "Regional retail chain"),
            A("Sunrise Healthcare",      "Customer",  "Healthcare",         "+1-617-555-0400", "https://sunrisehealthcare.com",  "25 Shattuck Sq",     "Boston",        "MA", "02115", "USA",     "Healthcare services provider"),
            A("Pacific Ventures",        "Partner",   "Financial Services", "+1-206-555-0500", "https://pacificventures.com",    "701 5th Ave",        "Seattle",       "WA", "98104", "USA",     "VC-backed fintech firm"),
            A("BlueRidge Energy",        "Prospect",  "Energy",             "+1-713-555-0600", "https://blueridgeenergy.com",    "1600 Smith St",      "Houston",       "TX", "77002", "USA",     "Renewable energy company"),
            A("SkyLine Logistics",       "Customer",  "Transportation",     "+1-404-555-0700", "https://skylinelogistics.com",   "191 Peachtree St",   "Atlanta",       "GA", "30303", "USA",     "Logistics and freight solutions"),
            A("Pinnacle Consulting",     "Customer",  "Consulting",         "+44-20-7946-0800","https://pinnacleconsulting.co.uk","1 Canada Square",    "London",        "",   "E14 5AB","UK",     "Management consulting firm"),
            A("TechNord GmbH",           "Prospect",  "Technology",         "+49-89-555-0900", "https://technord.de",            "Leopoldstrasse 10",  "Munich",        "",   "80802",  "Germany", "German SaaS startup"),
            A("Solaris Manufacturing",   "Customer",  "Manufacturing",      "+61-2-5550-1000", "https://solarismfg.com.au",      "1 Martin Place",     "Sydney",        "NSW","2000",   "Australia","Precision manufacturing"),
            A("Crest Financial Group",   "Investor",  "Financial Services", "+1-646-555-1100", "https://crestfinancial.com",     "400 Park Ave",       "New York",      "NY", "10022", "USA"),
            A("Meteor Retail Partners",  "Reseller",  "Retail",             "+1-480-555-1200", "https://meteorretail.com",       "2111 E Highland Ave","Phoenix",       "AZ", "85016", "USA",     "National retail distributor"),
            A("Cascade Analytics",       "Prospect",  "Technology",         "+1-503-555-1300", "https://cascadeanalytics.com",   "1 SW Columbia St",   "Portland",      "OR", "97258", "USA",     "Data analytics startup"),
            A("Harbor View Pharma",      "Customer",  "Pharmaceuticals",    "+1-858-555-1400", "https://harborviewpharma.com",   "3020 Callan Rd",     "San Diego",     "CA", "92121", "USA",     "Biotech and pharma company"),
            A("Ironclad Defense",        "Customer",  "Defense",            "+1-571-555-1500", "https://ironcladadefense.com",   "8300 Greensboro Dr", "McLean",        "VA", "22102", "USA"),
        ];
    }

    // ?
    // 9. Territory Accounts
    // ?
    private TerritoryAccount[] CreateTerritoryAccounts(SeedCtx ctx, Territory[] territories, Account[] accounts)
    {
        TerritoryAccount TA(Guid terrId, Guid accId)
        {
            var ta = new TerritoryAccount { TerritoryId = terrId, AccountId = accId };
            SetBase(ta, ctx);
            return ta;
        }

        var usEast = territories.First(t => t.TerritoryName == "US East");
        var usWest = territories.First(t => t.TerritoryName == "US West");
        var uk     = territories.First(t => t.TerritoryName == "UK & Ireland");
        var dach   = territories.First(t => t.TerritoryName == "DACH");
        var anz    = territories.First(t => t.TerritoryName == "ANZ");

        return
        [
            TA(usWest.Id, accounts[0].Id),
            TA(usEast.Id, accounts[1].Id),
            TA(usEast.Id, accounts[2].Id),
            TA(usEast.Id, accounts[3].Id),
            TA(usWest.Id, accounts[4].Id),
            TA(usEast.Id, accounts[6].Id),
            TA(uk.Id,     accounts[7].Id),
            TA(dach.Id,   accounts[8].Id),
            TA(anz.Id,    accounts[9].Id),
        ];
    }

    // ?
    // 10. Contacts
    // ?
    private Contact[] CreateContacts(SeedCtx ctx, Account[] accounts)
    {
        Contact C(string first, string last, string? title, string email, string phone,
                  Guid accountId, string? desc = null)
        {
            var c = new Contact
            {
                Salutation  = "Mr.",
                FirstName   = first,   LastName  = last,
                Title       = title,   Email     = email,   Phone = phone,
                AccountId   = accountId,
                OwnerId     = ctx.UserId,
                Description = desc,
                EmailOptOut = false
            };
            SetBase(c, ctx);
            return c;
        }

        // Default anonymous walk-in customer — the contact POS uses for counter sales with no
        // named customer. Not tied to a business Account. CustomerType.WalkIn + IsAnonymous.
        var walkIn = new Contact
        {
            FirstName      = "Walk-in",
            LastName       = "Customer",
            FullName       = "Walk-in Customer",
            ShortName      = "Walk-in",
            CustomerType   = CustomerType.WalkIn,
            CustomerNumber = "CUS-000000",
            IsAnonymous    = true,
            IsActive       = true,
            EmailOptOut    = true,
            OwnerId        = ctx.UserId,
            Description    = "Default anonymous walk-in customer for POS / counter sales",
        };
        SetBase(walkIn, ctx);

        return
        [
            walkIn,
            C("James",   "Carter",    "VP of Sales",            "j.carter@apextechnologies.com",     "+1-415-555-0111", accounts[0].Id,  "Primary contact at Apex"),
            C("Sarah",   "Mitchell",  "Director of IT",         "s.mitchell@apextechnologies.com",   "+1-415-555-0112", accounts[0].Id),
            C("Robert",  "Thompson",  "CEO",                    "r.thompson@globaldynamics.com",     "+1-312-555-0211", accounts[1].Id,  "Executive sponsor"),
            C("Linda",   "Nguyen",    "CFO",                    "l.nguyen@globaldynamics.com",       "+1-312-555-0212", accounts[1].Id),
            C("David",   "Anderson",  "CTO",                    "d.anderson@sunrisehealthcare.com",  "+1-617-555-0411", accounts[3].Id),
            C("Emily",   "Johnson",   "VP of Operations",       "e.johnson@sunrisehealthcare.com",   "+1-617-555-0412", accounts[3].Id),
            C("Michael", "Williams",  "Director of Finance",    "m.williams@pacificventures.com",    "+1-206-555-0511", accounts[4].Id),
            C("Amanda",  "Brown",     "Head of Procurement",    "a.brown@skylinelogistics.com",      "+1-404-555-0711", accounts[6].Id),
            C("Thomas",  "Evans",     "Managing Director",      "t.evans@pinnacleconsulting.co.uk",  "+44-20-7946-0811",accounts[7].Id,  "UK executive contact"),
            C("Sophie",  "Clarke",    "Head of Technology",     "s.clarke@pinnacleconsulting.co.uk", "+44-20-7946-0812",accounts[7].Id),
            C("Liam",    "Harrison",  "Operations Manager",     "l.harrison@solarismfg.com.au",      "+61-2-5550-1011", accounts[9].Id),
            C("Natalie", "King",      "IT Director",            "n.king@cascadeanalytics.com",       "+1-503-555-1311", accounts[12].Id),
            C("Daniel",  "Patel",     "SVP Research",           "d.patel@harborviewpharma.com",      "+1-858-555-1411", accounts[13].Id),
            C("Jessica", "Lee",       "VP Business Development","j.lee@harborviewpharma.com",        "+1-858-555-1412", accounts[13].Id),
            C("Marcus",  "Stone",     "Director of Sales",      "m.stone@apextechnologies.com",      "+1-415-555-0113", accounts[0].Id),
        ];
    }

    // ?
    // 11. Leads
    // ?
    private Lead[] CreateLeads(SeedCtx ctx)
    {
        Lead L(string first, string last, string company, string title,
               string email, string phone, string status, string source,
               string industry, string? desc = null)
        {
            var l = new Lead
            {
                Salutation  = "Mr.",
                FirstName   = first,   LastName   = last,
                Company     = company, Title      = title,
                Email       = email,   Phone      = phone,
                Status      = status,  LeadSource = source,
                Industry    = industry,
                OwnerId     = ctx.UserId,
                Description = desc
            };
            SetBase(l, ctx);
            return l;
        }

        return
        [
            L("Alex",     "Turner",    "InnovateTech Ltd",       "CTO",                    "a.turner@innovatetech.com",       "+1-408-555-2001", "New",       "Web",                "Technology",        "Filled demo form on website"),
            L("Maria",    "Santos",    "Santos Retail Group",    "CEO",                    "m.santos@santosretail.com",       "+1-305-555-2002", "Contacted", "Trade Show",         "Retail",            "Met at NRF Expo"),
            L("Kevin",    "O'Brien",   "O'Brien Manufacturing",  "VP Manufacturing",       "k.obrien@obrienmanufacturing.com","+1-216-555-2003", "Nurturing", "Partner",            "Manufacturing",     "Referred by Pacific Ventures"),
            L("Priya",    "Sharma",    "Sharma Financial",       "Head of IT",             "p.sharma@sharmafinancial.in",     "+91-22-5550-2004","New",       "Email",              "Financial Services","Newsletter subscriber"),
            L("Carlos",   "Mendez",    "Mendez Logistics",       "Operations Director",    "c.mendez@mendezlogistics.mx",     "+52-55-5550-2005","Contacted", "Referral",           "Transportation",    "Referred by SkyLine"),
            L("Yuki",     "Tanaka",    "Tanaka Electronics",     "Purchasing Manager",     "y.tanaka@tanakaelec.co.jp",       "+81-3-5550-2006", "New",       "Web",                "Technology"),
            L("Emma",     "Wilson",    "Wilson Pharma",          "VP Operations",          "e.wilson@wilsonpharma.com",       "+1-617-555-2007", "Nurturing", "Seminar - Internal", "Pharmaceuticals",   "Attended webinar"),
            L("Jack",     "Nguyen",    "Saigon Software",        "CEO",                    "j.nguyen@saigonsoftware.vn",      "+84-28-5550-2008","New",       "Web",                "Technology",        "Trial sign-up"),
            L("Olivia",   "Garcia",    "Garcia Energy Group",    "CFO",                    "o.garcia@garciaenergy.com",       "+1-512-555-2009", "Contacted", "Conference",         "Energy",            "Connected at Energy Summit"),
            L("Noah",     "Patel",     "Patel Construction",     "IT Manager",             "n.patel@patelconstruction.com",   "+1-303-555-2010", "New",       "Advertisement",      "Construction"),
            L("Isabella", "Rossi",     "Rossi Consulting SpA",   "Partner",                "i.rossi@rossiconsulting.it",      "+39-02-5550-2011","New",       "Word of mouth",      "Consulting",        "Heard from peer in Italy"),
            L("Ethan",    "Chen",      "Chen Biotech",           "Research Director",      "e.chen@chenbiotech.com",          "+1-650-555-2012", "Nurturing", "Public Relations",   "Biotechnology",     "PR article mention"),
        ];
    }

    // ?
    // 12. Deals
    // ?
    private Deal[] CreateDeals(SeedCtx ctx, Account[] accounts, Pipeline[] pipelines, PipelineStage[] stages)
    {
        PipelineStage Stage(Guid pipelineId, string name) =>
            stages.First(s => s.PipelineId == pipelineId && s.StageName == name);

        Deal D(string name, Guid accountId, Guid pipelineId, string stageName,
               decimal amount, DateTime closeDate, string? forecast = null, string? desc = null)
        {
            var stage = Stage(pipelineId, stageName);
            var d = new Deal
            {
                OpportunityName  = name,
                AccountId        = accountId,
                PipelineId       = pipelineId,
                PipelineStageId  = stage.Id,
                Stage            = stageName,
                Amount           = amount,
                CloseDate        = closeDate,
                ForecastCategory = forecast ?? stage.ForecastCategory,
                OwnerId          = ctx.UserId,
                Description      = desc
            };
            SetBase(d, ctx);
            return d;
        }

        var now  = ctx.Now;
        var sp   = pipelines[0].Id;
        var ep   = pipelines[1].Id;
        var smb  = pipelines[2].Id;
        var ren  = pipelines[3].Id;
        var par  = pipelines[4].Id;

        return
        [
            D("Apex CRM Enterprise Roll-out",    accounts[0].Id,  sp,  "Proposal / Price Quote", 48000m,  now.AddMonths(1),  "Commit",   "Full CRM roll-out for 100 users"),
            D("Global Dynamics ERP Deal",        accounts[1].Id,  ep,  "POC / Pilot",            180000m, now.AddMonths(3),  "BestCase", "ERP core + manufacturing + inventory"),
            D("Northwind CRM Starter",           accounts[2].Id,  smb, "Demo",                   9600m,   now.AddMonths(1),  "BestCase", "15-seat CRM starter pilot"),
            D("Sunrise EHR Integration",         accounts[3].Id,  sp,  "Negotiation / Review",   72000m,  now.AddMonths(2),  "Commit"),
            D("Pacific Fintech Platform",        accounts[4].Id,  ep,  "Discovery Call",         250000m, now.AddMonths(6),  "Pipeline", "Multi-module ERP for fintech"),
            D("BlueRidge Energy CRM",            accounts[5].Id,  smb, "Trial",                  15000m,  now.AddMonths(1),  "Commit"),
            D("SkyLine Logistics TMS Add-on",    accounts[6].Id,  sp,  "Closed Won",             38400m,  now.AddDays(-30),  "Closed",   "Won - implementing next quarter"),
            D("Pinnacle Consulting Platform",    accounts[7].Id,  ep,  "Business Case",          125000m, now.AddMonths(4)),
            D("TechNord SaaS Expansion",         accounts[8].Id,  sp,  "Value Proposition",      55000m,  now.AddMonths(2)),
            D("Solaris MFG ERP Expansion",       accounts[9].Id,  ren, "Renewal Due",            98000m,  now.AddMonths(2),  "Commit",   "Annual renewal + seat expansion"),
            D("Cascade Analytics BI Module",     accounts[12].Id, smb, "New Lead",               22000m,  now.AddMonths(3)),
            D("Harbor View Pharma CRM",          accounts[13].Id, ep,  "Initial Outreach",       310000m, now.AddMonths(5),  "Pipeline", "Strategic pharma CRM deal"),
            D("Crest Financial Group ERP",       accounts[10].Id, ep,  "Contract Review",        410000m, now.AddDays(15),   "Commit",   "Nearly closed - final legal review"),
            D("Meteor Retail Partner Resell",    accounts[11].Id, par, "Joint Proposal",         65000m,  now.AddMonths(2),  "Commit",   "Partner-sourced resell opportunity"),
            D("Apex Mobile App Expansion",       accounts[0].Id,  ren, "Upsell Review",          18000m,  now.AddMonths(1),  "Commit"),
        ];
    }

    // ?
    // 13. Campaigns
    // ?
    private Campaign[] CreateCampaigns(SeedCtx ctx)
    {
        Campaign C(string name, string type, string status,
                   DateTime start, DateTime end, decimal budget, decimal expRevenue, string? desc = null)
        {
            var c = new Campaign
            {
                CampaignName     = name,  Type   = type,    Status  = status,
                Active           = status == "In Progress",
                StartDate        = start, EndDate = end,
                BudgetedCost     = budget, ExpectedRevenue = expRevenue,
                OwnerId          = ctx.UserId, Description = desc
            };
            SetBase(c, ctx);
            return c;
        }

        var now = ctx.Now;

        return
        [
            C("Q1 SaaS Lead Gen",          "Email",           "Completed",   now.AddMonths(-4), now.AddMonths(-2), 5000m,  80000m,  "Email nurture for SaaS prospects"),
            C("Trade Show NRF 2025",       "Trade Show",      "Completed",   now.AddMonths(-3), now.AddMonths(-3), 20000m, 200000m, "NRF Retail Conference presence"),
            C("Q2 Webinar: CRM ROI",       "Webinar",         "In Progress", now.AddDays(-14),  now.AddDays(30),   3000m,  60000m,  "Mid-funnel webinar for prospects"),
            C("Summer Partner Promo",      "Advertisement",   "In Progress", now.AddDays(-7),   now.AddMonths(2),  8000m,  120000m, "Partner co-marketing campaign"),
            C("Enterprise Outbound Q3",    "Direct Mail",     "Planned",     now.AddDays(7),    now.AddMonths(3),  15000m, 500000m, "Direct outbound to enterprise list"),
            C("EMEA LinkedIn Campaign",    "Banner Ads",      "Planned",     now.AddDays(14),   now.AddMonths(2),  6000m,  90000m),
            C("Customer Referral Program", "Referral Program","In Progress", now.AddMonths(-1), now.AddMonths(2),  2000m,  150000m, "Existing customer referral incentive"),
            C("Healthcare Vertical Push",  "Email",           "Planned",     now.AddDays(21),   now.AddMonths(3),  7500m,  180000m, "Vertical campaign for healthcare"),
        ];
    }

    private CampaignMember[] CreateCampaignMembers(SeedCtx ctx, Campaign[] campaigns,
                                                    Lead[] leads, Contact[] contacts)
    {
        var members = new List<CampaignMember>();

        void Add(Guid campaignId, Guid? leadId, Guid? contactId, string status)
        {
            var m = new CampaignMember
            {
                CampaignId          = campaignId,
                LeadId              = leadId,
                ContactId           = contactId,
                Status              = status,
                FirstRespondedDate  = status == "Responded" ? ctx.Now.AddDays(-1) : null
            };
            SetBase(m, ctx);
            members.Add(m);
        }

        // Q1 Lead Gen - leads
        Add(campaigns[0].Id, leads[0].Id, null,         "Responded");
        Add(campaigns[0].Id, leads[2].Id, null,         "Sent");
        Add(campaigns[0].Id, leads[6].Id, null,         "Responded");

        // Trade Show - contacts
        Add(campaigns[1].Id, null, contacts[0].Id,      "Responded");
        Add(campaigns[1].Id, null, contacts[2].Id,      "Sent");
        Add(campaigns[1].Id, leads[1].Id, null,         "Responded");

        // Webinar
        Add(campaigns[2].Id, leads[4].Id,  null,        "Responded");
        Add(campaigns[2].Id, null, contacts[4].Id,      "Sent");
        Add(campaigns[2].Id, null, contacts[8].Id,      "Responded");
        Add(campaigns[2].Id, leads[10].Id, null,        "Sent");

        // Partner Promo
        Add(campaigns[3].Id, null, contacts[6].Id,      "Responded");
        Add(campaigns[3].Id, null, contacts[14].Id,     "Sent");

        return [.. members];
    }

    // ?
    // 14. Contact Lists
    // ?
    private ContactList[] CreateContactLists(SeedCtx ctx)
    {
        ContactList L(string name, string desc, bool isDynamic)
        {
            var l = new ContactList { ListName = name, Description = desc, IsDynamic = isDynamic, OwnerId = ctx.UserId };
            SetBase(l, ctx);
            return l;
        }

        return
        [
            L("All Prospects",         "Every active prospect in the system",        true),
            L("Enterprise Targets",    "Accounts with >500 employees",               true),
            L("Healthcare Segment",    "Contacts in healthcare industry",            true),
            L("Trade Show NRF Leads",  "Leads captured at NRF Trade Show",          false),
            L("Webinar Attendees Q2",  "Registered attendees from Q2 CRM webinar",  false),
            L("EMEA Newsletter",       "EMEA region newsletter subscribers",         false),
            L("Renewal Due 90 Days",   "Customers with renewals due in 90 days",    true),
        ];
    }

    // ?
    // 15. Entitlements
    // ?
    private Entitlement[] CreateEntitlements(SeedCtx ctx, Account[] accounts, Contact[] contacts)
    {
        Entitlement E(string name, Guid accountId, Guid? contactId, string level,
                      string type, int? maxCases, DateTime start, DateTime end)
        {
            var e = new Entitlement
            {
                EntitlementName    = name,
                AccountId          = accountId,
                ContactId          = contactId,
                ServiceLevelName   = level,
                Type               = type,
                CasesPerEntitlement = maxCases,
                StartDate          = start,
                EndDate            = end,
                IsActive           = true
            };
            SetBase(e, ctx);
            return e;
        }

        var now = ctx.Now;

        return
        [
            E("Apex Gold Support",        accounts[0].Id,  contacts[0].Id,  "Gold",     "Phone", 50,   now.AddYears(-1),  now.AddYears(1)),
            E("Global Dynamics Platinum", accounts[1].Id,  contacts[2].Id,  "Platinum", "Phone", null, now.AddYears(-1),  now.AddYears(1)),
            E("Sunrise Silver Support",   accounts[3].Id,  contacts[4].Id,  "Silver",   "Email", 30,   now.AddMonths(-6), now.AddMonths(6)),
            E("SkyLine Bronze Support",   accounts[6].Id,  contacts[7].Id,  "Bronze",   "Email", 20,   now.AddMonths(-3), now.AddMonths(9)),
            E("Pinnacle Gold Support",    accounts[7].Id,  contacts[8].Id,  "Gold",     "Phone", 40,   now.AddYears(-1),  now.AddYears(1)),
            E("Solaris Platinum Support", accounts[9].Id,  contacts[10].Id, "Platinum", "Phone", null, now.AddYears(-1),  now.AddYears(1)),
            E("Harbor View Gold",         accounts[13].Id, contacts[12].Id, "Gold",     "Phone", 60,   now.AddMonths(-2), now.AddMonths(10)),
        ];
    }

    // ?
    // 16. Cases
    // ?
    private Case[] CreateCases(SeedCtx ctx, Account[] accounts, Contact[] contacts, Entitlement[] entitlements)
    {
        int caseNum = 1000;
        Case C(string subject, string status, string priority, string? origin,
               Guid accountId, Guid? contactId, Guid? entitlementId, string? desc = null)
        {
            var c = new Case
            {
                CaseNumber    = $"CS-{++caseNum}",
                Subject       = subject,
                Status        = status,
                Priority      = priority,
                CaseOrigin    = origin,
                AccountId     = accountId,
                ContactId     = contactId,
                EntitlementId = entitlementId,
                OwnerId       = ctx.UserId,
                Description   = desc
            };
            SetBase(c, ctx);
            return c;
        }

        return
        [
            C("CRM dashboard not loading",                "Working",             "High",     "Web",   accounts[0].Id,  contacts[0].Id,  entitlements[0].Id, "User reports blank screen after login"),
            C("Data import failing for 10k records",      "Waiting on Customer", "Medium",   "Email", accounts[0].Id,  contacts[1].Id,  entitlements[0].Id, "CSV import returns error on row 5432"),
            C("ERP GL posting errors on month-end close", "New",                 "Critical", "Phone", accounts[1].Id,  contacts[2].Id,  entitlements[1].Id, "Month-end GL batch failing"),
            C("Integration timeout with SAP",             "Working",             "High",     "Phone", accounts[1].Id,  contacts[3].Id,  entitlements[1].Id),
            C("HIPAA compliance report missing fields",   "Escalated",           "Critical", "Email", accounts[3].Id,  contacts[4].Id,  entitlements[2].Id, "Regulatory audit requirement"),
            C("Password reset not sending email",         "Closed",              "Low",      "Web",   accounts[3].Id,  contacts[5].Id,  entitlements[2].Id, "Fixed - SMTP relay config updated"),
            C("Mobile app crashes on iOS 17",             "New",                 "High",     "Web",   accounts[6].Id,  contacts[7].Id,  entitlements[3].Id),
            C("Slow report generation > 5 minutes",       "Working",             "Medium",   "Email", accounts[7].Id,  contacts[8].Id,  entitlements[4].Id),
            C("API rate limit exceeded in production",    "New",                 "High",     "Web",   accounts[7].Id,  contacts[9].Id,  entitlements[4].Id),
            C("Inventory sync lag from warehouse WMS",    "Waiting on Customer", "Medium",   "Email", accounts[9].Id,  contacts[10].Id, entitlements[5].Id),
            C("Custom field not saving on Contact form",  "Closed",              "Low",      "Web",   accounts[12].Id, contacts[11].Id, null,               "Workaround provided"),
            C("Batch email job stuck in queue",           "New",                 "High",     "Phone", accounts[13].Id, contacts[12].Id, entitlements[6].Id),
        ];
    }

    // ?
    // 17. Knowledge Articles
    // ?
    private KnowledgeArticle[] CreateKnowledgeArticles(SeedCtx ctx)
    {
        int artNum = 1000;
        KnowledgeArticle A(string title, string type, string category,
                           ArticleStatus status, string summary, string body)
        {
            var a = new KnowledgeArticle
            {
                ArticleNumber   = $"KB-{++artNum}",
                Title           = title,
                UrlName         = title.ToLower().Replace(" ", "-").Replace("/", ""),
                ArticleType     = type,
                CategoryGroup   = category,
                Status          = status,
                Summary         = summary,
                Body            = body,
                IsVisibleInApp  = true,
                IsVisibleInCsp  = status == ArticleStatus.Published,
                IsVisibleInPkb  = false,
                PublishedDate   = status == ArticleStatus.Published ? ctx.Now.AddDays(-30) : null,
                VersionNumber   = 1,
                OwnerId         = ctx.UserId
            };
            SetBase(a, ctx);
            return a;
        }

        return
        [
            A("How to Import Contacts via CSV",
              "How-To", "Data Management", ArticleStatus.Published,
              "Step-by-step guide to bulk import contacts using CSV upload.",
              "1. Navigate to Contacts ? Import.\n2. Download the CSV template.\n3. Fill in required fields (LastName, Email).\n4. Upload and map columns.\n5. Review and confirm the import."),

            A("Configuring SMTP for Email Notifications",
              "How-To", "Configuration", ArticleStatus.Published,
              "Configure outbound SMTP relay for system email notifications.",
              "Go to Admin ? Email Settings ? SMTP. Enter host, port, credentials and test the connection."),

            A("Understanding CRM Pipelines",
              "FAQ", "Sales", ArticleStatus.Published,
              "Explains how pipelines and stages work in the Crm.",
              "A pipeline represents a sales process. Each stage has a probability that drives forecasting. Move deals forward by drag-and-drop or editing the Stage field."),

            A("GL Month-End Close Checklist",
              "How-To", "Accounting", ArticleStatus.Published,
              "Steps to successfully close an accounting period.",
              "1. Reconcile bank accounts.\n2. Review open AR/AP.\n3. Post depreciation journal.\n4. Run trial balance.\n5. Lock the period in Fiscal Calendar settings."),

            A("API Rate Limits and Best Practices",
              "FAQ", "Integrations", ArticleStatus.Published,
              "Understand and work within API rate limits to avoid throttling.",
              "Default limits: 1000 calls/min per tenant. Use bulk endpoints for mass operations. Implement exponential backoff on 429 responses."),

            A("Resetting User Passwords as Admin",
              "How-To", "User Management", ArticleStatus.Published,
              "Administrators can reset any user's password from the Users admin panel.",
              "Navigate to Admin ? Users ? Select user ? Reset Password. An email with a reset link is sent automatically."),

            A("CRM Mobile App Troubleshooting",
              "Known Issue", "Mobile", ArticleStatus.Published,
              "Known issues and workarounds for the CRM mobile application.",
              "Issue: App crashes on iOS 17.x - Workaround: Enable Compatibility Mode in device Settings ? CRM App. Fix scheduled for v2.4.1."),

            A("Setting Up Custom Fields",
              "How-To", "Configuration", ArticleStatus.Published,
              "Add custom fields to any CRM entity to capture business-specific data.",
              "Admin ? Object Manager ? Select Entity ? Fields ? New Field. Supports Text, Number, Date, Picklist and Lookup types."),

            A("HIPAA Compliance Configuration Guide",
              "Policy", "Compliance", ArticleStatus.Published,
              "Required configuration steps to enable HIPAA-compliant data handling.",
              "1. Enable field-level encryption for PII fields.\n2. Configure audit logging.\n3. Set data retention policies.\n4. Enable IP-based access restriction."),

            A("Inventory Sync with External WMS",
              "How-To", "Integrations", ArticleStatus.Draft,
              "Configure real-time inventory synchronization with warehouse management systems.",
              "Use the Inventory Sync API endpoint. Requires WebHook registration. Sync frequency: configurable 1-60 minutes."),

            A("Understanding Entitlements and SLA Tracking",
              "FAQ", "Service", ArticleStatus.Published,
              "How entitlements control case access and SLA enforcement.",
              "Create an entitlement linked to an account. Assign it to cases. The system tracks case count against CasesPerEntitlement and alerts when nearing the limit."),

            A("Forecast Categories Explained",
              "FAQ", "Sales", ArticleStatus.Published,
              "A guide to CRM forecast categories and how they roll up to revenue forecasts.",
              "Pipeline: speculative. BestCase: likely to close. Commit: very likely. Closed: won/lost. Each stage maps to a category. Managers can override at the deal level."),
        ];
    }

    // ?
    // 18. Activities
    // ?
    private Activity[] CreateActivities(SeedCtx ctx, Account[] accounts, Contact[] contacts,
                                        Lead[] leads, Deal[] deals, Case[] cases)
    {
        var now = ctx.Now;

        Activity A(string type, string subject,
                   Guid? relatedToId, string? relatedToType,
                   Guid? nameId,      string? nameType,
                   string status,     string priority,
                   DateTime? due,     int? durationMins, string? callType,
                   string? desc = null)
        {
            var a = new Activity
            {
                Type          = type,          Subject      = subject,
                RelatedToId   = relatedToId,   RelatedToType = relatedToType,
                NameId        = nameId,        NameType      = nameType,
                Status        = status,        Priority      = priority,
                DueDate       = due,           DurationMinutes = durationMins,
                CallType      = callType,
                Description   = desc,
                AssignedToId  = ctx.UserId
            };
            SetBase(a, ctx);
            return a;
        }

        return
        [
            // Calls
            A("Call",  "Discovery call - Apex CRM requirements",       deals[0].Id,  "Deal",    contacts[0].Id,  "Contact", "Completed",   "Normal", now.AddDays(-10), 45,   "Outbound", "Reviewed requirements, confirmed 100-user rollout timeline"),
            A("Call",  "Follow-up call - Global Dynamics ERP scope",   deals[1].Id,  "Deal",    contacts[2].Id,  "Contact", "Completed",   "High",   now.AddDays(-7),  60,   "Outbound", "Deep-dive on ERP scope and integration requirements"),
            A("Call",  "Intro call - TechNord SaaS expansion",         deals[8].Id,  "Deal",    null,            null,      "Completed",   "Normal", now.AddDays(-5),  30,   "Outbound", "Cold outreach, connected with CTO"),
            A("Call",  "Support escalation - Sunrise HIPAA case",      cases[4].Id,  "Case",    contacts[4].Id,  "Contact", "Completed",   "High",   now.AddDays(-3),  30,   "Inbound",  "Customer escalated to management"),
            A("Call",  "Renewal discussion - Solaris MFG",             deals[9].Id,  "Deal",    contacts[10].Id, "Contact", "Completed",   "Normal", now.AddDays(-2),  20,   "Outbound", "Agreed to pricing, sending contract"),
            A("Call",  "Inbound inquiry - Cascade Analytics",          leads[0].Id,  "Lead",    leads[0].Id,     "Lead",    "Completed",   "Normal", now.AddDays(-1),  15,   "Inbound",  "Lead called about demo request"),

            // Tasks
            A("Task",  "Send proposal to Apex Technologies",           deals[0].Id,  "Deal",    contacts[0].Id,  "Contact", "In Progress", "High",   now.AddDays(1),   null, null, "Prepare SOW and pricing for Apex"),
            A("Task",  "Prepare ERP demo for Global Dynamics",         deals[1].Id,  "Deal",    contacts[2].Id,  "Contact", "Not Started", "High",   now.AddDays(3),   null, null, "Custom manufacturing demo environment"),
            A("Task",  "Send contract to Crest Financial",             deals[12].Id, "Deal",    null,            null,      "In Progress", "High",   now.AddDays(1),   null, null, "Final MSA to legal team"),
            A("Task",  "Update HIPAA compliance doc - Sunrise",        cases[4].Id,  "Case",    contacts[4].Id,  "Contact", "Not Started", "High",   now.AddDays(2),   null, null),
            A("Task",  "Follow up with TechNord after demo",           deals[8].Id,  "Deal",    null,            null,      "Not Started", "Normal", now.AddDays(7),   null, null),
            A("Task",  "Qualify lead - Priya Sharma",                  leads[3].Id,  "Lead",    leads[3].Id,     "Lead",    "Not Started", "Normal", now.AddDays(2),   null, null, "Review LinkedIn, send qualifying email"),
            A("Task",  "Onboarding call - SkyLine post-win",           accounts[6].Id,"Account", contacts[7].Id, "Contact", "Completed",   "Normal", now.AddDays(-14), null, null, "Won deal - schedule implementation kickoff"),

            // Events / Meetings
            A("Event", "CRM Demo - Northwind Trading",                 deals[2].Id,  "Deal",    null,            null,      "Completed",   "Normal", now.AddDays(-8),  60,  null, "Product demo, positive reception"),
            A("Event", "QBR - Apex Technologies",                      accounts[0].Id,"Account", contacts[0].Id, "Contact", "Completed",   "High",   now.AddDays(-30), 90,  null, "Quarterly business review"),
            A("Event", "Kickoff meeting - Harbor View Pharma",         deals[11].Id, "Deal",    contacts[12].Id, "Contact", "Completed",   "High",   now.AddDays(-4),  60,  null, "Initial discovery and stakeholder alignment"),
            A("Event", "Partner planning session - Meteor Retail",     deals[13].Id, "Deal",    null,            null,      "Not Started", "Normal", now.AddDays(5),   60,  null, "Joint go-to-market planning"),
            A("Event", "Webinar follow-up - Emma Wilson",              leads[6].Id,  "Lead",    leads[6].Id,     "Lead",    "Not Started", "Normal", now.AddDays(4),   30,  null, "1:1 follow up after CRM ROI webinar"),
        ];
    }

    // ?
    // 19. Notes
    // ?
    private Note[] CreateNotes(SeedCtx ctx, Account[] accounts, Contact[] contacts,
                               Lead[] leads, Deal[] deals, Case[] cases)
    {
        Note N(string title, string body, Guid parentId, string parentType)
        {
            var n = new Note { Title = title, Body = body, ParentId = parentId, ParentType = parentType };
            SetBase(n, ctx);
            return n;
        }

        return
        [
            // Account notes
            N("Account Strategy",    "Apex is evaluating three CRM vendors. Our main differentiator is native ERP integration. Decision expected by end of quarter.", accounts[0].Id, "Account"),
            N("Exec Relationship",   "CEO Robert Thompson is a strong champion. Was introduced at CFO Summit. Prioritize executive engagement.", accounts[1].Id, "Account"),
            N("Competitive Intel",   "SkyLine is evaluating us against Oracle. Oracle's pricing is 3- higher - strong position. Emphasize implementation speed.", accounts[6].Id, "Account"),
            N("Partner Notes",       "Pinnacle Consulting can act as both customer and implementation partner. Explore co-delivery model.", accounts[7].Id, "Account"),
            N("Renewal Risk",        "Solaris raised concern about WMS integration timeline. CSM to address on next call to de-risk churn.", accounts[9].Id, "Account"),

            // Contact notes
            N("Champion Profile",    "James Carter is the internal champion at Apex. Reports to VP Sales. Budget pre-approved at $50k.", contacts[0].Id, "Contact"),
            N("Technical Gatekeeper","Sarah Mitchell controls all vendor approvals. Needs detailed security review doc before sign-off.", contacts[1].Id, "Contact"),
            N("Decision Maker",      "Robert Thompson signs off on deals > $100k. Last engaged at CXO Summit in Q4.", contacts[2].Id, "Contact"),
            N("Influencer",          "David Anderson is the technical evaluator at Sunrise. Prefers API-first architecture.", contacts[4].Id, "Contact"),

            // Lead notes
            N("Initial Qualification","Alex Turner from InnovateTech filled the demo form - 80-user team, budget confirmed at $80k ARR. High priority.", leads[0].Id, "Lead"),
            N("Trade Show Notes",     "Met Maria Santos at NRF. Currently using Salesforce - unhappy with pricing. Follow up in 2 weeks.", leads[1].Id, "Lead"),
            N("Partner Referral",     "Kevin O'Brien was referred by Pacific Ventures. Looking for manufacturing + CRM combined platform.", leads[2].Id, "Lead"),
            N("Webinar Attendee",     "Emma Wilson attended the CRM ROI webinar and rated it 5 stars. Strong buying signals in chat questions.", leads[6].Id, "Lead"),

            // Deal notes
            N("Deal Status",         "Apex deal in Proposal stage. SOW sent 3 days ago. Follow-up call booked for Friday. Legal review pending.", deals[0].Id, "Deal"),
            N("Scope Confirmed",     "Global Dynamics confirmed full ERP scope: GL, AR, AP, Manufacturing, Inventory. POC to start next month.", deals[1].Id, "Deal"),
            N("Competition",         "Harbor View Pharma also evaluating Salesforce Health Cloud. Our compliance story is stronger - leverage HIPAA guide.", deals[11].Id, "Deal"),
            N("Late Stage",          "Crest Financial at Contract Review. Legal approved MSA. Procurement PO in process - expected close this month.", deals[12].Id, "Deal"),

            // Case notes
            N("Issue Root Cause",    "CRM dashboard blank screen caused by browser caching of old JS bundle. Fix: force-clear cache or deploy cache-busting headers.", cases[0].Id, "Case"),
            N("Escalation Context",  "HIPAA report issue escalated to engineering. Root cause: missing fields in v2 report template. Patch scheduled for next release.", cases[4].Id, "Case"),
            N("Customer Impact",     "API rate limit exceeded during customer's nightly ETL job. Advised customer to switch to batch endpoint. Monitoring ongoing.", cases[8].Id, "Case"),
        ];
    }

    // ?
    // 20. Entity Tags
    // ?
    private EntityTag[] CreateEntityTags(SeedCtx ctx, Tag[] tags, Account[] accounts,
                                         Contact[] contacts, Lead[] leads, Deal[] deals)
    {
        EntityTag ET(string tagName, Guid entityId, string entityType)
        {
            var et = new EntityTag
            {
                TagId      = tags.First(t => t.TagName == tagName).Id,
                EntityId   = entityId,
                EntityType = entityType
            };
            SetBase(et, ctx);
            return et;
        }

        return
        [
            ET("VIP",           accounts[0].Id,  "Account"),
            ET("Strategic",     accounts[0].Id,  "Account"),
            ET("VIP",           accounts[1].Id,  "Account"),
            ET("At Risk",       accounts[9].Id,  "Account"),
            ET("Renewal",       accounts[9].Id,  "Account"),
            ET("Partner",       accounts[4].Id,  "Account"),
            ET("New Logo",      accounts[12].Id, "Account"),

            ET("Champion",      contacts[0].Id,  "Contact"),
            ET("VIP",           contacts[2].Id,  "Contact"),

            ET("Hot Lead",      leads[0].Id,     "Lead"),
            ET("Hot Lead",      leads[1].Id,     "Lead"),
            ET("Cold",          leads[5].Id,     "Lead"),
            ET("Prospect",      leads[8].Id,     "Lead"),

            ET("Upsell Target", deals[0].Id,     "Deal"),
            ET("Strategic",     deals[12].Id,    "Deal"),
        ];
    }

    // ?
    // 21. Sales Targets
    // ?
    private SalesTarget[] CreateSalesTargets(SeedCtx ctx, Team[] teams, Territory[] territories)
    {
        var now  = ctx.Now;
        var year = now.Year;

        SalesTarget T(Guid? teamId, Guid? territoryId, Guid? ownerId,
                      int fiscYear, int? quarter, decimal target, string currency)
        {
            int startMonth = quarter.HasValue ? (quarter.Value - 1) * 3 + 1 : 1;
            int endMonth   = quarter.HasValue ? quarter.Value * 3 : 12;
            var s = new SalesTarget
            {
                TeamId          = teamId,
                TerritoryId     = territoryId,
                UserId          = ownerId,
                FiscalYear      = fiscYear,
                FiscalQuarter   = quarter,
                TargetAmount    = target,
                CurrencyCode    = currency,
                PeriodStartDate = new DateTime(fiscYear, startMonth, 1),
                PeriodEndDate   = new DateTime(fiscYear, endMonth, DateTime.DaysInMonth(fiscYear, endMonth))
            };
            SetBase(s, ctx);
            return s;
        }

        var usEast = territories.First(t => t.TerritoryName == "US East");
        var usWest = territories.First(t => t.TerritoryName == "US West");
        var uk     = territories.First(t => t.TerritoryName == "UK & Ireland");

        return
        [
            // Annual team targets
            T(teams[0].Id, null, null, year, null, 2_000_000m, "USD"),   // NA Sales
            T(teams[1].Id, null, null, year, null, 1_500_000m, "USD"),   // EMEA Sales
            T(teams[2].Id, null, null, year, null, 5_000_000m, "USD"),   // Enterprise
            T(teams[3].Id, null, null, year, null,   800_000m, "USD"),   // SMB

            // Quarterly personal targets
            T(null, null, ctx.UserId, year, 1, 150_000m, "USD"),
            T(null, null, ctx.UserId, year, 2, 175_000m, "USD"),
            T(null, null, ctx.UserId, year, 3, 200_000m, "USD"),
            T(null, null, ctx.UserId, year, 4, 225_000m, "USD"),

            // Territory annual targets
            T(null, usEast.Id, null, year, null, 1_200_000m, "USD"),
            T(null, usWest.Id, null, year, null, 1_000_000m, "USD"),
            T(null, uk.Id,     null, year, null,   600_000m, "GBP"),
        ];
    }

    // ?
    // 22. Forecasts
    // ?
    private Forecast[] CreateForecasts(SeedCtx ctx)
    {
        var year = ctx.Now.Year;

        Forecast F(int fiscYear, int fiscQuarter,
                   decimal pipeline, decimal bestCase, decimal commit, decimal closed, decimal quota)
        {
            int startMonth = (fiscQuarter - 1) * 3 + 1;
            int endMonth   = fiscQuarter * 3;
            var f = new Forecast
            {
                UserId          = ctx.UserId,
                FiscalYear      = fiscYear,
                FiscalQuarter   = fiscQuarter,
                PipelineAmount  = pipeline,
                BestCaseAmount  = bestCase,
                CommitAmount    = commit,
                ClosedAmount    = closed,
                AdjustedAmount  = commit + closed,
                QuotaAmount     = quota,
                CurrencyCode    = "USD",
                PeriodStartDate = new DateTime(fiscYear, startMonth, 1),
                PeriodEndDate   = new DateTime(fiscYear, endMonth, DateTime.DaysInMonth(fiscYear, endMonth))
            };
            SetBase(f, ctx);
            return f;
        }

        return
        [
            F(year, 1, 420_000m, 280_000m, 160_000m,  95_000m, 150_000m),
            F(year, 2, 510_000m, 340_000m, 210_000m, 130_000m, 175_000m),
            F(year, 3, 680_000m, 450_000m, 290_000m,       0m, 200_000m),
            F(year, 4, 750_000m, 510_000m, 320_000m,       0m, 225_000m),
        ];
    }
}
