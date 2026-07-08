using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Crm.Infrastructure.Services;

public class CaseService : ICaseService
{
    private readonly ICaseRepository _repository;
    private readonly ICaseCommentRepository _commentRepository;
    private readonly ILogger<CaseService> _logger;

    public CaseService(ICaseRepository repository, ICaseCommentRepository commentRepository, ILogger<CaseService> logger)
    {
        _repository = repository;
        _commentRepository = commentRepository;
        _logger = logger;
    }

    public async Task<CaseDto> CreateAsync(CreateCaseDto dto, Guid userId)
    {
        var count = await _repository.CountAsync() + 1;
        var entity = new Case
        {
            CaseNumber = $"CS-{1000 + count}",
            Status = "New",
            CaseOrigin = dto.CaseOrigin,
            Priority = dto.Priority,
            ContactId = dto.ContactId,
            AccountId = dto.AccountId,
            EntitlementId = dto.EntitlementId,
            Subject = dto.Subject,
            Description = dto.Description,
            SendNotificationEmail = dto.SendNotificationEmail,
            OwnerId = dto.OwnerId ?? userId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Case created: {CaseId} {CaseNumber}", entity.Id, entity.CaseNumber);
        return MapToDto(entity);
    }

    public async Task<CaseDto?> GetByIdAsync(Guid id)
    {
        var e = await _repository.GetByIdWithDetailsAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<PaginatedResponse<CaseDto>> GetPagedAsync(PaginationParams pagination, string? status = null)
    {
        var (items, total) = await _repository.GetPagedByStatusAsync(pagination.PageNumber, pagination.PageSize, status);
        return PaginatedResponse<CaseDto>.Ok(items.OrderByDescending(x => x.CreatedAt).Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IEnumerable<CaseDto>> GetByAccountIdAsync(Guid accountId)
    {
        var items = await _repository.GetByAccountIdAsync(accountId);
        return items.OrderByDescending(x => x.CreatedAt).Select(MapToDto);
    }

    public async Task<CaseDto> UpdateAsync(Guid id, UpdateCaseDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Case not found");
        entity.Status = dto.Status;
        entity.CaseOrigin = dto.CaseOrigin;
        entity.Priority = dto.Priority;
        entity.ContactId = dto.ContactId;
        entity.AccountId = dto.AccountId;
        entity.EntitlementId = dto.EntitlementId;
        entity.Subject = dto.Subject;
        entity.Description = dto.Description;
        entity.SendNotificationEmail = dto.SendNotificationEmail;
        entity.OwnerId = dto.OwnerId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Case not found");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task<CaseCommentDto> AddCommentAsync(Guid caseId, CreateCaseCommentDto dto, Guid userId)
    {
        _ = await _repository.GetByIdAsync(caseId)
            ?? throw new InvalidOperationException("Case not found");
        var entity = new CaseComment
        {
            CaseId = caseId,
            CommentBody = dto.CommentBody,
            IsPublished = dto.IsPublished,
            IsInternal = dto.IsInternal,
            AuthorId = userId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _commentRepository.AddAsync(entity);
        await _commentRepository.SaveChangesAsync();
        return MapCommentDto(entity);
    }

    public async Task<IEnumerable<CaseCommentDto>> GetCommentsAsync(Guid caseId)
    {
        var items = await _commentRepository.GetByCaseIdAsync(caseId);
        return items.OrderBy(x => x.CreatedAt).Select(MapCommentDto);
    }

    public async Task DeleteCommentAsync(Guid caseId, Guid commentId, Guid userId)
    {
        var entity = await _commentRepository.GetByIdAsync(commentId)
            ?? throw new InvalidOperationException("Comment not found");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        _commentRepository.Update(entity);
        await _commentRepository.SaveChangesAsync();
    }

    private static CaseDto MapToDto(Case e) => new()
    {
        Id = e.Id, CaseNumber = e.CaseNumber, Status = e.Status,
        CaseOrigin = e.CaseOrigin, Priority = e.Priority,
        ContactId = e.ContactId,
        ContactName = e.Contact is null ? null : $"{e.Contact.FirstName} {e.Contact.LastName}",
        AccountId = e.AccountId, AccountName = e.Account?.AccountName,
        EntitlementId = e.EntitlementId, Subject = e.Subject,
        Description = e.Description, SendNotificationEmail = e.SendNotificationEmail,
        OwnerId = e.OwnerId, CreatedAt = e.CreatedAt
    };

    private static CaseCommentDto MapCommentDto(CaseComment e) => new()
    {
        Id = e.Id, CaseId = e.CaseId, CommentBody = e.CommentBody,
        IsPublished = e.IsPublished, IsInternal = e.IsInternal,
        AuthorId = e.AuthorId, IsCustomerComment = e.IsCustomerComment, CreatedAt = e.CreatedAt
    };
}
