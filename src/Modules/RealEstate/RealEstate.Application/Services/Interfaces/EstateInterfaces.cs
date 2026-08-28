using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Enums;

namespace RealEstate.Application.Services.Interfaces;

/// <summary>The society console, the gate, amenities, the helpdesk and building control.</summary>
public interface ISocietyService
{
    Task<PaginatedResponse<SocietyListItemDto>> GetSocietiesAsync(ListQueryDto query);
    Task<SocietyDetailDto?> GetSocietyAsync(Guid id);
    Task<SocietyDetailDto> SaveSocietyAsync(SocietyUpsertDto dto, Guid userId);
    Task<SocietyDashboardDto> GetDashboardAsync(Guid societyId);
    Task<SocietyDetailDto> HandOverFromDeveloperAsync(Guid societyId, DateOnly handoverDate, decimal corpusTransferred, Guid userId);

    Task<List<CommitteeMemberDto>> GetCommitteeAsync(Guid societyId);
    Task<CommitteeMemberDto> SaveCommitteeMemberAsync(Guid societyId, CommitteeMemberDto dto, Guid userId);

    // Residents
    Task<PaginatedResponse<ResidentListItemDto>> GetResidentsAsync(ListQueryDto query, Guid societyId, ResidentKind? kind, bool? defaultersOnly);
    Task<ResidentDetailDto?> GetResidentAsync(Guid id);
    Task<ResidentDetailDto> SaveResidentAsync(ResidentUpsertDto dto, Guid userId);
    Task<ResidentDetailDto> MoveOutAsync(Guid residentId, DateOnly movedOutOn, Guid userId);
    Task<ResidentVehicleDto> SaveVehicleAsync(Guid residentId, ResidentVehicleDto dto, Guid userId);
    Task<PaginatedResponse<DomesticStaffDto>> GetStaffAsync(ListQueryDto query, Guid societyId);
    Task<DomesticStaffDto> SaveStaffAsync(DomesticStaffDto dto, Guid userId);

    // Billing
    Task<List<MaintenanceChargeSchemeDto>> GetChargeSchemesAsync(Guid societyId);
    Task<MaintenanceChargeSchemeDto> SaveChargeSchemeAsync(MaintenanceChargeSchemeDto dto, Guid userId);
    Task<MaintenanceBillRunResultDto> RunBillingAsync(MaintenanceBillRunDto dto, Guid userId);
    Task<PaginatedResponse<MaintenanceBillDto>> GetBillsAsync(ListQueryDto query, Guid societyId, InstalmentStatus? status);
    Task<MaintenanceBillDto?> GetBillAsync(Guid id);
    Task<List<SocietyChargeDto>> GetSocietyChargesAsync(Guid societyId, Guid? unitId);
    Task<SocietyChargeDto> SaveSocietyChargeAsync(SocietyChargeDto dto, Guid userId);
    Task<SocietyPenaltyDto> ImposePenaltyAsync(SocietyPenaltyDto dto, Guid userId);
    Task<SocietyPenaltyDto> WaivePenaltyAsync(Guid id, string reason, Guid userId);
    Task<PaginatedResponse<SocietyPenaltyDto>> GetPenaltiesAsync(ListQueryDto query, Guid societyId);

    // Gate
    Task<VisitorPassDto> CreateVisitorPassAsync(VisitorPassCreateDto dto, Guid userId);
    Task<PaginatedResponse<VisitorPassDto>> GetVisitorPassesAsync(ListQueryDto query, Guid societyId, Guid? unitId);
    Task<GateEntryDto> RecordEntryAsync(GateEntryCreateDto dto, Guid userId);
    Task<GateEntryDto> ApproveEntryAsync(GateApprovalDto dto, Guid userId);
    Task<GateEntryDto> CheckOutAsync(Guid gateEntryId, Guid userId);
    Task<PaginatedResponse<GateEntryDto>> GetGateLogAsync(ListQueryDto query, Guid societyId, GateEntryStatus? status);
    Task<List<GateEntryDto>> GetInsideNowAsync(Guid societyId);
    Task<int> SyncGateBatchAsync(GateSyncBatchDto batch, Guid userId);
    Task<GatePassDto> CreateGatePassAsync(GatePassDto dto, Guid userId);
    Task<MoveRequestDto> CreateMoveRequestAsync(MoveRequestDto dto, Guid userId);
    Task<MoveRequestDto> DecideMoveRequestAsync(Guid id, string status, Guid userId);

