using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Accounting.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for TaxCodeController
/// Tests tax code operations
/// </summary>
public class TaxCodeControllerTests
{
    private readonly Mock<ITaxCodeService> _serviceMock;
    private readonly Mock<ILogger<TaxCodeController>> _loggerMock;
    private readonly TaxCodeController _controller;

    public TaxCodeControllerTests()
    {
        _serviceMock = new Mock<ITaxCodeService>();
        _loggerMock = new Mock<ILogger<TaxCodeController>>();
        _controller = new TaxCodeController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllTaxCodes_WhenCodesExist_ReturnsOkWithCollection()
    {
        // Arrange
        var codes = new List<TaxCodeDto>
        {
            new TaxCodeDto { Id = Guid.NewGuid(), Code = "VAT15", Name = "VAT 15%" },
            new TaxCodeDto { Id = Guid.NewGuid(), Code = "VAT20", Name = "VAT 20%" }
        };

        _serviceMock
            .Setup(x => x.GetAllAsync(It.IsAny<PaginationParams>()))
            .ReturnsAsync(PaginatedResponse<TaxCodeDto>.Ok(codes, codes.Count, 1, 10));

        // Act
        var result = await _controller.GetAllTaxCodes(new PaginationParams());

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value as PaginatedResponse<TaxCodeDto>;
        response?.Data?.Count().Should().Be(2);
    }
}
