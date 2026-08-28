using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Deals from agreement through to completion, and the chains they sit in.
///
/// A deal is not a sale — it is a sale that might happen. Somewhere between a quarter and a third
/// of them collapse, and the ones that collapse do so for reasons that were visible weeks earlier:
/// a solicitor who stopped answering, a survey nobody chased, a link in the chain that went quiet.
/// So the checklist is the product here, not decoration. Every step has an owner, a due date and a
/// flag saying whether it blocks, and the board sorts by how long a deal has been stuck rather
/// than by when it was agreed.
/// </summary>
public partial class BrokerageService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), IBrokerageService
{
    /// <summary>
    /// The steps a sale goes through between agreement and completion. Seeded on every new deal so
    /// that nobody has to remember them, and so two negotiators run the same process.
    /// </summary>
    private static readonly (string Key, string Label, DealPartyRole? Owner, int OffsetDays, bool Mandatory, bool Blocking)[]
        StandardChecklist =
    [
        ("memorandum", "Memorandum of sale issued", null, 1, true, true),
        ("id-checks", "Identity and source of funds checked", null, 3, true, true),
        ("solicitors", "Solicitors instructed both sides", DealPartyRole.BuyerSolicitor, 5, true, true),
        ("draft-contract", "Draft contract issued", DealPartyRole.SellerSolicitor, 10, true, true),
        ("searches", "Searches ordered", DealPartyRole.BuyerSolicitor, 12, true, false),
        ("mortgage-application", "Mortgage application submitted", DealPartyRole.MortgageBroker, 7, false, false),
        ("survey", "Survey booked", DealPartyRole.Surveyor, 14, false, false),
        ("survey-report", "Survey report received", DealPartyRole.Surveyor, 21, false, false),
        ("mortgage-offer", "Mortgage offer issued", DealPartyRole.Lender, 28, false, true),
        ("enquiries", "Pre-contract enquiries answered", DealPartyRole.SellerSolicitor, 30, true, true),
        ("title-approved", "Title approved", DealPartyRole.BuyerSolicitor, 35, true, true),
        ("exchange", "Contracts exchanged", null, 42, true, true),
        ("completion-funds", "Completion funds requested", DealPartyRole.BuyerSolicitor, 50, true, true),
        ("completion", "Completion", null, 56, true, true),
        ("keys", "Keys released", null, 56, true, false),
        ("registration", "Registration and mutation", DealPartyRole.Registrar, 90, false, false),
        ("fee-invoiced", "Our fee invoiced", null, 56, true, false),
        ("fee-received", "Our fee received", null, 70, true, false),
    ];