    // Amenities
    Task<List<AmenityDto>> GetAmenitiesAsync(Guid societyId);
    Task<AmenityDto> SaveAmenityAsync(AmenityDto dto, Guid userId);
    Task<AmenityAvailabilityDto> GetAvailabilityAsync(Guid amenityId, DateOnly date);
    Task<AmenityBookingDto> BookAmenityAsync(AmenityBookingCreateDto dto, Guid userId);
    Task<AmenityBookingDto> DecideAmenityBookingAsync(Guid id, bool approved, string? reason, Guid userId);
    Task<AmenityBookingDto> CancelAmenityBookingAsync(Guid id, string? reason, Guid userId);
    Task<PaginatedResponse<AmenityBookingDto>> GetAmenityBookingsAsync(ListQueryDto query, Guid societyId, AmenityBookingStatus? status);

    // Helpdesk
    Task<ComplaintDetailDto> CreateComplaintAsync(ComplaintCreateDto dto, Guid userId);
    Task<ComplaintDetailDto?> GetComplaintAsync(Guid id);
    Task<PaginatedResponse<ComplaintListItemDto>> GetComplaintsAsync(ComplaintSearchDto query);
    Task<ComplaintDetailDto> UpdateComplaintAsync(Guid id, TicketStatus? status, string note, List<string>? photoUrls, bool visibleToResident, Guid userId);
    Task<ComplaintDetailDto> AssignComplaintAsync(Guid id, Guid? userId2, Guid? contractorId, Guid userId);
    Task<ComplaintDetailDto> RateComplaintAsync(Guid id, int rating, string? note, Guid userId);
    Task<int> EscalateBreachedComplaintsAsync();

    // Notices & polls
    Task<PaginatedResponse<SocietyNoticeDto>> GetNoticesAsync(ListQueryDto query, Guid societyId);
    Task<SocietyNoticeDto> SaveNoticeAsync(SocietyNoticeDto dto, Guid userId);
    Task<List<SocietyPollDto>> GetPollsAsync(Guid societyId, bool openOnly);
    Task<SocietyPollDto> SavePollAsync(SocietyPollDto dto, Guid userId);
    Task<SocietyPollDto> VoteAsync(Guid pollId, Guid? unitId, string choice, string? comment, Guid userId);

    // Building control
    Task<PaginatedResponse<BuildingPlanApplicationDto>> GetBuildingApplicationsAsync(ListQueryDto query, Guid societyId, BuildingApplicationStatus? status);
    Task<BuildingPlanApplicationDto?> GetBuildingApplicationAsync(Guid id);
    Task<BuildingPlanApplicationDto> SaveBuildingApplicationAsync(BuildingPlanApplicationDto dto, Guid userId);
    Task<BuildingPlanApplicationDto> DecideBuildingApplicationAsync(Guid id, BuildingApplicationStatus status, string? conditions, string? rejectionReason, Guid userId);
    Task<BuildingInspectionDto> RecordBuildingInspectionAsync(BuildingInspectionDto dto, Guid userId);
    Task<ViolationNoticeDto> IssueViolationAsync(ViolationNoticeDto dto, Guid userId);
    Task<ViolationNoticeDto> CloseViolationAsync(Guid id, DateOnly compliedOn, Guid userId);
    Task<PaginatedResponse<ViolationNoticeDto>> GetViolationsAsync(ListQueryDto query, Guid societyId, bool openOnly);
}

public class ComplaintSearchDto : ListQueryDto
{
    public Guid? SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public List<ComplaintCategory> Categories { get; set; } = [];
    public List<TicketStatus> Statuses { get; set; } = [];
    public TicketPriority? Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? ContractorId { get; set; }
    public bool? BreachingSlaOnly { get; set; }
}

