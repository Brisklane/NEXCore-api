using Fitness.Application.DTOs;
using Fitness.Domain.Enums;
using Nexcore.SharedKernel.Api;

namespace Fitness.Application.Services.Interfaces;

/// <summary>
/// The member lifecycle: joining, the 360 view, status transitions and the data-protection
/// obligations that come with holding this much about a person.
/// </summary>
public interface IMemberService
{
    Task<PaginatedResponse<MemberSummaryDto>> ListAsync(
        Guid? clubId, MemberStatus? status, ChurnRiskBand? riskBand, string? search,
        Guid? planId, bool? hasBalance, PaginationParams pagination);

    Task<MemberDetailDto?> GetAsync(Guid memberId);

    /// <summary>Desk lookup: name, phone, member number or a scanned credential.</summary>
    Task<List<MemberSummaryDto>> SearchAsync(MemberSearchDto request);

    Task<MemberDetailDto> CreateAsync(SaveMemberDto request, Guid userId);
    Task<MemberDetailDto> UpdateAsync(Guid memberId, SaveMemberDto request, Guid userId);

    /// <summary>
    /// Person, plan, paperwork and payment in one transaction — because joining is one decision,
    /// and splitting it produces the half-joined member every club has a hundred of.
    /// </summary>
    Task<JoinResultDto> JoinAsync(JoinMemberDto request, Guid userId);

    Task<MemberDetailDto> ChangeStatusAsync(ChangeMemberStatusDto request, Guid userId);
    Task<MemberDetailDto> SetBanAsync(BanMemberDto request, Guid userId);

    Task<List<MemberNoteDto>> GetTimelineAsync(Guid memberId, int limit = 100);
    Task<MemberNoteDto> AddNoteAsync(MemberNoteDto request, Guid userId);
    Task<List<MemberAlertDto>> GetAlertsAsync(Guid memberId);

    /// <summary>Rebuilds the alert rows for a member. Called after anything that could change them.</summary>
    Task RefreshAlertsAsync(Guid memberId);

    Task<List<VisitHistoryDto>> GetVisitHistoryAsync(Guid memberId, int limit = 50);
    Task<List<MemberLedgerEntryDto>> GetLedgerAsync(Guid memberId, int limit = 100);
    Task<List<UpcomingBookingDto>> GetUpcomingAsync(Guid memberId);

    Task<MemberCredentialDto> IssueCredentialAsync(IssueCredentialDto request, Guid userId);
    Task DeactivateCredentialAsync(Guid credentialId, string reason, Guid userId);

    Task<HouseholdDto> SaveHouseholdAsync(SaveHouseholdDto request, Guid userId);
    Task<HouseholdDto?> GetHouseholdAsync(Guid householdId);
    Task<PaginatedResponse<HouseholdDto>> ListHouseholdsAsync(
        Guid? clubId, string? search, PaginationParams pagination);

    /// <summary>Shows what a merge would do before it does it, because a merge cannot be un-run.</summary>
    Task<MergePreviewDto> PreviewMergeAsync(MergeMembersDto request);
    Task<MemberDetailDto> MergeAsync(MergeMembersDto request, Guid userId);

    /// <summary>Everything held about a member, for a subject access request.</summary>
    Task<MemberExportDto> ExportAsync(Guid memberId, Guid userId);

    /// <summary>Erasure, keeping only what records law requires the club to hold.</summary>
    Task AnonymiseAsync(AnonymiseMemberDto request, Guid userId);
}

/// <summary>Plans, prices, entitlements and promotions — everything the club can sell.</summary>
public interface ICatalogueService
{
    Task<List<MembershipPlanDto>> GetPlansAsync(Guid? clubId, PlanKind? kind, bool sellableOnly);
    Task<MembershipPlanDto?> GetPlanAsync(Guid planId);
    Task<MembershipPlanDto> SavePlanAsync(Guid? planId, SavePlanDto request, Guid userId);
    Task DeletePlanAsync(Guid planId, Guid userId);

    /// <summary>What a join wizard or a till renders, priced for this club.</summary>
    Task<SalesCatalogueDto> GetSalesCatalogueAsync(Guid clubId);

    Task<List<PromotionRuleDto>> GetPromotionsAsync(Guid? clubId, bool activeOnly);
    Task<PromotionRuleDto> SavePromotionAsync(Guid? id, PromotionRuleDto request, Guid userId);
    Task<PromoCodeResultDto> CheckPromoCodeAsync(PromoCodeCheckDto request);

    Task<List<AppointmentServiceDto>> GetServicesAsync(Guid? clubId, bool activeOnly);
    Task<AppointmentServiceDto> SaveServiceAsync(Guid? id, AppointmentServiceDto request, Guid userId);

    Task<List<PlanChangePathDto>> GetChangePathsAsync(Guid fromPlanId);
}

/// <summary>
/// Agreements and everything that happens to them: signing, amending, freezing, suspending and
/// the cancellation conversation that decides whether the member stays.
/// </summary>
public interface IAgreementService
{
    Task<PaginatedResponse<AgreementSummaryDto>> ListAsync(
        Guid? clubId, Guid? memberId, AgreementStatus? status, Guid? planId,
        DateTime? endingBefore, PaginationParams pagination);

    Task<AgreementDetailDto?> GetAsync(Guid agreementId);
    Task<AgreementDetailDto> CreateAsync(CreateAgreementDto request, Guid userId);

    Task<AgreementSignatureDto> SignAsync(SignAgreementDto request, Guid userId);
    Task<AgreementSignatureDto> RequestRemoteSignatureAsync(RequestRemoteSignatureDto request, Guid userId);

    /// <summary>What an upgrade or downgrade costs, shown before anybody commits to it.</summary>
    Task<PlanChangePreviewDto> PreviewPlanChangeAsync(Guid agreementId, Guid newPlanId, DateTime? effectiveOn);
    Task<AgreementDetailDto> ChangePlanAsync(ChangePlanDto request, Guid userId);

    // ── Freezes ──────────────────────────────────────────────────────────────

    Task<FreezePreviewDto> PreviewFreezeAsync(RequestFreezeDto request);
    Task<MembershipFreezeDto> FreezeAsync(RequestFreezeDto request, Guid userId);
    Task<MembershipFreezeDto> EndFreezeAsync(EndFreezeDto request, Guid userId);
    Task<List<MembershipFreezeDto>> GetFreezesAsync(Guid? clubId, Guid? memberId, bool activeOnly);

    /// <summary>Releases freezes whose end date has passed. Runs nightly.</summary>
    Task<int> ReleaseDueFreezesAsync();

    // ── Suspensions ──────────────────────────────────────────────────────────

    Task<MembershipSuspensionDto> SuspendAsync(SuspendMemberDto request, Guid userId);
    Task<MembershipSuspensionDto> LiftSuspensionAsync(Guid suspensionId, string? reason, Guid userId);

