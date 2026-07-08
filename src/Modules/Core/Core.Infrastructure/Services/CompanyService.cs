using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.ValueObjects;
using Core.Application.DTOs.ValidateDtos;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using System.Net.Http;
using System.Net;
using System.Net.Http.Json;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services;

/// <summary>
/// Company lifecycle and the registration flow that provisions a tenant, company and admin user
/// together, then publishes <c>CompanyCreatedEvent</c> so other modules can seed their own data.
/// Also serves the anonymous, slug-addressed branding/logo lookups (which bypass the tenant query
/// filter on purpose, since they run pre-login across tenants).
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly CoreDbContext _context;
    private readonly ICompanyValidator _validator;
    private readonly TenantService _tenantService;
    private readonly HttpClient _httpClient;
    private readonly string _authBaseUrl;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CompanyService> _logger;
    private readonly string[] _reservedSubdomains;

    public CompanyService(
        CoreDbContext context,
        ICompanyValidator validator,
        TenantService tenantService,
        HttpClient httpClient,
        IConfiguration configuration,
        IEventPublisher eventPublisher,
        ILogger<CompanyService> logger)
    {
        _context = context;
        _validator = validator;
        _tenantService = tenantService;
        _httpClient = httpClient;
        _authBaseUrl = configuration["BaseUrls:AuthApiBaseUrl"]!;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _reservedSubdomains = configuration.GetSection("App:ReservedSubdomains")
            .GetChildren().Select(c => c.Value!)
            .Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();

        if (string.IsNullOrEmpty(_authBaseUrl))
        {
            throw new InvalidOperationException("AuthApiBaseUrl is not configured in the appsettings.");
        }
    }

    // ── Subdomain tenancy: anonymous, cross-tenant lookups by slug ──────────────────
    // These are addressed by the subdomain ({slug}.{BaseDomain}) and run pre-login, so they
    // intentionally IgnoreQueryFilters() (the Company tenant filter would otherwise scope them
    // to the caller's tenant — wrong for a global slug lookup).

    public async Task<CompanyBrandingDto?> GetBrandingBySlugAsync(string slug)
    {
        var normalized = SubdomainRules.Normalize(slug);
        if (string.IsNullOrEmpty(normalized))
            return null;

        return await _context.Companies
            .IgnoreQueryFilters()
            .Where(c => c.Slug == normalized && !c.IsDeleted && c.IsActive)
            .Select(c => new CompanyBrandingDto
            {
                Slug = c.Slug,
                CompanyName = c.CompanyName,
                HasLogo = c.CompanyLogo != null && c.CompanyLogo.Length > 0
            })
            .FirstOrDefaultAsync();
    }

    public async Task<byte[]?> GetLogoBySlugAsync(string slug)
    {
        var normalized = SubdomainRules.Normalize(slug);
        if (string.IsNullOrEmpty(normalized))
            return null;

        return await _context.Companies
            .IgnoreQueryFilters()
            .Where(c => c.Slug == normalized && !c.IsDeleted && c.IsActive)
            .Select(c => c.CompanyLogo)
            .FirstOrDefaultAsync();
    }

    public async Task<SlugAvailabilityDto> CheckSlugAvailabilityAsync(string slug)
    {
        var normalized = SubdomainRules.Normalize(slug);

        if (!SubdomainRules.IsValidFormat(normalized))
            return new SlugAvailabilityDto { Slug = normalized, Available = false, Reason = "invalid-format" };

        if (SubdomainRules.IsReserved(normalized, _reservedSubdomains))
            return new SlugAvailabilityDto { Slug = normalized, Available = false, Reason = "reserved" };

        var taken = await _context.Companies
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Slug == normalized && !c.IsDeleted);

        return new SlugAvailabilityDto
        {
            Slug = normalized,
            Available = !taken,
            Reason = taken ? "taken" : null
        };
    }

    public async Task<ApiResponse<CompanyDto>> CreateCompanyAndUserAsync(CreateCompanyAndUserDto request)
    {
        // The company code field was removed from the registration form. Guarantee a valid,
        // auto-generated code is persisted (never empty/default) so the company always has a
        // unique identifier — and so the validator's uniqueness/format rules pass.
        if (string.IsNullOrWhiteSpace(request.Company.Code))
            request.Company.Code = GenerateCompanyCode(request.Company.CompanyName);

        // STEP 1: Validate Company
        var companyValidationResult = await _validator.ValidateAsync(request.Company);

        if (!companyValidationResult.Success)
            return companyValidationResult;

        // STEP 2: Validate User via Auth API
        var userValidationResponse = await _httpClient.PostAsJsonAsync($"{_authBaseUrl}validate", request.User);

        if (!userValidationResponse.IsSuccessStatusCode)
        {
            if (userValidationResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                // Read the body as a string first, then try to parse it. A downstream 400 can be
                // an ApiErrorResponse OR ASP.NET's ValidationProblemDetails (whose `errors` is an
                // object, not List<string>) — parsing the latter directly would throw and surface
                // as a 500. This keeps a clean validation message either way.
                var raw = await userValidationResponse.Content.ReadAsStringAsync();
                var message = "User validation failed.";
                try
                {
                    var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(
                        raw, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (!string.IsNullOrWhiteSpace(errorResponse?.Message))
                        message = errorResponse!.Message;
                }
                catch
                {
                    // Non-ApiErrorResponse body — keep the generic message.
                }

                return new ApiResponse<CompanyDto>
                {
                    Success = false,
                    Message = message,
                    Data = null
                };
            }

            return new ApiResponse<CompanyDto>
            {
                Success = false,
                Message = "Unable to validate user due to an external service error.",
                Data = null
            };
        }

        try
        {
            // Ensure system user ID is valid
            if (!Guid.TryParse(AppConstants.CreatedUserId, out var createdByUserId))
            {
                return new ApiResponse<CompanyDto>
                {
                    Success = false,
                    Message = "Invalid system user identifier configuration.",
                    Data = null
                };
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // STEP 3a: Create Tenant (always before company)
                    var tenant = await _tenantService.CreateTenantInternalAsync(
                        request.Company.CompanyName,
                        request.Company.Email,
                        createdByUserId,
                        request.Company.CompanySlug);

                    _logger.LogInformation("Tenant {TenantId} created for company registration", tenant.Id);

                    // STEP 3b: Create Company bound to the new Tenant
                    var company = new Company
                    {
                        TenantId = tenant.Id,
                        Slug = tenant.Slug, // company slug mirrors the auto-generated/provided tenant slug
                        Code = request.Company.Code,
                        CompanyName = request.Company.CompanyName,
                        LegalName = request.Company.LegalName,
                        RegistrationNumber = request.Company.RegistrationNumber ?? string.Empty,
                        BaseCurrencyCode = request.Company.BaseCurrencyCode,
                        PhoneNumber = request.Company.PhoneNumber,
                        MobileNumber = request.Company.MobileNumber,
                        ContactPerson = request.Company.ContactPerson,
                        Email = request.Company.Email,
                        WebsiteUrl = request.Company.WebsiteUrl,
                        CompanyLogo = request.Company.CompanyLogo,
                        Latitude = request.Company.Latitude,
                        Longitude = request.Company.Longitude,
                        RadiusInMeters = request.Company.RadiusInMeters,
                        Address = new Address
                        {
                            StreetAddress = request.Company.StreetAddress,
                            City = request.Company.City,
                            State = request.Company.State,
                            PostalCode = request.Company.PostalCode,
                            Country = request.Company.Country
                        },
                        IsActive = true,
                        CreatedByUserId = createdByUserId,
                        CreatedAt = DateTime.UtcNow,
                        Branches = new List<Branch>()
                    };

                    // Create branches and business units (branches are optional)
                    foreach (var branchDto in request.Company.Branches ?? Enumerable.Empty<CreateBranchDto>())
                    {
                        var branch = new Branch
                        {
                            Code = branchDto.Code,
                            Name = branchDto.Name,
                            BranchType = branchDto.BranchType,
                            PhoneNumber = branchDto.PhoneNumber,
                            Email = branchDto.Email,
                            ManagerName = branchDto.ManagerName,
                            BranchLogo = branchDto.BranchLogo,
                            Latitude = branchDto.Latitude,
                            Longitude = branchDto.Longitude,
                            Address = new Address
                            {
                                StreetAddress = branchDto.StreetAddress,
                                City = branchDto.City,
                                State = branchDto.State,
                                PostalCode = branchDto.PostalCode
                            },
                            IsActive = branchDto.IsActive,
                            CreatedByUserId = createdByUserId,
                            CreatedAt = DateTime.UtcNow,
                            BusinessUnits = new List<BusinessUnit>()
                        };

                        // Add Business Units for this branch
                        foreach (var buDto in branchDto.BusinessUnits)
                        {
                            var businessUnit = new BusinessUnit
                            {
                                Code = buDto.Code,
                                Name = buDto.Name,
                                UnitType = buDto.UnitType,
                                Description = buDto.Description,
                                ManagerName = buDto.ManagerName,
                                ManagerEmail = buDto.ManagerEmail,
                                IsActive = buDto.IsActive,
                                CreatedByUserId = createdByUserId,
                                CreatedAt = DateTime.UtcNow,
                                CompanyId = company.Id
                            };

                            branch.BusinessUnits.Add(businessUnit);
                        }

                        // Guarantee every branch has at least one business unit. If the request
                        // supplied none, add a default "Main" BU. Code is left null so it is exempt
                        // from the (CompanyId, Code) unique index and each branch can have its own.
                        if (branch.BusinessUnits.Count == 0)
                        {
                            branch.BusinessUnits.Add(new BusinessUnit
                            {
                                Name = "Main Business Unit",
                                UnitType = "Main",
                                IsActive = true,
                                CreatedByUserId = createdByUserId,
                                CreatedAt = DateTime.UtcNow,
                                CompanyId = company.Id
                            });
                        }

                        company.Branches.Add(branch);
                    }

                    _context.Companies.Add(company);

                    // Save company inside transaction, but DO NOT commit yet
                    await _context.SaveChangesAsync();

                    // STEP 4: Create user DTO and register user through Auth API.
                    // Default the user's context to the first branch and its first (Main) business unit.
                    var firstBranch = company.Branches.FirstOrDefault();
                    var firstBusinessUnitId = firstBranch?.BusinessUnits.FirstOrDefault()?.Id ?? Guid.Empty;

                    var userDto = MapToCreateUserDto(request.User,
                        tenant.Id,
                        company.Id,
                        company.CompanyName,
                        company.Slug,
                        firstBranch?.Id ?? Guid.Empty,
                        firstBusinessUnitId);

                    var userRegistrationResponse = await _httpClient.PostAsJsonAsync($"{_authBaseUrl}register", userDto);

                    if (!userRegistrationResponse.IsSuccessStatusCode)
                    {
                        // Read the body as a string first, then try to parse it. A downstream 400 can be
                        // an ApiErrorResponse OR ASP.NET's ValidationProblemDetails (whose `errors` is an
                        // object, not List<string>) — deserializing the latter as ApiErrorResponse throws
                        // and would otherwise surface as a confusing "Error creating company setup" 500.
                        var raw = await userRegistrationResponse.Content.ReadAsStringAsync();
                        var registrationError = "Unknown error";
                        try
                        {
                            var apiError = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(
                                raw, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (!string.IsNullOrWhiteSpace(apiError?.Message))
                                registrationError = apiError!.Message!;
                        }
                        catch
                        {
                            // Non-ApiErrorResponse body — keep the generic message.
                        }

                        await transaction.RollbackAsync();

                        return new ApiResponse<CompanyDto>
                        {
                            Success = false,
                            Message = $"User registration failed: {registrationError}",
                            Data = null
                        };
                    }

                    await transaction.CommitAsync();

                    // STEP 5: Publish CompanyCreatedEvent to allow other modules to initialize their data
                    // This runs AFTER transaction commit to ensure company exists in database
                    try
                    {
                        if (firstBranch is not null)
                        {
                            var companyCreatedEvent = new CompanyCreatedEvent
                            {
                                CompanyId = company.Id,
                                BranchId = firstBranch.Id,
                                BusinessUnitId = firstBusinessUnitId,
                                CreatedByUserId = createdByUserId,
                                CompanyName = company.CompanyName,
                                IncludeSampleData = request.IncludeSampleData,
                                PublishedAt = DateTime.UtcNow
                            };

                            await _eventPublisher.PublishAsync(companyCreatedEvent);
                            _logger.LogInformation("CompanyCreatedEvent published for Company: {CompanyId}", company.Id);
                        }
                    }
                    
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error publishing CompanyCreatedEvent for Company: {CompanyId}", company.Id);
                        // Non-blocking - event publishing failure doesn't affect company creation
                    }

                    return new ApiResponse<CompanyDto>
                    {
                        Success = true,
                        Message = request.IncludeSampleData
                            ? "Company created. Master data (chart of accounts, templates, price lists, tax, POS setup) and sample data initialized."
                            : "Company created. Master data (chart of accounts, templates, price lists, tax, POS setup) initialized.",
                        Data = request.Company
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    return new ApiResponse<CompanyDto>
                    {
                        Success = false,
                        Message = $"Error creating company setup: {ex.Message}",
                        Data = null
                    };
                }
            });
        }
        catch (Exception ex)
        {
            return new ApiResponse<CompanyDto>
            {
                Success = false,
                Message = $"Error creating company setup: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<Result<CompanyDto>> GetCompanyByIdAsync(Guid companyId)
    {
        try
        {
            var company = await _context.Companies
                .Include(c => c.Branches.Where(b => !b.IsDeleted))
                    .ThenInclude(b => b.BusinessUnits.Where(bu => !bu.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == companyId && c.IsActive);

            if (company == null)
            {
                return Result<CompanyDto>.Fail("Company not found");
            }

            var dto = MapToCompanyDto(company);

            return Result<CompanyDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Fail($"Error retrieving company: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CompanyResponseDto>>> GetAllCompaniesAsync()
    {
        try
        {
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            return Result<IEnumerable<CompanyResponseDto>>.Ok(companies.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CompanyResponseDto>>.Fail($"Error retrieving companies: {ex.Message}");
        }
    }

    public async Task<Result<CompanyDto>> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest request, Guid userId)
    {
        try
        {
            // ===== STEP 1: RETRIEVE COMPANY WITH BRANCHES AND BUSINESS UNITS (live rows only) =====
            var company = await _context.Companies
                .Include(c => c.Branches.Where(b => !b.IsDeleted))
                    .ThenInclude(b => b.BusinessUnits.Where(bu => !bu.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == companyId && !c.IsDeleted);

            if (company == null)
            {
                return Result<CompanyDto>.Fail("Company not found");
            }

            // ===== STEP 2: VALIDATE COMPANY UPDATE (EXCLUDING NON-UPDATABLE FIELDS) =====
            var companyValidation = await _validator.ValidateCompanyUpdateAsync(request);
            if (companyValidation != null)
            {
                return Result<CompanyDto>.Fail(companyValidation.Message);
            }

            // ===== STEP 3: VALIDATE BRANCHES =====
            if (request.Branches != null && request.Branches.Any())
            {
                foreach (var branchUpdate in request.Branches)
                {
                    bool isNewBranch = !branchUpdate.BranchId.HasValue || branchUpdate.BranchId == Guid.Empty;

                    var branchValidation = await _validator.ValidateBranchUpdateAsync(branchUpdate, isNewBranch);
                    if (branchValidation != null)
                    {
                        return Result<CompanyDto>.Fail(branchValidation.Message);
                    }

                    // ===== STEP 4: VALIDATE BUSINESS UNITS WITHIN BRANCH =====
                    if (branchUpdate.BusinessUnits != null && branchUpdate.BusinessUnits.Any())
                    {
                        foreach (var unitUpdate in branchUpdate.BusinessUnits)
                        {
                            bool isNewUnit = !unitUpdate.BusinessUnitId.HasValue || unitUpdate.BusinessUnitId == Guid.Empty;

                            var unitValidation = await _validator.ValidateBusinessUnitUpdateAsync(unitUpdate, isNewUnit);
                            if (unitValidation != null)
                            {
                                return Result<CompanyDto>.Fail(unitValidation.Message);
                            }
                        }
                    }
                }
            }

            // ===== STEP 4b: UNIQUE NAMES PER PARENT (branch within company, BU within branch) =====
            if (request.Branches != null && request.Branches.Any())
            {
                var branchNames = request.Branches
                    .Select(b => (b.Name ?? string.Empty).Trim().ToLowerInvariant())
                    .Where(n => n.Length > 0)
                    .ToList();
                if (branchNames.Count != branchNames.Distinct().Count())
                    return Result<CompanyDto>.Fail("Branch names must be unique within the company.");

                foreach (var branchUpdate in request.Branches)
                {
                    if (branchUpdate.BusinessUnits == null) continue;
                    var buNames = branchUpdate.BusinessUnits
                        .Select(u => (u.Name ?? string.Empty).Trim().ToLowerInvariant())
                        .Where(n => n.Length > 0)
                        .ToList();
                    if (buNames.Count != buNames.Distinct().Count())
                        return Result<CompanyDto>.Fail($"Business unit names must be unique within branch '{branchUpdate.Name}'.");
                }
            }

            // ===== STEP 5: UPDATE COMPANY PROPERTIES =====
            // Note: Code, CompanyName, Email, RegistrationNumber are NOT updated (non-updatable fields)
            company.LegalName = request.LegalName;
            company.BaseCurrencyCode = request.BaseCurrencyCode;
            company.PhoneNumber = request.PhoneNumber;
            company.MobileNumber = request.MobileNumber;
            company.ContactPerson = request.ContactPerson;
            company.WebsiteUrl = request.WebsiteUrl;
            company.CompanyLogo = request.CompanyLogo;
            company.Latitude = request.Latitude;
            company.Longitude = request.Longitude;
            company.RadiusInMeters = request.RadiusInMeters;

            // Update address
            if (company.Address == null)
                company.Address = new Address();

            company.Address.StreetAddress = request.StreetAddress;
            company.Address.City = request.City;
            company.Address.State = request.State;
            company.Address.PostalCode = request.PostalCode;

            // ===== STEP 6: UPDATE/CREATE BRANCHES =====
            if (request.Branches != null && request.Branches.Any())
            {
                await UpdateBranchesAsync(company, request.Branches, userId);
            }

            // Update audit fields
            company.UpdatedAt = DateTime.UtcNow;
            company.UpdatedByUserId = userId;

            // ===== STEP 7: SAVE ALL CHANGES =====
            await _context.SaveChangesAsync();

            // ===== STEP 8: RETURN FULL COMPANY USING EXISTING METHOD =====
            return await GetCompanyByIdAsync(companyId);
        }
        catch (DbUpdateException)
        {
            return Result<CompanyDto>.Fail("Failed to update company. Database error occurred.");
        }
        catch (Exception ex)
        {
            return Result<CompanyDto>.Fail($"Error updating company: {ex.Message}");
        }
    }
    /// <summary>
    /// Updates existing branches or creates new ones for the company
    /// </summary>
    private async Task UpdateBranchesAsync(Company company, ICollection<UpdateBranchRequestDto> branchUpdates, Guid userId)
    {
        // Reconcile deletions: any live branch absent from the request is soft-deleted (cascade to its BUs).
        // Runs first, against the originally-loaded branches, so newly-added branches are unaffected.
        var keepBranchIds = branchUpdates
            .Where(b => b.BranchId.HasValue && b.BranchId.Value != Guid.Empty)
            .Select(b => b.BranchId!.Value)
            .ToHashSet();

        var deleteStamp = DateTime.UtcNow;
        foreach (var existing in company.Branches.Where(b => !b.IsDeleted).ToList())
        {
            if (!keepBranchIds.Contains(existing.Id))
                SoftDeleteBranch(existing, userId, deleteStamp);
        }

        foreach (var branchUpdate in branchUpdates)
        {
            Branch? branch;
            bool isNewBranch = !branchUpdate.BranchId.HasValue || branchUpdate.BranchId == Guid.Empty;

            if (!isNewBranch)
            {
                // ===== UPDATE EXISTING BRANCH =====
                branch = company.Branches.FirstOrDefault(b => b.Id == branchUpdate.BranchId);

                if (branch == null)
                {
                    // Branch exists but not in loaded collection
                    branch = await _context.Branches
                        .Include(b => b.BusinessUnits)
                        .FirstOrDefaultAsync(b =>
                            b.Id == branchUpdate.BranchId && b.CompanyId == company.Id && !b.IsDeleted);

                    if (branch == null)
                        continue; // Skip if branch doesn't belong to this company
                }

                // Update branch properties (NOTE: Code is NOT updated for existing branches)
                branch.Name = branchUpdate.Name;
                branch.BranchType = branchUpdate.BranchType;
                branch.PhoneNumber = branchUpdate.PhoneNumber;
                branch.Email = branchUpdate.Email;
                branch.ManagerName = branchUpdate.ManagerName;
                branch.BranchLogo = branchUpdate.BranchLogo;
                branch.Latitude = branchUpdate.Latitude;
                branch.Longitude = branchUpdate.Longitude;

                // Update address
                if (branch.Address == null)
                    branch.Address = new Address();

                branch.Address.StreetAddress = branchUpdate.StreetAddress;
                branch.Address.City = branchUpdate.City;
                branch.Address.State = branchUpdate.State;
                branch.Address.PostalCode = branchUpdate.PostalCode;

                branch.IsActive = branchUpdate.IsActive;
                branch.UpdatedAt = DateTime.UtcNow;
                branch.UpdatedByUserId = userId;
            }
            else
            {
                // ===== CREATE NEW BRANCH =====
                branch = new Branch
                {
                    Id = Guid.NewGuid(),
                    CompanyId = company.Id,
                    Code = branchUpdate.Code,
                    Name = branchUpdate.Name,
                    BranchType = branchUpdate.BranchType,
                    PhoneNumber = branchUpdate.PhoneNumber,
                    Email = branchUpdate.Email,
                    ManagerName = branchUpdate.ManagerName,
                    BranchLogo = branchUpdate.BranchLogo,
                    Latitude = branchUpdate.Latitude,
                    Longitude = branchUpdate.Longitude,
                    Address = new Address
                    {
                        StreetAddress = branchUpdate.StreetAddress,
                        City = branchUpdate.City,
                        State = branchUpdate.State,
                        PostalCode = branchUpdate.PostalCode
                    },
                    IsActive = branchUpdate.IsActive,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    BusinessUnits = new List<BusinessUnit>()
                };

                company.Branches.Add(branch);
                _context.Branches.Add(branch);
            }

            // ===== UPDATE/CREATE BUSINESS UNITS WITHIN BRANCH =====
            if (branchUpdate.BusinessUnits != null && branchUpdate.BusinessUnits.Any())
            {
                await UpdateBusinessUnitsForBranchAsync(branch, branchUpdate.BusinessUnits, company.Id, userId);
            }

            // A newly-created branch with no business units provided gets a default "Main" BU
            // (null code → exempt from the (CompanyId, Code) unique index).
            if (isNewBranch && !branch.BusinessUnits.Any())
            {
                var mainUnit = new BusinessUnit
                {
                    Id = Guid.NewGuid(),
                    CompanyId = company.Id,
                    BranchId = branch.Id,
                    Name = "Main Business Unit",
                    UnitType = "Main",
                    IsActive = true,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                branch.BusinessUnits.Add(mainUnit);
                _context.BusinessUnits.Add(mainUnit);
            }
        }
    }

    /// <summary>
    /// Updates existing business units or creates new ones for a branch
    /// </summary>
    private async Task UpdateBusinessUnitsForBranchAsync(Branch branch, ICollection<UpdateBusinessUnitRequestDto> unitUpdates, Guid companyId, Guid userId)
    {
        // Reconcile deletions: any live business unit on this branch absent from the request is soft-deleted.
        var keepUnitIds = unitUpdates
            .Where(u => u.BusinessUnitId.HasValue && u.BusinessUnitId.Value != Guid.Empty)
            .Select(u => u.BusinessUnitId!.Value)
            .ToHashSet();

        var deleteStamp = DateTime.UtcNow;
        foreach (var existing in branch.BusinessUnits.Where(u => !u.IsDeleted).ToList())
        {
            if (!keepUnitIds.Contains(existing.Id))
                SoftDeleteBusinessUnit(existing, userId, deleteStamp);
        }

        foreach (var unitUpdate in unitUpdates)
        {
            BusinessUnit? unit;
            bool isNewUnit = !unitUpdate.BusinessUnitId.HasValue || unitUpdate.BusinessUnitId == Guid.Empty;

            if (!isNewUnit)
            {
                // ===== UPDATE EXISTING BUSINESS UNIT =====
                unit = branch.BusinessUnits.FirstOrDefault(bu => bu.Id == unitUpdate.BusinessUnitId);

                if (unit == null)
                {
                    // Try to find in database
                    unit = await _context.BusinessUnits.FirstOrDefaultAsync(bu =>
                        bu.Id == unitUpdate.BusinessUnitId && bu.BranchId == branch.Id && !bu.IsDeleted);

                    if (unit == null)
                        continue; // Skip if unit doesn't belong to this branch
                }

                // Update unit properties (NOTE: Code is NOT updated for existing units)
                unit.Name = unitUpdate.Name;
                unit.UnitType = unitUpdate.UnitType;
                unit.Description = unitUpdate.Description;
                unit.ManagerName = unitUpdate.ManagerName;
                unit.ManagerEmail = unitUpdate.ManagerEmail;
                unit.IsActive = unitUpdate.IsActive;
                unit.UpdatedAt = DateTime.UtcNow;
                unit.UpdatedByUserId = userId;
            }
            else
            {
                // ===== CREATE NEW BUSINESS UNIT =====
                unit = new BusinessUnit
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = branch.Id,
                    Code = unitUpdate.Code,
                    Name = unitUpdate.Name,
                    UnitType = unitUpdate.UnitType,
                    Description = unitUpdate.Description,
                    ManagerName = unitUpdate.ManagerName,
                    ManagerEmail = unitUpdate.ManagerEmail,
                    IsActive = unitUpdate.IsActive,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                branch.BusinessUnits.Add(unit);
                _context.BusinessUnits.Add(unit);
            }
        }
    }
    // ── Soft-delete helpers (set IsDeleted + audit fields; cascade branch → business units) ──
    private static void SoftDeleteBranch(Branch branch, Guid? userId, DateTime now)
    {
        branch.IsActive = false;
        branch.IsDeleted = true;
        branch.DeletedAt = now;
        branch.DeletedByUserId = userId;
        branch.UpdatedAt = now;
        branch.UpdatedByUserId = userId;

        foreach (var bu in branch.BusinessUnits.Where(u => !u.IsDeleted))
            SoftDeleteBusinessUnit(bu, userId, now);
    }

    private static void SoftDeleteBusinessUnit(BusinessUnit unit, Guid? userId, DateTime now)
    {
        unit.IsActive = false;
        unit.IsDeleted = true;
        unit.DeletedAt = now;
        unit.DeletedByUserId = userId;
        unit.UpdatedAt = now;
        unit.UpdatedByUserId = userId;
    }

    public async Task<Result> DeactivateCompanyAsync(Guid companyId, Guid userId)
    {
        try
        {
            var company = await _context.Companies
                .Include(c => c.Branches)
                    .ThenInclude(b => b.BusinessUnits)
                .FirstOrDefaultAsync(c => c.Id == companyId && !c.IsDeleted);
            if (company == null)
            {
                return Result.Fail("Company not found");
            }

            var now = DateTime.UtcNow;
            company.IsActive = false;
            company.IsDeleted = true;
            company.DeletedAt = now;
            company.DeletedByUserId = userId;
            company.UpdatedAt = now;
            company.UpdatedByUserId = userId;

            // Cascade the soft-delete to all branches and their business units.
            foreach (var branch in company.Branches.Where(b => !b.IsDeleted))
                SoftDeleteBranch(branch, userId, now);

            await _context.SaveChangesAsync();

            return Result.Ok("Company deleted successfully");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error deleting company: {ex.Message}");
        }
    }

    public async Task<byte[]?> GetLogoAsync(Guid companyId)
    {
        return await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId && !c.IsDeleted)
            .Select(c => c.CompanyLogo)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateLogoAsync(Guid companyId, byte[] logoBytes)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.IsDeleted)
            ?? throw new InvalidOperationException("Company not found");

        company.CompanyLogo = logoBytes;
        company.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static CompanyResponseDto MapToDto(Company company)
    {
        return new CompanyResponseDto
        {
            Id = company.Id,
            Code = company.Code!,
            CompanyName = company.CompanyName,
            LegalName = company.LegalName,
            RegistrationNumber = company.RegistrationNumber,
            BaseCurrencyCode = company.BaseCurrencyCode,

            PhoneNumber = company.PhoneNumber,
            MobileNumber = company.MobileNumber,
            ContactPerson = company.ContactPerson,
            Email = company.Email,
            WebsiteUrl = company.WebsiteUrl,
            CompanyLogo = company.CompanyLogo!,
            Latitude = company.Latitude,
            Longitude = company.Longitude,
            RadiusInMeters = company.RadiusInMeters,

            StreetAddress = company.Address?.StreetAddress ?? string.Empty,
            City = company.Address?.City ?? string.Empty,
            Country = company.Address?.Country ?? string.Empty,
            State = company.Address?.State ?? string.Empty,
            PostalCode = company.Address?.PostalCode ?? string.Empty,
        };
    }
    /// <summary>
    /// Builds a unique, valid company code (alphanumeric, 3–20 chars) from the company name plus a
    /// timestamp. Used when the registration form supplies no code, so a real auto-generated value
    /// is stored rather than an empty/default one.
    /// </summary>
    private static string GenerateCompanyCode(string? companyName)
    {
        var prefix = new string((companyName ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        if (prefix.Length > 6)
            prefix = prefix.Substring(0, 6);
        if (prefix.Length == 0)
            prefix = "COMP";

        // Unix milliseconds → 13 digits, so total length stays within the 3–20 char rule.
        return $"{prefix}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    private CreateUserWithCompanyDto MapToCreateUserDto(CreateUserDto request, Guid tenantId, Guid companyId, string companyName, string companySlug, Guid branchId, Guid businessUnitId)
    {
        return new CreateUserWithCompanyDto
        {
            TenantId = tenantId,
            CompanyId = companyId,
            CompanyName = companyName,
            CompanySlug = companySlug,
            BranchId = branchId,
            BusinessUnitId = businessUnitId,
            Email = request.Email,
            Password = request.Password,
            FullName = request.FullName,
            UserName = request.UserName,
            Gender = request.Gender,
            Address = request.Address,
            DateOfBirth = request.DateOfBirth,
            PhoneNumber = request.PhoneNumber
        };
    }

    private CompanyDto MapToCompanyDto(Company company)
    {
        return new CompanyDto
        {
            CompanyId = company.Id,
            Code = company.Code,
            CompanyName = company.CompanyName,
            LegalName = company.LegalName,
            RegistrationNumber = company.RegistrationNumber,
            BaseCurrencyCode = company.BaseCurrencyCode,
            PhoneNumber = company.PhoneNumber,
            MobileNumber = company.MobileNumber,
            ContactPerson = company.ContactPerson,
            Email = company.Email,
            WebsiteUrl = company.WebsiteUrl,
            CompanyLogo = company.CompanyLogo ?? Array.Empty<byte>(),
            Latitude = company.Latitude,
            Longitude = company.Longitude,
            RadiusInMeters = company.RadiusInMeters,
            StreetAddress = company.Address?.StreetAddress ?? string.Empty,
            City = company.Address?.City ?? string.Empty,
            Country = company.Address?.Country ?? string.Empty,
            State = company.Address?.State ?? string.Empty,
            PostalCode = company.Address?.PostalCode ?? string.Empty,

            Branches = company.Branches?.Select(branch => new CreateBranchDto
            {
                BranchId = branch.Id,
                Code = branch.Code,
                Name = branch.Name,
                BranchType = branch.BranchType!,
                PhoneNumber = branch.PhoneNumber!,
                Email = branch.Email!,
                ManagerName = branch.ManagerName!,
                BranchLogo = branch.BranchLogo ?? Array.Empty<byte>(),
                StreetAddress = branch.Address?.StreetAddress ?? string.Empty,
                City = branch.Address?.City ?? string.Empty,
                State = branch.Address?.State ?? string.Empty,
                PostalCode = branch.Address?.PostalCode ?? string.Empty,

                Latitude = branch.Latitude,
                Longitude = branch.Longitude,
                IsActive = branch.IsActive,

                BusinessUnits = branch.BusinessUnits?.Select(unit => new CreateBusinessUnitDto
                {
                    BusinessUnitId = unit.Id,
                    Code = unit.Code,
                    Name = unit.Name,
                    UnitType = unit.UnitType!,
                    ManagerEmail = unit.ManagerEmail!,
                    ManagerName = unit.ManagerName!,
                    Description = unit.Description!,
                    IsActive = unit.IsActive
                }).ToList() ?? new List<CreateBusinessUnitDto>()
            }).ToList() ?? new List<CreateBranchDto>()
        };
    }
}