/// <summary>Work orders, contractors, planned maintenance, assets, meters and utility billing.</summary>
public interface IFacilityService
{
    Task<PaginatedResponse<WorkOrderListItemDto>> GetWorkOrdersAsync(WorkOrderSearchDto query);
    Task<WorkOrderDetailDto?> GetWorkOrderAsync(Guid id);
    Task<WorkOrderDetailDto> CreateWorkOrderAsync(WorkOrderCreateDto dto, Guid userId);
    Task<WorkOrderDetailDto> AssignWorkOrderAsync(Guid id, Guid? userId2, Guid? contractorId, DateTime? from, DateTime? to, Guid userId);
    Task<WorkOrderDetailDto> AuthoriseWorkOrderAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId);
    Task<WorkOrderDetailDto> CompleteWorkOrderAsync(WorkOrderCompletionDto dto, Guid userId);
    Task<WorkOrderDetailDto> CancelWorkOrderAsync(Guid id, Guid reasonCodeId, Guid userId);
    Task<int> FlagSlaBreachesAsync();

    Task<PaginatedResponse<ContractorListItemDto>> GetContractorsAsync(ListQueryDto query, string? trade, bool? approvedOnly);
    Task<ContractorDetailDto?> GetContractorAsync(Guid id);
    Task<ContractorDetailDto> SaveContractorAsync(ContractorDetailDto dto, Guid userId);
    Task<ContractorComplianceDto> SaveContractorComplianceAsync(Guid contractorId, ContractorComplianceDto dto, Guid userId);

    Task<PaginatedResponse<PpmScheduleDto>> GetPpmSchedulesAsync(ListQueryDto query, Guid? propertyId, bool? overdueOnly);
    Task<PpmScheduleDto> SavePpmScheduleAsync(PpmScheduleDto dto, Guid userId);
    Task<PaginatedResponse<PpmTaskDto>> GetPpmTasksAsync(ListQueryDto query, string? status);
    Task<int> GeneratePpmTasksAsync(DateOnly asOf);
    Task<PpmTaskDto> CompletePpmTaskAsync(Guid id, DateOnly completedOn, decimal? cost, string? note, string? certificateUrl, Guid userId);

    Task<PaginatedResponse<FacilityAssetDto>> GetAssetsAsync(ListQueryDto query, Guid? propertyId, AssetKind? kind);
    Task<FacilityAssetDto?> GetAssetAsync(Guid id);
    Task<FacilityAssetDto> SaveAssetAsync(FacilityAssetDto dto, Guid userId);
    Task<AssetServiceRecordDto> RecordServiceAsync(Guid assetId, AssetServiceRecordDto dto, Guid userId);
    Task<List<ServiceContractDto>> GetServiceContractsAsync(Guid? propertyId, bool expiringOnly);
    Task<ServiceContractDto> SaveServiceContractAsync(ServiceContractDto dto, Guid userId);

    Task<InspectionRoundDto> SaveInspectionRoundAsync(InspectionRoundDto dto, Guid userId);
    Task<PaginatedResponse<InspectionRoundDto>> GetInspectionRoundsAsync(ListQueryDto query, Guid? propertyId);

    Task<PaginatedResponse<MeterDto>> GetMetersAsync(ListQueryDto query, Guid? propertyId, Guid? societyId, MeterKind? kind);
    Task<MeterDto> SaveMeterAsync(MeterDto dto, Guid userId);
    Task<List<MeterDto>> GetReadingRoundAsync(Guid? societyId, Guid? propertyId, MeterKind? kind);
    Task<MeterReadingBatchResultDto> SubmitReadingsAsync(MeterReadingBatchDto batch, Guid userId);
    Task<PaginatedResponse<MeterReadingDto>> GetReadingsAsync(ListQueryDto query, Guid? meterId, bool? implausibleOnly);
    Task<MeterReadingDto> VerifyReadingAsync(Guid id, decimal? correctedValue, Guid userId);

    Task<List<UtilityTariffDto>> GetTariffsAsync(Guid? societyId, MeterKind? kind);
    Task<UtilityTariffDto> SaveTariffAsync(UtilityTariffDto dto, Guid userId);
    Task<List<UtilityBillDto>> GenerateUtilityBillsAsync(Guid? societyId, DateOnly periodFrom, DateOnly periodTo, bool dryRun, Guid userId);
    Task<PaginatedResponse<UtilityBillDto>> GetUtilityBillsAsync(ListQueryDto query, Guid? unitId);
    Task<UtilityReconciliationDto> ReconcileUtilityAsync(Guid? societyId, Guid? propertyId, MeterKind kind, DateOnly from, DateOnly to);

    Task<FuelLogDto> SaveFuelLogAsync(FuelLogDto dto, Guid userId);
    Task<PaginatedResponse<FuelLogDto>> GetFuelLogsAsync(ListQueryDto query, Guid? societyId);

    Task<PaginatedResponse<ParkingSlotDto>> GetParkingAsync(ListQueryDto query, Guid? societyId, bool? unallottedOnly);
    Task<ParkingSlotDto> SaveParkingSlotAsync(ParkingSlotDto dto, Guid userId);
    Task<ParkingAllotmentDto> AllotParkingAsync(ParkingAllotmentDto dto, Guid userId);
}