    /// <summary>Lifts suspensions whose blocking condition has cleared — the balance was paid, the waiver signed.</summary>
    Task<int> LiftResolvedSuspensionsAsync();

    // ── Cancellation ─────────────────────────────────────────────────────────

    Task<CancellationPreviewDto> PreviewCancellationAsync(Guid agreementId, DateTime? requestedEffectiveOn);
    Task<CancellationRequestDto> RequestCancellationAsync(RequestCancellationDto request, Guid userId);
    Task<SaveOfferDto> MakeSaveOfferAsync(MakeSaveOfferDto request, Guid userId);
    Task<CancellationRequestDto> RespondToOfferAsync(RespondToSaveOfferDto request, Guid userId);
    Task<AgreementDetailDto> ProcessCancellationAsync(Guid requestId, Guid userId);

    Task<List<CancellationRequestDto>> GetPendingCancellationsAsync(Guid? clubId);

    /// <summary>Ends agreements whose cancellation date has arrived. Runs nightly.</summary>
    Task<int> ProcessDueCancellationsAsync();
}

/// <summary>
/// The money engine: schedules, runs, invoices, collection, dunning and deferred revenue.
/// </summary>
public interface IBillingService
{
    // ── Schedule ─────────────────────────────────────────────────────────────

    Task<List<BillingScheduleDto>> GetScheduleAsync(Guid agreementId);

    /// <summary>Builds or rebuilds the forward schedule for an agreement.</summary>
    Task RebuildScheduleAsync(Guid agreementId, Guid userId);

    // ── Runs ─────────────────────────────────────────────────────────────────

    Task<BillingRunDto> StartRunAsync(StartBillingRunDto request, Guid userId);
    Task<BillingRunDto?> GetRunAsync(Guid runId);
    Task<PaginatedResponse<BillingRunLineDto>> GetRunLinesAsync(Guid runId, string? outcome, PaginationParams pagination);
    Task<PaginatedResponse<BillingRunDto>> ListRunsAsync(Guid? clubId, DateTime? from, DateTime? to, PaginationParams pagination);

    /// <summary>The nightly automatic run, if the tenant has it switched on.</summary>
    Task<BillingRunDto?> RunScheduledBillingAsync();

    // ── Invoices ─────────────────────────────────────────────────────────────

    Task<PaginatedResponse<InvoiceSummaryDto>> ListInvoicesAsync(
        Guid? clubId, Guid? memberId, InvoiceStatus? status, DateTime? from, DateTime? to,
        bool overdueOnly, PaginationParams pagination);

    Task<InvoiceDetailDto?> GetInvoiceAsync(Guid invoiceId);
    Task<InvoiceDetailDto> CreateAdHocInvoiceAsync(Guid memberId, Guid clubId, List<InvoiceLineDto> lines, Guid userId);
    Task<InvoiceDetailDto> CancelInvoiceAsync(Guid invoiceId, string reason, Guid userId);

    // ── Payments ─────────────────────────────────────────────────────────────

    Task<TakePaymentResultDto> TakePaymentAsync(TakePaymentDto request, Guid userId);
    Task<PaginatedResponse<PaymentDto>> ListPaymentsAsync(
        Guid? clubId, Guid? memberId, PaymentStatus? status, DateTime? from, DateTime? to,
        PaginationParams pagination);

    Task<List<PaymentMethodRefDto>> GetPaymentMethodsAsync(Guid memberId);
    Task<PaymentMethodRefDto> SavePaymentMethodAsync(SavePaymentMethodDto request, Guid userId);
    Task DeletePaymentMethodAsync(Guid id, Guid userId);

    // ── Credits, refunds, write-offs ─────────────────────────────────────────

    Task<CreditNoteDto> IssueCreditNoteAsync(IssueCreditNoteDto request, Guid userId);
    Task<RefundDto> IssueRefundAsync(IssueRefundDto request, Guid userId);
    Task<WriteOffDto> WriteOffAsync(Guid memberId, Guid? invoiceId, decimal amount, string reason, Guid userId);

    // ── Deferred revenue ─────────────────────────────────────────────────────

    Task<DeferredRevenueReportDto> GetDeferredRevenueAsync(Guid? clubId, DateTime from, DateTime to);

    /// <summary>Releases the deferred revenue earned by time passing. Runs nightly.</summary>
    Task<int> RecogniseDueRevenueAsync();
}

/// <summary>The collection ladder — failed payments chased automatically, and by hand when needed.</summary>
public interface IDunningService
{
    Task<PaginatedResponse<DunningCaseDto>> ListCasesAsync(
        Guid? clubId, DunningCaseStatus? status, Guid? assignedStaffId, PaginationParams pagination);

    Task<DunningCaseDto?> GetCaseAsync(Guid caseId);

    /// <summary>Opens a case when a collection fails. Idempotent per invoice.</summary>
    Task<DunningCaseDto> OpenCaseAsync(Guid invoiceId, PaymentFailureReason reason, Guid userId);

    Task<DunningCaseDto> ActionCaseAsync(DunningActionDto request, Guid userId);

    /// <summary>Walks every open case to its next due step. Runs nightly.</summary>
    Task<int> ProcessDueStepsAsync();

    Task<List<DunningPolicyDto>> GetPoliciesAsync(Guid? clubId);
    Task<DunningPolicyDto> SavePolicyAsync(Guid? id, DunningPolicyDto request, Guid userId);

    Task<ArrearsReportDto> GetArrearsAsync(Guid? clubId, DateTime? asAt);
}

/// <summary>
/// The door. The highest-traffic path in the product, and the one that must keep working when
/// nothing else does.
/// </summary>
public interface IAccessService
{
    /// <summary>Decides whether this credential opens this door, and what the person is told.</summary>
    Task<AccessDecisionDto> DecideAsync(AccessRequestDto request);

    /// <summary>Desk check-in, where staff have already identified the person.</summary>
    Task<AccessDecisionDto> ManualCheckInAsync(ManualCheckInDto request, Guid userId);

    Task<CheckInDto> CheckOutAsync(Guid checkInId, Guid userId);

    /// <summary>Closes anyone the system still thinks is inside. Runs overnight.</summary>
    Task<int> SweepOpenCheckInsAsync();

    Task<OccupancyDto> GetOccupancyAsync(Guid clubId);
    Task<OccupancyTrendDto> GetOccupancyTrendAsync(Guid clubId, DateTime from, DateTime to);

    Task<PaginatedResponse<AccessEventDto>> GetEventsAsync(
        Guid? clubId, Guid? doorId, Guid? memberId, AccessDecision? decision,
        DateTime? from, DateTime? to, PaginationParams pagination);

    Task<PaginatedResponse<CheckInDto>> GetCheckInsAsync(
        Guid? clubId, Guid? memberId, DateTime? from, DateTime? to, PaginationParams pagination);

    // ── Hardware ─────────────────────────────────────────────────────────────

