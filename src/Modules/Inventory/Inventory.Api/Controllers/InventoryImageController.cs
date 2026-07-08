using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers;

/// <summary>
/// Serves inventory item images that are stored in SQL Server (<c>inv.StoredFiles</c>).
///
/// The upload flow (<see cref="ItemController"/>) persists a URL of the form
/// <c>/api/inventory/images/{id}</c> on each <c>ItemImage</c>; this endpoint streams
/// the corresponding bytes back so an <c>&lt;img&gt;</c> tag can load them directly.
/// </summary>
[ApiController]
[Route("api/inventory/images")]
public class InventoryImageController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public InventoryImageController(InventoryDbContext db) => _db = db;

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
