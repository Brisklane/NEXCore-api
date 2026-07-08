using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;

namespace Manufacturing.Infrastructure.Services;

public class ProductionOrderService : IProductionOrderService
{
    private readonly IProductionOrderRepository _repo;
    private readonly IProductionOrderOperationRepository _opRepo;
    private readonly IProductionOrderComponentRepository _compRepo;
    private readonly IBillOfMaterialRepository _bomRepo;
    private readonly IRoutingRepository _routingRepo;
    private readonly IMaterialIssueRepository _issueRepo;
    private readonly IFinishedGoodsReceiptRepository _fgrRepo;
    private readonly IStandardCostRepository _stdCostRepo;
    private readonly IEventPublisher _events;
    private readonly ILogger<ProductionOrderService> _logger;

    public ProductionOrderService(IProductionOrderRepository repo,
        IProductionOrderOperationRepository opRepo,
        IProductionOrderComponentRepository compRepo,
        IBillOfMaterialRepository bomRepo,
        IRoutingRepository routingRepo,
        IMaterialIssueRepository issueRepo,
        IFinishedGoodsReceiptRepository fgrRepo,
        IStandardCostRepository stdCostRepo,
        IEventPublisher events,
        ILogger<ProductionOrderService> logger)
    {
        _repo = repo; _opRepo = opRepo; _compRepo = compRepo;
        _bomRepo = bomRepo; _routingRepo = routingRepo; _issueRepo = issueRepo;
        _fgrRepo = fgrRepo; _stdCostRepo = stdCostRepo; _events = events; _logger = logger;
    }

