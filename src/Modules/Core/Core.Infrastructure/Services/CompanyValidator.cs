using System.Text.RegularExpressions;
using Core.Application.DTOs;
using Core.Application.DTOs.ValidateDtos;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Core.Infrastructure.Services;

/// <summary>
/// Validates company registration (company + its branches + business units) and the later
/// Manage-Company updates. Registration is lenient - only names/types are mandatory and the
/// rest is format-checked when supplied; the update validators enforce the full field set.
/// Returns the first failure as a typed <see cref="ApiResponse{T}"/>, or null when valid.
/// </summary>
public class CompanyValidator : ICompanyValidator
{
    private readonly CoreDbContext _context;
    private readonly string[] _reservedSubdomains;

    public CompanyValidator(CoreDbContext context, IConfiguration configuration)
    {
        _context = context;
        // Per-environment extras on top of SubdomainRules.DefaultReserved (e.g. tier hosts).
        // GetChildren() avoids the Configuration.Binder dependency (not referenced by this lib).
        _reservedSubdomains = configuration.GetSection("App:ReservedSubdomains")
            .GetChildren().Select(c => c.Value!)
            .Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
    }

    public async Task<ApiResponse<CompanyDto>> ValidateAsync(CompanyDto request)
    {
        var companyValidation = await ValidateCompanyAsync(request);
        if (companyValidation != null)
            return companyValidation;

        if (request.Branches != null && request.Branches.Any())
        {
            foreach (var branch in request.Branches)
            {
                var branchValidation = await ValidateBranchAsync(branch);
                if (branchValidation != null)
                    return branchValidation;

                if (branch.BusinessUnits != null && branch.BusinessUnits.Any())
                {
                    foreach (var businessUnit in branch.BusinessUnits)
                    {
                        var businessUnitValidation = await ValidateBusinessUnitAsync(businessUnit);
                        if (businessUnitValidation != null)
                            return businessUnitValidation;
                    }
                }
            }
        }

        return new ApiResponse<CompanyDto>
        {
            Success = true,
            Message = "Validation successful.",
            Data = request
        };
    }

    // Company Validation
    private async Task<ApiResponse<CompanyDto>?> ValidateCompanyAsync(CompanyDto request)
    {
        // Normalize the code (convert empty string to null)
        request.Code = NormalizeCode(request.Code);

        // ── Company URL slug (= {slug}.{BaseDomain} subdomain): format, reserved, uniqueness ──
        if (!string.IsNullOrWhiteSpace(request.CompanySlug))
        {
            var slug = SubdomainRules.Normalize(request.CompanySlug);

            if (slug.Length < SubdomainRules.MinLength || slug.Length > SubdomainRules.MaxLength)
                return Fail($"Company URL must be between {SubdomainRules.MinLength} and {SubdomainRules.MaxLength} characters.");

            if (!SubdomainRules.IsValidFormat(slug))
                return Fail("Company URL may only contain lowercase letters, numbers, and hyphens, and cannot start or end with a hyphen.");

            if (SubdomainRules.IsReserved(slug, _reservedSubdomains))
                return Fail($"The company URL '{slug}' is reserved and cannot be used. Please choose a different one.");

            var slugTaken = await _context.Companies
                .AnyAsync(c => c.Slug == slug && !c.IsDeleted);

            if (slugTaken)
                return Fail($"The company URL '{slug}' is already taken. Please choose a different one.");

            request.CompanySlug = slug;
        }

        // Code (optional, but unique if provided)
        if (!string.IsNullOrEmpty(request.Code))
        {
            if (request.Code.Length < 3 || request.Code.Length > 20)
                return Fail("Company code must be between 3 and 20 characters.");

            if (!Regex.IsMatch(request.Code, @"^[a-zA-Z0-9]+$"))
                return Fail("Company code must be alphanumeric only.");

            var codeExists = await _context.Companies
                .AnyAsync(c => c.Code == request.Code && !c.IsDeleted);

            if (codeExists)
                return Fail("Company code already exists.");
        }

        // ── Mandatory fields (minimal registration) ──────────────────────
        // Only the company name is required up front. Everything else can be
        // completed later in Manage Company (whose update validators still
        // enforce these fields), so here they are validated for format only
        // when a value is supplied.
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return Fail("Company name is required.");

        if (request.CompanyName.Length > 100)
            return Fail("Company name must not exceed 100 characters.");

        var nameExists = await _context.Companies
            .AnyAsync(c => c.CompanyName == request.CompanyName && !c.IsDeleted);

        if (nameExists)
            return Fail("Company name already exists.");

        // ── Optional fields — validated only when a value is supplied ─────
        // Mobile Number
        if (!string.IsNullOrWhiteSpace(request.MobileNumber) && !IsValidPhone(request.MobileNumber))
            return Fail("Mobile number format is invalid.");

        // Contact Person
        if (!string.IsNullOrWhiteSpace(request.ContactPerson) && request.ContactPerson.Length > 100)
            return Fail("Contact person must not exceed 100 characters.");

        // Email (format + uniqueness when supplied)
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (!IsValidEmail(request.Email))
                return Fail("Email format is invalid.");

            var emailExists = await _context.Companies
                .AnyAsync(c => c.Email == request.Email && !c.IsDeleted);

            if (emailExists)
                return Fail("Email already exists.");
        }

