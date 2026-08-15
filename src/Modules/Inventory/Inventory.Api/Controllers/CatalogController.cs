using Inventory.Application.DTOs;
using Inventory.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Inventory.Api.Controllers;

/// <summary>
/// Catalogue identity for selling surfaces (POS, order entry, stock counts).
///
/// <para>Product identity lives across four columns — item SKU, item barcode, variant SKU,
/// variant barcode — and a barcode may be registered against a non-base unit (a case of
/// six). Resolving that correctly is easy to get wrong, so callers ask these endpoints
/// rather than querying the tables themselves.</para>
/// </summary>
[ApiController]
[Route("api/inventory/catalog")]
[Authorize]
[Produces("application/json")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogResolutionService _catalog;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ICatalogResolutionService catalog, ILogger<CatalogController> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    /// <summary>
    /// Resolve a scanned or typed code to a sellable line.
    ///
    /// Exact identifier matches win, most specific first: item barcode → variant barcode →
    /// variant SKU → item SKU. The response says which one matched, and carries
    /// <c>quantityInBaseUnits</c> — scan a case-of-6 barcode and that is 6, so the caller
    /// sells a case rather than a bottle.
    ///
    /// A code that matches nothing returns <c>isExactMatch: false</c> with name candidates
    /// (unless <paramref name="fallbackToSearch"/> is false), never a silent wrong product.
    /// </summary>
    /// <param name="code">Scanner or keyboard input.</param>
    /// <param name="fallbackToSearch">Return name candidates on a miss. Default true.</param>
    [HttpGet("resolve")]
    [ProducesResponseType(typeof(ApiResponse<CatalogResolveResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Resolve(
        [FromQuery] string code,
        [FromQuery] bool fallbackToSearch = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new ApiErrorResponse { Message = "Query parameter 'code' is required" });

        try
        {
            var result = await _catalog.ResolveAsync(code, fallbackToSearch, ct);

            return Ok(new ApiResponse<CatalogResolveResultDto>
            {
                Success = true,
                Data = result,
                Message = result.IsExactMatch
                    ? $"Matched on {result.Match!.MatchType}"
                    : $"No exact match; {result.Candidates.Count} candidate(s)",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving catalogue code {Code}", code);
            return StatusCode(500, new ApiErrorResponse { Message = "Error resolving code" });
        }
    }

    /// <summary>
    /// A page of catalogue changes since the caller's watermark, for tills that keep a
    /// local index instead of downloading the whole catalogue on every start.
    ///
    /// Omit <paramref name="since"/> for a first full sync, then replay with the
    /// <c>nextSince</c>/<c>nextSinceId</c> from the previous page while <c>hasMore</c> is
    /// true. Responses include tombstones (<c>isDeleted</c>) so withdrawn or deactivated
    /// products actually disappear from tills, and each barcode carries
    /// <c>quantityInBaseUnits</c> so pack scanning stays correct offline.
    /// </summary>
    /// <param name="since">Highest <c>changedAt</c> already held.</param>
    /// <param name="sinceId">Item id at that timestamp; disambiguates rows sharing it.</param>
    /// <param name="pageSize">Rows per page, 1–1000 (default 500).</param>
    [HttpGet("sync")]
    [ProducesResponseType(typeof(ApiResponse<CatalogSyncPageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync(
        [FromQuery] DateTime? since,
        [FromQuery] Guid? sinceId,
        [FromQuery] int pageSize = 500,
        CancellationToken ct = default)
    {
        try
        {
            var page = await _catalog.GetSyncPageAsync(since, sinceId, pageSize, ct);

            return Ok(new ApiResponse<CatalogSyncPageDto>
            {
                Success = true,
                Data = page,
                Message = $"{page.Entries.Count} change(s){(page.HasMore ? ", more available" : "")}",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building catalogue sync page since {Since}", since);
            return StatusCode(500, new ApiErrorResponse { Message = "Error building catalogue sync page" });
        }
    }

    /// <summary>
    /// Free-text catalogue search over item names, item SKUs, variant SKUs and barcodes.
    /// Case-insensitive, and ranks exact/prefix hits above contains-hits. Intended for the
    /// till's product search box — it returns the same shape as <c>resolve</c> so one
    /// renderer handles both.
    /// </summary>
    /// <param name="q">Search term.</param>
    /// <param name="limit">Max results, 1–100 (default 25).</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<CatalogResolutionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 25,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new ApiErrorResponse { Message = "Query parameter 'q' is required" });

        try
        {
            var results = await _catalog.SearchAsync(q, limit, ct);

            return Ok(new ApiResponse<List<CatalogResolutionDto>>
            {
                Success = true,
                Data = results,
                Message = $"{results.Count} result(s)",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching catalogue for {Query}", q);
            return StatusCode(500, new ApiErrorResponse { Message = "Error searching catalogue" });
        }
    }
}