    // ═══ Deals ═══════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<DealListItemDto>> GetDealsAsync(
        ListQueryDto query, DealStatus? status, Guid? agentId)
    {
        var q = Db.Deals.ForCompany(Tenant)
            .WhereIf(status.HasValue, d => d.Status == status)
            .WhereIf(agentId.HasValue, d => d.ListingAgentId == agentId || d.SellingAgentId == agentId)
            .WhereIf(query.OfficeId.HasValue, d => d.OfficeId == query.OfficeId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), d => d.Reference.Contains(query.Search!))

            // Stalled first. A board sorted by agreement date buries the deal that has not moved
            // in six weeks, which is the only one anybody needs to do something about today.
            .OrderByDescending(d => d.DaysSinceLastMilestone ?? 0)
            .ThenBy(d => d.TargetCompletionDate ?? DateOnly.MaxValue);

        return await PageAsync(q, query, MapDealListAsync);
    }

    private async Task<List<DealListItemDto>> MapDealListAsync(List<Deal> deals)
    {
        if (deals.Count == 0) return [];

        var currency = await CurrencyAsync();
        var ids = deals.Select(d => d.Id).ToList();

        var parties = deals.Select(d => d.BuyerPartyId)
            .Concat(deals.Where(d => d.SellerPartyId != null).Select(d => d.SellerPartyId!.Value));

        var names = await PartyNamesAsync(parties);
        var agents = await AgentDisplayNamesAsync(
            deals.Select(d => d.ListingAgentId).Concat(deals.Select(d => d.SellingAgentId)));

        var propertyIds = deals.Select(d => d.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToListAsync();

        var media = await Db.PropertyMedia.ForCompany(Tenant)
            .Where(m => propertyIds.Contains(m.PropertyId) && m.IsHero)
            .ToDictionaryAsync(m => m.PropertyId, m => m.Url);

        var checklist = await Db.DealChecklistItems.ForCompany(Tenant)
            .Where(c => ids.Contains(c.DealId))
            .Select(c => new { c.DealId, c.Label, c.SortOrder, c.IsCompleted, c.DueDate, c.IsMandatory })
            .ToListAsync();

        var chainIds = deals.Where(d => d.ChainId != null).Select(d => d.ChainId!.Value).Distinct().ToList();

        var chains = chainIds.Count == 0
            ? []
            : await Db.SalesChains.ForCompany(Tenant)
                .Where(c => chainIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c);

        return deals.Select(d =>
        {
            var mine = checklist.Where(c => c.DealId == d.Id).OrderBy(c => c.SortOrder).ToList();
            var next = mine.FirstOrDefault(c => !c.IsCompleted);
            var property = properties.FirstOrDefault(p => p.Id == d.PropertyId);
            var chain = d.ChainId is null ? null : chains.GetValueOrDefault(d.ChainId.Value);

            return new DealListItemDto
            {
                Id = d.Id,
                Reference = d.Reference,
                Status = d.Status,
                Kind = d.Kind,
                PropertyId = d.PropertyId,
                PropertyReference = property?.Reference ?? "—",
                AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                HeroImageUrl = media.GetValueOrDefault(d.PropertyId),
                BuyerName = names.GetValueOrDefault(d.BuyerPartyId, "—"),
                SellerName = d.SellerPartyId is null ? null : names.GetValueOrDefault(d.SellerPartyId.Value),
                ListingAgentName = d.ListingAgentId is null ? null : agents.GetValueOrDefault(d.ListingAgentId.Value),
                SellingAgentName = d.SellingAgentId is null ? null : agents.GetValueOrDefault(d.SellingAgentId.Value),
                AgreedPrice = d.AgreedPrice,
                GrossFee = d.GrossFee,
                CurrencyCode = string.IsNullOrWhiteSpace(d.CurrencyCode) ? currency : d.CurrencyCode,
                DepositReceived = d.DepositReceived,
                FeeInvoiced = d.FeeInvoiced,
                FeeReceived = d.FeeReceived,
                AgreedOn = d.AgreedOn,
                TargetExchangeDate = d.TargetExchangeDate,
                TargetCompletionDate = d.TargetCompletionDate,
                ActualCompletionDate = d.ActualCompletionDate,
                DaysInProgress = d.DaysInProgress,
                DaysSinceLastMilestone = d.DaysSinceLastMilestone,

                // Three weeks with nothing moving is not slow, it is stuck. That is the threshold
                // at which somebody should be telephoning rather than waiting.
                IsStalled = (d.DaysSinceLastMilestone ?? 0) > 21
                            && d.Status is DealStatus.Agreed or DealStatus.Progressing,
                CompletedSteps = mine.Count(c => c.IsCompleted),
                TotalSteps = mine.Count,
                ProgressPercent = RealEstateMapper.Percent(mine.Count(c => c.IsCompleted), mine.Count),
                NextStepLabel = next?.Label,
                NextStepDue = next?.DueDate,
                NextStepOverdue = next?.DueDate is not null && next.DueDate < Today,
                ChainId = d.ChainId,
                ChainPosition = d.ChainPosition,
                ChainAtRisk = chain is not null && (chain.IsBroken || chain.Links.Any(l => l.IsHoldingUpChain)),
            };
        }).ToList();
    }

    protected async Task<Dictionary<Guid, string>> AgentDisplayNamesAsync(IEnumerable<Guid?> agentIds)
    {
        var ids = agentIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.DisplayName);
    }

    protected async Task<Dictionary<Guid, string>> AgentUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    public async Task<DealBoardDto> GetDealBoardAsync(ListQueryDto query, Guid? agentId)
    {
        var deals = await Db.Deals.ForCompany(Tenant)
            .WhereIf(agentId.HasValue, d => d.ListingAgentId == agentId || d.SellingAgentId == agentId)
            .WhereIf(query.OfficeId.HasValue, d => d.OfficeId == query.OfficeId)
            .Where(d => d.Status != DealStatus.Completed && d.Status != DealStatus.Cancelled
                                                         && d.Status != DealStatus.FellThrough)
            .ToListAsync();

        var mapped = await MapDealListAsync(deals);

        var columns = new[] { DealStatus.Agreed, DealStatus.Progressing, DealStatus.Exchanged, DealStatus.OnHold }
            .Select(status =>
            {
                var mine = mapped.Where(d => d.Status == status).OrderByDescending(d => d.DaysSinceLastMilestone ?? 0).ToList();

                return new DealBoardColumnDto
                {
                    Status = status,
                    Label = status switch
                    {
                        DealStatus.Agreed => "Agreed",
                        DealStatus.Progressing => "In progress",
                        DealStatus.Exchanged => "Exchanged",
                        _ => "On hold",
                    },
                    Count = mine.Count,
                    Value = RealEstateMapper.Money(mine.Sum(d => d.AgreedPrice)),
                    Fee = RealEstateMapper.Money(mine.Sum(d => d.GrossFee)),

                    // Fifty a column is more than anybody scrolls; the rest are reachable from the
                    // list view, which is where somebody working through a backlog actually goes.
                    Items = mine.Take(50).ToList(),
                    HasMore = mine.Count > 50,
                };
            }).ToList();

        var closed = await Db.Deals.ForCompany(Tenant)
            .Where(d => d.AgreedOn >= Today.AddMonths(-12))
            .Where(d => d.Status == DealStatus.Completed || d.Status == DealStatus.FellThrough)
            .Select(d => new { d.Status, d.AgreedOn, d.ActualCompletionDate })
            .ToListAsync();

        var completed = closed.Where(d => d.Status == DealStatus.Completed).ToList();
        var fellThrough = closed.Count(d => d.Status == DealStatus.FellThrough);

        var withDates = completed.Where(d => d.ActualCompletionDate is not null).ToList();

        return new DealBoardDto
        {
            Columns = columns,
            TotalCount = mapped.Count,
            TotalValue = RealEstateMapper.Money(mapped.Sum(d => d.AgreedPrice)),
            TotalFee = RealEstateMapper.Money(mapped.Sum(d => d.GrossFee)),
            StalledCount = mapped.Count(d => d.IsStalled),
            ChainAtRiskCount = mapped.Count(d => d.ChainAtRisk),
            FallThroughRatePercent = RealEstateMapper.Percent(fellThrough, completed.Count + fellThrough),
            AverageDaysToComplete = withDates.Count == 0
                ? 0m
                : RealEstateMapper.Money((decimal)withDates
                    .Average(d => d.ActualCompletionDate!.Value.DayNumber - d.AgreedOn.DayNumber)),
        };
    }

    public async Task<DealDetailDto?> GetDealAsync(Guid id)
    {
        var deal = await Db.Deals.ForCompany(Tenant)
            .Include(d => d.Parties)
            .Include(d => d.Checklist)
            .Include(d => d.Milestones)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (deal is null) return null;

        var head = (await MapDealListAsync([deal]))[0];

        var detail = new DealDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            Status = head.Status,
            Kind = head.Kind,
            PropertyId = head.PropertyId,
            PropertyReference = head.PropertyReference,
            AddressOneLine = head.AddressOneLine,
            HeroImageUrl = head.HeroImageUrl,
            BuyerName = head.BuyerName,
            SellerName = head.SellerName,
            ListingAgentName = head.ListingAgentName,
            SellingAgentName = head.SellingAgentName,
            AgreedPrice = head.AgreedPrice,
            GrossFee = head.GrossFee,
            CurrencyCode = head.CurrencyCode,
            DepositReceived = head.DepositReceived,
            FeeInvoiced = head.FeeInvoiced,
            FeeReceived = head.FeeReceived,
            AgreedOn = head.AgreedOn,
            TargetExchangeDate = head.TargetExchangeDate,
            TargetCompletionDate = head.TargetCompletionDate,
            ActualCompletionDate = head.ActualCompletionDate,
            DaysInProgress = head.DaysInProgress,
            DaysSinceLastMilestone = head.DaysSinceLastMilestone,
            IsStalled = head.IsStalled,
            CompletedSteps = head.CompletedSteps,
            TotalSteps = head.TotalSteps,
            ProgressPercent = head.ProgressPercent,
            NextStepLabel = head.NextStepLabel,
            NextStepDue = head.NextStepDue,
            NextStepOverdue = head.NextStepOverdue,
            ChainId = head.ChainId,
            ChainPosition = head.ChainPosition,
            ChainAtRisk = head.ChainAtRisk,

            ListingId = deal.ListingId,
            OfferId = deal.OfferId,
            EnquiryId = deal.EnquiryId,
            InstructionId = deal.InstructionId,
            BuyerPartyId = deal.BuyerPartyId,
            SellerPartyId = deal.SellerPartyId,
            DepositAmount = deal.DepositAmount,
            FeePercent = deal.FeePercent,
            FeeTax = deal.FeeTax,
            ListingSideFee = deal.ListingSideFee,
            SellingSideFee = deal.SellingSideFee,
            CommissionCalculationId = deal.CommissionCalculationId,
            ActualExchangeDate = deal.ActualExchangeDate,
            FallThroughRecordId = deal.FallThroughRecordId,
            ConveyancingId = deal.ConveyancingId,
            ResultingTenancyId = deal.ResultingTenancyId,
            Notes = deal.Notes,
        };

        var owners = await AgentUserNamesAsync(deal.Checklist.Select(c => c.OwnerUserId));
        var partyNames = await PartyNamesAsync(deal.Parties.Where(p => p.PartyId != null).Select(p => p.PartyId!.Value));

        detail.Parties = deal.Parties.OrderBy(p => p.Role).Select(p => new DealPartyDto
        {
            Id = p.Id,
            Role = p.Role,
            PartyId = p.PartyId,
            OrganisationName = p.OrganisationName,
            ContactName = p.ContactName ?? (p.PartyId is null ? null : partyNames.GetValueOrDefault(p.PartyId.Value)),
            Phone = p.Phone,
            Email = p.Email,
            Reference = p.Reference,
            LastContactedAt = p.LastContactedAt,
            IsResponsive = p.IsResponsive,

            // How long since anybody spoke to this solicitor. The single most useful number on a
            // stalling deal, and the one nobody records unless the software asks for it.
            DaysSinceContact = p.LastContactedAt is null
                ? null
                : (int)(DateTime.UtcNow - p.LastContactedAt.Value).TotalDays,
            Note = p.Note,
        }).ToList();

        detail.Checklist = deal.Checklist.OrderBy(c => c.SortOrder).Select(c => new DealChecklistItemDto
        {
            Id = c.Id,
            StepKey = c.StepKey,
            Label = c.Label,
            SortOrder = c.SortOrder,
            OwnerName = c.OwnerUserId is null ? null : owners.GetValueOrDefault(c.OwnerUserId.Value),
            ResponsibleParty = c.ResponsibleParty,
            DueDate = c.DueDate,
            CompletedOn = c.CompletedOn,
            IsCompleted = c.IsCompleted,
            IsMandatory = c.IsMandatory,
            IsBlocking = c.IsBlocking,
            IsOverdue = !c.IsCompleted && c.DueDate is not null && c.DueDate < Today,
            Note = c.Note,
        }).ToList();

        detail.Milestones = deal.Milestones.OrderBy(m => m.SortOrder).Select(m => new DealMilestoneDto
        {
            Id = m.Id,
            Name = m.Name,
            TargetDate = m.TargetDate,
            ActualDate = m.ActualDate,
            VarianceDays = m.VarianceDays,
            DelayReason = m.DelayReason,
            SortOrder = m.SortOrder,
        }).ToList();

        if (deal.ConveyancingId is not null)
        {
            var conveyancing = await Db.Conveyancings.ForCompany(Tenant)
                .FirstOrDefaultAsync(c => c.Id == deal.ConveyancingId);

            if (conveyancing is not null) detail.Conveyancing = await MapConveyancingAsync(conveyancing);
        }

        if (deal.ChainId is not null) detail.Chain = await GetChainAsync(deal.ChainId.Value);

        if (deal.CommissionCalculationId is not null)
        {
            var calculation = await Db.CommissionCalculations.ForCompany(Tenant)
                .Include(c => c.Splits)
                .FirstOrDefaultAsync(c => c.Id == deal.CommissionCalculationId);

            if (calculation is not null)
                detail.Commission = (await MapCalculationsAsync([calculation]))[0];
        }

        detail.Documents = await Db.GeneratedDocuments.ForCompany(Tenant)
            .Where(d => d.DealId == deal.Id && !d.IsSuperseded)
            .OrderByDescending(d => d.GeneratedAt)
            .Select(d => new GeneratedDocumentDto
            {
                Id = d.Id,
                DocumentNumber = d.DocumentNumber,
                DocumentType = d.DocumentType,
                Title = d.Title,
                Url = d.Url,
                GeneratedAt = d.GeneratedAt,
                LanguageCode = d.LanguageCode,
                VerificationCode = d.VerificationCode,
                IsSent = d.IsSent,
                IsSigned = d.IsSigned,
                IsSuperseded = d.IsSuperseded,
                PageCount = d.PageCount,
            })
            .ToListAsync();

        detail.Timeline = BuildDealTimeline(deal);

        return detail;
    }

    private static List<TimelineEntryDto> BuildDealTimeline(Deal deal)
    {
        var entries = new List<TimelineEntryDto>
        {
            new()
            {
                Id = deal.Id,
                OccurredAt = deal.AgreedOn.ToDateTime(TimeOnly.MinValue),
                Kind = "Agreed",
                Title = "Sale agreed",
                Detail = $"{deal.AgreedPrice:N0} agreed.",
                Icon = "handshake",
                Tone = "positive",
                Amount = deal.AgreedPrice,
            },
        };

        if (deal.DepositReceived)
        {
            entries.Add(new TimelineEntryDto
            {
                Id = deal.Id,
                OccurredAt = deal.AgreedOn.ToDateTime(TimeOnly.MinValue).AddDays(1),
                Kind = "Deposit",
                Title = "Deposit received",
                Amount = deal.DepositAmount,
                Icon = "banknote",
                Tone = "positive",
            });
        }

        entries.AddRange(deal.Milestones
            .Where(m => m.ActualDate is not null)
            .Select(m => new TimelineEntryDto
            {
                Id = m.Id,
                OccurredAt = m.ActualDate!.Value.ToDateTime(TimeOnly.MinValue),
                Kind = "Milestone",
                Title = m.Name,
                Detail = m.VarianceDays is > 0 ? $"{m.VarianceDays} days later than target." : null,
                Icon = "flag",
                Tone = m.VarianceDays is > 0 ? "warning" : "neutral",
            }));

        entries.AddRange(deal.Checklist
            .Where(c => c.CompletedOn is not null)
            .Select(c => new TimelineEntryDto
            {
                Id = c.Id,
                OccurredAt = c.CompletedOn!.Value.ToDateTime(TimeOnly.MinValue),
                Kind = "Step",
                Title = c.Label,
                Icon = "check",
                Tone = "neutral",
            }));

        return entries.OrderByDescending(e => e.OccurredAt).ToList();
    }
}
