using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Enums;

namespace RealEstate.Application.Services.Interfaces;

/// <summary>Offers, tokens, bookings, allotment and balloting.</summary>
public interface IBookingService
{
    // Offers (brokerage)
    Task<PaginatedResponse<OfferListItemDto>> GetOffersAsync(ListQueryDto query, Guid? propertyId, OfferStatus? status);
    Task<OfferDetailDto?> GetOfferAsync(Guid id);
    Task<OfferDetailDto> SaveOfferAsync(OfferUpsertDto dto, Guid userId);
    Task<OfferDetailDto> DecideOfferAsync(OfferDecisionDto dto, Guid userId);

    // EOI & token (development)
    Task<PaginatedResponse<ExpressionOfInterestDto>> GetEoisAsync(ListQueryDto query, Guid? projectId);
    Task<ExpressionOfInterestDto> SaveEoiAsync(ExpressionOfInterestDto dto, ReceiptCreateDto? payment, Guid userId);
    Task<ExpressionOfInterestDto> RefundEoiAsync(Guid id, Guid userId);

    Task<PaginatedResponse<TokenReservationDto>> GetTokensAsync(ListQueryDto query, Guid? projectId, ReservationStatus? status);
    Task<TokenReservationDto> CreateTokenAsync(TokenReservationCreateDto dto, Guid userId);
    Task<TokenReservationDto> CancelTokenAsync(Guid id, Guid? reasonCodeId, bool forfeit, Guid userId);
    Task<int> ExpireTokensAsync();

    // Bookings
    Task<PaginatedResponse<BookingListItemDto>> GetBookingsAsync(BookingSearchDto query);
    Task<BookingDetailDto?> GetBookingAsync(Guid id);
    Task<BookingPreviewDto> PreviewBookingAsync(BookingCreateDto dto);
    Task<BookingDetailDto> CreateBookingAsync(BookingCreateDto dto, Guid userId);
    Task<BookingDetailDto> ApproveBookingAsync(BookingApprovalDto dto, Guid userId);
    Task<BookingDetailDto> ConfirmBookingAsync(Guid bookingId, Guid userId);
    Task<BookingAmendmentDto> RequestAmendmentAsync(BookingAmendmentDto dto, Guid userId);
    Task<BookingAmendmentDto> DecideAmendmentAsync(Guid amendmentId, ApprovalOutcome outcome, string? comment, Guid userId);

    // Allotment
    Task<AllotmentDto> IssueAllotmentAsync(Guid bookingId, Guid? templateId, Guid userId);
    Task<AllotmentDto> ReissueAllotmentAsync(Guid allotmentId, string reason, Guid userId);
    Task<PaginatedResponse<AllotmentDto>> GetAllotmentsAsync(ListQueryDto query, Guid? projectId);

    // Agreements
    Task<SaleAgreementDto> GenerateAgreementAsync(Guid bookingId, Guid? templateId, string languageCode, Guid userId);
    Task<SaleAgreementDto> RecordExecutionAsync(Guid agreementId, DateOnly executedOn, string? registrationNumber, Guid userId);

    // Ballot
    Task<PaginatedResponse<BallotDto>> GetBallotsAsync(ListQueryDto query, Guid? projectId);
    Task<BallotDto?> GetBallotAsync(Guid id);
    Task<BallotDto> SaveBallotAsync(BallotCreateDto dto, Guid userId);
    Task<BallotDto> LockPoolAsync(Guid ballotId, Guid userId);
    Task<BallotResultDto> DrawAsync(BallotDrawDto dto, Guid userId);
    Task<BallotDto> PublishBallotAsync(Guid ballotId, Guid userId);
    Task<List<BallotEntryDto>> GetBallotEntriesAsync(Guid ballotId, Guid? categoryId);
    Task<BallotEntryDto> OverrideAllocationAsync(Guid entryId, Guid unitId, string reason, Guid userId);
}

public class BookingSearchDto : ListQueryDto
{
    public List<BookingStatus> Statuses { get; set; } = [];
    public Guid? UnitId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? SalesExecutiveId { get; set; }
    public SourcingChannel? SourcingChannel { get; set; }
    public bool? OverdueOnly { get; set; }
    public bool? DefaultingOnly { get; set; }
    public decimal? MinCollectionPercent { get; set; }
    public decimal? MaxCollectionPercent { get; set; }
}