    public async Task<ProductionOrderDto> ProduceExpressAsync(
        ProduceExpressDto request, Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        if (request.Quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");

        // 1. Resolve the item's active Bill of Materials (latest active version), with its components.
        var boms = await _bomRepo.GetByProductAsync(request.ProductId);
        var bomHeader = boms
            .Where(b => b.IsActive && !b.IsDeleted)
            .OrderByDescending(b => b.Version)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("This item has no active Bill of Materials and cannot be produced.");

        var bom = await _bomRepo.GetWithItemsAsync(bomHeader.Id) ?? bomHeader;
        var components = (bom.Items ?? new List<BOMItem>()).ToList();

        // 2. Get-or-create a routing for the product (FK is required; food items have no real routing).
        var routings = await _routingRepo.GetByProductAsync(request.ProductId);
        var routing = routings.FirstOrDefault(r => r.IsActive && !r.IsDeleted) ?? routings.FirstOrDefault();
        if (routing is null)
        {
            routing = new Routing
            {
                ProductId = request.ProductId,
                Name = "Express (auto)",
                Version = 1,
                IsActive = true,
                Description = "Auto-created for express produce (no manufacturing steps).",
                CompanyId = companyId, BranchId = branchId, BusinessUnitId = businessUnitId,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _routingRepo.AddAsync(routing);
            await _routingRepo.SaveChangesAsync();
        }

        // 3. Create the production order, already Completed (express).
        var now = DateTime.UtcNow;
        var po = new ProductionOrder
        {
            OrderNumber = $"PO-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
            ProductId = request.ProductId,
            BillOfMaterialId = bom.Id,
            RoutingId = routing.Id,
            QuantityPlanned = request.Quantity,
            QuantityProduced = request.Quantity,
            Status = "Released",
            StartDate = now, EndDate = now,
            Notes = "Auto-created from POS sale (express): materials backflushed, finished goods received.",
            CompanyId = companyId, BranchId = branchId, BusinessUnitId = businessUnitId,
            CreatedById = userId, CreatedAt = now, CreatedByUserId = userId
        };
        await _repo.AddAsync(po);

        // 4. Record material issues (backflush) + finished-goods receipt against the order.
        var consumedLines = new List<StockDeductionLine>();
        foreach (var c in components)
        {
            var issuedQty = c.QuantityRequired * request.Quantity;
            await _issueRepo.AddAsync(new MaterialIssue
            {
                ProductionOrderId = po.Id,
                MaterialId = c.MaterialId,
                QuantityIssued = issuedQty,
                IssuedAt = now,
                IssuedById = userId,
                Notes = "Backflushed by express produce.",
                CompanyId = companyId, BranchId = branchId, BusinessUnitId = businessUnitId,
                CreatedAt = now, CreatedByUserId = userId
            });

            consumedLines.Add(new StockDeductionLine
            {
                ProductId = c.MaterialId,
                Quantity = issuedQty,
                UnitOfMeasure = c.UnitOfMeasure ?? string.Empty,
                WarehouseId = request.WarehouseId
            });
        }

        await _fgrRepo.AddAsync(new FinishedGoodsReceipt
        {
            ProductionOrderId = po.Id,
            ProductId = request.ProductId,
            QuantityReceived = request.Quantity,
            WarehouseId = request.WarehouseId ?? Guid.Empty,
            ReceivedAt = now,
            Notes = "Received by express produce.",
            CompanyId = companyId, BranchId = branchId, BusinessUnitId = businessUnitId,
            CreatedAt = now, CreatedByUserId = userId
        });

        await _repo.SaveChangesAsync();

        // 5. Non-material conversion cost per unit (labour + machine + overhead) from the product's
        // active Standard Cost — this is where recipe costs like "Vege" / "Other cost" live, instead
        // of being modelled as fake inventory items. Zero when no standard cost is defined.
        var stdCost = await _stdCostRepo.GetActiveByProductAsync(request.ProductId);
        var conversionUnitCost = stdCost is null
            ? 0m
            : stdCost.LaborCost + stdCost.MachineCost + stdCost.OverheadCost;

        // 6. Publish the inventory impact: consume components (OUT), receive finished goods (IN).
        await _events.PublishAsync(new ProductionCompletedEvent
        {
            ProductionOrderId = po.Id,
            OrderNumber = po.OrderNumber,
            CompanyId = companyId, BranchId = branchId, BusinessUnitId = businessUnitId,
            CreatedByUserId = userId,
            WarehouseId = request.WarehouseId,
            ConversionUnitCost = conversionUnitCost,
            ConsumedMaterials = consumedLines,
            ProducedGoods = new List<StockDeductionLine>
            {
                new() { ProductId = request.ProductId, Quantity = request.Quantity, WarehouseId = request.WarehouseId }
            }
        });

        _logger.LogInformation(
            "Express produce: Order {OrderNumber} for product {ProductId} x{Qty} ({Components} components backflushed)",
            po.OrderNumber, request.ProductId, request.Quantity, consumedLines.Count);

        return ManufacturingMapper.ToDto(po);
    }

    public async Task<ProductionOrderDto> CreateAsync(CreateProductionOrderDto request, Guid userId)
    {
        var entity = new ProductionOrder
        {
            OrderNumber = request.OrderNumber, ProductId = request.ProductId,
            BillOfMaterialId = request.BillOfMaterialId, RoutingId = request.RoutingId,
            PlannedOrderId = request.PlannedOrderId, QuantityPlanned = request.QuantityPlanned,
            Status = "Draft", StartDate = request.StartDate, EndDate = request.EndDate,
            DueDate = request.DueDate, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        _logger.LogInformation("ProductionOrder created: {OrderNumber}", entity.OrderNumber);
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<ProductionOrderDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<ProductionOrderDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<ProductionOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<ProductionOrderDto>> GetByProductAsync(Guid productId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: o => o.ProductId == productId);
        return PaginatedResponse<ProductionOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<ProductionOrderDto>> GetByStatusAsync(string status, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: o => o.Status == status);
        return PaginatedResponse<ProductionOrderDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ProductionOrderDto> UpdateAsync(Guid id, UpdateProductionOrderDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("ProductionOrder not found");
        if (request.QuantityPlanned.HasValue) e.QuantityPlanned = request.QuantityPlanned.Value;
        if (request.QuantityProduced.HasValue) e.QuantityProduced = request.QuantityProduced.Value;
        if (request.QuantityRejected.HasValue) e.QuantityRejected = request.QuantityRejected.Value;
        if (request.Status != null) e.Status = request.Status;
        if (request.StartDate.HasValue) e.StartDate = request.StartDate;
        if (request.EndDate.HasValue) e.EndDate = request.EndDate;
        if (request.DueDate.HasValue) e.DueDate = request.DueDate;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("ProductionOrder not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<ProductionOrderOperationDto> AddOperationAsync(CreateProductionOrderOperationDto request, Guid userId)
    {
        var op = new ProductionOrderOperation
        {
            ProductionOrderId = request.ProductionOrderId, RoutingOperationId = request.RoutingOperationId,
            WorkCenterId = request.WorkCenterId, SequenceNo = request.SequenceNo,
            OperationName = request.OperationName, PlannedSetupHours = request.PlannedSetupHours,
            PlannedLaborHours = request.PlannedLaborHours, PlannedMachineHours = request.PlannedMachineHours,
            Status = "Pending", Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _opRepo.AddAsync(op);
        await _opRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(op);
    }

    public async Task<ProductionOrderOperationDto> UpdateOperationAsync(Guid operationId, UpdateProductionOrderOperationDto request, Guid userId)
    {
        var e = await _opRepo.GetByIdAsync(operationId) ?? throw new InvalidOperationException("Operation not found");
        if (request.ActualSetupHours.HasValue) e.ActualSetupHours = request.ActualSetupHours.Value;
        if (request.ActualLaborHours.HasValue) e.ActualLaborHours = request.ActualLaborHours.Value;
        if (request.ActualMachineHours.HasValue) e.ActualMachineHours = request.ActualMachineHours.Value;
        if (request.ConfirmedQty.HasValue) e.ConfirmedQty = request.ConfirmedQty.Value;
        if (request.ScrapQty.HasValue) e.ScrapQty = request.ScrapQty.Value;
        if (request.Status != null) e.Status = request.Status;
        if (request.StartedAt.HasValue) e.StartedAt = request.StartedAt;
        if (request.CompletedAt.HasValue) e.CompletedAt = request.CompletedAt;
        if (request.ConfirmedById.HasValue) e.ConfirmedById = request.ConfirmedById;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _opRepo.Update(e);
        await _opRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<ProductionOrderOperationDto>> GetOperationsByOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _opRepo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: o => o.ProductionOrderId == productionOrderId);
        return PaginatedResponse<ProductionOrderOperationDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ProductionOrderComponentDto> AddComponentAsync(CreateProductionOrderComponentDto request, Guid userId)
    {
        var comp = new ProductionOrderComponent
        {
            ProductionOrderId = request.ProductionOrderId, BOMItemId = request.BOMItemId,
            MaterialId = request.MaterialId, PlannedQty = request.PlannedQty,
            UnitOfMeasure = request.UnitOfMeasure, ScrapPercentage = request.ScrapPercentage,
            StorageLocationId = request.StorageLocationId, IsManuallyAdded = request.IsManuallyAdded,
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _compRepo.AddAsync(comp);
        await _compRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(comp);
    }

    public async Task<ProductionOrderComponentDto> UpdateComponentAsync(Guid componentId, UpdateProductionOrderComponentDto request, Guid userId)
    {
        var e = await _compRepo.GetByIdAsync(componentId) ?? throw new InvalidOperationException("Component not found");
        if (request.PlannedQty.HasValue) e.PlannedQty = request.PlannedQty.Value;
        if (request.IssuedQty.HasValue) e.IssuedQty = request.IssuedQty.Value;
        if (request.ReturnedQty.HasValue) e.ReturnedQty = request.ReturnedQty.Value;
        if (request.UnitOfMeasure != null) e.UnitOfMeasure = request.UnitOfMeasure;
        if (request.ScrapPercentage.HasValue) e.ScrapPercentage = request.ScrapPercentage.Value;
        if (request.IsSubstituted.HasValue) e.IsSubstituted = request.IsSubstituted.Value;
        if (request.OriginalMaterialId.HasValue) e.OriginalMaterialId = request.OriginalMaterialId;
        if (request.StorageLocationId.HasValue) e.StorageLocationId = request.StorageLocationId;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _compRepo.Update(e);
        await _compRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<ProductionOrderComponentDto>> GetComponentsByOrderAsync(Guid productionOrderId, PaginationParams pagination)
    {
        var (items, total) = await _compRepo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: c => c.ProductionOrderId == productionOrderId);
        return PaginatedResponse<ProductionOrderComponentDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }
}
