using Distribution.Application.DTOs;
using Distribution.Domain.Enums;
using Nexcore.SharedKernel.Api;

namespace Distribution.Application.Services.Interfaces;

/// <summary>
/// The channel network and the retail universe — partners, outlets, assets.
///
/// Outlet creation is split deliberately: <see cref="SaveOutletAsync"/> is the back-office path
/// and produces an approved record, while <see cref="OnboardOutletAsync"/> is the doorstep path
/// and produces a pending one. Merging the two would let a rep create billable customers from a
/// phone with nobody checking.
/// </summary>
public interface INetworkService
{
    // ── Partners ─────────────────────────────────────────────────────────────
    Task<PaginatedResponse<PartnerDto>> ListPartnersAsync(
        string? search, PartnerType? type, PartnerStatus? status, Guid? territoryId,
        Guid? parentPartnerId, PaginationParams pagination);

    Task<PartnerDto?> GetPartnerAsync(Guid partnerId);
    Task<List<PartnerTreeNodeDto>> GetPartnerTreeAsync(Guid? rootPartnerId);
    Task<PartnerDto> SavePartnerAsync(Guid? partnerId, SavePartnerDto request, Guid userId);
    Task<PartnerDto> ChangePartnerStatusAsync(Guid partnerId, ChangePartnerStatusDto request, Guid userId);
    Task DeletePartnerAsync(Guid partnerId, Guid userId);

    Task<List<PartnerDocumentDto>> GetPartnerDocumentsAsync(Guid partnerId);
    Task<PartnerDocumentDto> SavePartnerDocumentAsync(Guid partnerId, PartnerDocumentDto request, Guid userId);
    Task<PartnerDocumentDto> VerifyPartnerDocumentAsync(Guid documentId, string? note, Guid userId);

    /// <summary>Licences and registrations expiring inside the window, across partners and outlets.</summary>
    Task<List<PartnerDocumentDto>> GetExpiringDocumentsAsync(int withinDays);

    // ── Outlets ──────────────────────────────────────────────────────────────
    Task<PaginatedResponse<OutletDto>> ListOutletsAsync(
        string? search, OutletChannel? channel, OutletGrade? grade, OutletStatus? status,
        Guid? partnerId, Guid? territoryId, Guid? routeId, bool? pendingApprovalOnly,
        PaginationParams pagination);

    Task<OutletDto?> GetOutletAsync(Guid outletId);
    Task<Outlet360Dto?> GetOutlet360Async(Guid outletId);
    Task<OutletDto> SaveOutletAsync(Guid? outletId, SaveOutletDto request, Guid userId);

    /// <summary>Doorstep onboarding. Lands pending approval and cannot be invoiced until cleared.</summary>
    Task<OutletDto> OnboardOutletAsync(OnboardOutletDto request, Guid userId);

    /// <summary>
    /// Run before onboarding. Returns candidates rather than blocking, because the rep standing
    /// in the shop is the only one who can tell a genuine second branch from a duplicate.
    /// </summary>
    Task<List<DuplicateCandidateDto>> FindDuplicateOutletsAsync(
        string? phone, string? taxNumber, string? name, double? latitude, double? longitude);

    Task<OutletDto> ApproveOutletAsync(Guid outletId, bool isApproved, string? reason, Guid userId);
    Task<OutletDto> ChangeOutletStatusAsync(Guid outletId, OutletStatus status, string? reason, Guid userId);

    /// <summary>Folds a duplicate into the survivor, moving history rather than losing it.</summary>
    Task<OutletDto> MergeOutletsAsync(Guid survivorId, Guid duplicateId, Guid userId);
    Task DeleteOutletAsync(Guid outletId, Guid userId);

    // ── Assets, photos and notes ─────────────────────────────────────────────
    Task<List<OutletAssetDto>> ListAssetsAsync(Guid? outletId, OutletAssetKind? kind, AssetCondition? condition);
    Task<OutletAssetDto> SaveAssetAsync(Guid? assetId, OutletAssetDto request, Guid userId);
    Task<OutletAssetDto> VerifyAssetAsync(Guid assetId, AssetCondition condition, string? photoUrl, string? note, Guid userId);

    Task<List<OutletPhotoDto>> ListPhotosAsync(Guid outletId, string? tag);
    Task<OutletPhotoDto> AddPhotoAsync(OutletPhotoDto request, Guid userId);

    Task<List<OutletNoteDto>> ListNotesAsync(Guid outletId);
    Task<OutletNoteDto> AddNoteAsync(OutletNoteDto request, Guid userId);
}

/// <summary>
/// Geography, territories, routes and the journey plan.
///
/// The route is the unit of work in distribution, so everything that changes one — resequencing,
/// reassignment, adding outlets — goes through here, and reassignment writes history rather than
/// overwriting it so last quarter still attributes to whoever actually walked the beat.
/// </summary>
public interface IRouteService
{
    // ── Geography ────────────────────────────────────────────────────────────
    Task<List<GeoNodeDto>> GetGeoTreeAsync(Guid? rootId);
    Task<GeoNodeDto> SaveGeoNodeAsync(Guid? nodeId, GeoNodeDto request, Guid userId);
    Task DeleteGeoNodeAsync(Guid nodeId, Guid userId);

    // ── Territories ──────────────────────────────────────────────────────────
    Task<List<TerritoryDto>> GetTerritoryTreeAsync(Guid? rootId);
    Task<PaginatedResponse<TerritoryDto>> ListTerritoriesAsync(string? search, PaginationParams pagination);
    Task<TerritoryDto?> GetTerritoryAsync(Guid territoryId);
    Task<TerritoryDto> SaveTerritoryAsync(Guid? territoryId, SaveTerritoryDto request, Guid userId);
    Task DeleteTerritoryAsync(Guid territoryId, Guid userId);

