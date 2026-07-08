using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Services;

public class RequestForQuotationService : IRequestForQuotationService
{
    private readonly IRequestForQuotationRepository _rfqs;
    private readonly IVendorQuotationRepository _quotations;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IVendorRepository _vendors;
    private readonly IDocumentSequenceService _sequences;
    private readonly ILogger<RequestForQuotationService> _logger;

    public RequestForQuotationService(
        IRequestForQuotationRepository rfqs,
        IVendorQuotationRepository quotations,
        IPurchaseOrderRepository orders,
        IVendorRepository vendors,
        IDocumentSequenceService sequences,
        ILogger<RequestForQuotationService> logger)
    {
        _rfqs      = rfqs;
        _quotations = quotations;
        _orders    = orders;
        _vendors   = vendors;
        _sequences = sequences;
        _logger    = logger;
    }

    public async Task<PaginatedResponse<RequestForQuotationDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _rfqs.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: e =>
                (string.IsNullOrEmpty(search) ||
                    e.RFQNumber.ToLower().Contains(search) ||
                    e.Title.ToLower().Contains(search)) &&
                (pagination.Status == null || (int)e.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "IssueDate"));
        return PaginatedResponse<RequestForQuotationDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<RequestForQuotationDto?> GetByIdAsync(Guid id)
    {
        var r = await _rfqs.GetWithFullDetailsAsync(id);
        return r is null ? null : MapToDto(r);
    }

    public async Task<RequestForQuotationDto?> GetByNumberAsync(string rfqNumber)
    {
        var r = await _rfqs.GetByNumberAsync(rfqNumber);
        return r is null ? null : MapToDto(r);
    }

    public async Task<List<RequestForQuotationDto>> GetByStatusAsync(RFQStatus status)
        => (await _rfqs.GetByStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<RequestForQuotationDto>> GetByRequisitionAsync(Guid requisitionId)
        => (await _rfqs.GetByRequisitionAsync(requisitionId)).Select(MapToDto).ToList();

    public async Task<RequestForQuotationDto> CreateAsync(CreateRFQDto dto)
    {
        var rfq = new RequestForQuotation
        {
            RFQNumber             = await _sequences.GetNextNumberAsync(ProcurementDocumentType.RequestForQuotation),
            Title                 = dto.Title,
            Description           = dto.Description,
            RequisitionId         = dto.RequisitionId,
            IssueDate             = DateTime.UtcNow,
            SubmissionDeadline    = dto.SubmissionDeadline,
            QuotationValidityDate = dto.QuotationValidityDate,
            CurrencyCode          = dto.CurrencyCode,
            DeliveryAddressId     = dto.DeliveryAddressId,
            RequiredDeliveryDate  = dto.RequiredDeliveryDate,
            TermsAndConditions    = dto.TermsAndConditions,
            EvaluationCriteria    = dto.EvaluationCriteria,
            Notes                 = dto.Notes,
            Status                = RFQStatus.Draft
        };

        var lineNumber = 1;
        foreach (var lineDto in dto.Lines)
        {
            rfq.Lines.Add(new RFQLine
            {
                LineNumber            = lineNumber++,
                RequisitionLineId     = lineDto.RequisitionLineId,
                ItemId                = lineDto.ItemId,
                ItemCode              = lineDto.ItemCode,
                ItemDescription       = lineDto.ItemDescription,
                Quantity              = lineDto.Quantity,
                UnitOfMeasureId       = lineDto.UnitOfMeasureId,
                UnitOfMeasureName     = lineDto.UnitOfMeasureName,
                EstimatedUnitPrice    = lineDto.EstimatedUnitPrice,
                ProcurementCategoryId = lineDto.ProcurementCategoryId,
                RequiredDeliveryDate  = lineDto.RequiredDeliveryDate,
                Specifications        = lineDto.Specifications,
                Notes                 = lineDto.Notes
            });
        }

        foreach (var vendorId in dto.VendorIds)
        {
            rfq.InvitedVendors.Add(new RFQVendor
            {
                RFQId    = rfq.Id,
                VendorId = vendorId,
                Status   = RFQVendorStatus.Invited
            });
        }

        await _rfqs.AddAsync(rfq);
        await _rfqs.SaveChangesAsync();
        return MapToDto(rfq);
    }

    public async Task<RequestForQuotationDto> UpdateAsync(Guid id, UpdateRFQDto dto)
    {
        var rfq = await _rfqs.GetWithFullDetailsAsync(id)
            ?? throw new KeyNotFoundException($"RFQ {id} not found");

        if (rfq.Status != RFQStatus.Draft)
            throw new InvalidOperationException($"Cannot update an RFQ in status {rfq.Status}");

        if (dto.Title is not null)               rfq.Title               = dto.Title;
        if (dto.Description is not null)         rfq.Description         = dto.Description;
        if (dto.SubmissionDeadline.HasValue)     rfq.SubmissionDeadline  = dto.SubmissionDeadline.Value;
        if (dto.QuotationValidityDate.HasValue)  rfq.QuotationValidityDate = dto.QuotationValidityDate;
        if (dto.RequiredDeliveryDate.HasValue)   rfq.RequiredDeliveryDate = dto.RequiredDeliveryDate;
        if (dto.TermsAndConditions is not null)  rfq.TermsAndConditions  = dto.TermsAndConditions;
        if (dto.EvaluationCriteria is not null)  rfq.EvaluationCriteria  = dto.EvaluationCriteria;
        if (dto.Notes is not null)               rfq.Notes               = dto.Notes;

        _rfqs.Update(rfq);
        await _rfqs.SaveChangesAsync();
        return MapToDto(rfq);
    }

    public async Task<RequestForQuotationDto> SendToVendorsAsync(Guid id)
    {
        var rfq = await _rfqs.GetWithFullDetailsAsync(id)
            ?? throw new KeyNotFoundException($"RFQ {id} not found");

        if (rfq.Status != RFQStatus.Draft)
            throw new InvalidOperationException($"Cannot send an RFQ in status {rfq.Status}");

        rfq.Status = RFQStatus.Sent;
        _rfqs.Update(rfq);
        await _rfqs.SaveChangesAsync();
        _logger.LogInformation("RFQ {Number} sent to {Count} vendors", rfq.RFQNumber, rfq.InvitedVendors.Count);
        return MapToDto(rfq);
    }

    public async Task<RequestForQuotationDto> CloseAsync(Guid id)
    {
        var rfq = await _rfqs.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"RFQ {id} not found");
        rfq.Status = RFQStatus.Closed;
        _rfqs.Update(rfq);
        await _rfqs.SaveChangesAsync();
        return MapToDto(rfq);
    }

    public async Task<RequestForQuotationDto> CancelAsync(Guid id)
    {
        var rfq = await _rfqs.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"RFQ {id} not found");
        rfq.Status = RFQStatus.Cancelled;
        _rfqs.Update(rfq);
        await _rfqs.SaveChangesAsync();
        return MapToDto(rfq);
    }

    public async Task DeleteAsync(Guid id)
    {
        var rfq = await _rfqs.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"RFQ {id} not found");
        if (rfq.Status != RFQStatus.Draft)
            throw new InvalidOperationException("Only draft RFQs can be deleted");
        _rfqs.SoftDelete(rfq);
        await _rfqs.SaveChangesAsync();
    }

    public async Task<VendorQuotationDto> SubmitQuotationAsync(Guid rfqId, SubmitVendorQuotationDto dto)
    {
        var rfq = await _rfqs.GetWithFullDetailsAsync(rfqId)
            ?? throw new KeyNotFoundException($"RFQ {rfqId} not found");

        if (rfq.Status != RFQStatus.Sent && rfq.Status != RFQStatus.PartiallyReceived)
            throw new InvalidOperationException("Quotations can only be submitted for an active RFQ");

        var quotation = new VendorQuotation
        {
            QuotationNumber          = await _sequences.GetNextNumberAsync(ProcurementDocumentType.PurchaseRequisition),
            VendorQuotationReference = dto.VendorQuotationReference,
            RFQId                    = rfqId,
            VendorId                 = dto.VendorId,
            SubmissionDate           = DateTime.UtcNow,
            ValidUntil               = dto.ValidUntil,
            CurrencyCode             = dto.CurrencyCode,
            ExchangeRate             = dto.ExchangeRate,
            PaymentTerms             = dto.PaymentTerms,
            Incoterm                 = dto.Incoterm,
            IncotermLocation         = dto.IncotermLocation,
            DeliveryLeadTimeDays     = dto.DeliveryLeadTimeDays,
            TermsAndConditions       = dto.TermsAndConditions,
            Notes                    = dto.Notes,
            Status                   = QuotationStatus.Submitted
        };

        var lineNumber = 1;
        foreach (var lineDto in dto.Lines)
        {
            var rfqLine = rfq.Lines.FirstOrDefault(l => l.Id == lineDto.RFQLineId)
                ?? throw new KeyNotFoundException($"RFQ line {lineDto.RFQLineId} not found");

            var subTotal   = rfqLine.Quantity * lineDto.UnitPrice * (1 - lineDto.DiscountPercent / 100);
            var taxAmount  = subTotal * lineDto.TaxPercent / 100;

            quotation.Lines.Add(new VendorQuotationLine
            {
                LineNumber            = lineNumber++,
                RFQLineId             = lineDto.RFQLineId,
                ItemId                = rfqLine.ItemId,
                ItemCode              = rfqLine.ItemCode,
                ItemDescription       = rfqLine.ItemDescription,
                Quantity              = rfqLine.Quantity,
                UnitOfMeasureId       = rfqLine.UnitOfMeasureId,
                UnitOfMeasureName     = rfqLine.UnitOfMeasureName,
                UnitPrice             = lineDto.UnitPrice,
                DiscountPercent       = lineDto.DiscountPercent,
                DiscountAmount        = rfqLine.Quantity * lineDto.UnitPrice * lineDto.DiscountPercent / 100,
                TaxPercent            = lineDto.TaxPercent,
                TaxAmount             = taxAmount,
                SubTotal              = subTotal,
                TotalPrice            = subTotal + taxAmount,
                PromisedDeliveryDate  = lineDto.PromisedDeliveryDate,
                LeadTimeDays          = lineDto.LeadTimeDays,
                IsAlternative         = lineDto.IsAlternative,
                Notes                 = lineDto.Notes
            });
        }

        quotation.SubTotalAmount  = quotation.Lines.Sum(l => l.SubTotal);
        quotation.TaxAmount       = quotation.Lines.Sum(l => l.TaxAmount);
        quotation.DiscountAmount  = quotation.Lines.Sum(l => l.DiscountAmount);
        quotation.TotalAmount     = quotation.SubTotalAmount + quotation.TaxAmount;

        // Insert the quotation (with its lines) directly as an Added entity. Adding it to
        // the tracked RFQ aggregate + Update(rfq) mis-states the new rows (pre-set Guid Ids)
        // as Modified, producing a 0-row UPDATE / phantom concurrency error.
        await _quotations.AddAsync(quotation);

        // Mark the responding vendor and recompute the RFQ receipt status.
        var invited = rfq.InvitedVendors.FirstOrDefault(v => v.VendorId == dto.VendorId);
        if (invited is not null) invited.Status = RFQVendorStatus.Responded;

        var allResponded = rfq.InvitedVendors.All(v =>
            v.Status == RFQVendorStatus.Responded || v.Status == RFQVendorStatus.Declined);
        rfq.Status = allResponded ? RFQStatus.FullyReceived : RFQStatus.PartiallyReceived;

        _rfqs.Update(rfq);
        await _rfqs.SaveChangesAsync();
        return MapQuotationToDto(quotation);
    }

    public async Task<RequestForQuotationDto> EvaluateQuotationsAsync(Guid rfqId, List<EvaluateQuotationDto> evaluations)
    {
        var rfq = await _rfqs.GetWithFullDetailsAsync(rfqId)
            ?? throw new KeyNotFoundException($"RFQ {rfqId} not found");

        foreach (var eval in evaluations)
        {
            var quotation = rfq.VendorQuotations.FirstOrDefault(q => q.Id == eval.QuotationId);
            if (quotation is null) continue;

            quotation.TechnicalScore   = eval.TechnicalScore;
            quotation.CommercialScore  = eval.CommercialScore;
            quotation.OverallScore     = (eval.TechnicalScore + eval.CommercialScore) / 2;
            quotation.IsRecommended    = eval.IsRecommended;
            quotation.Status           = QuotationStatus.UnderEvaluation;
        }

        _rfqs.Update(rfq);
        await _rfqs.SaveChangesAsync();
        return MapToDto(rfq);
    }

    public async Task<PurchaseOrderDto> AwardAndCreatePurchaseOrderAsync(Guid rfqId, Guid quotationId)
    {
        var rfq = await _rfqs.GetWithFullDetailsAsync(rfqId)
            ?? throw new KeyNotFoundException($"RFQ {rfqId} not found");

        var quotation = rfq.VendorQuotations.FirstOrDefault(q => q.Id == quotationId)
            ?? throw new KeyNotFoundException($"Quotation {quotationId} not found");

        // Award the RFQ
        rfq.Status    = RFQStatus.Awarded;
        rfq.AwardedAt = DateTime.UtcNow;
        quotation.Status = QuotationStatus.Accepted;

        foreach (var other in rfq.VendorQuotations.Where(q => q.Id != quotationId))
            other.Status = QuotationStatus.Rejected;

        _rfqs.Update(rfq);
        await _rfqs.SaveChangesAsync();

        // Create PO from awarded quotation
        var order = new PurchaseOrder
        {
            OrderNumber      = await _sequences.GetNextNumberAsync(ProcurementDocumentType.PurchaseOrder),
            VendorId         = quotation.VendorId,
            QuotationId      = quotation.Id,
            RequisitionId    = rfq.RequisitionId,
            OrderDate        = DateTime.UtcNow,
            CurrencyCode     = quotation.CurrencyCode,
            ExchangeRate     = quotation.ExchangeRate,
            PaymentTerms     = quotation.PaymentTerms,
            Incoterm         = quotation.Incoterm,
            IncotermLocation = quotation.IncotermLocation,
            Status           = PurchaseOrderStatus.Draft
        };

        var lineNumber = 1;
        foreach (var qLine in quotation.Lines)
        {
            order.Lines.Add(new PurchaseOrderLine
            {
                LineNumber      = lineNumber++,
                ItemId          = qLine.ItemId,
                ItemCode        = qLine.ItemCode,
                ItemDescription = qLine.ItemDescription,
                Quantity        = qLine.Quantity,
                UnitOfMeasureId = qLine.UnitOfMeasureId,
                UnitPrice       = qLine.UnitPrice,
                DiscountPercent = qLine.DiscountPercent,
                DiscountAmount  = qLine.DiscountAmount,
                TaxPercent      = qLine.TaxPercent,
                TaxAmount       = qLine.TaxAmount,
                SubTotal        = qLine.SubTotal,
                TotalPrice      = qLine.TotalPrice
            });
        }

        order.SubTotalAmount  = order.Lines.Sum(l => l.SubTotal);
        order.TaxAmount       = order.Lines.Sum(l => l.TaxAmount);
        order.DiscountAmount  = order.Lines.Sum(l => l.DiscountAmount);
        order.TotalAmount     = order.SubTotalAmount + order.TaxAmount;
        order.OutstandingAmount = order.TotalAmount;

        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();
        _logger.LogInformation("PO {Number} created from RFQ {RFQ} quotation {Quotation}",
            order.OrderNumber, rfq.RFQNumber, quotation.QuotationNumber);

        return PurchaseOrderService.MapToDtoStatic(order);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static RequestForQuotationDto MapToDto(RequestForQuotation r) => new()
    {
        Id                    = r.Id,
        RFQNumber             = r.RFQNumber,
        Title                 = r.Title,
        Description           = r.Description,
        RequisitionId         = r.RequisitionId,
        RequisitionNumber     = r.Requisition?.RequisitionNumber,
        IssueDate             = r.IssueDate,
        SubmissionDeadline    = r.SubmissionDeadline,
        QuotationValidityDate = r.QuotationValidityDate,
        AwardedAt             = r.AwardedAt,
        Status                = r.Status,
        CurrencyCode          = r.CurrencyCode,
        DeliveryAddressId     = r.DeliveryAddressId,
        RequiredDeliveryDate  = r.RequiredDeliveryDate,
        TermsAndConditions    = r.TermsAndConditions,
        EvaluationCriteria    = r.EvaluationCriteria,
        Notes                 = r.Notes,
        Lines = r.Lines.Select(l => new RFQLineDto
        {
            Id                    = l.Id,
            LineNumber            = l.LineNumber,
            RequisitionLineId     = l.RequisitionLineId,
            ItemId                = l.ItemId,
            ItemCode              = l.ItemCode,
            ItemDescription       = l.ItemDescription,
            Quantity              = l.Quantity,
            UnitOfMeasureId       = l.UnitOfMeasureId,
            UnitOfMeasureName     = l.UnitOfMeasureName,
            EstimatedUnitPrice    = l.EstimatedUnitPrice,
            ProcurementCategoryId = l.ProcurementCategoryId,
            RequiredDeliveryDate  = l.RequiredDeliveryDate,
            Specifications        = l.Specifications,
            Notes                 = l.Notes
        }).ToList(),
        InvitedVendors = r.InvitedVendors.Select(v => new RFQVendorDto
        {
            Id         = v.Id,
            VendorId   = v.VendorId,
            VendorName = v.Vendor?.Name ?? string.Empty,
            Status     = v.Status,
            InvitedAt  = v.InvitedAt,
            RespondedAt = v.RespondedAt,
            Notes      = v.Notes
        }).ToList(),
        VendorQuotations = r.VendorQuotations.Select(MapQuotationToDto).ToList()
    };

    private static VendorQuotationDto MapQuotationToDto(VendorQuotation q) => new()
    {
        Id                       = q.Id,
        QuotationNumber          = q.QuotationNumber,
        VendorQuotationReference = q.VendorQuotationReference,
        RFQId                    = q.RFQId,
        VendorId                 = q.VendorId,
        VendorName               = q.Vendor?.Name,
        SubmissionDate           = q.SubmissionDate,
        ValidUntil               = q.ValidUntil,
        Status                   = q.Status,
        CurrencyCode             = q.CurrencyCode,
        ExchangeRate             = q.ExchangeRate,
        SubTotalAmount           = q.SubTotalAmount,
        TaxAmount                = q.TaxAmount,
        DiscountAmount           = q.DiscountAmount,
        TotalAmount              = q.TotalAmount,
        PaymentTerms             = q.PaymentTerms,
        Incoterm                 = q.Incoterm,
        IncotermLocation         = q.IncotermLocation,
        DeliveryLeadTimeDays     = q.DeliveryLeadTimeDays,
        TechnicalScore           = q.TechnicalScore,
        CommercialScore          = q.CommercialScore,
        OverallScore             = q.OverallScore,
        IsRecommended            = q.IsRecommended,
        RejectionReason          = q.RejectionReason,
        Notes                    = q.Notes,
        Lines = q.Lines.Select(l => new VendorQuotationLineDto
        {
            Id                   = l.Id,
            LineNumber           = l.LineNumber,
            RFQLineId            = l.RFQLineId,
            ItemId               = l.ItemId,
            ItemCode             = l.ItemCode,
            ItemDescription      = l.ItemDescription,
            Quantity             = l.Quantity,
            UnitOfMeasureId      = l.UnitOfMeasureId,
            UnitOfMeasureName    = l.UnitOfMeasureName,
            UnitPrice            = l.UnitPrice,
            DiscountPercent      = l.DiscountPercent,
            DiscountAmount       = l.DiscountAmount,
            TaxPercent           = l.TaxPercent,
            TaxAmount            = l.TaxAmount,
            SubTotal             = l.SubTotal,
            TotalPrice           = l.TotalPrice,
            PromisedDeliveryDate = l.PromisedDeliveryDate,
            LeadTimeDays         = l.LeadTimeDays,
            IsAlternative        = l.IsAlternative,
            Notes                = l.Notes
        }).ToList()
    };
}
