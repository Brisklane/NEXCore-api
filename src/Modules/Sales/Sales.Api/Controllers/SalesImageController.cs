using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Sales.Infrastructure.Persistence;

namespace Sales.Api.Controllers;

/// <summary>
/// Serves Sales module images stored in the database (<c>sales.stored_files</c>),
/// such as POS receipt-template logos.
///
/// The upload flow persists a URL of the form <c>/api/sales/images/{id}</c>
/// (e.g. on a receipt template's <c>LogoUrl</c>); this endpoint streams the
/// corresponding bytes back so an <c>&lt;img&gt;</c> tag can load them directly.
/// </summary>
[ApiController]
[Route("api/sales/images")]
public class SalesImageController : ControllerBase
{
    private readonly SalesDbContext _db;

    public SalesImageController(SalesDbContext db) => _db = db;

    /// <summary>Returns the raw image bytes for the given stored-file id.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous] // images are loaded by <img> tags that can't send an auth header
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var file = await _db.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType);
    }
}
