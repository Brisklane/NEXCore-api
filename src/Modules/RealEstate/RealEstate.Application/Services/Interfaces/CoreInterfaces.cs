using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Enums;

namespace RealEstate.Application.Services.Interfaces;

// =====================================================================================
// Service contracts, grouped by the screen that uses them rather than by table.
//
// Every method that can legitimately refuse throws InvalidOperationException with a
// message written for the person holding the phone — the controller base maps that onto
// a 400 and shows it verbatim. A refusal a user cannot act on is a bug.
// =====================================================================================

/// <summary>Settings, offices, geography, territories, agents, approvals and bulk import.</summary>
public interface IRealEstateAdminService
{
    Task<RealEstateSettingsDto> GetSettingsAsync();
    Task<RealEstateSettingsDto> UpdateSettingsAsync(RealEstateSettingsDto dto, Guid userId);
    Task<LinesOfBusinessDto> GetLinesOfBusinessAsync();

    Task<PaginatedResponse<RealEstateOfficeDto>> GetOfficesAsync(ListQueryDto query);
    Task<RealEstateOfficeDto?> GetOfficeAsync(Guid id);
    Task<RealEstateOfficeDto> SaveOfficeAsync(RealEstateOfficeDto dto, Guid userId);

    Task<List<GeoAreaDto>> GetGeoTreeAsync(Guid? parentId, int depth);
    Task<List<LookupDto>> SearchGeoAreasAsync(string search, int take);
    Task<GeoAreaDto> SaveGeoAreaAsync(GeoAreaDto dto, Guid userId);
    Task DeleteGeoAreaAsync(Guid id, Guid userId);

    Task<List<TerritoryDto>> GetTerritoriesAsync(Guid? officeId);
    Task<TerritoryDto> SaveTerritoryAsync(TerritoryDto dto, Guid userId);

    Task<PaginatedResponse<AgentProfileDto>> GetAgentsAsync(ListQueryDto query);
    Task<AgentProfileDto?> GetAgentAsync(Guid id);
    Task<AgentProfileDto> SaveAgentAsync(AgentProfileDto dto, Guid userId);
    Task<List<LookupDto>> GetAgentLookupAsync(Guid? officeId);

    Task<List<SalesTeamDto>> GetTeamsAsync(Guid? officeId);
    Task<SalesTeamDto> SaveTeamAsync(SalesTeamDto dto, Guid userId);

    Task<List<ReasonCodeDto>> GetReasonCodesAsync(string? context);
    Task<ReasonCodeDto> SaveReasonCodeAsync(ReasonCodeDto dto, Guid userId);

    Task<List<ApprovalMatrixDto>> GetApprovalMatrixAsync(string? documentType);
    Task<ApprovalMatrixDto> SaveApprovalMatrixAsync(ApprovalMatrixDto dto, Guid userId);
    Task<PaginatedResponse<ApprovalRequestDto>> GetApprovalsAsync(ListQueryDto query, bool mineOnly, Guid userId);
    Task<ApprovalRequestDto> DecideApprovalAsync(ApprovalDecisionDto dto, Guid userId);

    Task<List<SavedViewDto>> GetSavedViewsAsync(string screenKey, Guid userId);
    Task<SavedViewDto> SaveViewAsync(SavedViewDto dto, Guid userId);
    Task DeleteSavedViewAsync(Guid id, Guid userId);

    Task<ImportBatchDto> RunImportAsync(ImportRequestDto request, Guid userId);
    Task<PaginatedResponse<ImportBatchDto>> GetImportBatchesAsync(ListQueryDto query);
    Task<ImportBatchDto?> GetImportBatchAsync(Guid id);
    Task<ImportBatchDto> CommitImportAsync(Guid batchId, Guid userId);
    Task<ImportBatchDto> RollbackImportAsync(Guid batchId, Guid userId);
}

/// <summary>The property master, the land bank behind it and the valuation evidence.</summary>
public interface IPropertyService
{
    Task<PaginatedResponse<PropertyListItemDto>> GetPropertiesAsync(PropertySearchDto query);
    Task<PropertyDetailDto?> GetPropertyAsync(Guid id);
    Task<PropertyDetailDto> SavePropertyAsync(PropertyUpsertDto dto, Guid userId);
    Task DeletePropertyAsync(Guid id, Guid userId);
    Task<List<DuplicateCandidateDto>> CheckDuplicatesAsync(PropertyUpsertDto dto);
    Task<PropertyDetailDto> ChangeStatusAsync(Guid id, PropertyStatus status, Guid? reasonCodeId, string? note, Guid userId);

