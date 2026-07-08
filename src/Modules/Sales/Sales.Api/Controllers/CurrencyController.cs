using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Enums;

namespace Sales.Api.Controllers;

/// <summary>
/// Currency master + exchange-rate management.
///
/// Workflow:
///   1. Create currencies           POST /api/sales/currency
///   2. Add rates (any type/date)   POST /api/sales/currency/{id}/rates
///   3. Run reports using           POST /api/sales/currency/convert
///                                  POST /api/sales/currency/bulk-convert
///
/// Multiple rate types per day are supported:
///   Official (central bank), Buying, Selling, Custom
/// Multiple historical rates are kept, so reporting can pick any past date.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currency;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(ICurrencyService currency, ILogger<CurrencyController> logger)
    {
        _currency = currency;
        _logger   = logger;
    }

    // ── Currency CRUD ─────────────────────────────────────────────────────────

    /// <summary>All currencies configured for this tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CurrencyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _currency.GetAllCurrenciesAsync();
            return Ok(new ApiResponse<List<CurrencyDto>> { Success = true, Data = list, Message = $"{list.Count} currencies" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving currencies"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving currencies" }); }
    }

    /// <summary>The tenant's base/functional currency (e.g. PKR).</summary>
    [HttpGet("base")]
    [ProducesResponseType(typeof(ApiResponse<CurrencyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBase()
    {
        try
        {
            var c = await _currency.GetBaseCurrencyAsync();
            if (c == null) return NotFound(new ApiErrorResponse { Message = "Base currency not configured" });
            return Ok(new ApiResponse<CurrencyDto> { Success = true, Data = c });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving base currency"); return StatusCode(500, new ApiErrorResponse { Message = "Error" }); }
    }

    /// <summary>Currency by ISO code.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ApiResponse<CurrencyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var c = await _currency.GetCurrencyByCodeAsync(code.ToUpperInvariant());
            if (c == null) return NotFound(new ApiErrorResponse { Message = $"Currency '{code}' not found" });
            return Ok(new ApiResponse<CurrencyDto> { Success = true, Data = c });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving currency {Code}", code); return StatusCode(500, new ApiErrorResponse { Message = "Error" }); }
    }

    /// <summary>Add a new currency (e.g. USD, EUR, AED).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CurrencyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var created = await _currency.CreateCurrencyAsync(dto);
            return CreatedAtAction(nameof(GetByCode), new { code = created.Code },
                new ApiResponse<CurrencyDto> { Success = true, Data = created, Message = "Currency created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating currency"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating currency" }); }
    }

    /// <summary>Update currency name / symbol / active flag.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CurrencyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCurrencyDto dto)
    {
        try
        {
            var updated = await _currency.UpdateCurrencyAsync(id, dto);
            return Ok(new ApiResponse<CurrencyDto> { Success = true, Data = updated, Message = "Currency updated" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating currency {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating currency" }); }
    }

    // ── Exchange rates ────────────────────────────────────────────────────────

    /// <summary>All rates on record for a currency (all types, all dates).</summary>
    [HttpGet("{id:guid}/rates")]
    [ProducesResponseType(typeof(ApiResponse<List<CurrencyRateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRates(Guid id)
    {
        try
        {
            var rates = await _currency.GetRatesForCurrencyAsync(id);
            return Ok(new ApiResponse<List<CurrencyRateDto>> { Success = true, Data = rates, Message = $"{rates.Count} rate(s)" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving rates for currency {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error" }); }
    }

    /// <summary>
    /// Add an exchange rate for a specific date and rate type.
    /// Multiple types (Official, Buying, Selling) can be added for the same date.
    /// </summary>
    [HttpPost("{id:guid}/rates")]
    [ProducesResponseType(typeof(ApiResponse<CurrencyRateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddRate(Guid id, [FromBody] CreateCurrencyRateDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            dto.CurrencyId = id;
            var rate = await _currency.AddRateAsync(dto);
            return StatusCode(201, new ApiResponse<CurrencyRateDto> { Success = true, Data = rate, Message = "Rate added" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding rate for currency {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error adding rate" }); }
    }

    /// <summary>Update an existing rate (e.g. to correct a typo or set ValidUntil).</summary>
    [HttpPut("rates/{rateId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CurrencyRateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRate(Guid rateId, [FromBody] UpdateCurrencyRateDto dto)
    {
        try
        {
            var updated = await _currency.UpdateRateAsync(rateId, dto);
            return Ok(new ApiResponse<CurrencyRateDto> { Success = true, Data = updated, Message = "Rate updated" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating rate {RateId}", rateId); return StatusCode(500, new ApiErrorResponse { Message = "Error updating rate" }); }
    }

    /// <summary>Delete a rate (e.g. entered in error).</summary>
    [HttpDelete("rates/{rateId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRate(Guid rateId)
    {
        try
        {
            await _currency.DeleteRateAsync(rateId);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Rate deleted" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting rate {RateId}", rateId); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting rate" }); }
    }

    // ── Rate overview ─────────────────────────────────────────────────────────

    /// <summary>
    /// Current rate snapshot for every active currency — one row per (currency, rateType).
    /// Use this for the exchange rate dashboard or to pre-fill a document's rate.
    /// Query param: asOfDate (ISO date, default = today).
    /// </summary>
    [HttpGet("rates/latest")]
    [ProducesResponseType(typeof(ApiResponse<List<CurrencyRateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestRates([FromQuery] DateOnly? asOfDate = null)
    {
        try
        {
            var rates = await _currency.GetLatestAllRatesAsync(asOfDate);
            return Ok(new ApiResponse<List<CurrencyRateDto>> { Success = true, Data = rates, Message = $"{rates.Count} rate(s) as of {asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)}" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving latest rates"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving rates" }); }
    }

    /// <summary>
    /// Rate history for one currency between two dates.
    /// All rate types are returned — filter client-side if needed.
    /// </summary>
    [HttpGet("{code}/rates/history")]
    [ProducesResponseType(typeof(ApiResponse<List<CurrencyRateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRateHistory(
        string code,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate)
    {
        try
        {
            var history = await _currency.GetRateHistoryAsync(code.ToUpperInvariant(), fromDate, toDate);
            return Ok(new ApiResponse<List<CurrencyRateDto>> { Success = true, Data = history, Message = $"{history.Count} rate(s)" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving rate history for {Code}", code); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving history" }); }
    }

    // ── Reporting: conversion endpoints ──────────────────────────────────────

    /// <summary>
    /// Convert a single amount to another currency using the chosen rate type and date.
    ///
    /// Examples:
    ///   • Convert invoice total from USD to PKR using today's Official rate
    ///   • Convert a payment from EUR to USD using the Selling rate on the payment date
    ///
    /// Rate types available: Official, Buying, Selling, Custom
    /// If no rate is found for the requested type/date, a 400 is returned.
    /// </summary>
    [HttpPost("convert")]
    [ProducesResponseType(typeof(ApiResponse<CurrencyConversionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Convert([FromBody] CurrencyConversionRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var result = await _currency.ConvertAsync(dto);
            return Ok(new ApiResponse<CurrencyConversionResultDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error converting currency"); return StatusCode(500, new ApiErrorResponse { Message = "Error converting" }); }
    }

    /// <summary>
    /// Bulk-convert a list of document amounts to one target currency.
    ///
    /// Use case — monthly report in PKR:
    ///   Send all invoices (each with their own currency + optional document date).
    ///   Each line is converted independently. Lines already in the target currency pass through.
    ///   The response includes the total in the target currency and the per-line breakdown.
    ///
    /// Use AsOfDate in the request to force all lines to use the same date
    /// (e.g. report end date), or omit it to let each line use its own DocumentDate.
    /// </summary>
    [HttpPost("bulk-convert")]
    [ProducesResponseType(typeof(ApiResponse<BulkConversionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkConvert([FromBody] BulkConversionRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            if (dto.Lines.Count == 0) return BadRequest(new ApiErrorResponse { Message = "At least one line is required" });
            var result = await _currency.BulkConvertAsync(dto);
            return Ok(new ApiResponse<BulkConversionResultDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error bulk converting"); return StatusCode(500, new ApiErrorResponse { Message = "Error bulk converting" }); }
    }

    /// <summary>
    /// Quick rate lookup — returns just the rate value for a given pair, type, and date.
    /// Returns 404 when no rate is available.
    /// Useful for pre-filling the ExchangeRate field when creating a new order/invoice.
    /// </summary>
    /// <summary>
    /// Quick rate lookup — returns the rate value for a given pair, type, optional name, and date.
    /// Returns 404 when no matching rate is available.
    ///
    /// Examples:
    ///   GET /rate-lookup?from=USD&amp;rateType=Official
    ///   GET /rate-lookup?from=USD&amp;rateType=Buying&amp;rateName=Bank+Al-Habib
    ///   GET /rate-lookup?from=EUR&amp;to=USD&amp;rateType=Selling&amp;asOfDate=2026-05-30
    /// </summary>
    [HttpGet("rate-lookup")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LookupRate(
        [FromQuery] string from,
        [FromQuery] string? to                = null,
        [FromQuery] ExchangeRateType rateType = ExchangeRateType.Official,
        [FromQuery] string? rateName          = null,
        [FromQuery] DateOnly? asOfDate        = null)
    {
        try
        {
            var baseCurrency = await _currency.GetBaseCurrencyAsync();
            var toCode       = to?.ToUpperInvariant() ?? baseCurrency?.Code ?? "PKR";
            var date         = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var rate = await _currency.GetRateValueAsync(from.ToUpperInvariant(), toCode, rateType, date, rateName);
            if (rate == null)
            {
                var nameHint = rateName != null ? $" (name: '{rateName}')" : string.Empty;
                return NotFound(new ApiErrorResponse { Message = $"No {rateType} rate{nameHint} found for {from}/{toCode} on or before {date}" });
            }

            var nameLabel = rateName != null ? $" [{rateName}]" : string.Empty;
            return Ok(new ApiResponse<decimal>
            {
                Success = true,
                Data    = rate.Value,
                Message = $"1 {from} = {rate.Value} {toCode} ({rateType}{nameLabel}, {date})",
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error looking up rate"); return StatusCode(500, new ApiErrorResponse { Message = "Error looking up rate" }); }
    }
}
