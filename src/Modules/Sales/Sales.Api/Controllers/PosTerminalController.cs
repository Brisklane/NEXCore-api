using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Domain.Entities;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// Terminal registration and management.
/// A terminal is a physical or virtual POS device (tablet, register, kiosk) within a branch/store.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PosTerminalController : ControllerBase
{
    private readonly IPosTerminalRepository _terminals;
    private readonly ILogger<PosTerminalController> _logger;

    public PosTerminalController(IPosTerminalRepository terminals, ILogger<PosTerminalController> logger)
    {
        _terminals = terminals;
        _logger = logger;
    }

    // ?? CRUD ???????????????????????????????????????????????????????????????????

    /// <summary>Get all terminals for a branch/store.</summary>
    [HttpGet("branch/{branchId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PosTerminalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByBranch(Guid branchId)
    {
        try
        {
            var list = await _terminals.GetByBranchAsync(branchId);
            return Ok(new ApiResponse<List<PosTerminalDto>>
            {
                Success = true,
                Data = list.Select(MapToDto).ToList(),
                Message = "Terminals retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving terminals for branch {BranchId}", branchId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving terminals" });
        }
    }

    /// <summary>Get all active terminals for a branch/store.</summary>
    [HttpGet("branch/{branchId}/active")]
    [ProducesResponseType(typeof(ApiResponse<List<PosTerminalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveByBranch(Guid branchId)
    {
        try
        {
            var list = await _terminals.GetActiveByBranchAsync(branchId);
            return Ok(new ApiResponse<List<PosTerminalDto>>
            {
                Success = true,
                Data = list.Select(MapToDto).ToList(),
                Message = "Active terminals retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active terminals for branch {BranchId}", branchId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving terminals" });
        }
    }

    /// <summary>Get a terminal by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosTerminalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var terminal = await _terminals.GetByIdAsync(id);
            if (terminal == null) return NotFound(new ApiErrorResponse { Message = "Terminal not found" });
            return Ok(new ApiResponse<PosTerminalDto> { Success = true, Data = MapToDto(terminal), Message = "Terminal retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving terminal {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving terminal" });
        }
    }

    /// <summary>
    /// Register a new terminal (admin action).
    /// Returns 400 if a terminal with the same code already exists in the branch.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PosTerminalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreatePosTerminalDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var existing = await _terminals.GetByCodeAsync(dto.TerminalCode, dto.PosStoreId);
            if (existing != null)
                return BadRequest(new ApiErrorResponse { Message = $"A terminal with code '{dto.TerminalCode}' already exists in this branch" });

            var terminal = new PosTerminal
            {
                TerminalCode       = dto.TerminalCode,
                TerminalName       = dto.TerminalName,
                PosStoreId         = dto.PosStoreId,
                DeviceIdentifier   = dto.DeviceIdentifier,
                IpAddress          = dto.IpAddress,
                CashDrawerId       = dto.CashDrawerId,
                ReceiptTemplateId  = dto.ReceiptTemplateId,
                IsActive           = true,
                IsOnline           = false,
            };

            await _terminals.AddAsync(terminal);
            await _terminals.SaveChangesAsync();

            _logger.LogInformation("Terminal registered: {TerminalCode} in branch {BranchId}", terminal.TerminalCode, terminal.BranchId);

            return StatusCode(201, new ApiResponse<PosTerminalDto>
            {
                Success = true,
                Data    = MapToDto(terminal),
                Message = "Terminal registered successfully",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering terminal");
            return StatusCode(500, new ApiErrorResponse { Message = "Error registering terminal" });
        }
    }

    /// <summary>Update terminal configuration (name, device ID, receipt template, etc.).</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PosTerminalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePosTerminalDto dto)
    {
        try
        {
            var terminal = await _terminals.GetByIdAsync(id);
            if (terminal == null) return NotFound(new ApiErrorResponse { Message = "Terminal not found" });

            if (dto.TerminalName       != null)   terminal.TerminalName      = dto.TerminalName;
            if (dto.DeviceIdentifier   != null)   terminal.DeviceIdentifier  = dto.DeviceIdentifier;
            if (dto.IpAddress          != null)   terminal.IpAddress         = dto.IpAddress;
            if (dto.CashDrawerId.HasValue)        terminal.CashDrawerId      = dto.CashDrawerId;
            if (dto.ReceiptTemplateId.HasValue)   terminal.ReceiptTemplateId = dto.ReceiptTemplateId;
            if (dto.IsActive.HasValue)            terminal.IsActive          = dto.IsActive.Value;
            if (dto.IsOnline.HasValue)            terminal.IsOnline          = dto.IsOnline.Value;

            await _terminals.SaveChangesAsync();

            return Ok(new ApiResponse<PosTerminalDto> { Success = true, Data = MapToDto(terminal), Message = "Terminal updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating terminal {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating terminal" });
        }
    }

    /// <summary>Deactivate a terminal. Blocked if the terminal has an open session.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var terminal = await _terminals.GetByIdAsync(id);
            if (terminal == null) return NotFound(new ApiErrorResponse { Message = "Terminal not found" });

            if (terminal.CurrentSessionId.HasValue)
                return BadRequest(new ApiErrorResponse { Message = "Terminal has an open session — close it before deleting" });

            _terminals.Delete(terminal);
            await _terminals.SaveChangesAsync();

            _logger.LogInformation("Terminal {Id} ({TerminalCode}) deleted", id, terminal.TerminalCode);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Terminal deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting terminal {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting terminal" });
        }
    }

    // ?? Online / Offline heartbeat ????????????????????????????????????????????

    /// <summary>
    /// Terminal heartbeat — called by the POS device on startup and periodically.
    /// Marks the terminal online and updates its IP address.
    /// </summary>
    [HttpPut("{id}/heartbeat")]
    [ProducesResponseType(typeof(ApiResponse<PosTerminalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Heartbeat(Guid id, [FromBody] TerminalHeartbeatDto dto)
    {
        try
        {
            var terminal = await _terminals.GetByIdAsync(id);
            if (terminal == null) return NotFound(new ApiErrorResponse { Message = "Terminal not found" });

            terminal.IsOnline  = true;
            if (!string.IsNullOrWhiteSpace(dto.IpAddress))
                terminal.IpAddress = dto.IpAddress;
            if (!string.IsNullOrWhiteSpace(dto.DeviceIdentifier))
                terminal.DeviceIdentifier = dto.DeviceIdentifier;

            await _terminals.SaveChangesAsync();

            return Ok(new ApiResponse<PosTerminalDto> { Success = true, Data = MapToDto(terminal), Message = "Heartbeat recorded" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing heartbeat for terminal {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error processing heartbeat" });
        }
    }

    /// <summary>Mark a terminal offline (called on graceful shutdown).</summary>
    [HttpPut("{id}/go-offline")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GoOffline(Guid id)
    {
        try
        {
            var terminal = await _terminals.GetByIdAsync(id);
            if (terminal == null) return NotFound(new ApiErrorResponse { Message = "Terminal not found" });

            terminal.IsOnline = false;
            await _terminals.SaveChangesAsync();

            _logger.LogInformation("Terminal {Id} ({TerminalCode}) went offline", id, terminal.TerminalCode);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Terminal marked offline" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking terminal {Id} offline", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating terminal status" });
        }
    }

    // ?? Mapper ????????????????????????????????????????????????????????????????

    private static PosTerminalDto MapToDto(PosTerminal t) => new()
    {
        Id                = t.Id,
        TerminalCode      = t.TerminalCode,
        TerminalName      = t.TerminalName,
        PosStoreId        = t.PosStoreId,
        DeviceIdentifier  = t.DeviceIdentifier,
        IpAddress         = t.IpAddress,
        CashDrawerId      = t.CashDrawerId,
        ReceiptTemplateId = t.ReceiptTemplateId,
        IsActive          = t.IsActive,
        IsOnline          = t.IsOnline,
        CurrentSessionId  = t.CurrentSessionId,
    };
}
