using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Core.Infrastructure.Events;

/// <summary>
/// Validates a context-switch request from the Auth module: confirms the company belongs to the
/// caller's tenant, the branch belongs to that company, and the business unit belongs to that
/// branch. Mirrors the predicates used by the Get-all list endpoints so validation accepts exactly
/// what the header dropdowns offer. The TenantId check is explicit (with IgnoreQueryFilters) so the
/// security boundary does not depend on the ambient request's tenant filter.
/// </summary>
public class ContextValidationHandler : IEventHandler<ContextValidationRequestedEvent>
{
    private readonly CoreDbContext _db;
    private readonly ILogger<ContextValidationHandler> _logger;

    public ContextValidationHandler(CoreDbContext db, ILogger<ContextValidationHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(ContextValidationRequestedEvent e, CancellationToken ct = default)
    {
        try
        {
            // Company must exist, be active, and belong to the caller's tenant.
            var company = await _db.Companies
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(c => c.Id == e.CompanyId && c.TenantId == e.TenantId && c.IsActive && !c.IsDeleted)
                .Select(c => new { c.CompanyName, c.Slug })
                .FirstOrDefaultAsync(ct);

            if (company is null)
            {
                e.Result.TrySetResult(ContextValidationResult.Invalid);
                return;
            }

            // Branch must belong to that company.
            var branchOk = await _db.Branches
                .AsNoTracking()
                .AnyAsync(b => b.Id == e.BranchId && b.CompanyId == e.CompanyId && !b.IsDeleted, ct);

            if (!branchOk)
            {
                e.Result.TrySetResult(ContextValidationResult.Invalid);
                return;
            }

            // Business unit must belong to that branch.
            var buOk = await _db.BusinessUnits
                .AsNoTracking()
                .AnyAsync(bu => bu.Id == e.BusinessUnitId && bu.BranchId == e.BranchId && !bu.IsDeleted, ct);

            if (!buOk)
            {
                e.Result.TrySetResult(ContextValidationResult.Invalid);
                return;
            }

            e.Result.TrySetResult(new ContextValidationResult(true, company.CompanyName, company.Slug));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ContextValidationHandler failed for Company {CompanyId}", e.CompanyId);
            e.Result.TrySetResult(ContextValidationResult.Invalid);
        }
    }
}
