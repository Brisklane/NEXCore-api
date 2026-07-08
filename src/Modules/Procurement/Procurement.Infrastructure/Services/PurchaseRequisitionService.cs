using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Services;

public class PurchaseRequisitionService : IPurchaseRequisitionService
{
    private readonly IPurchaseRequisitionRepository _requisitions;
    private readonly IDocumentSequenceService _sequences;
    private readonly IPurchaseOrderService _orders;
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<PurchaseRequisitionService> _logger;

    public PurchaseRequisitionService(
        IPurchaseRequisitionRepository requisitions,
        IDocumentSequenceService sequences,
        IPurchaseOrderService orders,
        ProcurementDbContext ctx,
        ILogger<PurchaseRequisitionService> logger)
    {
        _requisitions = requisitions;
        _sequences    = sequences;
        _orders       = orders;
        _ctx          = ctx;
        _logger       = logger;
    }

    public async Task<PaginatedResponse<PurchaseRequisitionDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _requisitions.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: e =>
                (string.IsNullOrEmpty(search) ||
                    e.RequisitionNumber.ToLower().Contains(search) ||
                    e.Title.ToLower().Contains(search) ||
                    (e.Description != null && e.Description.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)e.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "CreatedAt"));

        var dtos = items.Select(MapToDto).ToList();
        if (dtos.Count > 0)
        {
            // One query: which requisitions already have a PO (so we can hide "Convert to PO").
            // Note: deliberately NOT filtering by the page's ids in-query — list.Contains(column)
            // hits the EF Core 9 primitive-collection interpreter bug here. Filter in memory instead.
            var converted = (await _ctx.PurchaseOrders
                .Where(o => o.RequisitionId != null && !o.IsDeleted)
                .Select(o => o.RequisitionId!.Value)
                .Distinct()
                .ToListAsync()).ToHashSet();
            foreach (var d in dtos) d.HasPurchaseOrder = converted.Contains(d.Id);
        }
        return PaginatedResponse<PurchaseRequisitionDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PurchaseRequisitionDto?> GetByIdAsync(Guid id)
    {
        var r = await _requisitions.GetWithLinesAsync(id);
        if (r is null) return null;
        var dto = MapToDto(r);
        dto.HasPurchaseOrder = await _ctx.PurchaseOrders.AnyAsync(o => o.RequisitionId == id && !o.IsDeleted);
        return dto;
    }

    public async Task<PurchaseRequisitionDto?> GetByNumberAsync(string requisitionNumber)
    {
        var r = await _requisitions.GetByNumberAsync(requisitionNumber);
        return r is null ? null : MapToDto(r);
    }

    public async Task<List<PurchaseRequisitionDto>> GetByStatusAsync(RequisitionStatus status)
        => (await _requisitions.GetByStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<PurchaseRequisitionDto>> GetByRequesterAsync(Guid requestedByUserId)
        => (await _requisitions.GetByRequesterAsync(requestedByUserId)).Select(MapToDto).ToList();

    public async Task<List<PurchaseRequisitionDto>> GetByDepartmentAsync(Guid departmentId)
        => (await _requisitions.GetByDepartmentAsync(departmentId)).Select(MapToDto).ToList();

    public async Task<List<PurchaseRequisitionDto>> GetPendingApprovalAsync()
        => (await _requisitions.GetPendingApprovalAsync()).Select(MapToDto).ToList();

    public async Task<PurchaseRequisitionDto> CreateAsync(CreatePurchaseRequisitionDto dto)
    {
        var requisition = new PurchaseRequisition
        {
            RequisitionNumber    = await _sequences.GetNextNumberAsync(ProcurementDocumentType.PurchaseRequisition),
            Title                = dto.Title,
            Description          = dto.Description,
            RequestedByUserId    = dto.RequestedByUserId,
            RequestedByName      = dto.RequestedByName,
            DepartmentId         = dto.DepartmentId,
            DepartmentName       = dto.DepartmentName,
            CostCenterId         = dto.CostCenterId,
            RequiredByDate       = dto.RequiredByDate,
            Priority             = dto.Priority,
            SuggestedVendorId    = dto.SuggestedVendorId,
            CurrencyCode         = dto.CurrencyCode,
            BudgetId             = dto.BudgetId,
            Notes                = dto.Notes,
            InternalNotes        = dto.InternalNotes,
            Status               = RequisitionStatus.Draft,
            RequestDate          = DateTime.UtcNow
        };

        foreach (var lineDto in dto.Lines)
        {
            requisition.Lines.Add(new PurchaseRequisitionLine
            {
                LineNumber           = dto.Lines.IndexOf(lineDto) + 1,
                ItemId               = lineDto.ItemId,
                ItemCode             = lineDto.ItemCode,
                ItemDescription      = lineDto.ItemDescription,
                Quantity             = lineDto.Quantity,
                UnitOfMeasureId      = lineDto.UnitOfMeasureId,
                UnitOfMeasureName    = lineDto.UnitOfMeasureName,
                EstimatedUnitPrice   = lineDto.EstimatedUnitPrice,
                EstimatedTotalPrice  = lineDto.EstimatedUnitPrice.HasValue
                    ? lineDto.EstimatedUnitPrice.Value * lineDto.Quantity
                    : null,
                ProcurementCategoryId = lineDto.ProcurementCategoryId,
                RequiredByDate       = lineDto.RequiredByDate,
                DeliveryLocationId   = lineDto.DeliveryLocationId,
                SuggestedVendorId    = lineDto.SuggestedVendorId,
                Notes                = lineDto.Notes
            });
        }

        requisition.EstimatedTotalAmount = requisition.Lines
            .Sum(l => l.EstimatedTotalPrice ?? 0);

        await _requisitions.AddAsync(requisition);
        await _requisitions.SaveChangesAsync();
        return MapToDto(requisition);
    }

    public async Task<PurchaseRequisitionDto> UpdateAsync(Guid id, UpdatePurchaseRequisitionDto dto)
    {
        var r = await _requisitions.GetWithLinesAsync(id)
            ?? throw new KeyNotFoundException($"Requisition {id} not found");

        if (r.Status != RequisitionStatus.Draft)
            throw new InvalidOperationException($"Cannot update a requisition in status {r.Status}");

        if (dto.Title is not null)             r.Title             = dto.Title;
        if (dto.Description is not null)       r.Description       = dto.Description;
        if (dto.RequiredByDate.HasValue)       r.RequiredByDate    = dto.RequiredByDate;
        if (dto.Priority.HasValue)             r.Priority          = dto.Priority.Value;
        if (dto.SuggestedVendorId.HasValue)    r.SuggestedVendorId = dto.SuggestedVendorId;
        if (dto.Notes is not null)             r.Notes             = dto.Notes;
        if (dto.InternalNotes is not null)     r.InternalNotes     = dto.InternalNotes;

        if (dto.Lines is not null)
        {
            // Replace lines on the DbSet directly so new rows are tracked as Added (not
            // Modified via Update(graph), which throws a phantom 0-row concurrency error).
            _ctx.Set<PurchaseRequisitionLine>().RemoveRange(r.Lines);
            var lineNumber = 1;
            decimal total = 0;
            foreach (var lineDto in dto.Lines)
            {
                var estTotal = lineDto.EstimatedUnitPrice.HasValue
                    ? lineDto.EstimatedUnitPrice.Value * lineDto.Quantity
                    : (decimal?)null;
                total += estTotal ?? 0;
                _ctx.Set<PurchaseRequisitionLine>().Add(new PurchaseRequisitionLine
                {
                    RequisitionId        = r.Id,
                    LineNumber           = lineNumber++,
                    ItemId               = lineDto.ItemId,
                    ItemCode             = lineDto.ItemCode,
                    ItemDescription      = lineDto.ItemDescription,
                    Quantity             = lineDto.Quantity,
                    UnitOfMeasureId      = lineDto.UnitOfMeasureId,
                    UnitOfMeasureName    = lineDto.UnitOfMeasureName,
                    EstimatedUnitPrice   = lineDto.EstimatedUnitPrice,
                    EstimatedTotalPrice  = estTotal,
                    ProcurementCategoryId = lineDto.ProcurementCategoryId,
                    RequiredByDate       = lineDto.RequiredByDate,
                    DeliveryLocationId   = lineDto.DeliveryLocationId,
                    SuggestedVendorId    = lineDto.SuggestedVendorId,
                    Notes                = lineDto.Notes
                });
            }
            r.EstimatedTotalAmount = total;
        }

        // r is tracked (loaded with Include) — header changes persist without Update(graph).
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<PurchaseRequisitionDto> SubmitAsync(Guid id)
    {
        var r = await _requisitions.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Requisition {id} not found");

        if (r.Status != RequisitionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit a requisition in status {r.Status}");

        r.Status      = RequisitionStatus.Submitted;
        r.SubmittedAt = DateTime.UtcNow;

        _requisitions.Update(r);
        await _requisitions.SaveChangesAsync();
        return MapToDto(r);
    }

    public async Task<PurchaseRequisitionDto> ApproveAsync(Guid id, Guid approvedByUserId)
    {
        var r = await _requisitions.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Requisition {id} not found");

        if (r.Status != RequisitionStatus.Submitted && r.Status != RequisitionStatus.UnderApproval)
            throw new InvalidOperationException($"Cannot approve a requisition in status {r.Status}");

        r.Status     = RequisitionStatus.Approved;
        r.ApprovedAt = DateTime.UtcNow;

        _requisitions.Update(r);
        await _requisitions.SaveChangesAsync();

        // Auto-create draft purchase order(s) so the buyer just reviews/confirms.
        // Never let a failure here roll back the approval.
        try
        {
            await AutoCreateDraftPosFromRequisitionAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-create draft PO from approved requisition {Number} failed", r.RequisitionNumber);
        }

        _logger.LogInformation("Requisition {Number} approved by {UserId}", r.RequisitionNumber, approvedByUserId);
        return MapToDto(r);
    }

    /// <summary>
    /// On approval, build draft PO(s) from the requisition. Vendor comes from the line's, then the
    /// header's, SuggestedVendorId; lines are grouped into one draft PO per vendor (a PO must have a
    /// vendor). Estimated unit prices carry over as the PO's starting prices. Lines with no resolvable
    /// vendor are left for the buyer to assign a vendor and convert manually. Idempotent.
    /// </summary>
    private async Task AutoCreateDraftPosFromRequisitionAsync(Guid requisitionId)
    {
        var r = await _requisitions.GetWithLinesAsync(requisitionId);
        if (r is null || r.Lines.Count == 0) return;

        // Idempotent — don't auto-create if this requisition already has a purchase order.
        if (await _ctx.PurchaseOrders.AnyAsync(o => o.RequisitionId == requisitionId && !o.IsDeleted))
            return;

        var byVendor = r.Lines
            .Where(l => (l.SuggestedVendorId ?? r.SuggestedVendorId) is not null && l.QuantityRemaining > 0)
            .GroupBy(l => (l.SuggestedVendorId ?? r.SuggestedVendorId)!.Value);

        var created = 0;
        foreach (var group in byVendor)
        {
            var dto = new CreatePurchaseOrderDto
            {
                VendorId             = group.Key,
                RequisitionId        = r.Id,
                BudgetId             = r.BudgetId,
                CurrencyCode         = r.CurrencyCode ?? "PKR",
                OrderDate            = DateTime.UtcNow,
                ExpectedDeliveryDate = r.RequiredByDate,
                Notes                = $"Auto-created from requisition {r.RequisitionNumber}",
                Lines = group
                    .OrderBy(l => l.LineNumber)
                    .Select(l => new CreatePurchaseOrderLineDto
                    {
                        RequisitionLineId     = l.Id,
                        ItemId                = l.ItemId,
                        ItemCode              = l.ItemCode,
                        ItemDescription       = l.ItemDescription,
                        Quantity              = l.Quantity,
                        UnitOfMeasureId       = l.UnitOfMeasureId,
                        UnitOfMeasureName     = l.UnitOfMeasureName,
                        UnitPrice             = l.EstimatedUnitPrice ?? 0m,   // estimate → starting price
                        ProcurementCategoryId = l.ProcurementCategoryId,
                        DeliveryLocationId    = l.DeliveryLocationId,
                        ExpectedDeliveryDate  = l.RequiredByDate,
                        Notes                 = l.Notes,
                    })
                    .ToList(),
            };
            await _orders.CreateAsync(dto);
            created++;
        }

        if (created > 0)
            _logger.LogInformation("Auto-created {Count} draft PO(s) from approved requisition {Number}", created, r.RequisitionNumber);
    }

    public async Task<PurchaseRequisitionDto> RejectAsync(Guid id, RejectRequisitionDto dto, Guid rejectedByUserId)
    {
        var r = await _requisitions.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Requisition {id} not found");

        if (r.Status != RequisitionStatus.Submitted && r.Status != RequisitionStatus.UnderApproval)
            throw new InvalidOperationException($"Cannot reject a requisition in status {r.Status}");

        r.Status          = RequisitionStatus.Rejected;
        r.RejectedAt      = DateTime.UtcNow;
        r.RejectionReason = dto.RejectionReason;

        _requisitions.Update(r);
        await _requisitions.SaveChangesAsync();
        return MapToDto(r);
    }

    public async Task<PurchaseRequisitionDto> CancelAsync(Guid id)
    {
        var r = await _requisitions.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Requisition {id} not found");

        if (r.Status == RequisitionStatus.Fulfilled)
            throw new InvalidOperationException("Cannot cancel a fulfilled requisition");

        r.Status = RequisitionStatus.Cancelled;
        _requisitions.Update(r);
        await _requisitions.SaveChangesAsync();
        return MapToDto(r);
    }

    public async Task<PurchaseOrderDto> ConvertToPurchaseOrderAsync(Guid id)
    {
        var r = await _requisitions.GetWithLinesAsync(id)
            ?? throw new KeyNotFoundException($"Requisition {id} not found");

        if (r.Status != RequisitionStatus.Approved)
            throw new InvalidOperationException(
                $"Only approved requisitions can be converted to a purchase order (current status: {r.Status}).");

        if (r.SuggestedVendorId is null)
            throw new InvalidOperationException(
                "This requisition has no suggested vendor. Set a suggested vendor before converting it to a purchase order.");

        if (r.Lines.Count == 0)
            throw new InvalidOperationException("Cannot create a purchase order from a requisition with no lines.");

        // A draft PO may already have been auto-created on approval — don't duplicate it.
        if (await _ctx.PurchaseOrders.AnyAsync(o => o.RequisitionId == r.Id && !o.IsDeleted))
            throw new InvalidOperationException(
                "This requisition has already been converted to a purchase order. Edit the existing draft PO instead.");

        // Build a draft PO from the requisition. Estimated unit prices carry over as the
        // starting prices — the buyer confirms vendor pricing/terms on the draft before sending.
        var orderDto = new CreatePurchaseOrderDto
        {
            VendorId             = r.SuggestedVendorId.Value,
            RequisitionId        = r.Id,
            BudgetId             = r.BudgetId,
            CurrencyCode         = r.CurrencyCode ?? "USD",
            OrderDate            = DateTime.UtcNow,
            ExpectedDeliveryDate = r.RequiredByDate,
            Notes                = r.Notes,
            Lines = r.Lines
                .OrderBy(l => l.LineNumber)
                .Select(l => new CreatePurchaseOrderLineDto
                {
                    RequisitionLineId     = l.Id,
                    ItemId                = l.ItemId,
                    ItemCode              = l.ItemCode,
                    ItemDescription       = l.ItemDescription,
                    Quantity              = l.Quantity,
                    UnitOfMeasureId       = l.UnitOfMeasureId,
                    UnitOfMeasureName     = l.UnitOfMeasureName,
                    UnitPrice             = l.EstimatedUnitPrice ?? 0m,
                    ProcurementCategoryId = l.ProcurementCategoryId,
                    DeliveryLocationId    = l.DeliveryLocationId,
                    ExpectedDeliveryDate  = l.RequiredByDate,
                    Notes                 = l.Notes
                })
                .ToList()
        };

        var order = await _orders.CreateAsync(orderDto);

        // Mark the requisition (and its lines) as fully ordered so it can't be converted twice.
        r.Status = RequisitionStatus.Fulfilled;
        foreach (var line in r.Lines)
        {
            line.QuantityOrdered = line.Quantity;
            line.LineStatus      = RequisitionLineStatus.Fulfilled;
        }
        await _ctx.SaveChangesAsync();

        _logger.LogInformation("Converted requisition {ReqNumber} into purchase order {OrderNumber}",
            r.RequisitionNumber, order.OrderNumber);

        return order;
    }

    public async Task DeleteAsync(Guid id)
    {
        var r = await _requisitions.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Requisition {id} not found");

        if (r.Status != RequisitionStatus.Draft)
            throw new InvalidOperationException("Only draft requisitions can be deleted");

        _requisitions.SoftDelete(r);
        await _requisitions.SaveChangesAsync();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static PurchaseRequisitionDto MapToDto(PurchaseRequisition r) => new()
    {
        Id                   = r.Id,
        RequisitionNumber    = r.RequisitionNumber,
        Title                = r.Title,
        Description          = r.Description,
        RequestedByUserId    = r.RequestedByUserId,
        RequestedByName      = r.RequestedByName,
        DepartmentId         = r.DepartmentId,
        DepartmentName       = r.DepartmentName,
        CostCenterId         = r.CostCenterId,
        RequestDate          = r.RequestDate,
        RequiredByDate       = r.RequiredByDate,
        SubmittedAt          = r.SubmittedAt,
        ApprovedAt           = r.ApprovedAt,
        RejectedAt           = r.RejectedAt,
        Status               = r.Status,
        Priority             = r.Priority,
        SuggestedVendorId    = r.SuggestedVendorId,
        SuggestedVendorName  = r.SuggestedVendor?.Name,
        CurrencyCode         = r.CurrencyCode,
        EstimatedTotalAmount = r.EstimatedTotalAmount,
        BudgetId             = r.BudgetId,
        IsBudgetChecked      = r.IsBudgetChecked,
        IsBudgetAvailable    = r.IsBudgetAvailable,
        RejectionReason      = r.RejectionReason,
        Notes                = r.Notes,
        InternalNotes        = r.InternalNotes,
        Lines = r.Lines.Select(l => new PurchaseRequisitionLineDto
        {
            Id                    = l.Id,
            LineNumber            = l.LineNumber,
            ItemId                = l.ItemId,
            ItemCode              = l.ItemCode,
            ItemDescription       = l.ItemDescription,
            Quantity              = l.Quantity,
            UnitOfMeasureId       = l.UnitOfMeasureId,
            UnitOfMeasureName     = l.UnitOfMeasureName,
            EstimatedUnitPrice    = l.EstimatedUnitPrice,
            EstimatedTotalPrice   = l.EstimatedTotalPrice,
            ProcurementCategoryId = l.ProcurementCategoryId,
            RequiredByDate        = l.RequiredByDate,
            DeliveryLocationId    = l.DeliveryLocationId,
            LineStatus            = l.LineStatus,
            QuantityOrdered       = l.QuantityOrdered,
            QuantityRemaining     = l.Quantity - l.QuantityOrdered,
            SuggestedVendorId     = l.SuggestedVendorId,
            SuggestedVendorName   = l.SuggestedVendor?.Name,
            Notes                 = l.Notes
        }).ToList()
    };
}
