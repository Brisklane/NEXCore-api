using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Warehouse outbound: waves, picking, packing, staging and dispatch.
///
/// Two things make this distribution rather than generic warehousing:
///
/// **Waves group by route.** Picking in delivery sequence means the truck loads itself — the last
/// carton picked is the first one off at stop one. Grouping by order number instead produces a
/// perfectly efficient pick and a chaotic load.
///
/// **FEFO deviation leaves a trace.** The allocator nominates a lot; the picker may take another,
/// but only with a reason. A warehouse that overrides FEFO daily is a finding, and without the
/// nominated-versus-picked pair nobody would ever see it.
/// </summary>
public class FulfilmentService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering,
    IEventPublisher events) : IFulfilmentService
{
    public async Task<DispatchBoardDto> GetBoardAsync(Guid? warehouseId, DateTime? date)
    {
        var day = (date ?? DateTime.UtcNow).Date;

        var orders = await db.Orders.ForTenant(tenant)
            .Include(o => o.Outlet).Include(o => o.Partner)
            .Where(o => o.Status >= DistributionOrderStatus.Approved
                        && o.Status <= DistributionOrderStatus.Dispatched)
            .WhereIf(warehouseId.HasValue, o => o.WarehouseId == warehouseId)
            .ToListAsync();

        var board = new DispatchBoardDto
        {
            WarehouseId = warehouseId,
            AsOf = DateTime.UtcNow,
            PendingAllocation = orders.Count(o => o.Status == DistributionOrderStatus.Approved),
            AwaitingPick = orders.Count(o => o.Status == DistributionOrderStatus.Allocated),
            Picking = orders.Count(o => o.Status == DistributionOrderStatus.Picking),
            Packed = orders.Count(o => o.Status == DistributionOrderStatus.Packed),
            Staged = orders.Count(o => o.Status == DistributionOrderStatus.Loaded),
            Dispatched = orders.Count(o => o.Status == DistributionOrderStatus.Dispatched),
            PendingValue = orders.Where(o => o.Status < DistributionOrderStatus.Dispatched).Sum(o => o.TotalAmount),
            DispatchedValue = orders.Where(o => o.Status == DistributionOrderStatus.Dispatched).Sum(o => o.TotalAmount),
        };

        board.ActiveWaves = (await db.PickWaves.ForTenant(tenant)
                .Include(w => w.Tasks.Where(t => !t.IsDeleted))
                .Where(w => !w.IsClosed)
                .WhereIf(warehouseId.HasValue, w => w.WarehouseId == warehouseId)
                .OrderBy(w => w.Priority).ThenBy(w => w.ReleasedAt)
                .Take(20).ToListAsync())
            .Select(MapWave).ToList();

        // Urgent first, then the ones whose promise has already been broken.
        board.UrgentOrders = orders
            .Where(o => o.Kind == DistributionOrderKind.Urgent && o.Status < DistributionOrderStatus.Dispatched)
            .OrderBy(o => o.PromisedDeliveryDate ?? o.OrderDate)
            .Take(20).Select(o => o.ToSummary()).ToList();

        board.LateOrders = orders
            .Where(o => o.PromisedDeliveryDate is not null
                        && o.PromisedDeliveryDate.Value.Date < day
                        && o.Status < DistributionOrderStatus.Delivered)
            .OrderBy(o => o.PromisedDeliveryDate)
            .Take(20).Select(o => o.ToSummary()).ToList();

        board.TodayTrips = (await db.Trips.ForTenant(tenant)
                .Include(t => t.Vehicle).Include(t => t.Driver)
                .Where(t => t.TripDate == day)
                .WhereIf(warehouseId.HasValue, t => t.WarehouseId == warehouseId)
                .ToListAsync())
            .Select(t => t.ToSummary()).ToList();

        return board;
    }

    // ═══ Waves ═══════════════════════════════════════════════════════════════

    public async Task<PickWaveDto> CreateWaveAsync(CreatePickWaveDto request, Guid userId)
    {
        if (request.OrderIds.Count == 0)
            throw new InvalidOperationException("A wave needs at least one order.");

        var orders = await db.Orders.ForTenant(tenant)
            .Include(o => o.Lines.Where(l => !l.IsDeleted))
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync();

        var notReady = orders.Where(o => o.Status < DistributionOrderStatus.Allocated).ToList();
        if (notReady.Count > 0)
            throw new InvalidOperationException(
                $"{notReady.Count} order(s) have not been allocated stock yet.");

        var already = orders.Where(o => o.Status >= DistributionOrderStatus.Picking).ToList();
        if (already.Count > 0)
            throw new InvalidOperationException($"{already.Count} order(s) are already being picked.");

        var wave = new PickWave
        {
            WaveNumber = await numbering.NextWaveNumberAsync(DateTime.UtcNow),
            WarehouseId = request.WarehouseId ?? orders.FirstOrDefault()?.WarehouseId,
            Strategy = request.Strategy,
            ReleasedAt = DateTime.UtcNow,
            ReleasedByUserId = userId,
            RouteId = request.RouteId,
            TripId = request.TripId,
            DeliveryDate = request.DeliveryDate,
            CarrierCutOffAt = request.CarrierCutOffAt,
            Priority = request.Priority,
            OrderCount = orders.Count,
            Note = request.Note,
        }.StampNew(tenant, userId);

        var allocations = await db.StockAllocations.ForTenant(tenant)
            .Where(a => request.OrderIds.Contains(a.OrderId) && a.ReleasedAt == null)
            .ToListAsync();

        // Delivery sequence drives the pick order, so the truck loads in reverse-stop order.
        var sequence = await BuildRouteSequenceAsync(request.RouteId, orders);

        var tasks = request.Strategy switch
        {
            PickStrategy.Discrete => BuildDiscreteTasks(wave, orders, allocations, sequence, userId),
            PickStrategy.Zone => BuildZoneTasks(wave, orders, allocations, sequence, request.Zones, userId),
            _ => BuildBatchTasks(wave, orders, allocations, sequence, userId),
        };

        foreach (var task in tasks) wave.Tasks.Add(task);

        wave.TaskCount = wave.Tasks.Count;
        wave.TotalLines = wave.Tasks.Sum(t => t.Lines.Count);

        db.PickWaves.Add(wave);

        foreach (var order in orders)
        {
            order.Status = DistributionOrderStatus.Picking;
            order.StampUpdated(userId);
            order.StatusEvents.Add(new OrderStatusEvent
            {
                OrderId = order.Id,
                FromStatus = DistributionOrderStatus.Allocated,
                ToStatus = DistributionOrderStatus.Picking,
                OccurredAt = DateTime.UtcNow,
                ActorUserId = userId,
                Note = $"Released on wave {wave.WaveNumber}",
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        return (await GetWaveAsync(wave.Id))!;
    }

    public async Task<PickWaveDto?> GetWaveAsync(Guid waveId)
    {
        var entity = await db.PickWaves.ForTenant(tenant)
            .Include(w => w.Tasks.Where(t => !t.IsDeleted)).ThenInclude(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(w => w.Id == waveId);

        if (entity is null) return null;

        var dto = MapWave(entity);

        if (entity.RouteId.HasValue)
            dto.RouteName = await db.Routes.ForTenant(tenant)
                .Where(r => r.Id == entity.RouteId).Select(r => r.Name).FirstOrDefaultAsync();

        var orderIds = entity.Tasks.SelectMany(t => t.Lines).Where(l => l.OrderId.HasValue)
            .Select(l => l.OrderId!.Value).Distinct().ToList();

        var numbers = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.OrderNumber);

        foreach (var task in dto.Tasks)
        {
            if (task.OrderId.HasValue) task.OrderNumber = numbers.GetValueOrDefault(task.OrderId.Value);
            foreach (var line in task.Lines.Where(l => l.OrderId.HasValue))
                line.OrderNumber = numbers.GetValueOrDefault(line.OrderId!.Value);
        }

        return dto;
    }

    public async Task<PaginatedResponse<PickWaveDto>> ListWavesAsync(
        Guid? warehouseId, bool? openOnly, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.PickWaves.ForTenant(tenant)
            .Include(w => w.Tasks.Where(t => !t.IsDeleted))
            .WhereIf(warehouseId.HasValue, w => w.WarehouseId == warehouseId)
            .WhereIf(openOnly == true, w => !w.IsClosed)
            .WhereIf(from.HasValue, w => w.ReleasedAt >= from)
            .WhereIf(to.HasValue, w => w.ReleasedAt <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(w => w.ReleasedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = MapWave(r);
            dto.Tasks = [];
            return dto;
        });

        return PaginatedResponse<PickWaveDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PickWaveDto> CloseWaveAsync(Guid waveId, Guid userId)
    {
        var wave = await db.PickWaves.ForTenant(tenant)
            .Include(w => w.Tasks.Where(t => !t.IsDeleted)).ThenInclude(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(w => w.Id == waveId)
            ?? throw new InvalidOperationException("That wave no longer exists.");

        var open = wave.Tasks.Count(t => t.Status is PickTaskStatus.Released
                                         or PickTaskStatus.Assigned or PickTaskStatus.InProgress);

        if (open > 0)
            throw new InvalidOperationException($"{open} pick task(s) are still open on this wave.");

        wave.IsClosed = true;
        wave.CompletedAt = DateTime.UtcNow;
        wave.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetWaveAsync(waveId))!;
    }

    // ═══ Picking ═════════════════════════════════════════════════════════════

    public async Task<PickTaskDto?> GetTaskAsync(Guid taskId)
    {
        var entity = await db.PickTasks.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == taskId);

        return entity is null ? null : MapTask(entity);
    }

    public async Task<PickTaskDto> AssignTaskAsync(Guid taskId, Guid userId, string? name, Guid actorUserId)
    {
        var task = await db.PickTasks.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new InvalidOperationException("That pick task no longer exists.");

        if (task.Status >= PickTaskStatus.Picked)
            throw new InvalidOperationException("This task is already finished.");

        task.AssignedToUserId = userId;
        task.AssignedToName = name;
        task.Status = PickTaskStatus.Assigned;
        task.StampUpdated(actorUserId);

        await db.SaveChangesAsync();
        return MapTask(task);
    }

    public async Task<PickTaskDto> StartTaskAsync(Guid taskId, Guid userId)
    {
        var task = await db.PickTasks.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new InvalidOperationException("That pick task no longer exists.");

        task.Status = PickTaskStatus.InProgress;
        task.StartedAt ??= DateTime.UtcNow;
        task.AssignedToUserId ??= userId;
        task.StampUpdated(userId);

        await db.SaveChangesAsync();
        return MapTask(task);
    }

    public async Task<PickTaskDto> ConfirmPickAsync(ConfirmPickDto request, Guid userId)
    {
        var line = await db.PickTaskLines.ForTenant(tenant)
            .Include(l => l.Task)
            .FirstOrDefaultAsync(l => l.Id == request.TaskLineId)
            ?? throw new InvalidOperationException("That pick line no longer exists.");

        if (request.PickedQuantity < 0)
            throw new InvalidOperationException("A picked quantity cannot be negative.");
        if (request.PickedQuantity > line.RequiredQuantity)
            throw new InvalidOperationException(
                $"You cannot pick more than the {line.RequiredQuantity:N0} required.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // FEFO deviation needs a reason. This is the only thing that makes FEFO real rather than
        // a preference the warehouse quietly ignores.
        var deviated = request.PickedBatchId.HasValue
                       && line.NominatedBatchId.HasValue
                       && request.PickedBatchId != line.NominatedBatchId;

        if (deviated)
        {
            if (!(settings?.AllowFefoOverride ?? true))
                throw new InvalidOperationException(
                    $"FEFO requires batch {line.NominatedBatchNumber}. Taking another is not permitted here.");

            if (string.IsNullOrWhiteSpace(request.OverrideReason))
                throw new InvalidOperationException(
                    $"FEFO nominated batch {line.NominatedBatchNumber}. Give a reason for taking a different one.");

            line.IsFefoOverridden = true;
            line.OverrideReason = request.OverrideReason;
        }

        var isShort = request.PickedQuantity < line.RequiredQuantity;
        if (isShort && request.ShortReasonCodeId is null)
            throw new InvalidOperationException("A short pick needs a reason.");

        line.PickedQuantity = request.PickedQuantity;
        line.PickedBatchId = request.PickedBatchId ?? line.NominatedBatchId;
        line.IsShort = isShort;
        line.ShortReasonCodeId = request.ShortReasonCodeId;
        line.PickedAt = DateTime.UtcNow;
        line.PickedByUserId = userId;
        line.StampUpdated(userId);

        if (line.OrderLineId.HasValue)
        {
            var orderLine = await db.OrderLines.ForTenant(tenant)
                .FirstOrDefaultAsync(l => l.Id == line.OrderLineId);

            if (orderLine is not null)
            {
                orderLine.PickedQuantity += request.PickedQuantity;
                orderLine.IsShortPicked = isShort;
                orderLine.ShortPickReasonCodeId = request.ShortReasonCodeId;
                orderLine.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        await RefreshTaskAsync(line.TaskId, userId);

        return (await GetTaskAsync(line.TaskId))!;
    }

    public async Task<PickTaskDto> CompleteTaskAsync(Guid taskId, Guid userId)
    {
        var task = await db.PickTasks.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new InvalidOperationException("That pick task no longer exists.");

        var untouched = task.Lines.Count(l => l.PickedAt is null);
        if (untouched > 0)
            throw new InvalidOperationException($"{untouched} line(s) have not been confirmed yet.");

        task.Status = task.Lines.Any(l => l.IsShort) ? PickTaskStatus.ShortPicked : PickTaskStatus.Picked;
        task.CompletedAt = DateTime.UtcNow;
        task.ShortLineCount = task.Lines.Count(l => l.IsShort);
        task.StampUpdated(userId);

        await db.SaveChangesAsync();
        await AdvanceOrdersAsync(task, userId);
        await RefreshWaveAsync(task.WaveId, userId);

        return (await GetTaskAsync(taskId))!;
    }

    // ═══ Packing & dispatch ══════════════════════════════════════════════════

    public async Task<PackageDto> CreatePackageAsync(CreatePackageDto request, Guid userId)
    {
        if (request.Contents.Count == 0)
            throw new InvalidOperationException("A package needs contents.");

        var package = new PackageUnit
        {
            LicencePlate = await NextLicencePlateAsync(),
            Kind = request.Kind,
            OrderId = request.OrderId,
            WaveId = request.WaveId,
            WeightKg = request.WeightKg,
            LengthCm = request.LengthCm,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            StagingLocation = request.StagingLocation,
            SealNumber = request.SealNumber,
            IsSealed = !string.IsNullOrWhiteSpace(request.SealNumber),
            PackedAt = DateTime.UtcNow,
            PackedByUserId = userId,
        }.StampNew(tenant, userId);

        foreach (var content in request.Contents)
            package.Contents.Add(new PackageContent
            {
                PackageId = package.Id,
                OrderLineId = content.OrderLineId,
                ItemId = content.ItemId,
                ItemName = content.ItemName,
                BatchId = content.BatchId,
                BatchNumber = content.BatchNumber,
                ExpiryDate = content.ExpiryDate,
                Uom = content.Uom,
                Quantity = content.Quantity,
                SerialNumbers = content.SerialNumbers,
            }.StampNew(tenant, userId));

        db.Packages.Add(package);

        if (request.OrderId.HasValue)
        {
            var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == request.OrderId);
            if (order is not null && order.Status == DistributionOrderStatus.Picked)
            {
                order.Status = DistributionOrderStatus.Packed;
                order.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return MapPackage(package);
    }

    public async Task<List<PackageDto>> ListPackagesAsync(Guid? orderId, Guid? tripId, Guid? dispatchId)
        => (await db.Packages.ForTenant(tenant)
                .Include(p => p.Contents.Where(c => !c.IsDeleted))
                .WhereIf(orderId.HasValue, p => p.OrderId == orderId)
                .WhereIf(tripId.HasValue, p => p.TripId == tripId)
                .WhereIf(dispatchId.HasValue, p => p.DispatchId == dispatchId)
                .OrderBy(p => p.LicencePlate)
                .ToListAsync())
            .Select(MapPackage).ToList();

    public async Task<PackageDto> StagePackageAsync(Guid packageId, string stagingLocation, Guid userId)
    {
        var package = await db.Packages.ForTenant(tenant)
            .Include(p => p.Contents.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == packageId)
            ?? throw new InvalidOperationException("That package no longer exists.");

        package.StagingLocation = stagingLocation;
        package.StampUpdated(userId);

        await db.SaveChangesAsync();
        return MapPackage(package);
    }

    public async Task<PackageDto> LoadPackageAsync(Guid packageId, Guid tripId, Guid userId)
    {
        var package = await db.Packages.ForTenant(tenant)
            .Include(p => p.Contents.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == packageId)
            ?? throw new InvalidOperationException("That licence plate is not recognised.");

        // The scan that stops the wrong carton going on the wrong truck.
        if (package.TripId.HasValue && package.TripId != tripId)
            throw new InvalidOperationException(
                $"Carton {package.LicencePlate} belongs to a different trip.");

        if (package.DeliveredAt.HasValue)
            throw new InvalidOperationException($"Carton {package.LicencePlate} has already been delivered.");

        var trip = await db.Trips.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == tripId)
            ?? throw new InvalidOperationException("That trip no longer exists.");

        if (trip.Status >= TripStatus.Departed)
            throw new InvalidOperationException("This trip has already departed.");

        package.TripId = tripId;
        package.LoadedAt = DateTime.UtcNow;
        package.StampUpdated(userId);

        if (package.OrderId.HasValue)
        {
            var order = await db.Orders.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == package.OrderId);
            if (order is not null && order.Status == DistributionOrderStatus.Packed)
            {
                order.Status = DistributionOrderStatus.Loaded;
                order.TripId = tripId;
                order.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return MapPackage(package);
    }

    public async Task<DispatchDto> CreateDispatchAsync(CreateDispatchDto request, Guid userId)
    {
        if (request.OrderIds.Count == 0 && request.PackageIds.Count == 0)
            throw new InvalidOperationException("A dispatch needs orders or packages.");

        var orders = await db.Orders.ForTenant(tenant)
            .Include(o => o.Outlet)
            .Include(o => o.Lines.Where(l => !l.IsDeleted))
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync();

        var packages = await db.Packages.ForTenant(tenant)
            .Include(p => p.Contents.Where(c => !c.IsDeleted))
            .Where(p => request.PackageIds.Contains(p.Id)
                        || (p.OrderId != null && request.OrderIds.Contains(p.OrderId.Value)))
            .ToListAsync();

        var dispatch = new Dispatch
        {
            DispatchNumber = await numbering.NextDispatchNumberAsync(DateTime.UtcNow),
            WarehouseId = request.WarehouseId ?? orders.FirstOrDefault()?.WarehouseId,
            TripId = request.TripId,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            DispatchedAt = DateTime.UtcNow,
            DispatchedByUserId = userId,
            GatePassNumber = request.GatePassNumber,
            TransportDocumentNumber = request.TransportDocumentNumber,
            CarrierName = request.CarrierName,
            AirwayBillNumber = request.AirwayBillNumber,
            FreightCost = request.FreightCost,
            PackageCount = packages.Count,
            TotalWeightKg = packages.Sum(p => p.WeightKg),
            TotalValue = orders.Sum(o => o.TotalAmount),
            Note = request.Note,
        }.StampNew(tenant, userId);

        var sequence = await BuildRouteSequenceAsync(null, orders);

        foreach (var order in orders)
        {
            dispatch.Lines.Add(new DispatchLine
            {
                DispatchId = dispatch.Id,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                OutletId = order.OutletId,
                OutletName = order.Outlet?.Name,
                PackageCount = packages.Count(p => p.OrderId == order.Id),
                Value = order.TotalAmount,
                StopSequence = order.OutletId is not null ? sequence.GetValueOrDefault(order.OutletId.Value) : 0,
            }.StampNew(tenant, userId));

            order.Status = DistributionOrderStatus.Dispatched;
            order.DispatchedAt = DateTime.UtcNow;
            order.TripId ??= request.TripId;
            order.StampUpdated(userId);

            order.StatusEvents.Add(new OrderStatusEvent
            {
                OrderId = order.Id,
                FromStatus = DistributionOrderStatus.Loaded,
                ToStatus = DistributionOrderStatus.Dispatched,
                OccurredAt = DateTime.UtcNow,
                ActorUserId = userId,
                Note = $"Dispatched on {dispatch.DispatchNumber}",
            }.StampNew(tenant, userId));

            foreach (var line in order.Lines.Where(l => !l.IsDeleted))
            {
                line.DispatchedQuantity = line.PickedQuantity > 0 ? line.PickedQuantity : line.BaseQuantity;
                line.StampUpdated(userId);
            }
        }

        foreach (var package in packages)
        {
            package.DispatchId = dispatch.Id;
            package.TripId ??= request.TripId;
            package.StampUpdated(userId);
        }

        db.Dispatches.Add(dispatch);
        await db.SaveChangesAsync();

        // Goods have physically left: Inventory owns the ledger, so the stock leaves there too.
        if (dispatch.WarehouseId.HasValue)
            foreach (var order in orders)
                await events.PublishAsync(new DeliveryPostedEvent
                {
                    DeliveryId = dispatch.Id,
                    SalesOrderId = order.Id,
                    WarehouseId = dispatch.WarehouseId.Value,
                    CompanyId = tenant.CompanyId,
                    BranchId = tenant.BranchId,
                    BusinessUnitId = tenant.BusinessUnitId,
                    CreatedByUserId = userId,
                    Lines = order.Lines.Where(l => !l.IsDeleted).Select(l => new StockDeductionLine
                    {
                        ProductId = l.ItemId,
                        ProductCode = l.ItemCode ?? string.Empty,
                        WarehouseId = dispatch.WarehouseId,
                        Quantity = l.DispatchedQuantity,
                        UnitOfMeasure = l.Uom,
                        UnitCost = l.UnitCost,
                    }).ToList(),
                });

        return (await GetDispatchAsync(dispatch.Id))!;
    }

    public async Task<DispatchDto?> GetDispatchAsync(Guid dispatchId)
    {
        var entity = await db.Dispatches.ForTenant(tenant)
            .Include(d => d.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(d => d.Id == dispatchId);

        if (entity is null) return null;

        var dto = MapDispatch(entity);

        if (entity.TripId.HasValue)
            dto.TripNumber = await db.Trips.ForTenant(tenant)
                .Where(t => t.Id == entity.TripId).Select(t => t.TripNumber).FirstOrDefaultAsync();

        if (entity.VehicleId.HasValue)
            dto.VehicleRegistration = await db.Vehicles.ForTenant(tenant)
                .Where(v => v.Id == entity.VehicleId).Select(v => v.RegistrationNumber).FirstOrDefaultAsync();

        if (entity.DriverId.HasValue)
            dto.DriverName = await db.Drivers.ForTenant(tenant)
                .Where(d => d.Id == entity.DriverId).Select(d => d.FullName).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<PaginatedResponse<DispatchDto>> ListDispatchesAsync(
        Guid? warehouseId, Guid? tripId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Dispatches.ForTenant(tenant)
            .Include(d => d.Lines.Where(l => !l.IsDeleted))
            .WhereIf(warehouseId.HasValue, d => d.WarehouseId == warehouseId)
            .WhereIf(tripId.HasValue, d => d.TripId == tripId)
            .WhereIf(from.HasValue, d => d.DispatchedAt >= from)
            .WhereIf(to.HasValue, d => d.DispatchedAt <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(d => d.DispatchedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<DispatchDto>.Ok(
            rows.Select(MapDispatch), total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<Dictionary<Guid, int>> BuildRouteSequenceAsync(
        Guid? routeId, List<DistributionOrder> orders)
    {
        var outletIds = orders.Where(o => o.OutletId.HasValue).Select(o => o.OutletId!.Value).Distinct().ToList();
        if (outletIds.Count == 0) return [];

        var links = await db.RouteOutlets.ForTenant(tenant)
            .Where(r => outletIds.Contains(r.OutletId))
            .WhereIf(routeId.HasValue, r => r.RouteId == routeId)
            .ToListAsync();

        return links
            .GroupBy(l => l.OutletId)
            .ToDictionary(g => g.Key, g => g.Min(x => x.StopSequence));
    }

    private List<PickTask> BuildDiscreteTasks(
        PickWave wave, List<DistributionOrder> orders, List<StockAllocation> allocations,
        Dictionary<Guid, int> sequence, Guid userId)
    {
        var tasks = new List<PickTask>();
        var number = 0;

        foreach (var order in orders.OrderByDescending(o =>
            o.OutletId is not null ? sequence.GetValueOrDefault(o.OutletId.Value) : 0))
        {
            var task = NewTask(wave, ++number, order.Id, null, userId);
            var pick = 0;

            foreach (var line in order.Lines.Where(l => !l.IsDeleted && !l.IsFreeGoods))
                task.Lines.Add(NewLine(task, order, line, allocations, ++pick, userId));

            task.LineCount = task.Lines.Count;
            if (task.Lines.Count > 0) tasks.Add(task);
        }

        return tasks;
    }

    private List<PickTask> BuildBatchTasks(
        PickWave wave, List<DistributionOrder> orders, List<StockAllocation> allocations,
        Dictionary<Guid, int> sequence, Guid userId)
    {
        // One pass over the whole wave, sorted by bin so the picker walks once and sorts at the end.
        var task = NewTask(wave, 1, null, null, userId);
        var pick = 0;

        var lines = orders
            .SelectMany(o => o.Lines.Where(l => !l.IsDeleted && !l.IsFreeGoods).Select(l => (Order: o, Line: l)))
            .OrderBy(x => x.Line.ItemName)
            .ThenByDescending(x => x.Order.OutletId is not null
                ? sequence.GetValueOrDefault(x.Order.OutletId.Value) : 0);

        foreach (var (order, line) in lines)
            task.Lines.Add(NewLine(task, order, line, allocations, ++pick, userId));

        task.LineCount = task.Lines.Count;
        return task.Lines.Count > 0 ? [task] : [];
    }

    private List<PickTask> BuildZoneTasks(
        PickWave wave, List<DistributionOrder> orders, List<StockAllocation> allocations,
        Dictionary<Guid, int> sequence, List<string> zones, Guid userId)
    {
        if (zones.Count == 0) return BuildBatchTasks(wave, orders, allocations, sequence, userId);

        var tasks = new List<PickTask>();
        var number = 0;

        var allLines = orders
            .SelectMany(o => o.Lines.Where(l => !l.IsDeleted && !l.IsFreeGoods).Select(l => (Order: o, Line: l)))
            .ToList();

        // Without a bin map the split is by item name across zones — deterministic and even,
        // which is the best that can be done until Inventory exposes bin zoning.
        var perZone = (int)Math.Ceiling(allLines.Count / (double)zones.Count);

        for (var i = 0; i < zones.Count; i++)
        {
            var slice = allLines.Skip(i * perZone).Take(perZone).ToList();
            if (slice.Count == 0) continue;

            var task = NewTask(wave, ++number, null, zones[i], userId);
            var pick = 0;

            foreach (var (order, line) in slice.OrderBy(x => x.Line.ItemName))
                task.Lines.Add(NewLine(task, order, line, allocations, ++pick, userId));

            task.LineCount = task.Lines.Count;
            tasks.Add(task);
        }

        return tasks;
    }

    private PickTask NewTask(PickWave wave, int number, Guid? orderId, string? zone, Guid userId)
        => new PickTask
        {
            WaveId = wave.Id,
            TaskNumber = $"{wave.WaveNumber}-{number:D2}",
            Status = PickTaskStatus.Released,
            OrderId = orderId,
            ZoneName = zone,
        }.StampNew(tenant, userId);

    private PickTaskLine NewLine(
        PickTask task, DistributionOrder order, DistributionOrderLine line,
        List<StockAllocation> allocations, int sequence, Guid userId)
    {
        var allocation = allocations.FirstOrDefault(a => a.OrderLineId == line.Id);

        return new PickTaskLine
        {
            TaskId = task.Id,
            OrderId = order.Id,
            OrderLineId = line.Id,
            ItemId = line.ItemId,
            ItemName = line.ItemName,
            ItemCode = line.ItemCode,
            BinId = allocation?.BinId,
            // What FEFO said to take, so a deviation is visible rather than invisible.
            NominatedBatchId = allocation?.BatchId ?? line.BatchId,
            NominatedBatchNumber = allocation?.BatchNumber ?? line.BatchNumber,
            ExpiryDate = allocation?.ExpiryDate ?? line.ExpiryDate,
            Uom = line.Uom,
            RequiredQuantity = line.BaseQuantity,
            PickSequence = sequence,
        }.StampNew(tenant, userId);
    }

    private async Task RefreshTaskAsync(Guid taskId, Guid userId)
    {
        var task = await db.PickTasks.ForTenant(tenant)
            .Include(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null) return;

        task.ShortLineCount = task.Lines.Count(l => l.IsShort);
        if (task.Status == PickTaskStatus.Released) task.Status = PickTaskStatus.InProgress;
        task.StampUpdated(userId);

        await db.SaveChangesAsync();
    }

    private async Task RefreshWaveAsync(Guid waveId, Guid userId)
    {
        var wave = await db.PickWaves.ForTenant(tenant)
            .Include(w => w.Tasks.Where(t => !t.IsDeleted)).ThenInclude(t => t.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(w => w.Id == waveId);

        if (wave is null) return;

        wave.CompletedTaskCount = wave.Tasks.Count(t => t.Status is PickTaskStatus.Picked or PickTaskStatus.ShortPicked);
        wave.PickedLines = wave.Tasks.SelectMany(t => t.Lines).Count(l => l.PickedAt is not null);
        wave.ShortLines = wave.Tasks.SelectMany(t => t.Lines).Count(l => l.IsShort);
        wave.StampUpdated(userId);

        if (wave.CompletedTaskCount == wave.Tasks.Count && wave.Tasks.Count > 0)
        {
            wave.CompletedAt = DateTime.UtcNow;
            wave.IsClosed = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task AdvanceOrdersAsync(PickTask task, Guid userId)
    {
        var orderIds = task.Lines.Where(l => l.OrderId.HasValue)
            .Select(l => l.OrderId!.Value).Distinct().ToList();

        if (orderIds.Count == 0) return;

        var orders = await db.Orders.ForTenant(tenant)
            .Include(o => o.Lines.Where(l => !l.IsDeleted))
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync();

        foreach (var order in orders)
        {
            var sellable = order.Lines.Where(l => !l.IsFreeGoods).ToList();
            if (sellable.Count == 0) continue;

            var fullyPicked = sellable.All(l => l.PickedQuantity > 0);
            if (!fullyPicked) continue;

            order.Status = DistributionOrderStatus.Picked;
            order.FillRatePercent = DistributionMapper.Percent(
                sellable.Sum(l => l.PickedQuantity), sellable.Sum(l => l.BaseQuantity));
            order.StampUpdated(userId);

            order.StatusEvents.Add(new OrderStatusEvent
            {
                OrderId = order.Id,
                FromStatus = DistributionOrderStatus.Picking,
                ToStatus = DistributionOrderStatus.Picked,
                OccurredAt = DateTime.UtcNow,
                ActorUserId = userId,
                Note = order.FillRatePercent < 100 ? $"Short-picked at {order.FillRatePercent:N1}%" : null,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
    }

    private async Task<string> NextLicencePlateAsync()
    {
        var count = await db.Packages.ForCompany(tenant).CountAsync();
        return $"LP{count + 1:D8}";
    }

    private static PickWaveDto MapWave(PickWave e) => new()
    {
        Id = e.Id,
        WaveNumber = e.WaveNumber,
        WarehouseId = e.WarehouseId,
        Strategy = e.Strategy,
        ReleasedAt = e.ReleasedAt,
        CompletedAt = e.CompletedAt,
        RouteId = e.RouteId,
        TripId = e.TripId,
        DeliveryDate = e.DeliveryDate,
        CarrierCutOffAt = e.CarrierCutOffAt,
        Priority = e.Priority,
        OrderCount = e.OrderCount,
        TaskCount = e.TaskCount,
        CompletedTaskCount = e.CompletedTaskCount,
        TotalLines = e.TotalLines,
        PickedLines = e.PickedLines,
        ShortLines = e.ShortLines,
        IsClosed = e.IsClosed,
        Note = e.Note,
        ProgressPercent = DistributionMapper.Percent(e.PickedLines, e.TotalLines),
        Tasks = e.Tasks.Select(MapTask).ToList(),
    };

    private static PickTaskDto MapTask(PickTask e) => new()
    {
        Id = e.Id,
        WaveId = e.WaveId,
        TaskNumber = e.TaskNumber,
        Status = e.Status,
        OrderId = e.OrderId,
        AssignedToUserId = e.AssignedToUserId,
        AssignedToName = e.AssignedToName,
        ZoneName = e.ZoneName,
        StartedAt = e.StartedAt,
        CompletedAt = e.CompletedAt,
        LineCount = e.LineCount,
        ShortLineCount = e.ShortLineCount,
        Lines = e.Lines.OrderBy(l => l.PickSequence).Select(MapPickLine).ToList(),
    };

    private static PickTaskLineDto MapPickLine(PickTaskLine e) => new()
    {
        Id = e.Id,
        OrderId = e.OrderId,
        OrderLineId = e.OrderLineId,
        ItemId = e.ItemId,
        ItemName = e.ItemName,
        ItemCode = e.ItemCode,
        BinId = e.BinId,
        BinCode = e.BinCode,
        NominatedBatchId = e.NominatedBatchId,
        NominatedBatchNumber = e.NominatedBatchNumber,
        PickedBatchId = e.PickedBatchId,
        PickedBatchNumber = e.PickedBatchNumber,
        ExpiryDate = e.ExpiryDate,
        Uom = e.Uom,
        RequiredQuantity = e.RequiredQuantity,
        PickedQuantity = e.PickedQuantity,
        PickSequence = e.PickSequence,
        IsShort = e.IsShort,
        IsFefoOverridden = e.IsFefoOverridden,
        OverrideReason = e.OverrideReason,
        PickedAt = e.PickedAt,
    };

    private static PackageDto MapPackage(PackageUnit e) => new()
    {
        Id = e.Id,
        LicencePlate = e.LicencePlate,
        Kind = e.Kind,
        OrderId = e.OrderId,
        TripId = e.TripId,
        DispatchId = e.DispatchId,
        WeightKg = e.WeightKg,
        LengthCm = e.LengthCm,
        WidthCm = e.WidthCm,
        HeightCm = e.HeightCm,
        PackedAt = e.PackedAt,
        LoadedAt = e.LoadedAt,
        DeliveredAt = e.DeliveredAt,
        StagingLocation = e.StagingLocation,
        IsSealed = e.IsSealed,
        SealNumber = e.SealNumber,
        Contents = e.Contents.Select(c => new PackageContentDto
        {
            OrderLineId = c.OrderLineId,
            ItemId = c.ItemId,
            ItemName = c.ItemName,
            BatchId = c.BatchId,
            BatchNumber = c.BatchNumber,
            ExpiryDate = c.ExpiryDate,
            Uom = c.Uom,
            Quantity = c.Quantity,
            SerialNumbers = c.SerialNumbers,
        }).ToList(),
    };

    private static DispatchDto MapDispatch(Dispatch e) => new()
    {
        Id = e.Id,
        DispatchNumber = e.DispatchNumber,
        WarehouseId = e.WarehouseId,
        TripId = e.TripId,
        VehicleId = e.VehicleId,
        DriverId = e.DriverId,
        DispatchedAt = e.DispatchedAt,
        GatePassNumber = e.GatePassNumber,
        TransportDocumentNumber = e.TransportDocumentNumber,
        CarrierName = e.CarrierName,
        AirwayBillNumber = e.AirwayBillNumber,
        FreightCost = e.FreightCost,
        PackageCount = e.PackageCount,
        TotalWeightKg = e.TotalWeightKg,
        TotalValue = e.TotalValue,
        DriverAcknowledgement = e.DriverAcknowledgement,
        Note = e.Note,
        Lines = e.Lines.OrderBy(l => l.StopSequence).Select(l => new DispatchLineDto
        {
            OrderId = l.OrderId,
            OrderNumber = l.OrderNumber,
            OutletId = l.OutletId,
            OutletName = l.OutletName,
            PackageCount = l.PackageCount,
            Value = l.Value,
            StopSequence = l.StopSequence,
        }).ToList(),
    };
}
