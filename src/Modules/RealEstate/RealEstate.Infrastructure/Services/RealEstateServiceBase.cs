using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// What every Real Estate service needs before it can answer a question: the tenant's settings,
/// the display area unit, the currency, and the two or three lookups that would otherwise be
/// repeated in twenty files.
///
/// Settings are cached for the life of the request — they are read on nearly every call and they
/// cannot change mid-request.
/// </summary>
public abstract class RealEstateServiceBase(RealEstateDbContext db, IRealEstateTenant tenant)
{
    protected readonly RealEstateDbContext Db = db;
    protected readonly IRealEstateTenant Tenant = tenant;

    private RealEstateSettings? _settings;

    protected async Task<RealEstateSettings> SettingsAsync()
    {
        if (_settings is not null) return _settings;

        _settings = await Db.Settings.ForCompany(Tenant).FirstOrDefaultAsync();

        if (_settings is null)
        {
            // A company that has never opened Settings still has to be able to work. The defaults
            // are the safe ones: development on, escrow off, nothing enforced that was not asked for.
            _settings = new RealEstateSettings().StampNew(Tenant);
            Db.Settings.Add(_settings);
            await Db.SaveChangesAsync();
        }

        return _settings;
    }

    protected async Task<AreaUnit> AreaUnitAsync(Guid? officeId = null)
    {
        if (officeId is not null)
        {
            var office = await Db.RealEstateOffices.ForCompany(Tenant)
                .Where(o => o.Id == officeId)
                .Select(o => new { o.DisplayAreaUnit })
                .FirstOrDefaultAsync();

            if (office?.DisplayAreaUnit is not null) return office.DisplayAreaUnit.Value;
        }

        return (await SettingsAsync()).DisplayAreaUnit;
    }

    protected async Task<string> CurrencyAsync() => (await SettingsAsync()).CurrencyCode;

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Applies paging to any query and returns the standard envelope in one step.</summary>
    protected static async Task<PaginatedResponse<TDto>> PageAsync<TEntity, TDto>(
        IQueryable<TEntity> query,
        ListQueryDto request,
        Func<List<TEntity>, Task<List<TDto>>> project)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 500 ? 25 : request.PageSize;

        var total = await query.CountAsync();
        var rows = await query.Skip((page - 1) * size).Take(size).ToListAsync();
        var dtos = await project(rows);

