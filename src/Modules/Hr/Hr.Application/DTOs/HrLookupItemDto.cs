namespace Hr.Application.DTOs;

/// <summary>
/// Lightweight key/label pair used in frontend dropdown / select lists.
/// Value = the Guid stored on save; Label = human-readable text shown to the user.
/// </summary>
public class HrLookupItemDto
{
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
}

/// <summary>
/// All lookup lists required by the Job Creation form, returned in a single request.
/// </summary>
public class JobFormLookupsDto
{
    public List<HrLookupItemDto> Departments  { get; set; } = [];
    public List<HrLookupItemDto> Designations { get; set; } = [];
}
