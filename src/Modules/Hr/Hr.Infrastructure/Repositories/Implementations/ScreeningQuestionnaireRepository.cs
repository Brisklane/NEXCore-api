using Microsoft.AspNetCore.Http;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Implementations;

public class ScreeningQuestionnaireRepository : TenantAwareRepository<ScreeningQuestionnaire>, IScreeningQuestionnaireRepository
{
    public ScreeningQuestionnaireRepository(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<IEnumerable<ScreeningQuestionnaire>> GetAllByTenantAsync() => await GetAllAsync();
}
