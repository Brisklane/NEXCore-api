using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Programming, results, personal records, leaderboards and rank progression.
///
/// One idea holds the whole thing together: every result is reduced to a single **normalised
/// score** on the way in. A leaderboard has to sort "4:32", "12 rounds + 8" and "102.5 kg", and a
/// system that stores those as strings and sorts them at read time produces leaderboards that are
/// quietly wrong — which, in a room full of competitive people, is noticed immediately.
/// </summary>
public class TrainingService(FitnessDbContext db, IFitnessTenant tenant) : ITrainingService
{
    // ── Exercises ────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<ExerciseDto>> GetExercisesAsync(
        ExerciseCategory? category, string? search, PaginationParams pagination)
    {
        var query = db.Exercises.ForTenant(tenant)
            .WhereIf(category is not null, e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term)
                                  || (e.MuscleGroups != null && e.MuscleGroups.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(e => e.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<ExerciseDto>.Ok(
                   [.. page.Select(FitnessMapper.ToDto)],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<ExerciseDto> SaveExerciseAsync(Guid? id, ExerciseDto request, Guid userId)
    {
        Exercise exercise;
        if (id is null)
        {
            exercise = new Exercise().StampNew(tenant, userId);
            db.Exercises.Add(exercise);
        }
        else
        {
            exercise = await db.Exercises.ForTenant(tenant).FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new InvalidOperationException("Exercise not found.");

            if (exercise.IsSystemExercise)
                throw new InvalidOperationException(
                    "This is a built-in movement. Copy it and edit the copy so an update cannot overwrite your changes.");

            exercise.StampUpdated(userId);
        }

        exercise.Name = request.Name;
        exercise.Category = request.Category;
        exercise.MuscleGroups = request.MuscleGroups;
        exercise.Equipment = request.Equipment;
        exercise.Instructions = request.Instructions;
        exercise.VideoUrl = request.VideoUrl;
        exercise.ImageUrl = request.ImageUrl;
        exercise.ScalingOptions = request.ScalingOptions;
        exercise.TracksPersonalRecord = request.TracksPersonalRecord;
        exercise.PrScoreType = request.PrScoreType;
        exercise.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(exercise);
    }

    // ── Workouts ─────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<WorkoutDto>> GetWorkoutsAsync(
        Guid? clubId, bool? benchmarksOnly, bool? templatesOnly, string? search, PaginationParams pagination)
    {
        var query = db.Workouts.ForTenant(tenant)
            .WhereIf(clubId is not null, w => w.ClubId == clubId || w.ClubId == null)
            .WhereIf(benchmarksOnly == true, w => w.IsBenchmark)
            .WhereIf(templatesOnly == true, w => w.IsTemplate);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(term)
                                  || (w.BenchmarkName != null && w.BenchmarkName.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(w => w.IsBenchmark).ThenBy(w => w.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Include(w => w.Sections.Where(s => !s.IsDeleted)).ThenInclude(s => s.Movements.Where(m => !m.IsDeleted))
            .ToListAsync();

        var ids = page.Select(w => w.Id).ToList();
        var results = await db.WorkoutResults.ForTenant(tenant)
            .Where(r => r.WorkoutId != null && ids.Contains(r.WorkoutId.Value))
            .GroupBy(r => r.WorkoutId!.Value)
            .Select(g => new { WorkoutId = g.Key, Count = g.Count(), Last = g.Max(r => r.PerformedOn) })
            .ToListAsync();

        var items = page.Select(w =>
        {
            var dto = FitnessMapper.ToDto(w);
            var stats = results.FirstOrDefault(r => r.WorkoutId == w.Id);
            dto.ResultCount = stats?.Count ?? 0;
            dto.LastPerformedOn = stats?.Last;
            return dto;
        }).ToList();

        return PaginatedResponse<WorkoutDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<WorkoutDto?> GetWorkoutAsync(Guid workoutId)
    {
        var workout = await db.Workouts.ForTenant(tenant)
            .Include(w => w.Sections.Where(s => !s.IsDeleted))
                .ThenInclude(s => s.Movements.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(w => w.Id == workoutId);

        if (workout is null) return null;

        var dto = FitnessMapper.ToDto(workout);
        dto.ResultCount = await db.WorkoutResults.ForTenant(tenant).CountAsync(r => r.WorkoutId == workoutId);
        return dto;
    }

    public async Task<WorkoutDto> SaveWorkoutAsync(Guid? id, WorkoutDto request, Guid userId)
    {
        Workout workout;
        if (id is null)
        {
            workout = new Workout().StampNew(tenant, userId);
            db.Workouts.Add(workout);
        }
        else
        {
            workout = await db.Workouts.ForTenant(tenant)
                .Include(w => w.Sections.Where(s => !s.IsDeleted)).ThenInclude(s => s.Movements)
                .FirstOrDefaultAsync(w => w.Id == id)
                ?? throw new InvalidOperationException("Workout not found.");
            workout.StampUpdated(userId);
        }

        workout.Name = request.Name;
        workout.ClubId = request.ClubId;
        workout.Summary = request.Summary;
        workout.CoachNotes = request.CoachNotes;
        workout.ScoreType = request.ScoreType;
        workout.ScoreUnit = request.ScoreUnit;
        workout.TimeCapSeconds = request.TimeCapSeconds;
        workout.IsBenchmark = request.IsBenchmark;
        workout.BenchmarkName = request.BenchmarkName;
        workout.EstimatedMinutes = request.EstimatedMinutes;
        workout.IsTemplate = request.IsTemplate;
        workout.IsActive = request.IsActive;

        // Sections and movements are replaced wholesale — a workout is small, and a partial edit
        // of a nested structure is harder to reason about than a rewrite.
        foreach (var section in workout.Sections.Where(s => !s.IsDeleted))
        {
            foreach (var movement in section.Movements.Where(m => !m.IsDeleted)) movement.StampDeleted(userId);
            section.StampDeleted(userId);
        }

        var sectionOrder = 0;
        foreach (var sectionDto in request.Sections.OrderBy(s => s.DisplayOrder))
        {
            var section = new WorkoutSection
            {
                WorkoutId = workout.Id,
                Title = sectionDto.Title,
                Kind = sectionDto.Kind,
                DisplayOrder = sectionOrder++,
                Rounds = sectionDto.Rounds,
                DurationSeconds = sectionDto.DurationSeconds,
                RestSeconds = sectionDto.RestSeconds,
                ScoreType = sectionDto.ScoreType,
                Instructions = sectionDto.Instructions,
            }.StampNew(tenant, userId);

            db.WorkoutSections.Add(section);

            var movementOrder = 0;
            foreach (var movementDto in sectionDto.Movements.OrderBy(m => m.DisplayOrder))
            {
                db.WorkoutMovements.Add(new WorkoutMovement
                {
                    WorkoutSectionId = section.Id,
                    ExerciseId = movementDto.ExerciseId,
                    MovementName = movementDto.MovementName,
                    DisplayOrder = movementOrder++,
                    Sets = movementDto.Sets,
                    Reps = movementDto.Reps,
                    LoadKg = movementDto.LoadKg,
                    LoadPercentOfMax = movementDto.LoadPercentOfMax,
                    DistanceMetres = movementDto.DistanceMetres,
                    Calories = movementDto.Calories,
                    DurationSeconds = movementDto.DurationSeconds,
                    RestSeconds = movementDto.RestSeconds,
                    Tempo = movementDto.Tempo,
                    ScalingNote = movementDto.ScalingNote,
                }.StampNew(tenant, userId));
            }
        }

        await db.SaveChangesAsync();
        return (await GetWorkoutAsync(workout.Id))!;
    }

    public async Task DeleteWorkoutAsync(Guid id, Guid userId)
    {
        var workout = await db.Workouts.ForTenant(tenant).FirstOrDefaultAsync(w => w.Id == id)
            ?? throw new InvalidOperationException("Workout not found.");

        var results = await db.WorkoutResults.ForTenant(tenant).CountAsync(r => r.WorkoutId == id);
        if (results > 0)
            throw new InvalidOperationException(
                $"{results} results are logged against this workout. Deleting it would take them with it — " +
                "mark it inactive instead.");

        workout.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ── Programming ──────────────────────────────────────────────────────────

    public async Task<List<ProgramTrackDto>> GetTracksAsync(Guid? clubId, bool activeOnly)
    {
        var today = DateTime.UtcNow.Date;

        var tracks = await db.ProgramTracks.ForTenant(tenant)
            .WhereIf(clubId is not null, t => t.ClubId == clubId || t.ClubId == null)
            .WhereIf(activeOnly, t => t.IsActive)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
            .ToListAsync();

        var published = await db.ProgramDays.ForTenant(tenant)
            .Where(d => d.IsPublished && d.ScheduledOn >= today)
            .GroupBy(d => d.ProgramTrackId)
            .Select(g => new { TrackId = g.Key, Count = g.Count() })
            .ToListAsync();

        var unpublished = await db.ProgramDays.ForTenant(tenant)
            .Where(d => !d.IsPublished && d.ScheduledOn >= today)
            .GroupBy(d => d.ProgramTrackId)
            .Select(g => new { TrackId = g.Key, Next = g.Min(d => d.ScheduledOn) })
            .ToListAsync();

        return [.. tracks.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t);
            dto.PublishedDays = published.FirstOrDefault(p => p.TrackId == t.Id)?.Count ?? 0;
            dto.NextUnpublishedOn = unpublished.FirstOrDefault(u => u.TrackId == t.Id)?.Next;
            return dto;
        })];
    }

    public async Task<ProgramTrackDto> SaveTrackAsync(Guid? id, ProgramTrackDto request, Guid userId)
    {
        ProgramTrack track;
        if (id is null)
        {
            track = new ProgramTrack().StampNew(tenant, userId);
            db.ProgramTracks.Add(track);
        }
        else
        {
            track = await db.ProgramTracks.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Track not found.");
            track.StampUpdated(userId);
        }

        track.Name = request.Name;
        track.ClubId = request.ClubId;
        track.ColourHex = request.ColourHex;
        track.DisplayOrder = request.DisplayOrder;
        track.IsPublic = request.IsPublic;
        track.StartsOn = request.StartsOn;
        track.EndsOn = request.EndsOn;
        track.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(track);
    }

    public async Task<List<ProgramDayDto>> GetProgrammingAsync(Guid clubId, DateTime from, DateTime to, Guid? trackId)
    {
        var days = await db.ProgramDays.ForTenant(tenant)
            .Where(d => (d.ClubId == clubId || d.ClubId == null)
                     && d.ScheduledOn >= from.Date && d.ScheduledOn <= to.Date)
            .WhereIf(trackId is not null, d => d.ProgramTrackId == trackId)
            .Include(d => d.ProgramTrack)
            .Include(d => d.Workout).ThenInclude(w => w!.Sections.Where(s => !s.IsDeleted))
                .ThenInclude(s => s.Movements.Where(m => !m.IsDeleted))
            .OrderBy(d => d.ScheduledOn).ThenBy(d => d.ProgramTrack!.DisplayOrder)
            .ToListAsync();

        var dayIds = days.Select(d => d.Id).ToList();
        var workoutIds = days.Where(d => d.WorkoutId is not null).Select(d => d.WorkoutId!.Value).ToList();

        var resultCounts = await db.WorkoutResults.ForTenant(tenant)
            .Where(r => r.WorkoutId != null && workoutIds.Contains(r.WorkoutId.Value))
            .GroupBy(r => r.WorkoutId!.Value)
            .Select(g => new { WorkoutId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. days.Select(d =>
        {
            var dto = FitnessMapper.ToDto(d);
            dto.TrackName = d.ProgramTrack?.Name;
            dto.TrackColour = d.ProgramTrack?.ColourHex;
            dto.WorkoutName = d.Workout?.Name;
            dto.Workout = d.Workout is null ? null : FitnessMapper.ToDto(d.Workout);
            dto.ResultCount = d.WorkoutId is null ? 0
                : resultCounts.FirstOrDefault(r => r.WorkoutId == d.WorkoutId)?.Count ?? 0;
            return dto;
        })];
    }

    public async Task<ProgramDayDto> SaveProgramDayAsync(Guid? id, ProgramDayDto request, Guid userId)
    {
        ProgramDay day;
        if (id is null)
        {
            var clash = await db.ProgramDays.ForTenant(tenant)
                .AnyAsync(d => d.ProgramTrackId == request.ProgramTrackId
                            && d.ScheduledOn == request.ScheduledOn.Date);

            if (clash)
                throw new InvalidOperationException("That track already has programming on that day.");

            day = new ProgramDay { ProgramTrackId = request.ProgramTrackId }.StampNew(tenant, userId);
            db.ProgramDays.Add(day);
        }
        else
        {
            day = await db.ProgramDays.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new InvalidOperationException("Programming not found.");
            day.StampUpdated(userId);
        }

        day.WorkoutId = request.WorkoutId;
        day.ScheduledOn = request.ScheduledOn.Date;
        day.ClubId = request.ClubId;
        day.IsPublished = request.IsPublished;
        day.PublishAt = request.PublishAt;
        day.CoachBrief = request.CoachBrief;

        await db.SaveChangesAsync();

        var saved = await db.ProgramDays.ForTenant(tenant)
            .Include(d => d.ProgramTrack)
            .Include(d => d.Workout)
            .FirstAsync(d => d.Id == day.Id);

        var dto = FitnessMapper.ToDto(saved);
        dto.TrackName = saved.ProgramTrack?.Name;
        dto.WorkoutName = saved.Workout?.Name;
        return dto;
    }

    public async Task<ProgramDayDto> PublishProgramDayAsync(Guid id, Guid userId)
    {
        var day = await db.ProgramDays.ForTenant(tenant)
            .Include(d => d.ProgramTrack)
            .Include(d => d.Workout)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new InvalidOperationException("Programming not found.");

        if (day.WorkoutId is null)
            throw new InvalidOperationException("There is no workout attached to publish.");

        day.IsPublished = true;
        day.PublishAt = DateTime.UtcNow;
        day.StampUpdated(userId);

        await db.SaveChangesAsync();

        var dto = FitnessMapper.ToDto(day);
        dto.TrackName = day.ProgramTrack?.Name;
        dto.WorkoutName = day.Workout?.Name;
        return dto;
    }

    /// <summary>Today's programming across every track — what the whiteboard screen renders.</summary>
    public async Task<WodBoardDto> GetWodBoardAsync(Guid clubId, DateTime forDate)
    {
        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var tracks = await GetProgrammingAsync(clubId, forDate, forDate, null);
        var published = tracks.Where(t => t.IsPublished).ToList();

        var board = new WodBoardDto
        {
            ClubId = clubId,
            ClubName = club.Name,
            ForDate = forDate.Date,
            Tracks = published,
        };

        var workoutId = published.FirstOrDefault(t => t.WorkoutId is not null)?.WorkoutId;
        if (workoutId is not null)
        {
            var leaderboard = await GetLeaderboardAsync(clubId, workoutId, null, null, null,
                forDate.Date, forDate.Date.AddDays(1), null);

            board.TodaysLeaderboard = leaderboard.Entries;
        }

        return board;
    }

    // ── Results ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Logging a result.
    ///
    /// Normalises the score, detects whether it beat a personal record, and — if it did — updates
    /// the record and flags the result so the celebration fires exactly once. A PR notification
    /// that fires twice is worse than one that never fires.
    /// </summary>
    public async Task<WorkoutResultDto> LogResultAsync(LogResultDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var workout = request.WorkoutId is null
            ? null
            : await db.Workouts.ForTenant(tenant).FirstOrDefaultAsync(w => w.Id == request.WorkoutId);

        var result = new WorkoutResult
        {
            MemberId = request.MemberId,
            WorkoutId = request.WorkoutId,
            ClassOccurrenceId = request.ClassOccurrenceId,
            AppointmentId = request.AppointmentId,
            ProgramTrackId = request.ProgramTrackId,
            ClubId = request.ClubId,
            PerformedOn = request.PerformedOn ?? now,
            ScoreType = request.ScoreType,
            TimeSeconds = request.TimeSeconds,
            Rounds = request.Rounds,
            Reps = request.Reps,
            LoadKg = request.LoadKg,
            DistanceMetres = request.DistanceMetres,
            Calories = request.Calories,
            Points = request.Points,
            Passed = request.Passed,
            WasScaled = request.WasScaled,
            ScalingNote = request.ScalingNote,
            DidNotFinish = request.DidNotFinish,
            MemberNote = request.MemberNote,
            EnteredByStaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId);

        result.NormalisedScore = Normalise(result);

        db.WorkoutResults.Add(result);

        // A scaled result never sets an RX personal record — that is the whole point of scaling.
        if (workout is not null && !request.WasScaled && !request.DidNotFinish)
            await UpdatePersonalRecordAsync(result, workout, userId);

        await db.SaveChangesAsync();

        var saved = await db.WorkoutResults.ForTenant(tenant)
            .Include(r => r.Member)
            .Include(r => r.Workout)
            .FirstAsync(r => r.Id == result.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<PaginatedResponse<WorkoutResultDto>> GetResultsAsync(
        Guid? memberId, Guid? workoutId, Guid? clubId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.WorkoutResults.ForTenant(tenant)
            .Include(r => r.Member)
            .Include(r => r.Workout)
            .WhereIf(memberId is not null, r => r.MemberId == memberId)
            .WhereIf(workoutId is not null, r => r.WorkoutId == workoutId)
            .WhereIf(clubId is not null, r => r.ClubId == clubId)
            .WhereIf(from is not null, r => r.PerformedOn >= from)
            .WhereIf(to is not null, r => r.PerformedOn <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(r => r.PerformedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<WorkoutResultDto>.Ok(
                   [.. page.Select(FitnessMapper.ToDto)],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<List<PersonalRecordDto>> GetPersonalRecordsAsync(Guid memberId)
    {
        var records = await db.PersonalRecords.ForTenant(tenant)
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.AchievedOn)
            .ToListAsync();

        return [.. records.Select(FitnessMapper.ToDto)];
    }

    /// <summary>
    /// A leaderboard.
    ///
    /// Computed live rather than read from the cached table when a specific filter is asked for,
    /// because a coach changing the division should see it change. The cached rows exist for the
    /// wall display, which refreshes constantly and cannot afford the query.
    ///
    /// Members who opted out of leaderboards are excluded — quietly, without a gap in the ranking.
    /// </summary>
    public async Task<LeaderboardDto> GetLeaderboardAsync(
        Guid clubId, Guid? workoutId, Guid? classOccurrenceId, Guid? challengeId,
        string? division, DateTime? from, DateTime? to, Guid? viewerMemberId)
    {
        var now = DateTime.UtcNow;

        var dto = new LeaderboardDto
        {
            ClubId = clubId,
            WorkoutId = workoutId,
            ClassOccurrenceId = classOccurrenceId,
            ChallengeId = challengeId,
            Division = division,
            From = from,
            To = to,
            ComputedAt = now,
        };

        if (challengeId is not null) return await ChallengeLeaderboardAsync(dto, challengeId.Value, viewerMemberId);

        var query = db.WorkoutResults.ForTenant(tenant)
            .Include(r => r.Member)
            .Include(r => r.Workout)
            .Where(r => r.ClubId == clubId && !r.DidNotFinish)
            .WhereIf(workoutId is not null, r => r.WorkoutId == workoutId)
            .WhereIf(classOccurrenceId is not null, r => r.ClassOccurrenceId == classOccurrenceId)
            .WhereIf(from is not null, r => r.PerformedOn >= from)
            .WhereIf(to is not null, r => r.PerformedOn <= to);

        var results = await query.ToListAsync();

        results = [.. results.Where(r => r.Member?.LeaderboardOptIn != false)];

        if (division is not null)
        {
            results = division.ToLowerInvariant() switch
            {
                "rx" => [.. results.Where(r => !r.WasScaled)],
                "scaled" => [.. results.Where(r => r.WasScaled)],
                "female" => [.. results.Where(r => r.Member?.Gender == Gender.Female)],
                "male" => [.. results.Where(r => r.Member?.Gender == Gender.Male)],
                _ => results,
            };
        }

        // One entry per member — their best, not every attempt.
        var scoreType = results.FirstOrDefault()?.ScoreType ?? ScoreType.ForTime;
        dto.ScoreType = scoreType;
        var lowerIsBetter = scoreType is ScoreType.ForTime or ScoreType.TimeUnderLoad;

        var best = results
            .GroupBy(r => r.MemberId)
            .Select(g => lowerIsBetter
                ? g.OrderBy(r => r.NormalisedScore).First()
                : g.OrderByDescending(r => r.NormalisedScore).First())
            .ToList();

        var ordered = lowerIsBetter
            ? best.OrderBy(r => r.NormalisedScore).ToList()
            : best.OrderByDescending(r => r.NormalisedScore).ToList();

        var rank = 1;
        foreach (var entry in ordered)
        {
            dto.Entries.Add(new LeaderboardEntryDto
            {
                Rank = rank++,
                MemberId = entry.MemberId,
                MemberDisplayName = entry.Member is null ? "" : entry.Member.PreferredName ?? FitnessMapper.FullName(entry.Member),
                MemberPhotoUrl = entry.Member?.PhotoUrl,
                Score = entry.NormalisedScore,
                ScoreDisplay = FitnessMapper.FormatScore(entry),
                WasScaled = entry.WasScaled,
                Division = entry.WasScaled ? "Scaled" : "RX",
                WorkoutResultId = entry.Id,
                AchievedOn = entry.PerformedOn,
            });
        }

        dto.Title = workoutId is not null
            ? results.FirstOrDefault()?.Workout?.Name ?? "Leaderboard"
            : "Leaderboard";

        dto.TotalParticipants = dto.Entries.Count;
        dto.ViewerEntry = viewerMemberId is null ? null : dto.Entries.FirstOrDefault(e => e.MemberId == viewerMemberId);

        return dto;
    }

    // ── Effort & streaks ─────────────────────────────────────────────────────

    public async Task<EffortSessionDto> RecordEffortAsync(EffortSessionDto request, Guid userId)
    {
        // De-duplicated on the device's own reference: wearables retry, and two identical
        // sessions would double a member's challenge score.
        if (!string.IsNullOrWhiteSpace(request.DeviceType))
        {
            var existing = await db.EffortSessions.ForTenant(tenant)
                .Include(s => s.Member)
                .FirstOrDefaultAsync(s => s.MemberId == request.MemberId && s.StartedAt == request.StartedAt);

            if (existing is not null) return FitnessMapper.ToDto(existing);
        }

        var session = new EffortSession
        {
            MemberId = request.MemberId,
            ClubId = request.ClubId,
            ClassOccurrenceId = request.ClassOccurrenceId,
            StartedAt = request.StartedAt,
            EndedAt = request.EndedAt,
            DurationMinutes = request.DurationMinutes,
            EffortPoints = request.EffortPoints,
            GreyMinutes = request.GreyMinutes,
            BlueMinutes = request.BlueMinutes,
            GreenMinutes = request.GreenMinutes,
            YellowMinutes = request.YellowMinutes,
            RedMinutes = request.RedMinutes,
            AverageHeartRate = request.AverageHeartRate,
            PeakHeartRate = request.PeakHeartRate,
            CaloriesBurned = request.CaloriesBurned,
            PeakZone = request.PeakZone,
            DeviceType = request.DeviceType,
        }.StampNew(tenant, userId);

        // Effort points are derivable from zone minutes when the device does not send them:
        // the standard weighting is 1/2/3/4 points per minute from blue upward.
        if (session.EffortPoints == 0)
        {
            session.EffortPoints = session.BlueMinutes + session.GreenMinutes * 2
                                 + session.YellowMinutes * 3 + session.RedMinutes * 4;
        }

        db.EffortSessions.Add(session);
        await db.SaveChangesAsync();

        var saved = await db.EffortSessions.ForTenant(tenant)
            .Include(s => s.Member)
            .FirstAsync(s => s.Id == session.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<List<EffortSessionDto>> GetEffortSessionsAsync(Guid memberId, DateTime? from, DateTime? to)
    {
        var sessions = await db.EffortSessions.ForTenant(tenant)
            .Where(s => s.MemberId == memberId)
            .WhereIf(from is not null, s => s.StartedAt >= from)
            .WhereIf(to is not null, s => s.StartedAt <= to)
            .Include(s => s.Member)
            .OrderByDescending(s => s.StartedAt)
            .Take(200)
            .ToListAsync();

        return [.. sessions.Select(FitnessMapper.ToDto)];
    }

    public async Task<AttendanceStreakDto?> GetStreakAsync(Guid memberId, string cadence)
    {
        var now = DateTime.UtcNow;

        var streak = await db.Streaks.ForTenant(tenant)
            .Include(s => s.Member)
            .FirstOrDefaultAsync(s => s.MemberId == memberId && s.Cadence == cadence);

        if (streak is null) return null;

        var dto = new AttendanceStreakDto
        {
            Id = streak.Id,
            MemberId = streak.MemberId,
            Cadence = streak.Cadence,
            CurrentCount = streak.CurrentCount,
            LongestCount = streak.LongestCount,
            StartedOn = streak.StartedOn,
            LastQualifyingOn = streak.LastQualifyingOn,
            BrokenOn = streak.BrokenOn,
            RequiredPerPeriod = streak.RequiredPerPeriod,
        };

        // What it would take to keep it alive this period, which is the only actionable part.
        var (periodStart, periodEnd) = streak.Cadence == "Weekly"
            ? (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(7 - (int)now.DayOfWeek))
            : (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1));

        var visitsThisPeriod = await db.CheckIns.ForTenant(tenant)
            .CountAsync(c => c.MemberId == memberId && c.CheckedInAt >= periodStart && c.CheckedInAt < periodEnd);

        dto.VisitsNeededThisPeriod = Math.Max(0, streak.RequiredPerPeriod - visitsThisPeriod);
        dto.AtRiskOfBreaking = dto.VisitsNeededThisPeriod > 0 && (periodEnd - now).TotalDays <= 2;

        return dto;
    }

    // ── Ranks ────────────────────────────────────────────────────────────────

    public async Task<List<RankLadderDto>> GetLaddersAsync(Guid? clubId)
    {
        var ladders = await db.RankLadders.ForTenant(tenant)
            .WhereIf(clubId is not null, l => l.ClubId == clubId || l.ClubId == null)
            .Include(l => l.Levels.Where(v => !v.IsDeleted))
            .OrderBy(l => l.Name)
            .ToListAsync();

        var counts = await db.MemberRanks.ForTenant(tenant)
            .Where(r => r.IsCurrent)
            .GroupBy(r => r.RankLevelId)
            .Select(g => new { LevelId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. ladders.Select(l => new RankLadderDto
        {
            Id = l.Id,
            Name = l.Name,
            ClubId = l.ClubId,
            Discipline = l.Discipline,
            IsActive = l.IsActive,
            Levels = [.. l.Levels.Where(v => !v.IsDeleted).OrderBy(v => v.Ordinal).Select(v => new RankLevelDto
            {
                Id = v.Id,
                RankLadderId = v.RankLadderId,
                Name = v.Name,
                Ordinal = v.Ordinal,
                ColourHex = v.ColourHex,
                BadgeUrl = v.BadgeUrl,
                RequiredAttendances = v.RequiredAttendances,
                MinimumMonthsAtPrevious = v.MinimumMonthsAtPrevious,
                RequirementsNote = v.RequirementsNote,
                GradingFee = v.GradingFee,
                MinimumAge = v.MinimumAge,
                MembersAtThisRank = counts.FirstOrDefault(c => c.LevelId == v.Id)?.Count ?? 0,
            })],
        })];
    }

    public async Task<RankLadderDto> SaveLadderAsync(Guid? id, RankLadderDto request, Guid userId)
    {
        RankLadder ladder;
        if (id is null)
        {
            ladder = new RankLadder().StampNew(tenant, userId);
            db.RankLadders.Add(ladder);
        }
        else
        {
            ladder = await db.RankLadders.ForTenant(tenant)
                .Include(l => l.Levels.Where(v => !v.IsDeleted))
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new InvalidOperationException("Rank ladder not found.");
            ladder.StampUpdated(userId);
        }

        ladder.Name = request.Name;
        ladder.ClubId = request.ClubId;
        ladder.Discipline = request.Discipline;
        ladder.IsActive = request.IsActive;

        var existing = ladder.Levels.Where(v => !v.IsDeleted).ToList();
        var keptIds = request.Levels.Where(v => v.Id != Guid.Empty).Select(v => v.Id).ToHashSet();

        foreach (var gone in existing.Where(v => !keptIds.Contains(v.Id)))
        {
            var held = await db.MemberRanks.ForTenant(tenant).AnyAsync(r => r.RankLevelId == gone.Id);
            if (held)
                throw new InvalidOperationException(
                    $"Members hold the rank '{gone.Name}'. It cannot be removed from the ladder.");

            gone.StampDeleted(userId);
        }

        var ordinal = 1;
        foreach (var levelDto in request.Levels.OrderBy(v => v.Ordinal))
        {
            var level = existing.FirstOrDefault(v => v.Id == levelDto.Id);
            if (level is null)
            {
                level = new RankLevel { RankLadderId = ladder.Id }.StampNew(tenant, userId);
                db.RankLevels.Add(level);
            }
            else
            {
                level.StampUpdated(userId);
            }

            level.Name = levelDto.Name;
            level.Ordinal = ordinal++;
            level.ColourHex = levelDto.ColourHex;
            level.BadgeUrl = levelDto.BadgeUrl;
            level.RequiredAttendances = levelDto.RequiredAttendances;
            level.MinimumMonthsAtPrevious = levelDto.MinimumMonthsAtPrevious;
            level.RequirementsNote = levelDto.RequirementsNote;
            level.GradingFee = levelDto.GradingFee;
            level.MinimumAge = levelDto.MinimumAge;
        }

        await db.SaveChangesAsync();
        return (await GetLaddersAsync(request.ClubId)).First(l => l.Id == ladder.Id);
    }

    public async Task<List<MemberRankDto>> GetMemberRanksAsync(Guid memberId)
    {
        var now = DateTime.UtcNow;

        var ranks = await db.MemberRanks.ForTenant(tenant)
            .Where(r => r.MemberId == memberId)
            .Include(r => r.Member)
            .Include(r => r.RankLevel)
            .OrderByDescending(r => r.AwardedOn)
            .ToListAsync();

        var ladderIds = ranks.Select(r => r.RankLadderId).Distinct().ToList();

        var levels = await db.RankLevels.ForTenant(tenant)
            .Where(v => ladderIds.Contains(v.RankLadderId))
            .ToListAsync();

        var ladderNames = await db.RankLadders.ForTenant(tenant)
            .Where(l => ladderIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Name })
            .ToDictionaryAsync(l => l.Id, l => l.Name);

        return [.. ranks.Select(r =>
        {
            var next = levels
                .Where(v => v.RankLadderId == r.RankLadderId && v.Ordinal == (r.RankLevel?.Ordinal ?? 0) + 1)
                .FirstOrDefault();

            var dto = FitnessMapper.ToDto(r, next, now);
            dto.LadderName = ladderNames.GetValueOrDefault(r.RankLadderId);
            return dto;
        })];
    }

    /// <summary>
    /// Who is ready to grade.
    ///
    /// Both gates have to be met — attendances at the rank and time served — because a ladder that
    /// only counts classes lets someone buy a black belt with a busy fortnight.
    /// </summary>
    public async Task<List<MemberRankDto>> GetGradingCandidatesAsync(Guid clubId, Guid ladderId)
    {
        var all = await db.MemberRanks.ForTenant(tenant)
            .Where(r => r.RankLadderId == ladderId && r.IsCurrent)
            .Include(r => r.Member)
            .Include(r => r.RankLevel)
            .ToListAsync();

        var candidates = new List<MemberRankDto>();
        var now = DateTime.UtcNow;

        var levels = await db.RankLevels.ForTenant(tenant)
            .Where(v => v.RankLadderId == ladderId)
            .ToListAsync();

        foreach (var rank in all.Where(r => r.Member?.HomeClubId == clubId && r.Member.Status == MemberStatus.Active))
        {
            var next = levels.FirstOrDefault(v => v.Ordinal == (rank.RankLevel?.Ordinal ?? 0) + 1);
            if (next is null) continue;

            var dto = FitnessMapper.ToDto(rank, next, now);
            if (dto.IsEligibleForGrading) candidates.Add(dto);
        }

        return [.. candidates.OrderByDescending(c => c.ProgressPercent)];
    }

    public async Task<MemberRankDto> AwardRankAsync(AwardRankDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var level = await db.RankLevels.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == request.RankLevelId)
            ?? throw new InvalidOperationException("Rank not found.");

        var current = await db.MemberRanks.ForTenant(tenant)
            .Where(r => r.MemberId == request.MemberId && r.RankLadderId == request.RankLadderId && r.IsCurrent)
            .ToListAsync();

        foreach (var old in current)
        {
            old.IsCurrent = false;
            old.StampUpdated(userId);
        }

        var rank = new MemberRank
        {
            MemberId = request.MemberId,
            RankLadderId = request.RankLadderId,
            RankLevelId = request.RankLevelId,
            AwardedOn = request.AwardedOn ?? now,
            AwardedByStaffId = userId == Guid.Empty ? null : userId,
            GradingEventId = request.GradingEventId,
            Status = RankAwardStatus.Awarded,
            IsCurrent = true,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.MemberRanks.Add(rank);

        db.MemberNotes.Add(new MemberNote
        {
            MemberId = request.MemberId,
            Kind = InteractionKind.SystemEvent,
            Body = $"Graded to {level.Name}",
            OccurredAt = rank.AwardedOn,
        }.StampNew(tenant, userId));

        if (request.ChargeGradingFee && level.GradingFee > 0)
        {
            var member = await db.Members.ForTenant(tenant).FirstAsync(m => m.Id == request.MemberId);
            db.Invoices.Add(new FitnessInvoice
            {
                InvoiceNumber = $"GRD-{now:yyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
                MemberId = request.MemberId,
                ClubId = member.HomeClubId,
                Status = InvoiceStatus.Issued,
                IssuedOn = now,
                DueOn = now.Date.AddDays(7),
                Subtotal = level.GradingFee,
                Total = level.GradingFee,
                BalanceDue = level.GradingFee,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.MemberRanks.ForTenant(tenant)
            .Include(r => r.Member)
            .Include(r => r.RankLevel)
            .FirstAsync(r => r.Id == rank.Id);

        var next = await db.RankLevels.ForTenant(tenant)
            .FirstOrDefaultAsync(v => v.RankLadderId == request.RankLadderId && v.Ordinal == level.Ordinal + 1);

        return FitnessMapper.ToDto(saved, next, now);
    }

    public async Task<GradingEventDto> SaveGradingEventAsync(Guid? id, GradingEventDto request, Guid userId)
    {
        GradingEvent gradingEvent;
        if (id is null)
        {
            gradingEvent = new GradingEvent().StampNew(tenant, userId);
            db.GradingEvents.Add(gradingEvent);
        }
        else
        {
            gradingEvent = await db.GradingEvents.ForTenant(tenant).FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new InvalidOperationException("Grading event not found.");
            gradingEvent.StampUpdated(userId);
        }

        gradingEvent.ClubId = request.ClubId;
        gradingEvent.RankLadderId = request.RankLadderId;
        gradingEvent.Name = request.Name;
        gradingEvent.HeldOn = request.HeldOn;
        gradingEvent.ExaminerStaffId = request.ExaminerStaffId;
        gradingEvent.ExternalExaminerName = request.ExternalExaminerName;
        gradingEvent.FeePerCandidate = request.FeePerCandidate;
        gradingEvent.Notes = request.Notes;
        gradingEvent.IsCompleted = request.IsCompleted;

        await db.SaveChangesAsync();

        var candidates = await db.MemberRanks.ForTenant(tenant)
            .Where(r => r.GradingEventId == gradingEvent.Id)
            .Include(r => r.Member)
            .Include(r => r.RankLevel)
            .ToListAsync();

        gradingEvent.CandidateCount = candidates.Count;
        gradingEvent.PassCount = candidates.Count(c => c.Status == RankAwardStatus.Awarded);
        await db.SaveChangesAsync();

        return new GradingEventDto
        {
            Id = gradingEvent.Id,
            ClubId = gradingEvent.ClubId,
            RankLadderId = gradingEvent.RankLadderId,
            Name = gradingEvent.Name,
            HeldOn = gradingEvent.HeldOn,
            ExaminerStaffId = gradingEvent.ExaminerStaffId,
            ExternalExaminerName = gradingEvent.ExternalExaminerName,
            CandidateCount = gradingEvent.CandidateCount,
            PassCount = gradingEvent.PassCount,
            FeePerCandidate = gradingEvent.FeePerCandidate,
            Notes = gradingEvent.Notes,
            IsCompleted = gradingEvent.IsCompleted,
            Candidates = [.. candidates.Select(c => FitnessMapper.ToDto(c, null, DateTime.UtcNow))],
        };
    }

    public async Task<SkillClearanceDto> GrantClearanceAsync(SkillClearanceDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var clearance = new SkillClearance
        {
            MemberId = request.MemberId,
            ClubId = request.ClubId,
            SkillName = request.SkillName,
            ClearedOn = request.ClearedOn == default ? now : request.ClearedOn,
            ClearedByStaffId = request.ClearedByStaffId == Guid.Empty ? userId : request.ClearedByStaffId,
            ExpiresOn = request.ExpiresOn,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.SkillClearances.Add(clearance);
        await db.SaveChangesAsync();

        var saved = await db.SkillClearances.ForTenant(tenant)
            .Include(c => c.Member)
            .FirstAsync(c => c.Id == clearance.Id);

        return FitnessMapper.ToDto(saved, now);
    }

    public async Task<List<SkillClearanceDto>> GetClearancesAsync(Guid memberId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var clearances = await db.SkillClearances.ForTenant(tenant)
            .Where(c => c.MemberId == memberId)
            .WhereIf(activeOnly, c => !c.IsRevoked && (c.ExpiresOn == null || c.ExpiresOn > now))
            .Include(c => c.Member)
            .OrderByDescending(c => c.ClearedOn)
            .ToListAsync();

        var staffIds = clearances.Select(c => c.ClearedByStaffId).Distinct().ToList();
        var names = await db.Staff.ForTenant(tenant)
            .Where(s => staffIds.Contains(s.Id))
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. clearances.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c, now);
            dto.ClearedByName = names.GetValueOrDefault(c.ClearedByStaffId);
            return dto;
        })];
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Reduces any result to one sortable number.
    ///
    /// Rounds-and-reps is the only awkward one: "12 + 8" has to beat "12 + 3" and lose to "13 + 0",
    /// so rounds are weighted far above reps rather than added to them.
    /// </summary>
    private static decimal Normalise(WorkoutResult r) => r.ScoreType switch
    {
        ScoreType.ForTime or ScoreType.TimeUnderLoad => r.TimeSeconds ?? decimal.MaxValue,
        ScoreType.RoundsAndReps => (r.Rounds ?? 0) * 1000m + (r.Reps ?? 0),
        ScoreType.Reps => r.Reps ?? 0,
        ScoreType.MaxLoad => r.LoadKg ?? 0,
        ScoreType.Distance => r.DistanceMetres ?? 0,
        ScoreType.Calories => r.Calories ?? 0,
        ScoreType.Points => r.Points ?? 0,
        ScoreType.PassFail => r.Passed == true ? 1 : 0,
        _ => 0,
    };

    private async Task UpdatePersonalRecordAsync(WorkoutResult result, Workout workout, Guid userId)
    {
        var recordName = workout.IsBenchmark ? workout.BenchmarkName ?? workout.Name : workout.Name;
        var lowerIsBetter = result.ScoreType is ScoreType.ForTime or ScoreType.TimeUnderLoad;

        var existing = await db.PersonalRecords.ForTenant(tenant)
            .FirstOrDefaultAsync(p => p.MemberId == result.MemberId && p.RecordName == recordName && p.RepMax == null);

        var isBetter = existing is null
            || (lowerIsBetter ? result.NormalisedScore < existing.Value : result.NormalisedScore > existing.Value);

        if (!isBetter) return;

        if (existing is null)
        {
            existing = new PersonalRecord
            {
                MemberId = result.MemberId,
                WorkoutId = workout.Id,
                RecordName = recordName,
                ScoreType = result.ScoreType,
                Unit = workout.ScoreUnit,
            }.StampNew(tenant, userId);

            db.PersonalRecords.Add(existing);
        }
        else
        {
            existing.PreviousValue = existing.Value;
            existing.PreviousAchievedOn = existing.AchievedOn;
            existing.StampUpdated(userId);
        }

        existing.Value = result.NormalisedScore;
        existing.AchievedOn = result.PerformedOn;
        existing.WorkoutResultId = result.Id;

        result.IsPersonalRecord = true;
    }

    private async Task<LeaderboardDto> ChallengeLeaderboardAsync(
        LeaderboardDto dto, Guid challengeId, Guid? viewerMemberId)
    {
        var challenge = await db.Challenges.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == challengeId);

        var participants = await db.ChallengeParticipants.ForTenant(tenant)
            .Where(p => p.ChallengeId == challengeId)
            .Include(p => p.Member)
            .OrderByDescending(p => p.CurrentValue)
            .ToListAsync();

        participants = [.. participants.Where(p => p.Member?.LeaderboardOptIn != false)];

        var rank = 1;
        foreach (var participant in participants)
        {
            dto.Entries.Add(new LeaderboardEntryDto
            {
                Rank = rank++,
                MemberId = participant.MemberId,
                MemberDisplayName = participant.Member is null
                    ? ""
                    : participant.Member.PreferredName ?? FitnessMapper.FullName(participant.Member),
                MemberPhotoUrl = participant.Member?.PhotoUrl,
                Score = participant.CurrentValue,
                ScoreDisplay = $"{participant.CurrentValue:0.##} {challenge?.Unit}".Trim(),
                Division = participant.TeamName,
                AchievedOn = participant.LastProgressAt,
            });
        }

        dto.Title = challenge?.Name ?? "Challenge";
        dto.TotalParticipants = dto.Entries.Count;
        dto.ViewerEntry = viewerMemberId is null ? null : dto.Entries.FirstOrDefault(e => e.MemberId == viewerMemberId);

        return dto;
    }
}
