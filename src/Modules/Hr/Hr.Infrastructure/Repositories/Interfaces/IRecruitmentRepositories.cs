using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface ICandidateProfileRepository : IRepository<CandidateProfile>
{
    Task<IEnumerable<CandidateProfile>> GetAllByTenantAsync();
    Task<CandidateProfile?> GetByCandidateIdAsync(Guid candidateId);
}

public interface ICandidateContactRepository : IRepository<CandidateContact>
{
    Task<IEnumerable<CandidateContact>> GetAllByTenantAsync();
    Task<IEnumerable<CandidateContact>> GetByCandidateIdAsync(Guid candidateId);
}

public interface ICandidateAddressRepository : IRepository<CandidateAddress>
{
    Task<IEnumerable<CandidateAddress>> GetAllByTenantAsync();
    Task<IEnumerable<CandidateAddress>> GetByCandidateIdAsync(Guid candidateId);
}

public interface ICandidateMediaLinkRepository : IRepository<CandidateMediaLink>
{
    Task<IEnumerable<CandidateMediaLink>> GetAllByTenantAsync();
    Task<IEnumerable<CandidateMediaLink>> GetByCandidateIdAsync(Guid candidateId);
}

public interface ICandidateSkillRepository : IRepository<CandidateSkill>
{
    Task<IEnumerable<CandidateSkill>> GetAllByTenantAsync();
    Task<IEnumerable<CandidateSkill>> GetByCandidateIdAsync(Guid candidateId);
}

public interface IApplicationDetailRepository : IRepository<ApplicationDetail>
{
    Task<IEnumerable<ApplicationDetail>> GetAllByTenantAsync();
    Task<ApplicationDetail?> GetByApplicationIdAsync(Guid applicationId);
}

public interface IApplicationComplianceRepository : IRepository<ApplicationCompliance>
{
    Task<IEnumerable<ApplicationCompliance>> GetAllByTenantAsync();
    Task<ApplicationCompliance?> GetByApplicationIdAsync(Guid applicationId);
}

public interface IInterviewPanelMemberRepository : IRepository<InterviewPanelMember>
{
    Task<IEnumerable<InterviewPanelMember>> GetAllByTenantAsync();
    Task<IEnumerable<InterviewPanelMember>> GetByInterviewIdAsync(Guid interviewId);
}

public interface IInterviewerAvailabilityRepository : IRepository<InterviewerAvailability>
{
    Task<IEnumerable<InterviewerAvailability>> GetAllByTenantAsync();
    Task<IEnumerable<InterviewerAvailability>> GetByInterviewerIdAsync(Guid interviewerEmployeeId);
}

public interface IInterviewFeedbackRepository : IRepository<InterviewFeedback>
{
    Task<IEnumerable<InterviewFeedback>> GetAllByTenantAsync();
    Task<IEnumerable<InterviewFeedback>> GetByInterviewIdAsync(Guid interviewId);
}

public interface IOfferLetterDetailRepository : IRepository<OfferLetterDetail>
{
    Task<IEnumerable<OfferLetterDetail>> GetAllByTenantAsync();
    Task<OfferLetterDetail?> GetByOfferLetterIdAsync(Guid offerLetterId);
}

public interface IOfferNegotiationRepository : IRepository<OfferNegotiation>
{
    Task<IEnumerable<OfferNegotiation>> GetAllByTenantAsync();
    Task<IEnumerable<OfferNegotiation>> GetByOfferIdAsync(Guid offerId);
}

public interface IOnboardingTaskRepository : IRepository<OnboardingTask>
{
    Task<IEnumerable<OnboardingTask>> GetAllByTenantAsync();
    Task<IEnumerable<OnboardingTask>> GetByApplicationIdAsync(Guid applicationId);
}

public interface ICallLogRepository : IRepository<CallLog>
{
    Task<IEnumerable<CallLog>> GetAllByTenantAsync();
    Task<IEnumerable<CallLog>> GetByCandidateIdAsync(Guid candidateId);
}

public interface ICandidateTaskRepository : IRepository<CandidateTask>
{
    Task<IEnumerable<CandidateTask>> GetAllByTenantAsync();
    Task<IEnumerable<CandidateTask>> GetByApplicationIdAsync(Guid applicationId);
    Task<IEnumerable<CandidateTask>> GetByCandidateIdAsync(Guid candidateId);
}
