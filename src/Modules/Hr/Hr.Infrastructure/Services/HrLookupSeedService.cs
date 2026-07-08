using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Helpers;

namespace Hr.Infrastructure.Services;

/// <summary>
/// Seeds the mandatory LookupTypes and LookupValues required by the HR module
/// into the current tenant's schema.
///
/// Must be called once per tenant via POST /api/v1/hr/seed/lookup-values before
/// submitting any approval requests.
///
/// Idempotent - safe to call multiple times; existing records are never duplicated.
/// </summary>
public class HrLookupSeedService
{
    private readonly HrDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HrLookupSeedService> _logger;

    public HrLookupSeedService(
        HrDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<HrLookupSeedService> logger)
    {
        _context             = context;
        _httpContextAccessor = httpContextAccessor;
        _logger              = logger;
    }

    /// <summary>
    /// Seeds all LookupTypes and their values required by the HR module.
    /// Returns a summary of what was created vs already existed.
    /// </summary>
    public async Task<SeedResult> SeedAllAsync(Guid userId)
    {
        var user = _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("HTTP context not available.");

        var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(user);
        var resolvedBusinessUnitId = businessUnitId ?? Guid.Empty;

        var result = new SeedResult();

        await SeedApprovalStatusesAsync(companyId, branchId, resolvedBusinessUnitId, userId, result);
        await SeedJobStatusesAsync(companyId, branchId, resolvedBusinessUnitId, userId, result);
        await SeedApplicationStageAsync(companyId, branchId, resolvedBusinessUnitId, userId, result);
        await SeedApplicationStatusAsync(companyId, branchId, resolvedBusinessUnitId, userId, result);
        await SeedInterviewStatusAsync(companyId, branchId, resolvedBusinessUnitId, userId, result);
        await SeedPostingStatusAsync(companyId, branchId, resolvedBusinessUnitId, userId, result);
        await SeedPriorityAsync(companyId, branchId, resolvedBusinessUnitId, userId, result);
        await SeedChannelTypeAsync(companyId, branchId, resolvedBusinessUnitId, userId, result);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "HR seed completed for tenant {CompanyId} - {Created} created, {Skipped} already existed",
            companyId, result.Created, result.Skipped);

