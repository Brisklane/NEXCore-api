using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Investors, capital calls, contributions and distributions.
///
/// An investor's headline number is not what they put in — it is what came back and when. So the
/// return is worked out from the actual dated cash flows rather than from a stored percentage:
/// contributions are negative flows, distributions positive, and the internal rate of return is
/// solved numerically over exactly those dates. Where there is not yet enough history to solve for
/// a rate, nothing is shown rather than something reassuring and wrong.
/// </summary>
public partial class FinanceService
{
    public async Task<PaginatedResponse<InvestorDto>> GetInvestorsAsync(ListQueryDto query, Guid? projectId)
    {
        var search = query.Search?.Trim();

        var matchingParties = string.IsNullOrWhiteSpace(search)
            ? null
            : await Db.Parties.ForCompany(Tenant)
                .Where(p => (p.DisplayName ?? "").Contains(search)
                         || (p.OrganisationName ?? "").Contains(search)
                         || (p.FirstName ?? "").Contains(search)
                         || (p.LastName ?? "").Contains(search)
                         || (p.PrimaryPhone ?? "").Contains(search))
                .Select(p => p.Id)
                .ToListAsync();

        var q = Db.Investors.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, i => i.ProjectId == projectId)
            .WhereIf(matchingParties is not null,
                i => matchingParties!.Contains(i.PartyId) || i.Reference.Contains(search!))
            .OrderByDescending(i => i.CommittedAmount);

