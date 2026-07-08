using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Moq;
using System.Linq.Expressions;

namespace Accounting.Tests.Unit.Services;

/// <summary>
/// Unit tests for AccountBalanceService
/// Tests account balance calculations and reporting
/// </summary>
public class AccountBalanceServiceTests
{
    private readonly Mock<IAccountBalanceRepository> _repositoryMock;
    private readonly Mock<ILogger<AccountBalanceService>> _loggerMock;
    private readonly IAccountBalanceService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public AccountBalanceServiceTests()
    {
        _repositoryMock = new Mock<IAccountBalanceRepository>();
        _loggerMock = new Mock<ILogger<AccountBalanceService>>();
        _service = new AccountBalanceService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesBalance()
    {
        // Arrange
        var request = new CreateAccountBalanceDto
        {
            LedgerAccountId = Guid.NewGuid(),
            FiscalPeriodId = Guid.NewGuid(),
            Amount = 1000.00m
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<AccountBalance>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<AccountBalance>()), Times.Once);
    }

    #endregion

    #region GetByPeriodAsync Tests

    [Fact]
    public async Task GetByPeriodAsync_WhenBalancesExist_ReturnsBalances()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var balances = new List<AccountBalance>
        {
            new AccountBalance
            {
                Id = Guid.NewGuid(),
                LedgerAccountId = Guid.NewGuid(),
                FiscalPeriodId = periodId
            }
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<AccountBalance, bool>>>(), It.IsAny<Func<IQueryable<AccountBalance>, IOrderedQueryable<AccountBalance>>>(), It.IsAny<Func<IQueryable<AccountBalance>, IQueryable<AccountBalance>>>()))
            .ReturnsAsync((balances.AsEnumerable(), balances.Count));

        // Act
        var results = await _service.GetByPeriodAsync(periodId, new PaginationParams());

        // Assert
        results.Data.Should().HaveCount(1);
    }

    #endregion
}