    Task<List<PropertyMediaDto>> SaveMediaAsync(Guid propertyId, List<PropertyMediaDto> media, Guid userId);
    Task DeleteMediaAsync(Guid mediaId, Guid userId);
    Task<List<PropertyDocumentDto>> GetDocumentsAsync(Guid propertyId);
    Task<PropertyDocumentDto> SaveDocumentAsync(Guid propertyId, PropertyDocumentDto dto, Guid userId);

    Task<List<PropertyOwnershipDto>> GetOwnershipAsync(Guid propertyId, bool includeHistory);
    Task<PropertyOwnershipDto> SetOwnerAsync(Guid propertyId, PropertyOwnershipDto dto, Guid userId);

    Task<List<PropertyValuationDto>> GetValuationsAsync(Guid propertyId);
    Task<PropertyValuationDto> SaveValuationAsync(PropertyValuationDto dto, Guid userId);
    Task<PriceOpinionDto> GetPriceOpinionAsync(Guid propertyId, ListingKind kind);
    Task<List<PropertyComparableDto>> FindComparablesAsync(Guid propertyId, int take);

    // ── Land bank ──
    Task<PaginatedResponse<LandParcelListItemDto>> GetParcelsAsync(ListQueryDto query);
    Task<LandParcelDetailDto?> GetParcelAsync(Guid id);
    Task<LandParcelDetailDto> SaveParcelAsync(LandParcelDetailDto dto, Guid userId);
    Task<TitleChainEntryDto> SaveTitleEntryAsync(Guid parcelId, TitleChainEntryDto dto, Guid userId);
    Task<EncumbranceDto> SaveEncumbranceAsync(Guid? parcelId, Guid? propertyId, EncumbranceDto dto, Guid userId);
    Task<TitleVerificationItemDto> SaveVerificationAsync(Guid parcelId, TitleVerificationItemDto dto, Guid userId);
    Task<PaginatedResponse<LandAcquisitionDto>> GetAcquisitionsAsync(ListQueryDto query);
    Task<LandAcquisitionDto> SaveAcquisitionAsync(LandAcquisitionDto dto, Guid userId);
    Task<AcquisitionCostLineDto> SaveAcquisitionCostAsync(Guid acquisitionId, AcquisitionCostLineDto dto, Guid userId);
}

/// <summary>Search shape for the property list and the map.</summary>
public class PropertySearchDto : ListQueryDto
{
    public List<PropertyCategory> Categories { get; set; } = [];
    public List<PropertySubType> SubTypes { get; set; } = [];
    public List<PropertyStatus> Statuses { get; set; } = [];
    public Guid? GeoAreaId { get; set; }
    public Guid? OwnerPartyId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinArea { get; set; }
    public decimal? MaxArea { get; set; }
    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;
    public int? MinBedrooms { get; set; }
    public int? MaxBedrooms { get; set; }
    public OccupancyState? Occupancy { get; set; }
    public bool? HasLitigation { get; set; }

    /// <summary>Draw-your-own-area search. Beats a list of area ids when the patch is irregular.</summary>
    public string? BoundaryGeoJson { get; set; }

    public decimal? NearLatitude { get; set; }
    public decimal? NearLongitude { get; set; }
    public decimal? RadiusKm { get; set; }
}

/// <summary>Projects, their structure, milestones, budget and cash flow.</summary>
public interface IProjectService
{
    Task<PaginatedResponse<ProjectListItemDto>> GetProjectsAsync(ListQueryDto query);
    Task<ProjectDetailDto?> GetProjectAsync(Guid id);
    Task<ProjectDetailDto> SaveProjectAsync(ProjectUpsertDto dto, Guid userId);
    Task<List<LookupDto>> GetProjectLookupAsync();

    Task<List<ProjectNodeDto>> GetStructureAsync(Guid projectId);
    Task<ProjectNodeDto> SaveNodeAsync(ProjectNodeUpsertDto dto, Guid userId);
    Task DeleteNodeAsync(Guid nodeId, Guid userId);

