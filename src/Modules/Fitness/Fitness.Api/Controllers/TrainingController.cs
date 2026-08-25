using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Programming, results, leaderboards and ranks — the part of the app members actually enjoy.
///
/// Every result is normalised into one sortable number when it is written, so a leaderboard is an
/// indexed sort rather than a parse of "4 rounds + 7 reps" at read time. Leaderboards respect the
/// member's opt-out: someone who does not want their name on a screen does not appear on one.
/// </summary>
[Route("api/fitness/training")]
public class TrainingController(
    ITrainingService training,
    ILogger<TrainingController> logger) : FitnessControllerBase(logger)
{
    // ── Movements and workouts ───────────────────────────────────────────────

    [HttpGet("exercises")]
    public Task<IActionResult> GetExercises(
        [FromQuery] ExerciseCategory? category, [FromQuery] string? search,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => training.GetExercisesAsync(category, search, pagination ?? new PaginationParams()));

    [HttpPost("exercises")]
    public Task<IActionResult> SaveExercise([FromBody] ExerciseDto request, [FromQuery] Guid? id = null)
        => Run(() => training.SaveExerciseAsync(id, request, UserId), "Exercise saved.");

    [HttpGet("workouts")]
    public Task<IActionResult> GetWorkouts(
        [FromQuery] Guid? clubId, [FromQuery] bool? benchmarksOnly, [FromQuery] bool? templatesOnly,
        [FromQuery] string? search, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => training.GetWorkoutsAsync(clubId, benchmarksOnly, templatesOnly, search,
            pagination ?? new PaginationParams()));

    [HttpGet("workouts/{id:guid}")]
    public Task<IActionResult> GetWorkout(Guid id)
        => RunFound(() => training.GetWorkoutAsync(id), "Workout not found.");

    [HttpPost("workouts")]
    public Task<IActionResult> SaveWorkout([FromBody] WorkoutDto request, [FromQuery] Guid? id = null)
        => Run(() => training.SaveWorkoutAsync(id, request, UserId), "Workout saved.");

    [HttpDelete("workouts/{id:guid}")]
    public Task<IActionResult> DeleteWorkout(Guid id)
        => Run(() => training.DeleteWorkoutAsync(id, UserId), "Workout removed.");

    // ── Programming ──────────────────────────────────────────────────────────

    [HttpGet("tracks")]
    public Task<IActionResult> GetTracks([FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true)
        => Run(() => training.GetTracksAsync(clubId, activeOnly));

    [HttpPost("tracks")]
    public Task<IActionResult> SaveTrack([FromBody] ProgramTrackDto request, [FromQuery] Guid? id = null)
        => Run(() => training.SaveTrackAsync(id, request, UserId), "Track saved.");

    [HttpGet("programming")]
    public Task<IActionResult> GetProgramming(
        [FromQuery] Guid clubId, [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] Guid? trackId)
        => Run(() => training.GetProgrammingAsync(clubId, from, to, trackId));

    [HttpPost("programming")]
    public Task<IActionResult> SaveProgramDay([FromBody] ProgramDayDto request, [FromQuery] Guid? id = null)
        => Run(() => training.SaveProgramDayAsync(id, request, UserId), "Saved.");

    /// <summary>
    /// Publishes a day's programming.
    ///
    /// Held back until published, because a coach writing Thursday's session on Tuesday does not
    /// want members reading it and turning up having already done half of it.
    /// </summary>
    [HttpPost("programming/{id:guid}/publish")]
    public Task<IActionResult> PublishProgramDay(Guid id)
        => Run(() => training.PublishProgramDayAsync(id, UserId), "Published.");

    /// <summary>The whiteboard: today's workout, the scores so far, and the day's leaderboard.</summary>
    [HttpGet("wod-board")]
    public Task<IActionResult> GetWodBoard([FromQuery] Guid clubId, [FromQuery] DateTime forDate)
        => Run(() => training.GetWodBoardAsync(clubId, forDate));

    // ── Results ──────────────────────────────────────────────────────────────

    /// <summary>Logs a score, normalises it for sorting, and detects a personal record.</summary>
    [HttpPost("results")]
    public Task<IActionResult> LogResult([FromBody] LogResultDto request)
        => Run(() => training.LogResultAsync(request, UserId), "Score logged.");

    [HttpGet("results")]
    public Task<IActionResult> GetResults(
        [FromQuery] Guid? memberId, [FromQuery] Guid? workoutId, [FromQuery] Guid? clubId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => training.GetResultsAsync(memberId, workoutId, clubId, from, to,
            pagination ?? new PaginationParams()));

    [HttpGet("personal-records/{memberId:guid}")]
    public Task<IActionResult> GetPersonalRecords(Guid memberId)
        => Run(() => training.GetPersonalRecordsAsync(memberId));

    [HttpGet("leaderboard")]
    public Task<IActionResult> GetLeaderboard(
        [FromQuery] Guid clubId, [FromQuery] Guid? workoutId, [FromQuery] Guid? classOccurrenceId,
        [FromQuery] Guid? challengeId, [FromQuery] string? division,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? viewerMemberId)
        => Run(() => training.GetLeaderboardAsync(clubId, workoutId, classOccurrenceId, challengeId,
            division, from, to, viewerMemberId));

    // ── Effort and streaks ───────────────────────────────────────────────────

    [HttpPost("effort")]
    public Task<IActionResult> RecordEffort([FromBody] EffortSessionDto request)
        => Run(() => training.RecordEffortAsync(request, UserId), "Session recorded.");

    [HttpGet("effort/{memberId:guid}")]
    public Task<IActionResult> GetEffortSessions(
        Guid memberId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Run(() => training.GetEffortSessionsAsync(memberId, from, to));

    [HttpGet("streaks/{memberId:guid}")]
    public Task<IActionResult> GetStreak(Guid memberId, [FromQuery] string cadence = "Weekly")
        => RunFound(() => training.GetStreakAsync(memberId, cadence), "No streak recorded.");

    // ── Ranks ────────────────────────────────────────────────────────────────

    [HttpGet("ladders")]
    public Task<IActionResult> GetLadders([FromQuery] Guid? clubId)
        => Run(() => training.GetLaddersAsync(clubId));

    [HttpPost("ladders")]
    public Task<IActionResult> SaveLadder([FromBody] RankLadderDto request, [FromQuery] Guid? id = null)
        => Run(() => training.SaveLadderAsync(id, request, UserId), "Ladder saved.");

    [HttpGet("ranks/{memberId:guid}")]
    public Task<IActionResult> GetMemberRanks(Guid memberId)
        => Run(() => training.GetMemberRanksAsync(memberId));

    /// <summary>Who is eligible to grade — by time served, attendance and skills signed off.</summary>
    [HttpGet("ranks/candidates")]
    public Task<IActionResult> GetGradingCandidates([FromQuery] Guid clubId, [FromQuery] Guid ladderId)
        => Run(() => training.GetGradingCandidatesAsync(clubId, ladderId));

    [HttpPost("ranks/award")]
    public Task<IActionResult> AwardRank([FromBody] AwardRankDto request)
        => Run(() => training.AwardRankAsync(request, UserId), "Rank awarded.");

    [HttpPost("grading-events")]
    public Task<IActionResult> SaveGradingEvent([FromBody] GradingEventDto request, [FromQuery] Guid? id = null)
        => Run(() => training.SaveGradingEventAsync(id, request, UserId), "Grading saved.");

    // ── Skills ───────────────────────────────────────────────────────────────

    /// <summary>Signs a member off on a movement that a class requires before they can book it.</summary>
    [HttpPost("clearances")]
    public Task<IActionResult> GrantClearance([FromBody] SkillClearanceDto request)
        => Run(() => training.GrantClearanceAsync(request, UserId), "Cleared.");

    [HttpGet("clearances/{memberId:guid}")]
    public Task<IActionResult> GetClearances(Guid memberId, [FromQuery] bool activeOnly = true)
        => Run(() => training.GetClearancesAsync(memberId, activeOnly));
}
