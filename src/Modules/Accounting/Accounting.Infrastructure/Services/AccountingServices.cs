using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of account category service
/// </summary>
public class AccountCategoryService : IAccountCategoryService
{
    private readonly IAccountCategoryRepository _repository;
    private readonly ILogger<AccountCategoryService> _logger;

    public AccountCategoryService(IAccountCategoryRepository repository, ILogger<AccountCategoryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<AccountCategoryDto>> GetActiveAsync(PaginationParams pagination)
    {
        try
        {
            // Company-wide query (ignores BranchId/BusinessUnitId — categories are company-level data)
            var allActive = (await _repository.GetActiveAsync()).ToList();

            if (allActive.Count == 0 && pagination.PageNumber == 1)
            {
                try
                {
                    await SeedStandardCategoriesAsync();
                }
                catch (Exception seedEx)
                {
                    _logger.LogWarning(seedEx, "Standard category seeding failed (possibly concurrent): {Error}", seedEx.Message);
                }
                allActive = (await _repository.GetActiveAsync()).ToList();
            }

            var search = pagination.SearchTerm?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                allActive = allActive.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    (c.Description != null && c.Description.ToLower().Contains(search))).ToList();
            }

            var ordered = allActive.AsQueryable()
                .ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Name")
                .ToList();

            var total = ordered.Count;
            var items = ordered
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize);

            return PaginatedResponse<AccountCategoryDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active account categories");
            throw;
        }
    }

    private async Task SeedStandardCategoriesAsync()
    {
        var now = DateTime.UtcNow;
        var standard = new List<AccountCategory>
        {
            new() { Name = "Current Assets",        Type = AccountType.Asset,        NormalBalance = NormalBalance.Debit,  Description = "Short-term assets expected to be converted to cash within one year", IsActive = true, CreatedAt = now },
            new() { Name = "Fixed Assets",          Type = AccountType.Asset,        NormalBalance = NormalBalance.Debit,  Description = "Long-term assets with useful life greater than one year",            IsActive = true, CreatedAt = now },
            new() { Name = "Current Liabilities",   Type = AccountType.Liability,    NormalBalance = NormalBalance.Credit, Description = "Obligations due within one year",                                    IsActive = true, CreatedAt = now },
            new() { Name = "Long-term Liabilities", Type = AccountType.Liability,    NormalBalance = NormalBalance.Credit, Description = "Obligations due after one year",                                     IsActive = true, CreatedAt = now },
            new() { Name = "Shareholders' Equity",  Type = AccountType.Equity,       NormalBalance = NormalBalance.Credit, Description = "Owner's investment and retained earnings",                           IsActive = true, CreatedAt = now },
            new() { Name = "Sales Revenue",         Type = AccountType.Revenue,      NormalBalance = NormalBalance.Credit, Description = "Income from product/service sales",                                  IsActive = true, CreatedAt = now },
            new() { Name = "Cost of Goods Sold",    Type = AccountType.Expense,      NormalBalance = NormalBalance.Debit,  Description = "Direct costs of producing goods sold",                               IsActive = true, CreatedAt = now },
            new() { Name = "Operating Expenses",    Type = AccountType.Expense,      NormalBalance = NormalBalance.Debit,  Description = "Expenses incurred in normal business operations",                    IsActive = true, CreatedAt = now },
            new() { Name = "Other Income",          Type = AccountType.OtherIncome,  NormalBalance = NormalBalance.Credit, Description = "Non-operating income",                                               IsActive = true, CreatedAt = now },
            new() { Name = "Other Expenses",        Type = AccountType.OtherExpense, NormalBalance = NormalBalance.Debit,  Description = "Non-operating expenses",                                             IsActive = true, CreatedAt = now },
        };
        await _repository.AddCompanyWideRangeAsync(standard);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} standard account categories for company", standard.Count);
    }

    public async Task<AccountCategoryDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var category = await _repository.GetByIdAsync(id);
            return category == null ? null : MapToDto(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account category: {CategoryId}", id);
            throw;
        }
    }

    private static AccountCategoryDto MapToDto(AccountCategory category)
    {
        return new AccountCategoryDto
        {
            Id = category.Id,
            CompanyId = category.CompanyId,
            Name = category.Name,
            Type = category.Type.ToString(),
            NormalBalance = category.NormalBalance.ToString(),
            Description = category.Description,
            IsActive = category.IsActive
        };
    }
}
