namespace Hr.Application.Enums;

/// <summary>Work arrangement / remote type</summary>
public enum RemoteType
{
    OnSite,
    Remote,
    Hybrid
}

/// <summary>Worker category on a job detail</summary>
public enum WorkerCategory
{
    Employee,
    Contractor,
    Consultant,
    Intern,
    Temporary
}

/// <summary>Reason a vacancy was opened</summary>
public enum VacancyReason
{
    NewPosition,
    Replacement,
    Expansion,
    Backfill,
    Restructuring
}

/// <summary>Type of hire (internal / external)</summary>
public enum HiringType
{
    External,
    Internal,
    Rehire,
    Transfer
}

/// <summary>Criticality of a position for succession planning</summary>
public enum PositionCriticality
{
    Critical,
    High,
    Medium,
    Low
}

/// <summary>Security / clearance level required for a job</summary>
public enum SecurityClearanceLevel
{
    None,
    Confidential,
    Secret,
    TopSecret
}

/// <summary>EEO job category codes</summary>
public enum EEOCategory
{
    ExecutivesSeniorLevelManagers,
    FirstMidLevelManagers,
    Professionals,
    Technicians,
    SalesWorkers,
    AdministrativeSupport,
    CraftWorkers,
    Operatives,
    LaborersHelpers,
    ServiceWorkers
}

// Candidate / Application

/// <summary>Source channel through which a candidate applied or was sourced</summary>
public enum CandidateSource
{
    LinkedIn,
    Indeed,
    Referral,
    CareerSite,
    Agency,
    JobFair,
    Internal,
    Campus,
    Other
}

/// <summary>Rating / quality tier of a candidate</summary>
public enum CandidateRating
{
    A,
    B,
    C,
    D
}

// Call Log

/// <summary>Purpose / category of a call log entry</summary>
public enum CallType
{
    Screening,
    Interview,
    OfferDiscussion,
    FollowUp,
    Rejection,
    General
}

/// <summary>Result / next-step outcome of a call</summary>
public enum CallOutcome
{
    Interested,
    NotInterested,
    FollowUpRequired,
    ScheduledInterview,
    OfferAccepted,
    OfferDeclined,
    NoAnswer,
    LeftVoicemail,
    Rescheduled
}

/// <summary>Method used to contact the candidate</summary>
public enum ContactMethod
{
    Phone,
    Email,
    WhatsApp,
    VideoCall,
    InPerson,
    SMS
}

/// <summary>Direction of the call</summary>
public enum CallDirection
{
    Inbound,
    Outbound
}

/// <summary>Next action type after a call</summary>
public enum NextActionType
{
    ScheduleInterview,
    SendOffer,
    SendRejection,
    FollowUp,
    RequestDocuments,
    None
}

// Onboarding

/// <summary>Grouping category for an onboarding task</summary>
public enum OnboardingTaskCategory
{
    Documentation,
    ITSetup,
    Training,
    Orientation,
    Compliance,
    Benefits,
    Payroll,
    AccessSetup,
    Other
}

// Hire readiness (Interview Feedback)

/// <summary>Recruiter's hire-readiness assessment after an interview</summary>
public enum HireReadiness
{
    StrongYes,
    Yes,
    Maybe,
    No,
    StrongNo
}

/// <summary>Employee employment status</summary>
public enum EmployeeStatus
{
    Active,
    OnLeave,
    Probation,
    Terminated,
    Resigned,
    Retired,
    Suspended
}
