namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Stores and removes custom logo images for POS receipt templates.
/// Blob path: {companyId}/{branchId}/receipts/{templateId}{ext}
/// </summary>
public interface IReceiptLogoService
{
    /// <summary>
    /// Uploads an image to blob storage and returns its absolute URL.
    /// The URL is persisted on the template's LogoUrl field by the caller.
    /// </summary>
    Task<string> UploadAsync(
        Stream       stream,
        string       originalFileName,
        string       contentType,
        Guid         companyId,
        Guid         branchId,
        Guid?        businessUnitId,
        Guid         templateId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the blob at <paramref name="blobUrl"/>. No-op if null/empty.</summary>
    Task DeleteAsync(string? blobUrl, CancellationToken cancellationToken = default);
}
