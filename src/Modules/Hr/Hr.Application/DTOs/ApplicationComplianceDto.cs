namespace Hr.Application.DTOs;

public class ApplicationComplianceDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ApplicationId { get; set; }
    public bool EEODataCaptured { get; set; }
    public Guid? BackgroundCheckStatusLookupValueId { get; set; }
    public DateTime? BackgroundCheckDate { get; set; }
    public Guid? DrugTestStatusLookupValueId { get; set; }
    public DateTime? DrugTestDate { get; set; }
    public bool RightToWorkVerified { get; set; }
    public string? RightToWorkDocumentUrl { get; set; }
    public bool VisaSponsorshipRequired { get; set; }
    public bool DataConsentGiven { get; set; }
    public DateTime? DataConsentDate { get; set; }
    public DateTime? DataRetentionExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateApplicationComplianceDto
{
    public Guid ApplicationId { get; set; }
    public bool EEODataCaptured { get; set; }
    public Guid? BackgroundCheckStatusLookupValueId { get; set; }
    public DateTime? BackgroundCheckDate { get; set; }
    public Guid? DrugTestStatusLookupValueId { get; set; }
    public DateTime? DrugTestDate { get; set; }
    public bool RightToWorkVerified { get; set; }
    public string? RightToWorkDocumentUrl { get; set; }
    public bool VisaSponsorshipRequired { get; set; }
    public bool DataConsentGiven { get; set; }
    public DateTime? DataConsentDate { get; set; }
    public DateTime? DataRetentionExpiryDate { get; set; }
}

public class UpdateApplicationComplianceDto
{
    public bool? EEODataCaptured { get; set; }
    public Guid? BackgroundCheckStatusLookupValueId { get; set; }
    public DateTime? BackgroundCheckDate { get; set; }
    public Guid? DrugTestStatusLookupValueId { get; set; }
    public DateTime? DrugTestDate { get; set; }
    public bool? RightToWorkVerified { get; set; }
    public string? RightToWorkDocumentUrl { get; set; }
    public bool? VisaSponsorshipRequired { get; set; }
    public bool? DataConsentGiven { get; set; }
    public DateTime? DataConsentDate { get; set; }
    public DateTime? DataRetentionExpiryDate { get; set; }
}
