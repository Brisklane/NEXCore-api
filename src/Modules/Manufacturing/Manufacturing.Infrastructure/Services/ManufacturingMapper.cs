using Manufacturing.Application.DTOs;
using Manufacturing.Domain.Entities;

namespace Manufacturing.Infrastructure.Services;

/// <summary>
/// Centralised mapping helpers — entities to DTOs — for all Manufacturing services.
/// </summary>
internal static class ManufacturingMapper
{
    // ---- WorkCenter ----
    internal static WorkCenterDto ToDto(WorkCenter e) => new()
    {
        Id = e.Id, Code = e.Code, Name = e.Name,
        CapacityPerHour = e.CapacityPerHour, HourlyMachineCost = e.HourlyMachineCost,
        IsActive = e.IsActive, Description = e.Description,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    internal static WorkCenterShiftDto ToDto(WorkCenterShift e) => new()
    {
        Id = e.Id, WorkCenterId = e.WorkCenterId, ShiftName = e.ShiftName,
        StartTime = e.StartTime, EndTime = e.EndTime,
        AvailableHours = e.AvailableHours, CapacityUtilizationPercent = e.CapacityUtilizationPercent,
        WorkingDays = e.WorkingDays, IsActive = e.IsActive,
        EffectiveFrom = e.EffectiveFrom, EffectiveTo = e.EffectiveTo,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- BOM ----
    internal static BillOfMaterialDto ToDto(BillOfMaterial e) => new()
    {
        Id = e.Id, FinishedProductId = e.FinishedProductId, Version = e.Version,
        IsActive = e.IsActive, EffectiveFrom = e.EffectiveFrom, EffectiveTo = e.EffectiveTo,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
        Items = e.Items?.Select(ToDto).ToList() ?? [],
        ByProducts = e.ByProducts?.Select(ToDto).ToList() ?? []
    };

    internal static BOMItemDto ToDto(BOMItem e) => new()
    {
        Id = e.Id, BillOfMaterialId = e.BillOfMaterialId, MaterialId = e.MaterialId,
        QuantityRequired = e.QuantityRequired, ScrapPercentage = e.ScrapPercentage,
        UnitOfMeasure = e.UnitOfMeasure, Notes = e.Notes
    };

    internal static BOMByProductDto ToDto(BOMByProduct e) => new()
    {
        Id = e.Id, BillOfMaterialId = e.BillOfMaterialId, ProductId = e.ProductId,
        Type = e.Type, Quantity = e.Quantity, UnitOfMeasure = e.UnitOfMeasure,
        CostAllocationPercent = e.CostAllocationPercent, WarehouseId = e.WarehouseId, Notes = e.Notes
    };

    // ---- Routing ----
    internal static RoutingDto ToDto(Routing e) => new()
    {
        Id = e.Id, ProductId = e.ProductId, Name = e.Name, Version = e.Version,
        IsActive = e.IsActive, Description = e.Description,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
        Operations = e.Operations?.Select(ToDto).ToList() ?? []
    };

    internal static RoutingOperationDto ToDto(RoutingOperation e) => new()
    {
        Id = e.Id, RoutingId = e.RoutingId, SequenceNo = e.SequenceNo,
        OperationName = e.OperationName, WorkCenterId = e.WorkCenterId,
        StandardHours = e.StandardHours, SetupHours = e.SetupHours,
        LaborHours = e.LaborHours, MachineHours = e.MachineHours, Notes = e.Notes
    };

    // ---- Planned Order ----
    internal static PlannedOrderDto ToDto(PlannedOrder e) => new()
    {
        Id = e.Id, ProductId = e.ProductId, PlannedQty = e.PlannedQty,
        RequiredDate = e.RequiredDate, SourceType = e.SourceType, Status = e.Status,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Production Order ----
    internal static ProductionOrderDto ToDto(ProductionOrder e) => new()
    {
        Id = e.Id, OrderNumber = e.OrderNumber, ProductId = e.ProductId,
        BillOfMaterialId = e.BillOfMaterialId, RoutingId = e.RoutingId,
        PlannedOrderId = e.PlannedOrderId, QuantityPlanned = e.QuantityPlanned,
        QuantityProduced = e.QuantityProduced, QuantityRejected = e.QuantityRejected,
        Status = e.Status, StartDate = e.StartDate, EndDate = e.EndDate, DueDate = e.DueDate,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    internal static ProductionOrderOperationDto ToDto(ProductionOrderOperation e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId,
        RoutingOperationId = e.RoutingOperationId, WorkCenterId = e.WorkCenterId,
        SequenceNo = e.SequenceNo, OperationName = e.OperationName,
        PlannedSetupHours = e.PlannedSetupHours, PlannedLaborHours = e.PlannedLaborHours,
        PlannedMachineHours = e.PlannedMachineHours, ActualSetupHours = e.ActualSetupHours,
        ActualLaborHours = e.ActualLaborHours, ActualMachineHours = e.ActualMachineHours,
        ConfirmedQty = e.ConfirmedQty, ScrapQty = e.ScrapQty, Status = e.Status,
        StartedAt = e.StartedAt, CompletedAt = e.CompletedAt, ConfirmedById = e.ConfirmedById,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    internal static ProductionOrderComponentDto ToDto(ProductionOrderComponent e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId, BOMItemId = e.BOMItemId,
        MaterialId = e.MaterialId, PlannedQty = e.PlannedQty, IssuedQty = e.IssuedQty,
        ReturnedQty = e.ReturnedQty, UnitOfMeasure = e.UnitOfMeasure,
        ScrapPercentage = e.ScrapPercentage, IsSubstituted = e.IsSubstituted,
        OriginalMaterialId = e.OriginalMaterialId, StorageLocationId = e.StorageLocationId,
        IsManuallyAdded = e.IsManuallyAdded, Notes = e.Notes,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Execution ----
    internal static MaterialIssueDto ToDto(MaterialIssue e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId, MaterialId = e.MaterialId,
        QuantityIssued = e.QuantityIssued, QuantityReturned = e.QuantityReturned,
        IssuedAt = e.IssuedAt, IssuedById = e.IssuedById,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    internal static WorkInProgressDto ToDto(WorkInProgress e) => new()
    {
        Id = e.Id, 
        ProductionOrderId = e.ProductionOrderId,
        ProductionOrderNumber = e.ProductionOrder?.OrderNumber,
        ProductId = e.ProductionOrder?.ProductId,
        ProductName = null, // Will be populated by frontend from ProductId lookup
        QuantityInProgress = e.QuantityInProgress, 
        QuantityCompleted = e.QuantityCompleted,
        QuantityRejected = e.QuantityRejected, 
        LastUpdatedAt = e.LastUpdatedAt,
        Notes = e.Notes, 
        CreatedAt = e.CreatedAt, 
        UpdatedAt = e.UpdatedAt
    };

    internal static SubContractOrderDto ToDto(SubContractOrder e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId,
        ProductionOrderOperationId = e.ProductionOrderOperationId, VendorId = e.VendorId,
        PurchaseOrderId = e.PurchaseOrderId, QuantitySent = e.QuantitySent,
        QuantityReceived = e.QuantityReceived, QuantityRejected = e.QuantityRejected,
        SentAt = e.SentAt, ExpectedReturnDate = e.ExpectedReturnDate,
        ActualReturnDate = e.ActualReturnDate, UnitCost = e.UnitCost, TotalCost = e.TotalCost,
        Status = e.Status, Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    internal static ProductionScheduleDto ToDto(ProductionSchedule e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId, WorkCenterId = e.WorkCenterId,
        ProductionOrderOperationId = e.ProductionOrderOperationId,
        ScheduledStartDate = e.ScheduledStartDate, ScheduledEndDate = e.ScheduledEndDate,
        ScheduleType = e.ScheduleType, CapacityRequiredHours = e.CapacityRequiredHours,
        Status = e.Status, HasCapacityConflict = e.HasCapacityConflict,
        ConflictDescription = e.ConflictDescription,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Quality ----
    internal static InspectionDto ToDto(Inspection e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId,
        InspectedQty = e.InspectedQty, PassedQty = e.PassedQty, RejectedQty = e.RejectedQty,
        Status = e.Status, InspectedById = e.InspectedById, InspectedAt = e.InspectedAt,
        Remarks = e.Remarks, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
        Characteristics = e.Characteristics?.Select(ToDto).ToList() ?? []
    };

    internal static InspectionCharacteristicDto ToDto(InspectionCharacteristic e) => new()
    {
        Id = e.Id, InspectionId = e.InspectionId, CharacteristicName = e.CharacteristicName,
        InspectionType = e.InspectionType, UnitOfMeasure = e.UnitOfMeasure,
        TargetValue = e.TargetValue, UpperTolerance = e.UpperTolerance,
        LowerTolerance = e.LowerTolerance, ActualValue = e.ActualValue,
        QualitativeResult = e.QualitativeResult, Result = e.Result,
        IsCritical = e.IsCritical, SampleSize = e.SampleSize, Remarks = e.Remarks
    };

    internal static FinishedGoodsReceiptDto ToDto(FinishedGoodsReceipt e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId, ProductId = e.ProductId,
        QuantityReceived = e.QuantityReceived, WarehouseId = e.WarehouseId,
        BatchNo = e.BatchNo, ReceivedAt = e.ReceivedAt,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    internal static ProductionBatchDto ToDto(ProductionBatch e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId, ProductId = e.ProductId,
        BatchNumber = e.BatchNumber, ManufacturingDate = e.ManufacturingDate,
        ExpiryDate = e.ExpiryDate, ReTestDate = e.ReTestDate,
        Quantity = e.Quantity, UnitOfMeasure = e.UnitOfMeasure, Status = e.Status,
        WarehouseId = e.WarehouseId, CertificateOfAnalysis = e.CertificateOfAnalysis,
        VendorBatchNumber = e.VendorBatchNumber, QualityApproved = e.QualityApproved,
        InspectionId = e.InspectionId, Notes = e.Notes,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Costing ----
    internal static CostEntryDto ToDto(CostEntry e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId,
        MaterialCost = e.MaterialCost, LaborCost = e.LaborCost,
        MachineCost = e.MachineCost, OverheadCost = e.OverheadCost,
        ScrapCost = e.ScrapCost, TotalCost = e.TotalCost,
        JournalEntryId = e.JournalEntryId, PostedAt = e.PostedAt,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    internal static ProductionVarianceDto ToDto(ProductionVariance e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId, CostEntryId = e.CostEntryId,
        StandardMaterialCost = e.StandardMaterialCost, ActualMaterialCost = e.ActualMaterialCost,
        MaterialVariance = e.MaterialVariance,
        StandardLaborCost = e.StandardLaborCost, ActualLaborCost = e.ActualLaborCost,
        LaborVariance = e.LaborVariance,
        StandardMachineCost = e.StandardMachineCost, ActualMachineCost = e.ActualMachineCost,
        MachineVariance = e.MachineVariance,
        StandardOverheadCost = e.StandardOverheadCost, ActualOverheadCost = e.ActualOverheadCost,
        OverheadVariance = e.OverheadVariance, TotalVariance = e.TotalVariance,
        VarianceCategory = e.VarianceCategory, IsSettled = e.IsSettled,
        SettlementJournalEntryId = e.SettlementJournalEntryId, SettledAt = e.SettledAt,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Maintenance ----
    internal static MachineDowntimeDto ToDto(MachineDowntime e) => new()
    {
        Id = e.Id, WorkCenterId = e.WorkCenterId, ProductionOrderId = e.ProductionOrderId,
        StartTime = e.StartTime, EndTime = e.EndTime, DurationHours = e.DurationHours,
        Category = e.Category, Reason = e.Reason, RootCause = e.RootCause,
        Resolution = e.Resolution, ReportedById = e.ReportedById, ResolvedById = e.ResolvedById,
        Status = e.Status, MaintenanceWorkOrderId = e.MaintenanceWorkOrderId,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Rework ----
    internal static ReworkOrderDto ToDto(ReworkOrder e) => new()
    {
        Id = e.Id, ProductionOrderId = e.ProductionOrderId,
        InspectionId = e.InspectionId, ReworkRoutingId = e.ReworkRoutingId,
        Quantity = e.Quantity, QuantityCompleted = e.QuantityCompleted,
        QuantityRejected = e.QuantityRejected, UnitOfMeasure = e.UnitOfMeasure,
        Reason = e.Reason, Status = e.Status,
        ScheduledStartDate = e.ScheduledStartDate, ScheduledEndDate = e.ScheduledEndDate,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Material Planning Data ----
    internal static MaterialPlanningDataDto ToDto(MaterialPlanningData e) => new()
    {
        Id = e.Id, ProductId = e.ProductId,
        SafetyStock = e.SafetyStock, ReorderPoint = e.ReorderPoint,
        MaximumStockLevel = e.MaximumStockLevel, LotSize = e.LotSize,
        LeadTimeDays = e.LeadTimeDays, PlanningHorizonDays = e.PlanningHorizonDays,
        ScrapPercentage = e.ScrapPercentage, ProcurementType = e.ProcurementType,
        MRPType = e.MRPType, IsActive = e.IsActive,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Standard Cost ----
    internal static StandardCostDto ToDto(StandardCost e) => new()
    {
        Id = e.Id, ProductId = e.ProductId, Version = e.Version,
        CurrencyCode = e.CurrencyCode, MaterialCost = e.MaterialCost,
        LaborCost = e.LaborCost, MachineCost = e.MachineCost,
        OverheadCost = e.OverheadCost, TotalCost = e.TotalCost,
        IsActive = e.IsActive, EffectiveFrom = e.EffectiveFrom, EffectiveTo = e.EffectiveTo,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Overhead Rule ----
    internal static OverheadRuleDto ToDto(OverheadRule e) => new()
    {
        Id = e.Id, Code = e.Code, Name = e.Name,
        WorkCenterId = e.WorkCenterId, RateType = e.RateType,
        Value = e.Value, AppliesTo = e.AppliesTo, IsActive = e.IsActive,
        EffectiveFrom = e.EffectiveFrom, EffectiveTo = e.EffectiveTo,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Capacity Load ----
    internal static CapacityLoadDto ToDto(CapacityLoad e) => new()
    {
        Id = e.Id, WorkCenterId = e.WorkCenterId, WorkCenterShiftId = e.WorkCenterShiftId,
        Date = e.Date, RequiredHours = e.RequiredHours, AvailableHours = e.AvailableHours,
        LoadPercentage = e.LoadPercentage, ProductionOrderCount = e.ProductionOrderCount,
        IsOverloaded = e.IsOverloaded,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Demand ----
    internal static DemandDto ToDto(Demand e) => new()
    {
        Id = e.Id, ProductId = e.ProductId, Quantity = e.Quantity,
        FulfilledQty = e.FulfilledQty, DueDate = e.DueDate,
        SourceType = e.SourceType, ReferenceId = e.ReferenceId,
        Status = e.Status, Notes = e.Notes,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    // ---- Inventory Transaction ----
    internal static InventoryTransactionDto ToDto(InventoryTransaction e) => new()
    {
        Id = e.Id, ProductId = e.ProductId, Quantity = e.Quantity,
        TransactionType = e.TransactionType, ReferenceId = e.ReferenceId,
        ReferenceType = e.ReferenceType, TransactionDate = e.TransactionDate,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
