namespace Core.Domain.Entities;

/// <summary>
/// One app a company has installed.
/// </summary>
/// <remarks>
/// Only the key is stored. Everything else about an app — its name, icon, which URLs it
/// owns — is UI metadata that lives in the client's app registry, so shipping a new app
/// never needs a schema change or a data migration.
///
/// This is separate from <see cref="SubscriptionPlan.AllowedModules"/> on purpose: the
/// plan says what a tenant is *permitted* to run, this says what the company has actually
/// *chosen* to switch on. An app has to clear both to appear.
///
/// Rows are kept when an app is uninstalled (<see cref="IsInstalled"/> goes false) so that
/// removing an app and adding it back is not indistinguishable from never having had it —
/// the install history is useful for support and for billing questions.
/// </remarks>
public class CompanyApp
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>Matches a key in the client's app registry, e.g. "pos", "accounting".</summary>
    public required string AppKey { get; set; }

    public bool IsInstalled { get; set; } = true;

    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public Guid? InstalledByUserId { get; set; }

    public DateTime? UninstalledAt { get; set; }
    public Guid? UninstalledByUserId { get; set; }
}
