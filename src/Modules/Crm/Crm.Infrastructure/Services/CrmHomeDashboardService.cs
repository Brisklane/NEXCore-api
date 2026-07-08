using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class CrmHomeDashboardService : ICrmHomeDashboardService
{
    private readonly ILeadRepository _leadRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<CrmHomeDashboardService> _logger;

    public CrmHomeDashboardService(
        ILeadRepository leadRepository,
        IDealRepository dealRepository,
        ICaseRepository caseRepository,
        IContactRepository contactRepository,
        IAccountRepository accountRepository,
        ILogger<CrmHomeDashboardService> logger)
    {
        _leadRepository = leadRepository;
        _dealRepository = dealRepository;
        _caseRepository = caseRepository;
        _contactRepository = contactRepository;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<CrmHomeDashboardDto> GetDashboardAsync()
    {
        var dashboard = new CrmHomeDashboardDto();

        try
        {
            // Run all queries in parallel for better performance
            var leadCountsByStatusTask = GetLeadCountsByStatusAsync();
            var dealCountsByStageTask = GetDealCountsByStageAsync();
            var caseCountsByPriorityTask = GetCaseCountsByPriorityAsync();
            var recentContactsTask = GetRecentContactsAsync();
            var recentAccountsTask = GetRecentAccountsAsync();

            await Task.WhenAll(
                leadCountsByStatusTask,
                dealCountsByStageTask,
                caseCountsByPriorityTask,
                recentContactsTask,
                recentAccountsTask);

            dashboard.LeadsByStatus = await leadCountsByStatusTask;
            dashboard.DealsByStage = await dealCountsByStageTask;
            dashboard.CasesByPriority = await caseCountsByPriorityTask;
            dashboard.RecentContacts = await recentContactsTask;
            dashboard.RecentAccounts = await recentAccountsTask;

            _logger.LogInformation("CRM dashboard data retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CRM dashboard data");
            throw;
        }

        return dashboard;
    }

    private async Task<Dictionary<string, int>> GetLeadCountsByStatusAsync()
    {
        // Known lead statuses - must include all possible values
        var leadStatuses = new[] { "New", "Contacted", "Nurturing", "Qualified", "Unqualified" };
        var result = new Dictionary<string, int>();

        foreach (var status in leadStatuses)
        {
            result[status] = 0;
        }

        // Get leads for all statuses and count them
        try
        {
            // Get first page with large page size to count all leads
            // This is a workaround - ideally we'd have a GroupBy method in the repository
            var (leads, _) = await _leadRepository.SearchPagedAsync(1, 10000, null, null);
            foreach (var lead in leads)
            {
                if (result.ContainsKey(lead.Status))
                {
                    result[lead.Status]++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting leads by status");
        }

        return result;
    }

    private async Task<Dictionary<string, int>> GetDealCountsByStageAsync()
    {
        // Known deal stages - must include all 6 stages
        var dealStages = new[] { "Qualify", "Meet/Present", "Propose", "Negotiate", "Closed Won", "Closed Lost" };
        var result = new Dictionary<string, int>();

        foreach (var stage in dealStages)
        {
            result[stage] = 0;
        }

        // Get deals for all stages and count them
        try
        {
            var (deals, _) = await _dealRepository.SearchPagedAsync(1, 10000, null, null);
            foreach (var deal in deals)
            {
                if (result.ContainsKey(deal.Stage))
                {
                    result[deal.Stage]++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting deals by stage");
        }

        return result;
    }

    private async Task<Dictionary<string, int>> GetCaseCountsByPriorityAsync()
    {
        // Known case priorities
        var casePriorities = new[] { "Critical", "High", "Medium", "Low" };
        var result = new Dictionary<string, int>();

        foreach (var priority in casePriorities)
        {
            result[priority] = 0;
        }

        // Get cases for all priorities and count them
        try
        {
            var (cases, _) = await _caseRepository.GetPagedByStatusAsync(1, 10000, null);
            foreach (var caseEntity in cases)
            {
                if (result.ContainsKey(caseEntity.Priority))
                {
                    result[caseEntity.Priority]++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting cases by priority");
        }

        return result;
    }

    private async Task<List<RecentContactSummaryDto>> GetRecentContactsAsync()
    {
        try
        {
            // Get recent contacts (max 5, sorted by created date descending)
            var (contacts, _) = await _contactRepository.SearchPagedAsync(1, 5, null);

            var result = new List<RecentContactSummaryDto>();
            foreach (var contact in contacts.OrderByDescending(c => c.CreatedAt).Take(5))
            {
                string? accountName = null;
                if (contact.AccountId.HasValue)
                {
                    var account = await _accountRepository.GetByIdAsync(contact.AccountId.Value);
                    accountName = account?.AccountName;
                }

                result.Add(new RecentContactSummaryDto
                {
                    Id = contact.Id,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    AccountName = accountName,
                    Email = contact.Email
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent contacts");
            return new List<RecentContactSummaryDto>();
        }
    }

    private async Task<List<RecentAccountSummaryDto>> GetRecentAccountsAsync()
    {
        try
        {
            // Get recent accounts (max 5, sorted by created date descending)
            var (accounts, _) = await _accountRepository.SearchPagedAsync(1, 5, null);

            return accounts
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new RecentAccountSummaryDto
                {
                    Id = a.Id,
                    AccountName = a.AccountName,
                    Industry = a.Industry,
                    Type = a.Type
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent accounts");
            return new List<RecentAccountSummaryDto>();
        }
    }
}