    Task<List<DoorDto>> GetDoorsAsync(Guid? clubId);
    Task<DoorDto> SaveDoorAsync(Guid? id, SaveDoorDto request, Guid userId);
    Task DeleteDoorAsync(Guid id, Guid userId);

    Task<List<AccessControllerDto>> GetControllersAsync(Guid? clubId);
    Task<AccessControllerDto> SaveControllerAsync(Guid? id, SaveControllerDto request, Guid userId);

    /// <summary>The entitlement list a controller caches so it can decide offline.</summary>
    Task<AccessCacheDto> GetControllerCacheAsync(Guid controllerId);

    Task RecordHeartbeatAsync(Guid controllerId, int pendingEvents);

    /// <summary>Replays events a controller buffered while it could not reach the server.</summary>
    Task<int> ReplayOfflineEventsAsync(Guid controllerId, List<AccessRequestDto> events);

    Task<DoorDto> ReleaseDoorAsync(Guid doorId, string reason, Guid userId);

    Task<List<AccessRuleDto>> GetRulesAsync(Guid? clubId);
    Task<AccessRuleDto> SaveRuleAsync(Guid? id, AccessRuleDto request, Guid userId);

    // ── Guests & passes ──────────────────────────────────────────────────────

    Task<GuestVisitDto> RegisterGuestAsync(RegisterGuestDto request, Guid userId);
    Task<DayPassDto> IssueDayPassAsync(IssueDayPassDto request, Guid userId);
    Task<PaginatedResponse<DayPassDto>> ListDayPassesAsync(Guid? clubId, DateTime? from, DateTime? to, PaginationParams pagination);

    /// <summary>Everything the front-desk screen renders, in one call.</summary>
    Task<FrontDeskDto> GetFrontDeskAsync(Guid clubId, Guid? staffId);
}

/// <summary>
/// The timetable and the booking engine: capacity, entitlement, waitlist and the cancellation
/// policy that keeps the 06:00 class honest.
/// </summary>
public interface IScheduleService
{
    // ── Class types ──────────────────────────────────────────────────────────

    Task<List<ClassTypeDto>> GetClassTypesAsync(Guid? clubId, bool activeOnly);
    Task<ClassTypeDto> SaveClassTypeAsync(Guid? id, SaveClassTypeDto request, Guid userId);
    Task DeleteClassTypeAsync(Guid id, Guid userId);

    // ── Schedules ────────────────────────────────────────────────────────────

    Task<List<ClassScheduleDto>> GetSchedulesAsync(Guid clubId, string? seasonCode, bool publishedOnly);
    Task<ClassScheduleDto> SaveScheduleAsync(Guid? id, SaveClassScheduleDto request, Guid userId);
    Task DeleteScheduleAsync(Guid id, bool cancelFutureOccurrences, Guid userId);

    /// <summary>Everything wrong with a timetable, checked before members can see it.</summary>
    Task<List<ScheduleConflictDto>> CheckConflictsAsync(Guid clubId, string? seasonCode);

    Task<ClassScheduleDto> PublishScheduleAsync(Guid scheduleId, Guid userId);

    /// <summary>Materialises occurrences out to the schedule's horizon. Runs nightly and on publish.</summary>
    Task<int> GenerateOccurrencesAsync(Guid? clubId);

    // ── Occurrences ──────────────────────────────────────────────────────────

    Task<TimetableDto> GetTimetableAsync(
        Guid clubId, DateTime from, DateTime to,
        Guid? classTypeId, Guid? instructorStaffId, Guid? roomId, Guid? viewerMemberId);

    Task<ClassOccurrenceDetailDto?> GetOccurrenceAsync(Guid occurrenceId, Guid? viewerMemberId);
    Task<ClassOccurrenceDetailDto> UpdateOccurrenceAsync(UpdateOccurrenceDto request, Guid userId);
    Task<ClassOccurrenceDetailDto> CancelOccurrenceAsync(CancelOccurrenceDto request, Guid userId);

    // ── Bookings ─────────────────────────────────────────────────────────────

    Task<BookingEligibilityDto> CheckEligibilityAsync(Guid occurrenceId, Guid memberId);
    Task<ClassBookingDto> BookAsync(CreateBookingDto request, Guid userId);
    Task<CancelBookingPreviewDto> PreviewCancelAsync(Guid bookingId);
    Task<ClassBookingDto> CancelBookingAsync(CancelBookingDto request, Guid userId);
    Task<ClassBookingDto> CheckInToClassAsync(Guid bookingId, Guid userId);

    Task<List<ClassBookingDto>> GetMemberBookingsAsync(Guid memberId, DateTime? from, DateTime? to, bool upcomingOnly);
    Task<ClassOccurrenceDetailDto> MarkAttendanceAsync(MarkAttendanceDto request, Guid userId);

    /// <summary>
    /// Promotes waitlisted members into places that opened up, and releases spots nobody claimed
    /// shortly before the class starts. Runs on a short timer.
    /// </summary>
    Task<int> ProcessWaitlistsAsync();

    /// <summary>Closes finished classes and applies the no-show policy. Runs on a short timer.</summary>
    Task<int> ProcessFinishedClassesAsync();

    // ── Policies ─────────────────────────────────────────────────────────────

    Task<List<BookingPolicyDto>> GetBookingPoliciesAsync(Guid? clubId);
    Task<BookingPolicyDto> SaveBookingPolicyAsync(Guid? id, BookingPolicyDto request, Guid userId);
    Task<List<CancellationPolicyDto>> GetCancellationPoliciesAsync(Guid? clubId);
    Task<CancellationPolicyDto> SaveCancellationPolicyAsync(Guid? id, CancellationPolicyDto request, Guid userId);

    Task<List<LateCancelStrikeDto>> GetStrikesAsync(Guid memberId, bool activeOnly);
    Task<LateCancelStrikeDto> WaiveStrikeAsync(Guid strikeId, string reason, Guid userId);
}

/// <summary>Personal training, appointments, session packages and the credit ledger behind them.</summary>
public interface IAppointmentService
{
    Task<List<BookableStaffDto>> GetBookableStaffAsync(Guid? clubId, Guid? serviceId, bool activeOnly);
    Task<BookableStaffDto> SaveBookableStaffAsync(Guid? id, BookableStaffDto request, Guid userId);
    Task<List<StaffAvailabilityDto>> SaveAvailabilityAsync(Guid bookableStaffId, List<StaffAvailabilityDto> availability, Guid userId);
    Task<StaffTimeOffDto> AddTimeOffAsync(StaffTimeOffDto request, Guid userId);

    /// <summary>Open slots, either for one trainer or across every qualified one.</summary>
    Task<List<AvailabilitySlotDto>> FindAvailabilityAsync(AvailabilitySearchDto request);

    Task<PaginatedResponse<AppointmentSummaryDto>> ListAsync(
        Guid? clubId, Guid? staffId, Guid? memberId, AppointmentStatus? status,
        DateTime? from, DateTime? to, PaginationParams pagination);

