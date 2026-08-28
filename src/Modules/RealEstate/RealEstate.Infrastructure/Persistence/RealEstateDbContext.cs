using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Persistence;
using RealEstate.Domain.Entities;

namespace RealEstate.Infrastructure.Persistence;

/// <summary>
/// Real Estate module DbContext. Default schema: <c>realestate</c>.
///
/// The model is organised the way the four businesses are: the land and the projects built on it,
/// the properties that result, the people who buy and rent them, the money that collects itself
/// every month, the buildings that have to be run afterwards, and the construction that produced
/// them.
///
/// **Relationships are deliberately sparse.** Almost every cross-aggregate reference in this module
/// is a bare <see cref="Guid"/> with no navigation property — a booking points at a project, a unit,
/// a party and a payment plan, and none of those is a foreign key. That is not an oversight. A
/// property record outlives the project it was built in, a party outlives every booking they ever
/// made, and a unit's ownership chain has to survive the deletion of the price list it was sold on.
/// Composition — a booking and its charge lines, a BOQ and its lines, an inspection and its snags —
/// *does* get a real relationship, and cascades. Everything else is referenced by id and resolved
/// by the service layer, so no delete can ever take history with it.
///
/// Column types are applied by convention in <see cref="ApplyConventions"/> rather than declared
/// four hundred times: money is <c>numeric(18,2)</c>, percentages <c>numeric(9,4)</c>, quantities
/// <c>numeric(18,4)</c> and coordinates <c>numeric(10,7)</c>. Explicit configuration below is
/// reserved for the hierarchies EF cannot infer and the indexes that carry the hot reads.
/// </summary>
public class RealEstateDbContext : DbContext
{
    private const string DefaultSchema = "realestate";

    public RealEstateDbContext(DbContextOptions<RealEstateDbContext> options) : base(options) { }

