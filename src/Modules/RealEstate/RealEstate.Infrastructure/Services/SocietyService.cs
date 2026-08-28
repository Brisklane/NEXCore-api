using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The society or owners' association: the body, its committee, and the people who live there.
///
/// A society is not a landlord. Its residents are members, not tenants; its charges are levied by
/// a constitution rather than a lease; and the handover from the developer is a one-way transfer
/// of money, common areas and documents that has to be evidenced. All three shape this file.
/// </summary>
public partial class SocietyService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), ISocietyService
{
    // ═══ Societies ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<SocietyListItemDto>> GetSocietiesAsync(ListQueryDto query)
    {
        var q = Db.Societies.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                s => s.Name.Contains(query.Search!) || (s.RegistrationNumber != null && s.RegistrationNumber.Contains(query.Search!)))
            .WhereIf(query.ProjectId.HasValue, s => s.ProjectId == query.ProjectId)
            .OrderBy(s => s.Name);

        return await PageAsync(q, query, MapSocietyListAsync);
    }

    private async Task<List<SocietyListItemDto>> MapSocietyListAsync(List<Society> societies)
    {
        if (societies.Count == 0) return [];

        var currency = await CurrencyAsync();
        var ids = societies.Select(s => s.Id).ToList();
        var now = DateTime.UtcNow;
        var monthStart = new DateOnly(Today.Year, Today.Month, 1);

        var projects = await ProjectNamesAsync(societies.Select(s => s.ProjectId));

        var defaulters = await Db.Residents.ForCompany(Tenant)
            .Where(r => ids.Contains(r.SocietyId) && r.IsDefaulter && r.MovedOutOn == null)
            .GroupBy(r => r.SocietyId)
            .Select(g => new { SocietyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SocietyId, x => x.Count);

        var complaints = await Db.Complaints.ForCompany(Tenant)
            .Where(c => ids.Contains(c.SocietyId) && c.Status != TicketStatus.Closed && c.Status != TicketStatus.Resolved)
            .GroupBy(c => c.SocietyId)
            .Select(g => new
            {
                SocietyId = g.Key,
                Open = g.Count(),
                Breached = g.Count(x => x.SlaDueAt != null && x.SlaDueAt < now),
            })
            .ToDictionaryAsync(x => x.SocietyId, x => x);

        // Collection efficiency: what was collected against what was billed this month. The single
        // number a committee asks about at every meeting.
        var billing = await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(b => ids.Contains(b.SocietyId) && b.IssuedOn >= monthStart)
            .GroupBy(b => b.SocietyId)
            .Select(g => new
            {
                SocietyId = g.Key,
                Billed = g.Sum(x => x.TotalAmount),
                Collected = g.Sum(x => x.PaidAmount),
            })
            .ToDictionaryAsync(x => x.SocietyId, x => x);

        return societies.Select(s =>
        {
            var complaint = complaints.GetValueOrDefault(s.Id);
            var money = billing.GetValueOrDefault(s.Id);

            return new SocietyListItemDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                ProjectId = s.ProjectId,
                ProjectName = s.ProjectId is null ? null : projects.GetValueOrDefault(s.ProjectId.Value),
                RegistrationNumber = s.RegistrationNumber,
                TotalUnits = s.TotalUnits,
                OccupiedUnits = s.OccupiedUnits,
                OccupancyPercent = RealEstateMapper.Percent(s.OccupiedUnits, s.TotalUnits),
                MonthlyBillingTotal = s.MonthlyBillingTotal,
                OutstandingTotal = s.OutstandingTotal,
                CollectionEfficiencyPercent = money is null ? 0m : RealEstateMapper.Percent(money.Collected, money.Billed),
                CorpusFundBalance = s.CorpusFundBalance,
                CurrencyCode = currency,
                IsHandedOver = s.IsHandedOver,
                ManagedByDeveloper = s.ManagedByDeveloper,
                DefaulterCount = defaulters.GetValueOrDefault(s.Id),
                OpenComplaints = complaint?.Open ?? 0,
                ComplaintsBreachingSla = complaint?.Breached ?? 0,
                IsActive = s.IsActive,
            };
        }).ToList();
    }

    public async Task<SocietyDetailDto?> GetSocietyAsync(Guid id)
    {
        var society = await Db.Societies.ForCompany(Tenant)
            .Include(s => s.Committee)
            .Include(s => s.Amenities)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (society is null) return null;

        var head = (await MapSocietyListAsync([society]))[0];
        var unit = await AreaUnitAsync();

        var detail = new SocietyDetailDto
        {
            Id = head.Id,
            Name = head.Name,
            Code = head.Code,
            ProjectId = head.ProjectId,
            ProjectName = head.ProjectName,
            RegistrationNumber = head.RegistrationNumber,
            TotalUnits = head.TotalUnits,
            OccupiedUnits = head.OccupiedUnits,
            OccupancyPercent = head.OccupancyPercent,
            MonthlyBillingTotal = head.MonthlyBillingTotal,
            OutstandingTotal = head.OutstandingTotal,
            CollectionEfficiencyPercent = head.CollectionEfficiencyPercent,
            CorpusFundBalance = head.CorpusFundBalance,
            CurrencyCode = head.CurrencyCode,
            IsHandedOver = head.IsHandedOver,
            ManagedByDeveloper = head.ManagedByDeveloper,
            DefaulterCount = head.DefaulterCount,
            OpenComplaints = head.OpenComplaints,
            ComplaintsBreachingSla = head.ComplaintsBreachingSla,
            IsActive = head.IsActive,

            PropertyId = society.PropertyId,
            GeoAreaId = society.GeoAreaId,
            RegisteredOn = society.RegisteredOn,
            RegistrationAuthority = society.RegistrationAuthority,
            ConstitutionUrl = society.ConstitutionUrl,
            FinancialYearStartMonth = society.FinancialYearStartMonth,
            TotalArea = RealEstateMapper.Area(society.TotalAreaSqFt, unit),
            BankAccountId = society.BankAccountId,
            SinkingFundId = society.SinkingFundId,
            HandoverDate = society.HandoverDate,
            CorpusTransferred = society.CorpusTransferred,
            CommonAreasTransferred = society.CommonAreasTransferred,
            DocumentsTransferred = society.DocumentsTransferred,
            SuspendAmenitiesOnDefault = society.SuspendAmenitiesOnDefault,
            AmenitySuspensionThreshold = society.AmenitySuspensionThreshold,
        };

        if (society.SinkingFundId is not null)
        {
            detail.SinkingFundBalance = await Db.SinkingFunds.ForCompany(Tenant)
                .Where(f => f.Id == society.SinkingFundId)
                .Select(f => f.CurrentBalance)
                .FirstOrDefaultAsync();
        }

        if (society.FacilityManagerUserId is not null)
        {
            var names = await AgentUserNamesAsync([society.FacilityManagerUserId]);
            detail.FacilityManagerName = names.GetValueOrDefault(society.FacilityManagerUserId.Value);
        }

        detail.Committee = await MapCommitteeAsync(society.Committee.ToList());
        detail.Amenities = await MapAmenitiesAsync(society.Amenities.ToList());
        detail.ChargeSchemes = await GetChargeSchemesAsync(id);

        detail.RecentNotices = await MapNoticesAsync(
            await Db.SocietyNotices.ForCompany(Tenant)
                .Where(n => n.SocietyId == id)
                .OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.PublishedAt)
                .Take(10)
                .ToListAsync());

        detail.Dashboard = await GetDashboardAsync(id);

        return detail;
    }

    protected async Task<Dictionary<Guid, string>> AgentUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    public async Task<SocietyDetailDto> SaveSocietyAsync(SocietyUpsertDto dto, Guid userId)
    {
        var society = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Societies.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (society is null)
        {
            society = new Society
            {
                Code = string.IsNullOrWhiteSpace(dto.Code)
                    ? await numbering.NextMasterCodeAsync(Db.Societies, "SOC")
                    : dto.Code,
            }.StampNew(Tenant, userId);

            Db.Societies.Add(society);
        }
        else society.StampUpdated(userId);

        society.Name = dto.Name;
        society.ProjectId = dto.ProjectId;
        society.PropertyId = dto.PropertyId;
        society.GeoAreaId = dto.GeoAreaId;
        society.RegistrationNumber = dto.RegistrationNumber;
        society.RegisteredOn = dto.RegisteredOn;
        society.RegistrationAuthority = dto.RegistrationAuthority;
        society.ConstitutionUrl = dto.ConstitutionUrl;
        society.FinancialYearStartMonth = Math.Clamp(dto.FinancialYearStartMonth, 1, 12);
        society.BankAccountId = dto.BankAccountId;
        society.DefaultDunningPolicyId = dto.DefaultDunningPolicyId;
        society.FacilityManagerUserId = dto.FacilityManagerUserId;
        society.ManagedByDeveloper = dto.ManagedByDeveloper;
        society.SuspendAmenitiesOnDefault = dto.SuspendAmenitiesOnDefault;
        society.AmenitySuspensionThreshold = dto.AmenitySuspensionThreshold;

        await Db.SaveChangesAsync();

        // Unit and area counts come from the project rather than being typed, because every
        // apportionment and every collection-efficiency figure divides by them.
        if (dto.ProjectId is not null)
        {
            var units = await Db.Units.ForCompany(Tenant)
                .Where(u => u.ProjectId == dto.ProjectId)
                .Join(Db.Properties.ForCompany(Tenant), u => u.PropertyId, p => p.Id,
                    (u, p) => new { u.Id, Area = p.SaleableAreaSqFt ?? 0m, p.Status })
                .ToListAsync();

            society.TotalUnits = units.Count;
            society.TotalAreaSqFt = units.Sum(u => u.Area);

            society.OccupiedUnits = units.Count(u => u.Status is PropertyStatus.Possessed or PropertyStatus.Registered);

            await Db.SaveChangesAsync();
        }

        return (await GetSocietyAsync(society.Id))!;
    }

    /// <summary>
    /// The society's live operating picture — the gate, the helpdesk, the money and the plant.
    /// Deliberately one call, because this is a wall-board and a manager's home screen, and five
    /// round trips to paint it would make both feel broken.
    /// </summary>
    public async Task<SocietyDashboardDto> GetDashboardAsync(Guid societyId)
    {
        var now = DateTime.UtcNow;
        var today = Today;
        var dayStart = today.ToDateTime(TimeOnly.MinValue);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var dashboard = new SocietyDashboardDto();

        var gate = await Db.GateEntries.ForCompany(Tenant)
            .Where(e => e.SocietyId == societyId && e.CheckedInAt >= dayStart.AddDays(-1))
            .Select(e => new { e.Status, e.CheckedInAt, e.CheckedOutAt, e.ApprovalRequested, e.ApprovedAt, e.IsDenied })
            .ToListAsync();

        dashboard.VisitorsInsideNow = gate.Count(e => e.CheckedInAt != null && e.CheckedOutAt == null);
        dashboard.VisitorsToday = gate.Count(e => e.CheckedInAt >= dayStart);
        dashboard.PendingGateApprovals = gate.Count(e => e.ApprovalRequested && e.ApprovedAt == null && !e.IsDenied);

        var complaints = await Db.Complaints.ForCompany(Tenant)
            .Where(c => c.SocietyId == societyId && c.Status != TicketStatus.Closed && c.Status != TicketStatus.Resolved)
            .Select(c => new { c.Category, c.SlaDueAt })
            .ToListAsync();

        dashboard.OpenComplaints = complaints.Count;
        dashboard.ComplaintsBreachingSla = complaints.Count(c => c.SlaDueAt != null && c.SlaDueAt < now);

        dashboard.ComplaintsByCategory = complaints
            .GroupBy(c => c.Category)
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key.ToString(),
                Value = g.Count(),
                Percent = RealEstateMapper.Percent(g.Count(), complaints.Count),
                Count = g.Count(),
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        var bookings = await Db.AmenityBookings.ForCompany(Tenant)
            .Where(b => b.SocietyId == societyId && b.BookingDate == today)
            .Select(b => b.Status)
            .ToListAsync();

        dashboard.AmenityBookingsToday = bookings.Count(s => s != AmenityBookingStatus.Cancelled);
        dashboard.PendingAmenityApprovals = bookings.Count(s => s == AmenityBookingStatus.Requested);

        dashboard.MoveRequestsPending = await Db.MoveRequests.ForCompany(Tenant)
            .CountAsync(m => m.SocietyId == societyId && m.Status == "Requested");

        var bills = await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(b => b.SocietyId == societyId && b.IssuedOn >= monthStart)
            .Select(b => new { b.TotalAmount, b.PaidAmount })
            .ToListAsync();

        dashboard.BilledThisMonth = RealEstateMapper.Money(bills.Sum(b => b.TotalAmount));
        dashboard.CollectedThisMonth = RealEstateMapper.Money(bills.Sum(b => b.PaidAmount));

        dashboard.OutstandingTotal = RealEstateMapper.Money(await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(b => b.SocietyId == societyId && b.Balance > 0m)
            .SumAsync(b => b.Balance));

        var defaulters = await Db.Residents.ForCompany(Tenant)
            .Where(r => r.SocietyId == societyId && r.IsDefaulter && r.MovedOutOn == null)
            .Select(r => r.OutstandingDues)
            .ToListAsync();

        dashboard.DefaulterCount = defaulters.Count;
        dashboard.DefaulterExposure = RealEstateMapper.Money(defaulters.Sum());

        dashboard.OpenWorkOrders = await Db.WorkOrders.ForCompany(Tenant)
            .CountAsync(w => w.SocietyId == societyId
                          && w.Status != WorkOrderStatus.SignedOff
                          && w.Status != WorkOrderStatus.Cancelled);

        dashboard.PpmOverdue = await Db.PpmTasks.ForCompany(Tenant)
            .Join(Db.PpmSchedules.ForCompany(Tenant), t => t.PpmScheduleId, s => s.Id, (t, s) => new { t, s })
            .CountAsync(x => x.s.SocietyId == societyId && x.t.DueDate < today && x.t.CompletedOn == null);

        dashboard.AssetsFaulty = await Db.FacilityAssets.ForCompany(Tenant)
            .CountAsync(a => a.SocietyId == societyId && a.OperationalStatus != "Operational");

        dashboard.BuildingApplicationsPending = await Db.BuildingPlanApplications.ForCompany(Tenant)
            .CountAsync(a => a.SocietyId == societyId
                          && a.Status != BuildingApplicationStatus.Approved
                          && a.Status != BuildingApplicationStatus.Rejected);

        dashboard.ViolationsOpen = await Db.ViolationNotices.ForCompany(Tenant)
            .CountAsync(v => v.SocietyId == societyId && !v.IsComplied && !v.IsWithdrawn);

        // Twelve months of collection against billing, which is how a committee sees a trend
        // rather than a snapshot.
        var trendFrom = monthStart.AddMonths(-11);

        var history = await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(b => b.SocietyId == societyId && b.IssuedOn >= trendFrom)
            .Select(b => new { b.IssuedOn.Year, b.IssuedOn.Month, b.TotalAmount, b.PaidAmount })
            .ToListAsync();

        for (var i = 0; i < 12; i++)
        {
            var month = trendFrom.AddMonths(i);
            var mine = history.Where(h => h.Year == month.Year && h.Month == month.Month).ToList();

            dashboard.CollectionTrend.Add(new TrendPointDto
            {
                Label = month.ToString("MMM yy"),
                Date = month,
                Value = RealEstateMapper.Money(mine.Sum(m => m.PaidAmount)),
                SecondaryValue = RealEstateMapper.Money(mine.Sum(m => m.TotalAmount)),
            });
        }

        return dashboard;
    }

    /// <summary>
    /// The handover from the developer. Money, common areas and documents move together and the
    /// date is stamped once — it starts the society's own liability and ends the developer's, so
    /// it is not a flag somebody can toggle back and forth.
    /// </summary>
    public async Task<SocietyDetailDto> HandOverFromDeveloperAsync(
        Guid societyId, DateOnly handoverDate, decimal corpusTransferred, Guid userId)
    {
        var society = await RequireAsync<Society>(societyId, "That society does not exist.");

        if (society.IsHandedOver)
            throw new InvalidOperationException($"This society was handed over on {society.HandoverDate:dd MMM yyyy}.");

        if (society.Committee.Count == 0)
        {
            var committee = await Db.CommitteeMembers.ForCompany(Tenant)
                .CountAsync(m => m.SocietyId == societyId && (m.ToDate == null || m.ToDate >= handoverDate));

            if (committee == 0)
                throw new InvalidOperationException("A society cannot be handed over before its committee is elected — there is nobody to hand it to.");
        }

        await using var transaction = await Db.Database.BeginTransactionAsync();

        society.IsHandedOver = true;
        society.HandoverDate = handoverDate;
        society.CorpusTransferred = corpusTransferred;
        society.CommonAreasTransferred = true;
        society.DocumentsTransferred = true;
        society.ManagedByDeveloper = false;
        society.CorpusFundBalance = RealEstateMapper.Money(society.CorpusFundBalance + corpusTransferred);
        society.StampUpdated(userId);

        // The corpus lands in the sinking fund, which is where planned works are paid from. Left
        // in a general balance it gets spent on operating costs within two years.
        if (society.SinkingFundId is null && corpusTransferred > 0m)
        {
            var fund = new SinkingFund
            {
                Name = $"{society.Name} corpus fund",
                SocietyId = societyId,
                CurrentBalance = corpusTransferred,
                WithdrawalRequiresApproval = true,
            }.StampNew(Tenant, userId);

            Db.SinkingFunds.Add(fund);
            await Db.SaveChangesAsync();

            society.SinkingFundId = fund.Id;

            Db.SinkingFundEntries.Add(new SinkingFundEntry
            {
                SinkingFundId = fund.Id,
                EntryDate = handoverDate,
                EntryType = "DeveloperHandover",
                CreditAmount = corpusTransferred,
                RunningBalance = corpusTransferred,
            }.StampNew(Tenant, userId));
        }
        else if (society.SinkingFundId is not null && corpusTransferred > 0m)
        {
            var fund = await Db.SinkingFunds.ForCompany(Tenant).FirstAsync(f => f.Id == society.SinkingFundId);

            fund.CurrentBalance = RealEstateMapper.Money(fund.CurrentBalance + corpusTransferred);
            fund.StampUpdated(userId);

            Db.SinkingFundEntries.Add(new SinkingFundEntry
            {
                SinkingFundId = fund.Id,
                EntryDate = handoverDate,
                EntryType = "DeveloperHandover",
                CreditAmount = corpusTransferred,
                RunningBalance = fund.CurrentBalance,
            }.StampNew(Tenant, userId));
        }

        if (society.ProjectId is not null)
        {
            var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == society.ProjectId);

            if (project is not null)
            {
                project.HandoverToSocietyDate = handoverDate;
                project.Status = ProjectStatus.HandedOver;
                project.StampUpdated(userId);
            }
        }

        await WriteAuditNoteAsync(
            "Society", societyId, "SocietyHandedOver", Guid.Empty, userId,
            amountImpact: corpusTransferred,
            note: $"Handed over to the residents' committee on {handoverDate:dd MMM yyyy} with a corpus of {corpusTransferred:N0}.",
            entityReference: society.Code,
            highRisk: true);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetSocietyAsync(societyId))!;
    }

    // ═══ Committee ═══════════════════════════════════════════════════════════

    public async Task<List<CommitteeMemberDto>> GetCommitteeAsync(Guid societyId)
        => await MapCommitteeAsync(await Db.CommitteeMembers.ForCompany(Tenant)
            .Where(m => m.SocietyId == societyId)
            .OrderBy(m => m.Position)
            .ToListAsync());

    private async Task<List<CommitteeMemberDto>> MapCommitteeAsync(List<CommitteeMember> members)
    {
        if (members.Count == 0) return [];

        var today = Today;

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => members.Select(m => m.PartyId).Contains(p.Id))
            .ToListAsync();

        var unitIds = members.Where(m => m.UnitId.HasValue).Select(m => m.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        return members.Select(m =>
        {
            var person = people.FirstOrDefault(p => p.Id == m.PartyId);

            return new CommitteeMemberDto
            {
                Id = m.Id,
                PartyId = m.PartyId,
                Name = person is null ? "—" : RealEstateMapper.DisplayName(person),
                Phone = person?.PrimaryPhone,
                UnitId = m.UnitId,
                UnitLabel = m.UnitId is null ? null : units.GetValueOrDefault(m.UnitId.Value),
                Position = m.Position,
                FromDate = m.FromDate,
                ToDate = m.ToDate,
                CanApproveSpend = m.CanApproveSpend,
                SpendLimit = m.SpendLimit,
                IsActive = m.ToDate is null || m.ToDate >= today,
            };
        }).ToList();
    }

    public async Task<CommitteeMemberDto> SaveCommitteeMemberAsync(Guid societyId, CommitteeMemberDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(societyId, "That society does not exist.");

        var member = dto.Id != Guid.Empty
            ? await Db.CommitteeMembers.ForCompany(Tenant).FirstOrDefaultAsync(m => m.Id == dto.Id)
            : null;

        if (member is null)
        {
            member = new CommitteeMember { SocietyId = societyId, PartyId = dto.PartyId }.StampNew(Tenant, userId);
            Db.CommitteeMembers.Add(member);
        }
        else member.StampUpdated(userId);

        // Only one person holds an office at a time. Two chairmen is a governance dispute, and
        // spend approvals hang off the position.
        if (dto.Position is "Chairman" or "Secretary" or "Treasurer")
        {
            var incumbent = await Db.CommitteeMembers.ForCompany(Tenant)
                .Where(m => m.SocietyId == societyId
                         && m.Position == dto.Position
                         && m.Id != member.Id
                         && (m.ToDate == null || m.ToDate >= dto.FromDate))
                .ToListAsync();

            foreach (var previous in incumbent)
            {
                previous.ToDate = dto.FromDate.AddDays(-1);
                previous.StampUpdated(userId);
            }
        }

        member.UnitId = dto.UnitId;
        member.Position = dto.Position;
        member.FromDate = dto.FromDate == default ? Today : dto.FromDate;
        member.ToDate = dto.ToDate;
        member.CanApproveSpend = dto.CanApproveSpend;
        member.SpendLimit = dto.SpendLimit;

        await Db.SaveChangesAsync();
        return (await MapCommitteeAsync([member]))[0];
    }

    // ═══ Residents ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ResidentListItemDto>> GetResidentsAsync(
        ListQueryDto query, Guid societyId, ResidentKind? kind, bool? defaultersOnly)
    {
        var partyIds = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : Db.Parties.ForCompany(Tenant)
                .Where(p => (p.DisplayName != null && p.DisplayName.Contains(query.Search!))
                         || (p.PrimaryPhone != null && p.PrimaryPhone.Contains(query.Search!)))
                .Select(p => p.Id);

        var q = Db.Residents.ForCompany(Tenant)
            .Where(r => r.SocietyId == societyId)
            .WhereIf(kind.HasValue, r => r.Kind == kind)
            .WhereIf(defaultersOnly == true, r => r.IsDefaulter)
            .WhereIf(!query.IncludeInactive, r => r.MovedOutOn == null)
            .WhereIf(partyIds is not null, r => partyIds!.Contains(r.PartyId))
            .OrderBy(r => r.UnitId);

        return await PageAsync(q, query, MapResidentListAsync);
    }

    private async Task<List<ResidentListItemDto>> MapResidentListAsync(List<Resident> residents)
    {
        if (residents.Count == 0) return [];

        var today = Today;
        var ids = residents.Select(r => r.Id).ToList();

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => residents.Select(r => r.PartyId).Contains(p.Id))
            .ToListAsync();

        var unitIds = residents.Where(r => r.UnitId.HasValue).Select(r => r.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant)
                .Where(u => unitIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UnitNumber, u.ProjectNodeId })
                .ToListAsync();

        var nodeIds = units.Where(u => u.ProjectNodeId.HasValue).Select(u => u.ProjectNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var vehicles = await Db.ResidentVehicles.ForCompany(Tenant)
            .Where(v => v.ResidentId != null && ids.Contains(v.ResidentId.Value) && v.IsActive)
            .GroupBy(v => v.ResidentId!.Value)
            .Select(g => new { ResidentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResidentId, x => x.Count);

        var staff = await Db.DomesticStaffs.ForCompany(Tenant)
            .Where(s => s.ResidentId != null && ids.Contains(s.ResidentId.Value) && s.IsActive)
            .GroupBy(s => s.ResidentId!.Value)
            .Select(g => new { ResidentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResidentId, x => x.Count);

        var householdIds = residents.Where(r => r.HouseholdId.HasValue).Select(r => r.HouseholdId!.Value).Distinct().ToList();

        var households = householdIds.Count == 0
            ? []
            : await Db.ResidentHouseholds.ForCompany(Tenant)
                .Where(h => householdIds.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id, h => h.MemberCount);

        return residents.Select(r =>
        {
            var person = people.FirstOrDefault(p => p.Id == r.PartyId);
            var unit = r.UnitId is null ? null : units.FirstOrDefault(u => u.Id == r.UnitId);

            return new ResidentListItemDto
            {
                Id = r.Id,
                SocietyId = r.SocietyId,
                UnitId = r.UnitId,
                UnitLabel = unit?.UnitNumber ?? "—",
                BlockName = unit?.ProjectNodeId is null ? null : nodes.GetValueOrDefault(unit.ProjectNodeId.Value),
                PartyId = r.PartyId,
                Name = person is null ? "—" : RealEstateMapper.DisplayName(person),
                Phone = person?.PrimaryPhone,
                Email = person?.PrimaryEmail,
                PhotoUrl = person?.PhotoUrl,
                Kind = r.Kind,
                MembershipNumber = r.MembershipNumber,
                MovedInOn = r.MovedInOn,
                MovedOutOn = r.MovedOutOn,
                IsPrimaryContact = r.IsPrimaryContact,
                PortalAccessEnabled = r.PortalAccessEnabled,
                OutstandingDues = r.OutstandingDues,
                IsDefaulter = r.IsDefaulter,
                AmenitiesSuspended = r.AmenitiesSuspended,
                VehicleCount = vehicles.GetValueOrDefault(r.Id),
                StaffCount = staff.GetValueOrDefault(r.Id),
                HouseholdSize = r.HouseholdId is null ? 1 : households.GetValueOrDefault(r.HouseholdId.Value, 1),
                IsActive = r.MovedOutOn is null,
            };
        }).ToList();
    }

    public async Task<ResidentDetailDto?> GetResidentAsync(Guid id)
    {
        var resident = await Db.Residents.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == id);
        if (resident is null) return null;

        var head = (await MapResidentListAsync([resident]))[0];

        var detail = new ResidentDetailDto
        {
            Id = head.Id,
            SocietyId = head.SocietyId,
            UnitId = head.UnitId,
            UnitLabel = head.UnitLabel,
            BlockName = head.BlockName,
            PartyId = head.PartyId,
            Name = head.Name,
            Phone = head.Phone,
            Email = head.Email,
            PhotoUrl = head.PhotoUrl,
            Kind = head.Kind,
            MembershipNumber = head.MembershipNumber,
            MovedInOn = head.MovedInOn,
            MovedOutOn = head.MovedOutOn,
            IsPrimaryContact = head.IsPrimaryContact,
            PortalAccessEnabled = head.PortalAccessEnabled,
            OutstandingDues = head.OutstandingDues,
            IsDefaulter = head.IsDefaulter,
            AmenitiesSuspended = head.AmenitiesSuspended,
            VehicleCount = head.VehicleCount,
            StaffCount = head.StaffCount,
            HouseholdSize = head.HouseholdSize,
            IsActive = head.IsActive,

            PropertyId = resident.PropertyId,
            TenancyId = resident.TenancyId,
            HouseholdId = resident.HouseholdId,
            CanApproveVisitors = resident.CanApproveVisitors,
            CanBookAmenities = resident.CanBookAmenities,
            EmergencyContactName = resident.EmergencyContactName,
            EmergencyContactPhone = resident.EmergencyContactPhone,
            BloodGroup = resident.BloodGroup,
        };

        detail.Vehicles = await Db.ResidentVehicles.ForCompany(Tenant)
            .Where(v => v.ResidentId == id)
            .Select(v => new ResidentVehicleDto
            {
                Id = v.Id,
                RegistrationNumber = v.RegistrationNumber,
                VehicleType = v.VehicleType,
                MakeModel = v.MakeModel,
                Colour = v.Colour,
                ParkingSlotId = v.ParkingSlotId,
                StickerNumber = v.StickerNumber,
                StickerExpiresOn = v.StickerExpiresOn,
                RfidTag = v.RfidTag,
                IsActive = v.IsActive,
            })
            .ToListAsync();

        detail.Staff = await MapStaffAsync(
            await Db.DomesticStaffs.ForCompany(Tenant).Where(s => s.ResidentId == id).ToListAsync());

        detail.Bills = await MapBillsAsync(
            await Db.MaintenanceBills.ForCompany(Tenant)
                .Include(b => b.Lines)
                .Where(b => b.PartyId == resident.PartyId && b.SocietyId == resident.SocietyId)
                .OrderByDescending(b => b.PeriodTo)
                .Take(12)
                .ToListAsync());

        detail.RecentVisitors = await MapGateEntriesAsync(
            await Db.GateEntries.ForCompany(Tenant)
                .Where(e => e.ResidentId == id || (resident.UnitId != null && e.UnitId == resident.UnitId))
                .OrderByDescending(e => e.CheckedInAt)
                .Take(20)
                .ToListAsync());

        return detail;
    }

    public async Task<ResidentDetailDto> SaveResidentAsync(ResidentUpsertDto dto, Guid userId)
    {
        var resident = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Residents.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        var partyId = dto.PartyId ?? Guid.Empty;

        if (partyId == Guid.Empty && dto.NewParty is not null)
        {
            var party = new Party
            {
                Reference = await numbering.NextPartyReferenceAsync(),
                Kind = dto.NewParty.Kind,
                FirstName = dto.NewParty.FirstName,
                LastName = dto.NewParty.LastName,
                DisplayName = $"{dto.NewParty.FirstName} {dto.NewParty.LastName}".Trim(),
                FatherOrGuardianName = dto.NewParty.FatherOrGuardianName,
                PrimaryPhone = dto.NewParty.PrimaryPhone,
                PrimaryEmail = dto.NewParty.PrimaryEmail,
                PreferredChannel = dto.NewParty.PreferredChannel,
                PreferredLanguage = dto.NewParty.PreferredLanguage,
            }.StampNew(Tenant, userId);

            Db.Parties.Add(party);
            await Db.SaveChangesAsync();

            partyId = party.Id;
        }

        if (partyId == Guid.Empty)
            throw new InvalidOperationException("Name the person, or supply enough detail to create them.");

        if (resident is null)
        {
            // One primary resident per unit at a time; the previous one is moved out rather than
            // left overlapping, or two people receive the same bill.
            var occupying = await Db.Residents.ForCompany(Tenant)
                .Where(r => r.UnitId == dto.UnitId && r.MovedOutOn == null && r.Kind == dto.Kind && r.PartyId != partyId)
                .ToListAsync();

            foreach (var previous in occupying.Where(p => p.IsPrimaryContact && dto.IsPrimaryContact))
            {
                previous.IsPrimaryContact = false;
                previous.StampUpdated(userId);
            }

            resident = new Resident
            {
                SocietyId = dto.SocietyId,
                UnitId = dto.UnitId,
                PropertyId = dto.PropertyId,
                PartyId = partyId,
                MembershipNumber = dto.MembershipNumber ?? await numbering.NextMasterCodeAsync(Db.Residents, "MEM"),
            }.StampNew(Tenant, userId);

            Db.Residents.Add(resident);

            var hasRole = await Db.PartyRoles.ForCompany(Tenant)
                .AnyAsync(r => r.PartyId == partyId && r.Kind == PartyRoleKind.Resident && r.IsActive);

            if (!hasRole)
            {
                Db.PartyRoles.Add(new PartyRole
                {
                    PartyId = partyId,
                    Kind = PartyRoleKind.Resident,
                    FromDate = dto.MovedInOn,
                    IsActive = true,
                    ContextType = "Society",
                    ContextId = dto.SocietyId,
                }.StampNew(Tenant, userId));
            }
        }
        else resident.StampUpdated(userId);

        resident.Kind = dto.Kind;
        resident.TenancyId = dto.TenancyId;
        resident.MovedInOn = dto.MovedInOn == default ? Today : dto.MovedInOn;
        resident.IsPrimaryContact = dto.IsPrimaryContact;
        resident.PortalAccessEnabled = dto.PortalAccessEnabled;
        resident.CanApproveVisitors = dto.CanApproveVisitors;
        resident.CanBookAmenities = dto.CanBookAmenities;
        resident.EmergencyContactName = dto.EmergencyContactName;
        resident.EmergencyContactPhone = dto.EmergencyContactPhone;
        resident.BloodGroup = dto.BloodGroup;

        await Db.SaveChangesAsync();
        return (await GetResidentAsync(resident.Id))!;
    }

    public async Task<ResidentDetailDto> MoveOutAsync(Guid residentId, DateOnly movedOutOn, Guid userId)
    {
        var resident = await RequireAsync<Resident>(residentId, "That resident does not exist.");

        if (resident.MovedOutOn is not null)
            throw new InvalidOperationException($"This resident moved out on {resident.MovedOutOn:dd MMM yyyy}.");

        resident.MovedOutOn = movedOutOn;
        resident.PortalAccessEnabled = false;
        resident.CanApproveVisitors = false;
        resident.CanBookAmenities = false;
        resident.StampUpdated(userId);

        // Their staff passes and vehicle stickers stop with them. Live gate credentials for a
        // household that has left is the commonest security hole in a gated scheme.
        var staff = await Db.DomesticStaffs.ForCompany(Tenant)
            .Where(s => s.ResidentId == residentId && !s.WorksForMultipleUnits)
            .ToListAsync();

        foreach (var member in staff)
        {
            member.IsActive = false;
            member.StampUpdated(userId);
        }

        var vehicles = await Db.ResidentVehicles.ForCompany(Tenant)
            .Where(v => v.ResidentId == residentId)
            .ToListAsync();

        foreach (var vehicle in vehicles)
        {
            vehicle.IsActive = false;
            vehicle.StampUpdated(userId);
        }

        if (resident.OutstandingDues > 0m)
        {
            await WriteAuditNoteAsync(
                "Resident", residentId, "ResidentMovedOutWithDues", Guid.Empty, userId,
                amountImpact: resident.OutstandingDues,
                note: $"Moved out on {movedOutOn:dd MMM yyyy} owing {resident.OutstandingDues:N0}.",
                highRisk: true);
        }

        await Db.SaveChangesAsync();
        return (await GetResidentAsync(residentId))!;
    }

    public async Task<ResidentVehicleDto> SaveVehicleAsync(Guid residentId, ResidentVehicleDto dto, Guid userId)
    {
        var resident = await RequireAsync<Resident>(residentId, "That resident does not exist.");

        var vehicle = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.ResidentVehicles.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.Id)
            : null;

        if (vehicle is null)
        {
            // A registration number is unique in the scheme. Two households claiming one car is
            // how a barrier lets the wrong vehicle in.
            var clash = await Db.ResidentVehicles.ForCompany(Tenant)
                .AnyAsync(v => v.SocietyId == resident.SocietyId
                            && v.RegistrationNumber == dto.RegistrationNumber
                            && v.ResidentId != residentId
                            && v.IsActive);

            if (clash)
                throw new InvalidOperationException($"{dto.RegistrationNumber} is already registered to another resident.");

            vehicle = new ResidentVehicle
            {
                SocietyId = resident.SocietyId,
                ResidentId = residentId,
                UnitId = resident.UnitId,
            }.StampNew(Tenant, userId);

            Db.ResidentVehicles.Add(vehicle);
        }
        else vehicle.StampUpdated(userId);

        vehicle.RegistrationNumber = dto.RegistrationNumber.Trim().ToUpperInvariant();
        vehicle.VehicleType = dto.VehicleType;
        vehicle.MakeModel = dto.MakeModel;
        vehicle.Colour = dto.Colour;
        vehicle.ParkingSlotId = dto.ParkingSlotId;
        vehicle.StickerNumber = dto.StickerNumber;
        vehicle.StickerExpiresOn = dto.StickerExpiresOn;
        vehicle.RfidTag = dto.RfidTag;
        vehicle.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();

        dto.Id = vehicle.Id;
        dto.RegistrationNumber = vehicle.RegistrationNumber;
        return dto;
    }

    public async Task<PaginatedResponse<DomesticStaffDto>> GetStaffAsync(ListQueryDto query, Guid societyId)
    {
        var q = Db.DomesticStaffs.ForCompany(Tenant)
            .Where(s => s.SocietyId == societyId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                s => s.Name.Contains(query.Search!) || (s.Phone != null && s.Phone.Contains(query.Search!)))
            .WhereIf(!query.IncludeInactive, s => s.IsActive)
            .OrderBy(s => s.Name);

        return await PageAsync(q, query, MapStaffAsync);
    }

    private async Task<List<DomesticStaffDto>> MapStaffAsync(List<DomesticStaff> staff)
    {
        if (staff.Count == 0) return [];

        var today = Today;
        var ids = staff.Select(s => s.Id).ToList();

        var unitIds = staff.Where(s => s.UnitId.HasValue).Select(s => s.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var lastEntry = await Db.GateEntries.ForCompany(Tenant)
            .Where(e => e.DomesticStaffId != null && ids.Contains(e.DomesticStaffId.Value))
            .GroupBy(e => e.DomesticStaffId!.Value)
            .Select(g => new { StaffId = g.Key, Last = g.Max(x => x.CheckedInAt) })
            .ToDictionaryAsync(x => x.StaffId, x => x.Last);

        return staff.Select(s => new DomesticStaffDto
        {
            Id = s.Id,
            SocietyId = s.SocietyId,
            UnitId = s.UnitId,
            UnitLabel = s.UnitId is null ? null : units.GetValueOrDefault(s.UnitId.Value),
            Name = s.Name,
            Phone = s.Phone,
            PhotoUrl = s.PhotoUrl,
            IdentityNumber = s.IdentityNumber,
            StaffType = s.StaffType,
            PassNumber = s.PassNumber,
            PassIssuedOn = s.PassIssuedOn,
            PassExpiresOn = s.PassExpiresOn,
            PassExpired = s.PassExpiresOn is not null && s.PassExpiresOn < today,
            IsPoliceVerified = s.IsPoliceVerified,
            VerifiedOn = s.VerifiedOn,
            WorksForMultipleUnits = s.WorksForMultipleUnits,
            IsBlacklisted = s.IsBlacklisted,
            BlacklistReason = s.BlacklistReason,
            IsActive = s.IsActive,
            LastEntryAt = lastEntry.GetValueOrDefault(s.Id),
        }).ToList();
    }

    public async Task<DomesticStaffDto> SaveStaffAsync(DomesticStaffDto dto, Guid userId)
    {
        var staff = dto.Id != Guid.Empty
            ? await Db.DomesticStaffs.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (staff is null)
        {
            // The same person working for four flats is normal and should be one record with one
            // pass, not four. Matching on the identity number is what makes that possible.
            if (!string.IsNullOrWhiteSpace(dto.IdentityNumber))
            {
                var existing = await Db.DomesticStaffs.ForCompany(Tenant)
                    .FirstOrDefaultAsync(s => s.SocietyId == dto.SocietyId && s.IdentityNumber == dto.IdentityNumber);

                if (existing is not null)
                {
                    existing.WorksForMultipleUnits = true;
                    existing.StampUpdated(userId);
                    await Db.SaveChangesAsync();

                    return (await MapStaffAsync([existing]))[0];
                }
            }

            staff = new DomesticStaff
            {
                SocietyId = dto.SocietyId,
                PassNumber = await numbering.NextMasterCodeAsync(Db.DomesticStaffs, "STF"),
                PassIssuedOn = Today,
            }.StampNew(Tenant, userId);

            Db.DomesticStaffs.Add(staff);
        }
        else staff.StampUpdated(userId);

        staff.UnitId = dto.UnitId;
        staff.Name = dto.Name;
        staff.Phone = dto.Phone;
        staff.PhotoUrl = dto.PhotoUrl;
        staff.IdentityNumber = dto.IdentityNumber;
        staff.StaffType = dto.StaffType;
        staff.PassExpiresOn = dto.PassExpiresOn ?? Today.AddYears(1);
        staff.IsPoliceVerified = dto.IsPoliceVerified;
        staff.VerifiedOn = dto.VerifiedOn;
        staff.WorksForMultipleUnits = dto.WorksForMultipleUnits;
        staff.IsBlacklisted = dto.IsBlacklisted;
        staff.BlacklistReason = dto.BlacklistReason;
        staff.IsActive = dto.IsActive && !dto.IsBlacklisted;

        if (dto.IsBlacklisted && string.IsNullOrWhiteSpace(dto.BlacklistReason))
            throw new InvalidOperationException("Blacklisting somebody has to say why. It stops them working across the scheme.");

        await Db.SaveChangesAsync();
        return (await MapStaffAsync([staff]))[0];
    }
}
