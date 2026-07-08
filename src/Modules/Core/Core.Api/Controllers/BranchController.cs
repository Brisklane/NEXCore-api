using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Core.Api.Controllers;

/// <summary>Branch CRUD. Creation is public (used during registration); the rest require authentication.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BranchController : ControllerBase
{
    private readonly IBranchService _branchService;
    private readonly ILogger<BranchController> _logger;

    public BranchController(IBranchService branchService, ILogger<BranchController> logger)
    {
        _branchService = branchService;
        _logger = logger;
    }

    // Public: Create branch (no authorization)
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BranchResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequestDto request)
    {
        try
        {
            var result = await _branchService.CreateBranchAsync(request);
            if (!result.Success)
                return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });

            return Created(string.Empty, new ApiResponse<BranchResponseDto>
            {
                Data = result.Data,
                Message = result.Message,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    // Require authorization for all other endpoints
    [HttpGet("{branchId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BranchResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranch(Guid branchId)
    {
        var result = await _branchService.GetBranchByIdAsync(branchId);
        if (!result.Success)
            return NotFound(new ApiErrorResponse { Message = result.Message });

        return Ok(new ApiResponse<BranchResponseDto>
        {
            Data = result.Data,
            Success = true
        });
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BranchResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBranches([FromQuery] Guid? companyId = null)
    {
        var result = await _branchService.GetAllBranchesAsync(companyId);
        return Ok(new ApiResponse<IEnumerable<BranchResponseDto>>
        {
            Data = result.Data,
            Success = true
        });
    }

    [HttpPut("{branchId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BranchResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBranch(Guid branchId, [FromBody] UpdateBranchRequestDto request)
    {
        var result = await _branchService.UpdateBranchAsync(branchId, request);
        if (!result.Success)
            return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });

        return Ok(new ApiResponse<BranchResponseDto>
        {
            Data = result.Data,
            Message = result.Message,
            Success = true
        });
    }

    [HttpDelete("{branchId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateBranch(Guid branchId)
    {
        var result = await _branchService.DeactivateBranchAsync(branchId);
        if (!result.Success)
            return BadRequest(new ApiErrorResponse { Message = result.Message, Errors = result.Errors });

        return Ok(new ApiResponse
        {
            Message = result.Message,
            Success = true
        });
    }
}