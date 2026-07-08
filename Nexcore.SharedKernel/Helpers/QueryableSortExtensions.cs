using System.Linq.Expressions;
using System.Reflection;

namespace Nexcore.SharedKernel.Helpers;

/// <summary>
/// Reflection-based ordering helpers that turn a client-supplied <c>SortBy</c>/<c>SortDirection</c>
/// into the <c>orderBy</c> delegate <c>GetPagedAsync</c> expects — so a list endpoint can honour any
/// sortable column without hand-writing a switch per property.
/// </summary>
public static class QueryableSortExtensions
{
    /// <summary>
    /// Same as <see cref="ApplyOrder{T}"/>, but when the caller hasn't picked a column it defaults to
    /// newest-first (<c>CreatedAt</c> descending) so recently added rows stay on top across refreshes.
    /// Falls back to <paramref name="defaultProperty"/> only when the entity has no <c>CreatedAt</c>;
    /// an explicit <paramref name="sortBy"/> always wins.
    /// </summary>
    public static IOrderedQueryable<T> ApplyOrderNewestFirst<T>(
        this IQueryable<T> source,
        string? sortBy,
        string? sortDirection,
        string defaultProperty)
    {
        if (string.IsNullOrWhiteSpace(sortBy) && ResolveProperty<T>("CreatedAt") != null)
            return source.ApplyOrder("CreatedAt", "desc", defaultProperty);

        return source.ApplyOrder(sortBy, sortDirection, defaultProperty);
    }

    /// <summary>
    /// Orders <paramref name="source"/> by the public scalar property named <paramref name="sortBy"/>
    /// (case-insensitive), falling back to <paramref name="defaultProperty"/> when it is blank or does
    /// not resolve — so the query always has a stable, EF-translatable ORDER BY. Direction is descending
    /// only when <paramref name="sortDirection"/> is "desc"; anything else sorts ascending.
    /// </summary>
    public static IOrderedQueryable<T> ApplyOrder<T>(
        this IQueryable<T> source,
        string? sortBy,
        string? sortDirection,
        string defaultProperty)
    {
        var property = ResolveProperty<T>(sortBy) ?? ResolveProperty<T>(defaultProperty)
            ?? throw new ArgumentException($"Default sort property '{defaultProperty}' does not exist on {typeof(T).Name}.", nameof(defaultProperty));

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var keySelector = Expression.Lambda(propertyAccess, parameter);

        var methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var orderByMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.PropertyType);

        return (IOrderedQueryable<T>)orderByMethod.Invoke(null, new object[] { source, keySelector })!;
    }

    /// <summary>Resolves a public instance property by name (case-insensitive), or null if it doesn't exist.</summary>
    private static PropertyInfo? ResolveProperty<T>(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return typeof(T).GetProperty(
            name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    }
}
