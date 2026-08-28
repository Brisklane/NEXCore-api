using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The gate, amenity bookings and move requests.
///
/// The gate is the only screen in this whole application used by somebody standing outdoors on a
/// cheap tablet with intermittent signal, at speed, with a queue behind them. Everything here
/// follows from that: entries can be captured offline and replayed idempotently, a guard can
/// always override with a reason rather than being stuck, and "who is inside right now" is a
/// single indexed read because a fire marshal needs it in seconds.
/// </summary>
public partial class SocietyService
{
    // ═══ Visitor passes ══════════════════════════════════════════════════════

    /// <summary>
    /// A pass raised by a resident before their visitor arrives. Pre-approval is what removes the
    /// call to the flat at the barrier, which is the single biggest cause of a queue at the gate.
    /// </summary>
    public async Task<VisitorPassDto> CreateVisitorPassAsync(VisitorPassCreateDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");

        if (dto.ValidTo <= dto.ValidFrom)
            throw new InvalidOperationException("The pass has to be valid for a period of time.");

        var pass = new VisitorPass
        {
            PassNumber = await numbering.NextVisitorPassNumberAsync(DateTime.UtcNow),
            SocietyId = dto.SocietyId,
            UnitId = dto.UnitId,
            ResidentId = dto.ResidentId,
            VisitorName = dto.VisitorName,
            VisitorPhone = dto.VisitorPhone,
            Kind = dto.Kind,
            GuestCount = Math.Max(1, dto.GuestCount),
            VehicleNumber = dto.VehicleNumber?.Trim().ToUpperInvariant(),
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsRecurring = dto.IsRecurring,
            RecurrenceDays = dto.RecurrenceDays,
            Status = GateEntryStatus.Expected,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        // The code the guard types or scans. Short enough to read aloud over a barrier, and
        // meaningless outside the validity window.
        pass.OtpCode = Random.Shared.Next(100000, 999999).ToString();
        pass.QrCode = $"{pass.PassNumber}:{pass.OtpCode}";

        Db.VisitorPasses.Add(pass);
        await Db.SaveChangesAsync();

        return (await MapPassesAsync([pass]))[0];
    }

    public async Task<PaginatedResponse<VisitorPassDto>> GetVisitorPassesAsync(ListQueryDto query, Guid societyId, Guid? unitId)
    {
        var q = Db.VisitorPasses.ForCompany(Tenant)
            .Where(p => p.SocietyId == societyId)
            .WhereIf(unitId.HasValue, p => p.UnitId == unitId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                p => p.PassNumber.Contains(query.Search!) || p.VisitorName.Contains(query.Search!))
            .OrderByDescending(p => p.ValidFrom);

        return await PageAsync(q, query, MapPassesAsync);
    }

    private async Task<List<VisitorPassDto>> MapPassesAsync(List<VisitorPass> passes)
    {
        if (passes.Count == 0) return [];

        var now = DateTime.UtcNow;
        var unitIds = passes.Where(p => p.UnitId.HasValue).Select(p => p.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var residentIds = passes.Where(p => p.ResidentId.HasValue).Select(p => p.ResidentId!.Value).Distinct().ToList();

        var residents = residentIds.Count == 0
            ? []
            : await Db.Residents.ForCompany(Tenant)
                .Where(r => residentIds.Contains(r.Id))
                .Join(Db.Parties.ForCompany(Tenant), r => r.PartyId, p => p.Id, (r, p) => new { r.Id, p.DisplayName })
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName);

        return passes.Select(p => new VisitorPassDto
        {
            Id = p.Id,
            PassNumber = p.PassNumber,
            SocietyId = p.SocietyId,
            UnitId = p.UnitId,
            UnitLabel = p.UnitId is null ? null : units.GetValueOrDefault(p.UnitId.Value),
            ResidentName = p.ResidentId is null ? null : residents.GetValueOrDefault(p.ResidentId.Value),
            VisitorName = p.VisitorName,
            VisitorPhone = p.VisitorPhone,
            Kind = p.Kind,
            GuestCount = p.GuestCount,
            VehicleNumber = p.VehicleNumber,
            ValidFrom = p.ValidFrom,
            ValidTo = p.ValidTo,
            IsRecurring = p.IsRecurring,
            RecurrenceDays = p.RecurrenceDays,
            QrCode = p.QrCode,
            Status = p.Status,
            UsedAt = p.UsedAt,
            IsCancelled = p.IsCancelled,
            IsExpired = p.ValidTo < now,
            Note = p.Note,
        }).ToList();
    }

    // ═══ Gate entries ════════════════════════════════════════════════════════

    /// <summary>
    /// Records somebody arriving. A pre-approved pass admits them immediately; anyone else waits
    /// on the resident. A guard can always let somebody in with a reason — refusing a delivery
    /// because a resident is not answering their phone is not a workable rule, but it is a fact
    /// that has to be on the record.
    /// </summary>
    public async Task<GateEntryDto> RecordEntryAsync(GateEntryCreateDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");

        var now = DateTime.UtcNow;
        var capturedAt = dto.WasOffline ? dto.OfflineCapturedAt ?? now : now;

        // Replay protection for offline batches: the same client reference never lands twice.
        if (!string.IsNullOrWhiteSpace(dto.ClientReference))
        {
            var existing = await Db.GateEntries.ForCompany(Tenant)
                .FirstOrDefaultAsync(e => e.SocietyId == dto.SocietyId && e.Code == dto.ClientReference);

            if (existing is not null) return (await MapGateEntriesAsync([existing]))[0];
        }

        var entry = new GateEntry
        {
            SocietyId = dto.SocietyId,
            GateId = dto.GateId,
            UnitId = dto.UnitId,
            VisitorPassId = dto.VisitorPassId,
            VisitorId = dto.VisitorId,
            DomesticStaffId = dto.DomesticStaffId,
            ResidentId = dto.ResidentId,
            PersonName = dto.PersonName,
            Kind = dto.Kind,
            Purpose = dto.Purpose,
            VehicleNumber = dto.VehicleNumber?.Trim().ToUpperInvariant(),
            PersonCount = Math.Max(1, dto.PersonCount),
            EntryPhotoUrl = dto.EntryPhotoUrl,
            GuardUserId = userId,
            CheckedInAt = capturedAt,
            WasOffline = dto.WasOffline,
            OfflineSyncedAt = dto.WasOffline ? now : null,
            Code = dto.ClientReference,
        }.StampNew(Tenant, userId);

        // Blacklists are checked at the barrier, which is the only place checking them helps.
        if (dto.DomesticStaffId is not null)
        {
            var staff = await Db.DomesticStaffs.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == dto.DomesticStaffId);

            if (staff is not null)
            {
                if (staff.IsBlacklisted && !dto.GuardOverride)
                    throw new InvalidOperationException($"{staff.Name} is blacklisted: {staff.BlacklistReason}. A supervisor override is required.");

                if (staff.PassExpiresOn is not null && staff.PassExpiresOn < DateOnly.FromDateTime(now) && !dto.GuardOverride)
                    throw new InvalidOperationException($"{staff.Name}'s pass expired on {staff.PassExpiresOn:dd MMM yyyy}.");

                entry.PersonName ??= staff.Name;
                entry.UnitId ??= staff.UnitId;
                entry.Kind = VisitorKind.DomesticStaff;
            }
        }

        if (dto.VisitorId is not null)
        {
            var visitor = await Db.Visitors.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.VisitorId);

            if (visitor is not null)
            {
                if (visitor.IsBlacklisted && !dto.GuardOverride)
                    throw new InvalidOperationException($"{visitor.Name} is blacklisted: {visitor.BlacklistReason}.");

                visitor.VisitCount++;
                visitor.LastVisitAt = now;
                visitor.IsFrequent = visitor.VisitCount >= 5;
                visitor.StampUpdated(userId);

                entry.PersonName ??= visitor.Name;
            }
        }