    Task<AppointmentDetailDto?> GetAsync(Guid appointmentId);
    Task<List<AppointmentSummaryDto>> GetDiaryAsync(Guid clubId, DateTime forDate, Guid? staffId);

    Task<AppointmentDetailDto> CreateAsync(CreateAppointmentDto request, Guid userId);
    Task<AppointmentDetailDto> RescheduleAsync(Guid appointmentId, DateTime newStart, Guid? newStaffId, Guid userId);
    Task<AppointmentDetailDto> CancelAsync(Guid appointmentId, string? reason, bool waivePenalty, Guid userId);
    Task<AppointmentDetailDto> CheckInAsync(Guid appointmentId, Guid? memberId, Guid userId);

    /// <summary>The commercial event: consumes the credit and accrues the trainer's commission.</summary>
    Task<AppointmentDetailDto> SignOffAsync(SignOffSessionDto request, Guid userId);

    Task<AppointmentDetailDto> MarkNoShowAsync(Guid appointmentId, Guid? memberId, bool waivePenalty, Guid userId);

    // ── Packages & credits ───────────────────────────────────────────────────

    Task<SessionPackagePurchaseDto> SellPackageAsync(SellPackageDto request, Guid userId);
    Task<List<SessionPackagePurchaseDto>> GetPackagesAsync(Guid? clubId, Guid? memberId, bool activeOnly);
    Task<List<SessionCreditDto>> GetCreditsAsync(Guid memberId);
    Task<SessionCreditDto> AdjustCreditsAsync(AdjustCreditsDto request, Guid userId);

    /// <summary>Expires credits whose date has passed, releasing the deferred revenue. Runs nightly.</summary>
    Task<int> ExpireDueCreditsAsync();

    Task<CoachAssignmentDto> AssignCoachAsync(Guid memberId, Guid staffId, bool isPrimary, Guid userId);
    Task<List<CoachAssignmentDto>> GetCoachClientsAsync(Guid staffId, bool activeOnly);

    /// <summary>The trainer's own screen: today's diary, their clients, what they have earned.</summary>
    Task<TrainerDayDto> GetTrainerDayAsync(Guid staffId, DateTime forDate);
}

/// <summary>Programming, results, personal records, leaderboards and rank progression.</summary>
public interface ITrainingService
{
    Task<PaginatedResponse<ExerciseDto>> GetExercisesAsync(ExerciseCategory? category, string? search, PaginationParams pagination);
    Task<ExerciseDto> SaveExerciseAsync(Guid? id, ExerciseDto request, Guid userId);

    Task<PaginatedResponse<WorkoutDto>> GetWorkoutsAsync(Guid? clubId, bool? benchmarksOnly, bool? templatesOnly, string? search, PaginationParams pagination);
    Task<WorkoutDto?> GetWorkoutAsync(Guid workoutId);
    Task<WorkoutDto> SaveWorkoutAsync(Guid? id, WorkoutDto request, Guid userId);
    Task DeleteWorkoutAsync(Guid id, Guid userId);

    Task<List<ProgramTrackDto>> GetTracksAsync(Guid? clubId, bool activeOnly);
    Task<ProgramTrackDto> SaveTrackAsync(Guid? id, ProgramTrackDto request, Guid userId);
    Task<List<ProgramDayDto>> GetProgrammingAsync(Guid clubId, DateTime from, DateTime to, Guid? trackId);
    Task<ProgramDayDto> SaveProgramDayAsync(Guid? id, ProgramDayDto request, Guid userId);
    Task<ProgramDayDto> PublishProgramDayAsync(Guid id, Guid userId);

    /// <summary>Today's programming across every track — what the whiteboard screen renders.</summary>
    Task<WodBoardDto> GetWodBoardAsync(Guid clubId, DateTime forDate);

    Task<WorkoutResultDto> LogResultAsync(LogResultDto request, Guid userId);
    Task<PaginatedResponse<WorkoutResultDto>> GetResultsAsync(
        Guid? memberId, Guid? workoutId, Guid? clubId, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<List<PersonalRecordDto>> GetPersonalRecordsAsync(Guid memberId);

    Task<LeaderboardDto> GetLeaderboardAsync(
        Guid clubId, Guid? workoutId, Guid? classOccurrenceId, Guid? challengeId,
        string? division, DateTime? from, DateTime? to, Guid? viewerMemberId);

    Task<EffortSessionDto> RecordEffortAsync(EffortSessionDto request, Guid userId);
    Task<List<EffortSessionDto>> GetEffortSessionsAsync(Guid memberId, DateTime? from, DateTime? to);
    Task<AttendanceStreakDto?> GetStreakAsync(Guid memberId, string cadence);

    // ── Ranks ────────────────────────────────────────────────────────────────

    Task<List<RankLadderDto>> GetLaddersAsync(Guid? clubId);
    Task<RankLadderDto> SaveLadderAsync(Guid? id, RankLadderDto request, Guid userId);
    Task<List<MemberRankDto>> GetMemberRanksAsync(Guid memberId);
    Task<List<MemberRankDto>> GetGradingCandidatesAsync(Guid clubId, Guid ladderId);
    Task<MemberRankDto> AwardRankAsync(AwardRankDto request, Guid userId);
    Task<GradingEventDto> SaveGradingEventAsync(Guid? id, GradingEventDto request, Guid userId);

    Task<SkillClearanceDto> GrantClearanceAsync(SkillClearanceDto request, Guid userId);
    Task<List<SkillClearanceDto>> GetClearancesAsync(Guid memberId, bool activeOnly);
}

/// <summary>Assessments, progress, goals and the coaching layer around them.</summary>
public interface IAssessmentService
{
    Task<List<AssessmentTemplateDto>> GetTemplatesAsync(Guid? clubId, bool activeOnly);
    Task<AssessmentTemplateDto> SaveTemplateAsync(Guid? id, AssessmentTemplateDto request, Guid userId);

    Task<PaginatedResponse<AssessmentDto>> ListAsync(Guid? clubId, Guid? memberId, Guid? staffId, DateTime? from, DateTime? to, PaginationParams pagination);
    Task<AssessmentDto?> GetAsync(Guid assessmentId, Guid userId);
    Task<AssessmentDto> RecordAsync(RecordAssessmentDto request, Guid userId);

    /// <summary>One measure charted over time — the shape the progress screen actually wants.</summary>
    Task<List<ProgressSeriesDto>> GetProgressAsync(Guid memberId, List<string>? measureNames, Guid userId);

    Task<ProgressPhotoDto> AddPhotoAsync(ProgressPhotoDto request, Guid userId);
    Task<List<ProgressPhotoDto>> GetPhotosAsync(Guid memberId, Guid userId);

    Task<List<MemberGoalDto>> GetGoalsAsync(Guid memberId, bool activeOnly);
    Task<MemberGoalDto> SaveGoalAsync(Guid? id, MemberGoalDto request, Guid userId);

