using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Infrastructure.Services;

public class RoutingService : IRoutingService
{
    private readonly IRoutingRepository _repo;
    private readonly IRoutingOperationRepository _opRepo;
    private readonly ILogger<RoutingService> _logger;

    public RoutingService(IRoutingRepository repo, IRoutingOperationRepository opRepo, ILogger<RoutingService> logger)
    {
        _repo = repo; _opRepo = opRepo; _logger = logger;
    }

    public async Task<RoutingDto> CreateAsync(CreateRoutingDto request, Guid userId)
    {
        var routing = new Routing
        {
            ProductId = request.ProductId, Name = request.Name, Version = request.Version,
            Description = request.Description, IsActive = true,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(routing);
        await _repo.SaveChangesAsync();

        foreach (var opDto in request.Operations)
        {
            var op = new RoutingOperation
            {
                RoutingId = routing.Id, SequenceNo = opDto.SequenceNo,
                OperationName = opDto.OperationName, WorkCenterId = opDto.WorkCenterId,
                StandardHours = opDto.StandardHours, SetupHours = opDto.SetupHours,
                LaborHours = opDto.LaborHours, MachineHours = opDto.MachineHours,
                Notes = opDto.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _opRepo.AddAsync(op);
        }
        if (request.Operations.Count > 0) await _opRepo.SaveChangesAsync();

        _logger.LogInformation("Routing created: {Name}", routing.Name);
        var full = await _repo.GetWithOperationsAsync(routing.Id);
        return ManufacturingMapper.ToDto(full!);
    }

    public async Task<RoutingDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetWithOperationsAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<RoutingDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<RoutingDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<RoutingDto>> GetByProductAsync(Guid productId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: r => r.ProductId == productId);
        return PaginatedResponse<RoutingDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<RoutingDto> UpdateAsync(Guid id, UpdateRoutingDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Routing not found");
        if (request.Name != null) e.Name = request.Name;
        if (request.Version.HasValue) e.Version = request.Version.Value;
        if (request.IsActive.HasValue) e.IsActive = request.IsActive.Value;
        if (request.Description != null) e.Description = request.Description;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Routing not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<RoutingOperationDto> AddOperationAsync(Guid routingId, CreateRoutingOperationDto request, Guid userId)
    {
        var op = new RoutingOperation
        {
            RoutingId = routingId, SequenceNo = request.SequenceNo,
            OperationName = request.OperationName, WorkCenterId = request.WorkCenterId,
            StandardHours = request.StandardHours, SetupHours = request.SetupHours,
            LaborHours = request.LaborHours, MachineHours = request.MachineHours,
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _opRepo.AddAsync(op);
        await _opRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(op);
    }

    public async Task<RoutingOperationDto> UpdateOperationAsync(Guid operationId, UpdateRoutingOperationDto request, Guid userId)
    {
        var e = await _opRepo.GetByIdAsync(operationId) ?? throw new InvalidOperationException("Operation not found");
        if (request.SequenceNo.HasValue) e.SequenceNo = request.SequenceNo.Value;
        if (request.OperationName != null) e.OperationName = request.OperationName;
        if (request.WorkCenterId.HasValue) e.WorkCenterId = request.WorkCenterId.Value;
        if (request.StandardHours.HasValue) e.StandardHours = request.StandardHours.Value;
        if (request.SetupHours.HasValue) e.SetupHours = request.SetupHours;
        if (request.LaborHours.HasValue) e.LaborHours = request.LaborHours;
        if (request.MachineHours.HasValue) e.MachineHours = request.MachineHours;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _opRepo.Update(e);
        await _opRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteOperationAsync(Guid operationId, Guid userId)
    {
        var e = await _opRepo.GetByIdAsync(operationId) ?? throw new InvalidOperationException("Operation not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _opRepo.Update(e);
        await _opRepo.SaveChangesAsync();
    }
}
