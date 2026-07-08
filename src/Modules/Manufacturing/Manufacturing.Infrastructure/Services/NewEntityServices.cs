using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Infrastructure.Services;

public class ReworkOrderService : IReworkOrderService
{
    private readonly IReworkOrderRepository _repo;
    private readonly ILogger<ReworkOrderService> _logger;

    public ReworkOrderService(IReworkOrderRepository repo, ILogger<ReworkOrderService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<ReworkOrderDto> CreateAsync(CreateReworkOrderDto request, Guid userId)
    {
        var entity = new ReworkOrder
        {
            ProductionOrderId = request.ProductionOrderId, InspectionId = request.InspectionId,
            ReworkRoutingId = request.ReworkRoutingId, Quantity = request.Quantity,
            UnitOfMeasure = request.UnitOfMeasure, Reason = request.Reason,
            Status = "Draft", ScheduledStartDate = request.ScheduledStartDate,
            ScheduledEndDate = request.ScheduledEndDate, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
        _logger.LogInformation("ReworkOrder created for ProductionOrder {Id}", entity.ProductionOrderId);
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<ReworkOrderDto?> GetByIdAsync(Guid id)
    { var e = await _repo.GetByIdAsync(id); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<PaginatedResponse<ReworkOrderDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: r => r.ProductionOrderId == productionOrderId);
        return PaginatedResponse<ReworkOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<ReworkOrderDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<ReworkOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ReworkOrderDto> UpdateAsync(Guid id, UpdateReworkOrderDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("ReworkOrder not found");
        if (request.QuantityCompleted.HasValue) e.QuantityCompleted = request.QuantityCompleted.Value;
        if (request.QuantityRejected.HasValue) e.QuantityRejected = request.QuantityRejected.Value;
        if (request.Status != null) e.Status = request.Status;
        if (request.ScheduledStartDate.HasValue) e.ScheduledStartDate = request.ScheduledStartDate;
        if (request.ScheduledEndDate.HasValue) e.ScheduledEndDate = request.ScheduledEndDate;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("ReworkOrder not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
    }
}

public class MaterialPlanningDataService : IMaterialPlanningDataService
{
    private readonly IMaterialPlanningDataRepository _repo;
    private readonly ILogger<MaterialPlanningDataService> _logger;

    public MaterialPlanningDataService(IMaterialPlanningDataRepository repo, ILogger<MaterialPlanningDataService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<MaterialPlanningDataDto> CreateAsync(CreateMaterialPlanningDataDto request, Guid userId)
    {
        var entity = new MaterialPlanningData
        {
            ProductId = request.ProductId, SafetyStock = request.SafetyStock,
            ReorderPoint = request.ReorderPoint, MaximumStockLevel = request.MaximumStockLevel,
            LotSize = request.LotSize, LeadTimeDays = request.LeadTimeDays,
            PlanningHorizonDays = request.PlanningHorizonDays, ScrapPercentage = request.ScrapPercentage,
            ProcurementType = request.ProcurementType, MRPType = request.MRPType,
            IsActive = true, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<MaterialPlanningDataDto?> GetByIdAsync(Guid id)
    { var e = await _repo.GetByIdAsync(id); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<MaterialPlanningDataDto?> GetByProductAsync(Guid productId)
    { var e = await _repo.GetByProductAsync(productId); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<PaginatedResponse<MaterialPlanningDataDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<MaterialPlanningDataDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<MaterialPlanningDataDto> UpdateAsync(Guid id, UpdateMaterialPlanningDataDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("MaterialPlanningData not found");
        if (request.SafetyStock.HasValue) e.SafetyStock = request.SafetyStock.Value;
        if (request.ReorderPoint.HasValue) e.ReorderPoint = request.ReorderPoint.Value;
        if (request.MaximumStockLevel.HasValue) e.MaximumStockLevel = request.MaximumStockLevel.Value;
        if (request.LotSize.HasValue) e.LotSize = request.LotSize.Value;
        if (request.LeadTimeDays.HasValue) e.LeadTimeDays = request.LeadTimeDays.Value;
        if (request.PlanningHorizonDays.HasValue) e.PlanningHorizonDays = request.PlanningHorizonDays.Value;
        if (request.ScrapPercentage.HasValue) e.ScrapPercentage = request.ScrapPercentage.Value;
        if (request.ProcurementType != null) e.ProcurementType = request.ProcurementType;
        if (request.MRPType != null) e.MRPType = request.MRPType;
        if (request.IsActive.HasValue) e.IsActive = request.IsActive.Value;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("MaterialPlanningData not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
    }
}

public class StandardCostService : IStandardCostService
{
    private readonly IStandardCostRepository _repo;
    private readonly ILogger<StandardCostService> _logger;

    public StandardCostService(IStandardCostRepository repo, ILogger<StandardCostService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<StandardCostDto> CreateAsync(CreateStandardCostDto request, Guid userId)
    {
        var total = request.MaterialCost + request.LaborCost + request.MachineCost + request.OverheadCost;
        var entity = new StandardCost
        {
            ProductId = request.ProductId, Version = request.Version,
            CurrencyCode = request.CurrencyCode, MaterialCost = request.MaterialCost,
            LaborCost = request.LaborCost, MachineCost = request.MachineCost,
            OverheadCost = request.OverheadCost, TotalCost = total,
            IsActive = true, EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<StandardCostDto?> GetByIdAsync(Guid id)
    { var e = await _repo.GetByIdAsync(id); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<StandardCostDto?> GetActiveByProductAsync(Guid productId)
    { var e = await _repo.GetActiveByProductAsync(productId); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<PaginatedResponse<StandardCostDto>> GetByProductAsync(Guid productId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: s => s.ProductId == productId);
        return PaginatedResponse<StandardCostDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<StandardCostDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<StandardCostDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<StandardCostDto> UpdateAsync(Guid id, UpdateStandardCostDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("StandardCost not found");
        if (request.MaterialCost.HasValue) e.MaterialCost = request.MaterialCost.Value;
        if (request.LaborCost.HasValue) e.LaborCost = request.LaborCost.Value;
        if (request.MachineCost.HasValue) e.MachineCost = request.MachineCost.Value;
        if (request.OverheadCost.HasValue) e.OverheadCost = request.OverheadCost.Value;
        if (request.IsActive.HasValue) e.IsActive = request.IsActive.Value;
        if (request.EffectiveTo.HasValue) e.EffectiveTo = request.EffectiveTo;
        if (request.Notes != null) e.Notes = request.Notes;
        e.TotalCost = e.MaterialCost + e.LaborCost + e.MachineCost + e.OverheadCost;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("StandardCost not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
    }
}

public class OverheadRuleService : IOverheadRuleService
{
    private readonly IOverheadRuleRepository _repo;
    private readonly ILogger<OverheadRuleService> _logger;

    public OverheadRuleService(IOverheadRuleRepository repo, ILogger<OverheadRuleService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<OverheadRuleDto> CreateAsync(CreateOverheadRuleDto request, Guid userId)
    {
        var entity = new OverheadRule
        {
            Code = request.Code, Name = request.Name, WorkCenterId = request.WorkCenterId,
            RateType = request.RateType, Value = request.Value, AppliesTo = request.AppliesTo,
            IsActive = true, EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo,
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<OverheadRuleDto?> GetByIdAsync(Guid id)
    { var e = await _repo.GetByIdAsync(id); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<PaginatedResponse<OverheadRuleDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<OverheadRuleDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<OverheadRuleDto>> GetByWorkCenterAsync(Guid workCenterId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: o => o.WorkCenterId == workCenterId);
        return PaginatedResponse<OverheadRuleDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<OverheadRuleDto> UpdateAsync(Guid id, UpdateOverheadRuleDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("OverheadRule not found");
        if (request.Name != null) e.Name = request.Name;
        if (request.RateType != null) e.RateType = request.RateType;
        if (request.Value.HasValue) e.Value = request.Value.Value;
        if (request.AppliesTo != null) e.AppliesTo = request.AppliesTo;
        if (request.IsActive.HasValue) e.IsActive = request.IsActive.Value;
        if (request.EffectiveFrom.HasValue) e.EffectiveFrom = request.EffectiveFrom;
        if (request.EffectiveTo.HasValue) e.EffectiveTo = request.EffectiveTo;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("OverheadRule not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
    }
}

public class CapacityLoadService : ICapacityLoadService
{
    private readonly ICapacityLoadRepository _repo;
    private readonly ILogger<CapacityLoadService> _logger;

    public CapacityLoadService(ICapacityLoadRepository repo, ILogger<CapacityLoadService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<CapacityLoadDto> CreateAsync(CreateCapacityLoadDto request, Guid userId)
    {
        var loadPct = request.AvailableHours > 0
            ? Math.Round(request.RequiredHours / request.AvailableHours * 100, 2)
            : 0;
        var entity = new CapacityLoad
        {
            WorkCenterId = request.WorkCenterId, WorkCenterShiftId = request.WorkCenterShiftId,
            Date = request.Date, RequiredHours = request.RequiredHours,
            AvailableHours = request.AvailableHours, LoadPercentage = loadPct,
            ProductionOrderCount = request.ProductionOrderCount,
            IsOverloaded = loadPct > 100, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<CapacityLoadDto?> GetByIdAsync(Guid id)
    { var e = await _repo.GetByIdAsync(id); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<PaginatedResponse<CapacityLoadDto>> GetByWorkCenterAsync(Guid workCenterId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: c => c.WorkCenterId == workCenterId);
        return PaginatedResponse<CapacityLoadDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<CapacityLoadDto>> GetByDateRangeAsync(Guid workCenterId, DateTime from, DateTime to, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: c => c.WorkCenterId == workCenterId && c.Date >= from && c.Date <= to);
        return PaginatedResponse<CapacityLoadDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<CapacityLoadDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<CapacityLoadDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<CapacityLoadDto> UpdateAsync(Guid id, UpdateCapacityLoadDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("CapacityLoad not found");
        if (request.RequiredHours.HasValue) e.RequiredHours = request.RequiredHours.Value;
        if (request.AvailableHours.HasValue) e.AvailableHours = request.AvailableHours.Value;
        if (request.ProductionOrderCount.HasValue) e.ProductionOrderCount = request.ProductionOrderCount.Value;
        if (request.Notes != null) e.Notes = request.Notes;
        e.LoadPercentage = e.AvailableHours > 0 ? Math.Round(e.RequiredHours / e.AvailableHours * 100, 2) : 0;
        e.IsOverloaded = e.LoadPercentage > 100;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("CapacityLoad not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
    }
}

public class DemandService : IDemandService
{
    private readonly IDemandRepository _repo;
    private readonly ILogger<DemandService> _logger;

    public DemandService(IDemandRepository repo, ILogger<DemandService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<DemandDto> CreateAsync(CreateDemandDto request, Guid userId)
    {
        var entity = new Demand
        {
            ProductId = request.ProductId, Quantity = request.Quantity,
            DueDate = request.DueDate, SourceType = request.SourceType,
            ReferenceId = request.ReferenceId, Status = "Open",
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<DemandDto?> GetByIdAsync(Guid id)
    { var e = await _repo.GetByIdAsync(id); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<PaginatedResponse<DemandDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<DemandDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<DemandDto>> GetByProductAsync(Guid productId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: d => d.ProductId == productId);
        return PaginatedResponse<DemandDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<DemandDto>> GetOpenAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: d => d.Status == "Open");
        return PaginatedResponse<DemandDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<DemandDto> UpdateAsync(Guid id, UpdateDemandDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Demand not found");
        if (request.Quantity.HasValue) e.Quantity = request.Quantity.Value;
        if (request.FulfilledQty.HasValue) e.FulfilledQty = request.FulfilledQty.Value;
        if (request.DueDate.HasValue) e.DueDate = request.DueDate.Value;
        if (request.Status != null) e.Status = request.Status;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Demand not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
    }
}

public class InventoryTransactionService : IInventoryTransactionService
{
    private readonly IInventoryTransactionRepository _repo;
    private readonly ILogger<InventoryTransactionService> _logger;

    public InventoryTransactionService(IInventoryTransactionRepository repo, ILogger<InventoryTransactionService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<InventoryTransactionDto> CreateAsync(CreateInventoryTransactionDto request, Guid userId)
    {
        var entity = new InventoryTransaction
        {
            ProductId = request.ProductId, Quantity = request.Quantity,
            TransactionType = request.TransactionType, ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType, TransactionDate = request.TransactionDate,
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<InventoryTransactionDto?> GetByIdAsync(Guid id)
    { var e = await _repo.GetByIdAsync(id); return e == null ? null : ManufacturingMapper.ToDto(e); }

    public async Task<PaginatedResponse<InventoryTransactionDto>> GetByProductAsync(Guid productId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: i => i.ProductId == productId);
        return PaginatedResponse<InventoryTransactionDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<InventoryTransactionDto>> GetByReferenceAsync(Guid referenceId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: i => i.ReferenceId == referenceId);
        return PaginatedResponse<InventoryTransactionDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<InventoryTransactionDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<InventoryTransactionDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<InventoryTransactionDto> UpdateAsync(Guid id, UpdateInventoryTransactionDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("InventoryTransaction not found");
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("InventoryTransaction not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e); await _repo.SaveChangesAsync();
    }
}
