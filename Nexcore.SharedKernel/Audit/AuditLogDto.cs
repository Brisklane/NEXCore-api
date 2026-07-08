namespace Nexcore.SharedKernel.Audit;

/// <summary>Read model for a single audit-log entry, returned by audit API queries.</summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;

    /// <summary>Serialized entity state before the change (JSON), when captured.</summary>
    public string? OldValues { get; set; }

    /// <summary>Serialized entity state after the change (JSON), when captured.</summary>
    public string? NewValues { get; set; }

    public string? Description { get; set; }

    // Request context captured at the time of the change.
    public string? IpAddress { get; set; }
    public string? HttpMethod { get; set; }
    public string? Endpoint { get; set; }
    public int? HttpStatusCode { get; set; }

    public DateTime ChangedAt { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string? ChangedByUsername { get; set; }

    /// <summary>Trace/correlation id linking this entry to a request or workflow.</summary>
    public string? CorrelationId { get; set; }

    public string? Category { get; set; }
    public AuditSeverity Severity { get; set; }
}

/// <summary>Filter + paging/sorting criteria for querying audit logs. All filters are optional (AND-combined).</summary>
public class AuditLogFilterRequest
{
    public string? ModuleName { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? Action { get; set; }

    /// <summary>Inclusive UTC lower bound on <see cref="AuditLogDto.ChangedAt"/>.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Inclusive UTC upper bound on <see cref="AuditLogDto.ChangedAt"/>.</summary>
    public DateTime? EndDate { get; set; }

    public string? Category { get; set; }
    public AuditSeverity? Severity { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Sortable field name, e.g. "ChangedAt" or "EntityType".</summary>
    public string? SortBy { get; set; } = "ChangedAt";

    /// <summary>"Asc" or "Desc".</summary>
    public string SortDirection { get; set; } = "Desc";
}

/// <summary>One page of audit-log results plus the paging metadata needed to navigate the rest.</summary>
public class AuditLogPagedResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public List<AuditLogDto> Items { get; set; } = new();

    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