        // City
        if (!string.IsNullOrWhiteSpace(request.City) && request.City.Length > 100)
            return Fail("City must not exceed 100 characters.");

        // Country
        if (!string.IsNullOrWhiteSpace(request.Country) && request.Country.Length > 100)
            return Fail("Country must not exceed 100 characters.");

        // Legal Name
        if (!string.IsNullOrWhiteSpace(request.LegalName) && request.LegalName.Length > 100)
            return Fail("Legal name must not exceed 100 characters.");

        // Registration Number
        if (!string.IsNullOrWhiteSpace(request.RegistrationNumber) &&
            (request.RegistrationNumber.Length < 3 || request.RegistrationNumber.Length > 50))
            return Fail("Registration number must be between 3 and 50 characters.");

        // Base Currency Code
        if (!string.IsNullOrWhiteSpace(request.BaseCurrencyCode) &&
            (request.BaseCurrencyCode.Length != 3 || !Regex.IsMatch(request.BaseCurrencyCode, @"^[A-Za-z]{3}$")))
            return Fail("Base currency code must be exactly 3 alphabetic characters (e.g. USD).");

        // Phone Number
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !IsValidPhone(request.PhoneNumber))
            return Fail("Phone number format is invalid.");

        // Website
        if (!string.IsNullOrWhiteSpace(request.WebsiteUrl) && !IsValidWebsite(request.WebsiteUrl))
            return Fail("Website URL format is invalid.");

        // Latitude
        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
            return Fail("Latitude must be between -90 and 90.");

        // Longitude
        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
            return Fail("Longitude must be between -180 and 180.");

        // Radius
        if (request.RadiusInMeters < 0)
            return Fail("Radius cannot be negative.");

        // Street Address
        if (!string.IsNullOrWhiteSpace(request.StreetAddress) && request.StreetAddress.Length > 200)
            return Fail("Street address must not exceed 200 characters.");

        // State
        if (!string.IsNullOrWhiteSpace(request.State) && request.State.Length > 100)
            return Fail("State must not exceed 100 characters.");

        // Postal Code
        if (!string.IsNullOrWhiteSpace(request.PostalCode) &&
            (request.PostalCode.Length < 3 || request.PostalCode.Length > 20))
            return Fail("Postal code must be between 3 and 20 characters.");

        return null;
    }

    // Branch Validation
    private async Task<ApiResponse<CompanyDto>?> ValidateBranchAsync(CreateBranchDto request)
    {
        await Task.CompletedTask; // registration branch validation is synchronous (no DB lookups)

        // Normalize the code (convert empty string to null)
        request.Code = NormalizeCode(request.Code);

        // Code (optional, but unique if provided)
        if (!string.IsNullOrEmpty(request.Code))
        {
            if (request.Code.Length < 3 || request.Code.Length > 20)
                return Fail("Branch code must be between 3 and 20 characters.");

            if (!Regex.IsMatch(request.Code, @"^[a-zA-Z0-9]+$"))
                return Fail("Branch code must be alphanumeric only.");

            // Branch codes are unique PER COMPANY (enforced by the (CompanyId, Code)
            // DB index), so no global uniqueness check here — a brand-new company can
            // safely use default codes (e.g. BR01).
        }

        // Name
        if (string.IsNullOrWhiteSpace(request.Name))
            return Fail("Branch name is required.");

        if (request.Name.Length > 100)
            return Fail("Branch name must not exceed 100 characters.");

        // Branch Type
        if (string.IsNullOrWhiteSpace(request.BranchType))
            return Fail("Branch type is required.");

        if (request.BranchType.Length > 50)
            return Fail("Branch type must not exceed 50 characters.");

        // ── Optional at registration — validated for format only when supplied.
        //    Completed later in Manage Company (its branch-update validator
        //    still requires phone, email, manager, logo, address and geo). ──
        // Phone Number
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !IsValidPhone(request.PhoneNumber))
            return Fail("Branch phone number format is invalid.");

        // Email
        if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
            return Fail("Branch email format is invalid.");

        // Manager Name
        if (!string.IsNullOrWhiteSpace(request.ManagerName) && request.ManagerName.Length > 100)
            return Fail("Manager name must not exceed 100 characters.");

        // Street Address
        if (!string.IsNullOrWhiteSpace(request.StreetAddress) && request.StreetAddress.Length > 200)
            return Fail("Street address must not exceed 200 characters.");

        // City
        if (!string.IsNullOrWhiteSpace(request.City) && request.City.Length > 100)
            return Fail("City must not exceed 100 characters.");

        // State
        if (!string.IsNullOrWhiteSpace(request.State) && request.State.Length > 100)
            return Fail("State must not exceed 100 characters.");

        // Postal Code
        if (!string.IsNullOrWhiteSpace(request.PostalCode) &&
            (request.PostalCode.Length < 3 || request.PostalCode.Length > 20))
            return Fail("Postal code must be between 3 and 20 characters.");

        // Latitude
        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
            return Fail("Latitude must be between -90 and 90.");

        // Longitude
        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
            return Fail("Longitude must be between -180 and 180.");

        return null;
    }

    // Business Unit Validation
    private async Task<ApiResponse<CompanyDto>?> ValidateBusinessUnitAsync(CreateBusinessUnitDto request)
    {
        await Task.CompletedTask; // registration business-unit validation is synchronous (no DB lookups)

        // Normalize the code (convert empty string to null)
        request.Code = NormalizeCode(request.Code);

        // Code is optional now
        if (!string.IsNullOrEmpty(request.Code))
        {
            if (request.Code.Length < 3 || request.Code.Length > 20)
                return Fail("Business unit code must be between 3 and 20 characters.");

            if (!Regex.IsMatch(request.Code, @"^[a-zA-Z0-9]+$"))
                return Fail("Business unit code must be alphanumeric only.");

            // Business-unit codes are unique PER COMPANY (enforced by the
            // (CompanyId, Code) DB index), so no global uniqueness check here — a
            // brand-new company can safely use default codes (e.g. BU01).
        }

        // Name
        if (string.IsNullOrWhiteSpace(request.Name))
            return Fail("Business unit name is required.");

        if (request.Name.Length > 100)
            return Fail("Business unit name must not exceed 100 characters.");

        // Unit Type
        if (string.IsNullOrWhiteSpace(request.UnitType))
            return Fail("Unit type is required.");

        if (request.UnitType.Length > 50)
            return Fail("Unit type must not exceed 50 characters.");

        // ── Optional at registration — validated for format only when supplied;
        //    completed later in Manage Company. ──
        // Description
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 500)
            return Fail("Description must not exceed 500 characters.");

        // Manager Name
        if (!string.IsNullOrWhiteSpace(request.ManagerName) && request.ManagerName.Length > 100)
            return Fail("Manager name must not exceed 100 characters.");

        // Manager Email
        if (!string.IsNullOrWhiteSpace(request.ManagerEmail) && !IsValidEmail(request.ManagerEmail))
            return Fail("Manager email format is invalid.");

        return null;
    }

    // Update Validation Methods

    /// <summary>
    /// Validates company update request (excludes non-updatable fields: Code, CompanyName, Email, RegistrationNumber)
    /// </summary>
    public Task<ApiResponse<UpdateCompanyRequest>?> ValidateCompanyUpdateAsync(UpdateCompanyRequest request)
        => Task.FromResult(ValidateCompanyUpdate(request));

    private ApiResponse<UpdateCompanyRequest>? ValidateCompanyUpdate(UpdateCompanyRequest request)
    {
        // Legal Name
        if (string.IsNullOrWhiteSpace(request.LegalName))
            return FailUpdate("Legal name is required.");

        if (request.LegalName.Length > 100)
            return FailUpdate("Legal name must not exceed 100 characters.");

        // Base Currency Code
        if (string.IsNullOrWhiteSpace(request.BaseCurrencyCode))
            return FailUpdate("Base currency code is required.");

        if (request.BaseCurrencyCode.Length != 3 || !Regex.IsMatch(request.BaseCurrencyCode, @"^[A-Za-z]{3}$"))
            return FailUpdate("Base currency code must be exactly 3 alphabetic characters (e.g. USD).");

        // Phone Number
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return FailUpdate("Phone number is required.");

        if (!IsValidPhone(request.PhoneNumber))
            return FailUpdate("Phone number format is invalid.");

        // Mobile Number
        if (string.IsNullOrWhiteSpace(request.MobileNumber))
            return FailUpdate("Mobile number is required.");

        if (!IsValidPhone(request.MobileNumber))
            return FailUpdate("Mobile number format is invalid.");

        // Contact Person
        if (string.IsNullOrWhiteSpace(request.ContactPerson))
            return FailUpdate("Contact person is required.");

        if (request.ContactPerson.Length > 100)
            return FailUpdate("Contact person must not exceed 100 characters.");

        // Website
        if (string.IsNullOrWhiteSpace(request.WebsiteUrl))
            return FailUpdate("Website URL is required.");

        if (!IsValidWebsite(request.WebsiteUrl))
            return FailUpdate("Website URL format is invalid.");

        // Company Logo (optional — validated only when supplied elsewhere)

        // Latitude (optional — validated only when a value is supplied)
        if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
            return FailUpdate("Latitude must be between -90 and 90.");

        // Longitude (optional — validated only when a value is supplied)
        if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
            return FailUpdate("Longitude must be between -180 and 180.");

        // Radius (optional — cannot be negative)
        if (request.RadiusInMeters < 0)
            return FailUpdate("Radius cannot be negative.");

        // Street Address
        if (string.IsNullOrWhiteSpace(request.StreetAddress))
            return FailUpdate("Street address is required.");

        if (request.StreetAddress.Length > 200)
            return FailUpdate("Street address must not exceed 200 characters.");

        // City
        if (string.IsNullOrWhiteSpace(request.City))
            return FailUpdate("City is required.");

        if (request.City.Length > 100)
            return FailUpdate("City must not exceed 100 characters.");

        // State
        if (string.IsNullOrWhiteSpace(request.State))
            return FailUpdate("State is required.");

        if (request.State.Length > 100)
            return FailUpdate("State must not exceed 100 characters.");

        // Postal Code
        if (string.IsNullOrWhiteSpace(request.PostalCode))
            return FailUpdate("Postal code is required.");

        if (request.PostalCode.Length < 3 || request.PostalCode.Length > 20)
            return FailUpdate("Postal code must be between 3 and 20 characters.");

        return null; // Validation passed
    }

    /// <summary>
    /// Validates branch update request
    /// </summary>
    public async Task<ApiResponse<UpdateCompanyRequest>?> ValidateBranchUpdateAsync(UpdateBranchRequestDto request, bool isNewBranch)
    {
        // Normalize the code (convert empty string to null)
        request.Code = NormalizeCode(request.Code);

        if (isNewBranch && !string.IsNullOrEmpty(request.Code))
        {
            if (request.Code.Length < 3 || request.Code.Length > 20)
                return FailUpdate("Branch code must not exceed 20 characters.");

            if (!Regex.IsMatch(request.Code, @"^[a-zA-Z0-9]+$"))
                return FailUpdate("Branch code must be alphanumeric only.");

            var codeExists = await _context.Branches
                .AnyAsync(b => b.Code == request.Code && !b.IsDeleted);

            if (codeExists)
                return FailUpdate("Branch code already exists.");
        }

        // Name
        if (string.IsNullOrWhiteSpace(request.Name))
            return FailUpdate("Branch name is required.");

        if (request.Name.Length > 100)
            return FailUpdate("Branch name must not exceed 100 characters.");

        // Branch Type
        if (string.IsNullOrWhiteSpace(request.BranchType))
            return FailUpdate("Branch type is required.");

        if (request.BranchType.Length > 50)
            return FailUpdate("Branch type must not exceed 50 characters.");

        // Phone Number
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return FailUpdate("Branch phone number is required.");

        if (!IsValidPhone(request.PhoneNumber))
            return FailUpdate("Branch phone number format is invalid.");

        // Email
        if (string.IsNullOrWhiteSpace(request.Email))
            return FailUpdate("Branch email is required.");

        if (!IsValidEmail(request.Email))
            return FailUpdate("Branch email format is invalid.");

        // Manager Name
        if (string.IsNullOrWhiteSpace(request.ManagerName))
            return FailUpdate("Manager name is required.");

        if (request.ManagerName.Length > 100)
            return FailUpdate("Manager name must not exceed 100 characters.");

        // Branch Logo
        if (request.BranchLogo == null || request.BranchLogo.Length == 0)
            return FailUpdate("Branch logo is required.");

        // Street Address
        if (string.IsNullOrWhiteSpace(request.StreetAddress))
            return FailUpdate("Street address is required.");

        if (request.StreetAddress.Length > 200)
            return FailUpdate("Street address must not exceed 200 characters.");

        // City
        if (string.IsNullOrWhiteSpace(request.City))
            return FailUpdate("City is required.");

        if (request.City.Length > 100)
            return FailUpdate("City must not exceed 100 characters.");

        // State
        if (string.IsNullOrWhiteSpace(request.State))
            return FailUpdate("State is required.");

        if (request.State.Length > 100)
            return FailUpdate("State must not exceed 100 characters.");

        // Postal Code
        if (string.IsNullOrWhiteSpace(request.PostalCode))
            return FailUpdate("Postal code is required.");

        if (request.PostalCode.Length < 3 || request.PostalCode.Length > 20)
            return FailUpdate("Postal code must be between 3 and 20 characters.");

        // Latitude
        if (!request.Latitude.HasValue)
            return FailUpdate("Latitude is required.");

        if (request.Latitude < -90 || request.Latitude > 90)
            return FailUpdate("Latitude must be between -90 and 90.");

        // Longitude
        if (!request.Longitude.HasValue)
            return FailUpdate("Longitude is required.");

        if (request.Longitude < -180 || request.Longitude > 180)
            return FailUpdate("Longitude must be between -180 and 180.");

        return null; // Validation passed
    }

    /// <summary>
    /// Validates business unit update request
    /// </summary>
    public async Task<ApiResponse<UpdateCompanyRequest>?> ValidateBusinessUnitUpdateAsync(UpdateBusinessUnitRequestDto request, bool isNewUnit)
    {
        // Normalize the code (convert empty string to null)
        request.Code = NormalizeCode(request.Code);

        if (isNewUnit && !string.IsNullOrEmpty(request.Code))
        {
            if (request.Code.Length < 3 || request.Code.Length > 20)
                return FailUpdate("Business unit code must not exceed 20 characters.");

            if (!Regex.IsMatch(request.Code, @"^[a-zA-Z0-9]+$"))
                return FailUpdate("Business unit code must be alphanumeric only.");

            var codeExists = await _context.BusinessUnits
                .AnyAsync(bu => bu.Code == request.Code && !bu.IsDeleted);

            if (codeExists)
                return FailUpdate("Business unit code already exists.");
        }

        // Name
        if (string.IsNullOrWhiteSpace(request.Name))
            return FailUpdate("Business unit name is required.");

        if (request.Name.Length > 100)
            return FailUpdate("Business unit name must not exceed 100 characters.");

        // Unit Type
        if (string.IsNullOrWhiteSpace(request.UnitType))
            return FailUpdate("Unit type is required.");

        if (request.UnitType.Length > 50)
            return FailUpdate("Unit type must not exceed 50 characters.");

        // Description
        if (string.IsNullOrWhiteSpace(request.Description))
            return FailUpdate("Description is required.");

        if (request.Description.Length > 500)
            return FailUpdate("Description must not exceed 500 characters.");

        // Manager Name
        if (string.IsNullOrWhiteSpace(request.ManagerName))
            return FailUpdate("Manager name is required.");

        if (request.ManagerName.Length > 100)
            return FailUpdate("Manager name must not exceed 100 characters.");

        // Manager Email
        if (string.IsNullOrWhiteSpace(request.ManagerEmail))
            return FailUpdate("Manager email is required.");

        if (!IsValidEmail(request.ManagerEmail))
            return FailUpdate("Manager email format is invalid.");

        return null; // Validation passed
    }

    // Helper Methods

    /// <summary>
    /// Normalizes code by converting empty strings and whitespace to null
    /// </summary>
    private static string? NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim();
    }

    /// <summary>
    /// Helper method for update validation failures
    /// </summary>
    private static ApiResponse<UpdateCompanyRequest> FailUpdate(string message)
    {
        return new ApiResponse<UpdateCompanyRequest>
        {
            Success = false,
            Message = message,
            Data = null
        };
    }

    /// <summary>
    /// Helper method for validation failures
    /// </summary>
    private static ApiResponse<CompanyDto> Fail(string message)
    {
        return new ApiResponse<CompanyDto>
        {
            Success = false,
            Message = message,
            Data = null
        };
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Validates phone number format
    /// </summary>
    private static bool IsValidPhone(string phone)
    {
        return Regex.IsMatch(phone, @"^[\d\+\-\s\(\)]{7,20}$");
    }

    /// <summary>
    /// Validates website URL format
    /// </summary>
    private static bool IsValidWebsite(string website)
    {
        return Uri.TryCreate(website, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}