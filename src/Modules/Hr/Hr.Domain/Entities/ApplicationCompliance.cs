
using Nexcore.SharedKernel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hr.Domain.Entities;

public class ApplicationCompliance : BaseEntity
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

    public Application? Application { get; set; }
    public LookupValue? BackgroundCheckStatusLookupValue { get; set; }
    public LookupValue? DrugTestStatusLookupValue { get; set; }
}
