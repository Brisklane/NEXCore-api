using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;
using System.Security.Claims;

namespace Sales.Api.Controllers;

/// <summary>Delivery management</summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryRepository _deliveries;
    private readonly IDocumentSequenceService _sequences;
    private readonly IEventPublisher _events;
    private readonly ILogger<DeliveryController> _logger;

    public DeliveryController(
        IDeliveryRepository deliveries,
        IDocumentSequenceService sequences,
        IEventPublisher events,
        ILogger<DeliveryController> logger)
    {
        _deliveries = deliveries;
        _sequences  = sequences;
        _events     = events;
        _logger     = logger;
    }

    /// <summary>Get all deliveries</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DeliveryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _deliveries.GetAllAsync();
            return Ok(new ApiResponse<List<DeliveryDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Deliveries retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving deliveries"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving deliveries" }); }
    }

    /// <summary>Get delivery by ID (with lines)</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var delivery = await _deliveries.GetWithLinesAsync(id);
            if (delivery == null) return NotFound(new ApiErrorResponse { Message = "Delivery not found" });
            return Ok(new ApiResponse<DeliveryDto> { Success = true, Data = MapToDto(delivery), Message = "Delivery retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving delivery {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving delivery" }); }
    }

    /// <summary>Get delivery by number</summary>
    [HttpGet("by-number/{deliveryNumber}")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string deliveryNumber)
    {
        try
        {
            var delivery = await _deliveries.GetByNumberAsync(deliveryNumber);
            if (delivery == null) return NotFound(new ApiErrorResponse { Message = "Delivery not found" });
            return Ok(new ApiResponse<DeliveryDto> { Success = true, Data = MapToDto(delivery), Message = "Delivery retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving delivery {Number}", deliveryNumber); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving delivery" }); }
    }

    /// <summary>Get deliveries for a specific sales order</summary>
    [HttpGet("by-order/{salesOrderId}")]
    [ProducesResponseType(typeof(ApiResponse<List<DeliveryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid salesOrderId)
    {
        try
        {
            var list = await _deliveries.GetByOrderAsync(salesOrderId);
            return Ok(new ApiResponse<List<DeliveryDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Order deliveries" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving deliveries for order"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving deliveries" }); }
    }

    /// <summary>Get deliveries by status</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<DeliveryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(DeliveryStatus status)
    {
        try
        {
            var list = await _deliveries.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<DeliveryDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = $"Deliveries with status '{status}'" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving deliveries by status"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving deliveries" }); }
    }

    /// <summary>Create a delivery for a sales order</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DeliveryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var delSeq = await _sequences.GetNextNumberAsync(DocumentType.Delivery);
            var delivery = new Delivery
            {
                DeliveryNumber = delSeq.Code,
                Code           = delSeq.Code,
                CodeInt        = delSeq.CodeInt,
                SalesOrderId = dto.SalesOrderId,
                ContactId = dto.ContactId,
                PlannedDeliveryDate = dto.PlannedDeliveryDate,
                WarehouseId = dto.WarehouseId,
                Carrier = dto.Carrier,
                ShippingMethod = dto.ShippingMethod,
                Incoterm = dto.Incoterm,
                IncotermLocation = dto.IncotermLocation,
                RecipientName = dto.RecipientName,
                RecipientPhone = dto.RecipientPhone,
                RecipientWhatsApp = dto.RecipientWhatsApp,
                Street = dto.Street,
                City = dto.City,
                State = dto.State,
                PostalCode = dto.PostalCode,
                Country = dto.Country,
                DeliveryLatitude = dto.DeliveryLatitude,
                DeliveryLongitude = dto.DeliveryLongitude,
                DeliveryNotes = dto.DeliveryNotes,
                AssignedRiderId = dto.AssignedRiderId,
                NumberOfPackages = dto.NumberOfPackages,
                TotalWeight = dto.TotalWeight,
                WeightUnit = dto.WeightUnit,
                Notes = dto.Notes,
                Status = DeliveryStatus.Draft,
                Lines = dto.Lines.Select((l, i) => new DeliveryLine
                {
                    LineNumber = i + 1,
                    SalesOrderLineId = l.SalesOrderLineId,
                    DeliveredQuantity = l.DeliveredQuantity,
                    BinLocation = l.BinLocation,
                    LotNumber = l.LotNumber,
                    SerialNumber = l.SerialNumber,
                }).ToList(),
            };

            await _deliveries.AddAsync(delivery);
            await _deliveries.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = delivery.Id },
                new ApiResponse<DeliveryDto> { Success = true, Data = MapToDto(delivery), Message = "Delivery created" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating delivery"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating delivery" }); }
    }

    /// <summary>Ship delivery — transitions to Shipped status and fires the COGS accounting event and the inventory stock-deduction event.</summary>
    [HttpPost("{id}/ship")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ship(Guid id, [FromBody] ShipDeliveryDto dto)
    {
        try
        {
            var delivery = await _deliveries.GetWithLinesAsync(id);
            if (delivery == null) return NotFound(new ApiErrorResponse { Message = "Delivery not found" });
            if (delivery.Status != DeliveryStatus.Draft && delivery.Status != DeliveryStatus.Picked)
                return BadRequest(new ApiErrorResponse { Message = "Delivery cannot be shipped from its current status" });

            delivery.Status = DeliveryStatus.Shipped;
            delivery.ActualShipDate = DateTime.UtcNow;
            if (dto.TrackingNumber != null) delivery.TrackingNumber = dto.TrackingNumber;
            if (dto.Carrier != null) delivery.Carrier = dto.Carrier;

            // Reflect the shipped quantities back on the originating order so it can't be
            // re-shipped and the UI hides the "Create Shipment" action once everything is out.
            foreach (var l in delivery.Lines)
            {
                if (l.SalesOrderLine != null)
                    l.SalesOrderLine.DeliveredQuantity += l.DeliveredQuantity;
            }
            if (delivery.SalesOrder != null && delivery.SalesOrder.Status != SalesOrderStatus.FullyDelivered)
                delivery.SalesOrder.Status = SalesOrderStatus.PartiallyDelivered;

            await _deliveries.SaveChangesAsync();

            // On shipment: post the COGS journal (DR COGS / CR Inventory) AND deduct physical stock.
            await PublishShipmentEventsAsync(delivery);

            return Ok(new ApiResponse<DeliveryDto> { Success = true, Data = MapToDto(delivery), Message = "Delivery shipped" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error shipping delivery {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error shipping delivery" }); }
    }

    /// <summary>Mark delivery as delivered</summary>
    [HttpPost("{id}/deliver")]
    [ProducesResponseType(typeof(ApiResponse<DeliveryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deliver(Guid id)
    {
        try
        {
            var delivery = await _deliveries.GetByIdAsync(id);
            if (delivery == null) return NotFound(new ApiErrorResponse { Message = "Delivery not found" });
            if (delivery.Status != DeliveryStatus.Shipped)
                return BadRequest(new ApiErrorResponse { Message = "Only Shipped deliveries can be marked as Delivered" });

            delivery.Status = DeliveryStatus.Delivered;
            delivery.ActualDeliveryDate = DateTime.UtcNow;

            await _deliveries.SaveChangesAsync();
            return Ok(new ApiResponse<DeliveryDto> { Success = true, Data = MapToDto(delivery), Message = "Delivery confirmed" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error confirming delivery {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error confirming delivery" }); }
    }

    /// <summary>Delete delivery (only Draft allowed)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var delivery = await _deliveries.GetByIdAsync(id);
            if (delivery == null) return NotFound(new ApiErrorResponse { Message = "Delivery not found" });
            if (delivery.Status != DeliveryStatus.Draft) return BadRequest(new ApiErrorResponse { Message = "Only Draft deliveries can be deleted" });
            _deliveries.Delete(delivery);
            await _deliveries.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Delivery deleted" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting delivery {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting delivery" }); }
    }

    private static DeliveryDto MapToDto(Delivery d) => new()
    {
        Id = d.Id,
        DeliveryNumber = d.DeliveryNumber,
        SalesOrderId = d.SalesOrderId,
        ContactId = d.ContactId,
        ContactName = d.ContactName,
        Status = d.Status,
        PlannedDeliveryDate = d.PlannedDeliveryDate,
        ActualShipDate = d.ActualShipDate,
        ActualDeliveryDate = d.ActualDeliveryDate,
        Carrier = d.Carrier,
        ShippingMethod = d.ShippingMethod,
        TrackingNumber = d.TrackingNumber,
        Incoterm = d.Incoterm,
        IncotermLocation = d.IncotermLocation,
        WarehouseId = d.WarehouseId,
        RecipientName = d.RecipientName,
        RecipientPhone = d.RecipientPhone,
        RecipientWhatsApp = d.RecipientWhatsApp,
        Street = d.Street,
        City = d.City,
        State = d.State,
        PostalCode = d.PostalCode,
        Country = d.Country,
        DeliveryLatitude = d.DeliveryLatitude,
        DeliveryLongitude = d.DeliveryLongitude,
        DeliveryNotes = d.DeliveryNotes,
        AssignedRiderId = d.AssignedRiderId,
        RiderName = d.RiderName,
        NumberOfPackages = d.NumberOfPackages,
        TotalWeight = d.TotalWeight,
        WeightUnit = d.WeightUnit,
        TotalVolume = d.TotalVolume,
        VolumeUnit = d.VolumeUnit,
        Notes = d.Notes,
        Lines = d.Lines.Select(l => new DeliveryLineDto
        {
            Id = l.Id,
            LineNumber = l.LineNumber,
            SalesOrderLineId = l.SalesOrderLineId,
            DeliveredQuantity = l.DeliveredQuantity,
            BinLocation = l.BinLocation,
            LotNumber = l.LotNumber,
            SerialNumber = l.SerialNumber,
            ExpiryDate = l.ExpiryDate,
            Notes = l.Notes,
        }).ToList(),
    };

    /// <summary>
    /// On shipment, publishes the COGS accounting event (DR COGS / CR Inventory GL) and the inventory
    /// stock-deduction event (<see cref="DeliveryPostedEvent"/> → Inventory's SalesStockDeductionHandler,
    /// which reduces InventoryBalance.QuantityOnHand). Both are resolved from the same per-line unit cost.
    /// Both publishes are non-blocking: a failure is logged and never rolls back the shipment.
    /// </summary>
    private async Task PublishShipmentEventsAsync(Delivery delivery)
    {
        var companyId = Guid.TryParse(User.FindFirstValue("CompanyId"), out var c) ? c : Guid.Empty;
        var branchId = Guid.TryParse(User.FindFirstValue("BranchId"), out var b) ? b : Guid.Empty;
        var businessUnitId = Guid.TryParse(User.FindFirstValue("BusinessUnitId"), out var bu) ? bu : Guid.Empty;
        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var u) ? u : Guid.Empty;

        var cogsLines = new List<SalesAccountingLine>();
        var stockLines = new List<StockDeductionLine>();

        foreach (var l in delivery.Lines)
        {
            var sol = l.SalesOrderLine;
            var productId = sol?.ProductId ?? Guid.Empty;
            if (productId == Guid.Empty) continue;

            // Resolve the item's unit cost + GL accounts once; feed both the COGS journal and stock valuation.
            ItemGlData glData = ItemGlData.Empty;
            try
            {
                var lookup = new ItemGlLookupEvent { ProductId = productId };
                await _events.PublishAsync(lookup);
                glData = await lookup.Result.Task;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GL lookup failed for product {ProductId} on Delivery {DeliveryId}", productId, delivery.Id);
            }

            // Ship the delivered quantity (fall back to the ordered quantity if not explicitly picked).
            var qty = l.DeliveredQuantity > 0 ? l.DeliveredQuantity : (sol?.OrderedQuantity ?? 0);
            var unitCost = sol?.CostPrice ?? glData.UnitCost;

            cogsLines.Add(new SalesAccountingLine
            {
                ProductId = productId,
                ProductCode = sol?.ProductCode ?? string.Empty,
                ProductName = sol?.ProductName ?? string.Empty,
                Quantity = qty,
                UnitPrice = 0m,
                UnitCost = unitCost,
                DiscountAmount = 0m,
                TaxAmount = 0m,
                LineTotal = 0m,
                CogsGlAccountId = glData.CogsAccountId,
                InventoryGlAccountId = glData.InventoryAccountId,
                SalesGlAccountId = glData.SalesAccountId,
            });

            stockLines.Add(new StockDeductionLine
            {
                ProductId = productId,
                ProductCode = sol?.ProductCode ?? string.Empty,
                VariantId = sol?.VariantId,
                WarehouseId = sol?.WarehouseId ?? delivery.WarehouseId,
                Quantity = qty,
                UnitOfMeasure = sol?.UnitOfMeasure ?? string.Empty,
                UnitCost = unitCost,
            });
        }

        // ── COGS / inventory-relief journal (Accounting) — non-blocking ──
        try
        {
            await _events.PublishAsync(new SalesCogsPostedEvent
            {
                DeliveryId = delivery.Id,
                SalesOrderId = delivery.SalesOrderId,
                ShippedAt = delivery.ActualShipDate ?? DateTime.UtcNow,
                CurrencyCode = "USD",
                Lines = cogsLines,
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                CreatedByUserId = userId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "COGS accounting event failed for Delivery {DeliveryId}", delivery.Id);
        }

        // ── Physical stock deduction (Inventory) — non-blocking ──
        if (stockLines.Count > 0)
        {
            try
            {
                await _events.PublishAsync(new DeliveryPostedEvent
                {
                    DeliveryId = delivery.Id,
                    SalesOrderId = delivery.SalesOrderId,
                    WarehouseId = delivery.WarehouseId ?? Guid.Empty,
                    CompanyId = companyId,
                    BranchId = branchId,
                    BusinessUnitId = businessUnitId,
                    CreatedByUserId = userId,
                    Lines = stockLines,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock deduction event failed for Delivery {DeliveryId}", delivery.Id);
            }
        }
    }
}
