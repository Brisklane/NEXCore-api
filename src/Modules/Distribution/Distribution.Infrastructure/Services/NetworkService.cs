using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// The channel network and the retail universe.
///
/// Two rules run through this service and are worth stating plainly:
///
/// **An outlet created in the field is not a customer yet.** Doorstep onboarding produces a
/// pending record that cannot be invoiced. Reps are measured on new outlets, and without an
/// approval gate the retail universe fills with shops that do not exist.
///
/// **Duplicates are surfaced, not blocked.** The rep standing in the shop is the only person who
/// can tell a genuine second branch from a duplicate, so the check returns candidates and lets
/// them decide — with the decision recorded.
/// </summary>
public class NetworkService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : INetworkService
{
    // ═══ Partners ════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<PartnerDto>> ListPartnersAsync(
        string? search, PartnerType? type, PartnerStatus? status, Guid? territoryId,
        Guid? parentPartnerId, PaginationParams pagination)
    {
        var query = db.Partners
            .ForTenant(tenant)
            .Include(p => p.ParentPartner)
            .Include(p => p.Territory)
            .WhereIf(type.HasValue, p => p.PartnerType == type)
            .WhereIf(status.HasValue, p => p.Status == status)
            .WhereIf(territoryId.HasValue, p => p.TerritoryId == territoryId)
            .WhereIf(parentPartnerId.HasValue, p => p.ParentPartnerId == parentPartnerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, term) ||
                EF.Functions.ILike(p.Code ?? "", term) ||
                EF.Functions.ILike(p.Phone ?? "", term) ||
                EF.Functions.ILike(p.City ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(p => p.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var ids = rows.Select(r => r.Id).ToList();
        var dtos = rows.Select(r => r.ToDto()).ToList();

        // One grouped query per roll-up rather than one per row: a partner list of fifty would
        // otherwise fire two hundred round trips.
        var outletCounts = await db.Outlets.ForTenant(tenant)
            .Where(o => o.PartnerId != null && ids.Contains(o.PartnerId.Value))
            .GroupBy(o => o.PartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Count);

        var childCounts = await db.Partners.ForTenant(tenant)
            .Where(p => p.ParentPartnerId != null && ids.Contains(p.ParentPartnerId.Value))
            .GroupBy(p => p.ParentPartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Count);

        var credit = await db.CreditProfiles.ForTenant(tenant)
            .Where(c => c.PartnerId != null && ids.Contains(c.PartnerId.Value))
            .ToDictionaryAsync(c => c.PartnerId!.Value);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mtd = await db.Orders.ForTenant(tenant)
            .Where(o => o.PartnerId != null && ids.Contains(o.PartnerId.Value)
                        && o.OrderDate >= monthStart
                        && o.Status != DistributionOrderStatus.Cancelled
                        && o.Status != DistributionOrderStatus.Rejected)
            .GroupBy(o => o.PartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Value = g.Sum(x => x.TotalAmount) })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Value);

        var openClaims = await db.Claims.ForTenant(tenant)
            .Where(c => c.PartnerId != null && ids.Contains(c.PartnerId.Value)
                        && c.Status != ClaimStatus.Settled && c.Status != ClaimStatus.Rejected
                        && c.Status != ClaimStatus.Cancelled)
            .GroupBy(c => c.PartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Count);

        var horizon = DateTime.UtcNow.Date.AddDays(60);
        var expiring = await db.PartnerDocuments.ForTenant(tenant)
            .Where(d => ids.Contains(d.PartnerId) && d.ExpiresOn != null && d.ExpiresOn <= horizon)
            .GroupBy(d => d.PartnerId)
            .Select(g => new { PartnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Count);

        foreach (var dto in dtos)
        {
            dto.OutletCount = outletCounts.GetValueOrDefault(dto.Id);
            dto.ChildPartnerCount = childCounts.GetValueOrDefault(dto.Id);
            dto.MonthToDateSales = mtd.GetValueOrDefault(dto.Id);
            dto.OpenClaimCount = openClaims.GetValueOrDefault(dto.Id);
            dto.ExpiringDocumentCount = expiring.GetValueOrDefault(dto.Id);
            if (credit.TryGetValue(dto.Id, out var c))
            {
                dto.OutstandingAmount = c.OutstandingAmount;
                dto.OverdueAmount = c.OverdueAmount;
            }
        }

        return PaginatedResponse<PartnerDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PartnerDto?> GetPartnerAsync(Guid partnerId)
    {
        var entity = await db.Partners
            .ForTenant(tenant)
            .Include(p => p.ParentPartner)
            .Include(p => p.Territory)
            .Include(p => p.Contacts.Where(c => !c.IsDeleted))
            .Include(p => p.Documents.Where(d => !d.IsDeleted))
            .Include(p => p.Authorisations.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == partnerId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        dto.OutletCount = await db.Outlets.ForTenant(tenant).CountAsync(o => o.PartnerId == partnerId);
        dto.ChildPartnerCount = await db.Partners.ForTenant(tenant).CountAsync(p => p.ParentPartnerId == partnerId);

        var credit = await db.CreditProfiles.ForTenant(tenant).FirstOrDefaultAsync(c => c.PartnerId == partnerId);
        if (credit is not null)
        {
            dto.OutstandingAmount = credit.OutstandingAmount;
            dto.OverdueAmount = credit.OverdueAmount;
        }

        return dto;
    }

    public async Task<List<PartnerTreeNodeDto>> GetPartnerTreeAsync(Guid? rootPartnerId)
    {
        var all = await db.Partners.ForTenant(tenant)
            .Select(p => new
            {
                p.Id, p.Code, p.Name, p.PartnerType, p.Status, p.ParentPartnerId,
            })
            .ToListAsync();

        var outletCounts = await db.Outlets.ForTenant(tenant)
            .Where(o => o.PartnerId != null)
            .GroupBy(o => o.PartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Count);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mtd = await db.Orders.ForTenant(tenant)
            .Where(o => o.PartnerId != null && o.OrderDate >= monthStart)
            .GroupBy(o => o.PartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Value = g.Sum(x => x.TotalAmount) })
            .ToDictionaryAsync(x => x.PartnerId, x => x.Value);

        var outstanding = await db.CreditProfiles.ForTenant(tenant)
            .Where(c => c.PartnerId != null)
            .ToDictionaryAsync(c => c.PartnerId!.Value, c => c.OutstandingAmount);

        var byParent = all.ToLookup(p => p.ParentPartnerId);

        List<PartnerTreeNodeDto> Build(Guid? parentId) =>
            byParent[parentId]
                .OrderBy(p => p.Name)
                .Select(p => new PartnerTreeNodeDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    PartnerType = p.PartnerType,
                    Status = p.Status,
                    OutletCount = outletCounts.GetValueOrDefault(p.Id),
                    MonthToDateSales = mtd.GetValueOrDefault(p.Id),
                    OutstandingAmount = outstanding.GetValueOrDefault(p.Id),
                    Children = Build(p.Id),
                })
                .ToList();

        return rootPartnerId is null
            ? Build(null)
            : Build(rootPartnerId).Count > 0 || all.Any(p => p.Id == rootPartnerId)
                ? [.. Build(null).Where(n => n.Id == rootPartnerId).DefaultIfEmpty(new PartnerTreeNodeDto())]
                : [];
    }

    public async Task<PartnerDto> SavePartnerAsync(Guid? partnerId, SavePartnerDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A partner needs a name.");

        // A partner cannot be its own ancestor: a cycle in the network tree makes every roll-up
        // hang rather than merely be wrong.
        if (partnerId.HasValue && request.ParentPartnerId.HasValue)
            await GuardAgainstCycleAsync(partnerId.Value, request.ParentPartnerId.Value);

        ChannelPartner entity;
        if (partnerId.HasValue)
        {
            entity = await db.Partners.ForTenant(tenant)
                .Include(p => p.Contacts)
                .Include(p => p.Authorisations)
                .FirstOrDefaultAsync(p => p.Id == partnerId)
                ?? throw new InvalidOperationException("That partner no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new ChannelPartner().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.Partners, "DIST")
                : request.Code;
            db.Partners.Add(entity);
        }

        if (partnerId.HasValue && !string.IsNullOrWhiteSpace(request.Code))
            entity.Code = request.Code;

        entity.Name = request.Name.Trim();
        entity.TradeName = request.TradeName;
        entity.PartnerType = request.PartnerType;
        entity.Status = request.Status;
        entity.ServicingModel = request.ServicingModel;
        entity.ParentPartnerId = request.ParentPartnerId;
        entity.ContactPerson = request.ContactPerson;
        entity.Phone = request.Phone;
        entity.AlternatePhone = request.AlternatePhone;
        entity.Email = request.Email;
        entity.AddressLine = request.AddressLine;
        entity.City = request.City;
        entity.StateName = request.StateName;
        entity.PostalCode = request.PostalCode;
        entity.CountryCode = request.CountryCode;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.TaxRegistrationNumber = request.TaxRegistrationNumber;
        entity.SecondaryTaxNumber = request.SecondaryTaxNumber;
        entity.TerritoryId = request.TerritoryId;
        entity.ServicingWarehouseId = request.ServicingWarehouseId;
        entity.PartnerWarehouseId = request.PartnerWarehouseId;
        entity.PriceListId = request.PriceListId;
        entity.CrmAccountId = request.CrmAccountId;
        entity.CurrencyCode = request.CurrencyCode;
        entity.CreditLimit = request.CreditLimit;
        entity.CreditDays = request.CreditDays;
        entity.CreditEnforcement = request.CreditEnforcement;
        entity.SecurityDeposit = request.SecurityDeposit;
        entity.MarginPercent = request.MarginPercent;
        entity.MinimumMonthlyOfftake = request.MinimumMonthlyOfftake;
        entity.AppointedOn = request.AppointedOn;
        entity.AgreementExpiresOn = request.AgreementExpiresOn;
        entity.SecondaryCaptureMode = request.SecondaryCaptureMode;
        entity.Notes = request.Notes;
        entity.IsActive = request.IsActive;

        SyncContacts(entity, request.Contacts, userId);
        SyncAuthorisations(entity, request.Authorisations, userId);

        await db.SaveChangesAsync();
        await EnsureCreditProfileAsync(null, entity.Id, entity.CreditLimit, entity.CreditDays,
            entity.CreditEnforcement, entity.CurrencyCode, userId);

        return (await GetPartnerAsync(entity.Id))!;
    }

    public async Task<PartnerDto> ChangePartnerStatusAsync(Guid partnerId, ChangePartnerStatusDto request, Guid userId)
    {
        var entity = await db.Partners.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == partnerId)
            ?? throw new InvalidOperationException("That partner no longer exists.");

        // A backward move always carries a reason. "Why was this distributor suspended?" is asked
        // months later, by someone who was not in the room.
        var isBackward = request.Status is PartnerStatus.Suspended or PartnerStatus.Terminated;
        if (isBackward && string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Suspending or terminating a partner needs a reason.");

        entity.Status = request.Status;
        entity.StatusReason = request.Reason;
        if (request.Status == PartnerStatus.Terminated)
        {
            entity.TerminatedOn = DateTime.UtcNow;
            entity.TerminationReason = request.Reason;
            entity.IsActive = false;
        }
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetPartnerAsync(partnerId))!;
    }

    public async Task DeletePartnerAsync(Guid partnerId, Guid userId)
    {
        var entity = await db.Partners.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == partnerId)
            ?? throw new InvalidOperationException("That partner no longer exists.");

        var hasChildren = await db.Partners.ForTenant(tenant).AnyAsync(p => p.ParentPartnerId == partnerId);
        if (hasChildren)
            throw new InvalidOperationException("Reassign the sub-distributors under this partner before removing it.");

        var hasOutlets = await db.Outlets.ForTenant(tenant).AnyAsync(o => o.PartnerId == partnerId);
        if (hasOutlets)
            throw new InvalidOperationException("Reassign this partner's outlets before removing it.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<List<PartnerDocumentDto>> GetPartnerDocumentsAsync(Guid partnerId)
        => (await db.PartnerDocuments.ForTenant(tenant)
                .Where(d => d.PartnerId == partnerId)
                .OrderBy(d => d.ExpiresOn ?? DateTime.MaxValue)
                .ToListAsync())
            .Select(d => d.ToDto()).ToList();

    public async Task<PartnerDocumentDto> SavePartnerDocumentAsync(Guid partnerId, PartnerDocumentDto request, Guid userId)
    {
        PartnerDocument entity;
        if (request.Id != Guid.Empty)
        {
            entity = await db.PartnerDocuments.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == request.Id)
                ?? throw new InvalidOperationException("That document no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new PartnerDocument { PartnerId = partnerId }.StampNew(tenant, userId);
            db.PartnerDocuments.Add(entity);
        }

        entity.DocumentType = request.DocumentType;
        entity.DocumentNumber = request.DocumentNumber;
        entity.FileUrl = request.FileUrl;
        entity.IssuedOn = request.IssuedOn;
        entity.ExpiresOn = request.ExpiresOn;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<PartnerDocumentDto> VerifyPartnerDocumentAsync(Guid documentId, string? note, Guid userId)
    {
        var entity = await db.PartnerDocuments.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == documentId)
            ?? throw new InvalidOperationException("That document no longer exists.");

        entity.IsVerified = true;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.VerifiedByUserId = userId;
        entity.VerificationNote = note;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<List<PartnerDocumentDto>> GetExpiringDocumentsAsync(int withinDays)
    {
        var horizon = DateTime.UtcNow.Date.AddDays(withinDays);
        return (await db.PartnerDocuments.ForTenant(tenant)
                .Include(d => d.Partner)
                .Where(d => d.ExpiresOn != null && d.ExpiresOn <= horizon)
                .OrderBy(d => d.ExpiresOn)
                .ToListAsync())
            .Select(d => d.ToDto()).ToList();
    }

    // ═══ Outlets ═════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<OutletDto>> ListOutletsAsync(
        string? search, OutletChannel? channel, OutletGrade? grade, OutletStatus? status,
        Guid? partnerId, Guid? territoryId, Guid? routeId, bool? pendingApprovalOnly,
        PaginationParams pagination)
    {
        var query = db.Outlets
            .ForTenant(tenant)
            .Include(o => o.Partner)
            .WhereIf(channel.HasValue, o => o.Channel == channel)
            .WhereIf(grade.HasValue, o => o.Grade == grade)
            .WhereIf(status.HasValue, o => o.Status == status)
            .WhereIf(partnerId.HasValue, o => o.PartnerId == partnerId)
            .WhereIf(territoryId.HasValue, o => o.TerritoryId == territoryId)
            .WhereIf(pendingApprovalOnly == true, o => !o.IsApproved);

        if (routeId.HasValue)
        {
            var outletIds = db.RouteOutlets.ForTenant(tenant)
                .Where(r => r.RouteId == routeId)
                .Select(r => r.OutletId);
            query = query.Where(o => outletIds.Contains(o.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(o =>
                EF.Functions.ILike(o.Name, term) ||
                EF.Functions.ILike(o.Code ?? "", term) ||
                EF.Functions.ILike(o.OwnerName ?? "", term) ||
                EF.Functions.ILike(o.OwnerPhone ?? "", term) ||
                EF.Functions.ILike(o.Area ?? "", term) ||
                EF.Functions.ILike(o.City ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(o => o.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();

        var ids = rows.Select(r => r.Id).ToList();
        var credit = await db.CreditProfiles.ForTenant(tenant)
            .Where(c => c.OutletId != null && ids.Contains(c.OutletId.Value))
            .ToDictionaryAsync(c => c.OutletId!.Value);

        foreach (var dto in dtos)
            if (credit.TryGetValue(dto.Id, out var c))
                dto.Credit = c.ToSnapshot();

        return PaginatedResponse<OutletDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<OutletDto?> GetOutletAsync(Guid outletId)
    {
        var entity = await db.Outlets
            .ForTenant(tenant)
            .Include(o => o.Partner)
            .Include(o => o.Assets.Where(a => !a.IsDeleted))
            .Include(o => o.Contacts.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == outletId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        dto.Routes = await GetRouteLinksAsync(outletId);

        var credit = await db.CreditProfiles.ForTenant(tenant).FirstOrDefaultAsync(c => c.OutletId == outletId);
        if (credit is not null) dto.Credit = credit.ToSnapshot();

        return dto;
    }

    public async Task<Outlet360Dto?> GetOutlet360Async(Guid outletId)
    {
        var outlet = await GetOutletAsync(outletId);
        if (outlet is null) return null;

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = monthStart.AddMonths(-1);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var dto = new Outlet360Dto
        {
            Outlet = outlet,
            Credit = outlet.Credit ?? new CreditSnapshotDto { OutletId = outletId },
        };

        dto.RecentVisits = (await db.Visits.ForTenant(tenant)
                .Include(v => v.Outlet)
                .Where(v => v.OutletId == outletId)
                .OrderByDescending(v => v.CheckedInAt)
                .Take(10).ToListAsync())
            .Select(v => v.ToSummary()).ToList();

        dto.RecentOrders = (await db.Orders.ForTenant(tenant)
                .Include(o => o.Outlet)
                .Where(o => o.OutletId == outletId)
                .OrderByDescending(o => o.OrderDate)
                .Take(10).ToListAsync())
            .Select(o => o.ToSummary()).ToList();

        dto.RecentCollections = (await db.Collections.ForTenant(tenant)
                .Include(c => c.Outlet)
                .Where(c => c.OutletId == outletId && !c.IsReversed)
                .OrderByDescending(c => c.CollectedAt)
                .Take(10).ToListAsync())
            .Select(c => c.ToSummary()).ToList();

        dto.RecentReturns = (await db.Returns.ForTenant(tenant)
                .Include(r => r.Outlet).Include(r => r.Lines)
                .Where(r => r.OutletId == outletId)
                .OrderByDescending(r => r.RequestedOn)
                .Take(10).ToListAsync())
            .Select(r => r.ToSummary()).ToList();

        dto.Photos = (await db.OutletPhotos.ForTenant(tenant)
                .Where(p => p.OutletId == outletId)
                .OrderByDescending(p => p.CapturedAt).Take(20).ToListAsync())
            .Select(p => p.ToDto()).ToList();

        dto.Notes = (await db.OutletNotes.ForTenant(tenant)
                .Where(n => n.OutletId == outletId)
                .OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.NotedAt)
                .Take(20).ToListAsync())
            .Select(n => n.ToDto()).ToList();

        dto.SchemeHistory = (await db.SchemeApplications.ForTenant(tenant)
                .Where(s => s.OutletId == outletId && !s.IsReversed)
                .OrderByDescending(s => s.AppliedAt).Take(20).ToListAsync())
            .Select(s => s.ToDto()).ToList();

        dto.Audits = await db.MerchandisingAudits.ForTenant(tenant)
            .Where(a => a.OutletId == outletId)
            .OrderByDescending(a => a.AuditedAt).Take(10)
            .Select(a => new MerchandisingAuditSummaryDto
            {
                Id = a.Id,
                Kind = a.Kind,
                AuditedAt = a.AuditedAt,
                Score = a.Score,
                ShareOfShelfPercent = a.ShareOfShelfPercent,
                OnShelfAvailabilityPercent = a.OnShelfAvailabilityPercent,
            })
            .ToListAsync();

        // Period sales, computed once over the order set rather than three separate scans.
        var orderTotals = await db.Orders.ForTenant(tenant)
            .Where(o => o.OutletId == outletId && o.OrderDate >= yearStart && IsRevenue(o.Status))
            .Select(o => new { o.OrderDate, o.TotalAmount })
            .ToListAsync();

        dto.MonthToDateSales = orderTotals.Where(o => o.OrderDate >= monthStart).Sum(o => o.TotalAmount);
        dto.LastMonthSales = orderTotals
            .Where(o => o.OrderDate >= lastMonthStart && o.OrderDate < monthStart)
            .Sum(o => o.TotalAmount);
        dto.YearToDateSales = orderTotals.Sum(o => o.TotalAmount);
        dto.GrowthPercent = dto.LastMonthSales == 0
            ? 0
            : Math.Round((dto.MonthToDateSales - dto.LastMonthSales) / dto.LastMonthSales * 100, 2);

        dto.TopItems = await GetTopItemsAsync(outletId);
        dto.GapItems = await GetGapItemsAsync(outletId, outlet.Channel, outlet.TerritoryId);

        return dto;
    }

    public async Task<OutletDto> SaveOutletAsync(Guid? outletId, SaveOutletDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("An outlet needs a name.");

        RetailOutlet entity;
        if (outletId.HasValue)
        {
            entity = await db.Outlets.ForTenant(tenant)
                .Include(o => o.Contacts)
                .FirstOrDefaultAsync(o => o.Id == outletId)
                ?? throw new InvalidOperationException("That outlet no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new RetailOutlet().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.Outlets, "OUT")
                : request.Code;
            entity.OnboardedAt = DateTime.UtcNow;
            // Back-office creation is a deliberate act by someone with the authority, so it is
            // approved on arrival. The field path below is the one that needs a gate.
            entity.IsApproved = true;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByUserId = userId;
            db.Outlets.Add(entity);
        }

        if (outletId.HasValue && !string.IsNullOrWhiteSpace(request.Code))
            entity.Code = request.Code;

        ApplyOutletFields(entity, request);
        SyncOutletContacts(entity, request.Contacts, userId);

        await db.SaveChangesAsync();

        if (request.RouteIds.Count > 0)
            await SyncRouteLinksAsync(entity.Id, request.RouteIds, userId);

        await EnsureCreditProfileAsync(entity.Id, null, entity.CreditLimit, entity.CreditDays,
            entity.CreditEnforcement, entity.CurrencyCode, userId);

        return (await GetOutletAsync(entity.Id))!;
    }

    public async Task<OutletDto> OnboardOutletAsync(OnboardOutletDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("The shop needs a name.");
        if (string.IsNullOrWhiteSpace(request.OwnerPhone))
            throw new InvalidOperationException("A contact number is required to add a new outlet.");

        // Idempotency: a rep who taps twice on a flaky connection must not create two shops.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await db.Outlets.ForTenant(tenant)
                .Include(o => o.Partner)
                .FirstOrDefaultAsync(o => o.Description == request.IdempotencyKey);
            if (existing is not null) return existing.ToDto();
        }

        if (!request.OverrideDuplicateWarning)
        {
            var duplicates = await FindDuplicateOutletsAsync(
                request.OwnerPhone, request.TaxRegistrationNumber, request.Name,
                request.Latitude, request.Longitude);

            if (duplicates.Count > 0)
                throw new InvalidOperationException(
                    $"This looks like an existing outlet ({duplicates[0].Name}). Confirm it is a different shop to continue.");
        }

        var entity = new RetailOutlet
        {
            Name = request.Name.Trim(),
            OwnerName = request.OwnerName,
            OwnerPhone = request.OwnerPhone,
            Channel = request.Channel,
            Grade = request.Grade,
            AddressLine = request.AddressLine,
            Landmark = request.Landmark,
            Area = request.Area,
            City = request.City,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PhotoUrl = request.PhotoUrl,
            TaxRegistrationNumber = request.TaxRegistrationNumber,
            PartnerId = request.PartnerId,
            Notes = request.Note,
            OnboardedAt = DateTime.UtcNow,

            // The gate: created in the field, not yet billable.
            Status = OutletStatus.PendingApproval,
            IsApproved = false,

            // Reused as the idempotency marker; the field never writes a description otherwise.
            Description = request.IdempotencyKey,
        }.StampNew(tenant, userId);

        entity.Code = await numbering.NextMasterCodeAsync(db.Outlets, "OUT");

        db.Outlets.Add(entity);
        await db.SaveChangesAsync();

        if (request.RouteId.HasValue)
            await SyncRouteLinksAsync(entity.Id, [request.RouteId.Value], userId);

        // The rep's day counts this immediately — the approval gate is on billing, not on credit
        // for the work.
        if (request.FieldRepId.HasValue)
        {
            var day = await db.FieldDays.ForTenant(tenant)
                .Where(d => d.FieldRepId == request.FieldRepId && d.Status == FieldDayStatus.Started)
                .OrderByDescending(d => d.WorkDate)
                .FirstOrDefaultAsync();

            if (day is not null)
            {
                day.NewOutletsAdded++;
                await db.SaveChangesAsync();
            }
        }

        return (await GetOutletAsync(entity.Id))!;
    }

    public async Task<List<DuplicateCandidateDto>> FindDuplicateOutletsAsync(
        string? phone, string? taxNumber, string? name, double? latitude, double? longitude)
    {
        var results = new List<DuplicateCandidateDto>();

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var byPhone = await db.Outlets.ForTenant(tenant)
                .Where(o => o.OwnerPhone == phone || o.AlternatePhone == phone)
                .Take(5).ToListAsync();
            results.AddRange(byPhone.Select(o => ToCandidate(o, "SamePhone", null)));
        }

        if (!string.IsNullOrWhiteSpace(taxNumber))
        {
            var byTax = await db.Outlets.ForTenant(tenant)
                .Where(o => o.TaxRegistrationNumber == taxNumber)
                .Take(5).ToListAsync();
            results.AddRange(byTax.Select(o => ToCandidate(o, "SameTaxNumber", null)));
        }

        // Geo proximity: two shops within fifty metres with a similar name are almost always one
        // shop entered twice. The distance is computed in-memory on a coarse bounding box rather
        // than in SQL, because PostGIS is not a dependency of this module.
        if (latitude.HasValue && longitude.HasValue)
        {
            const double boxDegrees = 0.0015; // ~150m at the equator, wider toward the poles.
            var nearby = await db.Outlets.ForTenant(tenant)
                .Where(o => o.Latitude != null && o.Longitude != null
                            && o.Latitude > latitude - boxDegrees && o.Latitude < latitude + boxDegrees
                            && o.Longitude > longitude - boxDegrees && o.Longitude < longitude + boxDegrees)
                .Take(20).ToListAsync();

            foreach (var o in nearby)
            {
                var metres = GeoDistanceMetres(latitude.Value, longitude.Value, o.Latitude!.Value, o.Longitude!.Value);
                if (metres <= 50) results.Add(ToCandidate(o, "NearbyGeo", (decimal)metres));
            }
        }

        if (!string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 4)
        {
            var term = $"%{name.Trim()}%";
            var byName = await db.Outlets.ForTenant(tenant)
                .Where(o => EF.Functions.ILike(o.Name, term))
                .Take(5).ToListAsync();
            results.AddRange(byName.Select(o => ToCandidate(o, "SimilarName", null)));
        }

        return results
            .GroupBy(r => r.OutletId)
            .Select(g => g.OrderBy(x => x.MatchReason == "NearbyGeo" ? 0 : 1).First())
            .Take(10)
            .ToList();
    }

    public async Task<OutletDto> ApproveOutletAsync(Guid outletId, bool isApproved, string? reason, Guid userId)
    {
        var entity = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId)
            ?? throw new InvalidOperationException("That outlet no longer exists.");

        if (isApproved)
        {
            entity.IsApproved = true;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByUserId = userId;
            if (entity.Status == OutletStatus.PendingApproval) entity.Status = OutletStatus.Active;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Rejecting a new outlet needs a reason.");
            entity.IsApproved = false;
            entity.Status = OutletStatus.Blacklisted;
            entity.StatusReason = reason;
            entity.IsActive = false;
        }

        entity.StampUpdated(userId);
        await db.SaveChangesAsync();
        return (await GetOutletAsync(outletId))!;
    }

    public async Task<OutletDto> ChangeOutletStatusAsync(Guid outletId, OutletStatus status, string? reason, Guid userId)
    {
        var entity = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId)
            ?? throw new InvalidOperationException("That outlet no longer exists.");

        var needsReason = status is OutletStatus.Blacklisted or OutletStatus.CreditBlocked
            or OutletStatus.PermanentlyClosed or OutletStatus.TemporarilyClosed;

        if (needsReason && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Changing an outlet to this status needs a reason.");

        entity.Status = status;
        entity.StatusReason = reason;
        entity.IsActive = status is OutletStatus.Active or OutletStatus.Prospect or OutletStatus.TemporarilyClosed;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetOutletAsync(outletId))!;
    }

    public async Task<OutletDto> MergeOutletsAsync(Guid survivorId, Guid duplicateId, Guid userId)
    {
        if (survivorId == duplicateId)
            throw new InvalidOperationException("An outlet cannot be merged into itself.");

        var survivor = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == survivorId)
            ?? throw new InvalidOperationException("The surviving outlet no longer exists.");
        var duplicate = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == duplicateId)
            ?? throw new InvalidOperationException("The duplicate outlet no longer exists.");

        // History moves rather than disappearing. A merge that loses last year's orders is worse
        // than the duplicate it was meant to fix.
        await db.Orders.ForTenant(tenant).Where(o => o.OutletId == duplicateId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.OutletId, survivorId));
        await db.Visits.ForTenant(tenant).Where(v => v.OutletId == duplicateId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.OutletId, survivorId));
        await db.Collections.ForTenant(tenant).Where(c => c.OutletId == duplicateId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.OutletId, survivorId));
        await db.Returns.ForTenant(tenant).Where(r => r.OutletId == duplicateId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.OutletId, survivorId));
        await db.OutletAssets.ForTenant(tenant).Where(a => a.OutletId == duplicateId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.OutletId, survivorId));
        await db.OutletPhotos.ForTenant(tenant).Where(p => p.OutletId == duplicateId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.OutletId, survivorId));
        await db.OutletNotes.ForTenant(tenant).Where(n => n.OutletId == duplicateId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.OutletId, survivorId));
        await db.SecondarySales.ForTenant(tenant).Where(s2 => s2.OutletId == duplicateId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.OutletId, survivorId));

        // Route links move only where the survivor is not already on that beat.
        var dupLinks = await db.RouteOutlets.ForTenant(tenant).Where(r => r.OutletId == duplicateId).ToListAsync();
        var survivorRouteIds = await db.RouteOutlets.ForTenant(tenant)
            .Where(r => r.OutletId == survivorId).Select(r => r.RouteId).ToListAsync();

        foreach (var link in dupLinks)
        {
            if (survivorRouteIds.Contains(link.RouteId)) link.StampDeleted(userId);
            else link.OutletId = survivorId;
        }

        survivor.LifetimeSales += duplicate.LifetimeSales;
        survivor.TotalVisits += duplicate.TotalVisits;
        survivor.ProductiveVisits += duplicate.ProductiveVisits;
        survivor.OutstandingAmount += duplicate.OutstandingAmount;
        survivor.StampUpdated(userId);

        duplicate.Status = OutletStatus.PermanentlyClosed;
        duplicate.StatusReason = $"Merged into {survivor.Code} — {survivor.Name}";
        duplicate.IsActive = false;
        duplicate.StampDeleted(userId);

        await db.SaveChangesAsync();
        return (await GetOutletAsync(survivorId))!;
    }

    public async Task DeleteOutletAsync(Guid outletId, Guid userId)
    {
        var entity = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId)
            ?? throw new InvalidOperationException("That outlet no longer exists.");

        var hasOrders = await db.Orders.ForTenant(tenant).AnyAsync(o => o.OutletId == outletId);
        if (hasOrders)
            throw new InvalidOperationException(
                "This outlet has orders against it. Close it instead of deleting, so the history stays intact.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Assets, photos, notes ═══════════════════════════════════════════════

    public async Task<List<OutletAssetDto>> ListAssetsAsync(Guid? outletId, OutletAssetKind? kind, AssetCondition? condition)
        => (await db.OutletAssets.ForTenant(tenant)
                .Include(a => a.Outlet)
                .WhereIf(outletId.HasValue, a => a.OutletId == outletId)
                .WhereIf(kind.HasValue, a => a.Kind == kind)
                .WhereIf(condition.HasValue, a => a.Condition == condition)
                .OrderBy(a => a.AssetTag)
                .ToListAsync())
            .Select(a => a.ToDto()).ToList();

    public async Task<OutletAssetDto> SaveAssetAsync(Guid? assetId, OutletAssetDto request, Guid userId)
    {
        OutletAsset entity;
        if (assetId.HasValue)
        {
            entity = await db.OutletAssets.ForTenant(tenant).FirstOrDefaultAsync(a => a.Id == assetId)
                ?? throw new InvalidOperationException("That asset no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new OutletAsset().StampNew(tenant, userId);
            db.OutletAssets.Add(entity);
        }

        entity.OutletId = request.OutletId;
        entity.Kind = request.Kind;
        entity.AssetTag = string.IsNullOrWhiteSpace(request.AssetTag)
            ? await numbering.NextMasterCodeAsync(db.OutletAssets, "AST")
            : request.AssetTag;
        entity.SerialNumber = request.SerialNumber;
        entity.Model = request.Model;
        entity.Manufacturer = request.Manufacturer;
        entity.PlacedOn = request.PlacedOn == default ? DateTime.UtcNow : request.PlacedOn;
        entity.RetrievedOn = request.RetrievedOn;
        entity.AssetValue = request.AssetValue;
        entity.DepositTaken = request.DepositTaken;
        entity.Condition = request.Condition;
        entity.ServiceDueOn = request.ServiceDueOn;
        entity.PhotoUrl = request.PhotoUrl;
        entity.Note = request.Note;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<OutletAssetDto> VerifyAssetAsync(
        Guid assetId, AssetCondition condition, string? photoUrl, string? note, Guid userId)
    {
        var entity = await db.OutletAssets.ForTenant(tenant).FirstOrDefaultAsync(a => a.Id == assetId)
            ?? throw new InvalidOperationException("That asset no longer exists.");

        entity.Condition = condition;
        entity.LastVerifiedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(photoUrl)) entity.PhotoUrl = photoUrl;
        if (!string.IsNullOrWhiteSpace(note)) entity.Note = note;
        if (condition == AssetCondition.Retrieved) entity.RetrievedOn = DateTime.UtcNow;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<List<OutletPhotoDto>> ListPhotosAsync(Guid outletId, string? tag)
        => (await db.OutletPhotos.ForTenant(tenant)
                .Where(p => p.OutletId == outletId)
                .WhereIf(!string.IsNullOrWhiteSpace(tag), p => p.Tag == tag)
                .OrderByDescending(p => p.CapturedAt)
                .ToListAsync())
            .Select(p => p.ToDto()).ToList();

    public async Task<OutletPhotoDto> AddPhotoAsync(OutletPhotoDto request, Guid userId)
    {
        var entity = new OutletPhoto
        {
            OutletId = request.OutletId,
            VisitId = request.VisitId,
            ImageUrl = request.ImageUrl,
            Caption = request.Caption,
            Tag = request.Tag,
            CapturedAt = request.CapturedAt == default ? DateTime.UtcNow : request.CapturedAt,
            PairedPhotoId = request.PairedPhotoId,
        }.StampNew(tenant, userId);

        db.OutletPhotos.Add(entity);
        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<List<OutletNoteDto>> ListNotesAsync(Guid outletId)
        => (await db.OutletNotes.ForTenant(tenant)
                .Where(n => n.OutletId == outletId)
                .OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.NotedAt)
                .ToListAsync())
            .Select(n => n.ToDto()).ToList();

    public async Task<OutletNoteDto> AddNoteAsync(OutletNoteDto request, Guid userId)
    {
        var entity = new OutletNote
        {
            OutletId = request.OutletId,
            VisitId = request.VisitId,
            Text = request.Text,
            AuthorName = request.AuthorName,
            NotedAt = request.NotedAt == default ? DateTime.UtcNow : request.NotedAt,
            IsPinned = request.IsPinned,
        }.StampNew(tenant, userId);

        db.OutletNotes.Add(entity);
        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private static bool IsRevenue(DistributionOrderStatus s)
        => s != DistributionOrderStatus.Cancelled
           && s != DistributionOrderStatus.Rejected
           && s != DistributionOrderStatus.Draft;

    private static DuplicateCandidateDto ToCandidate(RetailOutlet o, string reason, decimal? metres) => new()
    {
        OutletId = o.Id,
        Code = o.Code,
        Name = o.Name,
        OwnerPhone = o.OwnerPhone,
        AddressLine = o.AddressLine,
        MatchReason = reason,
        DistanceMetres = metres,
    };

    /// <summary>Haversine, in metres. Good enough at the scale a geofence cares about.</summary>
    internal static double GeoDistanceMetres(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMetres = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusMetres * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private async Task GuardAgainstCycleAsync(Guid partnerId, Guid proposedParentId)
    {
        var cursor = proposedParentId;
        var seen = new HashSet<Guid> { partnerId };

        for (var depth = 0; depth < 20; depth++)
        {
            if (!seen.Add(cursor))
                throw new InvalidOperationException("That parent would create a loop in the network.");

            var next = await db.Partners.ForTenant(tenant)
                .Where(p => p.Id == cursor).Select(p => p.ParentPartnerId).FirstOrDefaultAsync();

            if (next is null) return;
            cursor = next.Value;
        }

        throw new InvalidOperationException("The network hierarchy is nested too deeply.");
    }

    private void ApplyOutletFields(RetailOutlet entity, SaveOutletDto request)
    {
        entity.Name = request.Name.Trim();
        entity.OwnerName = request.OwnerName;
        entity.OwnerPhone = request.OwnerPhone;
        entity.DecisionMakerName = request.DecisionMakerName;
        entity.AlternatePhone = request.AlternatePhone;
        entity.Email = request.Email;
        entity.Channel = request.Channel;
        entity.SubChannel = request.SubChannel;
        entity.Grade = request.Grade;
        entity.Status = request.Status;
        entity.ChainName = request.ChainName;
        entity.StoreFormat = request.StoreFormat;
        entity.ShelfCount = request.ShelfCount;
        entity.HasRefrigeration = request.HasRefrigeration;
        entity.AddressLine = request.AddressLine;
        entity.Landmark = request.Landmark;
        entity.Area = request.Area;
        entity.City = request.City;
        entity.StateName = request.StateName;
        entity.PostalCode = request.PostalCode;
        entity.CountryCode = request.CountryCode;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.GeofenceRadiusMetres = request.GeofenceRadiusMetres;
        entity.GeoNodeId = request.GeoNodeId;
        entity.PartnerId = request.PartnerId;
        entity.TerritoryId = request.TerritoryId;
        entity.PriceListId = request.PriceListId;
        entity.SchemeGroupId = request.SchemeGroupId;
        entity.CrmContactId = request.CrmContactId;
        entity.CurrencyCode = request.CurrencyCode;
        entity.CreditLimit = request.CreditLimit;
        entity.CreditDays = request.CreditDays;
        entity.CreditEnforcement = request.CreditEnforcement;
        entity.PreferredTender = request.PreferredTender;
        entity.TaxRegistrationNumber = request.TaxRegistrationNumber;
        entity.LicenceNumber = request.LicenceNumber;
        entity.LicenceExpiresOn = request.LicenceExpiresOn;
        entity.OpensAt = request.OpensAt;
        entity.ClosesAt = request.ClosesAt;
        entity.WeeklyOffDay = request.WeeklyOffDay;
        entity.PreferredDeliveryFrom = request.PreferredDeliveryFrom;
        entity.PreferredDeliveryTo = request.PreferredDeliveryTo;
        entity.PhotoUrl = request.PhotoUrl;
        entity.Notes = request.Notes;
        entity.IsActive = request.IsActive;
    }

    private void SyncContacts(ChannelPartner entity, List<PartnerContactDto> incoming, Guid userId)
    {
        foreach (var existing in entity.Contacts.Where(c => !c.IsDeleted).ToList())
            if (incoming.All(i => i.Id != existing.Id))
                existing.StampDeleted(userId);

        foreach (var dto in incoming)
        {
            var target = dto.Id != Guid.Empty
                ? entity.Contacts.FirstOrDefault(c => c.Id == dto.Id)
                : null;

            if (target is null)
            {
                target = new PartnerContact { PartnerId = entity.Id }.StampNew(tenant, userId);
                entity.Contacts.Add(target);
            }

            target.FullName = dto.FullName;
            target.Designation = dto.Designation;
            target.Phone = dto.Phone;
            target.Email = dto.Email;
            target.IsPrimary = dto.IsPrimary;
            target.HandlesClaims = dto.HandlesClaims;
            target.HandlesPayments = dto.HandlesPayments;
        }
    }

    private void SyncOutletContacts(RetailOutlet entity, List<OutletContactDto> incoming, Guid userId)
    {
        foreach (var existing in entity.Contacts.Where(c => !c.IsDeleted).ToList())
            if (incoming.All(i => i.Id != existing.Id))
                existing.StampDeleted(userId);

        foreach (var dto in incoming)
        {
            var target = dto.Id != Guid.Empty ? entity.Contacts.FirstOrDefault(c => c.Id == dto.Id) : null;
            if (target is null)
            {
                target = new OutletContact { OutletId = entity.Id }.StampNew(tenant, userId);
                entity.Contacts.Add(target);
            }

            target.FullName = dto.FullName;
            target.Designation = dto.Designation;
            target.Phone = dto.Phone;
            target.Email = dto.Email;
            target.IsPrimary = dto.IsPrimary;
        }
    }

    private void SyncAuthorisations(ChannelPartner entity, List<PartnerAuthorisationDto> incoming, Guid userId)
    {
        foreach (var existing in entity.Authorisations.Where(a => !a.IsDeleted).ToList())
            if (incoming.All(i => i.Id != existing.Id))
                existing.StampDeleted(userId);

        foreach (var dto in incoming)
        {
            var target = dto.Id != Guid.Empty ? entity.Authorisations.FirstOrDefault(a => a.Id == dto.Id) : null;
            if (target is null)
            {
                target = new PartnerAuthorisation { PartnerId = entity.Id }.StampNew(tenant, userId);
                entity.Authorisations.Add(target);
            }

            target.BrandId = dto.BrandId;
            target.CategoryId = dto.CategoryId;
            target.ItemId = dto.ItemId;
            target.ScopeName = dto.ScopeName;
            target.EffectiveFrom = dto.EffectiveFrom == default ? DateTime.UtcNow : dto.EffectiveFrom;
            target.EffectiveTo = dto.EffectiveTo;
            target.IsExclusive = dto.IsExclusive;
        }
    }

    private async Task<List<OutletRouteLinkDto>> GetRouteLinksAsync(Guid outletId)
        => await db.RouteOutlets.ForTenant(tenant)
            .Include(r => r.Route)
            .Where(r => r.OutletId == outletId && r.Route != null)
            .Select(r => new OutletRouteLinkDto
            {
                RouteId = r.RouteId,
                RouteName = r.Route!.Name,
                Kind = r.Route.Kind,
                StopSequence = r.StopSequence,
                IsMustVisit = r.IsMustVisit,
                Frequency = r.FrequencyOverride ?? r.Route.Frequency,
            })
            .ToListAsync();

    private async Task SyncRouteLinksAsync(Guid outletId, List<Guid> routeIds, Guid userId)
    {
        var existing = await db.RouteOutlets.ForTenant(tenant).Where(r => r.OutletId == outletId).ToListAsync();

        foreach (var link in existing.Where(l => !routeIds.Contains(l.RouteId)))
            link.StampDeleted(userId);

        foreach (var routeId in routeIds.Where(id => existing.All(l => l.RouteId != id || l.IsDeleted)))
        {
            var lastSeq = await db.RouteOutlets.ForTenant(tenant)
                .Where(r => r.RouteId == routeId)
                .Select(r => (int?)r.StopSequence).MaxAsync() ?? 0;

            db.RouteOutlets.Add(new RouteOutlet
            {
                RouteId = routeId,
                OutletId = outletId,
                StopSequence = lastSeq + 1,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        await RefreshRouteCountsAsync(routeIds);
    }

    private async Task RefreshRouteCountsAsync(IEnumerable<Guid> routeIds)
    {
        foreach (var routeId in routeIds.Distinct())
        {
            var count = await db.RouteOutlets.ForTenant(tenant).CountAsync(r => r.RouteId == routeId);
            await db.Routes.ForTenant(tenant).Where(r => r.Id == routeId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.OutletCount, count));
        }
    }

    /// <summary>
    /// Creates the credit profile when a master is first saved, so the field terminal always has
    /// something to evaluate against rather than falling back to "no limit found, allow it".
    /// </summary>
    private async Task EnsureCreditProfileAsync(
        Guid? outletId, Guid? partnerId, decimal limit, int days,
        CreditEnforcement enforcement, string currency, Guid userId)
    {
        var profile = await db.CreditProfiles.ForTenant(tenant)
            .FirstOrDefaultAsync(c => (outletId != null && c.OutletId == outletId)
                                      || (partnerId != null && c.PartnerId == partnerId));

        if (profile is null)
        {
            profile = new CreditProfile
            {
                OutletId = outletId,
                PartnerId = partnerId,
            }.StampNew(tenant, userId);
            db.CreditProfiles.Add(profile);
        }
        else
        {
            profile.StampUpdated(userId);
        }

        profile.CreditLimit = limit;
        profile.CreditDays = days;
        profile.Enforcement = enforcement;
        profile.CurrencyCode = currency;
        profile.AvailableCredit = DistributionMapper.EffectiveLimit(profile)
                                  - profile.OutstandingAmount - profile.UnbilledOrderValue;

        await db.SaveChangesAsync();
    }

    private async Task<List<OutletItemOfftakeDto>> GetTopItemsAsync(Guid outletId)
    {
        var since = DateTime.UtcNow.AddMonths(-6);

        return await db.OrderLines.ForTenant(tenant)
            .Where(l => db.Orders.ForTenant(tenant)
                .Any(o => o.Id == l.OrderId && o.OutletId == outletId && o.OrderDate >= since && IsRevenue(o.Status)))
            .GroupBy(l => new { l.ItemId, l.ItemName, l.ItemCode })
            .Select(g => new OutletItemOfftakeDto
            {
                ItemId = g.Key.ItemId,
                ItemName = g.Key.ItemName,
                ItemCode = g.Key.ItemCode,
                Quantity = g.Sum(x => x.BaseQuantity),
                Value = g.Sum(x => x.LineTotal),
            })
            .OrderByDescending(x => x.Value)
            .Take(15)
            .ToListAsync();
    }

    /// <summary>
    /// SKUs this outlet's peers buy and it does not.
    ///
    /// "Peers" is same channel and same territory, which is a crude comparison but a defensible
    /// one: it is the same shop type in the same market, which is exactly the argument a rep
    /// makes at the counter.
    /// </summary>
    private async Task<List<OutletItemOfftakeDto>> GetGapItemsAsync(
        Guid outletId, OutletChannel channel, Guid? territoryId)
    {
        var since = DateTime.UtcNow.AddMonths(-3);

        var bought = await db.OrderLines.ForTenant(tenant)
            .Where(l => db.Orders.ForTenant(tenant).Any(o => o.Id == l.OrderId && o.OutletId == outletId))
            .Select(l => l.ItemId)
            .Distinct()
            .ToListAsync();

        var peerOutletIds = db.Outlets.ForTenant(tenant)
            .Where(o => o.Id != outletId && o.Channel == channel
                        && (territoryId == null || o.TerritoryId == territoryId))
            .Select(o => o.Id);

        return await db.OrderLines.ForTenant(tenant)
            .Where(l => !bought.Contains(l.ItemId)
                        && db.Orders.ForTenant(tenant).Any(o =>
                            o.Id == l.OrderId && o.OutletId != null
                            && peerOutletIds.Contains(o.OutletId.Value)
                            && o.OrderDate >= since && IsRevenue(o.Status)))
            .GroupBy(l => new { l.ItemId, l.ItemName, l.ItemCode })
            .Select(g => new OutletItemOfftakeDto
            {
                ItemId = g.Key.ItemId,
                ItemName = g.Key.ItemName,
                ItemCode = g.Key.ItemCode,
                PeerBuyerCount = g.Select(x => x.OrderId).Distinct().Count(),
                PeerAverageQuantity = g.Average(x => x.BaseQuantity),
            })
            .OrderByDescending(x => x.PeerBuyerCount)
            .Take(15)
            .ToListAsync();
    }
}
