using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Tenancies: the agreement, the rent schedule, referencing and the deposit.
///
/// The rent schedule is generated rather than typed, because pro-rating a part month and applying
/// a rent-free period are exactly the two things an agent gets wrong by hand, and the tenant
/// notices. The deposit carries a statutory registration deadline that starts ticking the day the
/// money is received — in several markets missing it costs three times the deposit, so it is
/// surfaced as a countdown rather than a field somebody remembers to check.
/// </summary>
public partial class LeasingService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), ILeasingService
{
    // ═══ Tenancies ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<TenancyListItemDto>> GetTenanciesAsync(TenancySearchDto query)
    {
        var today = Today;

        var q = Db.Tenancies.ForCompany(Tenant)
            .WhereIf(query.Statuses.Count > 0, t => query.Statuses.Contains(t.Status))
            .WhereIf(query.Kinds.Count > 0, t => query.Kinds.Contains(t.Kind))
            .WhereIf(query.PropertyId.HasValue, t => t.PropertyId == query.PropertyId)
            .WhereIf(query.LandlordId.HasValue, t => t.LandlordId == query.LandlordId)
            .WhereIf(query.OfficeId.HasValue, t => t.OfficeId == query.OfficeId)
            .WhereIf(query.ManagedByUserId.HasValue, t => t.ManagedByUserId == query.ManagedByUserId)
            .WhereIf(query.InArrearsOnly == true, t => t.ArrearsAmount > 0m)
            .WhereIf(query.ExpiringOnly == true,
                t => t.EndDate != null && t.EndDate <= today.AddDays(query.ExpiringWithinDays) && t.EndDate >= today)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), t => t.Reference.Contains(query.Search!));

        if (query.TenantPartyId is not null)
        {
            var mine = Db.TenancyParties.ForCompany(Tenant)
                .Where(p => p.PartyId == query.TenantPartyId)
                .Select(p => p.TenancyId);

            q = q.Where(t => mine.Contains(t.Id));
        }

        var ordered = query.SortBy switch
        {
            "arrears" => q.OrderByDescending(t => t.ArrearsAmount),
            "expiry" => q.OrderBy(t => t.EndDate),
            "rent" => query.SortDescending ? q.OrderByDescending(t => t.Rent) : q.OrderBy(t => t.Rent),
            _ => q.OrderByDescending(t => t.StartDate),
        };

        return await PageAsync(ordered, query, MapTenancyListAsync);
    }

    private async Task<List<TenancyListItemDto>> MapTenancyListAsync(List<Tenancy> tenancies)
    {
        if (tenancies.Count == 0) return [];

        var today = Today;
        var unit = await AreaUnitAsync();
        var ids = tenancies.Select(t => t.Id).ToList();
        var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var leads = await Db.TenancyParties.ForCompany(Tenant)
            .Where(p => ids.Contains(p.TenancyId) && p.IsLeadTenant)
            .Select(p => new { p.TenancyId, p.PartyId })
            .ToListAsync();

        var tenants = await Db.Parties.ForCompany(Tenant)
            .Where(p => leads.Select(l => l.PartyId).Contains(p.Id))
            .ToListAsync();

        var landlordIds = tenancies.Where(t => t.LandlordId.HasValue).Select(t => t.LandlordId!.Value).Distinct().ToList();

        var landlords = landlordIds.Count == 0
            ? []
            : await Db.Landlords.ForCompany(Tenant)
                .Where(l => landlordIds.Contains(l.Id))
                .Join(Db.Parties.ForCompany(Tenant), l => l.PartyId, p => p.Id, (l, p) => new { l.Id, p.DisplayName })
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName);

        var managers = await AgentUserNamesAsync(tenancies.Select(t => t.ManagedByUserId));

        var deposits = await Db.SecurityDeposits.ForCompany(Tenant)
            .Where(d => ids.Contains(d.TenancyId))
            .Select(d => new { d.TenancyId, d.IsRegistered, d.RegistrationDeadline })
            .ToListAsync();

        var certificates = await Db.ComplianceCertificates.ForCompany(Tenant)
            .Where(c => propertyIds.Contains(c.PropertyId) && c.IsCurrent && (c.ExpiresOn < today || c.HasFailures))
            .GroupBy(c => c.PropertyId)
            .Select(g => new { PropertyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PropertyId, x => x.Count);

        var criticalDates = await Db.CriticalDates.ForCompany(Tenant)
            .Where(d => d.TenancyId != null && ids.Contains(d.TenancyId.Value) && !d.IsActioned && d.DueDate <= today.AddDays(90))
            .GroupBy(d => d.TenancyId!.Value)
            .Select(g => new { TenancyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenancyId, x => x.Count);

        return tenancies.Select(t =>
        {
            properties.TryGetValue(t.PropertyId, out var property);

            var lead = leads.FirstOrDefault(l => l.TenancyId == t.Id);
            var person = lead is null ? null : tenants.FirstOrDefault(p => p.Id == lead.PartyId);
            var deposit = deposits.FirstOrDefault(d => d.TenancyId == t.Id);

            var daysToExpiry = t.EndDate is null ? (int?)null : t.EndDate.Value.DayNumber - today.DayNumber;

            return new TenancyListItemDto
            {
                Id = t.Id,
                Reference = t.Reference,
                Kind = t.Kind,
                Status = t.Status,
                PropertyId = t.PropertyId,
                PropertyReference = property?.Reference ?? "—",
                AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                UnitNumber = property?.UnitNumber,
                BuildingName = property?.Name,
                SubType = property?.SubType,
                Area = RealEstateMapper.AreaOrNull(property?.SaleableAreaSqFt, unit),

                TenantName = person is null ? "—" : RealEstateMapper.DisplayName(person),
                TenantPhone = person?.PrimaryPhone,
                LandlordId = t.LandlordId,
                LandlordName = t.LandlordId is null ? null : landlords.GetValueOrDefault(t.LandlordId.Value),
                ManagedByName = t.ManagedByUserId is null ? null : managers.GetValueOrDefault(t.ManagedByUserId.Value),

                StartDate = t.StartDate,
                EndDate = t.EndDate,
                DaysToExpiry = daysToExpiry,
                IsExpiringSoon = daysToExpiry is >= 0 and <= 90,

                Rent = t.Rent,
                Frequency = t.Frequency,
                AnnualRent = t.AnnualRent ?? AnnualiseRent(t.Rent, t.Frequency),
                RentPerSqFt = t.RentPerSqFt,
                CurrencyCode = t.CurrencyCode,

                DepositAmount = t.DepositAmount,
                DepositRegistered = deposit?.IsRegistered ?? false,
                DepositRegistrationOverdue = deposit is not null && !deposit.IsRegistered
                                             && deposit.RegistrationDeadline is not null
                                             && deposit.RegistrationDeadline < today,

                ArrearsAmount = t.ArrearsAmount,
                DaysInArrears = t.DaysInArrears,
                MonthsInArrears = t.Rent > 0m ? RealEstateMapper.Money(t.ArrearsAmount / t.Rent, 1) : 0m,
                NextDueDate = t.NextDueDate,

                ManagementService = t.ManagementService,
                ManagementFeePercent = t.ManagementFeePercent,
                ServiceChargeApplies = t.ServiceChargeApplies,
                TurnoverRentApplies = t.TurnoverRentApplies,

                OpenComplianceIssues = certificates.GetValueOrDefault(t.PropertyId),
                CriticalDatesDue = criticalDates.GetValueOrDefault(t.Id),
            };
        }).ToList();
    }

    /// <summary>Rent stated at whatever frequency the lease uses, expressed as an annual figure.</summary>
    private static decimal AnnualiseRent(decimal rent, RentFrequency frequency) => RealEstateMapper.Money(frequency switch
    {
        RentFrequency.Weekly => rent * 52m,
        RentFrequency.Fortnightly => rent * 26m,
        RentFrequency.Monthly => rent * 12m,
        RentFrequency.Quarterly => rent * 4m,
        RentFrequency.HalfYearly => rent * 2m,
        RentFrequency.Yearly => rent,
        _ => rent * 12m,
    });

    private static int MonthsPerPeriod(RentFrequency frequency) => frequency switch
    {
        RentFrequency.Monthly => 1,
        RentFrequency.Quarterly => 3,
        RentFrequency.HalfYearly => 6,
        RentFrequency.Yearly => 12,
        _ => 1,
    };

    protected async Task<Dictionary<Guid, string>> AgentUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    public async Task<TenancyDetailDto?> GetTenancyAsync(Guid id)
    {
        var tenancy = await Db.Tenancies.ForCompany(Tenant)
            .Include(t => t.Parties)
            .Include(t => t.RentCharges)
            .Include(t => t.Options)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenancy is null) return null;

        var head = (await MapTenancyListAsync([tenancy]))[0];
        var today = Today;

        var detail = new TenancyDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            Kind = head.Kind,
            Status = head.Status,
            PropertyId = head.PropertyId,
            PropertyReference = head.PropertyReference,
            AddressOneLine = head.AddressOneLine,
            UnitNumber = head.UnitNumber,
            BuildingName = head.BuildingName,
            SubType = head.SubType,
            Area = head.Area,
            TenantName = head.TenantName,
            TenantPhone = head.TenantPhone,
            LandlordId = head.LandlordId,
            LandlordName = head.LandlordName,
            ManagedByName = head.ManagedByName,
            StartDate = head.StartDate,
            EndDate = head.EndDate,
            DaysToExpiry = head.DaysToExpiry,
            IsExpiringSoon = head.IsExpiringSoon,
            Rent = head.Rent,
            Frequency = head.Frequency,
            AnnualRent = head.AnnualRent,
            RentPerSqFt = head.RentPerSqFt,
            CurrencyCode = head.CurrencyCode,
            DepositAmount = head.DepositAmount,
            DepositRegistered = head.DepositRegistered,
            DepositRegistrationOverdue = head.DepositRegistrationOverdue,
            ArrearsAmount = head.ArrearsAmount,
            DaysInArrears = head.DaysInArrears,
            MonthsInArrears = head.MonthsInArrears,
            NextDueDate = head.NextDueDate,
            ManagementService = head.ManagementService,
            ManagementFeePercent = head.ManagementFeePercent,
            ServiceChargeApplies = head.ServiceChargeApplies,
            TurnoverRentApplies = head.TurnoverRentApplies,
            OpenComplianceIssues = head.OpenComplianceIssues,
            CriticalDatesDue = head.CriticalDatesDue,

            UnitId = tenancy.UnitId,
            InstructionId = tenancy.InstructionId,
            TermMonths = tenancy.TermMonths,
            RollsToPeriodic = tenancy.RollsToPeriodic,
            ActualEndDate = tenancy.ActualEndDate,
            PaymentDay = tenancy.PaymentDay,
            PaidInAdvance = tenancy.PaidInAdvance,
            Escalation = tenancy.Escalation,
            EscalationPercent = tenancy.EscalationPercent,
            EscalationMonths = tenancy.EscalationMonths,
            NextEscalationDate = tenancy.NextEscalationDate,
            AdvanceRentMonths = tenancy.AdvanceRentMonths,
            TotalCharged = tenancy.TotalCharged,
            TotalPaid = tenancy.TotalPaid,
            ServiceChargeBasis = tenancy.ServiceChargeBasis,
            ServiceChargePercent = tenancy.ServiceChargePercent,
            UtilitiesRecharged = tenancy.UtilitiesRecharged,
            PropertyTaxRecharged = tenancy.PropertyTaxRecharged,
            InsuranceRecharged = tenancy.InsuranceRecharged,
            RepairAuthorityLimit = tenancy.RepairAuthorityLimit,
            PetsAllowed = tenancy.PetsAllowed,
            SmokingAllowed = tenancy.SmokingAllowed,
            SublettingAllowed = tenancy.SublettingAllowed,
            MaxOccupants = tenancy.MaxOccupants,
            PermittedUse = tenancy.PermittedUse,
            NoticePeriodDaysTenant = tenancy.NoticePeriodDaysTenant,
            NoticePeriodDaysLandlord = tenancy.NoticePeriodDaysLandlord,
            AgreementUrl = tenancy.AgreementUrl,
            SignedOn = tenancy.SignedOn,
            IsRegistered = tenancy.IsRegistered,
            RegistrationNumber = tenancy.RegistrationNumber,
            PreviousTenancyId = tenancy.PreviousTenancyId,
            Notes = tenancy.Notes,
        };

        var partyIds = tenancy.Parties.Select(p => p.PartyId).ToList();

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => partyIds.Contains(p.Id))
            .ToListAsync();

        detail.Parties = tenancy.Parties.Select(p =>
        {
            var person = people.FirstOrDefault(x => x.Id == p.PartyId);

            return new TenancyPartyDto
            {
                Id = p.Id,
                PartyId = p.PartyId,
                Name = person is null ? "—" : RealEstateMapper.DisplayName(person),
                Phone = person?.PrimaryPhone,
                Email = person?.PrimaryEmail,
                Role = p.Role,
                IsLeadTenant = p.IsLeadTenant,
                IsJointlyAndSeverallyLiable = p.IsJointlyAndSeverallyLiable,
                LiabilitySharePercent = p.LiabilitySharePercent,
                FromDate = p.FromDate,
                ToDate = p.ToDate,
                KycStatus = p.KycStatus,
                ReferencingOutcome = p.ReferencingOutcome,
            };
        }).ToList();

        detail.RentCharges = tenancy.RentCharges
            .OrderBy(c => c.SequenceNumber)
            .Select(MapRentCharge)
            .ToList();

        detail.Options = tenancy.Options.OrderBy(o => o.OptionDate).Select(o => new LeaseOptionDto
        {
            Id = o.Id,
            Kind = o.Kind,
            HeldBy = o.HeldBy,
            OptionDate = o.OptionDate,
            NoticeWindowFrom = o.NoticeWindowFrom,
            NoticeWindowTo = o.NoticeWindowTo,
            NoticeMonths = o.NoticeMonths,
            Conditions = o.Conditions,
            OptionPrice = o.OptionPrice,
            PenaltyAmount = o.PenaltyAmount,
            IsExercised = o.IsExercised,
            ExercisedOn = o.ExercisedOn,
            IsLapsed = o.IsLapsed,
            IsWindowOpen = !o.IsExercised && !o.IsLapsed && today >= o.NoticeWindowFrom && today <= o.NoticeWindowTo,
            DaysToWindowClose = o.NoticeWindowTo.DayNumber - today.DayNumber,
        }).ToList();

        detail.CriticalDates = await MapCriticalDatesAsync(
            await Db.CriticalDates.ForCompany(Tenant).Where(d => d.TenancyId == id).OrderBy(d => d.DueDate).ToListAsync());

        detail.Escalations = await Db.EscalationRules.ForCompany(Tenant)
            .Where(e => e.TenancyId == id)
            .OrderBy(e => e.SortOrder).ThenBy(e => e.EffectiveFrom)
            .Select(e => new EscalationRuleDto
            {
                Id = e.Id,
                Kind = e.Kind,
                EffectiveFrom = e.EffectiveFrom,
                Percent = e.Percent,
                FixedAmount = e.FixedAmount,
                IndexName = e.IndexName,
                FloorPercent = e.FloorPercent,
                CapPercent = e.CapPercent,
                IntervalMonths = e.IntervalMonths,
                IsCompounding = e.IsCompounding,
                IsApplied = e.IsApplied,
                AppliedOn = e.AppliedOn,
                ResultingRent = e.ResultingRent,
                SortOrder = e.SortOrder,
            })
            .ToListAsync();

        detail.Recoveries = await Db.RecoveryCharges.ForCompany(Tenant)
            .Where(r => r.TenancyId == id)
            .Select(r => new RecoveryChargeDto
            {
                Id = r.Id,
                ChargeType = r.ChargeType,
                Label = r.Label,
                Basis = r.Basis,
                Rate = r.Rate,
                Amount = r.Amount,
                Frequency = r.Frequency,
                EffectiveFrom = r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                IsTaxable = r.IsTaxable,
                TaxPercent = r.TaxPercent,
                IncludeInRentRun = r.IncludeInRentRun,
                IsActive = r.IsActive,
            })
            .ToListAsync();

        var deposit = await Db.SecurityDeposits.ForCompany(Tenant)
            .Include(d => d.Deductions)
            .FirstOrDefaultAsync(d => d.TenancyId == id);

        if (deposit is not null) detail.Deposit = await MapDepositAsync(deposit);

        if (tenancy.ReferencingCaseId is not null)
        {
            var referencing = await Db.ReferencingCases.ForCompany(Tenant)
                .Include(r => r.Checks)
                .FirstOrDefaultAsync(r => r.Id == tenancy.ReferencingCaseId);

            if (referencing is not null) detail.Referencing = await MapReferencingAsync(referencing);
        }

        if (tenancy.TurnoverRentApplies)
        {
            var term = await Db.TurnoverRentTerms.ForCompany(Tenant)
                .Include(t => t.Slabs)
                .FirstOrDefaultAsync(t => t.TenancyId == id);

            if (term is not null) detail.TurnoverRent = MapTurnoverTerm(term);
        }

        detail.Inspections = await MapInspectionsAsync(
            await Db.MoveInspections.ForCompany(Tenant)
                .Include(i => i.Items)
                .Where(i => i.TenancyId == id)
                .OrderByDescending(i => i.InspectedAt)
                .ToListAsync());

        detail.Certificates = await MapCertificatesAsync(
            await Db.ComplianceCertificates.ForCompany(Tenant)
                .Where(c => c.PropertyId == tenancy.PropertyId && c.IsCurrent)
                .OrderBy(c => c.ExpiresOn)
                .ToListAsync());

        // Ageing on the rent account, which is the number a landlord asks about first.
        var open = tenancy.RentCharges.Where(c => c.Balance > 0m).ToList();

        detail.Ageing = new AgeingBucketsDto
        {
            Current = RealEstateMapper.Money(open.Where(c => c.DueDate >= today).Sum(c => c.Balance)),
            Days1To30 = RealEstateMapper.Money(open.Where(c => Bucket(c.DueDate, today) == 1).Sum(c => c.Balance)),
            Days31To60 = RealEstateMapper.Money(open.Where(c => Bucket(c.DueDate, today) == 2).Sum(c => c.Balance)),
            Days61To90 = RealEstateMapper.Money(open.Where(c => Bucket(c.DueDate, today) == 3).Sum(c => c.Balance)),
            Days90Plus = RealEstateMapper.Money(open.Where(c => Bucket(c.DueDate, today) == 4).Sum(c => c.Balance)),
        };

        detail.Ageing.Total = RealEstateMapper.Money(
            detail.Ageing.Current + detail.Ageing.Days1To30 + detail.Ageing.Days31To60
            + detail.Ageing.Days61To90 + detail.Ageing.Days90Plus);

        detail.Timeline = tenancy.RentCharges
            .Where(c => c.SettledOn is not null)
            .OrderByDescending(c => c.SettledOn)
            .Take(20)
            .Select(c => new TimelineEntryDto
            {
                Id = c.Id,
                OccurredAt = c.SettledOn!.Value.ToDateTime(TimeOnly.MinValue),
                Kind = "payment",
                Title = $"Rent {c.PeriodFrom:dd MMM} – {c.PeriodTo:dd MMM} settled",
                Detail = $"{c.PaidAmount:N0} {tenancy.CurrencyCode}",
                Icon = "payments",
            })
            .ToList();

        return detail;
    }

    private static int Bucket(DateOnly dueDate, DateOnly today)
    {
        var days = today.DayNumber - dueDate.DayNumber;
        return days <= 0 ? 0 : days <= 30 ? 1 : days <= 60 ? 2 : days <= 90 ? 3 : 4;
    }

    private static RentChargeDto MapRentCharge(RentCharge c) => new()
    {
        Id = c.Id,
        SequenceNumber = c.SequenceNumber,
        PeriodFrom = c.PeriodFrom,
        PeriodTo = c.PeriodTo,
        DueDate = c.DueDate,
        Amount = c.Amount,
        TaxAmount = c.TaxAmount,
        TotalAmount = c.TotalAmount,
        PaidAmount = c.PaidAmount,
        Balance = c.Balance,
        Status = c.Status,
        DaysOverdue = c.DaysOverdue,
        LateFeeAccrued = c.LateFeeAccrued,
        IsProRated = c.IsProRated,
        IsRentFree = c.IsRentFree,
        SettledOn = c.SettledOn,
    };

    public async Task<TenancyDetailDto> CreateTenancyAsync(TenancyCreateDto dto, Guid userId)
    {
        var property = await RequireAsync<Property>(dto.PropertyId, "That property does not exist.");

        if (dto.Parties.Count == 0)
            throw new InvalidOperationException("A tenancy needs at least one tenant.");

        // Two live tenancies on one unit is a double-let. It is refused here, where it is cheap.
        var clash = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => t.PropertyId == dto.PropertyId
                     && t.Status != TenancyStatus.Ended
                     && t.Status != TenancyStatus.Abandoned
                     && (t.EndDate == null || t.EndDate >= dto.StartDate))
            .WhereIf(dto.UnitId.HasValue, t => t.UnitId == dto.UnitId)
            .Select(t => new TenancyClash(t.Reference, t.StartDate, t.EndDate))
            .FirstOrDefaultAsync();

        if (clash is not null)
        {
            throw new InvalidOperationException(
                $"{clash.Reference} already runs on this property from {clash.StartDate:dd MMM yyyy}" +
                $"{(clash.EndDate is null ? " with no end date" : $" to {clash.EndDate:dd MMM yyyy}")}.");
        }

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var settings = await SettingsAsync();
        var today = Today;

        var endDate = dto.EndDate ?? (dto.TermMonths is > 0 ? dto.StartDate.AddMonths(dto.TermMonths.Value).AddDays(-1) : null);

        var tenancy = new Tenancy
        {
            Reference = await numbering.NextTenancyNumberAsync(DateTime.UtcNow),
            PropertyId = dto.PropertyId,
            UnitId = dto.UnitId,
            LandlordId = dto.LandlordId,
            InstructionId = dto.InstructionId,
            OfficeId = dto.OfficeId,
            ManagedByUserId = dto.ManagedByUserId ?? userId,
            Kind = dto.Kind,
            Status = TenancyStatus.Application,
            StartDate = dto.StartDate,
            EndDate = endDate,
            TermMonths = dto.TermMonths,
            RollsToPeriodic = dto.RollsToPeriodic,
            Rent = dto.Rent,
            Frequency = dto.Frequency,
            CurrencyCode = dto.CurrencyCode ?? property.CurrencyCode ?? settings.CurrencyCode,
            PaymentDay = dto.PaymentDay <= 0 ? dto.StartDate.Day : dto.PaymentDay,
            PaidInAdvance = dto.PaidInAdvance,
            AnnualRent = AnnualiseRent(dto.Rent, dto.Frequency),
            Escalation = dto.Escalation,
            EscalationPercent = dto.EscalationPercent,
            EscalationMonths = dto.EscalationMonths <= 0 ? 12 : dto.EscalationMonths,
            DepositAmount = dto.DepositAmount,
            AdvanceRentMonths = dto.AdvanceRentMonths,
            ManagementService = dto.ManagementService,
            ManagementFeePercent = dto.ManagementFeePercent,
            RepairAuthorityLimit = dto.RepairAuthorityLimit,
            ServiceChargeApplies = dto.ServiceChargeApplies,
            ServiceChargeBasis = dto.ServiceChargeBasis,
            ServiceChargePercent = dto.ServiceChargePercent,
            TurnoverRentApplies = dto.TurnoverRentApplies,
            UtilitiesRecharged = dto.UtilitiesRecharged,
            PropertyTaxRecharged = dto.PropertyTaxRecharged,
            InsuranceRecharged = dto.InsuranceRecharged,
            PetsAllowed = dto.PetsAllowed,
            SmokingAllowed = dto.SmokingAllowed,
            SublettingAllowed = dto.SublettingAllowed,
            MaxOccupants = dto.MaxOccupants,
            PermittedUse = dto.PermittedUse,
            NoticePeriodDaysTenant = dto.NoticePeriodDaysTenant,
            NoticePeriodDaysLandlord = dto.NoticePeriodDaysLandlord,
            Notes = dto.Notes,
        }.StampNew(Tenant, userId);

        var area = property.SaleableAreaSqFt ?? property.CoveredAreaSqFt;
        if (area is > 0m) tenancy.RentPerSqFt = RealEstateMapper.Money(tenancy.AnnualRent!.Value / area.Value);

        if (dto.Escalation != EscalationKind.None)
            tenancy.NextEscalationDate = dto.StartDate.AddMonths(tenancy.EscalationMonths);

        Db.Tenancies.Add(tenancy);

        await SaveTenancyChildrenAsync(tenancy, dto, userId);
        await Db.SaveChangesAsync();

        if (dto.GenerateSchedule) await BuildScheduleAsync(tenancy, dto.Concessions, userId);

        // The deposit exists from the moment the tenancy does, because its registration clock
        // starts on receipt and the desk needs to see the countdown immediately.
        if (dto.DepositAmount > 0m)
        {
            var lead = dto.Parties.FirstOrDefault(p => p.IsLeadTenant) ?? dto.Parties[0];

            var deposit = new SecurityDeposit
            {
                TenancyId = tenancy.Id,
                PropertyId = dto.PropertyId,
                PartyId = lead.PartyId,
                Amount = dto.DepositAmount,
                CurrencyCode = tenancy.CurrencyCode,
                ReceivedOn = today,
                Scheme = dto.DepositScheme,
                RegistrationDeadline = dto.DepositScheme == DepositScheme.HeldInClientAccount
                    ? null
                    : today.AddDays(settings.DepositRegistrationDays),
            }.StampNew(Tenant, userId);

            Db.SecurityDeposits.Add(deposit);
            await Db.SaveChangesAsync();

            tenancy.SecurityDepositId = deposit.Id;
        }

        await CreateCriticalDatesAsync(tenancy, userId);
        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetTenancyAsync(tenancy.Id))!;
    }

    private async Task SaveTenancyChildrenAsync(Tenancy tenancy, TenancyCreateDto dto, Guid userId)
    {
        var lead = dto.Parties.Any(p => p.IsLeadTenant) ? null : dto.Parties.FirstOrDefault();

        foreach (var p in dto.Parties)
        {
            tenancy.Parties.Add(new TenancyParty
            {
                PartyId = p.PartyId,
                Role = p.Role,
                IsLeadTenant = p.IsLeadTenant || ReferenceEquals(p, lead),
                IsJointlyAndSeverallyLiable = p.IsJointlyAndSeverallyLiable,

                // Joint and several means each is liable for the whole, so a share is meaningless
                // and storing one invites somebody to bill on it.
                LiabilitySharePercent = p.IsJointlyAndSeverallyLiable ? 100m : p.LiabilitySharePercent,
                FromDate = p.FromDate ?? tenancy.StartDate,
                KycStatus = p.KycStatus,
            }.StampNew(Tenant, userId));

            var hasRole = await Db.PartyRoles.ForCompany(Tenant)
                .AnyAsync(r => r.PartyId == p.PartyId && r.Kind == PartyRoleKind.Tenant && r.IsActive);

            if (!hasRole)
            {
                Db.PartyRoles.Add(new PartyRole
                {
                    PartyId = p.PartyId,
                    Kind = PartyRoleKind.Tenant,
                    FromDate = tenancy.StartDate,
                    IsActive = true,
                    ContextType = "Tenancy",
                    ContextId = tenancy.Id,
                }.StampNew(Tenant, userId));
            }
        }

        foreach (var o in dto.Options)
        {
            tenancy.Options.Add(new LeaseOption
            {
                Kind = o.Kind,
                HeldBy = o.HeldBy,
                OptionDate = o.OptionDate,
                NoticeWindowFrom = o.NoticeWindowFrom,
                NoticeWindowTo = o.NoticeWindowTo,
                NoticeMonths = o.NoticeMonths,
                Conditions = o.Conditions,
                OptionPrice = o.OptionPrice,
                PenaltyAmount = o.PenaltyAmount,
            }.StampNew(Tenant, userId));
        }

        foreach (var r in dto.Recoveries)
        {
            Db.RecoveryCharges.Add(new RecoveryCharge
            {
                TenancyId = tenancy.Id,
                PropertyId = tenancy.PropertyId,
                ChargeType = r.ChargeType,
                Label = r.Label,
                Basis = r.Basis,
                Rate = r.Rate,
                Amount = r.Amount,
                Frequency = r.Frequency,
                EffectiveFrom = r.EffectiveFrom == default ? tenancy.StartDate : r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                IsTaxable = r.IsTaxable,
                TaxPercent = r.TaxPercent,
                IncludeInRentRun = r.IncludeInRentRun,
            }.StampNew(Tenant, userId));
        }

        if (dto.TurnoverRent is not null && dto.TurnoverRentApplies)
            await SaveTurnoverTermInternalAsync(tenancy.Id, dto.TurnoverRent, userId);

        foreach (var c in dto.Concessions)
        {
            Db.LeaseConcessions.Add(new LeaseConcession
            {
                TenancyId = tenancy.Id,
                ConcessionType = c.ConcessionType,
                FromDate = c.FromDate,
                ToDate = c.ToDate,
                Amount = c.Amount,
                DiscountPercent = c.DiscountPercent,
                IsAmortised = c.IsAmortised,
                AmortisationMonths = c.AmortisationMonths,

                // The straight-lined charge a landlord's accounts need. IFRS 16 spreads an
                // incentive over the term rather than dropping it in the month it was given.
                MonthlyAmortisation = c.IsAmortised && c.AmortisationMonths > 0
                    ? RealEstateMapper.Money(c.Amount / c.AmortisationMonths)
                    : 0m,

                ClawbackOnEarlyBreak = c.ClawbackOnEarlyBreak,
                Note = c.Note,
            }.StampNew(Tenant, userId));
        }
    }

    /// <summary>
    /// Diaries the dates a lease turns on. Break notices and option windows are missed by
    /// forgetting, not by disagreement, so every one of them becomes an alertable row.
    /// </summary>
    private async Task CreateCriticalDatesAsync(Tenancy tenancy, Guid userId)
    {
        void Add(string title, string type, DateOnly due, int alertDays, AlertSeverity severity)
        {
            if (due < Today) return;

            Db.CriticalDates.Add(new CriticalDate
            {
                TenancyId = tenancy.Id,
                PropertyId = tenancy.PropertyId,
                Title = title,
                DateType = type,
                DueDate = due,
                AlertDaysBefore = alertDays,
                OwnerUserId = tenancy.ManagedByUserId,
                Severity = severity,
            }.StampNew(Tenant, userId));
        }

        if (tenancy.EndDate is not null)
        {
            Add($"{tenancy.Reference} expires", "LeaseExpiry", tenancy.EndDate.Value, 120, AlertSeverity.Warning);

            // The last day a renewal can realistically be agreed, working back from the notice
            // the landlord has to give.
            Add($"{tenancy.Reference} renewal decision", "RenewalDecision",
                tenancy.EndDate.Value.AddDays(-tenancy.NoticePeriodDaysLandlord), 30, AlertSeverity.Warning);
        }

        if (tenancy.NextEscalationDate is not null)
            Add($"{tenancy.Reference} rent escalation", "RentEscalation", tenancy.NextEscalationDate.Value, 60, AlertSeverity.Info);

        foreach (var option in tenancy.Options)
        {
            Add($"{option.Kind} notice window opens", "OptionWindowOpens", option.NoticeWindowFrom, 30, AlertSeverity.Info);

            // The close is the one that costs money. Missing it loses the break for the term.
            Add($"{option.Kind} notice window closes", "OptionWindowCloses", option.NoticeWindowTo, 45, AlertSeverity.Critical);
        }

        await Task.CompletedTask;
    }

    public async Task<TenancyDetailDto> UpdateTenancyAsync(Guid id, TenancyCreateDto dto, Guid userId)
    {
        var tenancy = await Db.Tenancies.ForCompany(Tenant)
            .Include(t => t.Parties)
            .Include(t => t.Options)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new InvalidOperationException("That tenancy does not exist.");

        if (tenancy.Status is TenancyStatus.Ended)
            throw new InvalidOperationException("An ended tenancy cannot be edited. Create a renewal instead.");

        var rentChanged = tenancy.Rent != dto.Rent || tenancy.Frequency != dto.Frequency;

        tenancy.Kind = dto.Kind;
        tenancy.LandlordId = dto.LandlordId;
        tenancy.ManagedByUserId = dto.ManagedByUserId ?? tenancy.ManagedByUserId;
        tenancy.EndDate = dto.EndDate ?? tenancy.EndDate;
        tenancy.TermMonths = dto.TermMonths;
        tenancy.RollsToPeriodic = dto.RollsToPeriodic;
        tenancy.Rent = dto.Rent;
        tenancy.Frequency = dto.Frequency;
        tenancy.AnnualRent = AnnualiseRent(dto.Rent, dto.Frequency);
        tenancy.PaymentDay = dto.PaymentDay;
        tenancy.PaidInAdvance = dto.PaidInAdvance;
        tenancy.Escalation = dto.Escalation;
        tenancy.EscalationPercent = dto.EscalationPercent;
        tenancy.EscalationMonths = dto.EscalationMonths;
        tenancy.ManagementService = dto.ManagementService;
        tenancy.ManagementFeePercent = dto.ManagementFeePercent;
        tenancy.RepairAuthorityLimit = dto.RepairAuthorityLimit;
        tenancy.ServiceChargeApplies = dto.ServiceChargeApplies;
        tenancy.ServiceChargeBasis = dto.ServiceChargeBasis;
        tenancy.ServiceChargePercent = dto.ServiceChargePercent;
        tenancy.TurnoverRentApplies = dto.TurnoverRentApplies;
        tenancy.UtilitiesRecharged = dto.UtilitiesRecharged;
        tenancy.PropertyTaxRecharged = dto.PropertyTaxRecharged;
        tenancy.InsuranceRecharged = dto.InsuranceRecharged;
        tenancy.PetsAllowed = dto.PetsAllowed;
        tenancy.SmokingAllowed = dto.SmokingAllowed;
        tenancy.SublettingAllowed = dto.SublettingAllowed;
        tenancy.MaxOccupants = dto.MaxOccupants;
        tenancy.PermittedUse = dto.PermittedUse;
        tenancy.NoticePeriodDaysTenant = dto.NoticePeriodDaysTenant;
        tenancy.NoticePeriodDaysLandlord = dto.NoticePeriodDaysLandlord;
        tenancy.Notes = dto.Notes;
        tenancy.StampUpdated(userId);

        await Db.SaveChangesAsync();

        // A rent change only affects what has not been charged yet. Rewriting a settled period
        // would silently alter what a tenant already paid.
        if (rentChanged && dto.GenerateSchedule)
            await RegenerateScheduleAsync(id, userId);

        return (await GetTenancyAsync(id))!;
    }

    public async Task<TenancyDetailDto> ChangeStatusAsync(Guid id, TenancyStatus status, Guid userId)
    {
        var tenancy = await RequireAsync<Tenancy>(id, "That tenancy does not exist.");
        var today = Today;

        if (status == TenancyStatus.Active && tenancy.Status != TenancyStatus.Active)
        {
            // A tenancy going live without its statutory certificates is a criminal offence in
            // several markets, not a compliance nicety.
            var blocking = await Db.ComplianceSchedules.ForCompany(Tenant)
                .Where(s => s.PropertyId == tenancy.PropertyId && s.BlocksLettingWhenOverdue && s.NextDueOn < today)
                .Select(s => s.Kind)
                .ToListAsync();

            if (blocking.Count > 0)
                throw new InvalidOperationException($"These certificates are overdue and block letting: {string.Join(", ", blocking)}.");

            var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == tenancy.PropertyId);

            if (property is not null)
            {
                property.Occupancy = OccupancyState.Tenanted;
                property.CurrentTenancyId = tenancy.Id;
                property.StampUpdated(userId);
            }

            // Close the void, so void-days statistics are true rather than open forever.
            var vacancy = await Db.VoidRecords.ForCompany(Tenant)
                .Where(v => v.PropertyId == tenancy.PropertyId && !v.IsClosed)
                .OrderByDescending(v => v.VacantFrom)
                .FirstOrDefaultAsync();

            if (vacancy is not null)
            {
                vacancy.LetFrom = tenancy.StartDate;
                vacancy.NewTenancyId = tenancy.Id;
                vacancy.DaysVoid = Math.Max(0, tenancy.StartDate.DayNumber - vacancy.VacantFrom.DayNumber);
                vacancy.LostRent = RealEstateMapper.Money(vacancy.AskingRent / 30m * vacancy.DaysVoid);
                vacancy.IsClosed = true;
                vacancy.StampUpdated(userId);
            }
        }

        if (status == TenancyStatus.Ended && tenancy.Status != TenancyStatus.Ended)
        {
            tenancy.ActualEndDate ??= today;

            var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == tenancy.PropertyId);

            if (property is not null)
            {
                property.Occupancy = OccupancyState.Vacant;
                property.CurrentTenancyId = null;
                property.StampUpdated(userId);
            }

            // Open a void the moment it goes empty. A void nobody opened is a void nobody chases.
            Db.VoidRecords.Add(new VoidRecord
            {
                PropertyId = tenancy.PropertyId,
                UnitId = tenancy.UnitId,
                VacantFrom = tenancy.ActualEndDate.Value,
                PreviousTenancyId = tenancy.Id,
                AskingRent = tenancy.Rent,
            }.StampNew(Tenant, userId));
        }

        tenancy.Status = status;
        tenancy.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetTenancyAsync(id))!;
    }

    // ═══ Rent schedule ═══════════════════════════════════════════════════════

    public async Task<List<RentChargeDto>> RegenerateScheduleAsync(Guid tenancyId, Guid userId)
    {
        var tenancy = await Db.Tenancies.ForCompany(Tenant)
            .Include(t => t.RentCharges)
            .FirstOrDefaultAsync(t => t.Id == tenancyId)
            ?? throw new InvalidOperationException("That tenancy does not exist.");

        var concessions = await Db.LeaseConcessions.ForCompany(Tenant)
            .Where(c => c.TenancyId == tenancyId)
            .Select(c => new LeaseConcessionDto
            {
                ConcessionType = c.ConcessionType,
                FromDate = c.FromDate,
                ToDate = c.ToDate,
                Amount = c.Amount,
                DiscountPercent = c.DiscountPercent,
            })
            .ToListAsync();

        await BuildScheduleAsync(tenancy, concessions, userId);
        await Db.SaveChangesAsync();

        var charges = await Db.RentCharges.ForCompany(Tenant)
            .Where(c => c.TenancyId == tenancyId)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync();

        return charges.Select(MapRentCharge).ToList();
    }

    /// <summary>
    /// Builds the rent schedule. Part periods at each end are pro-rated by day, rent-free periods
    /// are charged at zero rather than skipped so the sequence stays continuous, and anything
    /// already paid is left exactly as it stands.
    /// </summary>
    private async Task BuildScheduleAsync(Tenancy tenancy, List<LeaseConcessionDto> concessions, Guid userId)
    {
        var existing = await Db.RentCharges.ForCompany(Tenant)
            .Where(c => c.TenancyId == tenancy.Id)
            .ToListAsync();

        // A settled or part-settled period is history. It is never rewritten.
        var settled = existing.Where(c => c.PaidAmount > 0m || c.Status == InstalmentStatus.Paid).ToList();
        var replaceable = existing.Except(settled).ToList();

        Db.RentCharges.RemoveRange(replaceable);

        var from = settled.Count == 0 ? tenancy.StartDate : settled.Max(c => c.PeriodTo).AddDays(1);

        // Periodic tenancies have no end. Twenty-four periods ahead is enough for a screen and a
        // cash-flow, and the rent run extends it as time passes.
        var to = tenancy.EndDate ?? from.AddMonths(24 * MonthsPerPeriod(tenancy.Frequency));

        if (from > to) return;

        var months = MonthsPerPeriod(tenancy.Frequency);
        var sequence = settled.Count == 0 ? 0 : settled.Max(c => c.SequenceNumber);
        var cursor = from;

        while (cursor <= to)
        {
            var periodEnd = NextPeriodEnd(cursor, tenancy.Frequency, tenancy.PaymentDay);
            if (periodEnd > to) periodEnd = to;

            var wholeDays = periodEnd.DayNumber - cursor.DayNumber + 1;

            var fullPeriodEnd = NextPeriodEnd(cursor, tenancy.Frequency, tenancy.PaymentDay);
            var fullDays = fullPeriodEnd.DayNumber - cursor.DayNumber + 1;

            var isPartial = wholeDays < fullDays;

            var amount = isPartial
                ? RealEstateMapper.Money(tenancy.Rent * wholeDays / fullDays)
                : tenancy.Rent;

            // Rent-free periods are charged at zero, not omitted — a gap in the sequence reads as
            // a missing invoice to everyone who looks at it later.
            var concession = concessions.FirstOrDefault(c =>
                c.ConcessionType == "RentFree" && cursor >= c.FromDate && cursor <= c.ToDate);

            var isRentFree = concession is not null;

            if (isRentFree) amount = 0m;
            else
            {
                var discount = concessions.FirstOrDefault(c =>
                    c.ConcessionType == "Discount" && cursor >= c.FromDate && cursor <= c.ToDate);

                if (discount?.DiscountPercent is > 0m)
                    amount = RealEstateMapper.Money(amount * (1m - discount.DiscountPercent.Value / 100m));
            }

            Db.RentCharges.Add(new RentCharge
            {
                TenancyId = tenancy.Id,
                SequenceNumber = ++sequence,
                PeriodFrom = cursor,
                PeriodTo = periodEnd,

                // Rent in advance falls due on the first day of the period it covers; in arrears,
                // on the last. Getting this backwards misstates every arrears figure by a month.
                DueDate = tenancy.PaidInAdvance ? cursor : periodEnd,

                Amount = amount,
                TotalAmount = amount,
                Balance = amount,
                Status = InstalmentStatus.NotDue,
                IsProRated = isPartial,
                IsRentFree = isRentFree,
            }.StampNew(Tenant, userId));

            cursor = periodEnd.AddDays(1);
        }

        tenancy.TotalCharged = RealEstateMapper.Money(
            settled.Sum(c => c.TotalAmount) + await Task.FromResult(0m));

        tenancy.NextDueDate = tenancy.PaidInAdvance ? from : NextPeriodEnd(from, tenancy.Frequency, tenancy.PaymentDay);
        tenancy.StampUpdated(userId);
    }

    private static DateOnly NextPeriodEnd(DateOnly start, RentFrequency frequency, int paymentDay)
    {
        var months = MonthsPerPeriod(frequency);

        return frequency switch
        {
            RentFrequency.Weekly => start.AddDays(6),
            RentFrequency.Fortnightly => start.AddDays(13),
            _ => start.AddMonths(months).AddDays(-1),
        };
    }

    /// <summary>The three fields a double-let message needs. A named type so EF can project it.</summary>
    private sealed record TenancyClash(string Reference, DateOnly StartDate, DateOnly? EndDate);
}
