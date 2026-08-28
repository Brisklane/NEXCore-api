using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Possession, handover, snagging, the defect liability period, and NOCs.
///
/// Handover is the hinge of the whole customer relationship: it is where the money stops and the
/// warranty starts. So it is gated on a checklist that can only be bypassed in writing, an open
/// critical snag blocks it outright, and completing it is what starts the defect liability clock —
/// nothing else does, because a warranty that starts on a typed-in date is a warranty nobody
/// can enforce.
/// </summary>
public partial class ExitService
{
    // ═══ Possession ══════════════════════════════════════════════════════════

    /// <summary>
    /// Works out whether a unit can be offered, and what is left to pay. Read-only — it never
    /// creates the offer, so a sales desk can check twenty bookings without writing anything.
    /// </summary>
    public async Task<PossessionOfferDto> EvaluatePossessionAsync(Guid bookingId)
    {
        var booking = await RequireAsync<Booking>(bookingId, "That booking does not exist.");

        var existing = await Db.PossessionOffers.ForCompany(Tenant)
            .Include(o => o.Checklist)
            .FirstOrDefaultAsync(o => o.BookingId == bookingId);

        var offer = existing ?? new PossessionOffer
        {
            BookingId = bookingId,
            UnitId = booking.UnitId,
            PartyId = booking.PrimaryApplicantPartyId,
            Status = PossessionStatus.NotEligible,
        };

        await ComputeDuesAtPossessionAsync(offer, booking);
        var checklist = await BuildPossessionChecklistAsync(offer, booking);

        var dto = await MapPossessionAsync(offer, booking, checklist);
        return dto;
    }

    private async Task ComputeDuesAtPossessionAsync(PossessionOffer offer, Booking booking)
    {
        offer.BalanceDue = Math.Max(0m, booking.Outstanding);

        var charges = await Db.BookingChargeLines.ForCompany(Tenant)
            .Where(c => c.BookingId == booking.Id && c.IsAccepted && !c.IsPartOfSalePrice)
            .Select(c => new { c.Kind, Outstanding = c.Amount + c.TaxAmount })
            .ToListAsync();

        // These are the four that habitually appear on a possession letter and habitually get
        // forgotten until the customer is standing at the door.
        offer.PossessionChargesDue = RealEstateMapper.Money(
            charges.Where(c => c.Kind == ChargeKind.PossessionCharge).Sum(c => c.Outstanding));

        offer.MaintenanceAdvanceDue = RealEstateMapper.Money(
            charges.Where(c => c.Kind == ChargeKind.MaintenanceAdvance).Sum(c => c.Outstanding));

        offer.CorpusFundDue = RealEstateMapper.Money(
            charges.Where(c => c.Kind == ChargeKind.CorpusFund).Sum(c => c.Outstanding));

        offer.UtilityDepositsDue = RealEstateMapper.Money(
            charges.Where(c => c.Kind == ChargeKind.UtilityConnection).Sum(c => c.Outstanding));

        offer.TotalDueAtPossession = RealEstateMapper.Money(
            offer.BalanceDue + offer.PossessionChargesDue + offer.MaintenanceAdvanceDue
            + offer.CorpusFundDue + offer.UtilityDepositsDue);
    }

