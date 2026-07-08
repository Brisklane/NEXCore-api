using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IGradeService
{
    Task<IEnumerable<GradeDto>> GetAllAsync();
    Task<GradeDto?> GetByIdAsync(Guid id);
    Task<GradeDto> CreateAsync(CreateGradeDto request, Guid userId);
    Task<GradeDto> UpdateAsync(Guid id, UpdateGradeDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
