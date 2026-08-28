using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Shared plumbing for every Real Estate controller.
///
/// The exception mapping carries the weight here. The services throw
/// <see cref="InvalidOperationException"/> with a sentence written for the person at the desk —
/// "That is more than the certified entitlement allows", "The tripartite agreement has not been
/// signed" — and those sentences are the product. They are surfaced verbatim as 400 rather than
/// swallowed, because a sales executive who is told "Internal server error" while a customer is
/// sitting opposite them will ring somebody, and the answer they get will be wrong.
///
/// <see cref="UnauthorizedAccessException"/> means an approval that was needed and not given, and
/// becomes 403. Anything else is a defect: it is logged with the controller name and the caller
/// gets a neutral message, because an unexpected stack trace is not something to show a customer.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize]
public abstract class RealEstateControllerBase(ILogger logger) : ControllerBase
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

    /// <summary>Runs a paged list, whose envelope already carries its own success flag.</summary>
    protected async Task<IActionResult> RunPaged<T>(Func<Task<PaginatedResponse<T>>> action)
    {
        try
        {
            return Ok(await action());
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
}
