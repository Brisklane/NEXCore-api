using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Infrastructure.Services;

public class CostEntryService : ICostEntryService
{
    private readonly ICostEntryRepository _repo;
    private readonly ILogger<CostEntryService> _logger;

    public CostEntryService(ICostEntryRepository repo, ILogger<CostEntryService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<CostEntryDto> CreateAsync(CreateCostEntryDto request, Guid userId)
    {
        var total = request.MaterialCost + request.LaborCost
            + (request.MachineCost ?? 0) + request.OverheadCost + (request.ScrapCost ?? 0);
        var entity = new CostEntry
        {
            ProductionOrderId = request.ProductionOrderId,
            MaterialCost = request.MaterialCost, LaborCost = request.LaborCost,
            MachineCost = request.MachineCost, OverheadCost = request.OverheadCost,
            ScrapCost = request.ScrapCost, TotalCost = total,
            PostedAt = request.PostedAt, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<CostEntryDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<CostEntryDto?> GetByProductionOrderAsync(Guid productionOrderId)
    {
        var e = await _repo.GetByProductionOrderAsync(productionOrderId);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<CostEntryDto> UpdateAsync(Guid id, UpdateCostEntryDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("CostEntry not found");
        if (request.MaterialCost.HasValue) e.MaterialCost = request.MaterialCost.Value;
        if (request.LaborCost.HasValue) e.LaborCost = request.LaborCost.Value;
        if (request.MachineCost.HasValue) e.MachineCost = request.MachineCost;
        if (request.OverheadCost.HasValue) e.OverheadCost = request.OverheadCost.Value;
        if (request.ScrapCost.HasValue) e.ScrapCost = request.ScrapCost;
        if (request.JournalEntryId.HasValue) e.JournalEntryId = request.JournalEntryId;
        if (request.Notes != null) e.Notes = request.Notes;
        e.TotalCost = e.MaterialCost + e.LaborCost + (e.MachineCost ?? 0) + e.OverheadCost + (e.ScrapCost ?? 0);
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("CostEntry not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<CostEntryDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<CostEntryDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }
}

public class ProductionVarianceService : IProductionVarianceService
{
    private readonly IProductionVarianceRepository _repo;
    private readonly ILogger<ProductionVarianceService> _logger;

    public ProductionVarianceService(IProductionVarianceRepository repo, ILogger<ProductionVarianceService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<ProductionVarianceDto> CreateAsync(CreateProductionVarianceDto request, Guid userId)
    {
        var entity = new ProductionVariance
        {
            ProductionOrderId = request.ProductionOrderId, CostEntryId = request.CostEntryId,
            StandardMaterialCost = request.StandardMaterialCost, ActualMaterialCost = request.ActualMaterialCost,
            StandardLaborCost = request.StandardLaborCost, ActualLaborCost = request.ActualLaborCost,
            StandardMachineCost = request.StandardMachineCost, ActualMachineCost = request.ActualMachineCost,
            StandardOverheadCost = request.StandardOverheadCost, ActualOverheadCost = request.ActualOverheadCost,
            VarianceCategory = request.VarianceCategory, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<ProductionVarianceDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<ProductionVarianceDto?> GetByProductionOrderAsync(Guid productionOrderId)
    {
        var e = await _repo.GetByProductionOrderAsync(productionOrderId);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<ProductionVarianceDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<ProductionVarianceDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ProductionVarianceDto> UpdateAsync(Guid id, UpdateProductionVarianceDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Variance not found");
        if (request.IsSettled.HasValue) e.IsSettled = request.IsSettled.Value;
        if (request.SettlementJournalEntryId.HasValue) e.SettlementJournalEntryId = request.SettlementJournalEntryId;
        if (request.SettledAt.HasValue) e.SettledAt = request.SettledAt;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Variance not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }
}

public class MachineDowntimeService : IMachineDowntimeService
{
    private readonly IMachineDowntimeRepository _repo;
    private readonly ILogger<MachineDowntimeService> _logger;

    public MachineDowntimeService(IMachineDowntimeRepository repo, ILogger<MachineDowntimeService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<MachineDowntimeDto> CreateAsync(CreateMachineDowntimeDto request, Guid userId)
    {
        var entity = new MachineDowntime
        {
            WorkCenterId = request.WorkCenterId, ProductionOrderId = request.ProductionOrderId,
            StartTime = request.StartTime, Category = request.Category,
            Reason = request.Reason, Status = "Open",
            ReportedById = userId, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<MachineDowntimeDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<MachineDowntimeDto>> GetByWorkCenterAsync(Guid workCenterId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: m => m.WorkCenterId == workCenterId);
        return PaginatedResponse<MachineDowntimeDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<MachineDowntimeDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: m => m.ProductionOrderId == productionOrderId);
        return PaginatedResponse<MachineDowntimeDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<MachineDowntimeDto> UpdateAsync(Guid id, UpdateMachineDowntimeDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Downtime record not found");
        if (request.EndTime.HasValue)
        {
            e.EndTime = request.EndTime;
            e.DurationHours = request.DurationHours ?? (decimal)(request.EndTime.Value - e.StartTime).TotalHours;
        }
        if (request.RootCause != null) e.RootCause = request.RootCause;
        if (request.Resolution != null) e.Resolution = request.Resolution;
        if (request.ResolvedById.HasValue) e.ResolvedById = request.ResolvedById;
        if (request.Status != null) e.Status = request.Status;
        if (request.MaintenanceWorkOrderId.HasValue) e.MaintenanceWorkOrderId = request.MaintenanceWorkOrderId;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Downtime record not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<MachineDowntimeDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<MachineDowntimeDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }
}
