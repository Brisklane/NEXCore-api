using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Nexcore.SharedKernel.Audit;

/// <summary>
/// Generic implementation of audit service
/// Can be used directly or extended by module-specific services
/// </summary>
public class AuditService : IAuditService
{
    private readonly DbContext _context;
    private readonly DbSet<AuditLogEntry> _auditLogs;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">DbContext that inherits from AuditDbContextBase</param>
    public AuditService(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        
        // Get the AuditLogs DbSet dynamically
        _auditLogs = _context.Set<AuditLogEntry>();
        if (_auditLogs == null)
            throw new InvalidOperationException("DbContext must have DbSet<AuditLogEntry> AuditLogs property");
    }

    public async Task LogAsync(
        string moduleName,
        string entityType,
        Guid entityId,
        string action,
        Guid userId,
        string description,
        string? oldValues = null,
        string? newValues = null,
        string? category = null,
        AuditSeverity severity = AuditSeverity.Info,
        string? ipAddress = null,
        string? userAgent = null,
        string? httpMethod = null,
        string? endpoint = null,
        int? httpStatusCode = null,
        string? metadata = null)
    {
        var auditLog = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ModuleName = moduleName,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            Category = category,
            Severity = severity,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            HttpMethod = httpMethod,
            Endpoint = endpoint,
            HttpStatusCode = httpStatusCode,
            Metadata = metadata,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
            CompanyId = Guid.Empty,
            BranchId = Guid.Empty,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _auditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task LogCreateAsync(
        string moduleName,
        string entityType,
        Guid entityId,
        string description,
        Guid userId,
        string? newValues = null,
        string? category = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null)
    {
        await LogAsync(
            moduleName: moduleName,
            entityType: entityType,
            entityId: entityId,
            action: "Create",
            userId: userId,
            description: description,
            newValues: newValues,
            category: category,
            severity: AuditSeverity.Info,
            ipAddress: ipAddress,
            userAgent: userAgent,
            endpoint: endpoint);
    }

    public async Task LogUpdateAsync(
        string moduleName,
        string entityType,
        Guid entityId,
        string description,
        Guid userId,
        string? oldValues = null,
        string? newValues = null,
        string? category = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null)
    {
        await LogAsync(
            moduleName: moduleName,
            entityType: entityType,
            entityId: entityId,
            action: "Update",
            userId: userId,
            description: description,
            oldValues: oldValues,
            newValues: newValues,
            category: category,
            severity: AuditSeverity.Info,
            ipAddress: ipAddress,
            userAgent: userAgent,
            endpoint: endpoint);
    }

    public async Task LogDeleteAsync(
        string moduleName,
        string entityType,
        Guid entityId,
        string description,
        Guid userId,
        string? oldValues = null,
        string? category = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null)
    {
        await LogAsync(
            moduleName: moduleName,
            entityType: entityType,
            entityId: entityId,
            action: "Delete",
            userId: userId,
            description: description,
            oldValues: oldValues,
            category: category,
            severity: AuditSeverity.Warning,
            ipAddress: ipAddress,
            userAgent: userAgent,
            endpoint: endpoint);
    }

    public async Task LogSecurityEventAsync(
        string moduleName,
        string entityType,
        Guid entityId,
        string action,
        string description,
        Guid userId,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null)
    {
        await LogAsync(
            moduleName: moduleName,
            entityType: entityType,
            entityId: entityId,
            action: action,
            userId: userId,
            description: description,
            category: "Security",
            severity: AuditSeverity.Critical,
            metadata: metadata,
            ipAddress: ipAddress,
            userAgent: userAgent,
            endpoint: endpoint);
    }

    public async Task<AuditLogPagedResponse> GetEntityAuditLogsAsync(
        Guid entityId,
        string entityType,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _auditLogs
            .Where(a => a.EntityId == entityId && a.EntityType == entityType && !a.IsDeleted)
            .OrderByDescending(a => a.ChangedAt);

        return await GetPagedResultAsync(query, pageNumber, pageSize);
    }

    public async Task<AuditLogPagedResponse> GetUserActivityLogsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _auditLogs
            .Where(a => a.ChangedByUserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.ChangedAt);

        return await GetPagedResultAsync(query, pageNumber, pageSize);
    }

