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
/// The van as a moving warehouse.
///
/// Every quantity change writes a <see cref="VanStockMovement"/>, and the balance is a projection
/// of those movements. That is deliberately more work than keeping a running number: at eight in
/// the evening, when a van is forty units short, the only useful answer is *which transactions*
/// made it short, and a balance alone cannot give one.
///
/// The compartment split is the other load-bearing decision. Goods taken back at a door land in a
/// returns compartment, never back into sellable stock, so a dented case cannot be resold at the
/// next stop by accident.
/// </summary>
public class VanService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering,
    IEventPublisher events) : IVanService
{
    // ═══ Vans ════════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<VanUnitDto>> ListVansAsync(
        string? search, Guid? fieldRepId, PaginationParams pagination)
    {
        var query = db.VanUnits.ForTenant(tenant)
            .Include(v => v.Vehicle).Include(v => v.FieldRep)
            .WhereIf(fieldRepId.HasValue, v => v.FieldRepId == fieldRepId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(v => EF.Functions.ILike(v.Name, term) || EF.Functions.ILike(v.Code ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query.OrderBy(v => v.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();
        var ids = rows.Select(r => r.Id).ToList();

        var stock = await db.VanStockBalances.ForTenant(tenant)
            .Where(b => ids.Contains(b.VanUnitId) && b.Quantity > 0)
            .GroupBy(b => b.VanUnitId)
            .Select(g => new { VanId = g.Key, Value = g.Sum(x => x.Quantity * x.UnitCost), Lines = g.Count() })
            .ToDictionaryAsync(x => x.VanId);

        foreach (var dto in dtos)
        {
            if (!stock.TryGetValue(dto.Id, out var s)) continue;
            dto.StockValue = s.Value;
            dto.StockLineCount = s.Lines;
        }

        var routeNames = await db.Routes.ForTenant(tenant)
            .Where(r => r.VanUnitId != null && ids.Contains(r.VanUnitId.Value))
            .ToDictionaryAsync(r => r.VanUnitId!.Value, r => r.Name);

        foreach (var dto in dtos) dto.RouteName = routeNames.GetValueOrDefault(dto.Id);

        return PaginatedResponse<VanUnitDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<VanUnitDto?> GetVanAsync(Guid vanUnitId)
    {
        var entity = await db.VanUnits.ForTenant(tenant)
            .Include(v => v.Vehicle).Include(v => v.FieldRep)
            .FirstOrDefaultAsync(v => v.Id == vanUnitId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        var summary = await GetStockAsync(vanUnitId, null);
        dto.StockValue = summary.TotalValue;
        dto.StockLineCount = summary.LineCount;

        return dto;
    }

    public async Task<VanUnitDto> SaveVanAsync(Guid? vanUnitId, SaveVanUnitDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A van needs a name.");

        VanUnit entity;
        if (vanUnitId.HasValue)
        {
            entity = await db.VanUnits.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == vanUnitId)
                ?? throw new InvalidOperationException("That van no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new VanUnit().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.VanUnits, "VAN")
                : request.Code;
            db.VanUnits.Add(entity);
        }

        if (vanUnitId.HasValue && !string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code;

        entity.Name = request.Name.Trim();
        entity.WarehouseId = request.WarehouseId;
        entity.HomeWarehouseId = request.HomeWarehouseId;
        entity.VehicleId = request.VehicleId;
        entity.FieldRepId = request.FieldRepId;
        entity.RouteId = request.RouteId;
        entity.CapacityWeightKg = request.CapacityWeightKg;
        entity.CapacityVolumeM3 = request.CapacityVolumeM3;
        entity.IsRefrigerated = request.IsRefrigerated;
        entity.MinSafeCelsius = request.MinSafeCelsius;
        entity.MaxSafeCelsius = request.MaxSafeCelsius;
        entity.IsMultiDay = request.IsMultiDay;
        entity.IsActive = request.IsActive;
        entity.Note = request.Note;

        await db.SaveChangesAsync();
        return (await GetVanAsync(entity.Id))!;
    }

    public async Task DeleteVanAsync(Guid vanUnitId, Guid userId)
    {
        var entity = await db.VanUnits.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == vanUnitId)
            ?? throw new InvalidOperationException("That van no longer exists.");

        var hasStock = await db.VanStockBalances.ForTenant(tenant)
            .AnyAsync(b => b.VanUnitId == vanUnitId && b.Quantity > 0);

        if (hasStock)
            throw new InvalidOperationException("This van still holds stock. Unload it before removing the record.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Stock ═══════════════════════════════════════════════════════════════

    public async Task<VanStockSummaryDto> GetStockAsync(Guid vanUnitId, VanCompartment? compartment)
    {
        var balances = await db.VanStockBalances.ForTenant(tenant)
            .Where(b => b.VanUnitId == vanUnitId && b.Quantity != 0)
            .WhereIf(compartment.HasValue, b => b.Compartment == compartment)
            .OrderBy(b => b.ItemName)
            .ToListAsync();

        var name = await db.VanUnits.ForTenant(tenant)
            .Where(v => v.Id == vanUnitId).Select(v => v.Name).FirstOrDefaultAsync();

        var horizon = DateTime.UtcNow.Date.AddDays(30);

        return new VanStockSummaryDto
        {
            VanUnitId = vanUnitId,
            VanUnitName = name,
            SellableValue = Value(balances, VanCompartment.Sellable),
            ReturnValue = Value(balances, VanCompartment.SaleableReturn),
            DamagedValue = Value(balances, VanCompartment.Damaged),
            ExpiredValue = Value(balances, VanCompartment.Expired),
            TotalValue = balances.Sum(b => b.Quantity * b.UnitCost),
            LineCount = balances.Count,
            NearExpiryLineCount = balances.Count(b => b.ExpiryDate is not null && b.ExpiryDate <= horizon),
            Balances = balances.Select(b => b.ToDto()).ToList(),
        };

        static decimal Value(List<VanStockBalance> rows, VanCompartment c)
            => rows.Where(b => b.Compartment == c).Sum(b => b.Quantity * b.UnitCost);
    }

    public async Task<PaginatedResponse<VanStockMovementDto>> GetMovementsAsync(
        Guid vanUnitId, DateTime? from, DateTime? to, VanMovementKind? kind, PaginationParams pagination)
    {
        var query = db.VanStockMovements.ForTenant(tenant)
            .Where(m => m.VanUnitId == vanUnitId)
            .WhereIf(from.HasValue, m => m.OccurredAt >= from)
            .WhereIf(to.HasValue, m => m.OccurredAt <= to)
            .WhereIf(kind.HasValue, m => m.Kind == kind);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(m => m.OccurredAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<VanStockMovementDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Loading ═════════════════════════════════════════════════════════════

    public async Task<VanLoadSheetDto> CreateLoadAsync(CreateVanLoadDto request, Guid userId)
    {
        var van = await db.VanUnits.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == request.VanUnitId)
            ?? throw new InvalidOperationException("That van no longer exists.");

        if (!van.IsActive)
            throw new InvalidOperationException($"{van.Name} is not active.");

        var loadDate = request.LoadDate == default ? DateTime.UtcNow.Date : request.LoadDate.Date;

        var sheet = new VanLoadSheet
        {
            LoadNumber = await numbering.NextLoadNumberAsync(DateTime.UtcNow),
            VanUnitId = request.VanUnitId,
            RouteId = request.RouteId ?? van.RouteId,
            FieldRepId = request.FieldRepId ?? van.FieldRepId,
            FieldDayId = request.FieldDayId,
            SourceWarehouseId = request.SourceWarehouseId ?? van.HomeWarehouseId,
            LoadDate = loadDate,
            Status = VanLoadStatus.Draft,
            IsSuggested = request.AutoSuggest,
            Note = request.Note,
        }.StampNew(tenant, userId);

        var lines = request.AutoSuggest && request.Lines.Count == 0
            ? await SuggestLoadAsync(van, sheet.RouteId, loadDate)
            : request.Lines;

        var order = 0;
        foreach (var line in lines)
        {
            if (line.RequestedQuantity <= 0 && line.SuggestedQuantity <= 0) continue;

            sheet.Lines.Add(new VanLoadLine
            {
                LoadSheetId = sheet.Id,
                DisplayOrder = order++,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                ItemCode = line.ItemCode,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate,
                Uom = string.IsNullOrWhiteSpace(line.Uom) ? "PCS" : line.Uom,
                UomFactor = line.UomFactor <= 0 ? 1 : line.UomFactor,
                SuggestedQuantity = line.SuggestedQuantity,
                RequestedQuantity = line.RequestedQuantity > 0 ? line.RequestedQuantity : line.SuggestedQuantity,
                UnitCost = line.UnitCost,
                UnitPrice = line.UnitPrice,
                Compartment = line.Compartment,
            }.StampNew(tenant, userId));
        }

        if (sheet.Lines.Count == 0)
            throw new InvalidOperationException("A load sheet needs at least one line.");

        RecomputeSheetTotals(sheet);
        sheet.Status = VanLoadStatus.Requested;

        db.VanLoadSheets.Add(sheet);
        await db.SaveChangesAsync();

        return (await GetLoadAsync(sheet.Id))!;
    }

    public async Task<VanLoadSheetDto?> GetLoadAsync(Guid loadSheetId)
    {
        var entity = await db.VanLoadSheets.ForTenant(tenant)
            .Include(s => s.VanUnit)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == loadSheetId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        if (entity.RouteId.HasValue)
            dto.RouteName = await db.Routes.ForTenant(tenant)
                .Where(r => r.Id == entity.RouteId).Select(r => r.Name).FirstOrDefaultAsync();

        if (entity.FieldRepId.HasValue)
            dto.FieldRepName = await db.FieldReps.ForTenant(tenant)
                .Where(r => r.Id == entity.FieldRepId).Select(r => r.FullName).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<PaginatedResponse<VanLoadSheetDto>> ListLoadsAsync(
        Guid? vanUnitId, VanLoadStatus? status, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.VanLoadSheets.ForTenant(tenant)
            .Include(s => s.VanUnit)
            .WhereIf(vanUnitId.HasValue, s => s.VanUnitId == vanUnitId)
            .WhereIf(status.HasValue, s => s.Status == status)
            .WhereIf(from.HasValue, s => s.LoadDate >= from)
            .WhereIf(to.HasValue, s => s.LoadDate <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(s => s.LoadDate).ThenByDescending(s => s.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.Lines = [];
            return dto;
        });

        return PaginatedResponse<VanLoadSheetDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<VanLoadSheetDto> ApproveLoadAsync(
        Guid loadSheetId, bool isApproved, string? reason, Guid userId)
    {
        var sheet = await db.VanLoadSheets.ForTenant(tenant)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == loadSheetId)
            ?? throw new InvalidOperationException("That load sheet no longer exists.");

        if (sheet.Status is not (VanLoadStatus.Draft or VanLoadStatus.Requested))
            throw new InvalidOperationException("This load sheet has already been decided.");

        if (isApproved)
        {
            sheet.Status = VanLoadStatus.Approved;
            sheet.ApprovedAt = DateTime.UtcNow;
            sheet.ApprovedByUserId = userId;

            // Approving locks the quantities the warehouse will pick against.
            foreach (var line in sheet.Lines.Where(l => l.ApprovedQuantity == 0))
                line.ApprovedQuantity = line.RequestedQuantity;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Rejecting a load sheet needs a reason.");
            sheet.Status = VanLoadStatus.Rejected;
            sheet.RejectionReason = reason;
        }

        sheet.StampUpdated(userId);
        RecomputeSheetTotals(sheet);
        await db.SaveChangesAsync();

        return (await GetLoadAsync(loadSheetId))!;
    }

    public async Task<VanLoadSheetDto> ConfirmLoadAsync(ConfirmVanLoadDto request, Guid userId)
    {
        var sheet = await db.VanLoadSheets.ForTenant(tenant)
            .Include(s => s.Lines.Where(l => !l.IsDeleted))
            .Include(s => s.VanUnit)
            .FirstOrDefaultAsync(s => s.Id == request.LoadSheetId)
            ?? throw new InvalidOperationException("That load sheet no longer exists.");

        if (sheet.Status == VanLoadStatus.Loaded)
            throw new InvalidOperationException("This load has already been confirmed.");
        if (sheet.Status is not (VanLoadStatus.Approved or VanLoadStatus.Picked))
            throw new InvalidOperationException("Approve the load sheet before confirming it onto the van.");

        var variances = 0;

        foreach (var confirm in request.Lines)
        {
            var line = sheet.Lines.FirstOrDefault(l => l.Id == confirm.LineId);
            if (line is null) continue;

            var picked = line.PickedQuantity > 0 ? line.PickedQuantity : line.ApprovedQuantity;

            // The gap between what was picked and what went on the van is the one that matters:
            // it is the difference between a paperwork error and a missing case.
            if (confirm.LoadedQuantity != picked)
            {
                if (string.IsNullOrWhiteSpace(confirm.VarianceReason))
                    throw new InvalidOperationException(
                        $"{line.ItemName}: loaded {confirm.LoadedQuantity:N0} against {picked:N0} picked. A reason is required.");

                line.VarianceReason = confirm.VarianceReason;
                variances++;
            }

            line.PickedQuantity = picked;
            line.LoadedQuantity = confirm.LoadedQuantity;
            line.LineValue = confirm.LoadedQuantity * line.UnitPrice;
            line.StampUpdated(userId);
        }

        // Lines the confirmation did not mention loaded as approved.
        foreach (var line in sheet.Lines.Where(l => l.LoadedQuantity == 0 && request.Lines.All(r => r.LineId != l.Id)))
        {
            line.PickedQuantity = line.ApprovedQuantity;
            line.LoadedQuantity = line.ApprovedQuantity;
            line.LineValue = line.LoadedQuantity * line.UnitPrice;
        }

        sheet.Status = VanLoadStatus.Loaded;
        sheet.LoadedAt = DateTime.UtcNow;
        sheet.LoadedByUserId = userId;
        sheet.VarianceLineCount = variances;
        if (!string.IsNullOrWhiteSpace(request.Note)) sheet.Note = request.Note;
        sheet.StampUpdated(userId);

        RecomputeSheetTotals(sheet);

        foreach (var line in sheet.Lines.Where(l => l.LoadedQuantity > 0))
            await MoveAsync(new MovementRequest(
                sheet.VanUnitId, VanMovementKind.LoadOut, line.ItemId, line.ItemName,
                line.BatchId, line.BatchNumber, line.ExpiryDate, line.Compartment, line.Uom,
                line.LoadedQuantity * line.UomFactor, line.UnitCost, line.UnitPrice,
                sheet.Id, "VanLoadSheet", sheet.LoadNumber, sheet.FieldDayId, null, null, null), userId);

        if (sheet.VanUnit is not null)
        {
            sheet.VanUnit.LastLoadedAt = DateTime.UtcNow;
            sheet.VanUnit.StampUpdated(userId);
        }

        await db.SaveChangesAsync();

        // The stock physically left the warehouse: Inventory owns that ledger, so tell it.
        if (sheet.SourceWarehouseId.HasValue)
            await events.PublishAsync(new DeliveryPostedEvent
            {
                DeliveryId = sheet.Id,
                SalesOrderId = Guid.Empty,
                WarehouseId = sheet.SourceWarehouseId.Value,
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                BusinessUnitId = tenant.BusinessUnitId,
                CreatedByUserId = userId,
                Lines = sheet.Lines.Where(l => l.LoadedQuantity > 0).Select(l => new StockDeductionLine
                {
                    ProductId = l.ItemId,
                    ProductCode = l.ItemCode ?? string.Empty,
                    WarehouseId = sheet.SourceWarehouseId,
                    Quantity = l.LoadedQuantity * l.UomFactor,
                    UnitOfMeasure = l.Uom,
                    UnitCost = l.UnitCost,
                }).ToList(),
            });

        return (await GetLoadAsync(sheet.Id))!;
    }

    // ═══ Transfers ═══════════════════════════════════════════════════════════

    public async Task<VanStockSummaryDto> TransferAsync(VanTransferDto request, Guid userId)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("A transfer needs at least one line.");
        if (request.ToVanUnitId is null && request.ToWarehouseId is null)
            throw new InvalidOperationException("A transfer needs a destination.");
        if (request.ToVanUnitId == request.FromVanUnitId)
            throw new InvalidOperationException("A van cannot transfer to itself.");

        var balances = await db.VanStockBalances.ForTenant(tenant)
            .Where(b => b.VanUnitId == request.FromVanUnitId).ToListAsync();

        foreach (var line in request.Lines)
        {
            var source = balances.FirstOrDefault(b =>
                b.ItemId == line.ItemId && b.Compartment == line.Compartment
                && (line.BatchId is null || b.BatchId == line.BatchId));

            if (source is null || source.Quantity < line.Quantity)
                throw new InvalidOperationException(
                    $"The van does not have {line.Quantity:N0} of that item in {line.Compartment} stock.");

            await MoveAsync(new MovementRequest(
                request.FromVanUnitId, VanMovementKind.TransferOut, source.ItemId, source.ItemName,
                source.BatchId, source.BatchNumber, source.ExpiryDate, line.Compartment, source.Uom,
                -line.Quantity, source.UnitCost, source.UnitPrice,
                null, "VanTransfer", null, null, null, request.ToVanUnitId, request.Reason), userId);

            if (request.ToVanUnitId.HasValue)
                await MoveAsync(new MovementRequest(
                    request.ToVanUnitId.Value, VanMovementKind.TransferIn, source.ItemId, source.ItemName,
                    source.BatchId, source.BatchNumber, source.ExpiryDate, line.Compartment, source.Uom,
                    line.Quantity, source.UnitCost, source.UnitPrice,
                    null, "VanTransfer", null, null, null, request.FromVanUnitId, request.Reason), userId);
        }

        await db.SaveChangesAsync();
        return await GetStockAsync(request.FromVanUnitId, null);
    }

    // ═══ Counting ════════════════════════════════════════════════════════════

    public async Task<VanCycleCountDto> StartCountAsync(StartVanCountDto request, Guid userId)
    {
        var van = await db.VanUnits.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == request.VanUnitId)
            ?? throw new InvalidOperationException("That van no longer exists.");

        var open = await db.VanCycleCounts.ForTenant(tenant)
            .AnyAsync(c => c.VanUnitId == request.VanUnitId && !c.IsApproved && c.LineCount > 0);

        if (open)
            throw new InvalidOperationException("There is already an open count on this van. Finish it first.");

        var balances = await db.VanStockBalances.ForTenant(tenant)
            .Where(b => b.VanUnitId == request.VanUnitId && b.Quantity != 0)
            .WhereIf(request.ItemIds.Count > 0, b => request.ItemIds.Contains(b.ItemId))
            .ToListAsync();

        var count = new VanCycleCount
        {
            CountNumber = await numbering.NextCountNumberAsync(DateTime.UtcNow),
            VanUnitId = request.VanUnitId,
            FieldDayId = request.FieldDayId,
            FieldRepId = request.FieldRepId ?? van.FieldRepId,
            CountedAt = DateTime.UtcNow,
            IsFullCount = request.IsFullCount,
            IsBlind = request.IsBlind,
            LineCount = balances.Count,
        }.StampNew(tenant, userId);

        foreach (var balance in balances)
            count.Lines.Add(new VanCycleCountLine
            {
                CountId = count.Id,
                ItemId = balance.ItemId,
                ItemName = balance.ItemName,
                BatchId = balance.BatchId,
                BatchNumber = balance.BatchNumber,
                ExpiryDate = balance.ExpiryDate,
                Compartment = balance.Compartment,
                Uom = balance.Uom,
                ExpectedQuantity = balance.Quantity,
                UnitCost = balance.UnitCost,
            }.StampNew(tenant, userId));

        db.VanCycleCounts.Add(count);
        await db.SaveChangesAsync();

        var dto = (await GetCountAsync(count.Id))!;

        // A blind count hides the expected figure until the counter has committed to theirs.
        if (count.IsBlind)
            foreach (var line in dto.Lines)
                line.ExpectedQuantity = 0;

        return dto;
    }

    public async Task<VanCycleCountDto> SubmitCountAsync(SubmitVanCountDto request, Guid userId)
    {
        var count = await db.VanCycleCounts.ForTenant(tenant)
            .Include(c => c.Lines.Where(l => !l.IsDeleted))
            .Include(c => c.VanUnit)
            .FirstOrDefaultAsync(c => c.Id == request.CountId)
            ?? throw new InvalidOperationException("That count no longer exists.");

        if (count.IsApproved)
            throw new InvalidOperationException("This count has already been approved.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var variances = 0;
        decimal varianceValue = 0;

        foreach (var submitted in request.Lines)
        {
            var line = count.Lines.FirstOrDefault(l => l.Id == submitted.LineId);
            if (line is null) continue;

            line.CountedQuantity = submitted.CountedQuantity;
            line.VarianceQuantity = submitted.CountedQuantity - line.ExpectedQuantity;
            line.VarianceValue = line.VarianceQuantity * line.UnitCost;
            line.ReasonCodeId = submitted.ReasonCodeId;
            line.ReasonNote = submitted.ReasonNote;
            line.StampUpdated(userId);

            if (line.VarianceQuantity == 0) continue;

            variances++;
            varianceValue += line.VarianceValue;

            // The gate: a variance without a reason cannot be submitted. Everything else in the
            // settlement flow depends on this holding.
            if (line.ReasonCodeId is null)
                throw new InvalidOperationException(
                    $"{line.ItemName} is {Math.Abs(line.VarianceQuantity):N0} {(line.VarianceQuantity < 0 ? "short" : "over")}. A reason is required.");
        }

        count.VarianceLineCount = variances;
        count.VarianceValue = varianceValue;
        count.Note = request.Note;
        count.StampUpdated(userId);

        await db.SaveChangesAsync();

        // Small variances clear themselves; anything above tolerance waits for a supervisor.
        var tolerance = settings?.StockVarianceTolerancePercent ?? 1;
        var expectedValue = count.Lines.Sum(l => l.ExpectedQuantity * l.UnitCost);
        var variancePercent = expectedValue == 0 ? 0 : Math.Abs(varianceValue) / expectedValue * 100;

        if (variancePercent <= tolerance) await ApproveCountAsync(count.Id, userId);

        return (await GetCountAsync(count.Id))!;
    }

    public async Task<VanCycleCountDto> ApproveCountAsync(Guid countId, Guid userId)
    {
        var count = await db.VanCycleCounts.ForTenant(tenant)
            .Include(c => c.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == countId)
            ?? throw new InvalidOperationException("That count no longer exists.");

        if (count.IsApproved) return (await GetCountAsync(countId))!;

        foreach (var line in count.Lines.Where(l => l.VarianceQuantity != 0 && !l.IsAdjusted))
        {
            await MoveAsync(new MovementRequest(
                count.VanUnitId, VanMovementKind.CountAdjustment, line.ItemId, line.ItemName,
                line.BatchId, line.BatchNumber, line.ExpiryDate, line.Compartment, line.Uom,
                line.VarianceQuantity, line.UnitCost, 0,
                count.Id, "VanCycleCount", count.CountNumber, count.FieldDayId, null, null,
                line.ReasonNote ?? "Count adjustment"), userId);

            line.IsAdjusted = true;
            line.StampUpdated(userId);
        }

        count.IsApproved = true;
        count.ApprovedAt = DateTime.UtcNow;
        count.ApprovedByUserId = userId;
        count.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetCountAsync(countId))!;
    }

    public async Task<VanCycleCountDto?> GetCountAsync(Guid countId)
    {
        var entity = await db.VanCycleCounts.ForTenant(tenant)
            .Include(c => c.VanUnit)
            .Include(c => c.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == countId);

        if (entity is null) return null;

        var dto = entity.ToDto();

        var reasonIds = entity.Lines.Where(l => l.ReasonCodeId.HasValue)
            .Select(l => l.ReasonCodeId!.Value).Distinct().ToList();

        if (reasonIds.Count > 0)
        {
            var reasons = await db.ReasonCodes.ForCompany(tenant)
                .Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

            foreach (var line in dto.Lines.Where(l => l.ReasonCodeId.HasValue))
                line.ReasonCodeName = reasons.GetValueOrDefault(line.ReasonCodeId!.Value);
        }

        return dto;
    }

    public async Task<VanCycleCountDto> UnloadAsync(
        Guid vanUnitId, Guid? fieldDayId, SubmitVanCountDto request, Guid userId)
    {
        var count = await db.VanCycleCounts.ForTenant(tenant)
            .FirstOrDefaultAsync(c => c.Id == request.CountId)
            ?? throw new InvalidOperationException("Start a closing count before unloading.");

        var result = await SubmitCountAsync(request, userId);

        // The end-of-day move: saleable returns go back to stock, damaged and expired stay in
        // quarantine. Doing this automatically is what stops a driver quietly reselling a return.
        var balances = await db.VanStockBalances.ForTenant(tenant)
            .Where(b => b.VanUnitId == vanUnitId && b.Quantity > 0
                        && b.Compartment == VanCompartment.SaleableReturn)
            .ToListAsync();

        var van = await db.VanUnits.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == vanUnitId);

        foreach (var balance in balances)
        {
            await MoveAsync(new MovementRequest(
                vanUnitId, VanMovementKind.LoadIn, balance.ItemId, balance.ItemName,
                balance.BatchId, balance.BatchNumber, balance.ExpiryDate, VanCompartment.SaleableReturn,
                balance.Uom, -balance.Quantity, balance.UnitCost, balance.UnitPrice,
                count.Id, "VanUnload", count.CountNumber, fieldDayId, null, null,
                "Returned to warehouse at day end"), userId);
        }

        if (van is not null)
        {
            van.LastSettledAt = DateTime.UtcNow;
            van.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return result;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>Everything one van movement needs. A record keeps the call sites readable.</summary>
    private record MovementRequest(
        Guid VanUnitId, VanMovementKind Kind, Guid ItemId, string ItemName,
        Guid? BatchId, string? BatchNumber, DateTime? ExpiryDate, VanCompartment Compartment,
        string Uom, decimal SignedQuantity, decimal UnitCost, decimal UnitPrice,
        Guid? ReferenceId, string? ReferenceType, string? ReferenceNumber,
        Guid? FieldDayId, Guid? VisitId, Guid? CounterpartyVanId, string? Reason);

    /// <summary>
    /// The single place van stock changes. Writes the movement, then updates the balance from it,
    /// so the ledger and the projection can never disagree.
    /// </summary>
    private async Task MoveAsync(MovementRequest request, Guid userId)
    {
        var balance = await db.VanStockBalances.ForTenant(tenant)
            .FirstOrDefaultAsync(b => b.VanUnitId == request.VanUnitId
                                      && b.ItemId == request.ItemId
                                      && b.BatchId == request.BatchId
                                      && b.Compartment == request.Compartment);

        if (balance is null)
        {
            balance = new VanStockBalance
            {
                VanUnitId = request.VanUnitId,
                ItemId = request.ItemId,
                ItemName = request.ItemName,
                BatchId = request.BatchId,
                BatchNumber = request.BatchNumber,
                ExpiryDate = request.ExpiryDate,
                Compartment = request.Compartment,
                Uom = request.Uom,
                UnitCost = request.UnitCost,
                UnitPrice = request.UnitPrice,
            }.StampNew(tenant, userId);
            db.VanStockBalances.Add(balance);
        }
        else
        {
            balance.StampUpdated(userId);
        }

        balance.Quantity += request.SignedQuantity;
        balance.LastMovementAt = DateTime.UtcNow;
        if (request.UnitCost > 0) balance.UnitCost = request.UnitCost;
        if (request.UnitPrice > 0) balance.UnitPrice = request.UnitPrice;

        if (balance.Quantity < 0)
            throw new InvalidOperationException(
                $"That would take {request.ItemName} on this van below zero.");

        db.VanStockMovements.Add(new VanStockMovement
        {
            VanUnitId = request.VanUnitId,
            FieldDayId = request.FieldDayId,
            VisitId = request.VisitId,
            Kind = request.Kind,
            OccurredAt = DateTime.UtcNow,
            ItemId = request.ItemId,
            ItemName = request.ItemName,
            BatchId = request.BatchId,
            BatchNumber = request.BatchNumber,
            ExpiryDate = request.ExpiryDate,
            Compartment = request.Compartment,
            Uom = request.Uom,
            Quantity = request.SignedQuantity,
            UnitCost = request.UnitCost,
            UnitPrice = request.UnitPrice,
            Value = Math.Abs(request.SignedQuantity) * (request.UnitPrice > 0 ? request.UnitPrice : request.UnitCost),
            BalanceAfter = balance.Quantity,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType,
            ReferenceNumber = request.ReferenceNumber,
            CounterpartyVanUnitId = request.CounterpartyVanId,
            Reason = request.Reason,
        }.StampNew(tenant, userId));
    }

    /// <summary>
    /// Proposes a load from what the beat actually sold over the last four weeks, topped up to
    /// cover today. Crude by design — a rep who does not recognise the numbers will overwrite
    /// them, and a suggestion they distrust is worse than none.
    /// </summary>
    private async Task<List<VanLoadLineDto>> SuggestLoadAsync(VanUnit van, Guid? routeId, DateTime loadDate)
    {
        var since = loadDate.AddDays(-28);

        var history = await db.OrderLines.ForTenant(tenant)
            .Where(l => db.Orders.ForTenant(tenant).Any(o =>
                o.Id == l.OrderId
                && (routeId == null || o.RouteId == routeId)
                && o.OrderDate >= since
                && o.Status != DistributionOrderStatus.Cancelled
                && o.Status != DistributionOrderStatus.Rejected))
            .GroupBy(l => new { l.ItemId, l.ItemName, l.ItemCode, l.Uom, l.UomFactor })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.ItemName,
                g.Key.ItemCode,
                g.Key.Uom,
                g.Key.UomFactor,
                Quantity = g.Sum(x => x.Quantity),
                UnitPrice = g.Average(x => x.UnitPrice),
                UnitCost = g.Average(x => x.UnitCost),
            })
            .ToListAsync();

        var onVan = await db.VanStockBalances.ForTenant(tenant)
            .Where(b => b.VanUnitId == van.Id && b.Compartment == VanCompartment.Sellable)
            .GroupBy(b => b.ItemId)
            .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity);

        var lines = new List<VanLoadLineDto>();

        foreach (var row in history)
        {
            // Four weeks of history over roughly 24 selling days, with a fifth added as headroom.
            var dailyAverage = row.Quantity / 24m;
            var target = Math.Ceiling(dailyAverage * 1.2m);
            var have = onVan.GetValueOrDefault(row.ItemId) / (row.UomFactor <= 0 ? 1 : row.UomFactor);
            var top = Math.Max(0, target - have);

            if (top <= 0) continue;

            lines.Add(new VanLoadLineDto
            {
                ItemId = row.ItemId,
                ItemName = row.ItemName,
                ItemCode = row.ItemCode,
                Uom = row.Uom,
                UomFactor = row.UomFactor,
                SuggestedQuantity = top,
                RequestedQuantity = top,
                UnitPrice = row.UnitPrice,
                UnitCost = row.UnitCost,
                Compartment = VanCompartment.Sellable,
            });
        }

        return lines.OrderByDescending(l => l.SuggestedQuantity * l.UnitPrice).Take(60).ToList();
    }

    private static void RecomputeSheetTotals(VanLoadSheet sheet)
    {
        var lines = sheet.Lines.Where(l => !l.IsDeleted).ToList();

        var quantity = lines.Sum(l =>
            l.LoadedQuantity > 0 ? l.LoadedQuantity
            : l.ApprovedQuantity > 0 ? l.ApprovedQuantity
            : l.RequestedQuantity);

        sheet.TotalCostValue = lines.Sum(l =>
            (l.LoadedQuantity > 0 ? l.LoadedQuantity : l.RequestedQuantity) * l.UnitCost);

        sheet.TotalSaleValue = lines.Sum(l =>
            (l.LoadedQuantity > 0 ? l.LoadedQuantity : l.RequestedQuantity) * l.UnitPrice);

        sheet.VarianceLineCount = lines.Count(l => !string.IsNullOrWhiteSpace(l.VarianceReason));
        _ = quantity;
    }
}
