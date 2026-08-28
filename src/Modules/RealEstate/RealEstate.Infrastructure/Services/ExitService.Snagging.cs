using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Snagging, punch lists, the defect liability period, and NOCs.
///
/// Snagging is done on a tablet in an empty flat with no signal, so the sync endpoint is built to
/// be replayed: every snag carries a client reference and a second post of the same batch updates
/// rather than duplicates. A snag closes on a photograph, not on a status click — the before/after
/// pair is what a customer will accept and what a contractor cannot argue with.
/// </summary>
public partial class ExitService
{
    // ═══ Snagging ════════════════════════════════════════════════════════════

    public async Task<SnagInspectionDto> CreateInspectionAsync(SnagInspectionDto dto, Guid userId)
    {
        var inspection = dto.Id != Guid.Empty
            ? await Db.SnagInspections.ForCompany(Tenant).Include(i => i.Snags).FirstOrDefaultAsync(i => i.Id == dto.Id)
            : null;

        if (inspection is null)
        {
            inspection = new SnagInspection
            {
                Reference = await numbering.NextMasterCodeAsync(Db.SnagInspections, "SNG"),
                UnitId = dto.UnitId,
                ProjectId = dto.ProjectId,
                BookingId = dto.BookingId,
                ClientBuildContractId = dto.ClientBuildContractId,
            }.StampNew(Tenant, userId);

            Db.SnagInspections.Add(inspection);
        }
        else
        {
            if (inspection.IsClosed)
                throw new InvalidOperationException("This inspection is closed. Raise a re-inspection instead.");

            inspection.StampUpdated(userId);
        }

        inspection.InspectionType = dto.InspectionType;
        inspection.InspectedAt = dto.InspectedAt == default ? DateTime.UtcNow : dto.InspectedAt;
        inspection.InspectorUserId = userId;
        inspection.CustomerPresent = dto.CustomerPresent;
        inspection.TargetClosureDate = dto.TargetClosureDate;
        inspection.CustomerSignatureUrl = dto.CustomerSignatureUrl;
        inspection.InspectorSignatureUrl = dto.InspectorSignatureUrl;

        if (inspection.UnitId is not null && inspection.CustomerPartyId is null)
        {
            inspection.CustomerPartyId = await Db.Bookings.ForCompany(Tenant)
                .Where(b => b.UnitId == inspection.UnitId && b.Status != BookingStatus.Cancelled)
                .Select(b => (Guid?)b.PrimaryApplicantPartyId)
                .FirstOrDefaultAsync();
        }

        await Db.SaveChangesAsync();
        await RollUpInspectionAsync(inspection.Id, userId);

        return (await GetInspectionAsync(inspection.Id))!;
    }

