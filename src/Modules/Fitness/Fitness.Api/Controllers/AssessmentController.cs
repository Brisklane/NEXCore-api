using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Body composition, goals, nutrition plans and habits.
///
/// Everything here is special-category data. Reads are audited, access is restricted to staff
/// whose role allows it, and progress photos need the member's explicit consent before they can
/// be stored or shown. That is not caution for its own sake — a leaked body-fat reading is a
/// different kind of harm from a leaked email address.
/// </summary>
[Route("api/fitness/assessments")]
public class AssessmentController(
    IAssessmentService assessments,
    ILogger<AssessmentController> logger) : FitnessControllerBase(logger)
{
    [HttpGet("templates")]
    public Task<IActionResult> GetTemplates([FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true)
        => Run(() => assessments.GetTemplatesAsync(clubId, activeOnly));

    [HttpPost("templates")]
    public Task<IActionResult> SaveTemplate([FromBody] AssessmentTemplateDto request, [FromQuery] Guid? id = null)
        => Run(() => assessments.SaveTemplateAsync(id, request, UserId), "Template saved.");

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] Guid? staffId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => assessments.ListAsync(clubId, memberId, staffId, from, to,
            pagination ?? new PaginationParams()));

    /// <summary>Reading one assessment is an audited event, because of what it contains.</summary>
    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => assessments.GetAsync(id, UserId), "Assessment not found.");

    /// <summary>
    /// Records the measurements taken.
    ///
    /// The trainer enters what they measured; BMI, fat mass, lean mass and waist-hip ratio are
    /// worked out from those, and every value comes back with the change since last time and
    /// whether that change is in the right direction.
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Record([FromBody] RecordAssessmentDto request)
        => Run(() => assessments.RecordAsync(request, UserId), "Assessment saved.");

    /// <summary>One series per measure, for the charts on the member's progress screen.</summary>
    [HttpGet("progress/{memberId:guid}")]
    public Task<IActionResult> GetProgress(Guid memberId, [FromQuery] List<string>? measureNames)
        => Run(() => assessments.GetProgressAsync(memberId, measureNames, UserId));

    // ── Photos ───────────────────────────────────────────────────────────────

    /// <summary>Refused unless the member has consented to progress photos being held.</summary>
    [HttpPost("photos")]
    public Task<IActionResult> AddPhoto([FromBody] ProgressPhotoDto request)
        => Run(() => assessments.AddPhotoAsync(request, UserId), "Photo saved.");

    [HttpGet("photos/{memberId:guid}")]
    public Task<IActionResult> GetPhotos(Guid memberId)
        => Run(() => assessments.GetPhotosAsync(memberId, UserId));

    // ── Goals ────────────────────────────────────────────────────────────────

    [HttpGet("goals/{memberId:guid}")]
    public Task<IActionResult> GetGoals(Guid memberId, [FromQuery] bool activeOnly = true)
        => Run(() => assessments.GetGoalsAsync(memberId, activeOnly));

    [HttpPost("goals")]
    public Task<IActionResult> SaveGoal([FromBody] MemberGoalDto request, [FromQuery] Guid? id = null)
        => Run(() => assessments.SaveGoalAsync(id, request, UserId), "Goal saved.");

    // ── Nutrition ────────────────────────────────────────────────────────────

    /// <summary>
    /// A nutrition plan, which always carries a disclaimer.
    ///
    /// A personal trainer is not a dietitian, and a plan that reads like clinical advice is a plan
    /// that will one day be read that way by somebody's solicitor.
    /// </summary>
    [HttpPost("nutrition")]
    public Task<IActionResult> SaveNutritionPlan([FromBody] NutritionPlanDto request, [FromQuery] Guid? id = null)
        => Run(() => assessments.SaveNutritionPlanAsync(id, request, UserId), "Plan saved.");

    [HttpGet("nutrition/{memberId:guid}")]
    public Task<IActionResult> GetNutritionPlans(Guid memberId, [FromQuery] bool activeOnly = true)
        => Run(() => assessments.GetNutritionPlansAsync(memberId, activeOnly));

    // ── Habits ───────────────────────────────────────────────────────────────

    [HttpPost("habits")]
    public Task<IActionResult> SaveHabit([FromBody] HabitTrackerDto request, [FromQuery] Guid? id = null)
        => Run(() => assessments.SaveHabitAsync(id, request, UserId), "Habit saved.");

    [HttpGet("habits/{memberId:guid}")]
    public Task<IActionResult> GetHabits(Guid memberId, [FromQuery] bool activeOnly = true)
        => Run(() => assessments.GetHabitsAsync(memberId, activeOnly));

    [HttpPost("habits/{id:guid}/log")]
    public Task<IActionResult> LogHabit(
        Guid id, [FromQuery] DateTime forDate, [FromQuery] decimal? value,
        [FromQuery] bool completed = true, [FromQuery] string? note = null)
        => Run(() => assessments.LogHabitAsync(id, forDate, value, completed, note, UserId), "Logged.");

    // ── Check-ins ────────────────────────────────────────────────────────────

    [HttpGet("check-ins")]
    public Task<IActionResult> GetCheckIns(
        [FromQuery] Guid? staffId, [FromQuery] Guid? memberId, [FromQuery] bool dueOnly = false)
        => Run(() => assessments.GetCheckInsAsync(staffId, memberId, dueOnly));

    [HttpPost("check-ins")]
    public Task<IActionResult> SaveCheckIn([FromBody] CoachCheckInDto request, [FromQuery] Guid? id = null)
        => Run(() => assessments.SaveCheckInAsync(id, request, UserId), "Check-in saved.");
}
