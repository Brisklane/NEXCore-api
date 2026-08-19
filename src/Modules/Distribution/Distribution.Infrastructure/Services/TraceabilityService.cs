using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Batch traceability, cold chain, expiry and recall.
///
/// The trace links are written as goods move so that, during a recall, "which shops have it" is
/// one indexed scan rather than an afternoon of joining invoices. That is the entire justification
/// for the table: the question is rare, but when it is asked it is asked urgently and by someone
/// with a regulator on the phone.
///
/// A cold-chain excursion cannot be closed without a corrective action. An out-of-range log with
/// no action recorded is precisely what an inspector looks for.
/// </summary>
public class TraceabilityService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : ITraceabilityService
{
    // ═══ Cold chain ══════════════════════════════════════════════════════════

    public async Task<List<ColdChainCheckpointDto>> ListCheckpointsAsync(Guid? warehouseId, bool? breachedOnly)
    {
        var rows = await db.ColdChainCheckpoints.ForTenant(tenant)
            .WhereIf(warehouseId.HasValue, c => c.WarehouseId == warehouseId)
            .WhereIf(breachedOnly == true, c => c.IsInBreach)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();
        var ids = rows.Select(r => r.Id).ToList();

        var unresolved = await db.ColdChainLogs.ForTenant(tenant)
            .Where(l => ids.Contains(l.CheckpointId) && l.IsOutOfRange && !l.IsResolved)
            .GroupBy(l => l.CheckpointId)
            .Select(g => new { CheckpointId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CheckpointId, x => x.Count);

        var vehicleIds = rows.Where(r => r.VehicleId.HasValue).Select(r => r.VehicleId!.Value).Distinct().ToList();
        var outletIds = rows.Where(r => r.OutletId.HasValue).Select(r => r.OutletId!.Value).Distinct().ToList();

        var vehicles = await db.Vehicles.ForTenant(tenant)
            .Where(v => vehicleIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.RegistrationNumber);
        var outlets = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);

        foreach (var dto in dtos)
        {
            dto.UnresolvedExcursionCount = unresolved.GetValueOrDefault(dto.Id);
            if (dto.VehicleId.HasValue) dto.VehicleRegistration = vehicles.GetValueOrDefault(dto.VehicleId.Value);
            if (dto.OutletId.HasValue) dto.OutletName = outlets.GetValueOrDefault(dto.OutletId.Value);
        }

        return dtos;
    }

