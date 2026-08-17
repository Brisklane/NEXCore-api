using Core.Application.Services.Interfaces;
using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Services;

/// <inheritdoc cref="ICompanyAppService"/>
public class CompanyAppService : ICompanyAppService
{
    private readonly CoreDbContext _db;

    public CompanyAppService(CoreDbContext db) => _db = db;

    public async Task<List<string>> GetInstalledAsync(Guid companyId)
        => await _db.CompanyApps.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsInstalled)
            .Select(a => a.AppKey)
            .ToListAsync();

    public async Task<List<string>> SetInstalledAsync(
        Guid companyId, IEnumerable<string> keys, bool install, Guid userId)
    {
        // Keys are normalised on the way in so "POS" and "pos" can never become two rows
        // and defeat the unique index.
        var wanted = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (wanted.Count == 0) return await GetInstalledAsync(companyId);

        var now = DateTime.UtcNow;
        var existing = await _db.CompanyApps
            .Where(a => a.CompanyId == companyId && wanted.Contains(a.AppKey))
            .ToListAsync();

        foreach (var key in wanted)
        {
            var row = existing.FirstOrDefault(a => a.AppKey == key);

            if (row is null)
            {
                // Uninstalling something never installed is a no-op, not an error — the
                // client may be reconciling after a spell offline.
                if (!install) continue;

                _db.CompanyApps.Add(new CompanyApp
                {
                    CompanyId = companyId,
                    AppKey = key,
                    IsInstalled = true,
                    InstalledAt = now,
                    InstalledByUserId = userId,
                });
                continue;
            }

            if (row.IsInstalled == install) continue;

            row.IsInstalled = install;
            if (install)
            {
                row.InstalledAt = now;
                row.InstalledByUserId = userId;
                row.UninstalledAt = null;
                row.UninstalledByUserId = null;
            }
            else
            {
                row.UninstalledAt = now;
                row.UninstalledByUserId = userId;
            }
        }

        await _db.SaveChangesAsync();
        return await GetInstalledAsync(companyId);
    }
}
