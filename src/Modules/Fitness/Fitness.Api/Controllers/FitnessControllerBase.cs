using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Fitness.Api.Controllers;

/// <summary>
/// Shared plumbing for every Fitness controller.
///
/// The exception mapping is the point. The services throw
/// <see cref="InvalidOperationException"/> for a rule the caller broke ("that class is full")
/// and <see cref="UnauthorizedAccessException"/> for an approval that was not given. Both carry
/// a message written for the person at the desk, so they are surfaced verbatim as 400 and 403 —
/// a receptionist who taps "book" on a full class should read *why*, not "Internal server error".
///
/// That matters more here than in most modules, because a large share of this app's failures are
/// things a member is standing in front of you asking about.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize]
public abstract class FitnessControllerBase(ILogger logger) : ControllerBase
{
    protected Guid UserId => TenantContextHelper.ExtractUserId(User);

    /// <summary>Runs an action that returns a payload, mapping domain failures onto HTTP.</summary>
    protected async Task<IActionResult> Run<T>(Func<Task<T>> action, string? successMessage = null)
    {
        try
        {
            var result = await action();
            return Ok(new ApiResponse<T> { Success = true, Message = successMessage, Data = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in {Controller}", GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse { Message = "Something went wrong. Please try again." });
        }
    }

    /// <summary>Runs an action with no payload.</summary>
    protected async Task<IActionResult> Run(Func<Task> action, string? successMessage = null)
    {
        try
        {
            await action();
            return Ok(ApiResponseExtensions.Success(successMessage));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in {Controller}", GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse { Message = "Something went wrong. Please try again." });
        }
    }

    /// <summary>Runs a read that may legitimately find nothing, mapping null onto 404.</summary>
    protected async Task<IActionResult> RunFound<T>(Func<Task<T?>> action, string notFoundMessage) where T : class
    {
        try
        {
            var result = await action();
            if (result is null) return NotFound(new ApiErrorResponse { Message = notFoundMessage });
            return Ok(new ApiResponse<T> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in {Controller}", GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse { Message = "Something went wrong. Please try again." });
        }
    }

    /// <summary>Runs a paged list, whose envelope already carries its own success flag.</summary>
    protected async Task<IActionResult> RunPaged<T>(Func<Task<PaginatedResponse<T>>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in {Controller}", GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiErrorResponse { Message = "Something went wrong. Please try again." });
        }
    }
}
