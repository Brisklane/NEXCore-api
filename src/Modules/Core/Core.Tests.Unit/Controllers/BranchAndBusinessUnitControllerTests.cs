using Core.Api.Controllers;
using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;
using System.Security.Claims;

namespace Core.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for BranchController
/// Tests all branch management endpoints with various scenarios
/// </summary>
public class BranchControllerTests
{
    private readonly Mock<IBranchService> _branchServiceMock;
    private readonly Mock<ILogger<BranchController>> _loggerMock;
    private readonly BranchController _controller;

    public BranchControllerTests()
    {
        _branchServiceMock = new Mock<IBranchService>();
        _loggerMock = new Mock<ILogger<BranchController>>();

        _controller = new BranchController(
            _branchServiceMock.Object,
            _loggerMock.Object
        );
    }

    #region CreateBranch Endpoint Tests

    [Fact]
    public async Task CreateBranch_WithValidData_ReturnsCreatedResponse()
    {
        // Arrange
        var request = new CreateBranchRequestDto
        {
            CompanyId = Guid.NewGuid(),
            Code = "BR001",
            Name = "Main Branch",
            City = "New York",
            State = "NY",
            StreetAddress = "123 Main St",
            PostalCode = "10001"
        };

        var branchDto = new BranchResponseDto
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            CompanyId = request.CompanyId,
            City = request.City,
            State = request.State,
            StreetAddress = request.StreetAddress,
            PostalCode = request.PostalCode
        };

        var result = Result<BranchResponseDto>.Ok(branchDto, "Branch created successfully");

