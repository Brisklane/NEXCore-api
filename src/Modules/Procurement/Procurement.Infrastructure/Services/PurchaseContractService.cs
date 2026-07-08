using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Services;

public class PurchaseContractService : IPurchaseContractService
{
    private readonly IPurchaseContractRepository _contracts;
    private readonly IDocumentSequenceService _sequences;
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<PurchaseContractService> _logger;

    public PurchaseContractService(
        IPurchaseContractRepository contracts,
        IDocumentSequenceService sequences,
        ProcurementDbContext ctx,
        ILogger<PurchaseContractService> logger)
    {
        _contracts = contracts;
        _sequences = sequences;
        _ctx       = ctx;
        _logger    = logger;
    }

    public async Task<PaginatedResponse<PurchaseContractDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _contracts.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: e =>
                (string.IsNullOrEmpty(search) ||
                    e.ContractNumber.ToLower().Contains(search) ||
                    e.Title.ToLower().Contains(search) ||
                    (e.VendorName != null && e.VendorName.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)e.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "CreatedAt"));
        return PaginatedResponse<PurchaseContractDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PurchaseContractDto?> GetByIdAsync(Guid id)
    {
        var c = await _contracts.GetWithLinesAsync(id);
        return c is null ? null : MapToDto(c);
    }

    public async Task<PurchaseContractDto?> GetByNumberAsync(string contractNumber)
    {
        var c = await _contracts.GetByNumberAsync(contractNumber);
        return c is null ? null : MapToDto(c);
    }

    public async Task<List<PurchaseContractDto>> GetByStatusAsync(PurchaseContractStatus status)
        => (await _contracts.GetByStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<PurchaseContractDto>> GetByVendorAsync(Guid vendorId)
        => (await _contracts.GetByVendorAsync(vendorId)).Select(MapToDto).ToList();

    public async Task<List<PurchaseContractDto>> GetActiveAsync()
        => (await _contracts.GetActiveAsync()).Select(MapToDto).ToList();

    public async Task<List<PurchaseContractDto>> GetExpiringAsync(int withinDays)
        => (await _contracts.GetExpiringAsync(withinDays)).Select(MapToDto).ToList();

    public async Task<PurchaseContractDto> CreateAsync(CreatePurchaseContractDto dto)
    {
        var contract = new PurchaseContract
        {
            ContractNumber       = await _sequences.GetNextNumberAsync(ProcurementDocumentType.PurchaseContract),
            Title                = dto.Title,
            Description          = dto.Description,
            VendorId             = dto.VendorId,
            ContractType         = dto.ContractType,
            StartDate            = dto.StartDate,
            EndDate              = dto.EndDate,
            AutoRenew            = dto.AutoRenew,
            RenewalNoticeDays    = dto.RenewalNoticeDays,
            RenewalDurationMonths = dto.RenewalDurationMonths,
            CurrencyCode         = dto.CurrencyCode,
            MaximumContractValue = dto.MaximumContractValue,
            PaymentTerms         = dto.PaymentTerms,
            Incoterm             = dto.Incoterm,
            TermsAndConditions   = dto.TermsAndConditions,
            Notes                = dto.Notes,
            Status               = PurchaseContractStatus.Draft
        };

        var lineNumber = 1;
        foreach (var lineDto in dto.Lines)
        {
            contract.Lines.Add(new PurchaseContractLine
            {
                LineNumber            = lineNumber++,
                ItemId                = lineDto.ItemId,
                ItemCode              = lineDto.ItemCode,
                ItemDescription       = lineDto.ItemDescription,
                ProcurementCategoryId = lineDto.ProcurementCategoryId,
                UnitOfMeasureId       = lineDto.UnitOfMeasureId,
                UnitOfMeasureName     = lineDto.UnitOfMeasureName,
                MinimumQuantity       = lineDto.MinimumQuantity,
                MaximumQuantity       = lineDto.MaximumQuantity,
                CommittedQuantity     = lineDto.CommittedQuantity,
                UnitPrice             = lineDto.UnitPrice,
                DiscountPercent       = lineDto.DiscountPercent,
                ValidFrom             = lineDto.ValidFrom,
                ValidTo               = lineDto.ValidTo,
                Notes                 = lineDto.Notes
            });
        }

        await _contracts.AddAsync(contract);
        await _contracts.SaveChangesAsync();
        _logger.LogInformation("Created contract {Number} for vendor {VendorId}",
            contract.ContractNumber, contract.VendorId);
        return MapToDto(contract);
    }

    public async Task<PurchaseContractDto> UpdateAsync(Guid id, UpdatePurchaseContractDto dto)
    {
        var contract = await _contracts.GetWithLinesAsync(id)
            ?? throw new KeyNotFoundException($"Contract {id} not found");

        if (contract.Status != PurchaseContractStatus.Draft)
            throw new InvalidOperationException($"Cannot update a contract in status {contract.Status}");

        if (dto.Title is not null)               contract.Title               = dto.Title;
        if (dto.Description is not null)         contract.Description         = dto.Description;
        if (dto.EndDate.HasValue)                contract.EndDate             = dto.EndDate.Value;
        if (dto.MaximumContractValue.HasValue)   contract.MaximumContractValue = dto.MaximumContractValue;
        if (dto.AutoRenew.HasValue)              contract.AutoRenew           = dto.AutoRenew.Value;
        if (dto.RenewalNoticeDays.HasValue)      contract.RenewalNoticeDays   = dto.RenewalNoticeDays.Value;
        if (dto.RenewalDurationMonths.HasValue)  contract.RenewalDurationMonths = dto.RenewalDurationMonths;
        if (dto.TermsAndConditions is not null)  contract.TermsAndConditions  = dto.TermsAndConditions;
        if (dto.Notes is not null)               contract.Notes               = dto.Notes;

        if (dto.Lines is not null)
        {
            // Replace lines: delete existing, insert new. Done on the DbSet directly so the
            // new rows (pre-set Guid Ids) are tracked as Added — not Modified via Update(graph),
            // which would emit 0-row UPDATEs and throw a phantom concurrency exception.
            _ctx.Set<PurchaseContractLine>().RemoveRange(contract.Lines);
            var lineNumber = 1;
            foreach (var lineDto in dto.Lines)
            {
                _ctx.Set<PurchaseContractLine>().Add(new PurchaseContractLine
                {
                    ContractId            = contract.Id,
                    LineNumber            = lineNumber++,
                    ItemId                = lineDto.ItemId,
                    ItemCode              = lineDto.ItemCode,
                    ItemDescription       = lineDto.ItemDescription,
                    ProcurementCategoryId = lineDto.ProcurementCategoryId,
                    UnitOfMeasureId       = lineDto.UnitOfMeasureId,
                    UnitOfMeasureName     = lineDto.UnitOfMeasureName,
                    MinimumQuantity       = lineDto.MinimumQuantity,
                    MaximumQuantity       = lineDto.MaximumQuantity,
                    CommittedQuantity     = lineDto.CommittedQuantity,
                    UnitPrice             = lineDto.UnitPrice,
                    DiscountPercent       = lineDto.DiscountPercent,
                    ValidFrom             = lineDto.ValidFrom,
                    ValidTo               = lineDto.ValidTo,
                    Notes                 = lineDto.Notes
                });
            }
        }

        // contract is tracked (loaded with Include) — header changes persist without Update(graph).
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<PurchaseContractDto> ActivateAsync(Guid id, Guid approvedByUserId)
    {
        var contract = await _contracts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Contract {id} not found");

        if (contract.Status != PurchaseContractStatus.Draft &&
            contract.Status != PurchaseContractStatus.UnderReview)
            throw new InvalidOperationException($"Cannot activate contract in status {contract.Status}");

        contract.Status          = PurchaseContractStatus.Active;
        contract.ApprovedByUserId = approvedByUserId;
        contract.ApprovedAt      = DateTime.UtcNow;
        contract.SignedAt        = DateTime.UtcNow;

        _contracts.Update(contract);
        await _contracts.SaveChangesAsync();
        _logger.LogInformation("Contract {Number} activated by {UserId}", contract.ContractNumber, approvedByUserId);
        return MapToDto(contract);
    }

    public async Task<PurchaseContractDto> SuspendAsync(Guid id)
    {
        var contract = await _contracts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Contract {id} not found");

        if (contract.Status != PurchaseContractStatus.Active)
            throw new InvalidOperationException($"Cannot suspend contract in status {contract.Status}");

        contract.Status = PurchaseContractStatus.Suspended;
        _contracts.Update(contract);
        await _contracts.SaveChangesAsync();
        return MapToDto(contract);
    }

    public async Task<PurchaseContractDto> TerminateAsync(Guid id, TerminateContractDto dto)
    {
        var contract = await _contracts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Contract {id} not found");

        contract.Status            = PurchaseContractStatus.Terminated;
        contract.TerminatedAt      = DateTime.UtcNow;
        contract.TerminationReason = dto.TerminationReason;

        _contracts.Update(contract);
        await _contracts.SaveChangesAsync();
        _logger.LogInformation("Contract {Number} terminated: {Reason}", contract.ContractNumber, dto.TerminationReason);
        return MapToDto(contract);
    }

    public async Task<PurchaseContractDto> RenewAsync(Guid id)
    {
        var contract = await _contracts.GetWithLinesAsync(id)
            ?? throw new KeyNotFoundException($"Contract {id} not found");

        if (!contract.AutoRenew)
            throw new InvalidOperationException("Contract is not configured for auto-renewal");

        var months = contract.RenewalDurationMonths ?? 12;
        contract.StartDate = contract.EndDate.AddDays(1);
        contract.EndDate   = contract.StartDate.AddMonths(months);
        contract.Status    = PurchaseContractStatus.Active;
        contract.UsedValue = 0;

        _contracts.Update(contract);
        await _contracts.SaveChangesAsync();
        _logger.LogInformation("Contract {Number} renewed until {EndDate}", contract.ContractNumber, contract.EndDate);
        return MapToDto(contract);
    }

    public async Task DeleteAsync(Guid id)
    {
        var contract = await _contracts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Contract {id} not found");

        if (contract.Status != PurchaseContractStatus.Draft)
            throw new InvalidOperationException("Only draft contracts can be deleted");

        _contracts.SoftDelete(contract);
        await _contracts.SaveChangesAsync();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static PurchaseContractDto MapToDto(PurchaseContract c) => new()
    {
        Id                   = c.Id,
        ContractNumber       = c.ContractNumber,
        Title                = c.Title,
        Description          = c.Description,
        VendorId             = c.VendorId,
        VendorName           = c.VendorName ?? c.Vendor?.Name,
        ContractType         = c.ContractType,
        Status               = c.Status,
        StartDate            = c.StartDate,
        EndDate              = c.EndDate,
        SignedAt             = c.SignedAt,
        TerminatedAt         = c.TerminatedAt,
        AutoRenew            = c.AutoRenew,
        RenewalNoticeDays    = c.RenewalNoticeDays,
        RenewalDurationMonths = c.RenewalDurationMonths,
        CurrencyCode         = c.CurrencyCode,
        MaximumContractValue = c.MaximumContractValue,
        CommittedValue       = c.CommittedValue,
        UsedValue            = c.UsedValue,
        RemainingValue       = (c.MaximumContractValue ?? 0) - c.UsedValue,
        PaymentTerms         = c.PaymentTerms,
        Incoterm             = c.Incoterm,
        ApprovedByUserId     = c.ApprovedByUserId,
        ApprovedAt           = c.ApprovedAt,
        TermsAndConditions   = c.TermsAndConditions,
        TerminationReason    = c.TerminationReason,
        Notes                = c.Notes,
        Lines = c.Lines.Select(l => new PurchaseContractLineDto
        {
            Id                    = l.Id,
            LineNumber            = l.LineNumber,
            ItemId                = l.ItemId,
            ItemCode              = l.ItemCode,
            ItemDescription       = l.ItemDescription,
            ProcurementCategoryId = l.ProcurementCategoryId,
            UnitOfMeasureId       = l.UnitOfMeasureId,
            UnitOfMeasureName     = l.UnitOfMeasureName,
            MinimumQuantity       = l.MinimumQuantity,
            MaximumQuantity       = l.MaximumQuantity,
            CommittedQuantity     = l.CommittedQuantity,
            OrderedQuantity       = l.OrderedQuantity,
            RemainingQuantity     = (l.CommittedQuantity ?? l.MaximumQuantity ?? 0) - l.OrderedQuantity,
            UnitPrice             = l.UnitPrice,
            DiscountPercent       = l.DiscountPercent,
            ValidFrom             = l.ValidFrom,
            ValidTo               = l.ValidTo,
            Notes                 = l.Notes
        }).ToList()
    };
}
