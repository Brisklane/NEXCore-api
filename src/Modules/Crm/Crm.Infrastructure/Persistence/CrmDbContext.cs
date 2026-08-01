using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Nexcore.SharedKernel.Audit;

namespace Crm.Infrastructure.Persistence;

/// <summary>
/// CRM module database context.
/// Uses 'crm' schema for all CRM-related tables.
/// </summary>
public class CrmDbContext : AuditDbContextBase
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options) { }

    // Audit
    public override DbSet<AuditLogEntry> AuditLogs { get; set; } = null!;

    // Core CRM
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactAddress> ContactAddresses => Set<ContactAddress>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    // Service
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseComment> CaseComments => Set<CaseComment>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<Entitlement> Entitlements => Set<Entitlement>();

    // Marketing
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignMember> CampaignMembers => Set<CampaignMember>();
    public DbSet<ContactList> ContactLists => Set<ContactList>();
    public DbSet<ContactListMember> ContactListMembers => Set<ContactListMember>();

    // Sales
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Pricebook> Pricebooks => Set<Pricebook>();
    public DbSet<PricebookEntry> PricebookEntries => Set<PricebookEntry>();
    public DbSet<DealProduct> DealProducts => Set<DealProduct>();
    public DbSet<DealContact> DealContacts => Set<DealContact>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteLineItem> QuoteLineItems => Set<QuoteLineItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLineItem> OrderLineItems => Set<OrderLineItem>();
    public DbSet<Contract> Contracts => Set<Contract>();

    // Pipeline
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();

    // Engagement
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EntityTag> EntityTags => Set<EntityTag>();

    // Territory & Team
    public DbSet<Territory> Territories => Set<Territory>();
    public DbSet<TerritoryAccount> TerritoryAccounts => Set<TerritoryAccount>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    // Planning
    public DbSet<Forecast> Forecasts => Set<Forecast>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // Money defaults to (18,2); prices and quantities get (18,4) below so no monetary
        // value is left on EF's implicit convention.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("crm");

        // Prices and quantities carry 4 decimals (consistent with the Inventory module).
        foreach (var p in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                                 && (p.Name.Contains("UnitPrice") || p.Name.Contains("ListPrice")
                                     || p.Name.StartsWith("Quantity"))))
        {
            p.SetPrecision(18);
            p.SetScale(4);
        }

        // Account
        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("Accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountName).IsRequired().HasMaxLength(255);
            e.Property(x => x.Website).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Type).HasMaxLength(100);
            e.Property(x => x.Industry).HasMaxLength(100);
            e.HasOne(x => x.ParentAccount).WithMany(x => x.ChildAccounts)
                .HasForeignKey(x => x.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        // Contact
        modelBuilder.Entity<Contact>(e =>
        {
            e.ToTable("Contacts");
            e.HasKey(x => x.Id);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.FullName).HasMaxLength(255);
            e.Property(x => x.ShortName).HasMaxLength(100);
            e.Property(x => x.CustomerNumber).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Mobile).HasMaxLength(50);
            e.Property(x => x.DefaultCurrencyCode).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.DefaultPaymentTerms).HasMaxLength(50);
            e.Property(x => x.CreditLimit).HasPrecision(18, 2);
            e.Property(x => x.OutstandingBalance).HasPrecision(18, 2);
            e.HasOne(x => x.Account).WithMany(x => x.Contacts)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReportsTo).WithMany(x => x.DirectReports)
                .HasForeignKey(x => x.ReportsToId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ConvertedFromLead).WithMany()
                .HasForeignKey(x => x.ConvertedFromLeadId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Addresses).WithOne(a => a.Contact)
                .HasForeignKey(a => a.ContactId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.CustomerNumber })
                .HasFilter("customer_number IS NOT NULL")
                .IsUnique().HasDatabaseName("IX_Contact_Tenant_CustomerNumber");
        });

        // ── Contact Address ───────────────────────────────────────────────────
        modelBuilder.Entity<ContactAddress>(e =>
        {
            e.ToTable("ContactAddresses");
            e.HasKey(x => x.Id);
            e.Property(x => x.AddressType).IsRequired().HasMaxLength(20).HasDefaultValue("Shipping");
            e.Property(x => x.Label).HasMaxLength(100);
            e.Property(x => x.ContactName).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.WhatsApp).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Street).IsRequired().HasMaxLength(200);
            e.Property(x => x.Street2).HasMaxLength(200);
            e.Property(x => x.City).IsRequired().HasMaxLength(100);
            e.Property(x => x.State).HasMaxLength(100);
            e.Property(x => x.PostalCode).IsRequired().HasMaxLength(20);
            e.Property(x => x.Country).IsRequired().HasMaxLength(100);
            // Fast lookup: all addresses for a contact
            e.HasIndex(x => new { x.CompanyId, x.ContactId })
             .HasDatabaseName("IX_ContactAddress_Contact");
            // Enforce one default per contact per tenant
            e.HasIndex(x => new { x.CompanyId, x.ContactId, x.IsDefault })
             .HasFilter("is_default = true")
             .IsUnique()
             .HasDatabaseName("IX_ContactAddress_Contact_Default");
        });

        // ── Lead ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Lead>(e =>
        {
            e.ToTable("Leads");
            e.HasKey(x => x.Id);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Company).IsRequired().HasMaxLength(255);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Status).IsRequired().HasMaxLength(50);
            e.Property(x => x.AnnualRevenue).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.ConvertedAccount).WithMany()
                .HasForeignKey(x => x.ConvertedAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ConvertedContact).WithMany()
                .HasForeignKey(x => x.ConvertedContactId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ConvertedDeal).WithMany()
                .HasForeignKey(x => x.ConvertedDealId).OnDelete(DeleteBehavior.Restrict);
        });

        // Deal
        modelBuilder.Entity<Deal>(e =>
        {
            e.ToTable("Deals");
            e.HasKey(x => x.Id);
            e.Property(x => x.OpportunityName).IsRequired().HasMaxLength(255);
            e.Property(x => x.Stage).IsRequired().HasMaxLength(100);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Account).WithMany(x => x.Deals)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Pipeline).WithMany(x => x.Deals)
                .HasForeignKey(x => x.PipelineId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PipelineStage).WithMany(x => x.Deals)
                .HasForeignKey(x => x.PipelineStageId).OnDelete(DeleteBehavior.Restrict);
        });

        // Activity
        modelBuilder.Entity<Activity>(e =>
        {
            e.ToTable("Activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).IsRequired().HasMaxLength(255);
            e.Property(x => x.Type).IsRequired().HasMaxLength(50);
            // Polymorphic - no FK constraints on RelatedToId / NameId
            e.HasOne(x => x.Lead).WithMany(x => x.Activities)
                .HasForeignKey(x => x.NameId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Contact).WithMany(x => x.Activities)
                .HasForeignKey("ContactActivityId").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Account).WithMany(x => x.Activities)
                .HasForeignKey("AccountActivityId").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Deal).WithMany(x => x.Activities)
                .HasForeignKey("DealActivityId").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Case).WithMany(x => x.Activities)
                .HasForeignKey("CaseActivityId").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Campaign).WithMany()
                .HasForeignKey("CampaignActivityId").OnDelete(DeleteBehavior.Restrict);
            // Ignore EF shadow FKs - navigation is resolved via NameId/RelatedToId at app level
            e.Ignore(x => x.Contact);
            e.Ignore(x => x.Account);
            e.Ignore(x => x.Deal);
            e.Ignore(x => x.Case);
            e.Ignore(x => x.Campaign);
            e.Ignore(x => x.Lead);
        });

        // Note
        modelBuilder.Entity<Note>(e =>
        {
            e.ToTable("Notes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).IsRequired();
            e.Property(x => x.ParentType).IsRequired().HasMaxLength(50);
            e.Ignore(x => x.Lead);
            e.Ignore(x => x.Account);
            e.Ignore(x => x.Contact);
            e.Ignore(x => x.Deal);
            e.Ignore(x => x.Case);
        });

        // Attachment
        modelBuilder.Entity<Attachment>(e =>
        {
            e.ToTable("Attachments");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            e.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
            e.Property(x => x.ParentType).IsRequired().HasMaxLength(50);
            e.Ignore(x => x.Lead);
            e.Ignore(x => x.Account);
            e.Ignore(x => x.Contact);
            e.Ignore(x => x.Deal);
            e.Ignore(x => x.Case);
        });

        // Case
        modelBuilder.Entity<Case>(e =>
        {
            e.ToTable("Cases");
            e.HasKey(x => x.Id);
            e.Property(x => x.CaseNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Status).IsRequired().HasMaxLength(50);
            e.Property(x => x.Priority).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.Contact).WithMany(x => x.Cases)
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Entitlement).WithMany(x => x.Cases)
                .HasForeignKey(x => x.EntitlementId).OnDelete(DeleteBehavior.Restrict);
        });

        // CaseComment
        modelBuilder.Entity<CaseComment>(e =>
        {
            e.ToTable("CaseComments");
            e.HasKey(x => x.Id);
            e.Property(x => x.CommentBody).IsRequired();
            e.HasOne(x => x.Case).WithMany(x => x.Comments)
                .HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        // KnowledgeArticle
        modelBuilder.Entity<KnowledgeArticle>(e =>
        {
            e.ToTable("KnowledgeArticles");
            e.HasKey(x => x.Id);
            e.Property(x => x.ArticleNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.Body).IsRequired();
        });

        // Entitlement
        modelBuilder.Entity<Entitlement>(e =>
        {
            e.ToTable("Entitlements");
            e.HasKey(x => x.Id);
            e.Property(x => x.EntitlementName).IsRequired().HasMaxLength(255);
            e.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Contact).WithMany()
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
        });

        // Campaign
        modelBuilder.Entity<Campaign>(e =>
        {
            e.ToTable("Campaigns");
            e.HasKey(x => x.Id);
            e.Property(x => x.CampaignName).IsRequired().HasMaxLength(255);
            e.Property(x => x.Status).IsRequired().HasMaxLength(50);
            e.Property(x => x.ExpectedRevenue).HasColumnType("decimal(18,2)");
            e.Property(x => x.BudgetedCost).HasColumnType("decimal(18,2)");
            e.Property(x => x.ActualCost).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.ParentCampaign).WithMany(x => x.ChildCampaigns)
                .HasForeignKey(x => x.ParentCampaignId).OnDelete(DeleteBehavior.Restrict);
        });

        // CampaignMember
        modelBuilder.Entity<CampaignMember>(e =>
        {
            e.ToTable("CampaignMembers");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Campaign).WithMany(x => x.Members)
                .HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Lead).WithMany()
                .HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Contact).WithMany()
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
        });

        // ContactList
        modelBuilder.Entity<ContactList>(e =>
        {
            e.ToTable("ContactLists");
            e.HasKey(x => x.Id);
            e.Property(x => x.ListName).IsRequired().HasMaxLength(255);
        });

        // ContactListMember
        modelBuilder.Entity<ContactListMember>(e =>
        {
            e.ToTable("ContactListMembers");
            e.HasKey(x => x.Id);
            e.Property(x => x.MemberType).IsRequired().HasMaxLength(20);
            e.HasOne(x => x.ContactList).WithMany(x => x.Members)
                .HasForeignKey(x => x.ContactListId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Contact).WithMany()
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Lead).WithMany()
                .HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).IsRequired().HasMaxLength(255);
            e.Property(x => x.ProductCode).HasMaxLength(100);
            e.Property(x => x.QuantityUnitPrice).HasColumnType("decimal(18,2)");
        });

        // Pricebook
        modelBuilder.Entity<Pricebook>(e =>
        {
            e.ToTable("Pricebooks");
            e.HasKey(x => x.Id);
            e.Property(x => x.PricebookName).IsRequired().HasMaxLength(255);
            e.Property(x => x.CurrencyCode).HasMaxLength(3);
        });

        // PricebookEntry
        modelBuilder.Entity<PricebookEntry>(e =>
        {
            e.ToTable("PricebookEntries");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.CurrencyCode).HasMaxLength(3);
            e.HasOne(x => x.Pricebook).WithMany(x => x.Entries)
                .HasForeignKey(x => x.PricebookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(x => x.PricebookEntries)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // DealProduct
        modelBuilder.Entity<DealProduct>(e =>
        {
            e.ToTable("DealProducts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.ListPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(5,2)");
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Deal).WithMany(x => x.DealProducts)
                .HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(x => x.DealProducts)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // DealContact
        modelBuilder.Entity<DealContact>(e =>
        {
            e.ToTable("DealContacts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Role).HasMaxLength(100);
            e.HasOne(x => x.Deal).WithMany(x => x.DealContacts)
                .HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Contact).WithMany()
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
        });

        // Quote
        modelBuilder.Entity<Quote>(e =>
        {
            e.ToTable("Quotes");
            e.HasKey(x => x.Id);
            e.Property(x => x.QuoteNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.QuoteName).IsRequired().HasMaxLength(255);
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(5,2)");
            e.Property(x => x.Tax).HasColumnType("decimal(18,2)");
            e.Property(x => x.ShippingAndHandling).HasColumnType("decimal(18,2)");
            e.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Deal).WithMany()
                .HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Pricebook).WithMany()
                .HasForeignKey(x => x.PricebookId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Contact).WithMany()
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        // QuoteLineItem
        modelBuilder.Entity<QuoteLineItem>(e =>
        {
            e.ToTable("QuoteLineItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.ListPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(5,2)");
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Quote).WithMany(x => x.LineItems)
                .HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(x => x.QuoteLineItems)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // Order
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.Tax).HasColumnType("decimal(18,2)");
            e.Property(x => x.ShippingAndHandling).HasColumnType("decimal(18,2)");
            e.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Contract).WithMany(x => x.Orders)
                .HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Quote).WithMany()
                .HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Restrict);
        });

        // OrderLineItem
        modelBuilder.Entity<OrderLineItem>(e =>
        {
            e.ToTable("OrderLineItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.ListPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(5,2)");
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Order).WithMany(x => x.LineItems)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(x => x.OrderLineItems)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // Contract
        modelBuilder.Entity<Contract>(e =>
        {
            e.ToTable("Contracts");
            e.HasKey(x => x.Id);
            e.Property(x => x.ContractNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.ContractValue).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BillingContact).WithMany()
                .HasForeignKey(x => x.BillingContactId).OnDelete(DeleteBehavior.Restrict);
        });

        // Pipeline
        modelBuilder.Entity<Pipeline>(e =>
        {
            e.ToTable("Pipelines");
            e.HasKey(x => x.Id);
            e.Property(x => x.PipelineName).IsRequired().HasMaxLength(255);
        });

        // PipelineStage
        modelBuilder.Entity<PipelineStage>(e =>
        {
            e.ToTable("PipelineStages");
            e.HasKey(x => x.Id);
            e.Property(x => x.StageName).IsRequired().HasMaxLength(100);
            e.Property(x => x.ProbabilityPercent).HasColumnType("decimal(5,2)");
            e.HasOne(x => x.Pipeline).WithMany(x => x.Stages)
                .HasForeignKey(x => x.PipelineId).OnDelete(DeleteBehavior.Cascade);
        });

        // EmailMessage
        modelBuilder.Entity<EmailMessage>(e =>
        {
            e.ToTable("EmailMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).IsRequired().HasMaxLength(500);
        });

        // Tag
        modelBuilder.Entity<Tag>(e =>
        {
            e.ToTable("Tags");
            e.HasKey(x => x.Id);
            e.Property(x => x.TagName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Color).HasMaxLength(20);
            e.HasIndex(x => new { x.CompanyId, x.TagName }).IsUnique();
        });

        // EntityTag
        modelBuilder.Entity<EntityTag>(e =>
        {
            e.ToTable("EntityTags");
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.Tag).WithMany(x => x.EntityTags)
                .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TagId, x.EntityId, x.EntityType }).IsUnique();
        });

        // Territory
        modelBuilder.Entity<Territory>(e =>
        {
            e.ToTable("Territories");
            e.HasKey(x => x.Id);
            e.Property(x => x.TerritoryName).IsRequired().HasMaxLength(255);
            e.HasOne(x => x.ParentTerritory).WithMany(x => x.ChildTerritories)
                .HasForeignKey(x => x.ParentTerritoryId).OnDelete(DeleteBehavior.Restrict);
        });

        // TerritoryAccount
        modelBuilder.Entity<TerritoryAccount>(e =>
        {
            e.ToTable("TerritoryAccounts");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Territory).WithMany(x => x.TerritoryAccounts)
                .HasForeignKey(x => x.TerritoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TerritoryId, x.AccountId }).IsUnique();
        });

        // Team
        modelBuilder.Entity<Team>(e =>
        {
            e.ToTable("Teams");
            e.HasKey(x => x.Id);
            e.Property(x => x.TeamName).IsRequired().HasMaxLength(255);
        });

        // TeamMember
        modelBuilder.Entity<TeamMember>(e =>
        {
            e.ToTable("TeamMembers");
            e.HasKey(x => x.Id);
            e.Property(x => x.TeamRole).HasMaxLength(100);
            e.HasOne(x => x.Team).WithMany(x => x.Members)
                .HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
        });

        // SalesTarget is owned by the Sales module (sales.sales_targets) — quota drives
        // commission, and one record avoids commission and forecast attainment disagreeing.
        // Forecast below stays here: pipeline projection is a genuinely CRM concern.

        // Forecast
        modelBuilder.Entity<Forecast>(e =>
        {
            e.ToTable("Forecasts");
            e.HasKey(x => x.Id);
            e.Property(x => x.PipelineAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.BestCaseAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.CommitAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ClosedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.AdjustedAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.QuotaAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.CurrencyCode).HasMaxLength(3);
        });

        // Cross-cutting rules shared by every module: UTC normalisation for all
        // DateTime properties and the xmin optimistic-concurrency token. Must stay
        // last so it sees owned-type and DbSet-less properties configured above.
        modelBuilder.ApplyNexcoreConventions();
    }
}