public class WorkOrderSearchDto : ListQueryDto
{
    public List<WorkOrderStatus> Statuses { get; set; } = [];
    public List<WorkOrderSource> Sources { get; set; } = [];
    public TicketPriority? Priority { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? ContractorId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Trade { get; set; }
    public bool? BreachingSlaOnly { get; set; }
    public bool? AwaitingAuthorisationOnly { get; set; }
}

/// <summary>Construction, contracting and the turnkey client build.</summary>
public interface IConstructionService
{
    Task<PaginatedResponse<ConstructionProjectListItemDto>> GetProjectsAsync(ListQueryDto query, ProjectStatus? status);
    Task<ConstructionProjectDetailDto?> GetProjectAsync(Guid id);
    Task<ConstructionProjectDetailDto> SaveProjectAsync(ConstructionProjectDetailDto dto, Guid userId);

    Task<List<WbsNodeDto>> GetWbsAsync(Guid constructionProjectId);
    Task<WbsNodeDto> SaveWbsNodeAsync(WbsNodeUpsertDto dto, Guid userId);
    Task DeleteWbsNodeAsync(Guid id, Guid userId);

    Task<List<BillOfQuantitiesDto>> GetBoqsAsync(Guid constructionProjectId);
    Task<BillOfQuantitiesDto?> GetBoqAsync(Guid id);
    Task<BillOfQuantitiesDto> SaveBoqAsync(BillOfQuantitiesDto dto, Guid userId);
    Task<BoqLineDto> SaveBoqLineAsync(BoqLineUpsertDto dto, Guid userId);
    Task<BillOfQuantitiesDto> ImportBoqLinesAsync(Guid boqId, List<BoqLineUpsertDto> lines, Guid userId);

    Task<List<RateAnalysisDto>> GetRateAnalysesAsync(Guid? constructionProjectId, bool libraryOnly);
    Task<RateAnalysisDto> SaveRateAnalysisAsync(RateAnalysisDto dto, Guid userId);
    Task<PaginatedResponse<EstimateDto>> GetEstimatesAsync(ListQueryDto query);
    Task<EstimateDto> SaveEstimateAsync(EstimateDto dto, Guid userId);
    Task<BillOfQuantitiesDto> ConvertEstimateToBoqAsync(Guid estimateId, Guid constructionProjectId, Guid userId);

    Task<List<SpecificationScheduleDto>> GetSpecificationsAsync(Guid? constructionProjectId, Guid? clientBuildContractId);
    Task<SpecificationScheduleDto> SaveSpecificationAsync(SpecificationScheduleDto dto, Guid userId);
    Task<SpecificationScheduleDto> FreezeSpecificationAsync(Guid id, Guid userId);

    Task<List<ProgrammeActivityDto>> GetProgrammeAsync(Guid constructionProjectId);
    Task<ProgrammeActivityDto> SaveActivityAsync(Guid constructionProjectId, ProgrammeActivityDto dto, Guid userId);
    Task<List<ProgrammeActivityDto>> RecalculateCriticalPathAsync(Guid constructionProjectId, Guid userId);
    Task<List<ProgrammeActivityDto>> BaselineAsync(Guid constructionProjectId, Guid userId);

    Task<ProgressMeasurementDto> SaveProgressAsync(ProgressMeasurementDto dto, Guid userId);
    Task<ProgressMeasurementDto> SyncProgressAsync(ProgressSyncBatchDto batch, Guid userId);
    Task<ProgressMeasurementDto> CertifyProgressAsync(Guid id, Guid userId);
    Task<PaginatedResponse<ProgressMeasurementDto>> GetProgressAsync(ListQueryDto query, Guid? constructionProjectId, Guid? subcontractId);

    Task<InterimPaymentCertificateDetailDto> PrepareIpcAsync(IpcCreateDto dto, Guid userId);
    Task<InterimPaymentCertificateDetailDto> CertifyIpcAsync(Guid id, Guid userId);
    Task<InterimPaymentCertificateDetailDto> ApproveIpcAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId);
    Task<PaginatedResponse<InterimPaymentCertificateListItemDto>> GetIpcsAsync(ListQueryDto query, Guid? constructionProjectId, string? direction, CertificateStatus? status);
    Task<InterimPaymentCertificateDetailDto?> GetIpcAsync(Guid id);