    // ── Routes ───────────────────────────────────────────────────────────────
    Task<PaginatedResponse<RouteDto>> ListRoutesAsync(
        string? search, RouteKind? kind, Guid? territoryId, Guid? fieldRepId, Guid? partnerId,
        PaginationParams pagination);

    Task<RouteDto?> GetRouteAsync(Guid routeId);
    Task<RouteDto> SaveRouteAsync(Guid? routeId, SaveRouteDto request, Guid userId);
    Task DeleteRouteAsync(Guid routeId, Guid userId);

    Task<RouteDto> AddOutletsAsync(AddOutletsToRouteDto request, Guid userId);
    Task<RouteDto> RemoveOutletAsync(Guid routeId, Guid outletId, Guid userId);
    Task<RouteDto> ResequenceAsync(ResequenceRouteDto request, Guid userId);
    Task<RouteDto> AssignAsync(AssignRouteDto request, Guid userId);

    /// <summary>Splits a beat that has outgrown a day; the second half becomes a new route.</summary>
    Task<List<RouteDto>> SplitRouteAsync(Guid routeId, List<Guid> outletIdsToMove, string newRouteName, Guid userId);

    /// <summary>
    /// Outlets nobody is scheduled to visit — the most common silent revenue leak in distribution.
    /// </summary>
    Task<PaginatedResponse<OutletDto>> GetUnroutedOutletsAsync(Guid? territoryId, PaginationParams pagination);

    // ── Journey plans ────────────────────────────────────────────────────────
    Task<JourneyPlanDto?> GetJourneyPlanAsync(Guid fieldRepId, DateTime periodStart);
    Task<List<JourneyPlanDto>> ListJourneyPlansAsync(Guid? territoryId, DateTime periodStart);
    Task<JourneyPlanDto> GenerateJourneyPlanAsync(GenerateJourneyPlanDto request, Guid userId);
    Task<JourneyPlanDto> PublishJourneyPlanAsync(Guid planId, Guid userId);
    Task<JourneyPlanDayDto> UpdateJourneyPlanDayAsync(Guid dayId, UpdateJourneyPlanDayDto request, Guid userId);
}

/// <summary>
/// The field terminal: the day, the beat, the visit, and everything captured at the counter.
///
/// Every write here is idempotent by client key. A rep on a bad connection will retry, and a
/// double-submitted check-in that becomes two visits quietly corrupts coverage and strike rate
/// for the whole month.
/// </summary>
public interface IFieldService
{
    // ── Reps & devices ───────────────────────────────────────────────────────
    Task<PaginatedResponse<FieldRepDto>> ListRepsAsync(
        string? search, FieldRole? role, Guid? territoryId, Guid? partnerId, PaginationParams pagination);

    Task<FieldRepDto?> GetRepAsync(Guid repId);
    Task<FieldRepDto> SaveRepAsync(Guid? repId, SaveFieldRepDto request, Guid userId);
    Task DeleteRepAsync(Guid repId, Guid userId);

    /// <summary>PIN login on a shared device. Returns null when the PIN does not match.</summary>
    Task<FieldRepDto?> AuthenticatePinAsync(string code, string pin);

    Task<List<FieldDeviceDto>> ListDevicesAsync(Guid? fieldRepId, bool? staleOnly);
    Task<FieldDeviceDto> RegisterDeviceAsync(FieldDeviceDto request, Guid userId);
    Task<FieldDeviceDto> BlockDeviceAsync(Guid deviceId, bool isBlocked, string? reason, Guid userId);
    Task<FieldDeviceDto> RequestWipeAsync(Guid deviceId, Guid userId);

    // ── The day ──────────────────────────────────────────────────────────────
    Task<FieldDayDto> StartDayAsync(StartDayDto request, Guid userId);
    Task<FieldDayDto?> GetDayAsync(Guid fieldDayId);
    Task<FieldDayDto?> GetTodayAsync(Guid fieldRepId, DateTime? workDate);

    /// <summary>The field terminal's boot call: the whole day in one round trip.</summary>
    Task<FieldDayBoardDto?> GetDayBoardAsync(Guid fieldDayId);

    Task<FieldDayDto> CloseDayAsync(CloseDayDto request, Guid userId);
    Task<PaginatedResponse<FieldDayDto>> ListDaysAsync(
        Guid? fieldRepId, Guid? routeId, DateTime? from, DateTime? to, FieldDayStatus? status,
        PaginationParams pagination);

    // ── Visits ───────────────────────────────────────────────────────────────
    Task<VisitDto> CheckInAsync(CheckInDto request, Guid userId);
    Task<VisitDto> CheckOutAsync(CheckOutDto request, Guid userId);
    Task<VisitDto?> GetVisitAsync(Guid visitId);
    Task<PaginatedResponse<VisitSummaryDto>> ListVisitsAsync(
        Guid? fieldRepId, Guid? outletId, Guid? routeId, DateTime? from, DateTime? to,
        VisitStatus? status, bool? outOfFenceOnly, PaginationParams pagination);

    // ── Tasks ────────────────────────────────────────────────────────────────
    Task<List<VisitTaskDto>> ListTasksAsync(Guid? fieldRepId, Guid? outletId, Guid? routeId, bool? openOnly);
    Task<VisitTaskDto> SaveTaskAsync(Guid? taskId, SaveVisitTaskDto request, Guid userId);
    Task<VisitTaskDto> CompleteTaskAsync(CompleteVisitTaskDto request, Guid userId);
    Task DeleteTaskAsync(Guid taskId, Guid userId);

