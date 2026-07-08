namespace Hr.Application.DTOs;

public class CandidateProfileDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public string? MiddleName { get; set; }
    public string? AlternateEmail { get; set; }
    public string? MobilePhone { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentDesignation { get; set; }
    public string? ReferredBy { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? City { get; set; }
    public int? CurrentNoticePeriodDays { get; set; }
    public DateTime? AvailableFromDate { get; set; }
    public string? PreferredWorkLocation { get; set; }
    public string? RemotePreference { get; set; }
    public string? HighestEducationLevel { get; set; }
    public string? HighestEducationField { get; set; }
    public bool PortalRegistered { get; set; }
    public int? ProfileCompletionPercent { get; set; }
    public Guid? TalentPoolId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCandidateProfileDto
{
    public Guid CandidateId { get; set; }
    public string? MiddleName { get; set; }
    public string? AlternateEmail { get; set; }
    public string? MobilePhone { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentDesignation { get; set; }
    public string? ReferredBy { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? City { get; set; }
    public int? CurrentNoticePeriodDays { get; set; }
    public DateTime? AvailableFromDate { get; set; }
    public string? PreferredWorkLocation { get; set; }
    public string? RemotePreference { get; set; }
    public string? HighestEducationLevel { get; set; }
    public string? HighestEducationField { get; set; }
    public Guid? TalentPoolId { get; set; }
}

public class UpdateCandidateProfileDto
{
    public string? MiddleName { get; set; }
    public string? AlternateEmail { get; set; }
    public string? MobilePhone { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentDesignation { get; set; }
    public string? ReferredBy { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? City { get; set; }
    public int? CurrentNoticePeriodDays { get; set; }
    public DateTime? AvailableFromDate { get; set; }
    public string? PreferredWorkLocation { get; set; }
    public string? RemotePreference { get; set; }
    public string? HighestEducationLevel { get; set; }
    public string? HighestEducationField { get; set; }
    public Guid? TalentPoolId { get; set; }
}