        // A valid pre-approved pass is the fast path — no call, no wait.
        if (dto.VisitorPassId is not null)
        {
            var pass = await Db.VisitorPasses.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.VisitorPassId);

            if (pass is null || pass.IsCancelled)
                throw new InvalidOperationException("That pass does not exist or has been cancelled.");

            if (now < pass.ValidFrom || now > pass.ValidTo)
            {
                if (!dto.GuardOverride)
                    throw new InvalidOperationException($"Pass {pass.PassNumber} is valid from {pass.ValidFrom:dd MMM HH:mm} to {pass.ValidTo:dd MMM HH:mm}.");
            }
            else
            {
                entry.Status = GateEntryStatus.CheckedIn;
                entry.ApprovedAt = now;
                entry.ApprovalMethod = "PreApprovedPass";
                entry.ApprovedByResidentId = pass.ResidentId;

                if (!pass.IsRecurring)
                {
                    pass.Status = GateEntryStatus.CheckedIn;
                    pass.UsedAt = now;
                    pass.StampUpdated(userId);
                }
            }

            entry.PersonName ??= pass.VisitorName;
            entry.UnitId ??= pass.UnitId;
        }

        if (entry.Status != GateEntryStatus.CheckedIn)
        {
            if (dto.GuardOverride)
            {
                if (string.IsNullOrWhiteSpace(dto.OverrideReason))
                    throw new InvalidOperationException("A guard override has to say why.");

                entry.Status = GateEntryStatus.CheckedIn;
                entry.ApprovalMethod = "GuardOverride";
                entry.Note = dto.OverrideReason;
                entry.ApprovedAt = now;
            }
            else if (dto.RequestApproval && entry.UnitId is not null)
            {
                entry.Status = GateEntryStatus.AwaitingApproval;
                entry.ApprovalRequested = true;
                entry.ApprovalRequestedAt = now;

                // The clock is running with somebody standing at a barrier, so the resident gets
                // this immediately rather than in a batched digest.
                var residents = await Db.Residents.ForCompany(Tenant)
                    .Where(r => r.UnitId == entry.UnitId && r.MovedOutOn == null && r.CanApproveVisitors)
                    .Select(r => r.PartyId)
                    .ToListAsync();

                foreach (var party in residents)
                {
                    await QueueNotificationAsync(
                        "GateApprovalRequest",
                        $"{entry.PersonName ?? "A visitor"} is at the gate",
                        $"{entry.Kind}{(string.IsNullOrWhiteSpace(entry.Purpose) ? "" : $" — {entry.Purpose}")}. Approve or deny.",
                        $"/realestate/gate/{entry.Id}",
                        recipientPartyId: party,
                        entityType: "GateEntry",
                        entityId: entry.Id,
                        severity: AlertSeverity.Warning);
                }
            }
            else
            {
                entry.Status = GateEntryStatus.CheckedIn;
                entry.ApprovedAt = now;
                entry.ApprovalMethod = "NoApprovalRequired";
            }
        }

        Db.GateEntries.Add(entry);
        await Db.SaveChangesAsync();

        return (await MapGateEntriesAsync([entry]))[0];
    }

    public async Task<GateEntryDto> ApproveEntryAsync(GateApprovalDto dto, Guid userId)
    {
        var entry = await RequireAsync<GateEntry>(dto.GateEntryId, "That gate entry does not exist.");

        if (entry.CheckedOutAt is not null)
            throw new InvalidOperationException("This visitor has already left.");

        var now = DateTime.UtcNow;

        if (dto.Approved)
        {
            entry.Status = GateEntryStatus.CheckedIn;
            entry.ApprovedAt = now;
            entry.ApprovalMethod = dto.Method ?? "ResidentApproval";
            entry.CheckedInAt ??= now;
            entry.IsDenied = false;
        }
        else
        {
            entry.Status = GateEntryStatus.Denied;
            entry.IsDenied = true;
            entry.DenialReason = dto.DenialReason;

            // A denied entry never records a check-in. Otherwise "inside now" includes people who
            // were turned away, and that is the one list that has to be right.
            entry.CheckedInAt = null;
        }

        entry.StampUpdated(userId);
        await Db.SaveChangesAsync();

        return (await MapGateEntriesAsync([entry]))[0];
    }

    public async Task<GateEntryDto> CheckOutAsync(Guid gateEntryId, Guid userId)
    {
        var entry = await RequireAsync<GateEntry>(gateEntryId, "That gate entry does not exist.");

        if (entry.CheckedOutAt is not null)
            throw new InvalidOperationException($"This visitor left at {entry.CheckedOutAt:HH:mm}.");

        if (entry.CheckedInAt is null)
            throw new InvalidOperationException("This visitor never checked in.");

        entry.CheckedOutAt = DateTime.UtcNow;
        entry.Status = GateEntryStatus.CheckedOut;
        entry.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await MapGateEntriesAsync([entry]))[0];
    }

    public async Task<PaginatedResponse<GateEntryDto>> GetGateLogAsync(
        ListQueryDto query, Guid societyId, GateEntryStatus? status)
    {
        var q = Db.GateEntries.ForCompany(Tenant)
            .Where(e => e.SocietyId == societyId)
            .WhereIf(status.HasValue, e => e.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                e => (e.PersonName != null && e.PersonName.Contains(query.Search!))
                  || (e.VehicleNumber != null && e.VehicleNumber.Contains(query.Search!)))
            .WhereIf(query.FromDate.HasValue, e => e.CheckedInAt >= query.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(query.ToDate.HasValue, e => e.CheckedInAt <= query.ToDate!.Value.ToDateTime(TimeOnly.MaxValue))
            .OrderByDescending(e => e.CheckedInAt ?? e.CreatedAt);

        return await PageAsync(q, query, MapGateEntriesAsync);
    }

    /// <summary>
    /// Everyone currently inside the scheme. This is the fire-roll: it has to be correct and it
    /// has to be instant, so it reads one index and nothing else.
    /// </summary>
    public async Task<List<GateEntryDto>> GetInsideNowAsync(Guid societyId)
    {
        var entries = await Db.GateEntries.ForCompany(Tenant)
            .Where(e => e.SocietyId == societyId && e.CheckedInAt != null && e.CheckedOutAt == null && !e.IsDenied)
            .OrderBy(e => e.CheckedInAt)
            .ToListAsync();

        return await MapGateEntriesAsync(entries);
    }

    private async Task<List<GateEntryDto>> MapGateEntriesAsync(List<GateEntry> entries)
    {
        if (entries.Count == 0) return [];

        var now = DateTime.UtcNow;

        var unitIds = entries.Where(e => e.UnitId.HasValue).Select(e => e.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant)
                .Where(u => unitIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UnitNumber, u.ProjectNodeId })
                .ToListAsync();

        var nodeIds = units.Where(u => u.ProjectNodeId.HasValue).Select(u => u.ProjectNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var residentIds = entries.Where(e => e.ResidentId.HasValue).Select(e => e.ResidentId!.Value)
            .Concat(entries.Where(e => e.ApprovedByResidentId.HasValue).Select(e => e.ApprovedByResidentId!.Value))
            .Distinct().ToList();

        var residents = residentIds.Count == 0
            ? []
            : await Db.Residents.ForCompany(Tenant)
                .Where(r => residentIds.Contains(r.Id))
                .Join(Db.Parties.ForCompany(Tenant), r => r.PartyId, p => p.Id,
                    (r, p) => new { r.Id, p.DisplayName, p.PrimaryPhone })
                .ToListAsync();

        var guards = await AgentUserNamesAsync(entries.Select(e => e.GuardUserId));

        return entries.Select(e =>
        {
            var unit = e.UnitId is null ? null : units.FirstOrDefault(u => u.Id == e.UnitId);
            var resident = residents.FirstOrDefault(r => r.Id == (e.ResidentId ?? e.ApprovedByResidentId));

            return new GateEntryDto
            {
                Id = e.Id,
                SocietyId = e.SocietyId,
                UnitId = e.UnitId,
                UnitLabel = unit?.UnitNumber,
                BlockName = unit?.ProjectNodeId is null ? null : nodes.GetValueOrDefault(unit.ProjectNodeId.Value),
                ResidentName = resident?.DisplayName,
                ResidentPhone = resident?.PrimaryPhone,
                PersonName = e.PersonName,
                Kind = e.Kind,
                Purpose = e.Purpose,
                VehicleNumber = e.VehicleNumber,
                PersonCount = e.PersonCount,
                EntryPhotoUrl = e.EntryPhotoUrl,
                Status = e.Status,
                CheckedInAt = e.CheckedInAt,
                CheckedOutAt = e.CheckedOutAt,

                // How long they have been inside, live while they are still in.
                MinutesInside = e.CheckedInAt is null
                    ? null
                    : (int)((e.CheckedOutAt ?? now) - e.CheckedInAt.Value).TotalMinutes,

                GuardName = e.GuardUserId is null ? null : guards.GetValueOrDefault(e.GuardUserId.Value),
                ApprovalRequested = e.ApprovalRequested,
                ApprovalRequestedAt = e.ApprovalRequestedAt,
                ApprovedAt = e.ApprovedAt,
                ApprovalMethod = e.ApprovalMethod,
                IsDenied = e.IsDenied,
                DenialReason = e.DenialReason,
                WasOffline = e.WasOffline,
                Note = e.Note,
                VisitorPassId = e.VisitorPassId,
                DomesticStaffId = e.DomesticStaffId,
            };
        }).ToList();
    }

    /// <summary>
    /// Replays a batch the gate tablet captured while it was offline. Each entry carries its own
    /// client reference so the same batch posted twice produces one set of entries.
    /// </summary>
    public async Task<int> SyncGateBatchAsync(GateSyncBatchDto batch, Guid userId)
    {
        var applied = 0;

        foreach (var entry in batch.Entries)
        {
            entry.SocietyId = batch.SocietyId;
            entry.WasOffline = true;

            // Anything captured offline was already let in by the guard. Re-running the approval
            // path would leave a resident approving somebody who left an hour ago.
            entry.RequestApproval = false;
            entry.GuardOverride = true;
            entry.OverrideReason ??= "Captured at the gate while offline.";

            try
            {
                await RecordEntryAsync(entry, userId);
                applied++;
            }
            catch (InvalidOperationException)
            {
                // A blacklisted visitor who was admitted offline still has to appear on the log —
                // that is exactly the event a manager needs to see — so it is recorded and flagged.
                Db.GateEntries.Add(new GateEntry
                {
                    SocietyId = batch.SocietyId,
                    UnitId = entry.UnitId,
                    PersonName = entry.PersonName,
                    Kind = entry.Kind,
                    Purpose = entry.Purpose,
                    VehicleNumber = entry.VehicleNumber,
                    PersonCount = Math.Max(1, entry.PersonCount),
                    Status = GateEntryStatus.CheckedIn,
                    CheckedInAt = entry.OfflineCapturedAt ?? DateTime.UtcNow,
                    GuardUserId = userId,
                    WasOffline = true,
                    OfflineSyncedAt = DateTime.UtcNow,
                    ApprovalMethod = "OfflineFlagged",
                    Note = "Admitted offline; a rule would have blocked this entry online.",
                    Code = entry.ClientReference,
                }.StampNew(Tenant, userId));

                applied++;
            }
        }

        await Db.SaveChangesAsync();
        return applied;
    }

    public async Task<GatePassDto> CreateGatePassAsync(GatePassDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");

        var pass = new GatePass
        {
            PassNumber = await numbering.NextGatePassNumberAsync(DateTime.UtcNow),
            SocietyId = dto.SocietyId,
            UnitId = dto.UnitId,
            PassType = dto.PassType,
            ItemDescription = dto.ItemDescription,
            ItemCount = dto.ItemCount,
            VehicleNumber = dto.VehicleNumber?.Trim().ToUpperInvariant(),
            CarrierName = dto.CarrierName,
            ValidFrom = dto.ValidFrom == default ? DateTime.UtcNow : dto.ValidFrom,
            ValidTo = dto.ValidTo == default ? DateTime.UtcNow.AddHours(24) : dto.ValidTo,
            ApprovedByUserId = userId,
        }.StampNew(Tenant, userId);

        // Furniture leaving a flat with dues outstanding is how a society loses its money. The
        // check is on the pass, at the point somebody can still stop the van.
        if (dto.UnitId is not null)
        {
            var outstanding = await Db.MaintenanceBills.ForCompany(Tenant)
                .Where(b => b.UnitId == dto.UnitId && b.Balance > 0m)
                .SumAsync(b => b.Balance);

            pass.DuesCleared = outstanding <= 0m;

            if (!pass.DuesCleared && dto.PassType == "MaterialOut")
            {
                throw new InvalidOperationException(
                    $"{outstanding:N0} is outstanding on this unit. Clear it, or have the committee approve the pass explicitly.");
            }
        }

        Db.GatePasses.Add(pass);
        await Db.SaveChangesAsync();

        dto.Id = pass.Id;
        dto.PassNumber = pass.PassNumber;
        dto.DuesCleared = pass.DuesCleared;
        return dto;
    }

    // ═══ Move requests ═══════════════════════════════════════════════════════

    public async Task<MoveRequestDto> CreateMoveRequestAsync(MoveRequestDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");

        var request = new MoveRequest
        {
            Reference = await numbering.NextMasterCodeAsync(Db.MoveRequests, "MOV"),
            SocietyId = dto.SocietyId,
            UnitId = dto.UnitId,
            PartyId = dto.PartyId,
            Direction = dto.Direction,
            RequestedDate = dto.RequestedDate,
            SlotFrom = dto.SlotFrom,
            SlotTo = dto.SlotTo,
            LiftBooked = dto.LiftBooked,
            MoveCharge = dto.MoveCharge,
            SecurityDeposit = dto.SecurityDeposit,
            Status = "Requested",
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        var outstanding = await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(b => b.UnitId == dto.UnitId && b.Balance > 0m)
            .SumAsync(b => b.Balance);

        request.DuesCleared = outstanding <= 0m;

        // Two households moving through one lift on one afternoon is a fight in a stairwell. The
        // clash is refused at booking rather than discovered on the day.
        var clash = await Db.MoveRequests.ForCompany(Tenant)
            .Where(m => m.SocietyId == dto.SocietyId
                     && m.RequestedDate == dto.RequestedDate
                     && m.Status != "Rejected" && m.Status != "Cancelled"
                     && m.UnitId != dto.UnitId)
            .Where(m => m.LiftBooked != null && m.LiftBooked == dto.LiftBooked)
            .AnyAsync();

        if (clash && !string.IsNullOrWhiteSpace(dto.LiftBooked))
            throw new InvalidOperationException($"{dto.LiftBooked} is already booked for a move on {dto.RequestedDate:dd MMM yyyy}.");

        Db.MoveRequests.Add(request);
        await Db.SaveChangesAsync();

        dto.Id = request.Id;
        dto.Reference = request.Reference;
        dto.DuesCleared = request.DuesCleared;
        dto.OutstandingDues = RealEstateMapper.Money(outstanding);
        dto.Status = request.Status;
        return dto;
    }

    public async Task<MoveRequestDto> DecideMoveRequestAsync(Guid id, string status, Guid userId)
    {
        var request = await RequireAsync<MoveRequest>(id, "That move request does not exist.");

        if (status == "Approved" && !request.DuesCleared && request.Direction == "MoveOut")
            throw new InvalidOperationException("Dues are outstanding on this unit. A move-out cannot be approved until they are cleared.");

        request.Status = status;
        request.ApprovedByUserId = userId;
        request.StampUpdated(userId);

        // An approved move gets its gate pass automatically, so the guard has one thing to check.
        if (status == "Approved" && request.GatePassId is null)
        {
            var pass = new GatePass
            {
                PassNumber = await numbering.NextGatePassNumberAsync(DateTime.UtcNow),
                SocietyId = request.SocietyId,
                UnitId = request.UnitId,
                PassType = request.Direction == "MoveIn" ? "MaterialIn" : "MaterialOut",
                ItemDescription = $"Household move — {request.Reference}",
                ValidFrom = request.RequestedDate.ToDateTime(TimeOnly.MinValue),
                ValidTo = request.RequestedDate.ToDateTime(TimeOnly.MaxValue),
                ApprovedByUserId = userId,
                DuesCleared = request.DuesCleared,
            }.StampNew(Tenant, userId);

            Db.GatePasses.Add(pass);
            await Db.SaveChangesAsync();

            request.GatePassId = pass.Id;
        }

        await Db.SaveChangesAsync();

        var names = await PartyNamesAsync([request.PartyId]);

        var unitLabel = await Db.Units.ForCompany(Tenant)
            .Where(u => u.Id == request.UnitId)
            .Select(u => u.UnitNumber)
            .FirstOrDefaultAsync();

        return new MoveRequestDto
        {
            Id = request.Id,
            Reference = request.Reference,
            SocietyId = request.SocietyId,
            UnitId = request.UnitId,
            UnitLabel = unitLabel ?? "—",
            PartyId = request.PartyId,
            PartyName = names.GetValueOrDefault(request.PartyId, "—"),
            Direction = request.Direction,
            RequestedDate = request.RequestedDate,
            SlotFrom = request.SlotFrom,
            SlotTo = request.SlotTo,
            LiftBooked = request.LiftBooked,
            MoveCharge = request.MoveCharge,
            SecurityDeposit = request.SecurityDeposit,
            DuesCleared = request.DuesCleared,
            Status = request.Status,
            GatePassId = request.GatePassId,
            DamageInspectionDone = request.DamageInspectionDone,
            DamageCharge = request.DamageCharge,
            Note = request.Note,
        };
    }

    // ═══ Amenities ═══════════════════════════════════════════════════════════

    public async Task<List<AmenityDto>> GetAmenitiesAsync(Guid societyId)
        => await MapAmenitiesAsync(await Db.Amenities.ForCompany(Tenant)
            .Where(a => a.SocietyId == societyId)
            .OrderBy(a => a.Name)
            .ToListAsync());

    private async Task<List<AmenityDto>> MapAmenitiesAsync(List<Amenity> amenities)
    {
        if (amenities.Count == 0) return [];

        var today = Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var ids = amenities.Select(a => a.Id).ToList();

        var bookings = await Db.AmenityBookings.ForCompany(Tenant)
            .Where(b => ids.Contains(b.AmenityId)
                     && b.BookingDate >= monthStart
                     && b.Status != AmenityBookingStatus.Cancelled
                     && b.Status != AmenityBookingStatus.Rejected)
            .Select(b => new { b.AmenityId, b.StartTime, b.EndTime })
            .ToListAsync();

        return amenities.Select(a =>
        {
            var mine = bookings.Where(b => b.AmenityId == a.Id).ToList();

            // Utilisation is booked hours against bookable hours so far this month. It is what
            // tells a committee whether the clubhouse is worth what it costs to run.
            var open = (decimal)(a.ClosesAt - a.OpensAt).TotalHours;
            var elapsedDays = today.Day;
            var available = open * elapsedDays;
            var booked = mine.Sum(b => (decimal)(b.EndTime - b.StartTime).TotalHours);

            return new AmenityDto
            {
                Id = a.Id,
                SocietyId = a.SocietyId,
                Name = a.Name,
                AmenityType = a.AmenityType,
                Capacity = a.Capacity,
                Location = a.Location,
                PhotoUrl = a.PhotoUrl,
                IsBookable = a.IsBookable,
                RequiresApproval = a.RequiresApproval,
                OpensAt = a.OpensAt,
                ClosesAt = a.ClosesAt,
                SlotMinutes = a.SlotMinutes,
                MinAdvanceHours = a.MinAdvanceHours,
                MaxAdvanceDays = a.MaxAdvanceDays,
                MaxBookingsPerUnitPerMonth = a.MaxBookingsPerUnitPerMonth,
                ChargePerSlot = a.ChargePerSlot,
                ChargePerHour = a.ChargePerHour,
                SecurityDeposit = a.SecurityDeposit,
                CleaningCharge = a.CleaningCharge,
                CancellationWindowHours = a.CancellationWindowHours,
                LateCancellationPenalty = a.LateCancellationPenalty,
                BlockedForDefaulters = a.BlockedForDefaulters,
                IsUnderMaintenance = a.IsUnderMaintenance,
                Rules = a.Rules,
                IsActive = a.IsActive,
                BookingsThisMonth = mine.Count,
                UtilisationPercent = available > 0m ? RealEstateMapper.Percent(booked, available) : 0m,
            };
        }).ToList();
    }

    public async Task<AmenityDto> SaveAmenityAsync(AmenityDto dto, Guid userId)
    {
        var amenity = dto.Id != Guid.Empty
            ? await Db.Amenities.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (amenity is null)
        {
            amenity = new Amenity { SocietyId = dto.SocietyId }.StampNew(Tenant, userId);
            Db.Amenities.Add(amenity);
        }
        else amenity.StampUpdated(userId);

        if (dto.ClosesAt <= dto.OpensAt)
            throw new InvalidOperationException("The amenity has to close after it opens.");

        amenity.Name = dto.Name;
        amenity.AmenityType = dto.AmenityType;
        amenity.Capacity = dto.Capacity;
        amenity.Location = dto.Location;
        amenity.PhotoUrl = dto.PhotoUrl;
        amenity.IsBookable = dto.IsBookable;
        amenity.RequiresApproval = dto.RequiresApproval;
        amenity.OpensAt = dto.OpensAt;
        amenity.ClosesAt = dto.ClosesAt;
        amenity.SlotMinutes = dto.SlotMinutes <= 0 ? 60 : dto.SlotMinutes;
        amenity.MinAdvanceHours = dto.MinAdvanceHours;
        amenity.MaxAdvanceDays = dto.MaxAdvanceDays <= 0 ? 30 : dto.MaxAdvanceDays;
        amenity.MaxBookingsPerUnitPerMonth = dto.MaxBookingsPerUnitPerMonth;
        amenity.ChargePerSlot = dto.ChargePerSlot;
        amenity.ChargePerHour = dto.ChargePerHour;
        amenity.SecurityDeposit = dto.SecurityDeposit;
        amenity.CleaningCharge = dto.CleaningCharge;
        amenity.CancellationWindowHours = dto.CancellationWindowHours;
        amenity.LateCancellationPenalty = dto.LateCancellationPenalty;
        amenity.BlockedForDefaulters = dto.BlockedForDefaulters;
        amenity.IsUnderMaintenance = dto.IsUnderMaintenance;
        amenity.Rules = dto.Rules;
        amenity.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();
        return (await MapAmenitiesAsync([amenity]))[0];
    }

    /// <summary>
    /// The day's slots with what is already taken. A resident sees the whole day at once rather
    /// than guessing at a time and being refused.
    /// </summary>
    public async Task<AmenityAvailabilityDto> GetAvailabilityAsync(Guid amenityId, DateOnly date)
    {
        var amenity = await RequireAsync<Amenity>(amenityId, "That amenity does not exist.");

        var result = new AmenityAvailabilityDto
        {
            AmenityId = amenityId,
            AmenityName = amenity.Name,
            Date = date,
        };

        var bookings = await Db.AmenityBookings.ForCompany(Tenant)
            .Where(b => b.AmenityId == amenityId
                     && b.BookingDate == date
                     && b.Status != AmenityBookingStatus.Cancelled
                     && b.Status != AmenityBookingStatus.Rejected)
            .Select(b => new { b.StartTime, b.EndTime, b.GuestCount, b.UnitId })
            .ToListAsync();

        var unitIds = bookings.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var blocked = await Db.AmenitySlots.ForCompany(Tenant)
            .Where(s => s.AmenityId == amenityId && s.Date == date && s.IsBlocked)
            .Select(s => new { s.StartTime, s.EndTime, s.BlockReason })
            .ToListAsync();

        var slot = amenity.OpensAt;
        var length = TimeSpan.FromMinutes(amenity.SlotMinutes);

        while (slot + length <= amenity.ClosesAt)
        {
            var end = slot + length;

            var overlapping = bookings.Where(b => b.StartTime < end && b.EndTime > slot).ToList();
            var block = blocked.FirstOrDefault(b => b.StartTime < end && b.EndTime > slot);

            var booked = overlapping.Sum(b => b.GuestCount);
            var capacity = amenity.Capacity <= 0 ? 1 : amenity.Capacity;

            result.Slots.Add(new AmenitySlotDto
            {
                StartTime = slot,
                EndTime = end,
                Capacity = capacity,
                BookedCount = booked,
                IsBlocked = block is not null || amenity.IsUnderMaintenance,
                BlockReason = amenity.IsUnderMaintenance ? "Under maintenance" : block?.BlockReason,
                IsAvailable = block is null && !amenity.IsUnderMaintenance && booked < capacity,
                Charge = amenity.ChargePerSlot > 0m
                    ? amenity.ChargePerSlot
                    : RealEstateMapper.Money(amenity.ChargePerHour * (decimal)length.TotalHours),
                BookedByUnitLabel = overlapping.Count == 1 && overlapping[0].UnitId is not null
                    ? units.GetValueOrDefault(overlapping[0].UnitId!.Value)
                    : null,
            });

            slot = end;
        }

        return result;
    }

    public async Task<AmenityBookingDto> BookAmenityAsync(AmenityBookingCreateDto dto, Guid userId)
    {
        var amenity = await RequireAsync<Amenity>(dto.AmenityId, "That amenity does not exist.");
        var now = DateTime.UtcNow;
        var today = Today;

        if (!amenity.IsBookable) throw new InvalidOperationException($"{amenity.Name} is not bookable.");
        if (amenity.IsUnderMaintenance) throw new InvalidOperationException($"{amenity.Name} is under maintenance.");

        var start = dto.BookingDate.ToDateTime(TimeOnly.FromTimeSpan(dto.StartTime));

        if ((start - now).TotalHours < amenity.MinAdvanceHours)
            throw new InvalidOperationException($"{amenity.Name} has to be booked at least {amenity.MinAdvanceHours} hours ahead.");

        if ((dto.BookingDate.DayNumber - today.DayNumber) > amenity.MaxAdvanceDays)
            throw new InvalidOperationException($"{amenity.Name} cannot be booked more than {amenity.MaxAdvanceDays} days ahead.");

        if (dto.StartTime < amenity.OpensAt || dto.EndTime > amenity.ClosesAt)
            throw new InvalidOperationException($"{amenity.Name} is open from {amenity.OpensAt:hh\\:mm} to {amenity.ClosesAt:hh\\:mm}.");

        var partyId = dto.PartyId ?? Guid.Empty;
        Resident? resident = null;

        if (dto.ResidentId is not null)
        {
            resident = await Db.Residents.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == dto.ResidentId);

            if (resident is not null)
            {
                partyId = resident.PartyId;

                if (!resident.CanBookAmenities)
                    throw new InvalidOperationException("This resident is not permitted to book amenities.");

                // Suspending amenities for arrears is the society's most effective collection
                // lever, and it only works if the booking screen actually enforces it.
                if (amenity.BlockedForDefaulters && resident.AmenitiesSuspended)
                    throw new InvalidOperationException($"Amenity access is suspended while {resident.OutstandingDues:N0} is outstanding.");
            }
        }

        if (partyId == Guid.Empty)
            throw new InvalidOperationException("Say who the booking is for.");

        // The clash check runs inside the transaction, so two residents tapping at once cannot
        // both get the hall.
        await using var transaction = await Db.Database.BeginTransactionAsync();

        var overlapping = await Db.AmenityBookings.ForCompany(Tenant)
            .Where(b => b.AmenityId == dto.AmenityId
                     && b.BookingDate == dto.BookingDate
                     && b.Status != AmenityBookingStatus.Cancelled
                     && b.Status != AmenityBookingStatus.Rejected
                     && b.StartTime < dto.EndTime && b.EndTime > dto.StartTime)
            .Select(b => b.GuestCount)
            .ToListAsync();

        var capacity = amenity.Capacity <= 0 ? 1 : amenity.Capacity;

        if (overlapping.Sum() + dto.GuestCount > capacity)
            throw new InvalidOperationException($"{amenity.Name} is already booked for that slot.");

        if (amenity.MaxBookingsPerUnitPerMonth > 0 && dto.UnitId is not null)
        {
            var monthStart = new DateOnly(dto.BookingDate.Year, dto.BookingDate.Month, 1);

            var count = await Db.AmenityBookings.ForCompany(Tenant)
                .CountAsync(b => b.AmenityId == dto.AmenityId
                              && b.UnitId == dto.UnitId
                              && b.BookingDate >= monthStart
                              && b.BookingDate < monthStart.AddMonths(1)
                              && b.Status != AmenityBookingStatus.Cancelled
                              && b.Status != AmenityBookingStatus.Rejected);

            if (count >= amenity.MaxBookingsPerUnitPerMonth)
                throw new InvalidOperationException($"This unit has used all {amenity.MaxBookingsPerUnitPerMonth} of its bookings for the month.");
        }

        var hours = (decimal)(dto.EndTime - dto.StartTime).TotalHours;

        var charge = amenity.ChargePerSlot > 0m
            ? amenity.ChargePerSlot
            : RealEstateMapper.Money(amenity.ChargePerHour * hours);

        charge += amenity.CleaningCharge;

        var booking = new AmenityBooking
        {
            Reference = await numbering.NextMasterCodeAsync(Db.AmenityBookings, "AMB"),
            AmenityId = dto.AmenityId,
            SocietyId = amenity.SocietyId,
            UnitId = dto.UnitId ?? resident?.UnitId,
            ResidentId = dto.ResidentId,
            PartyId = partyId,
            BookingDate = dto.BookingDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            GuestCount = Math.Max(1, dto.GuestCount),
            Purpose = dto.Purpose,
            Status = amenity.RequiresApproval ? AmenityBookingStatus.Requested : AmenityBookingStatus.Approved,
            ChargeAmount = RealEstateMapper.Money(charge),
            DepositAmount = amenity.SecurityDeposit,
        }.StampNew(Tenant, userId);

        if (!amenity.RequiresApproval)
        {
            booking.ApprovedAt = now;
            booking.ApprovedByUserId = userId;
        }

        Db.AmenityBookings.Add(booking);
        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await MapBookingsAsync([booking]))[0];
    }

    public async Task<AmenityBookingDto> DecideAmenityBookingAsync(Guid id, bool approved, string? reason, Guid userId)
    {
        var booking = await RequireAsync<AmenityBooking>(id, "That booking does not exist.");

        if (booking.Status is AmenityBookingStatus.Cancelled)
            throw new InvalidOperationException("This booking was cancelled.");

        if (!approved && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Refusing a booking has to say why.");

        booking.Status = approved ? AmenityBookingStatus.Approved : AmenityBookingStatus.Rejected;
        booking.ApprovedByUserId = userId;
        booking.ApprovedAt = DateTime.UtcNow;
        booking.RejectionReason = approved ? null : reason;
        booking.StampUpdated(userId);

        await QueueNotificationAsync(
            approved ? "AmenityBookingConfirmed" : "AmenityBookingRejected",
            approved ? "Your amenity booking is confirmed" : "Your amenity booking could not be confirmed",
            approved
                ? $"{booking.BookingDate:ddd d MMM}, {booking.StartTime:hh\\:mm} to {booking.EndTime:hh\\:mm}."
                : reason,
            $"/realestate/amenity-bookings/{booking.Id}",
            recipientPartyId: booking.PartyId,
            entityType: "AmenityBooking",
            entityId: booking.Id);

        await Db.SaveChangesAsync();
        return (await MapBookingsAsync([booking]))[0];
    }

    public async Task<AmenityBookingDto> CancelAmenityBookingAsync(Guid id, string? reason, Guid userId)
    {
        var booking = await RequireAsync<AmenityBooking>(id, "That booking does not exist.");

        if (booking.Status == AmenityBookingStatus.Cancelled)
            throw new InvalidOperationException("This booking is already cancelled.");

        var amenity = await RequireAsync<Amenity>(booking.AmenityId, "The amenity is missing.");
        var start = booking.BookingDate.ToDateTime(TimeOnly.FromTimeSpan(booking.StartTime));
        var hoursNotice = (start - DateTime.UtcNow).TotalHours;

        booking.Status = AmenityBookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.PostUseNote = reason;

        // A late cancellation stops somebody else from using the slot, so it carries the penalty
        // the rules set rather than being free.
        if (hoursNotice < amenity.CancellationWindowHours && amenity.LateCancellationPenalty > 0m)
            booking.CancellationPenalty = amenity.LateCancellationPenalty;

        booking.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await MapBookingsAsync([booking]))[0];
    }

    public async Task<PaginatedResponse<AmenityBookingDto>> GetAmenityBookingsAsync(
        ListQueryDto query, Guid societyId, AmenityBookingStatus? status)
    {
        var q = Db.AmenityBookings.ForCompany(Tenant)
            .Where(b => b.SocietyId == societyId)
            .WhereIf(status.HasValue, b => b.Status == status)
            .WhereIf(query.FromDate.HasValue, b => b.BookingDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, b => b.BookingDate <= query.ToDate)
            .OrderByDescending(b => b.BookingDate).ThenBy(b => b.StartTime);

        return await PageAsync(q, query, MapBookingsAsync);
    }

    private async Task<List<AmenityBookingDto>> MapBookingsAsync(List<AmenityBooking> bookings)
    {
        if (bookings.Count == 0) return [];

        var currency = await CurrencyAsync();

        var amenityIds = bookings.Select(b => b.AmenityId).Distinct().ToList();

        var amenities = await Db.Amenities.ForCompany(Tenant)
            .Where(a => amenityIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name);

        var unitIds = bookings.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => bookings.Select(b => b.PartyId).Contains(p.Id))
            .ToListAsync();

        var approvers = await AgentUserNamesAsync(bookings.Select(b => b.ApprovedByUserId));

        return bookings.Select(b =>
        {
            var person = people.FirstOrDefault(p => p.Id == b.PartyId);

            return new AmenityBookingDto
            {
                Id = b.Id,
                Reference = b.Reference,
                AmenityId = b.AmenityId,
                AmenityName = amenities.GetValueOrDefault(b.AmenityId, "—"),
                SocietyId = b.SocietyId,
                UnitId = b.UnitId,
                UnitLabel = b.UnitId is null ? null : units.GetValueOrDefault(b.UnitId.Value),
                PartyId = b.PartyId,
                PartyName = person is null ? "—" : RealEstateMapper.DisplayName(person),
                Phone = person?.PrimaryPhone,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                GuestCount = b.GuestCount,
                Purpose = b.Purpose,
                Status = b.Status,
                ChargeAmount = b.ChargeAmount,
                DepositAmount = b.DepositAmount,
                PaidAmount = b.PaidAmount,
                CurrencyCode = currency,
                ApprovedByName = b.ApprovedByUserId is null ? null : approvers.GetValueOrDefault(b.ApprovedByUserId.Value),
                ApprovedAt = b.ApprovedAt,
                RejectionReason = b.RejectionReason,
                CancelledAt = b.CancelledAt,
                CancellationPenalty = b.CancellationPenalty,
                DepositRefunded = b.DepositRefunded,
                DamageDeduction = b.DamageDeduction,
                PostUseNote = b.PostUseNote,
            };
        }).ToList();
    }
}
