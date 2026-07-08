using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.DTOs;

public class CreateUserWithCompanyDto
{
    [Required]
    public Guid TenantId { get; set; }
    [Required]
    public Guid CompanyId { get; set; }
    [Required]
    public string CompanyName { get; set; } = string.Empty;
    [Required]
    public string CompanySlug { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public Guid? BusinessUnitId { get; set; }
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    [Required]
    public string FullName { get; set; } = string.Empty;
    [Required]
    public string UserName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    // Address & phone are optional at registration — completed later in Manage Company / profile.
    public string Address { get; set; } = string.Empty;
    public string? DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
}
