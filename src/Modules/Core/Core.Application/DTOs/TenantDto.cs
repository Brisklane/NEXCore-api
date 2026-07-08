using System.ComponentModel.DataAnnotations;

namespace Core.Application.DTOs;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public TenantSubscriptionDto? ActiveSubscription { get; set; }
}

public class CreateTenantDto
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional. Auto-generated from Name if not provided.
    /// </summary>
    [MaxLength(100)]
    public string? Slug { get; set; }

    [MaxLength(255), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

public class UpdateTenantDto
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}
