using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Infrastructure.Services;

public class MaterialIssueService : IMaterialIssueService
{
    private readonly IMaterialIssueRepository _repo;
    private readonly ILogger<MaterialIssueService> _logger;

    public MaterialIssueService(IMaterialIssueRepository repo, ILogger<MaterialIssueService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<MaterialIssueDto> CreateAsync(CreateMaterialIssueDto request, Guid userId)
    {
        var entity = new MaterialIssue
        {
            ProductionOrderId = request.ProductionOrderId, MaterialId = request.MaterialId,
            QuantityIssued = request.QuantityIssued, IssuedAt = request.IssuedAt,
            IssuedById = userId, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<MaterialIssueDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<MaterialIssueDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: m => m.ProductionOrderId == productionOrderId);
        return PaginatedResponse<MaterialIssueDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<MaterialIssueDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<MaterialIssueDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<MaterialIssueDto> UpdateAsync(Guid id, UpdateMaterialIssueDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("MaterialIssue not found");
        if (request.QuantityReturned.HasValue) e.QuantityReturned = request.QuantityReturned.Value;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("MaterialIssue not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }
}

public class WorkInProgressService : IWorkInProgressService
{
    private readonly IWorkInProgressRepository _repo;
    private readonly ILogger<WorkInProgressService> _logger;

    public WorkInProgressService(IWorkInProgressRepository repo, ILogger<WorkInProgressService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<WorkInProgressDto> CreateAsync(CreateWorkInProgressDto request, Guid userId)
    {
        // Pre-check: Verify if active WIP already exists for this production order in the current tenant
        var existing = await _repo.GetByProductionOrderAsync(request.ProductionOrderId);
        if (existing != null)
        {
            throw new InvalidOperationException($"Work In Progress already exists for Production Order ID: {request.ProductionOrderId}");
        }

        var entity = new WorkInProgress
        {
            ProductionOrderId = request.ProductionOrderId,
            QuantityInProgress = request.QuantityInProgress,
            LastUpdatedAt = DateTime.UtcNow, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<WorkInProgressDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<WorkInProgressDto?> GetByProductionOrderAsync(Guid productionOrderId)
    {
        var e = await _repo.GetByProductionOrderAsync(productionOrderId);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<WorkInProgressDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<WorkInProgressDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<WorkInProgressDto> UpdateAsync(Guid id, UpdateWorkInProgressDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("WIP record not found");
        if (request.QuantityInProgress.HasValue) e.QuantityInProgress = request.QuantityInProgress.Value;
        if (request.QuantityCompleted.HasValue) e.QuantityCompleted = request.QuantityCompleted.Value;
        if (request.QuantityRejected.HasValue) e.QuantityRejected = request.QuantityRejected.Value;
        if (request.Notes != null) e.Notes = request.Notes;
        e.LastUpdatedAt = DateTime.UtcNow;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("WIP record not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }
}

public class SubContractOrderService : ISubContractOrderService
{
    private readonly ISubContractOrderRepository _repo;
    private readonly ILogger<SubContractOrderService> _logger;

    public SubContractOrderService(ISubContractOrderRepository repo, ILogger<SubContractOrderService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<SubContractOrderDto> CreateAsync(CreateSubContractOrderDto request, Guid userId)
    {
        var entity = new SubContractOrder
        {
            ProductionOrderId = request.ProductionOrderId,
            ProductionOrderOperationId = request.ProductionOrderOperationId,
            VendorId = request.VendorId, PurchaseOrderId = request.PurchaseOrderId,
            QuantitySent = request.QuantitySent, SentAt = request.SentAt,
            ExpectedReturnDate = request.ExpectedReturnDate, UnitCost = request.UnitCost,
            Status = "Draft", Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<SubContractOrderDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<SubContractOrderDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: s => s.ProductionOrderId == productionOrderId);
        return PaginatedResponse<SubContractOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<SubContractOrderDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<SubContractOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<SubContractOrderDto> UpdateAsync(Guid id, UpdateSubContractOrderDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("SubContractOrder not found");
        if (request.QuantityReceived.HasValue) e.QuantityReceived = request.QuantityReceived.Value;
        if (request.QuantityRejected.HasValue) e.QuantityRejected = request.QuantityRejected.Value;
        if (request.PurchaseOrderId.HasValue) e.PurchaseOrderId = request.PurchaseOrderId;
        if (request.ActualReturnDate.HasValue) e.ActualReturnDate = request.ActualReturnDate;
        if (request.UnitCost.HasValue) e.UnitCost = request.UnitCost;
        if (request.TotalCost.HasValue) e.TotalCost = request.TotalCost;
        if (request.Status != null) e.Status = request.Status;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("SubContractOrder not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }
}

public class ProductionScheduleService : IProductionScheduleService
{
    private readonly IProductionScheduleRepository _repo;
    private readonly ILogger<ProductionScheduleService> _logger;

    public ProductionScheduleService(IProductionScheduleRepository repo, ILogger<ProductionScheduleService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<ProductionScheduleDto> CreateAsync(CreateProductionScheduleDto request, Guid userId)
    {
        var entity = new ProductionSchedule
        {
            ProductionOrderId = request.ProductionOrderId, WorkCenterId = request.WorkCenterId,
            ProductionOrderOperationId = request.ProductionOrderOperationId,
            ScheduledStartDate = request.ScheduledStartDate, ScheduledEndDate = request.ScheduledEndDate,
            ScheduleType = request.ScheduleType, CapacityRequiredHours = request.CapacityRequiredHours,
            Status = "Scheduled", Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<ProductionScheduleDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<ProductionScheduleDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: s => s.ProductionOrderId == productionOrderId);
        return PaginatedResponse<ProductionScheduleDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<ProductionScheduleDto>> GetByWorkCenterAsync(Guid workCenterId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: s => s.WorkCenterId == workCenterId);
        return PaginatedResponse<ProductionScheduleDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<ProductionScheduleDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<ProductionScheduleDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ProductionScheduleDto> UpdateAsync(Guid id, UpdateProductionScheduleDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Schedule not found");
        if (request.ScheduledStartDate.HasValue) e.ScheduledStartDate = request.ScheduledStartDate.Value;
        if (request.ScheduledEndDate.HasValue) e.ScheduledEndDate = request.ScheduledEndDate.Value;
        if (request.ScheduleType != null) e.ScheduleType = request.ScheduleType;
        if (request.CapacityRequiredHours.HasValue) e.CapacityRequiredHours = request.CapacityRequiredHours.Value;
        if (request.Status != null) e.Status = request.Status;
        if (request.HasCapacityConflict.HasValue) e.HasCapacityConflict = request.HasCapacityConflict.Value;
        if (request.ConflictDescription != null) e.ConflictDescription = request.ConflictDescription;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Schedule not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }
}