/// <summary>
/// Payment plans, demands, the surcharge engine, receipts, allocation and the collections desk.
/// The engine the whole Development line of business turns on.
/// </summary>
public interface IMoneyService
{
    // Templates & plans
    Task<List<PaymentPlanTemplateDto>> GetTemplatesAsync(Guid? projectId, bool activeOnly);
    Task<PaymentPlanTemplateDto?> GetTemplateAsync(Guid id);
    Task<PaymentPlanTemplateDto> SaveTemplateAsync(PaymentPlanTemplateDto dto, Guid userId);
    Task<PaymentPlanPreviewDto> PreviewPlanAsync(Guid? templateId, PaymentPlanCustomDto? custom, decimal totalConsideration, DateOnly startDate, Guid? projectId);

    Task<PaymentPlanDto?> GetPlanAsync(Guid bookingId);
    Task<PaymentPlanDto> RestructureAsync(PlanRestructureDto dto, Guid userId);

    // Surcharge
    Task<List<SurchargePolicyDto>> GetSurchargePoliciesAsync(Guid? projectId);
    Task<SurchargePolicyDto> SaveSurchargePolicyAsync(SurchargePolicyDto dto, Guid userId);
    Task<int> AccrueSurchargeAsync(DateOnly asOf);
    Task<SurchargeWaiverDto> RequestWaiverAsync(SurchargeWaiverRequestDto dto, Guid userId);
    Task<SurchargeWaiverDto> DecideWaiverAsync(Guid waiverId, ApprovalOutcome outcome, decimal? approvedAmount, string? comment, Guid userId);
    Task<PaginatedResponse<SurchargeWaiverDto>> GetWaiversAsync(ListQueryDto query, ApprovalOutcome? outcome);

    // Demands
    Task<DemandBatchDto> RunDemandsAsync(DemandRunRequestDto dto, Guid userId);
    Task<PaginatedResponse<DemandBatchDto>> GetDemandBatchesAsync(ListQueryDto query);
    Task<PaginatedResponse<DemandListItemDto>> GetDemandsAsync(ListQueryDto query, Guid? bookingId, DemandStatus? status);
    Task<DemandDetailDto?> GetDemandAsync(Guid id);
    Task<DemandDetailDto> SendDemandAsync(Guid id, List<NotificationChannel> channels, Guid userId);
    Task CancelDemandAsync(Guid id, Guid reasonCodeId, Guid userId);

    // Receipts
    Task<AllocationPreviewDto> PreviewAllocationAsync(ReceiptCreateDto dto);
    Task<ReceiptDetailDto> CreateReceiptAsync(ReceiptCreateDto dto, Guid userId);
    Task<PaginatedResponse<ReceiptListItemDto>> GetReceiptsAsync(ReceiptSearchDto query);
    Task<ReceiptDetailDto?> GetReceiptAsync(Guid id);
    Task<ReceiptDetailDto> ReverseReceiptAsync(Guid id, Guid reasonCodeId, string? note, Guid userId);
    Task<ReceiptDetailDto> ReallocateAsync(Guid receiptId, List<ManualAllocationDto> allocations, string reason, Guid userId);

    // Cheques
    Task<PaginatedResponse<ChequeRecordDto>> GetChequesAsync(ListQueryDto query, ChequeState? state);
    Task<ChequeRecordDto> ChangeChequeStateAsync(ChequeStateChangeDto dto, Guid userId);
    Task<List<ChequeRecordDto>> GetMaturityCalendarAsync(DateOnly from, DateOnly to);

    // Ledger & statements
    Task<PaginatedResponse<CustomerLedgerEntryDto>> GetLedgerAsync(Guid? bookingId, Guid? partyId, ListQueryDto query);
    Task<StatementOfAccountDto> GetStatementAsync(Guid bookingId, DateOnly? from, DateOnly? to);

    // Collections
    Task<CollectionWorklistDto> GetWorklistAsync(CollectionQueryDto query);
    Task<PromiseToPayDto> RecordPromiseAsync(PromiseToPayCreateDto dto, Guid userId);
    Task<List<PromiseToPayDto>> GetPromisesAsync(DateOnly? dueOn, PromiseState? state);
    Task<int> EvaluatePromisesAsync();

