using Microsoft.EntityFrameworkCore;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The inventory board and everything that acts on a unit.
///
/// This is the screen a developer's whole sales floor lives on, so two things matter more than
/// anything else here. **Speed**: the board renders five thousand units, so the read is one query
/// with one projection and no navigation loading. **Correctness under contention**: two
/// salespeople will try to hold or book the same unit in the same second, and exactly one of them
/// has to win — which is why holds and bookings re-read the unit inside the transaction rather
/// than trusting what the client sent.
/// </summary>
public class InventoryService(
    RealEstateDbContext db,
    IRealEstateTenant tenant)
    : RealEstateServiceBase(db, tenant), IInventoryService
{
    // ═══ The board ═══════════════════════════════════════════════════════════

    public async Task<InventoryBoardDto> GetBoardAsync(InventoryQueryDto query)
    {
        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == query.ProjectId)
            ?? throw new InvalidOperationException("That project does not exist.");

        var areaUnit = await AreaUnitAsync(project.OfficeId);
        var minSqFt = query.MinArea is null ? (decimal?)null : RealEstateMapper.ToSquareFeet(query.MinArea.Value, query.InputAreaUnit);
        var maxSqFt = query.MaxArea is null ? (decimal?)null : RealEstateMapper.ToSquareFeet(query.MaxArea.Value, query.InputAreaUnit);

        // One join, one projection. Anything the tile does not paint is not selected.
        var rows = await (
            from u in Db.Units.ForCompany(Tenant)
            join p in Db.Properties.ForCompany(Tenant) on u.PropertyId equals p.Id
            where u.ProjectId == query.ProjectId
            select new BoardRow
            {
                Id = u.Id,
                PropertyId = u.PropertyId,
                UnitNumber = u.UnitNumber,
                Status = u.Status,
                SubType = p.SubType,
                ProjectNodeId = u.ProjectNodeId,
                FloorNumber = p.FloorNumber,
                FloorLabel = p.FloorLabel,
                StackIndex = u.StackIndex,
                AreaSqFt = p.SaleableAreaSqFt ?? 0m,
                Bedrooms = p.Bedrooms,
                Facing = p.Facing,
                IsCorner = p.IsCorner,
                BasePrice = u.BasePrice,
                TotalPrice = u.TotalPrice,
                RatePerSqFt = u.BaseRatePerSqFt,
                CurrencyCode = u.CurrencyCode ?? project.CurrencyCode,
                CurrentHoldId = u.CurrentHoldId,
                CurrentBookingId = u.CurrentBookingId,
                CurrentBlockId = u.CurrentBlockId,
                LandownerAllocationId = u.LandownerAllocationId,
                HasLitigation = p.HasLitigation,
                IsMortgaged = p.IsMortgaged,
                SitePlanShapeId = u.SitePlanShapeId,
            })
            .WhereIf(query.ProjectNodeId.HasValue, r => r.ProjectNodeId == query.ProjectNodeId)
            .WhereIf(query.Statuses.Count > 0, r => query.Statuses.Contains(r.Status))
            .WhereIf(query.SubTypes.Count > 0, r => query.SubTypes.Contains(r.SubType))
            .WhereIf(minSqFt.HasValue, r => r.AreaSqFt >= minSqFt)
            .WhereIf(maxSqFt.HasValue, r => r.AreaSqFt <= maxSqFt)
            .WhereIf(query.MinPrice.HasValue, r => r.TotalPrice >= query.MinPrice)
            .WhereIf(query.MaxPrice.HasValue, r => r.TotalPrice <= query.MaxPrice)
            .WhereIf(query.Bedrooms.HasValue, r => r.Bedrooms == query.Bedrooms)
            .WhereIf(query.Facing.HasValue, r => r.Facing == query.Facing)
            .WhereIf(query.CornerOnly == true, r => r.IsCorner)
            .WhereIf(query.FromFloor.HasValue, r => r.FloorNumber >= query.FromFloor)
            .WhereIf(query.ToFloor.HasValue, r => r.FloorNumber <= query.ToFloor)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), r => r.UnitNumber.Contains(query.Search!))
            .OrderBy(r => r.FloorNumber).ThenBy(r => r.UnitNumber)
            .ToListAsync();

        // Live state, resolved in three batch reads rather than one per tile.
        var holdIds = rows.Where(r => r.CurrentHoldId.HasValue).Select(r => r.CurrentHoldId!.Value).ToList();
        var bookingIds = rows.Where(r => r.CurrentBookingId.HasValue).Select(r => r.CurrentBookingId!.Value).ToList();
        var blockIds = rows.Where(r => r.CurrentBlockId.HasValue).Select(r => r.CurrentBlockId!.Value).ToList();

        var holds = holdIds.Count == 0
            ? []
            : await Db.UnitHolds.ForCompany(Tenant)
                .Where(h => holdIds.Contains(h.Id))
                .Select(h => new { h.Id, h.PartyId, h.ExpiresAt, h.HeldByUserId, h.Status })
                .ToDictionaryAsync(h => h.Id, h => h);

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant)
                .Where(b => bookingIds.Contains(b.Id))
                .Select(b => new { b.Id, b.PrimaryApplicantPartyId, b.CollectionPercent })
                .ToDictionaryAsync(b => b.Id, b => b);

        var blocks = blockIds.Count == 0
            ? []
            : await Db.UnitBlockRecords.ForCompany(Tenant)
                .Where(b => blockIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Reason })
                .ToDictionaryAsync(b => b.Id, b => b.Reason);

        var partyIds = holds.Values.Where(h => h.PartyId.HasValue).Select(h => h.PartyId!.Value)
            .Concat(bookings.Values.Select(b => b.PrimaryApplicantPartyId))
            .ToList();
        var names = await PartyNamesAsync(partyIds);

        var shapes = query.ViewMode == "plan" && query.SitePlanId.HasValue
            ? await Db.SitePlanShapes.ForCompany(Tenant)
                .Where(s => s.SitePlanId == query.SitePlanId)
                .ToListAsync()
            : [];

        var shapeByUnit = shapes.Where(s => s.UnitId.HasValue).ToDictionary(s => s.UnitId!.Value, s => s);
        var now = DateTime.UtcNow;

        var units = rows.Select(r =>
        {
            var hold = r.CurrentHoldId is not null ? holds.GetValueOrDefault(r.CurrentHoldId.Value) : null;
            var booking = r.CurrentBookingId is not null ? bookings.GetValueOrDefault(r.CurrentBookingId.Value) : null;
            var shape = shapeByUnit.GetValueOrDefault(r.Id);

            return new InventoryUnitDto
            {
                Id = r.Id,
                PropertyId = r.PropertyId,
                UnitNumber = r.UnitNumber,
                Status = r.Status,
                SubType = r.SubType,
                ProjectNodeId = r.ProjectNodeId,
                FloorNumber = r.FloorNumber,
                FloorLabel = r.FloorLabel,
                StackIndex = r.StackIndex,
                AreaSqFt = r.AreaSqFt,
                AreaDisplay = RealEstateMapper.Area(r.AreaSqFt, areaUnit).DisplayText,
                Bedrooms = r.Bedrooms,
                Facing = r.Facing,
                IsCorner = r.IsCorner,
                BasePrice = r.BasePrice,
                TotalPrice = r.TotalPrice,
                RatePerSqFt = r.RatePerSqFt,
                CurrencyCode = r.CurrencyCode,
                HoldId = hold is null || hold.Status != HoldStatus.Active ? null : r.CurrentHoldId,
                HeldForName = hold?.PartyId is null ? null : names.GetValueOrDefault(hold.PartyId.Value),
                HoldExpiresAt = hold?.ExpiresAt,
                HoldMinutesRemaining = hold is null ? null : Math.Max(0, (int)(hold.ExpiresAt - now).TotalMinutes),
                BookingId = r.CurrentBookingId,
                BuyerName = booking is null ? null : names.GetValueOrDefault(booking.PrimaryApplicantPartyId),
                CollectionPercent = booking?.CollectionPercent,
                BlockReason = r.CurrentBlockId is null ? null : blocks.GetValueOrDefault(r.CurrentBlockId.Value),
                IsLandownerShare = r.LandownerAllocationId.HasValue,
                HasLitigation = r.HasLitigation,
                IsMortgaged = r.IsMortgaged,
                SitePlanShapeId = shape?.Id ?? r.SitePlanShapeId,
                PlanPoints = shape?.Points,
            };
        }).ToList();

        var blocksTree = await BuildBlockTreeAsync(query.ProjectId, units);

        var board = new InventoryBoardDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            CurrencyCode = project.CurrencyCode,
            DisplayAreaUnit = areaUnit,
            Units = units,
            Blocks = blocksTree,
            TotalUnits = units.Count,
            TotalValue = units.Sum(u => u.TotalPrice),
            AvailableValue = units.Where(u => u.Status == PropertyStatus.Available).Sum(u => u.TotalPrice),
            SoldValue = units.Where(u => u.Status is PropertyStatus.Sold or PropertyStatus.Booked
                                          or PropertyStatus.Registered or PropertyStatus.Possessed)
                             .Sum(u => u.TotalPrice),
            Floors = units.Where(u => u.FloorNumber.HasValue).Select(u => u.FloorNumber!.Value).Distinct().OrderByDescending(f => f).ToList(),
            StackCodes = units.Where(u => u.StackIndex.HasValue).Select(u => u.StackIndex!.Value.ToString()).Distinct().OrderBy(s => s).ToList(),
        };

        board.AbsorptionPercent = RealEstateMapper.Percent(board.SoldValue, board.TotalValue);

        board.StatusCounts = units
            .GroupBy(u => u.Status)
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Count = g.Count(),
                Value = g.Sum(x => x.TotalPrice),
                Percent = RealEstateMapper.Percent(g.Count(), units.Count),
                Tone = ToneFor(g.Key),
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        if (query.ViewMode == "plan")
        {
            var plan = query.SitePlanId.HasValue
                ? await Db.SitePlans.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == query.SitePlanId)
                : await Db.SitePlans.ForCompany(Tenant)
                    .Where(s => s.ProjectId == query.ProjectId)
                    .OrderByDescending(s => s.IsDefault).ThenBy(s => s.SortOrder)
                    .FirstOrDefaultAsync();

            if (plan is not null)
            {
                var planShapes = shapes.Count > 0
                    ? shapes
                    : await Db.SitePlanShapes.ForCompany(Tenant).Where(s => s.SitePlanId == plan.Id).ToListAsync();

                var statusByUnit = units.ToDictionary(u => u.Id, u => u.Status);

                board.SitePlan = new SitePlanDto
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    ImageUrl = plan.ImageUrl,
                    ImageWidthPx = plan.ImageWidthPx,
                    ImageHeightPx = plan.ImageHeightPx,
                    IsDefault = plan.IsDefault,
                    Shapes = planShapes.Select(s => new SitePlanShapeDto
                    {
                        Id = s.Id,
                        UnitId = s.UnitId,
                        Label = s.Label,
                        ShapeType = s.ShapeType,
                        Points = s.Points,
                        LabelX = s.LabelX,
                        LabelY = s.LabelY,
                        IsDecorative = s.IsDecorative,
                        FillOverride = s.FillOverride,
                        Status = s.UnitId is null ? null : statusByUnit.GetValueOrDefault(s.UnitId.Value),
                    }).ToList(),
                };
            }
        }

        return board;
    }

    /// <summary>Colour family per status, so the board and its legend never disagree.</summary>
    private static string ToneFor(PropertyStatus status) => status switch
    {
        PropertyStatus.Available => "success",
        PropertyStatus.Held => "warning",
        PropertyStatus.Reserved => "warning",
        PropertyStatus.Booked => "brand",
        PropertyStatus.Sold or PropertyStatus.Registered => "violet",
        PropertyStatus.Possessed => "cyan",
        PropertyStatus.Blocked or PropertyStatus.NotForSale => "muted",
        PropertyStatus.Litigation => "danger",
        _ => "muted",
    };

    private async Task<List<ProjectNodeDto>> BuildBlockTreeAsync(Guid projectId, List<InventoryUnitDto> units)
    {
        var nodes = await Db.ProjectNodes.ForCompany(Tenant)
            .Where(n => n.ProjectId == projectId)
            .OrderBy(n => n.Depth).ThenBy(n => n.SortOrder)
            .ToListAsync();

        var byNode = units.Where(u => u.ProjectNodeId.HasValue)
            .GroupBy(u => u.ProjectNodeId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<ProjectNodeDto> Build(Guid? parentId) => nodes
            .Where(n => n.ParentNodeId == parentId)
            .Select(n =>
            {
                var mine = byNode.GetValueOrDefault(n.Id) ?? [];
                var children = Build(n.Id);

                return new ProjectNodeDto
                {
                    Id = n.Id,
                    ParentNodeId = n.ParentNodeId,
                    Kind = n.Kind,
                    Name = n.Name,
                    Code = n.Code,
                    SortOrder = n.SortOrder,
                    Depth = n.Depth,
                    FloorNumber = n.FloorNumber,
                    PlannedUnitCount = n.PlannedUnitCount,
                    ActualUnitCount = mine.Count + children.Sum(c => c.ActualUnitCount),
                    Status = n.Status,
                    PlannedCompletionDate = n.PlannedCompletionDate,
                    ProgressPercent = n.ProgressPercent,
                    FloorPlanUrl = n.FloorPlanUrl,
                    SitePlanUrl = n.SitePlanUrl,
                    UnitsAvailable = mine.Count(u => u.Status == PropertyStatus.Available) + children.Sum(c => c.UnitsAvailable),
                    UnitsBooked = mine.Count(u => u.Status == PropertyStatus.Booked) + children.Sum(c => c.UnitsBooked),
                    UnitsSold = mine.Count(u => u.Status is PropertyStatus.Sold or PropertyStatus.Registered or PropertyStatus.Possessed)
                                + children.Sum(c => c.UnitsSold),
                    Children = children,
                };
            })
            .ToList();

        return Build(null);
    }

    private sealed class BoardRow
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public PropertyStatus Status { get; set; }
        public PropertySubType SubType { get; set; }
        public Guid? ProjectNodeId { get; set; }
        public int? FloorNumber { get; set; }
        public string? FloorLabel { get; set; }
        public int? StackIndex { get; set; }
        public decimal AreaSqFt { get; set; }
        public int? Bedrooms { get; set; }
        public Facing? Facing { get; set; }
        public bool IsCorner { get; set; }
        public decimal BasePrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal RatePerSqFt { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public Guid? CurrentHoldId { get; set; }
        public Guid? CurrentBookingId { get; set; }
        public Guid? CurrentBlockId { get; set; }
        public Guid? LandownerAllocationId { get; set; }
        public bool HasLitigation { get; set; }
        public bool IsMortgaged { get; set; }
        public Guid? SitePlanShapeId { get; set; }
    }

    // ═══ Units ═══════════════════════════════════════════════════════════════

    public async Task<InventoryUnitDto?> GetUnitAsync(Guid unitId)
    {
        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == unitId);
        if (unit is null) return null;

        var board = await GetBoardAsync(new InventoryQueryDto { ProjectId = unit.ProjectId, ViewMode = "grid" });
        return board.Units.FirstOrDefault(u => u.Id == unitId);
    }

    public async Task<InventoryUnitDto> SaveUnitAsync(InventoryUnitDto dto, Guid userId)
    {
        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == dto.Id)
            ?? throw new InvalidOperationException("That unit does not exist.");

        if (unit.CurrentBookingId is not null && unit.TotalPrice != dto.TotalPrice)
        {
            throw new InvalidOperationException(
                "This unit is booked. Change the price on the booking instead — editing it here would " +
                "make the signed cost sheet disagree with the record.");
        }

        unit.UnitNumber = dto.UnitNumber;
        unit.BaseRatePerSqFt = dto.RatePerSqFt;
        unit.BasePrice = dto.BasePrice;
        unit.TotalPrice = dto.TotalPrice;
        unit.StackIndex = dto.StackIndex;
        unit.ProjectNodeId = dto.ProjectNodeId;
        unit.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetUnitAsync(unit.Id))!;
    }

    // ═══ Holds ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Holds a unit for a named lead.
    ///
    /// The unit is re-read inside the transaction and the write is guarded by the row's concurrency
    /// stamp, so when two salespeople press Hold in the same second exactly one succeeds and the
    /// other is told who beat them — rather than both being told yes and the argument happening
    /// at booking time.
    /// </summary>
    public async Task<UnitHoldDto> HoldAsync(HoldRequestDto dto, Guid userId)
    {
        var settings = await SettingsAsync();

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == dto.UnitId)
            ?? throw new InvalidOperationException("That unit does not exist.");

        if (unit.Status == PropertyStatus.Blocked)
            throw new InvalidOperationException("This unit is blocked off-market and cannot be held.");

        if (unit.Status is PropertyStatus.Booked or PropertyStatus.Sold or PropertyStatus.Registered or PropertyStatus.Possessed)
            throw new InvalidOperationException("This unit has already been sold.");

        if (unit.CurrentHoldId is not null)
        {
            var existing = await Db.UnitHolds.ForCompany(Tenant)
                .FirstOrDefaultAsync(h => h.Id == unit.CurrentHoldId && h.Status == HoldStatus.Active);

            if (existing is not null && existing.ExpiresAt > DateTime.UtcNow)
            {
                var holderName = existing.PartyId is null
                    ? "another lead"
                    : (await PartyNamesAsync([existing.PartyId.Value])).GetValueOrDefault(existing.PartyId.Value, "another lead");

                throw new InvalidOperationException(
                    $"This unit is already held for {holderName} until {existing.ExpiresAt:HH:mm on d MMM}.");
            }

            // The old hold has lapsed — release it and carry on rather than refusing.
            if (existing is not null)
            {
                existing.Status = HoldStatus.Expired;
                existing.ReleasedAt = DateTime.UtcNow;
            }
        }

        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == unit.ProjectId);
        var defaultHours = project?.HoldHours > 0 ? project.HoldHours : settings.DefaultHoldHours;
        var hours = dto.Hours <= 0 ? defaultHours : dto.Hours;

        var hold = new UnitHold
        {
            UnitId = unit.Id,
            EnquiryId = dto.EnquiryId,
            PartyId = dto.PartyId,
            HeldByUserId = userId,
            HeldAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(hours),
            Status = HoldStatus.Active,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        // Beyond the role's limit this still goes through, but it goes through *visibly*.
        var needsApproval = hours > settings.MaxHoldHoursWithoutApproval;
        if (needsApproval)
        {
            var approval = await RaiseApprovalAsync(
                "UnitHold", unit.Id, unit.UnitNumber, 0m,
                $"Hold {unit.UnitNumber} for {hours} hours", userId, unit.ProjectId);

            hold.ApprovalRequestId = approval?.Id;
        }

        Db.UnitHolds.Add(hold);

        unit.CurrentHoldId = hold.Id;
        unit.Status = PropertyStatus.Held;
        unit.StampUpdated(userId);

        await RecordUnitStatusAsync(unit, PropertyStatus.Available, PropertyStatus.Held, userId, "UnitHold", hold.Id);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await MapHoldAsync(hold, unit, needsApproval);
    }

    public async Task ReleaseHoldAsync(Guid holdId, string? note, Guid userId)
    {
        var hold = await Db.UnitHolds.ForCompany(Tenant).FirstOrDefaultAsync(h => h.Id == holdId)
            ?? throw new InvalidOperationException("That hold does not exist.");

        if (hold.Status != HoldStatus.Active)
            throw new InvalidOperationException("That hold is no longer active.");

        hold.Status = HoldStatus.ReleasedManually;
        hold.ReleasedAt = DateTime.UtcNow;
        hold.ReleasedByUserId = userId;
        hold.Note = note ?? hold.Note;
        hold.StampUpdated(userId);

        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == hold.UnitId);
        if (unit is not null && unit.CurrentHoldId == hold.Id)
        {
            unit.CurrentHoldId = null;
            unit.Status = PropertyStatus.Available;
            unit.StampUpdated(userId);
            await RecordUnitStatusAsync(unit, PropertyStatus.Held, PropertyStatus.Available, userId, "UnitHold", hold.Id);
        }

        await Db.SaveChangesAsync();
    }

    public async Task<List<UnitHoldDto>> GetActiveHoldsAsync(Guid? projectId)
    {
        var holds = await (
            from h in Db.UnitHolds.ForCompany(Tenant)
            join u in Db.Units.ForCompany(Tenant) on h.UnitId equals u.Id
            where h.Status == HoldStatus.Active
            select new { Hold = h, Unit = u })
            .WhereIf(projectId.HasValue, x => x.Unit.ProjectId == projectId)
            .OrderBy(x => x.Hold.ExpiresAt)
            .ToListAsync();

        var names = await PartyNamesAsync(holds.Where(h => h.Hold.PartyId.HasValue).Select(h => h.Hold.PartyId!.Value));
        var projects = await ProjectNamesAsync(holds.Select(h => (Guid?)h.Unit.ProjectId));
        var now = DateTime.UtcNow;

        return holds.Select(x => new UnitHoldDto
        {
            Id = x.Hold.Id,
            UnitId = x.Unit.Id,
            UnitNumber = x.Unit.UnitNumber,
            ProjectName = projects.GetValueOrDefault(x.Unit.ProjectId),
            HeldForName = x.Hold.PartyId is null ? null : names.GetValueOrDefault(x.Hold.PartyId.Value),
            HeldByName = "—",
            HeldAt = x.Hold.HeldAt,
            ExpiresAt = x.Hold.ExpiresAt,
            MinutesRemaining = Math.Max(0, (int)(x.Hold.ExpiresAt - now).TotalMinutes),
            Status = x.Hold.Status,
            Note = x.Hold.Note,
            NeedsApproval = x.Hold.ApprovalRequestId is not null,
        }).ToList();
    }

    /// <summary>
    /// The nightly (and, in practice, every-few-minutes) sweep that gives lapsed holds back.
    /// Without it a board silts up with dead reservations and salespeople stop trusting it.
    /// </summary>
    public async Task<int> ExpireHoldsAsync()
    {
        var now = DateTime.UtcNow;

        var lapsed = await Db.UnitHolds.ForCompany(Tenant)
            .Where(h => h.Status == HoldStatus.Active && h.ExpiresAt <= now)
            .ToListAsync();

        if (lapsed.Count == 0) return 0;

        var unitIds = lapsed.Select(h => h.UnitId).ToList();
        var units = await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id)).ToListAsync();

        foreach (var hold in lapsed)
        {
            hold.Status = HoldStatus.Expired;
            hold.ReleasedAt = now;

            var unit = units.FirstOrDefault(u => u.Id == hold.UnitId);
            if (unit is null || unit.CurrentHoldId != hold.Id) continue;

            unit.CurrentHoldId = null;
            if (unit.Status == PropertyStatus.Held) unit.Status = PropertyStatus.Available;

            await QueueNotificationAsync(
                "hold_expired",
                $"Hold released on {unit.UnitNumber}",
                "The hold lapsed and the unit is back on the board.",
                $"/realestate/inventory?project={unit.ProjectId}",
                recipientUserId: hold.HeldByUserId,
                entityType: "Unit", entityId: unit.Id,
                severity: AlertSeverity.Info);
        }

        await Db.SaveChangesAsync();
        return lapsed.Count;
    }

    private async Task<UnitHoldDto> MapHoldAsync(UnitHold hold, Unit unit, bool needsApproval)
    {
        var names = hold.PartyId is null ? [] : await PartyNamesAsync([hold.PartyId.Value]);
        var projects = await ProjectNamesAsync([unit.ProjectId]);

        return new UnitHoldDto
        {
            Id = hold.Id,
            UnitId = unit.Id,
            UnitNumber = unit.UnitNumber,
            ProjectName = projects.GetValueOrDefault(unit.ProjectId),
            HeldForName = hold.PartyId is null ? null : names.GetValueOrDefault(hold.PartyId.Value),
            HeldByName = "—",
            HeldAt = hold.HeldAt,
            ExpiresAt = hold.ExpiresAt,
            MinutesRemaining = Math.Max(0, (int)(hold.ExpiresAt - DateTime.UtcNow).TotalMinutes),
            Status = hold.Status,
            Note = hold.Note,
            NeedsApproval = needsApproval,
        };
    }

    // ═══ Blocking ════════════════════════════════════════════════════════════

    public async Task<InventoryUnitDto> BlockAsync(BlockRequestDto dto, Guid userId)
    {
        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == dto.UnitId)
            ?? throw new InvalidOperationException("That unit does not exist.");

        if (unit.CurrentBookingId is not null)
            throw new InvalidOperationException("This unit is booked. Cancel the booking before blocking it.");

        var previous = unit.Status;

        var block = new UnitBlockRecord
        {
            UnitId = unit.Id,
            Reason = dto.Reason,
            Note = dto.Note,
            BlockedByUserId = userId,
            BlockedAt = DateTime.UtcNow,
            ExpectedReleaseDate = dto.ExpectedReleaseDate,
            IsActive = true,
        }.StampNew(Tenant, userId);

        Db.UnitBlockRecords.Add(block);

        unit.CurrentBlockId = block.Id;
        unit.Status = PropertyStatus.Blocked;
        unit.IsSaleable = false;
        unit.CurrentHoldId = null;
        unit.StampUpdated(userId);

        await RecordUnitStatusAsync(unit, previous, PropertyStatus.Blocked, userId, "UnitBlock", block.Id, dto.Note);
        await Db.SaveChangesAsync();

        return (await GetUnitAsync(unit.Id))!;
    }

    public async Task<InventoryUnitDto> UnblockAsync(Guid unitId, string? note, Guid userId)
    {
        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == unitId)
            ?? throw new InvalidOperationException("That unit does not exist.");

        if (unit.CurrentBlockId is null)
            throw new InvalidOperationException("This unit is not blocked.");

        var block = await Db.UnitBlockRecords.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == unit.CurrentBlockId);
        if (block is not null)
        {
            block.IsActive = false;
            block.ReleasedAt = DateTime.UtcNow;
            block.ReleasedByUserId = userId;
            block.StampUpdated(userId);
        }

        unit.CurrentBlockId = null;
        unit.Status = PropertyStatus.Available;
        unit.IsSaleable = true;
        unit.StampUpdated(userId);

        await RecordUnitStatusAsync(unit, PropertyStatus.Blocked, PropertyStatus.Available, userId, "UnitBlock", block?.Id, note);
        await Db.SaveChangesAsync();

        return (await GetUnitAsync(unit.Id))!;
    }

    private async Task RecordUnitStatusAsync(
        Unit unit, PropertyStatus from, PropertyStatus to, Guid userId,
        string? sourceType = null, Guid? sourceId = null, string? note = null)
    {
        Db.UnitStatusHistories.Add(new UnitStatusHistory
        {
            UnitId = unit.Id,
            FromStatus = from,
            ToStatus = to,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            SourceDocumentType = sourceType,
            SourceDocumentId = sourceId,
            Note = note,
        }.StampNew(Tenant, userId));

        // Keep the physical record in step, since every other screen reads status from there.
        var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == unit.PropertyId);
        if (property is not null && property.Status != to)
        {
            Db.PropertyStatusHistories.Add(new PropertyStatusHistory
            {
                PropertyId = property.Id,
                FromStatus = property.Status,
                ToStatus = to,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                SourceDocumentType = sourceType,
                SourceDocumentId = sourceId,
                Note = note,
            }.StampNew(Tenant, userId));

            property.Status = to;
            property.StampUpdated(userId);
        }
    }

    // ═══ Pricing ═════════════════════════════════════════════════════════════

    public async Task<List<PriceListDto>> GetPriceListsAsync(Guid projectId)
    {
        var lists = await Db.PriceLists.ForCompany(Tenant)
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.EffectiveFrom).ThenByDescending(p => p.Version)
            .ToListAsync();

        var ids = lists.Select(l => l.Id).ToList();

        var lineCounts = await Db.PriceListLines.ForCompany(Tenant)
            .Where(l => ids.Contains(l.PriceListId))
            .GroupBy(l => l.PriceListId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var unitCounts = await Db.Units.ForCompany(Tenant)
            .Where(u => u.PriceListId != null && ids.Contains(u.PriceListId!.Value))
            .GroupBy(u => u.PriceListId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        return lists.Select(l => new PriceListDto
        {
            Id = l.Id,
            ProjectId = l.ProjectId,
            Name = l.Name,
            Version = l.Version,
            EffectiveFrom = l.EffectiveFrom,
            EffectiveTo = l.EffectiveTo,
            IsPublished = l.IsPublished,
            PublishedAt = l.PublishedAt,
            CurrencyCode = l.CurrencyCode ?? "USD",
            Note = l.Note,
            LineCount = lineCounts.GetValueOrDefault(l.Id),
            UnitsAffected = unitCounts.GetValueOrDefault(l.Id),
        }).ToList();
    }

    public async Task<PriceListDto?> GetPriceListAsync(Guid id)
    {
        var list = await Db.PriceLists.ForCompany(Tenant)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (list is null) return null;

        var summary = (await GetPriceListsAsync(list.ProjectId)).First(l => l.Id == id);

        var unitIds = list.Lines.Where(l => l.UnitId.HasValue).Select(l => l.UnitId!.Value).ToList();
        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var nodeIds = list.Lines.Where(l => l.ProjectNodeId.HasValue).Select(l => l.ProjectNodeId!.Value).ToList();
        var nodes = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant).Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        summary.Lines = list.Lines.OrderByDescending(l => l.Specificity).Select(l => new PriceListLineDto
        {
            Id = l.Id,
            UnitId = l.UnitId,
            UnitNumber = l.UnitId is null ? null : units.GetValueOrDefault(l.UnitId.Value),
            ProjectNodeId = l.ProjectNodeId,
            BlockName = l.ProjectNodeId is null ? null : nodes.GetValueOrDefault(l.ProjectNodeId.Value),
            SubType = l.SubType,
            MinAreaSqFt = l.MinAreaSqFt,
            MaxAreaSqFt = l.MaxAreaSqFt,
            MinFloor = l.MinFloor,
            MaxFloor = l.MaxFloor,
            RatePerSqFt = l.RatePerSqFt,
            FlatPrice = l.FlatPrice,
            Specificity = l.Specificity,
        }).ToList();

        return summary;
    }

    public async Task<PriceListDto> SavePriceListAsync(PriceListDto dto, Guid userId)
    {
        var list = dto.Id != Guid.Empty
            ? await Db.PriceLists.ForCompany(Tenant).Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (list?.IsPublished == true)
            throw new InvalidOperationException(
                "A published price list cannot be edited. Create a new version instead, so the price a " +
                "customer was quoted stays provable.");

        if (list is null)
        {
            var latest = await Db.PriceLists.ForCompany(Tenant)
                .Where(p => p.ProjectId == dto.ProjectId)
                .MaxAsync(p => (int?)p.Version) ?? 0;

            list = new PriceList { ProjectId = dto.ProjectId, Version = latest + 1 }.StampNew(Tenant, userId);
            Db.PriceLists.Add(list);
        }
        else
        {
            Db.PriceListLines.RemoveRange(list.Lines);
            list.StampUpdated(userId);
        }

        list.Name = dto.Name;
        list.EffectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;
        list.EffectiveTo = dto.EffectiveTo;
        list.CurrencyCode = dto.CurrencyCode;
        list.Note = dto.Note;

        foreach (var line in dto.Lines)
        {
            list.Lines.Add(new PriceListLine
            {
                UnitId = line.UnitId,
                ProjectNodeId = line.ProjectNodeId,
                SubType = line.SubType,
                MinAreaSqFt = line.MinAreaSqFt,
                MaxAreaSqFt = line.MaxAreaSqFt,
                MinFloor = line.MinFloor,
                MaxFloor = line.MaxFloor,
                RatePerSqFt = line.RatePerSqFt,
                FlatPrice = line.FlatPrice,
                Specificity = Specificity(line),
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();
        return (await GetPriceListAsync(list.Id))!;
    }

    /// <summary>
    /// How specific a price line is. The most specific match wins, so a line naming one unit must
    /// always beat a line covering a size band, which must beat a blanket project rate.
    /// </summary>
    private static int Specificity(PriceListLineDto line)
    {
        var score = 0;
        if (line.UnitId.HasValue) score += 1000;
        if (line.ProjectNodeId.HasValue) score += 100;
        if (line.SubType.HasValue) score += 50;
        if (line.MinFloor.HasValue || line.MaxFloor.HasValue) score += 20;
        if (line.MinAreaSqFt.HasValue || line.MaxAreaSqFt.HasValue) score += 10;
        return score;
    }

    public async Task<PriceListDto> PublishPriceListAsync(Guid id, Guid userId)
    {
        var list = await Db.PriceLists.ForCompany(Tenant).Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("That price list does not exist.");

        if (list.IsPublished) throw new InvalidOperationException("This price list is already published.");
        if (list.Lines.Count == 0) throw new InvalidOperationException("A price list needs at least one rate before it can be published.");

        list.IsPublished = true;
        list.PublishedAt = DateTime.UtcNow;
        list.PublishedByUserId = userId;
        list.StampUpdated(userId);

        // Close the previous list so two lists are never simultaneously in force.
        var previous = await Db.PriceLists.ForCompany(Tenant)
            .Where(p => p.ProjectId == list.ProjectId && p.Id != id && p.IsPublished && p.EffectiveTo == null)
            .ToListAsync();

        foreach (var p in previous)
        {
            p.EffectiveTo = list.EffectiveFrom.AddDays(-1);
            p.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        await RepriceAvailableUnitsAsync(id, userId);

        return (await GetPriceListAsync(id))!;
    }

    /// <summary>
    /// Applies a published list to stock that is still ours to price.
    ///
    /// Booked and sold units are deliberately skipped: re-pricing them would rewrite a signed cost
    /// sheet, which is the one thing this module must never do.
    /// </summary>
    public async Task<int> RepriceAvailableUnitsAsync(Guid priceListId, Guid userId)
    {
        var list = await Db.PriceLists.ForCompany(Tenant).Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == priceListId)
            ?? throw new InvalidOperationException("That price list does not exist.");

        var units = await (
            from u in Db.Units.ForCompany(Tenant)
            join p in Db.Properties.ForCompany(Tenant) on u.PropertyId equals p.Id
            where u.ProjectId == list.ProjectId
                && (u.Status == PropertyStatus.Available || u.Status == PropertyStatus.Held)
            select new { Unit = u, Property = p })
            .ToListAsync();

        var premiums = await Db.PremiumCharges.ForCompany(Tenant)
            .Where(c => c.ProjectId == list.ProjectId && c.IsAutoApplied)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var existingPremiums = await Db.UnitPremiums.ForCompany(Tenant)
            .Where(p => units.Select(u => u.Unit.Id).Contains(p.UnitId))
            .ToListAsync();

        var changed = 0;

        foreach (var row in units)
        {
            var match = BestLine(list.Lines, row.Unit, row.Property);
            if (match is null) continue;

            var area = row.Property.SaleableAreaSqFt ?? 0m;
            var basePrice = match.FlatPrice ?? RealEstateMapper.Money(match.RatePerSqFt * area);

            row.Unit.PriceListId = list.Id;
            row.Unit.BaseRatePerSqFt = match.RatePerSqFt;
            row.Unit.BasePrice = basePrice;
            row.Unit.CurrencyCode = list.CurrencyCode;
            row.Unit.StampUpdated(userId);

            // Re-stamp the premium ladder, because a floor-rise premium is a function of the base.
            Db.UnitPremiums.RemoveRange(existingPremiums.Where(p => p.UnitId == row.Unit.Id));

            var total = basePrice;
            foreach (var charge in premiums.Where(c => AppliesTo(c, row.Property)))
            {
                var amount = ComputeCharge(charge, basePrice, area, row.Property.FloorNumber, total);
                if (amount == 0m) continue;

                Db.UnitPremiums.Add(new UnitPremium
                {
                    UnitId = row.Unit.Id,
                    Kind = charge.Kind,
                    Label = charge.Label,
                    Basis = charge.Basis,
                    Rate = charge.Rate,
                    Amount = amount,
                    IsTaxable = charge.IsTaxable,
                    TaxPercent = charge.TaxPercent,
                    IsPartOfSalePrice = charge.IsPartOfSalePrice,
                    IsOptional = charge.IsOptional,
                    SortOrder = charge.SortOrder,
                }.StampNew(Tenant, userId));

                if (charge.IsPartOfSalePrice) total += amount;
            }

            row.Unit.TotalPrice = RealEstateMapper.Money(total);
            changed++;
        }

        await Db.SaveChangesAsync();
        return changed;
    }

    private static PriceListLine? BestLine(ICollection<PriceListLine> lines, Unit unit, Property property)
    {
        var area = property.SaleableAreaSqFt ?? 0m;
        var floor = property.FloorNumber;

        return lines
            .Where(l => l.UnitId is null || l.UnitId == unit.Id)
            .Where(l => l.ProjectNodeId is null || l.ProjectNodeId == unit.ProjectNodeId)
            .Where(l => l.SubType is null || l.SubType == property.SubType)
            .Where(l => l.MinAreaSqFt is null || area >= l.MinAreaSqFt)
            .Where(l => l.MaxAreaSqFt is null || area <= l.MaxAreaSqFt)
            .Where(l => l.MinFloor is null || (floor.HasValue && floor >= l.MinFloor))
            .Where(l => l.MaxFloor is null || (floor.HasValue && floor <= l.MaxFloor))
            .OrderByDescending(l => l.Specificity)
            .FirstOrDefault();
    }

    private static bool AppliesTo(PremiumCharge charge, Property property)
    {
        if (charge.AppliesToCornerOnly && !property.IsCorner) return false;
        if (charge.AppliesToSubType is not null && charge.AppliesToSubType != property.SubType) return false;

        if (charge.AppliesToParkFacingOnly)
        {
            var view = property.ViewDescription ?? string.Empty;
            if (!view.Contains("park", StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }

    private static decimal ComputeCharge(
        PremiumCharge charge, decimal basePrice, decimal areaSqFt, int? floorNumber, decimal runningTotal)
        => charge.Basis switch
        {
            ChargeBasis.Fixed => charge.Rate,
            ChargeBasis.PerAreaUnit => RealEstateMapper.Money(charge.Rate * areaSqFt),
            ChargeBasis.PercentOfBase => RealEstateMapper.Money(basePrice * charge.Rate / 100m),
            ChargeBasis.PercentOfTotal => RealEstateMapper.Money(runningTotal * charge.Rate / 100m),
            ChargeBasis.PerFloor => floorNumber is null || charge.AppliesFromFloor is null
                ? 0m
                : RealEstateMapper.Money(Math.Max(0, floorNumber.Value - charge.AppliesFromFloor.Value) * charge.Rate * areaSqFt),
            _ => 0m,
        };

    public async Task<List<PremiumChargeDto>> GetPremiumsAsync(Guid projectId)
        => await Db.PremiumCharges.ForCompany(Tenant)
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new PremiumChargeDto
            {
                Id = c.Id,
                ProjectId = c.ProjectId,
                Kind = c.Kind,
                Label = c.Label,
                Basis = c.Basis,
                Rate = c.Rate,
                AppliesFromFloor = c.AppliesFromFloor,
                AppliesToCornerOnly = c.AppliesToCornerOnly,
                AppliesToParkFacingOnly = c.AppliesToParkFacingOnly,
                AppliesToSubType = c.AppliesToSubType,
                IsTaxable = c.IsTaxable,
                TaxPercent = c.TaxPercent,
                IsPartOfSalePrice = c.IsPartOfSalePrice,
                IsOptional = c.IsOptional,
                IsAutoApplied = c.IsAutoApplied,
                SortOrder = c.SortOrder,
            })
            .ToListAsync();

    public async Task<PremiumChargeDto> SavePremiumAsync(PremiumChargeDto dto, Guid userId)
    {
        var charge = dto.Id.HasValue && dto.Id != Guid.Empty
            ? await Db.PremiumCharges.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (charge is null)
        {
            charge = new PremiumCharge { ProjectId = dto.ProjectId }.StampNew(Tenant, userId);
            Db.PremiumCharges.Add(charge);
        }
        else charge.StampUpdated(userId);

        charge.Kind = dto.Kind;
        charge.Label = dto.Label;
        charge.Basis = dto.Basis;
        charge.Rate = dto.Rate;
        charge.AppliesFromFloor = dto.AppliesFromFloor;
        charge.AppliesToCornerOnly = dto.AppliesToCornerOnly;
        charge.AppliesToParkFacingOnly = dto.AppliesToParkFacingOnly;
        charge.AppliesToSubType = dto.AppliesToSubType;
        charge.IsTaxable = dto.IsTaxable;
        charge.TaxPercent = dto.TaxPercent;
        charge.IsPartOfSalePrice = dto.IsPartOfSalePrice;
        charge.IsOptional = dto.IsOptional;
        charge.IsAutoApplied = dto.IsAutoApplied;
        charge.SortOrder = dto.SortOrder;

        await Db.SaveChangesAsync();
        dto.Id = charge.Id;
        return dto;
    }

    // ═══ Cost sheet ══════════════════════════════════════════════════════════

    /// <summary>
    /// The printable quotation a buyer signs. Built from the unit's stored premium ladder rather
    /// than recomputed, so what is printed is exactly what the record says.
    /// </summary>
    public async Task<CostSheetDto> GetCostSheetAsync(
        Guid unitId, Guid? paymentPlanTemplateId, decimal discountAmount, decimal discountPercent)
    {
        var row = await (
            from u in Db.Units.ForCompany(Tenant)
            join p in Db.Properties.ForCompany(Tenant) on u.PropertyId equals p.Id
            where u.Id == unitId
            select new { Unit = u, Property = p })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("That unit does not exist.");

        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == row.Unit.ProjectId);
        var areaUnit = await AreaUnitAsync(project?.OfficeId);
        var currency = row.Unit.CurrencyCode ?? project?.CurrencyCode ?? await CurrencyAsync();

        var premiums = await Db.UnitPremiums.ForCompany(Tenant)
            .Where(p => p.UnitId == unitId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var node = row.Unit.ProjectNodeId is null
            ? null
            : await Db.ProjectNodes.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == row.Unit.ProjectNodeId);

        var area = row.Property.SaleableAreaSqFt ?? 0m;

        var salePriceLines = premiums.Where(p => p.IsPartOfSalePrice).Select(MapCostLine).ToList();
        var otherLines = premiums.Where(p => !p.IsPartOfSalePrice).Select(MapCostLine).ToList();

        var premiumTotal = salePriceLines.Sum(l => l.Amount);
        var discount = discountAmount > 0
            ? discountAmount
            : RealEstateMapper.Money((row.Unit.BasePrice + premiumTotal) * discountPercent / 100m);

        var netSalePrice = RealEstateMapper.Money(row.Unit.BasePrice + premiumTotal - discount);
        var otherTotal = otherLines.Sum(l => l.Amount);
        var taxTotal = salePriceLines.Sum(l => l.TaxAmount) + otherLines.Sum(l => l.TaxAmount);
        var grandTotal = RealEstateMapper.Money(netSalePrice + otherTotal + taxTotal);

        var sheet = new CostSheetDto
        {
            UnitId = unitId,
            UnitNumber = row.Unit.UnitNumber,
            ProjectName = project?.Name ?? "—",
            BlockName = node?.Name,
            FloorNumber = row.Property.FloorNumber,
            SubType = row.Property.SubType,
            SaleableArea = RealEstateMapper.Area(area, areaUnit),
            CarpetArea = RealEstateMapper.AreaOrNull(row.Property.CarpetAreaSqFt, areaUnit),
            RatePerSqFt = row.Unit.BaseRatePerSqFt,
            CurrencyCode = currency,
            SalePriceLines = salePriceLines,
            OtherChargeLines = otherLines,
            BasePrice = row.Unit.BasePrice,
            PremiumTotal = premiumTotal,
            DiscountAmount = discount,
            NetSalePrice = netSalePrice,
            OtherChargesTotal = otherTotal,
            TaxTotal = taxTotal,
            GrandTotal = grandTotal,
            AmountInWords = RealEstateMapper.AmountInWords(grandTotal, currency, RealEstateMapper.UsesIndianScale(currency)),
            GeneratedOn = Today,
            ValidUntil = Today.AddDays(15),
        };

        // The base price is the first line of the sheet, above the premiums.
        sheet.SalePriceLines.Insert(0, new CostSheetLineDto
        {
            Kind = ChargeKind.BasePrice,
            Label = $"Base price @ {row.Unit.BaseRatePerSqFt:N0} per sq ft",
            Basis = ChargeBasis.PerAreaUnit,
            Rate = row.Unit.BaseRatePerSqFt,
            Amount = row.Unit.BasePrice,
            SortOrder = -1,
        });

        return sheet;
    }

    private static CostSheetLineDto MapCostLine(UnitPremium p) => new()
    {
        Kind = p.Kind,
        Label = p.Label,
        Basis = p.Basis,
        Rate = p.Rate,
        Amount = p.Amount,
        TaxPercent = p.TaxPercent,
        TaxAmount = p.IsTaxable ? RealEstateMapper.Money(p.Amount * p.TaxPercent / 100m) : 0m,
        IsOptional = p.IsOptional,
        SortOrder = p.SortOrder,
    };
}
