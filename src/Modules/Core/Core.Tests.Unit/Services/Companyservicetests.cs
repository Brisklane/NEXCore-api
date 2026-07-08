using Core.Application.DTOs;
using Core.Application.DTOs.ValidateDtos;
using Core.Application.Services.Interfaces;
using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.ValueObjects;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Moq.Protected;

namespace Core.Tests.Unit.Services;

/// <summary>
/// Unit tests for CompanyService.
/// Tests cover: CreateCompanyAndUserAsync, GetCompanyByIdAsync, UpdateCompanyAsync, GetAllCompaniesAsync, DeactivateCompanyAsync.
/// </summary>
public class CompanyServiceTests : IDisposable
{
    private readonly CoreDbContext _context;
    private readonly Mock<ICompanyValidator> _validatorMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<CompanyService>> _loggerMock;
    private readonly CompanyService _service;

    private const string AuthBaseUrl = "https://auth-api.test/api/user/";

    public CompanyServiceTests()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // No-tenant context so the Company global query filter stays inert for these
        // tenant-agnostic unit tests (a null ITenantContext would NRE during EF filter evaluation).
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(false);
        _context = new CoreDbContext(options, tenantContextMock.Object);
        _validatorMock = new Mock<ICompanyValidator>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<CompanyService>>();

