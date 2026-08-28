using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Offers, tokens, bookings, allotment and balloting.
///
/// The booking wizard posts one payload and this creates the party, the booking, the frozen cost
/// sheet, the payment plan and the first receipt in one transaction — because a salesperson with a
/// customer in front of them cannot make five calls and cannot recover from the third one failing.
/// </summary>
public partial class BookingService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering,
    IInventoryService inventory,
    IMoneyService money,
    ICrmService crm)
    : RealEstateServiceBase(db, tenant), IBookingService
{
    // ═══ Offers ══════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<OfferListItemDto>> GetOffersAsync(
        ListQueryDto query, Guid? propertyId, OfferStatus? status)
    {
        var q = Db.Offers.ForCompany(Tenant)
            .WhereIf(propertyId.HasValue, o => o.PropertyId == propertyId)
            .WhereIf(status.HasValue, o => o.Status == status)
            .OrderByDescending(o => o.SubmittedAt);

        return await PageAsync(q, query, MapOffersAsync);
    }

    private async Task<List<OfferListItemDto>> MapOffersAsync(List<Offer> offers)
    {
        if (offers.Count == 0) return [];

        var propertyIds = offers.Select(o => o.PropertyId).Distinct().ToList();
        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var names = await PartyNamesAsync(
            offers.Select(o => o.BuyerPartyId).Concat(offers.Where(o => o.SellerPartyId.HasValue).Select(o => o.SellerPartyId!.Value)));

        var conditionCounts = await Db.OfferConditions.ForCompany(Tenant)
            .Where(c => offers.Select(o => o.Id).Contains(c.OfferId))
            .GroupBy(c => c.OfferId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var phones = await Db.Parties.ForCompany(Tenant)
            .Where(p => offers.Select(o => o.BuyerPartyId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PrimaryPhone);

        return offers.Select(o =>
        {
            var property = properties.GetValueOrDefault(o.PropertyId);
            var asking = property?.AskingPrice;

            return new OfferListItemDto
            {
                Id = o.Id,
                Reference = o.Reference,
                PropertyId = o.PropertyId,
                PropertyReference = property?.Reference ?? "—",
                AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                ListingId = o.ListingId,
                BuyerName = names.GetValueOrDefault(o.BuyerPartyId, "—"),
                BuyerPhone = phones.GetValueOrDefault(o.BuyerPartyId),
                SellerName = o.SellerPartyId is null ? null : names.GetValueOrDefault(o.SellerPartyId.Value),
                Amount = o.Amount,
                AskingPrice = asking,
                DifferenceFromAsking = asking is null ? null : o.Amount - asking,
                DifferencePercent = asking is null or 0m ? null : RealEstateMapper.Percent(o.Amount - asking.Value, asking.Value),
                CurrencyCode = o.CurrencyCode,
                Status = o.Status,
                SubmittedAt = o.SubmittedAt,
                ExpiresAt = o.ExpiresAt,
                RoundNumber = o.RoundNumber,
                IsBestAndFinal = o.IsBestAndFinal,
                Funding = o.Funding,
                ProofOfFundsProvided = o.ProofOfFundsProvided,
                MortgageInPrinciple = o.MortgageInPrinciple,
                IsChainFree = o.IsChainFree,
                ChainLength = o.ChainLength,
                ConditionCount = conditionCounts.GetValueOrDefault(o.Id),
                VendorNotified = o.VendorNotified,
                StrengthScore = ScoreOffer(o, asking),
            };
        }).ToList();
    }

    /// <summary>
    /// Why a lower offer can beat a higher one: cash beats a mortgage, chain-free beats a chain,
    /// proof of funds beats a promise, and every condition is a way for the deal to die.
    /// </summary>
    private static int ScoreOffer(Offer offer, decimal? asking)
    {
        var score = 50;

        if (asking is > 0m)
        {
            var ratio = offer.Amount / asking.Value;
            score += (int)Math.Clamp((ratio - 0.9m) * 200m, -30m, 30m);
        }

        score += offer.Funding switch
        {
            FundingKind.Cash => 20,
            FundingKind.CompanyFunds => 15,
            FundingKind.Mortgage => offer.MortgageInPrinciple ? 8 : 0,
            FundingKind.SaleOfExisting => -10,
            _ => 0,
        };

        if (offer.ProofOfFundsProvided) score += 10;
        if (offer.IsChainFree) score += 12;
        if (offer.ChainLength is > 2) score -= 8;

        return Math.Clamp(score, 0, 100);
    }

    public async Task<OfferDetailDto?> GetOfferAsync(Guid id)
    {
        var offer = await Db.Offers.ForCompany(Tenant)
            .Include(o => o.Conditions)
            .Include(o => o.Counters)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (offer is null) return null;

        var summary = (await MapOffersAsync([offer]))[0];
        var today = Today;

        return new OfferDetailDto
        {
            Id = summary.Id,
            Reference = summary.Reference,
            PropertyId = summary.PropertyId,
            PropertyReference = summary.PropertyReference,
            AddressOneLine = summary.AddressOneLine,
            ListingId = summary.ListingId,
            BuyerName = summary.BuyerName,
            BuyerPhone = summary.BuyerPhone,
            SellerName = summary.SellerName,
            Amount = summary.Amount,
            AskingPrice = summary.AskingPrice,
            DifferenceFromAsking = summary.DifferenceFromAsking,
            DifferencePercent = summary.DifferencePercent,
            CurrencyCode = summary.CurrencyCode,
            Status = summary.Status,
            SubmittedAt = summary.SubmittedAt,
            ExpiresAt = summary.ExpiresAt,
            RoundNumber = summary.RoundNumber,
            IsBestAndFinal = summary.IsBestAndFinal,
            Funding = summary.Funding,
            ProofOfFundsProvided = summary.ProofOfFundsProvided,
            MortgageInPrinciple = summary.MortgageInPrinciple,
            IsChainFree = summary.IsChainFree,
            ChainLength = summary.ChainLength,
            ConditionCount = summary.ConditionCount,
            VendorNotified = summary.VendorNotified,
            StrengthScore = summary.StrengthScore,
            BuyerPartyId = offer.BuyerPartyId,
            SellerPartyId = offer.SellerPartyId,
            EnquiryId = offer.EnquiryId,
            DepositAmount = offer.DepositAmount,
            ProofOfFundsUrl = offer.ProofOfFundsUrl,
            ProposedCompletionDate = offer.ProposedCompletionDate,
            DecidedAt = offer.DecidedAt,
            VendorNotifiedAt = offer.VendorNotifiedAt,
            Note = offer.Note,
            ResultingDealId = offer.ResultingDealId,
            Conditions = offer.Conditions.Select(c => new OfferConditionDto
            {
                Id = c.Id,
                Kind = c.Kind,
                Detail = c.Detail,
                SatisfyByDate = c.SatisfyByDate,
                IsSatisfied = c.IsSatisfied,
                SatisfiedOn = c.SatisfiedOn,
                IsOverdue = !c.IsSatisfied && c.SatisfyByDate is not null && c.SatisfyByDate < today,
            }).ToList(),
            Counters = offer.Counters.OrderBy(c => c.RoundNumber).Select(c => new OfferCounterDto
            {
                Id = c.Id,
                RoundNumber = c.RoundNumber,
                ProposedBy = c.ProposedBy,
                Amount = c.Amount,
                ProposedAt = c.ProposedAt,
                RespondedAt = c.RespondedAt,
                Outcome = c.Outcome,
                Note = c.Note,
            }).ToList(),
        };
    }

    public async Task<OfferDetailDto> SaveOfferAsync(OfferUpsertDto dto, Guid userId)
    {
        var offer = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Offers.ForCompany(Tenant).Include(o => o.Conditions).FirstOrDefaultAsync(o => o.Id == dto.Id)
            : null;

        if (offer is null)
        {
            // A new offer on the same property from the same buyer is a *round*, not a replacement.
            var previous = await Db.Offers.ForCompany(Tenant)
                .Where(o => o.PropertyId == dto.PropertyId && o.BuyerPartyId == dto.BuyerPartyId)
                .OrderByDescending(o => o.RoundNumber)
                .FirstOrDefaultAsync();

            offer = new Offer
            {
                Reference = await numbering.NextOfferNumberAsync(DateTime.UtcNow),
                PropertyId = dto.PropertyId,
                BuyerPartyId = dto.BuyerPartyId,
                RoundNumber = (previous?.RoundNumber ?? 0) + 1,
                PreviousOfferId = previous?.Id,
                SubmittedAt = DateTime.UtcNow,
            }.StampNew(Tenant, userId);

            Db.Offers.Add(offer);
        }
        else
        {
            Db.OfferConditions.RemoveRange(offer.Conditions);
            offer.StampUpdated(userId);
        }

        offer.ListingId = dto.ListingId;
        offer.EnquiryId = dto.EnquiryId;
        offer.SellerPartyId = dto.SellerPartyId;
        offer.AgentId = dto.AgentId;
        offer.Amount = dto.Amount;
        offer.CurrencyCode = dto.CurrencyCode ?? await CurrencyAsync();
        offer.ExpiresAt = dto.ExpiresAt;
        offer.ProposedCompletionDate = dto.ProposedCompletionDate;
        offer.Funding = dto.Funding;
        offer.DepositAmount = dto.DepositAmount;
        offer.ProofOfFundsProvided = dto.ProofOfFundsProvided;
        offer.ProofOfFundsUrl = dto.ProofOfFundsUrl;
        offer.MortgageInPrinciple = dto.MortgageInPrinciple;
        offer.IsChainFree = dto.IsChainFree;
        offer.ChainLength = dto.ChainLength;
        offer.IsBestAndFinal = dto.IsBestAndFinal;
        offer.Note = dto.Note;

        foreach (var c in dto.Conditions)
        {
            offer.Conditions.Add(new OfferCondition
            {
                Kind = c.Kind,
                Detail = c.Detail,
                SatisfyByDate = c.SatisfyByDate,
                IsSatisfied = c.IsSatisfied,
                SatisfiedOn = c.SatisfiedOn,
            }.StampNew(Tenant, userId));
        }

        // Several markets require every offer to be presented, and provably so.
        if (dto.NotifyVendor && !offer.VendorNotified)
        {
            offer.VendorNotified = true;
            offer.VendorNotifiedAt = DateTime.UtcNow;

            if (offer.SellerPartyId is not null)
            {
                await QueueNotificationAsync(
                    "offer_received",
                    $"Offer of {offer.Amount:N0} received",
                    $"On {offer.Reference}.",
                    $"/realestate/offers",
                    recipientPartyId: offer.SellerPartyId,
                    entityType: "Offer", entityId: offer.Id);
            }
        }

        // The property is visibly under offer the moment one lands.
        var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.PropertyId);
        if (property is not null && property.Status == PropertyStatus.Available)
        {
            property.Status = PropertyStatus.UnderOffer;
            property.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return (await GetOfferAsync(offer.Id))!;
    }

    public async Task<OfferDetailDto> DecideOfferAsync(OfferDecisionDto dto, Guid userId)
    {
        var offer = await Db.Offers.ForCompany(Tenant).Include(o => o.Counters).FirstOrDefaultAsync(o => o.Id == dto.OfferId)
            ?? throw new InvalidOperationException("That offer does not exist.");

        if (offer.Status is OfferStatus.Accepted or OfferStatus.Withdrawn)
            throw new InvalidOperationException("This offer has already been settled.");

        offer.Status = dto.Decision;
        offer.DecidedAt = DateTime.UtcNow;
        offer.RejectReasonCodeId = dto.RejectReasonCodeId;
        offer.Note = string.IsNullOrWhiteSpace(dto.Note) ? offer.Note : $"{offer.Note}\n{dto.Note}".Trim();
        offer.StampUpdated(userId);

        if (dto.Decision == OfferStatus.Countered && dto.CounterAmount is > 0m)
        {
            offer.Counters.Add(new OfferCounter
            {
                RoundNumber = offer.Counters.Count + 1,
                ProposedBy = "Seller",
                Amount = dto.CounterAmount.Value,
                ProposedAt = DateTime.UtcNow,
                Outcome = OfferStatus.Submitted,
                Note = dto.Note,
            }.StampNew(Tenant, userId));
        }

        if (dto.Decision == OfferStatus.Accepted)
        {
            // Every other live offer on the property lapses the moment one is accepted.
            var others = await Db.Offers.ForCompany(Tenant)
                .Where(o => o.PropertyId == offer.PropertyId && o.Id != offer.Id)
                .Where(o => o.Status == OfferStatus.Submitted || o.Status == OfferStatus.UnderConsideration || o.Status == OfferStatus.Countered)
                .ToListAsync();

            foreach (var other in others)
            {
                other.Status = OfferStatus.Lapsed;
                other.DecidedAt = DateTime.UtcNow;
                other.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();
        return (await GetOfferAsync(offer.Id))!;
    }

    // ═══ EOI ═════════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ExpressionOfInterestDto>> GetEoisAsync(ListQueryDto query, Guid? projectId)
    {
        var q = Db.ExpressionOfInterests.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, e => e.ProjectId == projectId)
            .OrderBy(e => e.PriorityNumber);

        return await PageAsync(q, query, async rows =>
        {
            var names = await PartyNamesAsync(rows.Select(r => r.PartyId));
            var projects = await ProjectNamesAsync(rows.Select(r => (Guid?)r.ProjectId));
            var partners = await Db.ChannelPartners.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);
            var receipts = await Db.Receipts.ForCompany(Tenant)
                .Where(r => rows.Where(x => x.ReceiptId.HasValue).Select(x => x.ReceiptId!.Value).Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.ReceiptNumber);

            var phones = await Db.Parties.ForCompany(Tenant)
                .Where(p => rows.Select(r => r.PartyId).Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PrimaryPhone);

            return rows.Select(e => new ExpressionOfInterestDto
            {
                Id = e.Id,
                Reference = e.Reference,
                ProjectId = e.ProjectId,
                ProjectName = projects.GetValueOrDefault(e.ProjectId, "—"),
                PartyId = e.PartyId,
                PartyName = names.GetValueOrDefault(e.PartyId, "—"),
                PartyPhone = phones.GetValueOrDefault(e.PartyId),
                PartnerName = e.ChannelPartnerId is null ? null : partners.GetValueOrDefault(e.ChannelPartnerId.Value),
                CategoryCode = e.CategoryCode,
                Amount = e.Amount,
                ReceivedAt = e.ReceivedAt,
                PriorityNumber = e.PriorityNumber,
                Status = e.Status,
                ReceiptId = e.ReceiptId,
                ReceiptNumber = e.ReceiptId is null ? null : receipts.GetValueOrDefault(e.ReceiptId.Value),
                ConvertedBookingId = e.ConvertedBookingId,
                IsRefundable = e.IsRefundable,
                RefundedAt = e.RefundedAt,
            }).ToList();
        });
    }

    public async Task<ExpressionOfInterestDto> SaveEoiAsync(
        ExpressionOfInterestDto dto, ReceiptCreateDto? payment, Guid userId)
    {
        // The priority number is the fairness guarantee at balloting, so it is allocated in strict
        // receipt order and never re-used.
        var highest = await Db.ExpressionOfInterests.ForCompany(Tenant)
            .Where(e => e.ProjectId == dto.ProjectId)
            .MaxAsync(e => (int?)e.PriorityNumber) ?? 0;

        var eoi = new ExpressionOfInterest
        {
            Reference = await numbering.NextEoiNumberAsync(DateTime.UtcNow),
            ProjectId = dto.ProjectId,
            PartyId = dto.PartyId,
            ChannelPartnerId = dto.PartnerName is null ? null : dto.Id,
            CategoryCode = dto.CategoryCode,
            Amount = dto.Amount,
            ReceivedAt = DateTime.UtcNow,
            PriorityNumber = highest + 1,
            Status = ReservationStatus.Active,
            IsRefundable = dto.IsRefundable,
        }.StampNew(Tenant, userId);

        Db.ExpressionOfInterests.Add(eoi);
        await Db.SaveChangesAsync();

        if (payment is not null)
        {
            payment.PartyId = dto.PartyId;
            payment.ProjectId = dto.ProjectId;
            payment.Amount = dto.Amount;
            var receipt = await money.CreateReceiptAsync(payment, userId);
            eoi.ReceiptId = receipt.Id;
            await Db.SaveChangesAsync();
        }

        var page = await GetEoisAsync(new ListQueryDto { PageSize = 500 }, dto.ProjectId);
        return page.Data.First(e => e.Id == eoi.Id);
    }

    public async Task<ExpressionOfInterestDto> RefundEoiAsync(Guid id, Guid userId)
    {
        var eoi = await Db.ExpressionOfInterests.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException("That expression of interest does not exist.");

        if (!eoi.IsRefundable)
            throw new InvalidOperationException("This registration was taken on non-refundable terms.");

        if (eoi.ConvertedBookingId is not null)
            throw new InvalidOperationException("This registration has already been converted into a booking.");

        eoi.Status = ReservationStatus.Refunded;
        eoi.RefundedAt = DateTime.UtcNow;
        eoi.StampUpdated(userId);

        await Db.SaveChangesAsync();

        var page = await GetEoisAsync(new ListQueryDto { PageSize = 500 }, eoi.ProjectId);
        return page.Data.First(e => e.Id == id);
    }

    // ═══ Tokens ══════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<TokenReservationDto>> GetTokensAsync(
        ListQueryDto query, Guid? projectId, ReservationStatus? status)
    {
        var q = (
            from t in Db.TokenReservations.ForCompany(Tenant)
            join u in Db.Units.ForCompany(Tenant) on t.UnitId equals u.Id
            select new { Token = t, Unit = u })
            .WhereIf(projectId.HasValue, x => x.Unit.ProjectId == projectId)
            .WhereIf(status.HasValue, x => x.Token.Status == status)
            .OrderByDescending(x => x.Token.ReceivedAt);

        return await PageAsync(q, query, async rows =>
        {
            var names = await PartyNamesAsync(rows.Select(r => r.Token.PartyId));
            var projects = await ProjectNamesAsync(rows.Select(r => (Guid?)r.Unit.ProjectId));
            var partners = await Db.ChannelPartners.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);

            var phones = await Db.Parties.ForCompany(Tenant)
                .Where(p => rows.Select(r => r.Token.PartyId).Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PrimaryPhone);

            var now = DateTime.UtcNow;

            return rows.Select(x => new TokenReservationDto
            {
                Id = x.Token.Id,
                Reference = x.Token.Reference,
                UnitId = x.Unit.Id,
                UnitNumber = x.Unit.UnitNumber,
                ProjectName = projects.GetValueOrDefault(x.Unit.ProjectId, "—"),
                PlotFileId = x.Token.PlotFileId,
                PartyId = x.Token.PartyId,
                PartyName = names.GetValueOrDefault(x.Token.PartyId, "—"),
                PartyPhone = phones.GetValueOrDefault(x.Token.PartyId),
                PartnerName = x.Token.ChannelPartnerId is null ? null : partners.GetValueOrDefault(x.Token.ChannelPartnerId.Value),
                Amount = x.Token.Amount,
                AgreedPrice = x.Token.AgreedPrice,
                ListPrice = x.Unit.TotalPrice,
                CurrencyCode = x.Token.CurrencyCode,
                ReceivedAt = x.Token.ReceivedAt,
                ValidUntil = x.Token.ValidUntil,
                HoursRemaining = Math.Max(0, (int)(x.Token.ValidUntil - now).TotalHours),
                Status = x.Token.Status,
                IsAdjustableAgainstPrice = x.Token.IsAdjustableAgainstPrice,
                IsForfeitableOnWithdrawal = x.Token.IsForfeitableOnWithdrawal,
                ConvertedBookingId = x.Token.ConvertedBookingId,
                AgreementUrl = x.Token.AgreementUrl,
            }).ToList();
        });
    }

    public async Task<TokenReservationDto> CreateTokenAsync(TokenReservationCreateDto dto, Guid userId)
    {
        await using var transaction = await Db.Database.BeginTransactionAsync();

        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == dto.UnitId)
            ?? throw new InvalidOperationException("That unit does not exist.");

        if (unit.Status is PropertyStatus.Booked or PropertyStatus.Sold or PropertyStatus.Registered or PropertyStatus.Possessed)
            throw new InvalidOperationException("This unit has already been sold.");

        if (unit.Status == PropertyStatus.Blocked)
            throw new InvalidOperationException("This unit is blocked off-market.");

        var partyId = dto.PartyId;
        if (partyId is null && dto.NewParty is not null)
        {
            var party = await crm.SavePartyAsync(dto.NewParty, userId);
            partyId = party.Id;
        }

        if (partyId is null) throw new InvalidOperationException("A token needs a customer.");

        var token = new TokenReservation
        {
            Reference = await numbering.NextTokenNumberAsync(DateTime.UtcNow),
            UnitId = unit.Id,
            PlotFileId = dto.PlotFileId,
            PartyId = partyId.Value,
            EnquiryId = dto.EnquiryId,
            ChannelPartnerId = dto.ChannelPartnerId,
            SalesExecutiveId = dto.SalesExecutiveId,
            Amount = dto.Amount,
            AgreedPrice = dto.AgreedPrice,
            CurrencyCode = unit.CurrencyCode ?? await CurrencyAsync(),
            ReceivedAt = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(dto.ValidDays <= 0 ? 7 : dto.ValidDays),
            Status = ReservationStatus.Active,
            IsAdjustableAgainstPrice = dto.IsAdjustableAgainstPrice,
            IsForfeitableOnWithdrawal = dto.IsForfeitableOnWithdrawal,
        }.StampNew(Tenant, userId);

        Db.TokenReservations.Add(token);

        unit.Status = PropertyStatus.Reserved;
        unit.StampUpdated(userId);

        await Db.SaveChangesAsync();

        if (dto.Payment is not null)
        {
            dto.Payment.PartyId = partyId;
            dto.Payment.ProjectId = unit.ProjectId;
            dto.Payment.Amount = dto.Amount;
            var receipt = await money.CreateReceiptAsync(dto.Payment, userId);
            token.ReceiptId = receipt.Id;
            await Db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        var page = await GetTokensAsync(new ListQueryDto { PageSize = 500 }, unit.ProjectId, null);
        return page.Data.First(t => t.Id == token.Id);
    }

    public async Task<TokenReservationDto> CancelTokenAsync(Guid id, Guid? reasonCodeId, bool forfeit, Guid userId)
    {
        var token = await Db.TokenReservations.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new InvalidOperationException("That token does not exist.");

        if (token.Status != ReservationStatus.Active)
            throw new InvalidOperationException("That token is no longer active.");

        token.Status = forfeit ? ReservationStatus.Forfeited : ReservationStatus.Cancelled;
        token.CancelledAt = DateTime.UtcNow;
        token.CancelReasonCodeId = reasonCodeId;
        token.StampUpdated(userId);

        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == token.UnitId);
        if (unit is not null && unit.Status == PropertyStatus.Reserved)
        {
            unit.Status = PropertyStatus.Available;
            unit.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        var page = await GetTokensAsync(new ListQueryDto { PageSize = 500 }, unit?.ProjectId, null);
        return page.Data.First(t => t.Id == id);
    }

    /// <summary>Releases units whose token window has closed. Runs alongside the hold sweep.</summary>
    public async Task<int> ExpireTokensAsync()
    {
        var now = DateTime.UtcNow;

        var lapsed = await Db.TokenReservations.ForCompany(Tenant)
            .Where(t => t.Status == ReservationStatus.Active && t.ValidUntil <= now)
            .ToListAsync();

        if (lapsed.Count == 0) return 0;

        var unitIds = lapsed.Select(t => t.UnitId).ToList();
        var units = await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id)).ToListAsync();

        foreach (var token in lapsed)
        {
            token.Status = ReservationStatus.Expired;
            token.ExpiredAt = now;

            var unit = units.FirstOrDefault(u => u.Id == token.UnitId);
            if (unit is not null && unit.Status == PropertyStatus.Reserved)
                unit.Status = PropertyStatus.Available;
        }

        await Db.SaveChangesAsync();
        return lapsed.Count;
    }
}