    Task<List<DunningPolicyDto>> GetDunningPoliciesAsync(string? appliesTo);
    Task<DunningPolicyDto> SaveDunningPolicyAsync(DunningPolicyDto dto, Guid userId);
    Task<PaginatedResponse<DunningCaseDto>> GetDunningCasesAsync(ListQueryDto query, bool openOnly);
    Task<DunningCaseDto?> GetDunningCaseAsync(Guid id);
    Task<DunningCaseDto> SuspendDunningAsync(Guid caseId, DateOnly? until, string reason, Guid userId);
    Task<int> RunDunningAsync(DateOnly asOf);

    Task<LegalNoticeDto> IssueNoticeAsync(Guid? bookingId, Guid? tenancyId, string noticeType, DateOnly? complyBy, Guid? templateId, Guid userId);
    Task<LegalNoticeDto> RecordServiceAsync(Guid noticeId, string method, string? reference, string? evidenceUrl, DateOnly servedOn, Guid userId);
    Task<PaginatedResponse<LegalNoticeDto>> GetNoticesAsync(ListQueryDto query);

    Task<WriteOffDto> RequestWriteOffAsync(WriteOffDto dto, Guid userId);
    Task<WriteOffDto> DecideWriteOffAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId);
}

public class ReceiptSearchDto : ListQueryDto
{
    public Guid? PartyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public PaymentInstrument? Instrument { get; set; }
    public ReceiptStatus? Status { get; set; }
    public bool? UnallocatedOnly { get; set; }
    public bool? ClientMoneyOnly { get; set; }
}

public class CollectionQueryDto : ListQueryDto
{
    public Guid? AssignedToUserId { get; set; }
    public int? MinDaysOverdue { get; set; }
    public decimal? MinAmount { get; set; }
    public string? AgeingBucket { get; set; }
    public bool? WithPromiseOnly { get; set; }
    public bool? BrokenPromiseOnly { get; set; }
    public int? DunningStep { get; set; }
}