    Task<UnitGenerationResultDto> GenerateUnitsAsync(UnitGenerationDto dto, Guid userId);

    Task<List<ProjectMilestoneDto>> GetMilestonesAsync(Guid projectId);
    Task<ProjectMilestoneDto> SaveMilestoneAsync(ProjectMilestoneDto dto, Guid userId);
    Task<MilestoneCertificateDto> CertifyMilestoneAsync(MilestoneCertificateDto dto, Guid userId);

    Task<List<ProjectBudgetLineDto>> GetBudgetAsync(Guid projectId);
    Task<ProjectBudgetLineDto> SaveBudgetLineAsync(Guid projectId, ProjectBudgetLineDto dto, Guid userId);
    Task<ProjectCashFlowDto> GetCashFlowAsync(Guid projectId, int months);

    Task<List<SitePlanDto>> GetSitePlansAsync(Guid projectId);
    Task<SitePlanDto> SaveSitePlanAsync(Guid projectId, SitePlanDto dto, Guid userId);
    Task<SitePlanDto> SaveShapesAsync(Guid sitePlanId, List<SitePlanShapeDto> shapes, Guid userId);

    Task<PaginatedResponse<PlotFileDto>> GetPlotFilesAsync(Guid projectId, ListQueryDto query);
    Task<List<PlotFileDto>> IssuePlotFilesAsync(Guid projectId, string categoryCode, int count, decimal price, decimal areaSqFt, Guid userId);
}

/// <summary>The inventory board and everything that acts on a unit. The flagship of Development.</summary>
public interface IInventoryService
{
    Task<InventoryBoardDto> GetBoardAsync(InventoryQueryDto query);
    Task<InventoryUnitDto?> GetUnitAsync(Guid unitId);
    Task<InventoryUnitDto> SaveUnitAsync(InventoryUnitDto dto, Guid userId);

    Task<UnitHoldDto> HoldAsync(HoldRequestDto dto, Guid userId);
    Task ReleaseHoldAsync(Guid holdId, string? note, Guid userId);
    Task<List<UnitHoldDto>> GetActiveHoldsAsync(Guid? projectId);
    Task<int> ExpireHoldsAsync();

    Task<InventoryUnitDto> BlockAsync(BlockRequestDto dto, Guid userId);
    Task<InventoryUnitDto> UnblockAsync(Guid unitId, string? note, Guid userId);

    Task<List<PriceListDto>> GetPriceListsAsync(Guid projectId);
    Task<PriceListDto?> GetPriceListAsync(Guid id);
    Task<PriceListDto> SavePriceListAsync(PriceListDto dto, Guid userId);
    Task<PriceListDto> PublishPriceListAsync(Guid id, Guid userId);

    Task<List<PremiumChargeDto>> GetPremiumsAsync(Guid projectId);
    Task<PremiumChargeDto> SavePremiumAsync(PremiumChargeDto dto, Guid userId);

    Task<CostSheetDto> GetCostSheetAsync(Guid unitId, Guid? paymentPlanTemplateId, decimal discountAmount, decimal discountPercent);
    Task<int> RepriceAvailableUnitsAsync(Guid priceListId, Guid userId);
}

/// <summary>Listings, agency instructions, portal syndication and marketing.</summary>
public interface IListingService
{
    Task<PaginatedResponse<ListingListItemDto>> GetListingsAsync(ListingSearchDto query);
    Task<ListingDetailDto?> GetListingAsync(Guid id);
    Task<ListingDetailDto> SaveListingAsync(ListingUpsertDto dto, Guid userId);
    Task<ListingDetailDto> ChangeStatusAsync(Guid id, ListingStatus status, Guid? reasonCodeId, Guid userId);
    Task<ListingDetailDto> ChangePriceAsync(ListingPriceChangeDto dto, Guid userId);
    Task<GateResultDto> CheckPublishReadinessAsync(Guid listingId);

    Task<PaginatedResponse<InstructionDto>> GetInstructionsAsync(ListQueryDto query);
    Task<InstructionDto?> GetInstructionAsync(Guid id);
    Task<InstructionDto> SaveInstructionAsync(InstructionUpsertDto dto, Guid userId);
    Task<InstructionDto> TerminateInstructionAsync(Guid id, Guid reasonCodeId, string? note, Guid userId);

