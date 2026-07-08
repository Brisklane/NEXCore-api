using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexcore.SharedKernel.Audit;

/// <summary>
/// Stateless helpers used while building audit-log entries — JSON snapshotting of before/after
/// state, human-readable change summaries, sensitive-field redaction, and category/severity
/// classification.
/// </summary>
public static class AuditUtilities
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>Serializes state for storage; falls back to <c>ToString()</c> if the object can't be serialized.</summary>
    public static string SerializeObject(object? obj)
    {
        if (obj == null)
            return null!;

        try
        {
            return JsonSerializer.Serialize(obj, JsonOptions);
        }
        catch
        {
            return obj.ToString() ?? string.Empty;
        }
    }

    /// <summary>Serializes a set of named values as one JSON object, dropping any that are null.</summary>
    public static string SerializeProperties(params (string name, object? value)[] properties)
    {
        var dict = properties
            .Where(p => p.value != null)
            .ToDictionary(p => p.name, p => p.value);

        return SerializeObject(dict);
    }

    /// <summary>
    /// Builds a one-line summary of a change. Spells out the field for a single edit; for several,
    /// lists the field names and their count to keep the log readable.
    /// </summary>
    public static string CreateChangeDescription(
        string entityName,
        string action,
        params (string propertyName, string oldValue, string newValue)[] changes)
    {
        var changeCount = changes.Length;
        var baseDescription = $"{action}d {entityName}";

        if (changeCount == 0)
            return baseDescription;

        if (changeCount == 1)
        {
            var change = changes[0];
            return $"{baseDescription}: {change.propertyName} changed from '{change.oldValue}' to '{change.newValue}'";
        }

        var propertyNames = string.Join(", ", changes.Select(c => c.propertyName));
        return $"{baseDescription}: {propertyNames} ({changeCount} properties)";
    }

    /// <summary>Flattens an exception and its inner chain into a single pipe-delimited message.</summary>
    public static string GetExceptionMessage(Exception ex)
    {
        if (ex == null)
            return string.Empty;

        var message = ex.Message;
        if (ex.InnerException != null)
            message += $" | {GetExceptionMessage(ex.InnerException)}";

        return message;
    }

    /// <summary>Buckets an entity type into a coarse audit category for filtering/reporting.</summary>
    public static string GetAuditCategory(string entityType)
    {
        return entityType switch
        {
            "User" or "Role" or "Permission" => "Security",
            "Company" or "Branch" or "BusinessUnit" => "Organization",
            "Invoice" or "Payment" or "PurchaseOrder" => "Financial",
            "Employee" or "Department" => "Personnel",
            _ => "System"
        };
    }

    /// <summary>Maps an action verb to a severity — destructive actions rank Critical, edits Warning.</summary>
    public static AuditSeverity GetSeverityForAction(string action)
    {
        return action switch
        {
            "Delete" or "Revoke" or "Disable" => AuditSeverity.Critical,
            "Update" => AuditSeverity.Warning,
            _ => AuditSeverity.Info
        };
    }

    /// <summary>Replaces the named fields in a JSON object with a redaction marker before it is logged.</summary>
    public static string MaskSensitiveData(string? jsonData, params string[] sensitiveFields)
    {
        if (string.IsNullOrEmpty(jsonData))
            return jsonData ?? string.Empty;

        try
        {
            var doc = JsonDocument.Parse(jsonData);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                var options = new JsonSerializerOptions { WriteIndented = false };
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData, options);

                if (dict != null)
                {
                    foreach (var field in sensitiveFields)
                    {
                        if (dict.ContainsKey(field))
                            dict[field] = "***REDACTED***";
                    }

                    return JsonSerializer.Serialize(dict, options);
                }
            }

            return jsonData;
        }
        catch
        {
            return jsonData;
        }
    }

    /// <summary>New correlation id for stitching together the log entries of one logical operation.</summary>
    public static string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString();
    }

    public static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