/// <summary>Cancellation, refund, transfer, possession, snagging and the NOCs we issue.</summary>
public interface IExitService
{
    // Cancellation & refund
    Task<CancellationPreviewDto> PreviewCancellationAsync(CancellationRequestDto dto);
    Task<CancellationDto> RequestCancellationAsync(CancellationRequestDto dto, Guid userId);
    Task<CancellationDto> DecideCancellationAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId);
    Task<PaginatedResponse<CancellationDto>> GetCancellationsAsync(ListQueryDto query);

    Task<List<DeductionPolicyDto>> GetDeductionPoliciesAsync(Guid? projectId);
    Task<DeductionPolicyDto> SaveDeductionPolicyAsync(DeductionPolicyDto dto, Guid userId);

    Task<RefundRequestDto> CreateRefundAsync(RefundRequestDto dto, Guid userId);
    Task<RefundRequestDto> DecideRefundAsync(Guid id, ApprovalOutcome outcome, decimal? approvedAmount, string? comment, Guid userId);
    Task<RefundRequestDto> RecordRefundPaymentAsync(Guid scheduleId, DateOnly paidOn, PaymentInstrument instrument, string? reference, Guid userId);
    Task<PaginatedResponse<RefundRequestDto>> GetRefundsAsync(ListQueryDto query, RefundStatus? status);

    Task<ResaleRequestDto> CreateResaleAsync(ResaleRequestDto dto, Guid userId);

    // Transfer
    Task<PaginatedResponse<TransferRequestListItemDto>> GetTransfersAsync(ListQueryDto query, TransferStatus? status, Guid? projectId);
    Task<TransferRequestDetailDto?> GetTransferAsync(Guid id);
    Task<TransferRequestDetailDto> CreateTransferAsync(TransferRequestCreateDto dto, Guid userId);
    Task<DuesClearanceDto> IssueDuesClearanceAsync(Guid? bookingId, Guid? unitId, Guid? propertyId, Guid partyId, Guid userId);
    Task<TransferRequestDetailDto> OverrideDuesAsync(Guid transferId, string reason, Guid userId);
    Task<TransferRequestDetailDto> ComputeFeesAsync(Guid transferId, Guid userId);
    Task<TransferSessionDto> ScheduleSessionAsync(Guid transferId, DateTime scheduledAt, string? venue, Guid userId);
    Task<TransferSessionDto> SaveSessionAsync(TransferSessionDto dto, Guid userId);
    Task<TransferRequestDetailDto> CompleteTransferAsync(Guid transferId, Guid userId);
    Task<TransferRequestDetailDto> RejectTransferAsync(Guid transferId, Guid reasonCodeId, string? note, Guid userId);
    Task<List<OwnershipChainEntryDto>> GetOwnershipChainAsync(Guid? unitId, Guid? propertyId, Guid? plotFileId);

    Task<DuplicateFileRequestDto> CreateDuplicateFileRequestAsync(DuplicateFileRequestDto dto, Guid userId);
    Task<DuplicateFileRequestDto> IssueDuplicateFileAsync(Guid id, Guid userId);

    // Possession
    Task<PossessionOfferDto> EvaluatePossessionAsync(Guid bookingId);
    Task<PossessionOfferDto> OfferPossessionAsync(Guid bookingId, DateOnly windowFrom, DateOnly windowTo, Guid? templateId, Guid userId);
    Task<PossessionOfferDto> SetAppointmentAsync(Guid possessionOfferId, DateOnly on, Guid userId);
    Task<PossessionOfferDto> OverrideChecklistAsync(Guid checklistItemId, string reason, Guid userId);
    Task<PaginatedResponse<PossessionOfferDto>> GetPossessionsAsync(ListQueryDto query, PossessionStatus? status, Guid? projectId);

    Task<HandoverDto> CompleteHandoverAsync(HandoverDto dto, Guid userId);
    Task<HandoverDto?> GetHandoverAsync(Guid id);

    // Snagging
    Task<SnagInspectionDto> CreateInspectionAsync(SnagInspectionDto dto, Guid userId);
    Task<SnagInspectionDto?> GetInspectionAsync(Guid id);
    Task<PaginatedResponse<SnagInspectionDto>> GetInspectionsAsync(ListQueryDto query, Guid? projectId, bool openOnly);
    Task<SnagDto> SaveSnagAsync(SnagUpsertDto dto, Guid userId);
    Task<SnagInspectionDto> SyncSnagsAsync(SnagSyncBatchDto batch, Guid userId);
    Task<SnagDto> ChangeSnagStatusAsync(Guid snagId, SnagStatus status, string? note, List<SnagPhotoDto>? photos, Guid userId);
    Task<PunchListDto> IssuePunchListAsync(Guid inspectionId, DateOnly? agreedClosureDate, Guid userId);

    // Defect liability
    Task<List<DefectLiabilityDto>> GetLiabilitiesAsync(Guid? unitId, Guid? projectId, bool activeOnly);
    Task<DefectClaimDto> CreateDefectClaimAsync(DefectClaimDto dto, Guid userId);
    Task<DefectClaimDto> DecideDefectClaimAsync(Guid id, bool accepted, string? reason, Guid userId);
    Task<PaginatedResponse<DefectClaimDto>> GetDefectClaimsAsync(ListQueryDto query, TicketStatus? status);

    // NOC
    Task<NocIssuanceDto> RequestNocAsync(NocRequestDto dto, Guid userId);
    Task<NocIssuanceDto> IssueNocAsync(Guid id, Guid userId);
    Task<NocIssuanceDto> RevokeNocAsync(Guid id, string reason, Guid userId);
    Task<PaginatedResponse<NocIssuanceDto>> GetNocsAsync(ListQueryDto query, NocKind? kind, NocStatus? status);
    Task<NocIssuanceDto?> VerifyNocAsync(string verificationCode);
}

