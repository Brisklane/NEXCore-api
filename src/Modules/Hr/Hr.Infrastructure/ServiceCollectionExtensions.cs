using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Hr.Infrastructure.Repositories.Implementations;
using Hr.Infrastructure.Services;
using Hr.Infrastructure.Events;
using Hr.Application.Services.Interfaces;
using Nexcore.SharedKernel.Repository;
using Nexcore.SharedKernel.Events;

namespace Hr.Infrastructure;

/// <summary>
/// Service collection extensions for the HR module.
/// Registers DbContext, repositories, and application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHrInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<HrDbContext>(options =>
            options.UseNexcorePostgres(configuration.GetConnectionString("DefaultConnection"), typeof(HrDbContext).Assembly));

        // Generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Repositories
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IJobPostingChannelRepository, JobPostingChannelRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IInterviewRepository, InterviewRepository>();
        services.AddScoped<IOfferLetterRepository, OfferLetterRepository>();
        services.AddScoped<ILookupTypeRepository, LookupTypeRepository>();
        services.AddScoped<ILookupValueRepository, LookupValueRepository>();
        services.AddScoped<IChannelTemplateRepository, ChannelTemplateRepository>();

        // Organisation / reference-data repositories
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDesignationRepository, DesignationRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IGradeRepository, GradeRepository>();
        services.AddScoped<IJobFamilyRepository, JobFamilyRepository>();
        services.AddScoped<IJobFunctionRepository, JobFunctionRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICostCenterRepository, CostCenterRepository>();
        services.AddScoped<IJobLocationRepository, JobLocationRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IPayScaleRepository, PayScaleRepository>();
        services.AddScoped<IAllowancesProfileRepository, AllowancesProfileRepository>();
        services.AddScoped<IBenefitsPlanRepository, BenefitsPlanRepository>();
        services.AddScoped<IScreeningQuestionnaireRepository, ScreeningQuestionnaireRepository>();
        services.AddScoped<ITalentPoolRepository, TalentPoolRepository>();
        services.AddScoped<ICommunicationTemplateRepository, CommunicationTemplateRepository>();
        services.AddScoped<IJobTemplateRepository, JobTemplateRepository>();

        // Application Services
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IJobPostingChannelService, JobPostingChannelService>();
        services.AddScoped<ICandidateService, CandidateService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<IOfferLetterService, OfferLetterService>();
        services.AddScoped<ILookupService, LookupService>();

        // Organisation / reference-data services
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IDesignationService, DesignationService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<IJobFamilyService, JobFamilyService>();
        services.AddScoped<IJobFunctionService, JobFunctionService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ICostCenterService, CostCenterService>();
        services.AddScoped<IJobLocationService, JobLocationService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IPayScaleService, PayScaleService>();
        services.AddScoped<IAllowancesProfileService, AllowancesProfileService>();
        services.AddScoped<IBenefitsPlanService, BenefitsPlanService>();
        services.AddScoped<IScreeningQuestionnaireService, ScreeningQuestionnaireService>();
        services.AddScoped<ITalentPoolService, TalentPoolService>();
        services.AddScoped<ICommunicationTemplateService, CommunicationTemplateService>();
        services.AddScoped<IChannelTemplateService, ChannelTemplateService>();
        services.AddScoped<IJobTemplateService, JobTemplateService>();

        // Master / configuration repositories (new)
        services.AddScoped<IOnboardingTaskTemplateRepository, OnboardingTaskTemplateRepository>();
        services.AddScoped<ISkillCategoryRepository, SkillCategoryRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ICompetencyFrameworkRepository, CompetencyFrameworkRepository>();
        services.AddScoped<ICompetencyFrameworkItemRepository, CompetencyFrameworkItemRepository>();
        services.AddScoped<IWorkflowConfigRepository, WorkflowConfigRepository>();
        services.AddScoped<IWorkflowConfigStepRepository, WorkflowConfigStepRepository>();
        services.AddScoped<IWorkflowConditionRepository, WorkflowConditionRepository>();
        services.AddScoped<IWorkflowEscalationRepository, WorkflowEscalationRepository>();

        // Recruitment aggregate repositories (new)
        services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();
        services.AddScoped<ICandidateContactRepository, CandidateContactRepository>();
        services.AddScoped<ICandidateAddressRepository, CandidateAddressRepository>();
        services.AddScoped<ICandidateMediaLinkRepository, CandidateMediaLinkRepository>();
        services.AddScoped<ICandidateSkillRepository, CandidateSkillRepository>();
        services.AddScoped<IApplicationDetailRepository, ApplicationDetailRepository>();
        services.AddScoped<IApplicationComplianceRepository, ApplicationComplianceRepository>();
        services.AddScoped<IInterviewPanelMemberRepository, InterviewPanelMemberRepository>();
        services.AddScoped<IInterviewerAvailabilityRepository, InterviewerAvailabilityRepository>();
        services.AddScoped<IInterviewFeedbackRepository, InterviewFeedbackRepository>();
        services.AddScoped<IOfferLetterDetailRepository, OfferLetterDetailRepository>();
        services.AddScoped<IOfferNegotiationRepository, OfferNegotiationRepository>();
        services.AddScoped<IOnboardingTaskRepository, OnboardingTaskRepository>();
        services.AddScoped<ICallLogRepository, CallLogRepository>();
        services.AddScoped<ICandidateTaskRepository, CandidateTaskRepository>();

        // Master / configuration services (new)
        services.AddScoped<IOnboardingTaskTemplateService, OnboardingTaskTemplateService>();
        services.AddScoped<ISkillCategoryService, SkillCategoryService>();
        services.AddScoped<ISkillService, SkillService>();
        services.AddScoped<ICompetencyFrameworkService, CompetencyFrameworkService>();
        services.AddScoped<ICompetencyFrameworkItemService, CompetencyFrameworkItemService>();
        services.AddScoped<IWorkflowConfigService, WorkflowConfigService>();
        services.AddScoped<IWorkflowConfigStepService, WorkflowConfigStepService>();
        services.AddScoped<IWorkflowConditionService, WorkflowConditionService>();
        services.AddScoped<IWorkflowEscalationService, WorkflowEscalationService>();

        // Recruitment aggregate services (new)
        services.AddScoped<ICandidateProfileService, CandidateProfileService>();
        services.AddScoped<ICandidateContactService, CandidateContactService>();
        services.AddScoped<ICandidateAddressService, CandidateAddressService>();
        services.AddScoped<ICandidateMediaLinkService, CandidateMediaLinkService>();
        services.AddScoped<ICandidateSkillService, CandidateSkillService>();
        services.AddScoped<IApplicationDetailService, ApplicationDetailService>();
        services.AddScoped<IApplicationComplianceService, ApplicationComplianceService>();
        services.AddScoped<IInterviewPanelMemberService, InterviewPanelMemberService>();
        services.AddScoped<IInterviewerAvailabilityService, InterviewerAvailabilityService>();
        services.AddScoped<IInterviewFeedbackService, InterviewFeedbackService>();
        services.AddScoped<IOfferLetterDetailService, OfferLetterDetailService>();
        services.AddScoped<IOfferNegotiationService, OfferNegotiationService>();
        services.AddScoped<IOnboardingTaskService, OnboardingTaskService>();
        services.AddScoped<ICallLogService, CallLogService>();
        services.AddScoped<ICandidateTaskService, CandidateTaskService>();

        // Approval repositories & service
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
        services.AddScoped<IApprovalRequestStepRepository, ApprovalRequestStepRepository>();
        services.AddScoped<IApprovalService, ApprovalService>();

        // HR seed service
        services.AddScoped<HrLookupSeedService>();

        // HR initialization service (triggered on CompanyCreatedEvent)
        services.AddScoped<IHrInitializationService, HrInitializationService>();
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, CompanyCreatedEventHandler>();

        return services;
    }
}
