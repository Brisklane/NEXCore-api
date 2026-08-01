using Crm.Application.Services.Interfaces;
using Crm.Infrastructure.Events;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Repositories.Implementations;
using Crm.Infrastructure.Repositories.Interfaces;
using Crm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.SharedKernel.Events;

namespace Crm.Infrastructure;

/// <summary>
/// Extension methods for registering CRM module infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrmInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<CrmDbContext>(options =>
            options.UseNexcorePostgres(configuration.GetConnectionString("DefaultConnection"), typeof(CrmDbContext).Assembly));

        // ========== REGISTER REPOSITORIES ==========
        // Core CRM Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IContactAddressRepository, ContactAddressRepository>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();

        // Service Module Repositories
        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddScoped<ICaseCommentRepository, CaseCommentRepository>();
        services.AddScoped<IKnowledgeArticleRepository, KnowledgeArticleRepository>();
        services.AddScoped<IEntitlementRepository, EntitlementRepository>();

        // Marketing Repositories
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<ICampaignMemberRepository, CampaignMemberRepository>();
        services.AddScoped<IContactListRepository, ContactListRepository>();
        services.AddScoped<IContactListMemberRepository, ContactListMemberRepository>();

        // Sales Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPricebookRepository, PricebookRepository>();
        services.AddScoped<IPricebookEntryRepository, PricebookEntryRepository>();
        services.AddScoped<IDealProductRepository, DealProductRepository>();
        services.AddScoped<IDealContactRepository, DealContactRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();

        // Platform Repositories
        services.AddScoped<IPipelineRepository, PipelineRepository>();
        services.AddScoped<IPipelineStageRepository, PipelineStageRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IEntityTagRepository, EntityTagRepository>();
        services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
        services.AddScoped<ITerritoryRepository, TerritoryRepository>();
        services.AddScoped<ITerritoryAccountRepository, TerritoryAccountRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IForecastRepository, ForecastRepository>();

        // ========== REGISTER APPLICATION SERVICES ==========
        // Core CRM Services
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IDealService, DealService>();
        services.AddScoped<IActivityService, ActivityService>();

        // Dashboard Services
        services.AddScoped<ICrmHomeDashboardService, CrmHomeDashboardService>();

        // Service Module Services
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IKnowledgeArticleService, KnowledgeArticleService>();
        services.AddScoped<IEntitlementService, EntitlementService>();

        // Marketing Services
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IContactListService, ContactListService>();

        // Notes & Attachments Services
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        // Sales Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPricebookService, PricebookService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IContractService, ContractService>();

        // Pipeline Services
        services.AddScoped<IPipelineService, PipelineService>();

        // Engagement Services
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IEmailMessageService, EmailMessageService>();

        // Territory & Team Services
        services.AddScoped<ITerritoryService, TerritoryService>();
        services.AddScoped<ITeamService, TeamService>();

        // Planning Services
        services.AddScoped<IForecastService, ForecastService>();

        // Initialization & Event Handlers
        // Seeds pipelines, products, pricebooks, accounts, contacts, leads, deals,
        // campaigns, cases, knowledge articles, activities, tags, territories, teams
        // and forecasts whenever a new company is created.
        services.AddScoped<ICrmInitializationService, CrmInitializationService>();

        // Subscribe to CompanyCreatedEvent published by the Core module.
        // Maintains loose coupling - CRM knows only about the shared-kernel event.
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, CrmCompanyCreatedEventHandler>();

        // Reverse geocoding via OpenStreetMap Nominatim (free, no API key required)
        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Nexcore-Platform/1.0 (contact@nexcore.com)");
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}