/// <summary>Brokerage deals, chains, conveyancing, commission and the channel-partner network.</summary>
public interface IBrokerageService
{
    Task<PaginatedResponse<DealListItemDto>> GetDealsAsync(ListQueryDto query, DealStatus? status, Guid? agentId);
    Task<DealBoardDto> GetDealBoardAsync(ListQueryDto query, Guid? agentId);
    Task<DealDetailDto?> GetDealAsync(Guid id);
    Task<DealDetailDto> CreateDealAsync(DealCreateDto dto, Guid userId);
    Task<DealDetailDto> UpdateChecklistAsync(Guid dealId, Guid itemId, bool completed, DateOnly? completedOn, string? note, Guid userId);
    Task<DealDetailDto> ChangeDealStatusAsync(Guid id, DealStatus status, Guid userId);
    Task<FallThroughRecordDto> RecordFallThroughAsync(FallThroughRecordDto dto, Guid userId);
    Task<DealPartyDto> SaveDealPartyAsync(Guid dealId, DealPartyDto dto, Guid userId);

    Task<SalesChainDto> SaveChainAsync(SalesChainDto dto, Guid userId);
    Task<List<SalesChainDto>> GetChainsAsync(bool atRiskOnly);
    Task<ConveyancingDto> SaveConveyancingAsync(ConveyancingDto dto, Guid userId);

    // Commission
    Task<List<CommissionPlanDto>> GetCommissionPlansAsync(string? appliesTo, Guid? projectId);
    Task<CommissionPlanDto> SaveCommissionPlanAsync(CommissionPlanDto dto, Guid userId);
    Task<CommissionCalculationDto> CalculateAsync(Guid? dealId, Guid? bookingId, Guid? tenancyId, bool commit, Guid userId);
    Task<PaginatedResponse<CommissionCalculationDto>> GetCalculationsAsync(ListQueryDto query, CommissionStatus? status);
    Task<CommissionDisbursementDto> CreateDisbursementAsync(Guid calculationId, Guid userId);
    Task<CommissionDisbursementDto> DecideDisbursementAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId);
    Task<CommissionPayoutDto> CreatePayoutAsync(Guid? agentId, Guid? partnerId, DateOnly from, DateOnly to, Guid userId);
    Task<PaginatedResponse<CommissionPayoutDto>> GetPayoutsAsync(ListQueryDto query);
    Task<AgentCapLedgerDto?> GetCapPositionAsync(Guid agentId, int? year);
    Task<int> ClawBackAsync(Guid bookingId, Guid userId);

    // Channel partners
    Task<PaginatedResponse<ChannelPartnerListItemDto>> GetPartnersAsync(ListQueryDto query, PartnerStatus? status);
    Task<ChannelPartnerDetailDto?> GetPartnerAsync(Guid id);
    Task<ChannelPartnerDetailDto> SavePartnerAsync(ChannelPartnerUpsertDto dto, Guid userId);
    Task<ChannelPartnerDetailDto> ChangePartnerStatusAsync(Guid id, PartnerStatus status, string? reason, Guid userId);
    Task<List<PartnerTierDto>> GetTiersAsync();
    Task<PartnerTierDto> SaveTierAsync(PartnerTierDto dto, Guid userId);
    Task<List<PartnerCommissionRateDto>> GetPartnerRatesAsync(Guid? partnerId, Guid? projectId);
    Task<PartnerCommissionRateDto> SavePartnerRateAsync(PartnerCommissionRateDto dto, Guid userId);

    Task<LeadRegistrationDto> RegisterLeadAsync(LeadRegistrationCreateDto dto, Guid userId);
    Task<PaginatedResponse<LeadRegistrationDto>> GetRegistrationsAsync(ListQueryDto query, Guid? partnerId, LeadRegistrationStatus? status);
    Task<LeadRegistrationDto> ExtendRegistrationAsync(Guid id, int days, Guid userId);
    Task<int> ExpireRegistrationsAsync();

    Task<PaginatedResponse<PartnerCommissionEntryDto>> GetPartnerCommissionAsync(ListQueryDto query, Guid? partnerId, CommissionStatus? status);
    Task<PartnerStatementDto> GeneratePartnerStatementAsync(Guid partnerId, DateOnly from, DateOnly to, Guid userId);
    Task<PartnerAdvanceDto> CreateAdvanceAsync(PartnerAdvanceDto dto, Guid userId);
    Task<List<PartnerContestDto>> GetContestsAsync(bool activeOnly);
    Task<PartnerContestDto> SaveContestAsync(PartnerContestDto dto, Guid userId);
    Task<PartnerPortalHomeDto> GetPartnerPortalHomeAsync(Guid partnerId);
}

