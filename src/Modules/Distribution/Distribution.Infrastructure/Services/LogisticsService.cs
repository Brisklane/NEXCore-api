using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Fleet, trips and proof of delivery.
///
/// The POD is the part that earns its keep. Line-level acceptance — accepted, short, damaged,
/// rejected — captured at the door, with the credit note raised there and then, is the difference
/// between a dispute settled in ten seconds and one settled by a phone call a week later with
/// nobody able to prove anything.
/// </summary>
public class LogisticsService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering,
    ICreditService credit) : ILogisticsService
{
    // ═══ Fleet ═══════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<VehicleDto>> ListVehiclesAsync(
        string? search, VehicleKind? kind, bool? expiringComplianceOnly, PaginationParams pagination)
    {
        var horizon = DateTime.UtcNow.Date.AddDays(30);

        var query = db.Vehicles.ForTenant(tenant)
            .Include(v => v.ComplianceRecords.Where(c => !c.IsDeleted && !c.IsSuperseded))
            .WhereIf(kind.HasValue, v => v.Kind == kind)
            .WhereIf(expiringComplianceOnly == true,
                v => v.ComplianceRecords.Any(c => !c.IsSuperseded && !c.IsDeleted && c.ExpiresOn <= horizon));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(v => EF.Functions.ILike(v.RegistrationNumber, term)
                                     || EF.Functions.ILike(v.Name ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query.OrderBy(v => v.RegistrationNumber)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            var live = r.ComplianceRecords.Where(c => !c.IsSuperseded && !c.IsDeleted).ToList();
            dto.ExpiringComplianceCount = live.Count(c => c.ExpiresOn <= horizon);
            dto.HasExpiredCompliance = live.Any(c => c.ExpiresOn.Date < DateTime.UtcNow.Date);
            dto.EarliestComplianceExpiry = live.Count == 0 ? null : live.Min(c => c.ExpiresOn);
            return dto;
        });

        return PaginatedResponse<VehicleDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<VehicleDto?> GetVehicleAsync(Guid vehicleId)
    {
        var entity = await db.Vehicles.ForTenant(tenant)
            .Include(v => v.ComplianceRecords.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(v => v.Id == vehicleId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        if (entity.DefaultDriverId.HasValue)
            dto.DefaultDriverName = await db.Drivers.ForTenant(tenant)
                .Where(d => d.Id == entity.DefaultDriverId).Select(d => d.FullName).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<VehicleDto> SaveVehicleAsync(Guid? vehicleId, SaveVehicleDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.RegistrationNumber))
            throw new InvalidOperationException("A vehicle needs a registration number.");

        Vehicle entity;
        if (vehicleId.HasValue)
        {
            entity = await db.Vehicles.ForTenant(tenant)
                .Include(v => v.ComplianceRecords)
                .FirstOrDefaultAsync(v => v.Id == vehicleId)
                ?? throw new InvalidOperationException("That vehicle no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new Vehicle().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.Vehicles, "VEH")
                : request.Code;
            db.Vehicles.Add(entity);
        }

        if (vehicleId.HasValue && !string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code;

        entity.RegistrationNumber = request.RegistrationNumber.Trim().ToUpperInvariant();
        entity.Name = request.Name;
        entity.Kind = request.Kind;
        entity.Ownership = request.Ownership;
        entity.Make = request.Make;
        entity.Model = request.Model;
        entity.ManufactureYear = request.ManufactureYear;
        entity.FuelType = request.FuelType;
        entity.CapacityWeightKg = request.CapacityWeightKg;
        entity.CapacityVolumeM3 = request.CapacityVolumeM3;
        entity.IsRefrigerated = request.IsRefrigerated;
        entity.MinSafeCelsius = request.MinSafeCelsius;
        entity.MaxSafeCelsius = request.MaxSafeCelsius;
        entity.DefaultDriverId = request.DefaultDriverId;
        entity.HomeWarehouseId = request.HomeWarehouseId;
        entity.TerritoryId = request.TerritoryId;
        entity.CurrentOdometerKm = request.CurrentOdometerKm;
        entity.FuelEfficiencyKmPerUnit = request.FuelEfficiencyKmPerUnit;
        entity.IsOutOfService = request.IsOutOfService;
        entity.OutOfServiceReason = request.OutOfServiceReason;
        entity.NextServiceDueOn = request.NextServiceDueOn;
        entity.NextServiceDueAtKm = request.NextServiceDueAtKm;
        entity.IsActive = request.IsActive;
        entity.Note = request.Note;

        // A renewal supersedes the previous certificate rather than overwriting it: the old dates
        // are the evidence that the vehicle was legal last March.
        foreach (var dto in request.ComplianceRecords)
        {
            var existing = dto.Id != Guid.Empty
                ? entity.ComplianceRecords.FirstOrDefault(c => c.Id == dto.Id)
                : null;

            if (existing is null)
            {
                foreach (var prior in entity.ComplianceRecords
                    .Where(c => c.Kind == dto.Kind && !c.IsSuperseded && !c.IsDeleted))
                {
                    prior.IsSuperseded = true;
                    prior.StampUpdated(userId);
                }

                existing = new VehicleCompliance { VehicleId = entity.Id, Kind = dto.Kind }.StampNew(tenant, userId);
                entity.ComplianceRecords.Add(existing);
            }

            existing.DocumentNumber = dto.DocumentNumber;
            existing.Issuer = dto.Issuer;
            existing.IssuedOn = dto.IssuedOn;
            existing.ExpiresOn = dto.ExpiresOn;
            existing.Cost = dto.Cost;
            existing.FileUrl = dto.FileUrl;
            existing.Note = dto.Note;
        }

        await db.SaveChangesAsync();
        return (await GetVehicleAsync(entity.Id))!;
    }

    public async Task DeleteVehicleAsync(Guid vehicleId, Guid userId)
    {
        var entity = await db.Vehicles.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == vehicleId)
            ?? throw new InvalidOperationException("That vehicle no longer exists.");

        var onTrip = await db.Trips.ForTenant(tenant)
            .AnyAsync(t => t.VehicleId == vehicleId && t.Status < TripStatus.Returned);

        if (onTrip) throw new InvalidOperationException("This vehicle is out on a trip.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<List<VehicleComplianceDto>> GetExpiringComplianceAsync(int withinDays)
    {
        var horizon = DateTime.UtcNow.Date.AddDays(withinDays);

        return (await db.VehicleCompliances.ForTenant(tenant)
                .Include(c => c.Vehicle)
                .Where(c => !c.IsSuperseded && c.ExpiresOn <= horizon)
                .OrderBy(c => c.ExpiresOn)
                .ToListAsync())
            .Select(c => c.ToDto()).ToList();
    }

    public async Task<PaginatedResponse<DriverDto>> ListDriversAsync(string? search, PaginationParams pagination)
    {
        var query = db.Drivers.ForTenant(tenant).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(d => EF.Functions.ILike(d.FullName, term)
                                     || EF.Functions.ILike(d.Phone ?? "", term)
                                     || EF.Functions.ILike(d.LicenceNumber ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query.OrderBy(d => d.FullName)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();

        var vehicleIds = rows.Where(r => r.DefaultVehicleId.HasValue)
            .Select(r => r.DefaultVehicleId!.Value).Distinct().ToList();

        var vehicles = await db.Vehicles.ForTenant(tenant)
            .Where(v => vehicleIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.RegistrationNumber);

        foreach (var dto in dtos.Where(d => d.DefaultVehicleId.HasValue))
            dto.DefaultVehicleRegistration = vehicles.GetValueOrDefault(dto.DefaultVehicleId!.Value);

        return PaginatedResponse<DriverDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<DriverDto?> GetDriverAsync(Guid driverId)
    {
        var entity = await db.Drivers.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == driverId);
        return entity?.ToDto();
    }

    public async Task<DriverDto> SaveDriverAsync(Guid? driverId, SaveDriverDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("A driver needs a name.");

        Driver entity;
        if (driverId.HasValue)
        {
            entity = await db.Drivers.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == driverId)
                ?? throw new InvalidOperationException("That driver no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new Driver().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.Drivers, "DRV")
                : request.Code;
            db.Drivers.Add(entity);
        }

        if (driverId.HasValue && !string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code;

        entity.FullName = request.FullName.Trim();
        entity.Phone = request.Phone;
        entity.PhotoUrl = request.PhotoUrl;
        entity.EmployeeId = request.EmployeeId;
        entity.FieldRepId = request.FieldRepId;
        entity.PartnerId = request.PartnerId;
        entity.LicenceNumber = request.LicenceNumber;
        entity.LicenceClass = request.LicenceClass;
        entity.LicenceExpiresOn = request.LicenceExpiresOn;
        entity.DefaultVehicleId = request.DefaultVehicleId;
        entity.JoinedOn = request.JoinedOn;
        entity.LeftOn = request.LeftOn;
        entity.IsActive = request.IsActive;
        entity.Note = request.Note;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task DeleteDriverAsync(Guid driverId, Guid userId)
    {
        var entity = await db.Drivers.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == driverId)
            ?? throw new InvalidOperationException("That driver no longer exists.");

        var onTrip = await db.Trips.ForTenant(tenant)
            .AnyAsync(t => t.DriverId == driverId && t.Status < TripStatus.Returned);

        if (onTrip) throw new InvalidOperationException("This driver is out on a trip.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Trips ═══════════════════════════════════════════════════════════════

    public async Task<TripDto> CreateTripAsync(CreateTripDto request, Guid userId)
    {
        var tripDate = request.TripDate == default ? DateTime.UtcNow.Date : request.TripDate.Date;

        if (request.VehicleId.HasValue)
        {
            var vehicle = await db.Vehicles.ForTenant(tenant)
                .Include(v => v.ComplianceRecords.Where(c => !c.IsSuperseded && !c.IsDeleted))
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId)
                ?? throw new InvalidOperationException("That vehicle no longer exists.");

            if (vehicle.IsOutOfService)
                throw new InvalidOperationException(
                    $"{vehicle.RegistrationNumber} is out of service: {vehicle.OutOfServiceReason}");

            // Refusing to plan a trip on a vehicle with lapsed papers is cheaper than the fine.
            var expired = vehicle.ComplianceRecords
                .Where(c => c.ExpiresOn.Date < tripDate)
                .Select(c => c.Kind.ToString()).ToList();

            if (expired.Count > 0)
                throw new InvalidOperationException(
                    $"{vehicle.RegistrationNumber} has expired {string.Join(", ", expired)}.");
        }

        if (request.DriverId.HasValue)
        {
            var driver = await db.Drivers.ForTenant(tenant)
                .FirstOrDefaultAsync(d => d.Id == request.DriverId)
                ?? throw new InvalidOperationException("That driver no longer exists.");

            if (driver.LicenceExpiresOn is not null && driver.LicenceExpiresOn.Value.Date < tripDate)
                throw new InvalidOperationException($"{driver.FullName}'s licence has expired.");
        }

        var trip = new DeliveryTrip
        {
            TripNumber = await numbering.NextTripNumberAsync(DateTime.UtcNow),
            TripDate = tripDate,
            Status = TripStatus.Planned,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            HelperName = request.HelperName,
            RouteId = request.RouteId,
            WarehouseId = request.WarehouseId,
            FieldRepId = request.FieldRepId,
            PlannedStartAt = request.PlannedStartAt,
            PlannedEndAt = request.PlannedEndAt,
            Note = request.Note,
        }.StampNew(tenant, userId);

        var orders = await db.Orders.ForTenant(tenant)
            .Include(o => o.Outlet)
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync();

        var sequence = request.UseRouteSequence && request.RouteId.HasValue
            ? await db.RouteOutlets.ForTenant(tenant)
                .Where(r => r.RouteId == request.RouteId)
                .ToDictionaryAsync(r => r.OutletId, r => r.StopSequence)
            : [];

        var stopNumber = 0;
        foreach (var order in orders.OrderBy(o =>
            o.OutletId is not null ? sequence.GetValueOrDefault(o.OutletId.Value, int.MaxValue) : int.MaxValue))
        {
            trip.Stops.Add(new TripStop
            {
                TripId = trip.Id,
                StopSequence = ++stopNumber,
                Status = TripStopStatus.Pending,
                OutletId = order.OutletId,
                PartnerId = order.PartnerId,
                OrderId = order.Id,
                DestinationName = order.Outlet?.Name ?? order.CustomerName,
                AddressLine = order.Outlet?.AddressLine,
                Latitude = order.Outlet?.Latitude,
                Longitude = order.Outlet?.Longitude,
                PlannedValue = order.TotalAmount,
            }.StampNew(tenant, userId));

            order.TripId = trip.Id;
            order.StampUpdated(userId);
        }

        trip.PlannedStops = trip.Stops.Count;
        trip.PlannedValue = orders.Sum(o => o.TotalAmount);

        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        return (await GetTripAsync(trip.Id))!;
    }

    public async Task<TripDto?> GetTripAsync(Guid tripId)
    {
        var entity = await db.Trips.ForTenant(tenant)
            .Include(t => t.Vehicle).Include(t => t.Driver)
            .Include(t => t.Stops.Where(s => !s.IsDeleted)).ThenInclude(s => s.Outlet)
            .Include(t => t.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        if (entity.RouteId.HasValue)
            dto.RouteName = await db.Routes.ForTenant(tenant)
                .Where(r => r.Id == entity.RouteId).Select(r => r.Name).FirstOrDefaultAsync();

        var orderIds = entity.Stops.Where(s => s.OrderId.HasValue).Select(s => s.OrderId!.Value).ToList();
        var numbers = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.OrderNumber);

        var reasonIds = entity.Stops.Where(s => s.FailureReasonCodeId.HasValue)
            .Select(s => s.FailureReasonCodeId!.Value).Distinct().ToList();

        var reasons = reasonIds.Count == 0
            ? []
            : await db.ReasonCodes.ForCompany(tenant)
                .Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

        foreach (var stop in dto.Stops)
        {
            if (stop.OrderId.HasValue) stop.OrderNumber = numbers.GetValueOrDefault(stop.OrderId.Value);
            if (stop.FailureReasonCodeId.HasValue)
                stop.FailureReasonName = reasons.GetValueOrDefault(stop.FailureReasonCodeId.Value);
        }

        return dto;
    }

    public async Task<PaginatedResponse<TripSummaryDto>> ListTripsAsync(
        Guid? vehicleId, Guid? driverId, Guid? routeId, TripStatus? status,
        DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Trips.ForTenant(tenant)
            .Include(t => t.Vehicle).Include(t => t.Driver)
            .WhereIf(vehicleId.HasValue, t => t.VehicleId == vehicleId)
            .WhereIf(driverId.HasValue, t => t.DriverId == driverId)
            .WhereIf(routeId.HasValue, t => t.RouteId == routeId)
            .WhereIf(status.HasValue, t => t.Status == status)
            .WhereIf(from.HasValue, t => t.TripDate >= from)
            .WhereIf(to.HasValue, t => t.TripDate <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(t => t.TripDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToSummary()).ToList();

        var routeIds = rows.Where(r => r.RouteId.HasValue).Select(r => r.RouteId!.Value).Distinct().ToList();
        var routes = await db.Routes.ForTenant(tenant)
            .Where(r => routeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

        for (var i = 0; i < rows.Count; i++)
            if (rows[i].RouteId.HasValue) dtos[i].RouteName = routes.GetValueOrDefault(rows[i].RouteId!.Value);

        return PaginatedResponse<TripSummaryDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<TripDto> ResequenceTripAsync(ResequenceTripDto request, Guid userId)
    {
        var stops = await db.TripStops.ForTenant(tenant)
            .Where(s => s.TripId == request.TripId).ToListAsync();

        var seq = 0;
        foreach (var id in request.OrderedStopIds)
        {
            var stop = stops.FirstOrDefault(s => s.Id == id);
            if (stop is null) continue;
            stop.StopSequence = ++seq;
            stop.StampUpdated(userId);
        }

        foreach (var stop in stops.Where(s => !request.OrderedStopIds.Contains(s.Id)).OrderBy(s => s.StopSequence))
        {
            stop.StopSequence = ++seq;
            stop.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return (await GetTripAsync(request.TripId))!;
    }

    public async Task<TripDto> StartTripAsync(StartTripDto request, Guid userId)
    {
        var trip = await LoadTripAsync(request.TripId);

        if (trip.Status >= TripStatus.Departed)
            throw new InvalidOperationException("This trip has already departed.");

        trip.Status = TripStatus.Departed;
        trip.DepartedAt = request.DepartedAt ?? DateTime.UtcNow;
        trip.OdometerOutKm = request.OdometerOutKm;
        trip.FuelIssued = request.FuelIssued;
        trip.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetTripAsync(request.TripId))!;
    }

    public async Task<TripDto> EndTripAsync(EndTripDto request, Guid userId)
    {
        var trip = await LoadTripAsync(request.TripId);

        if (trip.Status < TripStatus.Departed)
            throw new InvalidOperationException("This trip has not left yet.");

        var open = trip.Stops.Count(s => s.Status is TripStopStatus.Pending or TripStopStatus.Arrived);
        if (open > 0)
            throw new InvalidOperationException($"{open} stop(s) have no outcome recorded yet.");

        if (request.OdometerInKm > 0 && request.OdometerInKm < trip.OdometerOutKm)
            throw new InvalidOperationException("The closing odometer is lower than the opening one.");

        trip.Status = TripStatus.Returned;
        trip.ReturnedAt = request.ReturnedAt ?? DateTime.UtcNow;
        trip.OdometerInKm = request.OdometerInKm;
        trip.DistanceKm = Math.Max(0, request.OdometerInKm - trip.OdometerOutKm);
        if (!string.IsNullOrWhiteSpace(request.Note)) trip.Note = request.Note;
        trip.StampUpdated(userId);

        RecomputeTrip(trip);

        if (trip.VehicleId.HasValue && request.OdometerInKm > 0)
            await db.Vehicles.ForTenant(tenant).Where(v => v.Id == trip.VehicleId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.CurrentOdometerKm, request.OdometerInKm));

        await db.SaveChangesAsync();
        return (await GetTripAsync(request.TripId))!;
    }

    public async Task<TripDto> CancelTripAsync(Guid tripId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cancelling a trip needs a reason.");

        var trip = await LoadTripAsync(tripId);

        if (trip.Status >= TripStatus.Departed)
            throw new InvalidOperationException("This trip has already departed and cannot be cancelled.");

        trip.Status = TripStatus.Cancelled;
        trip.CancelReason = reason;
        trip.StampUpdated(userId);

        foreach (var stop in trip.Stops.Where(s => s.OrderId.HasValue))
            await db.Orders.ForTenant(tenant).Where(o => o.Id == stop.OrderId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.TripId, (Guid?)null));

        await db.SaveChangesAsync();
        return (await GetTripAsync(tripId))!;
    }

    public async Task<TripStopDto> ArriveAsync(ArriveAtStopDto request, Guid userId)
    {
        var stop = await db.TripStops.ForTenant(tenant)
            .Include(s => s.Outlet).Include(s => s.Trip)
            .FirstOrDefaultAsync(s => s.Id == request.StopId)
            ?? throw new InvalidOperationException("That stop no longer exists.");

        stop.Status = TripStopStatus.Arrived;
        stop.ArrivedAt = request.ArrivedAt ?? DateTime.UtcNow;
        stop.Latitude = request.Latitude ?? stop.Latitude;
        stop.Longitude = request.Longitude ?? stop.Longitude;
        stop.StampUpdated(userId);

        if (stop.Trip is not null && stop.Trip.Status == TripStatus.Departed)
        {
            stop.Trip.Status = TripStatus.InProgress;
            stop.Trip.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return stop.ToDto();
    }

    public async Task<TripStopDto> FailStopAsync(FailStopDto request, Guid userId)
    {
        var stop = await db.TripStops.ForTenant(tenant)
            .Include(s => s.Outlet).Include(s => s.Trip)
            .FirstOrDefaultAsync(s => s.Id == request.StopId)
            ?? throw new InvalidOperationException("That stop no longer exists.");

        var reason = await db.ReasonCodes.ForCompany(tenant)
            .FirstOrDefaultAsync(r => r.Id == request.ReasonCodeId)
            ?? throw new InvalidOperationException("That reason is not recognised.");

        if (reason.RequiresNote && string.IsNullOrWhiteSpace(request.Note))
            throw new InvalidOperationException($"'{reason.Name}' needs a note.");

        stop.Status = request.RescheduleFor.HasValue ? TripStopStatus.Rescheduled : TripStopStatus.Failed;
        stop.FailureReasonCodeId = request.ReasonCodeId;
        stop.FailureNote = request.Note;
        stop.RescheduledFor = request.RescheduleFor;
        stop.DepartedAt = DateTime.UtcNow;
        stop.StampUpdated(userId);

        // A rescheduled drop comes off this trip and goes back into the pool, ready to be
        // planned again rather than quietly forgotten.
        if (request.RescheduleFor.HasValue && stop.OrderId.HasValue)
            await db.Orders.ForTenant(tenant).Where(o => o.Id == stop.OrderId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.TripId, (Guid?)null)
                    .SetProperty(o => o.PromisedDeliveryDate, request.RescheduleFor));

        await db.SaveChangesAsync();

        if (stop.Trip is not null)
        {
            var trip = await LoadTripAsync(stop.TripId);
            RecomputeTrip(trip);
            await db.SaveChangesAsync();
        }

        return stop.ToDto();
    }

    public async Task<TripExpenseDto> AddExpenseAsync(SaveTripExpenseDto request, Guid userId)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("An expense needs an amount.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var entity = new TripExpense
        {
            TripId = request.TripId,
            Kind = request.Kind,
            Amount = request.Amount,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            IncurredAt = request.IncurredAt == default ? DateTime.UtcNow : request.IncurredAt,
            Reference = request.Reference,
            ReceiptUrl = request.ReceiptUrl,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.TripExpenses.Add(entity);
        await db.SaveChangesAsync();

        var trip = await LoadTripAsync(request.TripId);
        RecomputeTrip(trip);
        await db.SaveChangesAsync();

        return entity.ToDto();
    }

    public async Task<TripExpenseDto> DecideExpenseAsync(
        Guid expenseId, bool isApproved, string? reason, Guid userId)
    {
        var entity = await db.TripExpenses.ForTenant(tenant)
            .Include(e => e.Trip)
            .FirstOrDefaultAsync(e => e.Id == expenseId)
            ?? throw new InvalidOperationException("That expense no longer exists.");

        if (!isApproved && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Rejecting an expense needs a reason.");

        entity.IsApproved = isApproved;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.ApprovedByUserId = userId;
        entity.RejectionReason = isApproved ? null : reason;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    // ═══ Proof of delivery ═══════════════════════════════════════════════════

    public async Task<PodDto> CapturePodAsync(CapturePodDto request, Guid userId)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var replay = await db.ProofsOfDelivery.ForTenant(tenant)
                .Include(p => p.Lines.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(p => p.Note == request.IdempotencyKey);
            if (replay is not null) return replay.ToDto();
        }

        var pod = new ProofOfDelivery
        {
            PodNumber = await numbering.NextPodNumberAsync(DateTime.UtcNow),
            TripId = request.TripId,
            TripStopId = request.TripStopId,
            OrderId = request.OrderId,
            OutletId = request.OutletId,
            PartnerId = request.PartnerId,
            VisitId = request.VisitId,
            DeliveredAt = request.DeliveredAt ?? DateTime.UtcNow,
            ReceivedByName = request.ReceivedByName,
            ReceivedByPhone = request.ReceivedByPhone,
            SignatureImageUrl = request.SignatureImageUrl,
            PhotoUrl = request.PhotoUrl,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            OtpVerified = request.OtpVerified,
            OtpReference = request.OtpReference,
            ReceiptSentTo = request.ReceiptSentTo,
            Note = request.Note ?? request.IdempotencyKey,
        }.StampNew(tenant, userId);

        if (request.Latitude.HasValue && request.OutletId.HasValue)
        {
            var outlet = await db.Outlets.ForTenant(tenant)
                .Where(o => o.Id == request.OutletId)
                .Select(o => new { o.Latitude, o.Longitude, o.GeofenceRadiusMetres })
                .FirstOrDefaultAsync();

            if (outlet?.Latitude is not null && outlet.Longitude is not null)
            {
                var metres = NetworkService.GeoDistanceMetres(
                    request.Latitude.Value, request.Longitude!.Value, outlet.Latitude.Value, outlet.Longitude.Value);

                pod.GeoValidation = metres <= (outlet.GeofenceRadiusMetres > 0 ? outlet.GeofenceRadiusMetres : 150)
                    ? GeoValidation.InsideFence
                    : GeoValidation.OutsideFence;
            }
        }

        // Line-level acceptance. This is the record that ends a delivery dispute, so every
        // shortfall is classified rather than lumped into a note.
        foreach (var line in request.Lines)
        {
            var accepted = line.AcceptedQuantity;
            var shortQty = Math.Max(0, line.DespatchedQuantity - accepted - line.DamagedQuantity - line.RejectedQuantity);

            var outcome = line.DamagedQuantity > 0 ? PodLineOutcome.Damaged
                : line.RejectedQuantity > 0 ? PodLineOutcome.Rejected
                : shortQty > 0 ? PodLineOutcome.ShortReceived
                : PodLineOutcome.Accepted;

            var creditValue = (shortQty + line.DamagedQuantity + line.RejectedQuantity) * line.UnitPrice;

            pod.Lines.Add(new PodLine
            {
                PodId = pod.Id,
                OrderLineId = line.OrderLineId,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                Uom = line.Uom,
                DespatchedQuantity = line.DespatchedQuantity,
                AcceptedQuantity = accepted,
                ShortQuantity = shortQty,
                DamagedQuantity = line.DamagedQuantity,
                RejectedQuantity = line.RejectedQuantity,
                Outcome = outcome,
                UnitPrice = line.UnitPrice,
                CreditValue = creditValue,
                ReasonCodeId = line.ReasonCodeId,
                PhotoUrl = line.PhotoUrl,
                Note = line.Note,
            }.StampNew(tenant, userId));
        }

        pod.DeliveredValue = pod.Lines.Sum(l => l.AcceptedQuantity * l.UnitPrice);
        pod.ShortValue = pod.Lines.Sum(l => l.ShortQuantity * l.UnitPrice);
        pod.DamagedValue = pod.Lines.Sum(l => l.DamagedQuantity * l.UnitPrice);
        pod.RejectedValue = pod.Lines.Sum(l => l.RejectedQuantity * l.UnitPrice);
        pod.IsClean = pod.ShortValue + pod.DamagedValue + pod.RejectedValue == 0;

        db.ProofsOfDelivery.Add(pod);

        if (request.TripStopId.HasValue)
        {
            var stop = await db.TripStops.ForTenant(tenant)
                .FirstOrDefaultAsync(s => s.Id == request.TripStopId);

            if (stop is not null)
            {
                stop.Status = pod.IsClean ? TripStopStatus.Delivered : TripStopStatus.PartiallyDelivered;
                stop.DeliveredValue = pod.DeliveredValue;
                stop.DepartedAt = DateTime.UtcNow;
                stop.ProofOfDeliveryId = pod.Id;
                stop.ServiceMinutes = stop.ArrivedAt is null
                    ? null
                    : (int)(DateTime.UtcNow - stop.ArrivedAt.Value).TotalMinutes;
                stop.StampUpdated(userId);
            }
        }

        if (request.OrderId.HasValue)
        {
            var order = await db.Orders.ForTenant(tenant)
                .Include(o => o.Lines.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order is not null)
            {
                foreach (var podLine in pod.Lines.Where(l => l.OrderLineId.HasValue))
                {
                    var orderLine = order.Lines.FirstOrDefault(l => l.Id == podLine.OrderLineId);
                    if (orderLine is null) continue;
                    orderLine.DeliveredQuantity = podLine.AcceptedQuantity;
                    orderLine.StampUpdated(userId);
                }

                var sellable = order.Lines.Where(l => !l.IsFreeGoods).ToList();
                order.FillRatePercent = DistributionMapper.Percent(
                    sellable.Sum(l => l.DeliveredQuantity), sellable.Sum(l => l.BaseQuantity));

                order.Status = pod.IsClean
                    ? DistributionOrderStatus.Delivered
                    : DistributionOrderStatus.PartiallyDelivered;
                order.DeliveredAt = pod.DeliveredAt;
                order.StampUpdated(userId);

                order.StatusEvents.Add(new OrderStatusEvent
                {
                    OrderId = order.Id,
                    FromStatus = DistributionOrderStatus.Dispatched,
                    ToStatus = order.Status,
                    OccurredAt = DateTime.UtcNow,
                    ActorUserId = userId,
                    Note = pod.IsClean ? null : $"Short/damaged value {pod.ShortValue + pod.DamagedValue:N2}",
                }.StampNew(tenant, userId));
            }
        }

        await db.SaveChangesAsync();

        // The credit note at the door: the customer's ledger is right before the driver leaves.
        if (request.AutoCreditExceptions && !pod.IsClean)
            await RaiseExceptionReturnAsync(pod, userId);

        if (request.Collection is not null)
        {
            request.Collection.OutletId ??= pod.OutletId;
            request.Collection.PartnerId ??= pod.PartnerId;
            request.Collection.TripId ??= pod.TripId;
            var receipt = await credit.RecordCollectionAsync(request.Collection, userId);
            pod.CollectedAmount = receipt.Amount;
            await db.SaveChangesAsync();
        }

        if (pod.TripId.HasValue)
        {
            var trip = await LoadTripAsync(pod.TripId.Value);
            RecomputeTrip(trip);
            await db.SaveChangesAsync();
        }

        return (await GetPodAsync(pod.Id))!;
    }

    public async Task<PodDto?> GetPodAsync(Guid podId)
    {
        var entity = await db.ProofsOfDelivery.ForTenant(tenant)
            .Include(p => p.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == podId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        if (entity.OutletId.HasValue)
            dto.OutletName = await db.Outlets.ForTenant(tenant)
                .Where(o => o.Id == entity.OutletId).Select(o => o.Name).FirstOrDefaultAsync();

        if (entity.OrderId.HasValue)
            dto.OrderNumber = await db.Orders.ForTenant(tenant)
                .Where(o => o.Id == entity.OrderId).Select(o => o.OrderNumber).FirstOrDefaultAsync();

        if (entity.TripId.HasValue)
            dto.TripNumber = await db.Trips.ForTenant(tenant)
                .Where(t => t.Id == entity.TripId).Select(t => t.TripNumber).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<PaginatedResponse<PodDto>> ListPodsAsync(
        Guid? tripId, Guid? outletId, bool? exceptionsOnly, DateTime? from, DateTime? to,
        PaginationParams pagination)
    {
        var query = db.ProofsOfDelivery.ForTenant(tenant)
            .Include(p => p.Lines.Where(l => !l.IsDeleted))
            .WhereIf(tripId.HasValue, p => p.TripId == tripId)
            .WhereIf(outletId.HasValue, p => p.OutletId == outletId)
            .WhereIf(exceptionsOnly == true, p => !p.IsClean && !p.IsExceptionResolved)
            .WhereIf(from.HasValue, p => p.DeliveredAt >= from)
            .WhereIf(to.HasValue, p => p.DeliveredAt <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(p => p.DeliveredAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();

        var outletIds = rows.Where(r => r.OutletId.HasValue).Select(r => r.OutletId!.Value).Distinct().ToList();
        var names = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);

        foreach (var dto in dtos.Where(d => d.OutletId.HasValue))
            dto.OutletName = names.GetValueOrDefault(dto.OutletId!.Value);

        return PaginatedResponse<PodDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PodDto> ResolvePodExceptionAsync(Guid podId, string note, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("Closing a delivery exception needs a note.");

        var pod = await db.ProofsOfDelivery.ForTenant(tenant)
            .Include(p => p.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == podId)
            ?? throw new InvalidOperationException("That delivery record no longer exists.");

        pod.IsExceptionResolved = true;
        pod.ExceptionNote = note;
        pod.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetPodAsync(podId))!;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<DeliveryTrip> LoadTripAsync(Guid tripId)
        => await db.Trips.ForTenant(tenant)
            .Include(t => t.Stops.Where(s => !s.IsDeleted))
            .Include(t => t.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == tripId)
            ?? throw new InvalidOperationException("That trip no longer exists.");

    private static void RecomputeTrip(DeliveryTrip trip)
    {
        var stops = trip.Stops.Where(s => !s.IsDeleted).ToList();

        trip.PlannedStops = stops.Count;
        trip.CompletedStops = stops.Count(s => s.Status is TripStopStatus.Delivered
                                               or TripStopStatus.PartiallyDelivered);
        trip.FailedStops = stops.Count(s => s.Status is TripStopStatus.Failed
                                            or TripStopStatus.Refused or TripStopStatus.Rescheduled);

        trip.PlannedValue = stops.Sum(s => s.PlannedValue);
        trip.DeliveredValue = stops.Sum(s => s.DeliveredValue);
        trip.CollectedAmount = stops.Sum(s => s.CollectedAmount);
        trip.ReturnValue = stops.Sum(s => s.ReturnValue);
        trip.TotalExpense = trip.Expenses.Where(e => !e.IsDeleted).Sum(e => e.Amount);

        trip.FillRatePercent = DistributionMapper.Percent(trip.DeliveredValue, trip.PlannedValue);

        var timed = stops.Where(s => s.PlannedArrivalAt is not null && s.ArrivedAt is not null).ToList();
        trip.OnTimePercent = timed.Count == 0
            ? 100
            : DistributionMapper.Percent(timed.Count(s => s.ArrivedAt <= s.PlannedArrivalAt), timed.Count);
    }

    /// <summary>
    /// Turns a short or damaged POD into a return authorisation, already approved. The goods never
    /// arrived in sellable condition, so there is nothing left to decide — only to record.
    /// </summary>
    private async Task RaiseExceptionReturnAsync(ProofOfDelivery pod, Guid userId)
    {
        var exceptionLines = pod.Lines
            .Where(l => l.ShortQuantity > 0 || l.DamagedQuantity > 0 || l.RejectedQuantity > 0)
            .ToList();

        if (exceptionLines.Count == 0) return;

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var authorisation = new ReturnAuthorisation
        {
            ReturnNumber = await numbering.NextReturnNumberAsync(DateTime.UtcNow),
            Kind = exceptionLines.Any(l => l.DamagedQuantity > 0) ? ReturnKind.Damaged : ReturnKind.WrongSupply,
            Status = Domain.Enums.ReturnStatus.Approved,
            OutletId = pod.OutletId,
            PartnerId = pod.PartnerId,
            OriginalOrderId = pod.OrderId,
            RequestedOn = pod.DeliveredAt,
            ApprovedAt = DateTime.UtcNow,
            ApprovedByUserId = userId,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            ReasonNote = $"Raised automatically from delivery {pod.PodNumber}",
            PhotoUrl = pod.PhotoUrl,
        }.StampNew(tenant, userId);

        var order = 0;
        foreach (var line in exceptionLines)
        {
            var quantity = line.ShortQuantity + line.DamagedQuantity + line.RejectedQuantity;

            authorisation.Lines.Add(new ReturnAuthorisationLine
            {
                ReturnId = authorisation.Id,
                DisplayOrder = order++,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                Uom = line.Uom,
                RequestedQuantity = quantity,
                ApprovedQuantity = quantity,
                UnitPrice = line.UnitPrice,
                LineValue = line.CreditValue,
                ReasonCodeId = line.ReasonCodeId,
                PhotoUrl = line.PhotoUrl,
                Note = line.Note,
            }.StampNew(tenant, userId));
        }

        authorisation.ClaimedValue = authorisation.Lines.Sum(l => l.LineValue);
        authorisation.ApprovedValue = authorisation.ClaimedValue;

        db.Returns.Add(authorisation);
        await db.SaveChangesAsync();

        pod.CreditNoteId = authorisation.Id;
        await db.SaveChangesAsync();
    }
}
