using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Microsoft.Extensions.Logging;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Persistence;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Database-backed implementation of <see cref="IReceiptLogoService"/>.
///
/// Logo bytes are stored in the <c>sales.stored_files</c> table and the method
/// returns a relative API URL (<c>/api/sales/images/{id}</c>) that
/// <c>SalesImageController</c> serves. Replaces Azure Blob Storage so the platform
/// runs fully locally against PostgreSQL with no external object store.
/// </summary>
public class SqlReceiptLogoService : IReceiptLogoService
{
    /// <summary>Relative URL prefix for served logos; also used to parse the id back out.</summary>
    public const string UrlPrefix = "/api/sales/images/";

    private readonly SalesDbContext _db;
    private readonly ILogger<SqlReceiptLogoService> _logger;

    public SqlReceiptLogoService(SalesDbContext db, ILogger<SqlReceiptLogoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        Guid companyId,
        Guid branchId,
        Guid? businessUnitId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
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
            "Stored receipt logo {Id} for template {TemplateId} ({Size} bytes) in SQL Server",
            file.Id, templateId, bytes.LongLength);

        return $"{UrlPrefix}{file.Id}";
    }

    public async Task DeleteAsync(string? blobUrl, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(blobUrl, out var id))
            return; // null/empty or not one of ours — nothing to delete

        var file = await _db.StoredFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (file is null)
            return;

        _db.StoredFiles.Remove(file);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Deleted stored receipt logo {Id}", id);
    }

    /// <summary>Extracts the trailing Guid from a <c>/api/sales/images/{id}</c> URL.</summary>
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