/// <summary>Tenancies, the rent roll, service charges, landlords and client money.</summary>
public interface ILeasingService
{
    Task<PaginatedResponse<TenancyListItemDto>> GetTenanciesAsync(TenancySearchDto query);
    Task<TenancyDetailDto?> GetTenancyAsync(Guid id);
    Task<TenancyDetailDto> CreateTenancyAsync(TenancyCreateDto dto, Guid userId);
    Task<TenancyDetailDto> UpdateTenancyAsync(Guid id, TenancyCreateDto dto, Guid userId);
    Task<TenancyDetailDto> ChangeStatusAsync(Guid id, TenancyStatus status, Guid userId);
    Task<List<RentChargeDto>> RegenerateScheduleAsync(Guid tenancyId, Guid userId);

    Task<ReferencingCaseDto> SaveReferencingAsync(ReferencingCaseDto dto, Guid userId);
    Task<ReferencingCaseDto> DecideReferencingAsync(Guid id, ReferencingOutcome outcome, string? conditions, string? failureReason, Guid userId);

    Task<SecurityDepositDto> SaveDepositAsync(SecurityDepositDto dto, Guid userId);
    Task<SecurityDepositDto> RegisterDepositAsync(Guid depositId, string schemeName, string reference, DateOnly registeredOn, Guid userId);
    Task<SecurityDepositDto> ProposeDeductionsAsync(Guid depositId, List<DepositDeductionDto> deductions, Guid userId);
    Task<SecurityDepositDto> ReleaseDepositAsync(Guid depositId, decimal toTenant, decimal toLandlord, Guid userId);
    Task<PaginatedResponse<SecurityDepositDto>> GetDepositsAsync(ListQueryDto query, bool unregisteredOnly);

    Task<MoveInspectionDto> SaveInspectionAsync(MoveInspectionDto dto, Guid userId);
    Task<MoveInspectionDto?> GetInspectionAsync(Guid id);
    Task<PaginatedResponse<MoveInspectionDto>> GetInspectionsAsync(ListQueryDto query, InspectionKind? kind);

    Task<TenancyNoticeDto> ServeNoticeAsync(TenancyNoticeDto dto, Guid userId);
    Task<TenancyRenewalDto> OfferRenewalAsync(TenancyRenewalDto dto, Guid userId);
    Task<TenancyRenewalDto> DecideRenewalAsync(Guid id, string status, decimal? agreedRent, Guid? declineReasonCodeId, Guid userId);
    Task<PaginatedResponse<TenancyRenewalDto>> GetRenewalPipelineAsync(ListQueryDto query, int withinDays);
    Task<RentReviewDto> SaveRentReviewAsync(RentReviewDto dto, Guid userId);

    Task<List<CriticalDateDto>> GetCriticalDatesAsync(int withinDays, Guid? propertyId);
    Task<CriticalDateDto> ActionCriticalDateAsync(Guid id, string? note, Guid userId);

    Task<PaginatedResponse<ComplianceCertificateDto>> GetCertificatesAsync(ListQueryDto query, bool expiringOnly);
    Task<ComplianceCertificateDto> SaveCertificateAsync(ComplianceCertificateDto dto, Guid userId);

    // Rent roll
    Task<RentRollDto> GetRentRollAsync(Guid? propertyId, Guid? projectId, DateOnly asOf);
    Task<RentRunDto> RunRentAsync(RentRunRequestDto dto, Guid userId);
    Task<PaginatedResponse<RentRunDto>> GetRentRunsAsync(ListQueryDto query);
    Task<PaginatedResponse<ArrearsCaseDto>> GetArrearsAsync(ListQueryDto query, int? minDays);
    Task<List<VoidRecordDto>> GetVoidsAsync(Guid? propertyId, bool openOnly);
    Task<List<TenantCategoryDto>> GetTenantMixAsync(Guid propertyId);

