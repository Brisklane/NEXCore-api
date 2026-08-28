using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Landlords, owner statements, payout runs and client money.
///
/// Client money is the most heavily regulated thing an agency touches. The reconciliation here is
/// a genuine three-way — bank against the ledger control account against the sum of every client's
/// individual balance — because two-way reconciliation is exactly what lets a shortfall hide, and
/// a shortfall is the failure that closes firms down. It cannot be signed off out of balance, and
/// anything that does not tie becomes a tracked exception rather than a note somebody wrote.
/// </summary>
public partial class LeasingService
{
    // ═══ Landlords ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<LandlordListItemDto>> GetLandlordsAsync(ListQueryDto query)
    {
        var partyIds = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : Db.Parties.ForCompany(Tenant)
                .Where(p => (p.DisplayName != null && p.DisplayName.Contains(query.Search!)) || (p.PrimaryPhone != null && p.PrimaryPhone.Contains(query.Search!)))
                .Select(p => p.Id);

        var q = Db.Landlords.ForCompany(Tenant)
            .WhereIf(query.OfficeId.HasValue, l => l.OfficeId == query.OfficeId)
            .WhereIf(partyIds is not null, l => partyIds!.Contains(l.PartyId) || l.Reference.Contains(query.Search!))
            .OrderBy(l => l.Reference);

        return await PageAsync(q, query, MapLandlordListAsync);
    }

    private async Task<List<LandlordListItemDto>> MapLandlordListAsync(List<Landlord> landlords)
    {
        if (landlords.Count == 0) return [];

        var currency = await CurrencyAsync();
        var ids = landlords.Select(l => l.Id).ToList();

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => landlords.Select(l => l.PartyId).Contains(p.Id))
            .ToListAsync();

