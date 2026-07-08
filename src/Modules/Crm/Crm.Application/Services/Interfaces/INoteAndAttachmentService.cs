using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface INoteService
{
    Task<NoteDto> CreateAsync(CreateNoteDto dto, Guid userId);
    Task<NoteDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<NoteDto>> GetByParentAsync(Guid parentId, string parentType);
    Task<NoteDto> UpdateAsync(Guid id, UpdateNoteDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IAttachmentService
{
    Task<AttachmentDto> CreateAsync(CreateAttachmentDto dto, Guid userId);
    Task<AttachmentDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<AttachmentDto>> GetByParentAsync(Guid parentId, string parentType);
    Task DeleteAsync(Guid id, Guid userId);
}