    private async Task<List<PossessionChecklistItem>> BuildPossessionChecklistAsync(PossessionOffer offer, Booking booking)
    {
        var today = Today;
        var items = offer.Checklist.ToList();
        var order = 0;

        PossessionChecklistItem Item(string key, string label, bool satisfied, bool mandatory = true, string? note = null)
        {
            var item = items.FirstOrDefault(i => i.CheckKey == key)
                ?? new PossessionChecklistItem { PossessionOfferId = offer.Id, CheckKey = key, IsMandatory = mandatory };

            item.Label = label;

            // An override already granted stands. Recomputing must never quietly revoke a
            // decision somebody signed for.
            if (item.OverrideApprovalRequestId is null)
            {
                item.IsSatisfied = satisfied;
                if (satisfied && item.SatisfiedOn is null) item.SatisfiedOn = today;
            }

            item.Note = note;
            item.SortOrder = order += 10;

            if (!items.Contains(item)) items.Add(item);
            return item;
        }

        Item("DuesCleared", "All dues cleared", offer.TotalDueAtPossession <= 0m,
            note: offer.TotalDueAtPossession > 0m ? $"{offer.TotalDueAtPossession:N0} outstanding." : null);

        Item("KycComplete", "KYC verified for the buyer",
            await Db.KycCases.ForCompany(Tenant)
                .AnyAsync(k => k.PartyId == booking.PrimaryApplicantPartyId
                            && (k.Status == KycStatus.Verified)));

        Item("AgreementRegistered", "Sale agreement signed", booking.AgreementSignedOn is not null);

        var occupancy = await Db.ApprovalRecords.ForCompany(Tenant)
            .AnyAsync(a => a.ProjectId == booking.ProjectId
                        && a.Kind == ApprovalKind.OccupancyCertificate
                        && a.State == ApprovalState.Granted);

        Item("OccupancyCertificate", "Occupancy certificate granted for the building", occupancy,
            note: occupancy ? null : "The building cannot be lawfully occupied without it.");

        var milestonesLeft = await Db.ProjectMilestones.ForCompany(Tenant)
            .CountAsync(m => m.ProjectId == booking.ProjectId && m.Status != MilestoneStatus.Certified);

        Item("UnitReady", "Construction complete and certified", milestonesLeft == 0,
            note: milestonesLeft > 0 ? $"{milestonesLeft} milestones are not yet certified." : null);

        var criticalSnags = booking.UnitId is null
            ? 0
            : await Db.Snags.ForCompany(Tenant)
                .Join(Db.SnagInspections.ForCompany(Tenant), s => s.SnagInspectionId, i => i.Id, (s, i) => new { s, i })
                .CountAsync(x => x.i.UnitId == booking.UnitId
                              && x.s.Severity == SnagSeverity.Critical
                              && x.s.Status != SnagStatus.Verified
                              && x.s.Status != SnagStatus.Rejected);

        Item("SnagsClosed", "No critical snags outstanding", criticalSnags == 0,
            note: criticalSnags > 0 ? $"{criticalSnags} critical snags are still open." : null);

        var meters = booking.UnitId is null
            ? 0
            : await Db.Meters.ForCompany(Tenant).CountAsync(m => m.UnitId == booking.UnitId);

        Item("UtilitiesConnected", "Utilities connected and metered", meters > 0, mandatory: false,
            note: meters == 0 ? "No meters are registered against this unit." : null);

        Item("DocumentsComplete", "Handover pack prepared", true, mandatory: false);

        offer.Checklist = items;
        return items;
    }

