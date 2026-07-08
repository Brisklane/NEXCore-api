using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Services;
using Nexcore.SharedKernel.Api;
using System.Linq.Expressions;

namespace Accounting.Tests.Unit.Services;

/// <summary>
/// Unit tests for PostingProfileService
/// Tests posting profile management and GL account mapping
/// </summary>
public class PostingProfileServiceTests
{
    private readonly Mock<IPostingProfileRepository> _repositoryMock;
    private readonly Mock<ILogger<PostingProfileService>> _loggerMock;
    private readonly IPostingProfileService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public PostingProfileServiceTests()
    {
        _repositoryMock = new Mock<IPostingProfileRepository>();
        _loggerMock = new Mock<ILogger<PostingProfileService>>();
        _service = new PostingProfileService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesProfile()
    {
        // Arrange
        var request = new CreatePostingProfileDto
        {
            ModuleName = "Inventory",
            TransactionType = "PurchaseOrder",
            DebitAccountId = Guid.NewGuid(),
            CreditAccountId = Guid.NewGuid()
        };

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostingProfile>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request, _userId);

        // Assert
        result.Should().NotBeNull();
        result.ModuleName.Should().Be("Inventory");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenProfilesExist_ReturnsAll()
    {
        // Arrange
        var profiles = new List<PostingProfile>
        {
            new PostingProfile
            {
                Id = Guid.NewGuid(),
                ModuleName = "Inventory",
                TransactionType = "PO",
                DebitAccountId = Guid.NewGuid(),
                CreditAccountId = Guid.NewGuid()
            }
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<PostingProfile, bool>>>(), It.IsAny<Func<IQueryable<PostingProfile>, IOrderedQueryable<PostingProfile>>>(), It.IsAny<Func<IQueryable<PostingProfile>, IQueryable<PostingProfile>>>()))
            .ReturnsAsync((profiles.AsEnumerable(), profiles.Count));

        // Act
        var results = await _service.GetAllAsync(new PaginationParams());

        // Assert
        results.Data.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    #endregion
}
