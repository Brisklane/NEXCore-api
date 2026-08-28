using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Building control: plan approvals, stage inspections and violation notices.
///
/// A plot-scheme society is its own planning authority, and the thing it is worst at is comparing
/// what somebody proposed against what the bye-laws allow. So the permitted figures sit on the
/// application beside the proposed ones and the breaches are computed — an officer approving a
/// plan sees "coverage 68% against a permitted 60%" rather than having to remember the limit.
/// </summary>
public partial class SocietyService
{
    public async Task<PaginatedResponse<BuildingPlanApplicationDto>> GetBuildingApplicationsAsync(
        ListQueryDto query, Guid societyId, BuildingApplicationStatus? status)
    {
        var q = Db.BuildingPlanApplications.ForCompany(Tenant)
            .Where(a => a.SocietyId == societyId)
            .WhereIf(status.HasValue, a => a.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), a => a.Reference.Contains(query.Search!))
            .OrderByDescending(a => a.SubmittedOn);

        return await PageAsync(q, query, list => MapApplicationsAsync(list, false));
    }

    public async Task<BuildingPlanApplicationDto?> GetBuildingApplicationAsync(Guid id)
    {
        var application = await Db.BuildingPlanApplications.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == id);
        if (application is null) return null;

        return (await MapApplicationsAsync([application], true))[0];
    }

    private async Task<List<BuildingPlanApplicationDto>> MapApplicationsAsync(
        List<BuildingPlanApplication> applications, bool includeInspections)
    {
        if (applications.Count == 0) return [];

        var unit = await AreaUnitAsync();
        var ids = applications.Select(a => a.Id).ToList();

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => applications.Select(a => a.PartyId).Contains(p.Id))
            .ToListAsync();

        var propertyIds = applications.Select(a => a.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        var unitIds = applications.Where(a => a.UnitId.HasValue).Select(a => a.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var officers = await AgentUserNamesAsync(applications.Select(a => a.ScrutinisedByUserId));

        var inspections = includeInspections
            ? await Db.BuildingInspections.ForCompany(Tenant)
                .Where(i => ids.Contains(i.BuildingPlanApplicationId))
                .OrderBy(i => i.InspectedOn)
                .ToListAsync()
            : [];

        var inspectors = await AgentUserNamesAsync(inspections.Select(i => i.InspectorUserId));

        return applications.Select(a =>
        {
            var person = people.FirstOrDefault(p => p.Id == a.PartyId);

            var dto = new BuildingPlanApplicationDto
            {
                Id = a.Id,
                Reference = a.Reference,
                SocietyId = a.SocietyId,
                UnitId = a.UnitId,
                UnitLabel = a.UnitId is null ? null : units.GetValueOrDefault(a.UnitId.Value),
                PropertyId = a.PropertyId,
                AddressOneLine = properties.GetValueOrDefault(a.PropertyId),
                PartyId = a.PartyId,
                PartyName = person is null ? "—" : RealEstateMapper.DisplayName(person),
                Phone = person?.PrimaryPhone,
                ApplicationType = a.ApplicationType,
                Status = a.Status,
                SubmittedOn = a.SubmittedOn,
                ProposedCoveredArea = RealEstateMapper.Area(a.ProposedCoveredAreaSqFt, unit),
                ProposedFloors = a.ProposedFloors,
                ProposedHeightFt = a.ProposedHeightFt,
                ProposedCoveragePercent = a.ProposedCoveragePercent,
                PermittedCoveragePercent = a.PermittedCoveragePercent,
                PermittedHeightFt = a.PermittedHeightFt,
                PermittedFloors = a.PermittedFloors,
                ArchitectName = a.ArchitectName,
                ArchitectLicence = a.ArchitectLicence,
                DrawingUrl = a.DrawingUrl,
                ScrutinyFee = a.ScrutinyFee,
                SecurityDeposit = a.SecurityDeposit,
                FeesPaid = a.FeesPaid,
                DuesCleared = a.DuesCleared,
                ScrutinisedByName = a.ScrutinisedByUserId is null ? null : officers.GetValueOrDefault(a.ScrutinisedByUserId.Value),
                DecidedOn = a.DecidedOn,
                Conditions = a.Conditions,
                RejectionReason = a.RejectionReason,
                ApprovalValidUntil = a.ApprovalValidUntil,
                NocIssuanceId = a.NocIssuanceId,
                IsCompleted = a.IsCompleted,
                Breaches = FindBreaches(a),
            };

            if (includeInspections)
            {
                dto.Inspections = inspections
                    .Where(i => i.BuildingPlanApplicationId == a.Id)
                    .Select(i => new BuildingInspectionDto
                    {
                        Id = i.Id,
                        Stage = i.Stage,
                        InspectedOn = i.InspectedOn,
                        InspectorName = i.InspectorUserId is null ? null : inspectors.GetValueOrDefault(i.InspectorUserId.Value),
                        IsCompliant = i.IsCompliant,
                        Findings = i.Findings,
                        PhotoUrls = string.IsNullOrWhiteSpace(i.PhotoUrls)
                            ? []
                            : i.PhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        ViolationNoticeId = i.ViolationNoticeId,
                        ReInspectionDue = i.ReInspectionDue,
                    })
                    .ToList();
            }

            return dto;
        }).ToList();
    }

    /// <summary>
    /// Where the proposal exceeds what the bye-laws allow, stated in the units an owner will
    /// argue in. This is the whole point of scrutiny, and it is arithmetic rather than judgement.
    /// </summary>
    private static List<string> FindBreaches(BuildingPlanApplication a)
    {
        var breaches = new List<string>();

        if (a.PermittedCoveragePercent is > 0m && a.ProposedCoveragePercent > a.PermittedCoveragePercent)
        {
            breaches.Add(
                $"Coverage of {a.ProposedCoveragePercent:N1}% exceeds the permitted {a.PermittedCoveragePercent:N1}% " +
                $"by {a.ProposedCoveragePercent - a.PermittedCoveragePercent.Value:N1} points.");
        }

        if (a.PermittedHeightFt is > 0m && a.ProposedHeightFt > a.PermittedHeightFt)
        {
            breaches.Add(
                $"Height of {a.ProposedHeightFt:N1} ft exceeds the permitted {a.PermittedHeightFt:N1} ft " +
                $"by {a.ProposedHeightFt - a.PermittedHeightFt.Value:N1} ft.");
        }

        if (a.PermittedFloors is > 0 && a.ProposedFloors > a.PermittedFloors)
            breaches.Add($"{a.ProposedFloors} floors proposed against a permitted {a.PermittedFloors}.");

        return breaches;
    }

    public async Task<BuildingPlanApplicationDto> SaveBuildingApplicationAsync(BuildingPlanApplicationDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");

        var application = dto.Id != Guid.Empty
            ? await Db.BuildingPlanApplications.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (application is null)
        {
            application = new BuildingPlanApplication
            {
                Reference = await numbering.NextMasterCodeAsync(Db.BuildingPlanApplications, "BPA"),
                SocietyId = dto.SocietyId,
                UnitId = dto.UnitId,
                PropertyId = dto.PropertyId,
                PartyId = dto.PartyId,
                SubmittedOn = dto.SubmittedOn == default ? Today : dto.SubmittedOn,
            }.StampNew(Tenant, userId);

            Db.BuildingPlanApplications.Add(application);
        }
        else
        {
            if (application.Status is BuildingApplicationStatus.Approved)
                throw new InvalidOperationException("An approved plan cannot be edited. Submit a revision instead.");

            application.StampUpdated(userId);
        }

        application.ApplicationType = dto.ApplicationType;
        application.ProposedCoveredAreaSqFt = RealEstateMapper.ToSquareFeet(
            dto.ProposedCoveredArea.DisplayValue, dto.ProposedCoveredArea.DisplayUnit);
        application.ProposedFloors = dto.ProposedFloors;
        application.ProposedHeightFt = dto.ProposedHeightFt;
        application.ProposedCoveragePercent = dto.ProposedCoveragePercent;
        application.ArchitectName = dto.ArchitectName;
        application.ArchitectLicence = dto.ArchitectLicence;
        application.DrawingUrl = dto.DrawingUrl;
        application.ScrutinyFee = dto.ScrutinyFee;
        application.SecurityDeposit = dto.SecurityDeposit;
        application.FeesPaid = dto.FeesPaid;

        // The permitted figures come off the parcel's land-use record rather than being typed by
        // the applicant, which is the difference between scrutiny and a rubber stamp.
        var parcel = await Db.LandParcels.ForCompany(Tenant)
            .Where(p => p.ProjectId != null
                     && Db.Societies.ForCompany(Tenant).Any(s => s.Id == dto.SocietyId && s.ProjectId == p.ProjectId))
            .Select(p => new { p.MaxCoveragePercent, p.MaxHeightFt, p.MaxFloorAreaRatio })
            .FirstOrDefaultAsync();

        application.PermittedCoveragePercent = dto.PermittedCoveragePercent ?? parcel?.MaxCoveragePercent;
        application.PermittedHeightFt = dto.PermittedHeightFt ?? parcel?.MaxHeightFt;
        application.PermittedFloors = dto.PermittedFloors;

        // A plan cannot be scrutinised while the owner is in arrears — the deposit and the
        // scrutiny fee are society money and the leverage only exists at this point.
        var outstanding = await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(b => b.PropertyId == dto.PropertyId && b.Balance > 0m)
            .SumAsync(b => b.Balance);

        application.DuesCleared = outstanding <= 0m;

        await Db.SaveChangesAsync();
        return (await GetBuildingApplicationAsync(application.Id))!;
    }

    public async Task<BuildingPlanApplicationDto> DecideBuildingApplicationAsync(
        Guid id, BuildingApplicationStatus status, string? conditions, string? rejectionReason, Guid userId)
    {
        var application = await RequireAsync<BuildingPlanApplication>(id, "That application does not exist.");

        if (application.Status == BuildingApplicationStatus.Approved)
            throw new InvalidOperationException($"This plan was approved on {application.DecidedOn:dd MMM yyyy}.");

        var today = Today;

        if (status == BuildingApplicationStatus.Approved)
        {
            if (!application.FeesPaid && application.ScrutinyFee > 0m)
                throw new InvalidOperationException($"The scrutiny fee of {application.ScrutinyFee:N0} has not been received.");

            if (!application.DuesCleared)
                throw new InvalidOperationException("The owner is in arrears. Clear the dues before approving the plan.");

            var breaches = FindBreaches(application);

            // A breach can be approved — societies grant relaxations — but never silently. The
            // conditions have to record what was relaxed and on what basis.
            if (breaches.Count > 0 && string.IsNullOrWhiteSpace(conditions))
            {
                throw new InvalidOperationException(
                    $"This plan breaches the bye-laws: {string.Join(" ", breaches)} " +
                    "Record the relaxation and its basis in the conditions, or refuse the plan.");
            }

            application.ApprovalValidUntil = today.AddYears(2);
        }

        if (status == BuildingApplicationStatus.Rejected && string.IsNullOrWhiteSpace(rejectionReason))
            throw new InvalidOperationException("A refusal has to say why. The applicant is entitled to know what to change.");

        application.Status = status;
        application.DecidedOn = today;
        application.ScrutinisedByUserId = userId;
        application.Conditions = conditions;
        application.RejectionReason = rejectionReason;
        application.StampUpdated(userId);

        await Db.SaveChangesAsync();

        await QueueNotificationAsync(
            status == BuildingApplicationStatus.Approved ? "BuildingPlanApproved" : "BuildingPlanDecided",
            $"{application.Reference} — {status}",
            status == BuildingApplicationStatus.Approved
                ? $"Approved, valid to {application.ApprovalValidUntil:dd MMM yyyy}." +
                  (string.IsNullOrWhiteSpace(conditions) ? "" : $" Conditions: {conditions}")
                : rejectionReason,
            $"/realestate/building-applications/{application.Id}",
            recipientPartyId: application.PartyId,
            entityType: "BuildingPlanApplication",
            entityId: application.Id);

        await Db.SaveChangesAsync();
        return (await GetBuildingApplicationAsync(id))!;
    }

    public async Task<BuildingInspectionDto> RecordBuildingInspectionAsync(BuildingInspectionDto dto, Guid userId)
    {
        var inspection = dto.Id != Guid.Empty
            ? await Db.BuildingInspections.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == dto.Id)
            : null;

        if (inspection is null)
        {
            inspection = new BuildingInspection().StampNew(Tenant, userId);
            Db.BuildingInspections.Add(inspection);
        }
        else inspection.StampUpdated(userId);

        inspection.Stage = dto.Stage;
        inspection.InspectedOn = dto.InspectedOn == default ? Today : dto.InspectedOn;
        inspection.InspectorUserId = userId;
        inspection.IsCompliant = dto.IsCompliant;
        inspection.Findings = dto.Findings;
        inspection.PhotoUrls = dto.PhotoUrls.Count == 0 ? null : string.Join('\n', dto.PhotoUrls);
        inspection.ViolationNoticeId = dto.ViolationNoticeId;

        // A non-compliant stage gets re-inspected on a date, not "soon". Without one the file
        // goes quiet and the building goes up anyway.
        if (!dto.IsCompliant)
        {
            if (string.IsNullOrWhiteSpace(dto.Findings))
                throw new InvalidOperationException("A non-compliant inspection has to record what was found.");

            inspection.ReInspectionDue = dto.ReInspectionDue ?? inspection.InspectedOn.AddDays(14);
        }

        await Db.SaveChangesAsync();

        var inspectors = await AgentUserNamesAsync([inspection.InspectorUserId]);

        return new BuildingInspectionDto
        {
            Id = inspection.Id,
            Stage = inspection.Stage,
            InspectedOn = inspection.InspectedOn,
            InspectorName = inspection.InspectorUserId is null ? null : inspectors.GetValueOrDefault(inspection.InspectorUserId.Value),
            IsCompliant = inspection.IsCompliant,
            Findings = inspection.Findings,
            PhotoUrls = dto.PhotoUrls,
            ViolationNoticeId = inspection.ViolationNoticeId,
            ReInspectionDue = inspection.ReInspectionDue,
        };
    }

    /// <summary>
    /// A violation notice. A stop-work notice is the strongest thing a society can serve, so it
    /// carries a compliance date, a penalty and the evidence — and the security deposit taken at
    /// plan stage is what makes it enforceable.
    /// </summary>
    public async Task<ViolationNoticeDto> IssueViolationAsync(ViolationNoticeDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");

        if (string.IsNullOrWhiteSpace(dto.EvidenceUrl))
            throw new InvalidOperationException("A violation notice needs photographic evidence attached.");

        var today = Today;

        var notice = new ViolationNotice
        {
            NoticeNumber = await numbering.NextNoticeNumberAsync(DateTime.UtcNow),
            SocietyId = dto.SocietyId,
            PropertyId = dto.PropertyId,
            BuildingPlanApplicationId = dto.Id == Guid.Empty ? null : dto.Id,
            PartyId = dto.PartyId,
            ViolationType = dto.ViolationType,
            Description = dto.Description,
            IssuedOn = dto.IssuedOn == default ? today : dto.IssuedOn,
            ComplyByDate = dto.ComplyByDate == default ? today.AddDays(15) : dto.ComplyByDate,
            PenaltyAmount = dto.PenaltyAmount,
            IsStopWork = dto.IsStopWork,
            EvidenceUrl = dto.EvidenceUrl,
        }.StampNew(Tenant, userId);

        Db.ViolationNotices.Add(notice);

        // A stop-work notice halts the plan approval as well, so nobody can point at a live
        // permission while ignoring the notice.
        if (dto.IsStopWork && dto.PropertyId is not null)
        {
            var application = await Db.BuildingPlanApplications.ForCompany(Tenant)
                .Where(a => a.PropertyId == dto.PropertyId && a.Status == BuildingApplicationStatus.Approved)
                .FirstOrDefaultAsync();

            if (application is not null)
            {
                application.Status = BuildingApplicationStatus.ViolationNoticed;
                application.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();

        await QueueNotificationAsync(
            "ViolationNoticeIssued",
            $"{(dto.IsStopWork ? "Stop-work notice" : "Violation notice")} {notice.NoticeNumber}",
            $"{notice.ViolationType}. Comply by {notice.ComplyByDate:dd MMM yyyy}" +
            (notice.PenaltyAmount > 0m ? $", penalty {notice.PenaltyAmount:N0}." : "."),
            $"/realestate/violations/{notice.Id}",
            recipientPartyId: notice.PartyId,
            entityType: "ViolationNotice",
            entityId: notice.Id,
            severity: AlertSeverity.Critical);

        await Db.SaveChangesAsync();
        return (await MapViolationsAsync([notice]))[0];
    }

    public async Task<ViolationNoticeDto> CloseViolationAsync(Guid id, DateOnly compliedOn, Guid userId)
    {
        var notice = await RequireAsync<ViolationNotice>(id, "That notice does not exist.");

        if (notice.IsComplied)
            throw new InvalidOperationException($"This notice was complied with on {notice.CompliedOn:dd MMM yyyy}.");

        notice.IsComplied = true;
        notice.CompliedOn = compliedOn;
        notice.StampUpdated(userId);

        // Complying with a stop-work notice lifts the suspension on the plan.
        if (notice.IsStopWork && notice.PropertyId is not null)
        {
            var application = await Db.BuildingPlanApplications.ForCompany(Tenant)
                .Where(a => a.PropertyId == notice.PropertyId && a.Status == BuildingApplicationStatus.ViolationNoticed)
                .FirstOrDefaultAsync();

            if (application is not null)
            {
                var others = await Db.ViolationNotices.ForCompany(Tenant)
                    .CountAsync(v => v.PropertyId == notice.PropertyId && v.IsStopWork && !v.IsComplied && v.Id != id);

                if (others == 0)
                {
                    application.Status = BuildingApplicationStatus.Approved;
                    application.StampUpdated(userId);
                }
            }
        }

        await Db.SaveChangesAsync();
        return (await MapViolationsAsync([notice]))[0];
    }

    public async Task<PaginatedResponse<ViolationNoticeDto>> GetViolationsAsync(ListQueryDto query, Guid societyId, bool openOnly)
    {
        var q = Db.ViolationNotices.ForCompany(Tenant)
            .Where(v => v.SocietyId == societyId)
            .WhereIf(openOnly, v => !v.IsComplied && !v.IsWithdrawn)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), v => v.NoticeNumber.Contains(query.Search!))
            .OrderByDescending(v => v.IsStopWork).ThenBy(v => v.ComplyByDate);

        return await PageAsync(q, query, MapViolationsAsync);
    }

    private async Task<List<ViolationNoticeDto>> MapViolationsAsync(List<ViolationNotice> notices)
    {
        if (notices.Count == 0) return [];

        var today = Today;
        var names = await PartyNamesAsync(notices.Select(n => n.PartyId));

        var propertyIds = notices.Where(n => n.PropertyId.HasValue).Select(n => n.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        return notices.Select(n => new ViolationNoticeDto
        {
            Id = n.Id,
            NoticeNumber = n.NoticeNumber,
            SocietyId = n.SocietyId,
            PropertyId = n.PropertyId,
            AddressOneLine = n.PropertyId is null ? null : properties.GetValueOrDefault(n.PropertyId.Value),
            PartyId = n.PartyId,
            PartyName = names.GetValueOrDefault(n.PartyId, "—"),
            ViolationType = n.ViolationType,
            Description = n.Description ?? string.Empty,
            IssuedOn = n.IssuedOn,
            ComplyByDate = n.ComplyByDate,
            PenaltyAmount = n.PenaltyAmount,
            IsStopWork = n.IsStopWork,
            SecurityForfeited = n.SecurityForfeited,
            EvidenceUrl = n.EvidenceUrl,
            DocumentUrl = n.DocumentUrl,
            IsComplied = n.IsComplied,
            CompliedOn = n.CompliedOn,
            ReferredToAuthority = n.ReferredToAuthority,
            IsWithdrawn = n.IsWithdrawn,
            IsOverdue = !n.IsComplied && !n.IsWithdrawn && n.ComplyByDate < today,
        }).ToList();
    }
}