    Task<NutritionPlanDto> SaveNutritionPlanAsync(Guid? id, NutritionPlanDto request, Guid userId);
    Task<List<NutritionPlanDto>> GetNutritionPlansAsync(Guid memberId, bool activeOnly);

    Task<HabitTrackerDto> SaveHabitAsync(Guid? id, HabitTrackerDto request, Guid userId);
    Task<List<HabitTrackerDto>> GetHabitsAsync(Guid memberId, bool activeOnly);
    Task<HabitEntryDto> LogHabitAsync(Guid habitTrackerId, DateTime forDate, decimal? value, bool completed, string? note, Guid userId);

    Task<List<CoachCheckInDto>> GetCheckInsAsync(Guid? staffId, Guid? memberId, bool dueOnly);
    Task<CoachCheckInDto> SaveCheckInAsync(Guid? id, CoachCheckInDto request, Guid userId);
}

/// <summary>Leads, tours, trials, referrals and the pipeline the sales floor works from.</summary>
public interface ILeadService
{
    Task<LeadBoardDto> GetBoardAsync(Guid? clubId, Guid? assignedStaffId);

    Task<PaginatedResponse<LeadSummaryDto>> ListAsync(
        Guid? clubId, LeadStatus? status, Guid? sourceId, Guid? assignedStaffId,
        bool? slaBreached, DateTime? from, DateTime? to, string? search, PaginationParams pagination);

    Task<LeadDetailDto?> GetAsync(Guid leadId);
    Task<LeadDetailDto> CreateAsync(SaveLeadDto request, Guid userId);
    Task<LeadDetailDto> UpdateAsync(Guid leadId, SaveLeadDto request, Guid userId);

    Task<LeadDetailDto> AssignAsync(Guid leadId, Guid staffId, Guid userId);
    Task<LeadActivityDto> LogActivityAsync(LogLeadActivityDto request, Guid userId);
    Task<LeadDetailDto> MoveStageAsync(Guid leadId, LeadStatus status, Guid userId);
    Task<LeadDetailDto> CloseAsync(CloseLeadDto request, Guid userId);

    /// <summary>Converts a won lead into a member and an agreement, keeping the attribution.</summary>
    Task<JoinResultDto> ConvertAsync(Guid leadId, JoinMemberDto request, Guid userId);

    Task<TourDto> BookTourAsync(BookTourDto request, Guid userId);
    Task<TourDto> UpdateTourAsync(Guid tourId, TourDto request, Guid userId);
    Task<List<TourDto>> GetToursAsync(Guid clubId, DateTime from, DateTime to, Guid? staffId);

    Task<TrialPassDto> IssueTrialAsync(IssueTrialDto request, Guid userId);
    Task<List<TrialPassDto>> GetTrialsAsync(Guid? clubId, bool activeOnly);

    Task<ReferralDto> CreateReferralAsync(CreateReferralDto request, Guid userId);
    Task<List<ReferralDto>> GetReferralsAsync(Guid? clubId, Guid? memberId, bool? converted);

    Task<List<LeadSourceDto>> GetSourcesAsync(Guid? clubId, bool activeOnly);
    Task<LeadSourceDto> SaveSourceAsync(Guid? id, LeadSourceDto request, Guid userId);
    Task<List<LossReasonDto>> GetLossReasonsAsync();

    Task<List<SalesTargetDto>> GetTargetsAsync(Guid? clubId, DateTime? periodStart);
    Task<SalesTargetDto> SaveTargetAsync(Guid? id, SalesTargetDto request, Guid userId);

    /// <summary>Flags leads that have blown their first-response SLA. Runs on a short timer.</summary>
    Task<int> FlagSlaBreachesAsync();
}

/// <summary>
/// Retention: scoring who is about to leave, the board that gets them called, and the automation
/// that reaches the ones nobody has time to call.
/// </summary>
public interface IRetentionService
{
    Task<RetentionBoardDto> GetBoardAsync(Guid? clubId, ChurnRiskBand? band, Guid? ownerStaffId);

    Task<ChurnScoreDto?> GetScoreAsync(Guid memberId);

    /// <summary>Recomputes every member's risk band and its reasons. Runs nightly.</summary>
    Task<int> ScoreAllAsync(Guid? clubId);

    Task<List<RetentionTaskDto>> GetTasksAsync(Guid? clubId, Guid? staffId, bool openOnly);
    Task<RetentionTaskDto> CreateTaskAsync(RetentionTaskDto request, Guid userId);
    Task<RetentionTaskDto> CompleteTaskAsync(CompleteTaskDto request, Guid userId);

    // ── Journeys & campaigns ─────────────────────────────────────────────────

    Task<List<EngagementJourneyDto>> GetJourneysAsync(Guid? clubId);
    Task<EngagementJourneyDto> SaveJourneyAsync(Guid? id, EngagementJourneyDto request, Guid userId);
    Task<EngagementJourneyDto> SetJourneyActiveAsync(Guid id, bool active, Guid userId);
    Task<List<JourneyEnrolmentDto>> GetEnrolmentsAsync(Guid journeyId, bool activeOnly);

    /// <summary>Enrols members whose trigger has fired, and advances everyone mid-journey. Runs hourly.</summary>
    Task<int> ProcessJourneysAsync();

    Task<PaginatedResponse<CampaignDto>> ListCampaignsAsync(Guid? clubId, PaginationParams pagination);
    Task<CampaignDto> SaveCampaignAsync(Guid? id, CampaignDto request, Guid userId);
    Task<CampaignDto> SendCampaignAsync(SendCampaignDto request, Guid userId);

    Task<List<MessageTemplateDto>> GetTemplatesAsync(Guid? clubId, MessageChannel? channel);
    Task<MessageTemplateDto> SaveTemplateAsync(Guid? id, MessageTemplateDto request, Guid userId);

    Task<List<SegmentDto>> GetSegmentsAsync(Guid? clubId);
    Task<SegmentDto> SaveSegmentAsync(Guid? id, SaveSegmentDto request, Guid userId);
    Task<PaginatedResponse<MemberSummaryDto>> PreviewSegmentAsync(Guid segmentId, PaginationParams pagination);

    Task<PaginatedResponse<MessageLogDto>> GetMessageLogAsync(
        Guid? clubId, Guid? memberId, MessageChannel? channel, MessageStatus? status,
        DateTime? from, DateTime? to, PaginationParams pagination);

    // ── Loyalty & challenges ─────────────────────────────────────────────────

    Task<LoyaltyAccountDto?> GetLoyaltyAsync(Guid memberId);
    Task<LoyaltyTransactionDto> AwardPointsAsync(AwardPointsDto request, Guid userId);
    Task<LoyaltyTransactionDto> RedeemPointsAsync(RedeemPointsDto request, Guid userId);
    Task<List<LoyaltyTierDto>> GetTiersAsync(Guid? clubId);
    Task<LoyaltyTierDto> SaveTierAsync(Guid? id, LoyaltyTierDto request, Guid userId);

