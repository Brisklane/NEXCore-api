using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Joint ventures with landowners, and the investors who funded the project.
///
/// A landowner does not lend money — they contribute the one input that cannot be bought twice,
/// and they get paid in a share of what the land eventually produces. That makes the landowner a
/// creditor whose balance moves every time a customer pays, which is why this is a ledger rather
/// than a field. Every entitlement is accrued as a credit, every payment as a debit, and the
/// running balance is the number the landowner will argue about in twenty years. Nothing is
/// recomputed in place: an accrual that was raised stays raised.
/// </summary>
public partial class FinanceService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), IFinanceService
{
    // ═══ Joint ventures ══════════════════════════════════════════════════════

    public async Task<List<JointVentureDto>> GetVenturesAsync(Guid? projectId)
    {
        var ventures = await Db.JointVentures.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, v => v.ProjectId == projectId)
            .Include(v => v.Partners)
            .Include(v => v.ShareTerms)
            .OrderByDescending(v => v.AgreementDate)
            .ToListAsync();

        return await MapVenturesAsync(ventures, includeAllocations: false);
    }

    public async Task<JointVentureDto?> GetVentureAsync(Guid id)
    {
        var venture = await Db.JointVentures.ForCompany(Tenant)
            .Include(v => v.Partners)
            .Include(v => v.ShareTerms)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venture is null) return null;

        return (await MapVenturesAsync([venture], includeAllocations: true))[0];
    }

    private async Task<List<JointVentureDto>> MapVenturesAsync(List<JointVenture> ventures, bool includeAllocations)
    {
        if (ventures.Count == 0) return [];

        var unit = await AreaUnitAsync();
        var currency = await CurrencyAsync();
        var ids = ventures.Select(v => v.Id).ToList();

        var projectNames = await ProjectNamesAsync(ventures.Select(v => (Guid?)v.ProjectId));
        var partyNames = await PartyNamesAsync(ventures.SelectMany(v => v.Partners).Select(p => p.PartyId));

        var partyPhones = await Db.Parties.ForCompany(Tenant)
            .Where(p => ventures.SelectMany(v => v.Partners).Select(x => x.PartyId).Contains(p.Id))
            .Select(p => new { p.Id, p.PrimaryPhone })
            .ToDictionaryAsync(x => x.Id, x => x.PrimaryPhone);

        var milestoneNames = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => ventures.SelectMany(v => v.ShareTerms)
                                .Where(t => t.ProjectMilestoneId != null)
                                .Select(t => t.ProjectMilestoneId!.Value)
                                .Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        // Per-partner balances come from the ledger rather than from a stored figure, because the
        // ledger is what a partner is shown and the two must never be able to disagree.
        var partnerLedger = await Db.LandownerLedgerEntries.ForCompany(Tenant)
            .Where(e => ids.Contains(e.JointVentureId) && e.JvPartnerId != null)
            .GroupBy(e => e.JvPartnerId!.Value)
            .Select(g => new
            {
                PartnerId = g.Key,
                Credited = g.Sum(e => e.CreditAmount),
                Debited = g.Sum(e => e.DebitAmount),
            })
            .ToDictionaryAsync(x => x.PartnerId, x => x);

        var allocations = await Db.LandownerAllocations.ForCompany(Tenant)
            .Where(a => ids.Contains(a.JointVentureId))
            .OrderBy(a => a.AllocatedOn)
            .ToListAsync();

        var allocUnits = await UnitLabelsAsync(allocations.Select(a => a.UnitId));

        return ventures.Select(v =>
        {
            var mine = allocations.Where(a => a.JointVentureId == v.Id).ToList();

            var dto = new JointVentureDto
            {
                Id = v.Id,
                Reference = v.Reference,
                Name = v.Name,
                ProjectId = v.ProjectId,
                ProjectName = projectNames.GetValueOrDefault(v.ProjectId, "—"),
                LandParcelId = v.LandParcelId,
                Basis = v.Basis,
                AgreementDate = v.AgreementDate,
                EffectiveFrom = v.EffectiveFrom,
                ExpiresOn = v.ExpiresOn,
                DocumentUrl = v.DocumentUrl,
                LandownerSharePercent = v.LandownerSharePercent,
                DeveloperSharePercent = v.DeveloperSharePercent,
                LandownerArea = RealEstateMapper.AreaOrNull(v.LandownerAreaSqFt, unit),
                ManagementFeePercent = v.ManagementFeePercent,
                RefundableSecurity = v.RefundableSecurity,
                NonRefundableDeposit = v.NonRefundableDeposit,
                ShareOnCollection = v.ShareOnCollection,
                TotalLandownerEntitlement = v.TotalLandownerEntitlement,
                TotalLandownerPaid = v.TotalLandownerPaid,
                LandownerBalance = v.LandownerBalance,
                CurrencyCode = currency,
                HasSpecialPurposeVehicle = v.HasSpecialPurposeVehicle,
                SpvName = v.SpvName,
                IsActive = v.IsActive,
                Terms = v.Terms,
                AllocatedUnitCount = mine.Count,
                AllocatedUnitValue = RealEstateMapper.Money(mine.Sum(a => a.NotionalValue)),
                Partners = v.Partners.OrderByDescending(p => p.SharePercent).Select(p =>
                {
                    var led = partnerLedger.GetValueOrDefault(p.Id);
                    var entitlement = led?.Credited ?? 0m;
                    var paid = led?.Debited ?? 0m;

                    return new JvPartnerDto
                    {
                        Id = p.Id,
                        PartyId = p.PartyId,
                        PartyName = partyNames.GetValueOrDefault(p.PartyId, "—"),
                        Phone = partyPhones.GetValueOrDefault(p.PartyId),
                        Role = p.Role,
                        SharePercent = p.SharePercent,
                        ContributedValue = p.ContributedValue,
                        ContributionType = p.ContributionType,
                        BankName = p.BankName,
                        AccountNumber = p.AccountNumber,
                        WithholdingPercent = p.WithholdingPercent,
                        Entitlement = RealEstateMapper.Money(entitlement),
                        Paid = RealEstateMapper.Money(paid),
                        Balance = RealEstateMapper.Money(entitlement - paid),
                        IsActive = p.IsActive,
                    };
                }).ToList(),
                ShareTerms = v.ShareTerms.OrderBy(t => t.SortOrder).Select(t => new JvShareTermDto
                {
                    Id = t.Id,
                    Label = t.Label,
                    Trigger = t.Trigger,
                    ProjectMilestoneId = t.ProjectMilestoneId,
                    MilestoneName = t.ProjectMilestoneId is null
                        ? null
                        : milestoneNames.GetValueOrDefault(t.ProjectMilestoneId.Value),
                    TriggerCollectionPercent = t.TriggerCollectionPercent,
                    Percent = t.Percent,
                    FixedAmount = t.FixedAmount,
                    EntitlementAmount = t.EntitlementAmount,
                    PaidAmount = t.PaidAmount,
                    DueDate = t.DueDate,
                    IsSettled = t.IsSettled,
                    IsTriggered = t.EntitlementAmount > 0m,
                    SortOrder = t.SortOrder,
                }).ToList(),
            };

            if (includeAllocations)
            {
                dto.Allocations = mine.Select(a => new LandownerAllocationDto
                {
                    Id = a.Id,
                    JointVentureId = a.JointVentureId,
                    JvPartnerId = a.JvPartnerId,
                    PartnerName = a.JvPartnerId is null
                        ? null
                        : v.Partners.Where(p => p.Id == a.JvPartnerId)
                            .Select(p => partyNames.GetValueOrDefault(p.PartyId, "—"))
                            .FirstOrDefault(),
                    UnitId = a.UnitId,
                    UnitNumber = allocUnits.GetValueOrDefault(a.UnitId)?.UnitNumber ?? "—",
                    BlockName = allocUnits.GetValueOrDefault(a.UnitId)?.BlockName,
                    AllocatedOn = a.AllocatedOn,
                    Area = RealEstateMapper.Area(a.AreaSqFt, unit),
                    NotionalValue = a.NotionalValue,
                    Status = a.Status,
                    IsBoughtBack = a.IsBoughtBack,
                    BuyBackPrice = a.BuyBackPrice,
                    HandedOverOn = a.HandedOverOn,
                    Note = a.Note,
                }).ToList();
            }

            return dto;
        }).ToList();
    }

    /// <summary>The unit number and block name a person recognises, resolved in one round trip.</summary>
    private async Task<Dictionary<Guid, UnitLabel>> UnitLabelsAsync(IEnumerable<Guid> unitIds)
    {
        var ids = unitIds.Where(i => i != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return [];

        var rows = await Db.Units.ForCompany(Tenant)
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.UnitNumber,
                u.ProjectId,
                u.ProjectNodeId,
                u.PropertyId,
            })
            .ToListAsync();

        var nodeIds = rows.Where(r => r.ProjectNodeId != null).Select(r => r.ProjectNodeId!.Value).Distinct().ToList();

        var nodeNames = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        return rows.ToDictionary(r => r.Id, r => new UnitLabel(
            r.UnitNumber,
            r.ProjectNodeId is null ? null : nodeNames.GetValueOrDefault(r.ProjectNodeId.Value),
            r.ProjectId,
            r.PropertyId));
    }

    private sealed record UnitLabel(string UnitNumber, string? BlockName, Guid ProjectId, Guid PropertyId);

    /// <summary>Display names for internal users, resolved through their agent profiles.</summary>
    protected async Task<Dictionary<Guid, string>> AgentUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    public async Task<JointVentureDto> SaveVentureAsync(JointVentureDto dto, Guid userId)
    {
        // The split has to add up. A joint venture whose shares total 103% is not a rounding
        // problem, it is a drafting error, and it must be caught before anybody accrues against it.
        var total = dto.LandownerSharePercent + dto.DeveloperSharePercent;
        if (total is < 99.99m or > 100.01m)
            throw new InvalidOperationException(
                $"The landowner and developer shares add up to {total:0.##}%. They must total 100%.");

        if (dto.Partners.Count > 0)
        {
            var partnerTotal = dto.Partners.Where(p => p.Role == "Landowner").Sum(p => p.SharePercent);
            if (dto.Partners.Any(p => p.Role == "Landowner") && (partnerTotal is < 99.99m or > 100.01m))
                throw new InvalidOperationException(
                    $"The landowners' shares of the landowner side add up to {partnerTotal:0.##}%. They must total 100%.");
        }

        var isNew = dto.Id == Guid.Empty;

        var venture = isNew
            ? new JointVenture { Reference = await numbering.NextMasterCodeAsync(Db.JointVentures, "JV") }
            : await Db.JointVentures.ForCompany(Tenant)
                .Include(v => v.Partners)
                .Include(v => v.ShareTerms)
                .FirstOrDefaultAsync(v => v.Id == dto.Id)
              ?? throw new InvalidOperationException("That joint venture does not exist.");

        venture.Name = dto.Name;
        venture.ProjectId = dto.ProjectId;
        venture.LandParcelId = dto.LandParcelId;
        venture.Basis = dto.Basis;
        venture.AgreementDate = dto.AgreementDate;
        venture.EffectiveFrom = dto.EffectiveFrom;
        venture.ExpiresOn = dto.ExpiresOn;
        venture.DocumentUrl = dto.DocumentUrl;
        venture.LandownerSharePercent = dto.LandownerSharePercent;
        venture.DeveloperSharePercent = dto.DeveloperSharePercent;
        venture.LandownerAreaSqFt = dto.LandownerArea?.SquareFeet;
        venture.ManagementFeePercent = dto.ManagementFeePercent;
        venture.RefundableSecurity = dto.RefundableSecurity;
        venture.NonRefundableDeposit = dto.NonRefundableDeposit;
        venture.ShareOnCollection = dto.ShareOnCollection;
        venture.HasSpecialPurposeVehicle = dto.HasSpecialPurposeVehicle;
        venture.SpvName = dto.SpvName;
        venture.Terms = dto.Terms;
        venture.Description = dto.Terms;
        venture.IsActive = dto.IsActive;

        if (isNew)
        {
            venture.StampNew(Tenant, userId);
            Db.JointVentures.Add(venture);
        }
        else
        {
            venture.StampUpdated(userId);
        }

        SyncPartners(venture, dto, userId);
        SyncShareTerms(venture, dto, userId);

        // A project that has a joint venture behaves differently everywhere else — inventory shows
        // landowner units, and cost sheets carry a landowner liability — so the flag lives on the
        // project rather than being inferred by a join on every screen.
        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
        if (project is not null && !project.HasJointVenture)
        {
            project.HasJointVenture = true;
            project.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetVentureAsync(venture.Id))!;
    }

    private void SyncPartners(JointVenture venture, JointVentureDto dto, Guid userId)
    {
        var keep = dto.Partners.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToHashSet();

        foreach (var existing in venture.Partners.Where(p => !keep.Contains(p.Id)).ToList())
        {
            existing.StampDeleted(userId);
        }

        foreach (var row in dto.Partners)
        {
            var partner = row.Id.HasValue
                ? venture.Partners.FirstOrDefault(p => p.Id == row.Id.Value)
                : null;

            if (partner is null)
            {
                partner = new JvPartner { JointVentureId = venture.Id }.StampNew(Tenant, userId);
                venture.Partners.Add(partner);
            }
            else
            {
                partner.StampUpdated(userId);
            }

            partner.PartyId = row.PartyId;
            partner.Role = row.Role;
            partner.SharePercent = row.SharePercent;
            partner.ContributedValue = row.ContributedValue;
            partner.ContributionType = row.ContributionType;
            partner.BankName = row.BankName;
            partner.AccountNumber = row.AccountNumber;
            partner.WithholdingPercent = row.WithholdingPercent;
            partner.IsActive = row.IsActive;
        }
    }

    private void SyncShareTerms(JointVenture venture, JointVentureDto dto, Guid userId)
    {
        var keep = dto.ShareTerms.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();

        foreach (var existing in venture.ShareTerms.Where(t => !keep.Contains(t.Id)).ToList())
        {
            // A term that has already been accrued against cannot simply vanish — the ledger
            // entries point at it. It is closed instead, so the history stays readable.
            if (existing.EntitlementAmount > 0m)
                throw new InvalidOperationException(
                    $"“{existing.Label}” has already been accrued and cannot be removed. Mark it settled instead.");

            existing.StampDeleted(userId);
        }

        var order = 0;

        foreach (var row in dto.ShareTerms.OrderBy(t => t.SortOrder))
        {
            var term = row.Id.HasValue
                ? venture.ShareTerms.FirstOrDefault(t => t.Id == row.Id.Value)
                : null;

            if (term is null)
            {
                term = new JvShareTerm { JointVentureId = venture.Id }.StampNew(Tenant, userId);
                venture.ShareTerms.Add(term);
            }
            else
            {
                term.StampUpdated(userId);
            }

            term.Label = row.Label;
            term.Trigger = row.Trigger;
            term.ProjectMilestoneId = row.ProjectMilestoneId;
            term.TriggerCollectionPercent = row.TriggerCollectionPercent;
            term.Percent = row.Percent;
            term.FixedAmount = row.FixedAmount;
            term.DueDate = row.DueDate;
            term.IsSettled = row.IsSettled;
            term.SortOrder = order++;
        }
    }

    // ═══ Landowner unit allocation ═══════════════════════════════════════════

    public async Task<LandownerAllocationDto> AllocateUnitAsync(
        Guid ventureId, Guid unitId, Guid? jvPartnerId, Guid userId)
    {
        var venture = await RequireAsync<JointVenture>(ventureId, "That joint venture does not exist.");

        var unit = await Db.Units.ForCompany(Tenant)
            .Include(u => u.Property)
            .FirstOrDefaultAsync(u => u.Id == unitId)
            ?? throw new InvalidOperationException("That unit does not exist.");

        if (unit.ProjectId != venture.ProjectId)
            throw new InvalidOperationException("That unit belongs to a different project from this joint venture.");

        // A unit already sold cannot be handed to the landowner: somebody has paid for it. This is
        // the whole point of the check — allocation is an inventory movement, not a note.
        if (unit.Status is PropertyStatus.Booked or PropertyStatus.Sold or PropertyStatus.Registered
                        or PropertyStatus.Possessed or PropertyStatus.Held or PropertyStatus.Reserved)
            throw new InvalidOperationException(
                $"Unit {unit.UnitNumber} is {unit.Status} and cannot be allocated to the landowner. Release it first.");

        if (unit.LandownerAllocationId is not null)
            throw new InvalidOperationException($"Unit {unit.UnitNumber} is already allocated to a landowner.");

        if (jvPartnerId is not null)
        {
            var belongs = await Db.JvPartners.ForCompany(Tenant)
                .AnyAsync(p => p.Id == jvPartnerId && p.JointVentureId == ventureId);

            if (!belongs) throw new InvalidOperationException("That partner is not part of this joint venture.");
        }

        var area = unit.Property?.SaleableAreaSqFt
                   ?? unit.Property?.BuiltUpAreaSqFt
                   ?? unit.Property?.CoveredAreaSqFt
                   ?? unit.Property?.PlotAreaSqFt
                   ?? 0m;

        var allocation = new LandownerAllocation
        {
            JointVentureId = ventureId,
            JvPartnerId = jvPartnerId,
            UnitId = unitId,
            PropertyId = unit.PropertyId,
            AllocatedOn = Today,
            AreaSqFt = area,
            NotionalValue = RealEstateMapper.Money(unit.TotalPrice > 0m ? unit.TotalPrice : unit.BasePrice),
            Status = "Allocated",
        }.StampNew(Tenant, userId);

        Db.LandownerAllocations.Add(allocation);

        unit.LandownerAllocationId = allocation.Id;
        unit.IsSaleable = false;
        unit.Status = PropertyStatus.Blocked;
        unit.StampUpdated(userId);

        // The allocation is a credit against the landowner's entitlement in kind. Where the venture
        // pays out in area rather than cash, the notional value is what the ledger records so that
        // the two sides of the deal remain comparable.
        if (venture.Basis is JvShareBasis.BuiltUpAreaShare or JvShareBasis.SaleableAreaShare)
        {
            await AppendLandownerEntryAsync(
                venture, jvPartnerId, "AllocationInKind",
                credit: 0m, debit: allocation.NotionalValue,
                userId: userId, allocationId: allocation.Id);
        }

        await Db.SaveChangesAsync();

        var unit1 = await AreaUnitAsync();
        var partyNames = await PartyNamesAsync(
            await Db.JvPartners.ForCompany(Tenant)
                .Where(p => p.Id == jvPartnerId)
                .Select(p => p.PartyId)
                .ToListAsync());

        var labels = await UnitLabelsAsync([unitId]);

        return new LandownerAllocationDto
        {
            Id = allocation.Id,
            JointVentureId = ventureId,
            JvPartnerId = jvPartnerId,
            PartnerName = partyNames.Values.FirstOrDefault(),
            UnitId = unitId,
            UnitNumber = labels.GetValueOrDefault(unitId)?.UnitNumber ?? unit.UnitNumber,
            BlockName = labels.GetValueOrDefault(unitId)?.BlockName,
            AllocatedOn = allocation.AllocatedOn,
            Area = RealEstateMapper.Area(allocation.AreaSqFt, unit1),
            NotionalValue = allocation.NotionalValue,
            Status = allocation.Status,
        };
    }

    public async Task<LandownerAllocationDto> ReleaseAllocationAsync(Guid allocationId, Guid userId)
    {
        var allocation = await RequireAsync<LandownerAllocation>(allocationId, "That allocation does not exist.");

        if (allocation.HandedOverOn is not null)
            throw new InvalidOperationException(
                "That unit has already been handed over to the landowner and cannot be released.");

        if (allocation.IsBoughtBack)
            throw new InvalidOperationException("That allocation has been bought back and is already closed.");

        var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == allocation.UnitId);

        if (unit is not null)
        {
            if (unit.CurrentBookingId is not null)
                throw new InvalidOperationException(
                    $"Unit {unit.UnitNumber} now carries a booking and cannot be released back to inventory.");

            unit.LandownerAllocationId = null;
            unit.IsSaleable = true;
            unit.Status = PropertyStatus.Available;
            unit.StampUpdated(userId);
        }

        var venture = await Db.JointVentures.ForCompany(Tenant)
            .FirstOrDefaultAsync(v => v.Id == allocation.JointVentureId);

        // Releasing reverses the in-kind debit rather than deleting it. The landowner's balance
        // returns to where it was, and both movements stay on the ledger.
        if (venture is not null && venture.Basis is JvShareBasis.BuiltUpAreaShare or JvShareBasis.SaleableAreaShare)
        {
            await AppendLandownerEntryAsync(
                venture, allocation.JvPartnerId, "AllocationReleased",
                credit: allocation.NotionalValue, debit: 0m,
                userId: userId, allocationId: allocation.Id);
        }

        allocation.Status = "Released";
        allocation.StampUpdated(userId);

        await Db.SaveChangesAsync();

        var displayUnit = await AreaUnitAsync();
        var labels = await UnitLabelsAsync([allocation.UnitId]);

        return new LandownerAllocationDto
        {
            Id = allocation.Id,
            JointVentureId = allocation.JointVentureId,
            JvPartnerId = allocation.JvPartnerId,
            UnitId = allocation.UnitId,
            UnitNumber = labels.GetValueOrDefault(allocation.UnitId)?.UnitNumber ?? "—",
            BlockName = labels.GetValueOrDefault(allocation.UnitId)?.BlockName,
            AllocatedOn = allocation.AllocatedOn,
            Area = RealEstateMapper.Area(allocation.AreaSqFt, displayUnit),
            NotionalValue = allocation.NotionalValue,
            Status = allocation.Status,
            Note = allocation.Note,
        };
    }

    // ═══ Landowner ledger ════════════════════════════════════════════════════

    public async Task<PaginatedResponse<LandownerLedgerEntryDto>> GetLandownerLedgerAsync(
        Guid ventureId, ListQueryDto query)
    {
        var q = Db.LandownerLedgerEntries.ForCompany(Tenant)
            .Where(e => e.JointVentureId == ventureId)
            .WhereIf(query.FromDate.HasValue, e => e.EntryDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, e => e.EntryDate <= query.ToDate)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt);

        return await PageAsync(q, query, MapLedgerAsync);
    }

    private async Task<List<LandownerLedgerEntryDto>> MapLedgerAsync(List<LandownerLedgerEntry> entries)
    {
        if (entries.Count == 0) return [];

        var partnerIds = entries.Where(e => e.JvPartnerId != null).Select(e => e.JvPartnerId!.Value).Distinct().ToList();

        var partnerParties = partnerIds.Count == 0
            ? []
            : await Db.JvPartners.ForCompany(Tenant)
                .Where(p => partnerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PartyId);

        var names = await PartyNamesAsync(partnerParties.Values);

        var receiptIds = entries.Where(e => e.ReceiptId != null).Select(e => e.ReceiptId!.Value).Distinct().ToList();

        var receipts = receiptIds.Count == 0
            ? []
            : await Db.Receipts.ForCompany(Tenant)
                .Where(r => receiptIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.ReceiptNumber);

        return entries.Select(e => new LandownerLedgerEntryDto
        {
            Id = e.Id,
            EntryDate = e.EntryDate,
            EntryType = e.EntryType,
            Description = e.Description ?? DescribeLedgerEntry(e),
            DebitAmount = e.DebitAmount,
            CreditAmount = e.CreditAmount,
            RunningBalance = e.RunningBalance,
            PartnerName = e.JvPartnerId is not null && partnerParties.TryGetValue(e.JvPartnerId.Value, out var party)
                ? names.GetValueOrDefault(party)
                : null,
            ReceiptNumber = e.ReceiptId is null ? null : receipts.GetValueOrDefault(e.ReceiptId.Value),
        }).ToList();
    }

    private static string DescribeLedgerEntry(LandownerLedgerEntry e) => e.EntryType switch
    {
        "Entitlement" => "Share of collections accrued",
        "AllocationInKind" => "Unit allocated in kind",
        "AllocationReleased" => "Allocated unit released back to inventory",
        "Payment" => "Payment to landowner",
        "Withholding" => "Tax withheld at source",
        "Security" => "Refundable security",
        "Deposit" => "Non-refundable deposit",
        _ => e.EntryType,
    };

    /// <summary>
    /// Appends one movement to the landowner ledger and carries the running balance forward.
    /// The balance is computed from the previous row rather than recalculated from the whole
    /// history, so an entry once written never changes when a later one is added.
    /// </summary>
    private async Task<LandownerLedgerEntry> AppendLandownerEntryAsync(
        JointVenture venture, Guid? partnerId, string entryType,
        decimal credit, decimal debit, Guid userId,
        Guid? receiptId = null, Guid? shareTermId = null, Guid? allocationId = null,
        string? description = null, DateOnly? entryDate = null)
    {
        var previous = await Db.LandownerLedgerEntries.ForCompany(Tenant)
            .Where(e => e.JointVentureId == venture.Id)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => (decimal?)e.RunningBalance)
            .FirstOrDefaultAsync();

        var running = RealEstateMapper.Money((previous ?? 0m) + credit - debit);

        var entry = new LandownerLedgerEntry
        {
            JointVentureId = venture.Id,
            JvPartnerId = partnerId,
            EntryDate = entryDate ?? Today,
            EntryType = entryType,
            CreditAmount = RealEstateMapper.Money(credit),
            DebitAmount = RealEstateMapper.Money(debit),
            RunningBalance = running,
            ReceiptId = receiptId,
            JvShareTermId = shareTermId,
            LandownerAllocationId = allocationId,
            Description = description,
        }.StampNew(Tenant, userId);

        Db.LandownerLedgerEntries.Add(entry);

        venture.TotalLandownerEntitlement = RealEstateMapper.Money(venture.TotalLandownerEntitlement + credit);
        venture.TotalLandownerPaid = RealEstateMapper.Money(venture.TotalLandownerPaid + debit);
        venture.LandownerBalance = running;
        venture.StampUpdated(userId);

        return entry;
    }

    /// <summary>
    /// Accrues the landowner's share of everything collected since the last accrual.
    ///
    /// Two ideas keep this honest. First, the share is taken on collections rather than on bookings
    /// where the agreement says so — a booking that is later cancelled must not leave the landowner
    /// holding an entitlement against money nobody ever paid. Second, the accrual is incremental:
    /// it works out what the entitlement *should* be to date and raises only the difference, so
    /// running it twice in a day does nothing the second time.
    /// </summary>
    public async Task<int> AccrueLandownerShareAsync(Guid ventureId, Guid userId)
    {
        var venture = await Db.JointVentures.ForCompany(Tenant)
            .Include(v => v.Partners)
            .Include(v => v.ShareTerms)
            .FirstOrDefaultAsync(v => v.Id == ventureId)
            ?? throw new InvalidOperationException("That joint venture does not exist.");

        if (!venture.IsActive)
            throw new InvalidOperationException("That joint venture is closed. Reopen it before accruing.");

        var live = new[]
        {
            BookingStatus.Confirmed, BookingStatus.AgreementSigned, BookingStatus.Defaulting,
            BookingStatus.PossessionOffered, BookingStatus.Possessed, BookingStatus.Completed,
        };

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.ProjectId == venture.ProjectId && live.Contains(b.Status))
            .Select(b => new { b.TotalPaid, b.NetSalePrice, b.TotalConsideration })
            .ToListAsync();

        var basisAmount = venture.ShareOnCollection
            ? bookings.Sum(b => b.TotalPaid)
            : bookings.Sum(b => b.NetSalePrice > 0m ? b.NetSalePrice : b.TotalConsideration);

        // The developer's management fee comes off the top where one is agreed, because the
        // landowner shares in the net proceeds of the scheme, not in its gross turnover.
        var afterFee = venture.ManagementFeePercent is > 0m
            ? basisAmount * (1m - venture.ManagementFeePercent.Value / 100m)
            : basisAmount;

        var shouldBe = RealEstateMapper.Money(afterFee * venture.LandownerSharePercent / 100m);

        var accruedSoFar = await Db.LandownerLedgerEntries.ForCompany(Tenant)
            .Where(e => e.JointVentureId == ventureId && e.EntryType == "Entitlement")
            .SumAsync(e => (decimal?)e.CreditAmount) ?? 0m;

        var shortfall = RealEstateMapper.Money(shouldBe - accruedSoFar);

        if (shortfall <= 0.01m) return 0;

        var written = 0;
        var landowners = venture.Partners.Where(p => p.IsActive && p.Role == "Landowner").ToList();

        if (landowners.Count == 0)
        {
            await AppendLandownerEntryAsync(
                venture, null, "Entitlement", credit: shortfall, debit: 0m, userId: userId,
                description: venture.ShareOnCollection
                    ? $"Share of collections to {Today:dd MMM yyyy}"
                    : $"Share of sales value to {Today:dd MMM yyyy}");

            written = 1;
        }
        else
        {
            // Split between landowners by their own agreed shares, with the rounding difference
            // going to the largest holder so the total lands exactly on the shortfall.
            var running = 0m;

            for (var i = 0; i < landowners.Count; i++)
            {
                var partner = landowners[i];
                var isLast = i == landowners.Count - 1;

                var slice = isLast
                    ? RealEstateMapper.Money(shortfall - running)
                    : RealEstateMapper.Money(shortfall * partner.SharePercent / 100m);

                running += slice;

                if (slice <= 0m) continue;

                await AppendLandownerEntryAsync(
                    venture, partner.Id, "Entitlement", credit: slice, debit: 0m, userId: userId,
                    description: $"{partner.SharePercent:0.##}% share of {(venture.ShareOnCollection ? "collections" : "sales value")} to {Today:dd MMM yyyy}");

                written++;
            }
        }

        // Terms that have been reached are marked, so the schedule screen shows what is now payable
        // rather than leaving somebody to work it out from the ledger.
        var collectionPercent = RealEstateMapper.Percent(
            bookings.Sum(b => b.TotalPaid),
            bookings.Sum(b => b.NetSalePrice > 0m ? b.NetSalePrice : b.TotalConsideration));

        foreach (var term in venture.ShareTerms.Where(t => !t.IsSettled))
        {
            var triggered = term.Trigger switch
            {
                "OnCollection" => term.TriggerCollectionPercent is null || collectionPercent >= term.TriggerCollectionPercent,
                "OnMilestone" => term.ProjectMilestoneId is not null
                    && await Db.ProjectMilestones.ForCompany(Tenant)
                        .AnyAsync(m => m.Id == term.ProjectMilestoneId && m.Status == MilestoneStatus.Certified),
                "OnDate" => term.DueDate is not null && term.DueDate <= Today,
                _ => false,
            };

            if (!triggered) continue;

            var entitlement = term.FixedAmount > 0m
                ? term.FixedAmount
                : RealEstateMapper.Money(shouldBe * term.Percent / 100m);

            if (entitlement > term.EntitlementAmount)
            {
                term.EntitlementAmount = entitlement;
                term.StampUpdated(userId);
            }
        }

        await QueueNotificationAsync(
            "jv.accrual.raised",
            $"Landowner share accrued on {venture.Name}",
            $"{shortfall:N0} accrued. Balance now {venture.LandownerBalance:N0}.",
            $"/realestate/finance/ventures/{venture.Id}",
            entityType: nameof(JointVenture),
            entityId: venture.Id);

        await Db.SaveChangesAsync();

        return written;
    }
}
