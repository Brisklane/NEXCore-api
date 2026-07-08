using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Manufacturing.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Manufacturing.Infrastructure.Repositories.Implementations;
using Manufacturing.Infrastructure.Services;
using Manufacturing.Infrastructure.Events;
using Manufacturing.Application.Services.Interfaces;
using Nexcore.SharedKernel.Events;

namespace Manufacturing.Infrastructure;

/// <summary>
/// Service collection extensions for Manufacturing module.
/// Registers DbContext and all infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Manufacturing infrastructure services
    /// </summary>
    public static IServiceCollection AddManufacturingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ManufacturingDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b =>
                {
                    b.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    b.MigrationsAssembly("Manufacturing.Infrastructure");
                }));

        // ========== REPOSITORIES ==========
        // Master Data
        services.AddScoped<IWorkCenterRepository, WorkCenterRepository>();
        services.AddScoped<IWorkCenterShiftRepository, WorkCenterShiftRepository>();
        services.AddScoped<IBillOfMaterialRepository, BillOfMaterialRepository>();
        services.AddScoped<IBOMItemRepository, BOMItemRepository>();
        services.AddScoped<IBOMByProductRepository, BOMByProductRepository>();
        services.AddScoped<IRoutingRepository, RoutingRepository>();
        services.AddScoped<IRoutingOperationRepository, RoutingOperationRepository>();

        // Planning
        services.AddScoped<IPlannedOrderRepository, PlannedOrderRepository>();
        services.AddScoped<IProductionScheduleRepository, ProductionScheduleRepository>();

        // Execution
        services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
        services.AddScoped<IProductionOrderOperationRepository, ProductionOrderOperationRepository>();
        services.AddScoped<IProductionOrderComponentRepository, ProductionOrderComponentRepository>();
        services.AddScoped<IMaterialIssueRepository, MaterialIssueRepository>();
        services.AddScoped<IWorkInProgressRepository, WorkInProgressRepository>();
        services.AddScoped<ISubContractOrderRepository, SubContractOrderRepository>();

        // Quality & Completion
        services.AddScoped<IInspectionRepository, InspectionRepository>();
        services.AddScoped<IInspectionCharacteristicRepository, InspectionCharacteristicRepository>();
        services.AddScoped<IFinishedGoodsReceiptRepository, FinishedGoodsReceiptRepository>();
        services.AddScoped<IProductionBatchRepository, ProductionBatchRepository>();

        // Costing & Maintenance
        services.AddScoped<ICostEntryRepository, CostEntryRepository>();
        services.AddScoped<IProductionVarianceRepository, ProductionVarianceRepository>();
        services.AddScoped<IMachineDowntimeRepository, MachineDowntimeRepository>();

        // Rework
        services.AddScoped<IReworkOrderRepository, ReworkOrderRepository>();

        // MRP / Planning
        services.AddScoped<IMaterialPlanningDataRepository, MaterialPlanningDataRepository>();
        services.AddScoped<IDemandRepository, DemandRepository>();

        // Costing Standards
        services.AddScoped<IStandardCostRepository, StandardCostRepository>();
        services.AddScoped<IOverheadRuleRepository, OverheadRuleRepository>();

        // Capacity
        services.AddScoped<ICapacityLoadRepository, CapacityLoadRepository>();

        // Inventory Transactions
        services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();

        // ========== SERVICES ==========
        services.AddScoped<IWorkCenterService, WorkCenterService>();
        services.AddScoped<IBillOfMaterialService, BillOfMaterialService>();
        services.AddScoped<IRoutingService, RoutingService>();
        services.AddScoped<IPlannedOrderService, PlannedOrderService>();
        services.AddScoped<IProductionOrderService, ProductionOrderService>();
        services.AddScoped<IMaterialIssueService, MaterialIssueService>();
        services.AddScoped<IWorkInProgressService, WorkInProgressService>();
        services.AddScoped<ISubContractOrderService, SubContractOrderService>();
        services.AddScoped<IProductionScheduleService, ProductionScheduleService>();
        services.AddScoped<IInspectionService, InspectionService>();
        services.AddScoped<IFinishedGoodsReceiptService, FinishedGoodsReceiptService>();
        services.AddScoped<IProductionBatchService, ProductionBatchService>();
        services.AddScoped<ICostEntryService, CostEntryService>();
        services.AddScoped<IProductionVarianceService, ProductionVarianceService>();
        services.AddScoped<IMachineDowntimeService, MachineDowntimeService>();
        services.AddScoped<IReworkOrderService, ReworkOrderService>();
        services.AddScoped<IMaterialPlanningDataService, MaterialPlanningDataService>();
        services.AddScoped<IStandardCostService, StandardCostService>();
        services.AddScoped<IOverheadRuleService, OverheadRuleService>();
        services.AddScoped<ICapacityLoadService, CapacityLoadService>();
        services.AddScoped<IDemandService, DemandService>();
        services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();

        // ========== EVENT HANDLERS ==========
        services.AddScoped<ManufacturingInitializationService>();
        services.AddScoped<ManufacturingSeedDataService>();
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, ManufacturingCompanyCreatedEventHandler>();

        return services;
    }
}
