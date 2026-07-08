using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class CandidateProfileRepository : TenantAwareRepository<CandidateProfile>, ICandidateProfileRepository
{
    public CandidateProfileRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CandidateProfile>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<CandidateProfile?> GetByCandidateIdAsync(Guid candidateId)
        => (await FindAsync(p => p.CandidateId == candidateId && !p.IsDeleted)).FirstOrDefault();
}

public class CandidateContactRepository : TenantAwareRepository<CandidateContact>, ICandidateContactRepository
{
    public CandidateContactRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CandidateContact>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<CandidateContact>> GetByCandidateIdAsync(Guid candidateId)
        => await FindAsync(c => c.CandidateId == candidateId && !c.IsDeleted);
}

public class CandidateAddressRepository : TenantAwareRepository<CandidateAddress>, ICandidateAddressRepository
{
    public CandidateAddressRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CandidateAddress>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<CandidateAddress>> GetByCandidateIdAsync(Guid candidateId)
        => await FindAsync(a => a.CandidateId == candidateId && !a.IsDeleted);
}

public class CandidateMediaLinkRepository : TenantAwareRepository<CandidateMediaLink>, ICandidateMediaLinkRepository
{
    public CandidateMediaLinkRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CandidateMediaLink>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<CandidateMediaLink>> GetByCandidateIdAsync(Guid candidateId)
        => await FindAsync(m => m.CandidateId == candidateId && !m.IsDeleted);
}

public class CandidateSkillRepository : TenantAwareRepository<CandidateSkill>, ICandidateSkillRepository
{
    public CandidateSkillRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CandidateSkill>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<CandidateSkill>> GetByCandidateIdAsync(Guid candidateId)
        => await FindAsync(s => s.CandidateId == candidateId && !s.IsDeleted);
}

public class ApplicationDetailRepository : TenantAwareRepository<ApplicationDetail>, IApplicationDetailRepository
{
    public ApplicationDetailRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<ApplicationDetail>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<ApplicationDetail?> GetByApplicationIdAsync(Guid applicationId)
        => (await FindAsync(d => d.ApplicationId == applicationId && !d.IsDeleted)).FirstOrDefault();
}

public class ApplicationComplianceRepository : TenantAwareRepository<ApplicationCompliance>, IApplicationComplianceRepository
{
    public ApplicationComplianceRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<ApplicationCompliance>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<ApplicationCompliance?> GetByApplicationIdAsync(Guid applicationId)
        => (await FindAsync(c => c.ApplicationId == applicationId && !c.IsDeleted)).FirstOrDefault();
}

public class InterviewPanelMemberRepository : TenantAwareRepository<InterviewPanelMember>, IInterviewPanelMemberRepository
{
    public InterviewPanelMemberRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<InterviewPanelMember>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<InterviewPanelMember>> GetByInterviewIdAsync(Guid interviewId)
        => await FindAsync(p => p.InterviewId == interviewId && !p.IsDeleted);
}

public class InterviewerAvailabilityRepository : TenantAwareRepository<InterviewerAvailability>, IInterviewerAvailabilityRepository
{
    public InterviewerAvailabilityRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<InterviewerAvailability>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<InterviewerAvailability>> GetByInterviewerIdAsync(Guid interviewerEmployeeId)
        => await FindAsync(a => a.InterviewerEmployeeId == interviewerEmployeeId && !a.IsDeleted);
}

public class InterviewFeedbackRepository : TenantAwareRepository<InterviewFeedback>, IInterviewFeedbackRepository
{
    public InterviewFeedbackRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<InterviewFeedback>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<InterviewFeedback>> GetByInterviewIdAsync(Guid interviewId)
        => await FindAsync(f => f.InterviewId == interviewId && !f.IsDeleted);
}

public class OfferLetterDetailRepository : TenantAwareRepository<OfferLetterDetail>, IOfferLetterDetailRepository
{
    public OfferLetterDetailRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<OfferLetterDetail>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<OfferLetterDetail?> GetByOfferLetterIdAsync(Guid offerLetterId)
        => (await FindAsync(d => d.OfferLetterId == offerLetterId && !d.IsDeleted)).FirstOrDefault();
}

public class OfferNegotiationRepository : TenantAwareRepository<OfferNegotiation>, IOfferNegotiationRepository
{
    public OfferNegotiationRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<OfferNegotiation>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<OfferNegotiation>> GetByOfferIdAsync(Guid offerId)
        => await FindAsync(n => n.OfferId == offerId && !n.IsDeleted);
}

public class OnboardingTaskRepository : TenantAwareRepository<OnboardingTask>, IOnboardingTaskRepository
{
    public OnboardingTaskRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<OnboardingTask>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<OnboardingTask>> GetByApplicationIdAsync(Guid applicationId)
        => await FindAsync(t => t.ApplicationId == applicationId && !t.IsDeleted);
}

public class CallLogRepository : TenantAwareRepository<CallLog>, ICallLogRepository
{
    public CallLogRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CallLog>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<CallLog>> GetByCandidateIdAsync(Guid candidateId)
        => await FindAsync(l => l.CandidateId == candidateId && !l.IsDeleted);
}

public class CandidateTaskRepository : TenantAwareRepository<CandidateTask>, ICandidateTaskRepository
{
    public CandidateTaskRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
    public async Task<IEnumerable<CandidateTask>> GetAllByTenantAsync() => await GetAllAsync();
    public async Task<IEnumerable<CandidateTask>> GetByApplicationIdAsync(Guid applicationId)
        => await FindAsync(t => t.ApplicationId == applicationId && !t.IsDeleted);
    public async Task<IEnumerable<CandidateTask>> GetByCandidateIdAsync(Guid candidateId)
        => await FindAsync(t => t.CandidateId == candidateId && !t.IsDeleted);
}
