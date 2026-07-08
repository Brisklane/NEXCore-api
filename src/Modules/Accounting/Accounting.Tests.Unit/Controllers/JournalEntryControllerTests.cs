using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for JournalEntryController
/// Tests journal entry operations
/// </summary>
public class JournalEntryControllerTests
{
    private readonly Mock<IJournalEntryService> _serviceMock;
    private readonly Mock<ILogger<JournalEntryController>> _loggerMock;
    private readonly JournalEntryController _controller;

    public JournalEntryControllerTests()
    {
        _serviceMock = new Mock<IJournalEntryService>();
        _loggerMock = new Mock<ILogger<JournalEntryController>>();
        _controller = new JournalEntryController(_serviceMock.Object, _loggerMock.Object);
        
        // Setup user with claims
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("company_id", Guid.NewGuid().ToString()),
            new Claim("branch_id", Guid.NewGuid().ToString()),
            new Claim("business_unit_id", Guid.NewGuid().ToString())
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task CreateJournalEntry_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateJournalEntryDto
        {
            DocumentType = "JE",
            Description = "Test Entry",
            CurrencyCode = "USD"
        };

        var responseDto = new JournalEntryDto
        {
            Id = Guid.NewGuid(),
            JournalNumber = "JE001",
            Description = request.Description,
            DocumentType = "JE",
            CurrencyCode = "USD",
            Status = "Draft"
        };

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateJournalEntryDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateJournalEntry(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task GetJournalEntryById_WhenEntryExists_ReturnsOkWithData()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var expectedDto = new JournalEntryDto
        {
            Id = entryId,
            JournalNumber = "JE001",
            Description = "Test Entry",
            DocumentType = "JE",
            CurrencyCode = "USD",
            Status = "Draft"
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(entryId))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetJournalEntryById(entryId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<JournalEntryDto>;
        response?.Data.Should().Be(expectedDto);
    }
}
