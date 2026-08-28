using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Work orders and the contractors who carry them out.
///
/// Two rules shape this file. A repair above the landlord's authority limit needs their consent
/// before anyone is sent — spending an owner's money without asking is how a managing agent loses
/// a portfolio — and an emergency can always override that, in writing, because a burst main does
/// not wait for an email. And a contractor whose insurance has lapsed cannot be assigned at all:
/// that is the agency's liability, not the contractor's.
/// </summary>
public partial class FacilityService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), IFacilityService
{
    // ═══ Work orders ═════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<WorkOrderListItemDto>> GetWorkOrdersAsync(WorkOrderSearchDto query)
    {
        var now = DateTime.UtcNow;

        var q = Db.WorkOrders.ForCompany(Tenant)
            .WhereIf(query.Statuses.Count > 0, w => query.Statuses.Contains(w.Status))
            .WhereIf(query.Priority.HasValue, w => w.Priority == query.Priority)
            .WhereIf(query.PropertyId.HasValue, w => w.PropertyId == query.PropertyId)
            .WhereIf(query.SocietyId.HasValue, w => w.SocietyId == query.SocietyId)
            .WhereIf(query.ContractorId.HasValue, w => w.ContractorId == query.ContractorId)
            .WhereIf(query.AssignedToUserId.HasValue, w => w.AssignedToUserId == query.AssignedToUserId)
            .WhereIf(query.Sources.Count > 0, w => query.Sources.Contains(w.Source))
            .WhereIf(!string.IsNullOrWhiteSpace(query.Trade), w => w.Trade == query.Trade)
            .WhereIf(query.AwaitingAuthorisationOnly == true,
                w => w.RequiresAuthorisation && !w.IsAuthorised && w.Status != WorkOrderStatus.Cancelled)
            .WhereIf(query.BreachingSlaOnly == true,
                w => w.CompletionDueAt != null && w.CompletionDueAt < now
                  && w.Status != WorkOrderStatus.SignedOff && w.Status != WorkOrderStatus.Cancelled)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                w => w.OrderNumber.Contains(query.Search!) || w.Title.Contains(query.Search!))
            .OrderByDescending(w => w.Priority).ThenBy(w => w.CompletionDueAt);