    Task<List<ChallengeDto>> GetChallengesAsync(Guid? clubId, bool activeOnly, Guid? viewerMemberId);
    Task<ChallengeDto> SaveChallengeAsync(Guid? id, ChallengeDto request, Guid userId);
    Task<ChallengeParticipantDto> JoinChallengeAsync(Guid challengeId, Guid memberId, string? teamName, Guid userId);

    /// <summary>Recomputes challenge standings and awards badges that have been earned. Runs nightly.</summary>
    Task<int> ProcessChallengesAndBadgesAsync();

    Task<List<BadgeDto>> GetBadgesAsync(Guid? clubId);
    Task<List<MemberBadgeDto>> GetMemberBadgesAsync(Guid memberId);

    // ── Feedback ─────────────────────────────────────────────────────────────

    Task<NpsResponseDto> RecordNpsAsync(NpsResponseDto request, Guid userId);
    Task<NpsSummaryDto> GetNpsSummaryAsync(Guid? clubId, DateTime from, DateTime to);
    Task<PaginatedResponse<NpsResponseDto>> GetNpsResponsesAsync(Guid? clubId, string? band, bool needsFollowUpOnly, PaginationParams pagination);
    Task<NpsResponseDto> FollowUpNpsAsync(Guid responseId, string note, Guid userId);

    Task<FeedbackDto> RecordFeedbackAsync(FeedbackDto request, Guid userId);
    Task<PaginatedResponse<FeedbackDto>> GetFeedbackAsync(Guid? clubId, bool openOnly, PaginationParams pagination);

    Task<List<AnnouncementDto>> GetAnnouncementsAsync(Guid? clubId, bool liveOnly);
    Task<AnnouncementDto> SaveAnnouncementAsync(Guid? id, AnnouncementDto request, Guid userId);
}

/// <summary>Staff, the rota, the time clock and the commission engine.</summary>
public interface IStaffService
{
    Task<PaginatedResponse<StaffSummaryDto>> ListAsync(Guid? clubId, StaffRoleKind? role, bool activeOnly, string? search, PaginationParams pagination);
    Task<StaffDetailDto?> GetAsync(Guid staffId);
    Task<StaffDetailDto> SaveAsync(Guid? id, SaveStaffDto request, Guid userId);
    Task DeactivateAsync(Guid staffId, DateTime leftOn, Guid userId);

    Task<StaffPinResultDto> VerifyPinAsync(StaffPinLoginDto request);
    Task<bool> VerifyOverrideAsync(ManagerOverrideDto request, Guid userId);

    Task<List<StaffRoleDto>> GetRolesAsync(Guid? clubId);
    Task<StaffRoleDto> SaveRoleAsync(Guid? id, StaffRoleDto request, Guid userId);

    Task<List<StaffCertificationDto>> GetCertificationsAsync(Guid? clubId, Guid? staffId, bool expiringOnly);
    Task<StaffCertificationDto> SaveCertificationAsync(Guid? id, StaffCertificationDto request, Guid userId);

    // ── Rota ─────────────────────────────────────────────────────────────────

    Task<RotaDto> GetRotaAsync(Guid clubId, DateTime from, DateTime to);
    Task<ShiftDto> SaveShiftAsync(Guid? id, SaveShiftDto request, Guid userId);
    Task DeleteShiftAsync(Guid id, Guid userId);
    Task<List<ShiftDto>> PublishRotaAsync(Guid clubId, DateTime from, DateTime to, Guid userId);

    Task<ShiftSwapRequestDto> RequestSwapAsync(Guid assignmentId, Guid? offerToStaffId, string? reason, Guid userId);
    Task<ShiftSwapRequestDto> RespondToSwapAsync(Guid swapId, bool accept, Guid staffId, Guid userId);
    Task<List<ShiftSwapRequestDto>> GetSwapRequestsAsync(Guid clubId, bool openOnly);

    // ── Time clock ───────────────────────────────────────────────────────────

    Task<TimeClockEntryDto> ClockInAsync(ClockDto request, Guid userId);
    Task<TimeClockEntryDto> ClockOutAsync(ClockDto request, Guid userId);
    Task<TimesheetDto> GetTimesheetAsync(Guid staffId, DateTime from, DateTime to);
    Task<TimeClockEntryDto> AdjustEntryAsync(Guid entryId, DateTime? inAt, DateTime? outAt, int? breakMinutes, string note, Guid userId);
    Task<int> ApproveTimesheetAsync(Guid staffId, DateTime from, DateTime to, Guid userId);

    // ── Commission ───────────────────────────────────────────────────────────

    Task<List<CommissionRuleDto>> GetCommissionRulesAsync(Guid? clubId, Guid? staffId);
    Task<CommissionRuleDto> SaveCommissionRuleAsync(Guid? id, CommissionRuleDto request, Guid userId);

    /// <summary>Accrues a commission line the moment the thing that earns it happens.</summary>
    Task<List<CommissionAccrualDto>> AccrueAsync(
        Guid staffId, CommissionBasis basis, decimal baseValue, decimal quantity,
        Guid? sourceId, string? sourceType, Guid? memberId, string? narrative, Guid userId);

    Task<List<CommissionStatementDto>> GenerateStatementsAsync(GenerateCommissionDto request, Guid userId);
    Task<CommissionStatementDto?> GetStatementAsync(Guid statementId);
    Task<PaginatedResponse<CommissionStatementDto>> ListStatementsAsync(
        Guid? clubId, Guid? staffId, CommissionStatementStatus? status, PaginationParams pagination);
    Task<CommissionStatementDto> ApproveStatementAsync(ApproveStatementDto request, Guid userId);
    Task<CommissionStatementDto> ExportStatementAsync(Guid statementId, Guid userId);
}

/// <summary>Lockers, resources, courts, equipment and the maintenance behind them.</summary>
public interface IFacilityService
{
    Task<List<LockerBankDto>> GetLockerBanksAsync(Guid clubId);
    Task<LockerBankDto> SaveLockerBankAsync(Guid? id, LockerBankDto request, Guid userId);
    Task<LockerAssignmentDto> AssignLockerAsync(AssignLockerDto request, Guid userId);
    Task<LockerAssignmentDto> ReleaseLockerAsync(ReleaseLockerDto request, Guid userId);
    Task<PaginatedResponse<LockerAssignmentDto>> GetLockerAssignmentsAsync(Guid? clubId, Guid? memberId, bool activeOnly, PaginationParams pagination);

    /// <summary>Sweeps day lockers and chases expired rentals. Runs overnight.</summary>
    Task<int> SweepLockersAsync();

    Task<List<BookableResourceDto>> GetResourcesAsync(Guid clubId, ResourceKind? kind, bool activeOnly);
    Task<BookableResourceDto> SaveResourceAsync(Guid? id, BookableResourceDto request, Guid userId);

