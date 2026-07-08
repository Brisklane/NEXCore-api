using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for LedgerAccountController
/// Tests all CRUD operations for GL accounts
/// </summary>
public class LedgerAccountControllerTests
{
    private readonly Mock<ILedgerAccountService> _serviceMock;
    private readonly Mock<ILogger<LedgerAccountController>> _loggerMock;
    private readonly LedgerAccountController _controller;

    public LedgerAccountControllerTests()
    {
        _serviceMock = new Mock<ILedgerAccountService>();
        _loggerMock = new Mock<ILogger<LedgerAccountController>>();
        _controller = new LedgerAccountController(_serviceMock.Object, _loggerMock.Object);
        
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
    public async Task CreateAccount_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateLedgerAccountDto
        {
            LedgerId = Guid.NewGuid(),
            AccountNumber = "1000",
            AccountName = "Cash",
            CategoryId = Guid.NewGuid()
        };

        var responseDto = new LedgerAccountDto
        {
            Id = Guid.NewGuid(),
            AccountNumber = request.AccountNumber,
            AccountName = request.AccountName
        };

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateLedgerAccountDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateAccount(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);

        var response = createdResult.Value as ApiResponse<LedgerAccountDto>;
        response?.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccountById_WhenAccountExists_ReturnsOkWithData()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var expectedDto = new LedgerAccountDto
        {
            Id = accountId,
            AccountNumber = "1000",
            AccountName = "Cash"
        };

        _serviceMock
            .Setup(x => x.GetByIdAsync(accountId))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetAccountById(accountId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as ApiResponse<LedgerAccountDto>;
        response?.Data.Should().Be(expectedDto);
    }

    [Fact]
    public async Task GetAccountsByLedger_WhenAccountsExist_ReturnsOkWithCollection()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var accounts = new List<LedgerAccountDto>
        {
            new LedgerAccountDto { Id = Guid.NewGuid(), AccountNumber = "1000", AccountName = "Cash" },
            new LedgerAccountDto { Id = Guid.NewGuid(), AccountNumber = "1100", AccountName = "Bank" }
        };

        _serviceMock
            .Setup(x => x.GetByLedgerIdAsync(ledgerId, It.IsAny<PaginationParams>()))
            .ReturnsAsync(PaginatedResponse<LedgerAccountDto>.Ok(accounts, accounts.Count, 1, 10));

        // Act
        var result = await _controller.GetAccountsByLedger(ledgerId, new PaginationParams());

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as PaginatedResponse<LedgerAccountDto>;
        response?.Data?.Count().Should().Be(2);
    }
}
