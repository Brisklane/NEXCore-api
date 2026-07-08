using Microsoft.Extensions.Logging;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Rider service — handles rider registration, availability, and assignment dispatch.
/// </summary>
public class RiderService : IRiderService
{
    private readonly IRiderRepository _riders;
    private readonly IRiderAssignmentRepository _assignments;
    private readonly IDocumentSequenceService _sequences;
    private readonly ILogger<RiderService> _logger;

    public RiderService(
        IRiderRepository riders,
        IRiderAssignmentRepository assignments,
        IDocumentSequenceService sequences,
        ILogger<RiderService> logger)
    {
        _riders      = riders;
        _assignments = assignments;
        _sequences   = sequences;
        _logger      = logger;
    }

    public async Task<List<RiderDto>> GetAllAsync()
    {
        var list = await _riders.GetAllAsync();
        return list.OrderByDescending(r => r.CreatedAt).Select(MapToDto).ToList();
    }

    public async Task<RiderDto?> GetByIdAsync(Guid id)
    {
        var rider = await _riders.GetByIdAsync(id);
        return rider is null ? null : MapToDto(rider);
    }

    public async Task<List<RiderDto>> GetAvailableAsync(Guid? storeId)
    {
        var list = await _riders.GetAvailableAsync(storeId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<RiderDto>> GetByBranchAsync(Guid branchId)
    {
        var list = await _riders.GetByBranchAsync(branchId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<RiderDto> CreateAsync(CreateRiderDto dto)
    {
        var rider = new Rider
        {
            RiderCode = dto.RiderCode,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Email = dto.Email,
            VehicleType = dto.VehicleType,
            VehiclePlateNumber = dto.VehiclePlateNumber,
            VehicleModel = dto.VehicleModel,
            ContractType = dto.ContractType,
            HrEmployeeId = dto.HrEmployeeId,
            HomeBranchId = dto.HomeBranchId,
            ZoneId = dto.ZoneId,
            Status = RiderStatus.Offline,
            IsActive = true,
        };

        await _riders.AddAsync(rider);
        await _riders.SaveChangesAsync();

        _logger.LogInformation("Rider created: {RiderCode} (ID: {Id})", rider.RiderCode, rider.Id);
        return MapToDto(rider);
    }

    public async Task<RiderDto> UpdateAsync(Guid id, UpdateRiderDto dto)
    {
        var rider = await _riders.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Rider not found");

        if (dto.FirstName         != null) rider.FirstName         = dto.FirstName;
        if (dto.LastName          != null) rider.LastName          = dto.LastName;
        if (dto.Phone             != null) rider.Phone             = dto.Phone;
        if (dto.Email             != null) rider.Email             = dto.Email;
        if (dto.VehicleType       != null) rider.VehicleType       = dto.VehicleType.Value;
        if (dto.VehiclePlateNumber != null) rider.VehiclePlateNumber = dto.VehiclePlateNumber;
        if (dto.HomeBranchId      != null) rider.HomeBranchId      = dto.HomeBranchId;
        if (dto.IsActive          != null) rider.IsActive          = dto.IsActive.Value;

        await _riders.SaveChangesAsync();
        _logger.LogInformation("Rider updated: {Id}", id);
        return MapToDto(rider);
    }

    public async Task<RiderAssignmentDto?> GetActiveAssignmentAsync(Guid salesOrderId)
    {
        var assignment = await _assignments.GetActiveAssignmentAsync(salesOrderId);
        if (assignment is null) return null;
        return new RiderAssignmentDto
        {
            Id = assignment.Id,
            AssignmentNumber = assignment.AssignmentNumber,
            SalesOrderId = assignment.SalesOrderId,
            RiderId = assignment.RiderId,
            Status = assignment.Status,
            AssignedAt = assignment.AssignedAt,
            AcceptedAt = assignment.AcceptedAt,
            PickedUpAt = assignment.PickedUpAt,
            DeliveredAt = assignment.DeliveredAt,
            DeliveryLatitude = assignment.DeliveryLatitude,
            DeliveryLongitude = assignment.DeliveryLongitude,
            ProofImageUrl = assignment.ProofImageUrl,
            FailureReason = assignment.FailureReason,
        };
    }

    public async Task<RiderAssignmentDto> AssignAsync(CreateRiderAssignmentDto dto)
    {
        var rider = await _riders.GetByIdAsync(dto.RiderId)
            ?? throw new InvalidOperationException("Rider not found");

        var ridSeq = await _sequences.GetNextNumberAsync(DocumentType.RiderAssignment);
        var assignment = new RiderAssignment
        {
            AssignmentNumber = ridSeq.Code,
            Code             = ridSeq.Code,
            CodeInt          = ridSeq.CodeInt,
            SalesOrderId = dto.SalesOrderId,
            RiderId = dto.RiderId,
            PickupBranchId = dto.PickupBranchId,
            DeliveryLatitude = dto.DeliveryLatitude,
            DeliveryLongitude = dto.DeliveryLongitude,
            EstimatedDistanceKm = dto.EstimatedDistanceKm,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            Status = RiderAssignmentStatus.Assigned,
            AssignedAt = DateTime.UtcNow,
        };

        rider.Status = RiderStatus.Busy;

        await _assignments.AddAsync(assignment);
        await _assignments.SaveChangesAsync();

        _logger.LogInformation("Rider {RiderId} assigned to order {OrderId} via {AssignmentNumber}",
            dto.RiderId, dto.SalesOrderId, assignment.AssignmentNumber);

        return new RiderAssignmentDto
        {
            Id = assignment.Id,
            AssignmentNumber = assignment.AssignmentNumber,
            SalesOrderId = dto.SalesOrderId,
            RiderId = dto.RiderId,
            Status = assignment.Status,
            AssignedAt = assignment.AssignedAt,
            DeliveryLatitude = dto.DeliveryLatitude,
            DeliveryLongitude = dto.DeliveryLongitude,
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var rider = await _riders.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Rider not found");

        _riders.Delete(rider);
        await _riders.SaveChangesAsync();

        _logger.LogInformation("Rider deleted: {Id}", id);
    }

    // ?? Mapping ???????????????????????????????????????????????????????????????

    internal static RiderDto MapToDto(Rider r) => new()
    {
        Id = r.Id, RiderCode = r.RiderCode, FirstName = r.FirstName, LastName = r.LastName,
        Phone = r.Phone, Email = r.Email, ProfileImageUrl = r.ProfileImageUrl,
        VehicleType = r.VehicleType, VehiclePlateNumber = r.VehiclePlateNumber,
        Status = r.Status, CurrentLatitude = r.CurrentLatitude,
        CurrentLongitude = r.CurrentLongitude, TotalDeliveries = r.TotalDeliveries,
        AverageRating = r.AverageRating, IsActive = r.IsActive, HomeBranchId = r.HomeBranchId,
    };
}