    Task<PaginatedResponse<RetentionLedgerEntryDto>> GetRetentionAsync(ListQueryDto query, Guid? subcontractId, bool dueForReleaseOnly);
    Task<RetentionLedgerEntryDto> ReleaseRetentionAsync(Guid? subcontractId, Guid? clientBuildContractId, decimal amount, RetentionMovement movement, Guid userId);
    Task<AdvancePaymentDto> CreateAdvanceAsync(AdvancePaymentDto dto, Guid userId);
    Task<MaterialsOnSiteDto> SaveMaterialsOnSiteAsync(MaterialsOnSiteDto dto, Guid userId);
    Task<CostToCompleteDto> RecalculateCostToCompleteAsync(Guid constructionProjectId, DateOnly asOf, Guid userId);

    Task<PaginatedResponse<TenderDto>> GetTendersAsync(ListQueryDto query, TenderStatus? status);
    Task<TenderDto?> GetTenderAsync(Guid id);
    Task<TenderDto> SaveTenderAsync(TenderDto dto, Guid userId);
    Task<TenderDto> SubmitBidAsync(Guid tenderId, TenderBidDto bid, Guid userId);
    Task<TenderDto> AwardTenderAsync(Guid tenderId, Guid bidId, string justification, Guid userId);
    Task<List<BidComparisonLineDto>> GetBidComparisonAsync(Guid tenderId);

    Task<PaginatedResponse<SubcontractListItemDto>> GetSubcontractsAsync(ListQueryDto query, Guid? constructionProjectId, SubcontractStatus? status);
    Task<SubcontractDetailDto?> GetSubcontractAsync(Guid id);
    Task<SubcontractDetailDto> SaveSubcontractAsync(SubcontractCreateDto dto, Guid userId);
    Task<SubcontractDetailDto> ChangeSubcontractStatusAsync(Guid id, SubcontractStatus status, string? reason, Guid userId);
    Task<SubcontractorClaimDto> SubmitClaimAsync(SubcontractorClaimDto dto, Guid userId);
    Task<SubcontractorClaimDto> CertifyClaimAsync(Guid id, List<ClaimCertificationDto> certifications, Guid userId);
    Task<PaginatedResponse<SubcontractorClaimDto>> GetClaimsAsync(ListQueryDto query, Guid? subcontractId, CertificateStatus? status);
    Task<ContraChargeDto> SaveContraChargeAsync(ContraChargeDto dto, Guid userId);

    Task<PaginatedResponse<VariationOrderListItemDto>> GetVariationsAsync(ListQueryDto query, Guid? constructionProjectId, VariationStatus? status);
    Task<VariationOrderDetailDto?> GetVariationAsync(Guid id);
    Task<VariationOrderDetailDto> SaveVariationAsync(VariationOrderUpsertDto dto, Guid userId);
    Task<VariationOrderDetailDto> DecideVariationAsync(Guid id, VariationStatus status, string? reason, Guid userId);

    Task<SiteInstructionDto> IssueSiteInstructionAsync(SiteInstructionDto dto, Guid userId);
    Task<PaginatedResponse<SiteInstructionDto>> GetSiteInstructionsAsync(ListQueryDto query, Guid? constructionProjectId);
    Task<DelayEventDto> SaveDelayAsync(DelayEventDto dto, Guid userId);
    Task<PaginatedResponse<DelayEventDto>> GetDelaysAsync(ListQueryDto query, Guid? constructionProjectId);
    Task<ExtensionOfTimeDto> SaveExtensionOfTimeAsync(ExtensionOfTimeDto dto, Guid userId);
    Task<ExtensionOfTimeDto> DecideExtensionAsync(Guid id, int daysGranted, bool prolongationGranted, string? note, Guid userId);