        _httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Default);
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "BaseUrls:AuthApiBaseUrl", AuthBaseUrl }
            })
            .Build();

        var tenantService = new TenantService(
            _context,
            new Mock<ILogger<TenantService>>().Object);

        _service = new CompanyService(
            _context,
            _validatorMock.Object,
            tenantService,
            _httpClient,
            _configuration,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    #region Helpers

    private static CreateCompanyAndUserDto BuildValidCreateRequest()
    {
        return new CreateCompanyAndUserDto
        {
            Company = new CompanyDto
            {
                Code = "COMP001",
                CompanyName = "Test Company",
                LegalName = "Test Company Legal",
                RegistrationNumber = "REG123456",
                BaseCurrencyCode = "USD",
                PhoneNumber = "+1234567890",
                MobileNumber = "+1234567891",
                ContactPerson = "John Doe",
                Email = "company@test.com",
                WebsiteUrl = "https://test.com",
                CompanyLogo = new byte[] { 1, 2, 3 },
                Latitude = 40.7128m,
                Longitude = -74.0060m,
                RadiusInMeters = 500,
                StreetAddress = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Branches = new List<CreateBranchDto>()
            },
            User = new CreateUserDto
            {
                Email = "admin@test.com",
                Password = "Password123!",
                FullName = "Admin User",
                UserName = "admin",
                Gender = "Male",
                Address = "123 Main St",
                DateOfBirth = "1990-01-01",
                PhoneNumber = "+1234567890"
            }
        };
    }

    private static UpdateCompanyRequest BuildValidUpdateRequest()
    {
        return new UpdateCompanyRequest
        {
            CompanyName = "Updated Company",
            LegalName = "Updated Legal Name",
            BaseCurrencyCode = "USD",
            PhoneNumber = "+1234567890",
            MobileNumber = "+1234567891",
            ContactPerson = "Jane Doe",
            WebsiteUrl = "https://updated.com",
            CompanyLogo = new byte[] { 1, 2, 3 },
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            RadiusInMeters = 500,
            StreetAddress = "456 New St",
            City = "Chicago",
            State = "IL",
            PostalCode = "60601",
            Branches = new List<UpdateBranchRequestDto>(),
            Email = "company@test.com",
            RegistrationNumber = "REG123456"
        };
    }

    private void SetupHttpResponse(HttpStatusCode statusCode)
    {
        var responseMessage = new HttpResponseMessage(statusCode);

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
    }

    private void SetupSequentialHttpResponses(HttpStatusCode validateStatus, HttpStatusCode registerStatus)
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
                    return new HttpResponseMessage(validateStatus);
                return new HttpResponseMessage(registerStatus);
            });
    }

    #endregion

    #region CreateCompanyAndUserAsync Tests

    [Fact]
    public async Task CreateCompanyAndUserAsync_WhenAllStepsSucceed_ReturnsSuccess()
    {
        // Arrange
        var request = BuildValidCreateRequest();

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request.Company });

        SetupSequentialHttpResponses(HttpStatusCode.OK, HttpStatusCode.OK);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<CompanyCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(
            "Company created. Master data (chart of accounts, templates, price lists, tax, POS setup) initialized.",
            result.Message);
    }

    [Fact]
    public async Task CreateCompanyAndUserAsync_WhenCompanyValidationFails_ReturnsFailure()
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

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company name already exists.", result.Message);
    }

    [Fact]
    public async Task CreateCompanyAndUserAsync_WhenUserValidationFails_ReturnsFailure()
    {
        // Arrange
        var request = BuildValidCreateRequest();

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request.Company });

        // Auth API validation endpoint returns 400 with error message
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                response.Content = JsonContent.Create(new ApiErrorResponse
                {
                    Message = "Email already in use."
                });
                return response;
            });

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Email already in use.", result.Message);
    }

    [Fact]
    public async Task CreateCompanyAndUserAsync_WhenUserRegistrationFails_ReturnsFailure()
    {
        // Arrange
        var request = BuildValidCreateRequest();

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request.Company });

        // First call (validate) returns OK, second call (register) returns 400
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
                
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                response.Content = JsonContent.Create(new ApiErrorResponse
                {
                    Message = "Username already taken."
                });
                return response;
            });

        // Act
        var result = await _service.CreateCompanyAndUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User registration failed:", result.Message);
        Assert.Contains("Username already taken.", result.Message);
    }

    [Fact]
    public async Task CreateCompanyAndUserAsync_WhenAllSucceed_PersistsCompanyToDatabase()
    {
        // Arrange
        var request = BuildValidCreateRequest();

        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<CompanyDto>()))
            .ReturnsAsync(new ApiResponse<CompanyDto> { Success = true, Data = request.Company });

        SetupSequentialHttpResponses(HttpStatusCode.OK, HttpStatusCode.OK);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<CompanyCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateCompanyAndUserAsync(request);

        // Assert - verify company was persisted
        var companies = _context.Companies.ToList();
        Assert.Single(companies);
        Assert.Equal("Test Company", companies.First().CompanyName);
    }

    #endregion

    #region GetCompanyByIdAsync Tests

    [Fact]
    public async Task GetCompanyByIdAsync_WhenCompanyExists_ReturnsMappedDto()
    {
        // Arrange
        var company = new CompanyBuilder().Build();
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCompanyByIdAsync(company.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(company.CompanyName, result.Data.CompanyName);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_WhenCompanyDoesNotExist_ReturnsFailure()
    {
        // Act
        var result = await _service.GetCompanyByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company not found", result.Message);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_WhenCompanyIsInactive_ReturnsFailure()
    {
        // Arrange
        var company = new CompanyBuilder().WithIsActive(false).Build();
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCompanyByIdAsync(company.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company not found", result.Message);
    }

    #endregion

    #region GetAllCompaniesAsync Tests

    [Fact]
    public async Task GetAllCompaniesAsync_WhenCompaniesExist_ReturnsAll()
    {
        // Arrange
        _context.Companies.Add(new CompanyBuilder().Build());
        _context.Companies.Add(new CompanyBuilder().Build());
        _context.Companies.Add(new CompanyBuilder().Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCompaniesAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.Count());
    }

    [Fact]
    public async Task GetAllCompaniesAsync_FiltersInactiveCompanies()
    {
        // Arrange
        _context.Companies.Add(new CompanyBuilder().WithIsActive(true).Build());
        _context.Companies.Add(new CompanyBuilder().WithIsActive(false).Build());
        _context.Companies.Add(new CompanyBuilder().WithIsActive(true).Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCompaniesAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count());
    }

    #endregion

    #region UpdateCompanyAsync Tests

    [Fact]
    public async Task UpdateCompanyAsync_WhenCompanyExists_ReturnsSuccess()
    {
        // Arrange
        var company = new CompanyBuilder().Build();
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var request = BuildValidUpdateRequest();

        // Act
        var result = await _service.UpdateCompanyAsync(company.Id, request, Guid.NewGuid());

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateCompanyAsync_WhenCompanyNotFound_ReturnsFailure()
    {
        // Arrange
        var request = BuildValidUpdateRequest();

        // Act
        var result = await _service.UpdateCompanyAsync(Guid.NewGuid(), request, Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company not found", result.Message);
    }

    #endregion

    #region DeactivateCompanyAsync Tests

    [Fact]
    public async Task DeactivateCompanyAsync_WhenCompanyExists_DeactivatesIt()
    {
        // Arrange
        var company = new CompanyBuilder().WithIsActive(true).Build();
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateCompanyAsync(company.Id, Guid.NewGuid());

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task DeactivateCompanyAsync_WhenCompanyDoesNotExist_ReturnsFailure()
    {
        // Act
        var result = await _service.DeactivateCompanyAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Company not found", result.Message);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        _httpClient.Dispose();
    }
}
