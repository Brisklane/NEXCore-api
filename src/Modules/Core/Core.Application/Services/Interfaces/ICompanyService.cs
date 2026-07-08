using Core.Application.DTOs;
using Core.Application.DTOs.ValidateDtos;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;

namespace Core.Application.Services.Interfaces;

/// <summary>
/// Company management: creation (with its first admin user), CRUD, logo storage, and the
/// anonymous, slug-addressed branding/logo lookups that let the login page style itself before a
/// user is authenticated.
/// </summary>
public interface ICompanyService
{
    /// <summary>Provisions a new company together with its initial admin account.</summary>
    Task<ApiResponse<CompanyDto>> CreateCompanyAndUserAsync(CreateCompanyAndUserDto request);

    Task<Result<CompanyDto>> GetCompanyByIdAsync(Guid companyId);

    Task<Result<IEnumerable<CompanyResponseDto>>> GetAllCompaniesAsync();

    Task<Result<CompanyDto>> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest request, Guid userId);

    Task<Result> DeactivateCompanyAsync(Guid companyId, Guid userId);

    /// <summary>Raw logo bytes for the company, or null if none is set.</summary>
    Task<byte[]?> GetLogoAsync(Guid companyId);

    /// <summary>
    /// Public branding for the company at <c>{slug}.{BaseDomain}</c>, or null when no live company
    /// owns that slug. Cross-tenant by design — it runs anonymously, before login.
    /// </summary>
    Task<CompanyBrandingDto?> GetBrandingBySlugAsync(string slug);

    /// <summary>Logo bytes by slug (backs the anonymous login-page logo), or null.</summary>
    Task<byte[]?> GetLogoBySlugAsync(string slug);

    /// <summary>Registration helper: is the slug a valid, non-reserved, unused subdomain label?</summary>
    Task<SlugAvailabilityDto> CheckSlugAvailabilityAsync(string slug);

    /// <summary>Replaces only the logo bytes, leaving the rest of the company untouched.</summary>
    Task UpdateLogoAsync(Guid companyId, byte[] logoBytes);
}
