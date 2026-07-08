using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;

namespace Procurement.Infrastructure.Services;

// ── Vendor Category ───────────────────────────────────────────────────────────

public class VendorCategoryService : IVendorCategoryService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<VendorCategoryService> _logger;

    public VendorCategoryService(ProcurementDbContext ctx, ILogger<VendorCategoryService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task<List<VendorCategoryDto>> GetAllAsync()
        => await _ctx.VendorCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();

    public async Task<List<VendorCategoryDto>> GetActiveAsync()
        => await _ctx.VendorCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();

    public async Task<VendorCategoryDto?> GetByIdAsync(Guid id)
    {
        var entity = await _ctx.VendorCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<VendorCategoryDto> CreateAsync(CreateVendorCategoryDto dto)
    {
        var exists = await _ctx.VendorCategories
            .AnyAsync(c => c.Code == dto.Code && !c.IsDeleted);
        if (exists)
            throw new InvalidOperationException($"Vendor category with code '{dto.Code}' already exists");

        var entity = new VendorCategory
        {
            Code        = dto.Code,
            Name        = dto.Name,
            Description = dto.Description,
            IsActive    = true
        };

        _ctx.VendorCategories.Add(entity);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created VendorCategory {Code}", entity.Code);
        return MapToDto(entity);
    }

    public async Task<VendorCategoryDto> UpdateAsync(Guid id, UpdateVendorCategoryDto dto)
    {
        var entity = await _ctx.VendorCategories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorCategory {id} not found");

        if (dto.Code is not null)        entity.Code        = dto.Code;
        if (dto.Name is not null)        entity.Name        = dto.Name;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.IsActive.HasValue)       entity.IsActive    = dto.IsActive.Value;

        _ctx.VendorCategories.Update(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.VendorCategories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorCategory {id} not found");

        entity.IsDeleted  = true;
        entity.DeletedAt  = DateTime.UtcNow;
        _ctx.VendorCategories.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    private static VendorCategoryDto MapToDto(VendorCategory c) => new()
    {
        Id          = c.Id,
        Code        = c.Code,
        Name        = c.Name,
        Description = c.Description,
        IsActive    = c.IsActive
    };
}

// ── Procurement Category ──────────────────────────────────────────────────────

public class ProcurementCategoryService : IProcurementCategoryService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<ProcurementCategoryService> _logger;

    public ProcurementCategoryService(ProcurementDbContext ctx, ILogger<ProcurementCategoryService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task<List<ProcurementCategoryDto>> GetAllAsync()
        => await _ctx.ProcurementCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();

    public async Task<List<ProcurementCategoryDto>> GetActiveAsync()
        => await _ctx.ProcurementCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();

    public async Task<List<ProcurementCategoryDto>> GetTreeAsync()
    {
        var all = await _ctx.ProcurementCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var dict = all.ToDictionary(c => c.Id, c => MapToDto(c));

        foreach (var item in all)
        {
            if (item.ParentCategoryId.HasValue && dict.TryGetValue(item.ParentCategoryId.Value, out var parent))
                parent.Children.Add(dict[item.Id]);
        }

        return dict.Values.Where(c => !c.ParentCategoryId.HasValue).ToList();
    }

    public async Task<ProcurementCategoryDto?> GetByIdAsync(Guid id)
    {
        var entity = await _ctx.ProcurementCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<ProcurementCategoryDto> CreateAsync(CreateProcurementCategoryDto dto)
    {
        var exists = await _ctx.ProcurementCategories
            .AnyAsync(c => c.Code == dto.Code && !c.IsDeleted);
        if (exists)
            throw new InvalidOperationException($"Procurement category with code '{dto.Code}' already exists");

        var entity = new ProcurementCategory
        {
            Code             = dto.Code,
            Name             = dto.Name,
            Description      = dto.Description,
            ParentCategoryId = dto.ParentCategoryId,
            IsActive         = true
        };

        _ctx.ProcurementCategories.Add(entity);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created ProcurementCategory {Code}", entity.Code);
        return MapToDto(entity);
    }

    public async Task<ProcurementCategoryDto> UpdateAsync(Guid id, UpdateProcurementCategoryDto dto)
    {
        var entity = await _ctx.ProcurementCategories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"ProcurementCategory {id} not found");

        if (dto.Code is not null)              entity.Code             = dto.Code;
        if (dto.Name is not null)              entity.Name             = dto.Name;
        if (dto.Description is not null)       entity.Description      = dto.Description;
        if (dto.ParentCategoryId.HasValue)     entity.ParentCategoryId = dto.ParentCategoryId;
        if (dto.IsActive.HasValue)             entity.IsActive         = dto.IsActive.Value;

        _ctx.ProcurementCategories.Update(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.ProcurementCategories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"ProcurementCategory {id} not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _ctx.ProcurementCategories.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    private static ProcurementCategoryDto MapToDto(ProcurementCategory c) => new()
    {
        Id               = c.Id,
        Code             = c.Code,
        Name             = c.Name,
        Description      = c.Description,
        ParentCategoryId = c.ParentCategoryId,
        IsActive         = c.IsActive
    };
}

// ── Vendor Document ───────────────────────────────────────────────────────────

public class VendorDocumentService : IVendorDocumentService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<VendorDocumentService> _logger;

    public VendorDocumentService(ProcurementDbContext ctx, ILogger<VendorDocumentService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task<List<VendorDocumentDto>> GetByVendorAsync(Guid vendorId)
        => await _ctx.VendorDocuments
            .AsNoTracking()
            .Where(d => d.VendorId == vendorId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => MapToDto(d))
            .ToListAsync();

    public async Task<List<VendorDocumentDto>> GetExpiringAsync(int withinDays = 30)
    {
        var threshold = DateTime.UtcNow.AddDays(withinDays);
        return await _ctx.VendorDocuments
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                     && !d.NeverExpires
                     && d.ExpiryDate != null
                     && d.ExpiryDate <= threshold)
            .Select(d => MapToDto(d))
            .ToListAsync();
    }

    public async Task<VendorDocumentDto?> GetByIdAsync(Guid id)
    {
        var entity = await _ctx.VendorDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<VendorDocumentDto> CreateAsync(Guid vendorId, CreateVendorDocumentDto dto)
    {
        var entity = new VendorDocument
        {
            VendorId       = vendorId,
            DocumentType   = dto.DocumentType,
            DocumentName   = dto.DocumentName,
            DocumentNumber = dto.DocumentNumber,
            IssuedBy       = dto.IssuedBy,
            IssuedAt       = dto.IssuedAt,
            ExpiryDate     = dto.ExpiryDate,
            NeverExpires   = dto.NeverExpires,
            ReminderDays   = dto.ReminderDays,
            FilePath       = dto.FilePath,
            FileName       = dto.FileName,
            Notes          = dto.Notes,
            Status         = VendorDocumentStatus.Active
        };

        _ctx.VendorDocuments.Add(entity);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created VendorDocument {Name} for vendor {VendorId}", entity.DocumentName, vendorId);
        return MapToDto(entity);
    }

    public async Task<VendorDocumentDto> UpdateAsync(Guid id, UpdateVendorDocumentDto dto)
    {
        var entity = await _ctx.VendorDocuments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorDocument {id} not found");

        if (dto.DocumentName is not null)   entity.DocumentName   = dto.DocumentName;
        if (dto.DocumentNumber is not null) entity.DocumentNumber = dto.DocumentNumber;
        if (dto.IssuedBy is not null)       entity.IssuedBy       = dto.IssuedBy;
        if (dto.IssuedAt.HasValue)          entity.IssuedAt       = dto.IssuedAt;
        if (dto.ExpiryDate.HasValue)        entity.ExpiryDate     = dto.ExpiryDate;
        if (dto.NeverExpires.HasValue)      entity.NeverExpires   = dto.NeverExpires.Value;
        if (dto.Status.HasValue)            entity.Status         = dto.Status.Value;
        if (dto.ReminderDays.HasValue)      entity.ReminderDays   = dto.ReminderDays.Value;
        if (dto.FilePath is not null)       entity.FilePath       = dto.FilePath;
        if (dto.FileName is not null)       entity.FileName       = dto.FileName;
        if (dto.Notes is not null)          entity.Notes          = dto.Notes;

        _ctx.VendorDocuments.Update(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<VendorDocumentDto> VerifyAsync(Guid id, Guid verifiedByUserId)
    {
        var entity = await _ctx.VendorDocuments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorDocument {id} not found");

        entity.IsVerified        = true;
        entity.VerifiedAt        = DateTime.UtcNow;
        entity.VerifiedByUserId  = verifiedByUserId;

        _ctx.VendorDocuments.Update(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.VendorDocuments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorDocument {id} not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _ctx.VendorDocuments.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    private static VendorDocumentDto MapToDto(VendorDocument d) => new()
    {
        Id              = d.Id,
        VendorId        = d.VendorId,
        DocumentType    = d.DocumentType,
        DocumentName    = d.DocumentName,
        DocumentNumber  = d.DocumentNumber,
        IssuedBy        = d.IssuedBy,
        IssuedAt        = d.IssuedAt,
        ExpiryDate      = d.ExpiryDate,
        NeverExpires    = d.NeverExpires,
        Status          = d.Status,
        ReminderDays    = d.ReminderDays,
        FilePath        = d.FilePath,
        FileName        = d.FileName,
        IsVerified      = d.IsVerified,
        VerifiedAt      = d.VerifiedAt,
        Notes           = d.Notes,
        IsExpired       = d.IsExpired,
        DaysUntilExpiry = d.DaysUntilExpiry
    };
}

// ── Vendor Pricelist ──────────────────────────────────────────────────────────

public class VendorPricelistService : IVendorPricelistService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<VendorPricelistService> _logger;

    public VendorPricelistService(ProcurementDbContext ctx, ILogger<VendorPricelistService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task<List<VendorPricelistDto>> GetByVendorAsync(Guid vendorId)
        => await _ctx.VendorPricelists
            .AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.VendorId == vendorId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();

    public async Task<VendorPricelistDto?> GetByIdAsync(Guid id)
    {
        var entity = await _ctx.VendorPricelists
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<VendorPricelistDto> CreateAsync(Guid vendorId, CreateVendorPricelistDto dto)
    {
        var entity = new VendorPricelist
        {
            VendorId     = vendorId,
            Name         = dto.Name,
            CurrencyCode = dto.CurrencyCode,
            ValidFrom    = dto.ValidFrom,
            ValidTo      = dto.ValidTo,
            IsActive     = true
        };

        foreach (var itemDto in dto.Items)
        {
            entity.Items.Add(new VendorPricelistItem
            {
                ItemId          = itemDto.ItemId,
                ItemCode        = itemDto.ItemCode,
                ItemDescription = itemDto.ItemDescription ?? string.Empty,
                UnitPrice       = itemDto.UnitPrice,
                MinimumQuantity = itemDto.MinimumQuantity ?? 1,
                UnitOfMeasureId = itemDto.UnitOfMeasureId,
                ValidFrom       = itemDto.ValidFrom,
                ValidTo         = itemDto.ValidTo
            });
        }

        _ctx.VendorPricelists.Add(entity);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created VendorPricelist {Name} for vendor {VendorId}", entity.Name, vendorId);
        return MapToDto(entity);
    }

    public async Task<VendorPricelistDto> UpdateAsync(Guid id, CreateVendorPricelistDto dto)
    {
        var entity = await _ctx.VendorPricelists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorPricelist {id} not found");

        entity.Name         = dto.Name;
        entity.CurrencyCode = dto.CurrencyCode;
        entity.ValidFrom    = dto.ValidFrom;
        entity.ValidTo      = dto.ValidTo;

        entity.Items.Clear();
        foreach (var itemDto in dto.Items)
        {
            entity.Items.Add(new VendorPricelistItem
            {
                ItemId          = itemDto.ItemId,
                ItemCode        = itemDto.ItemCode,
                ItemDescription = itemDto.ItemDescription ?? string.Empty,
                UnitPrice       = itemDto.UnitPrice,
                MinimumQuantity = itemDto.MinimumQuantity ?? 1,
                UnitOfMeasureId = itemDto.UnitOfMeasureId,
                ValidFrom       = itemDto.ValidFrom,
                ValidTo         = itemDto.ValidTo
            });
        }

        _ctx.VendorPricelists.Update(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.VendorPricelists
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted)
            ?? throw new KeyNotFoundException($"VendorPricelist {id} not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _ctx.VendorPricelists.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    private static VendorPricelistDto MapToDto(VendorPricelist p) => new()
    {
        Id           = p.Id,
        VendorId     = p.VendorId,
        Name         = p.Name,
        CurrencyCode = p.CurrencyCode,
        ValidFrom    = p.ValidFrom,
        ValidTo      = p.ValidTo,
        IsActive     = p.IsActive,
        Items = p.Items.Select(i => new VendorPricelistItemDto
        {
            Id              = i.Id,
            ItemId          = i.ItemId,
            ItemCode        = i.ItemCode,
            ItemDescription = i.ItemDescription,
            UnitPrice       = i.UnitPrice,
            MinimumQuantity = i.MinimumQuantity,
            UnitOfMeasureId = i.UnitOfMeasureId,
            ValidFrom       = i.ValidFrom,
            ValidTo         = i.ValidTo
        }).ToList()
    };
}

// ── Approval Workflow ─────────────────────────────────────────────────────────

public class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<ApprovalWorkflowService> _logger;

    public ApprovalWorkflowService(ProcurementDbContext ctx, ILogger<ApprovalWorkflowService> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task<List<ApprovalWorkflowDto>> GetAllAsync()
        => await _ctx.ApprovalWorkflows
            .AsNoTracking()
            .Include(w => w.Steps)
            .Where(w => !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => MapToDto(w))
            .ToListAsync();

    public async Task<List<ApprovalWorkflowDto>> GetByDocumentTypeAsync(ApprovalDocumentType documentType)
        => await _ctx.ApprovalWorkflows
            .AsNoTracking()
            .Include(w => w.Steps)
            .Where(w => !w.IsDeleted && w.DocumentType == documentType)
            .Select(w => MapToDto(w))
            .ToListAsync();

    public async Task<ApprovalWorkflowDto?> GetByIdAsync(Guid id)
    {
        var entity = await _ctx.ApprovalWorkflows
            .AsNoTracking()
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<ApprovalWorkflowDto> CreateAsync(CreateApprovalWorkflowDto dto)
    {
        var entity = new ApprovalWorkflow
        {
            Name          = dto.Name,
            Description   = dto.Description,
            DocumentType  = dto.DocumentType,
            MinimumAmount = dto.MinimumAmount,
            MaximumAmount = dto.MaximumAmount,
            IsDefault     = dto.IsDefault,
            Priority      = dto.Priority,
            IsActive      = true
        };

        foreach (var stepDto in dto.Steps)
        {
            entity.Steps.Add(new ApprovalWorkflowStep
            {
                StepNumber         = stepDto.StepNumber,
                StepName           = stepDto.StepName,
                ApproverUserId     = stepDto.ApproverUserId,
                ApproverRole       = stepDto.ApproverRole,
                AmountThreshold    = stepDto.AmountThreshold,
                IsParallelStep     = stepDto.IsParallelStep,
                RequiredApprovals  = stepDto.RequiredApprovals,
                EscalationAfterDays = stepDto.EscalationAfterDays,
                EscalationUserId   = stepDto.EscalationUserId,
                IsOptional         = stepDto.IsOptional,
                Instructions       = stepDto.Instructions
            });
        }

        _ctx.ApprovalWorkflows.Add(entity);
        await _ctx.SaveChangesAsync();
        _logger.LogInformation("Created ApprovalWorkflow {Name} for {DocumentType}", entity.Name, entity.DocumentType);
        return MapToDto(entity);
    }

    public async Task<ApprovalWorkflowDto> UpdateAsync(Guid id, CreateApprovalWorkflowDto dto)
    {
        var entity = await _ctx.ApprovalWorkflows
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted)
            ?? throw new KeyNotFoundException($"ApprovalWorkflow {id} not found");

        entity.Name          = dto.Name;
        entity.Description   = dto.Description;
        entity.DocumentType  = dto.DocumentType;
        entity.MinimumAmount = dto.MinimumAmount;
        entity.MaximumAmount = dto.MaximumAmount;
        entity.IsDefault     = dto.IsDefault;
        entity.Priority      = dto.Priority;

        entity.Steps.Clear();
        foreach (var stepDto in dto.Steps)
        {
            entity.Steps.Add(new ApprovalWorkflowStep
            {
                StepNumber          = stepDto.StepNumber,
                StepName            = stepDto.StepName,
                ApproverUserId      = stepDto.ApproverUserId,
                ApproverRole        = stepDto.ApproverRole,
                AmountThreshold     = stepDto.AmountThreshold,
                IsParallelStep      = stepDto.IsParallelStep,
                RequiredApprovals   = stepDto.RequiredApprovals,
                EscalationAfterDays = stepDto.EscalationAfterDays,
                EscalationUserId    = stepDto.EscalationUserId,
                IsOptional          = stepDto.IsOptional,
                Instructions        = stepDto.Instructions
            });
        }

        _ctx.ApprovalWorkflows.Update(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _ctx.ApprovalWorkflows
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted)
            ?? throw new KeyNotFoundException($"ApprovalWorkflow {id} not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _ctx.ApprovalWorkflows.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    private static ApprovalWorkflowDto MapToDto(ApprovalWorkflow w) => new()
    {
        Id            = w.Id,
        Name          = w.Name,
        Description   = w.Description,
        DocumentType  = w.DocumentType,
        MinimumAmount = w.MinimumAmount,
        MaximumAmount = w.MaximumAmount,
        IsActive      = w.IsActive,
        IsDefault     = w.IsDefault,
        Priority      = w.Priority,
        Steps = w.Steps.OrderBy(s => s.StepNumber).Select(s => new ApprovalWorkflowStepDto
        {
            Id                  = s.Id,
            StepNumber          = s.StepNumber,
            StepName            = s.StepName,
            ApproverUserId      = s.ApproverUserId,
            ApproverRole        = s.ApproverRole,
            AmountThreshold     = s.AmountThreshold,
            IsParallelStep      = s.IsParallelStep,
            RequiredApprovals   = s.RequiredApprovals,
            EscalationAfterDays = s.EscalationAfterDays,
            EscalationUserId    = s.EscalationUserId,
            IsOptional          = s.IsOptional,
            Instructions        = s.Instructions
        }).ToList()
    };
}

// ── Document Sequence Management ──────────────────────────────────────────────

public class DocumentSequenceManagementService : IDocumentSequenceManagementService
{
    private readonly ProcurementDbContext _ctx;

    public DocumentSequenceManagementService(ProcurementDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<List<DocumentSequenceDto>> GetAllAsync()
        => await _ctx.DocumentSequences
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => MapToDto(s))
            .ToListAsync();

    public async Task<DocumentSequenceDto?> GetByDocumentTypeAsync(ProcurementDocumentType documentType)
    {
        var entity = await _ctx.DocumentSequences
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.DocumentType == documentType && !s.IsDeleted);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<DocumentSequenceDto> UpdateAsync(Guid id, UpdateDocumentSequenceDto dto)
    {
        var entity = await _ctx.DocumentSequences
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted)
            ?? throw new KeyNotFoundException($"DocumentSequence {id} not found");

        if (dto.Prefix is not null)          entity.Prefix          = dto.Prefix;
        if (dto.Suffix is not null)          entity.Suffix          = dto.Suffix;
        if (dto.Separator is not null)       entity.Separator       = dto.Separator;
        if (dto.IncludeYear.HasValue)        entity.IncludeYear     = dto.IncludeYear.Value;
        if (dto.IncludeMonth.HasValue)       entity.IncludeMonth    = dto.IncludeMonth.Value;
        if (dto.SequencePadding.HasValue)    entity.SequencePadding = dto.SequencePadding.Value;
        if (dto.ResetOn.HasValue)            entity.ResetOn         = dto.ResetOn.Value;
        if (dto.IsActive.HasValue)           entity.IsActive        = dto.IsActive.Value;

        _ctx.DocumentSequences.Update(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    private static DocumentSequenceDto MapToDto(DocumentSequence s) => new()
    {
        Id                 = s.Id,
        DocumentType       = s.DocumentType,
        Prefix             = s.Prefix,
        Suffix             = s.Suffix,
        Separator          = s.Separator,
        IncludeYear        = s.IncludeYear,
        IncludeMonth       = s.IncludeMonth,
        SequencePadding    = s.SequencePadding,
        ResetOn            = s.ResetOn,
        NextSequenceNumber = s.NextSequenceNumber,
        IsActive           = s.IsActive,
        Description        = s.Description
    };
}

// ── Procurement Settings ──────────────────────────────────────────────────────

public class ProcurementSettingsService : IProcurementSettingsService
{
    private readonly ProcurementDbContext _ctx;

    public ProcurementSettingsService(ProcurementDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<ProcurementSettingsDto?> GetAsync()
    {
        var entity = await _ctx.ProcurementSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<ProcurementSettingsDto> UpdateAsync(UpdateProcurementSettingsDto dto)
    {
        var entity = await _ctx.ProcurementSettings.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Procurement settings not found");

        if (dto.RequireRequisitionForPO.HasValue)      entity.RequireRequisitionForPO      = dto.RequireRequisitionForPO.Value;
        if (dto.RFQMandatoryAboveAmount.HasValue)       entity.RFQMandatoryAboveAmount      = dto.RFQMandatoryAboveAmount;
        if (dto.Enable3WayMatching.HasValue)            entity.Enable3WayMatching           = dto.Enable3WayMatching.Value;
        if (dto.EnforceInvoicePOTolerance.HasValue)     entity.EnforceInvoicePOTolerance    = dto.EnforceInvoicePOTolerance.Value;
        if (dto.InvoicePOTolerancePercent.HasValue)     entity.InvoicePOTolerancePercent    = dto.InvoicePOTolerancePercent.Value;
        if (dto.DefaultPaymentTerms.HasValue)           entity.DefaultPaymentTerms          = dto.DefaultPaymentTerms.Value;
        if (dto.DefaultCurrencyCode is not null)        entity.DefaultCurrencyCode          = dto.DefaultCurrencyCode;
        if (dto.DefaultLeadTimeDays.HasValue)           entity.DefaultLeadTimeDays          = dto.DefaultLeadTimeDays.Value;
        if (dto.POApprovalWorkflowId.HasValue)          entity.POApprovalWorkflowId         = dto.POApprovalWorkflowId;
        if (dto.RequisitionApprovalWorkflowId.HasValue) entity.RequisitionApprovalWorkflowId = dto.RequisitionApprovalWorkflowId;
        if (dto.InvoiceApprovalWorkflowId.HasValue)     entity.InvoiceApprovalWorkflowId    = dto.InvoiceApprovalWorkflowId;
        if (dto.RequireVendorApproval.HasValue)         entity.RequireVendorApproval        = dto.RequireVendorApproval.Value;
        if (dto.RequireVendorBankVerification.HasValue) entity.RequireVendorBankVerification = dto.RequireVendorBankVerification.Value;
        if (dto.SendPOToVendorByEmail.HasValue)         entity.SendPOToVendorByEmail        = dto.SendPOToVendorByEmail.Value;
        if (dto.SendRFQToVendorByEmail.HasValue)        entity.SendRFQToVendorByEmail       = dto.SendRFQToVendorByEmail.Value;
        if (dto.POApprovalReminderDays.HasValue)        entity.POApprovalReminderDays       = dto.POApprovalReminderDays.Value;

        _ctx.ProcurementSettings.Update(entity);
        await _ctx.SaveChangesAsync();
        return MapToDto(entity);
    }

    private static ProcurementSettingsDto MapToDto(ProcurementSettings s) => new()
    {
        Id                            = s.Id,
        RequireRequisitionForPO       = s.RequireRequisitionForPO,
        RFQMandatoryAboveAmount       = s.RFQMandatoryAboveAmount,
        Enable3WayMatching            = s.Enable3WayMatching,
        EnforceInvoicePOTolerance     = s.EnforceInvoicePOTolerance,
        InvoicePOTolerancePercent     = s.InvoicePOTolerancePercent,
        DefaultPaymentTerms           = s.DefaultPaymentTerms,
        DefaultCurrencyCode           = s.DefaultCurrencyCode,
        DefaultLeadTimeDays           = s.DefaultLeadTimeDays,
        POApprovalWorkflowId          = s.POApprovalWorkflowId,
        RequisitionApprovalWorkflowId = s.RequisitionApprovalWorkflowId,
        InvoiceApprovalWorkflowId     = s.InvoiceApprovalWorkflowId,
        RequireVendorApproval         = s.RequireVendorApproval,
        RequireVendorBankVerification  = s.RequireVendorBankVerification,
        SendPOToVendorByEmail         = s.SendPOToVendorByEmail,
        SendRFQToVendorByEmail        = s.SendRFQToVendorByEmail,
        POApprovalReminderDays        = s.POApprovalReminderDays
    };
}
