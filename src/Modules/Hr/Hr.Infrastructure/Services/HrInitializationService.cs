using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JobApplication = Hr.Domain.Entities.Application;

namespace Hr.Infrastructure.Services;

public class HrInitializationService : IHrInitializationService
{
    private readonly HrDbContext _context;
    private readonly ILogger<HrInitializationService> _logger;

    /// <summary>
    /// ISO 4217 code used for the sample pay scales, jobs and offers. Currencies themselves live
    /// in the Core module's catalogue; HR only records the code.
    /// </summary>
    private const string DefaultCurrencyCode = "USD";

    public HrInitializationService(HrDbContext context, ILogger<HrInitializationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> InitializeHrForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId,
        bool includeSampleData = false)
    {
        try
        {
            _logger.LogInformation(
                "Initializing HR data for Company: {CompanyId} IncludeSampleData: {Include}",
                companyId, includeSampleData);

            if (await HrDataExistsAsync(companyId))
            {
                _logger.LogWarning("HR data already exists for Company: {CompanyId}", companyId);
                await EnsureWorkflowConfigsAsync(companyId, branchId, businessUnitId, userId);
                return Result.Ok("HR data already initialized for this company");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;
                var t = (companyId, branchId, businessUnitId, userId, now);

                // Phase 1 – Lookups
                var lookups = await SeedLookupDataAsync(companyId, branchId, businessUnitId, userId);
                _logger.LogInformation("Seeded {Count} lookup values for Company: {CompanyId}", lookups.Count, companyId);

                // Currencies are no longer seeded here: the ISO 4217 catalogue is owned by the
                // Core module (core.currencies, seeded by GeoReferenceSeed) and HR records simply
                // carry the alpha-3 code.

                // Phase 3 – Grades
                var grades = CreateGrades(t);
                _context.Set<Grade>().AddRange(grades);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} grades for Company: {CompanyId}", grades.Length, companyId);

                // Phase 4 – Job Families
                var families = CreateJobFamilies(t);
                _context.Set<JobFamily>().AddRange(families);
                await _context.SaveChangesAsync();

                // Phase 5 – Job Functions
                var functions = CreateJobFunctions(t);
                _context.Set<JobFunction>().AddRange(functions);
                await _context.SaveChangesAsync();

                // Phase 6 – Cost Centers (no dept FK yet)
                var costCenters = CreateCostCenters(t);
                _context.Set<CostCenter>().AddRange(costCenters);
                await _context.SaveChangesAsync();

                // Phase 7 – Departments (with CostCenter FKs)
                var departments = CreateDepartments(costCenters, t);
                _context.Set<Department>().AddRange(departments);
                await _context.SaveChangesAsync();

                // Phase 8 – Designations
                var designations = CreateDesignations(families, functions, grades, t);
                _context.Set<Designation>().AddRange(designations);
                await _context.SaveChangesAsync();

                // Phase 9 – Pay Scales
                var payScales = CreatePayScales(DefaultCurrencyCode, t);
                _context.Set<PayScale>().AddRange(payScales);
                await _context.SaveChangesAsync();

                // Phase 10 – Job Locations
                var locations = CreateJobLocations(t);
                _context.Set<JobLocation>().AddRange(locations);
                await _context.SaveChangesAsync();

                // Phase 11 – Shifts
                var shifts = CreateShifts(t);
                _context.Set<Shift>().AddRange(shifts);
                await _context.SaveChangesAsync();

                // Phase 12 – Allowances Profiles
                var allowances = CreateAllowancesProfiles(t);
                _context.Set<AllowancesProfile>().AddRange(allowances);
                await _context.SaveChangesAsync();

                // Phase 13 – Benefits Plans
                var benefits = CreateBenefitsPlans(t);
                _context.Set<BenefitsPlan>().AddRange(benefits);
                await _context.SaveChangesAsync();

                // Phase 14 – Talent Pools
                var talentPools = CreateTalentPools(t);
                _context.Set<TalentPool>().AddRange(talentPools);
                await _context.SaveChangesAsync();

                // Phase 15 – Screening Questionnaires
                var questionnaires = CreateScreeningQuestionnaires(t);
                _context.Set<ScreeningQuestionnaire>().AddRange(questionnaires);
                await _context.SaveChangesAsync();

                // Phase 16 – Skill Categories
                var skillCategories = CreateSkillCategories(t);
                _context.Set<SkillCategory>().AddRange(skillCategories);
                await _context.SaveChangesAsync();

                // Phase 17 – Skills
                var skills = CreateSkills(skillCategories, t);
                _context.Set<Skill>().AddRange(skills);
                await _context.SaveChangesAsync();

                // Phase 18 – Competency Frameworks + Items
                var frameworks = CreateCompetencyFrameworks(families, skills, t);
                _context.Set<CompetencyFramework>().AddRange(frameworks.Select(f => f.Framework));
                await _context.SaveChangesAsync();
                var frameworkItems = frameworks.SelectMany(f => f.Items).ToArray();
                _context.Set<CompetencyFrameworkItem>().AddRange(frameworkItems);
                await _context.SaveChangesAsync();

                // Phase 19 – Workflow Configs + Steps
                var workflows = CreateWorkflowConfigs(t);
                _context.Set<WorkflowConfig>().AddRange(workflows.Select(w => w.Config));
                await _context.SaveChangesAsync();
                var workflowSteps = workflows.SelectMany(w => w.Steps).ToArray();
                _context.Set<WorkflowConfigStep>().AddRange(workflowSteps);
                await _context.SaveChangesAsync();

                // Phase 20 – Communication Templates (global unique index)
                var commTypeId = lookups.GetValueOrDefault("COMM_TYPE_EMAIL");
                var commTemplates = await SeedCommunicationTemplatesAsync(commTypeId, t);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} communication templates", commTemplates.Length);

                // Phase 21 – Channel Templates
                var linkedInTypeId = lookups.GetValueOrDefault("CHANNEL_LINKEDIN");
                var indeedTypeId = lookups.GetValueOrDefault("CHANNEL_INDEED");
                var siteTypeId = lookups.GetValueOrDefault("CHANNEL_CAREERSITE");
                var postingDraftId = lookups.GetValueOrDefault("POSTING_DRAFT");
                var channelTemplates = CreateChannelTemplates(linkedInTypeId, indeedTypeId, siteTypeId, postingDraftId, t);
                _context.Set<ChannelTemplate>().AddRange(channelTemplates);
                await _context.SaveChangesAsync();

                // Phase 22 – Interview Feedback Templates
                var techIntTypeId = lookups.GetValueOrDefault("INTERVIEW_TYPE_TECH");
                var behavIntTypeId = lookups.GetValueOrDefault("INTERVIEW_TYPE_BEHAV");
                var iftTemplates = CreateInterviewFeedbackTemplates(techIntTypeId, behavIntTypeId,
                    families.First(f => f.JobFamilyCode == "ENG").Id, t);
                _context.Set<InterviewFeedbackTemplate>().AddRange(iftTemplates);
                await _context.SaveChangesAsync();

                // Phase 23 – Job Templates
                var jobTemplates = CreateJobTemplates(departments, designations, families, functions, grades, t);
                _context.Set<JobTemplate>().AddRange(jobTemplates);
                await _context.SaveChangesAsync();

                // Phase 24 – Onboarding Task Templates
                var onboardingTemplates = CreateOnboardingTaskTemplates(departments, designations, locations, t);
                _context.Set<OnboardingTaskTemplate>().AddRange(onboardingTemplates);
                await _context.SaveChangesAsync();

                // Phase 25 – Positions
                var positions = CreatePositions(departments, designations, families, functions, grades, payScales, t);
                _context.Set<Position>().AddRange(positions);
                await _context.SaveChangesAsync();

                // ═══ SAMPLE DATA (opt-in) ═════════════════════════════════════
                // Phases 26-33 are the demo workforce and recruitment pipeline. Everything
                // above is the org structure and configuration a real company needs before
                // it can hire anyone, so it is always seeded.
                if (!includeSampleData)
                {
                    await transaction.CommitAsync();
                    _logger.LogInformation(
                        "HR sample data skipped (IncludeSampleData=false) for Company: {CompanyId}", companyId);
                    return Result.Ok("HR master data initialized successfully for new company");
                }

                // Phase 26 – Employees
                var employees = CreateEmployees(departments, designations, positions, locations, t);
                _context.Set<Employee>().AddRange(employees);
                await _context.SaveChangesAsync();

                // Phase 27 – Jobs + Job Details + Job Posting Channels
                var jobStatusOpenId = lookups.GetValueOrDefault("JOB_OPEN");
                var priorityHighId = lookups.GetValueOrDefault("PRIORITY_HIGH");
                var priorityMedId = lookups.GetValueOrDefault("PRIORITY_MEDIUM");
                var postingPublishedId = lookups.GetValueOrDefault("POSTING_PUBLISHED");
                var jobs = CreateJobs(departments, designations, families, functions, grades, payScales,
                    locations, shifts, allowances, employees, channelTemplates,
                    jobStatusOpenId, priorityHighId, priorityMedId, postingPublishedId,
                    DefaultCurrencyCode, t);
                _context.Set<Job>().AddRange(jobs.Select(j => j.Job));
                await _context.SaveChangesAsync();
                _context.Set<JobDetail>().AddRange(jobs.Select(j => j.Detail));
                _context.Set<JobPostingChannel>().AddRange(jobs.SelectMany(j => j.PostingChannels));
                await _context.SaveChangesAsync();

                // Phase 28 – Candidates + Profiles
                var candidates = CreateCandidates(designations, t);
                _context.Set<Candidate>().AddRange(candidates.Select(c => c.Candidate));
                await _context.SaveChangesAsync();
                _context.Set<CandidateProfile>().AddRange(candidates.Select(c => c.Profile));
                await _context.SaveChangesAsync();

                // Phase 29 – Applications + Details + Compliances
                var stageAppliedId = lookups.GetValueOrDefault("STAGE_APPLIED");
                var stageScreenedId = lookups.GetValueOrDefault("STAGE_SCREENED");
                var stageShortlistedId = lookups.GetValueOrDefault("STAGE_SHORTLISTED");
                var appActiveId = lookups.GetValueOrDefault("APP_ACTIVE");
                var apps = CreateApplications(
                    jobs, candidates, employees,
                    stageAppliedId, stageScreenedId, stageShortlistedId, appActiveId, t);
                _context.Set<JobApplication>().AddRange(apps.Select(a => a.App));
                await _context.SaveChangesAsync();
                _context.Set<ApplicationDetail>().AddRange(apps.Select(a => a.Detail));
                _context.Set<ApplicationCompliance>().AddRange(apps.Select(a => a.Compliance));
                await _context.SaveChangesAsync();

                // Phase 30 – Job Requisitions
                var requisitions = CreateJobRequisitions(
                    departments, designations, families, functions, grades, payScales,
                    locations, shifts, employees, jobStatusOpenId, priorityHighId, priorityMedId, DefaultCurrencyCode, t);
                _context.Set<Job>().AddRange(requisitions.Select(r => r.Job));
                await _context.SaveChangesAsync();
                _context.Set<JobDetail>().AddRange(requisitions.Select(r => r.Detail));
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} job requisitions for Company: {CompanyId}", requisitions.Length, companyId);

                // Phase 31 – Approval Requests + Steps for Requisitions
                var inProgressStatusId = lookups.GetValueOrDefault("IN_PROGRESS");
                var pendingStatusId    = lookups.GetValueOrDefault("PENDING");
                var waitingStatusId    = lookups.GetValueOrDefault("WAITING");
                var requisitionWorkflow = workflows.FirstOrDefault(w => w.Config.TransactionType == "Requisition");
                if (requisitionWorkflow != null)
                {
                    var approvals = CreateRequisitionApprovals(
                        requisitions, requisitionWorkflow, employees,
                        inProgressStatusId, pendingStatusId, waitingStatusId, priorityHighId, t);
                    _context.Set<ApprovalRequest>().AddRange(approvals.Select(a => a.Request));
                    await _context.SaveChangesAsync();
                    _context.Set<ApprovalRequestStep>().AddRange(approvals.SelectMany(a => a.Steps));
                    await _context.SaveChangesAsync();
                    foreach (var (req, apr) in requisitions.Zip(approvals))
                        req.Job.ApprovalRequestId = apr.Request.Id;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Seeded {Count} approval requests for Company: {CompanyId}", approvals.Length, companyId);
                }

