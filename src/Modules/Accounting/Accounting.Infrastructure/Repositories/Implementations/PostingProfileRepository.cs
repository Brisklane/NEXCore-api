using Microsoft.AspNetCore.Http;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Accounting.Infrastructure.Repositories.Implementations;

/// <summary>
/// PostingProfile repository implementation using TenantAwareRepository
/// Automatically filters all queries by CompanyId, BranchId, BusinessUnitId from JWT context
/// </summary>
public class PostingProfileRepository : TenantAwareRepository<PostingProfile>, IPostingProfileRepository
{
    public PostingProfileRepository(AccountingDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    /// <summary>
    /// Get posting profile by type
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<PostingProfile?> GetByTypeAsync(string moduleName, string transactionType)
    {
        return await FirstOrDefaultAsync(p => p.ModuleName == moduleName 
            && p.TransactionType == transactionType 
            && p.IsActive);
    }

    /// <summary>
    /// Get posting profiles by module
    /// Tenant context automatically applied by base class
    /// </summary>
    public async Task<IEnumerable<PostingProfile>> GetByModuleAsync(string moduleName)
    {
        return await FindAsync(p => p.ModuleName == moduleName && p.IsActive);
    }
}
