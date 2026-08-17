using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Enums;

namespace Restaurant.Api.Controllers;

/// <summary>Order lifecycle — open, add, hold, fire, serve, void, cancel.</summary>
[Route("api/restaurant/orders")]
public class OrderController(
    IRestaurantOrderService orders,
    ILogger<OrderController> logger) : RestaurantControllerBase(logger)
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? outletId,
        [FromQuery] RestaurantOrderStatus? status,
        [FromQuery] OrderType? orderType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await orders.ListOrdersAsync(outletId, status, orderType, from, to, pagination));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing restaurant orders");
            return StatusCode(500, new ApiErrorResponse { Message = "Could not load orders." });
        }
    }

    /// <summary>Open orders for the service rail — unpaged, because this is the live working set.</summary>
    [HttpGet("active/{outletId:guid}")]
    public Task<IActionResult> Active(Guid outletId, [FromQuery] Guid? waiterId)
        => Run(() => orders.GetActiveOrdersAsync(outletId, waiterId));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
        => RunFound(() => orders.GetOrderAsync(id), "Order not found.");

    [HttpGet("by-table/{tableId:guid}")]
    public Task<IActionResult> GetByTable(Guid tableId)
        => RunFound(() => orders.GetOrderByTableAsync(tableId), "This table has no open order.");

    [HttpPost]
    public Task<IActionResult> Open([FromBody] OpenOrderDto request)
        => Run(() => orders.OpenOrderAsync(request, UserId), "Order opened.");

    [HttpPost("lines")]
    public Task<IActionResult> AddLines([FromBody] AddLinesDto request)
        => Run(() => orders.AddLinesAsync(request, UserId), "Added to the order.");

    [HttpPut("lines/{lineId:guid}")]
    public Task<IActionResult> UpdateLine(Guid lineId, [FromBody] UpdateOrderLineDto request)
        => Run(() => orders.UpdateLineAsync(lineId, request, UserId), "Line updated.");

    [HttpPost("lines/void")]
    public Task<IActionResult> VoidLine([FromBody] VoidLineDto request)
        => Run(() => orders.VoidLineAsync(request, UserId), "Line voided.");

    [HttpPost("lines/move")]
    public Task<IActionResult> MoveLines([FromBody] MoveLinesDto request)
        => Run(() => orders.MoveLinesAsync(request, UserId), "Lines moved.");

    /// <summary>Sends a held course to the kitchen. Nothing reaches a station before this.</summary>
    [HttpPost("fire")]
    public Task<IActionResult> Fire([FromBody] FireCourseDto request)
        => Run(() => orders.FireCourseAsync(request, UserId), "Sent to the kitchen.");

    [HttpPost("{id:guid}/served")]
    public Task<IActionResult> MarkServed(Guid id, [FromBody] List<Guid>? lineIds)
        => Run(() => orders.MarkServedAsync(id, lineIds ?? [], UserId), "Marked as served.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderDto request)
        => Run(() => orders.UpdateOrderAsync(id, request, UserId), "Order updated.");

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderDto request)
        => Run(() => orders.CancelOrderAsync(id, request, UserId), "Order cancelled.");

    [HttpPut("{id:guid}/delivery")]
    public Task<IActionResult> UpdateDelivery(Guid id, [FromBody] UpdateDeliveryStatusDto request)
        => Run(() => orders.UpdateDeliveryAsync(id, request, UserId), "Delivery updated.");
}
