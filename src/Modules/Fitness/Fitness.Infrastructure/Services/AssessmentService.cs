using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Assessments, progress and the coaching layer.
///
/// Two things carry their weight here. First, **every value knows its direction** — a drop in
/// body fat is progress and a drop in grip strength is not, and a screen that shows a green arrow
/// for both is worse than one showing no arrows at all. Second, **reading this data is an
/// auditable act**: body composition, health answers and progress photos are special-category
/// data, and who looked is part of the record.
/// </summary>
public class AssessmentService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    IComplianceService compliance) : IAssessmentService
{
    // ── Templates ────────────────────────────────────────────────────────────

    public async Task<List<AssessmentTemplateDto>> GetTemplatesAsync(Guid? clubId, bool activeOnly)
    {
        var templates = await db.AssessmentTemplates.ForTenant(tenant)
            .WhereIf(clubId is not null, t => t.ClubId == clubId || t.ClubId == null)
            .WhereIf(activeOnly, t => t.IsActive)
            .Include(t => t.Measures.Where(m => !m.IsDeleted))
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
            .ToListAsync();

        var usage = await db.Assessments.ForTenant(tenant)
            .Where(a => a.AssessmentTemplateId != null)
            .GroupBy(a => a.AssessmentTemplateId!.Value)
            .Select(g => new { TemplateId = g.Key, Count = g.Count() })
            .ToListAsync();

        var services = await db.Services.ForTenant(tenant)
            .Select(s => new { s.Id, s.Name }).ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. templates.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t);
            dto.UsageCount = usage.FirstOrDefault(u => u.TemplateId == t.Id)?.Count ?? 0;
            if (t.ServiceId is not null) dto.ServiceName = services.GetValueOrDefault(t.ServiceId.Value);
            return dto;
        })];
    }

    public async Task<AssessmentTemplateDto> SaveTemplateAsync(Guid? id, AssessmentTemplateDto request, Guid userId)
    {
        AssessmentTemplate template;
        if (id is null)
        {
            template = new AssessmentTemplate().StampNew(tenant, userId);
            db.AssessmentTemplates.Add(template);
        }
        else
        {
            template = await db.AssessmentTemplates.ForTenant(tenant)
                .Include(t => t.Measures.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Template not found.");
            template.StampUpdated(userId);
        }

        template.Name = request.Name;
        template.ClubId = request.ClubId;
        template.Purpose = request.Purpose;
        template.DisplayOrder = request.DisplayOrder;
        template.RecommendedIntervalDays = request.RecommendedIntervalDays;
        template.ServiceId = request.ServiceId;
        template.IsActive = request.IsActive;

        var existing = template.Measures.Where(m => !m.IsDeleted).ToList();
        var keptIds = request.Measures.Where(m => m.Id != Guid.Empty).Select(m => m.Id).ToHashSet();

        // A measure with history is retired rather than deleted — removing it would orphan every
        // chart that has ever plotted it.
        foreach (var gone in existing.Where(m => !keptIds.Contains(m.Id))) gone.StampDeleted(userId);

        var order = 0;
        foreach (var measureDto in request.Measures.OrderBy(m => m.DisplayOrder))
        {
            var measure = existing.FirstOrDefault(m => m.Id == measureDto.Id);
            if (measure is null)
            {
                measure = new AssessmentMeasure { AssessmentTemplateId = template.Id }.StampNew(tenant, userId);
                db.AssessmentMeasures.Add(measure);
            }
            else
            {
                measure.StampUpdated(userId);
            }

            measure.Name = measureDto.Name;
            measure.MeasureType = measureDto.MeasureType;
            measure.Direction = measureDto.Direction;
            measure.Unit = measureDto.Unit;
            measure.DisplayOrder = order++;
            measure.Grouping = measureDto.Grouping;
            measure.MinValue = measureDto.MinValue;
            measure.MaxValue = measureDto.MaxValue;
            measure.NormalLow = measureDto.NormalLow;
            measure.NormalHigh = measureDto.NormalHigh;
            measure.Instructions = measureDto.Instructions;
            measure.IsCalculated = measureDto.IsCalculated;
            measure.CalculationNote = measureDto.CalculationNote;
            measure.IsRequired = measureDto.IsRequired;
            measure.IsDeviceImported = measureDto.IsDeviceImported;
            measure.DeviceFieldName = measureDto.DeviceFieldName;
        }

        await db.SaveChangesAsync();

        var saved = await db.AssessmentTemplates.ForTenant(tenant)
            .Include(t => t.Measures.Where(m => !m.IsDeleted))
            .FirstAsync(t => t.Id == template.Id);

        return FitnessMapper.ToDto(saved);
    }

    // ── Assessments ──────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<AssessmentDto>> ListAsync(
        Guid? clubId, Guid? memberId, Guid? staffId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Assessments.ForTenant(tenant)
            .Include(a => a.Member)
            .Include(a => a.AssessmentTemplate)
            .Include(a => a.Values.Where(v => !v.IsDeleted))
            .WhereIf(clubId is not null, a => a.ClubId == clubId)
            .WhereIf(memberId is not null, a => a.MemberId == memberId)
            .WhereIf(staffId is not null, a => a.StaffId == staffId)
            .WhereIf(from is not null, a => a.PerformedOn >= from)
            .WhereIf(to is not null, a => a.PerformedOn <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(a => a.PerformedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var items = page.Select(a =>
        {
            var dto = FitnessMapper.ToDto(a);
            if (a.StaffId is not null) dto.StaffName = staffNames.GetValueOrDefault(a.StaffId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<AssessmentDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<AssessmentDto?> GetAsync(Guid assessmentId, Guid userId)
    {
        var assessment = await db.Assessments.ForTenant(tenant)
            .Include(a => a.Member)
            .Include(a => a.AssessmentTemplate).ThenInclude(t => t!.Measures.Where(m => !m.IsDeleted))
            .Include(a => a.Values.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

        if (assessment is null) return null;

        // Special-category data: looking at it is itself an event worth recording.
        await compliance.LogAsync("Viewed", nameof(Assessment), assessmentId, assessment.MemberId,
            "Body composition and assessment data", true, userId);

        var dto = FitnessMapper.ToDto(assessment);

        if (assessment.StaffId is not null)
        {
            dto.StaffName = await db.Staff.ForTenant(tenant)
                .Where(s => s.Id == assessment.StaffId)
                .Select(s => s.DisplayName ?? s.FirstName + " " + s.LastName)
                .FirstOrDefaultAsync();
        }

        var previous = await db.Assessments.ForTenant(tenant)
            .Where(a => a.MemberId == assessment.MemberId && a.PerformedOn < assessment.PerformedOn)
            .OrderByDescending(a => a.PerformedOn)
            .FirstOrDefaultAsync();

        if (previous is not null)
        {
            dto.PreviousAssessmentId = previous.Id;
            dto.PreviousPerformedOn = previous.PerformedOn;
            dto.DaysSincePrevious = (int)(assessment.PerformedOn.Date - previous.PerformedOn.Date).TotalDays;
        }

        // Direction comes from the template, and decides which way "better" points.
        var measures = assessment.AssessmentTemplate?.Measures.Where(m => !m.IsDeleted).ToList() ?? [];

        foreach (var value in dto.Values)
        {
            var measure = measures.FirstOrDefault(m => m.Id == value.AssessmentMeasureId);
            if (measure is not null) value.Direction = measure.Direction;

            value.IsImprovement = value.Change is null ? null : IsImprovement(value.Direction, value.Change.Value);
        }

        return dto;
    }

    /// <summary>
    /// Recording an assessment.
    ///
    /// Each value is compared against the same measure's previous reading so the change and its
    /// direction are computed once, on the way in, rather than recomputed by every chart, export
    /// and printed report that reads it later.
    /// </summary>
    public async Task<AssessmentDto> RecordAsync(RecordAssessmentDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        var template = request.AssessmentTemplateId is null
            ? null
            : await db.AssessmentTemplates.ForTenant(tenant)
                .Include(t => t.Measures.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == request.AssessmentTemplateId);

        var assessment = new Assessment
        {
            MemberId = request.MemberId,
            ClubId = request.ClubId,
            AssessmentTemplateId = request.AssessmentTemplateId,
            StaffId = request.StaffId,
            AppointmentId = request.AppointmentId,
            PerformedOn = request.PerformedOn ?? now,
            Summary = request.Summary,
            Recommendations = request.Recommendations,
            DeviceSource = request.DeviceSource,
            DeviceReference = request.DeviceReference,
            SharedWithMember = request.SharedWithMember,
            NextDueOn = template is null ? null : (request.PerformedOn ?? now).AddDays(template.RecommendedIntervalDays),
        }.StampNew(tenant, userId);

        db.Assessments.Add(assessment);

        // Previous values for the same measures, so a change can be computed.
        var measureNames = request.Values.Select(v => v.MeasureName).ToList();

        var previousValues = await db.AssessmentValues.ForTenant(tenant)
            .Where(v => measureNames.Contains(v.MeasureName)
                     && v.Assessment!.MemberId == request.MemberId)
            .Include(v => v.Assessment)
            .OrderByDescending(v => v.Assessment!.PerformedOn)
            .ToListAsync();

        var order = 0;
        foreach (var valueDto in request.Values)
        {
            var measure = template?.Measures.FirstOrDefault(m =>
                m.Id == valueDto.AssessmentMeasureId || m.Name == valueDto.MeasureName);

            var previous = previousValues
                .Where(v => v.MeasureName == valueDto.MeasureName && v.NumericValue is not null)
                .Select(v => v.NumericValue)
                .FirstOrDefault();

            var value = new AssessmentValue
            {
                AssessmentId = assessment.Id,
                AssessmentMeasureId = measure?.Id,
                MeasureName = valueDto.MeasureName,
                MeasureType = measure?.MeasureType ?? MeasureType.Count,
                Unit = valueDto.Unit ?? measure?.Unit,
                NumericValue = valueDto.NumericValue,
                TextValue = valueDto.TextValue,
                BooleanValue = valueDto.BooleanValue,
                PreviousValue = previous,
                Note = valueDto.Note,
                DisplayOrder = measure?.DisplayOrder ?? order++,
            }.StampNew(tenant, userId);

            if (previous is not null && valueDto.NumericValue is not null)
            {
                value.Change = valueDto.NumericValue - previous;
                value.ChangePercent = previous == 0 ? null : Math.Round(value.Change.Value / previous.Value * 100m, 2);
            }

            if (measure is not null && valueDto.NumericValue is not null)
                value.NormBand = BandFor(measure, valueDto.NumericValue.Value);

            db.AssessmentValues.Add(value);
        }

        // Derived measures the club did not have to enter by hand.
        await AddCalculatedValuesAsync(assessment, request, template, userId);

        // Goals track their measure, so a re-test moves the progress bar without anybody updating it.
        await UpdateGoalsAsync(request.MemberId, request.Values, userId);

        await db.SaveChangesAsync();

        await compliance.LogAsync("Created", nameof(Assessment), assessment.Id, request.MemberId,
            $"Assessment recorded with {request.Values.Count} measures", true, userId);

        return (await GetAsync(assessment.Id, userId))!;
    }

    /// <summary>
    /// One measure charted over time.
    ///
    /// The shape the progress screen actually wants: a client asks "is my body fat going down?",
    /// not "show me every assessment I have ever had and let me find the row".
    /// </summary>
    public async Task<List<ProgressSeriesDto>> GetProgressAsync(Guid memberId, List<string>? measureNames, Guid userId)
    {
        await compliance.LogAsync("Viewed", nameof(Member), memberId, memberId,
            "Progress and body-composition history", true, userId);

        var values = await db.AssessmentValues.ForTenant(tenant)
            .Where(v => v.Assessment!.MemberId == memberId && v.NumericValue != null)
            .WhereIf(measureNames is { Count: > 0 }, v => measureNames!.Contains(v.MeasureName))
            .Include(v => v.Assessment)
            .OrderBy(v => v.Assessment!.PerformedOn)
            .ToListAsync();

        var measures = await db.AssessmentMeasures.ForTenant(tenant).ToListAsync();
        var goals = await db.Goals.ForTenant(tenant)
            .Where(g => g.MemberId == memberId && g.Status == GoalStatus.Active)
            .ToListAsync();

        var series = new List<ProgressSeriesDto>();

        foreach (var group in values.GroupBy(v => v.MeasureName))
        {
            var measure = measures.FirstOrDefault(m => m.Name == group.Key);
            var points = group
                .Select(v => new ProgressPointDto
                {
                    On = v.Assessment!.PerformedOn,
                    Value = v.NumericValue!.Value,
                    AssessmentId = v.AssessmentId,
                    Note = v.Note,
                })
                .OrderBy(p => p.On)
                .ToList();

            if (points.Count == 0) continue;

            var direction = measure?.Direction ?? MeasureDirection.Neutral;
            var first = points[0].Value;
            var latest = points[^1].Value;

            var dto = new ProgressSeriesDto
            {
                MemberId = memberId,
                MeasureName = group.Key,
                Unit = group.First().Unit,
                Direction = direction,
                Points = points,
                First = first,
                Latest = latest,
                Best = direction == MeasureDirection.LowerIsBetter
                    ? points.Min(p => p.Value)
                    : points.Max(p => p.Value),
                TotalChange = latest - first,
                TotalChangePercent = first == 0 ? null : Math.Round((latest - first) / first * 100m, 2),
                NormalLow = measure?.NormalLow,
                NormalHigh = measure?.NormalHigh,
            };

            dto.IsImproving = dto.TotalChange is null ? null : IsImprovement(direction, dto.TotalChange.Value);

            var goal = goals.FirstOrDefault(g => g.MeasureName == group.Key);
            if (goal is not null)
            {
                dto.GoalValue = goal.TargetValue;
                dto.GoalDate = goal.TargetDate;
            }

            series.Add(dto);
        }

        return series;
    }

    public async Task<ProgressPhotoDto> AddPhotoAsync(ProgressPhotoDto request, Guid userId)
    {
        // Consent is checked here rather than trusted from the client: a progress photo held
        // without it is a data-protection problem, not a missing checkbox.
        if (!request.ConsentGiven)
            throw new InvalidOperationException("A progress photo can only be stored with the member's explicit consent.");

        var photo = new ProgressPhoto
        {
            MemberId = request.MemberId,
            AssessmentId = request.AssessmentId,
            TakenOn = request.TakenOn == default ? DateTime.UtcNow : request.TakenOn,
            Pose = request.Pose,
            ImageUrl = request.ImageUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            ConsentGiven = true,
            ConsentGivenOn = DateTime.UtcNow,
            MayUseInMarketing = request.MayUseInMarketing,
            TakenByStaffId = userId == Guid.Empty ? null : userId,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.ProgressPhotos.Add(photo);
        await db.SaveChangesAsync();

        await compliance.LogAsync("Created", nameof(ProgressPhoto), photo.Id, request.MemberId,
            $"Progress photo ({request.Pose})", true, userId);

        return new ProgressPhotoDto
        {
            Id = photo.Id,
            MemberId = photo.MemberId,
            AssessmentId = photo.AssessmentId,
            TakenOn = photo.TakenOn,
            Pose = photo.Pose,
            ImageUrl = photo.ImageUrl,
            ThumbnailUrl = photo.ThumbnailUrl,
            ConsentGiven = photo.ConsentGiven,
            MayUseInMarketing = photo.MayUseInMarketing,
            Note = photo.Note,
        };
    }

    public async Task<List<ProgressPhotoDto>> GetPhotosAsync(Guid memberId, Guid userId)
    {
        await compliance.LogAsync("Viewed", nameof(ProgressPhoto), null, memberId,
            "Progress photos", true, userId);

        var photos = await db.ProgressPhotos.ForTenant(tenant)
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.TakenOn)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. photos.Select(p => new ProgressPhotoDto
        {
            Id = p.Id,
            MemberId = p.MemberId,
            AssessmentId = p.AssessmentId,
            TakenOn = p.TakenOn,
            Pose = p.Pose,
            ImageUrl = p.ImageUrl,
            ThumbnailUrl = p.ThumbnailUrl,
            ConsentGiven = p.ConsentGiven,
            MayUseInMarketing = p.MayUseInMarketing,
            TakenByName = p.TakenByStaffId is null ? null : staffNames.GetValueOrDefault(p.TakenByStaffId.Value),
            Note = p.Note,
        })];
    }

    // ── Goals ────────────────────────────────────────────────────────────────

    public async Task<List<MemberGoalDto>> GetGoalsAsync(Guid memberId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var goals = await db.Goals.ForTenant(tenant)
            .Where(g => g.MemberId == memberId)
            .WhereIf(activeOnly, g => g.Status == GoalStatus.Active)
            .OrderByDescending(g => g.SetOn)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. goals.Select(g =>
        {
            var dto = FitnessMapper.ToDto(g, now);
            if (g.SetByStaffId is not null) dto.SetByName = staffNames.GetValueOrDefault(g.SetByStaffId.Value);
            return dto;
        })];
    }

    public async Task<MemberGoalDto> SaveGoalAsync(Guid? id, MemberGoalDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        MemberGoal goal;
        if (id is null)
        {
            goal = new MemberGoal { MemberId = request.MemberId, SetOn = now }.StampNew(tenant, userId);
            db.Goals.Add(goal);
        }
        else
        {
            goal = await db.Goals.ForTenant(tenant).FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new InvalidOperationException("Goal not found.");
            goal.StampUpdated(userId);
        }

        goal.Title = request.Title;
        goal.MeasureName = request.MeasureName;
        goal.Unit = request.Unit;
        goal.StartValue = request.StartValue;
        goal.TargetValue = request.TargetValue;
        goal.CurrentValue = request.CurrentValue ?? request.StartValue;
        goal.TargetDate = request.TargetDate;
        goal.Status = request.Status;
        goal.SetByStaffId = request.SetByName is null ? userId : goal.SetByStaffId;
        goal.WhyItMatters = request.WhyItMatters;

        goal.ProgressPercent = ComputeProgress(goal);

        if (goal.ProgressPercent >= 100 && goal.Status == GoalStatus.Active)
        {
            goal.Status = GoalStatus.Achieved;
            goal.AchievedOn = now;
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(goal, now);
    }

    // ── Coaching ─────────────────────────────────────────────────────────────

    public async Task<NutritionPlanDto> SaveNutritionPlanAsync(Guid? id, NutritionPlanDto request, Guid userId)
    {
        NutritionPlan plan;
        if (id is null)
        {
            plan = new NutritionPlan { MemberId = request.MemberId, ClubId = request.ClubId }.StampNew(tenant, userId);
            db.NutritionPlans.Add(plan);
        }
        else
        {
            plan = await db.NutritionPlans.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new InvalidOperationException("Nutrition plan not found.");
            plan.StampUpdated(userId);
        }

        plan.StaffId = request.StaffId;
        plan.Name = request.Name;
        plan.StartsOn = request.StartsOn;
        plan.EndsOn = request.EndsOn;
        plan.DailyCalories = request.DailyCalories;
        plan.ProteinGrams = request.ProteinGrams;
        plan.CarbGrams = request.CarbGrams;
        plan.FatGrams = request.FatGrams;
        plan.FibreGrams = request.FibreGrams;
        plan.WaterMillilitres = request.WaterMillilitres;
        plan.MealGuidance = request.MealGuidance;
        plan.Restrictions = request.Restrictions;
        plan.SupplementNotes = request.SupplementNotes;
        plan.IsActive = request.IsActive;

        // The disclaimer is not optional. This is coaching support, not dietetics, and the plan
        // has to say so wherever it is shown.
        plan.Disclaimer = string.IsNullOrWhiteSpace(request.Disclaimer)
            ? "General guidance to support your training. It is not medical or dietetic advice — " +
              "please speak to your doctor or a registered dietitian about any medical condition, " +
              "medication or specific dietary requirement."
            : request.Disclaimer;

        await db.SaveChangesAsync();

        return new NutritionPlanDto
        {
            Id = plan.Id,
            MemberId = plan.MemberId,
            StaffId = plan.StaffId,
            ClubId = plan.ClubId,
            Name = plan.Name,
            StartsOn = plan.StartsOn,
            EndsOn = plan.EndsOn,
            DailyCalories = plan.DailyCalories,
            ProteinGrams = plan.ProteinGrams,
            CarbGrams = plan.CarbGrams,
            FatGrams = plan.FatGrams,
            FibreGrams = plan.FibreGrams,
            WaterMillilitres = plan.WaterMillilitres,
            MealGuidance = plan.MealGuidance,
            Restrictions = plan.Restrictions,
            SupplementNotes = plan.SupplementNotes,
            Disclaimer = plan.Disclaimer,
            IsActive = plan.IsActive,
        };
    }

    public async Task<List<NutritionPlanDto>> GetNutritionPlansAsync(Guid memberId, bool activeOnly)
    {
        var plans = await db.NutritionPlans.ForTenant(tenant)
            .Where(p => p.MemberId == memberId)
            .WhereIf(activeOnly, p => p.IsActive)
            .Include(p => p.Member)
            .OrderByDescending(p => p.StartsOn)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. plans.Select(p => new NutritionPlanDto
        {
            Id = p.Id,
            MemberId = p.MemberId,
            MemberName = p.Member is null ? null : FitnessMapper.FullName(p.Member),
            StaffId = p.StaffId,
            StaffName = p.StaffId is null ? null : staffNames.GetValueOrDefault(p.StaffId.Value),
            ClubId = p.ClubId,
            Name = p.Name,
            StartsOn = p.StartsOn,
            EndsOn = p.EndsOn,
            DailyCalories = p.DailyCalories,
            ProteinGrams = p.ProteinGrams,
            CarbGrams = p.CarbGrams,
            FatGrams = p.FatGrams,
            FibreGrams = p.FibreGrams,
            WaterMillilitres = p.WaterMillilitres,
            MealGuidance = p.MealGuidance,
            Restrictions = p.Restrictions,
            SupplementNotes = p.SupplementNotes,
            Disclaimer = p.Disclaimer,
            IsActive = p.IsActive,
        })];
    }

    public async Task<HabitTrackerDto> SaveHabitAsync(Guid? id, HabitTrackerDto request, Guid userId)
    {
        HabitTracker habit;
        if (id is null)
        {
            habit = new HabitTracker { MemberId = request.MemberId, StartsOn = request.StartsOn }.StampNew(tenant, userId);
            db.Habits.Add(habit);
        }
        else
        {
            habit = await db.Habits.ForTenant(tenant)
                .Include(h => h.Entries.Where(e => !e.IsDeleted))
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new InvalidOperationException("Habit not found.");
            habit.StampUpdated(userId);
        }

        habit.NutritionPlanId = request.NutritionPlanId;
        habit.StaffId = request.StaffId;
        habit.HabitName = request.HabitName;
        habit.Unit = request.Unit;
        habit.DailyTarget = request.DailyTarget;
        habit.EndsOn = request.EndsOn;
        habit.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return await BuildHabitDtoAsync(habit.Id);
    }

    public async Task<List<HabitTrackerDto>> GetHabitsAsync(Guid memberId, bool activeOnly)
    {
        var habits = await db.Habits.ForTenant(tenant)
            .Where(h => h.MemberId == memberId)
            .WhereIf(activeOnly, h => h.IsActive)
            .Select(h => h.Id)
            .ToListAsync();

        var results = new List<HabitTrackerDto>();
        foreach (var id in habits) results.Add(await BuildHabitDtoAsync(id));

        return results;
    }

    /// <summary>
    /// Logging one day of a habit.
    ///
    /// Recomputes the streak and adherence from the entries rather than incrementing counters,
    /// because backfilling a missed day should heal the streak rather than leave it wrong forever.
    /// </summary>
    public async Task<HabitEntryDto> LogHabitAsync(
        Guid habitTrackerId, DateTime forDate, decimal? value, bool completed, string? note, Guid userId)
    {
        var day = forDate.Date;

        var habit = await db.Habits.ForTenant(tenant)
            .Include(h => h.Entries.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(h => h.Id == habitTrackerId)
            ?? throw new InvalidOperationException("Habit not found.");

        var entry = habit.Entries.FirstOrDefault(e => e.ForDate == day);

        if (entry is null)
        {
            entry = new HabitEntry
            {
                HabitTrackerId = habitTrackerId,
                MemberId = habit.MemberId,
                ForDate = day,
            }.StampNew(tenant, userId);

            db.HabitEntries.Add(entry);
            habit.Entries.Add(entry);
        }
        else
        {
            entry.StampUpdated(userId);
        }

        entry.Value = value;
        entry.Completed = completed || (habit.DailyTarget is not null && value >= habit.DailyTarget);
        entry.Note = note;
        entry.LoggedAt = DateTime.UtcNow;

        RecomputeStreak(habit);
        await db.SaveChangesAsync();

        return new HabitEntryDto
        {
            Id = entry.Id,
            HabitTrackerId = entry.HabitTrackerId,
            ForDate = entry.ForDate,
            Value = entry.Value,
            Completed = entry.Completed,
            Note = entry.Note,
            LoggedAt = entry.LoggedAt,
        };
    }

    public async Task<List<CoachCheckInDto>> GetCheckInsAsync(Guid? staffId, Guid? memberId, bool dueOnly)
    {
        var now = DateTime.UtcNow;

        var checkIns = await db.CoachCheckIns.ForTenant(tenant)
            .WhereIf(staffId is not null, c => c.StaffId == staffId)
            .WhereIf(memberId is not null, c => c.MemberId == memberId)
            .WhereIf(dueOnly, c => !c.IsComplete)
            .Include(c => c.Member)
            .OrderBy(c => c.DueOn)
            .Take(200)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. checkIns.Select(c => new CoachCheckInDto
        {
            Id = c.Id,
            MemberId = c.MemberId,
            MemberName = c.Member is null ? null : FitnessMapper.FullName(c.Member),
            MemberPhotoUrl = c.Member?.PhotoUrl,
            StaffId = c.StaffId,
            StaffName = staffNames.GetValueOrDefault(c.StaffId),
            ClubId = c.ClubId,
            PeriodStart = c.PeriodStart,
            PeriodEnd = c.PeriodEnd,
            DueOn = c.DueOn,
            MemberSubmittedAt = c.MemberSubmittedAt,
            MemberResponse = c.MemberResponse,
            EnergyRating = c.EnergyRating,
            SleepRating = c.SleepRating,
            StressRating = c.StressRating,
            AdherenceRating = c.AdherenceRating,
            CoachRepliedAt = c.CoachRepliedAt,
            CoachResponse = c.CoachResponse,
            AdjustmentsMade = c.AdjustmentsMade,
            IsComplete = c.IsComplete,
            WasMissed = c.WasMissed,
            AwaitingCoach = c.MemberSubmittedAt is not null && c.CoachRepliedAt is null,
            AwaitingMember = c.MemberSubmittedAt is null,
            IsOverdue = !c.IsComplete && c.DueOn < now,
        })];
    }

    public async Task<CoachCheckInDto> SaveCheckInAsync(Guid? id, CoachCheckInDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        CoachCheckIn checkIn;
        if (id is null)
        {
            checkIn = new CoachCheckIn
            {
                MemberId = request.MemberId,
                StaffId = request.StaffId,
                ClubId = request.ClubId,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                DueOn = request.DueOn,
            }.StampNew(tenant, userId);

            db.CoachCheckIns.Add(checkIn);
        }
        else
        {
            checkIn = await db.CoachCheckIns.ForTenant(tenant)
                .Include(c => c.Member)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Check-in not found.");
            checkIn.StampUpdated(userId);
        }

        if (request.MemberResponse is not null && checkIn.MemberSubmittedAt is null)
        {
            checkIn.MemberResponse = request.MemberResponse;
            checkIn.MemberSubmittedAt = now;
            checkIn.EnergyRating = request.EnergyRating;
            checkIn.SleepRating = request.SleepRating;
            checkIn.StressRating = request.StressRating;
            checkIn.AdherenceRating = request.AdherenceRating;
        }

        if (request.CoachResponse is not null)
        {
            checkIn.CoachResponse = request.CoachResponse;
            checkIn.AdjustmentsMade = request.AdjustmentsMade;
            checkIn.CoachRepliedAt = now;
            checkIn.IsComplete = true;
        }

        await db.SaveChangesAsync();

        return (await GetCheckInsAsync(null, checkIn.MemberId, false))
            .First(c => c.Id == checkIn.Id);
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private static bool IsImprovement(MeasureDirection direction, decimal change) => direction switch
    {
        MeasureDirection.HigherIsBetter => change > 0,
        MeasureDirection.LowerIsBetter => change < 0,
        _ => change != 0,
    };

    private static string? BandFor(AssessmentMeasure measure, decimal value)
    {
        if (measure.NormalLow is null && measure.NormalHigh is null) return null;

        if (measure.NormalLow is not null && value < measure.NormalLow)
            return measure.Direction == MeasureDirection.LowerIsBetter ? "Excellent" : "Below normal";

        if (measure.NormalHigh is not null && value > measure.NormalHigh)
            return measure.Direction == MeasureDirection.HigherIsBetter ? "Excellent" : "Above normal";

        return "Within normal range";
    }

    /// <summary>
    /// Derives the measures the club should not have to type — BMI, waist-to-hip, lean mass.
    ///
    /// Calculated rather than entered so they cannot silently disagree with the numbers they come
    /// from, which is the usual fate of a BMI column somebody fills in by hand.
    /// </summary>
    private async Task AddCalculatedValuesAsync(
        Assessment assessment, RecordAssessmentDto request, AssessmentTemplate? template, Guid userId)
    {
        decimal? Find(params string[] names) => request.Values
            .FirstOrDefault(v => names.Any(n => string.Equals(v.MeasureName, n, StringComparison.OrdinalIgnoreCase)))
            ?.NumericValue;

        var weight = Find("Weight", "Body weight");
        var height = Find("Height");
        var waist = Find("Waist");
        var hip = Find("Hip", "Hips");
        var bodyFat = Find("Body fat", "Body fat %");

        var derived = new List<(string Name, decimal Value, string Unit, MeasureDirection Direction)>();

        if (weight is > 0 && height is > 0)
        {
            var metres = height.Value / 100m;
            derived.Add(("BMI", Math.Round(weight.Value / (metres * metres), 1), "kg/m²", MeasureDirection.RangeIsBetter));
        }

        if (waist is > 0 && hip is > 0)
            derived.Add(("Waist-to-hip ratio", Math.Round(waist.Value / hip.Value, 2), "", MeasureDirection.LowerIsBetter));

        if (weight is > 0 && bodyFat is > 0)
        {
            derived.Add(("Fat mass", Math.Round(weight.Value * bodyFat.Value / 100m, 1), "kg", MeasureDirection.LowerIsBetter));
            derived.Add(("Lean mass", Math.Round(weight.Value * (100m - bodyFat.Value) / 100m, 1), "kg", MeasureDirection.HigherIsBetter));
        }

        var order = 900;
        foreach (var (name, value, unit, direction) in derived)
        {
            // Skip anything the operator entered themselves.
            if (request.Values.Any(v => string.Equals(v.MeasureName, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var previous = await db.AssessmentValues.ForTenant(tenant)
                .Where(v => v.MeasureName == name && v.Assessment!.MemberId == assessment.MemberId
                         && v.NumericValue != null)
                .OrderByDescending(v => v.Assessment!.PerformedOn)
                .Select(v => v.NumericValue)
                .FirstOrDefaultAsync();

            var measure = template?.Measures.FirstOrDefault(m => m.Name == name);

            db.AssessmentValues.Add(new AssessmentValue
            {
                AssessmentId = assessment.Id,
                AssessmentMeasureId = measure?.Id,
                MeasureName = name,
                MeasureType = MeasureType.Score,
                Unit = unit,
                NumericValue = value,
                PreviousValue = previous,
                Change = previous is null ? null : value - previous,
                ChangePercent = previous is null or 0 ? null : Math.Round((value - previous.Value) / previous.Value * 100m, 2),
                Note = "Calculated",
                DisplayOrder = order++,
            }.StampNew(tenant, userId));
        }
    }

    private async Task UpdateGoalsAsync(Guid memberId, List<RecordMeasureValueDto> values, Guid userId)
    {
        var now = DateTime.UtcNow;

        var goals = await db.Goals.ForTenant(tenant)
            .Where(g => g.MemberId == memberId && g.Status == GoalStatus.Active && g.MeasureName != null)
            .ToListAsync();

        foreach (var goal in goals)
        {
            var value = values.FirstOrDefault(v =>
                string.Equals(v.MeasureName, goal.MeasureName, StringComparison.OrdinalIgnoreCase))?.NumericValue;

            if (value is null) continue;

            goal.CurrentValue = value;
            goal.ProgressPercent = ComputeProgress(goal);
            goal.StampUpdated(userId);

            if (goal.ProgressPercent >= 100)
            {
                goal.Status = GoalStatus.Achieved;
                goal.AchievedOn = now;

                db.MemberNotes.Add(new MemberNote
                {
                    MemberId = memberId,
                    Kind = InteractionKind.SystemEvent,
                    Body = $"Goal achieved: {goal.Title}",
                    OccurredAt = now,
                    IsPinned = true,
                }.StampNew(tenant, userId));
            }
        }
    }

    private static int ComputeProgress(MemberGoal goal)
    {
        if (goal.StartValue is null || goal.TargetValue is null || goal.CurrentValue is null) return 0;

        var total = goal.TargetValue.Value - goal.StartValue.Value;
        if (total == 0) return 100;

        var moved = goal.CurrentValue.Value - goal.StartValue.Value;
        return (int)Math.Clamp(moved / total * 100m, 0, 100);
    }

    private void RecomputeStreak(HabitTracker habit)
    {
        var entries = habit.Entries
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.ForDate)
            .ToList();

        var streak = 0;
        var cursor = DateTime.UtcNow.Date;

        // Today not being logged yet does not break a streak — the day is not over.
        if (!entries.Any(e => e.ForDate == cursor)) cursor = cursor.AddDays(-1);

        while (true)
        {
            var entry = entries.FirstOrDefault(e => e.ForDate == cursor);
            if (entry is null || !entry.Completed) break;
            streak++;
            cursor = cursor.AddDays(-1);
        }

        habit.CurrentStreak = streak;
        if (streak > habit.LongestStreak) habit.LongestStreak = streak;

        var totalDays = Math.Max(1, (DateTime.UtcNow.Date - habit.StartsOn.Date).Days + 1);
        habit.AdherencePercent = FitnessMapper.Percent(entries.Count(e => e.Completed), totalDays);
    }

    private async Task<HabitTrackerDto> BuildHabitDtoAsync(Guid habitId)
    {
        var today = DateTime.UtcNow.Date;

        var habit = await db.Habits.ForTenant(tenant)
            .Include(h => h.Member)
            .Include(h => h.Entries.Where(e => !e.IsDeleted))
            .FirstAsync(h => h.Id == habitId);

        return new HabitTrackerDto
        {
            Id = habit.Id,
            MemberId = habit.MemberId,
            MemberName = habit.Member is null ? null : FitnessMapper.FullName(habit.Member),
            NutritionPlanId = habit.NutritionPlanId,
            StaffId = habit.StaffId,
            HabitName = habit.HabitName,
            Unit = habit.Unit,
            DailyTarget = habit.DailyTarget,
            StartsOn = habit.StartsOn,
            EndsOn = habit.EndsOn,
            CurrentStreak = habit.CurrentStreak,
            LongestStreak = habit.LongestStreak,
            AdherencePercent = habit.AdherencePercent,
            IsActive = habit.IsActive,
            LoggedToday = habit.Entries.Any(e => !e.IsDeleted && e.ForDate == today),
            RecentEntries = [.. habit.Entries
                .Where(e => !e.IsDeleted)
                .OrderByDescending(e => e.ForDate)
                .Take(30)
                .Select(e => new HabitEntryDto
                {
                    Id = e.Id,
                    HabitTrackerId = e.HabitTrackerId,
                    ForDate = e.ForDate,
                    Value = e.Value,
                    Completed = e.Completed,
                    Note = e.Note,
                    LoggedAt = e.LoggedAt,
                })],
        };
    }
}