    /// <summary>The day-by-resource grid a leisure-centre desk books from.</summary>
    Task<ResourceGridDto> GetGridAsync(Guid clubId, DateTime forDate, ResourceKind? kind);

    Task<ResourceBookingDto> BookResourceAsync(CreateResourceBookingDto request, Guid userId);
    Task<ResourceBookingDto> CancelResourceBookingAsync(Guid bookingId, string? reason, bool waivePenalty, Guid userId);
    Task<ResourceBookingDto> CheckInResourceBookingAsync(Guid bookingId, Guid userId);
    Task<PaginatedResponse<ResourceBookingDto>> ListResourceBookingsAsync(
        Guid? clubId, Guid? memberId, Guid? resourceId, DateTime? from, DateTime? to, PaginationParams pagination);

    // ── Equipment ────────────────────────────────────────────────────────────

    Task<PaginatedResponse<EquipmentAssetDto>> GetEquipmentAsync(
        Guid? clubId, AssetStatus? status, string? category, string? search, PaginationParams pagination);

    Task<EquipmentAssetDto?> GetAssetAsync(Guid assetId);

    /// <summary>Resolves the sticker on the machine, which is how a fault gets reported at all.</summary>
    Task<EquipmentAssetDto?> GetAssetByQrAsync(string qrCode);

    Task<EquipmentAssetDto> SaveAssetAsync(Guid? id, SaveEquipmentDto request, Guid userId);
    Task<EquipmentAssetDto> SetAssetStatusAsync(Guid assetId, AssetStatus status, string? note, Guid userId);
    Task<EquipmentUsageLogDto> RecordUsageAsync(Guid assetId, decimal cumulativeHours, string? source, Guid userId);

    Task<List<MaintenanceScheduleDto>> GetMaintenanceSchedulesAsync(Guid? clubId, bool dueOnly);
    Task<MaintenanceScheduleDto> SaveMaintenanceScheduleAsync(Guid? id, MaintenanceScheduleDto request, Guid userId);

    /// <summary>Raises work orders for maintenance that has fallen due. Runs nightly.</summary>
    Task<int> GenerateDueWorkOrdersAsync();

    Task<PaginatedResponse<WorkOrderDto>> GetWorkOrdersAsync(
        Guid? clubId, WorkOrderStatus? status, WorkOrderPriority? priority, Guid? assignedStaffId, PaginationParams pagination);

    Task<WorkOrderDto> SaveWorkOrderAsync(Guid? id, SaveWorkOrderDto request, Guid userId);
    Task<WorkOrderDto> CompleteWorkOrderAsync(CompleteWorkOrderDto request, Guid userId);

    Task<FaultReportDto> ReportFaultAsync(ReportFaultDto request, Guid userId);
    Task<List<FaultReportDto>> GetFaultsAsync(Guid? clubId, bool openOnly);
}

/// <summary>
/// Waivers, health screening, clearances, incidents and the checks a club has to be able to prove
/// it did.
/// </summary>
public interface IComplianceService
{
    Task<List<WaiverTemplateDto>> GetWaiverTemplatesAsync(Guid? clubId, bool publishedOnly);
    Task<WaiverTemplateDto> SaveWaiverTemplateAsync(Guid? id, WaiverTemplateDto request, Guid userId);
    Task<WaiverTemplateDto> PublishWaiverAsync(Guid id, Guid userId);

    Task<WaiverSignatureDto> SignWaiverAsync(SignWaiverDto request, Guid userId);
    Task<List<WaiverSignatureDto>> GetSignaturesAsync(Guid? memberId, Guid? clubId, SignatureStatus? status);

    /// <summary>Members with no current waiver, which is who cannot be let in tomorrow.</summary>
    Task<PaginatedResponse<MemberSummaryDto>> GetOutstandingWaiversAsync(Guid? clubId, PaginationParams pagination);

    Task<HealthScreeningFormDto> GetScreeningFormAsync(Guid? clubId);
    Task<HealthScreeningDto> SubmitScreeningAsync(HealthScreeningDto request, Guid userId);
    Task<HealthScreeningDto?> GetScreeningAsync(Guid memberId, Guid userId);

    Task<MedicalClearanceDto> SubmitClearanceAsync(SubmitClearanceDto request, Guid userId);
    Task<MedicalClearanceDto> ReviewClearanceAsync(ReviewClearanceDto request, Guid userId);
    Task<List<MedicalClearanceDto>> GetClearancesAsync(Guid? clubId, ClearanceStatus? status);

    // ── Incidents & complaints ───────────────────────────────────────────────

    Task<PaginatedResponse<IncidentDto>> GetIncidentsAsync(
        Guid? clubId, IncidentKind? kind, IncidentStatus? status, IncidentSeverity? severity,
        DateTime? from, DateTime? to, PaginationParams pagination);

    Task<IncidentDto?> GetIncidentAsync(Guid incidentId);
    Task<IncidentDto> SaveIncidentAsync(Guid? id, SaveIncidentDto request, Guid userId);
    Task<IncidentActionDto> AddIncidentActionAsync(Guid incidentId, IncidentActionDto request, Guid userId);
    Task<IncidentDto> CloseIncidentAsync(Guid incidentId, string rootCause, string preventiveAction, Guid userId);

    Task<PaginatedResponse<ComplaintDto>> GetComplaintsAsync(Guid? clubId, ComplaintStatus? status, PaginationParams pagination);
    Task<ComplaintDto> SaveComplaintAsync(Guid? id, SaveComplaintDto request, Guid userId);
    Task<ComplaintDto> ResolveComplaintAsync(ResolveComplaintDto request, Guid userId);

    Task<PaginatedResponse<LostPropertyItemDto>> GetLostPropertyAsync(Guid? clubId, LostPropertyStatus? status, PaginationParams pagination);
    Task<LostPropertyItemDto> SaveLostPropertyAsync(Guid? id, SaveLostPropertyDto request, Guid userId);
    Task<LostPropertyItemDto> ClaimLostPropertyAsync(ClaimLostPropertyDto request, Guid userId);

    // ── Checks ───────────────────────────────────────────────────────────────

    Task<List<FacilityCheckDto>> GetChecksAsync(Guid clubId, bool dueTodayOnly);
    Task<FacilityCheckDto> SaveCheckAsync(Guid? id, FacilityCheckDto request, Guid userId);
    Task<FacilityCheckDto> SubmitCheckAsync(SubmitFacilityCheckDto request, Guid userId);

    Task<ShiftHandoverDto> SaveHandoverAsync(ShiftHandoverDto request, Guid userId);
    Task<List<ShiftHandoverDto>> GetHandoversAsync(Guid clubId, int limit);

    Task<PaginatedResponse<AuditEntryDto>> GetAuditAsync(
        Guid? clubId, Guid? memberId, Guid? actorUserId, string? entityType,
        bool sensitiveOnly, DateTime? from, DateTime? to, PaginationParams pagination);