    Task<List<PortalChannelDto>> GetPortalsAsync();
    Task<PortalChannelDto> SavePortalAsync(PortalChannelDto dto, Guid userId);
    Task<List<PortalMappingDto>> GetPortalMappingsAsync(Guid portalId);
    Task<PortalMappingDto> SavePortalMappingAsync(PortalMappingDto dto, Guid userId);
    Task<PortalPublishResultDto> PublishAsync(PortalPublishRequestDto dto, Guid userId);
    Task<PaginatedResponse<PortalPublicationDto>> GetPublicationsAsync(ListQueryDto query, Guid? portalId, PortalPublishState? state);

    Task<PaginatedResponse<CampaignDto>> GetCampaignsAsync(ListQueryDto query);
    Task<CampaignDto> SaveCampaignAsync(CampaignDto dto, Guid userId);
    Task<PaginatedResponse<MarketingEventDto>> GetEventsAsync(ListQueryDto query);
    Task<MarketingEventDto> SaveEventAsync(MarketingEventDto dto, Guid userId);
    Task<PaginatedResponse<ContentAssetDto>> GetContentAsync(ListQueryDto query);
    Task<ContentAssetDto> SaveContentAsync(ContentAssetDto dto, Guid userId);
}

public class ListingSearchDto : ListQueryDto
{
    public List<ListingKind> Kinds { get; set; } = [];
    public List<ListingStatus> Statuses { get; set; } = [];
    public Guid? AgentId { get; set; }
    public Guid? GeoAreaId { get; set; }
    public List<PropertySubType> SubTypes { get; set; } = [];
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinBedrooms { get; set; }
    public bool? StaleOnly { get; set; }
    public Guid? PortalChannelId { get; set; }
    public PortalPublishState? PublishState { get; set; }
}

/// <summary>People, enquiries, matching, the diary and the day's work.</summary>
public interface ICrmService
{
    // Parties
    Task<PaginatedResponse<PartyListItemDto>> GetPartiesAsync(PartySearchDto query);
    Task<PartyDetailDto?> GetPartyAsync(Guid id);
    Task<PartyDetailDto> SavePartyAsync(PartyUpsertDto dto, Guid userId);
    Task<List<LookupDto>> SearchPartiesAsync(string search, PartyRoleKind? role, int take);
    Task<List<PartyListItemDto>> FindDuplicatePartiesAsync(PartyUpsertDto dto);
    Task<PartyDetailDto> MergePartiesAsync(Guid keepId, Guid mergeId, Guid userId);

    Task<KycCaseDto?> GetKycAsync(Guid partyId);
    Task<KycCaseDto> SaveKycAsync(KycCaseDto dto, Guid userId);
    Task<KycCaseDto> DecideKycAsync(Guid kycCaseId, KycStatus status, string? note, Guid userId);
    Task<PaginatedResponse<KycCaseDto>> GetKycQueueAsync(ListQueryDto query, KycStatus? status);

    Task<CautionListEntryDto> AddCautionAsync(CautionListEntryDto dto, Guid userId);
    Task<CautionListEntryDto> ClearCautionAsync(Guid id, string? note, Guid userId);
    Task<PaginatedResponse<CautionListEntryDto>> GetCautionListAsync(ListQueryDto query);

    // Enquiries
    Task<PaginatedResponse<EnquiryListItemDto>> GetEnquiriesAsync(EnquirySearchDto query);
    Task<EnquiryBoardDto> GetBoardAsync(EnquirySearchDto query);
    Task<EnquiryDetailDto?> GetEnquiryAsync(Guid id);
    Task<EnquiryDetailDto> SaveEnquiryAsync(EnquiryUpsertDto dto, Guid userId);
    Task<EnquiryDetailDto> ChangeStageAsync(EnquiryStageChangeDto dto, Guid userId);
    Task<EnquiryDetailDto> AssignAsync(Guid enquiryId, Guid agentId, Guid userId);
    Task<int> RouteUnassignedAsync();
    Task<int> FlagSlaBreachesAsync();

