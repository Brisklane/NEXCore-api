using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class InterviewPanelMemberController : ControllerBase
{
    private readonly IInterviewPanelMemberService _service;
    private readonly ILogger<InterviewPanelMemberController> _logger;

    public InterviewPanelMemberController(IInterviewPanelMemberService service, ILogger<InterviewPanelMemberController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<InterviewPanelMemberDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview panel members"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Interview panel member not found" });
            return Ok(new ApiResponse<InterviewPanelMemberDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview panel member {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-interview/{interviewId}")]
    public async Task<IActionResult> GetByInterviewId(Guid interviewId)
    {
        try { return Ok(new ApiResponse<IEnumerable<InterviewPanelMemberDto>> { Success = true, Data = await _service.GetByInterviewIdAsync(interviewId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving panel members for interview {Id}", interviewId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInterviewPanelMemberDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<InterviewPanelMemberDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating interview panel member"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInterviewPanelMemberDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<InterviewPanelMemberDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating interview panel member {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting interview panel member {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class InterviewerAvailabilityController : ControllerBase
{
    private readonly IInterviewerAvailabilityService _service;
    private readonly ILogger<InterviewerAvailabilityController> _logger;

    public InterviewerAvailabilityController(IInterviewerAvailabilityService service, ILogger<InterviewerAvailabilityController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<InterviewerAvailabilityDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interviewer availabilities"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Interviewer availability not found" });
            return Ok(new ApiResponse<InterviewerAvailabilityDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interviewer availability {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-interviewer/{interviewerEmployeeId}")]
    public async Task<IActionResult> GetByInterviewerId(Guid interviewerEmployeeId)
    {
        try { return Ok(new ApiResponse<IEnumerable<InterviewerAvailabilityDto>> { Success = true, Data = await _service.GetByInterviewerIdAsync(interviewerEmployeeId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving availability for interviewer {Id}", interviewerEmployeeId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInterviewerAvailabilityDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<InterviewerAvailabilityDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating interviewer availability"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInterviewerAvailabilityDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<InterviewerAvailabilityDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating interviewer availability {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting interviewer availability {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class InterviewFeedbackController : ControllerBase
{
    private readonly IInterviewFeedbackService _service;
    private readonly ILogger<InterviewFeedbackController> _logger;

    public InterviewFeedbackController(IInterviewFeedbackService service, ILogger<InterviewFeedbackController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<InterviewFeedbackDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview feedbacks"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Interview feedback not found" });
            return Ok(new ApiResponse<InterviewFeedbackDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview feedback {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-interview/{interviewId}")]
    public async Task<IActionResult> GetByInterviewId(Guid interviewId)
    {
        try { return Ok(new ApiResponse<IEnumerable<InterviewFeedbackDto>> { Success = true, Data = await _service.GetByInterviewIdAsync(interviewId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving feedback for interview {Id}", interviewId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInterviewFeedbackDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<InterviewFeedbackDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating interview feedback"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInterviewFeedbackDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<InterviewFeedbackDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating interview feedback {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting interview feedback {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