    private async Task<PossessionOfferDto> MapPossessionAsync(
        PossessionOffer offer, Booking booking, List<PossessionChecklistItem> checklist)
    {
        var names = await PartyNamesAsync([offer.PartyId]);
        var projects = await ProjectNamesAsync([(Guid?)booking.ProjectId]);

        var unitNumber = offer.UnitId is null
            ? null
            : await Db.Units.ForCompany(Tenant).Where(u => u.Id == offer.UnitId).Select(u => u.UnitNumber).FirstOrDefaultAsync();

        var phone = await Db.Parties.ForCompany(Tenant)
            .Where(p => p.Id == offer.PartyId)
            .Select(p => p.PrimaryPhone)
            .FirstOrDefaultAsync();

        var criticalSnags = checklist.FirstOrDefault(c => c.CheckKey == "SnagsClosed");

        var mandatoryUnmet = checklist.Count(c => c.IsMandatory && !c.IsSatisfied);

        return new PossessionOfferDto
        {
            Id = offer.Id,
            Reference = offer.Reference,
            BookingId = offer.BookingId,
            BookingReference = booking.Reference,
            UnitId = offer.UnitId,
            UnitNumber = unitNumber,
            ProjectName = projects.GetValueOrDefault(booking.ProjectId, "—"),
            PartyId = offer.PartyId,
            PartyName = names.GetValueOrDefault(offer.PartyId, "—"),
            PartyPhone = phone,

            Status = offer.Status,
            OfferedOn = offer.OfferedOn,
            WindowFrom = offer.WindowFrom,
            WindowTo = offer.WindowTo,
            AppointmentOn = offer.AppointmentOn,

            BalanceDue = offer.BalanceDue,
            PossessionChargesDue = offer.PossessionChargesDue,
            MaintenanceAdvanceDue = offer.MaintenanceAdvanceDue,
            CorpusFundDue = offer.CorpusFundDue,
            UtilityDepositsDue = offer.UtilityDepositsDue,
            TotalDueAtPossession = offer.TotalDueAtPossession,
            CurrencyCode = booking.CurrencyCode ?? await CurrencyAsync(),

            DocumentUrl = offer.DocumentUrl,
            HandoverId = offer.HandoverId,
            CustomerDeclined = offer.CustomerDeclined,
            DeclineReason = offer.DeclineReason,
            DelayDays = offer.DelayDays,
            DelayCompensation = offer.DelayCompensation,

            IsEligible = mandatoryUnmet == 0,
            OpenCriticalSnags = criticalSnags?.IsSatisfied == false ? 1 : 0,

            Checklist = checklist.OrderBy(c => c.SortOrder).Select(c => new ChecklistItemDto
            {
                Id = c.Id,
                Key = c.CheckKey,
                Label = c.Label,
                IsSatisfied = c.IsSatisfied,
                IsMandatory = c.IsMandatory,
                SatisfiedOn = c.SatisfiedOn,
                Note = c.Note,
                WasOverridden = c.OverrideApprovalRequestId is not null,
                OverrideReason = c.OverrideReason,
                SortOrder = c.SortOrder,
            }).ToList(),
        };
    }

    public async Task<PossessionOfferDto> OfferPossessionAsync(
        Guid bookingId, DateOnly windowFrom, DateOnly windowTo, Guid? templateId, Guid userId)
    {
        var booking = await RequireAsync<Booking>(bookingId, "That booking does not exist.");

        if (windowTo < windowFrom)
            throw new InvalidOperationException("The possession window has to end after it starts.");

        var offer = await Db.PossessionOffers.ForCompany(Tenant)
            .Include(o => o.Checklist)
            .FirstOrDefaultAsync(o => o.BookingId == bookingId);

        if (offer is null)
        {
            offer = new PossessionOffer
            {
                Reference = await numbering.NextPossessionNumberAsync(DateTime.UtcNow),
                BookingId = bookingId,
                UnitId = booking.UnitId,
                PartyId = booking.PrimaryApplicantPartyId,
            }.StampNew(Tenant, userId);

            Db.PossessionOffers.Add(offer);
        }
        else
        {
            if (offer.Status == PossessionStatus.HandedOver)
                throw new InvalidOperationException("This unit has already been handed over.");

            offer.StampUpdated(userId);
        }

        await ComputeDuesAtPossessionAsync(offer, booking);
        var checklist = await BuildPossessionChecklistAsync(offer, booking);

        // Construction has to be complete and the building lawfully occupiable. The rest can be
        // outstanding when the letter goes out — that is what the letter is for.
        var blocking = checklist
            .Where(c => c.CheckKey is "UnitReady" or "OccupancyCertificate" && !c.IsSatisfied)
            .Select(c => c.Note ?? c.Label)
            .ToList();

        if (blocking.Count > 0)
            throw new InvalidOperationException($"Possession cannot be offered yet: {string.Join(" ", blocking)}");

        var today = Today;

        offer.Status = PossessionStatus.Offered;
        offer.OfferedOn = today;
        offer.WindowFrom = windowFrom;
        offer.WindowTo = windowTo;

        // Delay against the promise, and what it costs. In several regimes this is a statutory
        // entitlement, and it accrues whether or not anybody computes it.
        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == booking.ProjectId);

