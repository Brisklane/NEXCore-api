using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for AccountBalanceController
/// Tests account balance operations
/// </summary>
public class AccountBalanceControllerTests
{
    private readonly Mock<IAccountBalanceService> _serviceMock;
    private readonly Mock<ILogger<AccountBalanceController>> _loggerMock;
    private readonly AccountBalanceController _controller;

    public AccountBalanceControllerTests()
    {
        _serviceMock = new Mock<IAccountBalanceService>();
        _loggerMock = new Mock<ILogger<AccountBalanceController>>();
        _controller = new AccountBalanceController(_serviceMock.Object, _loggerMock.Object);

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
    public async Task CreateAccountBalance_WhenValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateAccountBalanceDto
        {
            LedgerAccountId = Guid.NewGuid(),
            FiscalPeriodId = Guid.NewGuid(),
            Amount = 1000.00m
        };

        var responseDto = new AccountBalanceDto
        {
            Id = Guid.NewGuid(),
            LedgerAccountId = request.LedgerAccountId,
            FiscalPeriodId = request.FiscalPeriodId
        };

        _serviceMock
            .Setup(x => x.CreateAsync(It.IsAny<CreateAccountBalanceDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateAccountBalance(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = (CreatedAtActionResult)result;
        createdResult.StatusCode.Should().Be(201);
    }
}