        return result;
    }

    // Approval Statuses

    private async Task SeedApprovalStatusesAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, SeedResult result)
    {
        var typeCode = "APPROVAL_STATUS";
        var lookupType = await EnsureLookupTypeAsync(
            typeCode, "Approval Status", "HR", "ApprovalRequest",
            companyId, branchId, businessUnitId, userId, result);

        // These codes MUST match the constants in ApprovalService
        var values = new[]
        {
            ("PENDING",      "Pending",       1),
            ("WAITING",      "Waiting",       2),
            ("APPROVED",     "Approved",      3),
            ("REJECTED",     "Rejected",      4),
            ("CANCELLED",    "Cancelled",     5),
            ("IN_PROGRESS",  "In Progress",   6)
        };

        foreach (var (code, name, sortOrder) in values)
            await EnsureLookupValueAsync(lookupType.Id, code, name, sortOrder,
                companyId, branchId, businessUnitId, userId, result);
    }

    // Job Statuses

    private async Task SeedJobStatusesAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, SeedResult result)
    {
        var typeCode = "JOB_STATUS";
        var lookupType = await EnsureLookupTypeAsync(
            typeCode, "Job Status", "HR", "Job",
            companyId, branchId, businessUnitId, userId, result);

        // These codes are used by ApprovalService.FinaliseEntityAsync
        var values = new[]
        {
            ("JOB_DRAFT",        "Draft",               1),
            ("JOB_PENDING",      "Pending Approval",    2),
            ("JOB_APPROVED",     "Approved",            3),
            ("JOB_REJECTED",     "Rejected",            4),
            ("JOB_OPEN",         "Open",                5),
            ("JOB_CLOSED",       "Closed",              6),
            ("JOB_CANCELLED",    "Cancelled",           7)
        };

        foreach (var (code, name, sortOrder) in values)
            await EnsureLookupValueAsync(lookupType.Id, code, name, sortOrder,
                companyId, branchId, businessUnitId, userId, result);
    }

    // Application Stage

    private async Task SeedApplicationStageAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, SeedResult result)
    {
        var typeCode = "APPLICATION_STAGE";
        var lookupType = await EnsureLookupTypeAsync(
            typeCode, "Application Stage", "Recruitment", "Application",
            companyId, branchId, businessUnitId, userId, result);

        var values = new[]
        {
            ("STAGE_APPLIED",           "Applied",              1),
            ("STAGE_SCREENED",          "Screened",             2),
            ("STAGE_SHORTLISTED",       "Shortlisted",          3),
            ("STAGE_INTERVIEW_INVITE",  "Interview Invited",    4),
            ("STAGE_INTERVIEW_COMPLETE","Interview Complete",   5),
            ("STAGE_OFFER_EXTENDED",   "Offer Extended",       6),
            ("STAGE_OFFER_ACCEPTED",   "Offer Accepted",       7),
            ("STAGE_OFFER_REJECTED",   "Offer Rejected",       8),
            ("STAGE_HIRED",             "Hired",                9),
            ("STAGE_REJECTED",          "Rejected",             10)
        };

        foreach (var (code, name, sortOrder) in values)
            await EnsureLookupValueAsync(lookupType.Id, code, name, sortOrder,
                companyId, branchId, businessUnitId, userId, result);
    }

    // Application Status

    private async Task SeedApplicationStatusAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, SeedResult result)
    {
        var typeCode = "APPLICATION_STATUS";
        var lookupType = await EnsureLookupTypeAsync(
            typeCode, "Application Status", "Recruitment", "Application",
            companyId, branchId, businessUnitId, userId, result);

        var values = new[]
        {
            ("APP_ACTIVE",       "Active",       1),
            ("APP_WITHDRAWN",    "Withdrawn",    2),
            ("APP_ARCHIVED",     "Archived",     3),
            ("APP_HOLD",         "Hold",         4)
        };

        foreach (var (code, name, sortOrder) in values)
            await EnsureLookupValueAsync(lookupType.Id, code, name, sortOrder,
                companyId, branchId, businessUnitId, userId, result);
    }

    // Interview Status

    private async Task SeedInterviewStatusAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, SeedResult result)
    {
        var typeCode = "INTERVIEW_STATUS";
        var lookupType = await EnsureLookupTypeAsync(
            typeCode, "Interview Status", "Recruitment", "Interview",
            companyId, branchId, businessUnitId, userId, result);

        var values = new[]
        {
            ("INTERVIEW_SCHEDULED",   "Scheduled",   1),
            ("INTERVIEW_INVITED",     "Invited",     2),
            ("INTERVIEW_COMPLETED",   "Completed",   3),
            ("INTERVIEW_CANCELLED",   "Cancelled",   4),
            ("INTERVIEW_RESCHEDULED", "Rescheduled", 5)
        };

        foreach (var (code, name, sortOrder) in values)
            await EnsureLookupValueAsync(lookupType.Id, code, name, sortOrder,
                companyId, branchId, businessUnitId, userId, result);
    }

    // Posting Status

    private async Task SeedPostingStatusAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, SeedResult result)
    {
        var typeCode = "POSTING_STATUS";
        var lookupType = await EnsureLookupTypeAsync(
            typeCode, "Posting Status", "Recruitment", "JobPostingChannel",
            companyId, branchId, businessUnitId, userId, result);

        var values = new[]
        {
            ("POSTING_DRAFT",       "Draft",        1),
            ("POSTING_PUBLISHED",   "Published",    2),
            ("POSTING_CLOSED",      "Closed",       3),
            ("POSTING_PAUSED",      "Paused",       4),
            ("POSTING_ARCHIVED",    "Archived",     5)
        };

        foreach (var (code, name, sortOrder) in values)
            await EnsureLookupValueAsync(lookupType.Id, code, name, sortOrder,
                companyId, branchId, businessUnitId, userId, result);
    }

    // Priority

    private async Task SeedPriorityAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, SeedResult result)
    {
        var typeCode = "PRIORITY";
        var lookupType = await EnsureLookupTypeAsync(
            typeCode, "Priority", "Recruitment", "Job",
            companyId, branchId, businessUnitId, userId, result);

        var values = new[]
        {
            ("PRIORITY_CRITICAL", "Critical",  1),
            ("PRIORITY_HIGH",     "High",      2),
            ("PRIORITY_MEDIUM",   "Medium",    3),
            ("PRIORITY_LOW",      "Low",       4)
        };

        foreach (var (code, name, sortOrder) in values)
            await EnsureLookupValueAsync(lookupType.Id, code, name, sortOrder,
                companyId, branchId, businessUnitId, userId, result);
    }

    // Channel Type

    private async Task SeedChannelTypeAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, SeedResult result)
    {
        var typeCode = "CHANNEL_TYPE";
        var lookupType = await EnsureLookupTypeAsync(
            typeCode, "Channel Type", "Recruitment", "JobPostingChannel",
            companyId, branchId, businessUnitId, userId, result);

        var values = new[]
        {
            ("CHANNEL_LINKEDIN",      "LinkedIn",           1),
            ("CHANNEL_INDEED",        "Indeed",             2),
            ("CHANNEL_CAREERSITE",    "Career Site",        3),
            ("CHANNEL_REFERRAL",      "Referral",           4),
            ("CHANNEL_AGENCY",        "Agency",             5),
            ("CHANNEL_JOBFAIR",       "Job Fair",           6),
            ("CHANNEL_INTERNAL",      "Internal",           7),
            ("CHANNEL_CAMPUS",        "Campus Recruiting",  8),
            ("CHANNEL_SOCIAL",        "Social Media",       9),
            ("CHANNEL_OTHER",         "Other",              10)
        };

        foreach (var (code, name, sortOrder) in values)
            await EnsureLookupValueAsync(lookupType.Id, code, name, sortOrder,
                companyId, branchId, businessUnitId, userId, result);
    }

    // Helpers

    private async Task<LookupType> EnsureLookupTypeAsync(
        string code, string name, string moduleName, string entityName,
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId,
        SeedResult result)
    {
        var existing = await _context.LookupTypes.FirstOrDefaultAsync(t =>
            t.Code         == code &&
            t.CompanyId    == companyId &&
            t.BranchId     == branchId &&
            t.BusinessUnitId == businessUnitId &&
            !t.IsDeleted);

        if (existing != null)
        {
            result.Skipped++;
            return existing;
        }

        var newType = new LookupType
        {
            Code            = code,
            Name            = name,
            ModuleName      = moduleName,
            EntityName      = entityName,
            IsActive        = true,
            IsSystem        = true,
            SortOrder       = 0,
            CompanyId       = companyId,
            BranchId        = branchId,
            BusinessUnitId  = businessUnitId,
            CreatedAt       = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _context.LookupTypes.AddAsync(newType);
        result.Created++;

        _logger.LogInformation("Seeded LookupType: {Code}", code);
        return newType;
    }

    private async Task EnsureLookupValueAsync(
        Guid lookupTypeId, string code, string name, int sortOrder,
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId,
        SeedResult result)
    {
        var exists = await _context.LookupValues.AnyAsync(v =>
            v.Code           == code &&
            v.LookupTypeId   == lookupTypeId &&
            v.CompanyId      == companyId &&
            v.BranchId       == branchId &&
            v.BusinessUnitId == businessUnitId &&
            !v.IsDeleted);

        if (exists)
        {
            result.Skipped++;
            return;
        }

        await _context.LookupValues.AddAsync(new LookupValue
        {
            LookupTypeId    = lookupTypeId,
            Code            = code,
            Name            = name,
            SortOrder       = sortOrder,
            IsActive        = true,
            IsDefault       = false,
            CompanyId       = companyId,
            BranchId        = branchId,
            BusinessUnitId  = businessUnitId,
            CreatedAt       = DateTime.UtcNow,
            CreatedByUserId = userId
        });

        result.Created++;
        _logger.LogInformation("Seeded LookupValue: {Code}", code);
    }
}

public class SeedResult
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public string Summary => $"{Created} record(s) created, {Skipped} already existed.";
}
