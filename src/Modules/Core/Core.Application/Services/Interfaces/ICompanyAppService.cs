namespace Core.Application.Services.Interfaces;

/// <summary>
/// Which apps a company has installed.
/// </summary>
/// <remarks>
/// Only keys are stored. The catalogue — names, icons, dependencies, which URLs an app
/// owns — is client-side metadata, so shipping a new app needs no schema change here.
/// </remarks>
public interface ICompanyAppService
{
    /// <summary>Keys currently installed for this company.</summary>
    Task<List<string>> GetInstalledAsync(Guid companyId);

    /// <summary>
    /// Turns apps on or off and returns the resulting installed set.
    ///
    /// Uninstalling flips a flag rather than deleting the row, so install history
    /// survives — useful when someone asks why an app disappeared.
    /// </summary>
    Task<List<string>> SetInstalledAsync(Guid companyId, IEnumerable<string> keys, bool install, Guid userId);
}
