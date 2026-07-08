using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

/// <summary>Payload for the pre-registration <c>/validate</c> check; the optional fields mirror <c>CreateUserDto</c>.</summary>
public class ValidateUserDto
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    [Required] public string FullName { get; set; } = string.Empty;
    [Required] public string UserName { get; set; } = string.Empty;

    public string? Gender { get; set; }

    // Optional at registration (completed later in profile / Manage Company).
    public string Address { get; set; } = string.Empty;
    public string? DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
}
