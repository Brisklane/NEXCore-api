namespace Crm.Application.DTOs;

/// <summary>
/// Recent contact summary for CRM home dashboard
/// </summary>
public class RecentContactSummaryDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// Recent account summary for CRM home dashboard
/// </summary>
public class RecentAccountSummaryDto
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Type { get; set; }
}

/// <summary>
/// CRM home dashboard data
/// </summary>
public class CrmHomeDashboardDto
{
    /// <summary>
    /// Count of leads grouped by status
    /// </summary>
    public Dictionary<string, int> LeadsByStatus { get; set; } = new();

    /// <summary>
    /// Count of deals grouped by stage
    /// </summary>
    public Dictionary<string, int> DealsByStage { get; set; } = new();

    /// <summary>
    /// Count of cases grouped by priority
    /// </summary>
    public Dictionary<string, int> CasesByPriority { get; set; } = new();

    /// <summary>
    /// Last 5 contacts sorted by created date descending
    /// </summary>
    public List<RecentContactSummaryDto> RecentContacts { get; set; } = new();

    /// <summary>
    /// Last 5 accounts sorted by created date descending
    /// </summary>
    public List<RecentAccountSummaryDto> RecentAccounts { get; set; } = new();
}