    // ── Surveys ──────────────────────────────────────────────────────────────
    Task<List<SurveyFormDto>> ListSurveyFormsAsync(bool? activeOnly);
    Task<SurveyFormDto?> GetSurveyFormAsync(Guid formId);
    Task<SurveyFormDto> SaveSurveyFormAsync(Guid? formId, SurveyFormDto request, Guid userId);
    Task DeleteSurveyFormAsync(Guid formId, Guid userId);
    Task<SurveyResponseDto> SubmitSurveyAsync(SubmitSurveyDto request, Guid userId);
    Task<PaginatedResponse<SurveyResponseDto>> ListSurveyResponsesAsync(
        Guid? formId, Guid? outletId, DateTime? from, DateTime? to, PaginationParams pagination);

    // ── Merchandising ────────────────────────────────────────────────────────
    Task<MerchandisingAuditDto> SubmitAuditAsync(SubmitAuditDto request, Guid userId);
    Task<MerchandisingAuditDto?> GetAuditAsync(Guid auditId);
    Task<PaginatedResponse<MerchandisingAuditDto>> ListAuditsAsync(
        Guid? outletId, Guid? fieldRepId, AuditKind? kind, DateTime? from, DateTime? to,
        PaginationParams pagination);

    Task<CompetitorObservationDto> RecordCompetitorAsync(CompetitorObservationDto request, Guid userId);
    Task<PaginatedResponse<CompetitorObservationDto>> ListCompetitorObservationsAsync(
        Guid? outletId, string? competitorName, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<PosmPlacementDto> RecordPosmAsync(PosmPlacementDto request, Guid userId);
    Task<List<PosmPlacementDto>> ListPosmAsync(Guid? outletId, Guid? schemeId, bool? activeOnly);
}

/// <summary>
/// Price resolution.
///
/// Kept as its own service because "why this price" is the most-asked question at a distributor's
/// counter, and the answer has to be a traceable chain of rules rather than a number the system
/// asserts.
/// </summary>
public interface IPricingService
{
    Task<PriceResolutionDto> ResolvePriceAsync(
        Guid itemId, string uom, decimal quantity, Guid? outletId, Guid? partnerId, DateTime? asOf);

    Task<List<PriceResolutionDto>> ResolvePricesAsync(
        List<(Guid ItemId, string Uom, decimal Quantity)> lines, Guid? outletId, Guid? partnerId, DateTime? asOf);

    Task<PaginatedResponse<PriceListDto>> ListPriceListsAsync(
        string? search, PriceScope? scope, Guid? partnerId, bool? activeOnly, PaginationParams pagination);

    Task<PriceListDto?> GetPriceListAsync(Guid priceListId);
    Task<PriceListDto> SavePriceListAsync(Guid? priceListId, SavePriceListDto request, Guid userId);
    Task<PriceListDto> ApprovePriceListAsync(Guid priceListId, Guid userId);
    Task DeletePriceListAsync(Guid priceListId, Guid userId);

    Task<PaginatedResponse<MarginLadderDto>> ListMarginLaddersAsync(
        Guid? itemId, Guid? partnerId, PaginationParams pagination);
    Task<MarginLadderDto> SaveMarginLadderAsync(Guid? id, MarginLadderDto request, Guid userId);

    Task<PaginatedResponse<MrpRevisionDto>> ListMrpRevisionsAsync(Guid? itemId, PaginationParams pagination);
    Task<MrpRevisionDto> SaveMrpRevisionAsync(Guid? id, MrpRevisionDto request, Guid userId);
}

/// <summary>
/// The trade scheme engine.
///
/// <see cref="EvaluateAsync"/> is the hot path: it runs on every quantity change at the counter,
/// online and offline, and it must be deterministic — same basket, same date, same result — or
/// the claim six weeks later becomes an argument.
/// </summary>
public interface ISchemeService
{
    Task<PaginatedResponse<TradeSchemeDto>> ListSchemesAsync(
        string? search, TradeSchemeKind? kind, SchemeStatus? status, Guid? territoryId,
        bool? activeOnly, PaginationParams pagination);

    Task<TradeSchemeDto?> GetSchemeAsync(Guid schemeId);
    Task<TradeSchemeDto> SaveSchemeAsync(Guid? schemeId, SaveTradeSchemeDto request, Guid userId);
    Task<TradeSchemeDto> DecideSchemeAsync(Guid schemeId, SchemeDecisionDto request, Guid userId);
    Task<TradeSchemeDto> ChangeStatusAsync(Guid schemeId, SchemeStatus status, string? reason, Guid userId);
    Task DeleteSchemeAsync(Guid schemeId, Guid userId);

    /// <summary>
    /// Works out every benefit a basket has earned, and what it would earn by buying a little more.
    /// Pure computation — nothing is persisted.
    /// </summary>
    Task<List<SchemeApplicationDto>> EvaluateAsync(
        Guid? outletId, Guid? partnerId, List<DistributionOrderLineDto> lines, DateTime asOf);

    Task<List<NextSlabHintDto>> GetNextSlabHintsAsync(
        Guid? outletId, Guid? partnerId, List<DistributionOrderLineDto> lines, DateTime asOf);

    /// <summary>Schemes live for this outlet right now, for the shelf-talker list on the terminal.</summary>
    Task<List<TradeSchemeDto>> GetApplicableSchemesAsync(Guid? outletId, Guid? partnerId, DateTime asOf);

    Task<SchemeSimulationDto> SimulateAsync(Guid? schemeId, SaveTradeSchemeDto? draft, DateTime from, DateTime to);
    Task<SchemePerformanceDto> GetPerformanceAsync(Guid schemeId);
    Task<List<SchemeBudgetEntryDto>> GetBudgetLedgerAsync(Guid schemeId);
    Task<TradeSchemeDto> AdjustBudgetAsync(Guid schemeId, decimal amount, string reason, Guid userId);

    Task<PaginatedResponse<SchemeApplicationDto>> ListApplicationsAsync(
        Guid? schemeId, Guid? outletId, Guid? partnerId, DateTime? from, DateTime? to,
        PaginationParams pagination);

