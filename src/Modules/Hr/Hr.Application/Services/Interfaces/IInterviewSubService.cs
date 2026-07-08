using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IInterviewPanelMemberService
{
    Task<IEnumerable<InterviewPanelMemberDto>> GetAllAsync();
    Task<InterviewPanelMemberDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<InterviewPanelMemberDto>> GetByInterviewIdAsync(Guid interviewId);
    Task<InterviewPanelMemberDto> CreateAsync(CreateInterviewPanelMemberDto request, Guid userId);
    Task<InterviewPanelMemberDto> UpdateAsync(Guid id, UpdateInterviewPanelMemberDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IInterviewerAvailabilityService
{
    Task<IEnumerable<InterviewerAvailabilityDto>> GetAllAsync();
    Task<InterviewerAvailabilityDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<InterviewerAvailabilityDto>> GetByInterviewerIdAsync(Guid interviewerEmployeeId);
    Task<InterviewerAvailabilityDto> CreateAsync(CreateInterviewerAvailabilityDto request, Guid userId);
    Task<InterviewerAvailabilityDto> UpdateAsync(Guid id, UpdateInterviewerAvailabilityDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IInterviewFeedbackService
{
    Task<IEnumerable<InterviewFeedbackDto>> GetAllAsync();
    Task<InterviewFeedbackDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<InterviewFeedbackDto>> GetByInterviewIdAsync(Guid interviewId);
    Task<InterviewFeedbackDto> CreateAsync(CreateInterviewFeedbackDto request, Guid userId);
    Task<InterviewFeedbackDto> UpdateAsync(Guid id, UpdateInterviewFeedbackDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
