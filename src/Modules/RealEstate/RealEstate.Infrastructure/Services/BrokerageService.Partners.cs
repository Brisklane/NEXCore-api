using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The channel partner network: partners, tiers, rates, lead registration, statements and contests.
///
/// Lead registration is the whole relationship in one record. A partner brings a buyer, registers
/// them, and for a fixed window that buyer is theirs — so if the same person walks into the sales
/// office next week, the commission still belongs to the partner who found them. Get this wrong
/// and partners stop bringing buyers, which is the only thing they are for. So a registration is
/// checked against every live one on the same telephone number, conflicts are refused with the
/// competing partner named, and expiry is a date rather than a judgement call.
/// </summary>
public partial class BrokerageService
{
    public async Task<PaginatedResponse<ChannelPartnerListItemDto>> GetPartnersAsync(
        ListQueryDto query, PartnerStatus? status)
    {
        var q = Db.ChannelPartners.ForCompany(Tenant)
            .WhereIf(status.HasValue, p => p.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                p => p.Name.Contains(query.Search!)
                  || (p.TradingName ?? "").Contains(query.Search!)
                  || p.Reference.Contains(query.Search!)
                  || (p.Phone ?? "").Contains(query.Search!))
            .OrderByDescending(p => p.BookingValue);

        return await PageAsync(q, query, MapPartnerListAsync);
    }