    /// <summary>Writes an audit row. Called from anywhere that touches money, entitlements or health data.</summary>
    Task LogAsync(string action, string entityType, Guid? entityId, Guid? memberId, string? summary, bool isSensitive, Guid userId);
}

/// <summary>The pro shop, corporate accounts, third-party payers and the cash drawer.</summary>
public interface ICommerceService
{
    Task<FitnessSaleDto> CreateSaleAsync(CreateSaleDto request, Guid userId);
    Task<FitnessSaleDto> ReturnSaleAsync(Guid saleId, string reason, List<Guid>? lineIds, Guid userId);
    Task<PaginatedResponse<FitnessSaleDto>> ListSalesAsync(
        Guid? clubId, Guid? memberId, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<List<RetailProductDto>> GetProductsAsync(Guid clubId, string? search);

    Task<List<HouseAccountChargeDto>> GetHouseAccountAsync(Guid memberId, bool unsettledOnly);

    // ── Cash ─────────────────────────────────────────────────────────────────

    Task<CashSessionDto> OpenSessionAsync(OpenCashSessionDto request, Guid userId);
    Task<CashSessionDto?> GetOpenSessionAsync(Guid clubId, Guid? staffId);
    Task<CashSessionDto?> GetSessionAsync(Guid sessionId);
    Task<CashSessionDto> RecordMovementAsync(CashMovementRequestDto request, Guid userId);
    Task<CashSessionDto> CloseSessionAsync(CloseCashSessionDto request, Guid userId);
    Task<PaginatedResponse<CashSessionDto>> ListSessionsAsync(Guid? clubId, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<DayEndReadDto> GetDayEndReadAsync(Guid clubId, DateTime forDate, bool isZRead);

    Task<GiftCardDto> IssueGiftCardAsync(IssueGiftCardDto request, Guid userId);
    Task<GiftCardDto?> GetGiftCardAsync(string cardNumber);
    Task<GiftCardDto> RedeemGiftCardAsync(string cardNumber, decimal amount, Guid? saleId, Guid userId);

    // ── Corporate ────────────────────────────────────────────────────────────

    Task<PaginatedResponse<CorporateAccountDto>> GetCorporateAccountsAsync(Guid? clubId, bool activeOnly, PaginationParams pagination);
    Task<CorporateAccountDto?> GetCorporateAccountAsync(Guid id);
    Task<CorporateAccountDto> SaveCorporateAccountAsync(Guid? id, CorporateAccountDto request, Guid userId);
    Task<List<CorporateMemberDto>> GetCorporateMembersAsync(Guid accountId, bool activeOnly);
    Task<CorporateMemberDto> AddCorporateMemberAsync(Guid accountId, Guid memberId, string? employeeReference, Guid userId);

    /// <summary>Checks an email domain, employee id or code against the scheme's rules.</summary>
    Task<bool> CheckEligibilityAsync(Guid accountId, string? email, string? employeeReference, string? code);

    Task<CorporateInvoiceDto> GenerateCorporateInvoiceAsync(Guid accountId, DateTime periodStart, DateTime periodEnd, Guid userId);
    Task<PaginatedResponse<CorporateInvoiceDto>> GetCorporateInvoicesAsync(Guid? accountId, InvoiceStatus? status, PaginationParams pagination);

    Task<List<ThirdPartyPayerDto>> GetPayersAsync(Guid? clubId, bool activeOnly);
    Task<ThirdPartyPayerDto> SavePayerAsync(Guid? id, ThirdPartyPayerDto request, Guid userId);
    Task<PayerAuthorisationDto> SaveAuthorisationAsync(Guid? id, PayerAuthorisationDto request, Guid userId);
    Task<List<PayerAuthorisationDto>> GetAuthorisationsAsync(Guid? payerId, Guid? memberId, bool activeOnly);

    Task<VendingRevenueEntryDto> RecordVendingRevenueAsync(VendingRevenueEntryDto request, Guid userId);
}

/// <summary>Every report the app ships, behind one filter shape so the toolbars all match.</summary>
public interface IFitnessReportService
{
    Task<FitnessDashboardDto> GetDashboardAsync(Guid? clubId);

    Task<MembershipReportDto> GetMembershipReportAsync(ReportFilterDto filter);
    Task<CohortRetentionDto> GetCohortRetentionAsync(ReportFilterDto filter);
    Task<RevenueReportDto> GetRevenueReportAsync(ReportFilterDto filter);
    Task<MrrMovementDto> GetMrrMovementAsync(ReportFilterDto filter);
    Task<AttendanceReportDto> GetAttendanceReportAsync(ReportFilterDto filter);
    Task<ClassPerformanceReportDto> GetClassPerformanceAsync(ReportFilterDto filter);
    Task<SalesReportDto> GetSalesReportAsync(ReportFilterDto filter);
    Task<StaffPerformanceReportDto> GetStaffPerformanceAsync(ReportFilterDto filter);
    Task<OperationsReportDto> GetOperationsReportAsync(ReportFilterDto filter);

    Task<List<ReportSubscriptionDto>> GetSubscriptionsAsync(Guid? clubId);
    Task<ReportSubscriptionDto> SaveSubscriptionAsync(Guid? id, ReportSubscriptionDto request, Guid userId);
}

/// <summary>Clubs, areas, rooms and the tenant-wide settings the app runs on.</summary>
public interface IClubService
{
    Task<List<ClubDto>> GetClubsAsync(bool activeOnly);
    Task<ClubDto?> GetClubAsync(Guid clubId);
    Task<ClubDto> SaveClubAsync(Guid? id, SaveClubDto request, Guid userId);
    Task DeleteClubAsync(Guid clubId, Guid userId);

    Task<List<ClubScheduleDto>> GetSchedulesAsync(Guid clubId);
    Task<List<ClubScheduleDto>> SaveSchedulesAsync(Guid clubId, List<ClubScheduleDto> schedules, Guid userId);

    Task<List<ClubClosureDto>> GetClosuresAsync(Guid clubId, bool upcomingOnly);
    Task<ClubClosureDto> SaveClosureAsync(Guid? id, ClubClosureDto request, Guid userId);

    Task<List<ClubAreaDto>> GetAreasAsync(Guid clubId);
    Task<ClubAreaDto> SaveAreaAsync(Guid? id, ClubAreaDto request, Guid userId);

    Task<List<RoomDto>> GetRoomsAsync(Guid clubId);
    Task<RoomDto> SaveRoomLayoutAsync(SaveRoomLayoutDto request, Guid userId);

    Task<FitnessSettingsDto> GetSettingsAsync();
    Task<FitnessSettingsDto> SaveSettingsAsync(FitnessSettingsDto request, Guid userId);

    /// <summary>Fills in whatever this company is missing. Safe to call repeatedly.</summary>
    Task EnsureProvisionedAsync(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData);
}
