using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Hr.Application.Enums;
using Nexcore.SharedKernel.Enums;

namespace Hr.Api.Controllers;

/// <summary>
/// HR Enum reference data controller.
/// Exposes all HR enums as key-value lists so the frontend can populate dropdowns
/// without hardcoding values.  All endpoints are read-only and require authentication.
/// </summary>
[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class EnumController : ControllerBase
{
    private static IEnumerable<EnumOptionDto> GetOptions<TEnum>() where TEnum : struct, Enum
        => Enum.GetValues<TEnum>()
               .Select(e => new EnumOptionDto
               {
                   Value = (int)(object)e,
                   Key = e.ToString(),
                   Label = ToLabel(e.ToString())
               });

    /// <summary>Convert a PascalCase enum name to a human-readable label.</summary>
    private static string ToLabel(string name)
    {
        // Insert a space before each uppercase letter that follows a lowercase letter
        var result = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        return result;
    }

    // Job

    /// <summary>Job record types (Job, Requisition, Direct)</summary>
    [HttpGet("job-record-types")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetJobRecordTypes()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<JobRecordType>() });

    /// <summary>Employment / contract types (FullTime, PartTime, Contract, …)</summary>
    [HttpGet("employment-types")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetEmploymentTypes()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<EmploymentType>() });

    /// <summary>Remote / work arrangement types (OnSite, Remote, Hybrid)</summary>
    [HttpGet("remote-types")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetRemoteTypes()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<RemoteType>() });

    /// <summary>Worker categories (Employee, Contractor, Consultant, …)</summary>
    [HttpGet("worker-categories")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetWorkerCategories()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<WorkerCategory>() });

    /// <summary>Vacancy reasons (NewPosition, Replacement, Expansion, …)</summary>
    [HttpGet("vacancy-reasons")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetVacancyReasons()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<VacancyReason>() });

    /// <summary>Hiring types (External, Internal, Rehire, Transfer)</summary>
    [HttpGet("hiring-types")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetHiringTypes()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<HiringType>() });

    /// <summary>Position criticality levels (Critical, High, Medium, Low)</summary>
    [HttpGet("position-criticalities")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetPositionCriticalities()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<PositionCriticality>() });

    /// <summary>Security clearance levels (None, Confidential, Secret, TopSecret)</summary>
    [HttpGet("security-clearance-levels")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSecurityClearanceLevels()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<SecurityClearanceLevel>() });

    /// <summary>EEO job category codes</summary>
    [HttpGet("eeo-categories")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetEEOCategories()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<EEOCategory>() });

    // Candidate

    /// <summary>Candidate source channels (LinkedIn, Indeed, Referral, …)</summary>
    [HttpGet("candidate-sources")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCandidateSources()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<CandidateSource>() });

    /// <summary>Candidate quality ratings (A, B, C, D)</summary>
    [HttpGet("candidate-ratings")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCandidateRatings()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<CandidateRating>() });

    // Call Log

    /// <summary>Call types (Screening, Interview, OfferDiscussion, …)</summary>
    [HttpGet("call-types")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCallTypes()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<CallType>() });

    /// <summary>Call outcomes (Interested, NotInterested, ScheduledInterview, …)</summary>
    [HttpGet("call-outcomes")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCallOutcomes()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<CallOutcome>() });

    /// <summary>Contact methods (Phone, Email, WhatsApp, VideoCall, …)</summary>
    [HttpGet("contact-methods")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetContactMethods()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<ContactMethod>() });

    /// <summary>Call directions (Inbound, Outbound)</summary>
    [HttpGet("call-directions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetCallDirections()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<CallDirection>() });

    /// <summary>Next action types after a call (ScheduleInterview, SendOffer, …)</summary>
    [HttpGet("next-action-types")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetNextActionTypes()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<NextActionType>() });

    // Interview

    /// <summary>Interview formats (InPerson, VideoCall, Phone, Panel, Technical, Assessment)</summary>
    [HttpGet("interview-formats")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetInterviewFormats()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<InterviewFormat>() });

    // Onboarding

    /// <summary>Onboarding task categories (Documentation, ITSetup, Training, …)</summary>
    [HttpGet("onboarding-task-categories")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetOnboardingTaskCategories()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<OnboardingTaskCategory>() });

    // Interview Feedback

    /// <summary>Hire readiness values (StrongYes, Yes, Maybe, No, StrongNo)</summary>
    [HttpGet("hire-readiness")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EnumOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetHireReadiness()
        => Ok(new ApiResponse<IEnumerable<EnumOptionDto>> { Success = true, Data = GetOptions<HireReadiness>() });

    // All enums in one call

    /// <summary>
    /// Returns every HR enum in a single response.
    /// Useful for bootstrapping the frontend reference data cache.
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiResponse<HrEnumReferenceDataDto>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var data = new HrEnumReferenceDataDto
        {
            JobRecordTypes = GetOptions<JobRecordType>(),
            EmploymentTypes = GetOptions<EmploymentType>(),
            RemoteTypes = GetOptions<RemoteType>(),
            WorkerCategories = GetOptions<WorkerCategory>(),
            VacancyReasons = GetOptions<VacancyReason>(),
            HiringTypes = GetOptions<HiringType>(),
            PositionCriticalities = GetOptions<PositionCriticality>(),
            SecurityClearanceLevels = GetOptions<SecurityClearanceLevel>(),
            EEOCategories = GetOptions<EEOCategory>(),
            CandidateSources = GetOptions<CandidateSource>(),
            CandidateRatings = GetOptions<CandidateRating>(),
            CallTypes = GetOptions<CallType>(),
            CallOutcomes = GetOptions<CallOutcome>(),
            ContactMethods = GetOptions<ContactMethod>(),
            CallDirections = GetOptions<CallDirection>(),
            NextActionTypes = GetOptions<NextActionType>(),
            InterviewFormats = GetOptions<InterviewFormat>(),
            OnboardingTaskCategories = GetOptions<OnboardingTaskCategory>(),
            HireReadiness = GetOptions<HireReadiness>()
        };

        return Ok(new ApiResponse<HrEnumReferenceDataDto> { Success = true, Data = data });
    }
}