    private async Task<List<ChannelPartnerListItemDto>> MapPartnerListAsync(List<ChannelPartner> partners)
    {
        if (partners.Count == 0) return [];

        var currency = await CurrencyAsync();
        var ids = partners.Select(p => p.Id).ToList();

        var tierIds = partners.Where(p => p.TierId != null).Select(p => p.TierId!.Value).Distinct().ToList();

        var tiers = tierIds.Count == 0
            ? []
            : await Db.PartnerTiers.ForCompany(Tenant)
                .Where(t => tierIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name);

        var managers = await AgentUserNamesAsync(partners.Select(p => p.RelationshipManagerUserId));

        var authorisations = await Db.PartnerAuthorisations.ForCompany(Tenant)
            .Where(a => ids.Contains(a.ChannelPartnerId) && a.IsActive)
            .GroupBy(a => a.ChannelPartnerId)
            .Select(g => new { PartnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Count);

        var expiredDocs = await Db.PartnerDocuments.ForCompany(Tenant)
            .Where(d => ids.Contains(d.ChannelPartnerId) && d.IsMandatory)
            .Where(d => d.State != DocumentState.Verified
                     || (d.ExpiresOn != null && d.ExpiresOn < Today))
            .Select(d => d.ChannelPartnerId)
            .Distinct()
            .ToListAsync();

        return partners.Select(p => new ChannelPartnerListItemDto
        {
            Id = p.Id,
            Reference = p.Reference,
            Name = p.Name,
            TradingName = p.TradingName,
            Status = p.Status,
            TierName = p.TierId is null ? null : tiers.GetValueOrDefault(p.TierId.Value),
            ContactName = p.ContactName,
            Phone = p.Phone,
            Email = p.Email,
            City = p.City,
            RelationshipManagerName = p.RelationshipManagerUserId is null
                ? null
                : managers.GetValueOrDefault(p.RelationshipManagerUserId.Value),
            OnboardedOn = p.OnboardedOn,
            LeadsRegistered = p.LeadsRegistered,
            SiteVisitsDone = p.SiteVisitsDone,
            BookingsMade = p.BookingsMade,
            BookingValue = p.BookingValue,
            CollectionContribution = p.CollectionContribution,
            CancellationCount = p.CancellationCount,
            ConversionPercent = p.ConversionPercent,
            CommissionEarned = p.CommissionEarned,
            CommissionPaid = p.CommissionPaid,
            CommissionPending = p.CommissionPending,
            AdvanceOutstanding = p.AdvanceOutstanding,
            CurrencyCode = currency,
            AuthorisedProjectCount = authorisations.GetValueOrDefault(p.Id),
            HasExpiredDocuments = expiredDocs.Contains(p.Id),

            // A partner trading on a lapsed licence is a liability the company inherits, so it is
            // on the list row rather than buried on the detail screen.
            LicenceExpiring = p.LicenceExpiresOn is not null
                && p.LicenceExpiresOn.Value.DayNumber - Today.DayNumber <= 30,
        }).ToList();
    }

    public async Task<ChannelPartnerDetailDto?> GetPartnerAsync(Guid id)
    {
        var partner = await Db.ChannelPartners.ForCompany(Tenant)
            .Include(p => p.Users)
            .Include(p => p.Authorisations)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (partner is null) return null;

        var head = (await MapPartnerListAsync([partner]))[0];
        var currency = await CurrencyAsync();

        var detail = new ChannelPartnerDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            Name = head.Name,
            TradingName = head.TradingName,
            Status = head.Status,
            TierName = head.TierName,
            ContactName = head.ContactName,
            Phone = head.Phone,
            Email = head.Email,
            City = head.City,
            RelationshipManagerName = head.RelationshipManagerName,
            OnboardedOn = head.OnboardedOn,
            LeadsRegistered = head.LeadsRegistered,
            SiteVisitsDone = head.SiteVisitsDone,
            BookingsMade = head.BookingsMade,
            BookingValue = head.BookingValue,
            CollectionContribution = head.CollectionContribution,
            CancellationCount = head.CancellationCount,
            ConversionPercent = head.ConversionPercent,
            CommissionEarned = head.CommissionEarned,
            CommissionPaid = head.CommissionPaid,
            CommissionPending = head.CommissionPending,
            AdvanceOutstanding = head.AdvanceOutstanding,
            CurrencyCode = head.CurrencyCode,
            AuthorisedProjectCount = head.AuthorisedProjectCount,
            HasExpiredDocuments = head.HasExpiredDocuments,
            LicenceExpiring = head.LicenceExpiring,

            PartyId = partner.PartyId,
            AddressLine = partner.AddressLine,
            RegistrationNumber = partner.RegistrationNumber,
            LicenceNumber = partner.LicenceNumber,
            LicenceExpiresOn = partner.LicenceExpiresOn,
            TaxNumber = partner.TaxNumber,
            BankName = partner.BankName,
            AccountTitle = partner.AccountTitle,
            AccountNumber = partner.AccountNumber,
            BankDetailsVerified = partner.BankDetailsVerified,
            WithholdingPercent = partner.WithholdingPercent,
            SuspendedOn = partner.SuspendedOn,
            SuspensionReason = partner.SuspensionReason,
            Notes = partner.Notes,
        };

        detail.Users = partner.Users.Select(u => new PartnerUserDto
        {
            Id = u.Id,
            UserId = u.UserId,
            Name = u.Name,
            Phone = u.Phone,
            Email = u.Email,
            Designation = u.Designation,
            IsPrimary = u.IsPrimary,
            CanViewCommission = u.CanViewCommission,
            CanRegisterLeads = u.CanRegisterLeads,
            CanBookVisits = u.CanBookVisits,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            LeadsRegistered = u.LeadsRegistered,
            BookingsMade = u.BookingsMade,
        }).ToList();

        var projects = await ProjectNamesAsync(partner.Authorisations.Select(a => (Guid?)a.ProjectId));

        var planIds = partner.Authorisations.Where(a => a.CommissionPlanId != null)
            .Select(a => a.CommissionPlanId!.Value).Distinct().ToList();

        var plans = planIds.Count == 0
            ? []
            : await Db.CommissionPlans.ForCompany(Tenant)
                .Where(p => planIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        detail.Authorisations = partner.Authorisations.Select(a => new PartnerAuthorisationDto
        {
            Id = a.Id,
            ProjectId = a.ProjectId,
            ProjectName = projects.GetValueOrDefault(a.ProjectId),
            CommissionPlanId = a.CommissionPlanId,
            CommissionPlanName = a.CommissionPlanId is null ? null : plans.GetValueOrDefault(a.CommissionPlanId.Value),
            TerritoryId = a.TerritoryId,
            EffectiveFrom = a.EffectiveFrom,
            EffectiveTo = a.EffectiveTo,
            MaxBookings = a.MaxBookings,
            BookingsMade = a.BookingsMade,
            CanSeePrices = a.CanSeePrices,
            CanHoldUnits = a.CanHoldUnits,
            IsActive = a.IsActive,
        }).ToList();

        detail.Documents = partner.Documents.Select(d => new ChecklistItemDto
        {
            Id = d.Id,
            Key = d.DocumentType,
            Label = d.DocumentType,
            IsSatisfied = d.State == DocumentState.Verified
                          && (d.ExpiresOn is null || d.ExpiresOn >= Today),
            IsMandatory = d.IsMandatory,
            Url = d.Url,
            State = d.ExpiresOn is not null && d.ExpiresOn < Today ? DocumentState.Expired : d.State,
            Note = d.ExpiresOn is null ? null : $"Expires {d.ExpiresOn:dd MMM yyyy}",
        }).ToList();

        detail.RecentRegistrations = await GetRegistrationsForPartnerAsync(partner.Id, 20);

        detail.Statements = await Db.PartnerStatements.ForCompany(Tenant)
            .Where(s => s.ChannelPartnerId == partner.Id)
            .OrderByDescending(s => s.PeriodTo)
            .Take(12)
            .Select(s => new PartnerStatementDto
            {
                Id = s.Id,
                Reference = s.Reference,
                ChannelPartnerId = s.ChannelPartnerId,
                PartnerName = partner.Name,
                PeriodFrom = s.PeriodFrom,
                PeriodTo = s.PeriodTo,
                IssuedOn = s.IssuedOn,
                OpeningBalance = s.OpeningBalance,
                CommissionEarned = s.CommissionEarned,
                CommissionPaid = s.CommissionPaid,
                WithholdingDeducted = s.WithholdingDeducted,
                AdvanceRecovered = s.AdvanceRecovered,
                ClawbackApplied = s.ClawbackApplied,
                ClosingBalance = s.ClosingBalance,
                CurrencyCode = currency,
                BookingCount = s.BookingCount,
                DocumentUrl = s.DocumentUrl,
                IsPublishedToPortal = s.IsPublishedToPortal,
                IsAcknowledged = s.IsAcknowledged,
            })
            .ToListAsync();

        detail.Advances = await Db.PartnerAdvances.ForCompany(Tenant)
            .Where(a => a.ChannelPartnerId == partner.Id)
            .OrderByDescending(a => a.AdvancedOn)
            .Select(a => new PartnerAdvanceDto
            {
                Id = a.Id,
                Reference = a.Reference,
                Amount = a.Amount,
                AdvancedOn = a.AdvancedOn,
                Purpose = a.Purpose,
                RecoveryPercent = a.RecoveryPercent,
                RecoveredAmount = a.RecoveredAmount,
                OutstandingAmount = a.OutstandingAmount,
                FullyRecoveredOn = a.FullyRecoveredOn,
                IsWrittenOff = a.IsWrittenOff,
            })
            .ToListAsync();

        return detail;
    }

    public async Task<ChannelPartnerDetailDto> SavePartnerAsync(ChannelPartnerUpsertDto dto, Guid userId)
    {
        var isNew = dto.Id is null || dto.Id == Guid.Empty;

        var partner = isNew
            ? new ChannelPartner { Reference = await numbering.NextPartnerReferenceAsync() }
            : await Db.ChannelPartners.ForCompany(Tenant)
                .Include(p => p.Authorisations)
                .FirstOrDefaultAsync(p => p.Id == dto.Id)
              ?? throw new InvalidOperationException("That partner does not exist.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("A partner needs a name.");

        partner.Name = dto.Name;
        partner.TradingName = dto.TradingName;
        partner.PartyId = dto.PartyId;
        partner.TierId = dto.TierId;
        partner.ContactName = dto.ContactName;
        partner.Phone = dto.Phone;
        partner.Email = dto.Email;
        partner.AddressLine = dto.AddressLine;
        partner.City = dto.City;
        partner.GeoAreaId = dto.GeoAreaId;
        partner.RegistrationNumber = dto.RegistrationNumber;
        partner.LicenceNumber = dto.LicenceNumber;
        partner.LicenceExpiresOn = dto.LicenceExpiresOn;
        partner.TaxNumber = dto.TaxNumber;
        partner.BankName = dto.BankName;
        partner.AccountTitle = dto.AccountTitle;
        partner.AccountNumber = dto.AccountNumber;
        partner.WithholdingPercent = dto.WithholdingPercent;
        partner.RelationshipManagerUserId = dto.RelationshipManagerUserId;
        partner.Notes = dto.Notes;
        partner.Description = dto.Notes;

        if (isNew)
        {
            partner.Status = PartnerStatus.Applied;
            partner.StampNew(Tenant, userId);
            Db.ChannelPartners.Add(partner);
        }
        else
        {
            partner.StampUpdated(userId);
        }

        var keep = dto.Authorisations.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();

        foreach (var removed in partner.Authorisations.Where(a => !keep.Contains(a.Id)).ToList())
        {
            removed.StampDeleted(userId);
        }

        foreach (var row in dto.Authorisations)
        {
            var authorisation = row.Id.HasValue
                ? partner.Authorisations.FirstOrDefault(a => a.Id == row.Id)
                : null;

            if (authorisation is null)
            {
                authorisation = new PartnerAuthorisation { ChannelPartnerId = partner.Id }.StampNew(Tenant, userId);
                partner.Authorisations.Add(authorisation);
                Db.PartnerAuthorisations.Add(authorisation);
            }
            else
            {
                authorisation.StampUpdated(userId);
            }

            authorisation.ProjectId = row.ProjectId;
            authorisation.CommissionPlanId = row.CommissionPlanId;
            authorisation.TerritoryId = row.TerritoryId;
            authorisation.EffectiveFrom = row.EffectiveFrom == default ? Today : row.EffectiveFrom;
            authorisation.EffectiveTo = row.EffectiveTo;
            authorisation.MaxBookings = row.MaxBookings;
            authorisation.CanSeePrices = row.CanSeePrices;
            authorisation.CanHoldUnits = row.CanHoldUnits;
            authorisation.IsActive = row.IsActive;
        }

        await Db.SaveChangesAsync();

        return (await GetPartnerAsync(partner.Id))!;
    }

    public async Task<ChannelPartnerDetailDto> ChangePartnerStatusAsync(
        Guid id, PartnerStatus status, string? reason, Guid userId)
    {
        var partner = await RequireAsync<ChannelPartner>(id, "That partner does not exist.");

        if (status is PartnerStatus.Suspended or PartnerStatus.Blacklisted && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException(
                "Suspending or blacklisting a partner needs a reason. They will ask, and so will their lawyer.");

        if (status == PartnerStatus.Active)
        {
            var missing = await Db.PartnerDocuments.ForCompany(Tenant)
                .Where(d => d.ChannelPartnerId == id && d.IsMandatory)
                .Where(d => d.State != DocumentState.Verified)
                .Select(d => d.DocumentType)
                .ToListAsync();

            // Activating a partner whose registration or licence has not been verified means their
            // bookings carry the company's name with nothing behind them.
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    "These mandatory documents are not yet verified: " + string.Join(", ", missing) + ".");

            if (partner.LicenceExpiresOn is not null && partner.LicenceExpiresOn < Today)
                throw new InvalidOperationException(
                    $"That partner's licence expired on {partner.LicenceExpiresOn:dd MMM yyyy}.");
        }

        partner.Status = status;

        if (status is PartnerStatus.Suspended or PartnerStatus.Blacklisted)
        {
            partner.SuspendedOn = Today;
            partner.SuspensionReason = reason;
        }
        else
        {
            partner.SuspendedOn = null;
            partner.SuspensionReason = null;

            if (status == PartnerStatus.Active) partner.OnboardedOn ??= Today;
        }

        partner.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return (await GetPartnerAsync(id))!;
    }

    // ═══ Tiers and rates ═════════════════════════════════════════════════════

    public async Task<List<PartnerTierDto>> GetTiersAsync()
    {
        var tiers = await Db.PartnerTiers.ForCompany(Tenant).OrderBy(t => t.Level).ToListAsync();

        var counts = await Db.ChannelPartners.ForCompany(Tenant)
            .Where(p => p.TierId != null)
            .GroupBy(p => p.TierId!.Value)
            .Select(g => new { TierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TierId, x => x.Count);

        return tiers.Select(t => new PartnerTierDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            Level = t.Level,
            MinBookingValue = t.MinBookingValue,
            MinBookingCount = t.MinBookingCount,
            CommissionUpliftPercent = t.CommissionUpliftPercent,
            PriorityAllocation = t.PriorityAllocation,
            LeadValidityDays = t.LeadValidityDays,
            Benefits = t.Benefits,
            AutoPromote = t.AutoPromote,
            PartnerCount = counts.GetValueOrDefault(t.Id),
        }).ToList();
    }

    public async Task<PartnerTierDto> SaveTierAsync(PartnerTierDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var tier = isNew
            ? new PartnerTier { Code = await numbering.NextMasterCodeAsync(Db.PartnerTiers, "TIER") }
            : await Db.PartnerTiers.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == dto.Id)
              ?? throw new InvalidOperationException("That tier does not exist.");

        tier.Name = dto.Name;
        tier.Level = dto.Level;
        tier.MinBookingValue = dto.MinBookingValue;
        tier.MinBookingCount = dto.MinBookingCount;
        tier.CommissionUpliftPercent = dto.CommissionUpliftPercent;
        tier.PriorityAllocation = dto.PriorityAllocation;
        tier.LeadValidityDays = dto.LeadValidityDays;
        tier.Benefits = dto.Benefits;
        tier.AutoPromote = dto.AutoPromote;
        tier.IsActive = true;

        if (isNew)
        {
            tier.StampNew(Tenant, userId);
            Db.PartnerTiers.Add(tier);
        }
        else
        {
            tier.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetTiersAsync()).First(t => t.Id == tier.Id);
    }

    public async Task<List<PartnerCommissionRateDto>> GetPartnerRatesAsync(Guid? partnerId, Guid? projectId)
    {
        var rates = await Db.PartnerCommissionRates.ForCompany(Tenant)
            .WhereIf(partnerId.HasValue, r => r.ChannelPartnerId == partnerId)
            .WhereIf(projectId.HasValue, r => r.ProjectId == projectId)
            .OrderBy(r => r.ProjectId)
            .ThenBy(r => r.FromValue)
            .ToListAsync();

        var projects = await ProjectNamesAsync(rates.Select(r => (Guid?)r.ProjectId));

        var tierIds = rates.Where(r => r.PartnerTierId != null).Select(r => r.PartnerTierId!.Value).Distinct().ToList();

        var tiers = tierIds.Count == 0
            ? []
            : await Db.PartnerTiers.ForCompany(Tenant)
                .Where(t => tierIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name);

        return rates.Select(r => new PartnerCommissionRateDto
        {
            Id = r.Id,
            ChannelPartnerId = r.ChannelPartnerId,
            PartnerTierId = r.PartnerTierId,
            TierName = r.PartnerTierId is null ? null : tiers.GetValueOrDefault(r.PartnerTierId.Value),
            ProjectId = r.ProjectId,
            ProjectName = projects.GetValueOrDefault(r.ProjectId),
            SubType = r.SubType,
            FromValue = r.FromValue,
            ToValue = r.ToValue,
            CommissionPercent = r.CommissionPercent,
            RatePerSqFt = r.RatePerSqFt,
            FlatAmount = r.FlatAmount,
            Trigger = r.Trigger,
            ReleaseAtCollectionPercent = r.ReleaseAtCollectionPercent,
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo,
            IsActive = r.IsActive,
        }).ToList();
    }

    public async Task<PartnerCommissionRateDto> SavePartnerRateAsync(PartnerCommissionRateDto dto, Guid userId)
    {
        var isNew = dto.Id is null || dto.Id == Guid.Empty;

        var rate = isNew
            ? new PartnerCommissionRate()
            : await Db.PartnerCommissionRates.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == dto.Id)
              ?? throw new InvalidOperationException("That rate does not exist.");

        if (dto.CommissionPercent <= 0m && dto.FlatAmount <= 0m && dto.RatePerSqFt <= 0m)
            throw new InvalidOperationException("A rate has to say what it pays — a percentage, a rate or a flat amount.");

        rate.ChannelPartnerId = dto.ChannelPartnerId;
        rate.PartnerTierId = dto.PartnerTierId;
        rate.ProjectId = dto.ProjectId;
        rate.SubType = dto.SubType;
        rate.FromValue = dto.FromValue;
        rate.ToValue = dto.ToValue;
        rate.CommissionPercent = dto.CommissionPercent;
        rate.RatePerSqFt = dto.RatePerSqFt;
        rate.FlatAmount = dto.FlatAmount;
        rate.Trigger = dto.Trigger;
        rate.ReleaseAtCollectionPercent = dto.ReleaseAtCollectionPercent;
        rate.EffectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;
        rate.EffectiveTo = dto.EffectiveTo;
        rate.IsActive = dto.IsActive;

        if (isNew)
        {
            rate.StampNew(Tenant, userId);
            Db.PartnerCommissionRates.Add(rate);
        }
        else
        {
            rate.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetPartnerRatesAsync(rate.ChannelPartnerId, rate.ProjectId)).First(r => r.Id == rate.Id);
    }

