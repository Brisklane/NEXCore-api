using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Bookings, allotment, agreements and balloting — the second half of <see cref="BookingService"/>.
/// </summary>
public partial class BookingService
{
    // ═══ Bookings ════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<BookingListItemDto>> GetBookingsAsync(BookingSearchDto query)
    {
        var q = Db.Bookings.ForCompany(Tenant)
            .WhereIf(query.Statuses.Count > 0, b => query.Statuses.Contains(b.Status))
            .WhereIf(query.ProjectId.HasValue, b => b.ProjectId == query.ProjectId)
            .WhereIf(query.UnitId.HasValue, b => b.UnitId == query.UnitId)
            .WhereIf(query.PartyId.HasValue, b => b.PrimaryApplicantPartyId == query.PartyId)
            .WhereIf(query.ChannelPartnerId.HasValue, b => b.ChannelPartnerId == query.ChannelPartnerId)
            .WhereIf(query.SalesExecutiveId.HasValue, b => b.SalesExecutiveId == query.SalesExecutiveId)
            .WhereIf(query.SourcingChannel.HasValue, b => b.SourcingChannel == query.SourcingChannel)
            .WhereIf(query.OverdueOnly == true, b => b.OverdueAmount > 0)
            .WhereIf(query.DefaultingOnly == true, b => b.Status == BookingStatus.Defaulting)
            .WhereIf(query.MinCollectionPercent.HasValue, b => b.CollectionPercent >= query.MinCollectionPercent)
            .WhereIf(query.MaxCollectionPercent.HasValue, b => b.CollectionPercent <= query.MaxCollectionPercent)
            .WhereIf(query.FromDate.HasValue, b => b.BookingDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, b => b.BookingDate <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), b => b.Reference.Contains(query.Search!))
            .OrderByDescending(b => b.BookingDate);