    public async Task<SnagInspectionDto?> GetInspectionAsync(Guid id)
    {
        var inspection = await Db.SnagInspections.ForCompany(Tenant)
            .Include(i => i.Snags).ThenInclude(s => s.Photos)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inspection is null) return null;
        return (await MapInspectionsAsync([inspection], true))[0];
    }

    public async Task<PaginatedResponse<SnagInspectionDto>> GetInspectionsAsync(ListQueryDto query, Guid? projectId, bool openOnly)
    {
        var q = Db.SnagInspections.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), i => i.Reference.Contains(query.Search!))
            .WhereIf(projectId.HasValue, i => i.ProjectId == projectId)
            .WhereIf(openOnly, i => !i.IsClosed)
            .OrderByDescending(i => i.InspectedAt);

        return await PageAsync(q, query, list => MapInspectionsAsync(list, false));
    }

    private async Task<List<SnagInspectionDto>> MapInspectionsAsync(List<SnagInspection> inspections, bool includeSnags)
    {
        if (inspections.Count == 0) return [];

        var today = Today;

        var unitIds = inspections.Where(i => i.UnitId.HasValue).Select(i => i.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var projects = await ProjectNamesAsync(inspections.Select(i => i.ProjectId));

        var partyIds = inspections.Where(i => i.CustomerPartyId.HasValue).Select(i => i.CustomerPartyId!.Value)
            .Concat(inspections.Where(i => i.ContractorPartyId.HasValue).Select(i => i.ContractorPartyId!.Value));

        var parties = await PartyNamesAsync(partyIds);
        var inspectors = await AgentUserNamesAsync(inspections.Select(i => i.InspectorUserId));

        var result = new List<SnagInspectionDto>();

        foreach (var i in inspections)
        {
            var total = i.CriticalCount + i.MajorCount + i.MinorCount;

            var dto = new SnagInspectionDto
            {
                Id = i.Id,
                Reference = i.Reference,
                UnitId = i.UnitId,
                UnitNumber = i.UnitId is null ? null : units.GetValueOrDefault(i.UnitId.Value),
                ProjectId = i.ProjectId,
                ProjectName = i.ProjectId is null ? null : projects.GetValueOrDefault(i.ProjectId.Value),
                BookingId = i.BookingId,
                ClientBuildContractId = i.ClientBuildContractId,
                InspectionType = i.InspectionType,
                InspectedAt = i.InspectedAt,
                InspectorName = i.InspectorUserId is null ? null : inspectors.GetValueOrDefault(i.InspectorUserId.Value),
                CustomerPresent = i.CustomerPresent,
                CustomerName = i.CustomerPartyId is null ? null : parties.GetValueOrDefault(i.CustomerPartyId.Value),
                ContractorName = i.ContractorPartyId is null ? null : parties.GetValueOrDefault(i.ContractorPartyId.Value),
                CriticalCount = i.CriticalCount,
                MajorCount = i.MajorCount,
                MinorCount = i.MinorCount,
                ClosedCount = i.ClosedCount,
                OpenCount = Math.Max(0, total - i.ClosedCount),
                PercentClosed = RealEstateMapper.Percent(i.ClosedCount, total),
                BlocksHandover = i.BlocksHandover,
                TargetClosureDate = i.TargetClosureDate,
                CustomerSignatureUrl = i.CustomerSignatureUrl,
                InspectorSignatureUrl = i.InspectorSignatureUrl,
                IsClosed = i.IsClosed,
                PunchListId = i.PunchListId,
            };

            if (includeSnags && i.Snags.Count > 0)
                dto.Snags = await MapSnagsAsync(i.Snags.OrderBy(s => s.SnagNumber).ToList(), today);

            result.Add(dto);
        }

        return result;
    }

    private async Task<List<SnagDto>> MapSnagsAsync(List<Snag> snags, DateOnly today)
    {
        if (snags.Count == 0) return [];

        var assignees = await AgentUserNamesAsync(
            snags.Select(s => s.AssignedToUserId).Concat(snags.Select(s => s.VerifiedByUserId)));

        var responsible = await PartyNamesAsync(
            snags.Where(s => s.ResponsiblePartyId.HasValue).Select(s => s.ResponsiblePartyId!.Value));

        return snags.Select(s => new SnagDto
        {
            Id = s.Id,
            SnagNumber = s.SnagNumber,
            Zone = s.Zone,
            Severity = s.Severity,
            Status = s.Status,
            Description = s.Description ?? string.Empty,
            Location = s.Location,
            PlanX = s.PlanX,
            PlanY = s.PlanY,
            ResponsibleParty = s.ResponsibleParty,
            ResponsibleName = s.ResponsiblePartyId is null ? null : responsible.GetValueOrDefault(s.ResponsiblePartyId.Value),
            AssignedToName = s.AssignedToUserId is null ? null : assignees.GetValueOrDefault(s.AssignedToUserId.Value),
            WorkOrderId = s.WorkOrderId,
            TargetDate = s.TargetDate,
            FixedOn = s.FixedOn,
            VerifiedOn = s.VerifiedOn,
            VerifiedByName = s.VerifiedByUserId is null ? null : assignees.GetValueOrDefault(s.VerifiedByUserId.Value),
            EstimatedCost = s.EstimatedCost,
            ActualCost = s.ActualCost,
            RejectionReason = s.RejectionReason,
            IsOverdue = s.Status is not (SnagStatus.Verified or SnagStatus.Rejected)
                     && s.TargetDate is not null && s.TargetDate < today,

            Photos = s.Photos.OrderBy(p => p.Stage).ThenBy(p => p.CapturedAt).Select(p => new SnagPhotoDto
            {
                Id = p.Id,
                Url = p.Url,
                Stage = p.Stage,
                CapturedAt = p.CapturedAt,
                Caption = p.Caption,
            }).ToList(),
        }).ToList();
    }

    public async Task<SnagDto> SaveSnagAsync(SnagUpsertDto dto, Guid userId)
    {
        var inspection = await Db.SnagInspections.ForCompany(Tenant)
            .FirstOrDefaultAsync(i => i.Id == dto.SnagInspectionId)
            ?? throw new InvalidOperationException("That inspection does not exist.");

        if (inspection.IsClosed)
            throw new InvalidOperationException("This inspection is closed. Raise a re-inspection to record new items.");

        var snag = await ResolveSnagAsync(dto, inspection.Id);

        if (snag is null)
        {
            var next = await Db.Snags.ForCompany(Tenant)
                .Where(s => s.SnagInspectionId == inspection.Id)
                .MaxAsync(s => (int?)s.SnagNumber) ?? 0;

            snag = new Snag
            {
                SnagInspectionId = inspection.Id,
                SnagNumber = next + 1,
                Code = dto.ClientReference,
            }.StampNew(Tenant, userId);

            Db.Snags.Add(snag);
        }
        else snag.StampUpdated(userId);

        snag.Zone = dto.Zone;
        snag.Severity = dto.Severity;
        snag.Description = dto.Description;
        snag.Location = dto.Location;
        snag.PlanX = dto.PlanX;
        snag.PlanY = dto.PlanY;
        snag.ResponsibleParty = dto.ResponsibleParty;
        snag.ResponsiblePartyId = dto.ResponsiblePartyId;
        snag.SubcontractId = dto.SubcontractId;
        snag.AssignedToUserId = dto.AssignedToUserId;
        snag.EstimatedCost = dto.EstimatedCost;

        if (dto.AssignedToUserId is not null && snag.Status == SnagStatus.Open)
            snag.Status = SnagStatus.Assigned;

        // A target date nobody sets is a target nobody meets, so severity implies one.
        snag.TargetDate = dto.TargetDate ?? snag.TargetDate ?? dto.Severity switch
        {
            SnagSeverity.Critical => Today.AddDays(3),
            SnagSeverity.Major => Today.AddDays(14),
            _ => Today.AddDays(30),
        };

        await SavePhotosAsync(snag, dto.Photos, userId);

        await Db.SaveChangesAsync();
        await RollUpInspectionAsync(inspection.Id, userId);

        var reloaded = await Db.Snags.ForCompany(Tenant)
            .Include(s => s.Photos)
            .FirstAsync(s => s.Id == snag.Id);

        return (await MapSnagsAsync([reloaded], Today))[0];
    }

    private async Task<Snag?> ResolveSnagAsync(SnagUpsertDto dto, Guid inspectionId)
    {
        if (dto.Id is not null && dto.Id != Guid.Empty)
        {
            return await Db.Snags.ForCompany(Tenant)
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(s => s.Id == dto.Id);
        }

        // The client reference is how an offline batch replayed twice updates rather than
        // duplicates. Without it, a flaky connection produces two of every snag.
        if (!string.IsNullOrWhiteSpace(dto.ClientReference))
        {
            return await Db.Snags.ForCompany(Tenant)
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(s => s.SnagInspectionId == inspectionId && s.Code == dto.ClientReference);
        }

        return null;
    }

    private async Task SavePhotosAsync(Snag snag, List<SnagPhotoDto> photos, Guid userId)
    {
        foreach (var p in photos)
        {
            if (snag.Photos.Any(existing => existing.Url == p.Url)) continue;

            snag.Photos.Add(new SnagPhoto
            {
                SnagId = snag.Id,
                Url = p.Url,
                Stage = p.Stage,
                CapturedAt = p.CapturedAt == default ? DateTime.UtcNow : p.CapturedAt,
                CapturedByUserId = userId,
                Caption = p.Caption,
            }.StampNew(Tenant, userId));
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Replays a batch captured offline. Idempotent by client reference, so a tablet that syncs,
    /// times out and syncs again produces one set of snags rather than two.
    /// </summary>
    public async Task<SnagInspectionDto> SyncSnagsAsync(SnagSyncBatchDto batch, Guid userId)
    {
        var inspection = await Db.SnagInspections.ForCompany(Tenant)
            .FirstOrDefaultAsync(i => i.Id == batch.SnagInspectionId)
            ?? throw new InvalidOperationException("That inspection does not exist.");

        if (inspection.IsClosed)
            throw new InvalidOperationException("This inspection was closed before your batch arrived. Nothing was applied.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var next = await Db.Snags.ForCompany(Tenant)
            .Where(s => s.SnagInspectionId == inspection.Id)
            .MaxAsync(s => (int?)s.SnagNumber) ?? 0;

        foreach (var dto in batch.Snags)
        {
            dto.SnagInspectionId = inspection.Id;

            var snag = await ResolveSnagAsync(dto, inspection.Id);

            if (snag is null)
            {
                snag = new Snag
                {
                    SnagInspectionId = inspection.Id,
                    SnagNumber = ++next,
                    Code = dto.ClientReference,
                }.StampNew(Tenant, userId);

                Db.Snags.Add(snag);
            }
            else snag.StampUpdated(userId);

            snag.Zone = dto.Zone;
            snag.Severity = dto.Severity;
            snag.Description = dto.Description;
            snag.Location = dto.Location;
            snag.PlanX = dto.PlanX;
            snag.PlanY = dto.PlanY;
            snag.ResponsibleParty = dto.ResponsibleParty;
            snag.ResponsiblePartyId = dto.ResponsiblePartyId;
            snag.SubcontractId = dto.SubcontractId;
            snag.AssignedToUserId = dto.AssignedToUserId;
            snag.EstimatedCost = dto.EstimatedCost;

            snag.TargetDate ??= dto.TargetDate ?? dto.Severity switch
            {
                SnagSeverity.Critical => Today.AddDays(3),
                SnagSeverity.Major => Today.AddDays(14),
                _ => Today.AddDays(30),
            };

            await SavePhotosAsync(snag, dto.Photos, userId);
        }

        await Db.SaveChangesAsync();
        await RollUpInspectionAsync(inspection.Id, userId);
        await transaction.CommitAsync();

        return (await GetInspectionAsync(inspection.Id))!;
    }

    public async Task<SnagDto> ChangeSnagStatusAsync(
        Guid snagId, SnagStatus status, string? note, List<SnagPhotoDto>? photos, Guid userId)
    {
        var snag = await Db.Snags.ForCompany(Tenant)
            .Include(s => s.Photos)
            .FirstOrDefaultAsync(s => s.Id == snagId)
            ?? throw new InvalidOperationException("That snag does not exist.");

        var today = Today;

        if (photos is not null) await SavePhotosAsync(snag, photos, userId);

        // A snag closes on evidence, not on a click. This is the rule that makes a punch list
        // worth signing and a retention release defensible.
        if (status == SnagStatus.Fixed && !snag.Photos.Any(p => p.Stage == "After"))
            throw new InvalidOperationException("Attach an \"after\" photograph before marking this snag fixed.");

        if (status == SnagStatus.Rejected && string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("Rejecting a snag has to say why.");

        if (status == SnagStatus.Verified && snag.Status != SnagStatus.Fixed)
            throw new InvalidOperationException("A snag has to be marked fixed before it can be verified.");

        snag.Status = status;

        if (status == SnagStatus.Fixed) snag.FixedOn = today;

        if (status == SnagStatus.Verified)
        {
            snag.VerifiedOn = today;
            snag.VerifiedByUserId = userId;
        }

        if (status == SnagStatus.Rejected) snag.RejectionReason = note;

        snag.StampUpdated(userId);

        await Db.SaveChangesAsync();
        await RollUpInspectionAsync(snag.SnagInspectionId, userId);

        return (await MapSnagsAsync([snag], today))[0];
    }

    /// <summary>
    /// Recomputes the inspection's counters and the handover block. Derived every time rather than
    /// incremented, because an incremented counter that drifts is worse than no counter at all.
    /// </summary>
    private async Task RollUpInspectionAsync(Guid inspectionId, Guid userId)
    {
        var inspection = await Db.SnagInspections.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == inspectionId);
        if (inspection is null) return;

        var snags = await Db.Snags.ForCompany(Tenant)
            .Where(s => s.SnagInspectionId == inspectionId)
            .Select(s => new { s.Severity, s.Status })
            .ToListAsync();

        inspection.CriticalCount = snags.Count(s => s.Severity == SnagSeverity.Critical);
        inspection.MajorCount = snags.Count(s => s.Severity == SnagSeverity.Major);
        inspection.MinorCount = snags.Count(s => s.Severity == SnagSeverity.Minor);

        inspection.ClosedCount = snags.Count(s => s.Status is SnagStatus.Verified or SnagStatus.Rejected);

        inspection.BlocksHandover = snags.Any(s => s.Severity == SnagSeverity.Critical
                                                && s.Status is not (SnagStatus.Verified or SnagStatus.Rejected));

        inspection.StampUpdated(userId);
        await Db.SaveChangesAsync();
    }

    public async Task<PunchListDto> IssuePunchListAsync(Guid inspectionId, DateOnly? agreedClosureDate, Guid userId)
    {
        var inspection = await Db.SnagInspections.ForCompany(Tenant)
            .Include(i => i.Snags)
            .FirstOrDefaultAsync(i => i.Id == inspectionId)
            ?? throw new InvalidOperationException("That inspection does not exist.");

        var open = inspection.Snags.Count(s => s.Status is not (SnagStatus.Verified or SnagStatus.Rejected));

        if (open == 0)
            throw new InvalidOperationException("Every snag on this inspection is closed. There is nothing to list.");

        var today = Today;

        var punchList = await Db.PunchLists.ForCompany(Tenant)
            .FirstOrDefaultAsync(p => p.SnagInspectionId == inspectionId);

        if (punchList is null)
        {
            punchList = new PunchList
            {
                Reference = await numbering.NextMasterCodeAsync(Db.PunchLists, "PNL"),
                SnagInspectionId = inspectionId,
                UnitId = inspection.UnitId,
                ProjectId = inspection.ProjectId,
                IssuedOn = today,
            }.StampNew(Tenant, userId);

            Db.PunchLists.Add(punchList);
        }
        else punchList.StampUpdated(userId);

        punchList.AgreedClosureDate = agreedClosureDate ?? inspection.TargetClosureDate;
        punchList.ItemCount = inspection.Snags.Count;
        punchList.ClosedCount = inspection.ClosedCount;
        punchList.PercentComplete = RealEstateMapper.Percent(inspection.ClosedCount, inspection.Snags.Count);
        punchList.IssuerSigned = true;
        punchList.IsClosed = open == 0;

        if (punchList.IsClosed) punchList.ClosedOn = today;

        // Retention against an open list is the leverage that gets it closed, so the number is
        // pulled from the subcontract rather than typed.
        if (punchList.SubcontractId is not null)
        {
            punchList.RetentionHeldAgainst = await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => s.Id == punchList.SubcontractId)
                .Select(s => s.RetentionHeld)
                .FirstOrDefaultAsync();
        }

        inspection.PunchListId = punchList.Id;
        inspection.StampUpdated(userId);

        await Db.SaveChangesAsync();

        var contractorName = punchList.SubcontractId is null
            ? null
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => s.Id == punchList.SubcontractId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();

        var unitNumber = punchList.UnitId is null
            ? null
            : await Db.Units.ForCompany(Tenant).Where(u => u.Id == punchList.UnitId).Select(u => u.UnitNumber).FirstOrDefaultAsync();

        return new PunchListDto
        {
            Id = punchList.Id,
            Reference = punchList.Reference,
            SnagInspectionId = punchList.SnagInspectionId,
            UnitId = punchList.UnitId,
            UnitNumber = unitNumber,
            SubcontractId = punchList.SubcontractId,
            ContractorName = contractorName,
            IssuedOn = punchList.IssuedOn,
            AgreedClosureDate = punchList.AgreedClosureDate,
            ItemCount = punchList.ItemCount,
            ClosedCount = punchList.ClosedCount,
            PercentComplete = punchList.PercentComplete,
            IssuerSigned = punchList.IssuerSigned,
            CounterpartySigned = punchList.CounterpartySigned,
            DocumentUrl = punchList.DocumentUrl,
            RetentionHeldAgainst = punchList.RetentionHeldAgainst,
            IsClosed = punchList.IsClosed,
            ClosedOn = punchList.ClosedOn,
            IsOverdue = !punchList.IsClosed && punchList.AgreedClosureDate is not null && punchList.AgreedClosureDate < today,
        };
    }

    // ═══ Defect liability ════════════════════════════════════════════════════

    public async Task<List<DefectLiabilityDto>> GetLiabilitiesAsync(Guid? unitId, Guid? projectId, bool activeOnly)
    {
        var today = Today;

        var liabilities = await Db.DefectLiabilities.ForCompany(Tenant)
            .WhereIf(unitId.HasValue, l => l.UnitId == unitId)
            .WhereIf(projectId.HasValue, l => l.ProjectId == projectId)
            .WhereIf(activeOnly, l => l.ExpiresOn >= today)
            .OrderBy(l => l.ExpiresOn)
            .ToListAsync();

        if (liabilities.Count == 0) return [];

        var ids = liabilities.Select(l => l.Id).ToList();

        var claims = await Db.DefectClaims.ForCompany(Tenant)
            .Where(c => c.DefectLiabilityId != null
                     && ids.Contains(c.DefectLiabilityId.Value)
                     && c.Status != TicketStatus.Closed
                     && c.Status != TicketStatus.Resolved)
            .GroupBy(c => c.DefectLiabilityId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var unitIds = liabilities.Where(l => l.UnitId.HasValue).Select(l => l.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var liable = await PartyNamesAsync(liabilities.Where(l => l.LiablePartyId.HasValue).Select(l => l.LiablePartyId!.Value));

        return liabilities.Select(l => new DefectLiabilityDto
        {
            Id = l.Id,
            UnitId = l.UnitId,
            UnitNumber = l.UnitId is null ? null : units.GetValueOrDefault(l.UnitId.Value),
            ProjectId = l.ProjectId,
            ClientBuildContractId = l.ClientBuildContractId,
            SubcontractId = l.SubcontractId,
            Category = l.Category,
            StartsOn = l.StartsOn,
            DurationMonths = l.DurationMonths,
            ExpiresOn = l.ExpiresOn,
            DaysRemaining = l.ExpiresOn.DayNumber - today.DayNumber,
            LiableParty = l.LiableParty,
            LiablePartyName = l.LiablePartyId is null ? null : liable.GetValueOrDefault(l.LiablePartyId.Value),
            ResponseSlaDaysCritical = l.ResponseSlaDaysCritical,
            ResponseSlaDaysMajor = l.ResponseSlaDaysMajor,
            ResponseSlaDaysMinor = l.ResponseSlaDaysMinor,
            ExpiryNoticeSent = l.ExpiryNoticeSent,
            IsExpired = l.ExpiresOn < today,
            OpenClaimCount = claims.GetValueOrDefault(l.Id),
        }).ToList();
    }

    /// <summary>
    /// A defect reported by the customer. The claim is matched to the liability period for its
    /// own category — reporting a cracked tile does not get the structural warranty's five years,
    /// and reporting a structural crack does not get the finishes' twelve months.
    /// </summary>
    public async Task<DefectClaimDto> CreateDefectClaimAsync(DefectClaimDto dto, Guid userId)
    {
        var today = Today;

        var liability = await Db.DefectLiabilities.ForCompany(Tenant)
            .Where(l => l.UnitId == dto.UnitId && l.Category == dto.Category)
            .OrderByDescending(l => l.ExpiresOn)
            .FirstOrDefaultAsync();

        var claim = new DefectClaim
        {
            Reference = await numbering.NextComplaintNumberAsync(DateTime.UtcNow),
            DefectLiabilityId = liability?.Id,
            UnitId = dto.UnitId,
            BookingId = dto.BookingId,
            PartyId = dto.PartyId,
            Category = dto.Category,
            Severity = dto.Severity,
            Description = dto.Description,
            Status = TicketStatus.Open,
            ReportedOn = dto.ReportedOn == default ? today : dto.ReportedOn,
            CostBearer = CostBearer.Developer,
        }.StampNew(Tenant, userId);

        // Outside the period is refused with the date, not with a shrug. A customer told "your
        // waterproofing cover ran to 12 March 2029" accepts it; one told "not covered" does not.
        if (liability is null)
        {
            claim.IsRejected = true;
            claim.Status = TicketStatus.Closed;
            claim.RejectionReason = $"No {dto.Category} warranty is recorded against this unit.";
        }
        else if (liability.ExpiresOn < claim.ReportedOn)
        {
            claim.IsRejected = true;
            claim.Status = TicketStatus.Closed;
            claim.RejectionReason = $"The {dto.Category} warranty on this unit ran to {liability.ExpiresOn:dd MMM yyyy}.";
        }
        else
        {
            var days = dto.Severity switch
            {
                SnagSeverity.Critical => liability.ResponseSlaDaysCritical,
                SnagSeverity.Major => liability.ResponseSlaDaysMajor,
                _ => liability.ResponseSlaDaysMinor,
            };

            claim.SlaDueAt = DateTime.UtcNow.AddDays(days);
        }

        Db.DefectClaims.Add(claim);
        await Db.SaveChangesAsync();

        return (await MapClaimsAsync([claim]))[0];
    }

    public async Task<DefectClaimDto> DecideDefectClaimAsync(Guid id, bool accepted, string? reason, Guid userId)
    {
        var claim = await RequireAsync<DefectClaim>(id, "That claim does not exist.");

        if (!accepted && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Refusing a warranty claim has to say why.");

        var today = Today;

        if (accepted)
        {
            claim.Status = TicketStatus.Assigned;
            claim.IsRejected = false;
            claim.RejectionReason = null;

            // An accepted claim becomes a work order, so it lands in the same queue the
            // maintenance team already works from rather than a parallel list.
            if (claim.WorkOrderId is null)
            {
                var workOrder = new WorkOrder
                {
                    OrderNumber = await numbering.NextWorkOrderNumberAsync(DateTime.UtcNow),
                    Source = WorkOrderSource.DefectClaim,
                    UnitId = claim.UnitId,
                    Title = $"Warranty claim {claim.Reference}",
                    Description = claim.Description,
                    DefectClaimId = claim.Id,
                    Priority = claim.Severity switch
                    {
                        SnagSeverity.Critical => TicketPriority.Emergency,
                        SnagSeverity.Major => TicketPriority.High,
                        _ => TicketPriority.Normal,
                    },
                    Status = WorkOrderStatus.Raised,
                    RaisedByPartyId = claim.PartyId,
                    RaisedByUserId = userId,
                    RaisedAt = DateTime.UtcNow,
                    CostBearer = claim.CostBearer,
                    CompletionDueAt = claim.SlaDueAt,
                }.StampNew(Tenant, userId);

                Db.WorkOrders.Add(workOrder);
                claim.WorkOrderId = workOrder.Id;
            }
        }
        else
        {
            claim.Status = TicketStatus.Closed;
            claim.IsRejected = true;
            claim.RejectionReason = reason;
            claim.ResolvedOn = today;
        }

        claim.StampUpdated(userId);

        await QueueNotificationAsync(
            accepted ? "DefectClaimAccepted" : "DefectClaimRejected",
            accepted ? $"{claim.Reference} accepted" : $"{claim.Reference} could not be accepted",
            accepted ? "A team has been assigned and will contact you." : reason,
            $"/realestate/defect-claims/{claim.Id}",
            recipientPartyId: claim.PartyId,
            entityType: "DefectClaim",
            entityId: claim.Id);

        await Db.SaveChangesAsync();
        return (await MapClaimsAsync([claim]))[0];
    }

    public async Task<PaginatedResponse<DefectClaimDto>> GetDefectClaimsAsync(ListQueryDto query, TicketStatus? status)
    {
        var q = Db.DefectClaims.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), c => c.Reference.Contains(query.Search!))
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(query.FromDate.HasValue, c => c.ReportedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, c => c.ReportedOn <= query.ToDate)
            .OrderByDescending(c => c.SlaBreached).ThenByDescending(c => c.ReportedOn);

        return await PageAsync(q, query, MapClaimsAsync);
    }

    private async Task<List<DefectClaimDto>> MapClaimsAsync(List<DefectClaim> claims)
    {
        if (claims.Count == 0) return [];

        var now = DateTime.UtcNow;
        var today = Today;

        var parties = await Db.Parties.ForCompany(Tenant)
            .Where(p => claims.Select(c => c.PartyId).Contains(p.Id))
            .ToListAsync();

        var unitIds = claims.Where(c => c.UnitId.HasValue).Select(c => c.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var liabilityIds = claims.Where(c => c.DefectLiabilityId.HasValue).Select(c => c.DefectLiabilityId!.Value).Distinct().ToList();

        var liabilities = liabilityIds.Count == 0
            ? []
            : await Db.DefectLiabilities.ForCompany(Tenant)
                .Where(l => liabilityIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.ExpiresOn);

        return claims.Select(c =>
        {
            var person = parties.FirstOrDefault(p => p.Id == c.PartyId);

            return new DefectClaimDto
            {
                Id = c.Id,
                Reference = c.Reference,
                UnitId = c.UnitId,
                UnitNumber = c.UnitId is null ? null : units.GetValueOrDefault(c.UnitId.Value),
                BookingId = c.BookingId,
                PartyId = c.PartyId,
                PartyName = person is null ? "—" : RealEstateMapper.DisplayName(person),
                PartyPhone = person?.PrimaryPhone,
                Category = c.Category,
                Severity = c.Severity,
                Status = c.Status,
                Description = c.Description ?? string.Empty,
                ReportedOn = c.ReportedOn,
                SlaDueAt = c.SlaDueAt,
                SlaBreached = c.SlaDueAt is not null && c.SlaDueAt < now
                              && c.Status is not (TicketStatus.Resolved or TicketStatus.Closed),
                WorkOrderId = c.WorkOrderId,
                CostBearer = c.CostBearer,
                Cost = c.Cost,
                IsRejected = c.IsRejected,
                RejectionReason = c.RejectionReason,
                ResolvedOn = c.ResolvedOn,
                CustomerRating = c.CustomerRating,
                IsInsideLiabilityPeriod = c.DefectLiabilityId is not null
                    && liabilities.TryGetValue(c.DefectLiabilityId.Value, out var expires)
                    && expires >= today,
            };
        }).ToList();
    }

    // ═══ NOCs ════════════════════════════════════════════════════════════════

    /// <summary>
    /// A no-objection certificate. Gated on dues, carries its conditions, and prints a
    /// verification code so a third party — a bank, a registrar, a utility — can confirm it is
    /// genuine without ringing the office.
    /// </summary>
    public async Task<NocIssuanceDto> RequestNocAsync(NocRequestDto dto, Guid userId)
    {
        var today = Today;

        var clearance = await IssueDuesClearanceAsync(dto.BookingId, dto.UnitId, dto.PropertyId, dto.PartyId, userId);

        var noc = new NocIssuance
        {
            NocNumber = await numbering.NextNocNumberAsync(DateTime.UtcNow),
            Kind = dto.Kind,
            Status = clearance.IsClear ? NocStatus.Approved : NocStatus.DuesCheckPending,
            ProjectId = dto.ProjectId,
            SocietyId = dto.SocietyId,
            UnitId = dto.UnitId,
            PropertyId = dto.PropertyId,
            BookingId = dto.BookingId,
            PartyId = dto.PartyId,
            TransferRequestId = dto.TransferRequestId,
            BuildingPlanApplicationId = dto.BuildingPlanApplicationId,
            CustomerMortgageId = dto.CustomerMortgageId,
            RequestedOn = today,
            DuesClearanceId = clearance.Id,
            DuesCleared = clearance.IsClear,
            OutstandingAtIssue = clearance.TotalOutstanding,
            Fee = dto.Fee ?? 0m,
            AddressedTo = dto.AddressedTo,
            Purpose = dto.Purpose,
        }.StampNew(Tenant, userId);

        Db.NocIssuances.Add(noc);

        var order = 0;

        foreach (var c in dto.Conditions)
        {
            noc.Conditions.Add(new NocCondition
            {
                Condition = c.Condition,
                ComplyByDate = c.ComplyByDate,
                BreachRevokesNoc = c.BreachRevokesNoc,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();

        // The transfer's NOC gate reads this, so linking it here saves a step on the desk.
        if (dto.TransferRequestId is not null)
        {
            var transfer = await Db.TransferRequests.ForCompany(Tenant)
                .FirstOrDefaultAsync(t => t.Id == dto.TransferRequestId);

            if (transfer is not null)
            {
                transfer.NocIssuanceId = noc.Id;
                transfer.StampUpdated(userId);
                await Db.SaveChangesAsync();
            }
        }

        return (await GetNocAsync(noc.Id))!;
    }

    public async Task<NocIssuanceDto> IssueNocAsync(Guid id, Guid userId)
    {
        var noc = await Db.NocIssuances.ForCompany(Tenant)
            .Include(n => n.Conditions)
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new InvalidOperationException("That NOC does not exist.");

        if (noc.Status == NocStatus.Issued)
            throw new InvalidOperationException($"{noc.NocNumber} was already issued on {noc.IssuedOn:dd MMM yyyy}.");

        if (noc.IsRevoked)
            throw new InvalidOperationException($"{noc.NocNumber} was revoked and cannot be re-issued.");

        var settings = await SettingsAsync();

        if (!noc.DuesCleared && settings.BlockTransferOnDues && noc.DuesOverrideApprovalId is null)
            throw new InvalidOperationException($"{noc.OutstandingAtIssue:N0} is still outstanding. Clear it or record a dues override.");

        if (noc.Fee > 0m && !noc.FeePaid)
            throw new InvalidOperationException($"The NOC fee of {noc.Fee:N0} has not been received.");

        var today = Today;

        noc.Status = NocStatus.Issued;
        noc.IssuedOn = today;
        noc.ValidUntil ??= today.AddDays(90);
        noc.IssuedByUserId = userId;

        // Short, unambiguous, and unguessable enough for its purpose. Not a secret — it only ever
        // confirms a document we already issued.
        noc.VerificationCode = $"{noc.NocNumber}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        noc.StampUpdated(userId);

        await Db.SaveChangesAsync();

        await QueueNotificationAsync(
            "NocIssued",
            $"{noc.Kind} NOC issued",
            $"{noc.NocNumber}, valid to {noc.ValidUntil:dd MMM yyyy}.",
            $"/realestate/nocs/{noc.Id}",
            recipientPartyId: noc.PartyId,
            entityType: "NocIssuance",
            entityId: noc.Id);

        await Db.SaveChangesAsync();
        return (await GetNocAsync(id))!;
    }

    public async Task<NocIssuanceDto> RevokeNocAsync(Guid id, string reason, Guid userId)
    {
        var noc = await RequireAsync<NocIssuance>(id, "That NOC does not exist.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Revoking an NOC has to say why.");

        noc.IsRevoked = true;
        noc.RevokedOn = Today;
        noc.RevocationReason = reason;
        noc.Status = NocStatus.Revoked;
        noc.StampUpdated(userId);

        // A revoked NOC no longer satisfies the transfer gate. Anything relying on it stops.
        if (noc.TransferRequestId is not null)
        {
            var transfer = await Db.TransferRequests.ForCompany(Tenant)
                .FirstOrDefaultAsync(t => t.Id == noc.TransferRequestId);

            if (transfer is not null && transfer.Status != TransferStatus.Completed)
            {
                transfer.Status = TransferStatus.DocumentsPending;
                transfer.StampUpdated(userId);
            }
        }

        await WriteAuditNoteAsync(
            "NocIssuance", id, "NocRevoked", Guid.Empty, userId,
            note: reason, entityReference: noc.NocNumber, highRisk: true);

        await Db.SaveChangesAsync();
        return (await GetNocAsync(id))!;
    }

    public async Task<PaginatedResponse<NocIssuanceDto>> GetNocsAsync(ListQueryDto query, NocKind? kind, NocStatus? status)
    {
        var q = Db.NocIssuances.ForCompany(Tenant)
            .Include(n => n.Conditions)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), n => n.NocNumber.Contains(query.Search!))
            .WhereIf(kind.HasValue, n => n.Kind == kind)
            .WhereIf(status.HasValue, n => n.Status == status)
            .WhereIf(query.ProjectId.HasValue, n => n.ProjectId == query.ProjectId)
            .OrderByDescending(n => n.RequestedOn);

        return await PageAsync(q, query, MapNocsAsync);
    }

    private async Task<NocIssuanceDto?> GetNocAsync(Guid id)
    {
        var noc = await Db.NocIssuances.ForCompany(Tenant)
            .Include(n => n.Conditions)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (noc is null) return null;
        return (await MapNocsAsync([noc]))[0];
    }

    /// <summary>
    /// Confirms a certificate from its printed code. Deliberately anonymous — a bank checking a
    /// customer's NOC should not need an account with us.
    /// </summary>
    public async Task<NocIssuanceDto?> VerifyNocAsync(string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(verificationCode)) return null;

        var noc = await Db.NocIssuances.ForCompany(Tenant)
            .Include(n => n.Conditions)
            .FirstOrDefaultAsync(n => n.VerificationCode == verificationCode.Trim());

        if (noc is null) return null;

        var dto = (await MapNocsAsync([noc]))[0];

        // A verifier is told plainly whether it still stands. Silence would be misread as "valid".
        if (noc.IsRevoked) dto.RejectionReason = $"This certificate was revoked on {noc.RevokedOn:dd MMM yyyy}.";
        else if (dto.IsExpired) dto.RejectionReason = $"This certificate expired on {noc.ValidUntil:dd MMM yyyy}.";

        return dto;
    }

    private async Task<List<NocIssuanceDto>> MapNocsAsync(List<NocIssuance> nocs)
    {
        if (nocs.Count == 0) return [];

        var today = Today;
        var parties = await PartyNamesAsync(nocs.Select(n => n.PartyId));
        var projects = await ProjectNamesAsync(nocs.Select(n => n.ProjectId));
        var issuers = await AgentUserNamesAsync(nocs.Select(n => n.IssuedByUserId));

        var unitIds = nocs.Where(n => n.UnitId.HasValue).Select(n => n.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        return nocs.Select(n => new NocIssuanceDto
        {
            Id = n.Id,
            NocNumber = n.NocNumber,
            Kind = n.Kind,
            Status = n.Status,
            ProjectId = n.ProjectId,
            ProjectName = n.ProjectId is null ? null : projects.GetValueOrDefault(n.ProjectId.Value),
            UnitId = n.UnitId,
            UnitNumber = n.UnitId is null ? null : units.GetValueOrDefault(n.UnitId.Value),
            PartyId = n.PartyId,
            PartyName = parties.GetValueOrDefault(n.PartyId, "—"),
            RequestedOn = n.RequestedOn,
            IssuedOn = n.IssuedOn,
            ValidUntil = n.ValidUntil,
            IsExpired = n.ValidUntil is not null && n.ValidUntil < today,
            DuesCleared = n.DuesCleared,
            OutstandingAtIssue = n.OutstandingAtIssue,
            Fee = n.Fee,
            FeePaid = n.FeePaid,
            IssuedByName = n.IssuedByUserId is null ? null : issuers.GetValueOrDefault(n.IssuedByUserId.Value),
            DocumentUrl = n.DocumentUrl,
            VerificationCode = n.VerificationCode,
            AddressedTo = n.AddressedTo,
            Purpose = n.Purpose,
            RejectionReason = n.RejectionReason,
            IsRevoked = n.IsRevoked,
            RevocationReason = n.RevocationReason,

            Conditions = n.Conditions.OrderBy(c => c.SortOrder).Select(c => new NocConditionDto
            {
                Id = c.Id,
                Condition = c.Condition,
                ComplyByDate = c.ComplyByDate,
                IsSatisfied = c.IsSatisfied,
                SatisfiedOn = c.SatisfiedOn,
                BreachRevokesNoc = c.BreachRevokesNoc,
                SortOrder = c.SortOrder,
            }).ToList(),
        }).ToList();
    }
}
