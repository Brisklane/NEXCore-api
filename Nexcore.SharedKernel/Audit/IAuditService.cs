namespace Nexcore.SharedKernel.Audit;

/// <summary>
/// Writes and queries the shared audit trail. Every module resolves this to record entity
/// changes and security events, and the audit screens read them back through the paged queries.
/// The <c>Log*Async</c> convenience methods are thin wrappers over <see cref="LogAsync"/>.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records one audit entry. The full-fidelity method every other <c>Log*</c> helper delegates to.
    /// </summary>
    /// <param name="moduleName">Owning module, e.g. "Auth".</param>
    /// <param name="entityType">Entity that changed, e.g. "Role".</param>
    /// <param name="entityId">Key of the affected record.</param>
    /// <param name="action">Verb performed (Create, Update, Delete, …).</param>
    /// <param name="userId">Actor who made the change.</param>
    /// <param name="description">Human-readable summary.</param>
    /// <param name="oldValues">Before-state JSON.</param>
    /// <param name="newValues">After-state JSON.</param>
    /// <param name="category">Optional grouping (Security, Financial, …).</param>
    /// <param name="severity">Risk band; defaults to Info.</param>
    /// <param name="ipAddress">Optional request context.</param>
    /// <param name="userAgent">Optional request context.</param>
    /// <param name="httpMethod">Optional request context.</param>
    /// <param name="endpoint">Optional request context.</param>
    /// <param name="httpStatusCode">Optional response status.</param>
    /// <param name="metadata">Optional extra JSON context.</param>
    Task LogAsync(
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
        string? metadata = null);

    Task LogCreateAsync(
        string moduleName,
        string entityType,
        Guid entityId,
        string description,
        Guid userId,
        string? newValues = null,
        string? category = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null);

    Task LogUpdateAsync(
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
        string? endpoint = null);

    Task LogDeleteAsync(
        string moduleName,
        string entityType,
        Guid entityId,
        string description,
        Guid userId,
        string? oldValues = null,
        string? category = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null);

    /// <summary>Convenience wrapper that stamps the entry with Critical severity.</summary>
    Task LogSecurityEventAsync(
        string moduleName,
        string entityType,
        Guid entityId,
        string action,
        string description,
        Guid userId,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null);

    Task<AuditLogPagedResponse> GetEntityAuditLogsAsync(
        Guid entityId,
        string entityType,
        int pageNumber = 1,
        int pageSize = 20);

    Task<AuditLogPagedResponse> GetUserActivityLogsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20);

    Task<AuditLogPagedResponse> GetModuleAuditLogsAsync(
        string moduleName,
        int pageNumber = 1,
        int pageSize = 20);

    Task<AuditLogPagedResponse> QueryAuditLogsAsync(AuditLogFilterRequest filter);

    /// <summary>Returns only Critical-severity entries.</summary>
    Task<AuditLogPagedResponse> GetSecurityEventsAsync(
        int pageNumber = 1,
        int pageSize = 20);

    Task<AuditLogPagedResponse> GetCompanyAuditLogsAsync(
        Guid companyId,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>Writes many entries in one round-trip.</summary>
    Task LogBulkAsync(IEnumerable<AuditLogEntry> entries);
}