                // Phase 32 – Interviews
                var intScheduledId = lookups.GetValueOrDefault("INTERVIEW_SCHEDULED");
                var intCompletedId = lookups.GetValueOrDefault("INTERVIEW_COMPLETED");
                var interviews = CreateInterviews(
                    apps, jobs, candidates, employees, iftTemplates,
                    techIntTypeId, behavIntTypeId, intScheduledId, intCompletedId, t);
                _context.Set<Interview>().AddRange(interviews);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} interviews for Company: {CompanyId}", interviews.Length, companyId);

                // Phase 33 – Offer Letters + Details
                var offerSentId    = lookups.GetValueOrDefault("OFFER_SENT");
                var offerPendingId = lookups.GetValueOrDefault("OFFER_PENDING");
                var candRespNoneId = lookups.GetValueOrDefault("CAND_RESP_NONE");
                var offerData = CreateOfferLetters(
                    apps, jobs, candidates, employees, grades, payScales, benefits, allowances,
                    offerSentId, offerPendingId, candRespNoneId, DefaultCurrencyCode, t);
                _context.Set<OfferLetter>().AddRange(offerData.Select(o => o.Offer));
                await _context.SaveChangesAsync();
                _context.Set<OfferLetterDetail>().AddRange(offerData.Select(o => o.Detail));
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} offer letters for Company: {CompanyId}", offerData.Length, companyId);

                await transaction.CommitAsync();
                _logger.LogInformation("Successfully initialized HR data for Company: {CompanyId}", companyId);
                return Result.Ok("HR data initialized successfully for new company");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during HR data initialization for Company: {CompanyId}", companyId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize HR data for Company: {CompanyId}", companyId);
            return Result.Fail($"Failed to initialize HR data: {ex.Message}");
        }
    }

    public async Task<bool> HrDataExistsAsync(Guid companyId)
        => await _context.Set<Grade>().AnyAsync(g => g.CompanyId == companyId && !g.IsDeleted);

    public async Task<Result> EnsureWorkflowConfigsAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var t = (companyId, branchId, businessUnitId, userId, now);
            var seeded = 0;

            async Task Ensure(
                string code, string transactionType, string name, string description, int totalLevels,
                (string approverType, string approverValue, int levelNo, int slaHours, int sortOrder)[] steps)
            {
                var exists = await _context.Set<WorkflowConfig>().AnyAsync(w =>
                    w.WorkflowCode == code && w.CompanyId == companyId &&
                    w.BranchId == branchId && w.BusinessUnitId == businessUnitId && !w.IsDeleted);
                if (exists) return;

                var wf = Base(new WorkflowConfig
                {
                    WorkflowCode = code, Module = "HR", TransactionType = transactionType,
                    WorkflowName = name, Description = description,
                    IsActive = true, TotalLevels = totalLevels, EffectiveFrom = now, VersionNo = 1
                }, t);
                _context.Set<WorkflowConfig>().Add(wf);
                await _context.SaveChangesAsync();

                foreach (var s in steps)
                {
                    _context.Set<WorkflowConfigStep>().Add(Base(new WorkflowConfigStep
                    {
                        WorkflowConfigId = wf.Id, LevelNo = s.levelNo,
                        ApproverType = s.approverType, ApproverValue = s.approverValue,
                        Mandatory = true, SLAHours = s.slaHours,
                        ExecutionType = "Sequential", SortOrder = s.sortOrder
                    }, t));
                }
                await _context.SaveChangesAsync();
                seeded++;
                _logger.LogInformation("Seeded WorkflowConfig {Code} for Company {CompanyId}", code, companyId);
            }

            await Ensure("WF-REQUISITION-APPROVAL", "Requisition",
                "Job Requisition Approval",
                "Two-level approval workflow for job requisitions raised by hiring managers",
                2, new[] { ("Role", "HR_MANAGER", 1, 48, 1), ("Role", "DIRECTOR", 2, 72, 2) });

            await Ensure("WF-JOB-APPROVAL", "JobApproval",
                "Job Approval",
                "Two-level approval workflow for new job requisitions",
                2, new[] { ("Role", "HR_MANAGER", 1, 48, 1), ("Role", "DIRECTOR", 2, 72, 2) });

            await Ensure("WF-OFFER-APPROVAL", "OfferApproval",
                "Offer Letter Approval",
                "Two-level approval workflow for offer letters",
                2, new[] { ("Role", "HR_MANAGER", 1, 24, 1), ("Role", "VP_HR", 2, 48, 2) });

            return Result.Ok($"Workflow configs ensured: {seeded} seeded, {3 - seeded} already existed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure workflow configs for Company: {CompanyId}", companyId);
            return Result.Fail($"Failed to ensure workflow configs: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) T(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now)
        => (companyId, branchId, businessUnitId, userId, now);

    private TEntity Base<TEntity>(TEntity e,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
        where TEntity : BaseEntity
    {
        e.CompanyId = t.companyId;
        e.BranchId = t.branchId;
        e.BusinessUnitId = t.businessUnitId;
        e.CreatedByUserId = t.userId;
        e.CreatedAt = t.now;
        return e;
    }

    // -------------------------------------------------------------------------
    // Phase 1 – Lookup Data
    // -------------------------------------------------------------------------

    private async Task<Dictionary<string, Guid>> SeedLookupDataAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        var ids = new Dictionary<string, Guid>();

        async Task<LookupType> EnsureType(string code, string name, string module, string entity)
        {
            var existing = await _context.LookupTypes.FirstOrDefaultAsync(t =>
                t.Code == code && t.CompanyId == companyId && t.BranchId == branchId
                && t.BusinessUnitId == businessUnitId && !t.IsDeleted);
            if (existing != null) return existing;
            var lt = new LookupType
            {
                Code = code, Name = name, ModuleName = module, EntityName = entity,
                IsActive = true, IsSystem = true, SortOrder = 0,
                CompanyId = companyId, BranchId = branchId, BusinessUnitId = businessUnitId,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            _context.LookupTypes.Add(lt);
            return lt;
        }

        async Task EnsureValue(Guid typeId, string code, string name, int sort)
        {
            var exists = await _context.LookupValues.AnyAsync(v =>
                v.Code == code && v.LookupTypeId == typeId
                && v.CompanyId == companyId && !v.IsDeleted);
            if (exists)
            {
                var existing = await _context.LookupValues.FirstAsync(v =>
                    v.Code == code && v.LookupTypeId == typeId && v.CompanyId == companyId && !v.IsDeleted);
                ids[code] = existing.Id;
                return;
            }
            var lv = new LookupValue
            {
                LookupTypeId = typeId, Code = code, Name = name, SortOrder = sort,
                IsActive = true, IsDefault = false,
                CompanyId = companyId, BranchId = branchId, BusinessUnitId = businessUnitId,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            _context.LookupValues.Add(lv);
            ids[code] = lv.Id;
        }

        // APPROVAL_STATUS
        var approvalType = await EnsureType("APPROVAL_STATUS", "Approval Status", "HR", "ApprovalRequest");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("PENDING","Pending",1),("WAITING","Waiting",2),("APPROVED","Approved",3),
            ("REJECTED","Rejected",4),("CANCELLED","Cancelled",5),("IN_PROGRESS","In Progress",6)})
            await EnsureValue(approvalType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // JOB_STATUS
        var jobStatusType = await EnsureType("JOB_STATUS", "Job Status", "HR", "Job");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("JOB_DRAFT","Draft",1),("JOB_PENDING","Pending Approval",2),("JOB_APPROVED","Approved",3),
            ("JOB_REJECTED","Rejected",4),("JOB_OPEN","Open",5),("JOB_CLOSED","Closed",6),("JOB_CANCELLED","Cancelled",7)})
            await EnsureValue(jobStatusType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // APPLICATION_STAGE
        var appStageType = await EnsureType("APPLICATION_STAGE", "Application Stage", "Recruitment", "Application");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("STAGE_APPLIED","Applied",1),("STAGE_SCREENED","Screened",2),("STAGE_SHORTLISTED","Shortlisted",3),
            ("STAGE_INTERVIEW_INVITE","Interview Invited",4),("STAGE_INTERVIEW_COMPLETE","Interview Complete",5),
            ("STAGE_OFFER_EXTENDED","Offer Extended",6),("STAGE_OFFER_ACCEPTED","Offer Accepted",7),
            ("STAGE_OFFER_REJECTED","Offer Rejected",8),("STAGE_HIRED","Hired",9),("STAGE_REJECTED","Rejected",10)})
            await EnsureValue(appStageType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // APPLICATION_STATUS
        var appStatusType = await EnsureType("APPLICATION_STATUS", "Application Status", "Recruitment", "Application");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("APP_ACTIVE","Active",1),("APP_WITHDRAWN","Withdrawn",2),("APP_ARCHIVED","Archived",3),("APP_HOLD","Hold",4)})
            await EnsureValue(appStatusType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // INTERVIEW_STATUS
        var intStatusType = await EnsureType("INTERVIEW_STATUS", "Interview Status", "Recruitment", "Interview");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("INTERVIEW_SCHEDULED","Scheduled",1),("INTERVIEW_INVITED","Invited",2),
            ("INTERVIEW_COMPLETED","Completed",3),("INTERVIEW_CANCELLED","Cancelled",4),("INTERVIEW_RESCHEDULED","Rescheduled",5)})
            await EnsureValue(intStatusType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // POSTING_STATUS
        var postStatusType = await EnsureType("POSTING_STATUS", "Posting Status", "Recruitment", "JobPostingChannel");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("POSTING_DRAFT","Draft",1),("POSTING_PUBLISHED","Published",2),("POSTING_CLOSED","Closed",3),
            ("POSTING_PAUSED","Paused",4),("POSTING_ARCHIVED","Archived",5)})
            await EnsureValue(postStatusType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // PRIORITY
        var priorityType = await EnsureType("PRIORITY", "Priority", "Recruitment", "Job");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("PRIORITY_CRITICAL","Critical",1),("PRIORITY_HIGH","High",2),
            ("PRIORITY_MEDIUM","Medium",3),("PRIORITY_LOW","Low",4)})
            await EnsureValue(priorityType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // CHANNEL_TYPE
        var channelType = await EnsureType("CHANNEL_TYPE", "Channel Type", "Recruitment", "JobPostingChannel");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("CHANNEL_LINKEDIN","LinkedIn",1),("CHANNEL_INDEED","Indeed",2),
            ("CHANNEL_CAREERSITE","Career Site",3),("CHANNEL_REFERRAL","Referral",4),
            ("CHANNEL_AGENCY","Agency",5),("CHANNEL_JOBFAIR","Job Fair",6),
            ("CHANNEL_INTERNAL","Internal",7),("CHANNEL_CAMPUS","Campus Recruiting",8),
            ("CHANNEL_SOCIAL","Social Media",9),("CHANNEL_OTHER","Other",10)})
            await EnsureValue(channelType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // INTERVIEW_TYPE
        var intType = await EnsureType("INTERVIEW_TYPE", "Interview Type", "Recruitment", "Interview");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("INTERVIEW_TYPE_TECH","Technical Interview",1),("INTERVIEW_TYPE_BEHAV","Behavioral Interview",2),
            ("INTERVIEW_TYPE_HR","HR Screening",3),("INTERVIEW_TYPE_FINAL","Final Round",4)})
            await EnsureValue(intType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // COMMUNICATION_TYPE
        var commType = await EnsureType("COMMUNICATION_TYPE", "Communication Type", "HR", "CommunicationTemplate");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("COMM_TYPE_EMAIL","Email",1),("COMM_TYPE_SMS","SMS",2),("COMM_TYPE_WHATSAPP","WhatsApp",3)})
            await EnsureValue(commType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // SKILL_TYPE
        var skillTypeType = await EnsureType("SKILL_TYPE", "Skill Type", "HR", "Skill");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("SKILL_TYPE_TECH","Technical",1),("SKILL_TYPE_SOFT","Soft Skill",2),
            ("SKILL_TYPE_DOMAIN","Domain Knowledge",3),("SKILL_TYPE_LANG","Programming Language",4)})
            await EnsureValue(skillTypeType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // OFFER_STATUS
        var offerStatusType = await EnsureType("OFFER_STATUS", "Offer Status", "Recruitment", "OfferLetter");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("OFFER_DRAFT","Draft",1),("OFFER_PENDING","Pending Approval",2),("OFFER_SENT","Sent",3),
            ("OFFER_ACCEPTED","Accepted",4),("OFFER_REJECTED","Rejected",5),("OFFER_REVOKED","Revoked",6)})
            await EnsureValue(offerStatusType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // PANEL_INVITE_STATUS
        var panelStatusType = await EnsureType("PANEL_INVITE_STATUS", "Panel Invite Status", "Recruitment", "InterviewPanelMember");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("PANEL_INVITED","Invited",1),("PANEL_ACCEPTED","Accepted",2),
            ("PANEL_DECLINED","Declined",3),("PANEL_TENTATIVE","Tentative",4)})
            await EnsureValue(panelStatusType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // CANDIDATE_RESPONSE
        var candRespType = await EnsureType("CANDIDATE_RESPONSE", "Candidate Response", "Recruitment", "OfferLetter");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("CAND_RESP_ACCEPTED","Accepted",1),("CAND_RESP_REJECTED","Rejected",2),
            ("CAND_RESP_NEGOTIATING","Negotiating",3),("CAND_RESP_NONE","No Response",4)})
            await EnsureValue(candRespType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // TASK_STATUS
        var taskStatusType = await EnsureType("TASK_STATUS", "Task Status", "Recruitment", "CandidateTask");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("TASK_PENDING","Pending",1),("TASK_IN_PROGRESS","In Progress",2),
            ("TASK_COMPLETED","Completed",3),("TASK_CANCELLED","Cancelled",4),("TASK_OVERDUE","Overdue",5)})
            await EnsureValue(taskStatusType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        // ONBOARDING_STATUS
        var onboardStatusType = await EnsureType("ONBOARDING_STATUS", "Onboarding Status", "HR", "OnboardingTask");
        await _context.SaveChangesAsync();
        foreach (var (code, name, sort) in new[] {
            ("ONBOARD_PENDING","Pending",1),("ONBOARD_IN_PROGRESS","In Progress",2),
            ("ONBOARD_COMPLETED","Completed",3),("ONBOARD_BLOCKED","Blocked",4),("ONBOARD_CANCELLED","Cancelled",5)})
            await EnsureValue(onboardStatusType.Id, code, name, sort);
        await _context.SaveChangesAsync();

        return ids;
    }

    // -------------------------------------------------------------------------
    // -------------------------------------------------------------------------

    // -------------------------------------------------------------------------
    // Phase 3 – Grades
    // -------------------------------------------------------------------------

    private Grade[] CreateGrades(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("G1","Entry Level",1),("G2","Junior",2),("G3","Mid-Level",3),
            ("G4","Senior",4),("G5","Principal / Lead",5),
            ("G6","Manager",6),("G7","Senior Manager",7),
            ("G8","Director",8),("G9","Vice President",9)
        };
        return data.Select(d => Base(new Grade
        {
            GradeCode = d.Item1, GradeName = d.Item2, LevelNo = d.Item3, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 4 – Job Families
    // -------------------------------------------------------------------------

    private JobFamily[] CreateJobFamilies(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("ENG","Engineering","Software and systems engineering"),
            ("SALES","Sales & BD","Sales and business development"),
            ("MKT","Marketing","Marketing and brand management"),
            ("HR","Human Resources","People operations and talent"),
            ("FIN","Finance","Finance and accounting"),
            ("IT","Information Technology","IT infrastructure and support"),
            ("OPS","Operations","Business operations and logistics")
        };
        return data.Select(d => Base(new JobFamily
        {
            JobFamilyCode = d.Item1, JobFamilyName = d.Item2, Description = d.Item3, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 5 – Job Functions
    // -------------------------------------------------------------------------

    private JobFunction[] CreateJobFunctions(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("DEV","Software Development","Building and maintaining software"),
            ("QA","Quality Assurance","Testing and quality processes"),
            ("DESIGN","UX / Product Design","User experience and product design"),
            ("SALES_REP","Sales Representative","Direct sales activities"),
            ("SALES_OPS","Sales Operations","Sales strategy and operations"),
            ("TALENT","Talent Acquisition","Recruiting and hiring"),
            ("HR_OPS","HR Operations","HR administration and compliance"),
            ("PAYROLL","Payroll","Compensation and payroll processing"),
            ("ACCT","Accounting","Financial accounting and reporting"),
            ("IT_SUPP","IT Support","Technical support and infrastructure"),
            ("DATA","Data & Analytics","Data engineering and analytics")
        };
        return data.Select(d => Base(new JobFunction
        {
            JobFunctionCode = d.Item1, JobFunctionName = d.Item2, Description = d.Item3, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 6 – Cost Centers
    // -------------------------------------------------------------------------

    private CostCenter[] CreateCostCenters(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("CC-ENG","Engineering Cost Center"),
            ("CC-SALES","Sales Cost Center"),
            ("CC-HR","Human Resources Cost Center"),
            ("CC-FIN","Finance Cost Center"),
            ("CC-OPS","Operations Cost Center")
        };
        return data.Select(d => Base(new CostCenter
        {
            CostCenterCode = d.Item1, CostCenterName = d.Item2, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 7 – Departments
    // -------------------------------------------------------------------------

    private Department[] CreateDepartments(CostCenter[] costCenters,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid CC(string code) => costCenters.First(c => c.CostCenterCode == code).Id;

        var data = new[]
        {
            ("ENG","Engineering",CC("CC-ENG")),
            ("SALES","Sales",CC("CC-SALES")),
            ("HR","Human Resources",CC("CC-HR")),
            ("FIN","Finance",CC("CC-FIN")),
            ("OPS","Operations",CC("CC-OPS"))
        };
        return data.Select(d => Base(new Department
        {
            DepartmentCode = d.Item1, DepartmentName = d.Item2,
            CostCenterId = d.Item3, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 8 – Designations
    // -------------------------------------------------------------------------

    private Designation[] CreateDesignations(JobFamily[] families, JobFunction[] functions, Grade[] grades,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid JF(string code) => families.First(f => f.JobFamilyCode == code).Id;
        Guid FN(string code) => functions.First(f => f.JobFunctionCode == code).Id;
        Guid GR(string code) => grades.First(g => g.GradeCode == code).Id;

        var data = new[]
        {
            ("SWE","Software Engineer",JF("ENG"),FN("DEV"),GR("G3")),
            ("SR_SWE","Senior Software Engineer",JF("ENG"),FN("DEV"),GR("G4")),
            ("LEAD_SWE","Lead Software Engineer",JF("ENG"),FN("DEV"),GR("G5")),
            ("QA_ENG","QA Engineer",JF("ENG"),FN("QA"),GR("G3")),
            ("SALES_EXEC","Sales Executive",JF("SALES"),FN("SALES_REP"),GR("G3")),
            ("SR_SALES","Senior Sales Executive",JF("SALES"),FN("SALES_OPS"),GR("G4")),
            ("RECRUITER","Recruiter",JF("HR"),FN("TALENT"),GR("G3")),
            ("HR_MGR","HR Manager",JF("HR"),FN("HR_OPS"),GR("G6")),
            ("ACCOUNTANT","Accountant",JF("FIN"),FN("ACCT"),GR("G3")),
            ("FIN_ANALYST","Financial Analyst",JF("FIN"),FN("ACCT"),GR("G4")),
            ("IT_ANALYST","IT Analyst",JF("IT"),FN("IT_SUPP"),GR("G3"))
        };
        return data.Select(d => Base(new Designation
        {
            DesignationCode = d.Item1, DesignationName = d.Item2,
            JobFamilyId = d.Item3, JobFunctionId = d.Item4, GradeId = d.Item5, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 9 – Pay Scales
    // -------------------------------------------------------------------------

    private PayScale[] CreatePayScales(string currencyCode,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("PS-ENTRY","Entry Level",30000m,55000m),
            ("PS-MID","Mid Level",55000m,85000m),
            ("PS-SENIOR","Senior Level",85000m,130000m),
            ("PS-LEAD","Lead / Management",130000m,200000m)
        };
        return data.Select(d => Base(new PayScale
        {
            PayScaleCode = d.Item1, PayScaleName = d.Item2,
            MinAmount = d.Item3, MaxAmount = d.Item4,
            CurrencyCode = currencyCode, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 10 – Job Locations
    // -------------------------------------------------------------------------

    private JobLocation[] CreateJobLocations(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("HQ","Headquarters","123 Main Street","New York","NY","10001","US"),
            ("REMOTE","Remote / Work From Home","","","","",""),
            ("NYC","New York Office","456 Park Avenue","New York","NY","10022","US"),
            ("LON","London Office","789 Baker Street","London","","W1U 6AG","GB")
        };
        return data.Select(d => Base(new JobLocation
        {
            LocationCode = d.Item1, LocationName = d.Item2, Address = d.Item3,
            City = d.Item4, StateProvince = d.Item5, PostalCode = d.Item6,
            Country = d.Item7, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 11 – Shifts
    // -------------------------------------------------------------------------

    private Shift[] CreateShifts(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("MORNING","Morning Shift",new TimeSpan(8,0,0),new TimeSpan(17,0,0)),
            ("EVENING","Evening Shift",new TimeSpan(14,0,0),new TimeSpan(23,0,0)),
            ("NIGHT","Night Shift",new TimeSpan(22,0,0),new TimeSpan(7,0,0)),
            ("FLEX","Flexible Hours",new TimeSpan(9,0,0),new TimeSpan(18,0,0))
        };
        return data.Select(d => Base(new Shift
        {
            ShiftCode = d.Item1, ShiftName = d.Item2,
            StartTime = d.Item3, EndTime = d.Item4,
            TimeZone = "UTC", IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 12 – Allowances Profiles
    // -------------------------------------------------------------------------

    private AllowancesProfile[] CreateAllowancesProfiles(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("AP-STD","Standard Allowances","Basic transport and meal allowances"),
            ("AP-SENIOR","Senior Allowances","Enhanced transport, meal, and phone allowances"),
            ("AP-EXEC","Executive Allowances","Full executive allowances including car and housing")
        };
        return data.Select(d => Base(new AllowancesProfile
        {
            AllowancesProfileCode = d.Item1, AllowancesProfileName = d.Item2,
            Description = d.Item3, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 13 – Benefits Plans
    // -------------------------------------------------------------------------

    private BenefitsPlan[] CreateBenefitsPlans(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("BP-BASIC","Basic Benefits","Essential health and dental coverage"),
            ("BP-STD","Standard Benefits","Comprehensive health, dental, vision, and 401k"),
            ("BP-PREMIUM","Premium Benefits","Full coverage plus life insurance, wellness, and stock options")
        };
        return data.Select(d => Base(new BenefitsPlan
        {
            BenefitsPlanCode = d.Item1, BenefitsPlanName = d.Item2,
            Description = d.Item3, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 14 – Talent Pools
    // -------------------------------------------------------------------------

    private TalentPool[] CreateTalentPools(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("TP-TECH","Tech Talent Pool","{\"skills\":[\"Java\",\"Python\",\"C#\"],\"minYears\":2}"),
            ("TP-SALES","Sales Talent Pool","{\"skills\":[\"Sales\",\"CRM\",\"Negotiation\"],\"minYears\":1}"),
            ("TP-MGMT","Leadership & Management","{\"grades\":[\"G6\",\"G7\",\"G8\"],\"minYears\":5}")
        };
        return data.Select(d => Base(new TalentPool
        {
            TalentPoolCode = d.Item1, TalentPoolName = d.Item2,
            CriteriaJson = d.Item3, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 15 – Screening Questionnaires
    // -------------------------------------------------------------------------

    private ScreeningQuestionnaire[] CreateScreeningQuestionnaires(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("SQ-GEN","General Screening",
             "[{\"id\":1,\"question\":\"Why are you interested in this role?\",\"type\":\"Text\"}," +
             "{\"id\":2,\"question\":\"What is your expected salary?\",\"type\":\"Number\"}," +
             "{\"id\":3,\"question\":\"Are you authorized to work in this country?\",\"type\":\"YesNo\"}]"),
            ("SQ-TECH","Technical Assessment",
             "[{\"id\":1,\"question\":\"Describe your experience with cloud platforms.\",\"type\":\"Text\"}," +
             "{\"id\":2,\"question\":\"Rate your proficiency in SQL (1-5).\",\"type\":\"Rating\"}," +
             "{\"id\":3,\"question\":\"Years of software development experience?\",\"type\":\"Number\"}]"),
            ("SQ-BEHAV","Behavioral Interview",
             "[{\"id\":1,\"question\":\"Describe a challenging project you led.\",\"type\":\"Text\"}," +
             "{\"id\":2,\"question\":\"How do you handle tight deadlines?\",\"type\":\"Text\"}," +
             "{\"id\":3,\"question\":\"Give an example of resolving team conflict.\",\"type\":\"Text\"}]")
        };
        return data.Select(d => Base(new ScreeningQuestionnaire
        {
            QuestionnaireCode = d.Item1, QuestionnaireName = d.Item2,
            QuestionsJson = d.Item3, IsActive = true, VersionNo = 1
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 16 – Skill Categories
    // -------------------------------------------------------------------------

    private SkillCategory[] CreateSkillCategories(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var data = new[]
        {
            ("SC-TECH","Technical Skills","Core technical and engineering skills"),
            ("SC-SOFT","Soft Skills","Interpersonal and professional skills"),
            ("SC-DOMAIN","Domain Knowledge","Industry and business domain expertise"),
            ("SC-LANG","Programming Languages","Coding and scripting languages")
        };
        return data.Select(d => Base(new SkillCategory
        {
            Code = d.Item1, Name = d.Item2, Description = d.Item3, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 17 – Skills
    // -------------------------------------------------------------------------

    private Skill[] CreateSkills(SkillCategory[] cats,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid Cat(string code) => cats.First(c => c.Code == code).Id;

        var data = new[]
        {
            ("S-JAVA","Java","Object-oriented language for enterprise apps",Cat("SC-LANG"),true),
            ("S-PYTHON","Python","General-purpose scripting and data language",Cat("SC-LANG"),true),
            ("S-CSHARP","C#","Microsoft .NET language for enterprise development",Cat("SC-LANG"),true),
            ("S-JS","JavaScript","Front-end and Node.js web development",Cat("SC-LANG"),false),
            ("S-SQL","SQL","Relational database query language",Cat("SC-TECH"),true),
            ("S-CLOUD","Cloud Computing","AWS, Azure, GCP infrastructure",Cat("SC-TECH"),true),
            ("S-DEVOPS","DevOps","CI/CD, Docker, Kubernetes",Cat("SC-TECH"),false),
            ("S-AGILE","Agile / Scrum","Iterative software development methodology",Cat("SC-TECH"),false),
            ("S-LEAD","Leadership","Team leadership and people management",Cat("SC-SOFT"),true),
            ("S-COMM","Communication","Verbal and written communication",Cat("SC-SOFT"),true),
            ("S-TEAMWORK","Teamwork","Collaboration and team dynamics",Cat("SC-SOFT"),false),
            ("S-SALES","Sales Technique","Consultative and solution selling",Cat("SC-DOMAIN"),true),
            ("S-CRM","CRM Tools","Salesforce, HubSpot, or similar CRM usage",Cat("SC-DOMAIN"),false)
        };
        return data.Select(d => Base(new Skill
        {
            Code = d.Item1, Name = d.Item2, Description = d.Item3,
            SkillCategoryId = d.Item4, IsCoreSkill = d.Item5, IsActive = true
        }, t)).ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 18 – Competency Frameworks + Items
    // -------------------------------------------------------------------------

    private record CompetencyData(CompetencyFramework Framework, CompetencyFrameworkItem[] Items);

    private CompetencyData[] CreateCompetencyFrameworks(
        JobFamily[] families, Skill[] skills,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid JF(string code) => families.First(f => f.JobFamilyCode == code).Id;
        Guid SK(string code) => skills.First(s => s.Code == code).Id;

        var engFramework = Base(new CompetencyFramework
        {
            Code = "CF-ENG", Name = "Engineering Competency Framework",
            Description = "Core competencies for engineering roles",
            JobFamilyId = JF("ENG"), VersionNumber = 1, IsActive = true,
            EffectiveFrom = t.now
        }, t);

        var engItems = new[]
        {
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = engFramework.Id,
                SkillId = SK("S-JAVA"), WeightPercent = 30m, MinimumRating = 3m,
                IsMandatory = true, SortOrder = 1, Notes = "Must have for backend engineers"
            }, t),
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = engFramework.Id,
                SkillId = SK("S-SQL"), WeightPercent = 25m, MinimumRating = 3m,
                IsMandatory = true, SortOrder = 2
            }, t),
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = engFramework.Id,
                SkillId = SK("S-CLOUD"), WeightPercent = 20m, MinimumRating = 2m,
                IsMandatory = false, SortOrder = 3
            }, t),
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = engFramework.Id,
                SkillId = SK("S-AGILE"), WeightPercent = 15m, MinimumRating = 2m,
                IsMandatory = false, SortOrder = 4
            }, t),
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = engFramework.Id,
                SkillId = SK("S-COMM"), WeightPercent = 10m, MinimumRating = 3m,
                IsMandatory = true, SortOrder = 5
            }, t)
        };

        var salesFramework = Base(new CompetencyFramework
        {
            Code = "CF-SALES", Name = "Sales Competency Framework",
            Description = "Core competencies for sales roles",
            JobFamilyId = JF("SALES"), VersionNumber = 1, IsActive = true,
            EffectiveFrom = t.now
        }, t);

        var salesItems = new[]
        {
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = salesFramework.Id,
                SkillId = SK("S-SALES"), WeightPercent = 40m, MinimumRating = 3m,
                IsMandatory = true, SortOrder = 1
            }, t),
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = salesFramework.Id,
                SkillId = SK("S-CRM"), WeightPercent = 25m, MinimumRating = 2m,
                IsMandatory = true, SortOrder = 2
            }, t),
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = salesFramework.Id,
                SkillId = SK("S-COMM"), WeightPercent = 25m, MinimumRating = 4m,
                IsMandatory = true, SortOrder = 3
            }, t),
            Base(new CompetencyFrameworkItem
            {
                CompetencyFrameworkId = salesFramework.Id,
                SkillId = SK("S-TEAMWORK"), WeightPercent = 10m, MinimumRating = 2m,
                IsMandatory = false, SortOrder = 4
            }, t)
        };

        return new[]
        {
            new CompetencyData(engFramework, engItems),
            new CompetencyData(salesFramework, salesItems)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 19 – Workflow Configs + Steps
    // -------------------------------------------------------------------------

    private record WorkflowData(WorkflowConfig Config, WorkflowConfigStep[] Steps);

    private WorkflowData[] CreateWorkflowConfigs(
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var jobWf = Base(new WorkflowConfig
        {
            WorkflowCode = "WF-JOB-APPROVAL", Module = "HR", TransactionType = "JobApproval",
            WorkflowName = "Job Requisition Approval", IsActive = true, TotalLevels = 2,
            EffectiveFrom = t.now, VersionNo = 1,
            Description = "Two-level approval workflow for new job requisitions"
        }, t);

        var jobSteps = new[]
        {
            Base(new WorkflowConfigStep
            {
                WorkflowConfigId = jobWf.Id, LevelNo = 1, ApproverType = "Role",
                ApproverValue = "HR_MANAGER", Mandatory = true, SLAHours = 48,
                ExecutionType = "Sequential", SortOrder = 1
            }, t),
            Base(new WorkflowConfigStep
            {
                WorkflowConfigId = jobWf.Id, LevelNo = 2, ApproverType = "Role",
                ApproverValue = "DIRECTOR", Mandatory = true, SLAHours = 72,
                ExecutionType = "Sequential", SortOrder = 2
            }, t)
        };

        var offerWf = Base(new WorkflowConfig
        {
            WorkflowCode = "WF-OFFER-APPROVAL", Module = "HR", TransactionType = "OfferApproval",
            WorkflowName = "Offer Letter Approval", IsActive = true, TotalLevels = 2,
            EffectiveFrom = t.now, VersionNo = 1,
            Description = "Two-level approval workflow for offer letters"
        }, t);

        var offerSteps = new[]
        {
            Base(new WorkflowConfigStep
            {
                WorkflowConfigId = offerWf.Id, LevelNo = 1, ApproverType = "Role",
                ApproverValue = "HR_MANAGER", Mandatory = true, SLAHours = 24,
                ExecutionType = "Sequential", SortOrder = 1
            }, t),
            Base(new WorkflowConfigStep
            {
                WorkflowConfigId = offerWf.Id, LevelNo = 2, ApproverType = "Role",
                ApproverValue = "VP_HR", Mandatory = true, SLAHours = 48,
                ExecutionType = "Sequential", SortOrder = 2
            }, t)
        };

        var requisitionWf = Base(new WorkflowConfig
        {
            WorkflowCode = "WF-REQUISITION-APPROVAL", Module = "HR", TransactionType = "Requisition",
            WorkflowName = "Job Requisition Approval", IsActive = true, TotalLevels = 2,
            EffectiveFrom = t.now, VersionNo = 1,
            Description = "Two-level approval workflow for job requisitions raised by hiring managers"
        }, t);

        var requisitionSteps = new[]
        {
            Base(new WorkflowConfigStep
            {
                WorkflowConfigId = requisitionWf.Id, LevelNo = 1, ApproverType = "Role",
                ApproverValue = "HR_MANAGER", Mandatory = true, SLAHours = 48,
                ExecutionType = "Sequential", SortOrder = 1
            }, t),
            Base(new WorkflowConfigStep
            {
                WorkflowConfigId = requisitionWf.Id, LevelNo = 2, ApproverType = "Role",
                ApproverValue = "DIRECTOR", Mandatory = true, SLAHours = 72,
                ExecutionType = "Sequential", SortOrder = 2
            }, t)
        };

        return new[]
        {
            new WorkflowData(jobWf, jobSteps),
            new WorkflowData(offerWf, offerSteps),
            new WorkflowData(requisitionWf, requisitionSteps)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 20 – Communication Templates (global unique on TemplateCode)
    // -------------------------------------------------------------------------

    private async Task<CommunicationTemplate[]> SeedCommunicationTemplatesAsync(
        Guid emailTypeId,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var result = new List<CommunicationTemplate>();

        async Task<CommunicationTemplate> EnsureTemplate(
            string code, string name, string subject, string body)
        {
            var existing = await _context.CommunicationTemplates
                .FirstOrDefaultAsync(ct => ct.TemplateCode == code && !ct.IsDeleted);
            if (existing != null) return existing;
            var ct = Base(new CommunicationTemplate
            {
                TemplateCode = code, TemplateName = name,
                TemplateTypeLookupValueId = emailTypeId,
                Subject = subject, Body = body,
                LanguageCode = "en", Version = 1,
                IsActive = true, IsSystemTemplate = true,
                PlaceholdersJson = "[\"CandidateName\",\"JobTitle\",\"CompanyName\",\"RecruiterName\"]"
            }, t);
            _context.CommunicationTemplates.Add(ct);
            result.Add(ct);
            return ct;
        }

        await EnsureTemplate("CT-INTERVIEW-INVITE", "Interview Invitation",
            "Interview Invitation – {{JobTitle}} at {{CompanyName}}",
            "Dear {{CandidateName}},\n\nWe are pleased to invite you to interview for the position of {{JobTitle}}.\n\nDate: {{InterviewDate}}\nTime: {{InterviewTime}}\nLocation: {{InterviewLocation}}\n\nPlease confirm your availability by replying to this email.\n\nBest regards,\n{{RecruiterName}}");

        await EnsureTemplate("CT-OFFER-LETTER", "Offer Letter",
            "Offer of Employment – {{JobTitle}} at {{CompanyName}}",
            "Dear {{CandidateName}},\n\nWe are delighted to offer you the position of {{JobTitle}} at {{CompanyName}}.\n\nStart Date: {{StartDate}}\nSalary: {{Salary}} per annum\nReporting to: {{ManagerName}}\n\nPlease review the attached terms and conditions.\n\nWarm regards,\n{{HRManagerName}}");

        await EnsureTemplate("CT-APP-REJECTION", "Application Rejection",
            "Update on your application for {{JobTitle}}",
            "Dear {{CandidateName}},\n\nThank you for your interest in the {{JobTitle}} role at {{CompanyName}}.\n\nAfter careful consideration, we have decided to move forward with other candidates whose experience more closely matches our current requirements.\n\nWe appreciate your time and encourage you to apply for future openings.\n\nBest regards,\n{{RecruiterName}}");

        await EnsureTemplate("CT-ONBOARDING-WELCOME", "Onboarding Welcome",
            "Welcome to {{CompanyName}}, {{CandidateName}}!",
            "Dear {{CandidateName}},\n\nWelcome to {{CompanyName}}! We are thrilled to have you join our team as {{JobTitle}}.\n\nYour start date is {{StartDate}}. Please report to {{Location}} at {{StartTime}}.\n\nYour manager {{ManagerName}} will be in touch to guide you through your first week.\n\nLooking forward to seeing you!\n\nHR Team");

        return result.ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 21 – Channel Templates
    // -------------------------------------------------------------------------

    private ChannelTemplate[] CreateChannelTemplates(
        Guid linkedInTypeId, Guid indeedTypeId, Guid siteTypeId, Guid postingDraftId,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        return new[]
        {
            Base(new ChannelTemplate
            {
                ChannelCode = "CH-LINKEDIN", ChannelName = "LinkedIn Jobs",
                ChannelTypeLookupValueId = linkedInTypeId,
                IsActive = true, SupportsAutoPosting = true, RequiresApproval = false,
                TrackingPrefix = "LI", DefaultStatusLookupValueId = postingDraftId
            }, t),
            Base(new ChannelTemplate
            {
                ChannelCode = "CH-INDEED", ChannelName = "Indeed",
                ChannelTypeLookupValueId = indeedTypeId,
                IsActive = true, SupportsAutoPosting = true, RequiresApproval = false,
                TrackingPrefix = "IND", DefaultStatusLookupValueId = postingDraftId
            }, t),
            Base(new ChannelTemplate
            {
                ChannelCode = "CH-CAREERSITE", ChannelName = "Company Career Site",
                ChannelTypeLookupValueId = siteTypeId,
                IsActive = true, SupportsAutoPosting = false, RequiresApproval = true,
                TrackingPrefix = "CS", DefaultStatusLookupValueId = postingDraftId
            }, t)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 22 – Interview Feedback Templates
    // -------------------------------------------------------------------------

    private InterviewFeedbackTemplate[] CreateInterviewFeedbackTemplates(
        Guid techTypeId, Guid behavTypeId, Guid engFamilyId,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var techQuestions =
            "[{\"id\":1,\"question\":\"Assess the candidate's algorithmic problem-solving ability.\",\"weight\":30}," +
            "{\"id\":2,\"question\":\"Rate their knowledge of system design principles.\",\"weight\":30}," +
            "{\"id\":3,\"question\":\"Evaluate coding quality and best practices.\",\"weight\":25}," +
            "{\"id\":4,\"question\":\"Assess debugging and troubleshooting skills.\",\"weight\":15}]";

        var behavQuestions =
            "[{\"id\":1,\"question\":\"Describe how the candidate handled a conflict.\",\"weight\":25}," +
            "{\"id\":2,\"question\":\"Assess their leadership and ownership mindset.\",\"weight\":25}," +
            "{\"id\":3,\"question\":\"Evaluate communication clarity and listening.\",\"weight\":25}," +
            "{\"id\":4,\"question\":\"Rate adaptability and learning agility.\",\"weight\":25}]";

        return new[]
        {
            Base(new InterviewFeedbackTemplate
            {
                TemplateCode = "IFT-TECH", TemplateName = "Technical Interview Feedback",
                InterviewTypeLookupValueId = techTypeId,
                QuestionsJson = techQuestions, RatingScale = "1-5",
                IsActive = true, VersionNo = 1,
                JobFamilyId = engFamilyId,
                EstimatedDurationMinutes = 60, IsMandatory = true,
                PassingScore = 3.0m, WeightInOverallScore = 60m,
                InstructionsForInterviewer = "Focus on practical coding ability and system design. Use the attached coding challenge.",
                InstructionsForCandidate = "You will be asked to solve coding problems and discuss system design concepts."
            }, t),
            Base(new InterviewFeedbackTemplate
            {
                TemplateCode = "IFT-BEHAV", TemplateName = "Behavioral Interview Feedback",
                InterviewTypeLookupValueId = behavTypeId,
                QuestionsJson = behavQuestions, RatingScale = "1-5",
                IsActive = true, VersionNo = 1,
                EstimatedDurationMinutes = 45, IsMandatory = true,
                PassingScore = 2.5m, WeightInOverallScore = 40m,
                InstructionsForInterviewer = "Use STAR method to elicit structured responses.",
                InstructionsForCandidate = "Prepare examples from your past work experience."
            }, t)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 23 – Job Templates
    // -------------------------------------------------------------------------

    private JobTemplate[] CreateJobTemplates(
        Department[] depts, Designation[] desigs,
        JobFamily[] families, JobFunction[] functions, Grade[] grades,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid Dept(string code) => depts.First(d => d.DepartmentCode == code).Id;
        Guid Desig(string code) => desigs.First(d => d.DesignationCode == code).Id;
        Guid JF(string code) => families.First(f => f.JobFamilyCode == code).Id;
        Guid FN(string code) => functions.First(f => f.JobFunctionCode == code).Id;
        Guid GR(string code) => grades.First(g => g.GradeCode == code).Id;

        return new[]
        {
            Base(new JobTemplate
            {
                TemplateCode = "JT-SWE", TemplateName = "Software Engineer Template",
                JobTitle = "Software Engineer",
                DepartmentId = Dept("ENG"), DesignationId = Desig("SWE"),
                EmploymentType = "FullTime",
                JobFamilyId = JF("ENG"), JobFunctionId = FN("DEV"), GradeId = GR("G3"),
                Description = "Build and maintain scalable software solutions.",
                Requirements = "3+ years of experience in software development.",
                RequiredSkills = "Java, SQL, Cloud Computing",
                Responsibilities = "Design, develop, test, and deploy software applications.",
                EducationRequirements = "Bachelor's in Computer Science or related field",
                MinExperienceYears = 2m, MaxExperienceYears = 8m,
                IsActive = true, VersionNumber = 1
            }, t),
            Base(new JobTemplate
            {
                TemplateCode = "JT-SALES-EXEC", TemplateName = "Sales Executive Template",
                JobTitle = "Sales Executive",
                DepartmentId = Dept("SALES"), DesignationId = Desig("SALES_EXEC"),
                EmploymentType = "FullTime",
                JobFamilyId = JF("SALES"), JobFunctionId = FN("SALES_REP"), GradeId = GR("G3"),
                Description = "Drive revenue growth through consultative selling.",
                Requirements = "2+ years of B2B sales experience.",
                RequiredSkills = "Sales Technique, CRM Tools, Communication",
                Responsibilities = "Prospect, qualify, and close new business opportunities.",
                EducationRequirements = "Bachelor's degree or equivalent experience",
                MinExperienceYears = 1m, MaxExperienceYears = 6m,
                IsActive = true, VersionNumber = 1
            }, t)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 24 – Onboarding Task Templates
    // -------------------------------------------------------------------------

    private OnboardingTaskTemplate[] CreateOnboardingTaskTemplates(
        Department[] depts, Designation[] desigs, JobLocation[] locations,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        return new[]
        {
            Base(new OnboardingTaskTemplate
            {
                TemplateCode = "OTT-IT-SETUP", TaskName = "IT Equipment Setup",
                TaskCategory = "IT", Description = "Set up laptop, accounts, and system access for new hire.",
                DefaultAssigneeRole = "IT_SUPPORT", DefaultDueDaysFromStart = -1,
                IsRequired = true, IsActive = true, SortOrder = 1,
                RequiresDocumentUpload = false, RequiresManagerSignoff = false,
                NotifyEmployeeOnAssign = true, NotifyAssigneeOnCreate = true
            }, t),
            Base(new OnboardingTaskTemplate
            {
                TemplateCode = "OTT-DOCS", TaskName = "Document Collection",
                TaskCategory = "HR", Description = "Collect signed employment contract, ID documents, and bank details.",
                DefaultAssigneeRole = "HR_COORDINATOR", DefaultDueDaysFromStart = 1,
                IsRequired = true, IsActive = true, SortOrder = 2,
                RequiresDocumentUpload = true, RequiresManagerSignoff = false,
                NotifyEmployeeOnAssign = true, NotifyAssigneeOnCreate = true
            }, t),
            Base(new OnboardingTaskTemplate
            {
                TemplateCode = "OTT-ORIENTATION", TaskName = "Company Orientation",
                TaskCategory = "HR", Description = "Attend company orientation session covering culture, policies, and benefits.",
                DefaultAssigneeRole = "HR_MANAGER", DefaultDueDaysFromStart = 1,
                IsRequired = true, IsActive = true, SortOrder = 3,
                RequiresDocumentUpload = false, RequiresManagerSignoff = false,
                NotifyEmployeeOnAssign = true, NotifyAssigneeOnCreate = false
            }, t),
            Base(new OnboardingTaskTemplate
            {
                TemplateCode = "OTT-MGR-MEET", TaskName = "Manager Introduction Meeting",
                TaskCategory = "Management", Description = "First 1:1 with reporting manager to discuss role expectations and 30-60-90 day plan.",
                DefaultAssigneeRole = "HIRING_MANAGER", DefaultDueDaysFromStart = 1,
                IsRequired = true, IsActive = true, SortOrder = 4,
                RequiresDocumentUpload = false, RequiresManagerSignoff = true,
                NotifyEmployeeOnAssign = true, NotifyAssigneeOnCreate = true
            }, t),
            Base(new OnboardingTaskTemplate
            {
                TemplateCode = "OTT-TRAINING", TaskName = "Initial Training Enrollment",
                TaskCategory = "Training", Description = "Enroll in mandatory compliance and role-specific training courses.",
                DefaultAssigneeRole = "HR_COORDINATOR", DefaultDueDaysFromStart = 3,
                IsRequired = true, IsActive = true, SortOrder = 5,
                RequiresDocumentUpload = false, RequiresManagerSignoff = false,
                NotifyEmployeeOnAssign = true, NotifyAssigneeOnCreate = false,
                EscalateAfterDays = 5, EscalateToRole = "HR_MANAGER"
            }, t)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 25 – Positions
    // -------------------------------------------------------------------------

    private Position[] CreatePositions(
        Department[] depts, Designation[] desigs,
        JobFamily[] families, JobFunction[] functions, Grade[] grades, PayScale[] payScales,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid Dept(string code) => depts.First(d => d.DepartmentCode == code).Id;
        Guid Desig(string code) => desigs.First(d => d.DesignationCode == code).Id;
        Guid JF(string code) => families.First(f => f.JobFamilyCode == code).Id;
        Guid FN(string code) => functions.First(f => f.JobFunctionCode == code).Id;
        Guid GR(string code) => grades.First(g => g.GradeCode == code).Id;
        Guid PS(string code) => payScales.First(p => p.PayScaleCode == code).Id;

        return new[]
        {
            Base(new Position
            {
                PositionCode = "POS-SWE-01", PositionName = "Software Engineer I",
                DepartmentId = Dept("ENG"), DesignationId = Desig("SWE"),
                JobFamilyId = JF("ENG"), JobFunctionId = FN("DEV"),
                GradeId = GR("G3"), PayScaleId = PS("PS-MID"),
                IsVacant = true, IsActive = true
            }, t),
            Base(new Position
            {
                PositionCode = "POS-SWE-02", PositionName = "Software Engineer II",
                DepartmentId = Dept("ENG"), DesignationId = Desig("SWE"),
                JobFamilyId = JF("ENG"), JobFunctionId = FN("DEV"),
                GradeId = GR("G3"), PayScaleId = PS("PS-MID"),
                IsVacant = true, IsActive = true
            }, t),
            Base(new Position
            {
                PositionCode = "POS-SR-SWE", PositionName = "Senior Software Engineer",
                DepartmentId = Dept("ENG"), DesignationId = Desig("SR_SWE"),
                JobFamilyId = JF("ENG"), JobFunctionId = FN("DEV"),
                GradeId = GR("G4"), PayScaleId = PS("PS-SENIOR"),
                IsVacant = false, IsActive = true
            }, t),
            Base(new Position
            {
                PositionCode = "POS-SALES-01", PositionName = "Sales Executive",
                DepartmentId = Dept("SALES"), DesignationId = Desig("SALES_EXEC"),
                JobFamilyId = JF("SALES"), JobFunctionId = FN("SALES_REP"),
                GradeId = GR("G3"), PayScaleId = PS("PS-MID"),
                IsVacant = true, IsActive = true
            }, t),
            Base(new Position
            {
                PositionCode = "POS-RECRUITER", PositionName = "Recruiter",
                DepartmentId = Dept("HR"), DesignationId = Desig("RECRUITER"),
                JobFamilyId = JF("HR"), JobFunctionId = FN("TALENT"),
                GradeId = GR("G3"), PayScaleId = PS("PS-MID"),
                IsVacant = false, IsActive = true
            }, t),
            Base(new Position
            {
                PositionCode = "POS-HR-MGR", PositionName = "HR Manager",
                DepartmentId = Dept("HR"), DesignationId = Desig("HR_MGR"),
                JobFamilyId = JF("HR"), JobFunctionId = FN("HR_OPS"),
                GradeId = GR("G6"), PayScaleId = PS("PS-LEAD"),
                IsVacant = false, IsActive = true
            }, t),
            Base(new Position
            {
                PositionCode = "POS-ACCOUNTANT", PositionName = "Accountant",
                DepartmentId = Dept("FIN"), DesignationId = Desig("ACCOUNTANT"),
                JobFamilyId = JF("FIN"), JobFunctionId = FN("ACCT"),
                GradeId = GR("G3"), PayScaleId = PS("PS-MID"),
                IsVacant = true, IsActive = true
            }, t),
            Base(new Position
            {
                PositionCode = "POS-IT-ANALYST", PositionName = "IT Analyst",
                DepartmentId = Dept("OPS"), DesignationId = Desig("IT_ANALYST"),
                JobFamilyId = JF("IT"), JobFunctionId = FN("IT_SUPP"),
                GradeId = GR("G3"), PayScaleId = PS("PS-MID"),
                IsVacant = true, IsActive = true
            }, t)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 26 – Employees
    // -------------------------------------------------------------------------

    private Employee[] CreateEmployees(
        Department[] depts, Designation[] desigs, Position[] positions, JobLocation[] locations,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid Dept(string code) => depts.First(d => d.DepartmentCode == code).Id;
        Guid Desig(string code) => desigs.First(d => d.DesignationCode == code).Id;
        Guid Pos(string code) => positions.First(p => p.PositionCode == code).Id;
        Guid Loc(string code) => locations.First(l => l.LocationCode == code).Id;

        return new[]
        {
            Base(new Employee
            {
                EmployeeCode = "EMP001", FirstName = "Jane", LastName = "Mitchell",
                Email = "jane.mitchell@company.com", Phone = "+1-212-555-0101",
                DepartmentId = Dept("HR"), DesignationId = Desig("HR_MGR"),
                PositionId = Pos("POS-HR-MGR"), JobLocationId = Loc("HQ"),
                Status = "Active", JoinDate = t.now.AddYears(-3), IsActive = true
            }, t),
            Base(new Employee
            {
                EmployeeCode = "EMP002", FirstName = "David", LastName = "Torres",
                Email = "david.torres@company.com", Phone = "+1-212-555-0102",
                DepartmentId = Dept("HR"), DesignationId = Desig("RECRUITER"),
                PositionId = Pos("POS-RECRUITER"), JobLocationId = Loc("HQ"),
                Status = "Active", JoinDate = t.now.AddYears(-2), IsActive = true
            }, t),
            Base(new Employee
            {
                EmployeeCode = "EMP003", FirstName = "Sarah", LastName = "Nguyen",
                Email = "sarah.nguyen@company.com", Phone = "+1-212-555-0103",
                DepartmentId = Dept("ENG"), DesignationId = Desig("SR_SWE"),
                PositionId = Pos("POS-SR-SWE"), JobLocationId = Loc("NYC"),
                Status = "Active", JoinDate = t.now.AddYears(-4), IsActive = true
            }, t),
            Base(new Employee
            {
                EmployeeCode = "EMP004", FirstName = "Marcus", LastName = "Webb",
                Email = "marcus.webb@company.com", Phone = "+1-212-555-0104",
                DepartmentId = Dept("SALES"), DesignationId = Desig("SR_SALES"),
                JobLocationId = Loc("HQ"),
                Status = "Active", JoinDate = t.now.AddYears(-5), IsActive = true
            }, t)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 27 – Jobs + Details + Posting Channels
    // -------------------------------------------------------------------------

    private record JobData(Job Job, JobDetail Detail, JobPostingChannel[] PostingChannels);

    private JobData[] CreateJobs(
        Department[] depts, Designation[] desigs,
        JobFamily[] families, JobFunction[] functions, Grade[] grades,
        PayScale[] payScales, JobLocation[] locations, Shift[] shifts,
        AllowancesProfile[] allowances, Employee[] employees,
        ChannelTemplate[] channelTemplates,
        Guid jobStatusOpenId, Guid priorityHighId, Guid priorityMedId, Guid postingPublishedId,
        string currencyCode,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid Dept(string code) => depts.First(d => d.DepartmentCode == code).Id;
        Guid Desig(string code) => desigs.First(d => d.DesignationCode == code).Id;
        Guid JF(string code) => families.First(f => f.JobFamilyCode == code).Id;
        Guid FN(string code) => functions.First(f => f.JobFunctionCode == code).Id;
        Guid GR(string code) => grades.First(g => g.GradeCode == code).Id;
        Guid PS(string code) => payScales.First(p => p.PayScaleCode == code).Id;
        Guid Loc(string code) => locations.First(l => l.LocationCode == code).Id;
        Guid Shift(string code) => shifts.First(s => s.ShiftCode == code).Id;
        Guid CT(string code) => channelTemplates.First(c => c.ChannelCode == code).Id;
        var hiringMgr = employees.First(e => e.EmployeeCode == "EMP003");
        var recruiter = employees.First(e => e.EmployeeCode == "EMP002");
        var salesMgr = employees.First(e => e.EmployeeCode == "EMP004");
        var linkedInType = channelTemplates.First(c => c.ChannelCode == "CH-LINKEDIN").ChannelTypeLookupValueId;
        var siteType = channelTemplates.First(c => c.ChannelCode == "CH-CAREERSITE").ChannelTypeLookupValueId;

        // Job 1 – Software Engineer
        var job1 = Base(new Job
        {
            JobCode = "JOB-2024-001", JobTitle = "Software Engineer",
            RecordType = JobRecordType.Job,
            DepartmentId = Dept("ENG"), DesignationId = Desig("SWE"),
            Headcount = 2, FilledCount = 0,
            EmploymentType = EmploymentType.FullTime,
            SalaryRangeMin = 70000m, SalaryRangeMax = 100000m,
            CurrencyCode = currencyCode,
            TargetStartDate = t.now.AddMonths(2),
            StatusLookupValueId = jobStatusOpenId,
            PriorityLookupValueId = priorityHighId,
            HiringManagerEmployeeId = hiringMgr.Id,
            RecruiterEmployeeId = recruiter.Id,
            PostingStartDate = t.now.AddDays(-7),
            PostingCloseDate = t.now.AddMonths(1)
        }, t);

        var job1Detail = Base(new JobDetail
        {
            JobId = job1.Id, JobLocationId = Loc("HQ"),
            JobFamilyId = JF("ENG"), JobFunctionId = FN("DEV"),
            GradeId = GR("G3"), PayScaleId = PS("PS-MID"),
            ShiftId = Shift("MORNING"), ReportingManagerEmployeeId = hiringMgr.Id,
            WorkerCategory = "Permanent", HiringType = "External",
            VacancyReason = "Business Growth",
            Description = "We are looking for a talented Software Engineer to join our growing engineering team.",
            Requirements = "3+ years of software development experience with Java or C#. Strong SQL skills required.",
            RequiredSkills = "Java, SQL, REST APIs, Agile",
            Responsibilities = "Design and develop scalable backend services, participate in code reviews, and contribute to architecture discussions.",
            EducationRequirements = "Bachelor's in Computer Science or equivalent",
            MinExperienceYears = 3m, MaxExperienceYears = 7m,
            BonusEligible = true, BudgetApprovedAmount = 100000m,
            PublishExternallyFlag = true, CareerSiteVisible = true,
            BackgroundCheckRequired = true, ReferralBonusEligible = true,
            Tags = "[\"backend\",\"java\",\"engineering\"]"
        }, t);

        var job1Channel1 = Base(new JobPostingChannel
        {
            JobId = job1.Id, ChannelTemplateId = CT("CH-LINKEDIN"),
            ChannelNameSnapshot = "LinkedIn Jobs",
            ChannelTypeLookupValueId = linkedInType,
            OpenDate = t.now.AddDays(-7), CloseDate = t.now.AddMonths(1),
            StatusLookupValueId = postingPublishedId,
            SourceTrackingCode = "LI-JOB-2024-001"
        }, t);

        var job1Channel2 = Base(new JobPostingChannel
        {
            JobId = job1.Id, ChannelTemplateId = CT("CH-CAREERSITE"),
            ChannelNameSnapshot = "Company Career Site",
            ChannelTypeLookupValueId = siteType,
            OpenDate = t.now.AddDays(-7), CloseDate = t.now.AddMonths(1),
            StatusLookupValueId = postingPublishedId,
            SourceTrackingCode = "CS-JOB-2024-001"
        }, t);

        // Job 2 – Sales Executive
        var job2 = Base(new Job
        {
            JobCode = "JOB-2024-002", JobTitle = "Sales Executive",
            RecordType = JobRecordType.Job,
            DepartmentId = Dept("SALES"), DesignationId = Desig("SALES_EXEC"),
            Headcount = 3, FilledCount = 0,
            EmploymentType = EmploymentType.FullTime,
            SalaryRangeMin = 50000m, SalaryRangeMax = 80000m,
            CurrencyCode = currencyCode,
            TargetStartDate = t.now.AddMonths(1),
            StatusLookupValueId = jobStatusOpenId,
            PriorityLookupValueId = priorityMedId,
            HiringManagerEmployeeId = salesMgr.Id,
            RecruiterEmployeeId = recruiter.Id,
            PostingStartDate = t.now.AddDays(-14),
            PostingCloseDate = t.now.AddMonths(2)
        }, t);

        var job2Detail = Base(new JobDetail
        {
            JobId = job2.Id, JobLocationId = Loc("HQ"),
            JobFamilyId = JF("SALES"), JobFunctionId = FN("SALES_REP"),
            GradeId = GR("G3"), PayScaleId = PS("PS-MID"),
            ShiftId = Shift("FLEX"), ReportingManagerEmployeeId = salesMgr.Id,
            WorkerCategory = "Permanent", HiringType = "External",
            VacancyReason = "Business Expansion",
            Description = "Join our dynamic sales team and drive revenue growth in new markets.",
            Requirements = "2+ years of B2B sales experience with proven track record.",
            RequiredSkills = "Sales Technique, CRM Tools, Communication, Negotiation",
            Responsibilities = "Prospect and qualify new leads, manage pipeline, close deals, and maintain client relationships.",
            EducationRequirements = "Bachelor's degree or equivalent experience",
            MinExperienceYears = 2m, MaxExperienceYears = 6m,
            BonusEligible = true, BudgetApprovedAmount = 80000m,
            PublishExternallyFlag = true, CareerSiteVisible = true,
            ReferralBonusEligible = true,
            Tags = "[\"sales\",\"b2b\",\"revenue\"]"
        }, t);

        var job2Channel1 = Base(new JobPostingChannel
        {
            JobId = job2.Id, ChannelTemplateId = CT("CH-LINKEDIN"),
            ChannelNameSnapshot = "LinkedIn Jobs",
            ChannelTypeLookupValueId = linkedInType,
            OpenDate = t.now.AddDays(-14), CloseDate = t.now.AddMonths(2),
            StatusLookupValueId = postingPublishedId,
            SourceTrackingCode = "LI-JOB-2024-002"
        }, t);

        return new[]
        {
            new JobData(job1, job1Detail, new[] { job1Channel1, job1Channel2 }),
            new JobData(job2, job2Detail, new[] { job2Channel1 })
        };
    }

    // -------------------------------------------------------------------------
    // Phase 28 – Candidates + Profiles
    // -------------------------------------------------------------------------

    private record CandidateData(Candidate Candidate, CandidateProfile Profile);

    private CandidateData[] CreateCandidates(Designation[] desigs,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid Desig(string code) => desigs.First(d => d.DesignationCode == code).Id;

        var cand1 = Base(new Candidate
        {
            CandidateCode = "CAND-001", FirstName = "Alice", LastName = "Wong",
            Email = "alice.wong@email.com", Phone = "+1-415-555-0201",
            Source = "LinkedIn", TotalExperienceYears = 5m,
            CurrentSalary = 85000m, ExpectedSalary = 95000m,
            CandidateRating = "A", IsBlacklisted = false, ConsentGiven = true,
            CurrentCompany = "TechCorp Inc.", CurrentDesignationId = Desig("SR_SWE"),
            DataRetentionExpiryDate = t.now.AddYears(3)
        }, t);

        var profile1 = Base(new CandidateProfile
        {
            CandidateId = cand1.Id, Gender = "Female", Nationality = "US",
            CountryOfResidence = "United States", City = "San Francisco",
            HighestEducationLevel = "Bachelor's Degree",
            HighestEducationField = "Computer Science",
            CurrentNoticePeriodDays = 30, AvailableFromDate = t.now.AddDays(30),
            RemotePreference = "Hybrid", ProfileCompletionPercent = 90,
            ConsentDate = t.now.AddDays(-5), RecruiterNotes = "Strong Java background, excellent communication."
        }, t);

        var cand2 = Base(new Candidate
        {
            CandidateCode = "CAND-002", FirstName = "Robert", LastName = "Martinez",
            Email = "robert.martinez@email.com", Phone = "+1-310-555-0202",
            Source = "Indeed", TotalExperienceYears = 3m,
            CurrentSalary = 65000m, ExpectedSalary = 75000m,
            CandidateRating = "B", IsBlacklisted = false, ConsentGiven = true,
            CurrentCompany = "StartupXYZ", CurrentDesignationId = Desig("SWE"),
            DataRetentionExpiryDate = t.now.AddYears(3)
        }, t);

        var profile2 = Base(new CandidateProfile
        {
            CandidateId = cand2.Id, Gender = "Male", Nationality = "US",
            CountryOfResidence = "United States", City = "Los Angeles",
            HighestEducationLevel = "Bachelor's Degree",
            HighestEducationField = "Software Engineering",
            CurrentNoticePeriodDays = 14, AvailableFromDate = t.now.AddDays(14),
            RemotePreference = "Remote", ProfileCompletionPercent = 75,
            ConsentDate = t.now.AddDays(-3), RecruiterNotes = "Good Python skills, looking for growth opportunity."
        }, t);

        var cand3 = Base(new Candidate
        {
            CandidateCode = "CAND-003", FirstName = "Priya", LastName = "Sharma",
            Email = "priya.sharma@email.com", Phone = "+1-212-555-0203",
            Source = "Referral", TotalExperienceYears = 4m,
            CurrentSalary = 60000m, ExpectedSalary = 70000m,
            CandidateRating = "A", IsBlacklisted = false, ConsentGiven = true,
            CurrentCompany = "SalesForce Partners", CurrentDesignationId = Desig("SALES_EXEC"),
            DataRetentionExpiryDate = t.now.AddYears(3)
        }, t);

        var profile3 = Base(new CandidateProfile
        {
            CandidateId = cand3.Id, Gender = "Female", Nationality = "US",
            CountryOfResidence = "United States", City = "New York",
            HighestEducationLevel = "Bachelor's Degree",
            HighestEducationField = "Business Administration",
            CurrentNoticePeriodDays = 21, AvailableFromDate = t.now.AddDays(21),
            RemotePreference = "Onsite", ProfileCompletionPercent = 85,
            ConsentDate = t.now.AddDays(-7), RecruiterNotes = "Referred by EMP004. Excellent sales track record."
        }, t);

        return new[]
        {
            new CandidateData(cand1, profile1),
            new CandidateData(cand2, profile2),
            new CandidateData(cand3, profile3)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 29 – Applications + Details + Compliances
    // -------------------------------------------------------------------------

    private record ApplicationData(
        JobApplication App, ApplicationDetail Detail, ApplicationCompliance Compliance);

    private ApplicationData[] CreateApplications(
        JobData[] jobs, CandidateData[] candidates, Employee[] employees,
        Guid stageAppliedId, Guid stageScreenedId, Guid stageShortlistedId, Guid appActiveId,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var job1 = jobs[0];
        var job2 = jobs[1];
        var cand1 = candidates[0].Candidate;
        var cand2 = candidates[1].Candidate;
        var cand3 = candidates[2].Candidate;
        var recruiter = employees.First(e => e.EmployeeCode == "EMP002");
        var job1Channel1 = job1.PostingChannels[0];
        var job2Channel1 = job2.PostingChannels[0];

        // App 1: Alice Wong applied for Software Engineer (screened)
        var app1 = Base(new JobApplication
        {
            ApplicationCode = "APP-2024-001",
            JobId = job1.Job.Id, CandidateId = cand1.Id,
            JobPostingChannelId = job1Channel1.Id,
            AppliedDate = t.now.AddDays(-10),
            CurrentStageLookupValueId = stageScreenedId,
            StatusLookupValueId = appActiveId,
            IsShortlisted = false, ScreeningScore = 78m,
            AssignedRecruiterEmployeeId = recruiter.Id,
            StageChangedAt = t.now.AddDays(-8)
        }, t);

        var detail1 = Base(new ApplicationDetail
        {
            ApplicationId = app1.Id,
            MinQualificationsMet = "Yes", OverallRating = "4",
            RecruiterNotes = "Strong profile, meets all minimum requirements.",
            ApplicationDeadline = t.now.AddMonths(1),
            NextFollowUpDate = t.now.AddDays(2),
            AssignedHiringManagerEmployeeId = employees.First(e => e.EmployeeCode == "EMP003").Id
        }, t);

        var compliance1 = Base(new ApplicationCompliance
        {
            ApplicationId = app1.Id,
            EEODataCaptured = true,
            RightToWorkVerified = true,
            DataConsentGiven = true, DataConsentDate = t.now.AddDays(-10),
            DataRetentionExpiryDate = t.now.AddYears(3)
        }, t);

        // App 2: Robert Martinez applied for Software Engineer (applied)
        var app2 = Base(new JobApplication
        {
            ApplicationCode = "APP-2024-002",
            JobId = job1.Job.Id, CandidateId = cand2.Id,
            JobPostingChannelId = job1Channel1.Id,
            AppliedDate = t.now.AddDays(-5),
            CurrentStageLookupValueId = stageAppliedId,
            StatusLookupValueId = appActiveId,
            IsShortlisted = false, ScreeningScore = 65m,
            AssignedRecruiterEmployeeId = recruiter.Id,
            StageChangedAt = t.now.AddDays(-5)
        }, t);

        var detail2 = Base(new ApplicationDetail
        {
            ApplicationId = app2.Id,
            MinQualificationsMet = "Yes", OverallRating = "3",
            RecruiterNotes = "Needs screening call to assess technical depth.",
            ApplicationDeadline = t.now.AddMonths(1),
            NextFollowUpDate = t.now.AddDays(1)
        }, t);

        var compliance2 = Base(new ApplicationCompliance
        {
            ApplicationId = app2.Id,
            EEODataCaptured = false,
            DataConsentGiven = true, DataConsentDate = t.now.AddDays(-5),
            DataRetentionExpiryDate = t.now.AddYears(3)
        }, t);

        // App 3: Priya Sharma applied for Sales Executive (shortlisted)
        var app3 = Base(new JobApplication
        {
            ApplicationCode = "APP-2024-003",
            JobId = job2.Job.Id, CandidateId = cand3.Id,
            JobPostingChannelId = job2Channel1.Id,
            AppliedDate = t.now.AddDays(-12),
            CurrentStageLookupValueId = stageShortlistedId,
            StatusLookupValueId = appActiveId,
            IsShortlisted = true, ScreeningScore = 88m,
            AssignedRecruiterEmployeeId = recruiter.Id,
            StageChangedAt = t.now.AddDays(-6)
        }, t);

        var detail3 = Base(new ApplicationDetail
        {
            ApplicationId = app3.Id,
            MinQualificationsMet = "Yes", OverallRating = "4",
            RecruiterNotes = "Excellent candidate. Referred by EMP004. Schedule interview.",
            ApplicationDeadline = t.now.AddMonths(2),
            NextFollowUpDate = t.now.AddDays(3),
            AssignedHiringManagerEmployeeId = employees.First(e => e.EmployeeCode == "EMP004").Id
        }, t);

        var compliance3 = Base(new ApplicationCompliance
        {
            ApplicationId = app3.Id,
            EEODataCaptured = true,
            RightToWorkVerified = true,
            DataConsentGiven = true, DataConsentDate = t.now.AddDays(-12),
            DataRetentionExpiryDate = t.now.AddYears(3)
        }, t);

        return new[]
        {
            new ApplicationData(app1, detail1, compliance1),
            new ApplicationData(app2, detail2, compliance2),
            new ApplicationData(app3, detail3, compliance3)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 30 – Job Requisitions
    // -------------------------------------------------------------------------

    private record RequisitionData(Job Job, JobDetail Detail);

    private RequisitionData[] CreateJobRequisitions(
        Department[] depts, Designation[] desigs,
        JobFamily[] families, JobFunction[] functions, Grade[] grades,
        PayScale[] payScales, JobLocation[] locations, Shift[] shifts,
        Employee[] employees,
        Guid jobStatusOpenId, Guid priorityHighId, Guid priorityMedId, string currencyCode,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        Guid Dept(string code)  => depts.First(d => d.DepartmentCode == code).Id;
        Guid Desig(string code) => desigs.First(d => d.DesignationCode == code).Id;
        Guid JF(string code)    => families.First(f => f.JobFamilyCode == code).Id;
        Guid FN(string code)    => functions.First(f => f.JobFunctionCode == code).Id;
        Guid GR(string code)    => grades.First(g => g.GradeCode == code).Id;
        Guid PS(string code)    => payScales.First(p => p.PayScaleCode == code).Id;
        Guid Loc(string code)   => locations.First(l => l.LocationCode == code).Id;
        Guid Sh(string code)    => shifts.First(s => s.ShiftCode == code).Id;
        var hiringMgr = employees.First(e => e.EmployeeCode == "EMP003");
        var recruiter = employees.First(e => e.EmployeeCode == "EMP002");
        var hrMgr     = employees.First(e => e.EmployeeCode == "EMP001");

        // Requisition 1 – Lead Software Engineer
        var req1 = Base(new Job
        {
            JobCode = "RQSTN-2024-001", JobTitle = "Lead Software Engineer",
            RecordType = JobRecordType.Requisition,
            DepartmentId = Dept("ENG"), DesignationId = Desig("LEAD_SWE"),
            Headcount = 1, FilledCount = 0,
            EmploymentType = EmploymentType.FullTime,
            SalaryRangeMin = 100000m, SalaryRangeMax = 140000m, CurrencyCode = currencyCode,
            TargetStartDate = t.now.AddMonths(3),
            StatusLookupValueId = jobStatusOpenId,
            PriorityLookupValueId = priorityHighId,
            HiringManagerEmployeeId = hiringMgr.Id,
            RecruiterEmployeeId = recruiter.Id,
            PostingStartDate = t.now, PostingCloseDate = t.now.AddMonths(2)
        }, t);

        var req1Detail = Base(new JobDetail
        {
            JobId = req1.Id, JobLocationId = Loc("HQ"),
            JobFamilyId = JF("ENG"), JobFunctionId = FN("DEV"),
            GradeId = GR("G5"), PayScaleId = PS("PS-LEAD"),
            ShiftId = Sh("MORNING"), ReportingManagerEmployeeId = hiringMgr.Id,
            WorkerCategory = "Permanent", HiringType = "External",
            VacancyReason = "Business Growth",
            Description = "Seeking a Lead Software Engineer to drive technical excellence across the engineering team.",
            Requirements = "7+ years of software development with team leadership experience.",
            RequiredSkills = "Java, C#, Cloud Computing, Agile",
            Responsibilities = "Lead development efforts, mentor junior engineers, and drive architecture decisions.",
            EducationRequirements = "Bachelor's in Computer Science or equivalent",
            MinExperienceYears = 7m, MaxExperienceYears = 12m,
            BonusEligible = true, BudgetApprovedAmount = 140000m,
            PublishExternallyFlag = false, CareerSiteVisible = false,
            BackgroundCheckRequired = true
        }, t);

        // Requisition 2 – HR Business Partner
        var req2 = Base(new Job
        {
            JobCode = "RQSTN-2024-002", JobTitle = "HR Business Partner",
            RecordType = JobRecordType.Requisition,
            DepartmentId = Dept("HR"), DesignationId = Desig("HR_MGR"),
            Headcount = 1, FilledCount = 0,
            EmploymentType = EmploymentType.FullTime,
            SalaryRangeMin = 70000m, SalaryRangeMax = 95000m, CurrencyCode = currencyCode,
            TargetStartDate = t.now.AddMonths(2),
            StatusLookupValueId = jobStatusOpenId,
            PriorityLookupValueId = priorityMedId,
            HiringManagerEmployeeId = hrMgr.Id,
            RecruiterEmployeeId = recruiter.Id,
            PostingStartDate = t.now, PostingCloseDate = t.now.AddMonths(2)
        }, t);

        var req2Detail = Base(new JobDetail
        {
            JobId = req2.Id, JobLocationId = Loc("HQ"),
            JobFamilyId = JF("HR"), JobFunctionId = FN("HR_OPS"),
            GradeId = GR("G6"), PayScaleId = PS("PS-LEAD"),
            ShiftId = Sh("MORNING"), ReportingManagerEmployeeId = hrMgr.Id,
            WorkerCategory = "Permanent", HiringType = "Internal",
            VacancyReason = "Business Growth",
            Description = "Seeking an experienced HR Business Partner to support organisational growth.",
            Requirements = "5+ years in HR with experience as an HRBP or HR Generalist.",
            RequiredSkills = "HR Operations, Talent Management, Employee Relations",
            Responsibilities = "Partner with business leaders on workforce planning, performance management, and culture.",
            EducationRequirements = "Bachelor's in Human Resources or Business Administration",
            MinExperienceYears = 5m, MaxExperienceYears = 10m,
            BonusEligible = true, BudgetApprovedAmount = 95000m,
            PublishExternallyFlag = false, CareerSiteVisible = false
        }, t);

        return new[]
        {
            new RequisitionData(req1, req1Detail),
            new RequisitionData(req2, req2Detail)
        };
    }

    // -------------------------------------------------------------------------
    // Phase 31 – Approval Requests + Steps
    // -------------------------------------------------------------------------

    private record ApprovalData(ApprovalRequest Request, ApprovalRequestStep[] Steps);

    private ApprovalData[] CreateRequisitionApprovals(
        RequisitionData[] requisitions, WorkflowData workflow, Employee[] employees,
        Guid inProgressStatusId, Guid pendingStatusId, Guid waitingStatusId, Guid priorityHighId,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var requester = employees.First(e => e.EmployeeCode == "EMP001");
        var result = new List<ApprovalData>();
        var seq = 1;

        foreach (var req in requisitions)
        {
            var approval = Base(new ApprovalRequest
            {
                ApprovalRequestCode   = $"APR-REQUISITION-{t.now:yyyyMMdd}-{seq:D3}",
                EntityType            = "Requisition",
                EntityId              = req.Job.Id,
                WorkflowConfigId      = workflow.Config.Id,
                RequestedByEmployeeId = requester.Id,
                CurrentLevel          = 1,
                TotalLevels           = workflow.Steps.Length,
                OverallStatusLookupValueId = inProgressStatusId,
                PriorityLookupValueId = priorityHighId,
                RequestedAt           = t.now.AddDays(-seq),
                ApprovalSubjectCode   = req.Job.JobCode,
                ApprovalSubjectTitle  = req.Job.JobTitle,
                ApprovalSummary       = $"Job Requisition: {req.Job.JobTitle} – Headcount: {req.Job.Headcount}",
                ApprovalDisplayName   = $"{req.Job.JobCode} – {req.Job.JobTitle}"
            }, t);

            var steps = workflow.Steps
                .OrderBy(s => s.SortOrder)
                .Select((wfStep, i) => Base(new ApprovalRequestStep
                {
                    ApprovalRequestId    = approval.Id,
                    WorkflowConfigStepId = wfStep.Id,
                    StepLevel            = wfStep.LevelNo,
                    ApproverType         = wfStep.ApproverType,
                    ApproverValue        = wfStep.ApproverValue,
                    Mandatory            = wfStep.Mandatory,
                    SLAHours             = wfStep.SLAHours,
                    ExecutionType        = wfStep.ExecutionType,
                    StatusLookupValueId  = i == 0 ? pendingStatusId : waitingStatusId
                }, t))
                .ToArray();

            result.Add(new ApprovalData(approval, steps));
            seq++;
        }

        return result.ToArray();
    }

    // -------------------------------------------------------------------------
    // Phase 32 – Interviews
    // -------------------------------------------------------------------------

    private Interview[] CreateInterviews(
        ApplicationData[] apps, JobData[] jobs, CandidateData[] candidates,
        Employee[] employees, InterviewFeedbackTemplate[] iftTemplates,
        Guid techTypeId, Guid behavTypeId, Guid scheduledStatusId, Guid completedStatusId,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var recruiter = employees.First(e => e.EmployeeCode == "EMP002");
        var techTemplate  = iftTemplates.FirstOrDefault(f => f.TemplateCode == "IFT-TECH");
        var behavTemplate = iftTemplates.FirstOrDefault(f => f.TemplateCode == "IFT-BEHAV");

        var app1 = apps[0]; // Alice Wong → Software Engineer
        var app3 = apps[2]; // Priya Sharma → Sales Executive

        // Interview 1 – Technical for Alice Wong (scheduled, upcoming)
        var int1 = Base(new Interview
        {
            InterviewCode               = "INT-2024-001",
            ApplicationId               = app1.App.Id,
            CandidateId                 = app1.App.CandidateId,
            JobId                       = app1.App.JobId,
            InterviewTitle              = "Technical Interview – Software Engineer",
            InterviewTypeLookupValueId  = techTypeId,
            InterviewSequenceNo         = 1,
            StatusLookupValueId         = scheduledStatusId,
            InterviewFeedbackTemplateId = techTemplate?.Id,
            ScheduledStart              = t.now.AddDays(3),
            ScheduledEnd                = t.now.AddDays(3).AddHours(1),
            DurationMinutes             = 60,
            Format                      = InterviewFormat.VideoCall,
            VideoLink                   = "https://meet.example.com/int-2024-001",
            ProposedByEmployeeId        = recruiter.Id,
            ProposedAt                  = t.now.AddDays(-2),
            CandidateConfirmed          = true,
            CandidateConfirmedAt        = t.now.AddDays(-1),
            FeedbackDueDate             = t.now.AddDays(4),
            IsMandatoryRound            = true
        }, t);

        // Interview 2 – Behavioral for Priya Sharma (completed, has a score)
        var int2 = Base(new Interview
        {
            InterviewCode               = "INT-2024-002",
            ApplicationId               = app3.App.Id,
            CandidateId                 = app3.App.CandidateId,
            JobId                       = app3.App.JobId,
            InterviewTitle              = "Behavioral Interview – Sales Executive",
            InterviewTypeLookupValueId  = behavTypeId,
            InterviewSequenceNo         = 1,
            StatusLookupValueId         = completedStatusId,
            InterviewFeedbackTemplateId = behavTemplate?.Id,
            ScheduledStart              = t.now.AddDays(-5),
            ScheduledEnd                = t.now.AddDays(-5).AddMinutes(45),
            DurationMinutes             = 45,
            Format                      = InterviewFormat.InPerson,
            Location                    = "HQ – Conference Room A",
            ProposedByEmployeeId        = recruiter.Id,
            ProposedAt                  = t.now.AddDays(-10),
            CandidateConfirmed          = true,
            CandidateConfirmedAt        = t.now.AddDays(-8),
            ActualStart                 = t.now.AddDays(-5),
            ActualEnd                   = t.now.AddDays(-5).AddMinutes(50),
            ActualDurationMinutes       = 50,
            WeightedPanelScore          = 4.2m,
            FeedbackSubmittedCount      = 1,
            FeedbackDueDate             = t.now.AddDays(-4),
            IsMandatoryRound            = true
        }, t);

        return new[] { int1, int2 };
    }

    // -------------------------------------------------------------------------
    // Phase 33 – Offer Letters + Details
    // -------------------------------------------------------------------------

    private record OfferData(OfferLetter Offer, OfferLetterDetail Detail);

    private OfferData[] CreateOfferLetters(
        ApplicationData[] apps, JobData[] jobs, CandidateData[] candidates,
        Employee[] employees, Grade[] grades, PayScale[] payScales,
        BenefitsPlan[] benefits, AllowancesProfile[] allowances,
        Guid offerSentId, Guid offerPendingId, Guid candRespNoneId, string currencyCode,
        (Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, DateTime now) t)
    {
        var hrMgr     = employees.First(e => e.EmployeeCode == "EMP001");
        var recruiter = employees.First(e => e.EmployeeCode == "EMP002");
        var salesMgr  = employees.First(e => e.EmployeeCode == "EMP004");

        Guid GR(string code) => grades.First(g => g.GradeCode == code).Id;
        Guid PS(string code) => payScales.First(p => p.PayScaleCode == code).Id;
        Guid BP(string code) => benefits.First(b => b.BenefitsPlanCode == code).Id;
        Guid AP(string code) => allowances.First(a => a.AllowancesProfileCode == code).Id;

        var app3 = apps[2]; // Priya Sharma – Sales Executive (shortlisted)
        var job2 = jobs[1]; // Sales Executive job

        // Offer 1 – Priya Sharma, Sales Executive (sent, awaiting response)
        var offer1 = Base(new OfferLetter
        {
            OfferCode                      = "OFR-2024-001",
            ApplicationId                  = app3.App.Id,
            CandidateId                    = app3.App.CandidateId,
            JobId                          = job2.Job.Id,
            JobTitle                       = "Sales Executive",
            ReportingManagerEmployeeId     = salesMgr.Id,
            BaseSalary                     = 68000m,
            TotalPackage                   = 76000m,
            CurrencyCode                     = currencyCode,
            EmploymentType                 = EmploymentType.FullTime,
            StartDate                      = t.now.AddDays(30),
            ExpiryDate                     = t.now.AddDays(10),
            ProbationPeriodMonths          = 3,
            NoticePeriodDays               = 30,
            StatusLookupValueId            = offerSentId,
            CandidateResponseLookupValueId = candRespNoneId,
            VersionNumber                  = 1,
            SentDate                       = t.now.AddDays(-1),
            SentByEmployeeId               = hrMgr.Id,
            SentVia                        = "Email"
        }, t);

        var detail1 = Base(new OfferLetterDetail
        {
            OfferLetterId        = offer1.Id,
            HousingAllowance     = 3000m,
            TransportAllowance   = 1500m,
            MedicalAllowance     = 1000m,
            OtherAllowances      = 500m,
            BonusTarget          = 8000m,
            BonusPercent         = 12m,
            GradeId              = GR("G3"),
            PayScaleId           = PS("PS-MID"),
            BenefitsPlanId       = BP("BP-STD"),
            AllowancesProfileId  = AP("AP-STD"),
            BenefitsSummary      = "Standard health, dental, vision, and 401k package.",
            IsDigitallySigned    = false
        }, t);

        // Offer 2 – Alice Wong, Software Engineer (pending approval)
        var app1 = apps[0];
        var job1 = jobs[0];

        var offer2 = Base(new OfferLetter
        {
            OfferCode                      = "OFR-2024-002",
            ApplicationId                  = app1.App.Id,
            CandidateId                    = app1.App.CandidateId,
            JobId                          = job1.Job.Id,
            JobTitle                       = "Software Engineer",
            ReportingManagerEmployeeId     = employees.First(e => e.EmployeeCode == "EMP003").Id,
            BaseSalary                     = 90000m,
            TotalPackage                   = 100000m,
            CurrencyCode                     = currencyCode,
            EmploymentType                 = EmploymentType.FullTime,
            StartDate                      = t.now.AddDays(45),
            ExpiryDate                     = t.now.AddDays(14),
            ProbationPeriodMonths          = 3,
            NoticePeriodDays               = 30,
            StatusLookupValueId            = offerPendingId,
            CandidateResponseLookupValueId = candRespNoneId,
            VersionNumber                  = 1
        }, t);

        var detail2 = Base(new OfferLetterDetail
        {
            OfferLetterId        = offer2.Id,
            HousingAllowance     = 4000m,
            TransportAllowance   = 2000m,
            MedicalAllowance     = 1500m,
            OtherAllowances      = 500m,
            BonusTarget          = 10000m,
            BonusPercent         = 11m,
            GradeId              = GR("G3"),
            PayScaleId           = PS("PS-MID"),
            BenefitsPlanId       = BP("BP-STD"),
            AllowancesProfileId  = AP("AP-STD"),
            BenefitsSummary      = "Standard health, dental, vision, and 401k package.",
            IsDigitallySigned    = false
        }, t);

        return new[]
        {
            new OfferData(offer1, detail1),
            new OfferData(offer2, detail2)
        };
    }
}