    /// <summary>
    /// Raises the claims a deferred scheme has earned over a period. The system already knows the
    /// answer, so making a distributor type it back would be theatre.
    /// </summary>
    Task<List<ClaimSummaryDto>> GenerateDeferredClaimsAsync(DateTime periodStart, DateTime periodEnd, Guid userId);
}

/// <summary>Order capture, pricing, approval and allocation.</summary>
public interface IOrderService
{
    /// <summary>Prices a basket without saving it. The field terminal's live-total call.</summary>
    Task<OrderQuoteDto> QuoteAsync(QuoteOrderDto request);

    Task<DistributionOrderDto> CreateAsync(CreateOrderDto request, Guid userId);
    Task<DistributionOrderDto?> GetAsync(Guid orderId);
    Task<DistributionOrderDto> UpdateAsync(Guid orderId, UpdateOrderDto request, Guid userId);
    Task<DistributionOrderDto> SubmitAsync(Guid orderId, Guid userId);
    Task<DistributionOrderDto> DecideAsync(OrderDecisionDto request, Guid userId);
    Task<DistributionOrderDto> HoldAsync(Guid orderId, string reason, Guid userId);
    Task<DistributionOrderDto> ReleaseHoldAsync(Guid orderId, Guid userId);
    Task<DistributionOrderDto> CancelAsync(Guid orderId, CancelOrderDto request, Guid userId);

    Task<PaginatedResponse<OrderSummaryDto>> ListAsync(
        string? search, DistributionOrderStatus? status, OrderSource? source, Guid? outletId,
        Guid? partnerId, Guid? routeId, Guid? fieldRepId, Guid? warehouseId,
        DateTime? from, DateTime? to, bool? awaitingApproval, PaginationParams pagination);

    /// <summary>The catalogue the terminal renders, priced and stock-checked for this outlet.</summary>
    Task<List<CatalogueItemDto>> GetCatalogueAsync(
        Guid? outletId, Guid? partnerId, Guid? warehouseId, Guid? vanUnitId, string? search,
        Guid? categoryId, Guid? brandId);

    Task<List<StockAllocationDto>> AllocateAsync(AllocateOrderDto request, Guid userId);
    Task ReleaseAllocationAsync(Guid orderId, Guid userId);

    /// <summary>Consolidates several small orders for one destination into a single delivery.</summary>
    Task<DistributionOrderDto> ConsolidateAsync(List<Guid> orderIds, Guid userId);
}

/// <summary>
/// The van as a moving warehouse: load out, sell, take back, transfer, count, load in.
///
/// Every quantity change writes a movement, and the balance is a projection of those movements,
/// so a settlement variance can always be walked back to the transaction that caused it.
/// </summary>
public interface IVanService
{
    Task<PaginatedResponse<VanUnitDto>> ListVansAsync(string? search, Guid? fieldRepId, PaginationParams pagination);
    Task<VanUnitDto?> GetVanAsync(Guid vanUnitId);
    Task<VanUnitDto> SaveVanAsync(Guid? vanUnitId, SaveVanUnitDto request, Guid userId);
    Task DeleteVanAsync(Guid vanUnitId, Guid userId);

    Task<VanStockSummaryDto> GetStockAsync(Guid vanUnitId, VanCompartment? compartment);
    Task<PaginatedResponse<VanStockMovementDto>> GetMovementsAsync(
        Guid vanUnitId, DateTime? from, DateTime? to, VanMovementKind? kind, PaginationParams pagination);

    Task<VanLoadSheetDto> CreateLoadAsync(CreateVanLoadDto request, Guid userId);
    Task<VanLoadSheetDto?> GetLoadAsync(Guid loadSheetId);
    Task<PaginatedResponse<VanLoadSheetDto>> ListLoadsAsync(
        Guid? vanUnitId, VanLoadStatus? status, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<VanLoadSheetDto> ApproveLoadAsync(Guid loadSheetId, bool isApproved, string? reason, Guid userId);

    /// <summary>Moves stock warehouse → van. Any gap between picked and loaded needs a reason.</summary>
    Task<VanLoadSheetDto> ConfirmLoadAsync(ConfirmVanLoadDto request, Guid userId);

    Task<VanStockSummaryDto> TransferAsync(VanTransferDto request, Guid userId);

    Task<VanCycleCountDto> StartCountAsync(StartVanCountDto request, Guid userId);
    Task<VanCycleCountDto> SubmitCountAsync(SubmitVanCountDto request, Guid userId);
    Task<VanCycleCountDto> ApproveCountAsync(Guid countId, Guid userId);
    Task<VanCycleCountDto?> GetCountAsync(Guid countId);

    /// <summary>
    /// End-of-day unload and reconciliation: expected against counted, every variance reasoned,
    /// saleable returns back to stock and damaged goods to quarantine.
    /// </summary>
    Task<VanCycleCountDto> UnloadAsync(Guid vanUnitId, Guid? fieldDayId, SubmitVanCountDto request, Guid userId);
}

/// <summary>Warehouse outbound: waves, picking, packing, staging and dispatch.</summary>
public interface IFulfilmentService
{
    Task<DispatchBoardDto> GetBoardAsync(Guid? warehouseId, DateTime? date);

    Task<PickWaveDto> CreateWaveAsync(CreatePickWaveDto request, Guid userId);
    Task<PickWaveDto?> GetWaveAsync(Guid waveId);
    Task<PaginatedResponse<PickWaveDto>> ListWavesAsync(
        Guid? warehouseId, bool? openOnly, DateTime? from, DateTime? to, PaginationParams pagination);
    Task<PickWaveDto> CloseWaveAsync(Guid waveId, Guid userId);

    Task<PickTaskDto?> GetTaskAsync(Guid taskId);
    Task<PickTaskDto> AssignTaskAsync(Guid taskId, Guid userId, string? name, Guid actorUserId);
    Task<PickTaskDto> StartTaskAsync(Guid taskId, Guid userId);