        return await PageAsync(q, query, rows => MapInvestorsAsync(rows, includeDetail: false));
    }

    public async Task<InvestorDto?> GetInvestorAsync(Guid id)
    {
        var investor = await Db.Investors.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == id);
        if (investor is null) return null;

        return (await MapInvestorsAsync([investor], includeDetail: true))[0];
    }

    private async Task<List<InvestorDto>> MapInvestorsAsync(List<Investor> investors, bool includeDetail)
    {
        if (investors.Count == 0) return [];

        var currency = await CurrencyAsync();
        var ids = investors.Select(i => i.Id).ToList();

        var names = await PartyNamesAsync(investors.Select(i => i.PartyId));
        var projectNames = await ProjectNamesAsync(investors.Select(i => i.ProjectId));

        var contacts = await Db.Parties.ForCompany(Tenant)
            .Where(p => investors.Select(i => i.PartyId).Contains(p.Id))
            .Select(p => new { p.Id, p.PrimaryPhone, p.PrimaryEmail })
            .ToDictionaryAsync(x => x.Id, x => x);

        var calls = await Db.CapitalCalls.ForCompany(Tenant)
            .Where(c => c.InvestorId != null && ids.Contains(c.InvestorId.Value))
            .OrderByDescending(c => c.IssuedOn)
            .ToListAsync();

        var contributions = await Db.Contributions.ForCompany(Tenant)
            .Where(c => ids.Contains(c.InvestorId))
            .OrderByDescending(c => c.ReceivedOn)
            .ToListAsync();

        var distributions = await Db.Distributions.ForCompany(Tenant)
            .Where(d => ids.Contains(d.InvestorId))
            .OrderByDescending(d => d.DistributedOn)
            .ToListAsync();

        var commitments = await Db.CapitalCommitments.ForCompany(Tenant)
            .Where(c => ids.Contains(c.InvestorId))
            .GroupBy(c => c.InvestorId)
            .Select(g => new { InvestorId = g.Key, Committed = g.Sum(c => c.Amount) })
            .ToDictionaryAsync(x => x.InvestorId, x => x.Committed);

        return investors.Select(i =>
        {
            var mineContrib = contributions.Where(c => c.InvestorId == i.Id).ToList();
            var mineDist = distributions.Where(d => d.InvestorId == i.Id).ToList();
            var mineCalls = calls.Where(c => c.InvestorId == i.Id).ToList();

            var contributed = RealEstateMapper.Money(mineContrib.Sum(c => c.Amount));
            var distributed = RealEstateMapper.Money(mineDist.Sum(d => d.NetAmount));

            // A commitment schedule, where one exists, overrides the headline figure: what an
            // investor is on the hook for is what they signed, not what somebody typed.
            var committed = commitments.TryGetValue(i.Id, out var fromSchedule) && fromSchedule > 0m
                ? fromSchedule
                : i.CommittedAmount;

            var flows = mineContrib
                .Select(c => (Date: c.ReceivedOn, Amount: -c.Amount))
                .Concat(mineDist.Where(d => d.IsPaid).Select(d => (Date: d.DistributedOn, Amount: d.NetAmount)))
                .OrderBy(f => f.Date)
                .ToList();

            var exited = i.ExitDate is not null;

            var dto = new InvestorDto
            {
                Id = i.Id,
                Reference = i.Reference,
                PartyId = i.PartyId,
                Name = names.GetValueOrDefault(i.PartyId, "—"),
                Phone = contacts.GetValueOrDefault(i.PartyId)?.PrimaryPhone,
                Email = contacts.GetValueOrDefault(i.PartyId)?.PrimaryEmail,
                ProjectId = i.ProjectId,
                ProjectName = i.ProjectId is null ? null : projectNames.GetValueOrDefault(i.ProjectId.Value),
                InvestmentType = i.InvestmentType,
                CommittedAmount = committed,
                ContributedAmount = contributed,
                UndrawnAmount = RealEstateMapper.Money(Math.Max(0m, committed - contributed)),
                DistributedAmount = distributed,
                SharePercent = i.SharePercent,
                PreferredReturnPercent = i.PreferredReturnPercent,
                CurrencyCode = currency,
                InvestedOn = i.InvestedOn,
                ExitDate = i.ExitDate,
                RealisedIrr = exited ? Irr(flows) : i.RealisedIrr,
                UnrealisedIrr = exited ? null : Irr(WithNotionalExit(flows, contributed, distributed, i)),
                MultipleOnInvestedCapital = contributed <= 0m
                    ? null
                    : Math.Round(distributed / contributed, 2, MidpointRounding.AwayFromZero),
                BankName = i.BankName,
                AccountNumber = i.AccountNumber,
                WithholdingPercent = i.WithholdingPercent,
                AgreementUrl = i.AgreementUrl,
                PortalAccessEnabled = i.PortalAccessEnabled,
                IsActive = i.IsActive,
            };

            if (includeDetail)
            {
                dto.Calls = mineCalls.Select(c => MapCall(c, names.GetValueOrDefault(i.PartyId, "—"),
                    c.ProjectId is null ? null : projectNames.GetValueOrDefault(c.ProjectId.Value), currency)).ToList();

                dto.Contributions = mineContrib.Select(c => new ContributionDto
                {
                    Id = c.Id,
                    InvestorId = c.InvestorId,
                    InvestorName = names.GetValueOrDefault(i.PartyId, "—"),
                    CapitalCallId = c.CapitalCallId,
                    ReceivedOn = c.ReceivedOn,
                    Amount = c.Amount,
                    Instrument = c.Instrument,
                    Reference = c.Reference,
                }).ToList();

                dto.Distributions = mineDist.Select(d => MapDistribution(d,
                    names.GetValueOrDefault(i.PartyId, "—"),
                    d.ProjectId is null ? null : projectNames.GetValueOrDefault(d.ProjectId.Value),
                    currency)).ToList();
            }

            return dto;
        }).ToList();
    }

    private static CapitalCallDto MapCall(CapitalCall c, string investorName, string? projectName, string currency)
        => new()
        {
            Id = c.Id,
            Reference = c.Reference,
            ProjectId = c.ProjectId,
            ProjectName = projectName,
            InvestorId = c.InvestorId,
            InvestorName = investorName,
            IssuedOn = c.IssuedOn,
            DueDate = c.DueDate,
            Amount = c.Amount,
            ReceivedAmount = c.ReceivedAmount,
            ReceivedOn = c.ReceivedOn,
            Purpose = c.Purpose,
            IsDefaulted = c.IsDefaulted,
            DefaultPenalty = c.DefaultPenalty,
            IsSettled = c.IsSettled,
            IsOverdue = !c.IsSettled && c.DueDate < DateOnly.FromDateTime(DateTime.UtcNow),
            NoticeUrl = c.NoticeUrl,
            CurrencyCode = currency,
        };

    private static DistributionDto MapDistribution(Distribution d, string investorName, string? projectName, string currency)
        => new()
        {
            Id = d.Id,
            Reference = d.Reference,
            InvestorId = d.InvestorId,
            InvestorName = investorName,
            ProjectId = d.ProjectId,
            ProjectName = projectName,
            DistributedOn = d.DistributedOn,
            DistributionType = d.DistributionType,
            GrossAmount = d.GrossAmount,
            WithholdingAmount = d.WithholdingAmount,
            NetAmount = d.NetAmount,
            WaterfallTier = d.WaterfallTier,
            PaymentReference = d.PaymentReference,
            IsPaid = d.IsPaid,
            CurrencyCode = currency,
        };

    /// <summary>
    /// For an investor still in the deal, the return so far only means something if the money still
    /// invested is assumed to come back today. That notional exit is added at today's date, at the
    /// capital still outstanding, and clearly labelled unrealised so nobody mistakes it for cash.
    /// </summary>
    private static List<(DateOnly Date, decimal Amount)> WithNotionalExit(
        List<(DateOnly Date, decimal Amount)> flows, decimal contributed, decimal distributed, Investor investor)
    {
        var outstanding = contributed - distributed;
        if (outstanding <= 0m || flows.Count == 0) return flows;

        // Carry the preferred return on the outstanding capital to today, which is the closest
        // defensible estimate of what the investor would be owed on a wind-up right now.
        var from = flows[0].Date;
        var years = (decimal)(DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - from.DayNumber) / 365.25m;
        var accrued = investor.PreferredReturnPercent > 0m
            ? outstanding * investor.PreferredReturnPercent / 100m * Math.Max(0m, years)
            : 0m;

        return [.. flows, (DateOnly.FromDateTime(DateTime.UtcNow), outstanding + accrued)];
    }

    /// <summary>
    /// Internal rate of return over dated cash flows, solved by bisection between −99% and +1000%.
    ///
    /// Bisection rather than Newton–Raphson because it cannot diverge: with cash flows as irregular
    /// as a property fund's, a derivative-based solver will happily wander off and return nonsense,
    /// and a wrong return figure on an investor statement is worse than no figure at all.
    /// </summary>
    private static decimal? Irr(List<(DateOnly Date, decimal Amount)> flows)
    {
        if (flows.Count < 2) return null;
        if (!flows.Any(f => f.Amount < 0m) || !flows.Any(f => f.Amount > 0m)) return null;

        var start = flows.Min(f => f.Date);

        double Npv(double rate)
        {
            var total = 0d;

            foreach (var (date, amount) in flows)
            {
                var years = (date.DayNumber - start.DayNumber) / 365.25d;
                total += (double)amount / Math.Pow(1d + rate, years);
            }

            return total;
        }

        var low = -0.99d;
        var high = 10d;

        var atLow = Npv(low);
        var atHigh = Npv(high);

        // No sign change means there is no rate in this range that zeroes the net present value.
        // That happens with genuinely pathological flows, and the honest answer is silence.
        if (atLow * atHigh > 0d) return null;

        for (var i = 0; i < 200; i++)
        {
            var mid = (low + high) / 2d;
            var atMid = Npv(mid);

            if (Math.Abs(atMid) < 0.0001d) { low = high = mid; break; }

            if (atLow * atMid < 0d) high = mid;
            else { low = mid; atLow = atMid; }
        }

        var answer = (low + high) / 2d * 100d;

        return Math.Round((decimal)answer, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<InvestorDto> SaveInvestorAsync(InvestorDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var investor = isNew
            ? new Investor { Reference = await numbering.NextMasterCodeAsync(Db.Investors, "INV") }
            : await Db.Investors.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == dto.Id)
              ?? throw new InvalidOperationException("That investor does not exist.");

        if (dto.PartyId == Guid.Empty)
            throw new InvalidOperationException("An investor has to be linked to a party record.");

        if (!isNew && investor.ExitDate is not null && dto.ExitDate is null)
            throw new InvalidOperationException(
                "That investor has already exited. Reversing an exit changes every return figure already reported and needs a new record.");

        investor.PartyId = dto.PartyId;
        investor.ProjectId = dto.ProjectId;
        investor.InvestmentType = dto.InvestmentType;
        investor.CommittedAmount = dto.CommittedAmount;
        investor.SharePercent = dto.SharePercent;
        investor.PreferredReturnPercent = dto.PreferredReturnPercent;
        investor.InvestedOn = dto.InvestedOn;
        investor.ExitDate = dto.ExitDate;
        investor.BankName = dto.BankName;
        investor.AccountNumber = dto.AccountNumber;
        investor.WithholdingPercent = dto.WithholdingPercent;
        investor.AgreementUrl = dto.AgreementUrl;
        investor.PortalAccessEnabled = dto.PortalAccessEnabled;
        investor.IsActive = dto.IsActive;

        if (isNew)
        {
            investor.StampNew(Tenant, userId);
            Db.Investors.Add(investor);
        }
        else
        {
            investor.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetInvestorAsync(investor.Id))!;
    }

    // ═══ Capital calls ═══════════════════════════════════════════════════════

    public async Task<CapitalCallDto> IssueCapitalCallAsync(CapitalCallDto dto, Guid userId)
    {
        if (dto.Amount <= 0m)
            throw new InvalidOperationException("A capital call has to be for more than nothing.");

        if (dto.DueDate < dto.IssuedOn)
            throw new InvalidOperationException("A capital call cannot fall due before it was issued.");

        Investor? investor = null;

        if (dto.InvestorId is not null)
        {
            investor = await Db.Investors.ForCompany(Tenant)
                .FirstOrDefaultAsync(i => i.Id == dto.InvestorId)
                ?? throw new InvalidOperationException("That investor does not exist.");

            if (investor.ExitDate is not null)
                throw new InvalidOperationException($"{investor.Reference} has exited and cannot be called on.");

            var contributed = await Db.Contributions.ForCompany(Tenant)
                .Where(c => c.InvestorId == investor.Id)
                .SumAsync(c => (decimal?)c.Amount) ?? 0m;

            var outstandingCalls = await Db.CapitalCalls.ForCompany(Tenant)
                .Where(c => c.InvestorId == investor.Id && !c.IsSettled)
                .SumAsync(c => (decimal?)(c.Amount - c.ReceivedAmount)) ?? 0m;

            // Calling more than the investor committed is the fastest way to lose one. It is
            // blocked outright, with the arithmetic shown, rather than warned about.
            var headroom = investor.CommittedAmount - contributed - outstandingCalls;

            if (dto.Amount > headroom + 0.01m)
                throw new InvalidOperationException(
                    $"That call exceeds the undrawn commitment. Committed {investor.CommittedAmount:N0}, "
                    + $"contributed {contributed:N0}, already called and unpaid {outstandingCalls:N0} — "
                    + $"leaving {Math.Max(0m, headroom):N0} available to call.");
        }

        var call = new CapitalCall
        {
            Reference = await numbering.NextMasterCodeAsync(Db.CapitalCalls, "CAP"),
            ProjectId = dto.ProjectId,
            InvestorId = dto.InvestorId,
            IssuedOn = dto.IssuedOn == default ? Today : dto.IssuedOn,
            DueDate = dto.DueDate,
            Amount = RealEstateMapper.Money(dto.Amount),
            Purpose = dto.Purpose,
            DefaultPenalty = dto.DefaultPenalty,
            NoticeUrl = dto.NoticeUrl,
            Description = dto.Purpose,
        }.StampNew(Tenant, userId);

        Db.CapitalCalls.Add(call);

        if (dto.ProjectId is not null)
        {
            var commitment = await Db.CapitalCommitments.ForCompany(Tenant)
                .FirstOrDefaultAsync(c => c.InvestorId == dto.InvestorId && c.ProjectId == dto.ProjectId);

            if (commitment is not null)
            {
                commitment.UndrawnAmount = RealEstateMapper.Money(
                    Math.Max(0m, commitment.Amount - commitment.DrawnAmount));
                commitment.StampUpdated(userId);
            }
        }

        await QueueNotificationAsync(
            "investor.capital.called",
            $"Capital call {call.Reference}",
            $"{call.Amount:N0} due {call.DueDate:dd MMM yyyy}."
            + (call.Purpose is null ? null : $" {call.Purpose}"),
            $"/realestate/finance/investors/{dto.InvestorId}",
            recipientPartyId: investor?.PartyId,
            entityType: nameof(CapitalCall),
            entityId: call.Id,
            severity: AlertSeverity.Warning);

        await Db.SaveChangesAsync();

        var currency = await CurrencyAsync();
        var names = investor is null ? [] : await PartyNamesAsync([investor.PartyId]);
        var projects = await ProjectNamesAsync([dto.ProjectId]);

        return MapCall(call,
            investor is null ? "—" : names.GetValueOrDefault(investor.PartyId, "—"),
            dto.ProjectId is null ? null : projects.GetValueOrDefault(dto.ProjectId.Value),
            currency);
    }

    public async Task<ContributionDto> RecordContributionAsync(ContributionDto dto, Guid userId)
    {
        if (dto.Amount <= 0m)
            throw new InvalidOperationException("A contribution has to be for more than nothing.");

        var investor = await Db.Investors.ForCompany(Tenant)
            .FirstOrDefaultAsync(i => i.Id == dto.InvestorId)
            ?? throw new InvalidOperationException("That investor does not exist.");

        CapitalCall? call = null;

        if (dto.CapitalCallId is not null)
        {
            call = await Db.CapitalCalls.ForCompany(Tenant)
                .FirstOrDefaultAsync(c => c.Id == dto.CapitalCallId)
                ?? throw new InvalidOperationException("That capital call does not exist.");

            if (call.InvestorId is not null && call.InvestorId != investor.Id)
                throw new InvalidOperationException("That capital call was issued to a different investor.");

            var outstanding = call.Amount - call.ReceivedAmount;

            if (dto.Amount > outstanding + 0.01m)
                throw new InvalidOperationException(
                    $"That is more than call {call.Reference} is asking for. {outstanding:N0} remains outstanding.");
        }

        var contribution = new Contribution
        {
            InvestorId = investor.Id,
            CapitalCallId = dto.CapitalCallId,
            ProjectId = investor.ProjectId,
            ReceivedOn = dto.ReceivedOn == default ? Today : dto.ReceivedOn,
            Amount = RealEstateMapper.Money(dto.Amount),
            Instrument = dto.Instrument,
            Reference = dto.Reference,
        }.StampNew(Tenant, userId);

        Db.Contributions.Add(contribution);

        investor.ContributedAmount = RealEstateMapper.Money(investor.ContributedAmount + contribution.Amount);

        if (investor.InvestedOn is null) investor.InvestedOn = contribution.ReceivedOn;

        investor.StampUpdated(userId);

        if (call is not null)
        {
            call.ReceivedAmount = RealEstateMapper.Money(call.ReceivedAmount + contribution.Amount);
            call.ReceivedOn = contribution.ReceivedOn;
            call.IsSettled = call.ReceivedAmount >= call.Amount - 0.01m;

            // A call paid late but paid in full is no longer in default. Leaving the flag set makes
            // the investor look like a defaulter forever over a week's delay.
            if (call.IsSettled) call.IsDefaulted = false;

            call.StampUpdated(userId);
        }

        var commitment = await Db.CapitalCommitments.ForCompany(Tenant)
            .FirstOrDefaultAsync(c => c.InvestorId == investor.Id
                                   && (investor.ProjectId == null || c.ProjectId == investor.ProjectId));

        if (commitment is not null)
        {
            commitment.DrawnAmount = RealEstateMapper.Money(commitment.DrawnAmount + contribution.Amount);
            commitment.UndrawnAmount = RealEstateMapper.Money(
                Math.Max(0m, commitment.Amount - commitment.DrawnAmount));
            commitment.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        var names = await PartyNamesAsync([investor.PartyId]);

        return new ContributionDto
        {
            Id = contribution.Id,
            InvestorId = contribution.InvestorId,
            InvestorName = names.GetValueOrDefault(investor.PartyId, "—"),
            CapitalCallId = contribution.CapitalCallId,
            ReceivedOn = contribution.ReceivedOn,
            Amount = contribution.Amount,
            Instrument = contribution.Instrument,
            Reference = contribution.Reference,
        };
    }

    // ═══ Distributions ═══════════════════════════════════════════════════════

    public async Task<DistributionDto> RecordDistributionAsync(DistributionDto dto, Guid userId)
    {
        if (dto.GrossAmount <= 0m)
            throw new InvalidOperationException("A distribution has to be for more than nothing.");

        var investor = await Db.Investors.ForCompany(Tenant)
            .FirstOrDefaultAsync(i => i.Id == dto.InvestorId)
            ?? throw new InvalidOperationException("That investor does not exist.");

        // Withholding is deducted at the investor's own rate unless the caller has worked out a
        // different figure — an exemption certificate, or a treaty rate — in which case theirs wins.
        var withheld = dto.WithholdingAmount > 0m
            ? dto.WithholdingAmount
            : RealEstateMapper.Money(dto.GrossAmount * investor.WithholdingPercent / 100m);

        if (withheld > dto.GrossAmount)
            throw new InvalidOperationException("The withholding cannot be more than the distribution itself.");

        var net = RealEstateMapper.Money(dto.GrossAmount - withheld);

        var distribution = new Distribution
        {
            Reference = await numbering.NextMasterCodeAsync(Db.Distributions, "DST"),
            InvestorId = investor.Id,
            ProjectId = dto.ProjectId ?? investor.ProjectId,
            DistributedOn = dto.DistributedOn == default ? Today : dto.DistributedOn,
            DistributionType = dto.DistributionType,
            GrossAmount = RealEstateMapper.Money(dto.GrossAmount),
            WithholdingAmount = withheld,
            NetAmount = net,
            WaterfallTier = dto.WaterfallTier,
            PaymentReference = dto.PaymentReference,
            IsPaid = dto.IsPaid,
        }.StampNew(Tenant, userId);

        var approval = await RaiseApprovalAsync(
            nameof(Distribution), distribution.Id, distribution.Reference, distribution.GrossAmount,
            $"Distribution of {distribution.GrossAmount:N0} to investor {investor.Reference}",
            userId, projectId: distribution.ProjectId);

        distribution.ApprovalRequestId = approval?.Id;

        // Money does not leave until the approval that governs it has been given. Marking it paid
        // while an approval is pending is exactly the hole this control exists to close.
        if (approval is not null && distribution.IsPaid)
        {
            distribution.IsPaid = false;
        }

        Db.Distributions.Add(distribution);

        investor.DistributedAmount = RealEstateMapper.Money(investor.DistributedAmount + net);
        investor.StampUpdated(userId);

        if (withheld > 0m)
        {
            Db.WithholdingRecords.Add(new WithholdingRecord
            {
                Reference = await numbering.NextMasterCodeAsync(Db.WithholdingRecords, "WHT"),
                Kind = WithholdingKind.OnProfessionalFee,
                PartyId = investor.PartyId,
                DeductedOn = distribution.DistributedOn,
                GrossAmount = distribution.GrossAmount,
                Rate = RealEstateMapper.Percent(withheld, distribution.GrossAmount),
                WithheldAmount = withheld,
                DistributionId = distribution.Id,
                ReturnPeriod = $"{distribution.DistributedOn:yyyy-MM}",
                Description = $"Withheld on distribution {distribution.Reference}",
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();

        var currency = await CurrencyAsync();
        var names = await PartyNamesAsync([investor.PartyId]);
        var projects = await ProjectNamesAsync([distribution.ProjectId]);

        var result = MapDistribution(distribution,
            names.GetValueOrDefault(investor.PartyId, "—"),
            distribution.ProjectId is null ? null : projects.GetValueOrDefault(distribution.ProjectId.Value),
            currency);

        return result;
    }
}