    // ── Organisation & configuration ────────────────────────────────────────────
    public DbSet<RealEstateSettings> Settings { get; set; } = null!;
    public DbSet<RealEstateOffice> RealEstateOffices { get; set; } = null!;
    public DbSet<OfficeHoliday> OfficeHolidays { get; set; } = null!;
    public DbSet<GeoArea> GeoAreas { get; set; } = null!;
    public DbSet<Territory> Territories { get; set; } = null!;
    public DbSet<TerritoryArea> TerritoryAreas { get; set; } = null!;
    public DbSet<TerritoryAssignment> TerritoryAssignments { get; set; } = null!;
    public DbSet<AgentProfile> AgentProfiles { get; set; } = null!;
    public DbSet<AgentLicence> AgentLicences { get; set; } = null!;
    public DbSet<AgentAvailability> AgentAvailabilities { get; set; } = null!;
    public DbSet<SalesTeam> SalesTeams { get; set; } = null!;
    public DbSet<SalesTeamMember> SalesTeamMembers { get; set; } = null!;
    public DbSet<ReasonCode> ReasonCodes { get; set; } = null!;
    public DbSet<ApprovalMatrix> ApprovalMatrices { get; set; } = null!;
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; } = null!;
    public DbSet<ApprovalDecisionLog> ApprovalDecisionLogs { get; set; } = null!;
    public DbSet<SavedView> SavedViews { get; set; } = null!;

    // ── Property master ─────────────────────────────────────────────────────────
    public DbSet<Property> Properties { get; set; } = null!;
    public DbSet<PropertyFeature> PropertyFeatures { get; set; } = null!;
    public DbSet<PropertyMedia> PropertyMedia { get; set; } = null!;
    public DbSet<PropertyOwnership> PropertyOwnerships { get; set; } = null!;
    public DbSet<PropertyRelationship> PropertyRelationships { get; set; } = null!;
    public DbSet<PropertyStatusHistory> PropertyStatusHistories { get; set; } = null!;
    public DbSet<PropertyNote> PropertyNotes { get; set; } = null!;
    public DbSet<PropertyDocument> PropertyDocuments { get; set; } = null!;
    public DbSet<PropertyValuation> PropertyValuations { get; set; } = null!;
    public DbSet<PropertyComparable> PropertyComparables { get; set; } = null!;
    public DbSet<PropertyTaxRecord> PropertyTaxRecords { get; set; } = null!;

    // ── Land bank & title ───────────────────────────────────────────────────────
    public DbSet<LandParcel> LandParcels { get; set; } = null!;
    public DbSet<TitleChainEntry> TitleChainEntries { get; set; } = null!;
    public DbSet<Encumbrance> Encumbrances { get; set; } = null!;
    public DbSet<TitleVerificationItem> TitleVerificationItems { get; set; } = null!;
    public DbSet<LandAcquisition> LandAcquisitions { get; set; } = null!;
    public DbSet<AcquisitionStage> AcquisitionStages { get; set; } = null!;
    public DbSet<AcquisitionCostLine> AcquisitionCostLines { get; set; } = null!;
    public DbSet<SurveyInstruction> SurveyInstructions { get; set; } = null!;
    public DbSet<LandUseRecord> LandUseRecords { get; set; } = null!;

    // ── Projects & inventory ────────────────────────────────────────────────────
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectNode> ProjectNodes { get; set; } = null!;
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<UnitPremium> UnitPremiums { get; set; } = null!;
    public DbSet<UnitHold> UnitHolds { get; set; } = null!;
    public DbSet<UnitBlockRecord> UnitBlockRecords { get; set; } = null!;
    public DbSet<UnitStatusHistory> UnitStatusHistories { get; set; } = null!;
    public DbSet<PriceList> PriceLists { get; set; } = null!;
    public DbSet<PriceListLine> PriceListLines { get; set; } = null!;
    public DbSet<PremiumCharge> PremiumCharges { get; set; } = null!;
    public DbSet<ProjectMilestone> ProjectMilestones { get; set; } = null!;
    public DbSet<ProjectTeamMember> ProjectTeamMembers { get; set; } = null!;
    public DbSet<ProjectBudgetLine> ProjectBudgetLines { get; set; } = null!;
    public DbSet<SitePlan> SitePlans { get; set; } = null!;
    public DbSet<SitePlanShape> SitePlanShapes { get; set; } = null!;
    public DbSet<PlotFile> PlotFiles { get; set; } = null!;

    // ── Listings & marketing ────────────────────────────────────────────────────
    public DbSet<Listing> Listings { get; set; } = null!;
    public DbSet<ListingPriceHistory> ListingPriceHistories { get; set; } = null!;
    public DbSet<Instruction> Instructions { get; set; } = null!;
    public DbSet<InstructionTerm> InstructionTerms { get; set; } = null!;
    public DbSet<PortalChannel> PortalChannels { get; set; } = null!;
    public DbSet<PortalMapping> PortalMappings { get; set; } = null!;
    public DbSet<PortalPublication> PortalPublications { get; set; } = null!;
    public DbSet<PortalPublishLog> PortalPublishLogs { get; set; } = null!;
    public DbSet<PortalLeadRef> PortalLeadRefs { get; set; } = null!;
    public DbSet<Campaign> Campaigns { get; set; } = null!;
    public DbSet<CampaignCost> CampaignCosts { get; set; } = null!;
    public DbSet<CampaignAttribution> CampaignAttributions { get; set; } = null!;
    public DbSet<MarketingEvent> MarketingEvents { get; set; } = null!;
    public DbSet<EventRegistration> EventRegistrations { get; set; } = null!;
    public DbSet<ContentAsset> ContentAssets { get; set; } = null!;

    // ── People & KYC ────────────────────────────────────────────────────────────
    public DbSet<Party> Parties { get; set; } = null!;
    public DbSet<PartyRole> PartyRoles { get; set; } = null!;
    public DbSet<PartyContact> PartyContacts { get; set; } = null!;
    public DbSet<PartyAddress> PartyAddresses { get; set; } = null!;
    public DbSet<PartyIdentity> PartyIdentities { get; set; } = null!;
    public DbSet<KycCase> KycCases { get; set; } = null!;
    public DbSet<KycDocument> KycDocuments { get; set; } = null!;
    public DbSet<PartyRelationship> PartyRelationships { get; set; } = null!;
    public DbSet<PartyConsent> PartyConsents { get; set; } = null!;
    public DbSet<PartyPreference> PartyPreferences { get; set; } = null!;
    public DbSet<CautionListEntry> CautionListEntries { get; set; } = null!;

    // ── CRM ─────────────────────────────────────────────────────────────────────
    public DbSet<Enquiry> Enquiries { get; set; } = null!;
    public DbSet<EnquiryStageHistory> EnquiryStageHistories { get; set; } = null!;
    public DbSet<RequirementProfile> RequirementProfiles { get; set; } = null!;
    public DbSet<RequirementArea> RequirementAreas { get; set; } = null!;
    public DbSet<SavedSearch> SavedSearches { get; set; } = null!;
    public DbSet<MatchResult> MatchResults { get; set; } = null!;
    public DbSet<Activity> Activities { get; set; } = null!;
    public DbSet<FollowUpTask> FollowUpTasks { get; set; } = null!;

    // ── Viewings & visits ───────────────────────────────────────────────────────
    public DbSet<Viewing> Viewings { get; set; } = null!;
    public DbSet<ViewingProperty> ViewingProperties { get; set; } = null!;
    public DbSet<ViewingAttendee> ViewingAttendees { get; set; } = null!;
    public DbSet<ViewingFeedback> ViewingFeedbacks { get; set; } = null!;
    public DbSet<SiteVisit> SiteVisits { get; set; } = null!;
    public DbSet<SiteVisitTransport> SiteVisitTransports { get; set; } = null!;
    public DbSet<SiteVisitFeedback> SiteVisitFeedbacks { get; set; } = null!;
    public DbSet<KeySet> KeySets { get; set; } = null!;
    public DbSet<KeyMovement> KeyMovements { get; set; } = null!;
    public DbSet<AccessNotice> AccessNotices { get; set; } = null!;
    public DbSet<OpenHouse> OpenHouses { get; set; } = null!;
    public DbSet<OpenHouseSlot> OpenHouseSlots { get; set; } = null!;

    // ── Offers, bookings & allotment ────────────────────────────────────────────
    public DbSet<Offer> Offers { get; set; } = null!;
    public DbSet<OfferCondition> OfferConditions { get; set; } = null!;
    public DbSet<OfferCounter> OfferCounters { get; set; } = null!;
    public DbSet<ExpressionOfInterest> ExpressionOfInterests { get; set; } = null!;
    public DbSet<TokenReservation> TokenReservations { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<BookingApplicant> BookingApplicants { get; set; } = null!;
    public DbSet<BookingChargeLine> BookingChargeLines { get; set; } = null!;
    public DbSet<BookingDiscount> BookingDiscounts { get; set; } = null!;
    public DbSet<BookingStatusHistory> BookingStatusHistories { get; set; } = null!;
    public DbSet<BookingAmendment> BookingAmendments { get; set; } = null!;
    public DbSet<Allotment> Allotments { get; set; } = null!;
    public DbSet<Ballot> Ballots { get; set; } = null!;
    public DbSet<BallotCategory> BallotCategories { get; set; } = null!;
    public DbSet<BallotEntry> BallotEntries { get; set; } = null!;
    public DbSet<BallotResult> BallotResults { get; set; } = null!;
    public DbSet<SaleAgreement> SaleAgreements { get; set; } = null!;
    public DbSet<AgreementClause> AgreementClauses { get; set; } = null!;

    // ── Payment plans & money in ────────────────────────────────────────────────
    public DbSet<PaymentPlanTemplate> PaymentPlanTemplates { get; set; } = null!;
    public DbSet<PaymentPlanTemplateLine> PaymentPlanTemplateLines { get; set; } = null!;
    public DbSet<PaymentPlan> PaymentPlans { get; set; } = null!;
    public DbSet<Instalment> Instalments { get; set; } = null!;
    public DbSet<Demand> Demands { get; set; } = null!;
    public DbSet<DemandLine> DemandLines { get; set; } = null!;
    public DbSet<DemandBatch> DemandBatches { get; set; } = null!;
    public DbSet<SurchargePolicy> SurchargePolicies { get; set; } = null!;
    public DbSet<SurchargeAccrual> SurchargeAccruals { get; set; } = null!;
    public DbSet<SurchargeWaiver> SurchargeWaivers { get; set; } = null!;
    public DbSet<Receipt> Receipts { get; set; } = null!;
    public DbSet<ReceiptAllocation> ReceiptAllocations { get; set; } = null!;
    public DbSet<ChequeRecord> ChequeRecords { get; set; } = null!;
    public DbSet<CustomerLedgerEntry> CustomerLedgerEntries { get; set; } = null!;
    public DbSet<PromiseToPay> PromiseToPays { get; set; } = null!;
    public DbSet<DunningPolicy> DunningPolicies { get; set; } = null!;
    public DbSet<DunningStep> DunningSteps { get; set; } = null!;
    public DbSet<DunningCase> DunningCases { get; set; } = null!;
    public DbSet<DunningEvent> DunningEvents { get; set; } = null!;
    public DbSet<LegalNotice> LegalNotices { get; set; } = null!;
    public DbSet<WriteOff> WriteOffs { get; set; } = null!;

    // ── Cancellation, transfer & possession ─────────────────────────────────────
    public DbSet<Cancellation> Cancellations { get; set; } = null!;
    public DbSet<DeductionPolicy> DeductionPolicies { get; set; } = null!;
    public DbSet<DeductionSlab> DeductionSlabs { get; set; } = null!;
    public DbSet<DeductionLine> DeductionLines { get; set; } = null!;
    public DbSet<RefundRequest> RefundRequests { get; set; } = null!;
    public DbSet<RefundSchedule> RefundSchedules { get; set; } = null!;
    public DbSet<ResaleRequest> ResaleRequests { get; set; } = null!;
    public DbSet<TransferRequest> TransferRequests { get; set; } = null!;
    public DbSet<TransferParty> TransferParties { get; set; } = null!;
    public DbSet<TransferFeeLine> TransferFeeLines { get; set; } = null!;
    public DbSet<DuesClearance> DuesClearances { get; set; } = null!;
    public DbSet<TransferSession> TransferSessions { get; set; } = null!;
    public DbSet<TransferWitness> TransferWitnesses { get; set; } = null!;
    public DbSet<OwnershipChainEntry> OwnershipChainEntries { get; set; } = null!;
    public DbSet<DuplicateFileRequest> DuplicateFileRequests { get; set; } = null!;
    public DbSet<PossessionOffer> PossessionOffers { get; set; } = null!;
    public DbSet<PossessionChecklistItem> PossessionChecklistItems { get; set; } = null!;
    public DbSet<Handover> Handovers { get; set; } = null!;
    public DbSet<HandoverItem> HandoverItems { get; set; } = null!;
    public DbSet<SnagInspection> SnagInspections { get; set; } = null!;
    public DbSet<Snag> Snags { get; set; } = null!;
    public DbSet<SnagPhoto> SnagPhotos { get; set; } = null!;
    public DbSet<PunchList> PunchLists { get; set; } = null!;
    public DbSet<DefectLiability> DefectLiabilities { get; set; } = null!;
    public DbSet<DefectClaim> DefectClaims { get; set; } = null!;

    // ── Deals & transaction progression ─────────────────────────────────────────
    public DbSet<Deal> Deals { get; set; } = null!;
    public DbSet<DealParty> DealParties { get; set; } = null!;
    public DbSet<DealChecklistItem> DealChecklistItems { get; set; } = null!;
    public DbSet<DealMilestone> DealMilestones { get; set; } = null!;
    public DbSet<SalesChain> SalesChains { get; set; } = null!;
    public DbSet<SalesChainLink> SalesChainLinks { get; set; } = null!;
    public DbSet<FallThroughRecord> FallThroughRecords { get; set; } = null!;
    public DbSet<Conveyancing> Conveyancings { get; set; } = null!;

    // ── Commission ──────────────────────────────────────────────────────────────
    public DbSet<CommissionPlan> CommissionPlans { get; set; } = null!;
    public DbSet<CommissionPlanTier> CommissionPlanTiers { get; set; } = null!;
    public DbSet<CommissionAgreement> CommissionAgreements { get; set; } = null!;
    public DbSet<CommissionCalculation> CommissionCalculations { get; set; } = null!;
    public DbSet<CommissionSplit> CommissionSplits { get; set; } = null!;
    public DbSet<CommissionDeduction> CommissionDeductions { get; set; } = null!;
    public DbSet<CommissionDisbursement> CommissionDisbursements { get; set; } = null!;
    public DbSet<CommissionPayout> CommissionPayouts { get; set; } = null!;
    public DbSet<CommissionPayoutLine> CommissionPayoutLines { get; set; } = null!;
    public DbSet<ReferralFee> ReferralFees { get; set; } = null!;
    public DbSet<AgentCapLedger> AgentCapLedgers { get; set; } = null!;

    // ── Channel partners ────────────────────────────────────────────────────────
    public DbSet<ChannelPartner> ChannelPartners { get; set; } = null!;
    public DbSet<PartnerUser> PartnerUsers { get; set; } = null!;
    public DbSet<PartnerAgreement> PartnerAgreements { get; set; } = null!;
    public DbSet<PartnerDocument> PartnerDocuments { get; set; } = null!;
    public DbSet<PartnerAuthorisation> PartnerAuthorisations { get; set; } = null!;
    public DbSet<PartnerTier> PartnerTiers { get; set; } = null!;
    public DbSet<LeadRegistration> LeadRegistrations { get; set; } = null!;
    public DbSet<PartnerCommissionRate> PartnerCommissionRates { get; set; } = null!;
    public DbSet<PartnerCommissionEntry> PartnerCommissionEntries { get; set; } = null!;
    public DbSet<PartnerStatement> PartnerStatements { get; set; } = null!;
    public DbSet<PartnerAdvance> PartnerAdvances { get; set; } = null!;
    public DbSet<PartnerContest> PartnerContests { get; set; } = null!;

    // ── Tenancy & leasing ───────────────────────────────────────────────────────
    public DbSet<Tenancy> Tenancies { get; set; } = null!;
    public DbSet<TenancyParty> TenancyParties { get; set; } = null!;
    public DbSet<TenancyGuarantor> TenancyGuarantors { get; set; } = null!;
    public DbSet<RentSchedule> RentSchedules { get; set; } = null!;
    public DbSet<RentCharge> RentCharges { get; set; } = null!;
    public DbSet<EscalationRule> EscalationRules { get; set; } = null!;
    public DbSet<RentReview> RentReviews { get; set; } = null!;
    public DbSet<LeaseOption> LeaseOptions { get; set; } = null!;
    public DbSet<CriticalDate> CriticalDates { get; set; } = null!;
    public DbSet<TenancyRenewal> TenancyRenewals { get; set; } = null!;
    public DbSet<ReferencingCase> ReferencingCases { get; set; } = null!;
    public DbSet<ReferencingCheck> ReferencingChecks { get; set; } = null!;
    public DbSet<HoldingDeposit> HoldingDeposits { get; set; } = null!;
    public DbSet<SecurityDeposit> SecurityDeposits { get; set; } = null!;
    public DbSet<DepositDeduction> DepositDeductions { get; set; } = null!;
    public DbSet<MoveInspection> MoveInspections { get; set; } = null!;
    public DbSet<InspectionRoomItem> InspectionRoomItems { get; set; } = null!;
    public DbSet<TenancyNotice> TenancyNotices { get; set; } = null!;
    public DbSet<TenancyTermination> TenancyTerminations { get; set; } = null!;
    public DbSet<ComplianceCertificate> ComplianceCertificates { get; set; } = null!;
    public DbSet<ComplianceSchedule> ComplianceSchedules { get; set; } = null!;

    // ── Rent roll, service charge & recoveries ──────────────────────────────────
    public DbSet<RentRun> RentRuns { get; set; } = null!;
    public DbSet<RentRunLine> RentRunLines { get; set; } = null!;
    public DbSet<ArrearsCase> ArrearsCases { get; set; } = null!;
    public DbSet<ServiceChargeBudget> ServiceChargeBudgets { get; set; } = null!;
    public DbSet<ServiceChargeBudgetLine> ServiceChargeBudgetLines { get; set; } = null!;
    public DbSet<ApportionmentSchedule> ApportionmentSchedules { get; set; } = null!;
    public DbSet<ApportionmentLine> ApportionmentLines { get; set; } = null!;
    public DbSet<ServiceChargeInvoice> ServiceChargeInvoices { get; set; } = null!;
    public DbSet<ServiceChargeReconciliation> ServiceChargeReconciliations { get; set; } = null!;
    public DbSet<ReconciliationLine> ReconciliationLines { get; set; } = null!;
    public DbSet<SinkingFund> SinkingFunds { get; set; } = null!;
    public DbSet<SinkingFundEntry> SinkingFundEntries { get; set; } = null!;
    public DbSet<TurnoverRentTerm> TurnoverRentTerms { get; set; } = null!;
    public DbSet<TurnoverRentSlab> TurnoverRentSlabs { get; set; } = null!;
    public DbSet<TenantSalesDeclaration> TenantSalesDeclarations { get; set; } = null!;
    public DbSet<OverageInvoice> OverageInvoices { get; set; } = null!;
    public DbSet<RecoveryCharge> RecoveryCharges { get; set; } = null!;
    public DbSet<TenantCategory> TenantCategories { get; set; } = null!;
    public DbSet<VoidRecord> VoidRecords { get; set; } = null!;
    public DbSet<LeaseConcession> LeaseConcessions { get; set; } = null!;

    // ── Landlords & client money ────────────────────────────────────────────────
    public DbSet<Landlord> Landlords { get; set; } = null!;
    public DbSet<ManagementAgreement> ManagementAgreements { get; set; } = null!;
    public DbSet<OwnerStatement> OwnerStatements { get; set; } = null!;
    public DbSet<OwnerStatementLine> OwnerStatementLines { get; set; } = null!;
    public DbSet<OwnerPayout> OwnerPayouts { get; set; } = null!;
    public DbSet<OwnerPayoutLine> OwnerPayoutLines { get; set; } = null!;
    public DbSet<RepairAuthorityLimit> RepairAuthorityLimits { get; set; } = null!;
    public DbSet<ClientAccount> ClientAccounts { get; set; } = null!;
    public DbSet<ClientLedgerEntry> ClientLedgerEntries { get; set; } = null!;
    public DbSet<ClientMoneyReconciliation> ClientMoneyReconciliations { get; set; } = null!;
    public DbSet<ClientMoneyException> ClientMoneyExceptions { get; set; } = null!;

    // ── Society & community ─────────────────────────────────────────────────────
    public DbSet<Society> Societies { get; set; } = null!;
    public DbSet<SocietyCommittee> SocietyCommittees { get; set; } = null!;
    public DbSet<CommitteeMember> CommitteeMembers { get; set; } = null!;
    public DbSet<Resident> Residents { get; set; } = null!;
    public DbSet<ResidentHousehold> ResidentHouseholds { get; set; } = null!;
    public DbSet<ResidentVehicle> ResidentVehicles { get; set; } = null!;
    public DbSet<DomesticStaff> DomesticStaffs { get; set; } = null!;
    public DbSet<MaintenanceChargeScheme> MaintenanceChargeSchemes { get; set; } = null!;
    public DbSet<MaintenanceChargeSlab> MaintenanceChargeSlabs { get; set; } = null!;
    public DbSet<MaintenanceBill> MaintenanceBills { get; set; } = null!;
    public DbSet<MaintenanceBillLine> MaintenanceBillLines { get; set; } = null!;
    public DbSet<SocietyCharge> SocietyCharges { get; set; } = null!;
    public DbSet<SocietyPenalty> SocietyPenalties { get; set; } = null!;
    public DbSet<Visitor> Visitors { get; set; } = null!;
    public DbSet<VisitorPass> VisitorPasses { get; set; } = null!;
    public DbSet<GateEntry> GateEntries { get; set; } = null!;
    public DbSet<GatePass> GatePasses { get; set; } = null!;
    public DbSet<MoveRequest> MoveRequests { get; set; } = null!;
    public DbSet<Amenity> Amenities { get; set; } = null!;
    public DbSet<AmenitySlot> AmenitySlots { get; set; } = null!;
    public DbSet<AmenityBooking> AmenityBookings { get; set; } = null!;
    public DbSet<Complaint> Complaints { get; set; } = null!;
    public DbSet<ComplaintUpdate> ComplaintUpdates { get; set; } = null!;
    public DbSet<SocietyNotice> SocietyNotices { get; set; } = null!;
    public DbSet<SocietyPoll> SocietyPolls { get; set; } = null!;
    public DbSet<PollVote> PollVotes { get; set; } = null!;
    public DbSet<SocietyDocument> SocietyDocuments { get; set; } = null!;
    public DbSet<BuildingPlanApplication> BuildingPlanApplications { get; set; } = null!;
    public DbSet<BuildingInspection> BuildingInspections { get; set; } = null!;
    public DbSet<ViolationNotice> ViolationNotices { get; set; } = null!;

    // ── Facilities & assets ─────────────────────────────────────────────────────
    public DbSet<WorkOrder> WorkOrders { get; set; } = null!;
    public DbSet<WorkOrderLine> WorkOrderLines { get; set; } = null!;
    public DbSet<WorkOrderPhoto> WorkOrderPhotos { get; set; } = null!;
    public DbSet<WorkOrderCost> WorkOrderCosts { get; set; } = null!;
    public DbSet<Contractor> Contractors { get; set; } = null!;
    public DbSet<ContractorTrade> ContractorTrades { get; set; } = null!;
    public DbSet<ContractorRate> ContractorRates { get; set; } = null!;
    public DbSet<ContractorCompliance> ContractorCompliances { get; set; } = null!;
    public DbSet<PpmSchedule> PpmSchedules { get; set; } = null!;
    public DbSet<PpmTask> PpmTasks { get; set; } = null!;
    public DbSet<FacilityAsset> FacilityAssets { get; set; } = null!;
    public DbSet<AssetServiceRecord> AssetServiceRecords { get; set; } = null!;
    public DbSet<ServiceContract> ServiceContracts { get; set; } = null!;
    public DbSet<InspectionRound> InspectionRounds { get; set; } = null!;
    public DbSet<InspectionFinding> InspectionFindings { get; set; } = null!;
    public DbSet<Meter> Meters { get; set; } = null!;
    public DbSet<MeterReading> MeterReadings { get; set; } = null!;
    public DbSet<UtilityTariff> UtilityTariffs { get; set; } = null!;
    public DbSet<UtilityTariffSlab> UtilityTariffSlabs { get; set; } = null!;
    public DbSet<UtilityBill> UtilityBills { get; set; } = null!;
    public DbSet<UtilityBillLine> UtilityBillLines { get; set; } = null!;
    public DbSet<FuelLog> FuelLogs { get; set; } = null!;
    public DbSet<ParkingSlot> ParkingSlots { get; set; } = null!;
    public DbSet<ParkingAllotment> ParkingAllotments { get; set; } = null!;

    // ── Construction & contracting ──────────────────────────────────────────────
    public DbSet<ConstructionProject> ConstructionProjects { get; set; } = null!;
    public DbSet<WbsNode> WbsNodes { get; set; } = null!;
    public DbSet<BillOfQuantities> BillsOfQuantities { get; set; } = null!;
    public DbSet<BoqSection> BoqSections { get; set; } = null!;
    public DbSet<BoqLine> BoqLines { get; set; } = null!;
    public DbSet<RateAnalysis> RateAnalysises { get; set; } = null!;
    public DbSet<RateComponent> RateComponents { get; set; } = null!;
    public DbSet<Estimate> Estimates { get; set; } = null!;
    public DbSet<EstimateLine> EstimateLines { get; set; } = null!;
    public DbSet<SpecificationSchedule> SpecificationSchedules { get; set; } = null!;
    public DbSet<SpecificationItem> SpecificationItems { get; set; } = null!;
    public DbSet<ProgrammeActivity> ProgrammeActivities { get; set; } = null!;
    public DbSet<ActivityDependency> ActivityDependencies { get; set; } = null!;
    public DbSet<ProgressMeasurement> ProgressMeasurements { get; set; } = null!;
    public DbSet<ProgressMeasurementLine> ProgressMeasurementLines { get; set; } = null!;
    public DbSet<MilestoneCertificate> MilestoneCertificates { get; set; } = null!;
    public DbSet<InterimPaymentCertificate> InterimPaymentCertificates { get; set; } = null!;
    public DbSet<IpcLine> IpcLines { get; set; } = null!;
    public DbSet<RetentionLedgerEntry> RetentionLedgerEntries { get; set; } = null!;
    public DbSet<AdvancePayment> AdvancePayments { get; set; } = null!;
    public DbSet<AdvanceRecovery> AdvanceRecoveries { get; set; } = null!;
    public DbSet<MaterialsOnSite> MaterialsOnSite { get; set; } = null!;
    public DbSet<CostToComplete> CostToCompletes { get; set; } = null!;
    public DbSet<Tender> Tenders { get; set; } = null!;
    public DbSet<TenderBidder> TenderBidders { get; set; } = null!;
    public DbSet<TenderBid> TenderBids { get; set; } = null!;
    public DbSet<BidComparisonLine> BidComparisonLines { get; set; } = null!;
    public DbSet<Subcontract> Subcontracts { get; set; } = null!;
    public DbSet<SubcontractBoqLine> SubcontractBoqLines { get; set; } = null!;
    public DbSet<SubcontractWorkOrder> SubcontractWorkOrders { get; set; } = null!;
    public DbSet<SubcontractorClaim> SubcontractorClaims { get; set; } = null!;
    public DbSet<ClaimCertification> ClaimCertifications { get; set; } = null!;
    public DbSet<ContraCharge> ContraCharges { get; set; } = null!;
    public DbSet<VariationOrder> VariationOrders { get; set; } = null!;
    public DbSet<VariationLine> VariationLines { get; set; } = null!;
    public DbSet<SiteInstruction> SiteInstructions { get; set; } = null!;
    public DbSet<DelayEvent> DelayEvents { get; set; } = null!;
    public DbSet<ExtensionOfTime> ExtensionOfTimes { get; set; } = null!;
    public DbSet<LabourRecord> LabourRecords { get; set; } = null!;
    public DbSet<PlantItem> PlantItems { get; set; } = null!;
    public DbSet<PlantAllocation> PlantAllocations { get; set; } = null!;
    public DbSet<MaterialRequisition> MaterialRequisitions { get; set; } = null!;
    public DbSet<MaterialRequisitionLine> MaterialRequisitionLines { get; set; } = null!;
    public DbSet<MaterialIssue> MaterialIssues { get; set; } = null!;
    public DbSet<MaterialConsumptionNorm> MaterialConsumptionNorms { get; set; } = null!;
    public DbSet<WastageRecord> WastageRecords { get; set; } = null!;
    public DbSet<SiteGateEntry> SiteGateEntries { get; set; } = null!;
    public DbSet<SafetyIncident> SafetyIncidents { get; set; } = null!;

    // ── Client build (turnkey) ──────────────────────────────────────────────────
    public DbSet<ClientBuildContract> ClientBuildContracts { get; set; } = null!;
    public DbSet<ContractScopeItem> ContractScopeItems { get; set; } = null!;
    public DbSet<ClientSuppliedMaterial> ClientSuppliedMaterials { get; set; } = null!;
    public DbSet<ClientVariation> ClientVariations { get; set; } = null!;
    public DbSet<DrawingRegister> DrawingRegisters { get; set; } = null!;
    public DbSet<DrawingRevision> DrawingRevisions { get; set; } = null!;
    public DbSet<ContractCostSheet> ContractCostSheets { get; set; } = null!;
    public DbSet<ContractCostLine> ContractCostLines { get; set; } = null!;

    // ── JV, investors, escrow & revenue ─────────────────────────────────────────
    public DbSet<JointVenture> JointVentures { get; set; } = null!;
    public DbSet<JvPartner> JvPartners { get; set; } = null!;
    public DbSet<JvShareTerm> JvShareTerms { get; set; } = null!;
    public DbSet<LandownerAllocation> LandownerAllocations { get; set; } = null!;
    public DbSet<LandownerLedgerEntry> LandownerLedgerEntries { get; set; } = null!;
    public DbSet<Investor> Investors { get; set; } = null!;
    public DbSet<CapitalCommitment> CapitalCommitments { get; set; } = null!;
    public DbSet<CapitalCall> CapitalCalls { get; set; } = null!;
    public DbSet<Contribution> Contributions { get; set; } = null!;
    public DbSet<Distribution> Distributions { get; set; } = null!;
    public DbSet<ProjectBankAccount> ProjectBankAccounts { get; set; } = null!;
    public DbSet<EscrowLedgerEntry> EscrowLedgerEntries { get; set; } = null!;
    public DbSet<EscrowWithdrawal> EscrowWithdrawals { get; set; } = null!;
    public DbSet<WithdrawalCertificate> WithdrawalCertificates { get; set; } = null!;
    public DbSet<ProjectLoan> ProjectLoans { get; set; } = null!;
    public DbSet<LoanDrawdown> LoanDrawdowns { get; set; } = null!;
    public DbSet<LoanRepayment> LoanRepayments { get; set; } = null!;
    public DbSet<BankGuarantee> BankGuarantees { get; set; } = null!;
    public DbSet<CustomerMortgage> CustomerMortgages { get; set; } = null!;
    public DbSet<MortgageDisbursement> MortgageDisbursements { get; set; } = null!;
    public DbSet<RecognitionPolicy> RecognitionPolicies { get; set; } = null!;
    public DbSet<RevenueRecognitionRun> RevenueRecognitionRuns { get; set; } = null!;
    public DbSet<RecognitionEntry> RecognitionEntries { get; set; } = null!;
    public DbSet<WipEntry> WipEntries { get; set; } = null!;
    public DbSet<CostAllocationRule> CostAllocationRules { get; set; } = null!;
    public DbSet<UnitCostAllocation> UnitCostAllocations { get; set; } = null!;
    public DbSet<UnitProfitability> UnitProfitabilities { get; set; } = null!;
    public DbSet<ProjectPnlSnapshot> ProjectPnlSnapshots { get; set; } = null!;
    public DbSet<TaxProfile> TaxProfiles { get; set; } = null!;
    public DbSet<TaxComputation> TaxComputations { get; set; } = null!;
    public DbSet<WithholdingRecord> WithholdingRecords { get; set; } = null!;

    // ── Compliance, documents & legal ───────────────────────────────────────────
    public DbSet<ApprovalRecord> ApprovalRecords { get; set; } = null!;
    public DbSet<ApprovalRenewal> ApprovalRenewals { get; set; } = null!;
    public DbSet<NocIssuance> NocIssuances { get; set; } = null!;
    public DbSet<NocCondition> NocConditions { get; set; } = null!;
    public DbSet<LicenceRecord> LicenceRecords { get; set; } = null!;
    public DbSet<ComplianceCalendarEntry> ComplianceCalendarEntries { get; set; } = null!;
    public DbSet<RegulatoryFiling> RegulatoryFilings { get; set; } = null!;
    public DbSet<QuarterlyProgressReport> QuarterlyProgressReports { get; set; } = null!;
    public DbSet<QprLine> QprLines { get; set; } = null!;
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; } = null!;
    public DbSet<TemplateVersion> TemplateVersions { get; set; } = null!;
    public DbSet<ClauseLibraryItem> ClauseLibraryItems { get; set; } = null!;
    public DbSet<GeneratedDocument> GeneratedDocuments { get; set; } = null!;
    public DbSet<SignatureSession> SignatureSessions { get; set; } = null!;
    public DbSet<SignatureParty> SignatureParties { get; set; } = null!;
    public DbSet<PhysicalFile> PhysicalFiles { get; set; } = null!;
    public DbSet<PhysicalFileMovement> PhysicalFileMovements { get; set; } = null!;
    public DbSet<LegalCase> LegalCases { get; set; } = null!;
    public DbSet<LegalHearing> LegalHearings { get; set; } = null!;
    public DbSet<DocumentChecklist> DocumentChecklists { get; set; } = null!;
    public DbSet<DocumentChecklistItem> DocumentChecklistItems { get; set; } = null!;

    // ── Portals, messaging & platform ───────────────────────────────────────────
    public DbSet<PortalUser> PortalUsers { get; set; } = null!;
    public DbSet<PortalSession> PortalSessions { get; set; } = null!;
    public DbSet<PortalMessage> PortalMessages { get; set; } = null!;
    public DbSet<NotificationRule> NotificationRules { get; set; } = null!;
    public DbSet<NotificationLog> NotificationLogs { get; set; } = null!;
    public DbSet<MessageTemplate> MessageTemplates { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<ConversationMessage> ConversationMessages { get; set; } = null!;
    public DbSet<CallLog> CallLogs { get; set; } = null!;
    public DbSet<BroadcastRun> BroadcastRuns { get; set; } = null!;
    public DbSet<BroadcastRecipient> BroadcastRecipients { get; set; } = null!;
    public DbSet<ImportBatch> ImportBatches { get; set; } = null!;
    public DbSet<ImportBatchError> ImportBatchErrors { get; set; } = null!;
    public DbSet<RealEstateAuditNote> RealEstateAuditNotes { get; set; } = null!;
    public DbSet<AttentionItem> AttentionItems { get; set; } = null!;
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

        ConfigureHierarchies(modelBuilder);
        ConfigureUniqueReferences(modelBuilder);
        ConfigureHotPathIndexes(modelBuilder);

        ApplyConventions(modelBuilder);
    }

    // ═══ Hierarchies ═════════════════════════════════════════════════════════
    //
    // Self-referencing trees EF cannot pair on its own, plus the one-to-one that has to be enforced.

    private static void ConfigureHierarchies(ModelBuilder b)
    {
        // Geography below city level: zone → sector → society → block → street.
        b.Entity<GeoArea>(e =>
        {
            e.HasMany(x => x.Children)
                .WithOne(x => x.Parent!)
                .HasForeignKey(x => x.ParentAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.CompanyId, x.ParentAreaId });
            e.HasIndex(x => x.Path);
        });

        // Project → phase → block → tower → floor. Depth is the market's choice, not ours.
        b.Entity<ProjectNode>(e =>
        {
            e.HasMany(x => x.Children)
                .WithOne(x => x.Parent!)
                .HasForeignKey(x => x.ParentNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Project)
                .WithMany(p => p.Nodes)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ProjectId, x.ParentNodeId, x.SortOrder });
        });

        // Work breakdown: project → package → activity → task.
        b.Entity<WbsNode>(e =>
        {
            e.HasMany(x => x.Children)
                .WithOne(x => x.Parent!)
                .HasForeignKey(x => x.ParentNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Project)
                .WithMany(p => p.WbsNodes)
                .HasForeignKey(x => x.ConstructionProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ConstructionProjectId, x.Path });
        });

        // A unit is the salesroom's view of exactly one property. Enforced, because two units on
        // one property would let the same apartment be sold twice with nothing to detect it.
        b.Entity<Unit>(e =>
        {
            e.HasOne(x => x.Property)
                .WithMany()
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.PropertyId).IsUnique();
        });
    }

    // ═══ Business keys ═══════════════════════════════════════════════════════
    //
    // Every document number is unique inside a tenant. These are the constraints that stop a
    // duplicated receipt number or a re-used allotment letter, both of which are fraud vectors.

    private static void ConfigureUniqueReferences(ModelBuilder b)
    {
        // Company-scoped, because a document number is never unique globally — two tenants both
        // start their receipts at 1, and both are right.
        b.Entity<Property>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<Listing>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<Instruction>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<Party>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<Enquiry>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<Booking>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<Demand>().HasIndex(x => new { x.CompanyId, x.DemandNumber }).IsUnique();
        b.Entity<Receipt>().HasIndex(x => new { x.CompanyId, x.ReceiptNumber }).IsUnique();
        b.Entity<Allotment>().HasIndex(x => new { x.CompanyId, x.AllotmentNumber }).IsUnique();
        b.Entity<TransferRequest>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<Cancellation>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<NocIssuance>().HasIndex(x => new { x.CompanyId, x.NocNumber }).IsUnique();
        b.Entity<LegalNotice>().HasIndex(x => new { x.CompanyId, x.NoticeNumber }).IsUnique();
        b.Entity<Deal>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<Tenancy>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<MaintenanceBill>().HasIndex(x => new { x.CompanyId, x.BillNumber }).IsUnique();
        b.Entity<WorkOrder>().HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique();
        b.Entity<InterimPaymentCertificate>().HasIndex(x => new { x.CompanyId, x.CertificateNumber }).IsUnique();
        b.Entity<VariationOrder>().HasIndex(x => new { x.CompanyId, x.VariationNumber }).IsUnique();
        b.Entity<Subcontract>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<ClientBuildContract>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<ChannelPartner>().HasIndex(x => new { x.CompanyId, x.Reference }).IsUnique();
        b.Entity<GeneratedDocument>().HasIndex(x => new { x.CompanyId, x.DocumentNumber }).IsUnique();
        b.Entity<PhysicalFile>().HasIndex(x => new { x.CompanyId, x.FileNumber }).IsUnique();
        b.Entity<RealEstateOffice>().HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        b.Entity<Project>().HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        b.Entity<Society>().HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();

        // A plot file number and a unit number are unique inside their project, not the tenant.
        b.Entity<PlotFile>().HasIndex(x => new { x.CompanyId, x.ProjectId, x.FileNumber }).IsUnique();
        b.Entity<Unit>().HasIndex(x => new { x.CompanyId, x.ProjectId, x.UnitNumber }).IsUnique();

        // One live portal login per identifier per audience.
        b.Entity<PortalUser>().HasIndex(x => new { x.CompanyId, x.Audience, x.LoginIdentifier }).IsUnique();
    }

    // ═══ Hot-path indexes ════════════════════════════════════════════════════
    //
    // Chosen for the reads that actually run at scale rather than for every column somebody might
    // sort by: the inventory board by project and status, the collections engine by due date, the
    // rent roll by property, the gate log by day, and the ownership chain by unit.

    private static void ConfigureHotPathIndexes(ModelBuilder b)
    {
        // The inventory board. Five thousand units, filtered and coloured by status.
        b.Entity<Unit>().HasIndex(x => new { x.ProjectId, x.Status });
        b.Entity<Unit>().HasIndex(x => new { x.ProjectId, x.ProjectNodeId, x.Status });
        b.Entity<UnitHold>().HasIndex(x => new { x.UnitId, x.Status, x.ExpiresAt });
        b.Entity<SitePlanShape>().HasIndex(x => new { x.SitePlanId, x.UnitId });

        // Property search and the map.
        b.Entity<Property>().HasIndex(x => new { x.CompanyId, x.Status, x.Category });
        b.Entity<Property>().HasIndex(x => new { x.GeoAreaId, x.Status });
        b.Entity<Property>().HasIndex(x => new { x.ProjectId, x.ProjectNodeId });
        b.Entity<Property>().HasIndex(x => new { x.Latitude, x.Longitude });
        b.Entity<PropertyOwnership>().HasIndex(x => new { x.PropertyId, x.ToDate });

        // Listings and syndication.
        b.Entity<Listing>().HasIndex(x => new { x.CompanyId, x.Status, x.Kind });
        b.Entity<Listing>().HasIndex(x => new { x.PropertyId, x.Status });
        b.Entity<PortalPublication>().HasIndex(x => new { x.PortalChannelId, x.State });

        // The enquiry board and the speed-to-lead clock.
        b.Entity<Enquiry>().HasIndex(x => new { x.CompanyId, x.Stage, x.AssignedAgentId });
        b.Entity<Enquiry>().HasIndex(x => new { x.ResponseDueAt, x.SlaBreached });
        b.Entity<Enquiry>().HasIndex(x => new { x.ProjectId, x.Stage });
        b.Entity<Enquiry>().HasIndex(x => x.ContactPhone);
        b.Entity<Activity>().HasIndex(x => new { x.PartyId, x.OccurredAt });
        b.Entity<Activity>().HasIndex(x => new { x.EnquiryId, x.OccurredAt });
        b.Entity<FollowUpTask>().HasIndex(x => new { x.AssignedToUserId, x.State, x.DueAt });

        // The diary.
        b.Entity<Viewing>().HasIndex(x => new { x.AgentId, x.ScheduledAt });
        b.Entity<SiteVisit>().HasIndex(x => new { x.ProjectId, x.ScheduledAt, x.Status });

        // Bookings and the collections engine — the two heaviest reads in the module.
        b.Entity<Booking>().HasIndex(x => new { x.ProjectId, x.Status });
        b.Entity<Booking>().HasIndex(x => new { x.PrimaryApplicantPartyId, x.Status });
        b.Entity<Booking>().HasIndex(x => new { x.CompanyId, x.NextDueDate, x.Status });
        b.Entity<Booking>().HasIndex(x => new { x.ChannelPartnerId, x.Status });
        b.Entity<Instalment>().HasIndex(x => new { x.BookingId, x.SequenceNumber });
        b.Entity<Instalment>().HasIndex(x => new { x.DueDate, x.Status });
        b.Entity<Instalment>().HasIndex(x => new { x.ProjectMilestoneId, x.Status });
        b.Entity<Demand>().HasIndex(x => new { x.BookingId, x.DueDate });
        b.Entity<Demand>().HasIndex(x => new { x.DemandBatchId, x.Status });
        b.Entity<Receipt>().HasIndex(x => new { x.PartyId, x.ReceivedOn });
        b.Entity<Receipt>().HasIndex(x => new { x.BookingId, x.Status });
        b.Entity<Receipt>().HasIndex(x => new { x.ProjectId, x.ReceivedOn });
        b.Entity<ReceiptAllocation>().HasIndex(x => new { x.InstalmentId, x.IsReversed });
        b.Entity<CustomerLedgerEntry>().HasIndex(x => new { x.BookingId, x.EntryDate });
        b.Entity<CustomerLedgerEntry>().HasIndex(x => new { x.PartyId, x.EntryDate });
        b.Entity<ChequeRecord>().HasIndex(x => new { x.State, x.ChequeDate });
        b.Entity<SurchargeAccrual>().HasIndex(x => new { x.InstalmentId, x.AccrualDate });
        b.Entity<DunningCase>().HasIndex(x => new { x.CompanyId, x.IsClosed, x.NextStepDueAt });

        // Ownership and transfer.
        b.Entity<OwnershipChainEntry>().HasIndex(x => new { x.UnitId, x.SequenceNumber });
        b.Entity<OwnershipChainEntry>().HasIndex(x => new { x.PlotFileId, x.SequenceNumber });
        b.Entity<TransferRequest>().HasIndex(x => new { x.ProjectId, x.Status });

        // Possession and snagging.
        b.Entity<Snag>().HasIndex(x => new { x.SnagInspectionId, x.Severity, x.Status });
        b.Entity<DefectClaim>().HasIndex(x => new { x.UnitId, x.Status });

        // Brokerage.
        b.Entity<Deal>().HasIndex(x => new { x.CompanyId, x.Status });
        b.Entity<Deal>().HasIndex(x => new { x.SellingAgentId, x.Status });
        b.Entity<Offer>().HasIndex(x => new { x.PropertyId, x.Status });
        b.Entity<CommissionSplit>().HasIndex(x => new { x.AgentProfileId, x.Status });
        b.Entity<PartnerCommissionEntry>().HasIndex(x => new { x.ChannelPartnerId, x.Status });
        b.Entity<LeadRegistration>().HasIndex(x => new { x.ProspectPhone, x.ProjectId, x.Status });

        // The rent roll and its money.
        b.Entity<Tenancy>().HasIndex(x => new { x.PropertyId, x.Status });
        b.Entity<Tenancy>().HasIndex(x => new { x.CompanyId, x.Status, x.EndDate });
        b.Entity<Tenancy>().HasIndex(x => new { x.LandlordId, x.Status });
        b.Entity<RentCharge>().HasIndex(x => new { x.TenancyId, x.DueDate });
        b.Entity<RentCharge>().HasIndex(x => new { x.DueDate, x.Status });
        b.Entity<CriticalDate>().HasIndex(x => new { x.DueDate, x.IsActioned });
        b.Entity<ComplianceCertificate>().HasIndex(x => new { x.PropertyId, x.Kind, x.ExpiresOn });
        b.Entity<ServiceChargeInvoice>().HasIndex(x => new { x.TenancyId, x.Status });
        b.Entity<ClientLedgerEntry>().HasIndex(x => new { x.ClientAccountId, x.EntryDate });

        // The society, the gate and the helpdesk.
        b.Entity<Resident>().HasIndex(x => new { x.SocietyId, x.UnitId });
        b.Entity<MaintenanceBill>().HasIndex(x => new { x.SocietyId, x.DueDate, x.Status });
        b.Entity<MaintenanceBill>().HasIndex(x => new { x.UnitId, x.PeriodFrom });
        b.Entity<GateEntry>().HasIndex(x => new { x.SocietyId, x.CheckedInAt });
        b.Entity<GateEntry>().HasIndex(x => new { x.UnitId, x.Status });
        b.Entity<VisitorPass>().HasIndex(x => new { x.SocietyId, x.ValidTo, x.Status });
        b.Entity<AmenityBooking>().HasIndex(x => new { x.AmenityId, x.BookingDate });
        b.Entity<Complaint>().HasIndex(x => new { x.SocietyId, x.Status, x.SlaDueAt });

        // Facilities.
        b.Entity<WorkOrder>().HasIndex(x => new { x.CompanyId, x.Status, x.Priority });
        b.Entity<WorkOrder>().HasIndex(x => new { x.PropertyId, x.Status });
        b.Entity<WorkOrder>().HasIndex(x => new { x.ContractorId, x.Status });
        b.Entity<PpmTask>().HasIndex(x => new { x.DueDate, x.Status });
        b.Entity<MeterReading>().HasIndex(x => new { x.MeterId, x.ReadingDate });

        // Construction.
        b.Entity<BoqLine>().HasIndex(x => new { x.BillOfQuantitiesId, x.SortOrder });
        b.Entity<ProgressMeasurementLine>().HasIndex(x => x.BoqLineId);
        b.Entity<InterimPaymentCertificate>().HasIndex(x => new { x.ConstructionProjectId, x.SequenceNumber });
        b.Entity<SubcontractorClaim>().HasIndex(x => new { x.SubcontractId, x.SequenceNumber });
        b.Entity<VariationOrder>().HasIndex(x => new { x.ConstructionProjectId, x.Status });
        b.Entity<MaterialIssue>().HasIndex(x => new { x.ConstructionProjectId, x.IssuedOn });
        b.Entity<RetentionLedgerEntry>().HasIndex(x => new { x.SubcontractId, x.EntryDate });

        // Escrow and compliance.
        b.Entity<EscrowLedgerEntry>().HasIndex(x => new { x.ProjectBankAccountId, x.EntryDate });
        b.Entity<ApprovalRecord>().HasIndex(x => new { x.ProjectId, x.State });
        b.Entity<ComplianceCalendarEntry>().HasIndex(x => new { x.DueDate, x.IsCompleted });
        b.Entity<LegalCase>().HasIndex(x => new { x.NextHearingDate, x.IsClosed });

        // Notifications and messaging.
        b.Entity<NotificationLog>().HasIndex(x => new { x.RecipientUserId, x.ReadAt });
        b.Entity<Conversation>().HasIndex(x => new { x.AssignedToUserId, x.Status, x.LastMessageAt });
        b.Entity<ConversationMessage>().HasIndex(x => new { x.ConversationId, x.SentAt });
        b.Entity<AttentionItem>().HasIndex(x => new { x.CompanyId, x.Rank, x.IsDismissed });
    }

    // ═══ Conventions ═════════════════════════════════════════════════════════

    /// <summary>
    /// Column types and the tenant index, applied once across four hundred entities rather than
    /// declared four hundred times.
    ///
    /// The rules are deliberately name-driven and boring: anything called <c>*Percent</c> is a
    /// percentage, anything called <c>Latitude</c> is a coordinate, anything called
    /// <c>*Quantity</c>, <c>*Hours</c> or <c>*Reading</c> is a measured quantity, and everything
    /// else that is a decimal is money. Getting a column type wrong here is a rounding error in
    /// somebody's ledger, so the fallback is the safe one — money at two places, never a float.
    /// </summary>
    private static void ApplyConventions(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entity.ClrType)) continue;

            var builder = b.Entity(entity.ClrType);

            // PostgreSQL maintains xmin itself; EF only has to know to check it.
            builder.Property(nameof(BaseEntity.RowVersion))
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.Property(nameof(BaseEntity.Description)).HasMaxLength(2000);
            builder.Property(nameof(BaseEntity.Code)).HasMaxLength(50);

            foreach (var property in entity.GetProperties())
            {
                var name = property.Name;
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                if (type == typeof(decimal))
                {
                    var (precision, scale) = DecimalShape(name);
                    builder.Property(name).HasPrecision(precision, scale);
                }
                else if (type == typeof(string) && property.GetMaxLength() is null)
                {
                    builder.Property(name).HasMaxLength(StringLength(name));
                }
            }

            // Every read filters on tenant and soft-delete, so every table is indexed for it.
            builder.HasIndex(
                nameof(BaseEntity.CompanyId),
                nameof(BaseEntity.BranchId),
                nameof(BaseEntity.BusinessUnitId),
                nameof(BaseEntity.IsDeleted));
        }
    }

    private static (int Precision, int Scale) DecimalShape(string name)
    {
        if (name is "Latitude" or "Longitude"
            || name.EndsWith("Latitude", StringComparison.Ordinal)
            || name.EndsWith("Longitude", StringComparison.Ordinal))
            return (10, 7);

        if (name.EndsWith("Percent", StringComparison.Ordinal)
            || name.EndsWith("Rating", StringComparison.Ordinal)
            || name is "ExchangeRate" or "Multiplier" or "InterestRate"
            || name.EndsWith("Index", StringComparison.Ordinal))
            return (9, 4);

        // Measured rather than paid: site quantities, meter readings, running hours, areas.
        if (name.EndsWith("Quantity", StringComparison.Ordinal)
            || name.EndsWith("Hours", StringComparison.Ordinal)
            || name.EndsWith("Reading", StringComparison.Ordinal)
            || name.EndsWith("Consumption", StringComparison.Ordinal)
            || name.EndsWith("SqFt", StringComparison.Ordinal)
            || name.EndsWith("Litres", StringComparison.Ordinal)
            || name.EndsWith("Units", StringComparison.Ordinal)
            || name is "Litres" or "Quantity" or "Weight" or "GrossWeight" or "TareWeight" or "NetWeight"
            || name.EndsWith("Ft", StringComparison.Ordinal))
            return (18, 4);

        // Everything else is money.
        return (18, 2);
    }

    private static int StringLength(string name)
    {
        if (name is "CountryCode" or "CurrencyCode") return 3;
        if (name is "LanguageCode" or "PreferredLanguage" or "ColourHex") return 16;
        if (name.EndsWith("Url", StringComparison.Ordinal)
            || name.EndsWith("Urls", StringComparison.Ordinal)) return 1000;

        // Free text and serialised blobs. Generous on purpose: a truncated clause, a truncated
        // snag description or a truncated import row is a data-loss bug, not a display one.
        if (name.EndsWith("Json", StringComparison.Ordinal)
            || name is "Body" or "Instruction" or "Terms" or "Conditions" or "Condition"
                or "Rationale" or "Findings" or "Summary" or "Notes" or "Note" or "Message"
                or "LongDescription" or "KeyFeatures" or "Rules" or "Assumptions" or "Exclusions"
                or "Qualifications" or "Justification" or "Commentary" or "Observations"
                or "Specification" or "Grounds" or "RootCause" or "CorrectiveAction"
                or "ImmediateAction" or "PlannedWorks" or "Scope" or "Options" or "Path"
                or "BoundaryGeoJson" or "Points" or "PreferredAreasGeoJson" or "RouteGeoJson"
                or "ScoreBreakdown" or "CalculationTrace" or "RawPayload" or "ChecklistJson")
            return 4000;

        if (name.EndsWith("Reason", StringComparison.Ordinal)
            || name.EndsWith("Note", StringComparison.Ordinal)
            || name.EndsWith("Description", StringComparison.Ordinal)
            || name.EndsWith("GeoJson", StringComparison.Ordinal)) return 2000;

        return 500;
    }
}
