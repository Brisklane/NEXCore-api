using Inventory.Application.Services.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// SQL Server-backed implementation of <see cref="IBlobStorageService"/>.
///
/// Image bytes are stored in the <c>inv.StoredFiles</c> table and the method
/// returns a relative API URL (<c>/api/inventory/images/{id}</c>) that
/// <c>InventoryImageController</c> serves. This replaces Azure Blob Storage so
/// the platform runs fully locally against SQL Server with no external object store.
/// </summary>
public class SqlImageStorageService : IBlobStorageService
{
    /// <summary>Relative URL prefix for served images; also used to parse the id back out.</summary>
    public const string UrlPrefix = "/api/inventory/images/";

    private readonly InventoryDbContext _db;
    private readonly ILogger<SqlImageStorageService> _logger;

    public SqlImageStorageService(InventoryDbContext db, ILogger<SqlImageStorageService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> UploadInventoryImageAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        Guid companyId,
        Guid branchId,
        Guid? businessUnitId,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var file = new StoredFile
        {
            Id = Guid.NewGuid(),
            FileName = Path.GetFileName(originalFileName),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Content = bytes,
            SizeBytes = bytes.LongLength,
            CompanyId = companyId,
            BranchId = branchId,
            BusinessUnitId = businessUnitId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _db.StoredFiles.Add(file);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stored inventory image {Id} ({Size} bytes) in SQL Server", file.Id, bytes.LongLength);

        return $"{UrlPrefix}{file.Id}";
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(blobUrl, out var id))
            return; // not one of ours — nothing to delete

        var file = await _db.StoredFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (file is null)
            return;

        _db.StoredFiles.Remove(file);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Deleted stored inventory image {Id}", id);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// SQL-served images need no signed URL — the stored relative URL is already
    /// directly loadable by the browser, so the value is returned unchanged.
    /// </remarks>
    public string GenerateSasUrl(string blobUrl, TimeSpan? expiry = null) => blobUrl;

    /// <summary>Extracts the trailing Guid from a <c>/api/inventory/images/{id}</c> URL.</summary>
    internal static bool TryParseId(string? url, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var slash = url.LastIndexOf('/');
        var token = slash >= 0 ? url[(slash + 1)..] : url;
        return Guid.TryParse(token, out id);
    }
}
