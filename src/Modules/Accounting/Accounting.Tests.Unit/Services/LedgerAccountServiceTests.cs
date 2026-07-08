using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Services;
using System.Linq.Expressions;

namespace Accounting.Tests.Unit.Services;

/// <summary>
/// Unit tests for LedgerAccountService
/// Tests chart of accounts management
/// </summary>
public class LedgerAccountServiceTests
{
    private readonly Mock<ILedgerAccountRepository> _repositoryMock;
    private readonly Mock<ILogger<LedgerAccountService>> _loggerMock;
    private readonly ILedgerAccountService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public LedgerAccountServiceTests()
    {
        _repositoryMock = new Mock<ILedgerAccountRepository>();
        _loggerMock = new Mock<ILogger<LedgerAccountService>>();
        _service = new LedgerAccountService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesAccount()
    {
        // Arrange
        var request = new CreateLedgerAccountDto
        {
            LedgerId = Guid.NewGuid(),
            AccountNumber = "1000",
            AccountName = "Cash",
            CategoryId = Guid.NewGuid()
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<LedgerAccount>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        result.AccountNumber.Should().Be("1000");
        result.AccountName.Should().Be("Cash");
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<LedgerAccount>()), Times.Once);
    }

    #endregion

    #region GetByAccountNumberAsync Tests

    [Fact]
    public async Task GetByAccountNumberAsync_WhenAccountExists_ReturnsAccount()
    {
        // Arrange
        var accountNumber = "1000";
        var account = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            AccountName = "Cash"
        };

        _repositoryMock
            .Setup(x => x.GetByAccountNumberAsync(accountNumber))
            .ReturnsAsync(account);

        // Act
        var result = await _service.GetByAccountNumberAsync(accountNumber);

        // Assert
        result.Should().NotBeNull();
        result?.AccountNumber.Should().Be(accountNumber);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenAccountExists_ReturnsAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new LedgerAccount
        {
            Id = accountId,
            AccountNumber = "1000",
            AccountName = "Cash"
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _service.GetByIdAsync(accountId);

        // Assert
        result.Should().NotBeNull();
        result?.AccountName.Should().Be("Cash");
    }

    #endregion
}
