using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Infrastructure.Services;

public class InspectionService : IInspectionService
{
    private readonly IInspectionRepository _repo;
    private readonly IInspectionCharacteristicRepository _charRepo;
    private readonly ILogger<InspectionService> _logger;

    public InspectionService(IInspectionRepository repo, IInspectionCharacteristicRepository charRepo, ILogger<InspectionService> logger)
    {
        _repo = repo; _charRepo = charRepo; _logger = logger;
    }

    public async Task<InspectionDto> CreateAsync(CreateInspectionDto request, Guid userId)
    {
        var inspection = new Inspection
        {
            ProductionOrderId = request.ProductionOrderId, InspectedQty = request.InspectedQty,
            PassedQty = request.PassedQty, RejectedQty = request.RejectedQty,
            Status = "Pending", InspectedById = userId, InspectedAt = request.InspectedAt,
            Remarks = request.Remarks, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(inspection);
        await _repo.SaveChangesAsync();

        foreach (var cDto in request.Characteristics)
        {
            var c = new InspectionCharacteristic
            {
                InspectionId = inspection.Id, CharacteristicName = cDto.CharacteristicName,
                InspectionType = cDto.InspectionType, UnitOfMeasure = cDto.UnitOfMeasure,
                TargetValue = cDto.TargetValue, UpperTolerance = cDto.UpperTolerance,
                LowerTolerance = cDto.LowerTolerance, ActualValue = cDto.ActualValue,
                QualitativeResult = cDto.QualitativeResult, Result = cDto.Result,
                IsCritical = cDto.IsCritical, SampleSize = cDto.SampleSize, Remarks = cDto.Remarks,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _charRepo.AddAsync(c);
        }
        if (request.Characteristics.Count > 0) await _charRepo.SaveChangesAsync();

        var full = await _repo.GetWithCharacteristicsAsync(inspection.Id);
        return ManufacturingMapper.ToDto(full!);
    }

    public async Task<InspectionDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetWithCharacteristicsAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<InspectionDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: i => i.ProductionOrderId == productionOrderId);
        return PaginatedResponse<InspectionDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<InspectionDto> UpdateAsync(Guid id, UpdateInspectionDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Inspection not found");
        if (request.InspectedQty.HasValue) e.InspectedQty = request.InspectedQty.Value;
        if (request.PassedQty.HasValue) e.PassedQty = request.PassedQty.Value;
        if (request.RejectedQty.HasValue) e.RejectedQty = request.RejectedQty.Value;
        if (request.Status != null) e.Status = request.Status;
        if (request.Remarks != null) e.Remarks = request.Remarks;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Inspection not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<InspectionCharacteristicDto> AddCharacteristicAsync(Guid inspectionId, CreateInspectionCharacteristicDto request, Guid userId)
    {
        var c = new InspectionCharacteristic
        {
            InspectionId = inspectionId, CharacteristicName = request.CharacteristicName,
            InspectionType = request.InspectionType, UnitOfMeasure = request.UnitOfMeasure,
            TargetValue = request.TargetValue, UpperTolerance = request.UpperTolerance,
            LowerTolerance = request.LowerTolerance, ActualValue = request.ActualValue,
            QualitativeResult = request.QualitativeResult, Result = request.Result,
            IsCritical = request.IsCritical, SampleSize = request.SampleSize, Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _charRepo.AddAsync(c);
        await _charRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(c);
    }

    public async Task<InspectionCharacteristicDto> UpdateCharacteristicAsync(Guid characteristicId, UpdateInspectionCharacteristicDto request, Guid userId)
    {
        var e = await _charRepo.GetByIdAsync(characteristicId) ?? throw new InvalidOperationException("Characteristic not found");
        if (request.ActualValue.HasValue) e.ActualValue = request.ActualValue;
        if (request.QualitativeResult != null) e.QualitativeResult = request.QualitativeResult;
        if (request.Result != null) e.Result = request.Result;
        if (request.Remarks != null) e.Remarks = request.Remarks;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _charRepo.Update(e);
        await _charRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteCharacteristicAsync(Guid characteristicId, Guid userId)
    {
        var e = await _charRepo.GetByIdAsync(characteristicId) ?? throw new InvalidOperationException("Characteristic not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _charRepo.Update(e);
        await _charRepo.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<InspectionDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<InspectionDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }
}

public class FinishedGoodsReceiptService : IFinishedGoodsReceiptService
{
    private readonly IFinishedGoodsReceiptRepository _repo;
    private readonly ILogger<FinishedGoodsReceiptService> _logger;

    public FinishedGoodsReceiptService(IFinishedGoodsReceiptRepository repo, ILogger<FinishedGoodsReceiptService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<FinishedGoodsReceiptDto> CreateAsync(CreateFinishedGoodsReceiptDto request, Guid userId)
    {
        var entity = new FinishedGoodsReceipt
        {
            ProductionOrderId = request.ProductionOrderId, ProductId = request.ProductId,
            QuantityReceived = request.QuantityReceived, WarehouseId = request.WarehouseId,
            BatchNo = request.BatchNo, ReceivedAt = request.ReceivedAt,
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<FinishedGoodsReceiptDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<FinishedGoodsReceiptDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: r => r.ProductionOrderId == productionOrderId);
        return PaginatedResponse<FinishedGoodsReceiptDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<FinishedGoodsReceiptDto> UpdateAsync(Guid id, UpdateFinishedGoodsReceiptDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Receipt not found");
        if (request.QuantityReceived.HasValue) e.QuantityReceived = request.QuantityReceived.Value;
        if (request.BatchNo != null) e.BatchNo = request.BatchNo;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Receipt not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<FinishedGoodsReceiptDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<FinishedGoodsReceiptDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }
}

public class ProductionBatchService : IProductionBatchService
{
    private readonly IProductionBatchRepository _repo;
    private readonly ILogger<ProductionBatchService> _logger;

    public ProductionBatchService(IProductionBatchRepository repo, ILogger<ProductionBatchService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task<ProductionBatchDto> CreateAsync(CreateProductionBatchDto request, Guid userId)
    {
        var entity = new ProductionBatch
        {
            ProductionOrderId = request.ProductionOrderId, ProductId = request.ProductId,
            BatchNumber = request.BatchNumber, ManufacturingDate = request.ManufacturingDate,
            ExpiryDate = request.ExpiryDate, ReTestDate = request.ReTestDate,
            Quantity = request.Quantity, UnitOfMeasure = request.UnitOfMeasure,
            Status = "Active", WarehouseId = request.WarehouseId,
            CertificateOfAnalysis = request.CertificateOfAnalysis,
            VendorBatchNumber = request.VendorBatchNumber, InspectionId = request.InspectionId,
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<ProductionBatchDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<ProductionBatchDto?> GetByBatchNumberAsync(string batchNumber)
    {
        var e = await _repo.GetByBatchNumberAsync(batchNumber);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<ProductionBatchDto>> GetByProductionOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: b => b.ProductionOrderId == productionOrderId);
        return PaginatedResponse<ProductionBatchDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<ProductionBatchDto>> GetByProductAsync(Guid productId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: b => b.ProductId == productId);
        return PaginatedResponse<ProductionBatchDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ProductionBatchDto> UpdateAsync(Guid id, UpdateProductionBatchDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Batch not found");
        if (request.ExpiryDate.HasValue) e.ExpiryDate = request.ExpiryDate;
        if (request.ReTestDate.HasValue) e.ReTestDate = request.ReTestDate;
        if (request.Status != null) e.Status = request.Status;
        if (request.CertificateOfAnalysis != null) e.CertificateOfAnalysis = request.CertificateOfAnalysis;
        if (request.QualityApproved.HasValue) e.QualityApproved = request.QualityApproved.Value;
        if (request.InspectionId.HasValue) e.InspectionId = request.InspectionId;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Batch not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<ProductionBatchDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<ProductionBatchDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }
}