    /// <summary>Confirms one pick. A lot other than the FEFO nomination requires a reason.</summary>
    Task<PickTaskDto> ConfirmPickAsync(ConfirmPickDto request, Guid userId);
    Task<PickTaskDto> CompleteTaskAsync(Guid taskId, Guid userId);

    Task<PackageDto> CreatePackageAsync(CreatePackageDto request, Guid userId);
    Task<List<PackageDto>> ListPackagesAsync(Guid? orderId, Guid? tripId, Guid? dispatchId);
    Task<PackageDto> StagePackageAsync(Guid packageId, string stagingLocation, Guid userId);

    /// <summary>Scans a carton onto a vehicle. Refuses a carton belonging to another trip.</summary>
    Task<PackageDto> LoadPackageAsync(Guid packageId, Guid tripId, Guid userId);

    Task<DispatchDto> CreateDispatchAsync(CreateDispatchDto request, Guid userId);
    Task<DispatchDto?> GetDispatchAsync(Guid dispatchId);
    Task<PaginatedResponse<DispatchDto>> ListDispatchesAsync(
        Guid? warehouseId, Guid? tripId, DateTime? from, DateTime? to, PaginationParams pagination);
}

/// <summary>Fleet, trips and proof of delivery.</summary>
public interface ILogisticsService
{
    Task<PaginatedResponse<VehicleDto>> ListVehiclesAsync(
        string? search, VehicleKind? kind, bool? expiringComplianceOnly, PaginationParams pagination);
    Task<VehicleDto?> GetVehicleAsync(Guid vehicleId);
    Task<VehicleDto> SaveVehicleAsync(Guid? vehicleId, SaveVehicleDto request, Guid userId);
    Task DeleteVehicleAsync(Guid vehicleId, Guid userId);
    Task<List<VehicleComplianceDto>> GetExpiringComplianceAsync(int withinDays);

    Task<PaginatedResponse<DriverDto>> ListDriversAsync(string? search, PaginationParams pagination);
    Task<DriverDto?> GetDriverAsync(Guid driverId);
    Task<DriverDto> SaveDriverAsync(Guid? driverId, SaveDriverDto request, Guid userId);
    Task DeleteDriverAsync(Guid driverId, Guid userId);

    Task<TripDto> CreateTripAsync(CreateTripDto request, Guid userId);
    Task<TripDto?> GetTripAsync(Guid tripId);
    Task<PaginatedResponse<TripSummaryDto>> ListTripsAsync(
        Guid? vehicleId, Guid? driverId, Guid? routeId, TripStatus? status,
        DateTime? from, DateTime? to, PaginationParams pagination);

    Task<TripDto> ResequenceTripAsync(ResequenceTripDto request, Guid userId);
    Task<TripDto> StartTripAsync(StartTripDto request, Guid userId);
    Task<TripDto> EndTripAsync(EndTripDto request, Guid userId);
    Task<TripDto> CancelTripAsync(Guid tripId, string reason, Guid userId);

    Task<TripStopDto> ArriveAsync(ArriveAtStopDto request, Guid userId);
    Task<TripStopDto> FailStopAsync(FailStopDto request, Guid userId);

    Task<TripExpenseDto> AddExpenseAsync(SaveTripExpenseDto request, Guid userId);
    Task<TripExpenseDto> DecideExpenseAsync(Guid expenseId, bool isApproved, string? reason, Guid userId);

    /// <summary>Captures the doorstep record and, when lines fall short, the credit note with it.</summary>
    Task<PodDto> CapturePodAsync(CapturePodDto request, Guid userId);
    Task<PodDto?> GetPodAsync(Guid podId);
    Task<PaginatedResponse<PodDto>> ListPodsAsync(
        Guid? tripId, Guid? outletId, bool? exceptionsOnly, DateTime? from, DateTime? to,
        PaginationParams pagination);
    Task<PodDto> ResolvePodExceptionAsync(Guid podId, string note, Guid userId);
}

/// <summary>Returns: authorisation, collection, receipt, inspection and disposition.</summary>
public interface IReturnService
{
    Task<ReturnDto> RequestAsync(RequestReturnDto request, Guid userId);
    Task<ReturnDto?> GetAsync(Guid returnId);
    Task<PaginatedResponse<ReturnSummaryDto>> ListAsync(
        string? search, ReturnKind? kind, Domain.Enums.ReturnStatus? status, Guid? outletId,
        Guid? partnerId, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<ReturnDto> DecideAsync(Guid returnId, DecideReturnDto request, Guid userId);
    Task<ReturnDto> CancelAsync(Guid returnId, string reason, Guid userId);

    Task<ReturnReceiptDto> ReceiveAsync(ReceiveReturnDto request, Guid userId);
    Task<ReturnReceiptDto> DispositionAsync(DispositionReturnDto request, Guid userId);

    /// <summary>Records destruction of expired goods, which regulated categories require on paper.</summary>
    Task<ReturnReceiptDto> RecordDestructionAsync(
        Guid receiptId, string certificateNumber, string? certificateUrl, DateTime destroyedOn, Guid userId);

    /// <summary>Raises the credit note and, where the loss is recoverable, the claim behind it.</summary>
    Task<ReturnDto> CreditAsync(Guid returnId, bool raiseClaim, Guid userId);
}

/// <summary>Credit control, collections and cheques.</summary>
public interface ICreditService
{
    Task<CreditSnapshotDto> GetSnapshotAsync(Guid? outletId, Guid? partnerId);
    Task<CreditCheckResultDto> CheckAsync(Guid? outletId, Guid? partnerId, decimal orderValue, CreditEnforcement? enforcement);
    Task<CreditProfileDto> SetLimitAsync(SetCreditLimitDto request, Guid userId);
    Task<CreditProfileDto> RecalculateAsync(Guid? outletId, Guid? partnerId);
    Task<CreditProfileDto> BlockAsync(Guid? outletId, Guid? partnerId, bool isBlocked, string? reason, Guid userId);

