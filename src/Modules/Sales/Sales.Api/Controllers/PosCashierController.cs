using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// Cashier management (CRUD + PIN), check-in / check-out, session management, and cash movements.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PosCashierController : ControllerBase
{
    private readonly IPosCashierRepository _cashiers;
    private readonly IPosSessionRepository _sessions;
    private readonly IPosTerminalRepository _terminals;
    private readonly IDocumentSequenceService _sequences;
    private readonly ILogger<PosCashierController> _logger;

    public PosCashierController(
        IPosCashierRepository cashiers,
        IPosSessionRepository sessions,
        IPosTerminalRepository terminals,
        IDocumentSequenceService sequences,
        ILogger<PosCashierController> logger)
    {
        _cashiers  = cashiers;
        _sessions  = sessions;
        _terminals = terminals;
        _sequences = sequences;
        _logger    = logger;
    }

    // Cashier CRUD

    /// <summary>Get all cashiers for a store.</summary>
    [HttpGet("store/{storeId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PosCashierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStore(Guid storeId)
    {
        try
        {
            var list = await _cashiers.GetByBranchAsync(storeId);
            return Ok(new ApiResponse<List<PosCashierDto>>
            {
                Success = true,
                Data = list.Select(MapCashierToDto).ToList(),
                Message = "Cashiers retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cashiers for store {StoreId}", storeId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving cashiers" });
        }
    }

    /// <summary>Get a cashier by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosCashierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var cashier = await _cashiers.GetByIdAsync(id);
            if (cashier == null) return NotFound(new ApiErrorResponse { Message = "Cashier not found" });
            return Ok(new ApiResponse<PosCashierDto> { Success = true, Data = MapCashierToDto(cashier), Message = "Cashier retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cashier {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving cashier" });
        }
    }

    /// <summary>
    /// Create a new POS cashier profile linked to an HR employee.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PosCashierDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePosCashierDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            // PosStoreId = the branch the cashier is working at; resolved from JWT BranchId.
            var branchId = Nexcore.SharedKernel.Helpers.TenantContextHelper
                .ExtractTenantContext(User).BranchId;

            var storeId = dto.PosStoreId != Guid.Empty ? dto.PosStoreId : branchId;

            var duplicate = await _cashiers.GetByEmployeeAsync(dto.EmployeeId, storeId);
            if (duplicate != null)
                return BadRequest(new ApiErrorResponse
                {
                    Message = "A cashier profile already exists for this employee at the selected store",
                });

            var cashier = new PosCashier
            {
                EmployeeId = dto.EmployeeId,
                DisplayName = dto.DisplayName ?? string.Empty,
                BadgeNumber = dto.BadgeNumber,
                PosStoreId = storeId,
                CanApplyManualDiscount = dto.CanApplyManualDiscount ?? false,
                MaxManualDiscountPercentage = dto.MaxManualDiscountPercentage ?? 0,
                CanVoidTransaction = dto.CanVoidTransaction ?? false,
                CanIssueRefund = dto.CanIssueRefund ?? false,
                CanOpenDrawer = dto.CanOpenDrawer ?? false,
                CanOverridePrices = dto.CanOverridePrices ?? false,
                CanApplyCoupons = dto.CanApplyCoupons ?? false,
                CanAccessReports = dto.CanAccessReports ?? false,
                IsActive = true,
            };

            await _cashiers.AddAsync(cashier);
            await _cashiers.SaveChangesAsync();

            _logger.LogInformation("Cashier created: {DisplayName} (EmployeeId: {EmployeeId})", cashier.DisplayName, cashier.EmployeeId);

            return StatusCode(201, new ApiResponse<PosCashierDto>
            {
                Success = true,
                Data = MapCashierToDto(cashier),
                Message = "Cashier created successfully",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cashier");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating cashier" });
        }
    }

    /// <summary>Update cashier profile and permissions.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosCashierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePosCashierDto dto)
    {
        try
        {
            var cashier = await _cashiers.GetByIdAsync(id);
            if (cashier == null) return NotFound(new ApiErrorResponse { Message = "Cashier not found" });

            if (dto.DisplayName != null) cashier.DisplayName = dto.DisplayName;
            if (dto.BadgeNumber != null) cashier.BadgeNumber = dto.BadgeNumber;
            if (dto.BranchId.HasValue) cashier.BranchId = dto.BranchId.Value;
            if (dto.CanApplyManualDiscount.HasValue) cashier.CanApplyManualDiscount = dto.CanApplyManualDiscount.Value;
            if (dto.MaxManualDiscountPercentage.HasValue) cashier.MaxManualDiscountPercentage = dto.MaxManualDiscountPercentage.Value;
            if (dto.CanVoidTransaction.HasValue) cashier.CanVoidTransaction = dto.CanVoidTransaction.Value;
            if (dto.CanIssueRefund.HasValue) cashier.CanIssueRefund = dto.CanIssueRefund.Value;
            if (dto.CanOpenDrawer.HasValue) cashier.CanOpenDrawer = dto.CanOpenDrawer.Value;
            if (dto.CanOverridePrices.HasValue) cashier.CanOverridePrices = dto.CanOverridePrices.Value;
            if (dto.CanApplyCoupons.HasValue) cashier.CanApplyCoupons = dto.CanApplyCoupons.Value;
            if (dto.CanAccessReports.HasValue) cashier.CanAccessReports = dto.CanAccessReports.Value;
            if (dto.IsActive.HasValue) cashier.IsActive = dto.IsActive.Value;

            _cashiers.Update(cashier);
            await _cashiers.SaveChangesAsync();

            return Ok(new ApiResponse<PosCashierDto> { Success = true, Data = MapCashierToDto(cashier), Message = "Cashier updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cashier {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating cashier" });
        }
    }

    /// <summary>Delete a cashier profile. Blocked if the cashier has an open session.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var cashier = await _cashiers.GetByIdAsync(id);
            if (cashier == null) return NotFound(new ApiErrorResponse { Message = "Cashier not found" });

            _cashiers.Delete(cashier);
            await _cashiers.SaveChangesAsync();

            _logger.LogInformation("Cashier {Id} deleted", id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Cashier deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cashier {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting cashier" });
        }
    }

    // PIN Management

    /// <summary>
    /// Set or reset a cashier PIN (manager / admin action).
    /// PIN is stored as a BCrypt hash - the plain PIN is never persisted.
    /// </summary>
    [HttpPut("{id}/set-pin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPin(Guid id, [FromBody] SetCashierPinDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Pin) || dto.Pin.Length < 4 || dto.Pin.Length > 6 || !dto.Pin.All(char.IsDigit))
                return BadRequest(new ApiErrorResponse { Message = "PIN must be 4-6 numeric digits" });

            var cashier = await _cashiers.GetByIdAsync(id);
            if (cashier == null) return NotFound(new ApiErrorResponse { Message = "Cashier not found" });

            cashier.PinCode = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
            _cashiers.Update(cashier);
            await _cashiers.SaveChangesAsync();

            _logger.LogInformation("PIN set for cashier {Id} ({DisplayName})", id, cashier.DisplayName);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "PIN set successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting PIN for cashier {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error setting PIN" });
        }
    }

    /// <summary>
    /// Cashier self-service PIN change.
    /// Requires the current PIN to be correct before accepting the new one.
    /// </summary>
    [HttpPut("{id}/change-pin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePin(Guid id, [FromBody] ChangeCashierPinDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.NewPin) || dto.NewPin.Length < 4 || dto.NewPin.Length > 6 || !dto.NewPin.All(char.IsDigit))
                return BadRequest(new ApiErrorResponse { Message = "New PIN must be 4-6 numeric digits" });

            var cashier = await _cashiers.GetByIdAsync(id);
            if (cashier == null) return NotFound(new ApiErrorResponse { Message = "Cashier not found" });

            if (string.IsNullOrEmpty(cashier.PinCode) || !BCrypt.Net.BCrypt.Verify(dto.CurrentPin, cashier.PinCode))
                return BadRequest(new ApiErrorResponse { Message = "Current PIN is incorrect" });

            cashier.PinCode = BCrypt.Net.BCrypt.HashPassword(dto.NewPin);
            _cashiers.Update(cashier);
            await _cashiers.SaveChangesAsync();

            _logger.LogInformation("PIN changed for cashier {Id}", id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "PIN changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing PIN for cashier {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error changing PIN" });
        }
    }

    // PIN Login

    /// <summary>
    /// Cashier PIN login at the POS screen.
    /// The cashier enters their 4-6 digit PIN and the store is resolved from the terminal.
    /// Returns the cashier profile so the UI can display name and permissions.
    /// </summary>
    [HttpPost("pin-login")]
    [ProducesResponseType(typeof(ApiResponse<PosCashierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PinLogin([FromBody] CashierPinLoginDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Pin))
                return BadRequest(new ApiErrorResponse { Message = "PIN is required" });

            var cashier = await _cashiers.GetByPinAsync(dto.Pin, dto.StoreId);
            if (cashier == null)
                return Unauthorized(new ApiErrorResponse { Message = "Invalid PIN" });

            if (!cashier.IsActive)
                return Unauthorized(new ApiErrorResponse { Message = "Cashier account is inactive" });

            _logger.LogInformation("Cashier {DisplayName} ({Id}) logged in via PIN at store {StoreId}", cashier.DisplayName, cashier.Id, dto.StoreId);

            return Ok(new ApiResponse<PosCashierDto>
            {
                Success = true,
                Data = MapCashierToDto(cashier),
                Message = $"Welcome, {cashier.DisplayName}",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PIN login for store {StoreId}", dto.StoreId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error processing PIN login" });
        }
    }

    // Session: Check-In (Open)

    /// <summary>
    /// Cashier check-in - opens a new POS session on a terminal with an opening float.
    /// Returns 400 if the cashier already has an open session on this terminal.
    /// </summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(ApiResponse<PosSessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckIn([FromBody] CashierCheckInDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            // Guard: one open session per terminal (one terminal = one cash drawer), regardless of cashier.
            var existing = await _sessions.GetOpenSessionByTerminalAsync(dto.TerminalId);
            if (existing != null)
                return BadRequest(new ApiErrorResponse
                {
                    Message = existing.PosCashierId == dto.CashierId
                        ? $"You already have an open session ({existing.SessionNumber}) on this terminal."
                        : $"This terminal already has an open session ({existing.SessionNumber}) opened by another cashier. Close it before opening a new one.",
                });

            var cashier = await _cashiers.GetByIdAsync(dto.CashierId);
            if (cashier == null) return NotFound(new ApiErrorResponse { Message = "Cashier not found" });

            var terminal = await _terminals.GetByIdAsync(dto.TerminalId);
            if (terminal == null) return NotFound(new ApiErrorResponse { Message = "Terminal not found" });

            var openingFloat = dto.Denominations.Count > 0
                ? dto.Denominations.Sum(d => d.Denomination * d.Count)
                : dto.OpeningFloat;

            var sessSeq = await _sequences.GetNextNumberAsync(DocumentType.PosSession);
            var session = new PosSession
            {
                SessionNumber              = sessSeq.Code,
                Code                       = sessSeq.Code,
                CodeInt                    = sessSeq.CodeInt,
                PosTerminalId              = dto.TerminalId,
                PosCashierId               = dto.CashierId,
                OpeningFloat               = openingFloat,
                OpeningNotes               = dto.OpeningNotes,
                OpeningDenominationsJson   = dto.Denominations.Count > 0
                                               ? JsonSerializer.Serialize(dto.Denominations)
                                               : null,
                Status    = PosSessionStatus.Open,
                OpenedAt  = DateTime.UtcNow,
            };

            await _sessions.AddAsync(session);
            await _sessions.SaveChangesAsync();

            _logger.LogInformation("Cashier {CashierId} checked in - session {SessionNumber}", dto.CashierId, session.SessionNumber);

            return StatusCode(201, new ApiResponse<PosSessionDto>
            {
                Success = true,
                Data = MapSessionToDto(session),
                Message = "Session opened - cashier checked in",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cashier check-in");
            return StatusCode(500, new ApiErrorResponse { Message = "Error opening session" });
        }
    }

    // Session: Check-Out (Close)

    /// <summary>
    /// Cashier check-out - closes an open session, records closing float, and calculates variance.
    /// </summary>
    [HttpPost("check-out/{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<PosSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckOut(Guid sessionId, [FromBody] CashierCheckOutDto dto)
    {
        try
        {
            var session = await _sessions.GetByIdAsync(sessionId);
            if (session == null) return NotFound(new ApiErrorResponse { Message = "Session not found" });

            if (session.Status != PosSessionStatus.Open)
                return BadRequest(new ApiErrorResponse { Message = "Session is not open" });

            var closingFloat = dto.Denominations.Count > 0
                ? dto.Denominations.Sum(d => d.Denomination * d.Count)
                : dto.ClosingFloat;

            // Cash that was banked or paid out mid-shift has to come off the expectation.
            // Without this a cashier who does a safe drop looks short by exactly the amount
            // they moved to the safe — which is the one thing a close must never get wrong.
            var movements = (await _sessions.GetByIdWithMovementsAsync(sessionId))?.CashMovements
                            ?? new List<PosCashMovement>();
            var cashIn = movements
                .Where(m => !m.IsDeleted && m.MovementType == PosCashMovementType.CashIn)
                .Sum(m => m.Amount);
            var cashOut = movements
                .Where(m => !m.IsDeleted && m.MovementType is PosCashMovementType.CashOut
                                                           or PosCashMovementType.SafeDrop
                                                           or PosCashMovementType.PettyCash)
                .Sum(m => m.Amount);

            session.ClosingFloat              = closingFloat;
            session.ExpectedClosingFloat      = session.OpeningFloat + session.CashCollected + cashIn - cashOut;
            session.FloatVariance             = closingFloat - session.ExpectedClosingFloat;
            session.ClosingNotes              = dto.Notes;
            session.ClosingDenominationsJson  = dto.Denominations.Count > 0
                                                  ? JsonSerializer.Serialize(dto.Denominations)
                                                  : null;
            session.Status   = PosSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;

            _sessions.Update(session);
            await _sessions.SaveChangesAsync();

            _logger.LogInformation(
                "Cashier {CashierId} checked out - session {SessionNumber} closed. Variance: {Variance:C}",
                session.PosCashierId, session.SessionNumber, session.FloatVariance);

            return Ok(new ApiResponse<PosSessionDto>
            {
                Success = true,
                Data = MapSessionToDto(session),
                Message = $"Session closed - float variance: {session.FloatVariance:C}",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cashier check-out for session {SessionId}", sessionId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error closing session" });
        }
    }

    // Session Queries

    /// <summary>
    /// Get the currently open session for a cashier.
    /// Pass terminalId to narrow to a specific terminal; omit to find any open session for the cashier.
    /// </summary>
    [HttpGet("open-session")]
    [ProducesResponseType(typeof(ApiResponse<PosSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOpenSession([FromQuery] Guid cashierId, [FromQuery] Guid? terminalId = null)
    {
        try
        {
            var session = terminalId.HasValue && terminalId.Value != Guid.Empty
                ? await _sessions.GetOpenSessionAsync(cashierId, terminalId.Value)
                : await _sessions.GetOpenSessionByCashierAsync(cashierId);

            if (session == null)
                return NotFound(new ApiErrorResponse { Message = "No open session found for this cashier" });

            return Ok(new ApiResponse<PosSessionDto> { Success = true, Data = MapSessionToDto(session), Message = "Open session retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving open session");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving session" });
        }
    }

    /// <summary>Get all sessions for a terminal (history).</summary>
    [HttpGet("sessions/by-terminal/{terminalId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PosSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionsByTerminal(Guid terminalId)
    {
        try
        {
            var list = await _sessions.GetByTerminalAsync(terminalId);
            return Ok(new ApiResponse<List<PosSessionDto>>
            {
                Success = true,
                Data = list.Select(MapSessionToDto).ToList(),
                Message = "Terminal sessions retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for terminal {TerminalId}", terminalId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sessions" });
        }
    }

    /// <summary>Get all sessions for a store on a specific date.</summary>
    [HttpGet("sessions/by-date")]
    [ProducesResponseType(typeof(ApiResponse<List<PosSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionsByDate([FromQuery] DateTime date, [FromQuery] Guid storeId)
    {
        try
        {
            var list = await _sessions.GetByDateAsync(date, storeId);
            return Ok(new ApiResponse<List<PosSessionDto>>
            {
                Success = true,
                Data = list.Select(MapSessionToDto).ToList(),
                Message = "Sessions retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions by date");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sessions" });
        }
    }

    // Cash Movements

    /// <summary>
    /// Cash-in - add cash to the drawer during an open session
    /// (e.g., change float top-up, petty cash received).
    /// </summary>
    [HttpPost("sessions/{sessionId}/cash-in")]
    [ProducesResponseType(typeof(ApiResponse<PosCashMovementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CashIn(Guid sessionId, [FromBody] CashMovementDto dto)
    {
        try
        {
            return await RecordCashMovement(sessionId, dto, PosCashMovementType.CashIn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cash-in for session {SessionId}", sessionId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error recording cash-in" });
        }
    }

    /// <summary>
    /// Cash-out - remove cash from the drawer during an open session
    /// (e.g., safe drop, petty cash disbursement).
    /// </summary>
    [HttpPost("sessions/{sessionId}/cash-out")]
    [ProducesResponseType(typeof(ApiResponse<PosCashMovementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CashOut(Guid sessionId, [FromBody] CashMovementDto dto)
    {
        try
        {
            return await RecordCashMovement(sessionId, dto, PosCashMovementType.CashOut);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cash-out for session {SessionId}", sessionId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error recording cash-out" });
        }
    }

    /// <summary>Get all cash movements for a session.</summary>
    [HttpGet("sessions/{sessionId}/cash-movements")]
    [ProducesResponseType(typeof(ApiResponse<List<PosCashMovementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCashMovements(Guid sessionId)
    {
        try
        {
            var session = await _sessions.GetByIdWithMovementsAsync(sessionId);
            if (session == null) return NotFound(new ApiErrorResponse { Message = "Session not found" });

            var movements = session.CashMovements.Select(MapMovementToDto).ToList();
            return Ok(new ApiResponse<List<PosCashMovementDto>>
            {
                Success = true,
                Data = movements,
                Message = $"{movements.Count} cash movement(s) found",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash movements for session {SessionId}", sessionId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving cash movements" });
        }
    }

    // Private helpers

    private async Task<IActionResult> RecordCashMovement(
        Guid sessionId, CashMovementDto dto, PosCashMovementType type)
    {
        if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
        if (dto.Amount <= 0) return BadRequest(new ApiErrorResponse { Message = "Amount must be greater than zero" });

        var session = await _sessions.GetByIdAsync(sessionId);
        if (session == null) return NotFound(new ApiErrorResponse { Message = "Session not found" });
        if (session.Status != PosSessionStatus.Open)
            return BadRequest(new ApiErrorResponse { Message = "Cash movements can only be recorded on an open session" });

        var movement = new PosCashMovement
        {
            PosSessionId = sessionId,
            PosCashierId = dto.CashierId,
            MovementType = type,
            Amount = dto.Amount,
            Reason = dto.Reason,
            MovementDate = DateTime.UtcNow,
        };

        await _sessions.AddCashMovementAsync(movement);
        await _sessions.SaveChangesAsync();

        _logger.LogInformation(
            "{MovementType} of {Amount:C} recorded on session {SessionNumber} by cashier {CashierId}",
            type, dto.Amount, session.SessionNumber, dto.CashierId);

        return StatusCode(201, new ApiResponse<PosCashMovementDto>
        {
            Success = true,
            Data = MapMovementToDto(movement),
            Message = $"{type} of {dto.Amount:C} recorded",
        });
    }

    // Mappers

    private static PosCashierDto MapCashierToDto(PosCashier c) => new()
    {
        Id = c.Id,
        EmployeeId = c.EmployeeId,
        DisplayName = c.DisplayName,
        BadgeNumber = c.BadgeNumber,
        BranchId = c.BranchId,
        CanApplyManualDiscount = c.CanApplyManualDiscount,
        MaxManualDiscountPercentage = c.MaxManualDiscountPercentage,
        CanVoidTransaction = c.CanVoidTransaction,
        CanIssueRefund = c.CanIssueRefund,
        CanOpenDrawer = c.CanOpenDrawer,
        CanOverridePrices = c.CanOverridePrices,
        CanApplyCoupons = c.CanApplyCoupons,
        CanAccessReports = c.CanAccessReports,
        IsActive = c.IsActive,
    };

    private static PosSessionDto MapSessionToDto(PosSession s) => new()
    {
        Id                   = s.Id,
        SessionNumber        = s.SessionNumber,
        PosTerminalId        = s.PosTerminalId,
        PosCashierId         = s.PosCashierId,
        Status               = s.Status,
        OpenedAt             = s.OpenedAt,
        ClosedAt             = s.ClosedAt,
        OpeningFloat         = s.OpeningFloat,
        OpeningNotes         = s.OpeningNotes,
        OpeningDenominations = DeserializeDenominations(s.OpeningDenominationsJson),
        ClosingFloat         = s.ClosingFloat,
        ExpectedClosingFloat = s.ExpectedClosingFloat,
        FloatVariance        = s.FloatVariance,
        ClosingDenominations = DeserializeDenominations(s.ClosingDenominationsJson),
        TotalSalesAmount     = s.TotalSalesAmount,
        TotalRefundsAmount   = s.TotalRefundsAmount,
        TotalDiscountsAmount = s.TotalDiscountsAmount,
        TotalTaxAmount       = s.TotalTaxAmount,
        NetSalesAmount       = s.NetSalesAmount,
        CashCollected        = s.CashCollected,
        CardCollected        = s.CardCollected,
        WalletCollected      = s.WalletCollected,
        OtherCollected       = s.OtherCollected,
        TransactionCount     = s.TransactionCount,
        ClosingNotes         = s.ClosingNotes,
    };

    private static List<DenominationCountDto> DeserializeDenominations(string? json) =>
        string.IsNullOrEmpty(json)
            ? []
            : JsonSerializer.Deserialize<List<DenominationCountDto>>(json) ?? [];

    private static PosCashMovementDto MapMovementToDto(PosCashMovement m) => new()
    {
        Id = m.Id,
        PosSessionId = m.PosSessionId,
        PosCashierId = m.PosCashierId,
        MovementType = m.MovementType,
        Amount = m.Amount,
        Reason = m.Reason,
        MovementDate = m.MovementDate,
    };
}
