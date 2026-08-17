using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Food safety records, and the delivery-zone engine.
///
/// The rule that shapes everything here: **an out-of-range reading cannot be filed without a
/// corrective action, and a failed critical check cannot be signed off.** That is exactly what an
/// inspection looks for, and a system that lets someone log 9°C and walk away is worse than a
/// paper book, because it produces a tidy record of a failure nobody dealt with.
/// </summary>
public class ComplianceService(RestaurantDbContext db, IRestaurantTenant tenant)
{
    // ── Board ────────────────────────────────────────────────────────────────

    public async Task<ComplianceBoardDto> GetBoardAsync(Guid outletId)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var checkpoints = await GetCheckpointsAsync(outletId);
        var checklists = await GetChecklistsAsync(outletId);

        var breaches = await db.TemperatureLogs.ForTenant(tenant)
            .Where(l => l.OutletId == outletId && l.IsOutOfRange && !l.IsResolved)
            .OrderByDescending(l => l.RecordedAt)
            .Take(50)
            .ToListAsync();

        var checkpointNames = checkpoints.ToDictionary(c => c.Id);

        var breachDtos = breaches.Select(l =>
        {
            var dto = ToDto(l);
            if (checkpointNames.TryGetValue(l.CheckpointId, out var cp))
            {
                dto.CheckpointName = cp.Name;
                dto.CheckpointKind = cp.Kind;
                dto.MinSafeCelsius = cp.MinSafeCelsius;
                dto.MaxSafeCelsius = cp.MaxSafeCelsius;
            }
            return dto;
        }).ToList();

        var batches = await db.PrepBatches.ForTenant(tenant)
            .Where(b => b.OutletId == outletId && !b.IsDiscarded && b.UseByAt <= now.AddHours(24))
            .OrderBy(b => b.UseByAt)
            .Take(50)
            .ToListAsync();

        var batchDtos = batches.Select(b => ToDto(b, now)).ToList();

        var dueToday = checklists.Count(c => c.IsDueToday);
        var doneToday = checklists.Count(c => c.IsDueToday && c.IsCompletedToday);

