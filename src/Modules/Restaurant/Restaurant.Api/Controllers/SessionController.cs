using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Enums;

namespace Restaurant.Api.Controllers;

/// <summary>Cash sessions, drawer movements and the X/Z reads that close a trading day.</summary>
[Route("api/restaurant/sessions")]
public class SessionController(
    IRestaurantSessionService sessions,
    ILogger<SessionController> logger) : RestaurantControllerBase(logger)
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? outletId,
        [FromQuery] SessionStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await sessions.ListSessionsAsync(outletId, status, from, to, pagination));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing restaurant sessions");
            return StatusCode(500, new ApiErrorResponse { Message = "Could not load sessions." });
        }
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
        => RunFound(() => sessions.GetSessionAsync(id), "Session not found.");

    [HttpGet("open/{outletId:guid}")]
    public Task<IActionResult> GetOpen(Guid outletId, [FromQuery] string? terminalName)
        => RunFound(() => sessions.GetOpenSessionAsync(outletId, terminalName), "No session is open on this till.");

    [HttpPost("open")]
    public Task<IActionResult> Open([FromBody] OpenSessionDto request)
        => Run(() => sessions.OpenSessionAsync(request, UserId), "Session opened.");

    [HttpPost("close")]
    public Task<IActionResult> Close([FromBody] CloseSessionDto request)
        => Run(() => sessions.CloseSessionAsync(request, UserId), "Session closed.");

    [HttpPost("cash-movements")]
    public Task<IActionResult> AddCashMovement([FromBody] AddCashMovementDto request)
        => Run(() => sessions.AddCashMovementAsync(request, UserId), "Recorded.");

    /// <summary>Mid-shift read. Repeatable, and does not close anything.</summary>
    [HttpGet("{id:guid}/x-read")]
    public Task<IActionResult> XRead(Guid id) => Run(() => sessions.GetXReadAsync(id));

    /// <summary>End-of-day read. Only available once the session is closed.</summary>
    [HttpGet("{id:guid}/z-read")]
    public Task<IActionResult> ZRead(Guid id) => Run(() => sessions.GetZReadAsync(id, UserId));
}
