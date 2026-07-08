using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Core.Infrastructure.Events;

/// <summary>Answers a cross-module request for a company's logo bytes, returning null if the company is missing or has none.</summary>
public class CompanyLogoLookupHandler : IEventHandler<CompanyLogoLookupEvent>
{
    private readonly CoreDbContext _db;
    private readonly ILogger<CompanyLogoLookupHandler> _logger;

    public CompanyLogoLookupHandler(CoreDbContext db, ILogger<CompanyLogoLookupHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(CompanyLogoLookupEvent e, CancellationToken ct = default)
    {
        try
        {
            var logo = await _db.Companies
                .AsNoTracking()
                .Where(c => c.Id == e.CompanyId && !c.IsDeleted)
                .Select(c => c.CompanyLogo)
                .FirstOrDefaultAsync(ct);

            e.Result.TrySetResult(logo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompanyLogoLookupHandler failed for CompanyId {CompanyId}", e.CompanyId);
            e.Result.TrySetResult(null);
        }
    }
}