        return await PageAsync(q, query, MapWorkOrderListAsync);
    }

    private async Task<List<WorkOrderListItemDto>> MapWorkOrderListAsync(List<WorkOrder> orders)
    {
        if (orders.Count == 0) return [];

        var now = DateTime.UtcNow;
        var currency = await CurrencyAsync();

        var propertyIds = orders.Where(w => w.PropertyId.HasValue).Select(w => w.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        var unitIds = orders.Where(w => w.UnitId.HasValue).Select(w => w.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var societyIds = orders.Where(w => w.SocietyId.HasValue).Select(w => w.SocietyId!.Value).Distinct().ToList();

        var societies = societyIds.Count == 0
            ? []
            : await Db.Societies.ForCompany(Tenant).Where(s => societyIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        var contractorIds = orders.Where(w => w.ContractorId.HasValue).Select(w => w.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant).Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        var assignees = await AgentUserNamesAsync(orders.Select(w => w.AssignedToUserId));
        var raisers = await PartyNamesAsync(orders.Where(w => w.RaisedByPartyId.HasValue).Select(w => w.RaisedByPartyId!.Value));

        return orders.Select(w =>
        {
            var live = w.Status is not (WorkOrderStatus.SignedOff or WorkOrderStatus.Cancelled or WorkOrderStatus.Rejected);

            return new WorkOrderListItemDto
            {
                Id = w.Id,
                OrderNumber = w.OrderNumber,
                Source = w.Source,
                Status = w.Status,
                Priority = w.Priority,
                PropertyId = w.PropertyId,
                AddressOneLine = w.PropertyId is null ? null : properties.GetValueOrDefault(w.PropertyId.Value),
                UnitId = w.UnitId,
                UnitLabel = w.UnitId is null ? null : units.GetValueOrDefault(w.UnitId.Value),
                SocietyName = w.SocietyId is null ? null : societies.GetValueOrDefault(w.SocietyId.Value),
                LocationDetail = w.LocationDetail,
                Title = w.Title,
                Trade = w.Trade,
                RaisedAt = w.RaisedAt,
                RaisedByName = w.RaisedByPartyId is null ? null : raisers.GetValueOrDefault(w.RaisedByPartyId.Value),
                AssignedToName = w.AssignedToUserId is null ? null : assignees.GetValueOrDefault(w.AssignedToUserId.Value),
                ContractorName = w.ContractorId is null ? null : contractors.GetValueOrDefault(w.ContractorId.Value),
                AppointmentFrom = w.AppointmentFrom,
                AppointmentTo = w.AppointmentTo,
                ResponseDueAt = w.ResponseDueAt,
                CompletionDueAt = w.CompletionDueAt,
                SlaBreached = live && w.CompletionDueAt is not null && w.CompletionDueAt < now,

                // Negative means overdue, so a list sorted by it puts the worst first.
                HoursToSla = w.CompletionDueAt is null || !live ? null : (int)(w.CompletionDueAt.Value - now).TotalHours,

                EstimatedCost = w.EstimatedCost,
                TotalCost = w.TotalCost,
                CurrencyCode = currency,
                CostBearer = w.CostBearer,
                RequiresAuthorisation = w.RequiresAuthorisation,
                IsAuthorised = w.IsAuthorised,
                OccupierSignedOff = w.OccupierSignedOff,
                SatisfactionRating = w.SatisfactionRating,
                AgeHours = (int)((w.CompletedAt ?? now) - w.RaisedAt).TotalHours,
            };
        }).ToList();
    }

    protected async Task<Dictionary<Guid, string>> AgentUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    public async Task<WorkOrderDetailDto?> GetWorkOrderAsync(Guid id)
    {
        var order = await Db.WorkOrders.ForCompany(Tenant)
            .Include(w => w.Lines)
            .Include(w => w.Photos)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (order is null) return null;

        var head = (await MapWorkOrderListAsync([order]))[0];

        var detail = new WorkOrderDetailDto
        {
            Id = head.Id,
            OrderNumber = head.OrderNumber,
            Source = head.Source,
            Status = head.Status,
            Priority = head.Priority,
            PropertyId = head.PropertyId,
            AddressOneLine = head.AddressOneLine,
            UnitId = head.UnitId,
            UnitLabel = head.UnitLabel,
            SocietyName = head.SocietyName,
            LocationDetail = head.LocationDetail,
            Title = head.Title,
            Trade = head.Trade,
            RaisedAt = head.RaisedAt,
            RaisedByName = head.RaisedByName,
            AssignedToName = head.AssignedToName,
            ContractorName = head.ContractorName,
            AppointmentFrom = head.AppointmentFrom,
            AppointmentTo = head.AppointmentTo,
            ResponseDueAt = head.ResponseDueAt,
            CompletionDueAt = head.CompletionDueAt,
            SlaBreached = head.SlaBreached,
            HoursToSla = head.HoursToSla,
            EstimatedCost = head.EstimatedCost,
            TotalCost = head.TotalCost,
            CurrencyCode = head.CurrencyCode,
            CostBearer = head.CostBearer,
            RequiresAuthorisation = head.RequiresAuthorisation,
            IsAuthorised = head.IsAuthorised,
            OccupierSignedOff = head.OccupierSignedOff,
            SatisfactionRating = head.SatisfactionRating,
            AgeHours = head.AgeHours,

            ProjectId = order.ProjectId,
            SocietyId = order.SocietyId,
            FacilityAssetId = order.FacilityAssetId,
            Description = order.Description ?? string.Empty,
            TenancyId = order.TenancyId,
            ComplaintId = order.ComplaintId,
            SnagId = order.SnagId,
            DefectClaimId = order.DefectClaimId,
            PpmTaskId = order.PpmTaskId,
            InspectionFindingId = order.InspectionFindingId,
            Access = order.Access,
            KeySetId = order.KeySetId,
            OccupierNotified = order.OccupierNotified,
            AccessNoticeId = order.AccessNoticeId,
            ApprovalRequestId = order.ApprovalRequestId,
            AuthorisedAt = order.AuthorisedAt,
            IsEmergencyOverride = order.IsEmergencyOverride,
            StartedAt = order.StartedAt,
            CompletedAt = order.CompletedAt,
            LabourHours = order.LabourHours,
            WorkDone = order.WorkDone,
            SignedOffAt = order.SignedOffAt,
            SignatureUrl = order.SignatureUrl,
            LabourCost = order.LabourCost,
            MaterialCost = order.MaterialCost,
            ContractorCost = order.ContractorCost,
            IsRecharged = order.IsRecharged,
            Note = order.Note,

            Lines = order.Lines.OrderBy(l => l.SortOrder).Select(l => new WorkOrderLineDto
            {
                Id = l.Id,
                Description = l.Description ?? l.LineType,
                LineType = l.LineType,
                Quantity = l.Quantity,
                Uom = l.Uom,
                Rate = l.Rate,
                Amount = l.Amount,
                ItemId = l.ItemId,
                WarehouseId = l.WarehouseId,
                StockIssued = l.StockIssued,
                SortOrder = l.SortOrder,
            }).ToList(),

            Photos = order.Photos.OrderBy(p => p.Stage).ThenBy(p => p.CapturedAt).Select(p => new WorkOrderPhotoDto
            {
                Id = p.Id,
                Url = p.Url,
                Stage = p.Stage,
                CapturedAt = p.CapturedAt,
                Caption = p.Caption,
            }).ToList(),
        };

        if (order.AuthorisedByUserId is not null)
        {
            var names = await AgentUserNamesAsync([order.AuthorisedByUserId]);
            detail.AuthorisedByName = names.GetValueOrDefault(order.AuthorisedByUserId.Value);
        }

        if (order.FacilityAssetId is not null)
        {
            detail.AssetName = await Db.FacilityAssets.ForCompany(Tenant)
                .Where(a => a.Id == order.FacilityAssetId)
                .Select(a => a.Name)
                .FirstOrDefaultAsync();
        }

        detail.LandlordAuthorityLimit = await AuthorityLimitAsync(order.PropertyId, order.TenancyId);

        var shares = await Db.WorkOrderCosts.ForCompany(Tenant)
            .Where(c => c.WorkOrderId == id)
            .ToListAsync();

        var shareParties = await PartyNamesAsync(shares.Where(s => s.PartyId.HasValue).Select(s => s.PartyId!.Value));

        detail.CostShares = shares.Select(s => new WorkOrderCostShareDto
        {
            Id = s.Id,
            Bearer = s.Bearer,
            PartyId = s.PartyId,
            PartyName = s.PartyId is null ? null : shareParties.GetValueOrDefault(s.PartyId.Value),
            Amount = s.Amount,
            SharePercent = s.SharePercent,
            Justification = s.Justification,
            IsInvoiced = s.IsInvoiced,
        }).ToList();

        return detail;
    }

    /// <summary>
    /// What the agent may spend on this property without asking. The tenancy's own limit beats the
    /// landlord's default, because a specific agreement always beats a general one.
    /// </summary>
    private async Task<decimal?> AuthorityLimitAsync(Guid? propertyId, Guid? tenancyId)
    {
        if (tenancyId is not null)
        {
            var fromTenancy = await Db.Tenancies.ForCompany(Tenant)
                .Where(t => t.Id == tenancyId && t.RepairAuthorityLimit > 0m)
                .Select(t => (decimal?)t.RepairAuthorityLimit)
                .FirstOrDefaultAsync();

            if (fromTenancy is not null) return fromTenancy;
        }

        if (propertyId is null) return null;

        var specific = await Db.RepairAuthorityLimits.ForCompany(Tenant)
            .Where(l => l.PropertyId == propertyId)
            .OrderByDescending(l => l.EffectiveFrom)
            .Select(l => (decimal?)l.LimitAmount)
            .FirstOrDefaultAsync();

        if (specific is not null) return specific;

        var ownerId = await Db.PropertyOwnerships.ForCompany(Tenant)
            .Where(o => o.PropertyId == propertyId && o.ToDate == null && o.IsPrimaryOwner)
            .Select(o => (Guid?)o.PartyId)
            .FirstOrDefaultAsync();

        if (ownerId is null) return null;

        return await Db.Landlords.ForCompany(Tenant)
            .Where(l => l.PartyId == ownerId)
            .Select(l => (decimal?)l.RepairAuthorityLimit)
            .FirstOrDefaultAsync();
    }

    public async Task<WorkOrderDetailDto> CreateWorkOrderAsync(WorkOrderCreateDto dto, Guid userId)
    {
        var now = DateTime.UtcNow;

        var order = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.WorkOrders.ForCompany(Tenant).Include(w => w.Lines).Include(w => w.Photos)
                .FirstOrDefaultAsync(w => w.Id == dto.Id)
            : null;

        if (order is null)
        {
            order = new WorkOrder
            {
                OrderNumber = await numbering.NextWorkOrderNumberAsync(now),
                RaisedAt = now,
                RaisedByUserId = userId,
            }.StampNew(Tenant, userId);

            Db.WorkOrders.Add(order);
        }
        else
        {
            if (order.Status is WorkOrderStatus.SignedOff or WorkOrderStatus.Cancelled)
                throw new InvalidOperationException("This work order is closed and cannot be edited.");

            order.StampUpdated(userId);
        }

        order.Source = dto.Source;
        order.Priority = dto.Priority;
        order.PropertyId = dto.PropertyId;
        order.UnitId = dto.UnitId;
        order.ProjectId = dto.ProjectId;
        order.SocietyId = dto.SocietyId;
        order.FacilityAssetId = dto.FacilityAssetId;
        order.LocationDetail = dto.LocationDetail;
        order.Title = dto.Title;
        order.Description = dto.Description;
        order.Trade = dto.Trade;
        order.TenancyId = dto.TenancyId;
        order.ComplaintId = dto.ComplaintId;
        order.SnagId = dto.SnagId;
        order.DefectClaimId = dto.DefectClaimId;
        order.PpmTaskId = dto.PpmTaskId;
        order.InspectionFindingId = dto.InspectionFindingId;
        order.RaisedByPartyId = dto.RaisedByPartyId;
        order.Access = dto.Access;
        order.KeySetId = dto.KeySetId;
        order.EstimatedCost = dto.EstimatedCost;
        order.CostBearer = dto.CostBearer;
        order.IsEmergencyOverride = dto.IsEmergencyOverride;
        order.Note = dto.Note;

        // The SLA is set from priority at the moment it is raised, so a job that sat in a queue
        // for a day is visibly late rather than quietly re-clocked.
        var (responseHours, completionHours) = SlaFor(dto.Priority);

        order.ResponseDueAt ??= now.AddHours(responseHours);
        order.CompletionDueAt ??= now.AddHours(completionHours);

        // Authorisation. Spending an owner's money above their limit without asking is the single
        // fastest way for a managing agent to lose a portfolio.
        var limit = await AuthorityLimitAsync(dto.PropertyId, dto.TenancyId);

        order.RequiresAuthorisation = limit is not null
            && dto.EstimatedCost > limit
            && dto.CostBearer == CostBearer.Landlord;

        if (order.RequiresAuthorisation && dto.IsEmergencyOverride)
        {
            if (string.IsNullOrWhiteSpace(dto.OverrideReason))
                throw new InvalidOperationException("An emergency override has to say what the emergency is.");

            // An emergency proceeds. It is recorded as an override rather than a normal approval,
            // because the owner will see it on their statement and is entitled to the reason.
            order.IsAuthorised = true;
            order.AuthorisedByUserId = userId;
            order.AuthorisedAt = now;
            order.Status = WorkOrderStatus.Authorised;

            await WriteAuditNoteAsync(
                "WorkOrder", order.Id, "EmergencyAuthorityOverride", Guid.Empty, userId,
                amountImpact: dto.EstimatedCost,
                note: $"{dto.OverrideReason} Estimated {dto.EstimatedCost:N0} against an authority limit of {limit:N0}.",
                entityReference: order.OrderNumber,
                highRisk: true);
        }
        else if (order.RequiresAuthorisation)
        {
            order.Status = WorkOrderStatus.AwaitingAuthorisation;
        }
        else if (order.Status == WorkOrderStatus.Raised)
        {
            order.IsAuthorised = true;
            order.Status = dto.ContractorId is not null || dto.AssignedToUserId is not null
                ? WorkOrderStatus.Assigned
                : WorkOrderStatus.Raised;
        }

        if (dto.Lines.Count > 0)
        {
            Db.WorkOrderLines.RemoveRange(order.Lines);
            AddLines(order, dto.Lines, userId);
        }

        foreach (var photo in dto.Photos.Where(p => order.Photos.All(x => x.Url != p.Url)))
        {
            order.Photos.Add(new WorkOrderPhoto
            {
                WorkOrderId = order.Id,
                Url = photo.Url,
                Stage = photo.Stage,
                CapturedAt = photo.CapturedAt == default ? now : photo.CapturedAt,
                CapturedByUserId = userId,
                Caption = photo.Caption,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();

        if (dto.ContractorId is not null || dto.AssignedToUserId is not null)
        {
            await AssignInternalAsync(order, dto.AssignedToUserId, dto.ContractorId,
                dto.AppointmentFrom, dto.AppointmentTo, userId);
        }

        if (dto.NotifyOccupier && dto.TenancyId is not null)
        {
            var occupier = await Db.TenancyParties.ForCompany(Tenant)
                .Where(p => p.TenancyId == dto.TenancyId && p.IsLeadTenant)
                .Select(p => (Guid?)p.PartyId)
                .FirstOrDefaultAsync();

            if (occupier is not null)
            {
                await QueueNotificationAsync(
                    "WorkOrderRaised",
                    $"Repair logged — {order.OrderNumber}",
                    $"{order.Title}. We will contact you to arrange access.",
                    $"/realestate/work-orders/{order.Id}",
                    recipientPartyId: occupier,
                    entityType: "WorkOrder",
                    entityId: order.Id);

                order.OccupierNotified = true;
            }
        }

        await Db.SaveChangesAsync();
        return (await GetWorkOrderAsync(order.Id))!;
    }

    /// <summary>Response and completion windows by priority, in hours.</summary>
    private static (int Response, int Completion) SlaFor(TicketPriority priority) => priority switch
    {
        TicketPriority.Emergency => (2, 24),
        TicketPriority.High => (8, 72),
        TicketPriority.Normal => (24, 168),
        _ => (72, 720),
    };

    private void AddLines(WorkOrder order, List<WorkOrderLineDto> lines, Guid userId)
    {
        var order2 = 0;

        foreach (var l in lines)
        {
            order.Lines.Add(new WorkOrderLine
            {
                WorkOrderId = order.Id,
                LineType = l.LineType,
                Description = l.Description,
                Quantity = l.Quantity <= 0m ? 1m : l.Quantity,
                Uom = l.Uom,
                Rate = l.Rate,
                Amount = RealEstateMapper.Money(l.Amount > 0m ? l.Amount : (l.Quantity <= 0m ? 1m : l.Quantity) * l.Rate),
                ItemId = l.ItemId,
                WarehouseId = l.WarehouseId,
                StockIssued = l.StockIssued,
                SortOrder = order2 += 10,
            }.StampNew(Tenant, userId));
        }
    }

    public async Task<WorkOrderDetailDto> AssignWorkOrderAsync(
        Guid id, Guid? userId2, Guid? contractorId, DateTime? from, DateTime? to, Guid userId)
    {
        var order = await RequireAsync<WorkOrder>(id, "That work order does not exist.");

        await AssignInternalAsync(order, userId2, contractorId, from, to, userId);
        await Db.SaveChangesAsync();

        return (await GetWorkOrderAsync(id))!;
    }

    private async Task AssignInternalAsync(
        WorkOrder order, Guid? assignedToUserId, Guid? contractorId, DateTime? from, DateTime? to, Guid userId)
    {
        if (order.RequiresAuthorisation && !order.IsAuthorised)
            throw new InvalidOperationException($"{order.OrderNumber} is awaiting the owner's authorisation and cannot be assigned yet.");

        if (contractorId is not null)
        {
            var contractor = await Db.Contractors.ForCompany(Tenant)
                .Include(c => c.Compliance)
                .FirstOrDefaultAsync(c => c.Id == contractorId)
                ?? throw new InvalidOperationException("That contractor does not exist.");

            if (contractor.IsSuspended)
                throw new InvalidOperationException($"{contractor.Name} is suspended: {contractor.SuspensionReason}");

            if (!contractor.IsApproved)
                throw new InvalidOperationException($"{contractor.Name} has not been approved for work. Approve them first.");

            // Sending an uninsured contractor to a client's property is the agency's liability,
            // not the contractor's. It is refused rather than warned about.
            var today = Today;

            var lapsed = contractor.Compliance
                .Where(c => c.BlocksAssignmentWhenExpired && c.ExpiresOn < today)
                .Select(c => $"{c.ComplianceType} expired {c.ExpiresOn:dd MMM yyyy}")
                .ToList();

            if (lapsed.Count > 0)
                throw new InvalidOperationException($"{contractor.Name} cannot be assigned: {string.Join("; ", lapsed)}.");
        }

        order.AssignedToUserId = assignedToUserId;
        order.ContractorId = contractorId;
        order.AssignedAt = DateTime.UtcNow;
        order.AppointmentFrom = from ?? order.AppointmentFrom;
        order.AppointmentTo = to ?? order.AppointmentTo;

        order.Status = from is not null ? WorkOrderStatus.AppointmentSet : WorkOrderStatus.Assigned;
        order.StampUpdated(userId);

        // An appointment the occupier does not know about is an appointment nobody is home for.
        if (from is not null && order.TenancyId is not null)
        {
            var occupier = await Db.TenancyParties.ForCompany(Tenant)
                .Where(p => p.TenancyId == order.TenancyId && p.IsLeadTenant)
                .Select(p => (Guid?)p.PartyId)
                .FirstOrDefaultAsync();

            if (occupier is not null)
            {
                await QueueNotificationAsync(
                    "WorkOrderAppointment",
                    $"Appointment confirmed — {order.OrderNumber}",
                    $"{from:ddd d MMM 'between' HH:mm}{(to is null ? "" : $" and {to:HH:mm}")}. {order.Title}.",
                    $"/realestate/work-orders/{order.Id}",
                    recipientPartyId: occupier,
                    entityType: "WorkOrder",
                    entityId: order.Id);

                order.OccupierNotified = true;
            }
        }
    }

    public async Task<WorkOrderDetailDto> AuthoriseWorkOrderAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId)
    {
        var order = await RequireAsync<WorkOrder>(id, "That work order does not exist.");

        if (order.IsAuthorised)
            throw new InvalidOperationException($"{order.OrderNumber} was already authorised on {order.AuthorisedAt:dd MMM yyyy}.");

        if (outcome is ApprovalOutcome.Approved or ApprovalOutcome.AutoApproved)
        {
            order.IsAuthorised = true;
            order.AuthorisedByUserId = userId;
            order.AuthorisedAt = DateTime.UtcNow;
            order.Status = WorkOrderStatus.Authorised;
        }
        else
        {
            order.Status = WorkOrderStatus.Rejected;
            order.Note = comment ?? order.Note;
        }

        order.StampUpdated(userId);

        await WriteAuditNoteAsync(
            "WorkOrder", id, $"WorkOrder{outcome}", Guid.Empty, userId,
            amountImpact: order.EstimatedCost,
            note: comment,
            entityReference: order.OrderNumber);

        await Db.SaveChangesAsync();
        return (await GetWorkOrderAsync(id))!;
    }

    /// <summary>
    /// Completes the job and settles who pays. The cost split is the interesting part: a repair
    /// caused by the tenant is recharged to them rather than absorbed by the landlord, and the
    /// justification is recorded because that is the conversation that follows.
    /// </summary>
    public async Task<WorkOrderDetailDto> CompleteWorkOrderAsync(WorkOrderCompletionDto dto, Guid userId)
    {
        var order = await Db.WorkOrders.ForCompany(Tenant)
            .Include(w => w.Lines)
            .Include(w => w.Photos)
            .FirstOrDefaultAsync(w => w.Id == dto.WorkOrderId)
            ?? throw new InvalidOperationException("That work order does not exist.");

        if (order.Status is WorkOrderStatus.SignedOff or WorkOrderStatus.Cancelled)
            throw new InvalidOperationException("This work order is already closed.");

        if (string.IsNullOrWhiteSpace(dto.WorkDone))
            throw new InvalidOperationException("Record what was actually done. \"Completed\" is not a record of works.");

        var now = DateTime.UtcNow;

        await using var transaction = await Db.Database.BeginTransactionAsync();

        if (dto.Lines.Count > 0)
        {
            Db.WorkOrderLines.RemoveRange(order.Lines);
            order.Lines.Clear();
            AddLines(order, dto.Lines, userId);
        }

        foreach (var photo in dto.Photos.Where(p => order.Photos.All(x => x.Url != p.Url)))
        {
            order.Photos.Add(new WorkOrderPhoto
            {
                WorkOrderId = order.Id,
                Url = photo.Url,
                Stage = photo.Stage,
                CapturedAt = photo.CapturedAt == default ? now : photo.CapturedAt,
                CapturedByUserId = userId,
                Caption = photo.Caption,
            }.StampNew(Tenant, userId));
        }

        order.StartedAt = dto.StartedAt ?? order.StartedAt ?? now;
        order.CompletedAt = dto.CompletedAt == default ? now : dto.CompletedAt;
        order.LabourHours = dto.LabourHours;
        order.WorkDone = dto.WorkDone;
        order.OccupierSignedOff = dto.OccupierSignedOff;
        order.SignedOffAt = dto.OccupierSignedOff ? now : null;
        order.SignatureUrl = dto.SignatureUrl;
        order.SatisfactionRating = dto.SatisfactionRating;
        order.CostBearer = dto.CostBearer;

        order.LabourCost = RealEstateMapper.Money(order.Lines.Where(l => l.LineType == "Labour").Sum(l => l.Amount));
        order.MaterialCost = RealEstateMapper.Money(order.Lines.Where(l => l.LineType == "Material").Sum(l => l.Amount));
        order.ContractorCost = RealEstateMapper.Money(order.Lines.Where(l => l.LineType == "Contractor").Sum(l => l.Amount));
        order.TotalCost = RealEstateMapper.Money(order.Lines.Sum(l => l.Amount));

        // An overspend against an authorised estimate is a fact the owner will notice on their
        // statement, so it is flagged now rather than found later.
        if (order.EstimatedCost > 0m && order.TotalCost > order.EstimatedCost * 1.1m)
        {
            await WriteAuditNoteAsync(
                "WorkOrder", order.Id, "WorkOrderOverspend", Guid.Empty, userId,
                amountImpact: order.TotalCost - order.EstimatedCost,
                before: order.EstimatedCost.ToString("N0"),
                after: order.TotalCost.ToString("N0"),
                note: $"Final cost exceeded the authorised estimate by {order.TotalCost - order.EstimatedCost:N0}.",
                entityReference: order.OrderNumber,
                highRisk: true);
        }

        order.Status = dto.OccupierSignedOff ? WorkOrderStatus.SignedOff : WorkOrderStatus.Completed;
        order.StampUpdated(userId);

        // The cost split. Where none is given the whole amount falls on the nominated bearer.
        var existing = await Db.WorkOrderCosts.ForCompany(Tenant)
            .Where(c => c.WorkOrderId == order.Id && !c.IsInvoiced)
            .ToListAsync();

        Db.WorkOrderCosts.RemoveRange(existing);

        var shares = dto.CostShares.Count > 0
            ? dto.CostShares
            : [new WorkOrderCostShareDto { Bearer = dto.CostBearer, Amount = order.TotalCost, SharePercent = 100m }];

        var total = shares.Sum(s => s.Amount);

        if (Math.Abs(total - order.TotalCost) > 0.01m && shares.Count > 1)
            throw new InvalidOperationException($"The cost split adds up to {total:N0}, not the {order.TotalCost:N0} the job cost.");

        foreach (var share in shares)
        {
            // Recharging a tenant needs a reason. "The tenant broke it" is a claim, and a claim
            // has to be written down before it is billed.
            if (share.Bearer == CostBearer.Tenant && string.IsNullOrWhiteSpace(share.Justification))
                throw new InvalidOperationException("Recharging a tenant has to record why the cost falls on them.");

            Db.WorkOrderCosts.Add(new WorkOrderCost
            {
                WorkOrderId = order.Id,
                Bearer = share.Bearer,
                PartyId = share.PartyId,
                Amount = share.Amount,
                SharePercent = share.SharePercent > 0m ? share.SharePercent : RealEstateMapper.Percent(share.Amount, order.TotalCost),
                Justification = share.Justification,
            }.StampNew(Tenant, userId));
        }

        order.IsRecharged = shares.Any(s => s.Bearer == CostBearer.Tenant);

        // Everything the work order came from closes with it, so nobody has two open records for
        // one job.
        if (order.ComplaintId is not null)
        {
            var complaint = await Db.Complaints.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == order.ComplaintId);

            if (complaint is not null && complaint.Status is not (TicketStatus.Closed or TicketStatus.Resolved))
            {
                complaint.Status = TicketStatus.Resolved;
                complaint.ResolvedAt = now;
                complaint.Resolution = dto.WorkDone;
                complaint.StampUpdated(userId);
            }
        }

        if (order.PpmTaskId is not null)
        {
            var task = await Db.PpmTasks.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == order.PpmTaskId);

            if (task is not null && task.CompletedOn is null)
            {
                task.CompletedOn = DateOnly.FromDateTime(order.CompletedAt.Value);
                task.Status = "Completed";
                task.Cost = order.TotalCost;
                task.CompletionNote = dto.WorkDone;
                task.StampUpdated(userId);
            }
        }

        if (order.FacilityAssetId is not null)
            await UpdateAssetAfterWorkAsync(order, userId);

        if (order.ContractorId is not null)
            await RefreshContractorStatsAsync(order.ContractorId.Value, userId);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetWorkOrderAsync(order.Id))!;
    }

    private async Task UpdateAssetAfterWorkAsync(WorkOrder order, Guid userId)
    {
        var asset = await Db.FacilityAssets.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == order.FacilityAssetId);
        if (asset is null) return;

        var servicedOn = DateOnly.FromDateTime(order.CompletedAt ?? DateTime.UtcNow);

        asset.LastServicedOn = servicedOn;
        asset.LifetimeMaintenanceCost = RealEstateMapper.Money(asset.LifetimeMaintenanceCost + order.TotalCost);

        // A breakdown is a different event from a service, and the count is what drives a
        // replace-rather-than-repair decision.
        if (order.Source is WorkOrderSource.MeterAlarm or WorkOrderSource.ResidentComplaint or WorkOrderSource.TenantRequest)
            asset.BreakdownCount++;

        asset.OperationalStatus = "Operational";
        asset.StampUpdated(userId);

        Db.AssetServiceRecords.Add(new AssetServiceRecord
        {
            FacilityAssetId = asset.Id,
            ServicedOn = servicedOn,
            ServiceType = order.Source == WorkOrderSource.PlannedMaintenance ? "Routine" : "Breakdown",
            ContractorId = order.ContractorId,
            WorkOrderId = order.Id,
            WorkDone = order.WorkDone,
            Cost = order.TotalCost,
        }.StampNew(Tenant, userId));
    }

    public async Task<WorkOrderDetailDto> CancelWorkOrderAsync(Guid id, Guid reasonCodeId, Guid userId)
    {
        var order = await RequireAsync<WorkOrder>(id, "That work order does not exist.");

        if (order.Status is WorkOrderStatus.SignedOff or WorkOrderStatus.Completed)
            throw new InvalidOperationException("A completed work order cannot be cancelled. Raise a credit instead.");

        order.Status = WorkOrderStatus.Cancelled;
        order.CancelReasonCodeId = reasonCodeId;
        order.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetWorkOrderAsync(id))!;
    }

    /// <summary>Flags everything past its completion window. Run on a schedule.</summary>
    public async Task<int> FlagSlaBreachesAsync()
    {
        var now = DateTime.UtcNow;

        var breached = await Db.WorkOrders.ForCompany(Tenant)
            .Where(w => !w.SlaBreached
                     && w.CompletionDueAt != null && w.CompletionDueAt < now
                     && w.Status != WorkOrderStatus.SignedOff
                     && w.Status != WorkOrderStatus.Completed
                     && w.Status != WorkOrderStatus.Cancelled)
            .ToListAsync();

        foreach (var order in breached)
        {
            order.SlaBreached = true;
            order.StampUpdated(order.AssignedToUserId ?? Guid.Empty);

            await QueueNotificationAsync(
                "WorkOrderSlaBreached",
                $"{order.OrderNumber} is past its completion time",
                $"{order.Title} was due {order.CompletionDueAt:ddd d MMM 'at' HH:mm}.",
                $"/realestate/work-orders/{order.Id}",
                recipientUserId: order.AssignedToUserId,
                entityType: "WorkOrder",
                entityId: order.Id,
                severity: order.Priority == TicketPriority.Emergency ? AlertSeverity.Critical : AlertSeverity.Warning);
        }

        // A breach is a fact about the contractor as well as the job, and it is what a renewal
        // conversation turns on.
        foreach (var contractorId in breached.Where(b => b.ContractorId.HasValue).Select(b => b.ContractorId!.Value).Distinct())
        {
            var contractor = await Db.Contractors.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == contractorId);

            if (contractor is not null)
            {
                contractor.SlaBreachCount += breached.Count(b => b.ContractorId == contractorId);
                contractor.StampUpdated(Guid.Empty);
            }
        }

        await Db.SaveChangesAsync();
        return breached.Count;
    }
}
