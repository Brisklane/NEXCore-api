using Fitness.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;

namespace Fitness.Infrastructure.Persistence;

/// <summary>
/// Fitness module DbContext. Default schema: <c>fitness</c>.
///
/// The model is organised the way a club is: the site and its rooms, the people who belong to it,
/// what they bought, the money that collects itself every month, the door they come through, the
/// timetable they book, the training they do, and the back office that keeps all of it honest.
///
/// Delete behaviour is deliberate throughout. Composition (an invoice and its lines, a workout and
/// its sections, a check and its items) cascades; every reference that crosses an aggregate
/// boundary is <see cref="DeleteBehavior.NoAction"/> so removing a class type can never take a
/// year of attendance history with it.
///
/// Indexes are chosen for the three reads that actually matter at scale: the door lookup by
/// credential, the member list by club and status, and the timetable by club and date range.
/// </summary>
public class FitnessDbContext : DbContext
{
    private const string DefaultSchema = "fitness";

    public FitnessDbContext(DbContextOptions<FitnessDbContext> options) : base(options) { }

    // ── Club & facility ──────────────────────────────────────────────────────
    public DbSet<FitnessClub> Clubs { get; set; } = null!;
    public DbSet<ClubSchedule> ClubSchedules { get; set; } = null!;
    public DbSet<ClubClosure> ClubClosures { get; set; } = null!;
    public DbSet<ClubArea> Areas { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<RoomSpot> RoomSpots { get; set; } = null!;
    public DbSet<FitnessSettings> Settings { get; set; } = null!;

    // ── Member ───────────────────────────────────────────────────────────────
    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<Household> Households { get; set; } = null!;
    public DbSet<HouseholdMember> HouseholdMembers { get; set; } = null!;
    public DbSet<EmergencyContact> EmergencyContacts { get; set; } = null!;
    public DbSet<MedicalFlag> MedicalFlags { get; set; } = null!;
    public DbSet<MemberNote> MemberNotes { get; set; } = null!;
    public DbSet<MemberTag> MemberTags { get; set; } = null!;
    public DbSet<MemberAlert> MemberAlerts { get; set; } = null!;
    public DbSet<MemberStatusHistory> MemberStatusHistory { get; set; } = null!;
    public DbSet<MemberDocument> MemberDocuments { get; set; } = null!;
    public DbSet<MemberCredential> Credentials { get; set; } = null!;
    public DbSet<MemberConsent> Consents { get; set; } = null!;
    public DbSet<MemberPreference> Preferences { get; set; } = null!;

    // ── Catalogue ────────────────────────────────────────────────────────────
    public DbSet<MembershipPlan> Plans { get; set; } = null!;
    public DbSet<PlanPrice> PlanPrices { get; set; } = null!;
    public DbSet<PlanEntitlement> PlanEntitlements { get; set; } = null!;
    public DbSet<AccessTimeBand> AccessTimeBands { get; set; } = null!;
    public DbSet<PromotionRule> Promotions { get; set; } = null!;
    public DbSet<PromoCode> PromoCodes { get; set; } = null!;
    public DbSet<PlanChangePath> PlanChangePaths { get; set; } = null!;
    public DbSet<AppointmentService> Services { get; set; } = null!;

    // ── Agreement ────────────────────────────────────────────────────────────
    public DbSet<Agreement> Agreements { get; set; } = null!;
    public DbSet<AgreementTemplate> AgreementTemplates { get; set; } = null!;
    public DbSet<AgreementAmendment> Amendments { get; set; } = null!;
    public DbSet<AgreementSignature> AgreementSignatures { get; set; } = null!;
    public DbSet<MembershipFreeze> Freezes { get; set; } = null!;
    public DbSet<MembershipSuspension> Suspensions { get; set; } = null!;
    public DbSet<CancellationRequest> CancellationRequests { get; set; } = null!;
    public DbSet<SaveOffer> SaveOffers { get; set; } = null!;

    // ── Money ────────────────────────────────────────────────────────────────
    public DbSet<BillingSchedule> BillingSchedules { get; set; } = null!;
    public DbSet<BillingRun> BillingRuns { get; set; } = null!;
    public DbSet<BillingRunLine> BillingRunLines { get; set; } = null!;
    public DbSet<FitnessInvoice> Invoices { get; set; } = null!;
    public DbSet<FitnessInvoiceLine> InvoiceLines { get; set; } = null!;
    public DbSet<FitnessPayment> Payments { get; set; } = null!;
    public DbSet<PaymentMethodRef> PaymentMethods { get; set; } = null!;
    public DbSet<PaymentMandate> Mandates { get; set; } = null!;
    public DbSet<DunningPolicy> DunningPolicies { get; set; } = null!;
    public DbSet<DunningStep> DunningSteps { get; set; } = null!;
    public DbSet<DunningCase> DunningCases { get; set; } = null!;
    public DbSet<DunningEvent> DunningEvents { get; set; } = null!;
    public DbSet<CreditNote> CreditNotes { get; set; } = null!;
    public DbSet<Refund> Refunds { get; set; } = null!;
    public DbSet<WriteOff> WriteOffs { get; set; } = null!;
    public DbSet<MemberLedgerEntry> Ledger { get; set; } = null!;
    public DbSet<DeferredRevenueSchedule> DeferredRevenue { get; set; } = null!;
    public DbSet<DeferredRevenueEntry> DeferredRevenueEntries { get; set; } = null!;
    public DbSet<MemberCreditBalance> CreditBalances { get; set; } = null!;
    public DbSet<GiftCard> GiftCards { get; set; } = null!;
    public DbSet<GiftCardTransaction> GiftCardTransactions { get; set; } = null!;

    // ── Access ───────────────────────────────────────────────────────────────
    public DbSet<Door> Doors { get; set; } = null!;
    public DbSet<AccessController> Controllers { get; set; } = null!;
    public DbSet<AccessRule> AccessRules { get; set; } = null!;
    public DbSet<AccessRuleWindow> AccessRuleWindows { get; set; } = null!;
    public DbSet<CheckIn> CheckIns { get; set; } = null!;
    public DbSet<AccessEvent> AccessEvents { get; set; } = null!;
    public DbSet<OccupancySnapshot> OccupancySnapshots { get; set; } = null!;
    public DbSet<GuestVisit> GuestVisits { get; set; } = null!;
    public DbSet<DayPass> DayPasses { get; set; } = null!;
    public DbSet<VisitAllowanceUsage> AllowanceUsage { get; set; } = null!;

    // ── Classes ──────────────────────────────────────────────────────────────
    public DbSet<ClassType> ClassTypes { get; set; } = null!;
    public DbSet<ClassSchedule> ClassSchedules { get; set; } = null!;
    public DbSet<ClassOccurrence> ClassOccurrences { get; set; } = null!;
    public DbSet<ClassBooking> ClassBookings { get; set; } = null!;
    public DbSet<BookingPolicy> BookingPolicies { get; set; } = null!;
    public DbSet<CancellationPolicy> CancellationPolicies { get; set; } = null!;
    public DbSet<LateCancelStrike> Strikes { get; set; } = null!;
    public DbSet<CourseEnrolment> CourseEnrolments { get; set; } = null!;
    public DbSet<MarketplaceChannel> MarketplaceChannels { get; set; } = null!;
    public DbSet<MarketplaceBooking> MarketplaceBookings { get; set; } = null!;

    // ── Appointments ─────────────────────────────────────────────────────────
    public DbSet<BookableStaff> BookableStaff { get; set; } = null!;
    public DbSet<StaffAvailability> Availability { get; set; } = null!;
    public DbSet<StaffTimeOff> TimeOff { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<AppointmentParticipant> AppointmentParticipants { get; set; } = null!;
    public DbSet<AppointmentSeries> AppointmentSeries { get; set; } = null!;
    public DbSet<SessionPackagePurchase> PackagePurchases { get; set; } = null!;
    public DbSet<SessionCredit> SessionCredits { get; set; } = null!;
    public DbSet<SessionCreditMovement> CreditMovements { get; set; } = null!;
    public DbSet<SessionSignOff> SignOffs { get; set; } = null!;
    public DbSet<CoachAssignment> CoachAssignments { get; set; } = null!;

    // ── Training ─────────────────────────────────────────────────────────────
    public DbSet<Exercise> Exercises { get; set; } = null!;
    public DbSet<Workout> Workouts { get; set; } = null!;
    public DbSet<WorkoutSection> WorkoutSections { get; set; } = null!;
    public DbSet<WorkoutMovement> WorkoutMovements { get; set; } = null!;
    public DbSet<ProgramTrack> ProgramTracks { get; set; } = null!;
    public DbSet<ProgramDay> ProgramDays { get; set; } = null!;
    public DbSet<WorkoutResult> WorkoutResults { get; set; } = null!;
    public DbSet<PersonalRecord> PersonalRecords { get; set; } = null!;
    public DbSet<LeaderboardEntry> Leaderboard { get; set; } = null!;
    public DbSet<EffortSession> EffortSessions { get; set; } = null!;
    public DbSet<AttendanceStreak> Streaks { get; set; } = null!;
    public DbSet<RankLadder> RankLadders { get; set; } = null!;
    public DbSet<RankLevel> RankLevels { get; set; } = null!;
    public DbSet<MemberRank> MemberRanks { get; set; } = null!;
    public DbSet<GradingEvent> GradingEvents { get; set; } = null!;
    public DbSet<SkillClearance> SkillClearances { get; set; } = null!;

    // ── Assessments ──────────────────────────────────────────────────────────
    public DbSet<AssessmentTemplate> AssessmentTemplates { get; set; } = null!;
    public DbSet<AssessmentMeasure> AssessmentMeasures { get; set; } = null!;
    public DbSet<Assessment> Assessments { get; set; } = null!;
    public DbSet<AssessmentValue> AssessmentValues { get; set; } = null!;
    public DbSet<ProgressPhoto> ProgressPhotos { get; set; } = null!;
    public DbSet<MemberGoal> Goals { get; set; } = null!;
    public DbSet<NutritionPlan> NutritionPlans { get; set; } = null!;
    public DbSet<HabitTracker> Habits { get; set; } = null!;
    public DbSet<HabitEntry> HabitEntries { get; set; } = null!;
    public DbSet<CoachCheckIn> CoachCheckIns { get; set; } = null!;

    // ── Sales ────────────────────────────────────────────────────────────────
    public DbSet<FitnessLead> Leads { get; set; } = null!;
    public DbSet<LeadSource> LeadSources { get; set; } = null!;
    public DbSet<LeadActivity> LeadActivities { get; set; } = null!;
    public DbSet<Tour> Tours { get; set; } = null!;
    public DbSet<TrialPass> Trials { get; set; } = null!;
    public DbSet<Referral> Referrals { get; set; } = null!;
    public DbSet<SalesTarget> SalesTargets { get; set; } = null!;
    public DbSet<LossReason> LossReasons { get; set; } = null!;

    // ── Retention ────────────────────────────────────────────────────────────
    public DbSet<ChurnScore> ChurnScores { get; set; } = null!;
    public DbSet<ChurnFactor> ChurnFactors { get; set; } = null!;
    public DbSet<RetentionTask> RetentionTasks { get; set; } = null!;
    public DbSet<EngagementJourney> Journeys { get; set; } = null!;
    public DbSet<JourneyStep> JourneySteps { get; set; } = null!;
    public DbSet<JourneyEnrolment> JourneyEnrolments { get; set; } = null!;
    public DbSet<Campaign> Campaigns { get; set; } = null!;
    public DbSet<MessageTemplate> MessageTemplates { get; set; } = null!;
    public DbSet<MessageLog> MessageLog { get; set; } = null!;
    public DbSet<Segment> Segments { get; set; } = null!;
    public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; } = null!;
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; } = null!;
    public DbSet<LoyaltyTier> LoyaltyTiers { get; set; } = null!;
    public DbSet<Challenge> Challenges { get; set; } = null!;
    public DbSet<ChallengeParticipant> ChallengeParticipants { get; set; } = null!;
    public DbSet<Badge> Badges { get; set; } = null!;
    public DbSet<MemberBadge> MemberBadges { get; set; } = null!;
    public DbSet<NpsResponse> NpsResponses { get; set; } = null!;
    public DbSet<Feedback> Feedback { get; set; } = null!;

    // ── Staff ────────────────────────────────────────────────────────────────
    public DbSet<FitnessStaff> Staff { get; set; } = null!;
    public DbSet<StaffRole> StaffRoles { get; set; } = null!;
    public DbSet<StaffCertification> Certifications { get; set; } = null!;
    public DbSet<Shift> Shifts { get; set; } = null!;
    public DbSet<ShiftAssignment> ShiftAssignments { get; set; } = null!;
    public DbSet<ShiftSwapRequest> SwapRequests { get; set; } = null!;
    public DbSet<TimeClockEntry> TimeClock { get; set; } = null!;
    public DbSet<CommissionRule> CommissionRules { get; set; } = null!;
    public DbSet<CommissionAccrual> CommissionAccruals { get; set; } = null!;
    public DbSet<CommissionStatement> CommissionStatements { get; set; } = null!;
    public DbSet<StaffTarget> StaffTargets { get; set; } = null!;

