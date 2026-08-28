using Microsoft.EntityFrameworkCore;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>Creating and progressing deals, chains, fall-throughs and conveyancing.</summary>
public partial class BrokerageService
{
    public async Task<DealDetailDto> CreateDealAsync(DealCreateDto dto, Guid userId)
    {
        var property = await Db.Properties.ForCompany(Tenant)
            .FirstOrDefaultAsync(p => p.Id == dto.PropertyId)
            ?? throw new InvalidOperationException("That property does not exist.");

        if (property.Status is PropertyStatus.Litigation)
            throw new InvalidOperationException(
                "That property is under litigation and cannot be sold until the case is closed.");

        var existing = await Db.Deals.ForCompany(Tenant)
            .Where(d => d.PropertyId == dto.PropertyId)
            .Where(d => d.Status == DealStatus.Agreed || d.Status == DealStatus.Progressing
                                                      || d.Status == DealStatus.Exchanged)
            .Select(d => d.Reference)
            .FirstOrDefaultAsync();

        // Two live deals on one property is how gazumping happens by accident rather than on
        // purpose. If it is deliberate, the first one has to be closed first.
        if (existing is not null)
            throw new InvalidOperationException(
                $"Deal {existing} is already running on this property. Close it before agreeing another.");

        if (dto.AgreedPrice <= 0m)
            throw new InvalidOperationException("A deal needs an agreed price.");

        var currency = await CurrencyAsync();

        var instruction = dto.InstructionId is null
            ? null
            : await Db.Instructions.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == dto.InstructionId);

        // The fee comes from the instruction the vendor signed, unless somebody has deliberately
        // overridden it. Defaulting to a house rate would quietly bill a vendor the wrong amount.
        var feePercent = dto.OverrideFeePercent ?? instruction?.FeePercent ?? 0m;

        var grossFee = dto.OverrideFeeAmount is > 0m
            ? dto.OverrideFeeAmount.Value
            : RealEstateMapper.Money(dto.AgreedPrice * feePercent / 100m);

        var deal = new Deal
        {
            Reference = await numbering.NextDealNumberAsync(DateTime.UtcNow),
            PropertyId = dto.PropertyId,
            ListingId = dto.ListingId,
            OfferId = dto.OfferId,
            EnquiryId = dto.EnquiryId,
            InstructionId = dto.InstructionId,
            Kind = dto.Kind,
            Status = DealStatus.Agreed,
            BuyerPartyId = dto.BuyerPartyId,
            SellerPartyId = dto.SellerPartyId,
            ListingAgentId = dto.ListingAgentId,
            SellingAgentId = dto.SellingAgentId,
            OfficeId = dto.OfficeId,
            AgreedPrice = dto.AgreedPrice,
            CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? currency : dto.CurrencyCode!,
            DepositAmount = dto.DepositAmount,
            GrossFee = grossFee,
            FeePercent = feePercent,
            AgreedOn = dto.AgreedOn == default ? Today : dto.AgreedOn,
            TargetExchangeDate = dto.TargetExchangeDate,
            TargetCompletionDate = dto.TargetCompletionDate,
            Notes = dto.Notes,
            Description = dto.Notes,
        }.StampNew(Tenant, userId);

        Db.Deals.Add(deal);

        foreach (var party in dto.Parties)
        {
            Db.DealParties.Add(new DealParty
            {
                DealId = deal.Id,
                Role = party.Role,
                PartyId = party.PartyId,
                OrganisationName = party.OrganisationName,
                ContactName = party.ContactName,
                Phone = party.Phone,
                Email = party.Email,
                Reference = party.Reference,
                IsResponsive = true,
                Note = party.Note,
            }.StampNew(Tenant, userId));
        }

        var order = 0;
        var basis = deal.AgreedOn;

        foreach (var (key, label, owner, offset, mandatory, blocking) in StandardChecklist)
        {
            Db.DealChecklistItems.Add(new DealChecklistItem
            {
                DealId = deal.Id,
                StepKey = key,
                Label = label,
                SortOrder = order++,
                ResponsibleParty = owner,
                DueDate = basis.AddDays(offset),
                IsMandatory = mandatory,
                IsBlocking = blocking,
            }.StampNew(Tenant, userId));
        }

