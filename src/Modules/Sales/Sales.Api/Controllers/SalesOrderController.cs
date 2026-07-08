using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class SalesOrderController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;
    private readonly ILogger<SalesOrderController> _logger;

    public SalesOrderController(ISalesOrderService salesOrderService, ILogger<SalesOrderController> logger)
    {
        _salesOrderService = salesOrderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _salesOrderService.GetAllAsync();
            return Ok(new ApiResponse<List<SalesOrderDto>> { Success = true, Data = list, Message = "Orders retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving orders"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving orders" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var order = await _salesOrderService.GetByIdAsync(id);
            if (order == null) return NotFound(new ApiErrorResponse { Message = "Order not found" });
            return Ok(new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = "Order retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving order" }); }
    }

    /// <summary>Get order with all lines and navigation properties loaded — for detail/edit pages.</summary>
    [HttpGet("{id}/full")]
    public async Task<IActionResult> GetFullDetails(Guid id)
    {
        try
        {
            var order = await _salesOrderService.GetWithFullDetailsAsync(id);
            if (order == null) return NotFound(new ApiErrorResponse { Message = "Order not found" });
            return Ok(new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = "Order retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving order details {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving order" }); }
    }

    /// <summary>Get order by order number.</summary>
    [HttpGet("by-number/{orderNumber}")]
    public async Task<IActionResult> GetByNumber(string orderNumber)
    {
        try
        {
            var order = await _salesOrderService.GetByNumberAsync(orderNumber);
            if (order == null) return NotFound(new ApiErrorResponse { Message = "Order not found" });
            return Ok(new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = "Order retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving order {Number}", orderNumber); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving order" }); }
    }

    /// <summary>Get orders filtered by status.</summary>
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(Sales.Domain.Enums.SalesOrderStatus status)
    {
        try
        {
            var list = await _salesOrderService.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<SalesOrderDto>> { Success = true, Data = list, Message = $"Orders with status '{status}'" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving orders by status"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving orders" }); }
    }

    /// <summary>Get orders for a specific sales channel (e.g. POS, DARAZ, WEBSITE).</summary>
    [HttpGet("by-channel/{channel}")]
    public async Task<IActionResult> GetByChannel(Sales.Domain.Enums.SalesChannel channel)
    {
        try
        {
            var list = await _salesOrderService.GetByChannelAsync(channel);
            return Ok(new ApiResponse<List<SalesOrderDto>> { Success = true, Data = list, Message = $"Orders for channel '{channel}'" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving orders by channel"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving orders" }); }
    }

    /// <summary>Get active draft orders for a customer — used by cart/resume-order flows.</summary>
    [HttpGet("active-drafts/{contactId}")]
    public async Task<IActionResult> GetActiveDrafts(Guid contactId)
    {
        try
        {
            var list = await _salesOrderService.GetActiveDraftsByContactAsync(contactId);
            return Ok(new ApiResponse<List<SalesOrderDto>> { Success = true, Data = list, Message = "Active draft orders" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active drafts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving orders" }); }
    }

    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        try
        {
            var list = await _salesOrderService.GetByCustomerAsync(customerId);
            return Ok(new ApiResponse<List<SalesOrderDto>> { Success = true, Data = list, Message = "Customer orders" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving orders for customer"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving orders" }); }
    }

    /// <summary>Store incoming order queue — never returns Draft orders.</summary>
    [HttpGet("store-queue/{storeId}")]
    public async Task<IActionResult> GetStoreQueue(Guid storeId)
    {
        try
        {
            var list = await _salesOrderService.GetStoreQueueAsync(storeId);
            return Ok(new ApiResponse<List<SalesOrderDto>> { Success = true, Data = list, Message = "Store order queue" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving store queue"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving store queue" }); }
    }

    /// <summary>
    /// Pending online-order count for a store's POS "Orders" button badge.
    /// Counts app / web-store / marketplace orders awaiting acknowledgement (Status = Placed).
    /// Use this on POS load and on hub reconnect; live changes arrive over the POS order hub
    /// (<c>/hubs/pos-orders</c> → <c>OnlineOrderReceived</c> / <c>PendingOrderCountChanged</c>).
    /// </summary>
    [HttpGet("store-queue/{storeId}/pending-online-count")]
    [ProducesResponseType(typeof(ApiResponse<PosPendingOrdersDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingOnlineCount(Guid storeId)
    {
        try
        {
            var count = await _salesOrderService.GetPendingOnlineOrderCountAsync(storeId);
            return Ok(new ApiResponse<PosPendingOrdersDto>
            {
                Success = true,
                Data = new PosPendingOrdersDto { StoreId = storeId, PendingCount = count },
                Message = $"{count} pending online order(s)",
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pending online count for store {StoreId}", storeId); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving pending online count" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSalesOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var order = await _salesOrderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id },
                new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = "Order created" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating order"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating order" }); }
    }

    /// <summary>Place order — transitions Draft ? Placed. Store becomes visible.</summary>
    [HttpPost("{id}/place")]
    public async Task<IActionResult> PlaceOrder(Guid id)
    {
        try
        {
            var order = await _salesOrderService.PlaceOrderAsync(id);
            return Ok(new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = "Order placed successfully" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error placing order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error placing order" }); }
    }

    /// <summary>Update order status (store workflow: Confirmed ? Preparing ? ReadyForPickup).</summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSalesOrderStatusDto dto)
    {
        try
        {
            var order = await _salesOrderService.UpdateStatusAsync(id, dto, User.Identity?.Name);
            return Ok(new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = $"Order status updated to {dto.Status}" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating order status {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating order status" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _salesOrderService.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Order deleted" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting order" }); }
    }

    // ── Invoicing endpoints ───────────────────────────────────────────────────

    /// <summary>
    /// Orders that have uninvoiced quantities — the "To Invoice" queue (Odoo menu item).
    /// </summary>
    [HttpGet("to-invoice")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetToInvoice()
    {
        try
        {
            var list = await _salesOrderService.GetOrdersToInvoiceAsync();
            return Ok(new ApiResponse<List<SalesOrderDto>> { Success = true, Data = list, Message = $"{list.Count} order(s) ready to invoice" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving orders to invoice"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving orders" }); }
    }

    /// <summary>
    /// Create a draft invoice from this order — equivalent to Odoo's "Create Invoice" button.
    ///
    /// Three invoice types (matching Odoo's dialog):
    ///   Regular                — invoices all uninvoiced line quantities
    ///   DownPaymentPercentage  — advance deposit as % of order total
    ///   DownPaymentFixed       — advance deposit as a fixed amount
    ///
    /// Previous down-payment invoices are automatically deducted from a Regular invoice.
    /// </summary>
    [HttpPost("{id}/create-invoice")]
    [ProducesResponseType(typeof(ApiResponse<SalesInvoiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvoice(Guid id, [FromBody] CreateInvoiceFromOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var invoice = await _salesOrderService.CreateInvoiceFromOrderAsync(id, dto);
            return StatusCode(201, new ApiResponse<SalesInvoiceDto> { Success = true, Data = invoice, Message = "Invoice created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating invoice for order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error creating invoice" }); }
    }

    // ── Lock / Unlock ─────────────────────────────────────────────────────────

    /// <summary>Lock a confirmed order to prevent further edits (Odoo "Lock Confirmed Sales").</summary>
    [HttpPost("{id}/lock")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Lock(Guid id)
    {
        try
        {
            var order = await _salesOrderService.LockAsync(id);
            return Ok(new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = "Order locked" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error locking order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error locking order" }); }
    }

    /// <summary>Unlock a previously locked order to allow edits.</summary>
    [HttpPost("{id}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unlock(Guid id)
    {
        try
        {
            var order = await _salesOrderService.UnlockAsync(id);
            return Ok(new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = "Order unlocked" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error unlocking order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error unlocking order" }); }
    }

    /// <summary>
    /// Cashier-created phone/walk-in order form.
    /// Pre-wires POS context and defaults the channel to PhoneOrder.
    /// Internally maps to CreateSalesOrderDto and calls the same CreateAsync pipeline.
    /// </summary>
    [HttpPost("phone-order")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePhoneOrder([FromBody] CreatePhoneOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            if (dto.Lines is null || dto.Lines.Count == 0)
                return BadRequest(new ApiErrorResponse { Message = "Order must contain at least one line" });

            var mapped = new CreateSalesOrderDto
            {
                ContactId            = dto.ContactId,
                ContactName          = (dto.ContactName ?? dto.ContactPhone)!,
                SalesChannel         = dto.SalesChannel,
                FulfillmentType      = dto.FulfillmentType,
                OriginBranchId       = dto.PosStoreId,
                OriginPosCashierId   = dto.PosCashierId,
                OriginPosTerminalId  = dto.PosTerminalId,
                OriginPosSessionId   = dto.PosSessionId,
                PriceListId          = dto.PriceListId,
                CurrencyCode         = dto.CurrencyCode,
                CouponCode           = dto.CouponCode,
                Notes                = dto.Notes,
                Lines = dto.Lines.Select(l => new CreateSalesOrderLineDto
                {
                    ProductId           = l.ProductId,
                    ProductCode         = l.ProductCode,
                    ProductName         = l.ProductName,
                    VariantId           = l.VariantId,
                    VariantName         = l.VariantName,
                    Quantity            = l.Quantity,
                    UnitOfMeasure       = l.UnitOfMeasure,
                    UnitPrice           = l.UnitPrice,
                    DiscountAmount      = l.DiscountAmount,
                    SpecialInstructions = l.SpecialInstructions,
                    Addons              = l.Addons,
                }).ToList(),
            };

            var order = await _salesOrderService.CreateAsync(mapped);

            _logger.LogInformation(
                "Phone order {OrderNumber} created by cashier {CashierId} at store {StoreId}",
                order.OrderNumber, dto.PosCashierId, dto.PosStoreId);

            return CreatedAtAction(nameof(GetById), new { id = order.Id },
                new ApiResponse<SalesOrderDto> { Success = true, Data = order, Message = "Phone order created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating phone order");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating phone order" });
        }
    }
}