    // ── Facility ─────────────────────────────────────────────────────────────
    public DbSet<LockerBank> LockerBanks { get; set; } = null!;
    public DbSet<Locker> Lockers { get; set; } = null!;
    public DbSet<LockerAssignment> LockerAssignments { get; set; } = null!;
    public DbSet<BookableResource> Resources { get; set; } = null!;
    public DbSet<ResourceSlotRule> ResourceSlotRules { get; set; } = null!;
    public DbSet<ResourceBooking> ResourceBookings { get; set; } = null!;
    public DbSet<EquipmentAsset> Equipment { get; set; } = null!;
    public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; } = null!;
    public DbSet<WorkOrder> WorkOrders { get; set; } = null!;
    public DbSet<FaultReport> FaultReports { get; set; } = null!;
    public DbSet<EquipmentUsageLog> EquipmentUsage { get; set; } = null!;

    // ── Compliance ───────────────────────────────────────────────────────────
    public DbSet<WaiverTemplate> WaiverTemplates { get; set; } = null!;
    public DbSet<WaiverSignature> WaiverSignatures { get; set; } = null!;
    public DbSet<HealthScreening> HealthScreenings { get; set; } = null!;
    public DbSet<HealthScreeningAnswer> ScreeningAnswers { get; set; } = null!;
    public DbSet<MedicalClearance> Clearances { get; set; } = null!;
    public DbSet<Incident> Incidents { get; set; } = null!;
    public DbSet<IncidentAction> IncidentActions { get; set; } = null!;
    public DbSet<LostPropertyItem> LostProperty { get; set; } = null!;
    public DbSet<Complaint> Complaints { get; set; } = null!;
    public DbSet<FacilityCheck> FacilityChecks { get; set; } = null!;
    public DbSet<FacilityCheckItem> FacilityCheckItems { get; set; } = null!;
    public DbSet<ShiftHandover> Handovers { get; set; } = null!;
    public DbSet<Announcement> Announcements { get; set; } = null!;
    public DbSet<AuditEntry> Audit { get; set; } = null!;

    // ── Commerce ─────────────────────────────────────────────────────────────
    public DbSet<CorporateAccount> CorporateAccounts { get; set; } = null!;
    public DbSet<CorporateEligibilityRule> EligibilityRules { get; set; } = null!;
    public DbSet<CorporateMember> CorporateMembers { get; set; } = null!;
    public DbSet<CorporateInvoice> CorporateInvoices { get; set; } = null!;
    public DbSet<ThirdPartyPayer> Payers { get; set; } = null!;
    public DbSet<PayerAuthorisation> Authorisations { get; set; } = null!;
    public DbSet<FitnessSale> Sales { get; set; } = null!;
    public DbSet<FitnessSaleLine> SaleLines { get; set; } = null!;
    public DbSet<HouseAccountCharge> HouseAccountCharges { get; set; } = null!;
    public DbSet<VendingRevenueEntry> VendingRevenue { get; set; } = null!;
    public DbSet<CashSession> CashSessions { get; set; } = null!;
    public DbSet<CashMovement> CashMovements { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);

        ConfigureClub(modelBuilder);
        ConfigureMember(modelBuilder);
        ConfigureCatalogue(modelBuilder);
        ConfigureAgreement(modelBuilder);
        ConfigureMoney(modelBuilder);
        ConfigureAccess(modelBuilder);
        ConfigureClasses(modelBuilder);
        ConfigureAppointments(modelBuilder);
        ConfigureTraining(modelBuilder);
        ConfigureAssessments(modelBuilder);
        ConfigureSales(modelBuilder);
        ConfigureRetention(modelBuilder);
        ConfigureStaff(modelBuilder);
        ConfigureFacility(modelBuilder);
        ConfigureCompliance(modelBuilder);
        ConfigureCommerce(modelBuilder);

        ApplyConventions(modelBuilder);
    }

    // ═══ Club ════════════════════════════════════════════════════════════════

    private static void ConfigureClub(ModelBuilder b)
    {
        b.Entity<FitnessClub>(e =>
        {
            e.ToTable("Clubs", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.PostCode).HasMaxLength(20);
            e.Property(x => x.CountryCode).HasMaxLength(3);
            e.Property(x => x.TimeZoneId).HasMaxLength(80);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.BrandCode).HasMaxLength(50);
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.ReceiptFooter).HasMaxLength(1000);
            e.Property(x => x.ClosureNote).HasMaxLength(500);
            e.Property(x => x.Latitude).HasPrecision(10, 7);
            e.Property(x => x.Longitude).HasPrecision(10, 7);
            e.Property(x => x.DefaultTaxPercent).HasPrecision(9, 4);
            e.Property(x => x.AccessBalanceThreshold).HasPrecision(18, 2);
            e.Property(x => x.CrossClubVisitFee).HasPrecision(18, 2);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.Code })
                .IsUnique().HasDatabaseName("IX_Club_Tenant_Code");

            e.HasMany(x => x.Schedules).WithOne(s => s.Club).HasForeignKey(s => s.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Areas).WithOne(a => a.Club).HasForeignKey(a => a.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Rooms).WithOne(r => r.Club).HasForeignKey(r => r.ClubId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ClubSchedule>(e =>
        {
            e.ToTable("ClubSchedules", DefaultSchema);
            e.Property(x => x.Note).HasMaxLength(300);
            e.HasIndex(x => new { x.ClubId, x.DayOfWeek, x.OverrideDate });
        });

        b.Entity<ClubClosure>(e =>
        {
            e.ToTable("ClubClosures", DefaultSchema);
            e.Property(x => x.Reason).IsRequired().HasMaxLength(300);
            e.Property(x => x.MemberNotice).HasMaxLength(1000);
            e.HasOne(x => x.Club).WithMany().HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClubId, x.StartsOn, x.EndsOn });
        });

        b.Entity<ClubArea>(e =>
        {
            e.ToTable("Areas", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.OutOfServiceNote).HasMaxLength(500);
            e.HasIndex(x => new { x.ClubId, x.DisplayOrder });
        });

        b.Entity<Room>(e =>
        {
            e.ToTable("Rooms", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.EquipmentNote).HasMaxLength(500);
            e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Spots).WithOne(s => s.Room).HasForeignKey(s => s.RoomId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClubId, x.DisplayOrder });
        });

        b.Entity<RoomSpot>(e =>
        {
            e.ToTable("RoomSpots", DefaultSchema);
            e.Property(x => x.Label).IsRequired().HasMaxLength(20);
            e.Property(x => x.ReservedNote).HasMaxLength(200);
            e.HasIndex(x => new { x.RoomId, x.Label }).IsUnique().HasDatabaseName("IX_RoomSpot_Room_Label");
        });

        b.Entity<FitnessSettings>(e =>
        {
            e.ToTable("Settings", DefaultSchema);
            e.Property(x => x.MemberNumberPrefix).IsRequired().HasMaxLength(10);
            e.Property(x => x.FromEmail).HasMaxLength(200);
            e.Property(x => x.FromName).HasMaxLength(200);
            e.Property(x => x.SmsSenderId).HasMaxLength(20);
            e.Property(x => x.DefaultFreezeFeePerMonth).HasPrecision(18, 2);
            e.Property(x => x.DefaultLateFee).HasPrecision(18, 2);
            e.Property(x => x.DiscountApprovalThresholdPercent).HasPrecision(9, 4);
            e.Property(x => x.RefundApprovalThreshold).HasPrecision(18, 2);
            e.Property(x => x.WriteOffApprovalThreshold).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId })
                .IsUnique().HasDatabaseName("IX_Settings_Tenant");
        });
    }

    // ═══ Member ══════════════════════════════════════════════════════════════

    private static void ConfigureMember(ModelBuilder b)
    {
        b.Entity<Member>(e =>
        {
            e.ToTable("Members", DefaultSchema);
            e.Property(x => x.MemberNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.PreferredName).HasMaxLength(100);
            e.Property(x => x.NationalId).HasMaxLength(50);
            e.Property(x => x.Occupation).HasMaxLength(150);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.AlternatePhone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.PostCode).HasMaxLength(20);
            e.Property(x => x.CountryCode).HasMaxLength(3);
            e.Property(x => x.PreferredLanguage).HasMaxLength(10);
            e.Property(x => x.MedicalSummary).HasMaxLength(1000);
            e.Property(x => x.BanReason).HasMaxLength(500);
            e.Property(x => x.AccountBalance).HasPrecision(18, 2);
            e.Property(x => x.CreditBalance).HasPrecision(18, 2);
            e.Property(x => x.VisitFrequencyBaseline).HasPrecision(9, 2);

            // The three reads that matter at scale.
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.MemberNumber })
                .IsUnique().HasDatabaseName("IX_Member_Tenant_Number");
            e.HasIndex(x => new { x.CompanyId, x.HomeClubId, x.Status })
                .HasDatabaseName("IX_Member_Club_Status");
            e.HasIndex(x => new { x.CompanyId, x.LastName, x.FirstName })
                .HasDatabaseName("IX_Member_Name");
            e.HasIndex(x => new { x.CompanyId, x.Phone }).HasDatabaseName("IX_Member_Phone");
            e.HasIndex(x => new { x.CompanyId, x.RiskBand }).HasDatabaseName("IX_Member_Risk");

            e.HasOne(x => x.HomeClub).WithMany().HasForeignKey(x => x.HomeClubId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Household).WithMany().HasForeignKey(x => x.HouseholdId).OnDelete(DeleteBehavior.NoAction);

            e.HasMany(x => x.Notes).WithOne(n => n.Member).HasForeignKey(n => n.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Tags).WithOne(t => t.Member).HasForeignKey(t => t.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Credentials).WithOne(c => c.Member).HasForeignKey(c => c.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.EmergencyContacts).WithOne(c => c.Member).HasForeignKey(c => c.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Agreements).WithOne(a => a.Member).HasForeignKey(a => a.MemberId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Household>(e =>
        {
            e.ToTable("Households", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.PostCode).HasMaxLength(20);
            e.HasMany(x => x.Members).WithOne(m => m.Household).HasForeignKey(m => m.HouseholdId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<HouseholdMember>(e =>
        {
            e.ToTable("HouseholdMembers", DefaultSchema);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.HouseholdId, x.MemberId }).IsUnique();
        });

        b.Entity<EmergencyContact>(e =>
        {
            e.ToTable("EmergencyContacts", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Relationship).IsRequired().HasMaxLength(80);
            e.Property(x => x.Phone).IsRequired().HasMaxLength(50);
            e.Property(x => x.AlternatePhone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
        });

        b.Entity<MedicalFlag>(e =>
        {
            e.ToTable("MedicalFlags", DefaultSchema);
            e.Property(x => x.Category).IsRequired().HasMaxLength(100);
            e.Property(x => x.Detail).IsRequired().HasMaxLength(1000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.MemberId);
        });

        b.Entity<MemberNote>(e =>
        {
            e.ToTable("MemberNotes", DefaultSchema);
            e.Property(x => x.Body).IsRequired().HasMaxLength(4000);
            e.Property(x => x.AuthorName).HasMaxLength(200);
            e.Property(x => x.RelatedEntityType).HasMaxLength(80);
            e.HasIndex(x => new { x.MemberId, x.OccurredAt }).HasDatabaseName("IX_MemberNote_Member_Time");
        });

        b.Entity<MemberTag>(e =>
        {
            e.ToTable("MemberTags", DefaultSchema);
            e.Property(x => x.Tag).IsRequired().HasMaxLength(60);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.HasIndex(x => new { x.MemberId, x.Tag }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Tag }).HasDatabaseName("IX_MemberTag_Lookup");
        });

        b.Entity<MemberAlert>(e =>
        {
            e.ToTable("MemberAlerts", DefaultSchema);
            e.Property(x => x.Message).IsRequired().HasMaxLength(500);
            e.Property(x => x.ActionLabel).HasMaxLength(100);
            e.Property(x => x.ActionRoute).HasMaxLength(200);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);

            // Read on every check-in, so it is indexed for exactly that query.
            e.HasIndex(x => new { x.MemberId, x.BlocksAccess }).HasDatabaseName("IX_MemberAlert_Member_Blocking");
        });

        b.Entity<MemberStatusHistory>(e =>
        {
            e.ToTable("MemberStatusHistory", DefaultSchema);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.SourceEntityType).HasMaxLength(80);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.ChangedAt });
        });

        b.Entity<MemberDocument>(e =>
        {
            e.ToTable("MemberDocuments", DefaultSchema);
            e.Property(x => x.FileName).IsRequired().HasMaxLength(300);
            e.Property(x => x.FileUrl).IsRequired().HasMaxLength(1000);
            e.Property(x => x.ContentType).HasMaxLength(120);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.Kind });
        });

        b.Entity<MemberCredential>(e =>
        {
            e.ToTable("Credentials", DefaultSchema);
            e.Property(x => x.Identifier).IsRequired().HasMaxLength(200);
            e.Property(x => x.DeactivationReason).HasMaxLength(300);
            e.Property(x => x.ReplacementFee).HasPrecision(18, 2);

            // The door's lookup. Filtered unique so a lost fob's number can be reissued later, but
            // two live credentials can never share one identifier.
            e.HasIndex(x => new { x.CompanyId, x.Identifier })
                .IsUnique()
                .HasFilter("\"Status\" = 1 AND \"IsDeleted\" = false")
                .HasDatabaseName("IX_Credential_Active_Identifier");

            e.HasIndex(x => new { x.CompanyId, x.Identifier, x.Status })
                .HasDatabaseName("IX_Credential_Lookup");
            e.HasIndex(x => x.MemberId);
        });

        b.Entity<MemberConsent>(e =>
        {
            e.ToTable("Consents", DefaultSchema);
            e.Property(x => x.Purpose).IsRequired().HasMaxLength(120);
            e.Property(x => x.ConsentText).HasMaxLength(2000);
            e.Property(x => x.CapturedVia).HasMaxLength(80);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.Channel, x.Purpose });
        });

        b.Entity<MemberPreference>(e =>
        {
            e.ToTable("Preferences", DefaultSchema);
            e.Property(x => x.InterestsJson).HasMaxLength(4000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.MemberId).IsUnique();
        });
    }

    // ═══ Catalogue ═══════════════════════════════════════════════════════════

    private static void ConfigureCatalogue(ModelBuilder b)
    {
        b.Entity<MembershipPlan>(e =>
        {
            e.ToTable("Plans", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.MarketingBlurb).HasMaxLength(1000);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);
            e.Property(x => x.JoiningFee).HasPrecision(18, 2);
            e.Property(x => x.AdminFee).HasPrecision(18, 2);
            e.Property(x => x.CardFee).HasPrecision(18, 2);
            e.Property(x => x.AnnualMaintenanceFee).HasPrecision(18, 2);
            e.Property(x => x.EarlyTerminationFee).HasPrecision(18, 2);
            e.Property(x => x.EarlyTerminationPercentOfRemaining).HasPrecision(9, 4);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.Code })
                .IsUnique().HasDatabaseName("IX_Plan_Tenant_Code");
            e.HasIndex(x => new { x.CompanyId, x.Kind, x.IsActive });

            e.HasMany(x => x.ClubPrices).WithOne(p => p.Plan).HasForeignKey(p => p.PlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Entitlements).WithOne(p => p.Plan).HasForeignKey(p => p.PlanId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PlanPrice>(e =>
        {
            e.ToTable("PlanPrices", DefaultSchema);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.JoiningFee).HasPrecision(18, 2);
            e.Property(x => x.CurrencyCode).HasMaxLength(3);
            e.HasIndex(x => new { x.PlanId, x.ClubId });
        });

        b.Entity<PlanEntitlement>(e =>
        {
            e.ToTable("PlanEntitlements", DefaultSchema);
            e.Property(x => x.TargetName).HasMaxLength(200);
            e.Property(x => x.OverageFee).HasPrecision(18, 2);
            e.HasMany(x => x.TimeBands).WithOne(t => t.Entitlement).HasForeignKey(t => t.EntitlementId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PlanId, x.Kind });
        });

        b.Entity<AccessTimeBand>(e =>
        {
            e.ToTable("AccessTimeBands", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });

        b.Entity<PromotionRule>(e =>
        {
            e.ToTable("Promotions", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Value).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.PlanId, x.IsActive });
        });

        b.Entity<PromoCode>(e =>
        {
            e.ToTable("PromoCodes", DefaultSchema);
            e.Property(x => x.CodeText).IsRequired().HasMaxLength(60);
            e.HasOne(x => x.PromotionRule).WithMany().HasForeignKey(x => x.PromotionRuleId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.CodeText }).IsUnique().HasDatabaseName("IX_PromoCode_Tenant_Code");
        });

        b.Entity<PlanChangePath>(e =>
        {
            e.ToTable("PlanChangePaths", DefaultSchema);
            e.Property(x => x.ChangeFee).HasPrecision(18, 2);
            e.HasIndex(x => new { x.FromPlanId, x.ToPlanId }).IsUnique();
        });

        b.Entity<AppointmentService>(e =>
        {
            e.ToTable("Services", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);
            e.HasIndex(x => new { x.CompanyId, x.Kind, x.IsActive });
        });
    }

    // ═══ Agreement ═══════════════════════════════════════════════════════════

    private static void ConfigureAgreement(ModelBuilder b)
    {
        b.Entity<Agreement>(e =>
        {
            e.ToTable("Agreements", DefaultSchema);
            e.Property(x => x.AgreementNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);
            e.Property(x => x.PromotionalPrice).HasPrecision(18, 2);
            e.Property(x => x.EarlyTerminationFeeCharged).HasPrecision(18, 2);
            e.Property(x => x.LeaveNote).HasMaxLength(1000);
            e.Property(x => x.SignatureImageUrl).HasMaxLength(1000);
            e.Property(x => x.DocumentUrl).HasMaxLength(1000);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.AgreementNumber })
                .IsUnique().HasDatabaseName("IX_Agreement_Tenant_Number");
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.Status });
            e.HasIndex(x => new { x.MemberId, x.Status });

            // The billing run's query: everything due on or before a date.
            e.HasIndex(x => new { x.CompanyId, x.NextBillingOn, x.Status })
                .HasDatabaseName("IX_Agreement_Billing_Due");

            e.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Amendments).WithOne(a => a.Agreement).HasForeignKey(a => a.AgreementId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Freezes).WithOne(f => f.Agreement).HasForeignKey(f => f.AgreementId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AgreementTemplate>(e =>
        {
            e.ToTable("AgreementTemplates", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.BodyHtml).IsRequired();
            e.Property(x => x.CountryCode).HasMaxLength(3);
            e.Property(x => x.LanguageCode).HasMaxLength(10);
            e.HasIndex(x => new { x.CompanyId, x.Name, x.Version }).IsUnique();
        });

        b.Entity<AgreementAmendment>(e =>
        {
            e.ToTable("Amendments", DefaultSchema);
            e.Property(x => x.PreviousPrice).HasPrecision(18, 2);
            e.Property(x => x.NewPrice).HasPrecision(18, 2);
            e.Property(x => x.ChangeFee).HasPrecision(18, 2);
            e.Property(x => x.ProrationAmount).HasPrecision(18, 2);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.DocumentUrl).HasMaxLength(1000);
        });

        b.Entity<AgreementSignature>(e =>
        {
            e.ToTable("AgreementSignatures", DefaultSchema);
            e.Property(x => x.SignerName).IsRequired().HasMaxLength(200);
            e.Property(x => x.GuardianName).HasMaxLength(200);
            e.Property(x => x.GuardianRelationship).HasMaxLength(80);
            e.Property(x => x.SignatureImageUrl).HasMaxLength(1000);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.CapturedVia).HasMaxLength(80);
            e.Property(x => x.RemoteToken).HasMaxLength(120);
            e.HasOne(x => x.Agreement).WithMany().HasForeignKey(x => x.AgreementId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.RemoteToken);
        });

        b.Entity<MembershipFreeze>(e =>
        {
            e.ToTable("Freezes", DefaultSchema);
            e.Property(x => x.ReasonNote).HasMaxLength(500);
            e.Property(x => x.FeePerPeriod).HasPrecision(18, 2);
            e.Property(x => x.TotalFeeCharged).HasPrecision(18, 2);
            e.HasIndex(x => new { x.MemberId, x.StartsOn });

            // The nightly release job's query.
            e.HasIndex(x => new { x.CompanyId, x.EndsOn, x.IsReleased })
                .HasDatabaseName("IX_Freeze_Release_Due");
        });

        b.Entity<MembershipSuspension>(e =>
        {
            e.ToTable("Suspensions", DefaultSchema);
            e.Property(x => x.ReasonNote).HasMaxLength(500);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.LiftedOn });
        });

        b.Entity<CancellationRequest>(e =>
        {
            e.ToTable("CancellationRequests", DefaultSchema);
            e.Property(x => x.ReasonNote).HasMaxLength(1000);
            e.Property(x => x.Channel).HasMaxLength(60);
            e.Property(x => x.EarlyTerminationFee).HasPrecision(18, 2);
            e.Property(x => x.RefundDue).HasPrecision(18, 2);
            e.Property(x => x.OutstandingBalance).HasPrecision(18, 2);
            e.HasOne(x => x.Agreement).WithMany().HasForeignKey(x => x.AgreementId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Offers).WithOne(o => o.CancellationRequest).HasForeignKey(o => o.CancellationRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.EffectiveOn, x.IsProcessed })
                .HasDatabaseName("IX_Cancellation_Due");
        });

        b.Entity<SaveOffer>(e =>
        {
            e.ToTable("SaveOffers", DefaultSchema);
            e.Property(x => x.Summary).IsRequired().HasMaxLength(500);
            e.Property(x => x.DiscountValue).HasPrecision(18, 2);
            e.Property(x => x.DeclineNote).HasMaxLength(500);
        });
    }

    // ═══ Money ═══════════════════════════════════════════════════════════════

    private static void ConfigureMoney(ModelBuilder b)
    {
        b.Entity<BillingSchedule>(e =>
        {
            e.ToTable("BillingSchedules", DefaultSchema);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.OriginalAmount).HasPrecision(18, 2);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.SkipReason).HasMaxLength(300);
            e.Property(x => x.AdjustmentNote).HasMaxLength(300);
            e.HasOne(x => x.Agreement).WithMany().HasForeignKey(x => x.AgreementId).OnDelete(DeleteBehavior.Cascade);

            // The billing run picks up everything due and unbilled.
            e.HasIndex(x => new { x.CompanyId, x.DueOn, x.IsBilled, x.IsSkipped })
                .HasDatabaseName("IX_Schedule_Due");
            e.HasIndex(x => new { x.AgreementId, x.PeriodNumber });
        });

        b.Entity<BillingRun>(e =>
        {
            e.ToTable("BillingRuns", DefaultSchema);
            e.Property(x => x.RunNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.TotalBilled).HasPrecision(18, 2);
            e.Property(x => x.TotalCollected).HasPrecision(18, 2);
            e.Property(x => x.TotalFailed).HasPrecision(18, 2);
            e.Property(x => x.ErrorSummary).HasMaxLength(4000);
            e.HasMany(x => x.Lines).WithOne(l => l.BillingRun).HasForeignKey(l => l.BillingRunId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.BillingDate });
        });

        b.Entity<BillingRunLine>(e =>
        {
            e.ToTable("BillingRunLines", DefaultSchema);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Outcome).IsRequired().HasMaxLength(40);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.HasIndex(x => new { x.BillingRunId, x.Outcome });
        });

        b.Entity<FitnessInvoice>(e =>
        {
            e.ToTable("Invoices", DefaultSchema);
            e.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.AmountRefunded).HasPrecision(18, 2);
            e.Property(x => x.BalanceDue).HasPrecision(18, 2);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 8);
            e.Property(x => x.DocumentUrl).HasMaxLength(1000);
            e.Property(x => x.Notes).HasMaxLength(2000);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.InvoiceNumber })
                .IsUnique().HasDatabaseName("IX_Invoice_Tenant_Number");
            e.HasIndex(x => new { x.MemberId, x.Status });

            // The arrears report: everything with a balance, oldest first.
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.Status, x.DueOn })
                .HasDatabaseName("IX_Invoice_Arrears");

            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Invoice).HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payments).WithOne(p => p.Invoice).HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<FitnessInvoiceLine>(e =>
        {
            e.ToTable("InvoiceLines", DefaultSchema);
            e.Property(x => x.LineDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.ProrationExplanation).HasMaxLength(500);
            e.Property(x => x.SourceEntityType).HasMaxLength(80);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
        });

        b.Entity<FitnessPayment>(e =>
        {
            e.ToTable("Payments", DefaultSchema);
            e.Property(x => x.PaymentNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RefundedAmount).HasPrecision(18, 2);
            e.Property(x => x.ProviderReference).HasMaxLength(200);
            e.Property(x => x.AuthorisationCode).HasMaxLength(50);
            e.Property(x => x.CardBrand).HasMaxLength(30);
            e.Property(x => x.CardLastFour).HasMaxLength(4);
            e.Property(x => x.MandateReference).HasMaxLength(100);
            e.Property(x => x.FailureMessage).HasMaxLength(1000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(1000);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.PaymentNumber })
                .IsUnique().HasDatabaseName("IX_Payment_Tenant_Number");

            // Stops a retried request taking the money twice.
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey })
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL")
                .HasDatabaseName("IX_Payment_Idempotency");

            e.HasIndex(x => new { x.MemberId, x.ReceivedOn });
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.ReceivedOn });
        });

        b.Entity<PaymentMethodRef>(e =>
        {
            e.ToTable("PaymentMethods", DefaultSchema);
            e.Property(x => x.ProviderToken).HasMaxLength(300);
            e.Property(x => x.ProviderName).HasMaxLength(80);
            e.Property(x => x.CardBrand).HasMaxLength(30);
            e.Property(x => x.CardLastFour).HasMaxLength(4);
            e.Property(x => x.BankName).HasMaxLength(150);
            e.Property(x => x.AccountLastFour).HasMaxLength(4);
            e.Property(x => x.AccountHolderName).HasMaxLength(200);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.IsDefault });

            // The card-expiry chase.
            e.HasIndex(x => new { x.CompanyId, x.ExpiryYear, x.ExpiryMonth })
                .HasDatabaseName("IX_PaymentMethod_Expiry");
        });

        b.Entity<PaymentMandate>(e =>
        {
            e.ToTable("Mandates", DefaultSchema);
            e.Property(x => x.MandateReference).IsRequired().HasMaxLength(100);
            e.Property(x => x.SchemeName).HasMaxLength(80);
            e.Property(x => x.CancellationReason).HasMaxLength(300);
            e.HasIndex(x => new { x.CompanyId, x.MandateReference });
        });

        b.Entity<DunningPolicy>(e =>
        {
            e.ToTable("DunningPolicies", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasMany(x => x.Steps).WithOne(s => s.DunningPolicy).HasForeignKey(s => s.DunningPolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DunningStep>(e =>
        {
            e.ToTable("DunningSteps", DefaultSchema);
            e.Property(x => x.FeeAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.DunningPolicyId, x.StepNumber }).IsUnique();
        });

        b.Entity<DunningCase>(e =>
        {
            e.ToTable("DunningCases", DefaultSchema);
            e.Property(x => x.CaseNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.AmountOutstanding).HasPrecision(18, 2);
            e.Property(x => x.AmountRecovered).HasPrecision(18, 2);
            e.Property(x => x.LateFeesAdded).HasPrecision(18, 2);
            e.Property(x => x.PauseReason).HasMaxLength(500);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Events).WithOne(v => v.DunningCase).HasForeignKey(v => v.DunningCaseId).OnDelete(DeleteBehavior.Cascade);

            // The nightly ladder walk.
            e.HasIndex(x => new { x.CompanyId, x.Status, x.NextStepDueOn })
                .HasDatabaseName("IX_Dunning_Due");
            e.HasIndex(x => new { x.MemberId, x.Status });
        });

        b.Entity<DunningEvent>(e =>
        {
            e.ToTable("DunningEvents", DefaultSchema);
            e.Property(x => x.Detail).HasMaxLength(1000);
            e.Property(x => x.AmountCollected).HasPrecision(18, 2);
        });

        b.Entity<CreditNote>(e =>
        {
            e.ToTable("CreditNotes", DefaultSchema);
            e.Property(x => x.CreditNoteNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.DocumentUrl).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.CreditNoteNumber })
                .IsUnique().HasDatabaseName("IX_CreditNote_Tenant_Number");
        });

        b.Entity<Refund>(e =>
        {
            e.ToTable("Refunds", DefaultSchema);
            e.Property(x => x.RefundNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.ProviderReference).HasMaxLength(200);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.RefundNumber })
                .IsUnique().HasDatabaseName("IX_Refund_Tenant_Number");
        });

        b.Entity<WriteOff>(e =>
        {
            e.ToTable("WriteOffs", DefaultSchema);
            e.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RecoveredAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.WrittenOffOn });
        });

        b.Entity<MemberLedgerEntry>(e =>
        {
            e.ToTable("Ledger", DefaultSchema);
            e.Property(x => x.EntryDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.MemberId, x.OccurredAt }).HasDatabaseName("IX_Ledger_Member_Time");
        });

        b.Entity<DeferredRevenueSchedule>(e =>
        {
            e.ToTable("DeferredRevenue", DefaultSchema);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.RecognisedAmount).HasPrecision(18, 2);
            e.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            e.HasMany(x => x.Entries).WithOne(v => v.Schedule).HasForeignKey(v => v.ScheduleId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.IsClosed, x.ServiceEnd })
                .HasDatabaseName("IX_Deferred_Open");
        });

        b.Entity<DeferredRevenueEntry>(e =>
        {
            e.ToTable("DeferredRevenueEntries", DefaultSchema);
            e.Property(x => x.Trigger).IsRequired().HasMaxLength(80);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ScheduleId, x.RecognisedOn });
        });

        b.Entity<MemberCreditBalance>(e =>
        {
            e.ToTable("CreditBalances", DefaultSchema);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.MemberId).IsUnique();
        });

        b.Entity<GiftCard>(e =>
        {
            e.ToTable("GiftCards", DefaultSchema);
            e.Property(x => x.CardNumber).IsRequired().HasMaxLength(40);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.InitialValue).HasPrecision(18, 2);
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.RecipientName).HasMaxLength(200);
            e.Property(x => x.RecipientEmail).HasMaxLength(200);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.HasMany(x => x.Transactions).WithOne(t => t.GiftCard).HasForeignKey(t => t.GiftCardId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.CardNumber }).IsUnique().HasDatabaseName("IX_GiftCard_Tenant_Number");
        });

        b.Entity<GiftCardTransaction>(e =>
        {
            e.ToTable("GiftCardTransactions", DefaultSchema);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.Property(x => x.Note).HasMaxLength(300);
        });
    }

    // ═══ Access ══════════════════════════════════════════════════════════════

    private static void ConfigureAccess(ModelBuilder b)
    {
        b.Entity<Door>(e =>
        {
            e.ToTable("Doors", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ReaderAddress).HasMaxLength(80);
            e.Property(x => x.HardwareKind).HasMaxLength(80);
            e.Property(x => x.HeldOpenReason).HasMaxLength(300);
            e.HasOne(x => x.Club).WithMany().HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Controller).WithMany(c => c.Doors).HasForeignKey(x => x.ControllerId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.ClubId);
        });

        b.Entity<AccessController>(e =>
        {
            e.ToTable("Controllers", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Vendor).HasMaxLength(80);
            e.Property(x => x.Model).HasMaxLength(80);
            e.Property(x => x.FirmwareVersion).HasMaxLength(40);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.SerialNumber).HasMaxLength(80);
            e.Property(x => x.ApiKeyHash).HasMaxLength(200);
            e.HasIndex(x => x.ClubId);
        });

        b.Entity<AccessRule>(e =>
        {
            e.ToTable("AccessRules", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.BalanceThreshold).HasPrecision(18, 2);
            e.HasMany(x => x.Windows).WithOne(w => w.AccessRule).HasForeignKey(w => w.AccessRuleId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AccessRuleWindow>(e =>
        {
            e.ToTable("AccessRuleWindows", DefaultSchema);
            e.Property(x => x.Label).HasMaxLength(100);
        });

        b.Entity<CheckIn>(e =>
        {
            e.ToTable("CheckIns", DefaultSchema);
            e.Property(x => x.FeeCharged).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);

            // Attendance reporting, and the "who is in the club right now" query.
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.CheckedInAt }).HasDatabaseName("IX_CheckIn_Club_Time");
            e.HasIndex(x => new { x.MemberId, x.CheckedInAt }).HasDatabaseName("IX_CheckIn_Member_Time");
            e.HasIndex(x => new { x.ClubId, x.CheckedOutAt }).HasDatabaseName("IX_CheckIn_Open");
        });

        b.Entity<AccessEvent>(e =>
        {
            e.ToTable("AccessEvents", DefaultSchema);
            e.Property(x => x.CredentialIdentifier).HasMaxLength(200);
            e.Property(x => x.DecisionMessage).HasMaxLength(500);
            e.Property(x => x.ImageUrl).HasMaxLength(1000);
            e.Property(x => x.OverrideReason).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.OccurredAt }).HasDatabaseName("IX_AccessEvent_Club_Time");
            e.HasIndex(x => new { x.MemberId, x.OccurredAt });
            e.HasIndex(x => new { x.ClubId, x.Decision, x.OccurredAt }).HasDatabaseName("IX_AccessEvent_Denials");
        });

        b.Entity<OccupancySnapshot>(e =>
        {
            e.ToTable("OccupancySnapshots", DefaultSchema);
            e.HasIndex(x => new { x.ClubId, x.TakenAt }).HasDatabaseName("IX_Occupancy_Club_Time");
        });

        b.Entity<GuestVisit>(e =>
        {
            e.ToTable("GuestVisits", DefaultSchema);
            e.Property(x => x.GuestName).IsRequired().HasMaxLength(200);
            e.Property(x => x.GuestPhone).HasMaxLength(50);
            e.Property(x => x.GuestEmail).HasMaxLength(200);
            e.Property(x => x.FeeCharged).HasPrecision(18, 2);
            e.HasOne(x => x.HostMember).WithMany().HasForeignKey(x => x.HostMemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.HostMemberId, x.VisitedOn });
        });

        b.Entity<DayPass>(e =>
        {
            e.ToTable("DayPasses", DefaultSchema);
            e.Property(x => x.PassNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.VisitorName).IsRequired().HasMaxLength(200);
            e.Property(x => x.VisitorPhone).HasMaxLength(50);
            e.Property(x => x.VisitorEmail).HasMaxLength(200);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.PassNumber }).IsUnique().HasDatabaseName("IX_DayPass_Tenant_Number");
            e.HasIndex(x => new { x.ClubId, x.ValidFrom, x.ValidTo });
        });

        b.Entity<VisitAllowanceUsage>(e =>
        {
            e.ToTable("AllowanceUsage", DefaultSchema);
            e.Property(x => x.OverageCharged).HasPrecision(18, 2);
            e.HasIndex(x => new { x.MemberId, x.AgreementId, x.Kind, x.PeriodStart })
                .HasDatabaseName("IX_Allowance_Lookup");
        });
    }

    // ═══ Classes ═════════════════════════════════════════════════════════════

    private static void ConfigureClasses(ModelBuilder b)
    {
        b.Entity<ClassType>(e =>
        {
            e.ToTable("ClassTypes", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Discipline).HasMaxLength(80);
            e.Property(x => x.MarketingBlurb).HasMaxLength(1000);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.Property(x => x.EquipmentNeeded).HasMaxLength(500);
            e.Property(x => x.DropInPrice).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.IsActive, x.DisplayOrder });
        });

        b.Entity<ClassSchedule>(e =>
        {
            e.ToTable("ClassSchedules", DefaultSchema);
            e.Property(x => x.SeasonCode).HasMaxLength(50);
            e.HasOne(x => x.ClassType).WithMany().HasForeignKey(x => x.ClassTypeId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ClubId, x.IsPublished });
        });

        b.Entity<ClassOccurrence>(e =>
        {
            e.ToTable("ClassOccurrences", DefaultSchema);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasOne(x => x.ClassType).WithMany().HasForeignKey(x => x.ClassTypeId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Bookings).WithOne(k => k.ClassOccurrence).HasForeignKey(k => k.ClassOccurrenceId).OnDelete(DeleteBehavior.Cascade);

            // The timetable's query: one club, one date range.
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.StartsAt })
                .HasDatabaseName("IX_Occurrence_Club_Start");
            e.HasIndex(x => new { x.InstructorStaffId, x.StartsAt });
            e.HasIndex(x => new { x.RoomId, x.StartsAt });
            e.HasIndex(x => new { x.CompanyId, x.Status, x.EndsAt })
                .HasDatabaseName("IX_Occurrence_Finishing");
        });

        b.Entity<ClassBooking>(e =>
        {
            e.ToTable("ClassBookings", DefaultSchema);
            e.Property(x => x.GuestName).HasMaxLength(200);
            e.Property(x => x.GuestPhone).HasMaxLength(50);
            e.Property(x => x.GuestEmail).HasMaxLength(200);
            e.Property(x => x.SpotLabel).HasMaxLength(20);
            e.Property(x => x.MarketplaceReference).HasMaxLength(120);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.PenaltyCharged).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);

            // One live booking per member per class. Cancelled rows are excluded so a member who
            // cancels and rebooks is not blocked by their own history.
            e.HasIndex(x => new { x.ClassOccurrenceId, x.MemberId })
                .IsUnique()
                .HasFilter("\"Status\" IN (1,2,3,4) AND \"IsDeleted\" = false")
                .HasDatabaseName("IX_Booking_One_Live_Per_Member");

            e.HasIndex(x => new { x.MemberId, x.BookedAt });
            e.HasIndex(x => new { x.ClassOccurrenceId, x.Status });

            // One spot can be held by one booking.
            e.HasIndex(x => new { x.ClassOccurrenceId, x.SpotId })
                .IsUnique()
                .HasFilter("\"SpotId\" IS NOT NULL AND \"Status\" IN (1,3,4) AND \"IsDeleted\" = false")
                .HasDatabaseName("IX_Booking_Spot_Unique");
        });

        b.Entity<BookingPolicy>(e =>
        {
            e.ToTable("BookingPolicies", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
        });

        b.Entity<CancellationPolicy>(e =>
        {
            e.ToTable("CancellationPolicies", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.LateCancelFee).HasPrecision(18, 2);
            e.Property(x => x.NoShowFee).HasPrecision(18, 2);
        });

        b.Entity<LateCancelStrike>(e =>
        {
            e.ToTable("Strikes", DefaultSchema);
            e.Property(x => x.ClassName).HasMaxLength(200);
            e.Property(x => x.WaivedReason).HasMaxLength(500);
            e.Property(x => x.FeeCharged).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.ExpiresOn, x.IsWaived }).HasDatabaseName("IX_Strike_Active");
        });

        b.Entity<CourseEnrolment>(e =>
        {
            e.ToTable("CourseEnrolments", DefaultSchema);
            e.Property(x => x.CourseName).IsRequired().HasMaxLength(200);
            e.Property(x => x.WithdrawalReason).HasMaxLength(500);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ClassScheduleId, x.MemberId });
        });

        b.Entity<MarketplaceChannel>(e =>
        {
            e.ToTable("MarketplaceChannels", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.RatePerBooking).HasPrecision(18, 2);
            e.Property(x => x.RevenueSharePercent).HasPrecision(9, 4);
            e.Property(x => x.ApiEndpoint).HasMaxLength(500);
            e.Property(x => x.ApiKeyHash).HasMaxLength(200);
        });

        b.Entity<MarketplaceBooking>(e =>
        {
            e.ToTable("MarketplaceBookings", DefaultSchema);
            e.Property(x => x.ExternalReference).IsRequired().HasMaxLength(120);
            e.Property(x => x.AttendeeName).IsRequired().HasMaxLength(200);
            e.Property(x => x.AttendeeEmail).HasMaxLength(200);
            e.Property(x => x.AmountDue).HasPrecision(18, 2);
            e.HasOne(x => x.MarketplaceChannel).WithMany().HasForeignKey(x => x.MarketplaceChannelId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.MarketplaceChannelId, x.ExternalReference }).IsUnique();
        });
    }

    // ═══ Appointments ════════════════════════════════════════════════════════

    private static void ConfigureAppointments(ModelBuilder b)
    {
        b.Entity<BookableStaff>(e =>
        {
            e.ToTable("BookableStaff", DefaultSchema);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Bio).HasMaxLength(2000);
            e.Property(x => x.Specialities).HasMaxLength(500);
            e.Property(x => x.ServiceIds).HasMaxLength(2000);
            e.Property(x => x.HourlyRate).HasPrecision(18, 2);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Availability).WithOne(a => a.BookableStaff).HasForeignKey(a => a.BookableStaffId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClubId, x.StaffId }).IsUnique();
        });

        b.Entity<StaffAvailability>(e =>
        {
            e.ToTable("Availability", DefaultSchema);
            e.HasIndex(x => new { x.BookableStaffId, x.DayOfWeek });
        });

        b.Entity<StaffTimeOff>(e =>
        {
            e.ToTable("TimeOff", DefaultSchema);
            e.Property(x => x.Reason).IsRequired().HasMaxLength(120);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.StaffId, x.StartsAt, x.EndsAt });
        });

        b.Entity<Appointment>(e =>
        {
            e.ToTable("Appointments", DefaultSchema);
            e.Property(x => x.AppointmentNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.Property(x => x.SessionNotes).HasMaxLength(4000);
            e.Property(x => x.PlanForNextSession).HasMaxLength(2000);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.PenaltyCharged).HasPrecision(18, 2);
            e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Participants).WithOne(p => p.Appointment).HasForeignKey(p => p.AppointmentId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.BusinessUnitId, x.AppointmentNumber })
                .IsUnique().HasDatabaseName("IX_Appointment_Tenant_Number");

            // The diary: one trainer, one day.
            e.HasIndex(x => new { x.StaffId, x.StartsAt }).HasDatabaseName("IX_Appointment_Staff_Time");
            e.HasIndex(x => new { x.MemberId, x.StartsAt });
            e.HasIndex(x => new { x.ClubId, x.StartsAt, x.Status });
        });

        b.Entity<AppointmentParticipant>(e =>
        {
            e.ToTable("AppointmentParticipants", DefaultSchema);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.PenaltyCharged).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.AppointmentId, x.MemberId }).IsUnique();
        });

        b.Entity<AppointmentSeries>(e => e.ToTable("AppointmentSeries", DefaultSchema));

        b.Entity<SessionPackagePurchase>(e =>
        {
            e.ToTable("PackagePurchases", DefaultSchema);
            e.Property(x => x.PurchaseNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);
            e.Property(x => x.PricePerSession).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.MemberId, x.IsExpired });
            e.HasIndex(x => new { x.CompanyId, x.ExpiresOn, x.IsExpired }).HasDatabaseName("IX_Package_Expiry");
        });

        b.Entity<SessionCredit>(e =>
        {
            e.ToTable("SessionCredits", DefaultSchema);
            e.Property(x => x.UnitValue).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Movements).WithOne(m => m.SessionCredit).HasForeignKey(m => m.SessionCreditId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.Kind, x.IsExpired }).HasDatabaseName("IX_Credit_Member_Kind");
            e.HasIndex(x => new { x.CompanyId, x.ExpiresOn, x.IsExpired }).HasDatabaseName("IX_Credit_Expiry");
        });

        b.Entity<SessionCreditMovement>(e =>
        {
            e.ToTable("CreditMovements", DefaultSchema);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.MemberId, x.OccurredAt });
        });

        b.Entity<SessionSignOff>(e =>
        {
            e.ToTable("SignOffs", DefaultSchema);
            e.Property(x => x.MemberSignatureUrl).HasMaxLength(1000);
            e.Property(x => x.Note).HasMaxLength(2000);
            e.Property(x => x.SessionValue).HasPrecision(18, 2);
            e.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StaffId, x.SignedOffAt });
        });

        b.Entity<CoachAssignment>(e =>
        {
            e.ToTable("CoachAssignments", DefaultSchema);
            e.Property(x => x.EndReason).HasMaxLength(300);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StaffId, x.EndedOn });
            e.HasIndex(x => new { x.MemberId, x.IsPrimary });
        });
    }

    // ═══ Training ════════════════════════════════════════════════════════════

    private static void ConfigureTraining(ModelBuilder b)
    {
        b.Entity<Exercise>(e =>
        {
            e.ToTable("Exercises", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.MuscleGroups).HasMaxLength(500);
            e.Property(x => x.Equipment).HasMaxLength(300);
            e.Property(x => x.Instructions).HasMaxLength(4000);
            e.Property(x => x.VideoUrl).HasMaxLength(1000);
            e.Property(x => x.ImageUrl).HasMaxLength(1000);
            e.Property(x => x.ScalingOptions).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.Name });
            e.HasIndex(x => new { x.CompanyId, x.Category });
        });

        b.Entity<Workout>(e =>
        {
            e.ToTable("Workouts", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Summary).HasMaxLength(2000);
            e.Property(x => x.CoachNotes).HasMaxLength(4000);
            e.Property(x => x.ScoreUnit).HasMaxLength(40);
            e.Property(x => x.BenchmarkName).HasMaxLength(120);
            e.HasMany(x => x.Sections).WithOne(s => s.Workout).HasForeignKey(s => s.WorkoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.IsBenchmark });
        });

        b.Entity<WorkoutSection>(e =>
        {
            e.ToTable("WorkoutSections", DefaultSchema);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Instructions).HasMaxLength(2000);
            e.HasMany(x => x.Movements).WithOne(m => m.WorkoutSection).HasForeignKey(m => m.WorkoutSectionId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WorkoutMovement>(e =>
        {
            e.ToTable("WorkoutMovements", DefaultSchema);
            e.Property(x => x.MovementName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Reps).HasMaxLength(60);
            e.Property(x => x.Tempo).HasMaxLength(20);
            e.Property(x => x.ScalingNote).HasMaxLength(500);
            e.Property(x => x.LoadKg).HasPrecision(9, 2);
            e.Property(x => x.LoadPercentOfMax).HasPrecision(9, 2);
            e.Property(x => x.DistanceMetres).HasPrecision(12, 2);
            e.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ProgramTrack>(e =>
        {
            e.ToTable("ProgramTracks", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.HasMany(x => x.Days).WithOne(d => d.ProgramTrack).HasForeignKey(d => d.ProgramTrackId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ProgramDay>(e =>
        {
            e.ToTable("ProgramDays", DefaultSchema);
            e.Property(x => x.CoachBrief).HasMaxLength(2000);
            e.HasOne(x => x.Workout).WithMany().HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ProgramTrackId, x.ScheduledOn }).IsUnique();
            e.HasIndex(x => new { x.ClubId, x.ScheduledOn, x.IsPublished });
        });

        b.Entity<WorkoutResult>(e =>
        {
            e.ToTable("WorkoutResults", DefaultSchema);
            e.Property(x => x.ScalingNote).HasMaxLength(500);
            e.Property(x => x.MemberNote).HasMaxLength(1000);
            e.Property(x => x.CoachNote).HasMaxLength(1000);
            e.Property(x => x.LoadKg).HasPrecision(9, 2);
            e.Property(x => x.DistanceMetres).HasPrecision(12, 2);
            e.Property(x => x.NormalisedScore).HasPrecision(18, 4);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Workout).WithMany().HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.NoAction);

            // Leaderboards sort on this.
            e.HasIndex(x => new { x.WorkoutId, x.NormalisedScore }).HasDatabaseName("IX_Result_Leaderboard");
            e.HasIndex(x => new { x.MemberId, x.PerformedOn });
        });

        b.Entity<PersonalRecord>(e =>
        {
            e.ToTable("PersonalRecords", DefaultSchema);
            e.Property(x => x.RecordName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Unit).HasMaxLength(40);
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.Property(x => x.PreviousValue).HasPrecision(18, 4);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.RecordName, x.RepMax }).IsUnique().HasDatabaseName("IX_Pr_Member_Record");
        });

        b.Entity<LeaderboardEntry>(e =>
        {
            e.ToTable("Leaderboard", DefaultSchema);
            e.Property(x => x.MemberDisplayName).IsRequired().HasMaxLength(200);
            e.Property(x => x.MemberPhotoUrl).HasMaxLength(500);
            e.Property(x => x.ScoreDisplay).HasMaxLength(60);
            e.Property(x => x.Division).HasMaxLength(60);
            e.Property(x => x.Score).HasPrecision(18, 4);
            e.HasIndex(x => new { x.WorkoutId, x.Division, x.Rank });
            e.HasIndex(x => new { x.ChallengeId, x.Rank });
        });

        b.Entity<EffortSession>(e =>
        {
            e.ToTable("EffortSessions", DefaultSchema);
            e.Property(x => x.DeviceType).HasMaxLength(80);
            e.Property(x => x.ExternalReference).HasMaxLength(120);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.StartedAt });
            e.HasIndex(x => new { x.CompanyId, x.ExternalReference });
        });

        b.Entity<AttendanceStreak>(e =>
        {
            e.ToTable("Streaks", DefaultSchema);
            e.Property(x => x.Cadence).IsRequired().HasMaxLength(20);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.Cadence }).IsUnique();
        });

        b.Entity<RankLadder>(e =>
        {
            e.ToTable("RankLadders", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Discipline).HasMaxLength(80);
            e.HasMany(x => x.Levels).WithOne(l => l.RankLadder).HasForeignKey(l => l.RankLadderId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RankLevel>(e =>
        {
            e.ToTable("RankLevels", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.Property(x => x.BadgeUrl).HasMaxLength(500);
            e.Property(x => x.RequirementsNote).HasMaxLength(2000);
            e.Property(x => x.GradingFee).HasPrecision(18, 2);
            e.HasIndex(x => new { x.RankLadderId, x.Ordinal }).IsUnique();
        });

        b.Entity<MemberRank>(e =>
        {
            e.ToTable("MemberRanks", DefaultSchema);
            e.Property(x => x.CertificateUrl).HasMaxLength(1000);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RankLevel).WithMany().HasForeignKey(x => x.RankLevelId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.MemberId, x.RankLadderId, x.IsCurrent });
        });

        b.Entity<GradingEvent>(e =>
        {
            e.ToTable("GradingEvents", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ExternalExaminerName).HasMaxLength(200);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.FeePerCandidate).HasPrecision(18, 2);
        });

        b.Entity<SkillClearance>(e =>
        {
            e.ToTable("SkillClearances", DefaultSchema);
            e.Property(x => x.SkillName).IsRequired().HasMaxLength(200);
            e.Property(x => x.RevokedReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.SkillName });
        });
    }

    // ═══ Assessments ═════════════════════════════════════════════════════════

    private static void ConfigureAssessments(ModelBuilder b)
    {
        b.Entity<AssessmentTemplate>(e =>
        {
            e.ToTable("AssessmentTemplates", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Purpose).HasMaxLength(500);
            e.HasMany(x => x.Measures).WithOne(m => m.AssessmentTemplate).HasForeignKey(m => m.AssessmentTemplateId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AssessmentMeasure>(e =>
        {
            e.ToTable("AssessmentMeasures", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Unit).IsRequired().HasMaxLength(30);
            e.Property(x => x.Grouping).HasMaxLength(80);
            e.Property(x => x.Instructions).HasMaxLength(1000);
            e.Property(x => x.CalculationNote).HasMaxLength(500);
            e.Property(x => x.DeviceFieldName).HasMaxLength(80);
            e.Property(x => x.MinValue).HasPrecision(18, 4);
            e.Property(x => x.MaxValue).HasPrecision(18, 4);
            e.Property(x => x.NormalLow).HasPrecision(18, 4);
            e.Property(x => x.NormalHigh).HasPrecision(18, 4);
        });

        b.Entity<Assessment>(e =>
        {
            e.ToTable("Assessments", DefaultSchema);
            e.Property(x => x.Summary).HasMaxLength(2000);
            e.Property(x => x.Recommendations).HasMaxLength(4000);
            e.Property(x => x.DeviceSource).HasMaxLength(80);
            e.Property(x => x.DeviceReference).HasMaxLength(120);
            e.Property(x => x.ReportUrl).HasMaxLength(1000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Values).WithOne(v => v.Assessment).HasForeignKey(v => v.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.PerformedOn });
        });

        b.Entity<AssessmentValue>(e =>
        {
            e.ToTable("AssessmentValues", DefaultSchema);
            e.Property(x => x.MeasureName).IsRequired().HasMaxLength(150);
            e.Property(x => x.Unit).HasMaxLength(30);
            e.Property(x => x.TextValue).HasMaxLength(1000);
            e.Property(x => x.NormBand).HasMaxLength(60);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.NumericValue).HasPrecision(18, 4);
            e.Property(x => x.PreviousValue).HasPrecision(18, 4);
            e.Property(x => x.Change).HasPrecision(18, 4);
            e.Property(x => x.ChangePercent).HasPrecision(9, 2);
            e.HasIndex(x => new { x.AssessmentId, x.DisplayOrder });
        });

        b.Entity<ProgressPhoto>(e =>
        {
            e.ToTable("ProgressPhotos", DefaultSchema);
            e.Property(x => x.Pose).IsRequired().HasMaxLength(40);
            e.Property(x => x.ImageUrl).IsRequired().HasMaxLength(1000);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(1000);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.TakenOn });
        });

        b.Entity<MemberGoal>(e =>
        {
            e.ToTable("Goals", DefaultSchema);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.MeasureName).HasMaxLength(150);
            e.Property(x => x.Unit).HasMaxLength(30);
            e.Property(x => x.WhyItMatters).HasMaxLength(1000);
            e.Property(x => x.StartValue).HasPrecision(18, 4);
            e.Property(x => x.TargetValue).HasPrecision(18, 4);
            e.Property(x => x.CurrentValue).HasPrecision(18, 4);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.Status });
        });

        b.Entity<NutritionPlan>(e =>
        {
            e.ToTable("NutritionPlans", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.MealGuidance).HasMaxLength(4000);
            e.Property(x => x.Restrictions).HasMaxLength(2000);
            e.Property(x => x.SupplementNotes).HasMaxLength(2000);
            e.Property(x => x.Disclaimer).HasMaxLength(2000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<HabitTracker>(e =>
        {
            e.ToTable("Habits", DefaultSchema);
            e.Property(x => x.HabitName).IsRequired().HasMaxLength(150);
            e.Property(x => x.Unit).HasMaxLength(30);
            e.Property(x => x.DailyTarget).HasPrecision(18, 4);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Entries).WithOne(v => v.HabitTracker).HasForeignKey(v => v.HabitTrackerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<HabitEntry>(e =>
        {
            e.ToTable("HabitEntries", DefaultSchema);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.HasIndex(x => new { x.HabitTrackerId, x.ForDate }).IsUnique();
        });

        b.Entity<CoachCheckIn>(e =>
        {
            e.ToTable("CoachCheckIns", DefaultSchema);
            e.Property(x => x.MemberResponse).HasMaxLength(4000);
            e.Property(x => x.CoachResponse).HasMaxLength(4000);
            e.Property(x => x.AdjustmentsMade).HasMaxLength(2000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StaffId, x.DueOn, x.IsComplete });
        });
    }

    // ═══ Sales ═══════════════════════════════════════════════════════════════

    private static void ConfigureSales(ModelBuilder b)
    {
        b.Entity<FitnessLead>(e =>
        {
            e.ToTable("Leads", DefaultSchema);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Goal).HasMaxLength(1000);
            e.Property(x => x.InterestedInPlanId).HasMaxLength(60);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.LossNote).HasMaxLength(1000);
            e.Property(x => x.WonValue).HasPrecision(18, 2);
            e.Property(x => x.AttributedCost).HasPrecision(18, 2);
            e.HasOne(x => x.LeadSource).WithMany().HasForeignKey(x => x.LeadSourceId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Activities).WithOne(a => a.Lead).HasForeignKey(a => a.LeadId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.Status }).HasDatabaseName("IX_Lead_Club_Status");
            e.HasIndex(x => new { x.AssignedStaffId, x.Status });

            // The SLA sweep.
            e.HasIndex(x => new { x.CompanyId, x.FirstContactedAt, x.ReceivedAt })
                .HasDatabaseName("IX_Lead_Sla");
        });

        b.Entity<LeadSource>(e =>
        {
            e.ToTable("LeadSources", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.TrackingCode).HasMaxLength(60);
            e.Property(x => x.MonthlyCost).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.TrackingCode });
        });

        b.Entity<LeadActivity>(e =>
        {
            e.ToTable("LeadActivities", DefaultSchema);
            e.Property(x => x.Summary).HasMaxLength(2000);
            e.Property(x => x.Outcome).HasMaxLength(500);
            e.Property(x => x.StaffName).HasMaxLength(200);
            e.HasIndex(x => new { x.LeadId, x.OccurredAt });
        });

        b.Entity<Tour>(e =>
        {
            e.ToTable("Tours", DefaultSchema);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClubId, x.ScheduledFor });
        });

        b.Entity<TrialPass>(e =>
        {
            e.ToTable("Trials", DefaultSchema);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClubId, x.EndsOn, x.Converted }).HasDatabaseName("IX_Trial_Expiring");
        });

        b.Entity<Referral>(e =>
        {
            e.ToTable("Referrals", DefaultSchema);
            e.Property(x => x.ReferredName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ReferredPhone).HasMaxLength(50);
            e.Property(x => x.ReferredEmail).HasMaxLength(200);
            e.Property(x => x.ReferralCode).HasMaxLength(60);
            e.Property(x => x.ReferrerRewardValue).HasPrecision(18, 2);
            e.Property(x => x.ReferredRewardValue).HasPrecision(18, 2);
            e.HasOne(x => x.ReferrerMember).WithMany().HasForeignKey(x => x.ReferrerMemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.CompanyId, x.ReferralCode });
            e.HasIndex(x => new { x.ReferrerMemberId, x.Converted });
        });

        b.Entity<SalesTarget>(e =>
        {
            e.ToTable("SalesTargets", DefaultSchema);
            e.Property(x => x.MetricName).IsRequired().HasMaxLength(80);
            e.Property(x => x.TargetValue).HasPrecision(18, 2);
            e.Property(x => x.ActualValue).HasPrecision(18, 2);
            e.Property(x => x.BonusOnAchievement).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ClubId, x.StaffId, x.PeriodStart });
        });

        b.Entity<LossReason>(e =>
        {
            e.ToTable("LossReasons", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Category).HasMaxLength(80);
        });
    }

    // ═══ Retention ═══════════════════════════════════════════════════════════

    private static void ConfigureRetention(ModelBuilder b)
    {
        b.Entity<ChurnScore>(e =>
        {
            e.ToTable("ChurnScores", DefaultSchema);
            e.Property(x => x.VisitsPerWeekNow).HasPrecision(9, 2);
            e.Property(x => x.VisitsPerWeekBaseline).HasPrecision(9, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Factors).WithOne(f => f.ChurnScore).HasForeignKey(f => f.ChurnScoreId).OnDelete(DeleteBehavior.Cascade);

            // One live score per member; history is in the audit, not here.
            e.HasIndex(x => x.MemberId).IsUnique().HasDatabaseName("IX_Churn_Member");
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.Band, x.Score })
                .HasDatabaseName("IX_Churn_Board");
        });

        b.Entity<ChurnFactor>(e =>
        {
            e.ToTable("ChurnFactors", DefaultSchema);
            e.Property(x => x.Explanation).IsRequired().HasMaxLength(500);
            e.Property(x => x.SuggestedAction).HasMaxLength(300);
        });

        b.Entity<RetentionTask>(e =>
        {
            e.ToTable("RetentionTasks", DefaultSchema);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.Detail).HasMaxLength(2000);
            e.Property(x => x.Trigger).HasMaxLength(120);
            e.Property(x => x.Outcome).HasMaxLength(1000);
            e.Property(x => x.DismissReason).HasMaxLength(500);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.AssignedStaffId, x.CompletedAt, x.DueOn })
                .HasDatabaseName("IX_Task_Board");
        });

        b.Entity<EngagementJourney>(e =>
        {
            e.ToTable("Journeys", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.SuccessMetric).HasMaxLength(120);
            e.HasMany(x => x.Steps).WithOne(s => s.EngagementJourney).HasForeignKey(s => s.EngagementJourneyId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.Trigger, x.IsActive });
        });

        b.Entity<JourneyStep>(e =>
        {
            e.ToTable("JourneySteps", DefaultSchema);
            e.Property(x => x.ConditionExpression).HasMaxLength(1000);
            e.Property(x => x.TagToApply).HasMaxLength(60);
            e.Property(x => x.TaskTitle).HasMaxLength(300);
            e.HasIndex(x => new { x.EngagementJourneyId, x.StepNumber }).IsUnique();
        });

        b.Entity<JourneyEnrolment>(e =>
        {
            e.ToTable("JourneyEnrolments", DefaultSchema);
            e.Property(x => x.ExitReason).HasMaxLength(300);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.NextStepDueAt, x.CompletedAt })
                .HasDatabaseName("IX_Enrolment_Due");
            e.HasIndex(x => new { x.EngagementJourneyId, x.MemberId });
        });

        b.Entity<Campaign>(e =>
        {
            e.ToTable("Campaigns", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Cost).HasPrecision(18, 2);
            e.Property(x => x.RevenueAttributed).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.SentAt });
        });

        b.Entity<MessageTemplate>(e =>
        {
            e.ToTable("MessageTemplates", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Subject).HasMaxLength(300);
            e.Property(x => x.Purpose).HasMaxLength(120);
            e.Property(x => x.LanguageCode).HasMaxLength(10);
            e.HasIndex(x => new { x.CompanyId, x.Channel, x.Purpose });
        });

        b.Entity<MessageLog>(e =>
        {
            e.ToTable("MessageLog", DefaultSchema);
            e.Property(x => x.Recipient).HasMaxLength(300);
            e.Property(x => x.Subject).HasMaxLength(300);
            e.Property(x => x.BodyPreview).HasMaxLength(500);
            e.Property(x => x.FailureReason).HasMaxLength(500);
            e.Property(x => x.ProviderReference).HasMaxLength(200);
            e.Property(x => x.Cost).HasPrecision(18, 4);
            e.HasIndex(x => new { x.MemberId, x.QueuedAt });
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.QueuedAt });
        });

        b.Entity<Segment>(e =>
        {
            e.ToTable("Segments", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.DefinitionJson).IsRequired();
        });

        b.Entity<LoyaltyAccount>(e =>
        {
            e.ToTable("LoyaltyAccounts", DefaultSchema);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tier).WithMany().HasForeignKey(x => x.TierId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.MemberId).IsUnique();
        });

        b.Entity<LoyaltyTransaction>(e =>
        {
            e.ToTable("LoyaltyTransactions", DefaultSchema);
            e.Property(x => x.Reason).IsRequired().HasMaxLength(300);
            e.Property(x => x.SourceEntityType).HasMaxLength(80);
            e.Property(x => x.RedemptionValue).HasPrecision(18, 2);
            e.HasIndex(x => new { x.MemberId, x.OccurredAt });
            e.HasIndex(x => new { x.CompanyId, x.ExpiresOn }).HasDatabaseName("IX_Loyalty_Expiry");
        });

        b.Entity<LoyaltyTier>(e =>
        {
            e.ToTable("LoyaltyTiers", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.Property(x => x.BadgeUrl).HasMaxLength(500);
            e.Property(x => x.Benefits).HasMaxLength(2000);
            e.Property(x => x.EarnMultiplier).HasPrecision(9, 4);
        });

        b.Entity<Challenge>(e =>
        {
            e.ToTable("Challenges", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Blurb).HasMaxLength(2000);
            e.Property(x => x.ImageUrl).HasMaxLength(1000);
            e.Property(x => x.CustomMetricName).HasMaxLength(120);
            e.Property(x => x.Unit).HasMaxLength(30);
            e.Property(x => x.Prize).HasMaxLength(500);
            e.Property(x => x.TargetValue).HasPrecision(18, 4);
            e.Property(x => x.EntryFee).HasPrecision(18, 2);
            e.HasMany(x => x.Participants).WithOne(p => p.Challenge).HasForeignKey(p => p.ChallengeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.StartsOn, x.EndsOn });
        });

        b.Entity<ChallengeParticipant>(e =>
        {
            e.ToTable("ChallengeParticipants", DefaultSchema);
            e.Property(x => x.TeamName).HasMaxLength(120);
            e.Property(x => x.CurrentValue).HasPrecision(18, 4);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ChallengeId, x.MemberId }).IsUnique();
        });

        b.Entity<Badge>(e =>
        {
            e.ToTable("Badges", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Blurb).HasMaxLength(500);
            e.Property(x => x.IconUrl).HasMaxLength(1000);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.Property(x => x.CriteriaDescription).HasMaxLength(500);
            e.Property(x => x.CriteriaExpression).HasMaxLength(1000);
        });

        b.Entity<MemberBadge>(e =>
        {
            e.ToTable("MemberBadges", DefaultSchema);
            e.Property(x => x.Context).HasMaxLength(300);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Badge).WithMany().HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.MemberId, x.BadgeId }).IsUnique();
        });

        b.Entity<NpsResponse>(e =>
        {
            e.ToTable("NpsResponses", DefaultSchema);
            e.Property(x => x.Band).IsRequired().HasMaxLength(20);
            e.Property(x => x.Comment).HasMaxLength(4000);
            e.Property(x => x.Trigger).HasMaxLength(80);
            e.Property(x => x.FollowUpNote).HasMaxLength(2000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.RespondedAt });
            e.HasIndex(x => new { x.ClubId, x.Band, x.FollowedUp }).HasDatabaseName("IX_Nps_Detractors");
        });

        b.Entity<Feedback>(e =>
        {
            e.ToTable("Feedback", DefaultSchema);
            e.Property(x => x.Body).IsRequired().HasMaxLength(4000);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.Channel).HasMaxLength(60);
            e.Property(x => x.ActionNote).HasMaxLength(2000);
            e.HasIndex(x => new { x.ClubId, x.IsActioned, x.SubmittedAt });
        });
    }

    // ═══ Staff ═══════════════════════════════════════════════════════════════

    private static void ConfigureStaff(ModelBuilder b)
    {
        b.Entity<FitnessStaff>(e =>
        {
            e.ToTable("Staff", DefaultSchema);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.PinHash).HasMaxLength(200);
            e.Property(x => x.AdditionalClubIds).HasMaxLength(2000);
            e.HasOne(x => x.Club).WithMany().HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Certifications).WithOne(c => c.Staff).HasForeignKey(c => c.StaffId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.IsActive });
            e.HasIndex(x => x.UserId);
        });

        b.Entity<StaffRole>(e =>
        {
            e.ToTable("StaffRoles", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Permissions).IsRequired().HasMaxLength(4000);
            e.Property(x => x.DiscountLimitPercent).HasPrecision(9, 4);
            e.Property(x => x.RefundLimit).HasPrecision(18, 2);
            e.Property(x => x.WriteOffLimit).HasPrecision(18, 2);
        });

        b.Entity<StaffCertification>(e =>
        {
            e.ToTable("Certifications", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.IssuingBody).HasMaxLength(200);
            e.Property(x => x.ReferenceNumber).HasMaxLength(80);
            e.Property(x => x.DocumentUrl).HasMaxLength(1000);
            e.HasIndex(x => new { x.CompanyId, x.ExpiresOn, x.Status }).HasDatabaseName("IX_Cert_Expiry");
        });

        b.Entity<Shift>(e =>
        {
            e.ToTable("Shifts", DefaultSchema);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Position).HasMaxLength(120);
            e.Property(x => x.RequiredCertification).HasMaxLength(120);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasMany(x => x.Assignments).WithOne(a => a.Shift).HasForeignKey(a => a.ShiftId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClubId, x.StartsAt });
        });

        b.Entity<ShiftAssignment>(e =>
        {
            e.ToTable("ShiftAssignments", DefaultSchema);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ShiftId, x.StaffId }).IsUnique();
            e.HasIndex(x => x.StaffId);
        });

        b.Entity<ShiftSwapRequest>(e =>
        {
            e.ToTable("SwapRequests", DefaultSchema);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasOne(x => x.ShiftAssignment).WithMany().HasForeignKey(x => x.ShiftAssignmentId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TimeClockEntry>(e =>
        {
            e.ToTable("TimeClock", DefaultSchema);
            e.Property(x => x.Device).HasMaxLength(120);
            e.Property(x => x.EditNote).HasMaxLength(500);
            e.Property(x => x.Latitude).HasPrecision(10, 7);
            e.Property(x => x.Longitude).HasPrecision(10, 7);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StaffId, x.ClockedInAt });
            e.HasIndex(x => new { x.ClubId, x.ClockedOutAt }).HasDatabaseName("IX_Clock_Open");
        });

        b.Entity<CommissionRule>(e =>
        {
            e.ToTable("CommissionRules", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.RatePerUnit).HasPrecision(18, 2);
            e.Property(x => x.Percentage).HasPrecision(9, 4);
            e.Property(x => x.Threshold).HasPrecision(18, 2);
            e.Property(x => x.AcceleratedRate).HasPrecision(18, 4);
            e.Property(x => x.PeriodCap).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.ClubId, x.Basis, x.IsActive });
        });

        b.Entity<CommissionAccrual>(e =>
        {
            e.ToTable("CommissionAccruals", DefaultSchema);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Narrative).HasMaxLength(500);
            e.Property(x => x.SourceEntityType).HasMaxLength(80);
            e.Property(x => x.ReversalReason).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BaseValue).HasPrecision(18, 2);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.StaffId, x.EarnedOn }).HasDatabaseName("IX_Accrual_Staff_Period");
            e.HasIndex(x => x.CommissionStatementId);
        });

        b.Entity<CommissionStatement>(e =>
        {
            e.ToTable("CommissionStatements", DefaultSchema);
            e.Property(x => x.StatementNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.RejectionNote).HasMaxLength(1000);
            e.Property(x => x.PayrollReference).HasMaxLength(80);
            e.Property(x => x.SessionCommission).HasPrecision(18, 2);
            e.Property(x => x.ClassCommission).HasPrecision(18, 2);
            e.Property(x => x.SalesCommission).HasPrecision(18, 2);
            e.Property(x => x.RetailCommission).HasPrecision(18, 2);
            e.Property(x => x.Bonus).HasPrecision(18, 2);
            e.Property(x => x.Adjustments).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.CompanyId, x.StatementNumber }).IsUnique().HasDatabaseName("IX_Statement_Tenant_Number");
            e.HasIndex(x => new { x.StaffId, x.PeriodStart });
        });

        b.Entity<StaffTarget>(e =>
        {
            e.ToTable("StaffTargets", DefaultSchema);
            e.Property(x => x.MetricName).IsRequired().HasMaxLength(80);
            e.Property(x => x.TargetValue).HasPrecision(18, 2);
            e.Property(x => x.ActualValue).HasPrecision(18, 2);
            e.Property(x => x.Bonus).HasPrecision(18, 2);
            e.HasIndex(x => new { x.StaffId, x.PeriodStart });
        });
    }

    // ═══ Facility ════════════════════════════════════════════════════════════

    private static void ConfigureFacility(ModelBuilder b)
    {
        b.Entity<LockerBank>(e =>
        {
            e.ToTable("LockerBanks", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Location).HasMaxLength(200);
            e.HasMany(x => x.Lockers).WithOne(l => l.LockerBank).HasForeignKey(l => l.LockerBankId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Locker>(e =>
        {
            e.ToTable("Lockers", DefaultSchema);
            e.Property(x => x.Number).IsRequired().HasMaxLength(20);
            e.Property(x => x.LockType).HasMaxLength(60);
            e.Property(x => x.KeyNumber).HasMaxLength(40);
            e.Property(x => x.OutOfOrderNote).HasMaxLength(500);
            e.Property(x => x.MonthlyRate).HasPrecision(18, 2);
            e.Property(x => x.AnnualRate).HasPrecision(18, 2);
            e.Property(x => x.Deposit).HasPrecision(18, 2);
            e.HasIndex(x => new { x.LockerBankId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.ClubId, x.Status });
        });

        b.Entity<LockerAssignment>(e =>
        {
            e.ToTable("LockerAssignments", DefaultSchema);
            e.Property(x => x.ReclaimNote).HasMaxLength(500);
            e.Property(x => x.KeyIssued).HasMaxLength(40);
            e.Property(x => x.Rate).HasPrecision(18, 2);
            e.Property(x => x.DepositHeld).HasPrecision(18, 2);
            e.Property(x => x.DepositReturned).HasPrecision(18, 2);
            e.HasOne(x => x.Locker).WithMany().HasForeignKey(x => x.LockerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.MemberId, x.ReleasedOn });
            e.HasIndex(x => new { x.CompanyId, x.EndsOn, x.ReleasedOn }).HasDatabaseName("IX_Locker_Expiry");
        });

        b.Entity<BookableResource>(e =>
        {
            e.ToTable("Resources", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.ColourHex).HasMaxLength(9);
            e.Property(x => x.OutOfServiceNote).HasMaxLength(500);
            e.Property(x => x.MemberRate).HasPrecision(18, 2);
            e.Property(x => x.NonMemberRate).HasPrecision(18, 2);
            e.Property(x => x.PeakSurcharge).HasPrecision(18, 2);
            e.HasMany(x => x.SlotRules).WithOne(r => r.BookableResource).HasForeignKey(r => r.BookableResourceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClubId, x.Kind, x.DisplayOrder });
        });

        b.Entity<ResourceSlotRule>(e =>
        {
            e.ToTable("ResourceSlotRules", DefaultSchema);
            e.Property(x => x.BlockReason).HasMaxLength(300);
            e.Property(x => x.RateOverride).HasPrecision(18, 2);
        });

        b.Entity<ResourceBooking>(e =>
        {
            e.ToTable("ResourceBookings", DefaultSchema);
            e.Property(x => x.BookingNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.GuestName).HasMaxLength(200);
            e.Property(x => x.GuestPhone).HasMaxLength(50);
            e.Property(x => x.ParticipantMemberIds).HasMaxLength(2000);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.PenaltyCharged).HasPrecision(18, 2);
            e.HasOne(x => x.BookableResource).WithMany().HasForeignKey(x => x.BookableResourceId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.BookableResourceId, x.StartsAt }).HasDatabaseName("IX_ResourceBooking_Slot");
            e.HasIndex(x => new { x.MemberId, x.StartsAt });
            e.HasIndex(x => new { x.CompanyId, x.BookingNumber }).IsUnique().HasDatabaseName("IX_ResourceBooking_Number");
        });

        b.Entity<EquipmentAsset>(e =>
        {
            e.ToTable("Equipment", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.Manufacturer).HasMaxLength(150);
            e.Property(x => x.Model).HasMaxLength(150);
            e.Property(x => x.SerialNumber).HasMaxLength(120);
            e.Property(x => x.AssetTag).HasMaxLength(60);
            e.Property(x => x.ServiceContractReference).HasMaxLength(120);
            e.Property(x => x.QrCode).HasMaxLength(120);
            e.Property(x => x.OutOfServiceNote).HasMaxLength(500);
            e.Property(x => x.PurchaseCost).HasPrecision(18, 2);
            e.Property(x => x.DisposalValue).HasPrecision(18, 2);
            e.Property(x => x.UsageHours).HasPrecision(12, 2);
            e.Property(x => x.TotalMaintenanceCost).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.QrCode }).IsUnique().HasFilter("\"QrCode\" IS NOT NULL")
                .HasDatabaseName("IX_Equipment_Qr");
            e.HasIndex(x => new { x.ClubId, x.Status });
        });

        b.Entity<MaintenanceSchedule>(e =>
        {
            e.ToTable("MaintenanceSchedules", DefaultSchema);
            e.Property(x => x.TaskName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Instructions).HasMaxLength(2000);
            e.Property(x => x.AppliesToCategory).HasMaxLength(80);
            e.Property(x => x.IntervalUsageHours).HasPrecision(12, 2);
            e.HasOne(x => x.EquipmentAsset).WithMany().HasForeignKey(x => x.EquipmentAssetId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.NextDueOn, x.IsActive }).HasDatabaseName("IX_Maintenance_Due");
        });

        b.Entity<WorkOrder>(e =>
        {
            e.ToTable("WorkOrders", DefaultSchema);
            e.Property(x => x.WorkOrderNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.Detail).HasMaxLength(4000);
            e.Property(x => x.ContractorName).HasMaxLength(200);
            e.Property(x => x.ContractorReference).HasMaxLength(120);
            e.Property(x => x.PartsUsed).HasMaxLength(2000);
            e.Property(x => x.ResolutionNote).HasMaxLength(2000);
            e.Property(x => x.PhotoUrls).HasMaxLength(4000);
            e.Property(x => x.LabourCost).HasPrecision(18, 2);
            e.Property(x => x.PartsCost).HasPrecision(18, 2);
            e.Property(x => x.TotalCost).HasPrecision(18, 2);
            e.HasOne(x => x.EquipmentAsset).WithMany().HasForeignKey(x => x.EquipmentAssetId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.CompanyId, x.WorkOrderNumber }).IsUnique().HasDatabaseName("IX_WorkOrder_Number");
            e.HasIndex(x => new { x.ClubId, x.Status, x.Priority });
        });

        b.Entity<FaultReport>(e =>
        {
            e.ToTable("FaultReports", DefaultSchema);
            e.Property(x => x.FaultDescription).IsRequired().HasMaxLength(2000);
            e.Property(x => x.PhotoUrl).HasMaxLength(1000);
            e.HasIndex(x => new { x.ClubId, x.IsResolved, x.ReportedAt });
        });

        b.Entity<EquipmentUsageLog>(e =>
        {
            e.ToTable("EquipmentUsage", DefaultSchema);
            e.Property(x => x.Source).HasMaxLength(80);
            e.Property(x => x.CumulativeHours).HasPrecision(12, 2);
            e.Property(x => x.HoursSinceLastRead).HasPrecision(12, 2);
            e.Property(x => x.DistanceKm).HasPrecision(12, 2);
            e.HasOne(x => x.EquipmentAsset).WithMany().HasForeignKey(x => x.EquipmentAssetId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EquipmentAssetId, x.ReadOn });
        });
    }

    // ═══ Compliance ══════════════════════════════════════════════════════════

    private static void ConfigureCompliance(ModelBuilder b)
    {
        b.Entity<WaiverTemplate>(e =>
        {
            e.ToTable("WaiverTemplates", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.BodyHtml).IsRequired();
            e.Property(x => x.CountryCode).HasMaxLength(3);
            e.Property(x => x.LanguageCode).HasMaxLength(10);
            e.Property(x => x.ActivityScope).HasMaxLength(200);
            e.HasIndex(x => new { x.CompanyId, x.Name, x.Version }).IsUnique();
        });

        b.Entity<WaiverSignature>(e =>
        {
            e.ToTable("WaiverSignatures", DefaultSchema);
            e.Property(x => x.SignerName).HasMaxLength(200);
            e.Property(x => x.SignerEmail).HasMaxLength(200);
            e.Property(x => x.SignerPhone).HasMaxLength(50);
            e.Property(x => x.GuardianName).HasMaxLength(200);
            e.Property(x => x.GuardianRelationship).HasMaxLength(80);
            e.Property(x => x.GuardianSignatureUrl).HasMaxLength(1000);
            e.Property(x => x.SignatureImageUrl).HasMaxLength(1000);
            e.Property(x => x.DocumentUrl).HasMaxLength(1000);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.CapturedVia).HasMaxLength(80);
            e.Property(x => x.RemoteToken).HasMaxLength(120);
            e.HasOne(x => x.WaiverTemplate).WithMany().HasForeignKey(x => x.WaiverTemplateId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.Status });
        });

        b.Entity<HealthScreening>(e =>
        {
            e.ToTable("HealthScreenings", DefaultSchema);
            e.Property(x => x.TemplateName).IsRequired().HasMaxLength(120);
            e.Property(x => x.RiskSummary).HasMaxLength(1000);
            e.Property(x => x.ReviewNote).HasMaxLength(2000);
            e.Property(x => x.CapturedVia).HasMaxLength(80);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Answers).WithOne(a => a.HealthScreening).HasForeignKey(a => a.HealthScreeningId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.CompletedAt });
        });

        b.Entity<HealthScreeningAnswer>(e =>
        {
            e.ToTable("ScreeningAnswers", DefaultSchema);
            e.Property(x => x.QuestionText).IsRequired().HasMaxLength(1000);
            e.Property(x => x.TextAnswer).HasMaxLength(2000);
            e.Property(x => x.FollowUpAnswer).HasMaxLength(2000);
            e.Property(x => x.NumericAnswer).HasPrecision(18, 4);
        });

        b.Entity<MedicalClearance>(e =>
        {
            e.ToTable("Clearances", DefaultSchema);
            e.Property(x => x.PractitionerName).HasMaxLength(200);
            e.Property(x => x.PractitionerRegistration).HasMaxLength(80);
            e.Property(x => x.PracticeName).HasMaxLength(200);
            e.Property(x => x.Restrictions).HasMaxLength(2000);
            e.Property(x => x.RejectionReason).HasMaxLength(1000);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MemberId, x.Status });
        });

        b.Entity<Incident>(e =>
        {
            e.ToTable("Incidents", DefaultSchema);
            e.Property(x => x.IncidentNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.Summary).IsRequired().HasMaxLength(1000);
            e.Property(x => x.Detail).HasMaxLength(8000);
            e.Property(x => x.InvolvedPersonName).HasMaxLength(200);
            e.Property(x => x.InvolvedPersonPhone).HasMaxLength(50);
            e.Property(x => x.WitnessNames).HasMaxLength(1000);
            e.Property(x => x.WitnessStatements).HasMaxLength(8000);
            e.Property(x => x.FirstAiderName).HasMaxLength(200);
            e.Property(x => x.ImmediateAction).HasMaxLength(4000);
            e.Property(x => x.PhotoUrls).HasMaxLength(4000);
            e.Property(x => x.RootCause).HasMaxLength(2000);
            e.Property(x => x.PreventiveAction).HasMaxLength(2000);
            e.Property(x => x.AuthorityReference).HasMaxLength(120);
            e.Property(x => x.InsurerReference).HasMaxLength(120);
            e.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            e.HasMany(x => x.Actions).WithOne(a => a.Incident).HasForeignKey(a => a.IncidentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.IncidentNumber }).IsUnique().HasDatabaseName("IX_Incident_Number");
            e.HasIndex(x => new { x.ClubId, x.Status, x.OccurredAt });
        });

        b.Entity<IncidentAction>(e =>
        {
            e.ToTable("IncidentActions", DefaultSchema);
            e.Property(x => x.Action).IsRequired().HasMaxLength(1000);
            e.Property(x => x.CompletionNote).HasMaxLength(2000);
        });

        b.Entity<LostPropertyItem>(e =>
        {
            e.ToTable("LostProperty", DefaultSchema);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.PhotoUrl).HasMaxLength(1000);
            e.Property(x => x.FoundLocation).HasMaxLength(200);
            e.Property(x => x.StorageLocation).HasMaxLength(200);
            e.Property(x => x.ClaimedByName).HasMaxLength(200);
            e.Property(x => x.DisposalNote).HasMaxLength(500);
            e.HasIndex(x => new { x.ClubId, x.Status, x.FoundOn });
        });

        b.Entity<Complaint>(e =>
        {
            e.ToTable("Complaints", DefaultSchema);
            e.Property(x => x.ComplaintNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.Category).IsRequired().HasMaxLength(80);
            e.Property(x => x.Summary).IsRequired().HasMaxLength(1000);
            e.Property(x => x.Detail).HasMaxLength(8000);
            e.Property(x => x.ComplainantName).HasMaxLength(200);
            e.Property(x => x.ComplainantContact).HasMaxLength(200);
            e.Property(x => x.Resolution).HasMaxLength(4000);
            e.Property(x => x.CompensationNote).HasMaxLength(1000);
            e.Property(x => x.Channel).HasMaxLength(60);
            e.Property(x => x.CompensationValue).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.CompanyId, x.ComplaintNumber }).IsUnique().HasDatabaseName("IX_Complaint_Number");
            e.HasIndex(x => new { x.ClubId, x.Status });
        });

        b.Entity<FacilityCheck>(e =>
        {
            e.ToTable("FacilityChecks", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasMany(x => x.Items).WithOne(i => i.FacilityCheck).HasForeignKey(i => i.FacilityCheckId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ClubId, x.IsActive });
        });

        b.Entity<FacilityCheckItem>(e =>
        {
            e.ToTable("FacilityCheckItems", DefaultSchema);
            e.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.Unit).HasMaxLength(30);
            e.Property(x => x.LastNote).HasMaxLength(1000);
            e.Property(x => x.AcceptableLow).HasPrecision(18, 4);
            e.Property(x => x.AcceptableHigh).HasPrecision(18, 4);
            e.Property(x => x.LastValue).HasPrecision(18, 4);
        });

        b.Entity<ShiftHandover>(e =>
        {
            e.ToTable("Handovers", DefaultSchema);
            e.Property(x => x.Notes).IsRequired().HasMaxLength(8000);
            e.Property(x => x.OutstandingItems).HasMaxLength(4000);
            e.HasIndex(x => new { x.ClubId, x.ShiftEndedAt });
        });

        b.Entity<Announcement>(e =>
        {
            e.ToTable("Announcements", DefaultSchema);
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.Body).IsRequired().HasMaxLength(4000);
            e.Property(x => x.ImageUrl).HasMaxLength(1000);
            e.HasIndex(x => new { x.ClubId, x.IsPublished, x.ShowFrom });
        });

        b.Entity<AuditEntry>(e =>
        {
            e.ToTable("Audit", DefaultSchema);
            e.Property(x => x.Action).IsRequired().HasMaxLength(60);
            e.Property(x => x.EntityType).IsRequired().HasMaxLength(80);
            e.Property(x => x.ActorName).HasMaxLength(200);
            e.Property(x => x.ChangeSummary).HasMaxLength(4000);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.HasIndex(x => new { x.CompanyId, x.OccurredAt });
            e.HasIndex(x => new { x.MemberId, x.OccurredAt });
            e.HasIndex(x => new { x.CompanyId, x.IsSensitiveAccess, x.OccurredAt })
                .HasDatabaseName("IX_Audit_Sensitive");
        });
    }

    // ═══ Commerce ════════════════════════════════════════════════════════════

    private static void ConfigureCommerce(ModelBuilder b)
    {
        b.Entity<CorporateAccount>(e =>
        {
            e.ToTable("CorporateAccounts", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.ContactName).HasMaxLength(200);
            e.Property(x => x.ContactEmail).HasMaxLength(200);
            e.Property(x => x.ContactPhone).HasMaxLength(50);
            e.Property(x => x.AddressLine).HasMaxLength(500);
            e.Property(x => x.TaxRegistrationNumber).HasMaxLength(60);
            e.Property(x => x.NegotiatedRate).HasPrecision(18, 2);
            e.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            e.Property(x => x.SubsidyPerMember).HasPrecision(18, 2);
            e.Property(x => x.SubsidyPercent).HasPrecision(9, 4);
            e.HasMany(x => x.EligibilityRules).WithOne(r => r.CorporateAccount).HasForeignKey(r => r.CorporateAccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Members).WithOne(m => m.CorporateAccount).HasForeignKey(m => m.CorporateAccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("IX_Corporate_Tenant_Code");
        });

        b.Entity<CorporateEligibilityRule>(e =>
        {
            e.ToTable("EligibilityRules", DefaultSchema);
            e.Property(x => x.MatchValue).HasMaxLength(300);
        });

        b.Entity<CorporateMember>(e =>
        {
            e.ToTable("CorporateMembers", DefaultSchema);
            e.Property(x => x.EmployeeReference).HasMaxLength(80);
            e.Property(x => x.Department).HasMaxLength(150);
            e.Property(x => x.LeaveReason).HasMaxLength(300);
            e.Property(x => x.EmployerContribution).HasPrecision(18, 2);
            e.Property(x => x.EmployeeContribution).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.CorporateAccountId, x.MemberId }).IsUnique();
        });

        b.Entity<CorporateInvoice>(e =>
        {
            e.ToTable("CorporateInvoices", DefaultSchema);
            e.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.BalanceDue).HasPrecision(18, 2);
            e.Property(x => x.BreakdownUrl).HasMaxLength(1000);
            e.Property(x => x.DocumentUrl).HasMaxLength(1000);
            e.Property(x => x.PurchaseOrderReference).HasMaxLength(120);
            e.HasIndex(x => new { x.CompanyId, x.InvoiceNumber }).IsUnique().HasDatabaseName("IX_CorpInvoice_Number");
        });

        b.Entity<ThirdPartyPayer>(e =>
        {
            e.ToTable("Payers", DefaultSchema);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.PayerType).HasMaxLength(80);
            e.Property(x => x.ContactName).HasMaxLength(200);
            e.Property(x => x.ContactEmail).HasMaxLength(200);
            e.Property(x => x.ContactPhone).HasMaxLength(50);
            e.Property(x => x.AgreedRate).HasPrecision(18, 2);
        });

        b.Entity<PayerAuthorisation>(e =>
        {
            e.ToTable("Authorisations", DefaultSchema);
            e.Property(x => x.AuthorisationNumber).IsRequired().HasMaxLength(80);
            e.Property(x => x.Purpose).HasMaxLength(500);
            e.Property(x => x.ReferrerName).HasMaxLength(200);
            e.Property(x => x.RatePerUnit).HasPrecision(18, 2);
            e.Property(x => x.ApprovedValue).HasPrecision(18, 2);
            e.Property(x => x.InvoicedValue).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ThirdPartyPayerId, x.AuthorisationNumber }).IsUnique();
        });

        b.Entity<FitnessSale>(e =>
        {
            e.ToTable("Sales", DefaultSchema);
            e.Property(x => x.SaleNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.ReturnReason).HasMaxLength(500);
            e.Property(x => x.DiscountReason).HasMaxLength(500);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Lines).WithOne(l => l.Sale).HasForeignKey(l => l.SaleId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.SaleNumber }).IsUnique().HasDatabaseName("IX_Sale_Tenant_Number");
            e.HasIndex(x => new { x.ClubId, x.SoldAt });
        });

        b.Entity<FitnessSaleLine>(e =>
        {
            e.ToTable("SaleLines", DefaultSchema);
            e.Property(x => x.ItemName).IsRequired().HasMaxLength(300);
            e.Property(x => x.Barcode).HasMaxLength(60);
            e.Property(x => x.Modifiers).HasMaxLength(500);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxPercent).HasPrecision(9, 4);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
        });

        b.Entity<HouseAccountCharge>(e =>
        {
            e.ToTable("HouseAccountCharges", DefaultSchema);
            e.Property(x => x.ChargeDescription).IsRequired().HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.MemberId, x.IsSettled });
        });

        b.Entity<VendingRevenueEntry>(e =>
        {
            e.ToTable("VendingRevenue", DefaultSchema);
            e.Property(x => x.RevenueSource).IsRequired().HasMaxLength(120);
            e.Property(x => x.MachineReference).HasMaxLength(120);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.Property(x => x.GrossRevenue).HasPrecision(18, 2);
            e.Property(x => x.CommissionPaid).HasPrecision(18, 2);
            e.Property(x => x.NetRevenue).HasPrecision(18, 2);
            e.HasIndex(x => new { x.ClubId, x.PeriodStart });
        });

        b.Entity<CashSession>(e =>
        {
            e.ToTable("CashSessions", DefaultSchema);
            e.Property(x => x.SessionNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.VarianceNote).HasMaxLength(1000);
            e.Property(x => x.OpeningFloat).HasPrecision(18, 2);
            e.Property(x => x.CashSales).HasPrecision(18, 2);
            e.Property(x => x.CardSales).HasPrecision(18, 2);
            e.Property(x => x.OtherSales).HasPrecision(18, 2);
            e.Property(x => x.Refunds).HasPrecision(18, 2);
            e.Property(x => x.PaidIn).HasPrecision(18, 2);
            e.Property(x => x.PaidOut).HasPrecision(18, 2);
            e.Property(x => x.Drops).HasPrecision(18, 2);
            e.Property(x => x.ExpectedCash).HasPrecision(18, 2);
            e.Property(x => x.CountedCash).HasPrecision(18, 2);
            e.Property(x => x.Variance).HasPrecision(18, 2);
            e.HasMany(x => x.Movements).WithOne(m => m.CashSession).HasForeignKey(m => m.CashSessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.SessionNumber }).IsUnique().HasDatabaseName("IX_CashSession_Number");
            e.HasIndex(x => new { x.ClubId, x.Status, x.OpenedAt });
        });

        b.Entity<CashMovement>(e =>
        {
            e.ToTable("CashMovements", DefaultSchema);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.Reference).HasMaxLength(120);
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });
    }

    /// <summary>
    /// Conventions applied to every entity, so the fifteen configure methods above only have to
    /// state what is different rather than restating the same six lines a hundred and fifty times.
    /// </summary>
    private static void ApplyConventions(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes())
        {
            if (!typeof(Nexcore.SharedKernel.BaseEntity).IsAssignableFrom(entity.ClrType)) continue;

            var builder = b.Entity(entity.ClrType);

            // PostgreSQL maintains xmin itself; EF only has to know to check it.
            builder.Property(nameof(Nexcore.SharedKernel.BaseEntity.RowVersion))
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.Property(nameof(Nexcore.SharedKernel.BaseEntity.Description)).HasMaxLength(1000);
            builder.Property(nameof(Nexcore.SharedKernel.BaseEntity.Code)).HasMaxLength(50);

            // Every read filters on tenant and soft-delete, so every table is indexed for it.
            builder.HasIndex(
                nameof(Nexcore.SharedKernel.BaseEntity.CompanyId),
                nameof(Nexcore.SharedKernel.BaseEntity.BranchId),
                nameof(Nexcore.SharedKernel.BaseEntity.BusinessUnitId),
                nameof(Nexcore.SharedKernel.BaseEntity.IsDeleted));
        }
    }
}