    Task<PaginatedResponse<CreditProfileDto>> ListProfilesAsync(
        string? search, Guid? territoryId, Guid? routeId, Guid? partnerId,
        bool? overdueOnly, bool? blockedOnly, PaginationParams pagination);

    Task<CreditOverrideDto> RequestOverrideAsync(RequestCreditOverrideDto request, Guid userId);
    Task<CreditOverrideDto> DecideOverrideAsync(Guid overrideId, DecideCreditOverrideDto request, Guid userId);
    Task<List<CreditOverrideDto>> ListOverridesAsync(Guid? outletId, Guid? partnerId, bool? pendingOnly);

    Task<CollectionDto> RecordCollectionAsync(RecordCollectionDto request, Guid userId);
    Task<CollectionDto?> GetCollectionAsync(Guid collectionId);
    Task<PaginatedResponse<CollectionSummaryDto>> ListCollectionsAsync(
        Guid? outletId, Guid? partnerId, Guid? fieldRepId, Guid? fieldDayId, PaymentTender? tender,
        DateTime? from, DateTime? to, bool? undepositedOnly, PaginationParams pagination);

    Task<CollectionDto> ReverseCollectionAsync(Guid collectionId, string reason, Guid userId);

    Task<PaginatedResponse<ChequeDto>> ListChequesAsync(
        ChequeStatus? status, Guid? outletId, DateTime? from, DateTime? to, PaginationParams pagination);

    /// <summary>A bounce reverses the allocation, charges the fee and blocks the outlet.</summary>
    Task<ChequeDto> UpdateChequeStatusAsync(Guid chequeId, UpdateChequeStatusDto request, Guid userId);
}

/// <summary>Claims, supplier rebates and chargebacks.</summary>
public interface IClaimService
{
    Task<ClaimDto> SubmitAsync(SubmitClaimDto request, Guid userId);
    Task<ClaimDto?> GetAsync(Guid claimId);
    Task<PaginatedResponse<ClaimSummaryDto>> ListAsync(
        string? search, ClaimKind? kind, ClaimStatus? status, Guid? partnerId, Guid? schemeId,
        DateTime? from, DateTime? to, bool? breachingSlaOnly, PaginationParams pagination);

    Task<ClaimDto> StartReviewAsync(Guid claimId, Guid userId);
    Task<ClaimDto> QueryAsync(Guid claimId, QueryClaimDto request, Guid userId);
    Task<ClaimDto> ResubmitAsync(Guid claimId, SubmitClaimDto request, Guid userId);
    Task<ClaimDto> DecideAsync(Guid claimId, DecideClaimDto request, Guid userId);
    Task<ClaimDto> SettleAsync(Guid claimId, SettleClaimDto request, Guid userId);
    Task<ClaimDto> CancelAsync(Guid claimId, string reason, Guid userId);
    Task<ClaimDocumentDto> AddDocumentAsync(Guid claimId, ClaimDocumentDto request, Guid userId);

    Task<PaginatedResponse<RebateAgreementDto>> ListRebatesAsync(
        string? search, Guid? supplierId, bool? activeOnly, PaginationParams pagination);
    Task<RebateAgreementDto?> GetRebateAsync(Guid agreementId);
    Task<RebateAgreementDto> SaveRebateAsync(Guid? agreementId, RebateAgreementDto request, Guid userId);
    Task<List<RebateAccrualDto>> AccrueRebatesAsync(DateTime periodStart, DateTime periodEnd, Guid userId);
    Task<RebateAccrualDto> ReconcileAccrualAsync(Guid accrualId, decimal receivedAmount, string? reference, Guid userId);

    Task<PaginatedResponse<ChargebackDto>> ListChargebacksAsync(
        ClaimStatus? status, Guid? supplierId, DateTime? from, DateTime? to, PaginationParams pagination);
    Task<ChargebackDto> SaveChargebackAsync(Guid? id, ChargebackDto request, Guid userId);
    Task<ChargebackDto> SettleChargebackAsync(Guid id, decimal settledAmount, string? reference, Guid userId);
}

/// <summary>
/// Route settlement — the moment a distribution day is proved correct.
///
/// The rule the whole service exists to enforce: a route cannot close with an unexplained
/// variance. Everything else here is bookkeeping around that constraint.
/// </summary>
public interface ISettlementService
{
    Task<SettlementBoardDto> GetBoardAsync(DateTime? date, Guid? territoryId);

    /// <summary>Opens a settlement and computes both sides — sales, collections, stock, expenses.</summary>
    Task<SettlementDto> OpenAsync(OpenSettlementDto request, Guid userId);
    Task<SettlementDto?> GetAsync(Guid settlementId);
    Task<SettlementDto> RecomputeAsync(Guid settlementId, Guid userId);
    Task<SettlementDto> SubmitAsync(SubmitSettlementDto request, Guid userId);
    Task<SettlementDto> ExplainVarianceAsync(ExplainVarianceDto request, Guid userId);
    Task<SettlementDto> ApproveAsync(ApproveSettlementDto request, Guid userId);
    Task<SettlementDto> CloseAsync(Guid settlementId, Guid userId);
    Task<SettlementDto> ReverseAsync(Guid settlementId, ReverseSettlementDto request, Guid userId);

    Task<PaginatedResponse<SettlementDto>> ListAsync(
        Guid? fieldRepId, Guid? routeId, SettlementStatus? status, DateTime? from, DateTime? to,
        PaginationParams pagination);

