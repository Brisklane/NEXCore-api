using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for LedgerController
/// Tests all CRUD operations for ledger management
/// </summary>
public class LedgerControllerTests
{
    private readonly Mock<ILedgerService> _serviceMock;
    private readonly Mock<ILogger<LedgerController>> _loggerMock;
    private readonly LedgerController _controller;

    public LedgerControllerTests()
    {
        _serviceMock = new Mock<ILedgerService>();
        _loggerMock = new Mock<ILogger<LedgerController>>();
        _controller = new LedgerController(_serviceMock.Object, _loggerMock.Object);
        
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

    #region CreateLedger Tests

    [Fact]
    public async Task CreateLedger_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateLedgerDto
        {
            Name = "General Ledger",
            BaseCurrencyCode = "USD",
            IsDefault = true
        };

        var responseDto = new LedgerDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BaseCurrencyCode = request.BaseCurrencyCode,
            IsDefault = request.IsDefault
        };

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateLedgerDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateLedger(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.ActionName.Should().Be(nameof(LedgerController.GetLedgerById));
        createdResult.StatusCode.Should().Be(201);

        var response = createdResult.Value as ApiResponse<LedgerDto>;
        response?.Success.Should().BeTrue();
        response?.Message.Should().Contain("successfully");
    }

    [Fact]
    public async Task CreateLedger_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateLedgerDto { Name = "", BaseCurrencyCode = "USD" };

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateLedgerDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Ledger name is required"));

        // Act
        var result = await _controller.CreateLedger(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.StatusCode.Should().Be(400);

        var errorResponse = badRequestResult.Value as ApiErrorResponse;
        errorResponse?.Message.Should().Contain("Ledger name is required");
    }

    #endregion

    #region GetLedgerById Tests

    [Fact]
    public async Task GetLedgerById_WhenLedgerExists_ReturnsOkWithData()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var expectedDto = new LedgerDto
        {
            Id = ledgerId,
            Name = "General Ledger",
            BaseCurrencyCode = "USD"
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetLedgerById(ledgerId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<LedgerDto>;
        response?.Success.Should().BeTrue();
        response?.Data.Should().Be(expectedDto);
    }

    [Fact]
    public async Task GetLedgerById_WhenLedgerNotFound_ReturnsNotFound()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();

        _serviceMock
            .Setup(x => x.GetByIdAsync(ledgerId))
            .ReturnsAsync((LedgerDto?)null);

        // Act
        var result = await _controller.GetLedgerById(ledgerId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        notFoundResult.StatusCode.Should().Be(404);

        var errorResponse = notFoundResult.Value as ApiErrorResponse;
        errorResponse?.Message.Should().Contain("not found");
    }

    #endregion

    #region GetCompanyLedgers Tests

    [Fact]
    public async Task GetCompanyLedgers_WhenLedgersExist_ReturnsOkWithCollection()
    {
        // Arrange
        var ledgers = new List<LedgerDto>
        {
            new LedgerDto { Id = Guid.NewGuid(), Name = "GL", BaseCurrencyCode = "USD" },
            new LedgerDto { Id = Guid.NewGuid(), Name = "Cost Ledger", BaseCurrencyCode = "USD" }
        };

        _serviceMock
            .Setup(x => x.GetAllAsync(It.IsAny<PaginationParams>()))
            .ReturnsAsync(PaginatedResponse<LedgerDto>.Ok(ledgers, ledgers.Count, 1, 10));

        // Act
        var result = await _controller.GetCompanyLedgers(new PaginationParams());

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as PaginatedResponse<LedgerDto>;
        response?.Success.Should().BeTrue();
        response?.Data?.Count().Should().Be(2);
    }

    #endregion

    #region GetDefaultLedger Tests

    [Fact]
    public async Task GetDefaultLedger_WhenDefaultExists_ReturnsOkWithDefault()
    {
        // Arrange
        var defaultLedger = new LedgerDto
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            BaseCurrencyCode = "USD",
            IsDefault = true
        };

        _serviceMock
            .Setup(x => x.GetDefaultAsync())
            .ReturnsAsync(defaultLedger);

        // Act
        var result = await _controller.GetDefaultLedger();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<LedgerDto>;
        response?.Data?.IsDefault.Should().BeTrue();
    }

    #endregion

    #region UpdateLedger Tests

    [Fact]
    public async Task UpdateLedger_WhenValidRequest_ReturnsOkWithUpdatedData()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var updateRequest = new UpdateLedgerDto { Name = "Updated Ledger" };

        var updatedDto = new LedgerDto
        {
            Id = ledgerId,
            Name = "Updated Ledger",
            BaseCurrencyCode = "USD"
        };

        _serviceMock
            .Setup(x => x.UpdateAsync(ledgerId, It.IsAny<UpdateLedgerDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.UpdateLedger(ledgerId, updateRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<LedgerDto>;
        response?.Success.Should().BeTrue();
        response?.Message.Should().Contain("successfully");
    }

    #endregion

    #region DeleteLedger Tests

    [Fact]
    public async Task DeleteLedger_WhenValidId_ReturnsNoContent()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();

        _serviceMock
            .Setup(x => x.DeleteAsync(ledgerId, It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteLedger(ledgerId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var noContentResult = (NoContentResult)result;
        noContentResult.StatusCode.Should().Be(204);
    }

    #endregion
}