    // Requirements & matching
    Task<RequirementProfileDto> SaveRequirementAsync(RequirementProfileUpsertDto dto, Guid userId);
    Task<List<RequirementProfileDto>> GetRequirementsAsync(Guid partyId);
    Task<List<MatchResultDto>> RunMatchAsync(Guid requirementProfileId, int take);
    Task<List<MatchResultDto>> MatchListingToRequirementsAsync(Guid listingId, int take);
    Task<int> SendMatchesAsync(SendMatchesDto dto, Guid userId);
    Task DismissMatchAsync(Guid matchId, Guid? reasonCodeId, Guid userId);

    // Activities & tasks
    Task<ActivityDto> LogActivityAsync(ActivityCreateDto dto, Guid userId);
    Task<PaginatedResponse<ActivityDto>> GetActivitiesAsync(Guid? partyId, Guid? enquiryId, ListQueryDto query);
    Task<FollowUpTaskDto> SaveTaskAsync(FollowUpTaskCreateDto dto, Guid userId);
    Task<FollowUpTaskDto> CompleteTaskAsync(TaskCompletionDto dto, Guid userId);
    Task<FollowUpTaskDto> SnoozeTaskAsync(Guid taskId, DateTime until, Guid userId);
    Task<MyDayDto> GetMyDayAsync(Guid userId, DateOnly date);

    // Viewings & visits
    Task<DiaryDayDto> GetDiaryAsync(Guid? agentId, DateOnly date, Guid userId);
    Task<PaginatedResponse<ViewingListItemDto>> GetViewingsAsync(ListQueryDto query, Guid? agentId, ViewingStatus? status);
    Task<ViewingDetailDto?> GetViewingAsync(Guid id);
    Task<ViewingDetailDto> SaveViewingAsync(ViewingUpsertDto dto, Guid userId);
    Task<ViewingDetailDto> ChangeViewingStatusAsync(Guid id, ViewingStatus status, Guid? reasonCodeId, string? note, Guid userId);
    Task<ViewingFeedbackDto> SaveFeedbackAsync(ViewingFeedbackDto dto, Guid userId);
    Task<ViewingFeedbackDto> ShareFeedbackWithVendorAsync(Guid feedbackId, Guid userId);

    Task<PaginatedResponse<SiteVisitListItemDto>> GetSiteVisitsAsync(ListQueryDto query, Guid? projectId, ViewingStatus? status);
    Task<SiteVisitDetailDto?> GetSiteVisitAsync(Guid id);
    Task<SiteVisitDetailDto> SaveSiteVisitAsync(SiteVisitUpsertDto dto, Guid userId);
    Task<SiteVisitDetailDto> ChangeVisitStatusAsync(Guid id, ViewingStatus status, Guid? reasonCodeId, Guid userId);
    Task<SiteVisitFeedbackDto> SaveVisitFeedbackAsync(SiteVisitFeedbackDto dto, Guid userId);

    Task<List<KeySetDto>> GetKeysAsync(Guid? propertyId, bool outOnly);
    Task<KeySetDto> SaveKeySetAsync(KeySetDto dto, Guid userId);
    Task<KeySetDto> MoveKeysAsync(Guid keySetId, string movement, Guid? holderUserId, Guid? holderPartyId, DateTime? dueBack, string? note, Guid userId);
}

public class PartySearchDto : ListQueryDto
{
    public PartyRoleKind? Role { get; set; }
    public PartyKind? Kind { get; set; }
    public KycStatus? KycStatus { get; set; }
    public bool? CautionedOnly { get; set; }
    public bool? WithOutstandingOnly { get; set; }
    public Guid? OwnerAgentId { get; set; }
}

public class EnquirySearchDto : ListQueryDto
{
    public List<EnquiryStage> Stages { get; set; } = [];
    public List<EnquiryChannel> Channels { get; set; } = [];
    public Guid? AssignedAgentId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? CampaignId { get; set; }
    public ListingKind? Interest { get; set; }
    public bool? BreachingSlaOnly { get; set; }
    public bool? UnassignedOnly { get; set; }
    public bool? OverdueFollowUpOnly { get; set; }
    public int? MinScore { get; set; }
    public int ItemsPerColumn { get; set; } = 25;
}