    Task<CashDepositDto> RecordDepositAsync(RecordDepositDto request, Guid userId);
    Task<CashDepositDto> ReconcileDepositAsync(Guid depositId, Guid userId);
    Task<PaginatedResponse<CashDepositDto>> ListDepositsAsync(
        Guid? fieldRepId, bool? unreconciledOnly, DateTime? from, DateTime? to, PaginationParams pagination);
}

/// <summary>
/// The secondary layer — what makes this a DMS rather than a sales module.
///
/// Three capture modes exist because distributors sit at three levels of maturity, and forcing
/// all of them onto one path is how DMS rollouts stall in month two.
/// </summary>
public interface ISecondarySalesService
{
    Task<PaginatedResponse<SecondarySaleDto>> ListAsync(
        Guid? partnerId, Guid? outletId, DateTime? from, DateTime? to, bool? unmappedOnly,
        PaginationParams pagination);

    Task<SecondarySaleDto?> GetAsync(Guid id);
    Task<List<SecondarySaleDto>> DeclareAsync(DeclareSecondarySalesDto request, Guid userId);

    /// <summary>Parses an uploaded file through the partner's mapping profile.</summary>
    Task<SecondaryUploadDto> UploadAsync(
        Guid partnerId, DateTime periodStart, DateTime periodEnd, string fileName,
        Stream content, Guid? mappingProfileId, Guid userId);

    Task<SecondaryUploadDto?> GetUploadAsync(Guid uploadId);
    Task<PaginatedResponse<SecondaryUploadDto>> ListUploadsAsync(
        Guid? partnerId, UploadBatchStatus? status, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<SecondaryUploadDto> PostUploadAsync(Guid uploadId, Guid userId);
    Task<SecondaryUploadDto> RejectUploadAsync(Guid uploadId, string reason, Guid userId);

    Task<List<MappingExceptionDto>> GetMappingExceptionsAsync(Guid? uploadId, Guid? partnerId);

    /// <summary>Resolving an exception teaches the profile, so the same code maps itself next time.</summary>
    Task<MappingExceptionDto?> ResolveMappingAsync(ResolveMappingDto request, Guid userId);

    Task<List<MappingProfileDto>> ListMappingProfilesAsync(Guid? partnerId);
    Task<MappingProfileDto> SaveMappingProfileAsync(Guid? id, MappingProfileDto request, Guid userId);

    Task<StockDeclarationDto> SubmitStockDeclarationAsync(SubmitStockDeclarationDto request, Guid userId);
    Task<StockDeclarationDto?> GetStockDeclarationAsync(Guid id);
    Task<PaginatedResponse<StockDeclarationDto>> ListStockDeclarationsAsync(
        Guid? partnerId, DateTime? from, DateTime? to, PaginationParams pagination);
    Task<StockDeclarationDto> VerifyStockDeclarationAsync(Guid id, Guid userId);

    Task<List<StockNormDto>> ListNormsAsync(Guid? partnerId, bool? exceptionsOnly);
    Task<StockNormDto> SaveNormAsync(Guid? id, StockNormDto request, Guid userId);

    /// <summary>Opening + primary − secondary − returns = closing, per partner per SKU per period.</summary>
    Task<List<ReconciliationDto>> ReconcileAsync(Guid? partnerId, DateTime periodStart, DateTime periodEnd, Guid userId);

    Task<PaginatedResponse<ReconciliationDto>> ListReconciliationsAsync(
        Guid? partnerId, ReconciliationOutcome? outcome, bool? unexplainedOnly,
        DateTime? from, DateTime? to, PaginationParams pagination);

    Task<ReconciliationDto> ExplainAsync(ExplainReconciliationDto request, Guid userId);

    Task<ChannelInventoryDto> GetChannelInventoryAsync(DateTime periodStart, DateTime periodEnd, Guid? territoryId);
    Task<List<PartnerDataQualityDto>> GetDataQualityAsync(DateTime periodStart, DateTime periodEnd);
}

/// <summary>Targets, incentives and the field KPI set.</summary>
public interface IPerformanceService
{
    Task<PaginatedResponse<TargetDto>> ListTargetsAsync(
        TargetScope? scope, TargetMetric? metric, Guid? fieldRepId, Guid? territoryId,
        DateTime? periodStart, PaginationParams pagination);

    Task<TargetDto?> GetTargetAsync(Guid targetId);
    Task<TargetDto> SaveTargetAsync(Guid? targetId, SaveTargetDto request, Guid userId);
    Task<TargetDto> PublishTargetAsync(Guid targetId, Guid userId);
    Task DeleteTargetAsync(Guid targetId, Guid userId);
    Task<List<TargetDto>> RecomputeTargetsAsync(DateTime periodStart, DateTime periodEnd);

    Task<List<IncentiveSchemeDto>> ListIncentiveSchemesAsync(bool? activeOnly);
    Task<IncentiveSchemeDto> SaveIncentiveSchemeAsync(Guid? id, IncentiveSchemeDto request, Guid userId);
    Task<List<IncentivePayoutDto>> ComputePayoutsAsync(Guid schemeId, DateTime periodStart, DateTime periodEnd, Guid userId);
    Task<IncentivePayoutDto> ApprovePayoutAsync(Guid payoutId, bool isApproved, string? note, Guid userId);
    Task<PaginatedResponse<IncentivePayoutDto>> ListPayoutsAsync(
        Guid? schemeId, Guid? fieldRepId, DateTime? periodStart, PaginationParams pagination);

    /// <summary>Recomputes and stores the KPI snapshots for a date. Idempotent per date and scope.</summary>
    Task<int> ComputeKpiSnapshotsAsync(DateTime date);

    Task<List<KpiDto>> GetKpisAsync(
        TargetScope scope, DateTime from, DateTime to, Guid? territoryId, Guid? fieldRepId, Guid? routeId);