        if (project?.PromisedPossessionDate is not null && windowFrom > project.PromisedPossessionDate)
        {
            offer.DelayDays = windowFrom.DayNumber - project.PromisedPossessionDate.Value.DayNumber;

            var settings = await SettingsAsync();

            // Simple interest on what the customer has paid, for the days of delay.
            offer.DelayCompensation = RealEstateMapper.Money(
                booking.TotalPaid * settings.DelayCompensationAnnualPercent / 100m * offer.DelayDays.Value / 365m);
        }

        foreach (var item in checklist.Where(i => i.Id == Guid.Empty))
        {
            item.PossessionOfferId = offer.Id;
            Db.PossessionChecklistItems.Add(item.StampNew(Tenant, userId));
        }

        booking.Status = BookingStatus.PossessionOffered;
        booking.StampUpdated(userId);

        // The letter always carries the amount, so the customer arrives able to pay it.
        if (offer.TotalDueAtPossession > 0m)
        {
            await QueueNotificationAsync(
                "PossessionOffered",
                "Your unit is ready for possession",
                $"Take possession between {windowFrom:dd MMM} and {windowTo:dd MMM yyyy}. {offer.TotalDueAtPossession:N0} is due at handover.",
                $"/realestate/possessions/{offer.Id}",
                recipientPartyId: offer.PartyId,
                entityType: "PossessionOffer",
                entityId: offer.Id);
        }
        else
        {
            await QueueNotificationAsync(
                "PossessionOffered",
                "Your unit is ready for possession",
                $"Take possession between {windowFrom:dd MMM} and {windowTo:dd MMM yyyy}. Nothing further is due.",
                $"/realestate/possessions/{offer.Id}",
                recipientPartyId: offer.PartyId,
                entityType: "PossessionOffer",
                entityId: offer.Id);
        }

