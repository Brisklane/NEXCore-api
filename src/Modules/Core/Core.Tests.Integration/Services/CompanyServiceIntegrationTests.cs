using Core.Application.DTOs;
using Core.Application.DTOs.ValidateDtos;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;

namespace Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for CompanyService.
/// Verifies the full flow: service → validator → InMemory database.
/// The Auth API HttpClient is mocked as it is an external dependency boundary.
/// </summary>
public class CompanyServiceIntegrationTests : CoreIntegrationTestFixture
{
    private Mock<ICompanyValidator> _validatorMock = null!;
    private Mock<HttpMessageHandler> _httpHandlerMock = null!;
    private HttpClient _httpClient = null!;
    private CompanyService _service = null!;

    private const string AuthBaseUrl = "https://auth-api.test/api/user/";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _validatorMock = new Mock<ICompanyValidator>();
        _httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Default);
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "BaseUrls:AuthApiBaseUrl", AuthBaseUrl }
            })
            .Build();

        var tenantService = new TenantService(
            DbContext,
            new Mock<ILogger<TenantService>>().Object);

        _service = new CompanyService(
            DbContext,
            _validatorMock.Object,
            tenantService,
            _httpClient,
            configuration,
            new Mock<IEventPublisher>().Object,
            new Mock<ILogger<CompanyService>>().Object);
    }

    public override async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await base.DisposeAsync();
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static CreateCompanyAndUserDto BuildValidCreateRequest(string companyName = "Integration Company")
    {
        return new CreateCompanyAndUserDto
        {
            Company = new CompanyDto
            {
                Code = "INTEG01",
                CompanyName = companyName,
                LegalName = "Integration Company Legal",
                RegistrationNumber = "REG999",
                BaseCurrencyCode = "USD",
                PhoneNumber = "+1234567890",
                MobileNumber = "+1234567891",
                ContactPerson = "Jane Smith",
                Email = "integ@company.com",
                WebsiteUrl = "https://integ.com",
                CompanyLogo = new byte[] { 1, 2, 3 },
                Latitude = 40.7128m,
                Longitude = -74.0060m,
                RadiusInMeters = 500,
                StreetAddress = "1 Integration Ave",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Branches = new List<CreateBranchDto>
                {
                    new CreateBranchDto
                    {
                        Code = "BR001",
                        Name = "Main Branch",
                        BranchType = "Headquarters",
                        PhoneNumber = "+1234567890",
                        Email = "branch@integ.com",
                        ManagerName = "Branch Manager",
                        BranchLogo = new byte[] { 1, 2, 3 },
                        StreetAddress = "1 Branch St",
                        City = "New York",
                        State = "NY",
                        PostalCode = "10001",
                        Latitude = 40.7128m,
                        Longitude = -74.0060m,
                        IsActive = true,
                        BusinessUnits = new List<CreateBusinessUnitDto>
                        {
                            new CreateBusinessUnitDto
                            {
                                Code = "BU001",
                                Name = "Sales",
                                UnitType = "Department",
                                Description = "Sales department",
                                ManagerName = "Sales Manager",
                                ManagerEmail = "sales@integ.com",
                                IsActive = true
                            }
                        }
                    }
                }
            },
            User = new CreateUserDto
            {
                Email = "admin@integ.com",
                Password = "Password123!",
                FullName = "Admin User",
                UserName = "admin",
                Gender = "Male",
                Address = "1 Integration Ave",
                DateOfBirth = "1990-01-01",
                PhoneNumber = "+1234567890"
            }
        };
    }

    private static UpdateCompanyRequest BuildValidUpdateRequest()
    {
        return new UpdateCompanyRequest
        {
            CompanyName = "Updated Integration Company",
            LegalName = "Updated Legal Name",
            BaseCurrencyCode = "USD",
            PhoneNumber = "+1987654321",
            MobileNumber = "+1987654322",
            ContactPerson = "Updated Contact",
            WebsiteUrl = "https://updated-integ.com",
            CompanyLogo = new byte[] { 9, 8, 7 },
            Latitude = 34.0522m,
            Longitude = -118.2437m,
            RadiusInMeters = 1000,
            StreetAddress = "99 Updated St",
            City = "Los Angeles",
            State = "CA",
            PostalCode = "90001",
            Branches = new List<UpdateBranchRequestDto>()
        };
    }

    /// <summary>Both Auth API calls (validate + register) return 200.</summary>
    private void SetupAuthApiSuccess()
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
    }

    /// <summary>First Auth call (validate) returns 200, second (register) returns 400.</summary>
    private void SetupAuthApiValidateOkRegisterFail(string errorMessage)
    {
        var callCount = 0;
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    return new HttpResponseMessage(HttpStatusCode.OK);

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new ApiErrorResponse { Message = errorMessage })
                };
            });
    }

    // =====================================================================
    // CreateCompanyAndUserAsync
    // =====================================================================

    [Fact]
    public async Task CreateCompanyAndUser_WhenAllValid_PersistsCompanyWithBranchAndBusinessUnit()
    {
        // Arrange
        var request = BuildValidCreateRequest("Full Hierarchy Company");

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request.Company });

        SetupAuthApiSuccess();

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert — response
        Assert.True(result.Success);
        Assert.Equal("Company setup created successfully.", result.Message);

        // Assert — DB state using a fresh context to avoid EF tracking
        await using var verify = CreateDbContext();
        var company = await verify.Companies
            .Include(c => c.Branches)
                .ThenInclude(b => b.BusinessUnits)
            .FirstOrDefaultAsync(c => c.CompanyName == "Full Hierarchy Company");

        Assert.NotNull(company);
        Assert.True(company.IsActive);
        Assert.Single(company.Branches);
        Assert.Single(company.Branches.First().BusinessUnits);
    }

    [Fact]
    public async Task CreateCompanyAndUser_WhenCompanyValidationFails_NothingPersistedToDatabase()
    {
        // Arrange
        var request = BuildValidCreateRequest();

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto>
            {
                Success = false,
                Message = "Company name already exists."
            });

        var countBefore = await DbContext.Companies.CountAsync();

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company name already exists.", result.Message);

        var countAfter = await DbContext.Companies.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task CreateCompanyAndUser_WhenUserValidationFails_NothingPersistedToDatabase()
    {
        // Arrange
        var request = BuildValidCreateRequest();

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request.Company });

        // Auth validate returns 400
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new ApiErrorResponse { Message = "Invalid user data." })
            });

        var countBefore = await DbContext.Companies.CountAsync();

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid user data.", result.Message);

        // No company saved because failure happens before DB write
        var countAfter = await DbContext.Companies.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task CreateCompanyAndUser_WhenUserRegistrationFails_ReturnsFailureWithAuthMessage()
    {
        // Arrange
        var request = BuildValidCreateRequest();

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request.Company });

        SetupAuthApiValidateOkRegisterFail("Username already taken.");

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Username already taken.", result.Message);
    }

    [Fact]
    public async Task CreateCompanyAndUser_WhenAuthServiceUnavailable_ReturnsGenericFailure()
    {
        // Arrange
        var request = BuildValidCreateRequest();

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request.Company });

        // Auth API returns 500
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Unable to validate user due to an external service error.", result.Message);
    }

    // =====================================================================
    // GetCompanyByIdAsync
    // =====================================================================

    [Fact]
    public async Task GetCompanyById_WhenCompanyExistsWithBranches_ReturnsMappedHierarchy()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("GETTEST", "Get Test Company");
        await testData.CreateBranchAsync(company.Id, "BR001", "Test Branch");

        // Act
        var result = await _service.GetCompanyByIdAsync(company.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Get Test Company", result.Data.CompanyName);
        Assert.Single(result.Data.Branches);
    }

    [Fact]
    public async Task GetCompanyById_WhenCompanyDoesNotExist_ReturnsFailure()
    {
        // Act
        var result = await _service.GetCompanyByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company not found", result.Message);
    }

    [Fact]
    public async Task GetCompanyById_WhenCompanyIsInactive_ReturnsFailure()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("INACTIVE", "Inactive Company");

        company.IsActive = false;
        DbContext.Companies.Update(company);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetCompanyByIdAsync(company.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company not found", result.Message);
    }

    [Fact]
    public async Task GetCompanyById_MapsAddressFieldsCorrectly()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("ADDR01", "Address Company");

        // Act
        var result = await _service.GetCompanyByIdAsync(company.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("123 Test Street", result.Data.StreetAddress);
        Assert.Equal("Test City", result.Data.City);
        Assert.Equal("Test State", result.Data.State);
        Assert.Equal("12345", result.Data.PostalCode);
    }

    [Fact]
    public async Task GetCompanyById_WhenCompanyHasMultipleBranches_ReturnsAllBranches()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("MULTIBR", "Multi Branch Company");
        await testData.CreateBranchAsync(company.Id, "BR001", "Branch One");
        await testData.CreateBranchAsync(company.Id, "BR002", "Branch Two");

        // Act
        var result = await _service.GetCompanyByIdAsync(company.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Branches.Count);
    }

    // =====================================================================
    // UpdateCompanyAsync
    // =====================================================================

    [Fact]
    public async Task UpdateCompany_WhenCompanyExists_PersistsAllFieldsToDatabase()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("UPD001", "Original Company");
        var userId = Guid.NewGuid();
        var request = BuildValidUpdateRequest();

        _validatorMock
            .Setup(x => x.ValidateCompanyUpdateAsync(It.IsAny<UpdateCompanyRequest>()))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        // Act
        var result = await _service.UpdateCompanyAsync(company.Id, request, userId);

        // Assert — service response
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        // Assert — DB state
        await using var verify = CreateDbContext();
        var updated = await verify.Companies.FindAsync(company.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Legal Name", updated.LegalName);
        Assert.Equal("+1987654321", updated.PhoneNumber);
        Assert.Equal("https://updated-integ.com", updated.WebsiteUrl);
        Assert.Equal(userId, updated.UpdatedByUserId);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCompany_WhenCompanyExists_PersistsAddressChanges()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("UPD002", "Address Update Company");
        var request = BuildValidUpdateRequest();

        _validatorMock
            .Setup(x => x.ValidateCompanyUpdateAsync(It.IsAny<UpdateCompanyRequest>()))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        // Act
        await _service.UpdateCompanyAsync(company.Id, request, Guid.NewGuid());

        // Assert
        await using var verify = CreateDbContext();
        var updated = await verify.Companies.FindAsync(company.Id);
        Assert.NotNull(updated);
        Assert.Equal("99 Updated St", updated.Address.StreetAddress);
        Assert.Equal("Los Angeles", updated.Address.City);
        Assert.Equal("CA", updated.Address.State);
        Assert.Equal("90001", updated.Address.PostalCode);
    }

    [Fact]
    public async Task UpdateCompany_WhenCompanyNotFound_ReturnsFailure()
    {
        // Act
        var result = await _service.UpdateCompanyAsync(Guid.NewGuid(), BuildValidUpdateRequest(), Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company not found", result.Message);
    }

    [Fact]
    public async Task UpdateCompany_WhenCompanyIsSoftDeleted_ReturnsFailure()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("SOFTDEL", "Soft Deleted Company");

        company.IsDeleted = true;
        DbContext.Companies.Update(company);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.UpdateCompanyAsync(company.Id, BuildValidUpdateRequest(), Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company not found", result.Message);
    }

    [Fact]
    public async Task UpdateCompany_WhenValidationFails_DoesNotPersistChanges()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("VALFAIL", "Validation Fail Company");
        var originalLegalName = company.LegalName;

        _validatorMock
            .Setup(x => x.ValidateCompanyUpdateAsync(It.IsAny<UpdateCompanyRequest>()))
            .ReturnsAsync(new ApiResponse<UpdateCompanyRequest>
            {
                Success = false,
                Message = "Legal name is required."
            });

        // Act
        var result = await _service.UpdateCompanyAsync(company.Id, BuildValidUpdateRequest(), Guid.NewGuid());

        // Assert — failure returned
        Assert.False(result.Success);
        Assert.Equal("Legal name is required.", result.Message);

        // Assert — DB unchanged
        await using var verify = CreateDbContext();
        var unchanged = await verify.Companies.FindAsync(company.Id);
        Assert.NotNull(unchanged);
        Assert.Equal(originalLegalName, unchanged.LegalName);
    }

    [Fact]
    public async Task UpdateCompany_WithNewBranch_BranchAppearsInDatabase()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("NEWBR01", "New Branch Company");
        var userId = Guid.NewGuid();

        var request = BuildValidUpdateRequest();
        request.Branches = new List<UpdateBranchRequestDto>
        {
            new UpdateBranchRequestDto
            {
                BranchId = null,
                Code = "NEWBR",
                Name = "Brand New Branch",
                BranchType = "Regional",
                PhoneNumber = "+1234567890",
                Email = "newbranch@test.com",
                ManagerName = "New Manager",
                BranchLogo = new byte[] { 1 },
                StreetAddress = "5 New Branch Rd",
                City = "Chicago",
                State = "IL",
                PostalCode = "60601",
                Latitude = 41.8781m,
                Longitude = -87.6298m,
                IsActive = true,
                BusinessUnits = new List<UpdateBusinessUnitRequestDto>()
            }
        };

        _validatorMock
            .Setup(x => x.ValidateCompanyUpdateAsync(It.IsAny<UpdateCompanyRequest>()))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        _validatorMock
            .Setup(x => x.ValidateBranchUpdateAsync(It.IsAny<UpdateBranchRequestDto>(), true))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        // Act
        var result = await _service.UpdateCompanyAsync(company.Id, request, userId);

        // Assert
        Assert.True(result.Success);

        await using var verify = CreateDbContext();
        var savedBranch = await verify.Branches
            .FirstOrDefaultAsync(b => b.Code == "NEWBR" && b.CompanyId == company.Id);

        Assert.NotNull(savedBranch);
        Assert.Equal("Brand New Branch", savedBranch.Name);
        Assert.Equal(company.Id, savedBranch.CompanyId);
    }

    [Fact]
    public async Task UpdateCompany_WithExistingBranch_UpdatesBranchInDatabase()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("UPDBR01", "Update Branch Company");
        var branch = await testData.CreateBranchAsync(company.Id, "BR001", "Old Branch Name");
        var userId = Guid.NewGuid();

        var request = BuildValidUpdateRequest();
        request.Branches = new List<UpdateBranchRequestDto>
        {
            new UpdateBranchRequestDto
            {
                BranchId = branch.Id,
                Name = "Updated Branch Name",
                BranchType = "Headquarters",
                PhoneNumber = "+1111111111",
                Email = "updated@branch.com",
                ManagerName = "Updated Manager",
                BranchLogo = new byte[] { 5 },
                StreetAddress = "Updated St",
                City = "Houston",
                State = "TX",
                PostalCode = "77001",
                Latitude = 29.7604m,
                Longitude = -95.3698m,
                IsActive = true,
                BusinessUnits = new List<UpdateBusinessUnitRequestDto>()
            }
        };

        _validatorMock
            .Setup(x => x.ValidateCompanyUpdateAsync(It.IsAny<UpdateCompanyRequest>()))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        _validatorMock
            .Setup(x => x.ValidateBranchUpdateAsync(It.IsAny<UpdateBranchRequestDto>(), false))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        // Act
        var result = await _service.UpdateCompanyAsync(company.Id, request, userId);

        // Assert
        Assert.True(result.Success);

        await using var verify = CreateDbContext();
        var updatedBranch = await verify.Branches.FindAsync(branch.Id);
        Assert.NotNull(updatedBranch);
        Assert.Equal("Updated Branch Name", updatedBranch.Name);
        Assert.Equal(userId, updatedBranch.UpdatedByUserId);
    }

    [Fact]
    public async Task UpdateCompany_WithNewBusinessUnit_BusinessUnitAppearsInDatabase()
    {
        // Arrange
        var testData = new CoreTestDataBuilder(DbContext);
        var company = await testData.CreateCompanyAsync("NEWBU01", "New BU Company");
        var branch = await testData.CreateBranchAsync(company.Id, "BR001", "Existing Branch");
        var userId = Guid.NewGuid();

        var request = BuildValidUpdateRequest();
        request.Branches = new List<UpdateBranchRequestDto>
        {
            new UpdateBranchRequestDto
            {
                BranchId = branch.Id,
                Name = branch.Name,
                BranchType = "Headquarters",
                PhoneNumber = "+1234567890",
                Email = "branch@test.com",
                ManagerName = "Manager",
                BranchLogo = new byte[] { 1 },
                StreetAddress = "1 Branch St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Latitude = 40.7128m,
                Longitude = -74.0060m,
                IsActive = true,
                BusinessUnits = new List<UpdateBusinessUnitRequestDto>
                {
                    new UpdateBusinessUnitRequestDto
                    {
                        BusinessUnitId = null,
                        Code = "NEWBU",
                        Name = "New Business Unit",
                        UnitType = "Department",
                        Description = "A new department",
                        ManagerName = "BU Manager",
                        ManagerEmail = "bu@test.com",
                        IsActive = true
                    }
                }
            }
        };

        _validatorMock
            .Setup(x => x.ValidateCompanyUpdateAsync(It.IsAny<UpdateCompanyRequest>()))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        _validatorMock
            .Setup(x => x.ValidateBranchUpdateAsync(It.IsAny<UpdateBranchRequestDto>(), false))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        _validatorMock
            .Setup(x => x.ValidateBusinessUnitUpdateAsync(It.IsAny<UpdateBusinessUnitRequestDto>(), true))
            .ReturnsAsync((ApiResponse<UpdateCompanyRequest>?)null);

        // Act
        var result = await _service.UpdateCompanyAsync(company.Id, request, userId);

        // Assert
        Assert.True(result.Success);

        await using var verify = CreateDbContext();
        var savedBU = await verify.BusinessUnits
            .FirstOrDefaultAsync(bu => bu.Code == "NEWBU" && bu.BranchId == branch.Id);

        Assert.NotNull(savedBU);
        Assert.Equal("New Business Unit", savedBU.Name);
        Assert.Equal(company.Id, savedBU.CompanyId);
    }
}
