
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CandidateProfile : BaseEntity
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
    public string? Address { get; set; }
    public int? CurrentNoticePeriodDays { get; set; }
    public DateTime? AvailableFromDate { get; set; }
    public string? PreferredWorkLocation { get; set; }
    public string? RemotePreference { get; set; }
    public string? HighestEducationLevel { get; set; }
    public string? HighestEducationField { get; set; }
    public bool PortalRegistered { get; set; }
    public DateTime? PortalRegistrationDate { get; set; }
    public DateTime? LastPortalLoginDate { get; set; }
    public int? ProfileCompletionPercent { get; set; }
    public DateTime? ConsentDate { get; set; }
    public Guid? TalentPoolId { get; set; }
    public DateTime? TalentPoolAddedDate { get; set; }
    public Guid? TalentPoolAddedByEmployeeId { get; set; }
    public string? DiversityData { get; set; }
    public string? EEOCategory { get; set; }
    public string? VeteranStatus { get; set; }
    public string? DisabilityStatus { get; set; }
    public string? RecruiterNotes { get; set; }
    public string? BlacklistNotes { get; set; }
    public Guid? BlacklistRemovedByEmployeeId { get; set; }
    public DateTime? BlacklistRemovedAt { get; set; }
    public string? BlacklistRemovalReason { get; set; }
    public Guid? MergedIntoCandidateId { get; set; }

    public Candidate? Candidate { get; set; }
    public TalentPool? TalentPool { get; set; }
    public Employee? TalentPoolAddedByEmployee { get; set; }
    public Employee? BlacklistRemovedByEmployee { get; set; }
    public Candidate? MergedIntoCandidate { get; set; }
}
