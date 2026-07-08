using Microsoft.EntityFrameworkCore;
using Hr.Domain.Entities;

namespace Hr.Infrastructure.Persistence;

/// <summary>
/// HR module DbContext
/// Default Schema: hr
/// Covers: Recruitment, Candidate Management, Interviews, Offers, Onboarding, Lookups
/// </summary>
public class HrDbContext : DbContext
{
    private const string DefaultSchema = "hr";

    public HrDbContext(DbContextOptions<HrDbContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // Salaries, allowances, scores and percentages default to (18,2); geo-coordinates are
        // overridden to (9,6) in OnModelCreating. Keeps every decimal off EF's implicit convention.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    // Lookup
    public DbSet<LookupType> LookupTypes { get; set; } = null!;
    public DbSet<LookupValue> LookupValues { get; set; } = null!;

    // Skills
    public DbSet<SkillCategory> SkillCategories { get; set; } = null!;
    public DbSet<Skill> Skills { get; set; } = null!;
    public DbSet<CandidateSkill> CandidateSkills { get; set; } = null!;

    // Competency
    public DbSet<CompetencyFramework> CompetencyFrameworks { get; set; } = null!;
    public DbSet<CompetencyFrameworkItem> CompetencyFrameworkItems { get; set; } = null!;

    // Job
    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<JobDetail> JobDetails { get; set; } = null!;
    public DbSet<JobTemplate> JobTemplates { get; set; } = null!;
    public DbSet<JobPostingChannel> JobPostingChannels { get; set; } = null!;

    // Approval
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; } = null!;
    public DbSet<ApprovalRequestStep> ApprovalRequestSteps { get; set; } = null!;

    // Candidate
    public DbSet<Candidate> Candidates { get; set; } = null!;
    public DbSet<CandidateProfile> CandidateProfiles { get; set; } = null!;
    public DbSet<CandidateAddress> CandidateAddresses { get; set; } = null!;
    public DbSet<CandidateContact> CandidateContacts { get; set; } = null!;
    public DbSet<CandidateMediaLink> CandidateMediaLinks { get; set; } = null!;
    public DbSet<CandidateStageHistory> CandidateStageHistories { get; set; } = null!;
    public DbSet<CallLog> CallLogs { get; set; } = null!;

    // Application
    public DbSet<Hr.Domain.Entities.Application> Applications { get; set; } = null!;
    public DbSet<ApplicationDetail> ApplicationDetails { get; set; } = null!;
    public DbSet<ApplicationCompliance> ApplicationCompliances { get; set; } = null!;

    // Candidate Tasks
    public DbSet<CandidateTask> CandidateTasks { get; set; } = null!;
    public DbSet<CandidateTaskSubmission> CandidateTaskSubmissions { get; set; } = null!;
    public DbSet<CandidateTaskEvaluation> CandidateTaskEvaluations { get; set; } = null!;

    // Interview
    public DbSet<Interview> Interviews { get; set; } = null!;
    public DbSet<InterviewPanelMember> InterviewPanelMembers { get; set; } = null!;
    public DbSet<InterviewFeedback> InterviewFeedbacks { get; set; } = null!;
    public DbSet<InterviewFeedbackTemplate> InterviewFeedbackTemplates { get; set; } = null!;
    public DbSet<InterviewNotification> InterviewNotifications { get; set; } = null!;
    public DbSet<InterviewerAvailability> InterviewerAvailabilities { get; set; } = null!;

    // Offer
    public DbSet<OfferLetter> OfferLetters { get; set; } = null!;
    public DbSet<OfferLetterDetail> OfferLetterDetails { get; set; } = null!;
    public DbSet<OfferNegotiation> OfferNegotiations { get; set; } = null!;

    // Onboarding
    public DbSet<OnboardingTask> OnboardingTasks { get; set; } = null!;
    public DbSet<OnboardingTaskTemplate> OnboardingTaskTemplates { get; set; } = null!;

    // Workflow
    public DbSet<WorkflowConfig> WorkflowConfigs { get; set; } = null!;
    public DbSet<WorkflowConfigStep> WorkflowConfigSteps { get; set; } = null!;
    public DbSet<WorkflowCondition> WorkflowConditions { get; set; } = null!;
    public DbSet<WorkflowEscalation> WorkflowEscalations { get; set; } = null!;