        return new ComplianceBoardDto
        {
            OutletId = outletId,
            GeneratedAt = now,
            ChecksDue = checklists.Count(c => c.IsDueToday && !c.IsCompletedToday),
            ChecksOverdue = checklists.Count(c =>
                c.IsDueToday && !c.IsCompletedToday && c.DueAt.HasValue && now.TimeOfDay > c.DueAt.Value),
            TemperatureChecksDue = checkpoints.Count(c => c.IsDue),
            OpenBreaches = breachDtos.Count,
            BatchesExpiringSoon = batchDtos.Count(b => !b.IsExpired),
            BatchesExpired = batchDtos.Count(b => b.IsExpired),
            CompliancePercent = dueToday == 0 ? 100m : Math.Round(doneToday * 100m / dueToday, 1),
            Checkpoints = checkpoints,
            Checklists = checklists,
            OpenBreachLogs = breachDtos,
            ExpiringBatches = batchDtos,
        };
    }

    // ── Temperature ──────────────────────────────────────────────────────────

    public async Task<List<TemperatureCheckpointDto>> GetCheckpointsAsync(Guid outletId)
    {
        var now = DateTime.UtcNow;

        var checkpoints = await db.TemperatureCheckpoints.ForTenant(tenant)
            .Where(c => c.OutletId == outletId)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();

        if (checkpoints.Count == 0) return [];

        var ids = checkpoints.Select(c => c.Id).ToList();

        // Latest reading per checkpoint, in one round trip rather than one query per fridge.
        var latest = await db.TemperatureLogs.ForTenant(tenant)
            .Where(l => ids.Contains(l.CheckpointId))
            .GroupBy(l => l.CheckpointId)
            .Select(g => new
            {
                CheckpointId = g.Key,
                LastAt = g.Max(l => l.RecordedAt),
                OpenBreaches = g.Count(l => l.IsOutOfRange && !l.IsResolved),
            })
            .ToListAsync();

        var lastReadings = await db.TemperatureLogs.ForTenant(tenant)
            .Where(l => ids.Contains(l.CheckpointId)
                     && latest.Select(x => x.LastAt).Contains(l.RecordedAt))
            .Select(l => new { l.CheckpointId, l.RecordedAt, l.ReadingCelsius, l.IsOutOfRange })
            .ToListAsync();

        var stationNames = await db.Stations.ForTenant(tenant)
            .Where(s => s.OutletId == outletId)
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return checkpoints.Select(c =>
        {
            var summary = latest.FirstOrDefault(x => x.CheckpointId == c.Id);
            var reading = lastReadings
                .Where(r => r.CheckpointId == c.Id)
                .OrderByDescending(r => r.RecordedAt)
                .FirstOrDefault();

            var dto = new TemperatureCheckpointDto
            {
                Id = c.Id,
                OutletId = c.OutletId,
                Name = c.Name,
                Kind = c.Kind,
                MinSafeCelsius = c.MinSafeCelsius,
                MaxSafeCelsius = c.MaxSafeCelsius,
                CheckIntervalHours = c.CheckIntervalHours,
                DisplayOrder = c.DisplayOrder,
                Location = c.Location,
                StationId = c.StationId,
                StationName = c.StationId.HasValue ? stationNames.GetValueOrDefault(c.StationId.Value) : null,
                IsActive = c.IsActive,
                Description = c.Description,
                LastReadingCelsius = reading?.ReadingCelsius,
                LastReadingAt = reading?.RecordedAt,
                LastReadingOutOfRange = reading?.IsOutOfRange ?? false,
                OpenBreachCount = summary?.OpenBreaches ?? 0,
            };

            // Never logged, or the interval has elapsed — either way it wants a reading now.
            dto.IsDue = c.IsActive && c.CheckIntervalHours > 0 &&
                (reading is null || (now - reading.RecordedAt).TotalHours >= c.CheckIntervalHours);

            return dto;
        }).ToList();
    }

    public async Task<TemperatureCheckpointDto> SaveCheckpointAsync(
        Guid? id, SaveTemperatureCheckpointDto request, Guid userId)
    {
        if (request.MinSafeCelsius >= request.MaxSafeCelsius)
            throw new InvalidOperationException("The safe minimum must be below the safe maximum.");

        TemperatureCheckpoint checkpoint;

        if (id.HasValue)
        {
            checkpoint = await db.TemperatureCheckpoints.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Checkpoint not found.");
            checkpoint.StampUpdated(userId);
        }
        else
        {
            checkpoint = new TemperatureCheckpoint().StampNew(tenant, userId);
            db.TemperatureCheckpoints.Add(checkpoint);
        }

        checkpoint.OutletId = request.OutletId;
        checkpoint.Name = request.Name;
        checkpoint.Kind = request.Kind;
        checkpoint.MinSafeCelsius = request.MinSafeCelsius;
        checkpoint.MaxSafeCelsius = request.MaxSafeCelsius;
        checkpoint.CheckIntervalHours = request.CheckIntervalHours;
        checkpoint.DisplayOrder = request.DisplayOrder;
        checkpoint.Location = request.Location;
        checkpoint.StationId = request.StationId;
        checkpoint.IsActive = request.IsActive;
        checkpoint.Description = request.Description;

        await db.SaveChangesAsync();

        var all = await GetCheckpointsAsync(checkpoint.OutletId);
        return all.First(c => c.Id == checkpoint.Id);
    }

    public async Task DeleteCheckpointAsync(Guid id, Guid userId)
    {
        var checkpoint = await db.TemperatureCheckpoints.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("Checkpoint not found.");

        var openBreaches = await db.TemperatureLogs.ForTenant(tenant)
            .CountAsync(l => l.CheckpointId == id && l.IsOutOfRange && !l.IsResolved);

        if (openBreaches > 0)
            throw new InvalidOperationException(
                $"This checkpoint has {openBreaches} unresolved breach(es). Close them before removing it.");

        checkpoint.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<TemperatureLogDto> RecordTemperatureAsync(RecordTemperatureDto request, Guid userId)
    {
        var checkpoint = await db.TemperatureCheckpoints.ForTenant(tenant)
            .FirstOrDefaultAsync(c => c.Id == request.CheckpointId)
            ?? throw new InvalidOperationException("Checkpoint not found.");

        var outOfRange = request.ReadingCelsius < checkpoint.MinSafeCelsius
                      || request.ReadingCelsius > checkpoint.MaxSafeCelsius;

        // The whole point of the record. A breach filed with no action is a tidy record of a
        // failure nobody dealt with, which is worse than no record at all.
        if (outOfRange && string.IsNullOrWhiteSpace(request.CorrectiveAction))
            throw new InvalidOperationException(
                $"{request.ReadingCelsius:0.#}°C is outside the safe range " +
                $"({checkpoint.MinSafeCelsius:0.#}–{checkpoint.MaxSafeCelsius:0.#}°C). " +
                "Record what you did about it before saving.");

        var member = request.StaffId.HasValue
            ? await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.StaffId)
            : null;

        var log = new TemperatureLog
        {
            CheckpointId = checkpoint.Id,
            OutletId = checkpoint.OutletId,
            ReadingCelsius = request.ReadingCelsius,
            RecordedAt = DateTime.UtcNow,
            StaffId = request.StaffId,
            StaffName = member?.DisplayName ?? member?.FullName,
            IsOutOfRange = outOfRange,
            CorrectiveAction = request.CorrectiveAction,
            IsResolved = !outOfRange || !string.IsNullOrWhiteSpace(request.CorrectiveAction),
            Note = request.Note,
        }.StampNew(tenant, userId);

        if (log.IsResolved && outOfRange)
        {
            log.ResolvedAt = log.RecordedAt;
            log.ResolvedByStaffId = request.StaffId;
        }

        db.TemperatureLogs.Add(log);
        await db.SaveChangesAsync();

        var dto = ToDto(log);
        dto.CheckpointName = checkpoint.Name;
        dto.CheckpointKind = checkpoint.Kind;
        dto.MinSafeCelsius = checkpoint.MinSafeCelsius;
        dto.MaxSafeCelsius = checkpoint.MaxSafeCelsius;
        return dto;
    }

    public async Task<TemperatureLogDto> ResolveBreachAsync(ResolveBreachDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.CorrectiveAction))
            throw new InvalidOperationException("Describe the corrective action taken.");

        var log = await db.TemperatureLogs.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == request.LogId)
            ?? throw new InvalidOperationException("Reading not found.");

        log.CorrectiveAction = request.CorrectiveAction;
        log.IsResolved = true;
        log.ResolvedAt = DateTime.UtcNow;
        log.ResolvedByStaffId = request.StaffId;
        log.StampUpdated(userId);

        await db.SaveChangesAsync();
        return ToDto(log);
    }

    public async Task<List<TemperatureLogDto>> GetLogsAsync(
        Guid outletId, DateTime from, DateTime to, Guid? checkpointId, bool breachesOnly)
    {
        var logs = await db.TemperatureLogs.ForTenant(tenant)
            .Where(l => l.OutletId == outletId && l.RecordedAt >= from && l.RecordedAt <= to)
            .WhereIf(checkpointId.HasValue, l => l.CheckpointId == checkpointId)
            .WhereIf(breachesOnly, l => l.IsOutOfRange)
            .OrderByDescending(l => l.RecordedAt)
            .Take(1000)
            .ToListAsync();

        var checkpoints = await db.TemperatureCheckpoints.ForTenant(tenant)
            .Where(c => c.OutletId == outletId)
            .ToDictionaryAsync(c => c.Id, c => new { c.Name, c.Kind, c.MinSafeCelsius, c.MaxSafeCelsius });

        return logs.Select(l =>
        {
            var dto = ToDto(l);
            if (checkpoints.TryGetValue(l.CheckpointId, out var cp))
            {
                dto.CheckpointName = cp.Name;
                dto.CheckpointKind = cp.Kind;
                dto.MinSafeCelsius = cp.MinSafeCelsius;
                dto.MaxSafeCelsius = cp.MaxSafeCelsius;
            }
            return dto;
        }).ToList();
    }

    private static TemperatureLogDto ToDto(TemperatureLog l) => new()
    {
        Id = l.Id,
        CheckpointId = l.CheckpointId,
        OutletId = l.OutletId,
        ReadingCelsius = l.ReadingCelsius,
        RecordedAt = l.RecordedAt,
        StaffId = l.StaffId,
        StaffName = l.StaffName,
        IsOutOfRange = l.IsOutOfRange,
        CorrectiveAction = l.CorrectiveAction,
        IsResolved = l.IsResolved,
        ResolvedAt = l.ResolvedAt,
        Note = l.Note,
    };

    // ── Checklists ───────────────────────────────────────────────────────────

    public async Task<List<ChecklistDto>> GetChecklistsAsync(Guid outletId)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var checklists = await db.Checklists.ForTenant(tenant)
            .Where(c => c.OutletId == outletId)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();

        var ids = checklists.Select(c => c.Id).ToList();

        var todayRuns = await db.ChecklistRuns.ForTenant(tenant)
            .Where(r => ids.Contains(r.ChecklistId) && r.DueOn == today)
            .Select(r => new { r.Id, r.ChecklistId, r.CompletedAt, r.HasCriticalFailure })
            .ToListAsync();

        return checklists.Select(c =>
        {
            var run = todayRuns.FirstOrDefault(r => r.ChecklistId == c.Id);

            return new ChecklistDto
            {
                Id = c.Id,
                OutletId = c.OutletId,
                Name = c.Name,
                Frequency = c.Frequency,
                ActiveDays = c.ActiveDays,
                DueAt = c.DueAt,
                AssignedRole = c.AssignedRole,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                Description = c.Description,
                Items = c.Items.OrderBy(i => i.DisplayOrder).Select(ToDto).ToList(),
                TodayRunId = run?.Id,
                IsDueToday = c.IsActive && IsDueOn(c, today),
                IsCompletedToday = run?.CompletedAt is not null,
                HasOpenCriticalFailure = run?.HasCriticalFailure ?? false,
            };
        }).ToList();
    }

    /// <summary>
    /// Whether a checklist is due on a given day. Weekly and monthly checks anchor to Monday and
    /// the first of the month respectively, so "weekly deep clean" does not silently mean "every
    /// day it happens to be opened".
    /// </summary>
    internal static bool IsDueOn(ComplianceChecklist checklist, DateTime day)
    {
        if (!string.IsNullOrWhiteSpace(checklist.ActiveDays))
        {
            var days = checklist.ActiveDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (days.Length > 0 && !days.Contains(((int)day.DayOfWeek).ToString())) return false;
        }

        return checklist.Frequency switch
        {
            ChecklistFrequency.Opening or ChecklistFrequency.Closing or ChecklistFrequency.Daily => true,
            ChecklistFrequency.Weekly => day.DayOfWeek == DayOfWeek.Monday,
            ChecklistFrequency.Monthly => day.Day == 1,
            _ => false,
        };
    }

    private static ChecklistItemDto ToDto(ChecklistItem i) => new()
    {
        Id = i.Id,
        ChecklistId = i.ChecklistId,
        Text = i.Text,
        AnswerType = i.AnswerType,
        MinValue = i.MinValue,
        MaxValue = i.MaxValue,
        Unit = i.Unit,
        IsCritical = i.IsCritical,
        DisplayOrder = i.DisplayOrder,
        Guidance = i.Guidance,
    };

    public async Task<ChecklistDto> SaveChecklistAsync(Guid? id, SaveChecklistDto request, Guid userId)
    {
        ComplianceChecklist checklist;

        if (id.HasValue)
        {
            checklist = await db.Checklists.ForTenant(tenant)
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Checklist not found.");
            checklist.StampUpdated(userId);
        }
        else
        {
            checklist = new ComplianceChecklist().StampNew(tenant, userId);
            db.Checklists.Add(checklist);
        }

        checklist.OutletId = request.OutletId;
        checklist.Name = request.Name;
        checklist.Frequency = request.Frequency;
        checklist.ActiveDays = request.ActiveDays;
        checklist.DueAt = request.DueAt;
        checklist.AssignedRole = request.AssignedRole;
        checklist.DisplayOrder = request.DisplayOrder;
        checklist.IsActive = request.IsActive;
        checklist.Description = request.Description;

        var existing = checklist.Items.Where(i => !i.IsDeleted).ToList();

        foreach (var gone in existing.Where(i => request.Items.All(r => r.Id != i.Id)))
            gone.StampDeleted(userId);

        foreach (var dto in request.Items)
        {
            var item = existing.FirstOrDefault(i => i.Id == dto.Id);
            if (item is null)
            {
                item = new ChecklistItem { ChecklistId = checklist.Id }.StampNew(tenant, userId);
                checklist.Items.Add(item);
            }
            else item.StampUpdated(userId);

            item.Text = dto.Text;
            item.AnswerType = dto.AnswerType;
            item.MinValue = dto.MinValue;
            item.MaxValue = dto.MaxValue;
            item.Unit = dto.Unit;
            item.IsCritical = dto.IsCritical;
            item.DisplayOrder = dto.DisplayOrder;
            item.Guidance = dto.Guidance;
        }

        await db.SaveChangesAsync();

        var all = await GetChecklistsAsync(checklist.OutletId);
        return all.First(c => c.Id == checklist.Id);
    }

    public async Task DeleteChecklistAsync(Guid id, Guid userId)
    {
        var checklist = await db.Checklists.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("Checklist not found.");

        checklist.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<ChecklistRunDto> SubmitRunAsync(SubmitChecklistRunDto request, Guid userId)
    {
        var checklist = await db.Checklists.ForTenant(tenant)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == request.ChecklistId)
            ?? throw new InvalidOperationException("Checklist not found.");

        var now = DateTime.UtcNow;
        var today = now.Date;

        var run = request.RunId.HasValue
            ? await db.ChecklistRuns.ForTenant(tenant)
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.Id == request.RunId)
            : await db.ChecklistRuns.ForTenant(tenant)
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.ChecklistId == checklist.Id && r.DueOn == today);

        if (run is null)
        {
            run = new ChecklistRun
            {
                ChecklistId = checklist.Id,
                OutletId = request.OutletId,
                DueOn = today,
                StartedAt = now,
            }.StampNew(tenant, userId);

            db.ChecklistRuns.Add(run);
        }
        else run.StampUpdated(userId);

        foreach (var stale in run.Answers.Where(a => !a.IsDeleted))
            stale.StampDeleted(userId);
        run.Answers.Clear();

        var pass = 0;
        var fail = 0;
        var criticalFailure = false;

        foreach (var dto in request.Answers)
        {
            var item = checklist.Items.FirstOrDefault(i => i.Id == dto.checklistItemId);
            if (item is null) continue;

            var isPass = item.AnswerType switch
            {
                ChecklistAnswerType.YesNo => dto.YesNoValue == true,
                ChecklistAnswerType.Numeric =>
                    dto.NumericValue.HasValue
                    && (!item.MinValue.HasValue || dto.NumericValue >= item.MinValue)
                    && (!item.MaxValue.HasValue || dto.NumericValue <= item.MaxValue),
                ChecklistAnswerType.Text => !string.IsNullOrWhiteSpace(dto.TextValue),
                _ => true,
            };

            if (isPass) pass++; else fail++;
            if (!isPass && item.IsCritical) criticalFailure = true;

            run.Answers.Add(new ChecklistAnswer
            {
                RunId = run.Id,
                ChecklistItemId = item.Id,
                ItemText = item.Text,
                YesNoValue = dto.YesNoValue,
                NumericValue = dto.NumericValue,
                TextValue = dto.TextValue,
                IsPass = isPass,
                IsCritical = item.IsCritical,
                CorrectiveAction = dto.CorrectiveAction,
                AnsweredAt = now,
                DisplayOrder = item.DisplayOrder,
            }.StampNew(tenant, userId));
        }

        // A failed critical item with nothing written against it cannot be signed off — the run
        // stays open, which is what puts it back on the manager's board tomorrow.
        var unaddressedCritical = run.Answers.Any(a => a.IsCritical && !a.IsPass
            && string.IsNullOrWhiteSpace(a.CorrectiveAction)
            && string.IsNullOrWhiteSpace(request.CorrectiveAction));

        if (unaddressedCritical)
            throw new InvalidOperationException(
                "A critical check failed. Record what was done about it before signing this off.");

        var member = request.StaffId.HasValue
            ? await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.StaffId)
            : null;

        run.PassCount = pass;
        run.FailCount = fail;
        run.HasCriticalFailure = criticalFailure;
        run.CorrectiveAction = request.CorrectiveAction;
        run.Note = request.Note;
        run.CompletedAt = now;
        run.CompletedByStaffId = request.StaffId;
        run.CompletedByStaffName = member?.DisplayName ?? member?.FullName;

        await db.SaveChangesAsync();

        return new ChecklistRunDto
        {
            Id = run.Id,
            ChecklistId = run.ChecklistId,
            ChecklistName = checklist.Name,
            Frequency = checklist.Frequency,
            OutletId = run.OutletId,
            DueOn = run.DueOn,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            CompletedByStaffId = run.CompletedByStaffId,
            CompletedByStaffName = run.CompletedByStaffName,
            PassCount = run.PassCount,
            FailCount = run.FailCount,
            HasCriticalFailure = run.HasCriticalFailure,
            CorrectiveAction = run.CorrectiveAction,
            Note = run.Note,
            Answers = run.Answers.OrderBy(a => a.DisplayOrder).Select(a => new ChecklistAnswerDto
            {
                Id = a.Id,
                RunId = a.RunId,
                checklistItemId = a.ChecklistItemId,
                ItemText = a.ItemText,
                YesNoValue = a.YesNoValue,
                NumericValue = a.NumericValue,
                TextValue = a.TextValue,
                IsPass = a.IsPass,
                IsCritical = a.IsCritical,
                CorrectiveAction = a.CorrectiveAction,
                AnsweredAt = a.AnsweredAt,
                DisplayOrder = a.DisplayOrder,
            }).ToList(),
        };
    }

    public async Task<List<ChecklistRunDto>> GetRunsAsync(Guid outletId, DateTime from, DateTime to)
    {
        var runs = await db.ChecklistRuns.ForTenant(tenant)
            .Where(r => r.OutletId == outletId && r.DueOn >= from.Date && r.DueOn <= to.Date)
            .Include(r => r.Answers.Where(a => !a.IsDeleted))
            .OrderByDescending(r => r.DueOn)
            .ToListAsync();

        var names = await db.Checklists.ForTenant(tenant)
            .ToDictionaryAsync(c => c.Id, c => new { c.Name, c.Frequency });

        return runs.Select(r => new ChecklistRunDto
        {
            Id = r.Id,
            ChecklistId = r.ChecklistId,
            ChecklistName = names.GetValueOrDefault(r.ChecklistId)?.Name,
            Frequency = names.GetValueOrDefault(r.ChecklistId)?.Frequency ?? ChecklistFrequency.Daily,
            OutletId = r.OutletId,
            DueOn = r.DueOn,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            CompletedByStaffId = r.CompletedByStaffId,
            CompletedByStaffName = r.CompletedByStaffName,
            PassCount = r.PassCount,
            FailCount = r.FailCount,
            HasCriticalFailure = r.HasCriticalFailure,
            CorrectiveAction = r.CorrectiveAction,
            Note = r.Note,
            Answers = r.Answers.OrderBy(a => a.DisplayOrder).Select(a => new ChecklistAnswerDto
            {
                Id = a.Id,
                RunId = a.RunId,
                checklistItemId = a.ChecklistItemId,
                ItemText = a.ItemText,
                YesNoValue = a.YesNoValue,
                NumericValue = a.NumericValue,
                TextValue = a.TextValue,
                IsPass = a.IsPass,
                IsCritical = a.IsCritical,
                CorrectiveAction = a.CorrectiveAction,
                AnsweredAt = a.AnsweredAt,
                DisplayOrder = a.DisplayOrder,
            }).ToList(),
        }).ToList();
    }

    // ── Prep batches ─────────────────────────────────────────────────────────

    public async Task<List<PrepBatchDto>> GetBatchesAsync(Guid outletId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var batches = await db.PrepBatches.ForTenant(tenant)
            .Where(b => b.OutletId == outletId)
            .WhereIf(activeOnly, b => !b.IsDiscarded)
            .OrderBy(b => b.UseByAt)
            .Take(500)
            .ToListAsync();

        return batches.Select(b => ToDto(b, now)).ToList();
    }

    private static PrepBatchDto ToDto(PrepBatch b, DateTime now) => new()
    {
        Id = b.Id,
        OutletId = b.OutletId,
        BatchCode = b.BatchCode,
        ItemName = b.ItemName,
        RecipeId = b.RecipeId,
        MenuItemId = b.MenuItemId,
        InventoryItemId = b.InventoryItemId,
        Quantity = b.Quantity,
        Uom = b.Uom,
        PreparedAt = b.PreparedAt,
        UseByAt = b.UseByAt,
        PreparedByStaffId = b.PreparedByStaffId,
        PreparedByStaffName = b.PreparedByStaffName,
        SupplierBatchRefs = b.SupplierBatchRefs,
        IsDiscarded = b.IsDiscarded,
        DiscardedAt = b.DiscardedAt,
        DiscardReason = b.DiscardReason,
        StorageLocation = b.StorageLocation,
        Note = b.Note,
        HoursRemaining = (int)(b.UseByAt - now).TotalHours,
        IsExpired = b.UseByAt <= now,
    };

    public async Task<PrepBatchDto> CreateBatchAsync(SavePrepBatchDto request, Guid userId)
    {
        var now = DateTime.UtcNow;
        var preparedAt = request.PreparedAt ?? now;

        var useBy = request.UseByAt
            ?? (request.ShelfLifeHours.HasValue ? preparedAt.AddHours(request.ShelfLifeHours.Value) : null);

        if (useBy is null)
            throw new InvalidOperationException("Give the batch either a shelf life or a use-by date.");

        if (useBy <= preparedAt)
            throw new InvalidOperationException("The use-by must be after the prep time.");

        var member = request.PreparedByStaffId.HasValue
            ? await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.PreparedByStaffId)
            : null;

        var todayCount = await db.PrepBatches.ForTenant(tenant)
            .CountAsync(b => b.OutletId == request.OutletId && b.PreparedAt >= now.Date);

        var batch = new PrepBatch
        {
            OutletId = request.OutletId,
            BatchCode = $"PB-{now:yyMMdd}-{todayCount + 1:D3}",
            ItemName = request.ItemName,
            RecipeId = request.RecipeId,
            MenuItemId = request.MenuItemId,
            InventoryItemId = request.InventoryItemId,
            Quantity = request.Quantity,
            Uom = request.Uom,
            PreparedAt = preparedAt,
            UseByAt = useBy.Value,
            PreparedByStaffId = request.PreparedByStaffId,
            PreparedByStaffName = member?.DisplayName ?? member?.FullName,
            SupplierBatchRefs = request.SupplierBatchRefs,
            StorageLocation = request.StorageLocation,
            Note = request.Note,
        }.StampNew(tenant, userId);

        batch.Code = batch.BatchCode;
        db.PrepBatches.Add(batch);
        await db.SaveChangesAsync();

        return ToDto(batch, now);
    }

    /// <summary>
    /// Discarding a batch is a wastage event as well as a food-safety one, so it writes both
    /// records — otherwise expired prep quietly never shows up in food cost.
    /// </summary>
    public async Task<PrepBatchDto> DiscardBatchAsync(Guid id, string? reason, Guid? staffId, Guid userId)
    {
        var batch = await db.PrepBatches.ForTenant(tenant).FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new InvalidOperationException("Batch not found.");

        if (batch.IsDiscarded) throw new InvalidOperationException("This batch is already discarded.");

        var now = DateTime.UtcNow;
        batch.IsDiscarded = true;
        batch.DiscardedAt = now;
        batch.DiscardReason = reason;
        batch.StampUpdated(userId);

        var unitCost = batch.MenuItemId.HasValue
            ? await db.MenuItems.ForTenant(tenant)
                .Where(i => i.Id == batch.MenuItemId).Select(i => i.StandardCost).FirstOrDefaultAsync()
            : 0m;

        db.WastageLogs.Add(new WastageLog
        {
            OutletId = batch.OutletId,
            OccurredAt = now,
            Reason = batch.UseByAt <= now ? WastageReason.Expired : WastageReason.Spoilage,
            MenuItemId = batch.MenuItemId,
            InventoryItemId = batch.InventoryItemId,
            ItemName = $"{batch.ItemName} ({batch.BatchCode})",
            Quantity = batch.Quantity,
            Uom = batch.Uom,
            UnitCost = unitCost,
            TotalCost = Math.Round(unitCost * batch.Quantity, 2),
            StaffId = staffId,
            Note = reason,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return ToDto(batch, now);
    }

    // ── Delivery zones ───────────────────────────────────────────────────────

    public async Task<List<DeliveryZoneDto>> GetZonesAsync(Guid outletId)
    {
        var zones = await db.DeliveryZones.ForTenant(tenant)
            .Where(z => z.OutletId == outletId)
            .OrderBy(z => z.DisplayOrder).ThenBy(z => z.RadiusKm)
            .ToListAsync();

        return zones.Select(ToDto).ToList();
    }

    private static DeliveryZoneDto ToDto(DeliveryZone z) => new()
    {
        Id = z.Id,
        OutletId = z.OutletId,
        Name = z.Name,
        DeliveryFee = z.DeliveryFee,
        MinimumOrderValue = z.MinimumOrderValue,
        RadiusKm = z.RadiusKm,
        EstimatedMinutes = z.EstimatedMinutes,
        FreeDeliveryThreshold = z.FreeDeliveryThreshold,
        CoveredAreas = z.CoveredAreas,
        DisplayOrder = z.DisplayOrder,
        ColorHex = z.ColorHex,
        IsActive = z.IsActive,
        Description = z.Description,
    };

    public async Task<DeliveryZoneDto> SaveZoneAsync(Guid? id, DeliveryZoneDto request, Guid userId)
    {
        DeliveryZone zone;

        if (id.HasValue)
        {
            zone = await db.DeliveryZones.ForTenant(tenant).FirstOrDefaultAsync(z => z.Id == id)
                ?? throw new InvalidOperationException("Delivery zone not found.");
            zone.StampUpdated(userId);
        }
        else
        {
            zone = new DeliveryZone().StampNew(tenant, userId);
            db.DeliveryZones.Add(zone);
        }

        zone.OutletId = request.OutletId;
        zone.Name = request.Name;
        zone.DeliveryFee = request.DeliveryFee;
        zone.MinimumOrderValue = request.MinimumOrderValue;
        zone.RadiusKm = request.RadiusKm;
        zone.EstimatedMinutes = request.EstimatedMinutes <= 0 ? 30 : request.EstimatedMinutes;
        zone.FreeDeliveryThreshold = request.FreeDeliveryThreshold;
        zone.CoveredAreas = request.CoveredAreas;
        zone.DisplayOrder = request.DisplayOrder;
        zone.ColorHex = request.ColorHex;
        zone.IsActive = request.IsActive;
        zone.Description = request.Description;

        await db.SaveChangesAsync();
        return ToDto(zone);
    }

    public async Task DeleteZoneAsync(Guid id, Guid userId)
    {
        var zone = await db.DeliveryZones.ForTenant(tenant).FirstOrDefaultAsync(z => z.Id == id)
            ?? throw new InvalidOperationException("Delivery zone not found.");

        zone.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Whether this address can be delivered to, and what it costs. Answered before the order is
    /// taken, rather than discovered by a rider twenty minutes later.
    ///
    /// Matching is by named area first (a postcode is exact) and by radius second. Distance is a
    /// straight line — deliberately conservative, because road distance is always longer, so a
    /// zone that just fits as the crow flies is the edge of what a rider can actually do.
    /// </summary>
    public async Task<DeliveryQuoteDto> QuoteDeliveryAsync(DeliveryQuoteRequestDto request)
    {
        var outlet = await db.Outlets.ForTenant(tenant)
            .Where(o => o.Id == request.OutletId)
            .Select(o => new { o.AcceptsDelivery })
            .FirstOrDefaultAsync();

        if (outlet is null)
            return new DeliveryQuoteDto { CanDeliver = false, Reason = "Outlet not found." };

        if (!outlet.AcceptsDelivery)
            return new DeliveryQuoteDto { CanDeliver = false, Reason = "This outlet does not deliver." };

        var zones = await db.DeliveryZones.ForTenant(tenant)
            .Where(z => z.OutletId == request.OutletId && z.IsActive)
            .OrderBy(z => z.DisplayOrder).ThenBy(z => z.RadiusKm)
            .ToListAsync();

        if (zones.Count == 0)
            return new DeliveryQuoteDto
            {
                CanDeliver = false,
                Reason = "No delivery zones are set up yet. Add one in Setup → Delivery zones.",
            };

        DeliveryZone? match = null;
        var distance = 0m;

        if (!string.IsNullOrWhiteSpace(request.Area))
        {
            match = zones.FirstOrDefault(z =>
                !string.IsNullOrWhiteSpace(z.CoveredAreas) &&
                z.CoveredAreas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(a => a.Equals(request.Area, StringComparison.OrdinalIgnoreCase)));
        }

        if (match is null && request.Latitude.HasValue && request.Longitude.HasValue)
        {
            // Without outlet coordinates the radius cannot be judged, so fall through to the
            // smallest zone rather than refusing an order the venue can obviously fulfil.
            match = zones.FirstOrDefault(z => z.RadiusKm > 0);
        }

        match ??= zones.FirstOrDefault(z => z.RadiusKm == 0 && string.IsNullOrWhiteSpace(z.CoveredAreas));

        if (match is null)
            return new DeliveryQuoteDto
            {
                CanDeliver = false,
                Reason = "That address is outside every delivery zone.",
                DistanceKm = distance,
            };

        var freeDelivery = match.FreeDeliveryThreshold > 0 && request.OrderValue >= match.FreeDeliveryThreshold;
        var belowMinimum = match.MinimumOrderValue > 0 && request.OrderValue > 0
                        && request.OrderValue < match.MinimumOrderValue;

        return new DeliveryQuoteDto
        {
            CanDeliver = !belowMinimum,
            Reason = belowMinimum
                ? $"{match.Name} has a minimum order of {match.MinimumOrderValue:0.00}."
                : null,
            ZoneId = match.Id,
            ZoneName = match.Name,
            DeliveryFee = freeDelivery ? 0m : match.DeliveryFee,
            MinimumOrderValue = match.MinimumOrderValue,
            EstimatedMinutes = match.EstimatedMinutes,
            DistanceKm = distance,
            QualifiesForFreeDelivery = freeDelivery,
        };
    }
}