    // ═══ Lead registration ═══════════════════════════════════════════════════

    public async Task<LeadRegistrationDto> RegisterLeadAsync(LeadRegistrationCreateDto dto, Guid userId)
    {
        var partner = await RequireAsync<ChannelPartner>(dto.ChannelPartnerId, "That partner does not exist.");

        if (partner.Status != PartnerStatus.Active)
            throw new InvalidOperationException(
                $"{partner.Name} is {partner.Status.ToString().ToLowerInvariant()} and cannot register leads.");

        var authorised = await Db.PartnerAuthorisations.ForCompany(Tenant)
            .AnyAsync(a => a.ChannelPartnerId == dto.ChannelPartnerId && a.ProjectId == dto.ProjectId
                        && a.IsActive && a.EffectiveFrom <= Today
                        && (a.EffectiveTo == null || a.EffectiveTo >= Today));

        if (!authorised)
            throw new InvalidOperationException($"{partner.Name} is not authorised to sell on that project.");

        if (string.IsNullOrWhiteSpace(dto.ProspectPhone))
            throw new InvalidOperationException(
                "A registration needs the prospect's telephone number — it is what conflicts are checked on.");

        var phone = NormalisePhone(dto.ProspectPhone);

        // The conflict check. Same number, same project, still live: the first partner keeps it,
        // and the second is told who has it so the two of them can sort it out between themselves.
        var conflict = await Db.LeadRegistrations.ForCompany(Tenant)
            .Where(r => r.ProjectId == dto.ProjectId && r.Status == LeadRegistrationStatus.Registered)
            .Where(r => r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var clash = conflict.FirstOrDefault(r => NormalisePhone(r.ProspectPhone) == phone);

        if (clash is not null)
        {
            var holder = await Db.ChannelPartners.ForCompany(Tenant)
                .Where(p => p.Id == clash.ChannelPartnerId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();

            var rejected = new LeadRegistration
            {
                Reference = await numbering.NextMasterCodeAsync(Db.LeadRegistrations, "REG"),
                ChannelPartnerId = dto.ChannelPartnerId,
                PartnerUserId = dto.PartnerUserId,
                ProjectId = dto.ProjectId,
                ProspectName = dto.ProspectName,
                ProspectPhone = dto.ProspectPhone,
                ProspectEmail = dto.ProspectEmail,
                ProspectIdentityNumber = dto.ProspectIdentityNumber,
                Status = LeadRegistrationStatus.DuplicateRejected,
                RegisteredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow,
                ConflictsWithRegistrationId = clash.Id,
                RejectionReason = $"Already registered by {holder ?? "another partner"} until {clash.ExpiresAt:dd MMM yyyy}.",
                Note = dto.Note,
            }.StampNew(Tenant, userId);

            Db.LeadRegistrations.Add(rejected);
            await Db.SaveChangesAsync();

            throw new InvalidOperationException(
                $"{dto.ProspectName} is already registered on this project by {holder ?? "another partner"}, "
                + $"valid until {clash.ExpiresAt:dd MMM yyyy}.");
        }

        var settings = await SettingsAsync();

        var validityDays = partner.TierId is null
            ? settings.PartnerLeadValidityDays
            : await Db.PartnerTiers.ForCompany(Tenant)
                .Where(t => t.Id == partner.TierId)
                .Select(t => t.LeadValidityDays)
                .FirstOrDefaultAsync();

        if (validityDays <= 0) validityDays = settings.PartnerLeadValidityDays;

        var registration = new LeadRegistration
        {
            Reference = await numbering.NextMasterCodeAsync(Db.LeadRegistrations, "REG"),
            ChannelPartnerId = dto.ChannelPartnerId,
            PartnerUserId = dto.PartnerUserId,
            ProjectId = dto.ProjectId,
            ProspectName = dto.ProspectName,
            ProspectPhone = dto.ProspectPhone,
            ProspectEmail = dto.ProspectEmail,
            ProspectIdentityNumber = dto.ProspectIdentityNumber,
            Status = LeadRegistrationStatus.Registered,
            RegisteredAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(validityDays),
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        Db.LeadRegistrations.Add(registration);

        partner.LeadsRegistered++;
        partner.StampUpdated(userId);

        if (dto.PartnerUserId is not null)
        {
            var partnerUser = await Db.PartnerUsers.ForCompany(Tenant)
                .FirstOrDefaultAsync(u => u.Id == dto.PartnerUserId);

            if (partnerUser is not null)
            {
                partnerUser.LeadsRegistered++;
                partnerUser.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();

        return (await GetRegistrationsForPartnerAsync(partner.Id, 1)).First();
    }

    /// <summary>
    /// Strips everything but the digits and keeps the last ten. Two partners writing the same
    /// number with and without a country code must not both hold a live registration on it.
    /// </summary>
    private static string NormalisePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length <= 10 ? digits : digits[^10..];
    }

    public async Task<PaginatedResponse<LeadRegistrationDto>> GetRegistrationsAsync(
        ListQueryDto query, Guid? partnerId, LeadRegistrationStatus? status)
    {
        var q = Db.LeadRegistrations.ForCompany(Tenant)
            .WhereIf(partnerId.HasValue, r => r.ChannelPartnerId == partnerId)
            .WhereIf(status.HasValue, r => r.Status == status)
            .WhereIf(query.ProjectId.HasValue, r => r.ProjectId == query.ProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                r => r.ProspectName.Contains(query.Search!) || r.ProspectPhone.Contains(query.Search!))
            .OrderByDescending(r => r.RegisteredAt);

        return await PageAsync(q, query, MapRegistrationsAsync);
    }

    private async Task<List<LeadRegistrationDto>> GetRegistrationsForPartnerAsync(Guid partnerId, int take)
    {
        var rows = await Db.LeadRegistrations.ForCompany(Tenant)
            .Where(r => r.ChannelPartnerId == partnerId)
            .OrderByDescending(r => r.RegisteredAt)
            .Take(take)
            .ToListAsync();

        return await MapRegistrationsAsync(rows);
    }

    private async Task<List<LeadRegistrationDto>> MapRegistrationsAsync(List<LeadRegistration> rows)
    {
        if (rows.Count == 0) return [];

        var partnerIds = rows.Select(r => r.ChannelPartnerId)
            .Concat(rows.Where(r => r.ConflictsWithRegistrationId != null).Select(r => r.ChannelPartnerId))
            .Distinct().ToList();

        var partners = await Db.ChannelPartners.ForCompany(Tenant)
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var projects = await ProjectNamesAsync(rows.Select(r => (Guid?)r.ProjectId));

        var partnerUserIds = rows.Where(r => r.PartnerUserId != null)
            .Select(r => r.PartnerUserId!.Value).Distinct().ToList();

        var partnerUsers = partnerUserIds.Count == 0
            ? []
            : await Db.PartnerUsers.ForCompany(Tenant)
                .Where(u => partnerUserIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name);

        var conflictIds = rows.Where(r => r.ConflictsWithRegistrationId != null)
            .Select(r => r.ConflictsWithRegistrationId!.Value).Distinct().ToList();

        var conflicts = conflictIds.Count == 0
            ? []
            : await Db.LeadRegistrations.ForCompany(Tenant)
                .Where(r => conflictIds.Contains(r.Id))
                .Select(r => new { r.Id, r.ChannelPartnerId })
                .ToListAsync();

        var conflictPartnerIds = conflicts.Select(c => c.ChannelPartnerId).Distinct().ToList();

        var conflictPartners = conflictPartnerIds.Count == 0
            ? []
            : await Db.ChannelPartners.ForCompany(Tenant)
                .Where(p => conflictPartnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        return rows.Select(r =>
        {
            var days = (int)(r.ExpiresAt - DateTime.UtcNow).TotalDays;
            var conflict = r.ConflictsWithRegistrationId is null
                ? null
                : conflicts.FirstOrDefault(c => c.Id == r.ConflictsWithRegistrationId);

            return new LeadRegistrationDto
            {
                Id = r.Id,
                Reference = r.Reference,
                ChannelPartnerId = r.ChannelPartnerId,
                PartnerName = partners.GetValueOrDefault(r.ChannelPartnerId, "—"),
                PartnerUserName = r.PartnerUserId is null ? null : partnerUsers.GetValueOrDefault(r.PartnerUserId.Value),
                ProjectId = r.ProjectId,
                ProjectName = projects.GetValueOrDefault(r.ProjectId, "—"),
                ProspectName = r.ProspectName,
                ProspectPhone = r.ProspectPhone,
                ProspectEmail = r.ProspectEmail,
                ProspectIdentityNumber = r.ProspectIdentityNumber,
                Status = r.Status,
                RegisteredAt = r.RegisteredAt,
                ExpiresAt = r.ExpiresAt,
                DaysRemaining = Math.Max(0, days),
                IsExpiringSoon = r.Status == LeadRegistrationStatus.Registered && days is >= 0 and <= 14,
                ConflictsWithRegistrationId = r.ConflictsWithRegistrationId,
                ConflictsWithPartnerName = conflict is null
                    ? null
                    : conflictPartners.GetValueOrDefault(conflict.ChannelPartnerId),
                RejectionReason = r.RejectionReason,
                EnquiryId = r.EnquiryId,
                SiteVisitId = r.SiteVisitId,
                BookingId = r.BookingId,
                ConvertedOn = r.ConvertedOn,
                ExtensionCount = r.ExtensionCount,
                Note = r.Note,
            };
        }).ToList();
    }

    public async Task<LeadRegistrationDto> ExtendRegistrationAsync(Guid id, int days, Guid userId)
    {
        var registration = await RequireAsync<LeadRegistration>(id, "That registration does not exist.");

        if (registration.Status != LeadRegistrationStatus.Registered)
            throw new InvalidOperationException(
                $"That registration is {SplitCamelCase(registration.Status.ToString())} and cannot be extended.");

        if (days is <= 0 or > 180)
            throw new InvalidOperationException("An extension is between one and a hundred and eighty days.");

        // Extending forever turns a registration into a permanent claim on a buyer who may never
        // have spoken to the partner again. Two extensions, then it goes back on the market.
        if (registration.ExtensionCount >= 2)
            throw new InvalidOperationException(
                "This registration has already been extended twice. Let it lapse, or re-register it fresh.");

        registration.ExpiresAt = registration.ExpiresAt.AddDays(days);
        registration.ExtensionCount++;
        registration.ExpiryWarningSent = false;
        registration.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return (await MapRegistrationsAsync([registration]))[0];
    }

    public async Task<int> ExpireRegistrationsAsync()
    {
        var due = await Db.LeadRegistrations.ForCompany(Tenant)
            .Where(r => r.Status == LeadRegistrationStatus.Registered && r.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var registration in due)
        {
            registration.Status = LeadRegistrationStatus.Expired;
            registration.StampUpdated(Guid.Empty);
        }

        // Warn on the ones about to go, so a partner has a chance to convert or extend before the
        // buyer they found becomes anybody's.
        var warning = await Db.LeadRegistrations.ForCompany(Tenant)
            .Where(r => r.Status == LeadRegistrationStatus.Registered && !r.ExpiryWarningSent)
            .Where(r => r.ExpiresAt < DateTime.UtcNow.AddDays(14))
            .ToListAsync();

        foreach (var registration in warning)
        {
            await QueueNotificationAsync(
                "partner.registration.expiring",
                $"Registration for {registration.ProspectName} expires soon",
                $"It lapses on {registration.ExpiresAt:dd MMM yyyy}.",
                "/realestate/brokerage/partners/registrations",
                entityType: nameof(LeadRegistration),
                entityId: registration.Id,
                severity: AlertSeverity.Warning);

            registration.ExpiryWarningSent = true;
            registration.StampUpdated(Guid.Empty);
        }

        await Db.SaveChangesAsync();

        return due.Count;
    }

    // ═══ Partner money ═══════════════════════════════════════════════════════

    public async Task<PaginatedResponse<PartnerCommissionEntryDto>> GetPartnerCommissionAsync(
        ListQueryDto query, Guid? partnerId, CommissionStatus? status)
    {
        var q = Db.PartnerCommissionEntries.ForCompany(Tenant)
            .WhereIf(partnerId.HasValue, e => e.ChannelPartnerId == partnerId)
            .WhereIf(status.HasValue, e => e.Status == status)
            .WhereIf(query.FromDate.HasValue, e => e.AccruedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, e => e.AccruedOn <= query.ToDate)
            .OrderByDescending(e => e.AccruedOn);

        return await PageAsync(q, query, MapCommissionEntriesAsync);
    }

    private async Task<List<PartnerCommissionEntryDto>> MapCommissionEntriesAsync(List<PartnerCommissionEntry> entries)
    {
        if (entries.Count == 0) return [];

        var currency = await CurrencyAsync();

        var partners = await Db.ChannelPartners.ForCompany(Tenant)
            .Where(p => entries.Select(e => e.ChannelPartnerId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var bookingIds = entries.Select(e => e.BookingId).Distinct().ToList();

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => bookingIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Reference, b.UnitId, b.ProjectId, b.PrimaryApplicantPartyId })
            .ToListAsync();

        var names = await PartyNamesAsync(bookings.Select(b => b.PrimaryApplicantPartyId));
        var projects = await ProjectNamesAsync(bookings.Select(b => (Guid?)b.ProjectId));

        var unitIds = bookings.Where(b => b.UnitId != null).Select(b => b.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant)
                .Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        return entries.Select(e =>
        {
            var booking = bookings.FirstOrDefault(b => b.Id == e.BookingId);

            return new PartnerCommissionEntryDto
            {
                Id = e.Id,
                ChannelPartnerId = e.ChannelPartnerId,
                PartnerName = partners.GetValueOrDefault(e.ChannelPartnerId, "—"),
                BookingId = e.BookingId,
                BookingReference = booking?.Reference ?? "—",
                UnitNumber = booking?.UnitId is null ? null : units.GetValueOrDefault(booking.UnitId.Value),
                ProjectName = booking is null ? "—" : projects.GetValueOrDefault(booking.ProjectId, "—"),
                CustomerName = booking is null ? "—" : names.GetValueOrDefault(booking.PrimaryApplicantPartyId, "—"),
                BookingValue = e.BookingValue,
                CommissionPercent = e.CommissionPercent,
                GrossCommission = e.GrossCommission,
                CollectionPercent = e.CollectionPercent,
                EarnedAmount = e.EarnedAmount,
                WithholdingAmount = e.WithholdingAmount,
                NetAmount = e.NetAmount,
                PaidAmount = e.PaidAmount,
                PendingAmount = RealEstateMapper.Money(Math.Max(0m, e.NetAmount - e.PaidAmount)),
                ClawedBackAmount = e.ClawedBackAmount,
                CurrencyCode = currency,
                Status = e.Status,
                AccruedOn = e.AccruedOn,
                LastPaidOn = e.LastPaidOn,
                Note = e.Note,
            };
        }).ToList();
    }

    public async Task<PartnerStatementDto> GeneratePartnerStatementAsync(
        Guid partnerId, DateOnly from, DateOnly to, Guid userId)
    {
        var partner = await RequireAsync<ChannelPartner>(partnerId, "That partner does not exist.");

        if (to < from) throw new InvalidOperationException("The period ends before it begins.");

        var entries = await Db.PartnerCommissionEntries.ForCompany(Tenant)
            .Where(e => e.ChannelPartnerId == partnerId && e.AccruedOn >= from && e.AccruedOn <= to)
            .ToListAsync();

        // The opening balance is the closing balance of the last statement, not a recomputation.
        // Recomputing invites the two statements to disagree, and the partner will spot it.
        var opening = await Db.PartnerStatements.ForCompany(Tenant)
            .Where(s => s.ChannelPartnerId == partnerId && s.PeriodTo < from)
            .OrderByDescending(s => s.PeriodTo)
            .Select(s => (decimal?)s.ClosingBalance)
            .FirstOrDefaultAsync() ?? 0m;

        var earned = RealEstateMapper.Money(entries.Sum(e => e.EarnedAmount));
        var paid = RealEstateMapper.Money(entries.Sum(e => e.PaidAmount));
        var withheld = RealEstateMapper.Money(entries.Sum(e => e.WithholdingAmount));
        var clawback = RealEstateMapper.Money(entries.Sum(e => e.ClawedBackAmount));

        var advanceRecovered = await Db.CommissionPayouts.ForCompany(Tenant)
            .Where(p => p.ChannelPartnerId == partnerId && p.PaidOn >= from && p.PaidOn <= to)
            .SumAsync(p => (decimal?)p.AdvanceRecovered) ?? 0m;

        var statement = new PartnerStatement
        {
            Reference = await numbering.NextMasterCodeAsync(Db.PartnerStatements, "PST"),
            ChannelPartnerId = partnerId,
            PeriodFrom = from,
            PeriodTo = to,
            IssuedOn = Today,
            OpeningBalance = opening,
            CommissionEarned = earned,
            CommissionPaid = paid,
            WithholdingDeducted = withheld,
            AdvanceRecovered = RealEstateMapper.Money(advanceRecovered),
            ClawbackApplied = clawback,
            ClosingBalance = RealEstateMapper.Money(
                opening + earned - paid - withheld - advanceRecovered - clawback),
            BookingCount = entries.Select(e => e.BookingId).Distinct().Count(),
        }.StampNew(Tenant, userId);

        Db.PartnerStatements.Add(statement);

        foreach (var entry in entries)
        {
            entry.StatementId = statement.Id;
            entry.StampUpdated(userId);
        }

        await QueueNotificationAsync(
            "partner.statement.issued",
            $"Statement {statement.Reference}",
            $"{from:dd MMM} to {to:dd MMM yyyy}. Closing balance {statement.ClosingBalance:N0}.",
            "/realestate/brokerage/partners",
            recipientPartyId: partner.PartyId,
            entityType: nameof(PartnerStatement),
            entityId: statement.Id);

        await Db.SaveChangesAsync();

        var currency = await CurrencyAsync();

        return new PartnerStatementDto
        {
            Id = statement.Id,
            Reference = statement.Reference,
            ChannelPartnerId = partnerId,
            PartnerName = partner.Name,
            PeriodFrom = statement.PeriodFrom,
            PeriodTo = statement.PeriodTo,
            IssuedOn = statement.IssuedOn,
            OpeningBalance = statement.OpeningBalance,
            CommissionEarned = statement.CommissionEarned,
            CommissionPaid = statement.CommissionPaid,
            WithholdingDeducted = statement.WithholdingDeducted,
            AdvanceRecovered = statement.AdvanceRecovered,
            ClawbackApplied = statement.ClawbackApplied,
            ClosingBalance = statement.ClosingBalance,
            CurrencyCode = currency,
            BookingCount = statement.BookingCount,
            Entries = await MapCommissionEntriesAsync(entries),
        };
    }

    public async Task<PartnerAdvanceDto> CreateAdvanceAsync(PartnerAdvanceDto dto, Guid userId)
    {
        if (dto.Amount <= 0m)
            throw new InvalidOperationException("An advance has to be for more than nothing.");

        if (dto.RecoveryPercent is <= 0m or > 100m)
            throw new InvalidOperationException(
                "An advance needs a recovery percentage, or it will never be recovered from anything.");

        var partnerId = await Db.PartnerAdvances.ForCompany(Tenant)
            .Where(a => a.Id == dto.Id)
            .Select(a => (Guid?)a.ChannelPartnerId)
            .FirstOrDefaultAsync();

        var advance = new PartnerAdvance
        {
            Reference = await numbering.NextMasterCodeAsync(Db.PartnerAdvances, "ADV"),
            ChannelPartnerId = partnerId ?? Guid.Empty,
            Amount = RealEstateMapper.Money(dto.Amount),
            AdvancedOn = dto.AdvancedOn == default ? Today : dto.AdvancedOn,
            Purpose = dto.Purpose,
            RecoveryPercent = dto.RecoveryPercent,
            OutstandingAmount = RealEstateMapper.Money(dto.Amount),
        };

        if (advance.ChannelPartnerId == Guid.Empty)
            throw new InvalidOperationException("An advance has to be against a named partner.");

        var partner = await RequireAsync<ChannelPartner>(advance.ChannelPartnerId, "That partner does not exist.");

        var approval = await RaiseApprovalAsync(
            nameof(PartnerAdvance), advance.Id, advance.Reference, advance.Amount,
            $"Advance of {advance.Amount:N0} to {partner.Name}", userId);

        advance.ApprovalRequestId = approval?.Id;
        advance.StampNew(Tenant, userId);

        Db.PartnerAdvances.Add(advance);

        partner.AdvanceOutstanding = RealEstateMapper.Money(partner.AdvanceOutstanding + advance.Amount);
        partner.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return new PartnerAdvanceDto
        {
            Id = advance.Id,
            Reference = advance.Reference,
            Amount = advance.Amount,
            AdvancedOn = advance.AdvancedOn,
            Purpose = advance.Purpose,
            RecoveryPercent = advance.RecoveryPercent,
            RecoveredAmount = advance.RecoveredAmount,
            OutstandingAmount = advance.OutstandingAmount,
            FullyRecoveredOn = advance.FullyRecoveredOn,
            IsWrittenOff = advance.IsWrittenOff,
        };
    }

    // ═══ Contests ════════════════════════════════════════════════════════════

    public async Task<List<PartnerContestDto>> GetContestsAsync(bool activeOnly)
    {
        var contests = await Db.PartnerContests.ForCompany(Tenant)
            .WhereIf(activeOnly, c => !c.IsClosed && c.EndsOn >= Today)
            .OrderByDescending(c => c.StartsOn)
            .ToListAsync();

        if (contests.Count == 0) return [];

        var projects = await ProjectNamesAsync(contests.Select(c => c.ProjectId));

        var winnerIds = contests.Where(c => c.WinnerPartnerId != null)
            .Select(c => c.WinnerPartnerId!.Value).Distinct().ToList();

        var winners = winnerIds.Count == 0
            ? []
            : await Db.ChannelPartners.ForCompany(Tenant)
                .Where(p => winnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        var result = new List<PartnerContestDto>();

        foreach (var contest in contests)
        {
            result.Add(new PartnerContestDto
            {
                Id = contest.Id,
                Name = contest.Name,
                ProjectId = contest.ProjectId,
                ProjectName = contest.ProjectId is null ? null : projects.GetValueOrDefault(contest.ProjectId.Value),
                StartsOn = contest.StartsOn,
                EndsOn = contest.EndsOn,
                MetricKey = contest.MetricKey,
                TargetValue = contest.TargetValue,
                PrizeDescription = contest.PrizeDescription,
                PrizeAmount = contest.PrizeAmount,
                IsPublished = contest.IsPublished,
                IsClosed = contest.IsClosed,
                WinnerPartnerName = contest.WinnerPartnerId is null
                    ? null
                    : winners.GetValueOrDefault(contest.WinnerPartnerId.Value),
                Rules = contest.Rules,
                Leaderboard = await BuildLeaderboardAsync(contest),
            });
        }

        return result;
    }

    /// <summary>
    /// The leaderboard, computed live from bookings inside the contest window. A contest whose
    /// standings are stale is one nobody believes, and a partner network runs on believing it.
    /// </summary>
    private async Task<List<ContestLeaderboardRowDto>> BuildLeaderboardAsync(PartnerContest contest)
    {
        var live = new[]
        {
            BookingStatus.Confirmed, BookingStatus.AgreementSigned, BookingStatus.Defaulting,
            BookingStatus.PossessionOffered, BookingStatus.Possessed, BookingStatus.Completed,
        };

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.ChannelPartnerId != null && live.Contains(b.Status))
            .Where(b => b.BookingDate >= contest.StartsOn && b.BookingDate <= contest.EndsOn)
            .WhereIf(contest.ProjectId.HasValue, b => b.ProjectId == contest.ProjectId)
            .Select(b => new { b.ChannelPartnerId, b.TotalConsideration, b.NetSalePrice, b.TotalPaid })
            .ToListAsync();

        if (bookings.Count == 0) return [];

        var partnerIds = bookings.Select(b => b.ChannelPartnerId!.Value).Distinct().ToList();

        var partners = await Db.ChannelPartners.ForCompany(Tenant)
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var rows = bookings
            .GroupBy(b => b.ChannelPartnerId!.Value)
            .Select(g =>
            {
                var value = contest.MetricKey switch
                {
                    "BookingCount" => g.Count(),
                    "Collection" => g.Sum(b => b.TotalPaid),
                    _ => g.Sum(b => b.TotalConsideration > 0m ? b.TotalConsideration : b.NetSalePrice),
                };

                return new ContestLeaderboardRowDto
                {
                    ChannelPartnerId = g.Key,
                    PartnerName = partners.GetValueOrDefault(g.Key, "—"),
                    Value = RealEstateMapper.Money(value),
                    PercentOfTarget = RealEstateMapper.Percent(value, contest.TargetValue),
                    BookingCount = g.Count(),
                };
            })
            .OrderByDescending(r => r.Value)
            .ToList();

        for (var i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;

        return rows;
    }

    public async Task<PartnerContestDto> SaveContestAsync(PartnerContestDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var contest = isNew
            ? new PartnerContest()
            : await Db.PartnerContests.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
              ?? throw new InvalidOperationException("That contest does not exist.");

        if (dto.EndsOn < dto.StartsOn)
            throw new InvalidOperationException("The contest ends before it begins.");

        // Changing the rules of a contest already running is how a partner network stops trusting
        // one. Once published, only the closing state may change.
        if (!isNew && contest.IsPublished)
        {
            var rulesChanged = contest.MetricKey != dto.MetricKey
                            || contest.TargetValue != dto.TargetValue
                            || contest.StartsOn != dto.StartsOn
                            || contest.EndsOn != dto.EndsOn;

            if (rulesChanged)
                throw new InvalidOperationException(
                    "This contest has been published. Its dates, metric and target cannot be changed — "
                    + "close it and run a new one instead.");
        }

        contest.Name = dto.Name;
        contest.ProjectId = dto.ProjectId;
        contest.StartsOn = dto.StartsOn;
        contest.EndsOn = dto.EndsOn;
        contest.MetricKey = dto.MetricKey;
        contest.TargetValue = dto.TargetValue;
        contest.PrizeDescription = dto.PrizeDescription;
        contest.PrizeAmount = dto.PrizeAmount;
        contest.IsPublished = dto.IsPublished;
        contest.IsClosed = dto.IsClosed;
        contest.Rules = dto.Rules;
        contest.Description = dto.Rules;

        if (dto.IsClosed && contest.WinnerPartnerId is null)
        {
            var leaderboard = await BuildLeaderboardAsync(contest);
            contest.WinnerPartnerId = leaderboard.FirstOrDefault()?.ChannelPartnerId;
        }

        if (isNew)
        {
            contest.StampNew(Tenant, userId);
            Db.PartnerContests.Add(contest);
        }
        else
        {
            contest.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetContestsAsync(false)).First(c => c.Id == contest.Id);
    }

    // ═══ Partner portal ══════════════════════════════════════════════════════

    public async Task<PartnerPortalHomeDto> GetPartnerPortalHomeAsync(Guid partnerId)
    {
        var partner = await Db.ChannelPartners.ForCompany(Tenant)
            .Include(p => p.Authorisations)
            .FirstOrDefaultAsync(p => p.Id == partnerId)
            ?? throw new InvalidOperationException("That partner does not exist.");

        var currency = await CurrencyAsync();
        var monthStart = new DateOnly(Today.Year, Today.Month, 1);

        var home = new PartnerPortalHomeDto
        {
            PartnerName = partner.Name,
            CurrencyCode = currency,
            CommissionEarned = partner.CommissionEarned,
            CommissionPaid = partner.CommissionPaid,
            CommissionPending = partner.CommissionPending,
        };

        if (partner.TierId is not null)
        {
            home.TierName = await Db.PartnerTiers.ForCompany(Tenant)
                .Where(t => t.Id == partner.TierId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
        }

        var authorisedProjectIds = partner.Authorisations
            .Where(a => a.IsActive && a.EffectiveFrom <= Today && (a.EffectiveTo == null || a.EffectiveTo >= Today))
            .Select(a => a.ProjectId)
            .Distinct()
            .ToList();

        if (authorisedProjectIds.Count > 0)
        {
            var projects = await Db.Projects.ForCompany(Tenant)
                .Where(p => authorisedProjectIds.Contains(p.Id))
                .ToListAsync();

            // A partner sees prices only where the authorisation says so. Some developers deal
            // with partners on a net-rate basis and do not publish the list price to them at all.
            var canSeePrices = partner.Authorisations.Any(a => a.CanSeePrices);

            home.AuthorisedProjects = projects.Select(p => new ProjectListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Kind = p.Kind,
                Status = p.Status,
                City = p.City,
                HeroImageUrl = p.HeroImageUrl,
                CurrencyCode = currency,
                PromisedPossessionDate = p.PromisedPossessionDate,
                TotalSalesValue = canSeePrices ? p.TotalSalesValue : 0m,
            }).ToList();
        }

        var registrations = await Db.LeadRegistrations.ForCompany(Tenant)
            .Where(r => r.ChannelPartnerId == partnerId)
            .OrderByDescending(r => r.RegisteredAt)
            .Take(50)
            .ToListAsync();

        home.Registrations = await MapRegistrationsAsync(registrations);
        home.ActiveRegistrations = home.Registrations.Count(r => r.Status == LeadRegistrationStatus.Registered);
        home.ExpiringRegistrations = home.Registrations.Count(r => r.IsExpiringSoon);

        var visits = await Db.SiteVisits.ForCompany(Tenant)
            .Where(v => v.ChannelPartnerId == partnerId)
            .Where(v => v.ScheduledAt >= DateTime.UtcNow)
            .OrderBy(v => v.ScheduledAt)
            .Take(20)
            .Select(v => new SiteVisitListItemDto
            {
                Id = v.Id,
                Reference = v.Reference,
                ProjectId = v.ProjectId,
                ScheduledAt = v.ScheduledAt,
                Status = v.Status,
                IsRevisit = v.IsRevisit,
                VisitNumber = v.VisitNumber,
                GuestCount = v.GuestCount,
            })
            .ToListAsync();

        home.UpcomingVisits = visits;

        home.SiteVisitsThisMonth = await Db.SiteVisits.ForCompany(Tenant)
            .CountAsync(v => v.ChannelPartnerId == partnerId
                          && v.ScheduledAt >= monthStart.ToDateTime(TimeOnly.MinValue));

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.ChannelPartnerId == partnerId && b.Status != BookingStatus.Cancelled)
            .Where(b => b.BookingDate >= monthStart)
            .Select(b => new { b.TotalConsideration, b.NetSalePrice })
            .ToListAsync();

        home.BookingsThisMonth = bookings.Count;
        home.BookingValue = RealEstateMapper.Money(
            bookings.Sum(b => b.TotalConsideration > 0m ? b.TotalConsideration : b.NetSalePrice));

        var entries = await Db.PartnerCommissionEntries.ForCompany(Tenant)
            .Where(e => e.ChannelPartnerId == partnerId)
            .OrderByDescending(e => e.AccruedOn)
            .Take(50)
            .ToListAsync();

        home.CommissionEntries = await MapCommissionEntriesAsync(entries);

        home.Statements = await Db.PartnerStatements.ForCompany(Tenant)
            .Where(s => s.ChannelPartnerId == partnerId && s.IsPublishedToPortal)
            .OrderByDescending(s => s.PeriodTo)
            .Take(12)
            .Select(s => new PartnerStatementDto
            {
                Id = s.Id,
                Reference = s.Reference,
                ChannelPartnerId = s.ChannelPartnerId,
                PartnerName = partner.Name,
                PeriodFrom = s.PeriodFrom,
                PeriodTo = s.PeriodTo,
                IssuedOn = s.IssuedOn,
                OpeningBalance = s.OpeningBalance,
                CommissionEarned = s.CommissionEarned,
                CommissionPaid = s.CommissionPaid,
                WithholdingDeducted = s.WithholdingDeducted,
                AdvanceRecovered = s.AdvanceRecovered,
                ClawbackApplied = s.ClawbackApplied,
                ClosingBalance = s.ClosingBalance,
                CurrencyCode = currency,
                BookingCount = s.BookingCount,
                DocumentUrl = s.DocumentUrl,
                IsPublishedToPortal = s.IsPublishedToPortal,
                IsAcknowledged = s.IsAcknowledged,
            })
            .ToListAsync();

        if (authorisedProjectIds.Count > 0)
        {
            home.Collateral = await Db.ContentAssets.ForCompany(Tenant)
                .Where(a => a.ProjectId != null && authorisedProjectIds.Contains(a.ProjectId.Value))
                .Where(a => a.IsExternallyShareable && a.IsCurrent)
                .OrderByDescending(a => a.CreatedAt)
                .Take(30)
                .Select(a => new ContentAssetDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    AssetType = a.AssetType,
                    ProjectId = a.ProjectId,
                    Url = a.Url,
                    ThumbnailUrl = a.ThumbnailUrl,
                    LanguageCode = a.LanguageCode,
                    DownloadCount = a.DownloadCount,
                    IsCurrent = a.IsCurrent,
                })
                .ToListAsync();
        }

        home.Contests = (await GetContestsAsync(true)).Where(c => c.IsPublished).ToList();

        return home;
    }
}
