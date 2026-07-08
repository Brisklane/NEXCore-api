using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

/// <summary>
/// Registration payload for <c>/register</c>. Its optional fields must mirror
/// <c>ValidateUserDto</c>: an over-strict <c>[Required]</c> here would let a payload the
/// <c>/validate</c> step already accepted fail model binding as ASP.NET
/// <c>ValidationProblemDetails</c>, which callers can't deserialize as our error envelope.
/// </summary>
public class CreateUserDto
{
    [Required] public Guid TenantId { get; set; }
    [Required] public Guid CompanyId { get; set; }
    [Required] public string CompanyName { get; set; } = string.Empty;
    [Required] public string CompanySlug { get; set; } = string.Empty;

    public Guid? BranchId { get; set; }
    public Guid? BusinessUnitId { get; set; }

    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    [Required] public string FullName { get; set; } = string.Empty;
    [Required] public string UserName { get; set; } = string.Empty;

    public string? Gender { get; set; }

    // Optional at registration (completed later in Manage Company / profile).
    public string Address { get; set; } = string.Empty;
    public string? DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
}
