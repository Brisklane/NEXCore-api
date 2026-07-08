using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Core.Infrastructure.Events;

/// <summary>
/// Resolves a city's short code for cross-module entity-code generation: uses the configured
/// <c>CityCode</c> when set, otherwise falls back to the first three letters of the city name.
/// </summary>
public class CityCodeLookupHandler : IEventHandler<CityCodeLookupEvent>
{
    private readonly CoreDbContext _db;
    private readonly ILogger<CityCodeLookupHandler> _logger;

    public CityCodeLookupHandler(CoreDbContext db, ILogger<CityCodeLookupHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(CityCodeLookupEvent e, CancellationToken ct = default)
    {
        try
        {
            var city = await _db.Cities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == e.CityId && c.IsActive, ct);

            if (city is null)
            {
                e.Result.TrySetResult(null);
                return;
            }

            var code = !string.IsNullOrWhiteSpace(city.CityCode)
                ? city.CityCode.ToUpper()
                : city.Name.Length >= 3
                    ? city.Name[..3].ToUpper()
                    : city.Name.ToUpper();

            e.Result.TrySetResult(code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CityCodeLookupHandler failed for CityId {CityId}", e.CityId);
            e.Result.TrySetResult(null);
        }
    }
}
