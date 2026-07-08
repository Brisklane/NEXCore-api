using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for AccountCategoryController
/// Tests account category operations
/// </summary>
public class AccountCategoryControllerTests
{
    private readonly Mock<IAccountCategoryService> _serviceMock;
    private readonly Mock<ILogger<AccountCategoryController>> _loggerMock;
    private readonly AccountCategoryController _controller;

    public AccountCategoryControllerTests()
    {
        _serviceMock = new Mock<IAccountCategoryService>();
        _loggerMock = new Mock<ILogger<AccountCategoryController>>();
        _controller = new AccountCategoryController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllCategories_WhenCategoriesExist_ReturnsOkWithCollection()
    {
        // Arrange
        var categories = new List<AccountCategoryDto>
        {
            new AccountCategoryDto { Id = Guid.NewGuid(), Name = "Asset", Type = "Asset", NormalBalance = "Debit" },
            new AccountCategoryDto { Id = Guid.NewGuid(), Name = "Liability", Type = "Liability", NormalBalance = "Credit" }
        };

        _serviceMock
            .Setup(x => x.GetActiveAsync(It.IsAny<PaginationParams>()))
            .ReturnsAsync(PaginatedResponse<AccountCategoryDto>.Ok(categories, categories.Count, 1, 10));

        // Act
        var result = await _controller.GetAllCategories(new PaginationParams());

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as PaginatedResponse<AccountCategoryDto>;
        response?.Data?.Count().Should().Be(2);
    }
}
