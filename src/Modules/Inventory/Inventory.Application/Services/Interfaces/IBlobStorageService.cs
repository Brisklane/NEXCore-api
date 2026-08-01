namespace Inventory.Application.Services.Interfaces;

/// <summary>
/// Uploads and manages inventory images. The active implementation
/// (<c>SqlImageStorageService</c>) stores the bytes in the database and returns a
/// relative API URL (<c>/api/inventory/images/{id}</c>) that the image endpoint serves.
///
/// This interface lives in Application so controllers and services can depend
/// on it without referencing Infrastructure.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file stream to the tenant-scoped inventory path and returns the
    /// public HTTPS URL of the uploaded blob.
    /// </summary>
    /// <param name="fileStream">The raw file stream to upload.</param>
    /// <param name="originalFileName">Used to extract the file extension.</param>
    /// <param name="contentType">MIME type (e.g., "image/jpeg").</param>
    /// <param name="companyId">Top-level tenant segment.</param>
    /// <param name="branchId">Second tenant segment.</param>
    /// <param name="businessUnitId">Third tenant segment - omitted when null.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>
    ///     Absolute HTTPS URL of the uploaded blob, e.g.
    ///     https://&lt;account&gt;.blob.core.windows.net/inventory/{companyId}/{branchId}/{businessUnitId}/inventory/abc.jpg
    /// </returns>
    Task<string> UploadInventoryImageAsync(
        Stream    fileStream,
        string    originalFileName,
        string    contentType,
        Guid      companyId,
        Guid      branchId,
        Guid?     businessUnitId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob by its full URL. Silently succeeds if the blob does not exist.
    /// </summary>
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a short-lived SAS URL for a private blob so authenticated API
    /// responses can serve image URLs that the browser can load directly.
    /// Returns the original URL unchanged if SAS generation is not available
    /// (e.g. the URL belongs to a different host or the credential is token-based).
    /// </summary>
    string GenerateSasUrl(string blobUrl, TimeSpan? expiry = null);
}
