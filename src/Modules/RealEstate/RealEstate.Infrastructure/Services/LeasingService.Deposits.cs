using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Referencing, deposits, inventories, notices, renewals and compliance certificates.
///
/// The deposit is the piece with real legal weight. In several markets a deposit not protected
/// within a statutory window costs the landlord up to three times its value and blocks a
/// possession notice outright, so the deadline is a countdown on the screen, releasing without
/// protection is refused, and every deduction has to carry its evidence before it can be proposed.
/// </summary>
public partial class LeasingService
{
    // ═══ Referencing ═════════════════════════════════════════════════════════

    public async Task<ReferencingCaseDto> SaveReferencingAsync(ReferencingCaseDto dto, Guid userId)
    {
        var referencing = dto.Id != Guid.Empty
            ? await Db.ReferencingCases.ForCompany(Tenant).Include(r => r.Checks).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        if (referencing is null)
        {
            referencing = new ReferencingCase
            {
                Reference = await numbering.NextMasterCodeAsync(Db.ReferencingCases, "REF"),
                PartyId = dto.PartyId,
                TenancyId = dto.TenancyId,
                PropertyId = dto.PropertyId,
                StartedOn = dto.StartedOn == default ? Today : dto.StartedOn,
                AssignedToUserId = userId,
            }.StampNew(Tenant, userId);

            Db.ReferencingCases.Add(referencing);

            // Every case starts with the standard set. The check nobody remembers to add is the
            // one that turns out to matter.
            var checks = dto.Checks.Count > 0
                ? dto.Checks.Select(c => c.Kind).ToList()
                : StandardChecks.ToList();

            foreach (var kind in checks)
            {
                referencing.Checks.Add(new ReferencingCheck
                {
                    Kind = kind,
                    Outcome = ReferencingOutcome.Pending,
                    RequestedOn = Today,
                    IsMandatory = kind is ReferencingCheckKind.Identity
                                       or ReferencingCheckKind.RightToRent
                                       or ReferencingCheckKind.Credit,
                }.StampNew(Tenant, userId));
            }
        }
        else referencing.StampUpdated(userId);

        referencing.DeclaredIncome = dto.DeclaredIncome;
        referencing.GuarantorRequired = dto.GuarantorRequired;
        referencing.Conditions = dto.Conditions;

        // The income multiple is the affordability test the whole decision turns on, so it is
        // derived from the rent rather than typed by whoever is filling the form in.
        if (dto.DeclaredIncome is > 0m && referencing.TenancyId is not null)
        {
            var annualRent = await Db.Tenancies.ForCompany(Tenant)
                .Where(t => t.Id == referencing.TenancyId)
                .Select(t => t.AnnualRent ?? 0m)
                .FirstOrDefaultAsync();

            if (annualRent > 0m)
                referencing.IncomeMultiple = RealEstateMapper.Money(dto.DeclaredIncome.Value / annualRent, 2);
        }

        foreach (var c in dto.Checks.Where(c => c.Id is not null && c.Id != Guid.Empty))
        {
            var check = referencing.Checks.FirstOrDefault(x => x.Id == c.Id);
            if (check is null) continue;

            check.Outcome = c.Outcome;
            check.CompletedOn = c.Outcome == ReferencingOutcome.Pending ? null : c.CompletedOn ?? Today;
            check.RefereeName = c.RefereeName;
            check.RefereeContact = c.RefereeContact;
            check.Findings = c.Findings;
            check.DocumentUrl = c.DocumentUrl;
            check.RecheckDue = c.RecheckDue;
            check.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return await MapReferencingAsync(referencing);
    }

    private static readonly ReferencingCheckKind[] StandardChecks =
    [
        ReferencingCheckKind.Identity,
        ReferencingCheckKind.RightToRent,
        ReferencingCheckKind.Credit,
        ReferencingCheckKind.Employment,
        ReferencingCheckKind.PreviousLandlord,
        ReferencingCheckKind.Income,
    ];

    public async Task<ReferencingCaseDto> DecideReferencingAsync(
        Guid id, ReferencingOutcome outcome, string? conditions, string? failureReason, Guid userId)
    {
        var referencing = await Db.ReferencingCases.ForCompany(Tenant)
            .Include(r => r.Checks)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("That referencing case does not exist.");

        if (outcome == ReferencingOutcome.Pass)
        {
            var open = referencing.Checks
                .Where(c => c.IsMandatory && c.Outcome == ReferencingOutcome.Pending)
                .Select(c => c.Kind.ToString())
                .ToList();

            if (open.Count > 0)
                throw new InvalidOperationException($"These checks are still outstanding: {string.Join(", ", open)}.");

            var failed = referencing.Checks
                .Where(c => c.IsMandatory && c.Outcome == ReferencingOutcome.Fail)
                .Select(c => c.Kind.ToString())
                .ToList();

            if (failed.Count > 0)
                throw new InvalidOperationException($"These checks failed: {string.Join(", ", failed)}. Pass with conditions, or refuse.");
        }

        if (outcome == ReferencingOutcome.PassWithConditions && string.IsNullOrWhiteSpace(conditions))
            throw new InvalidOperationException("A conditional pass has to say what the conditions are.");

        if (outcome == ReferencingOutcome.Fail && string.IsNullOrWhiteSpace(failureReason))
            throw new InvalidOperationException("A refusal has to say why. The applicant is entitled to know.");

        referencing.Outcome = outcome;
        referencing.CompletedOn = Today;
        referencing.Conditions = conditions ?? referencing.Conditions;
        referencing.FailureReason = failureReason;
        referencing.StampUpdated(userId);

        // The outcome lands on the tenancy party, so a tenancy screen shows it without a lookup.
        if (referencing.TenancyId is not null)
        {
            var party = await Db.TenancyParties.ForCompany(Tenant)
                .FirstOrDefaultAsync(p => p.TenancyId == referencing.TenancyId && p.PartyId == referencing.PartyId);

            if (party is not null)
            {
                party.ReferencingOutcome = outcome;
                party.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();
        return await MapReferencingAsync(referencing);
    }

    private async Task<ReferencingCaseDto> MapReferencingAsync(ReferencingCase r)
    {
        var today = Today;
        var names = await PartyNamesAsync([r.PartyId]);
        var assignees = await AgentUserNamesAsync([r.AssignedToUserId]);

        var address = r.PropertyId is null
            ? null
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => p.Id == r.PropertyId)
                .Select(p => p.AddressLine1)
                .FirstOrDefaultAsync();

        return new ReferencingCaseDto
        {
            Id = r.Id,
            Reference = r.Reference,
            PartyId = r.PartyId,
            PartyName = names.GetValueOrDefault(r.PartyId, "—"),
            TenancyId = r.TenancyId,
            PropertyId = r.PropertyId,
            AddressOneLine = address,
            Outcome = r.Outcome,
            StartedOn = r.StartedOn,
            CompletedOn = r.CompletedOn,
            AssignedToName = r.AssignedToUserId is null ? null : assignees.GetValueOrDefault(r.AssignedToUserId.Value),
            DeclaredIncome = r.DeclaredIncome,
            IncomeMultiple = r.IncomeMultiple,
            GuarantorRequired = r.GuarantorRequired,
            Conditions = r.Conditions,
            FailureReason = r.FailureReason,
            DaysOpen = (r.CompletedOn ?? today).DayNumber - r.StartedOn.DayNumber,

            Checks = r.Checks.OrderBy(c => c.Kind).Select(c => new ReferencingCheckDto
            {
                Id = c.Id,
                Kind = c.Kind,
                Outcome = c.Outcome,
                RequestedOn = c.RequestedOn,
                CompletedOn = c.CompletedOn,
                RefereeName = c.RefereeName,
                RefereeContact = c.RefereeContact,
                Findings = c.Findings,
                DocumentUrl = c.DocumentUrl,
                IsMandatory = c.IsMandatory,
                RecheckDue = c.RecheckDue,
            }).ToList(),
        };
    }

    // ═══ Deposits ════════════════════════════════════════════════════════════

    public async Task<SecurityDepositDto> SaveDepositAsync(SecurityDepositDto dto, Guid userId)
    {
        var settings = await SettingsAsync();

        var deposit = dto.Id != Guid.Empty
            ? await Db.SecurityDeposits.ForCompany(Tenant).Include(d => d.Deductions).FirstOrDefaultAsync(d => d.Id == dto.Id)
            : null;

        if (deposit is null)
        {
            var tenancy = await RequireAsync<Tenancy>(dto.TenancyId, "That tenancy does not exist.");

            deposit = new SecurityDeposit
            {
                TenancyId = dto.TenancyId,
                PropertyId = tenancy.PropertyId,
                PartyId = dto.PartyId,
                CurrencyCode = tenancy.CurrencyCode,
                ReceivedOn = dto.ReceivedOn == default ? Today : dto.ReceivedOn,
            }.StampNew(Tenant, userId);

            Db.SecurityDeposits.Add(deposit);

            tenancy.SecurityDepositId = deposit.Id;
            tenancy.DepositAmount = dto.Amount;
            tenancy.StampUpdated(userId);
        }
        else
        {
            if (deposit.ReleasedOn is not null)
                throw new InvalidOperationException("This deposit has been released and cannot be edited.");

            deposit.StampUpdated(userId);
        }

        deposit.Amount = dto.Amount;
        deposit.Scheme = dto.Scheme;

        // Money held in a scheme has a statutory clock. Money held in a client account does not,
        // so the deadline is set by the scheme choice rather than always.
        deposit.RegistrationDeadline = dto.Scheme == DepositScheme.HeldInClientAccount
            ? null
            : deposit.ReceivedOn.AddDays(settings.DepositRegistrationDays);

        await Db.SaveChangesAsync();
        return await MapDepositAsync(deposit);
    }

    public async Task<SecurityDepositDto> RegisterDepositAsync(
        Guid depositId, string schemeName, string reference, DateOnly registeredOn, Guid userId)
    {
        var deposit = await Db.SecurityDeposits.ForCompany(Tenant)
            .Include(d => d.Deductions)
            .FirstOrDefaultAsync(d => d.Id == depositId)
            ?? throw new InvalidOperationException("That deposit does not exist.");

        if (string.IsNullOrWhiteSpace(reference))
            throw new InvalidOperationException("Enter the scheme's own reference. Without it the protection cannot be proved.");

        deposit.SchemeName = schemeName;
        deposit.RegistrationReference = reference;
        deposit.RegisteredOn = registeredOn;
        deposit.IsRegistered = true;
        deposit.StampUpdated(userId);

        // Late protection still has to be recorded — it is a fact of the file and it changes what
        // the landlord may lawfully do next.
        if (deposit.RegistrationDeadline is not null && registeredOn > deposit.RegistrationDeadline)
        {
            await WriteAuditNoteAsync(
                "SecurityDeposit", depositId, "DepositProtectedLate", Guid.Empty, userId,
                amountImpact: deposit.Amount,
                note: $"Protected on {registeredOn:dd MMM yyyy}, {registeredOn.DayNumber - deposit.RegistrationDeadline.Value.DayNumber} days after the deadline.",
                highRisk: true);
        }

        await Db.SaveChangesAsync();
        return await MapDepositAsync(deposit);
    }

    /// <summary>
    /// Proposes deductions against the deposit. Each one has to carry evidence — a check-out
    /// finding, a work order or a document — because an adjudicator will not accept an assertion
    /// and neither will the tenant.
    /// </summary>
    public async Task<SecurityDepositDto> ProposeDeductionsAsync(
        Guid depositId, List<DepositDeductionDto> deductions, Guid userId)
    {
        var deposit = await Db.SecurityDeposits.ForCompany(Tenant)
            .Include(d => d.Deductions)
            .FirstOrDefaultAsync(d => d.Id == depositId)
            ?? throw new InvalidOperationException("That deposit does not exist.");

        if (deposit.ReleasedOn is not null)
            throw new InvalidOperationException("This deposit has already been released.");

        var unevidenced = deductions
            .Where(d => d.ProposedAmount > 0m
                     && string.IsNullOrWhiteSpace(d.EvidenceUrl)
                     && d.InspectionFindingId is null
                     && d.WorkOrderId is null)
            .Select(d => d.Category)
            .ToList();

        if (unevidenced.Count > 0)
            throw new InvalidOperationException($"These deductions have no evidence attached: {string.Join(", ", unevidenced)}.");

        var total = deductions.Sum(d => d.ProposedAmount);

        if (total > deposit.Amount)
            throw new InvalidOperationException($"The deductions total {total:N0}, which is more than the {deposit.Amount:N0} deposit. Claim the excess separately.");

        Db.DepositDeductions.RemoveRange(deposit.Deductions);

        foreach (var d in deductions)
        {
            deposit.Deductions.Add(new DepositDeduction
            {
                Category = d.Category,
                Description = d.Description,
                ProposedAmount = d.ProposedAmount,
                AgreedAmount = d.AgreedAmount,
                EvidenceUrl = d.EvidenceUrl,
                InspectionFindingId = d.InspectionFindingId,
                WorkOrderId = d.WorkOrderId,
                TenantResponse = d.TenantResponse,
                RespondedOn = d.RespondedOn,
                AdjudicatorNote = d.AdjudicatorNote,
            }.StampNew(Tenant, userId));
        }

        deposit.DeductionTotal = RealEstateMapper.Money(total);
        deposit.StampUpdated(userId);

        await Db.SaveChangesAsync();

        await QueueNotificationAsync(
            "DepositDeductionsProposed",
            "Proposed deductions from your deposit",
            $"{total:N0} of your {deposit.Amount:N0} deposit is proposed as deductions. You can accept or dispute each item.",
            $"/realestate/deposits/{deposit.Id}",
            recipientPartyId: deposit.PartyId,
            entityType: "SecurityDeposit",
            entityId: deposit.Id,
            severity: AlertSeverity.Warning);

        await Db.SaveChangesAsync();
        return await MapDepositAsync(deposit);
    }

    public async Task<SecurityDepositDto> ReleaseDepositAsync(
        Guid depositId, decimal toTenant, decimal toLandlord, Guid userId)
    {
        var deposit = await Db.SecurityDeposits.ForCompany(Tenant)
            .Include(d => d.Deductions)
            .FirstOrDefaultAsync(d => d.Id == depositId)
            ?? throw new InvalidOperationException("That deposit does not exist.");

        if (deposit.ReleasedOn is not null)
            throw new InvalidOperationException($"This deposit was released on {deposit.ReleasedOn:dd MMM yyyy}.");

        var total = RealEstateMapper.Money(toTenant + toLandlord);

        if (Math.Abs(total - deposit.Amount) > 0.01m)
            throw new InvalidOperationException($"The split has to add up to the deposit: {toTenant:N0} + {toLandlord:N0} is {total:N0}, not {deposit.Amount:N0}.");

        if (deposit.IsDisputed)
            throw new InvalidOperationException("This deposit is in dispute. It cannot be released until the adjudication concludes.");

        var contested = deposit.Deductions.Any(d => d.TenantResponse == "Disputed");

        if (contested && toLandlord > 0m)
            throw new InvalidOperationException("The tenant disputes at least one deduction. Refer it to the scheme rather than releasing the disputed amount.");

        deposit.ReturnedToTenant = toTenant;
        deposit.PaidToLandlord = toLandlord;
        deposit.ReleasedOn = Today;
        deposit.StampUpdated(userId);

        await WriteAuditNoteAsync(
            "SecurityDeposit", depositId, "DepositReleased", Guid.Empty, userId,
            amountImpact: deposit.Amount,
            note: $"{toTenant:N0} to the tenant, {toLandlord:N0} to the landlord.",
            highRisk: true);

        await QueueNotificationAsync(
            "DepositReleased",
            "Your deposit has been released",
            toLandlord > 0m
                ? $"{toTenant:N0} is being returned to you; {toLandlord:N0} was retained against agreed deductions."
                : $"Your full deposit of {toTenant:N0} is being returned.",
            $"/realestate/deposits/{deposit.Id}",
            recipientPartyId: deposit.PartyId,
            entityType: "SecurityDeposit",
            entityId: deposit.Id);

        await Db.SaveChangesAsync();
        return await MapDepositAsync(deposit);
    }

    public async Task<PaginatedResponse<SecurityDepositDto>> GetDepositsAsync(ListQueryDto query, bool unregisteredOnly)
    {
        var q = Db.SecurityDeposits.ForCompany(Tenant)
            .Include(d => d.Deductions)
            .WhereIf(unregisteredOnly, d => !d.IsRegistered && d.RegistrationDeadline != null)
            .WhereIf(query.FromDate.HasValue, d => d.ReceivedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, d => d.ReceivedOn <= query.ToDate)
            .OrderBy(d => d.IsRegistered).ThenBy(d => d.RegistrationDeadline);

        return await PageAsync(q, query, async deposits =>
        {
            var result = new List<SecurityDepositDto>();
            foreach (var d in deposits) result.Add(await MapDepositAsync(d));
            return result;
        });
    }

    private async Task<SecurityDepositDto> MapDepositAsync(SecurityDeposit d)
    {
        var today = Today;
        var names = await PartyNamesAsync([d.PartyId]);

        var tenancy = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => t.Id == d.TenancyId)
            .Select(t => new { t.Reference, t.PropertyId })
            .FirstOrDefaultAsync();

        var address = await Db.Properties.ForCompany(Tenant)
            .Where(p => p.Id == d.PropertyId)
            .Select(p => p.AddressLine1)
            .FirstOrDefaultAsync();

        return new SecurityDepositDto
        {
            Id = d.Id,
            TenancyId = d.TenancyId,
            TenancyReference = tenancy?.Reference ?? "—",
            AddressOneLine = address,
            PartyId = d.PartyId,
            PartyName = names.GetValueOrDefault(d.PartyId, "—"),
            Amount = d.Amount,
            CurrencyCode = d.CurrencyCode,
            ReceivedOn = d.ReceivedOn,
            Scheme = d.Scheme,
            SchemeName = d.SchemeName,
            RegistrationReference = d.RegistrationReference,
            RegistrationDeadline = d.RegistrationDeadline,
            RegisteredOn = d.RegisteredOn,
            IsRegistered = d.IsRegistered,
            DaysToRegistrationDeadline = d.RegistrationDeadline is null
                ? null
                : d.RegistrationDeadline.Value.DayNumber - today.DayNumber,
            RegistrationOverdue = !d.IsRegistered && d.RegistrationDeadline is not null && d.RegistrationDeadline < today,
            PrescribedInformationServed = d.PrescribedInformationServed,
            PrescribedInformationServedOn = d.PrescribedInformationServedOn,
            DeductionTotal = d.DeductionTotal,
            ReturnedToTenant = d.ReturnedToTenant,
            PaidToLandlord = d.PaidToLandlord,
            ReleasedOn = d.ReleasedOn,
            IsDisputed = d.IsDisputed,
            DisputeReference = d.DisputeReference,
            DisputeOutcome = d.DisputeOutcome,

            Deductions = d.Deductions.Select(x => new DepositDeductionDto
            {
                Id = x.Id,
                Category = x.Category,
                Description = x.Description ?? string.Empty,
                ProposedAmount = x.ProposedAmount,
                AgreedAmount = x.AgreedAmount,
                EvidenceUrl = x.EvidenceUrl,
                InspectionFindingId = x.InspectionFindingId,
                WorkOrderId = x.WorkOrderId,
                TenantResponse = x.TenantResponse,
                RespondedOn = x.RespondedOn,
                AdjudicatorNote = x.AdjudicatorNote,
            }).ToList(),
        };
    }

    // ═══ Inventories & check-outs ════════════════════════════════════════════

    /// <summary>
    /// The inventory. A check-out compares itself line by line to the check-in and flags what has
    /// deteriorated — which is the only defensible basis for a deduction, and the thing an agent
    /// doing it by eye gets wrong in the landlord's favour.
    /// </summary>
    public async Task<MoveInspectionDto> SaveInspectionAsync(MoveInspectionDto dto, Guid userId)
    {
        var inspection = dto.Id != Guid.Empty
            ? await Db.MoveInspections.ForCompany(Tenant).Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == dto.Id)
            : null;

        if (inspection is null)
        {
            inspection = new MoveInspection
            {
                Reference = await numbering.NextMasterCodeAsync(Db.MoveInspections, "INV"),
                PropertyId = dto.PropertyId,
                TenancyId = dto.TenancyId,
            }.StampNew(Tenant, userId);

            Db.MoveInspections.Add(inspection);
        }
        else
        {
            if (inspection.IsFinalised)
                throw new InvalidOperationException("This inspection is signed off and cannot be edited.");

            inspection.StampUpdated(userId);
        }

        inspection.Kind = dto.Kind;
        inspection.InspectedAt = dto.InspectedAt == default ? DateTime.UtcNow : dto.InspectedAt;
        inspection.InspectorUserId = userId;
        inspection.TenantPresent = dto.TenantPresent;
        inspection.LandlordPresent = dto.LandlordPresent;
        inspection.OverallCondition = dto.OverallCondition;
        inspection.CleanlinessRating = dto.CleanlinessRating;
        inspection.ReportUrl = dto.ReportUrl;
        inspection.TenantComments = dto.TenantComments;
        inspection.TenantDisputed = dto.TenantDisputed;
        inspection.IsFinalised = dto.IsFinalised;

        // A check-out is only meaningful against the check-in it is compared to, so the link is
        // resolved here rather than left to whoever fills the form in.
        if (dto.Kind == InspectionKind.MoveOut && inspection.ComparedToInspectionId is null && dto.TenancyId is not null)
        {
            inspection.ComparedToInspectionId = await Db.MoveInspections.ForCompany(Tenant)
                .Where(i => i.TenancyId == dto.TenancyId && i.Kind == InspectionKind.MoveIn)
                .OrderBy(i => i.InspectedAt)
                .Select(i => (Guid?)i.Id)
                .FirstOrDefaultAsync();
        }

        var baseline = inspection.ComparedToInspectionId is null
            ? []
            : await Db.InspectionRoomItems.ForCompany(Tenant)
                .Where(i => i.MoveInspectionId == inspection.ComparedToInspectionId)
                .ToListAsync();

        if (dto.Items.Count > 0)
        {
            Db.InspectionRoomItems.RemoveRange(inspection.Items);

            var order = 0;

            foreach (var item in dto.Items)
            {
                var previous = baseline.FirstOrDefault(b =>
                    b.RoomName.Equals(item.RoomName, StringComparison.OrdinalIgnoreCase)
                    && b.ItemName.Equals(item.ItemName, StringComparison.OrdinalIgnoreCase));

                // The condition grades run best to worst, so a higher number is a worse state.
                var deteriorated = previous is not null && (int)item.Condition > (int)previous.Condition;

                inspection.Items.Add(new InspectionRoomItem
                {
                    RoomName = item.RoomName,
                    ItemName = item.ItemName,
                    Condition = item.Condition,
                    CleanlinessGrade = item.CleanlinessGrade,
                    Note = item.Note,
                    PhotoUrls = item.PhotoUrls.Count == 0 ? null : string.Join('\n', item.PhotoUrls),
                    PreviousCondition = previous?.Condition,
                    HasDeteriorated = deteriorated,

                    // One grade's drop over a normal tenancy is wear, not damage. Claiming for it
                    // is the single most common reason a deposit adjudication goes against a
                    // landlord, so it is flagged rather than left to the operator's optimism.
                    IsFairWearAndTear = item.IsFairWearAndTear
                        || (deteriorated && previous is not null && (int)item.Condition - (int)previous.Condition == 1),

                    EstimatedCost = item.EstimatedCost,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await MapInspectionsAsync([inspection]))[0];
    }

    public async Task<MoveInspectionDto?> GetInspectionAsync(Guid id)
    {
        var inspection = await Db.MoveInspections.ForCompany(Tenant)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inspection is null) return null;
        return (await MapInspectionsAsync([inspection]))[0];
    }

    public async Task<PaginatedResponse<MoveInspectionDto>> GetInspectionsAsync(ListQueryDto query, InspectionKind? kind)
    {
        var q = Db.MoveInspections.ForCompany(Tenant)
            .Include(i => i.Items)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), i => i.Reference.Contains(query.Search!))
            .WhereIf(kind.HasValue, i => i.Kind == kind)
            .OrderByDescending(i => i.InspectedAt);

        return await PageAsync(q, query, MapInspectionsAsync);
    }