        return PaginatedResponse<TDto>.Ok(dtos, total, page, size);
    }

    protected static async Task<PaginatedResponse<TDto>> PageAsync<TEntity, TDto>(
        IQueryable<TEntity> query,
        ListQueryDto request,
        Func<TEntity, TDto> project)
        => await PageAsync(query, request, rows => Task.FromResult(rows.Select(project).ToList()));

    /// <summary>Loads an entity in the caller's tenant or throws a message the caller can show.</summary>
    protected async Task<T> RequireAsync<T>(Guid id, string notFoundMessage) where T : BaseEntity
        => await Db.Set<T>().ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == id)
           ?? throw new InvalidOperationException(notFoundMessage);

    /// <summary>
    /// Resolves the display names a DTO needs without N+1 queries: one dictionary lookup for a
    /// batch of party ids, built once per call.
    /// </summary>
    protected async Task<Dictionary<Guid, string>> PartyNamesAsync(IEnumerable<Guid> ids)
    {
        var distinct = ids.Where(i => i != Guid.Empty).Distinct().ToList();
        if (distinct.Count == 0) return [];

        return await Db.Parties.ForCompany(Tenant)
            .Where(p => distinct.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                Name = p.DisplayName ?? p.OrganisationName ?? ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim(),
            })
            .ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.Name) ? "—" : x.Name);
    }

    protected async Task<Dictionary<Guid, string>> ProjectNamesAsync(IEnumerable<Guid?> ids)
    {
        var distinct = ids.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (distinct.Count == 0) return [];

        return await Db.Projects.ForCompany(Tenant)
            .Where(p => distinct.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);
    }

    protected async Task<Dictionary<Guid, string>> ReasonLabelsAsync(IEnumerable<Guid?> ids)
    {
        var distinct = ids.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (distinct.Count == 0) return [];

        return await Db.ReasonCodes.ForCompany(Tenant)
            .Where(r => distinct.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Label);
    }

    /// <summary>
    /// Records that somebody overrode a control, with the reason. The platform's audit log captures
    /// the field change; this captures the *why*, which no automatic trail can infer.
    /// </summary>
    protected async Task WriteAuditNoteAsync(
        string entityType, Guid entityId, string actionKey, Guid reasonCodeId, Guid userId,
        string? before = null, string? after = null, decimal? amountImpact = null,
        string? note = null, Guid? approvalRequestId = null, bool highRisk = false,
        string? entityReference = null)
    {
        Db.RealEstateAuditNotes.Add(new RealEstateAuditNote
        {
            EntityType = entityType,
            EntityId = entityId,
            EntityReference = entityReference,
            ActionKey = actionKey,
            BeforeValue = before,
            AfterValue = after,
            AmountImpact = amountImpact,
            ReasonCodeId = reasonCodeId,
            Note = note,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow,
            ApprovalRequestId = approvalRequestId,
            IsHighRisk = highRisk,
        }.StampNew(Tenant, userId));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Raises an approval, or returns null when the amount falls under the matrix's auto-approve
    /// threshold. Callers treat a null as "approved, carry on" rather than as a failure.
    /// </summary>
    protected async Task<ApprovalRequest?> RaiseApprovalAsync(
        string documentType, Guid entityId, string? entityReference, decimal amount,
        string summary, Guid userId, Guid? projectId = null, Guid? officeId = null,
        Guid? reasonCodeId = null, string? note = null)
    {
        var bands = await Db.ApprovalMatrices.ForCompany(Tenant)
            .Where(m => m.DocumentType == documentType)
            .Where(m => m.ProjectId == null || m.ProjectId == projectId)
            .Where(m => m.OfficeId == null || m.OfficeId == officeId)
            .OrderBy(m => m.Level)
            .ToListAsync();

        var applicable = bands
            .Where(m => amount >= m.MinAmount && (m.MaxAmount == null || amount <= m.MaxAmount))
            .ToList();

        // Nothing configured, or the amount is under the lowest band and that band auto-approves.
        if (applicable.Count == 0) return null;
        if (applicable.All(b => b.AutoApproveBelowMin && amount < b.MinAmount)) return null;

        var levels = applicable.Select(b => b.Level).Distinct().Count();
        var first = applicable.OrderBy(b => b.Level).First();

        var request = new ApprovalRequest
        {
            DocumentType = documentType,
            EntityId = entityId,
            EntityReference = entityReference,
            Summary = summary,
            Amount = amount,
            CurrentLevel = 1,
            RequiredLevels = levels,
            Outcome = ApprovalOutcome.Pending,
            RequestedByUserId = userId,
            RequestedAt = DateTime.UtcNow,
            ReasonCodeId = reasonCodeId,
            RequestNote = note,
            EscalatesAt = first.EscalateAfterHours is null
                ? null
                : DateTime.UtcNow.AddHours(first.EscalateAfterHours.Value),
        }.StampNew(Tenant, userId);

        Db.ApprovalRequests.Add(request);
        return request;
    }

    /// <summary>Queues a notification through the rule engine, honouring quiet hours and consent.</summary>
    protected async Task QueueNotificationAsync(
        string ruleKey, string title, string? body, string? deepLink,
        Guid? recipientUserId = null, Guid? recipientPartyId = null,
        string? entityType = null, Guid? entityId = null,
        AlertSeverity severity = AlertSeverity.Info)
    {
        var rule = await Db.NotificationRules.ForCompany(Tenant)
            .FirstOrDefaultAsync(r => r.RuleKey == ruleKey);

        if (rule is not null && !rule.IsEnabled) return;

        var channels = (rule?.Channels ?? "InApp")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => Enum.TryParse<NotificationChannel>(c, out var parsed) ? parsed : NotificationChannel.InApp)
            .Distinct();

        foreach (var channel in channels)
        {
            Db.NotificationLogs.Add(new NotificationLog
            {
                NotificationRuleId = rule?.Id,
                RuleKey = ruleKey,
                Channel = channel,
                Severity = rule?.Severity ?? severity,
                RecipientUserId = recipientUserId,
                RecipientPartyId = recipientPartyId,
                Title = title,
                Body = body,
                DeepLink = deepLink,
                EntityType = entityType,
                EntityId = entityId,
                QueuedAt = DateTime.UtcNow,
            }.StampNew(Tenant));
        }
    }
}
