using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.DTOs;
using Inventory.Infrastructure.Repositories.Interfaces;
using Inventory.Infrastructure.Services;
using Inventory.Domain.Entities;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Api.Controllers;

/// <summary>
/// Inventory Documents - GRN, Issues, Transfers, Adjustments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryDocumentController : ControllerBase
{
    private readonly IInventoryDocumentRepository _documentRepository;
    private readonly IInventoryDocumentLineRepository _documentLineRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IInventoryBalanceService _balanceService;
    private readonly IItemTrackingService _trackingService;
    private readonly IItemRepository _itemRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<InventoryDocumentController> _logger;

    public InventoryDocumentController(
        IInventoryDocumentRepository documentRepository,
        IInventoryDocumentLineRepository documentLineRepository,
        IInventoryTransactionRepository transactionRepository,
        IInventoryBalanceService balanceService,
        IItemTrackingService trackingService,
        IItemRepository itemRepository,
        IWarehouseRepository warehouseRepository,
        ILogger<InventoryDocumentController> logger)
    {
        _documentRepository = documentRepository;
        _documentLineRepository = documentLineRepository;
        _transactionRepository = transactionRepository;
        _balanceService = balanceService;
        _trackingService = trackingService;
        _itemRepository = itemRepository;
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory documents
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams pagination,
        [FromQuery] string? documentType = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? warehouseId = null)
    {
        try
        {
            // Server-side filtering so search/type/status work alongside pagination.
            var term       = pagination.SearchTerm?.Trim();
            var hasTerm     = !string.IsNullOrEmpty(term);
            var hasType     = !string.IsNullOrEmpty(documentType);
            var hasStatus   = !string.IsNullOrEmpty(status);
            var typeUpper   = documentType?.Trim().ToUpper();
            var statusUpper = status?.Trim().ToUpper();
            // A single-store view needs its own documents only, and filtering after paging
            // would silently drop rows, so the warehouse has to be part of the predicate.
            var hasWarehouse = warehouseId.HasValue;

            var (documents, total) = await _documentRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: d =>
                    (!hasType   || d.DocumentType.ToUpper() == typeUpper!)
                 && (!hasStatus || d.Status.ToUpper() == statusUpper!)
                 && (!hasWarehouse || d.ToWarehouseId == warehouseId || d.FromWarehouseId == warehouseId)
                 && (!hasTerm   || d.DocumentNumber.Contains(term!) || (d.Description != null && d.Description.Contains(term!))),
                orderBy: q => q.ApplyOrderNewestFirst(
                    string.IsNullOrWhiteSpace(pagination.SortBy) ? "DocumentDate" : pagination.SortBy,
                    string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection,
                    "DocumentDate"));

            return Ok(PaginatedResponse<InventoryDocumentDto>.Ok(documents.Select(MapDocumentToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory documents");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving inventory documents" });
        }
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var document = await _documentRepository.GetWithLinesAsync(id);
            if (document == null)
                return NotFound(new ApiErrorResponse { Message = "Document not found" });

            return Ok(new ApiResponse<InventoryDocumentDto>
            {
                Success = true,
                Data = MapDocumentToDto(document),
                Message = "Document retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving document" });
        }
    }

    /// <summary>
    /// Get document by document number
    /// </summary>
    [HttpGet("by-number/{documentNumber}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string documentNumber)
    {
        try
        {
            var document = await _documentRepository.GetByNumberAsync(documentNumber);
            if (document == null)
                return NotFound(new ApiErrorResponse { Message = "Document not found" });

            return Ok(new ApiResponse<InventoryDocumentDto>
            {
                Success = true,
                Data = MapDocumentToDto(document),
                Message = "Document retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document by number {DocumentNumber}", documentNumber);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving document" });
        }
    }

    /// <summary>
    /// Create new inventory document (Draft)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InventoryDocumentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInventoryDocumentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            if (dto.ToWarehouseId.HasValue)
            {
                var toWarehouse = await _warehouseRepository.GetByIdAsync(dto.ToWarehouseId.Value);
                if (toWarehouse == null)
                    return BadRequest(new ApiErrorResponse { Message = "To warehouse not found" });
            }

            if (dto.FromWarehouseId.HasValue)
            {
                var fromWarehouse = await _warehouseRepository.GetByIdAsync(dto.FromWarehouseId.Value);
                if (fromWarehouse == null)
                    return BadRequest(new ApiErrorResponse { Message = "From warehouse not found" });
            }

            var document = new InventoryDocument
            {
                DocumentNumber = GenerateDocumentNumber(dto.DocumentType),
                DocumentType = dto.DocumentType,
                DocumentDate = dto.DocumentDate,
                Status = "Draft",
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                Description = dto.Description,
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                TotalQuantity = 0,
                TotalCost = 0
            };

            decimal totalQty = 0;
            decimal totalCost = 0;

            foreach (var lineDto in dto.Lines)
            {
                var item = await _itemRepository.GetByIdAsync(lineDto.ItemId);
                if (item == null)
                    return BadRequest(new ApiErrorResponse { Message = $"Item {lineDto.ItemId} not found" });

                var line = new InventoryDocumentLine
                {
                    ItemId = lineDto.ItemId,
                    WarehouseId = lineDto.WarehouseId,
                    BinId = lineDto.BinId,
                    VariantId = lineDto.VariantId,
                    Quantity = lineDto.Quantity,
                    UnitId = lineDto.UnitId,
                    UnitCost = lineDto.UnitCost,
                    TotalCost = lineDto.Quantity * lineDto.UnitCost,
                    LineNumber = lineDto.LineNumber,
                    Description = lineDto.Description,
                    ReferenceLineId = lineDto.ReferenceLineId,
                    // Lot capture (lot-tracked items)
                    BatchNumber = string.IsNullOrWhiteSpace(lineDto.BatchNumber) ? null : lineDto.BatchNumber.Trim(),
                    ManufactureDate = lineDto.ManufactureDate,
                    ExpiryDate = lineDto.ExpiryDate,
                };

                // Serial capture (serial-tracked items) — the specific units on this line.
                foreach (var s in lineDto.Serials)
                {
                    if (string.IsNullOrWhiteSpace(s.SerialNumber)) continue;
                    line.LineSerials.Add(new InventoryDocumentLineSerial
                    {
                        SerialNumber = s.SerialNumber.Trim(),
                        Imei = s.Imei,
                        Imei2 = s.Imei2,
                        MacAddress = s.MacAddress,
                    });
                }

                document.Lines.Add(line);
                totalQty += lineDto.Quantity;
                totalCost += line.TotalCost;
            }

            document.TotalQuantity = totalQty;
            document.TotalCost = totalCost;

            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = document.Id },
                new ApiResponse<InventoryDocumentDto>
                {
                    Success = true,
                    Data = MapDocumentToDto(document),
                    Message = "Document created successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory document");
            return StatusCode(500, new ApiErrorResponse { Message = "Error creating inventory document" });
        }
    }

    /// <summary>
    /// Post (finalize) inventory document - creates transactions and updates balances
    /// </summary>
    [HttpPost("{id}/post")]
    [ProducesResponseType(typeof(ApiResponse<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostDocument(Guid id, [FromBody] PostInventoryDocumentDto dto)
    {
        try
        {
            var document = await _documentRepository.GetWithLinesAsync(id);
            if (document == null)
                return NotFound(new ApiErrorResponse { Message = "Document not found" });

            if (document.Status != "Draft")
                return BadRequest(new ApiErrorResponse { Message = "Only draft documents can be posted" });

            var postingUserId = dto.PostedByUserId ?? document.CreatedByUserId;

            foreach (var line in document.Lines)
            {
                // Sign convention preserved from the original logic: only "Issue" documents are outbound.
                var signedQty = document.DocumentType == "Issue" ? -line.Quantity : line.Quantity;

                // Resolve the item's tracking mode so Serial/Lot registry rows are created alongside the ledger.
                var item = await _itemRepository.GetByIdAsync(line.ItemId);
                var trackingType = item?.TrackingType ?? Inventory.Domain.Constants.ItemTrackingType.None;

                // The tracking service creates the transaction(s) + any Serial/Lot identity rows on the
                // shared scoped context (committed with this document's SaveChanges below).
                await _trackingService.ApplyPostingAsync(
                    document, line, trackingType, signedQty,
                    GetTransactionType(document.DocumentType), dto.PostingDate, postingUserId);

                // Balance keying + moving-average via the shared service (same path POS deduction uses),
                // keyed on the document's tenant so inbound/outbound always reconcile. The full signed
                // line quantity is applied once regardless of how the units were tracked.
                await _balanceService.ApplyMovementAsync(new StockMovement(
                    ItemId:         line.ItemId,
                    WarehouseId:    line.WarehouseId,
                    BinId:          line.BinId,
                    VariantId:      line.VariantId,
                    SignedQuantity: signedQty,
                    UnitCost:       line.UnitCost,
                    CompanyId:      document.CompanyId,
                    BranchId:       document.BranchId,
                    BusinessUnitId: document.BusinessUnitId,
                    CreatedByUserId: postingUserId,
                    OccurredAtUtc:  dto.PostingDate));
            }

            document.Status = "Posted";
            document.PostingDate = dto.PostingDate;
            document.PostedByUserId = dto.PostedByUserId;
            _documentRepository.Update(document);

            await _documentRepository.SaveChangesAsync();

            return Ok(new ApiResponse<InventoryDocumentDto>
            {
                Success = true,
                Data = MapDocumentToDto(document),
                Message = "Document posted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting inventory document {DocumentId}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error posting inventory document" });
        }
    }

    /// <summary>
    /// Get documents by status
    /// </summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var (documents, total) = await _documentRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: d => d.Status == status);
            return Ok(PaginatedResponse<InventoryDocumentDto>.Ok(documents.Select(MapDocumentToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents by status");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving documents" });
        }
    }

    /// <summary>
    /// Get documents by type
    /// </summary>
    [HttpGet("by-type/{documentType}")]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByType(string documentType, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var (documents, total) = await _documentRepository.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: d => d.DocumentType == documentType);
            return Ok(PaginatedResponse<InventoryDocumentDto>.Ok(documents.Select(MapDocumentToDto), total, pagination.PageNumber, pagination.PageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents by type");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving documents" });
        }
    }

    /// <summary>
    /// Quick adjust — set item qty in a warehouse to a target value. Creates and posts an Adjustment document atomically.
    /// </summary>
    [HttpPost("quick-adjust")]
    [ProducesResponseType(typeof(ApiResponse<InventoryDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QuickAdjust([FromBody] QuickAdjustDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var item = await _itemRepository.GetByIdAsync(dto.ItemId);
            if (item == null)
                return NotFound(new ApiErrorResponse { Message = "Item not found" });

            var warehouse = await _warehouseRepository.GetByIdAsync(dto.WarehouseId);
            if (warehouse == null)
                return NotFound(new ApiErrorResponse { Message = "Warehouse not found" });

            var (companyId, branchId, businessUnitIdNullable) = TenantContextHelper.ExtractTenantContext(User);
            var businessUnitId = businessUnitIdNullable ?? Guid.Empty;
            // Audit user is optional — don't fail the adjustment if the token has no (or a non-GUID) user id claim.
            Guid userId = Guid.Empty;
            try { userId = TenantContextHelper.ExtractUserId(User); } catch { /* leave Guid.Empty */ }

            var existing = await _balanceService.FindBalanceAsync(
                dto.ItemId, dto.WarehouseId, binId: null, variantId: dto.VariantId, companyId);
            var currentQty = existing?.QuantityOnHand ?? 0m;
            var delta = dto.NewQuantity - currentQty;

            if (delta == 0m)
                return Ok(new ApiResponse<InventoryDocumentDto>
                {
                    Success = true,
                    Data = null,
                    Message = $"Quantity is already {dto.NewQuantity:N2} — no adjustment needed"
                });

            var unitCost = (dto.UnitCost.HasValue && dto.UnitCost.Value > 0)
                ? dto.UnitCost.Value
                : (existing?.AverageCost ?? 0m);
            var now = DateTime.UtcNow;

            var document = new InventoryDocument
            {
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                CreatedByUserId = userId,
                DocumentNumber = GenerateDocumentNumber("ADJUSTMENT"),
                DocumentType = "Adjustment",
                DocumentDate = now,
                Status = "Draft",
                Description = string.IsNullOrWhiteSpace(dto.Reason) ? "Quick quantity adjustment" : dto.Reason,
                TotalQuantity = delta,
                TotalCost = delta * unitCost
            };

            var line = new InventoryDocumentLine
            {
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                CreatedByUserId = userId,
                ItemId = dto.ItemId,
                WarehouseId = dto.WarehouseId,
                VariantId = dto.VariantId,
                Quantity = delta,
                UnitId = item.BaseUnitId,
                UnitCost = unitCost,
                TotalCost = delta * unitCost,
                LineNumber = 1,
                Description = dto.Reason
            };
            document.Lines.Add(line);
            await _documentRepository.AddAsync(document);

            var transaction = new InventoryTransaction
            {
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                CreatedByUserId = userId,
                ItemId = dto.ItemId,
                WarehouseId = dto.WarehouseId,
                VariantId = dto.VariantId,
                TransactionType = "ADJUSTMENT",
                Quantity = delta,
                UnitId = item.BaseUnitId,
                UnitCost = unitCost,
                TotalCost = delta * unitCost,
                DocumentId = document.Id,
                DocumentLineId = line.Id,
                TransactionDate = now
            };
            await _transactionRepository.AddAsync(transaction);

            // Apply the signed delta through the shared service (sets on-hand to the target qty).
            await _balanceService.ApplyMovementAsync(new StockMovement(
                ItemId:         dto.ItemId,
                WarehouseId:    dto.WarehouseId,
                BinId:          null,
                VariantId:      dto.VariantId,
                SignedQuantity: delta,
                UnitCost:       unitCost,
                CompanyId:      companyId,
                BranchId:       branchId,
                BusinessUnitId: businessUnitId,
                CreatedByUserId: userId,
                OccurredAtUtc:  now));

            document.Status = "Posted";
            document.PostingDate = now;

            await _documentRepository.SaveChangesAsync();

            return Ok(new ApiResponse<InventoryDocumentDto>
            {
                Success = true,
                Data = MapDocumentToDto(document),
                Message = $"Quantity adjusted from {currentQty:N2} to {dto.NewQuantity:N2}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in quick adjust for item {ItemId} warehouse {WarehouseId}", dto.ItemId, dto.WarehouseId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error performing quick adjustment" });
        }
    }

    private InventoryDocumentDto MapDocumentToDto(InventoryDocument document)
    {
        return new InventoryDocumentDto
        {
            Id = document.Id,
            DocumentNumber = document.DocumentNumber,
            DocumentType = document.DocumentType,
            DocumentDate = document.DocumentDate,
            Status = document.Status,
            ReferenceType = document.ReferenceType,
            ReferenceId = document.ReferenceId,
            Description = document.Description,
            FromWarehouseId = document.FromWarehouseId,
            ToWarehouseId = document.ToWarehouseId,
            PostingDate = document.PostingDate,
            PostedByUserId = document.PostedByUserId,
            TotalQuantity = document.TotalQuantity,
            TotalCost = document.TotalCost,
            Lines = document.Lines.Select(l => new InventoryDocumentLineDto
            {
                Id = l.Id,
                DocumentId = l.DocumentId,
                ItemId = l.ItemId,
                WarehouseId = l.WarehouseId,
                BinId = l.BinId,
                VariantId = l.VariantId,
                Quantity = l.Quantity,
                UnitId = l.UnitId,
                UnitCost = l.UnitCost,
                TotalCost = l.TotalCost,
                LineNumber = l.LineNumber,
                Description = l.Description,
                ReferenceLineId = l.ReferenceLineId,
                BatchNumber = l.BatchNumber,
                ManufactureDate = l.ManufactureDate,
                ExpiryDate = l.ExpiryDate,
                Serials = l.LineSerials.Select(s => new DocumentLineSerialDto
                {
                    Id = s.Id,
                    SerialNumber = s.SerialNumber,
                    Imei = s.Imei,
                    Imei2 = s.Imei2,
                    MacAddress = s.MacAddress,
                }).ToList()
            }).ToList()
        };
    }

    private static string GenerateDocumentNumber(string documentType)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd");
        var random = Random.Shared.Next(1000);
        return $"{documentType[..3].ToUpper()}-{timestamp}-{random:D3}";
    }

    private static string GetTransactionType(string documentType) => documentType switch
    {
        "GRN" => "IN",
        "Issue" => "OUT",
        "Delivery" => "OUT",
        "Transfer" => "TRANSFER_OUT",
        "Adjustment" => "ADJUSTMENT",
        _ => "IN"
    };
}