    // Service charge
    Task<PaginatedResponse<ServiceChargeBudgetDto>> GetBudgetsAsync(ListQueryDto query, Guid? propertyId);
    Task<ServiceChargeBudgetDto?> GetBudgetAsync(Guid id);
    Task<ServiceChargeBudgetDto> SaveBudgetAsync(ServiceChargeBudgetDto dto, Guid userId);
    Task<ServiceChargeBudgetDto> ApproveBudgetAsync(Guid id, Guid userId);
    Task<List<ServiceChargeInvoiceDto>> RaiseOnAccountAsync(Guid budgetId, DateOnly periodFrom, DateOnly periodTo, bool dryRun, Guid userId);
    Task<ServiceChargeReconciliationDto> ReconcileAsync(Guid budgetId, bool dryRun, Guid userId);
    Task<ServiceChargeReconciliationDto> FinaliseReconciliationAsync(Guid id, Guid userId);
    Task<List<ApportionmentScheduleDto>> GetApportionmentsAsync(Guid propertyId);
    Task<ApportionmentScheduleDto> SaveApportionmentAsync(ApportionmentScheduleDto dto, Guid userId);

    // Turnover rent
    Task<TurnoverRentTermDto> SaveTurnoverTermAsync(TurnoverRentTermDto dto, Guid userId);
    Task<TenantSalesDeclarationDto> SaveSalesDeclarationAsync(TenantSalesDeclarationDto dto, Guid userId);
    Task<PaginatedResponse<TenantSalesDeclarationDto>> GetSalesDeclarationsAsync(ListQueryDto query, Guid? propertyId, bool overdueOnly);
    Task<List<OverageInvoiceDto>> CalculateOverageAsync(Guid? propertyId, DateOnly periodFrom, DateOnly periodTo, bool dryRun, Guid userId);

    // Landlords & client money
    Task<PaginatedResponse<LandlordListItemDto>> GetLandlordsAsync(ListQueryDto query);
    Task<LandlordDetailDto?> GetLandlordAsync(Guid id);
    Task<LandlordDetailDto> SaveLandlordAsync(LandlordDetailDto dto, Guid userId);
    Task<ManagementAgreementDto> SaveManagementAgreementAsync(Guid landlordId, ManagementAgreementDto dto, Guid userId);
    Task<OwnerStatementDto> GenerateOwnerStatementAsync(Guid landlordId, DateOnly from, DateOnly to, Guid? propertyId, Guid userId);
    Task<PaginatedResponse<OwnerStatementDto>> GetOwnerStatementsAsync(ListQueryDto query, Guid? landlordId);
    Task<OwnerPayoutDto> CreatePayoutRunAsync(DateOnly payoutDate, Guid? officeId, Guid clientAccountId, bool dryRun, Guid userId);
    Task<OwnerPayoutDto> SubmitPayoutRunAsync(Guid id, Guid userId);
    Task<PaginatedResponse<OwnerPayoutDto>> GetPayoutRunsAsync(ListQueryDto query);

    Task<List<ClientAccountDto>> GetClientAccountsAsync();
    Task<ClientAccountDto> SaveClientAccountAsync(ClientAccountDto dto, Guid userId);
    Task<PaginatedResponse<ClientLedgerEntryDto>> GetClientLedgerAsync(Guid accountId, ListQueryDto query);
    Task<ClientMoneyReconciliationDto> ReconcileClientMoneyAsync(Guid accountId, DateOnly asOf, decimal bankBalance, Guid userId);
    Task<ClientMoneyReconciliationDto> SignOffReconciliationAsync(Guid id, Guid userId);
    Task<PaginatedResponse<ClientMoneyExceptionDto>> GetClientMoneyExceptionsAsync(ListQueryDto query, bool openOnly);
    Task<OwnerPortalHomeDto> GetOwnerPortalHomeAsync(Guid landlordId);
    Task<TenantPortalHomeDto> GetTenantPortalHomeAsync(Guid partyId);
}

public class TenancySearchDto : ListQueryDto
{
    public List<TenancyStatus> Statuses { get; set; } = [];
    public List<TenancyKind> Kinds { get; set; } = [];
    public Guid? PropertyId { get; set; }
    public Guid? LandlordId { get; set; }
    public Guid? TenantPartyId { get; set; }
    public Guid? ManagedByUserId { get; set; }
    public bool? InArrearsOnly { get; set; }
    public bool? ExpiringOnly { get; set; }
    public int ExpiringWithinDays { get; set; } = 90;
    public ManagementService? ManagementService { get; set; }
}