    Task<List<RankedRowDto>> GetLeaderboardAsync(TargetScope scope, TargetMetric metric, DateTime from, DateTime to);
}

/// <summary>Demand forecasting, replenishment and stock transfers.</summary>
public interface IPlanningService
{
    Task<ForecastDto> GenerateForecastAsync(GenerateForecastDto request, Guid userId);
    Task<ForecastDto?> GetForecastAsync(Guid forecastId);
    Task<PaginatedResponse<ForecastDto>> ListForecastsAsync(
        Guid? partnerId, Guid? warehouseId, DateTime? periodStart, PaginationParams pagination);
    Task<ForecastDto> OverrideLineAsync(OverrideForecastLineDto request, Guid userId);
    Task<ForecastDto> ApproveForecastAsync(Guid forecastId, Guid userId);

    /// <summary>Rebuilds the replenishment queue from current stock, norms and demand.</summary>
    Task<int> GenerateSuggestionsAsync(ReplenishmentTargetKind targetKind, Guid? scopeId, Guid userId);

    Task<PaginatedResponse<ReplenishmentSuggestionDto>> ListSuggestionsAsync(
        ReplenishmentTargetKind? targetKind, Guid? partnerId, Guid? warehouseId, Guid? vanUnitId,
        bool? openOnly, PaginationParams pagination);

    Task<ReplenishmentSuggestionDto> DismissSuggestionAsync(Guid id, string reason, Guid userId);

    /// <summary>Turns suggestions into a real transfer request or purchase requisition.</summary>
    Task<TransferRequestDto> CreateTransferAsync(List<Guid> suggestionIds, Guid userId);

    Task<PaginatedResponse<TransferRequestDto>> ListTransfersAsync(
        TransferRequestStatus? status, Guid? partnerId, DateTime? from, DateTime? to, PaginationParams pagination);
    Task<TransferRequestDto> DecideTransferAsync(Guid id, bool isApproved, string? reason, Guid userId);
}

/// <summary>Batch traceability, cold chain, expiry and recall.</summary>
public interface ITraceabilityService
{
    Task<List<ColdChainCheckpointDto>> ListCheckpointsAsync(Guid? warehouseId, bool? breachedOnly);
    Task<ColdChainCheckpointDto> SaveCheckpointAsync(Guid? id, ColdChainCheckpointDto request, Guid userId);
    Task<ColdChainLogDto> RecordReadingAsync(RecordColdChainReadingDto request, Guid userId);
    Task<PaginatedResponse<ColdChainLogDto>> ListReadingsAsync(
        Guid? checkpointId, bool? excursionsOnly, bool? unresolvedOnly, DateTime? from, DateTime? to,
        PaginationParams pagination);
    Task<ColdChainLogDto> ResolveExcursionAsync(Guid logId, string correctiveAction, decimal affectedValue, Guid userId);

    /// <summary>Forward and backward trace in one answer — what a recall actually needs.</summary>
    Task<BatchTraceDto?> TraceBatchAsync(Guid? itemId, string batchNumber);
    Task<BatchTraceDto?> TraceFromOutletAsync(Guid outletId, Guid itemId, DateTime? asOf);

    Task<RecallDto> InitiateRecallAsync(InitiateRecallDto request, Guid userId);
    Task<RecallDto?> GetRecallAsync(Guid recallId);
    Task<PaginatedResponse<RecallDto>> ListRecallsAsync(RecallStatus? status, PaginationParams pagination);

    /// <summary>Builds the affected-outlet list from the trace links and announces the recall.</summary>
    Task<RecallDto> AnnounceRecallAsync(Guid recallId, Guid userId);
    Task<RecallNoticeDto> UpdateNoticeAsync(Guid noticeId, decimal returnedQuantity, bool isClosed, string? note, Guid userId);
    Task<RecallDto> CompleteRecallAsync(Guid recallId, string closureReport, Guid userId);

    Task<PaginatedResponse<NearExpiryDto>> GetNearExpiryAsync(
        int? withinDays, Guid? warehouseId, Guid? vanUnitId, Guid? partnerId, PaginationParams pagination);
}

/// <summary>Dashboards, reports and the exception queue.</summary>
public interface IDistributionReportService
{
    Task<DistributionDashboardDto> GetDashboardAsync(Guid? territoryId, DateTime? asOf);
    Task<ExceptionDashboardDto> GetExceptionsAsync(Guid? territoryId);

    Task<SalesReportDto> GetSalesReportAsync(DistributionReportFilter filter, string groupBy);
    Task<ProductivityReportDto> GetProductivityReportAsync(DistributionReportFilter filter);
    Task<OutletAnalyticsDto> GetOutletAnalyticsAsync(DistributionReportFilter filter);
    Task<LogisticsReportDto> GetLogisticsReportAsync(DistributionReportFilter filter);
    Task<ReceivablesReportDto> GetReceivablesReportAsync(DistributionReportFilter filter);
    Task<ReturnsReportDto> GetReturnsReportAsync(DistributionReportFilter filter);
    Task<ClaimsReportDto> GetClaimsReportAsync(DistributionReportFilter filter);
    Task<StockReportDto> GetStockReportAsync(DistributionReportFilter filter);
}

/// <summary>Reason codes, settings and notifications — the module's own administration.</summary>
public interface IDistributionAdminService
{
    Task<List<ReasonCodeDto>> ListReasonCodesAsync(ReasonSurface? surface, bool? activeOnly);
    Task<ReasonCodeDto> SaveReasonCodeAsync(Guid? id, ReasonCodeDto request, Guid userId);
    Task DeleteReasonCodeAsync(Guid id, Guid userId);

    Task<DistributionSettingsDto> GetSettingsAsync();
    Task<DistributionSettingsDto> SaveSettingsAsync(DistributionSettingsDto request, Guid userId);

    Task<PaginatedResponse<NotificationDto>> ListNotificationsAsync(
        Guid? userId, bool? unreadOnly, PaginationParams pagination);
    Task<NotificationDto> MarkReadAsync(Guid notificationId, Guid userId);
    Task MarkAllReadAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
}
