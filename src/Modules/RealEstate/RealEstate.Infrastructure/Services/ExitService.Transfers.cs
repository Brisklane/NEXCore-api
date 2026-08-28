using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Transfer: a booking changing hands.
///
/// This is the defining transaction of the plot market, and it is modelled as the four-gate
/// process it actually is — dues, NOC, fees, and a witnessed session — because that is how a
/// transfer desk works and because skipping a gate is how a plot gets sold twice. Each gate
/// reports its own state, says what is blocking it, and says who could override it. The ownership
/// chain entry written at completion is append-only: it is the record that has to stand up
/// twenty years and one court case later.
/// </summary>
public partial class ExitService
{
    public async Task<PaginatedResponse<TransferRequestListItemDto>> GetTransfersAsync(
        ListQueryDto query, TransferStatus? status, Guid? projectId)
    {
        var q = Db.TransferRequests.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), t => t.Reference.Contains(query.Search!))
            .WhereIf(status.HasValue, t => t.Status == status)
            .WhereIf(projectId.HasValue, t => t.ProjectId == projectId)
            .WhereIf(query.FromDate.HasValue, t => t.RequestedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, t => t.RequestedOn <= query.ToDate)
            .OrderByDescending(t => t.RequestedOn);

        return await PageAsync(q, query, MapTransferListAsync);
    }

    private async Task<List<TransferRequestListItemDto>> MapTransferListAsync(List<TransferRequest> transfers)
    {
        if (transfers.Count == 0) return [];

        var today = Today;
        var currency = await CurrencyAsync();
        var ids = transfers.Select(t => t.Id).ToList();

        var projects = await ProjectNamesAsync(transfers.Select(t => (Guid?)t.ProjectId));

        var unitIds = transfers.Where(t => t.UnitId.HasValue).Select(t => t.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var fileIds = transfers.Where(t => t.PlotFileId.HasValue).Select(t => t.PlotFileId!.Value).Distinct().ToList();

        var files = fileIds.Count == 0
            ? []
            : await Db.PlotFiles.ForCompany(Tenant).Where(f => fileIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.FileNumber);

        var bookingIds = transfers.Where(t => t.BookingId.HasValue).Select(t => t.BookingId!.Value).Distinct().ToList();

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant).Where(b => bookingIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Reference);

        var parties = await Db.TransferParties.ForCompany(Tenant)
            .Where(p => ids.Contains(p.TransferRequestId))
            .Select(p => new { p.TransferRequestId, p.PartyId, p.Side })
            .ToListAsync();

        var names = await PartyNamesAsync(parties.Select(p => p.PartyId));
        var handlers = await AgentUserNamesAsync(transfers.Select(t => t.HandledByUserId));

        var sessions = await Db.TransferSessions.ForCompany(Tenant)
            .Where(s => ids.Contains(s.TransferRequestId))
            .Select(s => new { s.TransferRequestId, s.ScheduledAt })
            .ToListAsync();

        var nocIds = transfers.Where(t => t.NocIssuanceId.HasValue).Select(t => t.NocIssuanceId!.Value).Distinct().ToList();

        var nocs = nocIds.Count == 0
            ? []
            : await Db.NocIssuances.ForCompany(Tenant)
                .Where(n => nocIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Status);

        return transfers.Select(t =>
        {
            var mine = parties.Where(p => p.TransferRequestId == t.Id).ToList();

            var transferor = mine.FirstOrDefault(p => p.Side == "Transferor");
            var transferee = mine.FirstOrDefault(p => p.Side == "Transferee");

            return new TransferRequestListItemDto
            {
                Id = t.Id,
                Reference = t.Reference,
                Kind = t.Kind,
                Status = t.Status,
                RequestedOn = t.RequestedOn,
                ProjectId = t.ProjectId,
                ProjectName = projects.GetValueOrDefault(t.ProjectId, "—"),
                UnitNumber = t.UnitId is null ? null : units.GetValueOrDefault(t.UnitId.Value),
                FileNumber = t.PlotFileId is null ? null : files.GetValueOrDefault(t.PlotFileId.Value),
                BookingId = t.BookingId,
                BookingReference = t.BookingId is null ? null : bookings.GetValueOrDefault(t.BookingId.Value),
                TransferorName = transferor is null ? "—" : names.GetValueOrDefault(transferor.PartyId, "—"),
                TransfereeName = transferee is null ? null : names.GetValueOrDefault(transferee.PartyId),
                SaleConsideration = t.SaleConsideration,
                CurrencyCode = currency,
                OutstandingAtRequest = t.OutstandingAtRequest,
                DuesCleared = t.DuesCleared,
                NocIssued = t.NocIssuanceId is not null && nocs.GetValueOrDefault(t.NocIssuanceId.Value) == NocStatus.Issued,
                TotalTransferFee = t.TotalTransferFee,
                FeesPaid = t.FeesPaid,
                FeesCleared = t.FeesPaid >= t.TotalTransferFee,
                SessionScheduledAt = sessions.FirstOrDefault(s => s.TransferRequestId == t.Id)?.ScheduledAt,
                CompletedOn = t.CompletedOn,
                BlockedByLitigation = t.BlockedByLitigation,
                HandledByName = t.HandledByUserId is null ? null : handlers.GetValueOrDefault(t.HandledByUserId.Value),
                DaysOpen = (t.CompletedOn ?? today).DayNumber - t.RequestedOn.DayNumber,
            };
        }).ToList();
    }

    public async Task<TransferRequestDetailDto?> GetTransferAsync(Guid id)
    {
        var transfer = await Db.TransferRequests.ForCompany(Tenant)
            .Include(t => t.Parties)
            .Include(t => t.FeeLines)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transfer is null) return null;

        var head = (await MapTransferListAsync([transfer]))[0];

        var detail = new TransferRequestDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            Kind = head.Kind,
            Status = head.Status,
            RequestedOn = head.RequestedOn,
            ProjectId = head.ProjectId,
            ProjectName = head.ProjectName,
            UnitNumber = head.UnitNumber,
            FileNumber = head.FileNumber,
            BookingId = head.BookingId,
            BookingReference = head.BookingReference,
            TransferorName = head.TransferorName,
            TransfereeName = head.TransfereeName,
            SaleConsideration = head.SaleConsideration,
            CurrencyCode = head.CurrencyCode,
            OutstandingAtRequest = head.OutstandingAtRequest,
            DuesCleared = head.DuesCleared,
            NocIssued = head.NocIssued,
            TotalTransferFee = head.TotalTransferFee,
            FeesPaid = head.FeesPaid,
            FeesCleared = head.FeesCleared,
            SessionScheduledAt = head.SessionScheduledAt,
            CompletedOn = head.CompletedOn,
            BlockedByLitigation = head.BlockedByLitigation,
            HandledByName = head.HandledByName,
            DaysOpen = head.DaysOpen,

            UnitId = transfer.UnitId,
            PlotFileId = transfer.PlotFileId,
            PropertyId = transfer.PropertyId,
            RequestedByPartyId = transfer.RequestedByPartyId,
            DuesClearanceId = transfer.DuesClearanceId,
            NocIssuanceId = transfer.NocIssuanceId,
            FeeReceiptId = transfer.FeeReceiptId,
            NewBookingId = transfer.NewBookingId,
            NewAllotmentId = transfer.NewAllotmentId,
            SuccessionCertificateNumber = transfer.SuccessionCertificateNumber,
            PowerOfAttorneyRelationshipId = transfer.PowerOfAttorneyRelationshipId,
            CourtDecreeReference = transfer.CourtDecreeReference,
            ShareTransferredPercent = transfer.ShareTransferredPercent,
            Note = transfer.Note,
        };

        var partyIds = transfer.Parties.Select(p => p.PartyId).ToList();

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => partyIds.Contains(p.Id))
            .ToListAsync();

        var identities = await Db.PartyIdentities.ForCompany(Tenant)
            .Where(i => partyIds.Contains(i.PartyId) && i.IsPrimary)
            .ToDictionaryAsync(i => i.PartyId, i => i.Number);

        detail.Parties = transfer.Parties.Select(p =>
        {
            var person = people.FirstOrDefault(x => x.Id == p.PartyId);

            return new TransferPartyDto
            {
                Id = p.Id,
                PartyId = p.PartyId,
                Name = person is null ? "—" : RealEstateMapper.DisplayName(person),
                FatherOrGuardianName = person?.FatherOrGuardianName,
                IdentityNumber = identities.GetValueOrDefault(p.PartyId),
                Phone = person?.PrimaryPhone,
                PhotoUrl = p.PhotoUrl ?? person?.PhotoUrl,
                Side = p.Side,
                SharePercent = p.SharePercent,
                KycStatus = p.KycStatus,
                IdentityVerified = p.IdentityVerified,
                IsPresent = p.IsPresent,
                SignatureUrl = p.SignatureUrl,
                ThumbImpressionUrl = p.ThumbImpressionUrl,
            };
        }).ToList();

        detail.FeeLines = transfer.FeeLines.OrderBy(f => f.SortOrder).Select(f => new TransferFeeLineDto
        {
            Id = f.Id,
            FeeType = f.FeeType,
            Label = f.Label,
            Basis = f.Basis,
            Rate = f.Rate,
            Amount = f.Amount,
            PayableBy = f.PayableBy,
            IsPaid = f.IsPaid,
            IsWaived = f.IsWaived,
            SortOrder = f.SortOrder,
        }).ToList();

        if (transfer.DuesClearanceId is not null)
            detail.DuesClearance = await MapDuesClearanceAsync(transfer.DuesClearanceId.Value);

        if (transfer.NocIssuanceId is not null)
            detail.Noc = await GetNocAsync(transfer.NocIssuanceId.Value);

        var session = await Db.TransferSessions.ForCompany(Tenant)
            .Include(s => s.Witnesses)
            .FirstOrDefaultAsync(s => s.TransferRequestId == id);

        if (session is not null) detail.Session = await MapSessionAsync(session);

        detail.Gates = await BuildGatesAsync(transfer, detail);
        detail.DocumentChecklist = BuildDocumentChecklist(transfer, detail);

        return detail;
    }

    /// <summary>
    /// The four gates, in the order a transfer desk works them, each saying what is blocking it
    /// and who could override. A screen built on this needs no branching logic of its own.
    /// </summary>
    private async Task<List<TransferGateDto>> BuildGatesAsync(TransferRequest t, TransferRequestDetailDto detail)
    {
        var gates = new List<TransferGateDto>();
        var settings = await SettingsAsync();

        // Gate 0 — litigation. Not one of the four; it stops everything before them.
        if (t.BlockedByLitigation)
        {
            gates.Add(new TransferGateDto
            {
                Order = 0,
                Key = "litigation",
                Label = "Litigation clear",
                IsSatisfied = t.LitigationOverrideApprovalId is not null,
                BlockingReason = "This property is under litigation.",
                CanOverride = true,
                OverrideRole = "LegalHead",
                WasOverridden = t.LitigationOverrideApprovalId is not null,
            });
        }

        gates.Add(new TransferGateDto
        {
            Order = 1,
            Key = "dues",
            Label = "Dues cleared",
            IsSatisfied = t.DuesCleared || t.DuesOverrideApprovalId is not null,
            Detail = t.OutstandingAtRequest > 0m
                ? $"{t.OutstandingAtRequest:N0} outstanding at the time of request."
                : "Nothing outstanding.",
            BlockingReason = t.DuesCleared ? null : $"{t.OutstandingAtRequest:N0} is still owed.",
            CanOverride = settings.BlockTransferOnDues,
            OverrideRole = "FinanceHead",
            WasOverridden = t.DuesOverrideApprovalId is not null,
            Route = t.BookingId is null ? null : $"/realestate/bookings/{t.BookingId}/ledger",
        });

        gates.Add(new TransferGateDto
        {
            Order = 2,
            Key = "noc",
            Label = "No-objection certificate issued",
            IsSatisfied = detail.NocIssued,
            Detail = detail.Noc is null ? "No NOC has been requested yet." : $"NOC {detail.Noc.NocNumber}, {detail.Noc.Status}.",
            BlockingReason = detail.NocIssued ? null : "The transfer NOC has not been issued.",
            CanOverride = false,
            Route = t.NocIssuanceId is null ? "/realestate/nocs/new" : $"/realestate/nocs/{t.NocIssuanceId}",
        });

        gates.Add(new TransferGateDto
        {
            Order = 3,
            Key = "fees",
            Label = "Transfer fees paid",
            IsSatisfied = t.TotalTransferFee <= 0m || t.FeesPaid >= t.TotalTransferFee,
            Detail = $"{t.FeesPaid:N0} of {t.TotalTransferFee:N0} received.",
            BlockingReason = t.FeesPaid >= t.TotalTransferFee ? null : $"{t.TotalTransferFee - t.FeesPaid:N0} of fees is unpaid.",
            CanOverride = true,
            OverrideRole = "FinanceHead",
            Route = $"/realestate/transfers/{t.Id}/fees",
        });

        var identityGap = detail.Parties.Count(p => !p.IdentityVerified);

        gates.Add(new TransferGateDto
        {
            Order = 4,
            Key = "session",
            Label = "Session held and deed executed",
            IsSatisfied = detail.Session?.IsCompleted == true,
            Detail = detail.Session is null
                ? "No session has been scheduled."
                : detail.Session.IsCompleted
                    ? $"Executed on {detail.Session.CompletedAt:dd MMM yyyy}."
                    : $"Scheduled for {detail.Session.ScheduledAt:dd MMM yyyy HH:mm}.",
            BlockingReason = detail.Session?.IsCompleted == true
                ? null
                : identityGap > 0
                    ? $"{identityGap} of the parties have not had their identity verified."
                    : "The transfer session has not been completed.",
            CanOverride = false,
            Route = $"/realestate/transfers/{t.Id}/session",
        });

        return gates;
    }

    /// <summary>
    /// What paperwork this kind of transfer needs. An inheritance transfer and a gift transfer
    /// need completely different documents, and asking for the wrong ones wastes a desk's day.
    /// </summary>
    private static List<ChecklistItemDto> BuildDocumentChecklist(TransferRequest t, TransferRequestDetailDto detail)
    {
        var items = new List<ChecklistItemDto>();
        var order = 0;

        void Add(string key, string label, bool satisfied, bool mandatory = true)
            => items.Add(new ChecklistItemDto
            {
                Key = key,
                Label = label,
                IsSatisfied = satisfied,
                IsMandatory = mandatory,
                SortOrder = order += 10,
            });

        Add("transferor-id", "Transferor's identity document",
            detail.Parties.Any(p => p.Side == "Transferor" && !string.IsNullOrWhiteSpace(p.IdentityNumber)));

        Add("transferee-id", "Transferee's identity document",
            detail.Parties.Any(p => p.Side == "Transferee" && !string.IsNullOrWhiteSpace(p.IdentityNumber)));

        Add("transferee-photos", "Transferee's photographs",
            detail.Parties.Any(p => p.Side == "Transferee" && !string.IsNullOrWhiteSpace(p.PhotoUrl)));

        Add("kyc", "KYC complete for every party",
            detail.Parties.All(p => p.KycStatus is KycStatus.Verified));

        switch (t.Kind)
        {
            case TransferKind.Inheritance:
                Add("succession", "Succession certificate", !string.IsNullOrWhiteSpace(t.SuccessionCertificateNumber));
                Add("death-certificate", "Death certificate of the registered owner", false);
                Add("heir-consent", "Consent of all other legal heirs", false);
                break;

            case TransferKind.PowerOfAttorney:
                Add("poa", "Registered power of attorney", t.PowerOfAttorneyRelationshipId is not null);
                Add("poa-alive", "Confirmation the grantor is living and the POA is unrevoked", false);
                break;

            case TransferKind.CourtDecree:
                Add("decree", "Certified copy of the court decree", !string.IsNullOrWhiteSpace(t.CourtDecreeReference));
                Add("decree-final", "Confirmation the decree is final and unappealed", false);
                break;

            case TransferKind.Gift:
                Add("gift-deed", "Gift deed", false);
                Add("relationship", "Evidence of the relationship claimed", false, mandatory: false);
                break;

            case TransferKind.PartialShare:
                Add("share-consent", "Consent of the remaining co-owners", false);
                break;

            case TransferKind.Sale:
                Add("sale-agreement", "Sale agreement between the parties", false);
                Add("payment-proof", "Evidence the consideration was paid", t.SaleConsideration is > 0m, mandatory: false);
                break;
        }

        Add("original-file", "Original file or allotment letter surrendered", false);

        return items;
    }

    public async Task<TransferRequestDetailDto> CreateTransferAsync(TransferRequestCreateDto dto, Guid userId)
    {
        if (dto.BookingId is null && dto.UnitId is null && dto.PlotFileId is null && dto.PropertyId is null)
            throw new InvalidOperationException("A transfer has to name a booking, a unit, a file or a property.");

        var settings = await SettingsAsync();
        var today = Today;

        await using var transaction = await Db.Database.BeginTransactionAsync();

        // One live transfer at a time. Two open transfers on one plot is precisely how it gets
        // sold twice, and it is refused here rather than caught at the session.
        var open = await Db.TransferRequests.ForCompany(Tenant)
            .Where(t => t.Status != TransferStatus.Completed
                     && t.Status != TransferStatus.Rejected
                     && t.Status != TransferStatus.Cancelled)
            .WhereIf(dto.BookingId.HasValue, t => t.BookingId == dto.BookingId)
            .WhereIf(dto.UnitId.HasValue, t => t.UnitId == dto.UnitId)
            .WhereIf(dto.PlotFileId.HasValue, t => t.PlotFileId == dto.PlotFileId)
            .Select(t => t.Reference)
            .FirstOrDefaultAsync();

        if (open is not null)
            throw new InvalidOperationException($"{open} is already open on this unit. Complete or reject it first.");

        var outstanding = 0m;

        if (dto.BookingId is not null)
        {
            var booking = await RequireAsync<Booking>(dto.BookingId.Value, "That booking does not exist.");
            outstanding = booking.Outstanding;
        }

        var litigation = dto.PropertyId is not null
            && await Db.Properties.ForCompany(Tenant).AnyAsync(p => p.Id == dto.PropertyId && p.HasLitigation);

        var transfer = new TransferRequest
        {
            Reference = await numbering.NextTransferNumberAsync(DateTime.UtcNow),
            BookingId = dto.BookingId,
            UnitId = dto.UnitId,
            PlotFileId = dto.PlotFileId,
            PropertyId = dto.PropertyId,
            ProjectId = dto.ProjectId,
            Kind = dto.Kind,
            Status = TransferStatus.DuesCheckPending,
            RequestedOn = dto.RequestedOn == default ? today : dto.RequestedOn,
            RequestedByPartyId = dto.RequestedByPartyId,
            HandledByUserId = userId,
            SaleConsideration = dto.SaleConsideration,
            OutstandingAtRequest = outstanding,
            DuesCleared = outstanding <= 0m,
            ShareTransferredPercent = dto.ShareTransferredPercent,
            SuccessionCertificateNumber = dto.SuccessionCertificateNumber,
            PowerOfAttorneyRelationshipId = dto.PowerOfAttorneyRelationshipId,
            CourtDecreeReference = dto.CourtDecreeReference,
            BlockedByLitigation = litigation,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        Db.TransferRequests.Add(transfer);

        foreach (var p in dto.Parties)
        {
            var kyc = await Db.KycCases.ForCompany(Tenant)
                .Where(k => k.PartyId == p.PartyId)
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => k.Status)
                .FirstOrDefaultAsync();

            transfer.Parties.Add(new TransferParty
            {
                PartyId = p.PartyId,
                Side = p.Side,
                SharePercent = p.SharePercent <= 0m ? 100m : p.SharePercent,
                KycStatus = kyc == default ? KycStatus.NotStarted : kyc,
                IdentityVerified = p.IdentityVerified,
                PhotoUrl = p.PhotoUrl,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();
        await ComputeFeesInternalAsync(transfer, userId);
        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetTransferAsync(transfer.Id))!;
    }

    /// <summary>
    /// A statement that nothing is owed, at a point in time. Kept as a record rather than computed
    /// on demand, because a transfer completed on Tuesday relied on Tuesday's position and that
    /// has to stay provable afterwards.
    /// </summary>
    public async Task<DuesClearanceDto> IssueDuesClearanceAsync(
        Guid? bookingId, Guid? unitId, Guid? propertyId, Guid partyId, Guid userId)
    {
        var today = Today;
        var instalments = 0m;
        var surcharge = 0m;

        if (bookingId is not null)
        {
            var lines = await Db.Instalments.ForCompany(Tenant)
                .Where(i => i.BookingId == bookingId
                         && i.Status != InstalmentStatus.Paid
                         && i.Status != InstalmentStatus.Cancelled
                         && i.Status != InstalmentStatus.Waived)
                .Select(i => new
                {
                    Principal = i.Amount - i.PaidAmount,
                    Surcharge = i.SurchargeAccrued - i.SurchargePaid - i.SurchargeWaived,
                })
                .ToListAsync();

            instalments = lines.Sum(l => l.Principal);
            surcharge = lines.Sum(l => l.Surcharge);
        }

        var maintenance = unitId is null && propertyId is null
            ? 0m
            : await Db.MaintenanceBills.ForCompany(Tenant)
                .Where(b => (unitId != null && b.UnitId == unitId) || (propertyId != null && b.PropertyId == propertyId))
                .Where(b => b.Status != InstalmentStatus.Paid && b.Status != InstalmentStatus.Cancelled)
                .SumAsync(b => b.TotalAmount - b.PaidAmount);

        var utilities = unitId is null && propertyId is null
            ? 0m
            : await Db.UtilityBills.ForCompany(Tenant)
                .Where(b => unitId != null && b.UnitId == unitId)
                .Where(b => b.Status != InstalmentStatus.Paid && b.Status != InstalmentStatus.Cancelled)
                .SumAsync(b => b.TotalAmount - b.PaidAmount);

        var other = bookingId is null
            ? 0m
            : await Db.BookingChargeLines.ForCompany(Tenant)
                .Where(c => c.BookingId == bookingId && c.IsAccepted && !c.IsPartOfSalePrice)
                .SumAsync(c => c.Amount + c.TaxAmount);

        var total = RealEstateMapper.Money(
            Math.Max(0m, instalments) + Math.Max(0m, surcharge) + Math.Max(0m, maintenance)
            + Math.Max(0m, utilities) + Math.Max(0m, other));

        var clearance = new DuesClearance
        {
            Reference = await numbering.NextMasterCodeAsync(Db.DuesClearances, "DUE"),
            BookingId = bookingId,
            UnitId = unitId,
            PropertyId = propertyId,
            PartyId = partyId,
            IssuedOn = today,

            // A clearance goes stale. Thirty days is long enough to complete a transfer and short
            // enough that a new instalment cannot fall due behind it unnoticed.
            ValidUntil = today.AddDays(30),

            InstalmentsOutstanding = RealEstateMapper.Money(Math.Max(0m, instalments)),
            SurchargeOutstanding = RealEstateMapper.Money(Math.Max(0m, surcharge)),
            MaintenanceOutstanding = RealEstateMapper.Money(Math.Max(0m, maintenance)),
            UtilityOutstanding = RealEstateMapper.Money(Math.Max(0m, utilities)),
            OtherOutstanding = RealEstateMapper.Money(Math.Max(0m, other)),
            TotalOutstanding = total,
            IsClear = total <= 0m,
            IssuedByUserId = userId,
        }.StampNew(Tenant, userId);

        Db.DuesClearances.Add(clearance);
        await Db.SaveChangesAsync();

        return (await MapDuesClearanceAsync(clearance.Id))!;
    }

    private async Task<DuesClearanceDto?> MapDuesClearanceAsync(Guid id)
    {
        var clearance = await Db.DuesClearances.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == id);
        if (clearance is null) return null;

        var currency = await CurrencyAsync();
        var names = await AgentUserNamesAsync([clearance.IssuedByUserId]);

        return new DuesClearanceDto
        {
            Id = clearance.Id,
            Reference = clearance.Reference,
            IssuedOn = clearance.IssuedOn,
            ValidUntil = clearance.ValidUntil,
            InstalmentsOutstanding = clearance.InstalmentsOutstanding,
            SurchargeOutstanding = clearance.SurchargeOutstanding,
            MaintenanceOutstanding = clearance.MaintenanceOutstanding,
            UtilityOutstanding = clearance.UtilityOutstanding,
            OtherOutstanding = clearance.OtherOutstanding,
            TotalOutstanding = clearance.TotalOutstanding,
            CurrencyCode = currency,
            IsClear = clearance.IsClear,
            IsExpired = clearance.ValidUntil < Today,
            IssuedByName = clearance.IssuedByUserId is null ? null : names.GetValueOrDefault(clearance.IssuedByUserId.Value),
            DocumentUrl = clearance.DocumentUrl,
            Note = clearance.Note,
        };
    }

    public async Task<TransferRequestDetailDto> OverrideDuesAsync(Guid transferId, string reason, Guid userId)
    {
        var transfer = await RequireAsync<TransferRequest>(transferId, "That transfer does not exist.");

        if (transfer.DuesCleared)
            throw new InvalidOperationException("There is nothing to override — dues on this transfer are already clear.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("An override has to say why.");

        var approval = await RaiseApprovalAsync(
            "TransferDuesOverride", transferId, transfer.Reference, transfer.OutstandingAtRequest,
            $"Proceed with {transfer.Reference} while {transfer.OutstandingAtRequest:N0} is outstanding. {reason}",
            userId, projectId: transfer.ProjectId, note: reason);

        transfer.DuesOverrideApprovalId = approval?.Id ?? Guid.Empty;

        if (approval is null)
        {
            transfer.DuesCleared = true;
            transfer.Status = TransferStatus.DocumentsPending;
        }

        transfer.StampUpdated(userId);

        await WriteAuditNoteAsync(
            "TransferRequest", transferId, "TransferDuesOverride", Guid.Empty, userId,
            amountImpact: transfer.OutstandingAtRequest,
            note: reason,
            approvalRequestId: approval?.Id,
            entityReference: transfer.Reference,
            highRisk: true);

        await Db.SaveChangesAsync();
        return (await GetTransferAsync(transferId))!;
    }

    public async Task<TransferRequestDetailDto> ComputeFeesAsync(Guid transferId, Guid userId)
    {
        var transfer = await Db.TransferRequests.ForCompany(Tenant)
            .Include(t => t.FeeLines)
            .FirstOrDefaultAsync(t => t.Id == transferId)
            ?? throw new InvalidOperationException("That transfer does not exist.");

        await ComputeFeesInternalAsync(transfer, userId);
        await Db.SaveChangesAsync();

        return (await GetTransferAsync(transferId))!;
    }

    /// <summary>
    /// The fee schedule. Split by who pays it, because getting that wrong is what holds up a
    /// session while two families argue in a corridor.
    /// </summary>
    private async Task ComputeFeesInternalAsync(TransferRequest transfer, Guid userId)
    {
        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == transfer.ProjectId);
        if (project is null) return;

        // Waived lines are the operator's decision and survive a recompute.
        var waived = transfer.FeeLines.Where(f => f.IsWaived || f.IsPaid).ToList();
        var recomputable = transfer.FeeLines.Except(waived).ToList();

        Db.TransferFeeLines.RemoveRange(recomputable);
        foreach (var line in recomputable) transfer.FeeLines.Remove(line);

        var areaSqFt = 0m;

        if (transfer.UnitId is not null)
        {
            areaSqFt = await Db.Units.ForCompany(Tenant)
                .Where(u => u.Id == transfer.UnitId)
                .Join(Db.Properties.ForCompany(Tenant), u => u.PropertyId, p => p.Id, (u, p) => p.SaleableAreaSqFt ?? p.PlotAreaSqFt ?? 0m)
                .FirstOrDefaultAsync();
        }
        else if (transfer.PlotFileId is not null)
        {
            areaSqFt = await Db.PlotFiles.ForCompany(Tenant)
                .Where(f => f.Id == transfer.PlotFileId)
                .Select(f => f.NominalAreaSqFt)
                .FirstOrDefaultAsync();
        }

        var order = waived.Count == 0 ? 0 : waived.Max(w => w.SortOrder);

        void Add(string type, string label, ChargeBasis basis, decimal rate, decimal amount, string payableBy)
        {
            if (amount <= 0m) return;
            if (waived.Any(w => w.FeeType == type)) return;

            transfer.FeeLines.Add(new TransferFeeLine
            {
                FeeType = type,
                Label = label,
                Basis = basis,
                Rate = rate,
                Amount = RealEstateMapper.Money(amount),
                PayableBy = payableBy,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));
        }

        if (project.TransferFeePerSqFt > 0m && areaSqFt > 0m)
        {
            Add("TransferFee", $"Transfer fee at {project.TransferFeePerSqFt:N0} per sq ft",
                ChargeBasis.PerAreaUnit, project.TransferFeePerSqFt, project.TransferFeePerSqFt * areaSqFt, "Transferee");
        }
        else if (project.TransferFeeFlat > 0m)
        {
            Add("TransferFee", "Transfer fee", ChargeBasis.Fixed, project.TransferFeeFlat, project.TransferFeeFlat, "Transferee");
        }

        // Inheritance and name-correction transfers are usually charged a nominal documentation
        // fee rather than the full transfer fee. That is a real term, not a discount.
        if (transfer.Kind is TransferKind.Inheritance or TransferKind.NameCorrection)
        {
            var full = transfer.FeeLines.FirstOrDefault(f => f.FeeType == "TransferFee");

            if (full is not null)
            {
                full.Amount = RealEstateMapper.Money(full.Amount * 0.25m);
                full.Label += transfer.Kind == TransferKind.Inheritance
                    ? " (reduced — inheritance)"
                    : " (reduced — name correction)";
            }
        }

        Add("DocumentationCharge", "Documentation and file charges", ChargeBasis.Fixed, 0m, 2500m, "Transferee");

        if (transfer.Kind == TransferKind.Sale && transfer.SaleConsideration is > 0m)
        {
            var settings = await SettingsAsync();

            // Withholding on the gain is a real obligation in most of these markets, and the desk
            // is the last point at which it can be collected.
            var gain = transfer.SaleConsideration.Value;

            Add("WithholdingTax", "Withholding tax on the sale consideration",
                ChargeBasis.PercentOfTotal, 1m, gain * 0.01m, "Transferor");
        }

        transfer.TotalTransferFee = RealEstateMapper.Money(transfer.FeeLines.Where(f => !f.IsWaived).Sum(f => f.Amount));
        transfer.FeesPaid = RealEstateMapper.Money(transfer.FeeLines.Where(f => f.IsPaid).Sum(f => f.Amount));
        transfer.StampUpdated(userId);
    }

    public async Task<TransferSessionDto> ScheduleSessionAsync(Guid transferId, DateTime scheduledAt, string? venue, Guid userId)
    {
        var transfer = await RequireAsync<TransferRequest>(transferId, "That transfer does not exist.");

        var detail = await GetTransferAsync(transferId)
            ?? throw new InvalidOperationException("That transfer does not exist.");

        // Everything before the session has to be closed. Scheduling around an open gate is how
        // two families arrive to find the deed cannot be executed.
        var blocking = detail.Gates
            .Where(g => g.Order < 4 && !g.IsSatisfied)
            .Select(g => g.BlockingReason ?? g.Label)
            .ToList();

        if (blocking.Count > 0)
            throw new InvalidOperationException($"This transfer is not ready for a session: {string.Join(" ", blocking)}");

        if (scheduledAt < DateTime.UtcNow)
            throw new InvalidOperationException("A session cannot be scheduled in the past.");

        var session = await Db.TransferSessions.ForCompany(Tenant)
            .Include(s => s.Witnesses)
            .FirstOrDefaultAsync(s => s.TransferRequestId == transferId);

        if (session is null)
        {
            session = new TransferSession { TransferRequestId = transferId }.StampNew(Tenant, userId);
            Db.TransferSessions.Add(session);
        }
        else
        {
            if (session.IsCompleted)
                throw new InvalidOperationException("This transfer's session has already been held.");

            session.StampUpdated(userId);
        }

        session.ScheduledAt = scheduledAt;
        session.Venue = venue;
        session.ConductedByUserId = userId;

        transfer.Status = TransferStatus.SessionScheduled;
        transfer.TransferSessionId = session.Id;
        transfer.StampUpdated(userId);

        await Db.SaveChangesAsync();

        foreach (var party in detail.Parties)
        {
            await QueueNotificationAsync(
                "TransferSessionScheduled",
                "Transfer appointment confirmed",
                $"{scheduledAt:dd MMM yyyy 'at' HH:mm}{(string.IsNullOrWhiteSpace(venue) ? "" : $", {venue}")}. Bring your original identity document.",
                $"/realestate/transfers/{transferId}",
                recipientPartyId: party.PartyId,
                entityType: "TransferRequest",
                entityId: transferId);
        }

        await Db.SaveChangesAsync();
        return (await MapSessionAsync(session))!;
    }

    public async Task<TransferSessionDto> SaveSessionAsync(TransferSessionDto dto, Guid userId)
    {
        var session = await Db.TransferSessions.ForCompany(Tenant)
            .Include(s => s.Witnesses)
            .FirstOrDefaultAsync(s => s.Id == dto.Id)
            ?? throw new InvalidOperationException("That session does not exist.");

        if (session.IsCompleted)
            throw new InvalidOperationException("This session is closed and cannot be edited.");

        session.StartedAt = dto.StartedAt;
        session.Venue = dto.Venue;
        session.TransferorPresent = dto.TransferorPresent;
        session.TransfereePresent = dto.TransfereePresent;
        session.IdentitiesVerified = dto.IdentitiesVerified;
        session.DeedNumber = dto.DeedNumber;
        session.SessionPhotoUrl = dto.SessionPhotoUrl;
        session.VideoUrl = dto.VideoUrl;
        session.AbortReason = dto.AbortReason;
        session.StampUpdated(userId);

        if (dto.Witnesses.Count > 0)
        {
            Db.TransferWitnesses.RemoveRange(session.Witnesses);

            var sequence = 0;

            foreach (var w in dto.Witnesses)
            {
                session.Witnesses.Add(new TransferWitness
                {
                    PartyId = w.PartyId,
                    Name = w.Name,
                    IdentityNumber = w.IdentityNumber,
                    Phone = w.Phone,
                    Address = w.Address,
                    SignatureUrl = w.SignatureUrl,
                    ThumbImpressionUrl = w.ThumbImpressionUrl,
                    SequenceNumber = ++sequence,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await MapSessionAsync(session))!;
    }

    private async Task<TransferSessionDto> MapSessionAsync(TransferSession session)
    {
        var names = await AgentUserNamesAsync([session.ConductedByUserId]);

        return new TransferSessionDto
        {
            Id = session.Id,
            TransferRequestId = session.TransferRequestId,
            ScheduledAt = session.ScheduledAt,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            Venue = session.Venue,
            ConductedByName = session.ConductedByUserId is null ? null : names.GetValueOrDefault(session.ConductedByUserId.Value),
            TransferorPresent = session.TransferorPresent,
            TransfereePresent = session.TransfereePresent,
            IdentitiesVerified = session.IdentitiesVerified,
            DeedNumber = session.DeedNumber,
            SessionPhotoUrl = session.SessionPhotoUrl,
            VideoUrl = session.VideoUrl,
            IsCompleted = session.IsCompleted,
            AbortReason = session.AbortReason,
            Witnesses = session.Witnesses.OrderBy(w => w.SequenceNumber).Select(w => new TransferWitnessDto
            {
                Id = w.Id,
                PartyId = w.PartyId,
                Name = w.Name,
                IdentityNumber = w.IdentityNumber,
                Phone = w.Phone,
                Address = w.Address,
                SignatureUrl = w.SignatureUrl,
                ThumbImpressionUrl = w.ThumbImpressionUrl,
                SequenceNumber = w.SequenceNumber,
            }).ToList(),
        };
    }

    /// <summary>
    /// Completes the transfer. One transaction moves the booking, the unit, the file and the
    /// ownership chain together — a half-completed transfer is the state that produces two owners.
    /// </summary>
    public async Task<TransferRequestDetailDto> CompleteTransferAsync(Guid transferId, Guid userId)
    {
        var transfer = await Db.TransferRequests.ForCompany(Tenant)
            .Include(t => t.Parties)
            .Include(t => t.FeeLines)
            .FirstOrDefaultAsync(t => t.Id == transferId)
            ?? throw new InvalidOperationException("That transfer does not exist.");

        if (transfer.Status == TransferStatus.Completed)
            throw new InvalidOperationException($"{transfer.Reference} was already completed on {transfer.CompletedOn:dd MMM yyyy}.");

        var detail = await GetTransferAsync(transferId)!;
        var unmet = detail!.Gates.Where(g => !g.IsSatisfied).Select(g => g.BlockingReason ?? g.Label).ToList();

        if (unmet.Count > 0)
            throw new InvalidOperationException($"This transfer cannot complete: {string.Join(" ", unmet)}");

        var session = await Db.TransferSessions.ForCompany(Tenant)
            .FirstOrDefaultAsync(s => s.TransferRequestId == transferId)
            ?? throw new InvalidOperationException("The transfer session has not been held.");

        if (!session.IdentitiesVerified)
            throw new InvalidOperationException("Identities were not verified at the session. A transfer cannot complete without that.");

        var transferee = transfer.Parties.FirstOrDefault(p => p.Side == "Transferee")
            ?? throw new InvalidOperationException("No transferee is recorded on this transfer.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var today = Today;

        session.CompletedAt = DateTime.UtcNow;
        session.IsCompleted = true;
        session.StampUpdated(userId);

        transfer.Status = TransferStatus.Completed;
        transfer.CompletedOn = today;
        transfer.StampUpdated(userId);

        // The old booking closes and a new one opens in the buyer's name, carrying the remaining
        // schedule. Editing the old booking would erase who owned it and what they paid.
        if (transfer.BookingId is not null)
        {
            var old = await RequireAsync<Booking>(transfer.BookingId.Value, "The booking behind this transfer is missing.");

            var replacement = new Booking
            {
                Reference = await numbering.NextBookingNumberAsync(DateTime.UtcNow),
                ProjectId = old.ProjectId,
                UnitId = old.UnitId,
                PlotFileId = old.PlotFileId,
                PropertyId = old.PropertyId,
                PrimaryApplicantPartyId = transferee.PartyId,
                Status = old.Status == BookingStatus.AgreementSigned ? BookingStatus.AgreementSigned : BookingStatus.Confirmed,
                BookingDate = today,
                CurrencyCode = old.CurrencyCode,
                TotalConsideration = transfer.SaleConsideration ?? old.TotalConsideration,
                NetSalePrice = old.NetSalePrice,
                ListPrice = old.ListPrice,
                AreaSqFt = old.AreaSqFt,
                RatePerSqFt = old.RatePerSqFt,
                PaymentPlanId = old.PaymentPlanId,

                // What the seller paid does not become what the buyer paid. The buyer's ledger
                // starts at what is still owed on the plan.
                TotalPaid = old.TotalPaid,
                Outstanding = old.Outstanding,
                SalesExecutiveId = old.SalesExecutiveId,
                ChannelPartnerId = old.ChannelPartnerId,
                PreviousBookingId = old.Id,
                TransferRequestId = transfer.Id,
            }.StampNew(Tenant, userId);

            Db.Bookings.Add(replacement);

            old.Status = BookingStatus.Transferred;
            old.TransferRequestId = transfer.Id;
            old.StampUpdated(userId);

            // The remaining schedule moves across, unpaid lines only.
            var remaining = await Db.Instalments.ForCompany(Tenant)
                .Where(i => i.BookingId == old.Id
                         && i.Status != InstalmentStatus.Paid
                         && i.Status != InstalmentStatus.Cancelled)
                .ToListAsync();

            foreach (var instalment in remaining)
            {
                instalment.BookingId = replacement.Id;
                instalment.StampUpdated(userId);
            }

            transfer.NewBookingId = replacement.Id;

            if (transfer.UnitId is not null)
            {
                var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == transfer.UnitId);

                if (unit is not null)
                {
                    unit.CurrentBookingId = replacement.Id;
                    unit.StampUpdated(userId);
                }
            }

            if (transfer.PlotFileId is not null)
            {
                var file = await Db.PlotFiles.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == transfer.PlotFileId);

                if (file is not null)
                {
                    file.CurrentBookingId = replacement.Id;
                    file.StampUpdated(userId);
                }
            }
        }

        await WriteOwnershipChainAsync(transfer, transferee, session, today, userId);

        // The NOC is spent. It authorised this transfer and no other.
        if (transfer.NocIssuanceId is not null)
        {
            var noc = await Db.NocIssuances.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == transfer.NocIssuanceId);

            if (noc is not null && noc.Status == NocStatus.Issued)
            {
                noc.Status = NocStatus.Expired;
                noc.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        await QueueNotificationAsync(
            "TransferCompleted",
            $"{transfer.Reference} completed",
            $"Ownership passed on {today:dd MMM yyyy}. Deed {session.DeedNumber ?? "—"}.",
            $"/realestate/transfers/{transferId}",
            recipientPartyId: transferee.PartyId,
            entityType: "TransferRequest",
            entityId: transferId);

        await Db.SaveChangesAsync();
        return (await GetTransferAsync(transferId))!;
    }

    /// <summary>
    /// Appends to the ownership chain. The previous holder's row is closed rather than edited,
    /// and the buyer's identity details are copied in rather than referenced — the chain has to
    /// still read correctly if the party record is later merged or corrected.
    /// </summary>
    private async Task WriteOwnershipChainAsync(
        TransferRequest transfer, TransferParty transferee, TransferSession session, DateOnly today, Guid userId)
    {
        var current = await Db.OwnershipChainEntries.ForCompany(Tenant)
            .Where(e => e.IsCurrent)
            .WhereIf(transfer.UnitId.HasValue, e => e.UnitId == transfer.UnitId)
            .WhereIf(transfer.PlotFileId.HasValue, e => e.PlotFileId == transfer.PlotFileId)
            .WhereIf(transfer.PropertyId.HasValue, e => e.PropertyId == transfer.PropertyId)
            .ToListAsync();

        foreach (var entry in current)
        {
            entry.ToDate = today;
            entry.IsCurrent = false;
            entry.StampUpdated(userId);
        }

        var sequence = await Db.OwnershipChainEntries.ForCompany(Tenant)
            .WhereIf(transfer.UnitId.HasValue, e => e.UnitId == transfer.UnitId)
            .WhereIf(transfer.PlotFileId.HasValue, e => e.PlotFileId == transfer.PlotFileId)
            .WhereIf(transfer.PropertyId.HasValue, e => e.PropertyId == transfer.PropertyId)
            .MaxAsync(e => (int?)e.SequenceNumber) ?? 0;

        var person = await Db.Parties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == transferee.PartyId);

        var identity = await Db.PartyIdentities.ForCompany(Tenant)
            .Where(i => i.PartyId == transferee.PartyId && i.IsPrimary)
            .Select(i => i.Number)
            .FirstOrDefaultAsync();

        Db.OwnershipChainEntries.Add(new OwnershipChainEntry
        {
            UnitId = transfer.UnitId,
            PropertyId = transfer.PropertyId,
            PlotFileId = transfer.PlotFileId,
            SequenceNumber = sequence + 1,
            PartyId = transferee.PartyId,
            OwnerName = person is null ? "—" : RealEstateMapper.DisplayName(person),
            FatherOrGuardianName = person?.FatherOrGuardianName,
            IdentityNumber = identity,
            SharePercent = transferee.SharePercent,
            FromDate = today,
            AcquiredBy = transfer.Kind,
            TransferRequestId = transfer.Id,
            BookingId = transfer.NewBookingId,
            Consideration = transfer.SaleConsideration,
            DocumentReference = session.DeedNumber,
            IsCurrent = true,
        }.StampNew(Tenant, userId));
    }

    public async Task<TransferRequestDetailDto> RejectTransferAsync(Guid transferId, Guid reasonCodeId, string? note, Guid userId)
    {
        var transfer = await RequireAsync<TransferRequest>(transferId, "That transfer does not exist.");

        if (transfer.Status == TransferStatus.Completed)
            throw new InvalidOperationException("A completed transfer cannot be rejected. Reverse it with a fresh transfer instead.");

        transfer.Status = TransferStatus.Rejected;
        transfer.RejectReasonCodeId = reasonCodeId;
        transfer.Note = note ?? transfer.Note;
        transfer.StampUpdated(userId);

        await WriteAuditNoteAsync(
            "TransferRequest", transferId, "TransferRejected", reasonCodeId, userId,
            note: note, entityReference: transfer.Reference);

        await Db.SaveChangesAsync();
        return (await GetTransferAsync(transferId))!;
    }

    public async Task<List<OwnershipChainEntryDto>> GetOwnershipChainAsync(Guid? unitId, Guid? propertyId, Guid? plotFileId)
    {
        if (unitId is null && propertyId is null && plotFileId is null)
            throw new InvalidOperationException("Name a unit, a property or a file to read its chain.");

        var entries = await Db.OwnershipChainEntries.ForCompany(Tenant)
            .WhereIf(unitId.HasValue, e => e.UnitId == unitId)
            .WhereIf(propertyId.HasValue, e => e.PropertyId == propertyId)
            .WhereIf(plotFileId.HasValue, e => e.PlotFileId == plotFileId)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        var today = Today;

        return entries.Select(e => new OwnershipChainEntryDto
        {
            Id = e.Id,
            SequenceNumber = e.SequenceNumber,
            PartyId = e.PartyId,
            OwnerName = e.OwnerName,
            FatherOrGuardianName = e.FatherOrGuardianName,
            IdentityNumber = e.IdentityNumber,
            SharePercent = e.SharePercent,
            FromDate = e.FromDate,
            ToDate = e.ToDate,
            AcquiredBy = e.AcquiredBy,
            Consideration = e.Consideration,
            DocumentReference = e.DocumentReference,
            TransferRequestId = e.TransferRequestId,
            IsCurrent = e.IsCurrent,
            HeldForDays = (e.ToDate ?? today).DayNumber - e.FromDate.DayNumber,
        }).ToList();
    }

    // ═══ Duplicate files ═════════════════════════════════════════════════════

    /// <summary>
    /// A lost paper file. The indemnity trail matters more than the paperwork: a duplicate issued
    /// without an affidavit, a public notice and an objection window is a fraud waiting to be
    /// discovered, and the original file is still out there in somebody's hands.
    /// </summary>
    public async Task<DuplicateFileRequestDto> CreateDuplicateFileRequestAsync(DuplicateFileRequestDto dto, Guid userId)
    {
        var request = dto.Id != Guid.Empty
            ? await Db.DuplicateFileRequests.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        if (request is null)
        {
            request = new DuplicateFileRequest
            {
                Reference = await numbering.NextMasterCodeAsync(Db.DuplicateFileRequests, "DUP"),
                PlotFileId = dto.PlotFileId,
                BookingId = dto.BookingId,
                PartyId = dto.PartyId,
                RequestedOn = dto.RequestedOn == default ? Today : dto.RequestedOn,
            }.StampNew(Tenant, userId);

            Db.DuplicateFileRequests.Add(request);
        }
        else
        {
            if (request.IsIssued)
                throw new InvalidOperationException("A duplicate has already been issued against this request.");

            request.StampUpdated(userId);
        }

        request.LossCircumstances = dto.LossCircumstances;
        request.AffidavitReceived = dto.AffidavitReceived;
        request.AffidavitUrl = dto.AffidavitUrl;
        request.PoliceReportReceived = dto.PoliceReportReceived;
        request.PoliceReportNumber = dto.PoliceReportNumber;
        request.NewspaperNoticePublished = dto.NewspaperNoticePublished;
        request.NoticePublishedOn = dto.NoticePublishedOn;
        request.NoticeClippingUrl = dto.NoticeClippingUrl;
        request.ObjectionWindowDays = dto.ObjectionWindowDays <= 0 ? 14 : dto.ObjectionWindowDays;
        request.ObjectionReceived = dto.ObjectionReceived;
        request.IndemnityBondReceived = dto.IndemnityBondReceived;
        request.IndemnityBondUrl = dto.IndemnityBondUrl;
        request.Fee = dto.Fee;

        await Db.SaveChangesAsync();
        return (await MapDuplicateAsync(request))!;
    }

    public async Task<DuplicateFileRequestDto> IssueDuplicateFileAsync(Guid id, Guid userId)
    {
        var request = await RequireAsync<DuplicateFileRequest>(id, "That request does not exist.");

        if (request.IsIssued)
            throw new InvalidOperationException($"A duplicate was already issued on {request.IssuedOn:dd MMM yyyy}.");

        var mapped = await MapDuplicateAsync(request);
        var missing = mapped!.Checklist.Where(c => c.IsMandatory && !c.IsSatisfied).Select(c => c.Label).ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException($"Not yet: {string.Join("; ", missing)}.");

        if (request.ObjectionReceived)
            throw new InvalidOperationException("An objection was received against the public notice. Resolve it before issuing.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var today = Today;

        request.IsIssued = true;
        request.IssuedOn = today;
        request.StampUpdated(userId);

        if (request.PlotFileId is not null)
        {
            var file = await Db.PlotFiles.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == request.PlotFileId);

            if (file is not null)
            {
                // The flag stays on the file forever. Anyone dealing with it later needs to know
                // an original is unaccounted for.
                file.IsDuplicateIssued = true;
                file.StampUpdated(userId);

                request.DuplicateFileNumber = $"{file.FileNumber}-D";
            }
        }

        await WriteAuditNoteAsync(
            "DuplicateFileRequest", id, "DuplicateFileIssued", Guid.Empty, userId,
            note: request.LossCircumstances,
            entityReference: request.Reference,
            highRisk: true);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await MapDuplicateAsync(request))!;
    }

    private async Task<DuplicateFileRequestDto?> MapDuplicateAsync(DuplicateFileRequest request)
    {
        var names = await PartyNamesAsync([request.PartyId]);

        var fileNumber = request.PlotFileId is null
            ? null
            : await Db.PlotFiles.ForCompany(Tenant)
                .Where(f => f.Id == request.PlotFileId)
                .Select(f => f.FileNumber)
                .FirstOrDefaultAsync();

        var windowEnds = request.NoticePublishedOn?.AddDays(request.ObjectionWindowDays);
        var today = Today;
        var order = 0;

        var checklist = new List<ChecklistItemDto>
        {
            new()
            {
                Key = "affidavit", Label = "Sworn affidavit describing the loss",
                IsSatisfied = request.AffidavitReceived, IsMandatory = true,
                Url = request.AffidavitUrl, SortOrder = order += 10,
            },
            new()
            {
                Key = "police", Label = "Police report lodged",
                IsSatisfied = request.PoliceReportReceived, IsMandatory = true,
                Note = request.PoliceReportNumber, SortOrder = order += 10,
            },
            new()
            {
                Key = "notice", Label = "Public notice published in a newspaper",
                IsSatisfied = request.NewspaperNoticePublished, IsMandatory = true,
                Url = request.NoticeClippingUrl, SortOrder = order += 10,
            },
            new()
            {
                Key = "window",
                Label = $"Objection window of {request.ObjectionWindowDays} days elapsed",
                IsSatisfied = windowEnds is not null && windowEnds <= today,
                IsMandatory = true,
                Note = windowEnds is null ? null : $"Ends {windowEnds:dd MMM yyyy}.",
                SortOrder = order += 10,
            },
            new()
            {
                Key = "indemnity", Label = "Indemnity bond executed",
                IsSatisfied = request.IndemnityBondReceived, IsMandatory = true,
                Url = request.IndemnityBondUrl, SortOrder = order += 10,
            },
            new()
            {
                Key = "fee", Label = "Duplicate file fee paid",
                IsSatisfied = request.Fee <= 0m || request.FeeReceiptId is not null,
                IsMandatory = true, SortOrder = order + 10,
            },
        };

        return new DuplicateFileRequestDto
        {
            Id = request.Id,
            Reference = request.Reference,
            PlotFileId = request.PlotFileId,
            FileNumber = fileNumber,
            BookingId = request.BookingId,
            PartyId = request.PartyId,
            PartyName = names.GetValueOrDefault(request.PartyId, "—"),
            RequestedOn = request.RequestedOn,
            LossCircumstances = request.LossCircumstances,
            AffidavitReceived = request.AffidavitReceived,
            AffidavitUrl = request.AffidavitUrl,
            PoliceReportReceived = request.PoliceReportReceived,
            PoliceReportNumber = request.PoliceReportNumber,
            NewspaperNoticePublished = request.NewspaperNoticePublished,
            NoticePublishedOn = request.NoticePublishedOn,
            NoticeClippingUrl = request.NoticeClippingUrl,
            ObjectionWindowDays = request.ObjectionWindowDays,
            ObjectionWindowEndsOn = windowEnds,
            ObjectionReceived = request.ObjectionReceived,
            IndemnityBondReceived = request.IndemnityBondReceived,
            IndemnityBondUrl = request.IndemnityBondUrl,
            Fee = request.Fee,
            IsIssued = request.IsIssued,
            IssuedOn = request.IssuedOn,
            DuplicateFileNumber = request.DuplicateFileNumber,
            Checklist = checklist,
        };
    }
}