        _branchServiceMock.Setup(x => x.CreateBranchAsync(It.IsAny<CreateBranchRequestDto>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CreateBranch(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(response);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateBranch_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBranchRequestDto
        {
            CompanyId = Guid.Empty,
            Code = "",
            Name = ""
        };

        var result = Result<BranchResponseDto>.Fail("Invalid branch data");

        _branchServiceMock.Setup(x => x.CreateBranchAsync(It.IsAny<CreateBranchRequestDto>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CreateBranch(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateBranch_WithException_Returns500Error()
    {
        // Arrange
        var request = new CreateBranchRequestDto
        {
            CompanyId = Guid.NewGuid(),
            Code = "BR001",
            Name = "Main Branch",
            City = "New York",
            State = "NY",
            StreetAddress = "123 Main St",
            PostalCode = "10001"
        };

        _branchServiceMock.Setup(x => x.CreateBranchAsync(It.IsAny<CreateBranchRequestDto>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var response = await _controller.CreateBranch(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    #endregion

    #region GetBranch Endpoint Tests

    [Fact]
    public async Task GetBranch_WithValidId_ReturnsOkWithBranch()
    {
        // Arrange
        var branchId = Guid.NewGuid();
        var branchDto = new BranchResponseDto
        {
            Id = branchId,
            Code = "BR001",
            Name = "Main Branch",
            CompanyId = Guid.NewGuid(),
            City = "New York",
            State = "NY",
            StreetAddress = "123 Main St",
            PostalCode = "10001"
        };

        var result = Result<BranchResponseDto>.Ok(branchDto);

        _branchServiceMock.Setup(x => x.GetBranchByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetBranch(branchId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GetBranch_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        var branchId = Guid.NewGuid();
        var result = Result<BranchResponseDto>.Fail("Branch not found");

        _branchServiceMock.Setup(x => x.GetBranchByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetBranch(branchId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region GetAllBranches Endpoint Tests

    [Fact]
    public async Task GetAllBranches_WithNoFilter_ReturnsOkWithList()
    {
        // Arrange
        var branches = new List<BranchResponseDto>
        {
            new BranchResponseDto 
            { 
                Id = Guid.NewGuid(), Code = "BR001", Name = "Branch 1", CompanyId = Guid.NewGuid(),
                City = "NY", State = "NY", StreetAddress = "123 St", PostalCode = "10001"
            },
            new BranchResponseDto 
            { 
                Id = Guid.NewGuid(), Code = "BR002", Name = "Branch 2", CompanyId = Guid.NewGuid(),
                City = "LA", State = "CA", StreetAddress = "456 St", PostalCode = "90001"
            }
        };

        var result = Result<IEnumerable<BranchResponseDto>>.Ok((IEnumerable<BranchResponseDto>)branches);

        _branchServiceMock.Setup(x => x.GetAllBranchesAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetAllBranches();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GetAllBranches_WithCompanyFilter_ReturnsFilteredList()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var branches = new List<BranchResponseDto>
        {
            new BranchResponseDto 
            { 
                Id = Guid.NewGuid(), Code = "BR001", Name = "Branch 1", CompanyId = companyId,
                City = "NY", State = "NY", StreetAddress = "123 St", PostalCode = "10001"
            }
        };

        var result = Result<IEnumerable<BranchResponseDto>>.Ok((IEnumerable<BranchResponseDto>)branches);

        _branchServiceMock.Setup(x => x.GetAllBranchesAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetAllBranches(companyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    #endregion

    #region UpdateBranch Endpoint Tests

    [Fact]
    public async Task UpdateBranch_WithValidData_ReturnsOkWithUpdatedBranch()
    {
        // Arrange
        var branchId = Guid.NewGuid();
        var request = new UpdateBranchRequestDto
        {
            Name = "Updated Branch Name"
        };

        var updatedBranch = new BranchResponseDto
        {
            Id = branchId,
            Code = "BR001",
            Name = request.Name,
            CompanyId = Guid.NewGuid(),
            City = "NY",
            State = "NY",
            StreetAddress = "123 St",
            PostalCode = "10001"
        };

        var result = Result<BranchResponseDto>.Ok(updatedBranch, "Branch updated successfully");

        _branchServiceMock.Setup(x => x.UpdateBranchAsync(It.IsAny<Guid>(), It.IsAny<UpdateBranchRequestDto>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.UpdateBranch(branchId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task UpdateBranch_WithNonexistentId_ReturnsBadRequest()
    {
        // Arrange
        var branchId = Guid.NewGuid();
        var request = new UpdateBranchRequestDto
        {
            Name = "Updated Branch Name"
        };

        var result = Result<BranchResponseDto>.Fail("Branch not found");

        _branchServiceMock.Setup(x => x.UpdateBranchAsync(It.IsAny<Guid>(), It.IsAny<UpdateBranchRequestDto>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.UpdateBranch(branchId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion

    #region DeactivateBranch Endpoint Tests

    [Fact]
    public async Task DeactivateBranch_WithValidId_ReturnsOk()
    {
        // Arrange
        var branchId = Guid.NewGuid();
        var result = Result.Ok("Branch deactivated successfully");

        _branchServiceMock.Setup(x => x.DeactivateBranchAsync(It.IsAny<Guid>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.DeactivateBranch(branchId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task DeactivateBranch_WithNonexistentId_ReturnsBadRequest()
    {
        // Arrange
        var branchId = Guid.NewGuid();
        var result = Result.Fail("Branch not found");

        _branchServiceMock.Setup(x => x.DeactivateBranchAsync(It.IsAny<Guid>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.DeactivateBranch(branchId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion
}

/// <summary>
/// Unit tests for BusinessUnitController
/// Tests all business unit management endpoints with various scenarios
/// </summary>
public class BusinessUnitControllerTests
{
    private readonly Mock<IBusinessUnitService> _businessUnitServiceMock;
    private readonly Mock<ILogger<BusinessUnitController>> _loggerMock;
    private readonly BusinessUnitController _controller;

    public BusinessUnitControllerTests()
    {
        _businessUnitServiceMock = new Mock<IBusinessUnitService>();
        _loggerMock = new Mock<ILogger<BusinessUnitController>>();

        _controller = new BusinessUnitController(
            _businessUnitServiceMock.Object,
            _loggerMock.Object
        );
    }

    #region CreateBusinessUnit Endpoint Tests

    [Fact]
    public async Task CreateBusinessUnit_WithValidData_ReturnsCreatedResponse()
    {
        // Arrange
        var request = new CreateBusinessUnitRequestDto
        {
            CompanyId = Guid.NewGuid(),
            Code = "BU001",
            Name = "Sales Unit"
        };

        var unitDto = new BusinessUnitResponseDto
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            CompanyId = request.CompanyId
        };

        var result = Result<BusinessUnitResponseDto>.Ok(unitDto, "Business unit created successfully");

        _businessUnitServiceMock.Setup(x => x.CreateBusinessUnitAsync(It.IsAny<CreateBusinessUnitRequestDto>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CreateBusinessUnit(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(response);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateBusinessUnit_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBusinessUnitRequestDto
        {
            CompanyId = Guid.Empty,
            Code = "",
            Name = ""
        };

        var result = Result<BusinessUnitResponseDto>.Fail("Invalid business unit data");

        _businessUnitServiceMock.Setup(x => x.CreateBusinessUnitAsync(It.IsAny<CreateBusinessUnitRequestDto>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.CreateBusinessUnit(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion

    #region GetBusinessUnit Endpoint Tests

    [Fact]
    public async Task GetBusinessUnit_WithValidId_ReturnsOkWithUnit()
    {
        // Arrange
        var unitId = Guid.NewGuid();
        var unitDto = new BusinessUnitResponseDto
        {
            Id = unitId,
            Code = "BU001",
            Name = "Sales Unit",
            CompanyId = Guid.NewGuid()
        };

        var result = Result<BusinessUnitResponseDto>.Ok(unitDto);

        _businessUnitServiceMock.Setup(x => x.GetBusinessUnitByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetBusinessUnit(unitId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GetBusinessUnit_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        var unitId = Guid.NewGuid();
        var result = Result<BusinessUnitResponseDto>.Fail("Business unit not found");

        _businessUnitServiceMock.Setup(x => x.GetBusinessUnitByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetBusinessUnit(unitId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region GetAllBusinessUnits Endpoint Tests

    [Fact]
    public async Task GetAllBusinessUnits_WithNoFilter_ReturnsOkWithList()
    {
        // Arrange
        var units = new List<BusinessUnitResponseDto>
        {
            new BusinessUnitResponseDto { Id = Guid.NewGuid(), Code = "BU001", Name = "Unit 1", CompanyId = Guid.NewGuid() },
            new BusinessUnitResponseDto { Id = Guid.NewGuid(), Code = "BU002", Name = "Unit 2", CompanyId = Guid.NewGuid() }
        };

        var result = Result<IEnumerable<BusinessUnitResponseDto>>.Ok((IEnumerable<BusinessUnitResponseDto>)units);

        _businessUnitServiceMock.Setup(x => x.GetAllBusinessUnitsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.GetAllBusinessUnits();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    #endregion

    #region UpdateBusinessUnit Endpoint Tests

    [Fact]
    public async Task UpdateBusinessUnit_WithValidData_ReturnsOkWithUpdatedUnit()
    {
        // Arrange
        var unitId = Guid.NewGuid();
        var request = new UpdateBusinessUnitRequestDto
        {
            Name = "Updated Unit Name"
        };

        var updatedUnit = new BusinessUnitResponseDto
        {
            Id = unitId,
            Code = "BU001",
            Name = request.Name,
            CompanyId = Guid.NewGuid()
        };

        var result = Result<BusinessUnitResponseDto>.Ok(updatedUnit, "Business unit updated successfully");

        _businessUnitServiceMock.Setup(x => x.UpdateBusinessUnitAsync(It.IsAny<Guid>(), It.IsAny<UpdateBusinessUnitRequestDto>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.UpdateBusinessUnit(unitId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    #endregion

    #region DeactivateBusinessUnit Endpoint Tests

    [Fact]
    public async Task DeactivateBusinessUnit_WithValidId_ReturnsOk()
    {
        // Arrange
        var unitId = Guid.NewGuid();
        var result = Result.Ok("Business unit deactivated successfully");

        _businessUnitServiceMock.Setup(x => x.DeactivateBusinessUnitAsync(It.IsAny<Guid>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.DeactivateBusinessUnit(unitId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    #endregion
}
