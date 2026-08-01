using Hr.Application.DTOs;
using Hr.Application.Enums;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Enums;

namespace Hr.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly IJobRepository _repository;
    private readonly IApprovalService _approvalService;
    private readonly ILogger<JobService> _logger;

    public JobService(
        IJobRepository repository,
        IApprovalService approvalService,
        ILogger<JobService> logger)
    {
        _repository      = repository;
        _approvalService = approvalService;
        _logger          = logger;
    }

    public async Task<IEnumerable<JobDto>> GetAllAsync(JobRecordType? recordType = null)
    {
        try
        {
            if (recordType.HasValue)
            {
                var filtered = await _repository.GetByRecordTypeAsync(recordType.Value);
                return filtered.Select(MapToDto);
            }

            var jobs = await _repository.GetAllByTenantAsync();
            return jobs.Where(j => !j.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs");
            throw;
        }
    }

    public async Task<JobDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var job = await _repository.GetByIdAsync(id);
            return job == null ? null : MapToDto(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job: {JobId}", id);
            throw;
        }
    }

    public async Task<JobDto> CreateAsync(CreateJobDto request, Guid userId)
    {
        try
        {
            var job = new Job
            {
                JobCode = request.JobCode,
                JobTitle = request.JobTitle,
                RecordType = request.RecordType,
                ParentJobId = request.ParentJobId,
                DepartmentId = request.DepartmentId,
                DesignationId = request.DesignationId,
                Headcount = request.Headcount,
                EmploymentType = request.EmploymentType,
                PriorityLookupValueId = request.PriorityLookupValueId,
                StatusLookupValueId = request.StatusLookupValueId,
                HiringManagerEmployeeId = request.HiringManagerEmployeeId,
                RecruiterEmployeeId = request.RecruiterEmployeeId,
                CurrencyCode = request.CurrencyCode,
                SalaryRangeMin = request.SalaryRangeMin,
                SalaryRangeMax = request.SalaryRangeMax,
                TargetStartDate = request.TargetStartDate,
                PostingStartDate = request.PostingStartDate,
                PostingCloseDate = request.PostingCloseDate,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(job);
            await _repository.SaveChangesAsync();

            // Auto-submit an approval request for job requisitions
            if (job.RecordType == JobRecordType.Requisition)
            {
                if (!request.RequestedByEmployeeId.HasValue)
                    throw new InvalidOperationException(
                        "RequestedByEmployeeId is required when creating a Job Requisition.");

                var approvalRequest = new SubmitApprovalRequestDto
                {
                    EntityType              = nameof(JobRecordType.Requisition),
                    EntityId                = job.Id,
                    RequestedByEmployeeId   = request.RequestedByEmployeeId.Value,
                    PriorityLookupValueId   = request.PriorityLookupValueId ?? Guid.Empty,
                    ApprovalSubjectCode     = job.JobCode,
                    ApprovalSubjectTitle    = job.JobTitle,
                    ApprovalSummary         = $"Job Requisition: {job.JobTitle} � Headcount: {job.Headcount}",
                    ApprovalDisplayName     = $"{job.JobCode} � {job.JobTitle}",
                    // Entity data snapshot for WorkflowCondition evaluation
                    EntityData = new Dictionary<string, string>
                    {
                        ["SalaryRangeMin"]  = job.SalaryRangeMin.ToString("F2"),
                        ["SalaryRangeMax"]  = job.SalaryRangeMax.ToString("F2"),
                        ["Headcount"]       = job.Headcount.ToString(),
                        ["EmploymentType"]  = job.EmploymentType.ToString(),
                        ["DepartmentId"]    = job.DepartmentId.ToString(),
                        ["DesignationId"]   = job.DesignationId.ToString(),
                        ["Priority"]        = request.PriorityLookupValueId?.ToString() ?? string.Empty
                    }
                };

                var approval = await _approvalService.SubmitAsync(approvalRequest, userId);

                // Link the approval back to the job record
                job.ApprovalRequestId = approval.Id;
                _repository.Update(job);
                await _repository.SaveChangesAsync();

                _logger.LogInformation(
                    "Approval request {Code} created for Requisition {JobId}",
                    approval.ApprovalRequestCode, job.Id);
            }

            _logger.LogInformation("Job created: {JobTitle} (ID: {JobId})", job.JobTitle, job.Id);
            return MapToDto(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job: {JobTitle}", request.JobTitle);
            throw;
        }
    }

    public async Task<JobDto> UpdateAsync(Guid id, UpdateJobDto request, Guid userId)
    {
        try
        {
            var job = await _repository.GetByIdAsync(id);
            if (job == null || job.IsDeleted)
                throw new InvalidOperationException("Job not found");

            if (!string.IsNullOrWhiteSpace(request.JobTitle)) job.JobTitle = request.JobTitle;
            if (request.EmploymentType.HasValue) job.EmploymentType = request.EmploymentType.Value;
            if (request.Headcount.HasValue) job.Headcount = request.Headcount.Value;
            if (request.PriorityLookupValueId.HasValue) job.PriorityLookupValueId = request.PriorityLookupValueId.Value;
            if (request.StatusLookupValueId.HasValue) job.StatusLookupValueId = request.StatusLookupValueId.Value;
            if (request.RecruiterEmployeeId.HasValue) job.RecruiterEmployeeId = request.RecruiterEmployeeId.Value;
            if (request.SalaryRangeMin.HasValue) job.SalaryRangeMin = request.SalaryRangeMin.Value;
            if (request.SalaryRangeMax.HasValue) job.SalaryRangeMax = request.SalaryRangeMax.Value;
            if (request.TargetStartDate.HasValue) job.TargetStartDate = request.TargetStartDate;
            if (request.PostingStartDate.HasValue) job.PostingStartDate = request.PostingStartDate;
            if (request.PostingCloseDate.HasValue) job.PostingCloseDate = request.PostingCloseDate;

            job.UpdatedAt = DateTime.UtcNow;
            job.UpdatedByUserId = userId;

            _repository.Update(job);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Job updated: {JobTitle} (ID: {JobId})", job.JobTitle, job.Id);
            return MapToDto(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job: {JobId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var job = await _repository.GetByIdAsync(id);
            if (job == null || job.IsDeleted)
                throw new InvalidOperationException("Job not found");

            job.IsDeleted = true;
            job.DeletedAt = DateTime.UtcNow;
            job.DeletedByUserId = userId;

            _repository.Update(job);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Job deleted: {JobId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job: {JobId}", id);
            throw;
        }
    }

    private static JobDto MapToDto(Job j) => new()
    {
        Id = j.Id,
        CompanyId = j.CompanyId,
        JobCode = j.JobCode,
        JobTitle = j.JobTitle,
        RecordType = j.RecordType,
        ParentJobId = j.ParentJobId,
        DepartmentId = j.DepartmentId,
        DesignationId = j.DesignationId,
        Headcount = j.Headcount,
        FilledCount = j.FilledCount,
        EmploymentType = j.EmploymentType,
        PriorityLookupValueId = j.PriorityLookupValueId,
        StatusLookupValueId = j.StatusLookupValueId,
        HiringManagerEmployeeId = j.HiringManagerEmployeeId,
        RecruiterEmployeeId = j.RecruiterEmployeeId,
        CurrencyCode = j.CurrencyCode,
        SalaryRangeMin = j.SalaryRangeMin,
        SalaryRangeMax = j.SalaryRangeMax,
        TargetStartDate = j.TargetStartDate,
        PostingStartDate = j.PostingStartDate,
        PostingCloseDate = j.PostingCloseDate,
        ApprovalRequestId = j.ApprovalRequestId,
        ClosedByEmployeeId = j.ClosedByEmployeeId,
        ClosedAt = j.ClosedAt,
        CancelReason = j.CancelReason,
        CreatedAt = j.CreatedAt,
        UpdatedAt = j.UpdatedAt
    };
}