        return await PageAsync(q, query, MapBookingListAsync);
    }

    private async Task<List<BookingListItemDto>> MapBookingListAsync(List<Booking> bookings)
    {
        if (bookings.Count == 0) return [];

        var names = await PartyNamesAsync(bookings.Select(b => b.PrimaryApplicantPartyId));
        var projects = await ProjectNamesAsync(bookings.Select(b => (Guid?)b.ProjectId));
        var areaUnit = await AreaUnitAsync();

        var parties = await Db.Parties.ForCompany(Tenant)
            .Where(p => bookings.Select(b => b.PrimaryApplicantPartyId).Contains(p.Id))
            .Select(p => new { p.Id, p.PrimaryPhone, p.FatherOrGuardianName, p.KycStatus })
            .ToDictionaryAsync(p => p.Id, p => p);

        var unitIds = bookings.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).ToList();
        var units = unitIds.Count == 0
            ? []
            : await (
                from u in Db.Units.ForCompany(Tenant)
                join p in Db.Properties.ForCompany(Tenant) on u.PropertyId equals p.Id
                where unitIds.Contains(u.Id)
                select new { u.Id, u.UnitNumber, u.ProjectNodeId, p.SubType, p.SaleableAreaSqFt })
                .ToDictionaryAsync(x => x.Id, x => x);

        var nodeIds = units.Values.Where(u => u.ProjectNodeId.HasValue).Select(u => u.ProjectNodeId!.Value).Distinct().ToList();
        var nodes = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant).Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var fileIds = bookings.Where(b => b.PlotFileId.HasValue).Select(b => b.PlotFileId!.Value).ToList();
        var files = fileIds.Count == 0
            ? []
            : await Db.PlotFiles.ForCompany(Tenant).Where(f => fileIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.FileNumber);

        var partners = await Db.ChannelPartners.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);
        var agents = await Db.AgentProfiles.ForCompany(Tenant).ToDictionaryAsync(a => a.Id, a => a.DisplayName);

        return bookings.Select(b =>
        {
            var unit = b.UnitId is null ? null : units.GetValueOrDefault(b.UnitId.Value);
            var party = parties.GetValueOrDefault(b.PrimaryApplicantPartyId);

            return new BookingListItemDto
            {
                Id = b.Id,
                Reference = b.Reference,
                Status = b.Status,
                BookingDate = b.BookingDate,
                ProjectId = b.ProjectId,
                ProjectName = projects.GetValueOrDefault(b.ProjectId, "—"),
                UnitId = b.UnitId,
                UnitNumber = unit?.UnitNumber,
                BlockName = unit?.ProjectNodeId is null ? null : nodes.GetValueOrDefault(unit.ProjectNodeId.Value),
                FileNumber = b.PlotFileId is null ? null : files.GetValueOrDefault(b.PlotFileId.Value),
                SubType = unit?.SubType,
                Area = RealEstateMapper.AreaOrNull(b.AreaSqFt, areaUnit),
                PrimaryApplicantPartyId = b.PrimaryApplicantPartyId,
                ApplicantName = names.GetValueOrDefault(b.PrimaryApplicantPartyId, "—"),
                ApplicantPhone = party?.PrimaryPhone,
                FatherOrGuardianName = party?.FatherOrGuardianName,
                SourcingChannel = b.SourcingChannel,
                PartnerName = b.ChannelPartnerId is null ? null : partners.GetValueOrDefault(b.ChannelPartnerId.Value),
                SalesExecutiveName = b.SalesExecutiveId is null ? null : agents.GetValueOrDefault(b.SalesExecutiveId.Value),
                TotalConsideration = b.TotalConsideration,
                TotalPaid = b.TotalPaid,
                Outstanding = b.Outstanding,
                OverdueAmount = b.OverdueAmount,
                CollectionPercent = b.CollectionPercent,
                CurrencyCode = b.CurrencyCode,
                NextDueDate = b.NextDueDate,
                NextDueAmount = b.NextDueAmount,
                DaysOverdue = b.DaysOverdue,
                IsDefaulting = b.Status == BookingStatus.Defaulting,
                IsUnderLitigation = b.IsUnderLitigation,
                KycStatus = party?.KycStatus ?? KycStatus.NotStarted,
            };
        }).ToList();
    }

    public async Task<BookingDetailDto?> GetBookingAsync(Guid id)
    {
        var booking = await Db.Bookings.ForCompany(Tenant)
            .Include(b => b.Applicants)
            .Include(b => b.ChargeLines)
            .Include(b => b.StatusHistory)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return null;

        var summary = (await MapBookingListAsync([booking]))[0];
        var applicantIds = booking.Applicants.Select(a => a.PartyId).ToList();
        var applicantNames = await PartyNamesAsync(applicantIds);

        var applicantDetails = await Db.Parties.ForCompany(Tenant)
            .Where(p => applicantIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PrimaryPhone, p.FatherOrGuardianName, p.PhotoUrl })
            .ToDictionaryAsync(p => p.Id, p => p);

        var identities = await Db.PartyIdentities.ForCompany(Tenant)
            .Where(i => applicantIds.Contains(i.PartyId) && i.IsPrimary)
            .ToDictionaryAsync(i => i.PartyId, i => i.Number);

        var detail = new BookingDetailDto
        {
            Id = summary.Id,
            Reference = summary.Reference,
            Status = summary.Status,
            BookingDate = summary.BookingDate,
            ProjectId = summary.ProjectId,
            ProjectName = summary.ProjectName,
            UnitId = summary.UnitId,
            UnitNumber = summary.UnitNumber,
            BlockName = summary.BlockName,
            FileNumber = summary.FileNumber,
            SubType = summary.SubType,
            Area = summary.Area,
            PrimaryApplicantPartyId = summary.PrimaryApplicantPartyId,
            ApplicantName = summary.ApplicantName,
            ApplicantPhone = summary.ApplicantPhone,
            FatherOrGuardianName = summary.FatherOrGuardianName,
            SourcingChannel = summary.SourcingChannel,
            PartnerName = summary.PartnerName,
            SalesExecutiveName = summary.SalesExecutiveName,
            TotalConsideration = summary.TotalConsideration,
            TotalPaid = summary.TotalPaid,
            Outstanding = summary.Outstanding,
            OverdueAmount = summary.OverdueAmount,
            CollectionPercent = summary.CollectionPercent,
            CurrencyCode = summary.CurrencyCode,
            NextDueDate = summary.NextDueDate,
            NextDueAmount = summary.NextDueAmount,
            DaysOverdue = summary.DaysOverdue,
            IsDefaulting = summary.IsDefaulting,
            IsUnderLitigation = summary.IsUnderLitigation,
            KycStatus = summary.KycStatus,

            PropertyId = booking.PropertyId,
            PlotFileId = booking.PlotFileId,
            EnquiryId = booking.EnquiryId,
            SiteVisitId = booking.SiteVisitId,
            TokenReservationId = booking.TokenReservationId,
            ExpressionOfInterestId = booking.ExpressionOfInterestId,
            LeadRegistrationId = booking.LeadRegistrationId,
            CampaignId = booking.CampaignId,
            ListPrice = booking.ListPrice,
            DiscountAmount = booking.DiscountAmount,
            DiscountPercent = booking.DiscountPercent,
            NetSalePrice = booking.NetSalePrice,
            RatePerSqFt = booking.RatePerSqFt,
            TotalDemanded = booking.TotalDemanded,
            TotalSurcharge = booking.TotalSurcharge,
            TotalWaived = booking.TotalWaived,
            PaymentPlanId = booking.PaymentPlanId,
            AllotmentId = booking.AllotmentId,
            SaleAgreementId = booking.SaleAgreementId,
            ConfirmedAt = booking.ConfirmedAt,
            AgreementSignedOn = booking.AgreementSignedOn,
            PossessionOfferedOn = booking.PossessionOfferedOn,
            PossessionTakenOn = booking.PossessionTakenOn,
            RegisteredOn = booking.RegisteredOn,
            CancellationId = booking.CancellationId,
            TransferRequestId = booking.TransferRequestId,
            PreviousBookingId = booking.PreviousBookingId,
            DunningCaseId = booking.DunningCaseId,
            CustomerMortgageId = booking.CustomerMortgageId,
            Notes = booking.Notes,

            Applicants = booking.Applicants.OrderBy(a => a.SequenceNumber).Select(a => new BookingApplicantDto
            {
                Id = a.Id,
                PartyId = a.PartyId,
                Name = applicantNames.GetValueOrDefault(a.PartyId, "—"),
                FatherOrGuardianName = applicantDetails.GetValueOrDefault(a.PartyId)?.FatherOrGuardianName,
                Phone = applicantDetails.GetValueOrDefault(a.PartyId)?.PrimaryPhone,
                IdentityNumber = identities.GetValueOrDefault(a.PartyId),
                PhotoUrl = applicantDetails.GetValueOrDefault(a.PartyId)?.PhotoUrl,
                SequenceNumber = a.SequenceNumber,
                IsPrimary = a.IsPrimary,
                SharePercent = a.SharePercent,
                Role = a.Role,
                KycStatus = a.KycStatus,
                AddedOn = a.AddedOn,
                RemovedOn = a.RemovedOn,
            }).ToList(),

            ChargeLines = booking.ChargeLines.OrderBy(c => c.SortOrder).Select(c => new CostSheetLineDto
            {
                Kind = c.Kind,
                Label = c.Label,
                Basis = c.Basis,
                Rate = c.Rate,
                Amount = c.Amount,
                TaxPercent = c.TaxPercent,
                TaxAmount = c.TaxAmount,
                IsOptional = c.IsOptional,
                IsAccepted = c.IsAccepted,
                SortOrder = c.SortOrder,
            }).ToList(),

            StatusHistory = booking.StatusHistory.OrderByDescending(h => h.ChangedAt).Select(h => new BookingStatusHistoryDto
            {
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                ChangedAt = h.ChangedAt,
                Note = h.Note,
            }).ToList(),
        };

        detail.PaymentPlan = await money.GetPlanAsync(id);

        var demands = await money.GetDemandsAsync(new ListQueryDto { PageSize = 50 }, id, null);
        detail.Demands = demands.Data.ToList();

        var receipts = await money.GetReceiptsAsync(new ReceiptSearchDto { BookingId = id, PageSize = 50 });
        detail.Receipts = receipts.Data.ToList();

        var ledger = await money.GetLedgerAsync(id, null, new ListQueryDto { PageSize = 200 });
        detail.Ledger = ledger.Data.ToList();

        detail.Timeline = BuildTimeline(booking, detail);

        return detail;
    }

    private static List<TimelineEntryDto> BuildTimeline(Booking booking, BookingDetailDto detail)
    {
        var entries = new List<TimelineEntryDto>
        {
            new()
            {
                Id = booking.Id,
                OccurredAt = booking.BookingDate.ToDateTime(TimeOnly.MinValue),
                Kind = "status",
                Title = "Booked",
                Detail = $"{detail.UnitNumber ?? detail.FileNumber} — {booking.TotalConsideration:N0} {booking.CurrencyCode}",
                Icon = "how_to_reg",
                Tone = "brand",
                Amount = booking.TotalConsideration,
            },
        };

        entries.AddRange(detail.StatusHistory.Select(h => new TimelineEntryDto
        {
            OccurredAt = h.ChangedAt,
            Kind = "status",
            Title = $"{h.FromStatus} → {h.ToStatus}",
            Detail = h.Note,
            Icon = "swap_horiz",
        }));

        entries.AddRange(detail.Receipts.Select(r => new TimelineEntryDto
        {
            Id = r.Id,
            OccurredAt = r.ReceivedOn.ToDateTime(TimeOnly.MinValue),
            Kind = "money",
            Title = $"Receipt {r.ReceiptNumber}",
            Detail = $"{r.Instrument}",
            Icon = "payments",
            Tone = "success",
            Amount = r.Amount,
        }));

        entries.AddRange(detail.Demands.Select(d => new TimelineEntryDto
        {
            Id = d.Id,
            OccurredAt = d.IssuedOn.ToDateTime(TimeOnly.MinValue),
            Kind = "money",
            Title = $"Demand {d.DemandNumber}",
            Detail = $"due {d.DueDate:d MMM yyyy}",
            Icon = "request_quote",
            Tone = d.Balance > 0 ? "warning" : "muted",
            Amount = d.TotalAmount,
        }));

        return entries.OrderByDescending(e => e.OccurredAt).ToList();
    }

    /// <summary>
    /// What the wizard's last step shows: the computed price, the plan it would build, the
    /// approvals it will need and anything that blocks — before anything is written.
    /// </summary>
    public async Task<BookingPreviewDto> PreviewBookingAsync(BookingCreateDto dto)
    {
        var settings = await SettingsAsync();
        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.ProjectId)
            ?? throw new InvalidOperationException("That project does not exist.");

        if (dto.UnitId is null && dto.PlotFileId is null)
            throw new InvalidOperationException("Pick a unit or a plot file before continuing.");

        var costSheet = dto.UnitId is not null
            ? await inventory.GetCostSheetAsync(dto.UnitId.Value, dto.PaymentPlanTemplateId, dto.DiscountAmount, dto.DiscountPercent)
            : await PlotFileCostSheetAsync(dto);

        var templateId = dto.PaymentPlanTemplateId ?? project.DefaultPaymentPlanTemplateId;
        var plan = await money.PreviewPlanAsync(
            templateId, dto.CustomPlan, costSheet.GrandTotal,
            dto.BookingDate == default ? Today : dto.BookingDate, project.Id);

        var preview = new BookingPreviewDto { CostSheet = costSheet, PaymentPlan = plan };

        // Approvals: discount above the role's ceiling, and any plan that departs from the template.
        if (costSheet.DiscountAmount > 0m)
        {
            var discountPercent = RealEstateMapper.Percent(costSheet.DiscountAmount, costSheet.BasePrice + costSheet.PremiumTotal);
            if (discountPercent > settings.MaxDiscountPercentWithoutApproval)
            {
                preview.RequiresApproval = true;
                preview.ApprovalsRequired.Add(
                    $"Discount of {discountPercent:N1}% is above the {settings.MaxDiscountPercentWithoutApproval:N1}% limit.");
            }
        }

        if (dto.CustomPlan is not null)
        {
            preview.RequiresApproval = true;
            preview.ApprovalsRequired.Add("The payment plan departs from the project's template.");
        }

        // Gates: unit availability, KYC, and anything blocking on the property.
        var gate = new GateResultDto { Passed = true };

        if (dto.UnitId is not null)
        {
            var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == dto.UnitId);
            if (unit is null)
            {
                gate.Passed = false;
                gate.Message = "That unit does not exist.";
            }
            else if (unit.CurrentBookingId is not null)
            {
                gate.Passed = false;
                gate.Message = "This unit has already been booked.";
            }
            else if (unit.Status == PropertyStatus.Blocked)
            {
                gate.Passed = false;
                gate.Message = "This unit is blocked off-market.";
                gate.CanOverride = true;
                gate.OverrideRole = "SalesHead";
            }
            else if (unit.CurrentHoldId is not null && dto.HoldId != unit.CurrentHoldId)
            {
                var hold = await Db.UnitHolds.ForCompany(Tenant)
                    .FirstOrDefaultAsync(h => h.Id == unit.CurrentHoldId && h.Status == HoldStatus.Active);

                if (hold is not null && hold.ExpiresAt > DateTime.UtcNow)
                {
                    gate.Passed = false;
                    gate.Message = "This unit is held for somebody else. Release the hold first.";
                    gate.CanOverride = true;
                    gate.OverrideRole = "SalesHead";
                }
            }
        }

        if (settings.RequireKycBeforeCompletion && dto.PrimaryApplicantPartyId is not null)
        {
            var party = await Db.Parties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.PrimaryApplicantPartyId);
            if (party is not null && party.KycStatus != KycStatus.Verified)
            {
                gate.Failures.Add(new ChecklistItemDto
                {
                    Key = "kyc",
                    Label = "Customer KYC is not verified",
                    IsSatisfied = false,
                    IsMandatory = false,
                    Note = "The booking can be written, but it cannot complete until KYC clears.",
                });
            }

            if (party?.IsCautioned == true)
            {
                var blocking = await Db.CautionListEntries.ForCompany(Tenant)
                    .AnyAsync(c => c.PartyId == party.Id && c.IsActive && c.BlocksNewBusiness);

                if (blocking)
                {
                    gate.Passed = false;
                    gate.Message = "This customer is on the caution list and blocked from new business.";
                    gate.CanOverride = true;
                    gate.OverrideRole = "Compliance";
                }
            }
        }

        preview.Gate = gate;

        var escrowPercent = project.EscrowPercent ?? settings.DefaultEscrowPercent;
        if (settings.EscrowEnforced && escrowPercent > 0m)
        {
            preview.EscrowPortion = RealEstateMapper.Money(costSheet.GrandTotal * escrowPercent / 100m);
            preview.FreePortion = RealEstateMapper.Money(costSheet.GrandTotal - preview.EscrowPortion);
        }
        else preview.FreePortion = costSheet.GrandTotal;

        if (dto.ChannelPartnerId is not null)
        {
            var partner = await Db.ChannelPartners.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.ChannelPartnerId);
            preview.PartnerName = partner?.Name;

            var rate = await Db.PartnerCommissionRates.ForCompany(Tenant)
                .Where(r => r.ProjectId == project.Id && r.IsActive)
                .Where(r => r.ChannelPartnerId == dto.ChannelPartnerId || r.ChannelPartnerId == null)
                .Where(r => costSheet.GrandTotal >= r.FromValue && (r.ToValue == null || costSheet.GrandTotal <= r.ToValue))
                .OrderByDescending(r => r.ChannelPartnerId != null)
                .FirstOrDefaultAsync();

            if (rate is not null)
            {
                preview.CommissionEstimate = rate.FlatAmount > 0m
                    ? rate.FlatAmount
                    : rate.RatePerSqFt > 0m
                        ? RealEstateMapper.Money(rate.RatePerSqFt * costSheet.SaleableArea.SquareFeet)
                        : RealEstateMapper.Money(costSheet.GrandTotal * rate.CommissionPercent / 100m);
            }
        }

        return preview;
    }

    private async Task<CostSheetDto> PlotFileCostSheetAsync(BookingCreateDto dto)
    {
        var file = await Db.PlotFiles.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == dto.PlotFileId)
            ?? throw new InvalidOperationException("That plot file does not exist.");

        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == file.ProjectId);
        var areaUnit = await AreaUnitAsync(project?.OfficeId);
        var currency = project?.CurrencyCode ?? await CurrencyAsync();

        var price = dto.OverrideListPrice ?? file.Price;
        var discount = dto.DiscountAmount > 0m ? dto.DiscountAmount : RealEstateMapper.Money(price * dto.DiscountPercent / 100m);
        var net = RealEstateMapper.Money(price - discount);

        return new CostSheetDto
        {
            UnitNumber = file.FileNumber,
            ProjectName = project?.Name ?? "—",
            SubType = file.SubType,
            SaleableArea = RealEstateMapper.Area(file.NominalAreaSqFt, areaUnit),
            RatePerSqFt = file.NominalAreaSqFt == 0m ? 0m : RealEstateMapper.Money(price / file.NominalAreaSqFt),
            CurrencyCode = currency,
            BasePrice = price,
            DiscountAmount = discount,
            NetSalePrice = net,
            GrandTotal = net,
            AmountInWords = RealEstateMapper.AmountInWords(net, currency, RealEstateMapper.UsesIndianScale(currency)),
            GeneratedOn = Today,
            SalePriceLines =
            [
                new CostSheetLineDto
                {
                    Kind = ChargeKind.BasePrice,
                    Label = $"File price — {file.CategoryCode}",
                    Basis = ChargeBasis.Fixed,
                    Amount = price,
                },
            ],
        };
    }

    /// <summary>
    /// Writes the booking.
    ///
    /// One transaction: party, booking, frozen cost sheet, payment plan, first receipt, unit status
    /// and any approval the price needed. The cost-sheet lines are **copied**, not referenced, so
    /// re-pricing the project later cannot rewrite what the customer signed.
    /// </summary>
    public async Task<BookingDetailDto> CreateBookingAsync(BookingCreateDto dto, Guid userId)
    {
        var preview = await PreviewBookingAsync(dto);

        if (!preview.Gate.Passed && !preview.Gate.CanOverride)
            throw new InvalidOperationException(preview.Gate.Message ?? "This unit cannot be booked.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var settings = await SettingsAsync();
        var project = await Db.Projects.ForCompany(Tenant).FirstAsync(p => p.Id == dto.ProjectId);

        var partyId = dto.PrimaryApplicantPartyId;
        if (partyId is null && dto.NewApplicant is not null)
        {
            var party = await crm.SavePartyAsync(dto.NewApplicant, userId);
            partyId = party.Id;
        }

        if (partyId is null) throw new InvalidOperationException("A booking needs an applicant.");

        Unit? unit = null;
        if (dto.UnitId is not null)
        {
            // Re-read inside the transaction: two salespeople will race for the same unit.
            unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == dto.UnitId)
                ?? throw new InvalidOperationException("That unit does not exist.");

            if (unit.CurrentBookingId is not null)
                throw new InvalidOperationException("This unit was booked by somebody else a moment ago.");
        }

        var costSheet = preview.CostSheet;

        var booking = new Booking
        {
            Reference = await numbering.NextBookingNumberAsync(DateTime.UtcNow),
            ProjectId = dto.ProjectId,
            UnitId = dto.UnitId,
            PlotFileId = dto.PlotFileId,
            PropertyId = unit?.PropertyId,
            Status = BookingStatus.Provisional,
            BookingDate = dto.BookingDate == default ? Today : dto.BookingDate,
            PrimaryApplicantPartyId = partyId.Value,
            NomineePartyId = dto.NomineePartyId,
            SourcingChannel = dto.SourcingChannel,
            ChannelPartnerId = dto.ChannelPartnerId,
            LeadRegistrationId = dto.LeadRegistrationId,
            SalesExecutiveId = dto.SalesExecutiveId,
            EnquiryId = dto.EnquiryId,
            CampaignId = dto.CampaignId,
            SiteVisitId = dto.SiteVisitId,
            TokenReservationId = dto.TokenReservationId,
            ExpressionOfInterestId = dto.ExpressionOfInterestId,
            PriceListId = dto.PriceListId ?? unit?.PriceListId,
            ListPrice = costSheet.BasePrice + costSheet.PremiumTotal,
            DiscountAmount = costSheet.DiscountAmount,
            DiscountPercent = RealEstateMapper.Percent(costSheet.DiscountAmount, costSheet.BasePrice + costSheet.PremiumTotal),
            NetSalePrice = costSheet.NetSalePrice,
            TotalConsideration = costSheet.GrandTotal,
            CurrencyCode = costSheet.CurrencyCode,
            AreaSqFt = costSheet.SaleableArea.SquareFeet,
            RatePerSqFt = costSheet.RatePerSqFt,
            Outstanding = costSheet.GrandTotal,
            Notes = dto.Notes,
        }.StampNew(Tenant, userId);

        // Frozen cost sheet.
        foreach (var line in costSheet.SalePriceLines.Concat(costSheet.OtherChargeLines))
        {
            if (line.IsOptional && dto.DeclinedOptionalCharges.Contains(line.Kind)) continue;

            booking.ChargeLines.Add(new BookingChargeLine
            {
                Kind = line.Kind,
                Label = line.Label,
                Basis = line.Basis,
                Rate = line.Rate,
                Amount = line.Amount,
                TaxPercent = line.TaxPercent,
                TaxAmount = line.TaxAmount,
                IsPartOfSalePrice = costSheet.SalePriceLines.Contains(line),
                IsOptional = line.IsOptional,
                IsAccepted = true,
                SortOrder = line.SortOrder,
            }.StampNew(Tenant, userId));
        }

        booking.Applicants.Add(new BookingApplicant
        {
            PartyId = partyId.Value,
            SequenceNumber = 1,
            IsPrimary = true,
            SharePercent = dto.CoApplicants.Count == 0 ? 100m : 100m / (dto.CoApplicants.Count + 1),
            Role = "Applicant",
            AddedOn = booking.BookingDate,
        }.StampNew(Tenant, userId));

        var sequence = 2;
        foreach (var co in dto.CoApplicants)
        {
            booking.Applicants.Add(new BookingApplicant
            {
                PartyId = co.PartyId,
                SequenceNumber = sequence++,
                IsPrimary = false,
                SharePercent = co.SharePercent > 0m ? co.SharePercent : 100m / (dto.CoApplicants.Count + 1),
                Role = string.IsNullOrWhiteSpace(co.Role) ? "CoApplicant" : co.Role,
                AddedOn = booking.BookingDate,
            }.StampNew(Tenant, userId));
        }

        Db.Bookings.Add(booking);

        // The payment plan, expanded from the preview so what was shown is what is written.
        var plan = new PaymentPlan
        {
            BookingId = booking.Id,
            PaymentPlanTemplateId = dto.PaymentPlanTemplateId ?? project.DefaultPaymentPlanTemplateId,
            Reference = $"{booking.Reference}-P1",
            Version = 1,
            IsCurrent = true,
            IsCustom = dto.CustomPlan is not null,
            StartDate = preview.PaymentPlan.StartDate,
            EndDate = preview.PaymentPlan.EndDate,
            TotalAmount = preview.PaymentPlan.TotalAmount,
            Outstanding = preview.PaymentPlan.TotalAmount,
            SurchargePolicyId = project.DefaultSurchargePolicyId,
            DunningPolicyId = project.DefaultDunningPolicyId,
        }.StampNew(Tenant, userId);

        foreach (var i in preview.PaymentPlan.Instalments)
        {
            plan.Instalments.Add(new Instalment
            {
                BookingId = booking.Id,
                SequenceNumber = i.SequenceNumber,
                Kind = i.Kind,
                Label = i.Label,
                DueDate = i.DueDate,
                ProjectMilestoneId = i.ProjectMilestoneId,
                Amount = i.Amount,
                TaxAmount = i.TaxAmount,
                TotalAmount = i.TotalAmount,
                Balance = i.TotalAmount,
                Status = i.DueDate is not null && i.DueDate <= Today ? InstalmentStatus.Due : InstalmentStatus.NotDue,
                ChargeKind = i.ChargeKind,
            }.StampNew(Tenant, userId));
        }

        Db.PaymentPlans.Add(plan);
        booking.PaymentPlanId = plan.Id;

        if (unit is not null)
        {
            unit.CurrentBookingId = booking.Id;
            unit.CurrentHoldId = null;
            unit.Status = PropertyStatus.Booked;
            unit.StampUpdated(userId);

            var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == unit.PropertyId);
            if (property is not null)
            {
                property.Status = PropertyStatus.Booked;
                property.CurrentBookingId = booking.Id;
                property.StampUpdated(userId);
            }

            Db.UnitStatusHistories.Add(new UnitStatusHistory
            {
                UnitId = unit.Id,
                FromStatus = PropertyStatus.Available,
                ToStatus = PropertyStatus.Booked,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                SourceDocumentType = "Booking",
                SourceDocumentId = booking.Id,
            }.StampNew(Tenant, userId));
        }

        if (dto.PlotFileId is not null)
        {
            var file = await Db.PlotFiles.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == dto.PlotFileId);
            if (file is not null)
            {
                file.Status = PropertyStatus.Booked;
                file.CurrentBookingId = booking.Id;
                file.StampUpdated(userId);
            }
        }

        // Close out the hold, the token and the EOI this booking came from.
        if (dto.HoldId is not null)
        {
            var hold = await Db.UnitHolds.ForCompany(Tenant).FirstOrDefaultAsync(h => h.Id == dto.HoldId);
            if (hold is not null)
            {
                hold.Status = HoldStatus.Converted;
                hold.ConvertedToBookingId = booking.Id;
                hold.ReleasedAt = DateTime.UtcNow;
            }
        }

        if (dto.TokenReservationId is not null)
        {
            var token = await Db.TokenReservations.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == dto.TokenReservationId);
            if (token is not null)
            {
                token.Status = ReservationStatus.ConvertedToBooking;
                token.ConvertedBookingId = booking.Id;
            }
        }

        if (dto.ExpressionOfInterestId is not null)
        {
            var eoi = await Db.ExpressionOfInterests.ForCompany(Tenant)
                .FirstOrDefaultAsync(e => e.Id == dto.ExpressionOfInterestId);
            if (eoi is not null)
            {
                eoi.Status = ReservationStatus.ConvertedToBooking;
                eoi.ConvertedBookingId = booking.Id;
            }
        }

        if (dto.EnquiryId is not null)
        {
            var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == dto.EnquiryId);
            if (enquiry is not null)
            {
                enquiry.Stage = EnquiryStage.Booked;
                enquiry.ConvertedBookingId = booking.Id;
                enquiry.ClosedAt = DateTime.UtcNow;
                enquiry.StampUpdated(userId);
            }
        }

        if (dto.LeadRegistrationId is not null)
        {
            var registration = await Db.LeadRegistrations.ForCompany(Tenant)
                .FirstOrDefaultAsync(r => r.Id == dto.LeadRegistrationId);
            if (registration is not null)
            {
                registration.Status = LeadRegistrationStatus.Converted;
                registration.BookingId = booking.Id;
                registration.ConvertedOn = booking.BookingDate;
            }
        }

        // Approvals, raised against the booking so the board shows what is waiting on whom.
        if (preview.RequiresApproval)
        {
            var approval = await RaiseApprovalAsync(
                "Booking", booking.Id, booking.Reference, booking.TotalConsideration,
                string.Join(" ", preview.ApprovalsRequired), userId, project.Id,
                reasonCodeId: dto.DiscountReasonCodeId, note: dto.DiscountNote);

            booking.ApprovalRequestId = approval?.Id;
            if (approval is not null) booking.Status = BookingStatus.PendingApproval;
        }

        if (booking.DiscountAmount > 0m)
        {
            Db.BookingDiscounts.Add(new BookingDiscount
            {
                BookingId = booking.Id,
                DiscountType = "Negotiated",
                Percent = booking.DiscountPercent,
                Amount = booking.DiscountAmount,
                ReasonCodeId = dto.DiscountReasonCodeId,
                Note = dto.DiscountNote,
                RequestedByUserId = userId,
                ApprovalRequestId = booking.ApprovalRequestId,
                ApprovalOutcome = booking.ApprovalRequestId is null ? ApprovalOutcome.AutoApproved : ApprovalOutcome.Pending,
            }.StampNew(Tenant, userId));

            if (dto.DiscountReasonCodeId is not null)
            {
                await WriteAuditNoteAsync("Booking", booking.Id, "DiscountOverride", dto.DiscountReasonCodeId.Value, userId,
                    before: $"{booking.ListPrice:N0}", after: $"{booking.NetSalePrice:N0}",
                    amountImpact: booking.DiscountAmount, note: dto.DiscountNote,
                    approvalRequestId: booking.ApprovalRequestId, entityReference: booking.Reference);
            }
        }

        Db.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            FromStatus = BookingStatus.Provisional,
            ToStatus = booking.Status,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            Note = "Booking created",
        }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();

        if (dto.BookingPayment is not null)
        {
            dto.BookingPayment.BookingId = booking.Id;
            dto.BookingPayment.PartyId = partyId;
            dto.BookingPayment.ProjectId = project.Id;
            await money.CreateReceiptAsync(dto.BookingPayment, userId);
        }

        await transaction.CommitAsync();
        return (await GetBookingAsync(booking.Id))!;
    }

    public async Task<BookingDetailDto> ApproveBookingAsync(BookingApprovalDto dto, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        if (booking.Status != BookingStatus.PendingApproval)
            throw new InvalidOperationException("This booking is not waiting for approval.");

        var previous = booking.Status;

        if (dto.Outcome == ApprovalOutcome.Approved)
        {
            booking.Status = booking.TotalPaid > 0m ? BookingStatus.Confirmed : BookingStatus.Provisional;
            booking.ConfirmedAt = booking.TotalPaid > 0m ? DateTime.UtcNow : null;
        }
        else
        {
            booking.Status = BookingStatus.Cancelled;

            // Give the unit back the moment the booking is refused.
            if (booking.UnitId is not null)
            {
                var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == booking.UnitId);
                if (unit is not null && unit.CurrentBookingId == booking.Id)
                {
                    unit.CurrentBookingId = null;
                    unit.Status = PropertyStatus.Available;
                    unit.StampUpdated(userId);
                }
            }
        }

        Db.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            FromStatus = previous,
            ToStatus = booking.Status,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            Note = dto.Comment,
        }.StampNew(Tenant, userId));

        booking.StampUpdated(userId);
        await Db.SaveChangesAsync();

        return (await GetBookingAsync(booking.Id))!;
    }

    public async Task<BookingDetailDto> ConfirmBookingAsync(Guid bookingId, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        if (booking.Status == BookingStatus.PendingApproval)
            throw new InvalidOperationException("This booking is still waiting for approval.");

        if (booking.TotalPaid <= 0m)
            throw new InvalidOperationException(
                "Nothing has been received against this booking yet. A provisional booking is confirmed " +
                "when the booking amount clears.");

        var previous = booking.Status;
        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = DateTime.UtcNow;
        booking.StampUpdated(userId);

        Db.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            FromStatus = previous,
            ToStatus = BookingStatus.Confirmed,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            Note = "Booking amount received",
        }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();
        return (await GetBookingAsync(bookingId))!;
    }

    public async Task<BookingAmendmentDto> RequestAmendmentAsync(BookingAmendmentDto dto, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        var amendment = new BookingAmendment
        {
            BookingId = booking.Id,
            Reference = await numbering.NextMasterCodeAsync(Db.BookingAmendments, "AMD"),
            AmendmentType = dto.AmendmentType,
            BeforeValue = dto.BeforeValue,
            AfterValue = dto.AfterValue,
            Reason = dto.Reason,
            Fee = dto.Fee,
            RequestedByUserId = userId,
            RequestedOn = Today,
            Outcome = ApprovalOutcome.Pending,
            NewUnitId = dto.NewUnitId,
            PriceDifference = dto.PriceDifference,
        }.StampNew(Tenant, userId);

        var approval = await RaiseApprovalAsync(
            "BookingAmendment", amendment.Id, amendment.Reference, dto.Fee,
            $"{dto.AmendmentType} on {booking.Reference}", userId, booking.ProjectId, note: dto.Reason);

        amendment.ApprovalRequestId = approval?.Id;
        if (approval is null) amendment.Outcome = ApprovalOutcome.AutoApproved;

        Db.BookingAmendments.Add(amendment);
        await Db.SaveChangesAsync();

        if (amendment.Outcome == ApprovalOutcome.AutoApproved)
            await ApplyAmendmentAsync(amendment, userId);

        dto.Id = amendment.Id;
        dto.Reference = amendment.Reference;
        dto.Outcome = amendment.Outcome;
        return dto;
    }

    public async Task<BookingAmendmentDto> DecideAmendmentAsync(
        Guid amendmentId, ApprovalOutcome outcome, string? comment, Guid userId)
    {
        var amendment = await Db.BookingAmendments.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == amendmentId)
            ?? throw new InvalidOperationException("That amendment does not exist.");

        if (amendment.Outcome != ApprovalOutcome.Pending)
            throw new InvalidOperationException("This amendment has already been decided.");

        amendment.Outcome = outcome;
        amendment.Reason = string.IsNullOrWhiteSpace(comment) ? amendment.Reason : $"{amendment.Reason}\n{comment}".Trim();
        amendment.EffectiveFrom = Today;
        amendment.StampUpdated(userId);

        await Db.SaveChangesAsync();

        if (outcome == ApprovalOutcome.Approved) await ApplyAmendmentAsync(amendment, userId);

        return new BookingAmendmentDto
        {
            Id = amendment.Id,
            BookingId = amendment.BookingId,
            Reference = amendment.Reference,
            AmendmentType = amendment.AmendmentType,
            BeforeValue = amendment.BeforeValue,
            AfterValue = amendment.AfterValue,
            Reason = amendment.Reason,
            Fee = amendment.Fee,
            RequestedOn = amendment.RequestedOn,
            Outcome = amendment.Outcome,
            EffectiveFrom = amendment.EffectiveFrom,
            NewUnitId = amendment.NewUnitId,
            PriceDifference = amendment.PriceDifference,
        };
    }

    /// <summary>
    /// Applies an approved amendment. A unit change is the interesting one: the old unit goes back
    /// on the board, the new one comes off it, the price difference is settled and the plan is
    /// rebuilt — all of which is why it needs an approval in the first place.
    /// </summary>
    private async Task ApplyAmendmentAsync(BookingAmendment amendment, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant)
            .Include(b => b.Applicants)
            .FirstOrDefaultAsync(b => b.Id == amendment.BookingId);

        if (booking is null) return;

        switch (amendment.AmendmentType)
        {
            case "UnitChange" when amendment.NewUnitId is not null:
                var oldUnit = booking.UnitId is null
                    ? null
                    : await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == booking.UnitId);

                var newUnit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == amendment.NewUnitId)
                    ?? throw new InvalidOperationException("The replacement unit does not exist.");

                if (newUnit.CurrentBookingId is not null)
                    throw new InvalidOperationException("The replacement unit has already been booked.");

                if (oldUnit is not null)
                {
                    oldUnit.CurrentBookingId = null;
                    oldUnit.Status = PropertyStatus.Available;
                    oldUnit.StampUpdated(userId);
                }

                newUnit.CurrentBookingId = booking.Id;
                newUnit.Status = PropertyStatus.Booked;
                newUnit.StampUpdated(userId);

                booking.UnitId = newUnit.Id;
                booking.PropertyId = newUnit.PropertyId;
                booking.TotalConsideration += amendment.PriceDifference ?? 0m;
                booking.Outstanding += amendment.PriceDifference ?? 0m;
                break;

            case "AddApplicant" when amendment.PriceDifference is null:
                // Handled by the caller adding the applicant row; nothing further to do here.
                break;

            case "NameCorrection":
            case "AddressChange":
            case "ChangeNominee":
                // These change the party record rather than the booking, and are applied there.
                break;
        }

        if (amendment.Fee > 0m)
        {
            Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                PartyId = booking.PrimaryApplicantPartyId,
                BookingId = booking.Id,
                EntryDate = Today,
                Kind = LedgerEntryKind.Adjustment,
                Description = $"{amendment.AmendmentType} fee — {amendment.Reference}",
                DebitAmount = amendment.Fee,
                CurrencyCode = booking.CurrencyCode,
            }.StampNew(Tenant, userId));
        }

        booking.StampUpdated(userId);
        await Db.SaveChangesAsync();
    }

    // ═══ Allotment ═══════════════════════════════════════════════════════════

    public async Task<AllotmentDto> IssueAllotmentAsync(Guid bookingId, Guid? templateId, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        if (booking.Status is BookingStatus.Provisional or BookingStatus.PendingApproval)
            throw new InvalidOperationException(
                "An allotment letter is issued once the booking is confirmed. Confirm it first.");

        if (booking.AllotmentId is not null)
            throw new InvalidOperationException("An allotment letter has already been issued. Re-issue it instead.");

        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == booking.ProjectId);

        var allotment = new Allotment
        {
            BookingId = booking.Id,
            AllotmentNumber = await numbering.NextAllotmentNumberAsync(DateTime.UtcNow),
            Status = AllotmentStatus.Issued,
            IssuedOn = Today,
            IssuedByUserId = userId,
            TemplateVersionId = templateId,
            PossessionTargetDate = project?.PromisedPossessionDate,
            Version = 1,
        }.StampNew(Tenant, userId);

        Db.Allotments.Add(allotment);
        booking.AllotmentId = allotment.Id;
        booking.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await MapAllotmentsAsync([allotment]))[0];
    }

    public async Task<AllotmentDto> ReissueAllotmentAsync(Guid allotmentId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Re-issuing an allotment letter needs a reason.");

        var existing = await Db.Allotments.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == allotmentId)
            ?? throw new InvalidOperationException("That allotment does not exist.");

        existing.Status = AllotmentStatus.Superseded;
        existing.StampUpdated(userId);

        var reissued = new Allotment
        {
            BookingId = existing.BookingId,
            AllotmentNumber = await numbering.NextAllotmentNumberAsync(DateTime.UtcNow),
            Status = AllotmentStatus.Reissued,
            IssuedOn = Today,
            IssuedByUserId = userId,
            TemplateVersionId = existing.TemplateVersionId,
            PossessionTargetDate = existing.PossessionTargetDate,
            Version = existing.Version + 1,
            SupersedesAllotmentId = existing.Id,
            ReissueReason = reason,
        }.StampNew(Tenant, userId);

        Db.Allotments.Add(reissued);

        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == existing.BookingId);
        if (booking is not null)
        {
            booking.AllotmentId = reissued.Id;
            booking.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return (await MapAllotmentsAsync([reissued]))[0];
    }

    public async Task<PaginatedResponse<AllotmentDto>> GetAllotmentsAsync(ListQueryDto query, Guid? projectId)
    {
        var q = (
            from a in Db.Allotments.ForCompany(Tenant)
            join b in Db.Bookings.ForCompany(Tenant) on a.BookingId equals b.Id
            select new { Allotment = a, Booking = b })
            .WhereIf(projectId.HasValue, x => x.Booking.ProjectId == projectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), x => x.Allotment.AllotmentNumber.Contains(query.Search!))
            .OrderByDescending(x => x.Allotment.IssuedOn)
            .Select(x => x.Allotment);

        return await PageAsync(q, query, MapAllotmentsAsync);
    }

    private async Task<List<AllotmentDto>> MapAllotmentsAsync(List<Allotment> allotments)
    {
        if (allotments.Count == 0) return [];

        var bookingIds = allotments.Select(a => a.BookingId).Distinct().ToList();
        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => bookingIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b);

        var names = await PartyNamesAsync(bookings.Values.Select(b => b.PrimaryApplicantPartyId));
        var projects = await ProjectNamesAsync(bookings.Values.Select(b => (Guid?)b.ProjectId));

        var unitIds = bookings.Values.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).ToList();
        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        return allotments.Select(a =>
        {
            var booking = bookings.GetValueOrDefault(a.BookingId);

            return new AllotmentDto
            {
                Id = a.Id,
                BookingId = a.BookingId,
                BookingReference = booking?.Reference ?? "—",
                AllotmentNumber = a.AllotmentNumber,
                Status = a.Status,
                IssuedOn = a.IssuedOn,
                UnitNumber = booking?.UnitId is null ? null : units.GetValueOrDefault(booking.UnitId.Value),
                ProjectName = booking is null ? "—" : projects.GetValueOrDefault(booking.ProjectId, "—"),
                ApplicantName = booking is null ? "—" : names.GetValueOrDefault(booking.PrimaryApplicantPartyId, "—"),
                PossessionTargetDate = a.PossessionTargetDate,
                DocumentUrl = a.DocumentUrl,
                Version = a.Version,
                SupersedesAllotmentId = a.SupersedesAllotmentId,
                ReissueReason = a.ReissueReason,
                IsDelivered = a.IsDelivered,
                DeliveredOn = a.DeliveredOn,
                DeliveryReference = a.DeliveryReference,
            };
        }).ToList();
    }

    // ═══ Agreements ══════════════════════════════════════════════════════════

    public async Task<SaleAgreementDto> GenerateAgreementAsync(
        Guid bookingId, Guid? templateId, string languageCode, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        var clauses = await Db.ClauseLibraryItems.ForCompany(Tenant)
            .Where(c => c.IsActive && (c.ProjectId == null || c.ProjectId == booking.ProjectId))
            .Where(c => c.LanguageCode == languageCode)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var agreement = new SaleAgreement
        {
            BookingId = booking.Id,
            AgreementNumber = await numbering.NextAgreementNumberAsync(DateTime.UtcNow),
            AgreementType = "AgreementToSell",
            TemplateVersionId = templateId,
            LanguageCode = languageCode,
        }.StampNew(Tenant, userId);

        // Clauses are snapshotted, not referenced — the library moves on, the signed contract must not.
        foreach (var clause in clauses)
        {
            agreement.Clauses.Add(new AgreementClause
            {
                ClauseLibraryItemId = clause.Id,
                ClauseKey = clause.ClauseKey,
                Heading = clause.Heading,
                Body = clause.Body,
                SortOrder = clause.SortOrder,
            }.StampNew(Tenant, userId));
        }

        Db.SaleAgreements.Add(agreement);
        booking.SaleAgreementId = agreement.Id;
        booking.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return MapAgreement(agreement);
    }

    public async Task<SaleAgreementDto> RecordExecutionAsync(
        Guid agreementId, DateOnly executedOn, string? registrationNumber, Guid userId)
    {
        var agreement = await Db.SaleAgreements.ForCompany(Tenant)
            .Include(a => a.Clauses)
            .FirstOrDefaultAsync(a => a.Id == agreementId)
            ?? throw new InvalidOperationException("That agreement does not exist.");

        agreement.ExecutedOn = executedOn;
        agreement.IsSigned = true;

        if (!string.IsNullOrWhiteSpace(registrationNumber))
        {
            agreement.RegistrationNumber = registrationNumber;
            agreement.RegisteredOn = executedOn;
        }

        agreement.StampUpdated(userId);

        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == agreement.BookingId);
        if (booking is not null)
        {
            var previous = booking.Status;
            booking.AgreementSignedOn = executedOn;
            if (booking.Status == BookingStatus.Confirmed) booking.Status = BookingStatus.AgreementSigned;
            booking.StampUpdated(userId);

            Db.BookingStatusHistories.Add(new BookingStatusHistory
            {
                BookingId = booking.Id,
                FromStatus = previous,
                ToStatus = booking.Status,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                Note = $"Agreement {agreement.AgreementNumber} executed",
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();
        return MapAgreement(agreement);
    }

    private static SaleAgreementDto MapAgreement(SaleAgreement a) => new()
    {
        Id = a.Id,
        BookingId = a.BookingId,
        AgreementNumber = a.AgreementNumber,
        AgreementType = a.AgreementType,
        ExecutedOn = a.ExecutedOn,
        RegisteredOn = a.RegisteredOn,
        RegistrationNumber = a.RegistrationNumber,
        RegistrarOffice = a.RegistrarOffice,
        StampDuty = a.StampDuty,
        RegistrationFee = a.RegistrationFee,
        LanguageCode = a.LanguageCode,
        DocumentUrl = a.DocumentUrl,
        IsSigned = a.IsSigned,
        IsSuperseded = a.IsSuperseded,
        SignatureSessionId = a.SignatureSessionId,
        Clauses = a.Clauses.OrderBy(c => c.SortOrder).Select(c => new AgreementClauseDto
        {
            Id = c.Id,
            ClauseKey = c.ClauseKey,
            Heading = c.Heading,
            Body = c.Body,
            SortOrder = c.SortOrder,
            IsNonStandard = c.IsNonStandard,
        }).ToList(),
    };

    // ═══ Balloting ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<BallotDto>> GetBallotsAsync(ListQueryDto query, Guid? projectId)
    {
        var q = Db.Ballots.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, b => b.ProjectId == projectId)
            .OrderByDescending(b => b.ScheduledOn);

        var projects = await ProjectNamesAsync([projectId]);

        return await PageAsync(q, query, b => new BallotDto
        {
            Id = b.Id,
            Reference = b.Reference,
            Name = b.Name,
            ProjectId = b.ProjectId,
            ProjectName = projects.GetValueOrDefault(b.ProjectId, "—"),
            Status = b.Status,
            ScheduledOn = b.ScheduledOn,
            DrawnAt = b.DrawnAt,
            PublishedAt = b.PublishedAt,
            EntryCount = b.EntryCount,
            PrizeCount = b.PrizeCount,
            AllocatedCount = b.AllocatedCount,
            UnallocatedCount = Math.Max(0, b.EntryCount - b.AllocatedCount),
            WitnessNames = b.WitnessNames,
            VideoUrl = b.VideoUrl,
            ResultDocumentUrl = b.ResultDocumentUrl,
            IsSupplementary = b.IsSupplementary,
            RandomSeed = b.Status == BallotStatus.Draft ? null : b.RandomSeed,
            PoolHash = b.PoolHash,
        });
    }

    public async Task<BallotDto?> GetBallotAsync(Guid id)
    {
        var ballot = await Db.Ballots.ForCompany(Tenant)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (ballot is null) return null;

        var projects = await ProjectNamesAsync([ballot.ProjectId]);
        var areaUnit = await AreaUnitAsync();

        var allocatedByCategory = await Db.BallotEntries.ForCompany(Tenant)
            .Where(e => e.BallotId == id && e.AllottedUnitId != null)
            .GroupBy(e => e.BallotCategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        return new BallotDto
        {
            Id = ballot.Id,
            Reference = ballot.Reference,
            Name = ballot.Name,
            ProjectId = ballot.ProjectId,
            ProjectName = projects.GetValueOrDefault(ballot.ProjectId, "—"),
            Status = ballot.Status,
            ScheduledOn = ballot.ScheduledOn,
            DrawnAt = ballot.DrawnAt,
            PublishedAt = ballot.PublishedAt,
            EntryCount = ballot.EntryCount,
            PrizeCount = ballot.PrizeCount,
            AllocatedCount = ballot.AllocatedCount,
            UnallocatedCount = Math.Max(0, ballot.EntryCount - ballot.AllocatedCount),
            WitnessNames = ballot.WitnessNames,
            VideoUrl = ballot.VideoUrl,
            ResultDocumentUrl = ballot.ResultDocumentUrl,
            IsSupplementary = ballot.IsSupplementary,
            RandomSeed = ballot.Status == BallotStatus.Draft ? null : ballot.RandomSeed,
            PoolHash = ballot.PoolHash,
            Categories = ballot.Categories.OrderBy(c => c.SortOrder).Select(c => new BallotCategoryDto
            {
                Id = c.Id,
                Code = c.Code ?? string.Empty,
                Name = c.Name,
                NominalArea = RealEstateMapper.Area(c.NominalAreaSqFt, areaUnit),
                QuotaType = c.QuotaType,
                ReservedCount = c.ReservedCount,
                EntryCount = c.EntryCount,
                PrizeCount = c.PrizeCount,
                AllocatedCount = allocatedByCategory.FirstOrDefault(a => a.Key == c.Id)?.Count ?? 0,
                SortOrder = c.SortOrder,
            }).ToList(),
        };
    }

    public async Task<BallotDto> SaveBallotAsync(BallotCreateDto dto, Guid userId)
    {
        var ballot = new Ballot
        {
            ProjectId = dto.ProjectId,
            Reference = await numbering.NextBallotNumberAsync(DateTime.UtcNow),
            Name = dto.Name,
            Status = BallotStatus.Draft,
            ScheduledOn = dto.ScheduledOn,
            IsSupplementary = dto.IsSupplementary,
            ParentBallotId = dto.ParentBallotId,
        }.StampNew(Tenant, userId);

        foreach (var c in dto.Categories)
        {
            var category = new BallotCategory
            {
                Code = c.Code,
                Name = c.Name,
                NominalAreaSqFt = RealEstateMapper.ToSquareFeet(c.NominalArea, c.InputAreaUnit),
                QuotaType = c.QuotaType,
                ReservedCount = c.ReservedCount,
                PrizeCount = c.PrizeUnitIds.Count,
                SortOrder = c.SortOrder,
            }.StampNew(Tenant, userId);

            ballot.Categories.Add(category);
            ballot.PrizeCount += c.PrizeUnitIds.Count;
        }

        Db.Ballots.Add(ballot);
        await Db.SaveChangesAsync();

        return (await GetBallotAsync(ballot.Id))!;
    }

    /// <summary>
    /// Freezes the pool. From this point the entry list cannot change, and its hash is recorded so
    /// nobody can later claim the pool was different when the draw ran.
    /// </summary>
    public async Task<BallotDto> LockPoolAsync(Guid ballotId, Guid userId)
    {
        var ballot = await Db.Ballots.ForCompany(Tenant)
            .Include(b => b.Categories)
            .Include(b => b.Entries)
            .FirstOrDefaultAsync(b => b.Id == ballotId)
            ?? throw new InvalidOperationException("That ballot does not exist.");

        if (ballot.Status != BallotStatus.Draft)
            throw new InvalidOperationException("This ballot's pool is already locked.");

        // Every confirmed file in the project that has not already been allotted a plot.
        var files = await Db.PlotFiles.ForCompany(Tenant)
            .Where(f => f.ProjectId == ballot.ProjectId && f.AllottedUnitId == null && !f.IsCancelled)
            .Where(f => f.CurrentBookingId != null)
            .ToListAsync();

        var bookingIds = files.Where(f => f.CurrentBookingId.HasValue).Select(f => f.CurrentBookingId!.Value).ToList();
        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => bookingIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b);

        var eois = await Db.ExpressionOfInterests.ForCompany(Tenant)
            .Where(e => e.ProjectId == ballot.ProjectId && e.ConvertedBookingId != null)
            .ToDictionaryAsync(e => e.ConvertedBookingId!.Value, e => e.PriorityNumber);

        var sequence = 0;

        foreach (var file in files.OrderBy(f => f.IssuedOn).ThenBy(f => f.FileNumber))
        {
            var booking = file.CurrentBookingId is null ? null : bookings.GetValueOrDefault(file.CurrentBookingId.Value);
            if (booking is null) continue;

            var category = ballot.Categories.FirstOrDefault(c => c.Code == file.CategoryCode);

            // A file behind on its plan is not eligible, and the reason is on the record.
            var eligible = booking.Status != BookingStatus.Cancelled;
            var reason = booking.OverdueAmount > 0m ? "Instalments overdue" : null;
            if (reason is not null) eligible = false;

            ballot.Entries.Add(new BallotEntry
            {
                BallotCategoryId = category?.Id,
                PlotFileId = file.Id,
                BookingId = booking.Id,
                PartyId = booking.PrimaryApplicantPartyId,
                FileNumber = file.FileNumber,
                SequenceInPool = ++sequence,
                PriorityNumber = eois.GetValueOrDefault(booking.Id),
                IsEligible = eligible,
                IneligibilityReason = reason,
            }.StampNew(Tenant, userId));

            if (category is not null) category.EntryCount++;
        }

        ballot.EntryCount = ballot.Entries.Count;
        ballot.Status = BallotStatus.PoolLocked;

        // Hash of the locked list. Cheap, and it settles any later argument about the pool.
        var poolText = string.Join("|", ballot.Entries.OrderBy(e => e.SequenceInPool).Select(e => $"{e.SequenceInPool}:{e.FileNumber}"));
        ballot.PoolHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(poolText)))[..32];

        ballot.StampUpdated(userId);
        await Db.SaveChangesAsync();

        return (await GetBallotAsync(ballotId))!;
    }

    /// <summary>
    /// Runs the draw.
    ///
    /// Seeded and therefore **reproducible**: given the same seed and the same locked pool, the
    /// result is identical every time. That is the only thing that makes a ballot defensible when
    /// somebody challenges it, and it is why the seed is stored rather than discarded.
    /// </summary>
    public async Task<BallotResultDto> DrawAsync(BallotDrawDto dto, Guid userId)
    {
        var ballot = await Db.Ballots.ForCompany(Tenant)
            .Include(b => b.Categories)
            .Include(b => b.Entries)
            .FirstOrDefaultAsync(b => b.Id == dto.BallotId)
            ?? throw new InvalidOperationException("That ballot does not exist.");

        if (ballot.Status == BallotStatus.Draft)
            throw new InvalidOperationException("Lock the pool before drawing.");

        if (ballot.Status is BallotStatus.Drawn or BallotStatus.Published && !dto.DryRun)
            throw new InvalidOperationException("This ballot has already been drawn.");

        var seed = string.IsNullOrWhiteSpace(dto.Seed)
            ? Guid.NewGuid().ToString("N")[..16]
            : dto.Seed.Trim();

        // Deterministic from the seed alone — no clock, no Guid, nothing a re-run could differ on.
        var random = new Random(BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(seed)), 0));

        var prizeUnits = await Db.Units.ForCompany(Tenant)
            .Where(u => u.ProjectId == ballot.ProjectId && u.Status == PropertyStatus.Available && u.PlotFileId == null)
            .OrderBy(u => u.UnitNumber)
            .ToListAsync();

        var results = new List<BallotEntry>();
        var drawOrder = 0;

        foreach (var category in ballot.Categories.OrderBy(c => c.SortOrder))
        {
            var pool = ballot.Entries
                .Where(e => e.BallotCategoryId == category.Id && e.IsEligible && e.AllottedUnitId is null)
                .OrderBy(e => e.PriorityNumber ?? int.MaxValue).ThenBy(e => e.SequenceInPool)
                .ToList();

            var available = prizeUnits
                .Where(u => results.All(r => r.AllottedUnitId != u.Id))
                .Where(u => category.NominalAreaSqFt == 0m || true)
                .ToList();

            // Fisher-Yates over the eligible entries, driven by the seeded generator.
            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            for (var i = 0; i < pool.Count && i < available.Count; i++)
            {
                var entry = pool[i];
                var unit = available[i];

                entry.AllottedUnitId = unit.Id;
                entry.AllottedPlotNumber = unit.UnitNumber;
                entry.DrawOrder = ++drawOrder;
                results.Add(entry);
            }
        }

        var result = new BallotResultDto
        {
            BallotId = ballot.Id,
            Seed = seed,
            PoolHash = ballot.PoolHash ?? string.Empty,
            DrawnAt = DateTime.UtcNow,
            AllocatedCount = results.Count,
            UnallocatedEntries = ballot.Entries.Count(e => e.IsEligible) - results.Count,
            UnallocatedPrizes = Math.Max(0, prizeUnits.Count - results.Count),
            Results = await MapEntriesAsync(results),
        };

        if (dto.DryRun)
        {
            // Undo the in-memory allocation so a preview never leaks into the record.
            foreach (var entry in results)
            {
                entry.AllottedUnitId = null;
                entry.AllottedPlotNumber = null;
                entry.DrawOrder = null;
            }
            return result;
        }

        foreach (var entry in results)
        {
            Db.BallotResults.Add(new BallotResult
            {
                BallotId = ballot.Id,
                BallotEntryId = entry.Id,
                UnitId = entry.AllottedUnitId!.Value,
                DrawOrder = entry.DrawOrder!.Value,
                DrawnAt = result.DrawnAt,
                ClaimDeadline = Today.AddDays(30),
            }.StampNew(Tenant, userId));

            // The file becomes a numbered plot; both numbers travel together from here on.
            if (entry.PlotFileId is not null)
            {
                var file = await Db.PlotFiles.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == entry.PlotFileId);
                if (file is not null)
                {
                    file.BallotId = ballot.Id;
                    file.AllottedUnitId = entry.AllottedUnitId;
                    file.AllottedPlotNumber = entry.AllottedPlotNumber;
                    file.AllottedOn = Today;
                    file.StampUpdated(userId);
                }
            }

            var unit = prizeUnits.First(u => u.Id == entry.AllottedUnitId);
            unit.Status = PropertyStatus.Booked;
            unit.CurrentBookingId = entry.BookingId;
            unit.PlotFileId = entry.PlotFileId;
            unit.StampUpdated(userId);

            if (entry.BookingId is not null)
            {
                var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == entry.BookingId);
                if (booking is not null)
                {
                    booking.UnitId = unit.Id;
                    booking.PropertyId = unit.PropertyId;
                    booking.StampUpdated(userId);
                }
            }
        }

        ballot.RandomSeed = seed;
        ballot.DrawnAt = result.DrawnAt;
        ballot.Status = BallotStatus.Drawn;
        ballot.AllocatedCount = results.Count;
        ballot.ConductedByUserId = userId;
        ballot.WitnessNames = dto.WitnessNames;
        ballot.VideoUrl = dto.VideoUrl;
        ballot.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return result;
    }

    public async Task<BallotDto> PublishBallotAsync(Guid ballotId, Guid userId)
    {
        var ballot = await Db.Ballots.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == ballotId)
            ?? throw new InvalidOperationException("That ballot does not exist.");

        if (ballot.Status != BallotStatus.Drawn)
            throw new InvalidOperationException("Draw the ballot before publishing the result.");

        ballot.Status = BallotStatus.Published;
        ballot.PublishedAt = DateTime.UtcNow;
        ballot.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetBallotAsync(ballotId))!;
    }

    public async Task<List<BallotEntryDto>> GetBallotEntriesAsync(Guid ballotId, Guid? categoryId)
    {
        var entries = await Db.BallotEntries.ForCompany(Tenant)
            .Where(e => e.BallotId == ballotId)
            .WhereIf(categoryId.HasValue, e => e.BallotCategoryId == categoryId)
            .OrderBy(e => e.DrawOrder ?? int.MaxValue).ThenBy(e => e.SequenceInPool)
            .ToListAsync();

        return await MapEntriesAsync(entries);
    }

    private async Task<List<BallotEntryDto>> MapEntriesAsync(List<BallotEntry> entries)
    {
        if (entries.Count == 0) return [];

        var names = await PartyNamesAsync(entries.Select(e => e.PartyId));
        var details = await Db.Parties.ForCompany(Tenant)
            .Where(p => entries.Select(e => e.PartyId).Contains(p.Id))
            .Select(p => new { p.Id, p.FatherOrGuardianName })
            .ToDictionaryAsync(p => p.Id, p => p.FatherOrGuardianName);

        var identities = await Db.PartyIdentities.ForCompany(Tenant)
            .Where(i => entries.Select(e => e.PartyId).Contains(i.PartyId) && i.IsPrimary)
            .ToDictionaryAsync(i => i.PartyId, i => i.Number);

        var categoryIds = entries.Where(e => e.BallotCategoryId.HasValue).Select(e => e.BallotCategoryId!.Value).Distinct().ToList();
        var categories = categoryIds.Count == 0
            ? []
            : await Db.BallotCategories.ForCompany(Tenant).Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Code);

        var results = await Db.BallotResults.ForCompany(Tenant)
            .Where(r => entries.Select(e => e.Id).Contains(r.BallotEntryId))
            .ToDictionaryAsync(r => r.BallotEntryId, r => r);

        return entries.Select(e =>
        {
            var result = results.GetValueOrDefault(e.Id);

            return new BallotEntryDto
            {
                Id = e.Id,
                BallotCategoryId = e.BallotCategoryId,
                CategoryCode = e.BallotCategoryId is null ? null : categories.GetValueOrDefault(e.BallotCategoryId.Value),
                PlotFileId = e.PlotFileId,
                FileNumber = e.FileNumber,
                BookingId = e.BookingId,
                PartyId = e.PartyId,
                PartyName = names.GetValueOrDefault(e.PartyId, "—"),
                FatherOrGuardianName = details.GetValueOrDefault(e.PartyId),
                IdentityNumber = identities.GetValueOrDefault(e.PartyId),
                SequenceInPool = e.SequenceInPool,
                PriorityNumber = e.PriorityNumber,
                IsEligible = e.IsEligible,
                IneligibilityReason = e.IneligibilityReason,
                AllottedUnitId = e.AllottedUnitId,
                AllottedPlotNumber = e.AllottedPlotNumber,
                DrawOrder = e.DrawOrder,
                WasManuallyAssigned = e.WasManuallyAssigned,
                OverrideReason = e.OverrideReason,
                IsClaimed = result?.IsClaimed ?? false,
                ClaimDeadline = result?.ClaimDeadline,
            };
        }).ToList();
    }

    /// <summary>
    /// Overrides a draw result by hand. Possible, because real ballots need corrections — but never
    /// silent: who did it and why is written to the entry and to the audit log.
    /// </summary>
    public async Task<BallotEntryDto> OverrideAllocationAsync(Guid entryId, Guid unitId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Overriding a ballot result needs a reason.");

        var entry = await Db.BallotEntries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == entryId)
            ?? throw new InvalidOperationException("That ballot entry does not exist.");

        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == unitId)
            ?? throw new InvalidOperationException("That unit does not exist.");

        if (unit.CurrentBookingId is not null && unit.CurrentBookingId != entry.BookingId)
            throw new InvalidOperationException("That unit has already been allotted to somebody else.");

        var before = entry.AllottedPlotNumber;

        entry.AllottedUnitId = unit.Id;
        entry.AllottedPlotNumber = unit.UnitNumber;
        entry.WasManuallyAssigned = true;
        entry.OverrideByUserId = userId;
        entry.OverrideReason = reason;
        entry.StampUpdated(userId);

        unit.Status = PropertyStatus.Booked;
        unit.CurrentBookingId = entry.BookingId;
        unit.StampUpdated(userId);

        Db.RealEstateAuditNotes.Add(new RealEstateAuditNote
        {
            EntityType = "BallotEntry",
            EntityId = entry.Id,
            EntityReference = entry.FileNumber,
            ActionKey = "BallotOverride",
            BeforeValue = before,
            AfterValue = unit.UnitNumber,
            ReasonCodeId = Guid.Empty,
            Note = reason,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow,
            IsHighRisk = true,
        }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();
        return (await MapEntriesAsync([entry]))[0];
    }
}