    public async Task<ColdChainCheckpointDto> SaveCheckpointAsync(
        Guid? id, ColdChainCheckpointDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A checkpoint needs a name.");
        if (request.MaxSafeCelsius <= request.MinSafeCelsius)
            throw new InvalidOperationException("The safe maximum must be above the safe minimum.");

        ColdChainCheckpoint entity;
        if (id.HasValue)
        {
            entity = await db.ColdChainCheckpoints.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("That checkpoint no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new ColdChainCheckpoint().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.ColdChainCheckpoints, "CCP")
                : request.Code;
            db.ColdChainCheckpoints.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Kind = request.Kind;
        entity.WarehouseId = request.WarehouseId;
        entity.VehicleId = request.VehicleId;
        entity.VanUnitId = request.VanUnitId;
        entity.OutletId = request.OutletId;
        entity.OutletAssetId = request.OutletAssetId;
        entity.MinSafeCelsius = request.MinSafeCelsius;
        entity.MaxSafeCelsius = request.MaxSafeCelsius;
        entity.CheckIntervalHours = request.CheckIntervalHours <= 0 ? 8 : request.CheckIntervalHours;
        entity.Location = request.Location;
        entity.SensorIdentifier = request.SensorIdentifier;
        entity.IsActive = request.IsActive;
        entity.Note = request.Note;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<ColdChainLogDto> RecordReadingAsync(RecordColdChainReadingDto request, Guid userId)
    {
        var checkpoint = await db.ColdChainCheckpoints.ForTenant(tenant)
            .FirstOrDefaultAsync(c => c.Id == request.CheckpointId)
            ?? throw new InvalidOperationException("That checkpoint no longer exists.");

        var outOfRange = request.ReadingCelsius < checkpoint.MinSafeCelsius
                         || request.ReadingCelsius > checkpoint.MaxSafeCelsius;

        // An out-of-range reading cannot be filed away without saying what was done about it.
        if (outOfRange && string.IsNullOrWhiteSpace(request.CorrectiveAction))
            throw new InvalidOperationException(
                $"{request.ReadingCelsius:N1}°C is outside the safe range of "
                + $"{checkpoint.MinSafeCelsius:N1}–{checkpoint.MaxSafeCelsius:N1}°C. Record what was done about it.");

        var log = new ColdChainLog
        {
            CheckpointId = request.CheckpointId,
            RecordedAt = request.RecordedAt ?? DateTime.UtcNow,
            ReadingCelsius = request.ReadingCelsius,
            IsOutOfRange = outOfRange,
            RecordedByUserId = userId,
            CorrectiveAction = request.CorrectiveAction,
            AffectedStockValue = request.AffectedStockValue,
            PhotoUrl = request.PhotoUrl,
            Note = request.Note,
            IsResolved = outOfRange && !string.IsNullOrWhiteSpace(request.CorrectiveAction),
        }.StampNew(tenant, userId);

        if (log.IsResolved)
        {
            log.ResolvedAt = DateTime.UtcNow;
            log.ResolvedByUserId = userId;
        }

        // How long the excursion ran, measured from the last known-good reading.
        if (outOfRange)
        {
            var lastGood = await db.ColdChainLogs.ForTenant(tenant)
                .Where(l => l.CheckpointId == request.CheckpointId && !l.IsOutOfRange)
                .OrderByDescending(l => l.RecordedAt)
                .Select(l => (DateTime?)l.RecordedAt)
                .FirstOrDefaultAsync();

            if (lastGood.HasValue)
                log.ExcursionMinutes = (int)(log.RecordedAt - lastGood.Value).TotalMinutes;
        }

        db.ColdChainLogs.Add(log);

        checkpoint.LastReadingAt = log.RecordedAt;
        checkpoint.LastReadingCelsius = log.ReadingCelsius;
        checkpoint.IsInBreach = outOfRange;
        checkpoint.StampUpdated(userId);

        if (outOfRange)
            db.Notifications.Add(new DistributionNotification
            {
                Kind = DistributionAlertKind.ColdChainExcursion,
                Severity = AlertSeverity.Critical,
                Title = $"{checkpoint.Name} at {request.ReadingCelsius:N1}°C",
                Body = $"Outside the safe range of {checkpoint.MinSafeCelsius:N1}–{checkpoint.MaxSafeCelsius:N1}°C.",
                ReferenceId = checkpoint.Id,
                ReferenceType = "ColdChainCheckpoint",
                ActionRoute = "/distribution/traceability",
                RaisedAt = DateTime.UtcNow,
            }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return log.ToDto();
    }

    public async Task<PaginatedResponse<ColdChainLogDto>> ListReadingsAsync(
        Guid? checkpointId, bool? excursionsOnly, bool? unresolvedOnly, DateTime? from, DateTime? to,
        PaginationParams pagination)
    {
        var query = db.ColdChainLogs.ForTenant(tenant)
            .Include(l => l.Checkpoint)
            .WhereIf(checkpointId.HasValue, l => l.CheckpointId == checkpointId)
            .WhereIf(excursionsOnly == true, l => l.IsOutOfRange)
            .WhereIf(unresolvedOnly == true, l => l.IsOutOfRange && !l.IsResolved)
            .WhereIf(from.HasValue, l => l.RecordedAt >= from)
            .WhereIf(to.HasValue, l => l.RecordedAt <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(l => l.RecordedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<ColdChainLogDto>.Ok(
            rows.Select(r => r.ToDto()), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ColdChainLogDto> ResolveExcursionAsync(
        Guid logId, string correctiveAction, decimal affectedValue, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(correctiveAction))
            throw new InvalidOperationException("Closing an excursion needs a corrective action.");

        var log = await db.ColdChainLogs.ForTenant(tenant)
            .Include(l => l.Checkpoint)
            .FirstOrDefaultAsync(l => l.Id == logId)
            ?? throw new InvalidOperationException("That reading no longer exists.");

        log.CorrectiveAction = correctiveAction;
        log.AffectedStockValue = affectedValue;
        log.IsResolved = true;
        log.ResolvedAt = DateTime.UtcNow;
        log.ResolvedByUserId = userId;
        log.StampUpdated(userId);

        var stillBreached = await db.ColdChainLogs.ForTenant(tenant)
            .AnyAsync(l => l.CheckpointId == log.CheckpointId && l.IsOutOfRange && !l.IsResolved && l.Id != logId);

        if (!stillBreached && log.Checkpoint is not null)
        {
            log.Checkpoint.IsInBreach = false;
            log.Checkpoint.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return log.ToDto();
    }

    // ═══ Trace ═══════════════════════════════════════════════════════════════

    public async Task<BatchTraceDto?> TraceBatchAsync(Guid? itemId, string batchNumber)
    {
        if (string.IsNullOrWhiteSpace(batchNumber)) return null;

        var links = await db.BatchTraceLinks.ForTenant(tenant)
            .Where(l => l.BatchNumber == batchNumber.Trim())
            .WhereIf(itemId.HasValue, l => l.ItemId == itemId)
            .OrderBy(l => l.MovedAt)
            .ToListAsync();

        if (links.Count == 0) return null;

        var first = links[0];

        var dto = new BatchTraceDto
        {
            ItemId = first.ItemId,
            ItemName = first.ItemName,
            BatchNumber = first.BatchNumber,
            ExpiryDate = first.ExpiryDate,
            ManufactureDate = first.ManufactureDate,
            SupplierLotReference = first.SupplierLotReference,
            SupplierId = first.SupplierId,
            ReceivedQuantity = links.Where(l => l.MovementType == "Receipt").Sum(l => l.Quantity),
            DespatchedQuantity = links.Where(l => l.MovementType == "Sale").Sum(l => l.Quantity),
            ReturnedQuantity = links.Where(l => l.MovementType == "Return").Sum(l => l.Quantity),
        };

        dto.RemainingQuantity = dto.ReceivedQuantity - dto.DespatchedQuantity + dto.ReturnedQuantity;

        dto.Hops = links.Select(l => new BatchTraceHopDto
        {
            MovedAt = l.MovedAt,
            MovementType = l.MovementType,
            DocumentNumber = l.DocumentNumber,
            WarehouseId = l.WarehouseId,
            VanUnitId = l.VanUnitId,
            PartnerId = l.PartnerId,
            OutletId = l.OutletId,
            OutletName = l.OutletName,
            Uom = l.Uom,
            Quantity = l.Quantity,
            Value = l.Value,
        }).ToList();

        // The recall notice list: every distinct destination that received any of it.
        dto.Destinations = links
            .Where(l => l.MovementType == "Sale" && (l.OutletId.HasValue || l.PartnerId.HasValue))
            .GroupBy(l => new { l.OutletId, l.PartnerId })
            .Select(g => new BatchTraceDestinationDto
            {
                OutletId = g.Key.OutletId,
                OutletName = g.First().OutletName,
                PartnerId = g.Key.PartnerId,
                Quantity = g.Sum(x => x.Quantity),
                Value = g.Sum(x => x.Value),
                LastSuppliedAt = g.Max(x => x.MovedAt),
            })
            .OrderByDescending(d => d.Quantity)
            .ToList();

        var outletIds = dto.Destinations.Where(d => d.OutletId.HasValue)
            .Select(d => d.OutletId!.Value).ToList();

        var contacts = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Name, o.OwnerPhone })
            .ToDictionaryAsync(o => o.Id);

        var partnerIds = dto.Destinations.Where(d => d.PartnerId.HasValue)
            .Select(d => d.PartnerId!.Value).Distinct().ToList();

        var partners = await db.Partners.ForTenant(tenant)
            .Where(p => partnerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Phone })
            .ToDictionaryAsync(p => p.Id);

        foreach (var destination in dto.Destinations)
        {
            if (destination.OutletId.HasValue && contacts.TryGetValue(destination.OutletId.Value, out var outlet))
            {
                destination.OutletName ??= outlet.Name;
                destination.Phone = outlet.OwnerPhone;
            }

            if (destination.PartnerId.HasValue && partners.TryGetValue(destination.PartnerId.Value, out var partner))
            {
                destination.PartnerName = partner.Name;
                destination.Phone ??= partner.Phone;
            }
        }

        return dto;
    }

    public async Task<BatchTraceDto?> TraceFromOutletAsync(Guid outletId, Guid itemId, DateTime? asOf)
    {
        var at = asOf ?? DateTime.UtcNow;

        // Backward trace: the complaint arrived from a shop, so start at the shop and walk back
        // to the batch that reached it most recently before the date in question.
        var link = await db.BatchTraceLinks.ForTenant(tenant)
            .Where(l => l.OutletId == outletId && l.ItemId == itemId && l.MovedAt <= at)
            .OrderByDescending(l => l.MovedAt)
            .FirstOrDefaultAsync();

        return link is null ? null : await TraceBatchAsync(itemId, link.BatchNumber);
    }

    // ═══ Recall ══════════════════════════════════════════════════════════════

    public async Task<RecallDto> InitiateRecallAsync(InitiateRecallDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("A recall needs a title.");

        var entity = new ProductRecall
        {
            RecallNumber = await numbering.NextRecallNumberAsync(DateTime.UtcNow),
            Title = request.Title.Trim(),
            Status = RecallStatus.Draft,
            Severity = request.Severity,
            ItemId = request.ItemId,
            BatchId = request.BatchId,
            BatchNumber = request.BatchNumber,
            SupplierId = request.SupplierId,
            InitiatedOn = DateTime.UtcNow,
            TargetCompletionOn = request.TargetCompletionOn
                // Class I is a health risk: three days, not three weeks.
                ?? DateTime.UtcNow.Date.AddDays(request.Severity == RecallSeverity.ClassI ? 3 : 14),
            Reason = request.Reason,
            RegulatoryReference = request.RegulatoryReference,
            PublicNotice = request.PublicNotice,
            InitiatedByUserId = userId,
        }.StampNew(tenant, userId);

        var trace = await TraceBatchAsync(request.ItemId, request.BatchNumber ?? string.Empty);

        entity.ItemName = trace?.ItemName ?? "Unknown item";
        entity.ExpiryDate = trace?.ExpiryDate;
        entity.DespatchedQuantity = trace?.DespatchedQuantity ?? 0;
        entity.AffectedOutletCount = trace?.Destinations.Count ?? 0;
        entity.EstimatedValue = trace?.Destinations.Sum(d => d.Value) ?? 0;

        db.Recalls.Add(entity);
        await db.SaveChangesAsync();

        return (await GetRecallAsync(entity.Id))!;
    }

    public async Task<RecallDto?> GetRecallAsync(Guid recallId)
    {
        var entity = await db.Recalls.ForTenant(tenant)
            .Include(r => r.Notices.Where(n => !n.IsDeleted)).ThenInclude(n => n.Outlet)
            .FirstOrDefaultAsync(r => r.Id == recallId);

        return entity?.ToDto();
    }

    public async Task<PaginatedResponse<RecallDto>> ListRecallsAsync(
        RecallStatus? status, PaginationParams pagination)
    {
        var query = db.Recalls.ForTenant(tenant)
            .WhereIf(status.HasValue, r => r.Status == status);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.InitiatedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.Notices = [];
            return dto;
        });

        return PaginatedResponse<RecallDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<RecallDto> AnnounceRecallAsync(Guid recallId, Guid userId)
    {
        var entity = await db.Recalls.ForTenant(tenant)
            .Include(r => r.Notices.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == recallId)
            ?? throw new InvalidOperationException("That recall no longer exists.");

        if (entity.Status != RecallStatus.Draft)
            throw new InvalidOperationException("This recall has already been announced.");

        var trace = await TraceBatchAsync(entity.ItemId, entity.BatchNumber ?? string.Empty)
            ?? throw new InvalidOperationException(
                "No movement history was found for that batch, so there is nobody to notify.");

        // Build the affected-outlet list from the trace, which is the point of keeping the links.
        foreach (var destination in trace.Destinations)
        {
            if (entity.Notices.Any(n => n.OutletId == destination.OutletId && n.PartnerId == destination.PartnerId))
                continue;

            entity.Notices.Add(new RecallOutletNotice
            {
                RecallId = entity.Id,
                OutletId = destination.OutletId,
                PartnerId = destination.PartnerId,
                DestinationName = destination.OutletName ?? destination.PartnerName,
                SuppliedQuantity = destination.Quantity,
                Value = destination.Value,
                NotifiedAt = DateTime.UtcNow,
                NotificationChannel = "InApp",
            }.StampNew(tenant, userId));

            db.Notifications.Add(new DistributionNotification
            {
                Kind = DistributionAlertKind.RecallAnnounced,
                Severity = entity.Severity == RecallSeverity.ClassI ? AlertSeverity.Critical : AlertSeverity.Warning,
                Title = $"Recall: {entity.Title}",
                Body = $"{destination.OutletName ?? destination.PartnerName} received "
                       + $"{destination.Quantity:N0} of batch {entity.BatchNumber}.",
                ReferenceId = entity.Id,
                ReferenceType = "ProductRecall",
                ActionRoute = "/distribution/traceability",
                RaisedAt = DateTime.UtcNow,
                TargetPartnerId = destination.PartnerId,
            }.StampNew(tenant, userId));
        }

        entity.Status = RecallStatus.Announced;
        entity.AnnouncedAt = DateTime.UtcNow;
        entity.DespatchedQuantity = trace.DespatchedQuantity;
        entity.AffectedOutletCount = entity.Notices.Count;
        entity.NotifiedOutletCount = entity.Notices.Count(n => n.NotifiedAt.HasValue);
        entity.EstimatedValue = entity.Notices.Sum(n => n.Value);
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetRecallAsync(recallId))!;
    }

    public async Task<RecallNoticeDto> UpdateNoticeAsync(
        Guid noticeId, decimal returnedQuantity, bool isClosed, string? note, Guid userId)
    {
        var notice = await db.RecallNotices.ForTenant(tenant)
            .Include(n => n.Outlet).Include(n => n.Recall)
            .FirstOrDefaultAsync(n => n.Id == noticeId)
            ?? throw new InvalidOperationException("That recall notice no longer exists.");

        if (returnedQuantity > notice.SuppliedQuantity)
            throw new InvalidOperationException("More cannot come back than was supplied.");

        notice.ReturnedQuantity = returnedQuantity;
        notice.IsClosed = isClosed;
        notice.Note = note;
        if (returnedQuantity > 0) notice.CollectedAt ??= DateTime.UtcNow;
        notice.AcknowledgedAt ??= DateTime.UtcNow;
        notice.StampUpdated(userId);

        await db.SaveChangesAsync();
        await RefreshRecoveryAsync(notice.RecallId, userId);

        return notice.ToDto();
    }

    public async Task<RecallDto> CompleteRecallAsync(Guid recallId, string closureReport, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(closureReport))
            throw new InvalidOperationException("Closing a recall needs a report.");

        var entity = await db.Recalls.ForTenant(tenant)
            .Include(r => r.Notices.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == recallId)
            ?? throw new InvalidOperationException("That recall no longer exists.");

        var open = entity.Notices.Count(n => !n.IsClosed);
        if (open > 0)
            throw new InvalidOperationException(
                $"{open} outlet(s) have not been closed out. A recall report cannot claim a recovery it does not have.");

        entity.Status = RecallStatus.Completed;
        entity.CompletedAt = DateTime.UtcNow;
        entity.ClosureReport = closureReport;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        await RefreshRecoveryAsync(recallId, userId);

        return (await GetRecallAsync(recallId))!;
    }

    // ═══ Near expiry ═════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<NearExpiryDto>> GetNearExpiryAsync(
        int? withinDays, Guid? warehouseId, Guid? vanUnitId, Guid? partnerId, PaginationParams pagination)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var warning = withinDays ?? settings?.NearExpiryWarningDays ?? 90;
        var critical = settings?.NearExpiryCriticalDays ?? 30;

        var horizon = DateTime.UtcNow.Date.AddDays(warning);
        var results = new List<NearExpiryDto>();

        // Van stock, which nobody else is watching and which travels in a hot vehicle all day.
        if (warehouseId is null || vanUnitId is not null)
        {
            var vanRows = await db.VanStockBalances.ForTenant(tenant)
                .Include(b => b.VanUnit)
                .Where(b => b.Quantity > 0 && b.ExpiryDate != null && b.ExpiryDate <= horizon)
                .WhereIf(vanUnitId.HasValue, b => b.VanUnitId == vanUnitId)
                .ToListAsync();

            results.AddRange(vanRows.Select(b => new NearExpiryDto
            {
                ItemId = b.ItemId,
                ItemName = b.ItemName,
                ItemCode = b.ItemCode,
                BatchId = b.BatchId,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate!.Value,
                DaysToExpiry = (int)(b.ExpiryDate.Value.Date - DateTime.UtcNow.Date).TotalDays,
                Location = b.VanUnit?.Name ?? "Van",
                VanUnitId = b.VanUnitId,
                Uom = b.Uom,
                Quantity = b.Quantity,
                Value = b.Quantity * b.UnitCost,
            }));
        }

        // Channel stock, from the partners' own declarations.
        if (partnerId is not null || warehouseId is null)
        {
            var declaredRows = await db.StockDeclarationLines.ForTenant(tenant)
                .Include(l => l.Declaration).ThenInclude(d => d!.Partner)
                .Where(l => l.Quantity > 0 && l.ExpiryDate != null && l.ExpiryDate <= horizon)
                .WhereIf(partnerId.HasValue, l => l.Declaration != null && l.Declaration.PartnerId == partnerId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(500)
                .ToListAsync();

            results.AddRange(declaredRows.Select(l => new NearExpiryDto
            {
                ItemId = l.ItemId ?? Guid.Empty,
                ItemName = l.ItemName ?? "Unknown",
                BatchId = l.BatchId,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate!.Value,
                DaysToExpiry = (int)(l.ExpiryDate.Value.Date - DateTime.UtcNow.Date).TotalDays,
                Location = l.Declaration?.Partner?.Name ?? "Channel",
                PartnerId = l.Declaration?.PartnerId,
                Uom = l.Uom,
                Quantity = l.Quantity,
                Value = l.TotalValue,
            }));
        }

        foreach (var row in results)
            row.Severity = row.DaysToExpiry <= 0 ? "Expired"
                : row.DaysToExpiry <= critical ? "Critical"
                : row.DaysToExpiry <= warning / 2 ? "Warning" : "Watch";

        // Match against any live liquidation scheme, so the answer comes with an action.
        var liquidation = await db.Schemes.ForCompany(tenant)
            .Include(s => s.Products.Where(p => !p.IsDeleted))
            .Where(s => s.Kind == TradeSchemeKind.Liquidation && s.Status == SchemeStatus.Active)
            .ToListAsync();

        foreach (var row in results)
        {
            var scheme = liquidation.FirstOrDefault(s =>
                s.Products.Count == 0 || s.Products.Any(p => p.ItemId == row.ItemId && !p.IsExcluded));

            if (scheme is null) continue;
            row.LiquidationSchemeId = scheme.Id;
            row.LiquidationSchemeName = scheme.Name;
        }

        var ordered = results
            .OrderBy(r => r.DaysToExpiry).ThenByDescending(r => r.Value)
            .ToList();

        var page = ordered
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToList();

        return PaginatedResponse<NearExpiryDto>.Ok(page, ordered.Count, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task RefreshRecoveryAsync(Guid recallId, Guid userId)
    {
        var entity = await db.Recalls.ForTenant(tenant)
            .Include(r => r.Notices.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == recallId);

        if (entity is null) return;

        entity.RecoveredQuantity = entity.Notices.Sum(n => n.ReturnedQuantity);
        entity.RecoveredValue = entity.Notices
            .Where(n => n.SuppliedQuantity > 0)
            .Sum(n => n.Value / n.SuppliedQuantity * n.ReturnedQuantity);

        entity.RespondedOutletCount = entity.Notices.Count(n => n.AcknowledgedAt.HasValue);
        entity.RecoveryPercent = DistributionMapper.Percent(entity.RecoveredQuantity, entity.DespatchedQuantity);

        if (entity.Status == RecallStatus.Announced && entity.RecoveredQuantity > 0)
            entity.Status = RecallStatus.InProgress;

        entity.StampUpdated(userId);
        await db.SaveChangesAsync();
    }
}
