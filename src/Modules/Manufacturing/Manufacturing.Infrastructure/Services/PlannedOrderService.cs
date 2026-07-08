using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Infrastructure.Services;

public class PlannedOrderService : IPlannedOrderService
{
    private readonly IPlannedOrderRepository _repo;
    private readonly ILogger<PlannedOrderService> _logger;

    public PlannedOrderService(IPlannedOrderRepository repo, ILogger<PlannedOrderService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<PlannedOrderDto> CreateAsync(CreatePlannedOrderDto request, Guid userId)
    {
        var entity = new PlannedOrder
        {
            ProductId = request.ProductId, PlannedQty = request.PlannedQty,
            RequiredDate = request.RequiredDate, SourceType = request.SourceType,
            Status = "Draft", Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        _logger.LogInformation("PlannedOrder created for product {ProductId}", entity.ProductId);
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<PlannedOrderDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<PlannedOrderDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<PlannedOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<PlannedOrderDto>> GetByProductAsync(Guid productId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: o => o.ProductId == productId);
        return PaginatedResponse<PlannedOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PlannedOrderDto> UpdateAsync(Guid id, UpdatePlannedOrderDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("PlannedOrder not found");
        if (request.PlannedQty.HasValue) e.PlannedQty = request.PlannedQty.Value;
        if (request.RequiredDate.HasValue) e.RequiredDate = request.RequiredDate.Value;
        if (request.Status != null) e.Status = request.Status;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("PlannedOrder not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }
}
