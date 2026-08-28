using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Revenue recognition, cost allocation, unit profitability and tax.
///
/// The question this answers is the one a developer's accountant loses sleep over: of the money
/// customers have paid, how much has actually been *earned*? Under IFRS 15 that depends on whether
/// control passes over time or at a point in time, and the two give wildly different answers in the
/// same year. So the basis is a decision recorded against the contract, with its rationale, before
/// any run happens — never a switch flipped inside the arithmetic.
///
/// Everything here is additive. A run computes revenue *to date* and books the difference against
/// what was recognised before, so re-running a period cannot double-count, and a prior period's
/// figures are never quietly restated.
/// </summary>
public partial class FinanceService
{
    public async Task<RecognitionPolicyDto> SaveRecognitionPolicyAsync(RecognitionPolicyDto dto, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Rationale))
            throw new InvalidOperationException(
                "A recognition policy needs its rationale in writing. It is the first thing an auditor asks for "
                + "and the last thing anybody can reconstruct from memory.");

        if (dto.Basis == RecognitionBasis.OverTime && string.IsNullOrWhiteSpace(dto.OverTimeMethod))
            throw new InvalidOperationException(
                "Recognising over time needs the method of measuring progress — cost-to-cost, surveys of work "
                + "performed, or units delivered.");

        if (dto.Basis == RecognitionBasis.PointInTime && string.IsNullOrWhiteSpace(dto.PointInTimeTrigger))
            throw new InvalidOperationException(
                "Recognising at a point in time needs the event that transfers control — possession, "
                + "registration, or handover.");

        if (dto.ProjectId is null && dto.BookingId is null && dto.ClientBuildContractId is null)
            throw new InvalidOperationException("A recognition policy has to attach to a project, a booking or a contract.");

        var isNew = dto.Id == Guid.Empty;

        var policy = isNew
            ? new RecognitionPolicy()
            : await Db.RecognitionPolicies.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
              ?? throw new InvalidOperationException("That policy does not exist.");

        // Changing the basis after revenue has been recognised restates the accounts. It needs an
        // approval, not a save button.
        if (!isNew && policy.Basis != dto.Basis)
        {
            var alreadyRecognised = await Db.RecognitionEntries.ForCompany(Tenant)
                .Where(e => (dto.BookingId != null && e.BookingId == dto.BookingId)
                         || (dto.ClientBuildContractId != null && e.ClientBuildContractId == dto.ClientBuildContractId)
                         || (dto.BookingId == null && dto.ClientBuildContractId == null && e.ProjectId == dto.ProjectId))
                .AnyAsync(e => e.RevenueRecognisedToDate > 0m);

            if (alreadyRecognised)
                throw new InvalidOperationException(
                    "Revenue has already been recognised under the current basis. Changing it restates the "
                    + "accounts and has to go through a prior-period adjustment rather than an edit here.");
        }

        policy.ProjectId = dto.ProjectId;
        policy.BookingId = dto.BookingId;
        policy.ClientBuildContractId = dto.ClientBuildContractId;
        policy.Basis = dto.Basis;
        policy.OverTimeMethod = dto.OverTimeMethod;
        policy.PointInTimeTrigger = dto.PointInTimeTrigger;
        policy.Rationale = dto.Rationale;
        policy.Description = dto.Rationale;
        policy.DeterminedOn = dto.DeterminedOn == default ? Today : dto.DeterminedOn;
        policy.DeterminedByUserId = userId;
        policy.IsActive = dto.IsActive;

        if (isNew)
        {
            policy.StampNew(Tenant, userId);
            Db.RecognitionPolicies.Add(policy);
        }
        else
        {
            policy.StampUpdated(userId);
        }

        // Where the policy governs a whole project, the project carries the basis too, so screens
        // that show a single figure do not have to resolve the policy hierarchy each time.
        if (policy is { ProjectId: not null, BookingId: null, ClientBuildContractId: null })
        {
            var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == policy.ProjectId);

            if (project is not null)
            {
                project.RecognitionBasis = policy.Basis;
                project.RecognitionRationale = policy.Rationale;
                project.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();

        var names = await ProjectNamesAsync([policy.ProjectId]);
        var users = await AgentUserNamesAsync([policy.DeterminedByUserId]);

        return new RecognitionPolicyDto
        {
            Id = policy.Id,
            ProjectId = policy.ProjectId,
            ProjectName = policy.ProjectId is null ? null : names.GetValueOrDefault(policy.ProjectId.Value),
            BookingId = policy.BookingId,
            ClientBuildContractId = policy.ClientBuildContractId,
            Basis = policy.Basis,
            OverTimeMethod = policy.OverTimeMethod,
            PointInTimeTrigger = policy.PointInTimeTrigger,
            Rationale = policy.Rationale,
            DeterminedOn = policy.DeterminedOn,
            DeterminedByName = policy.DeterminedByUserId is null
                ? null
                : users.GetValueOrDefault(policy.DeterminedByUserId.Value),
            IsActive = policy.IsActive,
        };
    }

    // ═══ The run ═════════════════════════════════════════════════════════════

    public async Task<RevenueRecognitionRunDto> RunRecognitionAsync(
        Guid? projectId, DateOnly periodFrom, DateOnly periodTo, bool dryRun, Guid userId)
    {
        if (periodTo < periodFrom)
            throw new InvalidOperationException("The period ends before it begins.");

        if (!dryRun)
        {
            var overlapping = await Db.RevenueRecognitionRuns.ForCompany(Tenant)
                .Where(r => !r.IsDryRun && r.ProjectId == projectId)
                .Where(r => r.PeriodTo >= periodFrom && r.PeriodFrom <= periodTo)
                .Select(r => r.Reference)
                .FirstOrDefaultAsync();

            // Two posted runs over the same period would each book "the difference since last
            // time", and the second would find nothing. Better to say so than to post a zero.
            if (overlapping is not null)
                throw new InvalidOperationException(
                    $"Run {overlapping} already covers part of this period. Reverse it before running again.");
        }

        var run = new RevenueRecognitionRun
        {
            Reference = await numbering.NextMasterCodeAsync(Db.RevenueRecognitionRuns, "REV"),
            ProjectId = projectId,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            RunDate = Today,
            IsDryRun = dryRun,
            RunByUserId = userId,
        };

        var entries = new List<RecognitionEntry>();
        var problems = new List<string>();

        entries.AddRange(await BuildBookingEntriesAsync(projectId, periodTo, problems));
        entries.AddRange(await BuildContractEntriesAsync(projectId, periodTo, problems));

        run.ContractCount = entries.Count;
        run.RevenueRecognised = RealEstateMapper.Money(entries.Sum(e => e.RevenueThisPeriod));
        run.CostRecognised = RealEstateMapper.Money(entries.Sum(e => e.CostRecognisedThisPeriod));
        run.GrossMargin = RealEstateMapper.Money(run.RevenueRecognised - run.CostRecognised);
        run.ContractAssetTotal = RealEstateMapper.Money(entries.Sum(e => e.ContractAsset));
        run.ContractLiabilityTotal = RealEstateMapper.Money(entries.Sum(e => e.ContractLiability));
        run.ErrorSummary = problems.Count == 0 ? null : string.Join(" ", problems);

        if (dryRun)
        {
            // Nothing is written. The caller gets exactly what a real run would produce, which is
            // the only way anybody can sensibly review a period close before committing it.
            return await MapRunAsync(run, entries, includeEntries: true);
        }

        run.StampNew(Tenant, userId);
        Db.RevenueRecognitionRuns.Add(run);

        foreach (var entry in entries)
        {
            entry.RevenueRecognitionRunId = run.Id;
            entry.StampNew(Tenant, userId);
            Db.RecognitionEntries.Add(entry);
        }

        await Db.SaveChangesAsync();

        return await MapRunAsync(run, entries, includeEntries: true);
    }

    /// <summary>
    /// One recognition entry per live booking, on whichever basis the policy hierarchy resolves to:
    /// a policy on the booking beats a policy on the project, which beats the project's own default.
    /// </summary>
    private async Task<List<RecognitionEntry>> BuildBookingEntriesAsync(
        Guid? projectId, DateOnly periodTo, List<string> problems)
    {
        var live = new[]
        {
            BookingStatus.Confirmed, BookingStatus.AgreementSigned, BookingStatus.Defaulting,
            BookingStatus.PossessionOffered, BookingStatus.Possessed, BookingStatus.Completed,
        };

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => live.Contains(b.Status))
            .WhereIf(projectId.HasValue, b => b.ProjectId == projectId)
            .Where(b => b.BookingDate <= periodTo)
            .ToListAsync();

        if (bookings.Count == 0) return [];

        var projectIds = bookings.Select(b => b.ProjectId).Distinct().ToList();

        var projects = await Db.Projects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var policies = await Db.RecognitionPolicies.ForCompany(Tenant)
            .Where(p => p.IsActive)
            .Where(p => (p.ProjectId != null && projectIds.Contains(p.ProjectId.Value))
                     || (p.BookingId != null && bookings.Select(b => b.Id).Contains(p.BookingId.Value)))
            .ToListAsync();

        var progress = await ProjectProgressAsync(projectIds);

        var costs = await Db.UnitCostAllocations.ForCompany(Tenant)
            .Where(c => projectIds.Contains(c.ProjectId))
            .GroupBy(c => c.UnitId)
            .Select(g => new
            {
                UnitId = g.Key,
                Cost = g.OrderByDescending(x => x.AsOfDate).Select(x => x.TotalAllocatedCost).First(),
            })
            .ToDictionaryAsync(x => x.UnitId, x => x.Cost);

        var previous = await PreviousRecognitionAsync(bookings.Select(b => b.Id).ToList(), forBookings: true);

        var entries = new List<RecognitionEntry>();

        foreach (var booking in bookings)
        {
            var policy = policies.FirstOrDefault(p => p.BookingId == booking.Id)
                         ?? policies.FirstOrDefault(p => p.ProjectId == booking.ProjectId && p.BookingId == null);

            var basis = policy?.Basis
                        ?? (projects.TryGetValue(booking.ProjectId, out var proj) ? proj.RecognitionBasis : RecognitionBasis.PointInTime);

            var contractValue = booking.TotalConsideration > 0m ? booking.TotalConsideration : booking.NetSalePrice;

            var allocated = booking.UnitId is not null ? costs.GetValueOrDefault(booking.UnitId.Value) : 0m;

            if (allocated <= 0m && contractValue > 0m)
                problems.Add($"Booking {booking.Reference} has no allocated cost, so its margin is shown as the whole sale value.");

            var totalEstimated = allocated;

            decimal percentComplete;
            decimal revenueToDate;

            if (basis == RecognitionBasis.OverTime)
            {
                // Cost-to-cost is the default measure because it is the only one derivable from
                // records the developer already keeps. Where an over-time method says otherwise,
                // certified milestone progress stands in for it.
                percentComplete = policy?.OverTimeMethod == "SurveyOfWork"
                    ? progress.GetValueOrDefault(booking.ProjectId)
                    : progress.GetValueOrDefault(booking.ProjectId);

                percentComplete = Math.Clamp(percentComplete, 0m, 100m);
                revenueToDate = RealEstateMapper.Money(contractValue * percentComplete / 100m);
            }
            else
            {
                var trigger = policy?.PointInTimeTrigger ?? "Possession";

                var transferred = trigger switch
                {
                    "Registration" => booking.RegisteredOn is not null && booking.RegisteredOn <= periodTo,
                    "AgreementSigned" => booking.AgreementSignedOn is not null && booking.AgreementSignedOn <= periodTo,
                    _ => booking.PossessionTakenOn is not null && booking.PossessionTakenOn <= periodTo,
                };

                percentComplete = transferred ? 100m : 0m;
                revenueToDate = transferred ? contractValue : 0m;
            }

            var previouslyRecognised = previous.GetValueOrDefault(booking.Id);
            var thisPeriod = RealEstateMapper.Money(revenueToDate - previouslyRecognised);

            var costToDate = RealEstateMapper.Money(totalEstimated * percentComplete / 100m);
            var costPreviously = RealEstateMapper.Money(totalEstimated * PreviousPercent(previouslyRecognised, contractValue) / 100m);

            var isLoss = totalEstimated > contractValue && contractValue > 0m;

            entries.Add(new RecognitionEntry
            {
                BookingId = booking.Id,
                ProjectId = booking.ProjectId,
                Basis = basis,
                PeriodTo = periodTo,
                ContractValue = RealEstateMapper.Money(contractValue),
                CostIncurredToDate = costToDate,
                TotalEstimatedCost = RealEstateMapper.Money(totalEstimated),
                PercentComplete = RealEstateMapper.Money(percentComplete),
                RevenueRecognisedToDate = revenueToDate,
                RevenueRecognisedPreviously = previouslyRecognised,
                RevenueThisPeriod = thisPeriod,
                CostRecognisedThisPeriod = RealEstateMapper.Money(costToDate - costPreviously),
                AmountsInvoiced = booking.TotalDemanded,
                AmountsCollected = booking.TotalPaid,
                ContractAsset = RealEstateMapper.Money(Math.Max(0m, revenueToDate - booking.TotalDemanded)),
                ContractLiability = RealEstateMapper.Money(Math.Max(0m, booking.TotalDemanded - revenueToDate)),
                IsLossMaking = isLoss,

                // An onerous contract is provided for in full the moment it is identified, not
                // spread over the remaining life. That is the whole point of the requirement.
                ProvisionForLoss = isLoss
                    ? RealEstateMapper.Money((totalEstimated - contractValue) * (100m - percentComplete) / 100m)
                    : null,
            });
        }

        return entries;
    }

    private async Task<List<RecognitionEntry>> BuildContractEntriesAsync(
        Guid? projectId, DateOnly periodTo, List<string> problems)
    {
        var contracts = await Db.ClientBuildContracts.ForCompany(Tenant)
            .Where(c => c.Status != "Draft" && c.Status != "Cancelled")
            .WhereIf(projectId.HasValue, c => c.ConstructionProjectId == projectId)
            .ToListAsync();

        if (contracts.Count == 0) return [];

        var policies = await Db.RecognitionPolicies.ForCompany(Tenant)
            .Where(p => p.IsActive && p.ClientBuildContractId != null
                     && contracts.Select(c => c.Id).Contains(p.ClientBuildContractId.Value))
            .ToListAsync();

        var previous = await PreviousRecognitionAsync(contracts.Select(c => c.Id).ToList(), forBookings: false);

        var entries = new List<RecognitionEntry>();

        foreach (var contract in contracts)
        {
            var policy = policies.FirstOrDefault(p => p.ClientBuildContractId == contract.Id);

            // A build contract on somebody else's land creates an asset with no alternative use and
            // an enforceable right to payment for work done. That is over time by construction, and
            // the default reflects it rather than making every builder set a policy by hand.
            var basis = policy?.Basis ?? RecognitionBasis.OverTime;

            var contractValue = contract.RevisedContractValue > 0m
                ? contract.RevisedContractValue
                : contract.ContractValue;

            var totalEstimated = contract.ForecastFinalCost > 0m
                ? contract.ForecastFinalCost
                : contract.BudgetCost;

            if (totalEstimated <= 0m)
            {
                problems.Add($"Contract {contract.Reference} has no cost forecast, so it was left out of this run.");
                continue;
            }

            decimal percentComplete;

            if (basis == RecognitionBasis.OverTime)
            {
                percentComplete = policy?.OverTimeMethod == "SurveyOfWork"
                    ? Math.Clamp(contract.ProgressPercent, 0m, 100m)
                    : Math.Clamp(RealEstateMapper.Percent(contract.ActualCost, totalEstimated), 0m, 100m);
            }
            else
            {
                percentComplete = contract.ActualCompletionDate is not null && contract.ActualCompletionDate <= periodTo
                    ? 100m
                    : 0m;
            }

            var revenueToDate = RealEstateMapper.Money(contractValue * percentComplete / 100m);
            var previouslyRecognised = previous.GetValueOrDefault(contract.Id);

            var costToDate = RealEstateMapper.Money(totalEstimated * percentComplete / 100m);
            var costPreviously = RealEstateMapper.Money(
                totalEstimated * PreviousPercent(previouslyRecognised, contractValue) / 100m);

            var isLoss = totalEstimated > contractValue;

            entries.Add(new RecognitionEntry
            {
                ClientBuildContractId = contract.Id,
                ProjectId = contract.ConstructionProjectId,
                Basis = basis,
                PeriodTo = periodTo,
                ContractValue = RealEstateMapper.Money(contractValue),
                CostIncurredToDate = RealEstateMapper.Money(contract.ActualCost),
                TotalEstimatedCost = RealEstateMapper.Money(totalEstimated),
                PercentComplete = RealEstateMapper.Money(percentComplete),
                RevenueRecognisedToDate = revenueToDate,
                RevenueRecognisedPreviously = previouslyRecognised,
                RevenueThisPeriod = RealEstateMapper.Money(revenueToDate - previouslyRecognised),
                CostRecognisedThisPeriod = RealEstateMapper.Money(costToDate - costPreviously),
                AmountsInvoiced = contract.TotalDemanded,
                AmountsCollected = contract.TotalReceived,
                ContractAsset = RealEstateMapper.Money(Math.Max(0m, revenueToDate - contract.TotalDemanded)),
                ContractLiability = RealEstateMapper.Money(Math.Max(0m, contract.TotalDemanded - revenueToDate)),
                IsLossMaking = isLoss,
                ProvisionForLoss = isLoss
                    ? RealEstateMapper.Money((totalEstimated - contractValue) * (100m - percentComplete) / 100m)
                    : null,
            });
        }

        return entries;
    }

    private static decimal PreviousPercent(decimal previouslyRecognised, decimal contractValue)
        => contractValue <= 0m ? 0m : Math.Clamp(previouslyRecognised / contractValue * 100m, 0m, 100m);

    /// <summary>
    /// What each contract had already been credited with before this run, taken from the most
    /// recent posted entry rather than by summing — the entry already carries a to-date figure.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> PreviousRecognitionAsync(List<Guid> ids, bool forBookings)
    {
        if (ids.Count == 0) return [];

        var query = Db.RecognitionEntries.ForCompany(Tenant)
            .Join(Db.RevenueRecognitionRuns.ForCompany(Tenant),
                e => e.RevenueRecognitionRunId, r => r.Id, (e, r) => new { Entry = e, Run = r })
            .Where(x => !x.Run.IsDryRun);

        if (forBookings)
        {
            return await query
                .Where(x => x.Entry.BookingId != null && ids.Contains(x.Entry.BookingId.Value))
                .GroupBy(x => x.Entry.BookingId!.Value)
                .Select(g => new
                {
                    Id = g.Key,
                    Amount = g.OrderByDescending(x => x.Entry.PeriodTo)
                              .Select(x => x.Entry.RevenueRecognisedToDate)
                              .First(),
                })
                .ToDictionaryAsync(x => x.Id, x => x.Amount);
        }

        return await query
            .Where(x => x.Entry.ClientBuildContractId != null && ids.Contains(x.Entry.ClientBuildContractId.Value))
            .GroupBy(x => x.Entry.ClientBuildContractId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Amount = g.OrderByDescending(x => x.Entry.PeriodTo)
                          .Select(x => x.Entry.RevenueRecognisedToDate)
                          .First(),
            })
            .ToDictionaryAsync(x => x.Id, x => x.Amount);
    }

    public async Task<PaginatedResponse<RevenueRecognitionRunDto>> GetRecognitionRunsAsync(ListQueryDto query)
    {
        var q = Db.RevenueRecognitionRuns.ForCompany(Tenant)
            .WhereIf(query.ProjectId.HasValue, r => r.ProjectId == query.ProjectId)
            .WhereIf(query.FromDate.HasValue, r => r.PeriodTo >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, r => r.PeriodFrom <= query.ToDate)
            .OrderByDescending(r => r.PeriodTo)
            .ThenByDescending(r => r.CreatedAt);

        return await PageAsync(q, query, async rows =>
        {
            var result = new List<RevenueRecognitionRunDto>();

            foreach (var row in rows) result.Add(await MapRunAsync(row, [], includeEntries: false));

            return result;
        });
    }

    private async Task<RevenueRecognitionRunDto> MapRunAsync(
        RevenueRecognitionRun run, List<RecognitionEntry> entries, bool includeEntries)
    {
        var currency = await CurrencyAsync();
        var projectNames = await ProjectNamesAsync([run.ProjectId]);
        var users = await AgentUserNamesAsync([run.RunByUserId]);

        var dto = new RevenueRecognitionRunDto
        {
            Id = run.Id,
            Reference = run.Reference,
            ProjectId = run.ProjectId,
            ProjectName = run.ProjectId is null ? null : projectNames.GetValueOrDefault(run.ProjectId.Value),
            PeriodFrom = run.PeriodFrom,
            PeriodTo = run.PeriodTo,
            RunDate = run.RunDate,
            IsDryRun = run.IsDryRun,
            ContractCount = run.ContractCount,
            RevenueRecognised = run.RevenueRecognised,
            CostRecognised = run.CostRecognised,
            GrossMargin = run.GrossMargin,
            ContractAssetTotal = run.ContractAssetTotal,
            ContractLiabilityTotal = run.ContractLiabilityTotal,
            CurrencyCode = currency,
            RunByName = run.RunByUserId is null ? null : users.GetValueOrDefault(run.RunByUserId.Value),
            IsPosted = run.IsPosted,
            ErrorSummary = run.ErrorSummary,
        };

        if (!includeEntries) return dto;

        if (entries.Count == 0 && run.Id != Guid.Empty)
        {
            entries = await Db.RecognitionEntries.ForCompany(Tenant)
                .Where(e => e.RevenueRecognitionRunId == run.Id)
                .ToListAsync();
        }

        var bookingIds = entries.Where(e => e.BookingId != null).Select(e => e.BookingId!.Value).Distinct().ToList();
        var contractIds = entries.Where(e => e.ClientBuildContractId != null)
            .Select(e => e.ClientBuildContractId!.Value).Distinct().ToList();

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant)
                .Where(b => bookingIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Reference, b.PrimaryApplicantPartyId })
                .ToListAsync();

        var contracts = contractIds.Count == 0
            ? []
            : await Db.ClientBuildContracts.ForCompany(Tenant)
                .Where(c => contractIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Reference, c.ClientPartyId })
                .ToListAsync();

        var names = await PartyNamesAsync(
            bookings.Select(b => b.PrimaryApplicantPartyId).Concat(contracts.Select(c => c.ClientPartyId)));

        dto.Entries = entries
            .OrderByDescending(e => e.RevenueThisPeriod)
            .Select(e =>
            {
                var booking = e.BookingId is null ? null : bookings.FirstOrDefault(b => b.Id == e.BookingId);
                var contract = e.ClientBuildContractId is null
                    ? null
                    : contracts.FirstOrDefault(c => c.Id == e.ClientBuildContractId);

                return new RecognitionEntryDto
                {
                    Id = e.Id,
                    BookingId = e.BookingId,
                    BookingReference = booking?.Reference,
                    ClientBuildContractId = e.ClientBuildContractId,
                    ContractReference = contract?.Reference,
                    CustomerName = booking is not null
                        ? names.GetValueOrDefault(booking.PrimaryApplicantPartyId)
                        : contract is not null ? names.GetValueOrDefault(contract.ClientPartyId) : null,
                    Basis = e.Basis,
                    PeriodTo = e.PeriodTo,
                    ContractValue = e.ContractValue,
                    CostIncurredToDate = e.CostIncurredToDate,
                    TotalEstimatedCost = e.TotalEstimatedCost,
                    PercentComplete = e.PercentComplete,
                    RevenueRecognisedToDate = e.RevenueRecognisedToDate,
                    RevenueRecognisedPreviously = e.RevenueRecognisedPreviously,
                    RevenueThisPeriod = e.RevenueThisPeriod,
                    CostRecognisedThisPeriod = e.CostRecognisedThisPeriod,
                    AmountsInvoiced = e.AmountsInvoiced,
                    AmountsCollected = e.AmountsCollected,
                    ContractAsset = e.ContractAsset,
                    ContractLiability = e.ContractLiability,
                    IsLossMaking = e.IsLossMaking,
                    ProvisionForLoss = e.ProvisionForLoss,
                };
            }).ToList();

        return dto;
    }

    // ═══ Work in progress ════════════════════════════════════════════════════

    public async Task<PaginatedResponse<WipEntryDto>> GetWipAsync(Guid projectId, ListQueryDto query)
    {
        var q = Db.WipEntries.ForCompany(Tenant)
            .Where(w => w.ProjectId == projectId)
            .WhereIf(query.FromDate.HasValue, w => w.EntryDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, w => w.EntryDate <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), w => w.CostCategory.Contains(query.Search!))
            .OrderByDescending(w => w.EntryDate)
            .ThenByDescending(w => w.CreatedAt);

        return await PageAsync(q, query, async rows =>
        {
            var names = await ProjectNamesAsync(rows.Select(r => (Guid?)r.ProjectId));

            return rows.Select(w => new WipEntryDto
            {
                Id = w.Id,
                ProjectId = w.ProjectId,
                ProjectName = names.GetValueOrDefault(w.ProjectId),
                WbsNodeId = w.WbsNodeId,
                UnitId = w.UnitId,
                EntryDate = w.EntryDate,
                CostCategory = w.CostCategory,
                Description = w.Description ?? w.CostCategory,
                DebitAmount = w.DebitAmount,
                CreditAmount = w.CreditAmount,
                RunningBalance = w.RunningBalance,
                SourceDocumentType = w.SourceDocumentType,
                IsReleasedToCogs = w.IsReleasedToCogs,
            }).ToList();
        });
    }

    // ═══ Cost allocation ═════════════════════════════════════════════════════

    /// <summary>
    /// Spreads the project's incurred cost across its saleable units.
    ///
    /// The basis matters more than it looks. Allocating land cost by area gives every square foot
    /// the same land value, which is right. Allocating it by *value* gives the penthouse a larger
    /// share, which is also defensible and produces a very different margin per unit. So the basis
    /// is configured per cost category rather than assumed, and the result records which one it used
    /// — a cost sheet that cannot say how it was built is not evidence of anything.
    /// </summary>
    public async Task<int> AllocateCostsAsync(Guid projectId, DateOnly asOf, Guid userId)
    {
        var project = await RequireAsync<Project>(projectId, "That project does not exist.");

        var budget = await Db.ProjectBudgetLines.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId)
            .ToListAsync();

        if (budget.Count == 0)
            throw new InvalidOperationException(
                "This project has no budget lines, so there is nothing to allocate. Build the budget first.");

        var rules = await Db.CostAllocationRules.ForCompany(Tenant)
            .Where(r => r.ProjectId == projectId && r.EffectiveFrom <= asOf)
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync();

        var units = await Db.Units.ForCompany(Tenant)
            .Where(u => u.ProjectId == projectId && u.IsSaleable)
            .Include(u => u.Property)
            .ToListAsync();

        if (units.Count == 0)
            throw new InvalidOperationException("This project has no saleable units to allocate cost to.");

        var areas = units.ToDictionary(u => u.Id, u => UnitArea(u));
        var values = units.ToDictionary(u => u.Id, u => u.TotalPrice > 0m ? u.TotalPrice : u.BasePrice);

        var totalArea = areas.Values.Sum();
        var totalValue = values.Values.Sum();

        // Buckets mirror the cost sheet a buyer is eventually shown, so that the allocation and the
        // statement of account can be reconciled line for line.
        var buckets = new Dictionary<Guid, decimal[]>();

        foreach (var unit in units) buckets[unit.Id] = new decimal[8];

        var basisUsed = project.CostAllocationBasis;

        foreach (var line in budget)
        {
            var incurred = line.ActualAmount > 0m ? line.ActualAmount : line.CommittedAmount;
            if (incurred <= 0m) continue;

            var bucket = BucketFor(line.CostHead);

            var rule = rules.FirstOrDefault(r =>
                string.Equals(r.CostCategory, line.CostHead, StringComparison.OrdinalIgnoreCase));

            var basis = rule?.Basis ?? project.CostAllocationBasis;
            basisUsed = basis;

            var eligible = units
                .Where(u => rule?.ProjectNodeId is null || u.ProjectNodeId == rule.ProjectNodeId)
                .Where(u => rule?.AppliesToSubType is null || u.Property?.SubType == rule.AppliesToSubType)
                .ToList();

            if (eligible.Count == 0) continue;

            var eligibleArea = eligible.Sum(u => areas[u.Id]);
            var eligibleValue = eligible.Sum(u => values[u.Id]);

            var running = 0m;

            for (var i = 0; i < eligible.Count; i++)
            {
                var unit = eligible[i];
                var isLast = i == eligible.Count - 1;

                var share = basis switch
                {
                    CostAllocationBasis.ByValue => eligibleValue <= 0m ? 0m : values[unit.Id] / eligibleValue,
                    CostAllocationBasis.ByUnitCount => 1m / eligible.Count,
                    CostAllocationBasis.Direct => 1m / eligible.Count,
                    _ => eligibleArea <= 0m ? 0m : areas[unit.Id] / eligibleArea,
                };

                // The last unit takes the rounding difference, so the allocated total equals the
                // incurred total exactly rather than being a penny or two short across a tower.
                var amount = isLast
                    ? RealEstateMapper.Money(incurred - running)
                    : RealEstateMapper.Money(incurred * share);

                running += amount;
                buckets[unit.Id][bucket] += amount;
            }
        }

        var written = 0;

        foreach (var unit in units)
        {
            var b = buckets[unit.Id];
            var total = RealEstateMapper.Money(b.Sum());
            var area = areas[unit.Id];

            var existing = await Db.UnitCostAllocations.ForCompany(Tenant)
                .FirstOrDefaultAsync(a => a.UnitId == unit.Id && a.AsOfDate == asOf);

            var allocation = existing ?? new UnitCostAllocation
            {
                UnitId = unit.Id,
                ProjectId = projectId,
                AsOfDate = asOf,
            }.StampNew(Tenant, userId);

            allocation.LandCost = b[0];
            allocation.ConstructionCost = b[1];
            allocation.InfrastructureCost = b[2];
            allocation.ApprovalCost = b[3];
            allocation.FinanceCost = b[4];
            allocation.MarketingCost = b[5];
            allocation.CommissionCost = b[6];
            allocation.OverheadCost = b[7];
            allocation.TotalAllocatedCost = total;
            allocation.AreaSqFt = area;
            allocation.CostPerSqFt = area <= 0m ? 0m : RealEstateMapper.Money(total / area);
            allocation.Basis = basisUsed;

            if (existing is null) Db.UnitCostAllocations.Add(allocation);
            else allocation.StampUpdated(userId);

            written++;

            await UpsertProfitabilityAsync(unit, allocation, asOf, userId);
        }

        // The project's totals are checked against the sum of what was just spread, because a
        // difference here means a budget line was skipped and every unit margin is understated.
        var allocatedTotal = buckets.Values.Sum(b => b.Sum());
        var incurredTotal = budget.Sum(l => l.ActualAmount > 0m ? l.ActualAmount : l.CommittedAmount);

        if (Math.Abs(allocatedTotal - incurredTotal) > 1m)
        {
            await WriteAuditNoteAsync(
                nameof(Project), projectId, "cost.allocation.residual", Guid.Empty, userId,
                before: incurredTotal.ToString("N2"),
                after: allocatedTotal.ToString("N2"),
                amountImpact: RealEstateMapper.Money(incurredTotal - allocatedTotal),
                note: "Some budget lines had no eligible units under their allocation rule and were not spread.");
        }

        await Db.SaveChangesAsync();

        return written;
    }

    private static decimal UnitArea(Unit unit)
        => unit.Property?.SaleableAreaSqFt
           ?? unit.Property?.BuiltUpAreaSqFt
           ?? unit.Property?.CoveredAreaSqFt
           ?? unit.Property?.PlotAreaSqFt
           ?? 0m;

    /// <summary>Maps a free-text cost head onto one of the eight buckets a cost sheet shows.</summary>
    private static int BucketFor(string costHead)
    {
        var head = costHead.ToLowerInvariant();

        if (head.Contains("land") || head.Contains("acquisition") || head.Contains("plot")) return 0;
        if (head.Contains("infra") || head.Contains("road") || head.Contains("sewer")
            || head.Contains("utility") || head.Contains("external")) return 2;
        if (head.Contains("approval") || head.Contains("noc") || head.Contains("licence")
            || head.Contains("license") || head.Contains("statutory") || head.Contains("regulator")) return 3;
        if (head.Contains("interest") || head.Contains("finance") || head.Contains("loan")) return 4;
        if (head.Contains("market") || head.Contains("advert") || head.Contains("brand")
            || head.Contains("launch")) return 5;
        if (head.Contains("commission") || head.Contains("brokerage") || head.Contains("channel")) return 6;
        if (head.Contains("overhead") || head.Contains("admin") || head.Contains("salary")
            || head.Contains("office")) return 7;

        return 1;
    }

    private async Task UpsertProfitabilityAsync(Unit unit, UnitCostAllocation allocation, DateOnly asOf, Guid userId)
    {
        var booking = unit.CurrentBookingId is null
            ? null
            : await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == unit.CurrentBookingId);

        var listPrice = unit.TotalPrice > 0m ? unit.TotalPrice : unit.BasePrice;
        var discount = booking?.DiscountAmount ?? 0m;
        var net = booking is not null ? booking.NetSalePrice : listPrice;
        var surcharge = booking?.TotalSurcharge ?? 0m;
        var revenue = RealEstateMapper.Money(net + surcharge);
        var totalCost = allocation.TotalAllocatedCost;
        var margin = RealEstateMapper.Money(revenue - totalCost);
        var area = allocation.AreaSqFt;

        var existing = await Db.UnitProfitabilities.ForCompany(Tenant)
            .FirstOrDefaultAsync(p => p.UnitId == unit.Id && p.AsOfDate == asOf);

        var row = existing ?? new UnitProfitability
        {
            UnitId = unit.Id,
            ProjectId = unit.ProjectId,
            AsOfDate = asOf,
        }.StampNew(Tenant, userId);

        row.BookingId = booking?.Id;
        row.ListPrice = listPrice;
        row.DiscountGiven = discount;
        row.NetRealisation = net;
        row.SurchargeEarned = surcharge;
        row.TotalRevenue = revenue;
        row.AllocatedCost = totalCost;
        row.DirectCost = 0m;
        row.TotalCost = totalCost;
        row.GrossMargin = margin;
        row.MarginPercent = RealEstateMapper.Percent(margin, revenue);
        row.AreaSqFt = area;
        row.RealisationPerSqFt = area <= 0m ? 0m : RealEstateMapper.Money(revenue / area);
        row.CostPerSqFt = area <= 0m ? 0m : RealEstateMapper.Money(totalCost / area);
        row.MarginPerSqFt = area <= 0m ? 0m : RealEstateMapper.Money(margin / area);
        row.SubType = unit.Property?.SubType;
        row.ProjectNodeId = unit.ProjectNodeId;

        if (existing is null) Db.UnitProfitabilities.Add(row);
        else row.StampUpdated(userId);
    }

    public async Task<List<UnitProfitabilityDto>> GetUnitProfitabilityAsync(Guid projectId, ListQueryDto query)
    {
        var latest = await Db.UnitProfitabilities.ForCompany(Tenant)
            .Where(p => p.ProjectId == projectId)
            .Select(p => (DateOnly?)p.AsOfDate)
            .MaxAsync();

        if (latest is null) return [];

        var rows = await Db.UnitProfitabilities.ForCompany(Tenant)
            .Where(p => p.ProjectId == projectId && p.AsOfDate == latest)
            .OrderBy(p => p.MarginPercent)
            .Take(query.PageSize is < 1 or > 2000 ? 500 : query.PageSize)
            .ToListAsync();

        var unit = await AreaUnitAsync();
        var currency = await CurrencyAsync();
        var projectNames = await ProjectNamesAsync([projectId]);
        var labels = await UnitLabelsAsync(rows.Select(r => r.UnitId));

        return rows.Select(p => new UnitProfitabilityDto
        {
            UnitId = p.UnitId,
            UnitNumber = labels.GetValueOrDefault(p.UnitId)?.UnitNumber ?? "—",
            BlockName = labels.GetValueOrDefault(p.UnitId)?.BlockName,
            ProjectId = p.ProjectId,
            ProjectName = projectNames.GetValueOrDefault(p.ProjectId, "—"),
            SubType = p.SubType,
            BookingId = p.BookingId,
            AsOfDate = p.AsOfDate,
            ListPrice = p.ListPrice,
            DiscountGiven = p.DiscountGiven,
            NetRealisation = p.NetRealisation,
            SurchargeEarned = p.SurchargeEarned,
            TotalRevenue = p.TotalRevenue,
            AllocatedCost = p.AllocatedCost,
            DirectCost = p.DirectCost,
            TotalCost = p.TotalCost,
            GrossMargin = p.GrossMargin,
            MarginPercent = p.MarginPercent,
            Area = RealEstateMapper.Area(p.AreaSqFt, unit),
            RealisationPerSqFt = p.RealisationPerSqFt,
            CostPerSqFt = p.CostPerSqFt,
            MarginPerSqFt = p.MarginPerSqFt,
            CurrencyCode = currency,
        }).ToList();
    }

    // ═══ Project profit and loss ═════════════════════════════════════════════

    public async Task<ProjectPnlDto> GetProjectPnlAsync(Guid projectId, DateOnly asOf)
    {
        var project = await RequireAsync<Project>(projectId, "That project does not exist.");
        var currency = await CurrencyAsync();

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId && b.Status != BookingStatus.Cancelled)
            .Select(b => new { b.TotalConsideration, b.NetSalePrice, b.TotalPaid, b.BookingDate, b.Status })
            .ToListAsync();

        var salesValue = RealEstateMapper.Money(
            bookings.Sum(b => b.TotalConsideration > 0m ? b.TotalConsideration : b.NetSalePrice));

        var collected = RealEstateMapper.Money(bookings.Sum(b => b.TotalPaid));

        var recognised = await Db.RecognitionEntries.ForCompany(Tenant)
            .Join(Db.RevenueRecognitionRuns.ForCompany(Tenant),
                e => e.RevenueRecognitionRunId, r => r.Id, (e, r) => new { Entry = e, Run = r })
            .Where(x => !x.Run.IsDryRun && x.Entry.ProjectId == projectId && x.Entry.PeriodTo <= asOf)
            .GroupBy(x => x.Entry.BookingId)
            .Select(g => g.OrderByDescending(x => x.Entry.PeriodTo)
                          .Select(x => new { x.Entry.RevenueRecognisedToDate, x.Entry.CostIncurredToDate })
                          .First())
            .ToListAsync();

        var revenueRecognised = RealEstateMapper.Money(recognised.Sum(r => r.RevenueRecognisedToDate));
        var costRecognised = RealEstateMapper.Money(recognised.Sum(r => r.CostIncurredToDate));

        var budget = await Db.ProjectBudgetLines.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

        var costIncurred = RealEstateMapper.Money(budget.Sum(b => b.ActualAmount));
        var forecastCost = RealEstateMapper.Money(budget.Sum(b => b.ForecastAmount > 0m ? b.ForecastAmount : b.BudgetAmount));

        var wip = await Db.WipEntries.ForCompany(Tenant)
            .Where(w => w.ProjectId == projectId && w.EntryDate <= asOf && !w.IsReleasedToCogs)
            .SumAsync(w => (decimal?)(w.DebitAmount - w.CreditAmount)) ?? 0m;

        var accounts = await Db.ProjectBankAccounts.ForCompany(Tenant)
            .Where(a => a.ProjectId == projectId)
            .Select(a => new { a.Kind, a.Balance })
            .ToListAsync();

        var loans = await Db.ProjectLoans.ForCompany(Tenant)
            .Where(l => l.ProjectId == projectId)
            .SumAsync(l => (decimal?)l.OutstandingAmount) ?? 0m;

        var landowner = await Db.JointVentures.ForCompany(Tenant)
            .Where(v => v.ProjectId == projectId && v.IsActive)
            .SumAsync(v => (decimal?)v.LandownerBalance) ?? 0m;

        var unitCounts = await Db.Units.ForCompany(Tenant)
            .Where(u => u.ProjectId == projectId && u.IsSaleable)
            .GroupBy(u => u.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = unitCounts.Sum(u => u.Count);

        var sold = unitCounts
            .Where(u => u.Status is PropertyStatus.Booked or PropertyStatus.Sold or PropertyStatus.Registered
                                 or PropertyStatus.Possessed)
            .Sum(u => u.Count);

        var available = unitCounts.Where(u => u.Status == PropertyStatus.Available).Sum(u => u.Count);

        var margin = RealEstateMapper.Money(revenueRecognised - costRecognised);

        var progress = (await ProjectProgressAsync([projectId])).GetValueOrDefault(projectId);

        // Twelve months of recognised revenue, so the trend on the screen is a trend rather than a
        // single point somebody has to interpret.
        var trend = await Db.RevenueRecognitionRuns.ForCompany(Tenant)
            .Where(r => r.ProjectId == projectId && !r.IsDryRun && r.PeriodTo <= asOf)
            .OrderByDescending(r => r.PeriodTo)
            .Take(12)
            .Select(r => new { r.PeriodTo, r.RevenueRecognised, r.CostRecognised })
            .ToListAsync();

        return new ProjectPnlDto
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            AsOfDate = asOf,
            CurrencyCode = currency,
            RecognitionBasis = project.RecognitionBasis,
            TotalSalesValue = salesValue,
            RevenueRecognised = revenueRecognised,
            RevenueDeferred = RealEstateMapper.Money(Math.Max(0m, collected - revenueRecognised)),
            CollectionsToDate = collected,
            CostIncurred = costIncurred,
            CostRecognised = costRecognised,
            WipBalance = RealEstateMapper.Money(wip),
            ForecastTotalCost = forecastCost,
            GrossMargin = margin,
            MarginPercent = RealEstateMapper.Percent(margin, revenueRecognised),
            ForecastMargin = RealEstateMapper.Money(salesValue - forecastCost),
            EscrowBalance = RealEstateMapper.Money(
                accounts.Where(a => a.Kind == ProjectAccountKind.Escrow).Sum(a => a.Balance)),
            FreeCashBalance = RealEstateMapper.Money(
                accounts.Where(a => a.Kind != ProjectAccountKind.Escrow).Sum(a => a.Balance)),
            LoanOutstanding = RealEstateMapper.Money(loans),
            LandownerLiability = RealEstateMapper.Money(landowner),
            UnitsTotal = total,
            UnitsSold = sold,
            UnitsAvailable = available,
            AbsorptionPercent = RealEstateMapper.Percent(sold, total),
            PhysicalProgressPercent = progress,
            CostBreakdown = budget.Select(b =>
            {
                var forecast = b.ForecastAmount > 0m ? b.ForecastAmount : b.BudgetAmount;
                var variance = RealEstateMapper.Money(b.BudgetAmount - forecast);

                return new ProjectBudgetLineDto
                {
                    Id = b.Id,
                    CostHead = b.CostHead,
                    Description = b.Description,
                    BudgetAmount = b.BudgetAmount,
                    CommittedAmount = b.CommittedAmount,
                    ActualAmount = b.ActualAmount,
                    ForecastAmount = forecast,
                    VarianceAmount = variance,
                    VariancePercent = RealEstateMapper.Percent(variance, b.BudgetAmount),
                    SortOrder = b.SortOrder,
                };
            }).ToList(),
            RevenueTrend = trend.OrderBy(t => t.PeriodTo).Select(t => new TrendPointDto
            {
                Label = t.PeriodTo.ToString("MMM yy"),
                Date = t.PeriodTo,
                Value = t.RevenueRecognised,
                SecondaryValue = t.CostRecognised,
            }).ToList(),
        };
    }

    // ═══ Tax ═════════════════════════════════════════════════════════════════

    public async Task<List<TaxProfileDto>> GetTaxProfilesAsync(Guid? projectId)
    {
        var profiles = await Db.TaxProfiles.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, p => p.ProjectId == projectId || p.ProjectId == null)
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync();

        var names = await ProjectNamesAsync(profiles.Select(p => p.ProjectId));

        return profiles.Select(p => new TaxProfileDto
        {
            Id = p.Id,
            Name = p.Name,
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectId is null ? null : names.GetValueOrDefault(p.ProjectId.Value),
            SalesTaxPercent = p.SalesTaxPercent,
            RentTaxPercent = p.RentTaxPercent,
            ServiceTaxPercent = p.ServiceTaxPercent,
            StampDutyPercent = p.StampDutyPercent,
            RegistrationFeePercent = p.RegistrationFeePercent,
            WithholdingOnCommissionPercent = p.WithholdingOnCommissionPercent,
            WithholdingOnRentPercent = p.WithholdingOnRentPercent,
            WithholdingOnContractorPercent = p.WithholdingOnContractorPercent,
            WithholdingOnPropertySalePercent = p.WithholdingOnPropertySalePercent,
            NonFilerUpliftPercent = p.NonFilerUpliftPercent,
            EffectiveFrom = p.EffectiveFrom,
            EffectiveTo = p.EffectiveTo,
            IsActive = p.IsActive,
        }).ToList();
    }

    public async Task<TaxProfileDto> SaveTaxProfileAsync(TaxProfileDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var profile = isNew
            ? new TaxProfile()
            : await Db.TaxProfiles.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
              ?? throw new InvalidOperationException("That tax profile does not exist.");

        if (dto.EffectiveTo is not null && dto.EffectiveTo < dto.EffectiveFrom)
            throw new InvalidOperationException("The profile expires before it takes effect.");

        // Rates that have already been applied to a computation cannot be edited, because the
        // computation is what a tax authority will look at and it has to match the rate on file.
        if (!isNew)
        {
            var used = await Db.TaxComputations.ForCompany(Tenant)
                .AnyAsync(c => c.TaxProfileId == profile.Id && c.IsPaid);

            var ratesChanged = profile.SalesTaxPercent != dto.SalesTaxPercent
                || profile.RentTaxPercent != dto.RentTaxPercent
                || profile.StampDutyPercent != dto.StampDutyPercent
                || profile.WithholdingOnPropertySalePercent != dto.WithholdingOnPropertySalePercent;

            if (used && ratesChanged)
                throw new InvalidOperationException(
                    "Tax has already been paid under this profile. Close it with an end date and create a new "
                    + "profile for the revised rates rather than editing this one.");
        }

        profile.Name = dto.Name;
        profile.ProjectId = dto.ProjectId;
        profile.SalesTaxPercent = dto.SalesTaxPercent;
        profile.RentTaxPercent = dto.RentTaxPercent;
        profile.ServiceTaxPercent = dto.ServiceTaxPercent;
        profile.StampDutyPercent = dto.StampDutyPercent;
        profile.RegistrationFeePercent = dto.RegistrationFeePercent;
        profile.WithholdingOnCommissionPercent = dto.WithholdingOnCommissionPercent;
        profile.WithholdingOnRentPercent = dto.WithholdingOnRentPercent;
        profile.WithholdingOnContractorPercent = dto.WithholdingOnContractorPercent;
        profile.WithholdingOnPropertySalePercent = dto.WithholdingOnPropertySalePercent;
        profile.NonFilerUpliftPercent = dto.NonFilerUpliftPercent;
        profile.EffectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;
        profile.EffectiveTo = dto.EffectiveTo;
        profile.IsActive = dto.IsActive;

        if (isNew)
        {
            profile.StampNew(Tenant, userId);
            Db.TaxProfiles.Add(profile);
        }
        else
        {
            profile.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        var names = await ProjectNamesAsync([profile.ProjectId]);

        return new TaxProfileDto
        {
            Id = profile.Id,
            Name = profile.Name,
            ProjectId = profile.ProjectId,
            ProjectName = profile.ProjectId is null ? null : names.GetValueOrDefault(profile.ProjectId.Value),
            SalesTaxPercent = profile.SalesTaxPercent,
            RentTaxPercent = profile.RentTaxPercent,
            ServiceTaxPercent = profile.ServiceTaxPercent,
            StampDutyPercent = profile.StampDutyPercent,
            RegistrationFeePercent = profile.RegistrationFeePercent,
            WithholdingOnCommissionPercent = profile.WithholdingOnCommissionPercent,
            WithholdingOnRentPercent = profile.WithholdingOnRentPercent,
            WithholdingOnContractorPercent = profile.WithholdingOnContractorPercent,
            WithholdingOnPropertySalePercent = profile.WithholdingOnPropertySalePercent,
            NonFilerUpliftPercent = profile.NonFilerUpliftPercent,
            EffectiveFrom = profile.EffectiveFrom,
            EffectiveTo = profile.EffectiveTo,
            IsActive = profile.IsActive,
        };
    }

    public async Task<PaginatedResponse<WithholdingRecordDto>> GetWithholdingAsync(
        ListQueryDto query, WithholdingKind? kind, bool? undepositedOnly)
    {
        var q = Db.WithholdingRecords.ForCompany(Tenant)
            .WhereIf(kind.HasValue, w => w.Kind == kind)
            .WhereIf(undepositedOnly == true, w => !w.IsDeposited)
            .WhereIf(query.FromDate.HasValue, w => w.DeductedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, w => w.DeductedOn <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                w => w.Reference.Contains(query.Search!) || (w.TaxNumber ?? "").Contains(query.Search!))
            .OrderByDescending(w => w.DeductedOn);

        return await PageAsync(q, query, async rows =>
        {
            var currency = await CurrencyAsync();
            var names = await PartyNamesAsync(rows.Select(r => r.PartyId));

            return rows.Select(w => new WithholdingRecordDto
            {
                Id = w.Id,
                Reference = w.Reference,
                Kind = w.Kind,
                PartyId = w.PartyId,
                PartyName = names.GetValueOrDefault(w.PartyId, "—"),
                TaxNumber = w.TaxNumber,
                IsFiler = w.IsFiler,
                DeductedOn = w.DeductedOn,
                GrossAmount = w.GrossAmount,
                Rate = w.Rate,
                WithheldAmount = w.WithheldAmount,
                CurrencyCode = currency,
                IsDeposited = w.IsDeposited,
                DepositedOn = w.DepositedOn,
                ChallanNumber = w.ChallanNumber,
                CertificateIssued = w.CertificateIssued,
                CertificateUrl = w.CertificateUrl,
                ReturnPeriod = w.ReturnPeriod,
            }).ToList();
        });
    }
}