    Task<MaterialRequisitionDto> SaveRequisitionAsync(MaterialRequisitionDto dto, Guid userId);
    Task<MaterialRequisitionDto> ApproveRequisitionAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId);
    Task<PaginatedResponse<MaterialRequisitionDto>> GetRequisitionsAsync(ListQueryDto query, Guid? constructionProjectId, string? status);
    Task<MaterialIssueDto> IssueMaterialAsync(MaterialIssueDto dto, Guid userId);
    Task<PaginatedResponse<MaterialIssueDto>> GetIssuesAsync(ListQueryDto query, Guid? constructionProjectId);
    Task<List<WastageRecordDto>> CalculateWastageAsync(Guid constructionProjectId, DateOnly from, DateOnly to, Guid userId);

    Task<LabourRecordDto> SaveLabourAsync(LabourRecordDto dto, Guid userId);
    Task<PaginatedResponse<LabourRecordDto>> GetLabourAsync(ListQueryDto query, Guid? constructionProjectId);
    Task<List<PlantItemDto>> GetPlantAsync(string? status);
    Task<PlantItemDto> SavePlantAsync(PlantItemDto dto, Guid userId);
    Task<PlantAllocationDto> AllocatePlantAsync(PlantAllocationDto dto, Guid userId);
    Task<SiteGateEntryDto> RecordSiteGateEntryAsync(SiteGateEntryDto dto, Guid userId);
    Task<SafetyIncidentDto> SaveSafetyIncidentAsync(SafetyIncidentDto dto, Guid userId);
    Task<PaginatedResponse<SafetyIncidentDto>> GetSafetyIncidentsAsync(ListQueryDto query, Guid? constructionProjectId);

    // Client build
    Task<PaginatedResponse<ClientBuildContractListItemDto>> GetClientBuildsAsync(ListQueryDto query, string? status);
    Task<ClientBuildContractDetailDto?> GetClientBuildAsync(Guid id);
    Task<ClientBuildContractDetailDto> SaveClientBuildAsync(ClientBuildContractUpsertDto dto, Guid userId);
    Task<ClientBuildContractDetailDto> ChangeClientBuildStatusAsync(Guid id, string status, Guid userId);
    Task<ClientVariationDto> SaveClientVariationAsync(ClientVariationUpsertDto dto, Guid userId);
    Task<ClientVariationDto> DecideClientVariationAsync(Guid id, bool approved, string? reason, string? evidenceUrl, Guid userId);
    Task<PaginatedResponse<ClientVariationDto>> GetClientVariationsAsync(ListQueryDto query, Guid? contractId, VariationStatus? status);
    Task<ContractCostSheetDto> GetContractCostSheetAsync(Guid contractId, DateOnly? asOf);
    Task<ContractCostSheetDto> RecalculateCostSheetAsync(Guid contractId, Guid userId);
    Task<List<DrawingRegisterDto>> GetDrawingsAsync(Guid? clientBuildContractId, Guid? constructionProjectId);
    Task<DrawingRegisterDto> SaveDrawingAsync(DrawingRegisterDto dto, Guid userId);
    Task<DrawingRegisterDto> AddRevisionAsync(Guid drawingId, DrawingRevisionDto dto, Guid userId);
    Task<CustomerPortalHomeDto> GetClientPortalHomeAsync(Guid partyId);
}

/// <summary>Joint ventures, investors, escrow, project finance, revenue recognition and compliance.</summary>
public interface IFinanceService
{
    Task<List<JointVentureDto>> GetVenturesAsync(Guid? projectId);
    Task<JointVentureDto?> GetVentureAsync(Guid id);
    Task<JointVentureDto> SaveVentureAsync(JointVentureDto dto, Guid userId);
    Task<LandownerAllocationDto> AllocateUnitAsync(Guid ventureId, Guid unitId, Guid? jvPartnerId, Guid userId);
    Task<LandownerAllocationDto> ReleaseAllocationAsync(Guid allocationId, Guid userId);
    Task<PaginatedResponse<LandownerLedgerEntryDto>> GetLandownerLedgerAsync(Guid ventureId, ListQueryDto query);
    Task<int> AccrueLandownerShareAsync(Guid ventureId, Guid userId);

    Task<PaginatedResponse<InvestorDto>> GetInvestorsAsync(ListQueryDto query, Guid? projectId);
    Task<InvestorDto?> GetInvestorAsync(Guid id);
    Task<InvestorDto> SaveInvestorAsync(InvestorDto dto, Guid userId);
    Task<CapitalCallDto> IssueCapitalCallAsync(CapitalCallDto dto, Guid userId);
    Task<ContributionDto> RecordContributionAsync(ContributionDto dto, Guid userId);
    Task<DistributionDto> RecordDistributionAsync(DistributionDto dto, Guid userId);

