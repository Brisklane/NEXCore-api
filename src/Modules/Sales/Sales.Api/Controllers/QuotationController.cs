using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>Quotation (Sales Quote) management</summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class QuotationController : ControllerBase
{
    private readonly IQuotationRepository _quotations;
    private readonly IDocumentSequenceService _sequences;
    private readonly ILogger<QuotationController> _logger;

    public QuotationController(
        IQuotationRepository quotations,
        IDocumentSequenceService sequences,
        ILogger<QuotationController> logger)
    {
        _quotations = quotations;
        _sequences  = sequences;
        _logger     = logger;
    }

    /// <summary>Get all quotations</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<QuotationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _quotations.GetAllAsync();
            return Ok(new ApiResponse<List<QuotationDto>> { Success = true, Data = list.OrderByDescending(q => q.CreatedAt).Select(MapToDto).ToList(), Message = "Quotations retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving quotations"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving quotations" }); }
    }

    /// <summary>Get quotation by ID (with lines)</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<QuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var q = await _quotations.GetWithLinesAsync(id);
            if (q == null) return NotFound(new ApiErrorResponse { Message = "Quotation not found" });
            return Ok(new ApiResponse<QuotationDto> { Success = true, Data = MapToDto(q), Message = "Quotation retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving quotation {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving quotation" }); }
    }

    /// <summary>Get quotation by number</summary>
    [HttpGet("by-number/{quotationNumber}")]
    [ProducesResponseType(typeof(ApiResponse<QuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string quotationNumber)
    {
        try
        {
            var q = await _quotations.GetByNumberAsync(quotationNumber);
            if (q == null) return NotFound(new ApiErrorResponse { Message = "Quotation not found" });
            return Ok(new ApiResponse<QuotationDto> { Success = true, Data = MapToDto(q), Message = "Quotation retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving quotation {Number}", quotationNumber); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving quotation" }); }
    }

    /// <summary>Get quotations by customer</summary>
    [HttpGet("by-customer/{contactId}")]
    [ProducesResponseType(typeof(ApiResponse<List<QuotationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer(Guid contactId)
    {
        try
        {
            var list = await _quotations.GetByContactAsync(contactId);
            return Ok(new ApiResponse<List<QuotationDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Customer quotations" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving quotations for customer"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving quotations" }); }
    }

    /// <summary>Get quotations by status</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<QuotationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(QuotationStatus status)
    {
        try
        {
            var list = await _quotations.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<QuotationDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = $"Quotations with status '{status}'" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving quotations by status"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving quotations" }); }
    }

    /// <summary>Create a new quotation</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<QuotationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateQuotationDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            if (dto.ContactId == null || dto.ContactId == Guid.Empty)
                return BadRequest(new ApiErrorResponse { Message = "Customer (ContactId) is required" });
            if (string.IsNullOrWhiteSpace(dto.ContactName))
                return BadRequest(new ApiErrorResponse { Message = "Customer name (ContactName) is required" });

            var qtSeq = await _sequences.GetNextNumberAsync(DocumentType.Quotation);
            var quotation = new Quotation
            {
                QuotationNumber = qtSeq.Code,
                Code            = qtSeq.Code,
                CodeInt         = qtSeq.CodeInt,
                QuotationName = dto.QuotationName,
                ContactId = dto.ContactId,
                ContactName = dto.ContactName,
                ValidUntil = dto.ValidUntil,
                CurrencyCode = dto.CurrencyCode,
                ExchangeRate = dto.ExchangeRate,
                PriceListId = dto.PriceListId,
                PaymentTerms = dto.PaymentTerms,
                Incoterm = dto.Incoterm,
                IncotermLocation = dto.IncotermLocation,
                RequestedDeliveryDate = dto.RequestedDeliveryDate,
                ShipToAddressId = dto.ShipToAddressId,
                ShippingMethod = dto.ShippingMethod,
                SalesRepId = dto.SalesRepId,
                SalesTerritoryId = dto.SalesTerritoryId,
                CrmDealId = dto.CrmDealId,
                CustomerPONumber = dto.CustomerPONumber,
                Notes = dto.Notes,
                TermsAndConditions = dto.TermsAndConditions,
                Status = QuotationStatus.Draft,
                Lines = dto.Lines.Select((l, i) => new QuotationLine
                {
                    LineNumber = i + 1,
                    ProductId = l.ProductId,
                    ProductCode = l.ProductCode,
                    ProductName = l.ProductName,
                    ProductDescription = l.ProductDescription,
                    Quantity = l.Quantity,
                    UnitOfMeasure = l.UnitOfMeasure,
                    UnitPrice = l.UnitPrice,
                    DiscountPercentage = l.DiscountPercentage,
                    DiscountAmount = l.DiscountAmount,
                    NetUnitPrice = l.UnitPrice - (l.UnitPrice * l.DiscountPercentage / 100m),
                    LineAmount = (l.UnitPrice - l.DiscountAmount) * l.Quantity,
                    TaxCategory = l.TaxCategory,
                    RequestedDeliveryDate = l.RequestedDeliveryDate,
                    Notes = l.Notes,
                    TotalAmount = (l.UnitPrice * l.Quantity) - l.DiscountAmount,
                }).ToList(),
            };

            quotation.TotalAmount = quotation.Lines.Sum(l => l.TotalAmount);

            await _quotations.AddAsync(quotation);
            await _quotations.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = quotation.Id },
                new ApiResponse<QuotationDto> { Success = true, Data = MapToDto(quotation), Message = "Quotation created" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating quotation"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating quotation" }); }
    }

    /// <summary>Send quotation to customer (Draft ? Sent)</summary>
    [HttpPost("{id}/send")]
    [ProducesResponseType(typeof(ApiResponse<QuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Send(Guid id)
    {
        try
        {
            var q = await _quotations.GetByIdAsync(id);
            if (q == null) return NotFound(new ApiErrorResponse { Message = "Quotation not found" });
            if (q.Status != QuotationStatus.Draft) return BadRequest(new ApiErrorResponse { Message = "Only Draft quotations can be sent" });
            q.Status = QuotationStatus.Sent;
            q.SentDate = DateTime.UtcNow;
            _quotations.Update(q);
            await _quotations.SaveChangesAsync();
            return Ok(new ApiResponse<QuotationDto> { Success = true, Data = MapToDto(q), Message = "Quotation sent" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error sending quotation {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error sending quotation" }); }
    }

    /// <summary>Convert quotation to sales order (Accepted)</summary>
    [HttpPost("{id}/accept")]
    [ProducesResponseType(typeof(ApiResponse<QuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Accept(Guid id)
    {
        try
        {
            var q = await _quotations.GetByIdAsync(id);
            if (q == null) return NotFound(new ApiErrorResponse { Message = "Quotation not found" });
            if (q.Status != QuotationStatus.Sent) return BadRequest(new ApiErrorResponse { Message = "Only Sent quotations can be accepted" });
            q.Status = QuotationStatus.Accepted;
            q.AcceptedDate = DateTime.UtcNow;
            _quotations.Update(q);
            await _quotations.SaveChangesAsync();
            return Ok(new ApiResponse<QuotationDto> { Success = true, Data = MapToDto(q), Message = "Quotation accepted" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error accepting quotation {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error accepting quotation" }); }
    }

    /// <summary>Delete quotation (only Draft allowed)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var q = await _quotations.GetByIdAsync(id);
            if (q == null) return NotFound(new ApiErrorResponse { Message = "Quotation not found" });
            if (q.Status != QuotationStatus.Draft) return BadRequest(new ApiErrorResponse { Message = "Only Draft quotations can be deleted" });
            _quotations.Delete(q);
            await _quotations.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Quotation deleted" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting quotation {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting quotation" }); }
    }

    private static QuotationDto MapToDto(Quotation q) => new()
    {
        Id = q.Id,
        QuotationNumber = q.QuotationNumber,
        QuotationName = q.QuotationName,
        ContactId = q.ContactId,
        ContactName = q.ContactName,
        Status = q.Status,
        QuotationDate = q.QuotationDate,
        ValidUntil = q.ValidUntil,
        SentDate = q.SentDate,
        AcceptedDate = q.AcceptedDate,
        PriceListId = q.PriceListId,
        CurrencyCode = q.CurrencyCode,
        ExchangeRate = q.ExchangeRate,
        PaymentTerms = q.PaymentTerms,
        Incoterm = q.Incoterm,
        IncotermLocation = q.IncotermLocation,
        RequestedDeliveryDate = q.RequestedDeliveryDate,
        ShipToAddressId = q.ShipToAddressId,
        ShippingMethod = q.ShippingMethod,
        SubtotalAmount = q.SubtotalAmount,
        DiscountAmount = q.DiscountAmount,
        TaxAmount = q.TaxAmount,
        ShippingAmount = q.ShippingAmount,
        TotalAmount = q.TotalAmount,
        SalesRepId = q.SalesRepId,
        SalesTerritoryId = q.SalesTerritoryId,
        CrmDealId = q.CrmDealId,
        ConvertedToSalesOrderId = q.ConvertedToSalesOrderId,
        CustomerPONumber = q.CustomerPONumber,
        Notes = q.Notes,
        TermsAndConditions = q.TermsAndConditions,
        Lines = q.Lines.Select(l => new QuotationLineDto
        {
            Id = l.Id,
            LineNumber = l.LineNumber,
            ProductId = l.ProductId,
            ProductCode = l.ProductCode,
            ProductName = l.ProductName,
            ProductDescription = l.ProductDescription,
            Quantity = l.Quantity,
            UnitOfMeasure = l.UnitOfMeasure,
            UnitPrice = l.UnitPrice,
            DiscountPercentage = l.DiscountPercentage,
            DiscountAmount = l.DiscountAmount,
            NetUnitPrice = l.NetUnitPrice,
            LineAmount = l.LineAmount,
            TaxCategory = l.TaxCategory,
            TaxRate = l.TaxRate,
            TaxAmount = l.TaxAmount,
            TotalAmount = l.TotalAmount,
            RequestedDeliveryDate = l.RequestedDeliveryDate,
            ConfirmedDeliveryDate = l.ConfirmedDeliveryDate,
            Notes = l.Notes,
        }).ToList(),
    };
}