        var milestones = new (string Name, DateOnly? Target)[]
        {
            ("Sale agreed", deal.AgreedOn),
            ("Contracts exchanged", deal.TargetExchangeDate),
            ("Completion", deal.TargetCompletionDate),
        };

        var milestoneOrder = 0;

        foreach (var (name, target) in milestones)
        {
            Db.DealMilestones.Add(new DealMilestone
            {
                DealId = deal.Id,
                Name = name,
                TargetDate = target,
                ActualDate = milestoneOrder == 0 ? deal.AgreedOn : null,
                SortOrder = milestoneOrder++,
            }.StampNew(Tenant, userId));
        }

        // The property comes off the market. Leaving it live is how a vendor gets three more
        // viewings on something they have already agreed to sell.
        property.Status = PropertyStatus.UnderOffer;
        property.StampUpdated(userId);

        if (dto.ListingId is not null)
        {
            var listing = await Db.Listings.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == dto.ListingId);

            if (listing is not null && listing.Status == ListingStatus.Live)
            {
                listing.Status = ListingStatus.UnderOffer;
                listing.StampUpdated(userId);
            }
        }

        if (dto.EnquiryId is not null)
        {
            var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == dto.EnquiryId);

            if (enquiry is not null)
            {
                enquiry.Stage = EnquiryStage.Agreed;
                enquiry.ConvertedDealId = deal.Id;
                enquiry.LastActivityAt = DateTime.UtcNow;
                enquiry.StampUpdated(userId);
            }
        }

        await QueueNotificationAsync(
            "deal.agreed",
            $"Sale agreed — {deal.Reference}",
            $"{deal.AgreedPrice:N0} agreed. Fee {grossFee:N0}.",
            $"/realestate/brokerage/deals/{deal.Id}",
            entityType: nameof(Deal),
            entityId: deal.Id);

        await Db.SaveChangesAsync();

        return (await GetDealAsync(deal.Id))!;
    }

    public async Task<DealDetailDto> UpdateChecklistAsync(
        Guid dealId, Guid itemId, bool completed, DateOnly? completedOn, string? note, Guid userId)
    {
        var deal = await Db.Deals.ForCompany(Tenant)
            .Include(d => d.Checklist)
            .FirstOrDefaultAsync(d => d.Id == dealId)
            ?? throw new InvalidOperationException("That deal does not exist.");

        var item = deal.Checklist.FirstOrDefault(c => c.Id == itemId)
            ?? throw new InvalidOperationException("That step is not on this deal's checklist.");

        item.IsCompleted = completed;
        item.CompletedOn = completed ? completedOn ?? Today : null;
        item.Note = note ?? item.Note;
        item.StampUpdated(userId);

        // Any movement resets the stall clock. That is the whole point of recording steps — the
        // board should stop shouting about a deal the moment somebody actually does something.
        deal.DaysSinceLastMilestone = 0;
        deal.DaysInProgress = Today.DayNumber - deal.AgreedOn.DayNumber;

        if (completed)
        {
            switch (item.StepKey)
            {
                case "exchange":
                    deal.Status = DealStatus.Exchanged;
                    deal.ActualExchangeDate = item.CompletedOn;
                    await StampMilestoneAsync(deal, "Contracts exchanged", item.CompletedOn!.Value, userId);
                    break;

                case "completion":
                    deal.Status = DealStatus.Completed;
                    deal.ActualCompletionDate = item.CompletedOn;
                    await StampMilestoneAsync(deal, "Completion", item.CompletedOn!.Value, userId);
                    await OnDealCompletedAsync(deal, userId);
                    break;

                case "fee-invoiced":
                    deal.FeeInvoiced = true;
                    break;

                case "fee-received":
                    deal.FeeReceived = true;
                    break;

                default:
                    if (deal.Status == DealStatus.Agreed) deal.Status = DealStatus.Progressing;
                    break;
            }
        }

        deal.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return (await GetDealAsync(deal.Id))!;
    }

    private async Task StampMilestoneAsync(Deal deal, string name, DateOnly actual, Guid userId)
    {
        var milestone = await Db.DealMilestones.ForCompany(Tenant)
            .FirstOrDefaultAsync(m => m.DealId == deal.Id && m.Name == name);

        if (milestone is null) return;

        milestone.ActualDate = actual;
        milestone.VarianceDays = milestone.TargetDate is null
            ? null
            : actual.DayNumber - milestone.TargetDate.Value.DayNumber;
        milestone.StampUpdated(userId);
    }

    private async Task OnDealCompletedAsync(Deal deal, Guid userId)
    {
        var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == deal.PropertyId);

        if (property is not null)
        {
            property.Status = deal.Kind == ListingKind.ForRent ? PropertyStatus.Let : PropertyStatus.Sold;
            property.StampUpdated(userId);
        }

        var listing = deal.ListingId is null
            ? null
            : await Db.Listings.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == deal.ListingId);

        if (listing is not null)
        {
            listing.Status = ListingStatus.Completed;
            listing.StampUpdated(userId);
        }

        // A chain link completing may complete the whole chain, and everybody in it wants to know.
        if (deal.ChainId is not null)
        {
            var link = await Db.SalesChainLinks.ForCompany(Tenant)
                .FirstOrDefaultAsync(l => l.SalesChainId == deal.ChainId && l.DealId == deal.Id);

            if (link is not null)
            {
                link.Status = DealStatus.Completed;
                link.IsHoldingUpChain = false;
                link.HoldUpReason = null;
                link.LastUpdatedAt = DateTime.UtcNow;
                link.StampUpdated(userId);
            }

            await RefreshChainAsync(deal.ChainId.Value, userId);
        }

        await QueueNotificationAsync(
            "deal.completed",
            $"Completed — {deal.Reference}",
            $"{deal.AgreedPrice:N0}. Fee of {deal.GrossFee:N0} is now earned.",
            $"/realestate/brokerage/deals/{deal.Id}",
            entityType: nameof(Deal),
            entityId: deal.Id);
    }

    public async Task<DealDetailDto> ChangeDealStatusAsync(Guid id, DealStatus status, Guid userId)
    {
        var deal = await RequireAsync<Deal>(id, "That deal does not exist.");

        if (deal.Status == status) return (await GetDealAsync(id))!;

        if (deal.Status == DealStatus.Completed)
            throw new InvalidOperationException(
                "That deal has completed. It cannot be moved back — record a new transaction instead.");

        if (status == DealStatus.FellThrough)
            throw new InvalidOperationException(
                "Record a fall-through with its cause rather than setting the status directly. "
                + "The cause is the only part anybody can learn from.");

        if (status == DealStatus.Completed)
        {
            var blocking = await Db.DealChecklistItems.ForCompany(Tenant)
                .Where(c => c.DealId == id && c.IsBlocking && c.IsMandatory && !c.IsCompleted)
                .Select(c => c.Label)
                .ToListAsync();

            if (blocking.Count > 0)
                throw new InvalidOperationException(
                    "These steps have to be completed before the deal can: " + string.Join("; ", blocking) + ".");
        }

        deal.Status = status;
        deal.DaysSinceLastMilestone = 0;
        deal.StampUpdated(userId);

        if (status == DealStatus.Completed)
        {
            deal.ActualCompletionDate = Today;
            await OnDealCompletedAsync(deal, userId);
        }

        await Db.SaveChangesAsync();

        return (await GetDealAsync(id))!;
    }

    // ═══ Fall-through ════════════════════════════════════════════════════════

    public async Task<FallThroughRecordDto> RecordFallThroughAsync(FallThroughRecordDto dto, Guid userId)
    {
        var deal = await Db.Deals.ForCompany(Tenant)
            .Include(d => d.Checklist)
            .FirstOrDefaultAsync(d => d.Id == dto.DealId)
            ?? throw new InvalidOperationException("That deal does not exist.");

        if (deal.Status == DealStatus.Completed)
            throw new InvalidOperationException("That deal has already completed.");

        if (deal.FallThroughRecordId is not null)
            throw new InvalidOperationException("A fall-through has already been recorded on this deal.");

        var lastCompleted = deal.Checklist
            .Where(c => c.IsCompleted)
            .OrderByDescending(c => c.SortOrder)
            .Select(c => c.Label)
            .FirstOrDefault();

        var record = new FallThroughRecord
        {
            DealId = deal.Id,
            PropertyId = deal.PropertyId,
            Cause = dto.Cause,
            Detail = dto.Detail,
            OccurredOn = dto.OccurredOn == default ? Today : dto.OccurredOn,
            StageReached = dto.StageReached ?? lastCompleted,
            DaysInProgress = Today.DayNumber - deal.AgreedOn.DayNumber,
            CostIncurred = dto.CostIncurred,

            // The fee lost is what the deal would have earned. Recording nought here makes a year
            // of collapsed deals look free, which is the opposite of what the number is for.
            LostFee = dto.LostFee > 0m ? dto.LostFee : deal.GrossFee,
            RelistedImmediately = dto.RelistedImmediately,
            PreviousViewersNotified = dto.PreviousViewersNotified,
        }.StampNew(Tenant, userId);

        Db.FallThroughRecords.Add(record);

        deal.Status = DealStatus.FellThrough;
        deal.FallThroughRecordId = record.Id;
        deal.StampUpdated(userId);

        var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == deal.PropertyId);

        if (property is not null)
        {
            property.Status = PropertyStatus.Available;
            property.StampUpdated(userId);
        }

        if (deal.ListingId is not null)
        {
            var listing = await Db.Listings.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == deal.ListingId);

            if (listing is not null && dto.RelistedImmediately)
            {
                listing.Status = ListingStatus.Live;
                listing.StampUpdated(userId);
            }
        }

        // A collapse takes the whole chain with it unless somebody moves quickly, so the chain is
        // marked broken at this position rather than left looking healthy.
        if (deal.ChainId is not null)
        {
            var chain = await Db.SalesChains.ForCompany(Tenant)
                .Include(c => c.Links)
                .FirstOrDefaultAsync(c => c.Id == deal.ChainId);

            if (chain is not null)
            {
                chain.IsBroken = true;
                chain.BrokenOn = record.OccurredOn;
                chain.BrokenAtPosition = deal.ChainPosition;
                chain.StampUpdated(userId);

                await QueueNotificationAsync(
                    "chain.broken",
                    $"Chain {chain.Reference} has broken",
                    $"{deal.Reference} fell through at position {deal.ChainPosition}. "
                    + $"{chain.Links.Count - 1} other transactions are affected.",
                    "/realestate/brokerage/chains",
                    entityType: nameof(SalesChain),
                    entityId: chain.Id,
                    severity: AlertSeverity.Critical);
            }
        }

        await Db.SaveChangesAsync();

        return new FallThroughRecordDto
        {
            Id = record.Id,
            DealId = record.DealId,
            DealReference = deal.Reference,
            PropertyId = record.PropertyId,
            AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
            Cause = record.Cause,
            ReasonLabel = dto.ReasonLabel,
            Detail = record.Detail,
            OccurredOn = record.OccurredOn,
            StageReached = record.StageReached,
            DaysInProgress = record.DaysInProgress,
            CostIncurred = record.CostIncurred,
            LostFee = record.LostFee,
            RelistedImmediately = record.RelistedImmediately,
            PreviousViewersNotified = record.PreviousViewersNotified,
        };
    }

    public async Task<DealPartyDto> SaveDealPartyAsync(Guid dealId, DealPartyDto dto, Guid userId)
    {
        _ = await RequireAsync<Deal>(dealId, "That deal does not exist.");

        var party = dto.Id.HasValue
            ? await Db.DealParties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (party is null)
        {
            party = new DealParty { DealId = dealId }.StampNew(Tenant, userId);
            Db.DealParties.Add(party);
        }
        else
        {
            party.StampUpdated(userId);
        }

        party.Role = dto.Role;
        party.PartyId = dto.PartyId;
        party.OrganisationName = dto.OrganisationName;
        party.ContactName = dto.ContactName;
        party.Phone = dto.Phone;
        party.Email = dto.Email;
        party.Reference = dto.Reference;
        party.LastContactedAt = dto.LastContactedAt;
        party.IsResponsive = dto.IsResponsive;
        party.Note = dto.Note;

        await Db.SaveChangesAsync();

        return new DealPartyDto
        {
            Id = party.Id,
            Role = party.Role,
            PartyId = party.PartyId,
            OrganisationName = party.OrganisationName,
            ContactName = party.ContactName,
            Phone = party.Phone,
            Email = party.Email,
            Reference = party.Reference,
            LastContactedAt = party.LastContactedAt,
            IsResponsive = party.IsResponsive,
            DaysSinceContact = party.LastContactedAt is null
                ? null
                : (int)(DateTime.UtcNow - party.LastContactedAt.Value).TotalDays,
            Note = party.Note,
        };
    }

    // ═══ Chains ══════════════════════════════════════════════════════════════

    public async Task<SalesChainDto> SaveChainAsync(SalesChainDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var chain = isNew
            ? new SalesChain { Reference = await numbering.NextMasterCodeAsync(Db.SalesChains, "CHN") }
            : await Db.SalesChains.ForCompany(Tenant)
                .Include(c => c.Links)
                .FirstOrDefaultAsync(c => c.Id == dto.Id)
              ?? throw new InvalidOperationException("That chain does not exist.");

        chain.TargetCompletionDate = dto.TargetCompletionDate;

        if (isNew)
        {
            chain.StampNew(Tenant, userId);
            Db.SalesChains.Add(chain);
        }
        else
        {
            chain.StampUpdated(userId);
        }

        var keep = dto.Links.Where(l => l.Id != Guid.Empty).Select(l => l.Id).ToHashSet();

        foreach (var removed in chain.Links.Where(l => !keep.Contains(l.Id)).ToList())
        {
            removed.StampDeleted(userId);
        }

        var position = 0;

        foreach (var row in dto.Links.OrderBy(l => l.Position))
        {
            var link = row.Id == Guid.Empty ? null : chain.Links.FirstOrDefault(l => l.Id == row.Id);

            if (link is null)
            {
                link = new SalesChainLink { SalesChainId = chain.Id }.StampNew(Tenant, userId);
                chain.Links.Add(link);
                Db.SalesChainLinks.Add(link);
            }
            else
            {
                link.StampUpdated(userId);
            }

            link.Position = position++;
            link.DealId = row.DealId;
            link.ExternalDescription = row.ExternalDescription;
            link.ExternalAgentName = row.ExternalAgentName;
            link.ExternalAgentPhone = row.ExternalAgentPhone;
            link.Status = row.Status;
            link.SolicitorName = row.SolicitorName;
            link.LastUpdatedAt = DateTime.UtcNow;
            link.IsHoldingUpChain = row.IsHoldingUpChain;
            link.HoldUpReason = row.HoldUpReason;

            // A link is our deal or somebody else's. Ours carries a deal id and we can see its
            // progress; theirs is a name and a telephone number, which is all a chain ever gives.
            if (row.DealId is not null)
            {
                var deal = await Db.Deals.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == row.DealId);

                if (deal is not null)
                {
                    deal.ChainId = chain.Id;
                    deal.ChainPosition = link.Position;
                    deal.StampUpdated(userId);

                    link.Status = deal.Status;
                }
            }
        }

        chain.LinkCount = position;

        await Db.SaveChangesAsync();
        await RefreshChainAsync(chain.Id, userId);
        await Db.SaveChangesAsync();

        return (await GetChainAsync(chain.Id))!;
    }

    private async Task RefreshChainAsync(Guid chainId, Guid userId)
    {
        var chain = await Db.SalesChains.ForCompany(Tenant)
            .Include(c => c.Links)
            .FirstOrDefaultAsync(c => c.Id == chainId);

        if (chain is null) return;

        chain.LinkCount = chain.Links.Count;
        chain.IsComplete = chain.Links.Count > 0 && chain.Links.All(l => l.Status == DealStatus.Completed);

        if (chain.IsComplete)
        {
            chain.IsBroken = false;
            chain.BrokenOn = null;
            chain.BrokenAtPosition = null;
        }

        chain.StampUpdated(userId);
    }

    private async Task<SalesChainDto?> GetChainAsync(Guid id)
    {
        var chain = await Db.SalesChains.ForCompany(Tenant)
            .Include(c => c.Links)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chain is null) return null;

        return (await MapChainsAsync([chain]))[0];
    }

    private async Task<List<SalesChainDto>> MapChainsAsync(List<SalesChain> chains)
    {
        if (chains.Count == 0) return [];

        var dealIds = chains.SelectMany(c => c.Links)
            .Where(l => l.DealId != null).Select(l => l.DealId!.Value).Distinct().ToList();

        var deals = dealIds.Count == 0
            ? []
            : await Db.Deals.ForCompany(Tenant)
                .Where(d => dealIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Reference, d.PropertyId })
                .ToListAsync();

        var propertyIds = deals.Select(d => d.PropertyId).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant).Where(p => propertyIds.Contains(p.Id)).ToListAsync();

        return chains.Select(c => new SalesChainDto
        {
            Id = c.Id,
            Reference = c.Reference,
            LinkCount = c.LinkCount,
            IsComplete = c.IsComplete,
            IsBroken = c.IsBroken,
            BrokenOn = c.BrokenOn,
            BrokenAtPosition = c.BrokenAtPosition,
            TargetCompletionDate = c.TargetCompletionDate,
            Links = c.Links.OrderBy(l => l.Position).Select(l =>
            {
                var deal = l.DealId is null ? null : deals.FirstOrDefault(d => d.Id == l.DealId);
                var property = deal is null ? null : properties.FirstOrDefault(p => p.Id == deal.PropertyId);

                return new SalesChainLinkDto
                {
                    Id = l.Id,
                    Position = l.Position,
                    DealId = l.DealId,
                    DealReference = deal?.Reference,
                    AddressOneLine = property is null ? null : RealEstateMapper.OneLineAddress(property),
                    ExternalDescription = l.ExternalDescription,
                    ExternalAgentName = l.ExternalAgentName,
                    ExternalAgentPhone = l.ExternalAgentPhone,
                    Status = l.Status,
                    SolicitorName = l.SolicitorName,
                    LastUpdatedAt = l.LastUpdatedAt,
                    IsHoldingUpChain = l.IsHoldingUpChain,
                    HoldUpReason = l.HoldUpReason,
                    IsOurs = l.DealId is not null,
                };
            }).ToList(),
        }).ToList();
    }

    public async Task<List<SalesChainDto>> GetChainsAsync(bool atRiskOnly)
    {
        var chains = await Db.SalesChains.ForCompany(Tenant)
            .Include(c => c.Links)
            .Where(c => !c.IsComplete)
            .OrderByDescending(c => c.IsBroken)
            .ThenBy(c => c.TargetCompletionDate ?? DateOnly.MaxValue)
            .ToListAsync();

        var mapped = await MapChainsAsync(chains);

        return atRiskOnly
            ? mapped.Where(c => c.IsBroken || c.Links.Any(l => l.IsHoldingUpChain)).ToList()
            : mapped;
    }

    // ═══ Conveyancing ════════════════════════════════════════════════════════

    public async Task<ConveyancingDto> SaveConveyancingAsync(ConveyancingDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var record = isNew
            ? new Conveyancing()
            : await Db.Conveyancings.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
              ?? throw new InvalidOperationException("That conveyancing record does not exist.");

        record.DealId = dto.DealId;
        record.BookingId = dto.BookingId;
        record.PropertyId = dto.PropertyId;
        record.DraftDeedOn = dto.DraftDeedOn;
        record.DeedApprovedOn = dto.DeedApprovedOn;
        record.ConsiderationValue = dto.ConsiderationValue;
        record.GovernmentValue = dto.GovernmentValue;
        record.StampDutyRate = dto.StampDutyRate;
        record.RegistrationAppointmentOn = dto.RegistrationAppointmentOn;
        record.TokenNumber = dto.TokenNumber;
        record.RegistrarOffice = dto.RegistrarOffice;
        record.RegisteredOn = dto.RegisteredOn;
        record.DeedNumber = dto.DeedNumber;
        record.DeedUrl = dto.DeedUrl;
        record.MutationAppliedOn = dto.MutationAppliedOn;
        record.MutationNumber = dto.MutationNumber;
        record.MutationCompletedOn = dto.MutationCompletedOn;
        record.IsMutationComplete = dto.MutationCompletedOn is not null;
        record.Note = dto.Note;

        // Duty is charged on the higher of the consideration and the government's own valuation.
        // Computing it on the consideration alone is the single most common way a transaction is
        // registered short, and it surfaces years later as a demand plus penalty.
        var dutiableValue = Math.Max(record.ConsiderationValue, record.GovernmentValue ?? 0m);

        record.StampDutyAmount = dto.StampDutyAmount > 0m
            ? dto.StampDutyAmount
            : RealEstateMapper.Money(dutiableValue * record.StampDutyRate / 100m);

        record.RegistrationFee = dto.RegistrationFee;
        record.WithholdingTax = dto.WithholdingTax;
        record.OtherLevies = dto.OtherLevies;
        record.TotalTransactionCost = RealEstateMapper.Money(
            record.StampDutyAmount + record.RegistrationFee + record.WithholdingTax + record.OtherLevies);

        if (isNew)
        {
            record.StampNew(Tenant, userId);
            Db.Conveyancings.Add(record);
        }
        else
        {
            record.StampUpdated(userId);
        }

        if (record.DealId is not null)
        {
            var deal = await Db.Deals.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == record.DealId);

            if (deal is not null && deal.ConveyancingId != record.Id)
            {
                deal.ConveyancingId = record.Id;
                deal.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();

        return await MapConveyancingAsync(record);
    }

    private async Task<ConveyancingDto> MapConveyancingAsync(Conveyancing record)
    {
        var currency = await CurrencyAsync();

        var solicitorIds = new List<Guid>();
        if (record.BuyerSolicitorPartyId is not null) solicitorIds.Add(record.BuyerSolicitorPartyId.Value);
        if (record.SellerSolicitorPartyId is not null) solicitorIds.Add(record.SellerSolicitorPartyId.Value);

        var names = await PartyNamesAsync(solicitorIds);

        return new ConveyancingDto
        {
            Id = record.Id,
            DealId = record.DealId,
            BookingId = record.BookingId,
            PropertyId = record.PropertyId,
            BuyerSolicitorName = record.BuyerSolicitorPartyId is null
                ? null
                : names.GetValueOrDefault(record.BuyerSolicitorPartyId.Value),
            SellerSolicitorName = record.SellerSolicitorPartyId is null
                ? null
                : names.GetValueOrDefault(record.SellerSolicitorPartyId.Value),
            DraftDeedOn = record.DraftDeedOn,
            DeedApprovedOn = record.DeedApprovedOn,
            ConsiderationValue = record.ConsiderationValue,
            GovernmentValue = record.GovernmentValue,
            StampDutyRate = record.StampDutyRate,
            StampDutyAmount = record.StampDutyAmount,
            RegistrationFee = record.RegistrationFee,
            WithholdingTax = record.WithholdingTax,
            OtherLevies = record.OtherLevies,
            TotalTransactionCost = record.TotalTransactionCost,
            CurrencyCode = currency,
            RegistrationAppointmentOn = record.RegistrationAppointmentOn,
            TokenNumber = record.TokenNumber,
            RegistrarOffice = record.RegistrarOffice,
            RegisteredOn = record.RegisteredOn,
            DeedNumber = record.DeedNumber,
            DeedUrl = record.DeedUrl,
            MutationAppliedOn = record.MutationAppliedOn,
            MutationNumber = record.MutationNumber,
            MutationCompletedOn = record.MutationCompletedOn,
            IsMutationComplete = record.IsMutationComplete,
            Note = record.Note,
        };
    }
}