    Task<List<ProjectBankAccountDto>> GetProjectAccountsAsync(Guid projectId);
    Task<ProjectBankAccountDto> SaveProjectAccountAsync(ProjectBankAccountDto dto, Guid userId);
    Task<PaginatedResponse<EscrowLedgerEntryDto>> GetEscrowLedgerAsync(Guid accountId, ListQueryDto query);
    Task<EscrowWithdrawalDto> RequestWithdrawalAsync(EscrowWithdrawalRequestDto dto, Guid userId);
    Task<EscrowWithdrawalDto> DecideWithdrawalAsync(Guid id, ApprovalOutcome outcome, decimal? approvedAmount, string? comment, Guid userId);
    Task<PaginatedResponse<EscrowWithdrawalDto>> GetWithdrawalsAsync(ListQueryDto query, Guid? projectId);
    Task<ProjectBankAccountDto> RecomputeEntitlementAsync(Guid accountId, Guid userId);

    Task<PaginatedResponse<ProjectLoanDto>> GetLoansAsync(ListQueryDto query, Guid? projectId);
    Task<ProjectLoanDto> SaveLoanAsync(ProjectLoanDto dto, Guid userId);
    Task<LoanDrawdownDto> RecordDrawdownAsync(Guid loanId, LoanDrawdownDto dto, Guid userId);
    Task<List<BankGuaranteeDto>> GetGuaranteesAsync(Guid? projectId, bool expiringOnly);
    Task<BankGuaranteeDto> SaveGuaranteeAsync(BankGuaranteeDto dto, Guid userId);

    Task<PaginatedResponse<CustomerMortgageDto>> GetMortgagesAsync(ListQueryDto query, string? status);
    Task<CustomerMortgageDto> SaveMortgageAsync(CustomerMortgageDto dto, Guid userId);
    Task<MortgageDisbursementDto> RecordDisbursementAsync(Guid mortgageId, MortgageDisbursementDto dto, Guid userId);

    Task<RecognitionPolicyDto> SaveRecognitionPolicyAsync(RecognitionPolicyDto dto, Guid userId);
    Task<RevenueRecognitionRunDto> RunRecognitionAsync(Guid? projectId, DateOnly periodFrom, DateOnly periodTo, bool dryRun, Guid userId);
    Task<PaginatedResponse<RevenueRecognitionRunDto>> GetRecognitionRunsAsync(ListQueryDto query);
    Task<PaginatedResponse<WipEntryDto>> GetWipAsync(Guid projectId, ListQueryDto query);
    Task<List<UnitProfitabilityDto>> GetUnitProfitabilityAsync(Guid projectId, ListQueryDto query);
    Task<int> AllocateCostsAsync(Guid projectId, DateOnly asOf, Guid userId);
    Task<ProjectPnlDto> GetProjectPnlAsync(Guid projectId, DateOnly asOf);

    Task<List<TaxProfileDto>> GetTaxProfilesAsync(Guid? projectId);
    Task<TaxProfileDto> SaveTaxProfileAsync(TaxProfileDto dto, Guid userId);
    Task<PaginatedResponse<WithholdingRecordDto>> GetWithholdingAsync(ListQueryDto query, WithholdingKind? kind, bool? undepositedOnly);

    // Compliance & documents
    Task<PaginatedResponse<ApprovalRecordDto>> GetApprovalRecordsAsync(ListQueryDto query, Guid? projectId, ApprovalState? state);
    Task<ApprovalRecordDto> SaveApprovalRecordAsync(ApprovalRecordDto dto, Guid userId);
    Task<List<LicenceRecordDto>> GetLicencesAsync(bool expiringOnly);
    Task<LicenceRecordDto> SaveLicenceAsync(LicenceRecordDto dto, Guid userId);
    Task<List<ComplianceCalendarEntryDto>> GetComplianceCalendarAsync(DateOnly from, DateOnly to, string? category);
    Task<ComplianceCalendarEntryDto> CompleteCalendarEntryAsync(Guid id, string? evidenceUrl, Guid userId);
    Task<PaginatedResponse<RegulatoryFilingDto>> GetFilingsAsync(ListQueryDto query, string? status);
    Task<QuarterlyProgressReportDto> GenerateQprAsync(Guid projectId, int year, int quarter, Guid userId);
    Task<QuarterlyProgressReportDto> FileQprAsync(Guid id, string acknowledgementNumber, Guid userId);
    Task<QuarterlyProgressReportDto?> GetQprAsync(Guid id);