/// <summary>Single enum option returned to the frontend.</summary>
public class EnumOptionDto
{
    /// <summary>Numeric value of the enum member</summary>
    public int Value { get; set; }

    /// <summary>Enum member name (safe for API round-trips)</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Human-readable label for display in dropdowns</summary>
    public string Label { get; set; } = string.Empty;
}

/// <summary>Aggregated payload returned by GET /enum/all</summary>
public class HrEnumReferenceDataDto
{
    public IEnumerable<EnumOptionDto> JobRecordTypes { get; set; } = [];
    public IEnumerable<EnumOptionDto> EmploymentTypes { get; set; } = [];
    public IEnumerable<EnumOptionDto> RemoteTypes { get; set; } = [];
    public IEnumerable<EnumOptionDto> WorkerCategories { get; set; } = [];
    public IEnumerable<EnumOptionDto> VacancyReasons { get; set; } = [];
    public IEnumerable<EnumOptionDto> HiringTypes { get; set; } = [];
    public IEnumerable<EnumOptionDto> PositionCriticalities { get; set; } = [];
    public IEnumerable<EnumOptionDto> SecurityClearanceLevels { get; set; } = [];
    public IEnumerable<EnumOptionDto> EEOCategories { get; set; } = [];
    public IEnumerable<EnumOptionDto> CandidateSources { get; set; } = [];
    public IEnumerable<EnumOptionDto> CandidateRatings { get; set; } = [];
    public IEnumerable<EnumOptionDto> CallTypes { get; set; } = [];
    public IEnumerable<EnumOptionDto> CallOutcomes { get; set; } = [];
    public IEnumerable<EnumOptionDto> ContactMethods { get; set; } = [];
    public IEnumerable<EnumOptionDto> CallDirections { get; set; } = [];
    public IEnumerable<EnumOptionDto> NextActionTypes { get; set; } = [];
    public IEnumerable<EnumOptionDto> InterviewFormats { get; set; } = [];
    public IEnumerable<EnumOptionDto> OnboardingTaskCategories { get; set; } = [];
    public IEnumerable<EnumOptionDto> HireReadiness { get; set; } = [];
}
