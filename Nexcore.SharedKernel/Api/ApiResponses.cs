namespace Nexcore.SharedKernel.Api;

/// <summary>Envelope for endpoints that return only a success flag and a message.</summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>Envelope for endpoints that return a single payload alongside the status.</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

/// <summary>
/// Page/navigation info attached to list endpoints. Prefer <see cref="Create"/> so the
/// derived fields (<see cref="TotalPages"/>, has-next/prev, start/end index) stay consistent.
/// </summary>
public class PaginationMetadata
{
    /// <summary>Row count across the whole result set, before paging.</summary>
    public int TotalCount { get; set; }

    /// <summary>1-based page currently returned.</summary>
    public int PageNumber { get; set; }

    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }

    /// <summary>1-based position of the first row on this page.</summary>
    public int StartIndex { get; set; }

    /// <summary>1-based position of the last row on this page.</summary>
    public int EndIndex { get; set; }

    /// <summary>Builds a fully-populated instance from the three inputs a query actually knows.</summary>
    public static PaginationMetadata Create(int totalCount, int pageNumber, int pageSize)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var startIndex = (pageNumber - 1) * pageSize + 1;
        var endIndex = Math.Min(pageNumber * pageSize, totalCount);

        return new PaginationMetadata
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1,
            StartIndex = startIndex,
            EndIndex = endIndex
        };
    }
}

/// <summary>List-endpoint envelope: the page of rows plus its <see cref="PaginationMetadata"/>.</summary>
public class PaginatedResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IEnumerable<T> Data { get; set; } = [];
    public PaginationMetadata? Pagination { get; set; }

    public static PaginatedResponse<T> Ok(IEnumerable<T> data, int totalCount, int pageNumber, int pageSize, string? message = null)
    {
        return new PaginatedResponse<T>
        {
            Success = true,
            Message = message ?? "Request completed successfully",
            Data = data,
            Pagination = PaginationMetadata.Create(totalCount, pageNumber, pageSize)
        };
    }

    public static PaginatedResponse<T> Fail(string message)
    {
        return new PaginatedResponse<T>
        {
            Success = false,
            Message = message,
            Data = [],
            Pagination = null
        };
    }

    public static PaginatedResponse<T> Empty(string? message = null)
    {
        return new PaginatedResponse<T>
        {
            Success = true,
            Message = message ?? "No records found",
            Data = [],
            Pagination = PaginationMetadata.Create(0, 1, 10)
        };
    }
}

/// <summary>
/// Query-string inputs for a paged list. The setters self-clamp — page number floors at 1,
/// page size is held to 1..100 — so a bad request degrades gracefully instead of erroring.
/// </summary>
public class PaginationParams
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > 100 ? 100 : value);
    }

    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }

    /// <summary>"asc" or "desc"; validated by <see cref="IsValid"/>.</summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>Optional status-enum filter for list dropdowns; null means every status.</summary>
    public int? Status { get; set; }

    /// <summary>Rows to skip for the current page — feed straight into <c>IQueryable.Skip</c>.</summary>
    public int CalculateSkip() => (PageNumber - 1) * PageSize;

    /// <summary>Explicit validation for callers that want to reject rather than clamp.</summary>
    public bool IsValid(out string? errorMessage)
    {
        errorMessage = null;

        if (PageNumber < 1)
        {
            errorMessage = "Page number must be at least 1";
            return false;
        }

        if (PageSize < 1 || PageSize > 100)
        {
            errorMessage = "Page size must be between 1 and 100";
            return false;
        }

        if (!string.IsNullOrEmpty(SortDirection)
            && !SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
            && !SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Sort direction must be 'asc' or 'desc'";
            return false;
        }

        return true;
    }
}

/// <summary>One clause in an advanced filter: a field, an operator, and the value to match.</summary>
public class FilterParams
{
    public string? FieldName { get; set; }

    /// <summary>equals · contains · startsWith · endsWith · greaterThan · lessThan.</summary>
    public string? Operator { get; set; }

    public string? Value { get; set; }

    /// <summary>How this clause joins the others: "and" (default) or "or".</summary>
    public string LogicalOperator { get; set; } = "and";
}

/// <summary>Paging + sorting (inherited) plus a filter set and eager-load hints.</summary>
public class QueryParams : PaginationParams
{
    public List<FilterParams> Filters { get; set; } = [];

    /// <summary>Comma-separated navigation properties to include.</summary>
    public string? IncludeFields { get; set; }
}

/// <summary>Error envelope: a headline message and the individual validation failures.</summary>
public class ApiErrorResponse
{
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];
}

/// <summary>Fluent builders for the response envelopes above.</summary>
public static class ApiResponseExtensions
{
    public static ApiResponse Success(string? message = null) => new()
    {
        Success = true,
        Message = message ?? "Request completed successfully"
    };

    public static ApiResponse Fail(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static ApiResponse<T> Success<T>(T data, string? message = null) => new()
    {
        Success = true,
        Message = message ?? "Request completed successfully",
        Data = data
    };

    public static ApiResponse<T> Fail<T>(string message) => new()
    {
        Success = false,
        Message = message,
        Data = default
    };

    public static PaginatedResponse<T> ToPaginatedResponse<T>(
        this IEnumerable<T> data,
        int totalCount,
        int pageNumber,
        int pageSize,
        string? message = null) => PaginatedResponse<T>.Ok(data, totalCount, pageNumber, pageSize, message);

    /// <summary>Overload for the common <c>(items, totalCount)</c> tuple returned by paged repositories.</summary>
    public static PaginatedResponse<T> ToPaginatedResponse<T>(
        this (IEnumerable<T> Items, int TotalCount) result,
        int pageNumber,
        int pageSize,
        string? message = null) => PaginatedResponse<T>.Ok(result.Items, result.TotalCount, pageNumber, pageSize, message);
}