        var managers = await AgentUserNamesAsync(landlords.Select(l => l.ManagedByUserId));

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => t.LandlordId != null && ids.Contains(t.LandlordId.Value) && t.Status == TenancyStatus.Active)
            .Select(t => new { LandlordId = t.LandlordId!.Value, t.Rent, t.Frequency, t.ArrearsAmount, t.PropertyId })
            .ToListAsync();

        var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();
        var today = Today;

        var compliance = propertyIds.Count == 0
            ? []
            : await Db.ComplianceSchedules.ForCompany(Tenant)
                .Where(s => s.PropertyId != null && propertyIds.Contains(s.PropertyId.Value) && s.NextDueOn < today)
                .Select(s => s.PropertyId!.Value)
                .ToListAsync();

        return landlords.Select(l =>
        {
            var person = people.FirstOrDefault(p => p.Id == l.PartyId);
            var mine = tenancies.Where(t => t.LandlordId == l.Id).ToList();

            return new LandlordListItemDto
            {
                Id = l.Id,
                Reference = l.Reference,
                PartyId = l.PartyId,
                Name = person is null ? "—" : RealEstateMapper.DisplayName(person),
                Phone = person?.PrimaryPhone,
                Email = person?.PrimaryEmail,
                DefaultService = l.DefaultService,
                DefaultFeePercent = l.DefaultFeePercent,
                PropertyCount = l.PropertyCount,
                TenancyCount = mine.Count,
                MonthlyRent = RealEstateMapper.Money(mine.Sum(t => AnnualiseRent(t.Rent, t.Frequency) / 12m)),
                TotalRentCollected = l.TotalRentCollected,
                CurrentBalance = l.CurrentBalance,
                ArrearsOnPortfolio = RealEstateMapper.Money(mine.Sum(t => t.ArrearsAmount)),
                CurrencyCode = currency,
                PayoutsOnHold = l.PayoutsOnHold,
                HoldReason = l.HoldReason,
                IsNonResident = l.IsNonResident,
                WithholdingPercent = l.WithholdingPercent,
                ManagedByName = l.ManagedByUserId is null ? null : managers.GetValueOrDefault(l.ManagedByUserId.Value),
                OpenComplianceIssues = mine.Count(t => compliance.Contains(t.PropertyId)),
            };
        }).ToList();
    }

    public async Task<LandlordDetailDto?> GetLandlordAsync(Guid id)
    {
        var landlord = await Db.Landlords.ForCompany(Tenant)
            .Include(l => l.Agreements)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (landlord is null) return null;

        var head = (await MapLandlordListAsync([landlord]))[0];

        var detail = new LandlordDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            PartyId = head.PartyId,
            Name = head.Name,
            Phone = head.Phone,
            Email = head.Email,
            DefaultService = head.DefaultService,
            DefaultFeePercent = head.DefaultFeePercent,
            PropertyCount = head.PropertyCount,
            TenancyCount = head.TenancyCount,
            MonthlyRent = head.MonthlyRent,
            TotalRentCollected = head.TotalRentCollected,
            CurrentBalance = head.CurrentBalance,
            ArrearsOnPortfolio = head.ArrearsOnPortfolio,
            CurrencyCode = head.CurrencyCode,
            PayoutsOnHold = head.PayoutsOnHold,
            HoldReason = head.HoldReason,
            IsNonResident = head.IsNonResident,
            WithholdingPercent = head.WithholdingPercent,
            ManagedByName = head.ManagedByName,
            OpenComplianceIssues = head.OpenComplianceIssues,

            PayoutFrequency = landlord.PayoutFrequency,
            PayoutDay = landlord.PayoutDay,
            BankName = landlord.BankName,
            AccountTitle = landlord.AccountTitle,

            // The last four only. A statement screen never needs the whole number.
            AccountNumber = string.IsNullOrWhiteSpace(landlord.AccountNumber) || landlord.AccountNumber.Length <= 4
                ? landlord.AccountNumber
                : $"••••{landlord.AccountNumber[^4..]}",

            SortCodeOrIban = landlord.SortCodeOrIban,
            BankDetailsVerified = landlord.BankDetailsVerified,
            TaxExemptionReference = landlord.TaxExemptionReference,
            ExemptionValidUntil = landlord.ExemptionValidUntil,
            FloatRequired = landlord.FloatRequired,
            FloatBalance = landlord.FloatBalance,
            RepairAuthorityLimit = landlord.RepairAuthorityLimit,
            StatementChannel = landlord.StatementChannel,
            PortalAccessEnabled = landlord.PortalAccessEnabled,
            Notes = landlord.Notes,
        };

        var propertyIds = await Db.PropertyOwnerships.ForCompany(Tenant)
            .Where(o => o.PartyId == landlord.PartyId && o.ToDate == null)
            .Select(o => o.PropertyId)
            .ToListAsync();

        detail.Agreements = await MapAgreementsAsync(landlord.Agreements.ToList());

        detail.Tenancies = await MapTenancyListAsync(
            await Db.Tenancies.ForCompany(Tenant)
                .Where(t => t.LandlordId == id)
                .OrderByDescending(t => t.StartDate)
                .Take(50)
                .ToListAsync());

        detail.Statements = await MapStatementsAsync(
            await Db.OwnerStatements.ForCompany(Tenant)
                .Where(s => s.LandlordId == id)
                .OrderByDescending(s => s.PeriodTo)
                .Take(12)
                .ToListAsync(), false);

        return detail;
    }

    private async Task<List<ManagementAgreementDto>> MapAgreementsAsync(List<ManagementAgreement> agreements)
    {
        if (agreements.Count == 0) return [];

        var today = Today;
        var propertyIds = agreements.Where(a => a.PropertyId.HasValue).Select(a => a.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        return agreements.Select(a => new ManagementAgreementDto
        {
            Id = a.Id,
            Reference = a.Reference,
            PropertyId = a.PropertyId,
            AddressOneLine = a.PropertyId is null ? "Whole portfolio" : properties.GetValueOrDefault(a.PropertyId.Value),
            Service = a.Service,
            FeePercent = a.FeePercent,
            FixedMonthlyFee = a.FixedMonthlyFee,
            SetupFee = a.SetupFee,
            RenewalFee = a.RenewalFee,
            TenantFindFee = a.TenantFindFee,
            StartDate = a.StartDate,
            EndDate = a.EndDate,
            NoticePeriodDays = a.NoticePeriodDays,
            RepairAuthorityLimit = a.RepairAuthorityLimit,
            CanSignTenancyOnBehalf = a.CanSignTenancyOnBehalf,
            CanServeNoticeOnBehalf = a.CanServeNoticeOnBehalf,
            HoldsDeposit = a.HoldsDeposit,
            SignedOn = a.SignedOn,
            IsActive = a.TerminatedOn is null && (a.EndDate is null || a.EndDate >= today),
            TerminatedOn = a.TerminatedOn,
        }).ToList();
    }

    public async Task<LandlordDetailDto> SaveLandlordAsync(LandlordDetailDto dto, Guid userId)
    {
        var landlord = dto.Id != Guid.Empty
            ? await Db.Landlords.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == dto.Id)
            : null;

        if (landlord is null)
        {
            landlord = new Landlord
            {
                PartyId = dto.PartyId,
                Reference = await numbering.NextLandlordReferenceAsync(),
            }.StampNew(Tenant, userId);

            Db.Landlords.Add(landlord);

            var hasRole = await Db.PartyRoles.ForCompany(Tenant)
                .AnyAsync(r => r.PartyId == dto.PartyId && r.Kind == PartyRoleKind.Landlord && r.IsActive);

            if (!hasRole)
            {
                Db.PartyRoles.Add(new PartyRole
                {
                    PartyId = dto.PartyId,
                    Kind = PartyRoleKind.Landlord,
                    FromDate = Today,
                    IsActive = true,
                }.StampNew(Tenant, userId));
            }
        }
        else landlord.StampUpdated(userId);

        landlord.DefaultService = dto.DefaultService;
        landlord.DefaultFeePercent = dto.DefaultFeePercent;
        landlord.RepairAuthorityLimit = dto.RepairAuthorityLimit;
        landlord.PayoutFrequency = dto.PayoutFrequency;
        landlord.PayoutDay = dto.PayoutDay;
        landlord.BankName = dto.BankName;
        landlord.AccountTitle = dto.AccountTitle;
        landlord.SortCodeOrIban = dto.SortCodeOrIban;
        landlord.PayoutsOnHold = dto.PayoutsOnHold;
        landlord.HoldReason = dto.HoldReason;
        landlord.IsNonResident = dto.IsNonResident;
        landlord.WithholdingPercent = dto.WithholdingPercent;
        landlord.TaxExemptionReference = dto.TaxExemptionReference;
        landlord.ExemptionValidUntil = dto.ExemptionValidUntil;
        landlord.FloatRequired = dto.FloatRequired;
        landlord.StatementChannel = dto.StatementChannel;
        landlord.PortalAccessEnabled = dto.PortalAccessEnabled;
        landlord.Notes = dto.Notes;

        // Changing bank details un-verifies them. Payment redirection fraud works precisely by
        // editing an account number on a file nobody re-checks.
        var masked = !string.IsNullOrWhiteSpace(dto.AccountNumber) && dto.AccountNumber.StartsWith("••••");

        if (!masked && dto.AccountNumber != landlord.AccountNumber)
        {
            landlord.AccountNumber = dto.AccountNumber;
            landlord.BankDetailsVerified = false;

            await WriteAuditNoteAsync(
                "Landlord", landlord.Id, "BankDetailsChanged", Guid.Empty, userId,
                note: "Bank details changed and must be re-verified before the next payout.",
                entityReference: landlord.Reference,
                highRisk: true);
        }

        landlord.PropertyCount = await Db.PropertyOwnerships.ForCompany(Tenant)
            .CountAsync(o => o.PartyId == landlord.PartyId && o.ToDate == null);

        await Db.SaveChangesAsync();
        return (await GetLandlordAsync(landlord.Id))!;
    }

    public async Task<ManagementAgreementDto> SaveManagementAgreementAsync(Guid landlordId, ManagementAgreementDto dto, Guid userId)
    {
        _ = await RequireAsync<Landlord>(landlordId, "That landlord does not exist.");

        var agreement = dto.Id != Guid.Empty
            ? await Db.ManagementAgreements.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (agreement is null)
        {
            agreement = new ManagementAgreement
            {
                LandlordId = landlordId,
                Reference = await numbering.NextMasterCodeAsync(Db.ManagementAgreements, "MGT"),
            }.StampNew(Tenant, userId);

            Db.ManagementAgreements.Add(agreement);
        }
        else agreement.StampUpdated(userId);

        agreement.PropertyId = dto.PropertyId;
        agreement.Service = dto.Service;
        agreement.FeePercent = dto.FeePercent;
        agreement.FixedMonthlyFee = dto.FixedMonthlyFee;
        agreement.SetupFee = dto.SetupFee;
        agreement.RenewalFee = dto.RenewalFee;
        agreement.TenantFindFee = dto.TenantFindFee;
        agreement.StartDate = dto.StartDate;
        agreement.EndDate = dto.EndDate;
        agreement.NoticePeriodDays = dto.NoticePeriodDays;
        agreement.RepairAuthorityLimit = dto.RepairAuthorityLimit;
        agreement.CanSignTenancyOnBehalf = dto.CanSignTenancyOnBehalf;
        agreement.CanServeNoticeOnBehalf = dto.CanServeNoticeOnBehalf;
        agreement.HoldsDeposit = dto.HoldsDeposit;
        agreement.SignedOn = dto.SignedOn;
        agreement.TerminatedOn = dto.TerminatedOn;

        await Db.SaveChangesAsync();
        return (await MapAgreementsAsync([agreement]))[0];
    }

    // ═══ Owner statements ════════════════════════════════════════════════════

    /// <summary>
    /// The landlord's statement for a period: what came in, what was spent, what we kept, and what
    /// is payable. Built from the ledger rather than typed, so the total on the statement is the
    /// total in the client account and the two can never quietly diverge.
    /// </summary>
    public async Task<OwnerStatementDto> GenerateOwnerStatementAsync(
        Guid landlordId, DateOnly from, DateOnly to, Guid? propertyId, Guid userId)
    {
        var landlord = await RequireAsync<Landlord>(landlordId, "That landlord does not exist.");
        var currency = await CurrencyAsync();

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => t.LandlordId == landlordId)
            .WhereIf(propertyId.HasValue, t => t.PropertyId == propertyId)
            .Select(t => new { t.Id, t.PropertyId, t.ManagementFeePercent })
            .ToListAsync();

        var tenancyIds = tenancies.Select(t => t.Id).ToList();

        var previous = await Db.OwnerStatements.ForCompany(Tenant)
            .Where(s => s.LandlordId == landlordId && s.PeriodTo < from)
            .OrderByDescending(s => s.PeriodTo)
            .Select(s => (decimal?)s.ClosingBalance)
            .FirstOrDefaultAsync();

        var statement = new OwnerStatement
        {
            Reference = await numbering.NextMasterCodeAsync(Db.OwnerStatements, "OWS"),
            LandlordId = landlordId,
            PropertyId = propertyId,
            PeriodFrom = from,
            PeriodTo = to,
            IssuedOn = Today,
            OpeningBalance = previous ?? 0m,
            CurrencyCode = currency,
        }.StampNew(Tenant, userId);

        Db.OwnerStatements.Add(statement);

        var order = 0;

        // Income — rent actually received in the period, not rent charged.
        var receipts = tenancyIds.Count == 0
            ? []
            : await Db.RentCharges.ForCompany(Tenant)
                .Where(c => tenancyIds.Contains(c.TenancyId)
                         && c.SettledOn != null && c.SettledOn >= from && c.SettledOn <= to)
                .Select(c => new { c.TenancyId, c.SettledOn, c.PaidAmount, c.PeriodFrom, c.PeriodTo })
                .ToListAsync();

        var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        foreach (var receipt in receipts.OrderBy(r => r.SettledOn))
        {
            var tenancy = tenancies.First(t => t.Id == receipt.TenancyId);

            statement.Lines.Add(new OwnerStatementLine
            {
                EntryDate = receipt.SettledOn!.Value,
                PropertyId = tenancy.PropertyId,
                TenancyId = receipt.TenancyId,
                Category = "Rent",
                Description = $"Rent {receipt.PeriodFrom:dd MMM} – {receipt.PeriodTo:dd MMM yyyy}",
                IncomeAmount = receipt.PaidAmount,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));
        }

        statement.RentCollected = RealEstateMapper.Money(receipts.Sum(r => r.PaidAmount));

        // Deductions — work orders completed in the period that the landlord bears.
        var works = propertyIds.Count == 0
            ? []
            : await Db.WorkOrders.ForCompany(Tenant)
                .Where(w => w.PropertyId != null && propertyIds.Contains(w.PropertyId.Value)
                         && w.CostBearer == CostBearer.Landlord
                         && w.CompletedAt != null
                         && w.CompletedAt >= from.ToDateTime(TimeOnly.MinValue)
                         && w.CompletedAt <= to.ToDateTime(TimeOnly.MaxValue))
                .Select(w => new { w.Id, w.PropertyId, w.OrderNumber, w.Title, w.TotalCost, w.CompletedAt })
                .ToListAsync();

        foreach (var work in works)
        {
            statement.Lines.Add(new OwnerStatementLine
            {
                EntryDate = DateOnly.FromDateTime(work.CompletedAt!.Value),
                PropertyId = work.PropertyId,
                Category = "Maintenance",
                Description = $"{work.OrderNumber} — {work.Title}",
                DeductionAmount = work.TotalCost,
                WorkOrderId = work.Id,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));
        }

        statement.MaintenanceCost = RealEstateMapper.Money(works.Sum(w => w.TotalCost));

        // The management fee is charged on what was collected, not on what was demanded. An agent
        // who has not collected has not earned it.
        var fee = 0m;

        foreach (var tenancy in tenancies)
        {
            var collected = receipts.Where(r => r.TenancyId == tenancy.Id).Sum(r => r.PaidAmount);
            if (collected <= 0m) continue;

            var rate = tenancy.ManagementFeePercent > 0m ? tenancy.ManagementFeePercent : landlord.DefaultFeePercent;
            var amount = RealEstateMapper.Money(collected * rate / 100m);

            if (amount <= 0m) continue;

            fee += amount;

            statement.Lines.Add(new OwnerStatementLine
            {
                EntryDate = to,
                PropertyId = tenancy.PropertyId,
                TenancyId = tenancy.Id,
                Category = "ManagementFee",
                Description = $"Management fee at {rate:N1}% of {collected:N0} collected",
                DeductionAmount = amount,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));
        }

        statement.ManagementFee = RealEstateMapper.Money(fee);

        // Withholding on a non-resident landlord is an obligation on the agent, not the landlord.
        // Getting it wrong is the agent's liability, so it is computed rather than remembered.
        if (landlord.IsNonResident && landlord.WithholdingPercent > 0m)
        {
            var exemptionValid = landlord.ExemptionValidUntil is not null && landlord.ExemptionValidUntil >= to;

            if (!exemptionValid)
            {
                var taxable = statement.RentCollected - statement.ManagementFee - statement.MaintenanceCost;
                statement.TaxWithheld = RealEstateMapper.Money(Math.Max(0m, taxable * landlord.WithholdingPercent / 100m));

                statement.Lines.Add(new OwnerStatementLine
                {
                    EntryDate = to,
                    Category = "TaxWithheld",
                    Description = $"Withholding tax at {landlord.WithholdingPercent:N1}% (non-resident landlord)",
                    DeductionAmount = statement.TaxWithheld,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        // The float is topped back up before anything is paid out, so the next repair does not
        // have to wait for the next rent.
        var netBeforeFloat = statement.OpeningBalance + statement.RentCollected + statement.OtherIncome
                           - statement.ManagementFee - statement.MaintenanceCost - statement.OtherDeductions
                           - statement.TaxWithheld;

        if (landlord.FloatRequired > 0m && landlord.FloatBalance < landlord.FloatRequired)
        {
            var shortfall = Math.Min(Math.Max(0m, netBeforeFloat), landlord.FloatRequired - landlord.FloatBalance);

            if (shortfall > 0m)
            {
                statement.FloatRetained = RealEstateMapper.Money(shortfall);

                statement.Lines.Add(new OwnerStatementLine
                {
                    EntryDate = to,
                    Category = "FloatRetained",
                    Description = $"Retained to restore the maintenance float to {landlord.FloatRequired:N0}",
                    DeductionAmount = statement.FloatRetained,
                    SortOrder = order + 10,
                }.StampNew(Tenant, userId));
            }
        }

        statement.NetPayable = RealEstateMapper.Money(Math.Max(0m, netBeforeFloat - statement.FloatRetained));
        statement.ClosingBalance = RealEstateMapper.Money(netBeforeFloat - statement.FloatRetained - statement.NetPayable);

        await Db.SaveChangesAsync();

        landlord.FloatBalance = RealEstateMapper.Money(landlord.FloatBalance + statement.FloatRetained);
        landlord.TotalRentCollected = RealEstateMapper.Money(landlord.TotalRentCollected + statement.RentCollected);
        landlord.CurrentBalance = statement.ClosingBalance;
        landlord.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return (await MapStatementsAsync([statement], true))[0];
    }

    public async Task<PaginatedResponse<OwnerStatementDto>> GetOwnerStatementsAsync(ListQueryDto query, Guid? landlordId)
    {
        var q = Db.OwnerStatements.ForCompany(Tenant)
            .Include(s => s.Lines)
            .WhereIf(landlordId.HasValue, s => s.LandlordId == landlordId)
            .WhereIf(query.FromDate.HasValue, s => s.PeriodFrom >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, s => s.PeriodTo <= query.ToDate)
            .OrderByDescending(s => s.PeriodTo);

        return await PageAsync(q, query, list => MapStatementsAsync(list, true));
    }

    private async Task<List<OwnerStatementDto>> MapStatementsAsync(List<OwnerStatement> statements, bool includeLines)
    {
        if (statements.Count == 0) return [];

        var landlordIds = statements.Select(s => s.LandlordId).Distinct().ToList();

        var landlords = await Db.Landlords.ForCompany(Tenant)
            .Where(l => landlordIds.Contains(l.Id))
            .Join(Db.Parties.ForCompany(Tenant), l => l.PartyId, p => p.Id, (l, p) => new { l.Id, p.DisplayName })
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName);

        var propertyIds = statements.SelectMany(s => s.Lines).Where(l => l.PropertyId.HasValue)
            .Select(l => l.PropertyId!.Value)
            .Concat(statements.Where(s => s.PropertyId.HasValue).Select(s => s.PropertyId!.Value))
            .Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        return statements.Select(s => new OwnerStatementDto
        {
            Id = s.Id,
            Reference = s.Reference,
            LandlordId = s.LandlordId,
            LandlordName = landlords.GetValueOrDefault(s.LandlordId) ?? "—",
            PropertyId = s.PropertyId,
            AddressOneLine = s.PropertyId is null ? "Whole portfolio" : properties.GetValueOrDefault(s.PropertyId.Value),
            PeriodFrom = s.PeriodFrom,
            PeriodTo = s.PeriodTo,
            IssuedOn = s.IssuedOn,
            OpeningBalance = s.OpeningBalance,
            RentCollected = s.RentCollected,
            OtherIncome = s.OtherIncome,
            ManagementFee = s.ManagementFee,
            MaintenanceCost = s.MaintenanceCost,
            OtherDeductions = s.OtherDeductions,
            TaxWithheld = s.TaxWithheld,
            FloatRetained = s.FloatRetained,
            NetPayable = s.NetPayable,
            ClosingBalance = s.ClosingBalance,
            CurrencyCode = s.CurrencyCode,
            DocumentUrl = s.DocumentUrl,
            OwnerPayoutId = s.OwnerPayoutId,
            IsPublishedToPortal = s.IsPublishedToPortal,
            IsSent = s.IsSent,

            Lines = !includeLines ? [] : s.Lines.OrderBy(l => l.SortOrder).Select(l => new OwnerStatementLineDto
            {
                Id = l.Id,
                EntryDate = l.EntryDate,
                AddressOneLine = l.PropertyId is null ? null : properties.GetValueOrDefault(l.PropertyId.Value),
                Category = l.Category,
                Description = l.Description ?? string.Empty,
                IncomeAmount = l.IncomeAmount,
                DeductionAmount = l.DeductionAmount,
                WorkOrderId = l.WorkOrderId,
                SupportingDocumentUrl = l.SupportingDocumentUrl,
                SortOrder = l.SortOrder,
            }).ToList(),
        }).ToList();
    }

    // ═══ Payout runs ═════════════════════════════════════════════════════════

    /// <summary>
    /// The payout run. Every landlord with an unpaid statement is included unless something stops
    /// them — unverified bank details, a hold, an unresolved client-money exception — and the ones
    /// that are stopped are listed with the reason rather than silently dropped.
    /// </summary>
    public async Task<OwnerPayoutDto> CreatePayoutRunAsync(
        DateOnly payoutDate, Guid? officeId, Guid clientAccountId, bool dryRun, Guid userId)
    {
        var account = await RequireAsync<ClientAccount>(clientAccountId, "That client account does not exist.");

        var payout = new OwnerPayout
        {
            Reference = await numbering.NextMasterCodeAsync(Db.OwnerPayouts, "PAY"),
            PayoutDate = payoutDate,
            OfficeId = officeId,
            ClientAccountId = clientAccountId,
        }.StampNew(Tenant, userId);

        var statements = await Db.OwnerStatements.ForCompany(Tenant)
            .Where(s => s.OwnerPayoutId == null && s.NetPayable > 0m)
            .ToListAsync();

        var landlordIds = statements.Select(s => s.LandlordId).Distinct().ToList();

        var landlords = await Db.Landlords.ForCompany(Tenant)
            .Where(l => landlordIds.Contains(l.Id))
            .WhereIf(officeId.HasValue, l => l.OfficeId == officeId)
            .ToListAsync();

        var names = await Db.Parties.ForCompany(Tenant)
            .Where(p => landlords.Select(l => l.PartyId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.DisplayName);

        var exceptions = await Db.ClientMoneyExceptions.ForCompany(Tenant)
            .Where(e => !e.IsResolved && e.Severity == AlertSeverity.Critical && e.PartyId != null)
            .Select(e => e.PartyId!.Value)
            .ToListAsync();

        foreach (var landlord in landlords)
        {
            var mine = statements.Where(s => s.LandlordId == landlord.Id).ToList();
            var gross = RealEstateMapper.Money(mine.Sum(s => s.NetPayable));

            if (gross <= 0m) continue;

            var line = new OwnerPayoutLine
            {
                OwnerPayoutId = payout.Id,
                LandlordId = landlord.Id,
                OwnerStatementId = mine.Count == 1 ? mine[0].Id : null,
                GrossAmount = gross,
                WithheldAmount = RealEstateMapper.Money(mine.Sum(s => s.TaxWithheld)),
                NetAmount = gross,
                AccountNumber = landlord.AccountNumber,
                PaymentReference = $"{payout.Reference}-{landlord.Reference}",
            }.StampNew(Tenant, userId);

            if (landlord.PayoutsOnHold)
            {
                line.IsHeld = true;
                line.HoldReason = landlord.HoldReason ?? "Payouts are on hold for this landlord.";
            }
            else if (!landlord.BankDetailsVerified)
            {
                line.IsHeld = true;
                line.HoldReason = "Bank details have not been verified.";
            }
            else if (string.IsNullOrWhiteSpace(landlord.AccountNumber))
            {
                line.IsHeld = true;
                line.HoldReason = "No bank account is recorded.";
            }
            else if (exceptions.Contains(landlord.PartyId))
            {
                line.IsHeld = true;
                line.HoldReason = "An unresolved client-money exception is open against this landlord.";
            }

            payout.Lines.Add(line);
        }

        payout.LandlordCount = payout.Lines.Count(l => !l.IsHeld);
        payout.TotalAmount = RealEstateMapper.Money(payout.Lines.Where(l => !l.IsHeld).Sum(l => l.NetAmount));
        payout.TotalWithheld = RealEstateMapper.Money(payout.Lines.Sum(l => l.WithheldAmount));

        // Paying out more than the account holds is a client-money breach, not an overdraft.
        if (payout.TotalAmount > account.Balance)
        {
            throw new InvalidOperationException(
                $"This run pays out {payout.TotalAmount:N0} but {account.Name} holds {account.Balance:N0}. " +
                "A client account may never go overdrawn.");
        }

        if (!dryRun)
        {
            Db.OwnerPayouts.Add(payout);

            foreach (var line in payout.Lines.Where(l => !l.IsHeld))
            {
                var mine = statements.Where(s => s.LandlordId == line.LandlordId).ToList();

                foreach (var statement in mine)
                {
                    statement.OwnerPayoutId = payout.Id;
                    statement.StampUpdated(userId);
                }
            }

            await Db.SaveChangesAsync();
        }

        var held = payout.Lines.Count(l => l.IsHeld);

        return new OwnerPayoutDto
        {
            Id = payout.Id,
            Reference = payout.Reference,
            PayoutDate = payout.PayoutDate,
            LandlordCount = payout.LandlordCount,
            TotalAmount = payout.TotalAmount,
            TotalWithheld = payout.TotalWithheld,
            CurrencyCode = account.CurrencyCode,
            PaymentMethod = payout.PaymentMethod,
            HeldCount = held,
            FailureSummary = held == 0 ? null : $"{held} landlords are held back and will not be paid in this run.",

            Lines = payout.Lines.Select(l => new OwnerPayoutLineDto
            {
                Id = l.Id,
                LandlordId = l.LandlordId,
                LandlordName = landlords.Where(x => x.Id == l.LandlordId)
                    .Select(x => names.GetValueOrDefault(x.PartyId, "—")).FirstOrDefault() ?? "—",
                OwnerStatementId = l.OwnerStatementId,
                GrossAmount = l.GrossAmount,
                WithheldAmount = l.WithheldAmount,
                NetAmount = l.NetAmount,
                AccountNumber = string.IsNullOrWhiteSpace(l.AccountNumber) || l.AccountNumber.Length <= 4
                    ? l.AccountNumber
                    : $"••••{l.AccountNumber[^4..]}",
                PaymentReference = l.PaymentReference,
                IsHeld = l.IsHeld,
                HoldReason = l.HoldReason,
                IsPaid = l.IsPaid,
            }).ToList(),
        };
    }

    public async Task<OwnerPayoutDto> SubmitPayoutRunAsync(Guid id, Guid userId)
    {
        var payout = await Db.OwnerPayouts.ForCompany(Tenant)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("That payout run does not exist.");

        if (payout.SubmittedAt is not null)
            throw new InvalidOperationException($"This run was submitted on {payout.SubmittedAt:dd MMM yyyy}.");

        var approval = await RaiseApprovalAsync(
            "OwnerPayout", payout.Id, payout.Reference, payout.TotalAmount,
            $"Pay {payout.TotalAmount:N0} to {payout.LandlordCount} landlords on {payout.PayoutDate:dd MMM yyyy}.",
            userId, officeId: payout.OfficeId);

        payout.ApprovalRequestId = approval?.Id;

        if (approval is not null)
            throw new InvalidOperationException("This run needs approval before it can be submitted. It has been sent for sign-off.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var account = await RequireAsync<ClientAccount>(payout.ClientAccountId, "The client account is missing.");
        var today = Today;

        foreach (var line in payout.Lines.Where(l => !l.IsHeld))
        {
            line.IsPaid = true;
            line.StampUpdated(userId);

            // Every movement of client money is a ledger entry. Without one the three-way
            // reconciliation cannot balance and the shortfall has nowhere to show.
            account.Balance = RealEstateMapper.Money(account.Balance - line.NetAmount);

            Db.ClientLedgerEntries.Add(new ClientLedgerEntry
            {
                ClientAccountId = account.Id,
                EntryDate = today,
                EntryType = "OwnerPayout",
                DebitAmount = line.NetAmount,
                RunningBalance = account.Balance,
                OwnerPayoutLineId = line.Id,
                AuthorisedByUserId = userId,
            }.StampNew(Tenant, userId));

            var landlord = await Db.Landlords.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == line.LandlordId);

            if (landlord is not null)
            {
                landlord.CurrentBalance = RealEstateMapper.Money(landlord.CurrentBalance - line.NetAmount);
                landlord.StampUpdated(userId);
            }
        }

        payout.SubmittedAt = DateTime.UtcNow;
        payout.SubmittedByUserId = userId;
        payout.IsCompleted = true;
        payout.BatchReference = $"{payout.Reference}-{DateTime.UtcNow:yyyyMMddHHmm}";
        payout.StampUpdated(userId);

        account.StampUpdated(userId);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetPayoutRunsAsync(new ListQueryDto { PageSize = 1, Search = payout.Reference })).Data.First();
    }

    public async Task<PaginatedResponse<OwnerPayoutDto>> GetPayoutRunsAsync(ListQueryDto query)
    {
        var q = Db.OwnerPayouts.ForCompany(Tenant)
            .Include(p => p.Lines)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), p => p.Reference.Contains(query.Search!))
            .OrderByDescending(p => p.PayoutDate);

        return await PageAsync(q, query, async payouts =>
        {
            if (payouts.Count == 0) return [];

            var currency = await CurrencyAsync();
            var landlordIds = payouts.SelectMany(p => p.Lines).Select(l => l.LandlordId).Distinct().ToList();

            var landlords = await Db.Landlords.ForCompany(Tenant)
                .Where(l => landlordIds.Contains(l.Id))
                .Join(Db.Parties.ForCompany(Tenant), l => l.PartyId, p => p.Id, (l, p) => new { l.Id, p.DisplayName })
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName);

            var users = await AgentUserNamesAsync(payouts.Select(p => p.SubmittedByUserId));

            return payouts.Select(p => new OwnerPayoutDto
            {
                Id = p.Id,
                Reference = p.Reference,
                PayoutDate = p.PayoutDate,
                LandlordCount = p.LandlordCount,
                TotalAmount = p.TotalAmount,
                TotalWithheld = p.TotalWithheld,
                CurrencyCode = currency,
                PaymentMethod = p.PaymentMethod,
                BankFileUrl = p.BankFileUrl,
                BatchReference = p.BatchReference,
                SubmittedAt = p.SubmittedAt,
                SubmittedByName = p.SubmittedByUserId is null ? null : users.GetValueOrDefault(p.SubmittedByUserId.Value),
                IsCompleted = p.IsCompleted,
                FailedCount = p.FailedCount,
                HeldCount = p.Lines.Count(l => l.IsHeld),
                FailureSummary = p.FailureSummary,

                Lines = p.Lines.Select(l => new OwnerPayoutLineDto
                {
                    Id = l.Id,
                    LandlordId = l.LandlordId,
                    LandlordName = landlords.GetValueOrDefault(l.LandlordId) ?? "—",
                    OwnerStatementId = l.OwnerStatementId,
                    GrossAmount = l.GrossAmount,
                    WithheldAmount = l.WithheldAmount,
                    NetAmount = l.NetAmount,
                    AccountNumber = string.IsNullOrWhiteSpace(l.AccountNumber) || l.AccountNumber.Length <= 4
                        ? l.AccountNumber
                        : $"••••{l.AccountNumber[^4..]}",
                    PaymentReference = l.PaymentReference,
                    IsHeld = l.IsHeld,
                    HoldReason = l.HoldReason,
                    Failed = l.Failed,
                    FailureReason = l.FailureReason,
                    IsPaid = l.IsPaid,
                }).ToList(),
            }).ToList();
        });
    }

    // ═══ Client money ════════════════════════════════════════════════════════

    public async Task<List<ClientAccountDto>> GetClientAccountsAsync()
    {
        var today = Today;

        var accounts = await Db.ClientAccounts.ForCompany(Tenant)
            .OrderBy(a => a.Name)
            .ToListAsync();

        if (accounts.Count == 0) return [];

        var ids = accounts.Select(a => a.Id).ToList();

        var exceptions = await Db.ClientMoneyExceptions.ForCompany(Tenant)
            .Where(e => e.ClientAccountId != null && ids.Contains(e.ClientAccountId.Value) && !e.IsResolved)
            .GroupBy(e => e.ClientAccountId!.Value)
            .Select(g => new { AccountId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AccountId, x => x.Count);

        var landlordIds = accounts.Where(a => a.LandlordId.HasValue).Select(a => a.LandlordId!.Value).Distinct().ToList();

        var landlords = landlordIds.Count == 0
            ? []
            : await Db.Landlords.ForCompany(Tenant)
                .Where(l => landlordIds.Contains(l.Id))
                .Join(Db.Parties.ForCompany(Tenant), l => l.PartyId, p => p.Id, (l, p) => new { l.Id, p.DisplayName })
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName);

        return accounts.Select(a => new ClientAccountDto
        {
            Id = a.Id,
            Name = a.Name,
            Reference = a.Reference,
            Kind = a.Kind,
            BankName = a.BankName,
            AccountNumber = string.IsNullOrWhiteSpace(a.AccountNumber) || a.AccountNumber.Length <= 4
                ? a.AccountNumber
                : $"••••{a.AccountNumber[^4..]}",
            CurrencyCode = a.CurrencyCode,
            LandlordName = a.LandlordId is null ? null : landlords.GetValueOrDefault(a.LandlordId.Value),
            Balance = a.Balance,
            UnallocatedBalance = a.UnallocatedBalance,

            // A client account can never legitimately be overdrawn. If it is, that is the finding.
            IsOverdrawn = a.Balance < 0m,

            LastReconciledOn = a.LastReconciledOn,
            ReconciliationIntervalDays = a.ReconciliationIntervalDays,
            ReconciliationOverdue = a.LastReconciledOn is null
                || a.LastReconciledOn.Value.AddDays(a.ReconciliationIntervalDays) < today,
            OpenExceptionCount = exceptions.GetValueOrDefault(a.Id),
            IsActive = a.IsActive,
        }).ToList();
    }

    public async Task<ClientAccountDto> SaveClientAccountAsync(ClientAccountDto dto, Guid userId)
    {
        var account = dto.Id != Guid.Empty
            ? await Db.ClientAccounts.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (account is null)
        {
            account = new ClientAccount
            {
                Reference = string.IsNullOrWhiteSpace(dto.Reference)
                    ? await numbering.NextMasterCodeAsync(Db.ClientAccounts, "CLA")
                    : dto.Reference,
            }.StampNew(Tenant, userId);

            Db.ClientAccounts.Add(account);
        }
        else account.StampUpdated(userId);

        account.Name = dto.Name;
        account.Kind = dto.Kind;
        account.BankName = dto.BankName;
        account.CurrencyCode = dto.CurrencyCode;
        account.ReconciliationIntervalDays = dto.ReconciliationIntervalDays <= 0 ? 30 : dto.ReconciliationIntervalDays;
        account.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.AccountNumber) && !dto.AccountNumber.StartsWith("••••"))
            account.AccountNumber = dto.AccountNumber;

        await Db.SaveChangesAsync();
        return (await GetClientAccountsAsync()).First(a => a.Id == account.Id);
    }

    public async Task<PaginatedResponse<ClientLedgerEntryDto>> GetClientLedgerAsync(Guid accountId, ListQueryDto query)
    {
        var q = Db.ClientLedgerEntries.ForCompany(Tenant)
            .Where(e => e.ClientAccountId == accountId)
            .WhereIf(query.FromDate.HasValue, e => e.EntryDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, e => e.EntryDate <= query.ToDate)
            .OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.CreatedAt);

        return await PageAsync(q, query, async entries =>
        {
            if (entries.Count == 0) return [];

            var parties = await PartyNamesAsync(entries.Where(e => e.PartyId.HasValue).Select(e => e.PartyId!.Value));

            var propertyIds = entries.Where(e => e.PropertyId.HasValue).Select(e => e.PropertyId!.Value).Distinct().ToList();

            var properties = propertyIds.Count == 0
                ? []
                : await Db.Properties.ForCompany(Tenant)
                    .Where(p => propertyIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

            var users = await AgentUserNamesAsync(entries.Select(e => e.AuthorisedByUserId));

            return entries.Select(e => new ClientLedgerEntryDto
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                EntryType = e.EntryType,
                Description = e.Description ?? e.EntryType,
                DebitAmount = e.DebitAmount,
                CreditAmount = e.CreditAmount,
                RunningBalance = e.RunningBalance,
                PartyName = e.PartyId is null ? null : parties.GetValueOrDefault(e.PartyId.Value),
                AddressOneLine = e.PropertyId is null ? null : properties.GetValueOrDefault(e.PropertyId.Value),
                IsReconciled = e.IsReconciled,
                ReconciledOn = e.ReconciledOn,
                BankReference = e.BankReference,
                IsCorrection = e.IsCorrection,
                AuthorisedByName = e.AuthorisedByUserId is null ? null : users.GetValueOrDefault(e.AuthorisedByUserId.Value),
            }).ToList();
        });
    }

    /// <summary>
    /// The three-way reconciliation: bank, ledger control, and the sum of every client's balance.
    /// All three have to agree. Two-way is what lets a shortfall hide behind a suspense entry,
    /// and a shortfall on client money is the failure that closes an agency.
    /// </summary>
    public async Task<ClientMoneyReconciliationDto> ReconcileClientMoneyAsync(
        Guid accountId, DateOnly asOf, decimal bankBalance, Guid userId)
    {
        var account = await RequireAsync<ClientAccount>(accountId, "That client account does not exist.");

        var entries = await Db.ClientLedgerEntries.ForCompany(Tenant)
            .Where(e => e.ClientAccountId == accountId && e.EntryDate <= asOf)
            .Select(e => new { e.CreditAmount, e.DebitAmount, e.PartyId, e.IsReconciled, e.EntryType })
            .ToListAsync();

        var control = RealEstateMapper.Money(entries.Sum(e => e.CreditAmount - e.DebitAmount));

        // Leg three: what every individual client is owed, added up. If it does not match the
        // control account, somebody's money has been posted to somebody else's ledger.
        var clientBalances = entries
            .Where(e => e.PartyId.HasValue)
            .GroupBy(e => e.PartyId!.Value)
            .Select(g => g.Sum(e => e.CreditAmount - e.DebitAmount))
            .ToList();

        var sumOfClients = RealEstateMapper.Money(clientBalances.Sum());

        var unpresented = RealEstateMapper.Money(entries.Where(e => !e.IsReconciled).Sum(e => e.DebitAmount));
        var undeposited = RealEstateMapper.Money(entries.Where(e => !e.IsReconciled).Sum(e => e.CreditAmount));

        var adjusted = RealEstateMapper.Money(bankBalance - unpresented + undeposited);
        var difference = RealEstateMapper.Money(adjusted - control);

        var reconciliation = new ClientMoneyReconciliation
        {
            Reference = await numbering.NextMasterCodeAsync(Db.ClientMoneyReconciliations, "CMR"),
            ClientAccountId = accountId,
            OfficeId = account.OfficeId,
            ReconciliationDate = asOf,
            BankStatementBalance = bankBalance,
            LedgerControlBalance = control,
            SumOfClientBalances = sumOfClients,
            UnpresentedPayments = unpresented,
            UndepositedReceipts = undeposited,
            AdjustedBankBalance = adjusted,
            Difference = difference,
            IsBalanced = Math.Abs(difference) <= 0.01m && Math.Abs(control - sumOfClients) <= 0.01m,
            PreparedByUserId = userId,
        }.StampNew(Tenant, userId);

        Db.ClientMoneyReconciliations.Add(reconciliation);
        await Db.SaveChangesAsync();

        var exceptions = new List<ClientMoneyException>();

        if (Math.Abs(difference) > 0.01m)
        {
            exceptions.Add(new ClientMoneyException
            {
                ClientMoneyReconciliationId = reconciliation.Id,
                ClientAccountId = accountId,
                Kind = ClientMoneyExceptionKind.UnreconciledDifference,
                Severity = AlertSeverity.Critical,
                Description = difference < 0m
                    ? $"The bank holds {Math.Abs(difference):N2} less than the ledger says it should. This is a shortfall and must be made good immediately."
                    : $"The bank holds {difference:N2} more than the ledger accounts for. Identify whose money it is.",
                Amount = Math.Abs(difference),
                RaisedOn = asOf,
                RequiresRegulatoryReport = difference < 0m,
            }.StampNew(Tenant, userId));
        }

        if (Math.Abs(control - sumOfClients) > 0.01m)
        {
            exceptions.Add(new ClientMoneyException
            {
                ClientMoneyReconciliationId = reconciliation.Id,
                ClientAccountId = accountId,
                Kind = ClientMoneyExceptionKind.UnreconciledDifference,
                Severity = AlertSeverity.Critical,
                Description = $"The control account says {control:N2} but the individual client ledgers add up to {sumOfClients:N2}. Money is posted to the wrong ledger.",
                Amount = Math.Abs(control - sumOfClients),
                RaisedOn = asOf,
            }.StampNew(Tenant, userId));
        }

        // A negative individual balance means one client's money has funded another's payment.
        var overdrawnClients = entries
            .Where(e => e.PartyId.HasValue)
            .GroupBy(e => e.PartyId!.Value)
            .Where(g => g.Sum(e => e.CreditAmount - e.DebitAmount) < -0.01m)
            .ToList();

        foreach (var client in overdrawnClients)
        {
            exceptions.Add(new ClientMoneyException
            {
                ClientMoneyReconciliationId = reconciliation.Id,
                ClientAccountId = accountId,
                Kind = ClientMoneyExceptionKind.OverdrawnClientBalance,
                Severity = AlertSeverity.Critical,
                Description = "A client ledger is overdrawn, which means another client's money has funded it.",
                Amount = Math.Abs(client.Sum(e => e.CreditAmount - e.DebitAmount)),
                RaisedOn = asOf,
                PartyId = client.Key,
                RequiresRegulatoryReport = true,
            }.StampNew(Tenant, userId));
        }

        Db.ClientMoneyExceptions.AddRange(exceptions);

        reconciliation.ExceptionCount = exceptions.Count;

        account.LastReconciledOn = asOf;
        account.Balance = control;
        account.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return await MapReconciliationAsync(reconciliation, account, exceptions);
    }

    public async Task<ClientMoneyReconciliationDto> SignOffReconciliationAsync(Guid id, Guid userId)
    {
        var reconciliation = await Db.ClientMoneyReconciliations.ForCompany(Tenant)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("That reconciliation does not exist.");

        if (reconciliation.SignedOffAt is not null)
            throw new InvalidOperationException($"This reconciliation was signed off on {reconciliation.SignedOffAt:dd MMM yyyy}.");

        // Signing off an unbalanced reconciliation is how a shortfall becomes a cover-up.
        if (!reconciliation.IsBalanced)
            throw new InvalidOperationException($"This reconciliation is out by {reconciliation.Difference:N2}. Resolve it before signing off — an out-of-balance client account cannot be certified.");

        // Two pairs of eyes, always. The preparer cannot be the reviewer.
        if (reconciliation.PreparedByUserId == userId)
            throw new InvalidOperationException("The person who prepared a client-money reconciliation cannot also sign it off.");

        var open = await Db.ClientMoneyExceptions.ForCompany(Tenant)
            .CountAsync(e => e.ClientMoneyReconciliationId == id && !e.IsResolved && e.Severity == AlertSeverity.Critical);

        if (open > 0)
            throw new InvalidOperationException($"{open} critical exceptions are still open on this reconciliation.");

        reconciliation.ReviewedByUserId = userId;
        reconciliation.SignedOffAt = DateTime.UtcNow;
        reconciliation.StampUpdated(userId);

        await Db.SaveChangesAsync();

        var account = reconciliation.ClientAccountId is null
            ? null
            : await Db.ClientAccounts.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == reconciliation.ClientAccountId);

        return await MapReconciliationAsync(reconciliation, account, []);
    }

    private async Task<ClientMoneyReconciliationDto> MapReconciliationAsync(
        ClientMoneyReconciliation r, ClientAccount? account, List<ClientMoneyException> exceptions)
    {
        var today = Today;
        var users = await AgentUserNamesAsync([r.PreparedByUserId, r.ReviewedByUserId]);
        var names = await PartyNamesAsync(exceptions.Where(e => e.PartyId.HasValue).Select(e => e.PartyId!.Value));

        return new ClientMoneyReconciliationDto
        {
            Id = r.Id,
            Reference = r.Reference,
            ClientAccountId = r.ClientAccountId,
            AccountName = account?.Name,
            ReconciliationDate = r.ReconciliationDate,
            BankStatementBalance = r.BankStatementBalance,
            LedgerControlBalance = r.LedgerControlBalance,
            SumOfClientBalances = r.SumOfClientBalances,
            UnpresentedPayments = r.UnpresentedPayments,
            UndepositedReceipts = r.UndepositedReceipts,
            AdjustedBankBalance = r.AdjustedBankBalance,
            Difference = r.Difference,
            IsBalanced = r.IsBalanced,
            CurrencyCode = account?.CurrencyCode ?? await CurrencyAsync(),
            ExceptionCount = r.ExceptionCount,
            PreparedByName = r.PreparedByUserId is null ? null : users.GetValueOrDefault(r.PreparedByUserId.Value),
            ReviewedByName = r.ReviewedByUserId is null ? null : users.GetValueOrDefault(r.ReviewedByUserId.Value),
            SignedOffAt = r.SignedOffAt,
            BankStatementUrl = r.BankStatementUrl,
            Notes = r.Notes,
            IsOverdue = account is not null && account.LastReconciledOn is not null
                        && account.LastReconciledOn.Value.AddDays(account.ReconciliationIntervalDays) < today,

            Exceptions = exceptions.Select(e => new ClientMoneyExceptionDto
            {
                Id = e.Id,
                Kind = e.Kind,
                Severity = e.Severity,
                Description = e.Description ?? string.Empty,
                Amount = e.Amount,
                RaisedOn = e.RaisedOn,
                PartyName = e.PartyId is null ? null : names.GetValueOrDefault(e.PartyId.Value),
                IsResolved = e.IsResolved,
                RequiresRegulatoryReport = e.RequiresRegulatoryReport,
                IsReported = e.IsReported,
                DaysOpen = 0,
            }).ToList(),
        };
    }

    public async Task<PaginatedResponse<ClientMoneyExceptionDto>> GetClientMoneyExceptionsAsync(ListQueryDto query, bool openOnly)
    {
        var q = Db.ClientMoneyExceptions.ForCompany(Tenant)
            .WhereIf(openOnly, e => !e.IsResolved)
            .OrderByDescending(e => e.Severity).ThenBy(e => e.RaisedOn);

        var today = Today;

        return await PageAsync(q, query, async exceptions =>
        {
            if (exceptions.Count == 0) return [];

            var names = await PartyNamesAsync(exceptions.Where(e => e.PartyId.HasValue).Select(e => e.PartyId!.Value));
            var assignees = await AgentUserNamesAsync(exceptions.Select(e => e.AssignedToUserId));

            return exceptions.Select(e => new ClientMoneyExceptionDto
            {
                Id = e.Id,
                Kind = e.Kind,
                Severity = e.Severity,
                Description = e.Description ?? string.Empty,
                Amount = e.Amount,
                RaisedOn = e.RaisedOn,
                PartyName = e.PartyId is null ? null : names.GetValueOrDefault(e.PartyId.Value),
                AssignedToName = e.AssignedToUserId is null ? null : assignees.GetValueOrDefault(e.AssignedToUserId.Value),
                IsResolved = e.IsResolved,
                ResolvedOn = e.ResolvedOn,
                Resolution = e.Resolution,
                RequiresRegulatoryReport = e.RequiresRegulatoryReport,
                IsReported = e.IsReported,
                DaysOpen = (e.ResolvedOn ?? today).DayNumber - e.RaisedOn.DayNumber,
            }).ToList();
        });
    }

    // ═══ Portals ═════════════════════════════════════════════════════════════

    public async Task<OwnerPortalHomeDto> GetOwnerPortalHomeAsync(Guid landlordId)
    {
        var landlord = await RequireAsync<Landlord>(landlordId, "That landlord does not exist.");
        var head = (await MapLandlordListAsync([landlord]))[0];
        var today = Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => t.LandlordId == landlordId && t.Status == TenancyStatus.Active)
            .ToListAsync();

        var tenancyIds = tenancies.Select(t => t.Id).ToList();

        var collected = tenancyIds.Count == 0
            ? 0m
            : await Db.RentCharges.ForCompany(Tenant)
                .Where(c => tenancyIds.Contains(c.TenancyId) && c.SettledOn >= monthStart)
                .SumAsync(c => c.PaidAmount);

        var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

        var home = new OwnerPortalHomeDto
        {
            OwnerName = head.Name,
            CurrencyCode = head.CurrencyCode,
            PropertyCount = head.PropertyCount,
            MonthlyRent = head.MonthlyRent,
            CollectedThisMonth = RealEstateMapper.Money(collected),
            Arrears = head.ArrearsOnPortfolio,
            CurrentBalance = head.CurrentBalance,
            NextPayoutDate = new DateOnly(today.Year, today.Month, Math.Min(landlord.PayoutDay, 28)),
            Tenancies = await MapTenancyListAsync(tenancies),
        };

        home.NextPayoutAmount = await Db.OwnerStatements.ForCompany(Tenant)
            .Where(s => s.LandlordId == landlordId && s.OwnerPayoutId == null)
            .SumAsync(s => s.NetPayable);

        home.Statements = await MapStatementsAsync(
            await Db.OwnerStatements.ForCompany(Tenant)
                .Where(s => s.LandlordId == landlordId)
                .OrderByDescending(s => s.PeriodTo)
                .Take(6)
                .ToListAsync(), false);

        home.ExpiringCertificates = await MapCertificatesAsync(
            await Db.ComplianceCertificates.ForCompany(Tenant)
                .Where(c => propertyIds.Contains(c.PropertyId) && c.IsCurrent && c.ExpiresOn <= today.AddDays(90))
                .OrderBy(c => c.ExpiresOn)
                .ToListAsync());

        return home;
    }

    public async Task<TenantPortalHomeDto> GetTenantPortalHomeAsync(Guid partyId)
    {
        var names = await PartyNamesAsync([partyId]);
        var currency = await CurrencyAsync();

        var tenancyIds = await Db.TenancyParties.ForCompany(Tenant)
            .Where(p => p.PartyId == partyId && (p.ToDate == null || p.ToDate >= Today))
            .Select(p => p.TenancyId)
            .ToListAsync();

        var tenancies = tenancyIds.Count == 0
            ? []
            : await Db.Tenancies.ForCompany(Tenant)
                .Where(t => tenancyIds.Contains(t.Id))
                .ToListAsync();

        var home = new TenantPortalHomeDto
        {
            TenantName = names.GetValueOrDefault(partyId, "—"),
            CurrencyCode = currency,
            Tenancies = await MapTenancyListAsync(tenancies),
            Arrears = RealEstateMapper.Money(tenancies.Sum(t => t.ArrearsAmount)),
        };

        var next = await Db.RentCharges.ForCompany(Tenant)
            .Where(c => tenancyIds.Contains(c.TenancyId) && c.Balance > 0m)
            .OrderBy(c => c.DueDate)
            .Select(c => new { c.DueDate, c.Balance })
            .FirstOrDefaultAsync();

        home.NextRentDate = next?.DueDate;
        home.RentDue = next?.Balance ?? 0m;

        var deposit = await Db.SecurityDeposits.ForCompany(Tenant)
            .Include(d => d.Deductions)
            .Where(d => tenancyIds.Contains(d.TenancyId) && d.ReleasedOn == null)
            .FirstOrDefaultAsync();

        if (deposit is not null) home.Deposit = await MapDepositAsync(deposit);

        var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

        home.Certificates = await MapCertificatesAsync(
            await Db.ComplianceCertificates.ForCompany(Tenant)
                .Where(c => propertyIds.Contains(c.PropertyId) && c.IsCurrent && c.ServedToTenant)
                .OrderBy(c => c.ExpiresOn)
                .ToListAsync());

        return home;
    }
}
