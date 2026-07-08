using Hr.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Hr.Infrastructure.Repositories.Interfaces;

public interface IScreeningQuestionnaireRepository : IRepository<ScreeningQuestionnaire>
{
    Task<IEnumerable<ScreeningQuestionnaire>> GetAllByTenantAsync();
}