    // Channel
    public DbSet<ChannelTemplate> ChannelTemplates { get; set; } = null!;

    // NEW ENTITIES

    // Currency
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<PayScale> PayScales { get; set; } = null!;
    public DbSet<AllowancesProfile> AllowancesProfiles { get; set; } = null!;
    public DbSet<BenefitsPlan> BenefitsPlans { get; set; } = null!;

    // Recruiting support
    public DbSet<ScreeningQuestionnaire> ScreeningQuestionnaires { get; set; } = null!;
    public DbSet<TalentPool> TalentPools { get; set; } = null!;
    public DbSet<CommunicationTemplate> CommunicationTemplates { get; set; } = null!;

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
        ApplyUtcDateTimeConverters(modelBuilder);

        // Geo-coordinates need 6 decimal places (~0.1 m); (18,2) would round them uselessly.
        foreach (var p in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                                 && (p.Name is "Latitude" or "Longitude")))
        {
            p.SetPrecision(9);
            p.SetScale(6);
        }

        // Lookups

        modelBuilder.Entity<LookupType>(entity =>
        {
            entity.ToTable("LookupTypes", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<LookupValue>(entity =>
        {
            entity.ToTable("LookupValues", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.LookupType).WithMany(t => t.Values)
                  .HasForeignKey(e => e.LookupTypeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ParentLookupValue).WithMany(v => v.Children)
                  .HasForeignKey(e => e.ParentLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        // Skills

        modelBuilder.Entity<SkillCategory>(entity =>
        {
            entity.ToTable("SkillCategories", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("Skills", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.SkillCategory).WithMany(c => c.Skills)
                  .HasForeignKey(e => e.SkillCategoryId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.SkillTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.SkillTypeLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CandidateSkill>(entity =>
        {
            entity.ToTable("CandidateSkills", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Candidate).WithMany(c => c.CandidateSkills)
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Skill).WithMany(s => s.CandidateSkills)
                  .HasForeignKey(e => e.SkillId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ProficiencyLookupValue).WithMany()
                  .HasForeignKey(e => e.ProficiencyLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.VerifiedByEmployee).WithMany()
                  .HasForeignKey(e => e.VerifiedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        // Competency

        modelBuilder.Entity<CompetencyFramework>(entity =>
        {
            entity.ToTable("CompetencyFrameworks", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<CompetencyFrameworkItem>(entity =>
        {
            entity.ToTable("CompetencyFrameworkItems", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CompetencyFramework).WithMany(f => f.Items)
                  .HasForeignKey(e => e.CompetencyFrameworkId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Skill).WithMany(s => s.CompetencyFrameworkItems)
                  .HasForeignKey(e => e.SkillId).OnDelete(DeleteBehavior.NoAction);
        });

        // Workflow

        modelBuilder.Entity<WorkflowConfig>(entity =>
        {
            entity.ToTable("WorkflowConfigs", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkflowName).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<WorkflowConfigStep>(entity =>
        {
            entity.ToTable("WorkflowConfigSteps", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.WorkflowConfig).WithMany(w => w.Steps)
                  .HasForeignKey(e => e.WorkflowConfigId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowCondition>(entity =>
        {
            entity.ToTable("WorkflowConditions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.WorkflowConfig).WithMany(w => w.Conditions)
                  .HasForeignKey(e => e.WorkflowConfigId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowEscalation>(entity =>
        {
            entity.ToTable("WorkflowEscalations", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.WorkflowConfigStep).WithMany(s => s.Escalations)
                  .HasForeignKey(e => e.WorkflowConfigStepId).OnDelete(DeleteBehavior.Cascade);
        });

        // Approval

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.ToTable("ApprovalRequests", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.WorkflowConfig).WithMany(w => w.ApprovalRequests)
                  .HasForeignKey(e => e.WorkflowConfigId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.RequestedByEmployee).WithMany()
                  .HasForeignKey(e => e.RequestedByEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.OverallStatusLookupValue).WithMany()
                  .HasForeignKey(e => e.OverallStatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.PriorityLookupValue).WithMany()
                  .HasForeignKey(e => e.PriorityLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ApprovalRequestStep>(entity =>
        {
            entity.ToTable("ApprovalRequestSteps", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ApprovalRequest).WithMany(r => r.Steps)
                  .HasForeignKey(e => e.ApprovalRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.WorkflowConfigStep).WithMany()
                  .HasForeignKey(e => e.WorkflowConfigStepId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ApproverEmployee).WithMany()
                  .HasForeignKey(e => e.ApproverEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.DelegatedToEmployee).WithMany()
                  .HasForeignKey(e => e.DelegatedToEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.EscalatedToEmployee).WithMany()
                  .HasForeignKey(e => e.EscalatedToEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        // Organisation structure

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.ToTable("Currencies", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.CurrencyName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.CurrencyCode).IsUnique();
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.ToTable("Grades", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GradeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.GradeName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<JobFamily>(entity =>
        {
            entity.ToTable("JobFamilies", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobFamilyCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.JobFamilyName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<JobFunction>(entity =>
        {
            entity.ToTable("JobFunctions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobFunctionCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.JobFunctionName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Designation>(entity =>
        {
            entity.ToTable("Designations", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DesignationCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DesignationName).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.JobFamily).WithMany(jf => jf.Designations)
                  .HasForeignKey(e => e.JobFamilyId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.JobFunction).WithMany(jf => jf.Designations)
                  .HasForeignKey(e => e.JobFunctionId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Grade).WithMany(g => g.Designations)
                  .HasForeignKey(e => e.GradeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CostCenter>(entity =>
        {
            entity.ToTable("CostCenters", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostCenterCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CostCenterName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<JobLocation>(entity =>
        {
            entity.ToTable("JobLocations", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LocationCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LocationName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.ToTable("Shifts", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShiftCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ShiftName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DepartmentCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.ParentDepartment).WithMany(d => d.ChildDepartments)
                  .HasForeignKey(e => e.ParentDepartmentId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CostCenter).WithMany()
                  .HasForeignKey(e => e.CostCenterId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            // DepartmentHeadEmployeeId configured after Employee to avoid forward-reference issues
        });

        modelBuilder.Entity<PayScale>(entity =>
        {
            entity.ToTable("PayScales", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PayScaleCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PayScaleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MinAmount).HasPrecision(18, 2);
            entity.Property(e => e.MaxAmount).HasPrecision(18, 2);
            entity.HasOne(e => e.Currency).WithMany(c => c.PayScales)
                  .HasForeignKey(e => e.CurrencyId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable("Positions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PositionCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PositionName).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Department).WithMany(d => d.Positions)
                  .HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Designation).WithMany(d => d.Positions)
                  .HasForeignKey(e => e.DesignationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.JobFamily).WithMany(jf => jf.Positions)
                  .HasForeignKey(e => e.JobFamilyId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.JobFunction).WithMany()
                  .HasForeignKey(e => e.JobFunctionId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Grade).WithMany(g => g.Positions)
                  .HasForeignKey(e => e.GradeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.PayScale).WithMany(ps => ps.Positions)
                  .HasForeignKey(e => e.PayScaleId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ReportsToPosition).WithMany(p => p.ChildPositions)
                  .HasForeignKey(e => e.ReportsToPositionId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            // Employee code is unique per tenant.
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId, e.EmployeeCode })
                .IsUnique().HasDatabaseName("IX_Employee_Tenant_Code");
            entity.HasOne(e => e.Department).WithMany(d => d.Employees)
                  .HasForeignKey(e => e.DepartmentId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Designation).WithMany(d => d.Employees)
                  .HasForeignKey(e => e.DesignationId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Position).WithMany(p => p.Employees)
                  .HasForeignKey(e => e.PositionId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ReportingManager).WithMany(e => e.DirectReports)
                  .HasForeignKey(e => e.ReportingManagerId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.JobLocation).WithMany(l => l.Employees)
                  .HasForeignKey(e => e.JobLocationId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        // Wire Department.DepartmentHeadEmployee after Employee is configured
        modelBuilder.Entity<Department>()
            .HasOne(e => e.DepartmentHeadEmployee).WithMany()
            .HasForeignKey(e => e.DepartmentHeadEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CostCenter>()
            .HasOne(e => e.Department).WithMany()
            .HasForeignKey(e => e.DepartmentId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CostCenter>()
            .HasOne(e => e.BudgetOwnerEmployee).WithMany()
            .HasForeignKey(e => e.BudgetOwnerEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);

        // Channel / Communication

        modelBuilder.Entity<ChannelTemplate>(entity =>
        {
            entity.ToTable("ChannelTemplates", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChannelName).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.ChannelTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.ChannelTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.DefaultStatusLookupValue).WithMany()
                  .HasForeignKey(e => e.DefaultStatusLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CommunicationTemplate>(entity =>
        {
            entity.ToTable("CommunicationTemplates", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Body).IsRequired();
            entity.HasOne(e => e.TemplateTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.TemplateTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => e.TemplateCode).IsUnique();
        });

        // Candidate

        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.ToTable("Candidates", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CandidateCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CurrentSalary).HasPrecision(18, 2);
            entity.Property(e => e.ExpectedSalary).HasPrecision(18, 2);
            entity.Property(e => e.TotalExperienceYears).HasPrecision(5, 2);
            entity.HasOne(e => e.BlacklistReasonLookupValue).WithMany()
                  .HasForeignKey(e => e.BlacklistReasonLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CurrentDesignation).WithMany()
                  .HasForeignKey(e => e.CurrentDesignationId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.BlacklistedByEmployee).WithMany()
                  .HasForeignKey(e => e.BlacklistedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.DuplicateOfCandidate).WithMany()
                  .HasForeignKey(e => e.DuplicateOfCandidateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.MergedIntoCandidate).WithMany()
                  .HasForeignKey(e => e.MergedIntoCandidateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Profile).WithOne(p => p.Candidate)
                  .HasForeignKey<CandidateProfile>(p => p.CandidateId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateProfile>(entity =>
        {
            entity.ToTable("CandidateProfiles", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Candidate).WithOne(c => c.Profile)
                  .HasForeignKey<CandidateProfile>(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TalentPool).WithMany()
                  .HasForeignKey(e => e.TalentPoolId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.TalentPoolAddedByEmployee).WithMany()
                  .HasForeignKey(e => e.TalentPoolAddedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.BlacklistRemovedByEmployee).WithMany()
                  .HasForeignKey(e => e.BlacklistRemovedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.MergedIntoCandidate).WithMany()
                  .HasForeignKey(e => e.MergedIntoCandidateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CandidateAddress>(entity =>
        {
            entity.ToTable("CandidateAddresses", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Candidate).WithMany(c => c.Addresses)
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AddressTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.AddressTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CandidateContact>(entity =>
        {
            entity.ToTable("CandidateContacts", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Candidate).WithMany(c => c.Contacts)
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ContactTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.ContactTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CandidateMediaLink>(entity =>
        {
            entity.ToTable("CandidateMediaLinks", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Candidate).WithMany(c => c.MediaLinks)
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.MediaTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.MediaTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CandidateStageHistory>(entity =>
        {
            entity.ToTable("CandidateStageHistories", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Candidate).WithMany(c => c.StageHistories)
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Application).WithMany(a => a.StageHistories)
                  .HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ChangedByEmployee).WithMany()
                  .HasForeignKey(e => e.ChangedByEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.FromStageLookupValue).WithMany()
                  .HasForeignKey(e => e.FromStageLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ToStageLookupValue).WithMany()
                  .HasForeignKey(e => e.ToStageLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CallLog>(entity =>
        {
            entity.ToTable("CallLogs", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Candidate).WithMany(c => c.CallLogs)
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Application).WithMany(a => a.CallLogs)
                  .HasForeignKey(e => e.ApplicationId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CalledByEmployee).WithMany()
                  .HasForeignKey(e => e.CalledByEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CommunicationTemplate).WithMany()
                  .HasForeignKey(e => e.CommunicationTemplateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        // Job

        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("Jobs", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.JobTitle).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RecordType).IsRequired();
            entity.Property(e => e.EmploymentType).IsRequired();
            entity.Property(e => e.SalaryRangeMin).HasPrecision(18, 2);
            entity.Property(e => e.SalaryRangeMax).HasPrecision(18, 2);
            entity.HasOne(e => e.ParentJob).WithMany(j => j.ChildJobs)
                  .HasForeignKey(e => e.ParentJobId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ApprovalRequest).WithMany()
                  .HasForeignKey(e => e.ApprovalRequestId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.PriorityLookupValue).WithMany()
                  .HasForeignKey(e => e.PriorityLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Department).WithMany()
                  .HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Designation).WithMany()
                  .HasForeignKey(e => e.DesignationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Currency).WithMany(c => c.Jobs)
                  .HasForeignKey(e => e.CurrencyId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.HiringManagerEmployee).WithMany()
                  .HasForeignKey(e => e.HiringManagerEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.RecruiterEmployee).WithMany()
                  .HasForeignKey(e => e.RecruiterEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ClosedByEmployee).WithMany()
                  .HasForeignKey(e => e.ClosedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Detail).WithOne(d => d.Job)
                  .HasForeignKey<JobDetail>(d => d.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobDetail>(entity =>
        {
            entity.ToTable("JobDetails", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Job).WithOne(j => j.Detail)
                  .HasForeignKey<JobDetail>(e => e.JobId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobTemplate>(entity =>
        {
            entity.ToTable("JobTemplates", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<JobPostingChannel>(entity =>
        {
            entity.ToTable("JobPostingChannels", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Job).WithMany(j => j.PostingChannels)
                  .HasForeignKey(e => e.JobId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ChannelTemplate).WithMany(ct => ct.JobPostingChannels)
                  .HasForeignKey(e => e.ChannelTemplateId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ChannelTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.ChannelTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        // Application

        modelBuilder.Entity<Hr.Domain.Entities.Application>(entity =>
        {
            entity.ToTable("Applications", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApplicationCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ScreeningScore).HasPrecision(5, 2);
            entity.Property(e => e.InternalScore).HasPrecision(5, 2);
            entity.HasOne(e => e.Job).WithMany(j => j.Applications)
                  .HasForeignKey(e => e.JobId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Candidate).WithMany(c => c.Applications)
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.JobPostingChannel).WithMany()
                  .HasForeignKey(e => e.JobPostingChannelId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.AssignedRecruiterEmployee).WithMany()
                  .HasForeignKey(e => e.AssignedRecruiterEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ConvertedToEmployee).WithMany()
                  .HasForeignKey(e => e.ConvertedToEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CurrentStageLookupValue).WithMany()
                  .HasForeignKey(e => e.CurrentStageLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.PriorityLookupValue).WithMany()
                  .HasForeignKey(e => e.PriorityLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Offer).WithOne(o => o.Application)
                  .HasForeignKey<OfferLetter>(o => o.ApplicationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Detail).WithOne(d => d.Application)
                  .HasForeignKey<ApplicationDetail>(d => d.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Compliance).WithOne(c => c.Application)
                  .HasForeignKey<ApplicationCompliance>(c => c.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationDetail>(entity =>
        {
            entity.ToTable("ApplicationDetails", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Application).WithOne(a => a.Detail)
                  .HasForeignKey<ApplicationDetail>(e => e.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationCompliance>(entity =>
        {
            entity.ToTable("ApplicationCompliances", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Application).WithOne(a => a.Compliance)
                  .HasForeignKey<ApplicationCompliance>(e => e.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });

        // Candidate Tasks

        modelBuilder.Entity<CandidateTask>(entity =>
        {
            entity.ToTable("CandidateTasks", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Application).WithMany(a => a.Tasks)
                  .HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Candidate).WithMany()
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Job).WithMany()
                  .HasForeignKey(e => e.JobId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.AssignedByEmployee).WithMany()
                  .HasForeignKey(e => e.AssignedByEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.TaskTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.TaskTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CandidateTaskSubmission>(entity =>
        {
            entity.ToTable("CandidateTaskSubmissions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CandidateTask).WithMany(t => t.Submissions)
                  .HasForeignKey(e => e.CandidateTaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Candidate).WithMany()
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Application).WithMany()
                  .HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CandidateTaskEvaluation>(entity =>
        {
            entity.ToTable("CandidateTaskEvaluations", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CandidateTask).WithMany(t => t.Evaluations)
                  .HasForeignKey(e => e.CandidateTaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Submission).WithMany(s => s.Evaluations)
                  .HasForeignKey(e => e.SubmissionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.EvaluatedByEmployee).WithMany()
                  .HasForeignKey(e => e.EvaluatedByEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ResultLookupValue).WithMany()
                  .HasForeignKey(e => e.ResultLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        // Interview

        modelBuilder.Entity<Interview>(entity =>
        {
            entity.ToTable("Interviews", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InterviewCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InterviewTitle).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Format).IsRequired();
            entity.Property(e => e.WeightedPanelScore).HasPrecision(5, 2);
            entity.HasOne(e => e.Application).WithMany(a => a.Interviews)
                  .HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Candidate).WithMany()
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Job).WithMany()
                  .HasForeignKey(e => e.JobId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.InterviewFeedbackTemplate).WithMany()
                  .HasForeignKey(e => e.InterviewFeedbackTemplateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ProposedByEmployee).WithMany()
                  .HasForeignKey(e => e.ProposedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.InterviewTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.InterviewTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.DecisionLookupValue).WithMany()
                  .HasForeignKey(e => e.DecisionLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<InterviewPanelMember>(entity =>
        {
            entity.ToTable("InterviewPanelMembers", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Interview).WithMany(i => i.PanelMembers)
                  .HasForeignKey(e => e.InterviewId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InterviewerEmployee).WithMany()
                  .HasForeignKey(e => e.InterviewerEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.AlternateInterviewerEmployee).WithMany()
                  .HasForeignKey(e => e.AlternateInterviewerEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.InviteStatusLookupValue).WithMany()
                  .HasForeignKey(e => e.InviteStatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<InterviewFeedback>(entity =>
        {
            entity.ToTable("InterviewFeedbacks", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OverallScore).HasPrecision(5, 2);
            entity.HasOne(e => e.Interview).WithMany(i => i.Feedbacks)
                  .HasForeignKey(e => e.InterviewId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PanelMember).WithMany()
                  .HasForeignKey(e => e.PanelMemberId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Candidate).WithMany()
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Application).WithMany()
                  .HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.SubmittedByEmployee).WithMany()
                  .HasForeignKey(e => e.SubmittedByEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.RecommendationLookupValue).WithMany()
                  .HasForeignKey(e => e.RecommendationLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.DecisionLookupValue).WithMany()
                  .HasForeignKey(e => e.DecisionLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<InterviewFeedbackTemplate>(entity =>
        {
            entity.ToTable("InterviewFeedbackTemplates", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<InterviewNotification>(entity =>
        {
            entity.ToTable("InterviewNotifications", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Interview).WithMany(i => i.Notifications)
                  .HasForeignKey(e => e.InterviewId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PanelMember).WithMany()
                  .HasForeignKey(e => e.InterviewPanelMemberId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.RecipientEmployee).WithMany()
                  .HasForeignKey(e => e.RecipientEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.NotificationTypeLookupValue).WithMany()
                  .HasForeignKey(e => e.NotificationTypeLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.DeliveryStatusLookupValue).WithMany()
                  .HasForeignKey(e => e.DeliveryStatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ResponseActionLookupValue).WithMany()
                  .HasForeignKey(e => e.ResponseActionLookupValueId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CommunicationTemplate).WithMany()
                  .HasForeignKey(e => e.CommunicationTemplateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<InterviewerAvailability>(entity =>
        {
            entity.ToTable("InterviewerAvailabilities", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Interview).WithMany(i => i.InterviewerAvailabilities)
                  .HasForeignKey(e => e.InterviewId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.InterviewerEmployee).WithMany()
                  .HasForeignKey(e => e.InterviewerEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.SlotStatusLookupValue).WithMany()
                  .HasForeignKey(e => e.SlotStatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        // Offer

        modelBuilder.Entity<OfferLetter>(entity =>
        {
            entity.ToTable("OfferLetters", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OfferCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.JobTitle).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EmploymentType).IsRequired();
            entity.Property(e => e.BaseSalary).HasPrecision(18, 2);
            entity.Property(e => e.TotalPackage).HasPrecision(18, 2);
            entity.HasOne(e => e.Application).WithOne(a => a.Offer)
                  .HasForeignKey<OfferLetter>(e => e.ApplicationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Candidate).WithMany()
                  .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Job).WithMany()
                  .HasForeignKey(e => e.JobId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ApprovalRequest).WithMany()
                  .HasForeignKey(e => e.ApprovalRequestId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Currency).WithMany(c => c.OfferLetters)
                  .HasForeignKey(e => e.CurrencyId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ReportingManagerEmployee).WithMany()
                  .HasForeignKey(e => e.ReportingManagerEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.SentByEmployee).WithMany()
                  .HasForeignKey(e => e.SentByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.RevokedByEmployee).WithMany()
                  .HasForeignKey(e => e.RevokedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CandidateResponseLookupValue).WithMany()
                  .HasForeignKey(e => e.CandidateResponseLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CommunicationTemplate).WithMany(c => c.OfferLetters)
                  .HasForeignKey(e => e.CommunicationTemplateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Detail).WithOne(d => d.OfferLetter)
                  .HasForeignKey<OfferLetterDetail>(d => d.OfferLetterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OfferLetterDetail>(entity =>
        {
            entity.ToTable("OfferLetterDetails", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.OfferLetter).WithOne(o => o.Detail)
                  .HasForeignKey<OfferLetterDetail>(e => e.OfferLetterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OfferNegotiation>(entity =>
        {
            entity.ToTable("OfferNegotiations", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProposedSalary).HasPrecision(18, 2);
            entity.HasOne(e => e.Offer).WithMany(o => o.Negotiations)
                  .HasForeignKey(e => e.OfferId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ResponseByEmployee).WithMany()
                  .HasForeignKey(e => e.ResponseByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ReApprovalRequest).WithMany()
                  .HasForeignKey(e => e.ReApprovalRequestId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        // Onboarding

        modelBuilder.Entity<OnboardingTaskTemplate>(entity =>
        {
            entity.ToTable("OnboardingTaskTemplates", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaskName).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<OnboardingTask>(entity =>
        {
            entity.ToTable("OnboardingTasks", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Application).WithMany(a => a.OnboardingTasks)
                  .HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Candidate).WithMany()
                  .HasForeignKey(e => e.CandidateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Employee).WithMany()
                  .HasForeignKey(e => e.EmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.AssignedToEmployee).WithMany()
                  .HasForeignKey(e => e.AssignedToEmployeeId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CompletedByEmployee).WithMany()
                  .HasForeignKey(e => e.CompletedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.VerifiedByEmployee).WithMany()
                  .HasForeignKey(e => e.VerifiedByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.EscalatedToEmployee).WithMany()
                  .HasForeignKey(e => e.EscalatedToEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.CancelledByEmployee).WithMany()
                  .HasForeignKey(e => e.CancelledByEmployeeId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.OnboardingTaskTemplate).WithMany()
                  .HasForeignKey(e => e.OnboardingTaskTemplateId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.DependsOnTask).WithMany()
                  .HasForeignKey(e => e.DependsOnTaskId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.StatusLookupValue).WithMany()
                  .HasForeignKey(e => e.StatusLookupValueId).OnDelete(DeleteBehavior.NoAction);
        });

        // Recruiting support

        modelBuilder.Entity<AllowancesProfile>(entity =>
        {
            entity.ToTable("AllowancesProfiles", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AllowancesProfileCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AllowancesProfileName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<BenefitsPlan>(entity =>
        {
            entity.ToTable("BenefitsPlans", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BenefitsPlanCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BenefitsPlanName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<ScreeningQuestionnaire>(entity =>
        {
            entity.ToTable("ScreeningQuestionnaires", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuestionnaireCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QuestionnaireName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<TalentPool>(entity =>
        {
            entity.ToTable("TalentPools", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TalentPoolCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TalentPoolName).IsRequired().HasMaxLength(100);
        });
    }

    protected static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        var utc = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcN = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
        foreach (var e in modelBuilder.Model.GetEntityTypes())
            foreach (var p in e.GetProperties())
            {
                if (p.ClrType == typeof(DateTime))  p.SetValueConverter(utc);
                if (p.ClrType == typeof(DateTime?)) p.SetValueConverter(utcN);
            }
    }
}
