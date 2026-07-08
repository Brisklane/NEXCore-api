
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class CandidateStageHistory : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid? FromStageLookupValueId { get; set; }
    public Guid ToStageLookupValueId { get; set; }
    public Guid ChangedByEmployeeId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public int? DaysInPreviousStage { get; set; }
    public bool IsAutomated { get; set; }
    public string? TriggerEvent { get; set; }
    public bool SLABreached { get; set; }
    public int? SLABreachHours { get; set; }
    public string? ReasonCode { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime? NotificationSentAt { get; set; }

    public Application? Application { get; set; }
    public Candidate? Candidate { get; set; }
    public Employee? ChangedByEmployee { get; set; }
    public LookupValue? FromStageLookupValue { get; set; }
    public LookupValue? ToStageLookupValue { get; set; }
}
