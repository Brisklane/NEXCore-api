using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of posting profile service
/// Maps modules and transaction types to GL accounts
/// Coordinates between controllers and repositories
/// </summary>
public class PostingProfileService : IPostingProfileService
{
    private readonly IPostingProfileRepository _repository;
    private readonly ILogger<PostingProfileService> _logger;

    public PostingProfileService(IPostingProfileRepository repository, ILogger<PostingProfileService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<PostingProfileDto>> GetAllAsync(PaginationParams pagination)
    {
        try
        {
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                orderBy: q => q.OrderBy(p => p.ModuleName).ThenBy(p => p.TransactionType));
            return PaginatedResponse<PostingProfileDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posting profiles");
            throw;
        }
    }

    public async Task<PostingProfileDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var profile = await _repository.GetByIdAsync(id);
            if (profile == null || profile.IsDeleted)
                return null;

            return MapToDto(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posting profile: {ProfileId}", id);
            throw;
        }
    }

    public async Task<PostingProfileDto?> GetByTypeAsync(string moduleName, string transactionType)
    {
        try
        {
            var profile = await _repository.GetByTypeAsync(moduleName, transactionType);
            if (profile == null || profile.IsDeleted)
                return null;

            return MapToDto(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posting profile: {ModuleName}/{TransactionType}", 
                moduleName, transactionType);
            throw;
        }
    }

    public async Task<IEnumerable<PostingProfileDto>> GetByModuleAsync(string moduleName)
    {
        try
        {
            var profiles = await _repository.GetByModuleAsync(moduleName);
            return profiles.Where(p => !p.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posting profiles for module: {ModuleName}", moduleName);
            throw;
        }
    }

    public async Task<PostingProfileDto> CreateAsync(CreatePostingProfileDto request, Guid userId)
    {
        try
        {
            var profile = new PostingProfile
            {
                ModuleName = request.ModuleName,
                TransactionType = request.TransactionType,
                DebitAccountId = request.DebitAccountId,
                CreditAccountId = request.CreditAccountId,
                TaxAccountId = request.TaxAccountId,
                IsActive = true,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(profile);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Posting profile created: {ModuleName}/{TransactionType}", 
                profile.ModuleName, profile.TransactionType);

            return MapToDto(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating posting profile");
            throw;
        }
    }

    public async Task<PostingProfileDto> UpdateAsync(Guid id, UpdatePostingProfileDto request, Guid userId)
    {
        try
        {
            var profile = await _repository.GetByIdAsync(id);
            if (profile == null || profile.IsDeleted)
                throw new InvalidOperationException("Posting profile not found");

            if (!string.IsNullOrWhiteSpace(request.ModuleName))
                profile.ModuleName = request.ModuleName;

            if (!string.IsNullOrWhiteSpace(request.TransactionType))
                profile.TransactionType = request.TransactionType;

            if (request.DebitAccountId.HasValue)
                profile.DebitAccountId = request.DebitAccountId.Value;

            if (request.CreditAccountId.HasValue)
                profile.CreditAccountId = request.CreditAccountId.Value;

            if (request.TaxAccountId.HasValue)
                profile.TaxAccountId = request.TaxAccountId.Value;

            if (request.Description != null)
                profile.Description = request.Description;

            profile.UpdatedAt = DateTime.UtcNow;
            profile.UpdatedByUserId = userId;

            _repository.Update(profile);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Posting profile updated: {ModuleName}/{TransactionType}", 
                profile.ModuleName, profile.TransactionType);

            return MapToDto(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating posting profile: {ProfileId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var profile = await _repository.GetByIdAsync(id);
            if (profile == null || profile.IsDeleted)
                throw new InvalidOperationException("Posting profile not found");

            profile.IsDeleted = true;
            profile.DeletedAt = DateTime.UtcNow;
            profile.DeletedByUserId = userId;

            _repository.Update(profile);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Posting profile deleted: {ModuleName}/{TransactionType}", 
                profile.ModuleName, profile.TransactionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting posting profile: {ProfileId}", id);
            throw;
        }
    }

    private static PostingProfileDto MapToDto(PostingProfile profile)
    {
        return new PostingProfileDto
        {
            Id = profile.Id,
            CompanyId = profile.CompanyId,
            ModuleName = profile.ModuleName,
            TransactionType = profile.TransactionType,
            DebitAccountId = profile.DebitAccountId,
            CreditAccountId = profile.CreditAccountId,
            TaxAccountId = profile.TaxAccountId,
            IsActive = profile.IsActive,
            Description = profile.Description
        };
    }
}