    public async Task<AuditLogPagedResponse> GetModuleAuditLogsAsync(
        string moduleName,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _auditLogs
            .Where(a => a.ModuleName == moduleName && !a.IsDeleted)
            .OrderByDescending(a => a.ChangedAt);

        return await GetPagedResultAsync(query, pageNumber, pageSize);
    }

    public async Task<AuditLogPagedResponse> QueryAuditLogsAsync(AuditLogFilterRequest filter)
    {
        var query = _auditLogs.Where(a => !a.IsDeleted);

        // Apply filters
        if (!string.IsNullOrEmpty(filter.ModuleName))
            query = query.Where(a => a.ModuleName == filter.ModuleName);

        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (filter.EntityId.HasValue && filter.EntityId != Guid.Empty)
            query = query.Where(a => a.EntityId == filter.EntityId);

        if (filter.UserId.HasValue && filter.UserId != Guid.Empty)
            query = query.Where(a => a.ChangedByUserId == filter.UserId);

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(a => a.Action == filter.Action);

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.ChangedAt >= filter.StartDate);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.ChangedAt <= filter.EndDate);

        if (!string.IsNullOrEmpty(filter.Category))
            query = query.Where(a => a.Category == filter.Category);

        if (filter.Severity.HasValue)
            query = query.Where(a => a.Severity == filter.Severity);

        // Apply sorting
        query = filter.SortDirection?.ToLower() == "asc"
            ? query.OrderBy(a => GetSortProperty(a, filter.SortBy))
            : query.OrderByDescending(a => GetSortProperty(a, filter.SortBy));

        return await GetPagedResultAsync(query, filter.PageNumber, filter.PageSize);
    }

    public async Task<AuditLogPagedResponse> GetSecurityEventsAsync(
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _auditLogs
            .Where(a => a.Severity == AuditSeverity.Critical && !a.IsDeleted)
            .OrderByDescending(a => a.ChangedAt);

        return await GetPagedResultAsync(query, pageNumber, pageSize);
    }

    public async Task<AuditLogPagedResponse> GetCompanyAuditLogsAsync(
        Guid companyId,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _auditLogs
            .Where(a => a.CompanyId == companyId && !a.IsDeleted)
            .OrderByDescending(a => a.ChangedAt);

        return await GetPagedResultAsync(query, pageNumber, pageSize);
    }

    public async Task LogBulkAsync(IEnumerable<AuditLogEntry> entries)
    {
        if (entries == null || !entries.Any())
            return;

        _auditLogs.AddRange(entries);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to get paginated results
    /// </summary>
    private async Task<AuditLogPagedResponse> GetPagedResultAsync(
        IQueryable<AuditLogEntry> query,
        int pageNumber,
        int pageSize)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return new AuditLogPagedResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            TotalPages = totalPages,
            Items = items
        };
    }

    /// <summary>
    /// Helper method to map AuditLogEntry to DTO
    /// </summary>
    private static AuditLogDto MapToDto(AuditLogEntry entry)
    {
        return new AuditLogDto
        {
            Id = entry.Id,
            ModuleName = entry.ModuleName,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Action = entry.Action,
            OldValues = entry.OldValues,
            NewValues = entry.NewValues,
            Description = entry.Description,
            IpAddress = entry.IpAddress,
            HttpMethod = entry.HttpMethod,
            Endpoint = entry.Endpoint,
            HttpStatusCode = entry.HttpStatusCode,
            ChangedAt = entry.ChangedAt,
            ChangedByUserId = entry.ChangedByUserId,
            ChangedByUsername = entry.ChangedByUsername,
            CorrelationId = entry.CorrelationId,
            Category = entry.Category,
            Severity = entry.Severity
        };
    }

    /// <summary>
    /// Helper method for dynamic sorting
    /// </summary>
    private static object GetSortProperty(AuditLogEntry entry, string? sortBy)
    {
        return sortBy?.ToLower() switch
        {
            "entitytype" => entry.EntityType,
            "action" => entry.Action,
            "modulename" => entry.ModuleName,
            "category" => entry.Category ?? string.Empty,
            "severity" => entry.Severity,
            _ => entry.ChangedAt
        };
    }
}
