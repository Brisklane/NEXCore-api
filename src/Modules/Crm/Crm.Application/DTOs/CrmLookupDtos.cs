namespace Crm.Application.DTOs;

/// <summary>
/// A generic key/label pair used in frontend dropdown lists.
/// </summary>
public class CrmLookupItemDto
{
    /// <summary>The stored value (passed to the API on save)</summary>
    public string Value { get; set; } = null!;

    /// <summary>Human-readable label shown in the dropdown</summary>
    public string Label { get; set; } = null!;
}

/// <summary>
/// All CRM lookup lists bundled in a single response.
/// Call GET /api/crm-lookup to pre-load all dropdowns in one request.
/// </summary>
public class CrmLookupsDto
{
    public List<CrmLookupItemDto> LeadStatuses { get; set; } = [];
    public List<CrmLookupItemDto> Salutations { get; set; } = [];
    public List<CrmLookupItemDto> LeadSources { get; set; } = [];
    public List<CrmLookupItemDto> Industries { get; set; } = [];
    public List<CrmLookupItemDto> Countries { get; set; } = [];
    public List<CrmLookupItemDto> StatesProvinces { get; set; } = [];
    public List<CrmLookupItemDto> AccountTypes { get; set; } = [];
    public List<CrmLookupItemDto> DealStages { get; set; } = [];
    public List<CrmLookupItemDto> ForecastCategories { get; set; } = [];
    public List<CrmLookupItemDto> CaseStatuses { get; set; } = [];
    public List<CrmLookupItemDto> CaseOrigins { get; set; } = [];
    public List<CrmLookupItemDto> CasePriorities { get; set; } = [];
}