    private async Task<List<MoveInspectionDto>> MapInspectionsAsync(List<MoveInspection> inspections)
    {
        if (inspections.Count == 0) return [];

        var propertyIds = inspections.Select(i => i.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var inspectors = await AgentUserNamesAsync(inspections.Select(i => i.InspectorUserId));

        return inspections.Select(i =>
        {
            properties.TryGetValue(i.PropertyId, out var property);

            var deteriorated = i.Items.Where(x => x.HasDeteriorated && !x.IsFairWearAndTear).ToList();

            return new MoveInspectionDto
            {
                Id = i.Id,
                Reference = i.Reference,
                PropertyId = i.PropertyId,
                AddressOneLine = property is null ? null : RealEstateMapper.OneLineAddress(property),
                TenancyId = i.TenancyId,
                Kind = i.Kind,
                InspectedAt = i.InspectedAt,
                InspectorName = i.InspectorUserId is null ? null : inspectors.GetValueOrDefault(i.InspectorUserId.Value),
                TenantPresent = i.TenantPresent,
                LandlordPresent = i.LandlordPresent,
                OverallCondition = i.OverallCondition,
                CleanlinessRating = i.CleanlinessRating,
                ReportUrl = i.ReportUrl,
                ComparedToInspectionId = i.ComparedToInspectionId,
                TenantDisputed = i.TenantDisputed,
                TenantComments = i.TenantComments,
                TenantResponseDeadline = i.TenantResponseDeadline,
                IsFinalised = i.IsFinalised,
                ItemCount = i.Items.Count,
                DeterioratedCount = deteriorated.Count,

                // Fair wear and tear is excluded from the headline figure, because that is what a
                // landlord will actually be able to recover.
                EstimatedDeductions = deteriorated.Count == 0
                    ? null
                    : RealEstateMapper.Money(deteriorated.Sum(x => x.EstimatedCost ?? 0m)),

                Items = i.Items.OrderBy(x => x.SortOrder).Select(x => new InspectionRoomItemDto
                {
                    Id = x.Id,
                    RoomName = x.RoomName,
                    ItemName = x.ItemName,
                    Condition = x.Condition,
                    CleanlinessGrade = x.CleanlinessGrade,
                    Note = x.Note,
                    PhotoUrls = string.IsNullOrWhiteSpace(x.PhotoUrls)
                        ? []
                        : x.PhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    PreviousCondition = x.PreviousCondition,
                    HasDeteriorated = x.HasDeteriorated,
                    IsFairWearAndTear = x.IsFairWearAndTear,
                    EstimatedCost = x.EstimatedCost,
                    SortOrder = x.SortOrder,
                }).ToList(),
            };
        }).ToList();
    }

    // ═══ Notices, renewals and reviews ═══════════════════════════════════════

    /// <summary>
    /// Serves a notice. The validity check is the whole value here: a notice served a day short
    /// of the required period, or while the deposit is unprotected, is void — and the landlord
    /// discovers that months later in a courtroom rather than now.
    /// </summary>
    public async Task<TenancyNoticeDto> ServeNoticeAsync(TenancyNoticeDto dto, Guid userId)
    {
        var tenancy = await RequireAsync<Tenancy>(dto.TenancyId, "That tenancy does not exist.");

        var servedOn = dto.ServedOn == default ? Today : dto.ServedOn;
        var required = dto.ServedBy == "Tenant" ? tenancy.NoticePeriodDaysTenant : tenancy.NoticePeriodDaysLandlord;
        var given = dto.EffectiveFrom.DayNumber - servedOn.DayNumber;

        var problems = new List<string>();

        if (given < required)
            problems.Add($"only {given} days' notice is given where {required} are required");

        if (dto.ServedBy == "Landlord")
        {
            var deposit = await Db.SecurityDeposits.ForCompany(Tenant)
                .FirstOrDefaultAsync(d => d.TenancyId == dto.TenancyId);

            if (deposit is not null && deposit.RegistrationDeadline is not null && !deposit.IsRegistered)
                problems.Add("the deposit is not protected, which invalidates a possession notice in many jurisdictions");

            if (deposit is not null && deposit.IsRegistered && !deposit.PrescribedInformationServed)
                problems.Add("the prescribed deposit information has not been served on the tenant");

            var overdue = await Db.ComplianceSchedules.ForCompany(Tenant)
                .Where(s => s.PropertyId == tenancy.PropertyId && s.IsMandatory && s.NextDueOn < Today)
                .Select(s => s.Kind.ToString())
                .ToListAsync();

            if (overdue.Count > 0)
                problems.Add($"these certificates are overdue: {string.Join(", ", overdue)}");
        }

        var notice = new TenancyNotice
        {
            TenancyId = dto.TenancyId,
            NoticeNumber = await numbering.NextNoticeNumberAsync(DateTime.UtcNow),
            NoticeType = dto.NoticeType,
            ServedBy = dto.ServedBy,
            ServedOn = servedOn,
            EffectiveFrom = dto.EffectiveFrom,
            NoticePeriodDays = given,
            Grounds = dto.Grounds,
            ServiceMethod = dto.ServiceMethod,
            ServiceEvidenceUrl = dto.ServiceEvidenceUrl,

            // The notice is recorded either way — refusing to record it would leave the file
            // silent about something that happened. It is recorded as invalid, with the reason.
            IsValid = problems.Count == 0,
            ValidityNote = problems.Count == 0
                ? null
                : "This notice may be invalid because " + string.Join("; ", problems) + ".",
        }.StampNew(Tenant, userId);

        Db.TenancyNotices.Add(notice);

        if (dto.NoticeType is "Termination" or "Section21" or "Section8" or "NoticeToQuit")
        {
            tenancy.Status = TenancyStatus.NoticeGiven;
            tenancy.StampUpdated(userId);

            Db.CriticalDates.Add(new CriticalDate
            {
                TenancyId = tenancy.Id,
                PropertyId = tenancy.PropertyId,
                Title = $"{tenancy.Reference} notice expires",
                DateType = "NoticeExpiry",
                DueDate = dto.EffectiveFrom,
                AlertDaysBefore = 21,
                OwnerUserId = tenancy.ManagedByUserId,
                Severity = AlertSeverity.Critical,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();

        dto.Id = notice.Id;
        dto.NoticeNumber = notice.NoticeNumber;
        dto.ServedOn = servedOn;
        dto.NoticePeriodDays = given;
        dto.IsValid = notice.IsValid;
        dto.ValidityNote = notice.ValidityNote;
        return dto;
    }

    public async Task<TenancyRenewalDto> OfferRenewalAsync(TenancyRenewalDto dto, Guid userId)
    {
        var tenancy = await RequireAsync<Tenancy>(dto.TenancyId, "That tenancy does not exist.");

        if (tenancy.EndDate is null)
            throw new InvalidOperationException("A periodic tenancy has no expiry to renew against.");

        var renewal = await Db.TenancyRenewals.ForCompany(Tenant)
            .FirstOrDefaultAsync(r => r.TenancyId == dto.TenancyId && r.Status == "Offered");

        if (renewal is null)
        {
            renewal = new TenancyRenewal
            {
                TenancyId = dto.TenancyId,
                Reference = await numbering.NextMasterCodeAsync(Db.TenancyRenewals, "RNW"),
                ExpiryDate = tenancy.EndDate.Value,
                CurrentRent = tenancy.Rent,
            }.StampNew(Tenant, userId);

            Db.TenancyRenewals.Add(renewal);
        }
        else renewal.StampUpdated(userId);

        renewal.OfferedOn = Today;
        renewal.ProposedRent = dto.ProposedRent;
        renewal.ProposedTermMonths = dto.ProposedTermMonths;
        renewal.LandlordApproved = dto.LandlordApproved;
        renewal.Status = "Offered";
        renewal.Note = dto.Note;

        await Db.SaveChangesAsync();

        var uplift = renewal.ProposedRent is > 0m && renewal.CurrentRent > 0m
            ? RealEstateMapper.Percent(renewal.ProposedRent.Value - renewal.CurrentRent, renewal.CurrentRent)
            : (decimal?)null;

        await QueueNotificationAsync(
            "RenewalOffered",
            "Your tenancy renewal offer",
            renewal.ProposedRent is null
                ? $"We would like to renew your tenancy beyond {renewal.ExpiryDate:dd MMM yyyy}."
                : $"We can offer a renewal at {renewal.ProposedRent:N0}{(uplift is null ? "" : $", a change of {uplift:N1}%")}.",
            $"/realestate/renewals/{renewal.Id}",
            entityType: "TenancyRenewal",
            entityId: renewal.Id);

        await Db.SaveChangesAsync();
        return (await MapRenewalsAsync([renewal]))[0];
    }

    public async Task<TenancyRenewalDto> DecideRenewalAsync(
        Guid id, string status, decimal? agreedRent, Guid? declineReasonCodeId, Guid userId)
    {
        var renewal = await RequireAsync<TenancyRenewal>(id, "That renewal does not exist.");

        if (renewal.Status is "Accepted" or "Declined")
            throw new InvalidOperationException($"This renewal was already {renewal.Status.ToLowerInvariant()}.");

        renewal.Status = status;
        renewal.RespondedOn = Today;
        renewal.AgreedRent = agreedRent;
        renewal.DeclineReasonCodeId = declineReasonCodeId;
        renewal.StampUpdated(userId);

        var tenancy = await RequireAsync<Tenancy>(renewal.TenancyId, "The tenancy behind this renewal is missing.");

        if (status == "Accepted")
        {
            // Extending in place keeps the ledger, the deposit and the inventory continuous.
            // A new record would orphan all three and start the deposit clock again.
            var months = renewal.ProposedTermMonths ?? tenancy.TermMonths ?? 12;

            tenancy.EndDate = renewal.ExpiryDate.AddMonths(months);
            tenancy.TermMonths = months;
            tenancy.Rent = agreedRent ?? renewal.ProposedRent ?? tenancy.Rent;
            tenancy.AnnualRent = AnnualiseRent(tenancy.Rent, tenancy.Frequency);
            tenancy.Status = TenancyStatus.Active;
            tenancy.StampUpdated(userId);

            await BuildScheduleAsync(tenancy, [], userId);
            await CreateCriticalDatesAsync(tenancy, userId);
        }
        else if (status == "Declined")
        {
            Db.CriticalDates.Add(new CriticalDate
            {
                TenancyId = tenancy.Id,
                PropertyId = tenancy.PropertyId,
                Title = $"{tenancy.Reference} ends — arrange check-out and re-let",
                DateType = "CheckOutDue",
                DueDate = renewal.ExpiryDate,
                AlertDaysBefore = 30,
                OwnerUserId = tenancy.ManagedByUserId,
                Severity = AlertSeverity.Warning,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();
        return (await MapRenewalsAsync([renewal]))[0];
    }

    public async Task<PaginatedResponse<TenancyRenewalDto>> GetRenewalPipelineAsync(ListQueryDto query, int withinDays)
    {
        var today = Today;
        var horizon = today.AddDays(withinDays <= 0 ? 120 : withinDays);

        var q = Db.TenancyRenewals.ForCompany(Tenant)
            .Where(r => r.ExpiryDate <= horizon && r.Status != "Accepted")
            .OrderBy(r => r.ExpiryDate);

        return await PageAsync(q, query, MapRenewalsAsync);
    }

    private async Task<List<TenancyRenewalDto>> MapRenewalsAsync(List<TenancyRenewal> renewals)
    {
        if (renewals.Count == 0) return [];

        var today = Today;
        var tenancyIds = renewals.Select(r => r.TenancyId).Distinct().ToList();

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => tenancyIds.Contains(t.Id))
            .ToListAsync();

        var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var leads = await Db.TenancyParties.ForCompany(Tenant)
            .Where(p => tenancyIds.Contains(p.TenancyId) && p.IsLeadTenant)
            .Select(p => new { p.TenancyId, p.PartyId })
            .ToListAsync();

        var names = await PartyNamesAsync(leads.Select(l => l.PartyId));
        var reasons = await ReasonLabelsAsync(renewals.Select(r => r.DeclineReasonCodeId));

        return renewals.Select(r =>
        {
            var tenancy = tenancies.FirstOrDefault(t => t.Id == r.TenancyId);
            var property = tenancy is null ? null : properties.GetValueOrDefault(tenancy.PropertyId);
            var lead = leads.FirstOrDefault(l => l.TenancyId == r.TenancyId);

            var proposed = r.AgreedRent ?? r.ProposedRent;

            return new TenancyRenewalDto
            {
                Id = r.Id,
                Reference = r.Reference,
                TenancyId = r.TenancyId,
                TenancyReference = tenancy?.Reference ?? "—",
                AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                TenantName = lead is null ? "—" : names.GetValueOrDefault(lead.PartyId, "—"),
                ExpiryDate = r.ExpiryDate,
                DaysToExpiry = r.ExpiryDate.DayNumber - today.DayNumber,
                OfferedOn = r.OfferedOn,
                CurrentRent = r.CurrentRent,
                ProposedRent = r.ProposedRent,
                AgreedRent = r.AgreedRent,
                UpliftPercent = proposed is > 0m && r.CurrentRent > 0m
                    ? RealEstateMapper.Percent(proposed.Value - r.CurrentRent, r.CurrentRent)
                    : null,
                ProposedTermMonths = r.ProposedTermMonths,
                Status = r.Status,
                RespondedOn = r.RespondedOn,
                NewTenancyId = r.NewTenancyId,
                DeclineReason = r.DeclineReasonCodeId is null ? null : reasons.GetValueOrDefault(r.DeclineReasonCodeId.Value),
                LandlordApproved = r.LandlordApproved,

                // Annual income that walks out of the door if this one is not renewed. It is what
                // makes a renewal pipeline worth working in priority order.
                ValueAtRisk = tenancy is null ? 0m : AnnualiseRent(tenancy.Rent, tenancy.Frequency),
                Note = r.Note,
            };
        }).ToList();
    }

    public async Task<RentReviewDto> SaveRentReviewAsync(RentReviewDto dto, Guid userId)
    {
        var tenancy = await RequireAsync<Tenancy>(dto.TenancyId, "That tenancy does not exist.");

        var review = dto.Id != Guid.Empty
            ? await Db.RentReviews.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        if (review is null)
        {
            review = new RentReview
            {
                TenancyId = dto.TenancyId,
                Reference = await numbering.NextMasterCodeAsync(Db.RentReviews, "RRV"),
                CurrentRent = tenancy.Rent,
            }.StampNew(Tenant, userId);

            Db.RentReviews.Add(review);
        }
        else review.StampUpdated(userId);

        review.ReviewDate = dto.ReviewDate;
        review.NoticeDeadline = dto.NoticeDeadline;
        review.NoticeServed = dto.NoticeServed;
        review.NoticeServedOn = dto.NoticeServedOn;
        review.ProposedRent = dto.ProposedRent;
        review.CounterProposedRent = dto.CounterProposedRent;
        review.EffectiveFrom = dto.EffectiveFrom ?? dto.ReviewDate;
        review.IsUpwardOnly = dto.IsUpwardOnly;
        review.Note = dto.Note;

        // Upward-only is the commonest commercial review clause in the world. Agreeing a lower
        // figure under one is a variation of the lease, not a review outcome.
        if (dto.AgreedRent is not null && dto.IsUpwardOnly && dto.AgreedRent < review.CurrentRent)
            throw new InvalidOperationException($"This review is upward-only. {dto.AgreedRent:N0} is below the passing rent of {review.CurrentRent:N0}.");

        if (dto.AgreedRent is not null)
        {
            review.AgreedRent = dto.AgreedRent;
            review.AgreedOn = dto.AgreedOn ?? Today;
            review.Outcome = dto.Outcome ?? "Agreed";

            tenancy.Rent = dto.AgreedRent.Value;
            tenancy.AnnualRent = AnnualiseRent(tenancy.Rent, tenancy.Frequency);
            tenancy.StampUpdated(userId);

            await Db.SaveChangesAsync();
            await BuildScheduleAsync(tenancy, [], userId);
        }

        await Db.SaveChangesAsync();

        var address = await Db.Properties.ForCompany(Tenant)
            .Where(p => p.Id == tenancy.PropertyId)
            .Select(p => p.AddressLine1)
            .FirstOrDefaultAsync();

        return new RentReviewDto
        {
            Id = review.Id,
            Reference = review.Reference,
            TenancyId = review.TenancyId,
            TenancyReference = tenancy.Reference,
            AddressOneLine = address,
            ReviewDate = review.ReviewDate,
            NoticeDeadline = review.NoticeDeadline,
            NoticeServed = review.NoticeServed,
            NoticeServedOn = review.NoticeServedOn,

            // Missing the notice deadline on a review usually means the passing rent stands until
            // the next one. It is worth stating plainly rather than leaving to be discovered.
            NoticeDeadlineMissed = !review.NoticeServed && review.NoticeDeadline is not null && review.NoticeDeadline < Today,

            CurrentRent = review.CurrentRent,
            ProposedRent = review.ProposedRent,
            CounterProposedRent = review.CounterProposedRent,
            AgreedRent = review.AgreedRent,
            UpliftPercent = review.AgreedRent is > 0m && review.CurrentRent > 0m
                ? RealEstateMapper.Percent(review.AgreedRent.Value - review.CurrentRent, review.CurrentRent)
                : null,
            AgreedOn = review.AgreedOn,
            EffectiveFrom = review.EffectiveFrom,
            Outcome = review.Outcome,
            IsUpwardOnly = review.IsUpwardOnly,
            Note = review.Note,
        };
    }

    // ═══ Critical dates & compliance ═════════════════════════════════════════

    public async Task<List<CriticalDateDto>> GetCriticalDatesAsync(int withinDays, Guid? propertyId)
    {
        var today = Today;
        var horizon = today.AddDays(withinDays <= 0 ? 90 : withinDays);

        var dates = await Db.CriticalDates.ForCompany(Tenant)
            .Where(d => !d.IsActioned && d.DueDate <= horizon)
            .WhereIf(propertyId.HasValue, d => d.PropertyId == propertyId)
            .OrderBy(d => d.DueDate)
            .ToListAsync();

        return await MapCriticalDatesAsync(dates);
    }

    private async Task<List<CriticalDateDto>> MapCriticalDatesAsync(List<CriticalDate> dates)
    {
        if (dates.Count == 0) return [];

        var today = Today;

        var tenancyIds = dates.Where(d => d.TenancyId.HasValue).Select(d => d.TenancyId!.Value).Distinct().ToList();

        var tenancies = tenancyIds.Count == 0
            ? []
            : await Db.Tenancies.ForCompany(Tenant)
                .Where(t => tenancyIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Reference);

        var propertyIds = dates.Where(d => d.PropertyId.HasValue).Select(d => d.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        var owners = await AgentUserNamesAsync(dates.Select(d => d.OwnerUserId));

        return dates.Select(d => new CriticalDateDto
        {
            Id = d.Id,
            Title = d.Title,
            DateType = d.DateType,
            TenancyId = d.TenancyId,
            TenancyReference = d.TenancyId is null ? null : tenancies.GetValueOrDefault(d.TenancyId.Value),
            PropertyId = d.PropertyId,
            AddressOneLine = d.PropertyId is null ? null : properties.GetValueOrDefault(d.PropertyId.Value),
            DueDate = d.DueDate,
            DaysToDue = d.DueDate.DayNumber - today.DayNumber,
            AlertDaysBefore = d.AlertDaysBefore,
            OwnerName = d.OwnerUserId is null ? null : owners.GetValueOrDefault(d.OwnerUserId.Value),
            Severity = d.Severity,
            AlertSent = d.AlertSent,
            IsActioned = d.IsActioned,
            IsOverdue = !d.IsActioned && d.DueDate < today,
            Note = d.Note,

            // The screen a date should open. Without it the alert is a reminder to go looking.
            Route = d.TenancyId is not null
                ? $"/realestate/tenancies/{d.TenancyId}"
                : d.PropertyId is not null
                    ? $"/realestate/properties/{d.PropertyId}"
                    : null,
        }).ToList();
    }

    public async Task<CriticalDateDto> ActionCriticalDateAsync(Guid id, string? note, Guid userId)
    {
        var date = await RequireAsync<CriticalDate>(id, "That critical date does not exist.");

        date.IsActioned = true;
        date.ActionedOn = Today;
        date.Note = note ?? date.Note;
        date.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await MapCriticalDatesAsync([date]))[0];
    }

    public async Task<PaginatedResponse<ComplianceCertificateDto>> GetCertificatesAsync(ListQueryDto query, bool expiringOnly)
    {
        var today = Today;
        var soon = today.AddDays(60);

        var q = Db.ComplianceCertificates.ForCompany(Tenant)
            .WhereIf(expiringOnly, c => c.IsCurrent && c.ExpiresOn <= soon)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), c => c.CertificateNumber != null && c.CertificateNumber.Contains(query.Search!))
            .OrderBy(c => c.ExpiresOn);

        return await PageAsync(q, query, MapCertificatesAsync);
    }

    private async Task<List<ComplianceCertificateDto>> MapCertificatesAsync(List<ComplianceCertificate> certificates)
    {
        if (certificates.Count == 0) return [];

        var today = Today;
        var propertyIds = certificates.Select(c => c.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        return certificates.Select(c => new ComplianceCertificateDto
        {
            Id = c.Id,
            PropertyId = c.PropertyId,
            AddressOneLine = properties.GetValueOrDefault(c.PropertyId),
            TenancyId = c.TenancyId,
            Kind = c.Kind,
            CertificateNumber = c.CertificateNumber,
            IssuedOn = c.IssuedOn,
            ExpiresOn = c.ExpiresOn,
            DaysToExpiry = c.ExpiresOn.DayNumber - today.DayNumber,
            IsExpired = c.ExpiresOn < today,
            IsExpiringSoon = c.ExpiresOn >= today && c.ExpiresOn <= today.AddDays(60),
            IssuerName = c.IssuerName,
            IssuerRegistration = c.IssuerRegistration,
            Cost = c.Cost,
            BorneBy = c.BorneBy,
            DocumentUrl = c.DocumentUrl,
            ServedToTenant = c.ServedToTenant,
            HasFailures = c.HasFailures,
            Findings = c.Findings,
            RemedialWorkOrderId = c.RemedialWorkOrderId,
            RenewalBooked = c.RenewalBooked,
            IsCurrent = c.IsCurrent,
        }).ToList();
    }

    public async Task<ComplianceCertificateDto> SaveCertificateAsync(ComplianceCertificateDto dto, Guid userId)
    {
        var certificate = dto.Id != Guid.Empty
            ? await Db.ComplianceCertificates.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (certificate is null)
        {
            // A new certificate supersedes the one it replaces, so "current" always means one row.
            var previous = await Db.ComplianceCertificates.ForCompany(Tenant)
                .Where(c => c.PropertyId == dto.PropertyId && c.Kind == dto.Kind && c.IsCurrent)
                .ToListAsync();

            foreach (var old in previous)
            {
                old.IsCurrent = false;
                old.StampUpdated(userId);
            }

            certificate = new ComplianceCertificate
            {
                PropertyId = dto.PropertyId,
                TenancyId = dto.TenancyId,
                IsCurrent = true,
            }.StampNew(Tenant, userId);

            Db.ComplianceCertificates.Add(certificate);
        }
        else certificate.StampUpdated(userId);

        certificate.Kind = dto.Kind;
        certificate.CertificateNumber = dto.CertificateNumber;
        certificate.IssuedOn = dto.IssuedOn;
        certificate.ExpiresOn = dto.ExpiresOn;
        certificate.ValidityMonths = Math.Max(1, ((dto.ExpiresOn.Year - dto.IssuedOn.Year) * 12) + dto.ExpiresOn.Month - dto.IssuedOn.Month);
        certificate.IssuerName = dto.IssuerName;
        certificate.IssuerRegistration = dto.IssuerRegistration;
        certificate.Cost = dto.Cost;
        certificate.BorneBy = dto.BorneBy;
        certificate.DocumentUrl = dto.DocumentUrl;
        certificate.ServedToTenant = dto.ServedToTenant;
        certificate.HasFailures = dto.HasFailures;
        certificate.Findings = dto.Findings;

        // A failed inspection is not a certificate, it is a work order waiting to happen.
        if (dto.HasFailures && string.IsNullOrWhiteSpace(dto.Findings))
            throw new InvalidOperationException("A certificate recording failures has to say what they are.");

        await Db.SaveChangesAsync();

        // Roll the schedule forward so the next one is already diarised.
        var schedule = await Db.ComplianceSchedules.ForCompany(Tenant)
            .FirstOrDefaultAsync(s => s.PropertyId == dto.PropertyId && s.Kind == dto.Kind);

        if (schedule is not null)
        {
            schedule.LastCompletedOn = dto.IssuedOn;
            schedule.NextDueOn = dto.ExpiresOn;
            schedule.StampUpdated(userId);
        }
        else
        {
            Db.ComplianceSchedules.Add(new ComplianceSchedule
            {
                PropertyId = dto.PropertyId,
                Kind = dto.Kind,
                IntervalMonths = certificate.ValidityMonths,
                LastCompletedOn = dto.IssuedOn,
                NextDueOn = dto.ExpiresOn,
                BlocksLettingWhenOverdue = dto.Kind is ComplianceCertificateKind.GasSafety
                                                    or ComplianceCertificateKind.Electrical
                                                    or ComplianceCertificateKind.FireRiskAssessment,
            }.StampNew(Tenant, userId));
        }

        Db.CriticalDates.Add(new CriticalDate
        {
            PropertyId = dto.PropertyId,
            TenancyId = dto.TenancyId,
            Title = $"{dto.Kind} certificate expires",
            DateType = "CertificateExpiry",
            DueDate = dto.ExpiresOn,
            AlertDaysBefore = 45,
            Severity = AlertSeverity.Critical,
        }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();
        return (await MapCertificatesAsync([certificate]))[0];
    }
}
