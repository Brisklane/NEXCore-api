using System.Text.RegularExpressions;

namespace Nexcore.SharedKernel.Helpers;

/// <summary>
/// Single source of truth for company-slug ⇄ subdomain rules used across the platform.
/// A company is reached at <c>{slug}.{BaseDomain}</c> (e.g. <c>acme.nexcore.com</c>), so the
/// slug must be a valid DNS label and must never collide with a platform/host subdomain.
/// Used by registration validation (CompanyValidator), auto-slug generation (TenantService),
/// and the slug-availability endpoint.
/// </summary>
public static class SubdomainRules
{
    /// <summary>
    /// Platform / infrastructure subdomains that must never be handed to a tenant because they
    /// shadow real hosts (api.nexcore.com, www.nexcore.com, the per-tier app hosts, etc.).
    /// Additional names can be supplied per-environment via the <c>App:ReservedSubdomains</c> config.
    /// </summary>
    public static readonly IReadOnlySet<string> DefaultReserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "app", "admin", "auth", "login", "register", "signup", "signin",
        "static", "cdn", "assets", "img", "images", "media", "files", "download", "downloads",
        "mail", "email", "smtp", "imap", "pop", "ftp", "ns", "ns1", "ns2", "mx", "dns",
        "dev", "qa", "uat", "staging", "stage", "test", "demo", "sandbox", "preview", "local",
        "portal", "dashboard", "console", "status", "health", "support", "help", "helpdesk",
        "docs", "doc", "blog", "news", "about", "contact", "legal", "privacy", "terms",
        "billing", "payments", "pay", "checkout", "account", "accounts", "my", "go", "get",
        "public", "internal", "intranet", "system", "sys", "root", "secure", "vpn", "git",
        "store", "shop", "orders", "order", "pos"
    };

    /// <summary>Minimum slug length (matches the existing CompanyValidator rule).</summary>
    public const int MinLength = 3;

    /// <summary>Maximum slug length. Kept ≤ 63 (the DNS label limit) and at the app's historical cap.</summary>
    public const int MaxLength = 50;

    // DNS label: lowercase alphanumeric, hyphens allowed internally, no leading/trailing hyphen.
    private static readonly Regex LabelRegex =
        new(@"^[a-z0-9](?:[a-z0-9\-]*[a-z0-9])?$", RegexOptions.Compiled);

    /// <summary>Trim + lowercase a candidate slug. Never returns null.</summary>
    public static string Normalize(string? slug) => (slug ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>True when the (already-normalized) slug is a valid, correctly-sized DNS label.</summary>
    public static bool IsValidFormat(string slug) =>
        !string.IsNullOrEmpty(slug)
        && slug.Length >= MinLength
        && slug.Length <= MaxLength
        && LabelRegex.IsMatch(slug);

    /// <summary>
    /// True when the slug is a reserved/platform subdomain (default set ∪ optional per-env extras).
    /// Compare against an already-normalized slug.
    /// </summary>
    public static bool IsReserved(string slug, IEnumerable<string>? extraReserved = null)
    {
        if (DefaultReserved.Contains(slug))
            return true;

        if (extraReserved != null)
        {
            foreach (var name in extraReserved)
            {
                if (!string.IsNullOrWhiteSpace(name) &&
                    string.Equals(name.Trim(), slug, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Best-effort conversion of a company name into a DNS-safe slug label. Not guaranteed unique
    /// or non-reserved — callers must still dedupe (see TenantService) and reject reserved names.
    /// </summary>
    public static string Slugify(string? name)
    {
        var slug = (name ?? string.Empty).ToLowerInvariant().Replace(' ', '-').Replace('_', '-');
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        if (slug.Length > MaxLength)
            slug = slug[..MaxLength].Trim('-');
        return slug;
    }
}