        await Db.SaveChangesAsync();
        return await MapPossessionAsync(offer, booking, checklist);
    }

    public async Task<PossessionOfferDto> SetAppointmentAsync(Guid possessionOfferId, DateOnly on, Guid userId)
    {
        var offer = await Db.PossessionOffers.ForCompany(Tenant)
            .Include(o => o.Checklist)
            .FirstOrDefaultAsync(o => o.Id == possessionOfferId)
            ?? throw new InvalidOperationException("That possession offer does not exist.");

        if (offer.Status == PossessionStatus.HandedOver)
            throw new InvalidOperationException("This unit has already been handed over.");

        offer.AppointmentOn = on;
        offer.Status = PossessionStatus.AppointmentSet;
        offer.StampUpdated(userId);

        var booking = await RequireAsync<Booking>(offer.BookingId, "The booking behind this offer is missing.");

        await QueueNotificationAsync(
            "PossessionAppointment",
            "Handover appointment confirmed",
            $"{on:dddd d MMMM yyyy}. Bring your identity document and the outstanding amount of {offer.TotalDueAtPossession:N0}.",
            $"/realestate/possessions/{offer.Id}",
            recipientPartyId: offer.PartyId,
            entityType: "PossessionOffer",
            entityId: offer.Id);

        await Db.SaveChangesAsync();
        return await MapPossessionAsync(offer, booking, offer.Checklist.ToList());
    }

    public async Task<PossessionOfferDto> OverrideChecklistAsync(Guid checklistItemId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("An override has to say why.");

        var item = await Db.PossessionChecklistItems.ForCompany(Tenant)
            .FirstOrDefaultAsync(i => i.Id == checklistItemId)
            ?? throw new InvalidOperationException("That checklist item does not exist.");

        var offer = await Db.PossessionOffers.ForCompany(Tenant)
            .Include(o => o.Checklist)
            .FirstOrDefaultAsync(o => o.Id == item.PossessionOfferId)
            ?? throw new InvalidOperationException("The possession offer is missing.");

        // Some things cannot be waived by anybody. Handing over a unit the authority has not
        // certified for occupation is unlawful, not merely risky.
        if (item.CheckKey == "OccupancyCertificate")
            throw new InvalidOperationException("The occupancy certificate cannot be overridden. The building may not be lawfully occupied without it.");

        var approval = await RaiseApprovalAsync(
            "PossessionChecklistOverride", item.Id, offer.Reference, offer.TotalDueAtPossession,
            $"Waive \"{item.Label}\" on {offer.Reference}. {reason}", userId, note: reason);

        item.OverrideApprovalRequestId = approval?.Id ?? Guid.Empty;
        item.OverrideReason = reason;
        item.IsSatisfied = approval is null;
        item.SatisfiedOn = approval is null ? Today : null;
        item.StampUpdated(userId);

        await WriteAuditNoteAsync(
            "PossessionOffer", offer.Id, "PossessionChecklistOverride", Guid.Empty, userId,
            before: item.Label, note: reason,
            approvalRequestId: approval?.Id,
            entityReference: offer.Reference,
            highRisk: true);

        await Db.SaveChangesAsync();

        var booking = await RequireAsync<Booking>(offer.BookingId, "The booking behind this offer is missing.");
        return await MapPossessionAsync(offer, booking, offer.Checklist.ToList());
    }

    public async Task<PaginatedResponse<PossessionOfferDto>> GetPossessionsAsync(
        ListQueryDto query, PossessionStatus? status, Guid? projectId)
    {
        var bookingIds = projectId is null
            ? null
            : Db.Bookings.ForCompany(Tenant).Where(b => b.ProjectId == projectId).Select(b => b.Id);

        var q = Db.PossessionOffers.ForCompany(Tenant)
            .Include(o => o.Checklist)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), o => o.Reference.Contains(query.Search!))
            .WhereIf(status.HasValue, o => o.Status == status)
            .WhereIf(bookingIds is not null, o => bookingIds!.Contains(o.BookingId))
            .OrderByDescending(o => o.OfferedOn);

        return await PageAsync(q, query, async offers =>
        {
            if (offers.Count == 0) return [];

            var ids = offers.Select(o => o.BookingId).Distinct().ToList();

            var bookings = await Db.Bookings.ForCompany(Tenant)
                .Where(b => ids.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b);

            var result = new List<PossessionOfferDto>();

            foreach (var offer in offers)
            {
                if (!bookings.TryGetValue(offer.BookingId, out var booking)) continue;
                result.Add(await MapPossessionAsync(offer, booking, offer.Checklist.ToList()));
            }

            return result;
        });
    }

    // ═══ Handover ════════════════════════════════════════════════════════════

    /// <summary>
    /// The handover itself. One transaction: the pack is recorded, the unit changes status, the
    /// possession offer closes and the defect liability periods start. Splitting these would
    /// leave a unit handed over with no warranty, which is the worst of both worlds.
    /// </summary>
    public async Task<HandoverDto> CompleteHandoverAsync(HandoverDto dto, Guid userId)
    {
        var offer = dto.BookingId is null
            ? null
            : await Db.PossessionOffers.ForCompany(Tenant)
                .Include(o => o.Checklist)
                .FirstOrDefaultAsync(o => o.BookingId == dto.BookingId);

        if (offer is not null)
        {
            var unmet = offer.Checklist.Where(c => c.IsMandatory && !c.IsSatisfied).Select(c => c.Note ?? c.Label).ToList();

            if (unmet.Count > 0)
                throw new InvalidOperationException($"Handover is blocked: {string.Join("; ", unmet)}.");
        }

        // An open critical snag blocks handover regardless of what the checklist says. This is the
        // one rule the site and the sales desk are most likely to disagree about.
        if (dto.SnagInspectionId is not null)
        {
            var critical = await Db.Snags.ForCompany(Tenant)
                .CountAsync(s => s.SnagInspectionId == dto.SnagInspectionId
                              && s.Severity == SnagSeverity.Critical
                              && s.Status != SnagStatus.Verified
                              && s.Status != SnagStatus.Rejected);

            if (critical > 0)
                throw new InvalidOperationException($"{critical} critical snags are still open on the inspection. Close them before handover.");
        }

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        var today = Today;

        var handover = new Handover
        {
            Reference = await numbering.NextHandoverNumberAsync(now),
            BookingId = dto.BookingId,
            UnitId = dto.UnitId ?? offer?.UnitId,
            ClientBuildContractId = dto.ClientBuildContractId,
            PartyId = dto.PartyId,
            HandedOverAt = dto.HandedOverAt == default ? now : dto.HandedOverAt,
            HandedOverByUserId = userId,
            SnagInspectionId = dto.SnagInspectionId,
            CertificateUrl = dto.CertificateUrl,
            CustomerSignatureUrl = dto.CustomerSignatureUrl,

            // The clock starts here and nowhere else.
            DefectLiabilityStartsOn = today,
            IsCompleted = true,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        Db.Handovers.Add(handover);

        var order = 0;

        foreach (var item in dto.Items)
        {
            handover.Items.Add(new HandoverItem
            {
                ItemType = item.ItemType,
                Label = item.Label,
                Detail = item.Detail,
                Quantity = item.Quantity,
                MeterId = item.MeterId,
                ReadingValue = item.ReadingValue,
                DocumentUrl = item.DocumentUrl,
                IsHandedOver = item.IsHandedOver,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));

            // A meter reading handed across is also the opening reading for the new occupier's
            // first bill. Recording it in one place and not the other causes the first dispute.
            if (item.MeterId is not null && item.ReadingValue is not null)
            {
                Db.MeterReadings.Add(new MeterReading
                {
                    MeterId = item.MeterId.Value,
                    ReadingDate = today,
                    ReadingValue = item.ReadingValue.Value,
                    Source = ReadingSource.PhotoVerified,
                    Note = "Opening reading taken at handover.",
                }.StampNew(Tenant, userId));
            }
        }

        if (offer is not null)
        {
            offer.Status = PossessionStatus.HandedOver;
            offer.HandoverId = handover.Id;
            offer.StampUpdated(userId);
        }

        if (dto.BookingId is not null)
        {
            var booking = await RequireAsync<Booking>(dto.BookingId.Value, "The booking behind this handover is missing.");

            booking.Status = BookingStatus.Possessed;
            booking.PossessionTakenOn = today;
            booking.StampUpdated(userId);

            if (booking.UnitId is not null)
            {
                var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == booking.UnitId);

                if (unit is not null)
                {
                    unit.Status = PropertyStatus.Possessed;
                    unit.StampUpdated(userId);

                    var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == unit.PropertyId);

                    if (property is not null)
                    {
                        property.Status = PropertyStatus.Possessed;
                        property.Occupancy = OccupancyState.OwnerOccupied;
                        property.StampUpdated(userId);
                    }
                }
            }
        }

        await CreateDefectLiabilitiesAsync(handover, today, userId);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        await QueueNotificationAsync(
            "HandoverComplete",
            "Possession handed over",
            $"Your warranty period runs from {today:dd MMM yyyy}. Report any defect through the resident portal.",
            $"/realestate/handovers/{handover.Id}",
            recipientPartyId: handover.PartyId,
            entityType: "Handover",
            entityId: handover.Id);

        await Db.SaveChangesAsync();
        return (await GetHandoverAsync(handover.Id))!;
    }

    /// <summary>
    /// Opens a liability period per category. Structure is covered for years and a paint blemish
    /// for months; a single duration would be generous on one and indefensible on the other.
    /// </summary>
    private async Task CreateDefectLiabilitiesAsync(Handover handover, DateOnly start, Guid userId)
    {
        var settings = await SettingsAsync();

        var durations = new (DefectCategory Category, int Months)[]
        {
            (DefectCategory.Structural, settings.StructuralWarrantyMonths),
            (DefectCategory.Waterproofing, 60),
            (DefectCategory.Services, 24),
            (DefectCategory.Equipment, 12),
            (DefectCategory.Workmanship, settings.DefectLiabilityMonths),
            (DefectCategory.Finishes, 12),
        };

        var projectId = handover.UnitId is null
            ? null
            : await Db.Units.ForCompany(Tenant)
                .Where(u => u.Id == handover.UnitId)
                .Select(u => (Guid?)u.ProjectId)
                .FirstOrDefaultAsync();

        foreach (var (category, months) in durations)
        {
            var duration = Math.Max(1, months);

            Db.DefectLiabilities.Add(new DefectLiability
            {
                UnitId = handover.UnitId,
                ProjectId = projectId,
                HandoverId = handover.Id,
                ClientBuildContractId = handover.ClientBuildContractId,
                Category = category,
                StartsOn = start,
                DurationMonths = duration,
                ExpiresOn = start.AddMonths(duration),
                LiableParty = CostBearer.Developer,
            }.StampNew(Tenant, userId));
        }
    }

    public async Task<HandoverDto?> GetHandoverAsync(Guid id)
    {
        var handover = await Db.Handovers.ForCompany(Tenant)
            .Include(h => h.Items)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (handover is null) return null;

        var names = await PartyNamesAsync([handover.PartyId]);
        var users = await AgentUserNamesAsync([handover.HandedOverByUserId]);

        var unitNumber = handover.UnitId is null
            ? null
            : await Db.Units.ForCompany(Tenant).Where(u => u.Id == handover.UnitId).Select(u => u.UnitNumber).FirstOrDefaultAsync();

        var meterIds = handover.Items.Where(i => i.MeterId.HasValue).Select(i => i.MeterId!.Value).Distinct().ToList();

        var meters = meterIds.Count == 0
            ? []
            : await Db.Meters.ForCompany(Tenant).Where(m => meterIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.MeterNumber);

        return new HandoverDto
        {
            Id = handover.Id,
            Reference = handover.Reference,
            BookingId = handover.BookingId,
            UnitId = handover.UnitId,
            UnitNumber = unitNumber,
            ClientBuildContractId = handover.ClientBuildContractId,
            PartyId = handover.PartyId,
            PartyName = names.GetValueOrDefault(handover.PartyId, "—"),
            HandedOverAt = handover.HandedOverAt,
            HandedOverByName = handover.HandedOverByUserId is null ? null : users.GetValueOrDefault(handover.HandedOverByUserId.Value),
            SnagInspectionId = handover.SnagInspectionId,
            CertificateUrl = handover.CertificateUrl,
            CustomerSignatureUrl = handover.CustomerSignatureUrl,
            DefectLiabilityStartsOn = handover.DefectLiabilityStartsOn,
            IsCompleted = handover.IsCompleted,
            Note = handover.Note,

            Items = handover.Items.OrderBy(i => i.SortOrder).Select(i => new HandoverItemDto
            {
                Id = i.Id,
                ItemType = i.ItemType,
                Label = i.Label,
                Detail = i.Detail,
                Quantity = i.Quantity,
                MeterId = i.MeterId,
                MeterNumber = i.MeterId is null ? null : meters.GetValueOrDefault(i.MeterId.Value),
                ReadingValue = i.ReadingValue,
                DocumentUrl = i.DocumentUrl,
                IsHandedOver = i.IsHandedOver,
                SortOrder = i.SortOrder,
            }).ToList(),

            Liabilities = await GetLiabilitiesAsync(handover.UnitId, null, true),
        };
    }
}