    Task<List<DocumentTemplateDto>> GetTemplatesAsync(string? documentType, Guid? projectId);
    Task<DocumentTemplateDto> SaveTemplateAsync(DocumentTemplateDto dto, Guid userId);
    Task<TemplateVersionDto> PublishTemplateVersionAsync(Guid templateId, TemplateVersionDto dto, Guid userId);
    Task<List<ClauseLibraryItemDto>> GetClausesAsync(string? category, Guid? projectId);
    Task<ClauseLibraryItemDto> SaveClauseAsync(ClauseLibraryItemDto dto, Guid userId);
    Task<GeneratedDocumentDto> GenerateDocumentAsync(DocumentGenerationRequestDto dto, Guid userId);
    Task<PaginatedResponse<GeneratedDocumentDto>> GetDocumentsAsync(ListQueryDto query, string? documentType, Guid? entityId);
    Task<GeneratedDocumentDto?> VerifyDocumentAsync(string verificationCode);

    Task<SignatureSessionDto> StartSigningAsync(Guid documentId, List<SignaturePartyDto> parties, SignatureMethod method, bool sequential, Guid userId);
    Task<SignatureSessionDto> RecordSignatureAsync(Guid sessionId, Guid partyId, string? signatureUrl, string? thumbUrl, string? photoUrl, Guid userId);
    Task<SignatureSessionDto?> GetSigningAsync(Guid id);

    Task<PaginatedResponse<PhysicalFileDto>> GetPhysicalFilesAsync(ListQueryDto query, PhysicalFileState? state);
    Task<PhysicalFileDto> SavePhysicalFileAsync(PhysicalFileDto dto, Guid userId);
    Task<PhysicalFileDto> MovePhysicalFileAsync(Guid id, string movement, Guid? toUserId, string? toName, string? purpose, DateOnly? dueBack, Guid userId);

    Task<PaginatedResponse<LegalCaseDto>> GetLegalCasesAsync(ListQueryDto query, LegalCaseStatus? status);
    Task<LegalCaseDto?> GetLegalCaseAsync(Guid id);
    Task<LegalCaseDto> SaveLegalCaseAsync(LegalCaseDto dto, Guid userId);
    Task<LegalHearingDto> RecordHearingAsync(Guid caseId, LegalHearingDto dto, Guid userId);
}

/// <summary>The dashboard and the report suite.</summary>
public interface IRealEstateReportService
{
    Task<RealEstateDashboardDto> GetDashboardAsync(Guid? officeId, Guid? projectId);
    Task<List<AttentionItemDto>> GetAttentionAsync(Guid? officeId, Guid? projectId);
    Task<List<ProjectListItemDto>> GetPortfolioAsync(ListQueryDto query);
    Task<List<ReportDefinitionDto>> GetReportCatalogueAsync();
    Task<ReportResultDto> RunReportAsync(ReportRequestDto request);
}

/// <summary>Conversations, templated messaging, broadcasts, notifications and portal users.</summary>
public interface ICommunicationService
{
    Task<PaginatedResponse<ConversationDto>> GetConversationsAsync(ListQueryDto query, NotificationChannel? channel, string? status, Guid? assignedToUserId);
    Task<ConversationDto?> GetConversationAsync(Guid id);
    Task<ConversationMessageDto> SendAsync(SendMessageDto dto, Guid userId);
    Task<ConversationDto> AssignConversationAsync(Guid id, Guid userId2, Guid userId);
    Task<ConversationDto> CloseConversationAsync(Guid id, Guid userId);

    Task<List<MessageTemplateDto>> GetMessageTemplatesAsync(NotificationChannel? channel, string? category);
    Task<MessageTemplateDto> SaveMessageTemplateAsync(MessageTemplateDto dto, Guid userId);

    Task<BroadcastRunDto> RunBroadcastAsync(BroadcastRequestDto dto, Guid userId);
    Task<PaginatedResponse<BroadcastRunDto>> GetBroadcastsAsync(ListQueryDto query);

    Task<List<NotificationRuleDto>> GetNotificationRulesAsync();
    Task<NotificationRuleDto> SaveNotificationRuleAsync(NotificationRuleDto dto, Guid userId);
    Task<PaginatedResponse<NotificationDto>> GetNotificationsAsync(ListQueryDto query, Guid userId, bool unreadOnly);
    Task MarkNotificationsReadAsync(List<Guid> ids, Guid userId);
    Task<int> RunAlertSweepAsync();

    Task<PaginatedResponse<PortalUserDto>> GetPortalUsersAsync(ListQueryDto query, PortalAudience? audience);
    Task<PortalUserDto> SavePortalUserAsync(PortalUserDto dto, Guid userId);
    Task<PortalUserDto> InvitePortalUserAsync(Guid id, Guid userId);
    Task<CustomerPortalHomeDto> GetCustomerPortalHomeAsync(Guid partyId);
}
