using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Statutory approvals, licences, the compliance calendar, regulatory filings, document generation,
/// signatures, the physical record room and litigation.
///
/// Every item here shares one property: missing it costs money on a date somebody could have seen
/// coming. An approval lapses, a licence expires, a quarterly return goes in late, a case is heard
/// with nobody present. So everything with a date lands on one calendar with one owner and one
/// alert, rather than living in the folder of whoever set it up.
/// </summary>
public partial class FinanceService
{
    // ═══ Statutory approvals ═════════════════════════════════════════════════

    public async Task<PaginatedResponse<ApprovalRecordDto>> GetApprovalRecordsAsync(
        ListQueryDto query, Guid? projectId, ApprovalState? state)
    {
        var q = Db.ApprovalRecords.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, a => a.ProjectId == projectId)
            .WhereIf(state.HasValue, a => a.State == state)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                a => a.Reference.Contains(query.Search!)
                  || (a.Authority ?? "").Contains(query.Search!)
                  || (a.ApprovalNumber ?? "").Contains(query.Search!))
            .OrderBy(a => a.State == ApprovalState.Granted)
            .ThenBy(a => a.ValidUntil ?? DateOnly.MaxValue);

        return await PageAsync(q, query, MapApprovalRecordsAsync);
    }

    private async Task<List<ApprovalRecordDto>> MapApprovalRecordsAsync(List<ApprovalRecord> records)
    {
        if (records.Count == 0) return [];

        var owners = await AgentUserNamesAsync(records.Select(r => r.OwnerUserId));

        var milestoneIds = records.Where(r => r.BlocksMilestoneId != null)
            .Select(r => r.BlocksMilestoneId!.Value).Distinct().ToList();

        var milestones = milestoneIds.Count == 0
            ? []
            : await Db.ProjectMilestones.ForCompany(Tenant)
                .Where(m => milestoneIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Name);

        return records.Select(a => new ApprovalRecordDto
        {
            Id = a.Id,
            Reference = a.Reference,
            Kind = a.Kind,
            State = a.State,
            Authority = a.Authority,
            ApplicationNumber = a.ApplicationNumber,
            ApprovalNumber = a.ApprovalNumber,
            AppliedOn = a.AppliedOn,
            GrantedOn = a.GrantedOn,
            ValidUntil = a.ValidUntil,
            TotalCost = a.TotalCost,
            OwnerName = a.OwnerUserId is null ? null : owners.GetValueOrDefault(a.OwnerUserId.Value),
            Conditions = a.Conditions,
            DocumentUrl = a.DocumentUrl,
            IsBlocking = a.IsBlocking,
            BlocksMilestoneName = a.BlocksMilestoneId is null
                ? null
                : milestones.GetValueOrDefault(a.BlocksMilestoneId.Value),
            IsMandatory = a.IsMandatory,
            DaysToExpiry = a.ValidUntil is null ? null : a.ValidUntil.Value.DayNumber - Today.DayNumber,

            // Overdue means one of two different failures: a query nobody answered, or an approval
            // that has quietly expired while work carried on under it.
            IsOverdue = (a.QueryResponseDue is not null && a.QueryResponseDue < Today
                         && a.State == ApprovalState.QueryRaised)
                        || (a.ValidUntil is not null && a.ValidUntil < Today
                            && a.State is ApprovalState.Granted or ApprovalState.GrantedWithConditions),
        }).ToList();
    }

    public async Task<ApprovalRecordDto> SaveApprovalRecordAsync(ApprovalRecordDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var record = isNew
            ? new ApprovalRecord { Reference = await numbering.NextMasterCodeAsync(Db.ApprovalRecords, "APR") }
            : await Db.ApprovalRecords.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
              ?? throw new InvalidOperationException("That approval record does not exist.");

        if (dto.State is ApprovalState.Granted or ApprovalState.GrantedWithConditions)
        {
            if (string.IsNullOrWhiteSpace(dto.ApprovalNumber))
                throw new InvalidOperationException(
                    "A granted approval needs its approval number. It is what everything downstream quotes.");

            if (dto.GrantedOn is null)
                throw new InvalidOperationException("A granted approval needs the date it was granted.");
        }

        if (dto.State == ApprovalState.GrantedWithConditions && string.IsNullOrWhiteSpace(dto.Conditions))
            throw new InvalidOperationException(
                "An approval granted with conditions needs those conditions recorded — they are what a "
                + "site engineer has to work to.");

        var wasBlocking = record.State != ApprovalState.Granted && record.State != ApprovalState.GrantedWithConditions;

        record.Kind = dto.Kind;
        record.State = dto.State;
        record.Authority = dto.Authority;
        record.ApplicationNumber = dto.ApplicationNumber;
        record.ApprovalNumber = dto.ApprovalNumber;
        record.AppliedOn = dto.AppliedOn;
        record.GrantedOn = dto.GrantedOn;
        record.ValidUntil = dto.ValidUntil;
        record.TotalCost = dto.TotalCost;
        record.Conditions = dto.Conditions;
        record.DocumentUrl = dto.DocumentUrl;
        record.IsBlocking = dto.IsBlocking;
        record.IsMandatory = dto.IsMandatory;

        if (isNew)
        {
            record.StampNew(Tenant, userId);
            Db.ApprovalRecords.Add(record);
        }
        else
        {
            record.StampUpdated(userId);
            record.RenewalAlertSent = false;
        }

        if (record.ValidUntil is not null)
        {
            await UpsertCalendarEntryAsync(
                $"{Humanise(record.Kind)} expires — {record.ApprovalNumber ?? record.Reference}",
                "Approval", record.ValidUntil.Value, userId,
                projectId: record.ProjectId,
                approvalRecordId: record.Id,
                severity: record.IsMandatory ? AlertSeverity.Critical : AlertSeverity.Warning,
                alertDaysBefore: record.AlertDaysBefore);
        }

        if (record.QueryResponseDue is not null && record.State == ApprovalState.QueryRaised)
        {
            await UpsertCalendarEntryAsync(
                $"Respond to query on {record.Reference}", "Approval",
                record.QueryResponseDue.Value, userId,
                projectId: record.ProjectId, approvalRecordId: record.Id,
                severity: AlertSeverity.Critical, alertDaysBefore: 7);
        }

        // An approval coming through unblocks whatever was waiting on it. Telling the people who
        // were waiting is the whole value of having recorded the dependency.
        var nowGranted = record.State is ApprovalState.Granted or ApprovalState.GrantedWithConditions;

        if (wasBlocking && nowGranted && record.BlocksMilestoneId is not null)
        {
            await QueueNotificationAsync(
                "approval.granted",
                $"{Humanise(record.Kind)} granted",
                $"{record.ApprovalNumber} granted on {record.GrantedOn:dd MMM yyyy}. The milestone it was holding can now proceed.",
                $"/realestate/projects/{record.ProjectId}/approvals",
                entityType: nameof(ApprovalRecord),
                entityId: record.Id);
        }

        await Db.SaveChangesAsync();

        return (await MapApprovalRecordsAsync([record]))[0];
    }

    private static string Humanise(ApprovalKind kind)
        => Regex.Replace(kind.ToString(), "(?<!^)([A-Z])", " $1");

    // ═══ Licences ════════════════════════════════════════════════════════════

    public async Task<List<LicenceRecordDto>> GetLicencesAsync(bool expiringOnly)
    {
        var licences = await Db.LicenceRecords.ForCompany(Tenant)
            .WhereIf(expiringOnly, l => l.IsCurrent)
            .OrderBy(l => l.ExpiresOn)
            .ToListAsync();

        var offices = await Db.RealEstateOffices.ForCompany(Tenant)
            .ToDictionaryAsync(o => o.Id, o => o.Name);

        var agents = await Db.AgentProfiles.ForCompany(Tenant)
            .ToDictionaryAsync(a => a.Id, a => a.DisplayName);

        var mapped = licences.Select(l =>
        {
            var days = l.ExpiresOn.DayNumber - Today.DayNumber;

            return new LicenceRecordDto
            {
                Id = l.Id,
                Reference = l.Reference,
                LicenceType = l.LicenceType,
                LicenceNumber = l.LicenceNumber,
                Authority = l.Authority,
                OfficeName = l.OfficeId is null ? null : offices.GetValueOrDefault(l.OfficeId.Value),
                AgentName = l.AgentProfileId is null ? null : agents.GetValueOrDefault(l.AgentProfileId.Value),
                IssuedOn = l.IssuedOn,
                ExpiresOn = l.ExpiresOn,
                DaysToExpiry = days,
                IsExpired = days < 0,
                IsExpiringSoon = days >= 0 && days <= l.AlertDaysBefore,
                RenewalFee = l.RenewalFee,
                IsMandatoryToTrade = l.IsMandatoryToTrade,
                DocumentUrl = l.DocumentUrl,
                IsCurrent = l.IsCurrent,
            };
        }).ToList();

        return expiringOnly
            ? mapped.Where(l => l.IsExpired || l.IsExpiringSoon).ToList()
            : mapped;
    }

    public async Task<LicenceRecordDto> SaveLicenceAsync(LicenceRecordDto dto, Guid userId)
    {
        if (dto.ExpiresOn <= dto.IssuedOn)
            throw new InvalidOperationException("A licence cannot expire on or before the day it was issued.");

        var isNew = dto.Id == Guid.Empty;

        var licence = isNew
            ? new LicenceRecord { Reference = await numbering.NextMasterCodeAsync(Db.LicenceRecords, "LIC") }
            : await Db.LicenceRecords.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == dto.Id)
              ?? throw new InvalidOperationException("That licence does not exist.");

        licence.LicenceType = dto.LicenceType;
        licence.LicenceNumber = dto.LicenceNumber;
        licence.Authority = dto.Authority;
        licence.IssuedOn = dto.IssuedOn;
        licence.ExpiresOn = dto.ExpiresOn;
        licence.RenewalFee = dto.RenewalFee;
        licence.IsMandatoryToTrade = dto.IsMandatoryToTrade;
        licence.DocumentUrl = dto.DocumentUrl;
        licence.IsCurrent = dto.IsCurrent;
        licence.RenewalAlertSent = false;

        if (isNew)
        {
            licence.StampNew(Tenant, userId);
            Db.LicenceRecords.Add(licence);
        }
        else
        {
            licence.StampUpdated(userId);
        }

        await UpsertCalendarEntryAsync(
            $"{licence.LicenceType} licence renewal — {licence.LicenceNumber}", "Licence",
            licence.ExpiresOn, userId,
            licenceRecordId: licence.Id,
            severity: licence.IsMandatoryToTrade ? AlertSeverity.Critical : AlertSeverity.Warning,
            alertDaysBefore: licence.AlertDaysBefore,
            penalty: licence.RenewalFee);

        await Db.SaveChangesAsync();

        return (await GetLicencesAsync(false)).First(l => l.Id == licence.Id);
    }

    // ═══ Compliance calendar ═════════════════════════════════════════════════

    public async Task<List<ComplianceCalendarEntryDto>> GetComplianceCalendarAsync(
        DateOnly from, DateOnly to, string? category)
    {
        var entries = await Db.ComplianceCalendarEntries.ForCompany(Tenant)
            .Where(e => e.DueDate >= from && e.DueDate <= to)
            .WhereIf(!string.IsNullOrWhiteSpace(category), e => e.Category == category)
            .OrderBy(e => e.DueDate)
            .ToListAsync();

        // Anything already overdue belongs on the screen whatever window was asked for. A missed
        // deadline does not stop mattering because somebody scrolled to next month.
        var overdue = await Db.ComplianceCalendarEntries.ForCompany(Tenant)
            .Where(e => !e.IsCompleted && e.DueDate < from)
            .WhereIf(!string.IsNullOrWhiteSpace(category), e => e.Category == category)
            .OrderBy(e => e.DueDate)
            .ToListAsync();

        var all = overdue.Concat(entries).DistinctBy(e => e.Id).ToList();

        if (all.Count == 0) return [];

        var projectNames = await ProjectNamesAsync(all.Select(e => e.ProjectId));
        var owners = await AgentUserNamesAsync(all.Select(e => e.OwnerUserId));

        var propertyIds = all.Where(e => e.PropertyId != null).Select(e => e.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToListAsync();

        var societyIds = all.Where(e => e.SocietyId != null).Select(e => e.SocietyId!.Value).Distinct().ToList();

        var societies = societyIds.Count == 0
            ? []
            : await Db.Societies.ForCompany(Tenant)
                .Where(s => societyIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        return all.Select(e => new ComplianceCalendarEntryDto
        {
            Id = e.Id,
            Title = e.Title,
            Category = e.Category,
            ProjectId = e.ProjectId,
            ProjectName = e.ProjectId is null ? null : projectNames.GetValueOrDefault(e.ProjectId.Value),
            PropertyId = e.PropertyId,
            AddressOneLine = e.PropertyId is null
                ? null
                : properties.Where(p => p.Id == e.PropertyId).Select(RealEstateMapper.OneLineAddress).FirstOrDefault(),
            SocietyName = e.SocietyId is null ? null : societies.GetValueOrDefault(e.SocietyId.Value),
            DueDate = e.DueDate,
            DaysToDue = e.DueDate.DayNumber - Today.DayNumber,
            Recurrence = e.Recurrence,
            OwnerName = e.OwnerUserId is null ? null : owners.GetValueOrDefault(e.OwnerUserId.Value),
            Severity = e.Severity,
            AlertSent = e.AlertSent,
            IsCompleted = e.IsCompleted,
            CompletedOn = e.CompletedOn,
            EvidenceUrl = e.EvidenceUrl,
            IsOverdue = !e.IsCompleted && e.DueDate < Today,
            PenaltyIfMissed = e.PenaltyIfMissed,
            Route = RouteFor(e),
        }).ToList();
    }

    private static string? RouteFor(ComplianceCalendarEntry e) => e.Category switch
    {
        "Approval" when e.ApprovalRecordId is not null => $"/realestate/compliance/approvals/{e.ApprovalRecordId}",
        "Licence" when e.LicenceRecordId is not null => $"/realestate/compliance/licences/{e.LicenceRecordId}",
        "Filing" when e.RegulatoryFilingId is not null => $"/realestate/compliance/filings/{e.RegulatoryFilingId}",
        "Certificate" when e.ComplianceCertificateId is not null
            => $"/realestate/compliance/certificates/{e.ComplianceCertificateId}",
        "Guarantee" or "Loan" when e.ProjectId is not null => $"/realestate/finance/projects/{e.ProjectId}",
        _ => null,
    };

    public async Task<ComplianceCalendarEntryDto> CompleteCalendarEntryAsync(
        Guid id, string? evidenceUrl, Guid userId)
    {
        var entry = await RequireAsync<ComplianceCalendarEntry>(id, "That calendar entry does not exist.");

        if (entry.IsCompleted)
            throw new InvalidOperationException("That entry is already marked complete.");

        entry.IsCompleted = true;
        entry.CompletedOn = Today;
        entry.EvidenceUrl = evidenceUrl;
        entry.IsOverdue = false;
        entry.StampUpdated(userId);

        // A recurring obligation regenerates the moment it is closed, because the only reliable
        // time to schedule the next one is while somebody is looking at the last one.
        if (!string.IsNullOrWhiteSpace(entry.Recurrence))
        {
            var next = NextOccurrence(entry.DueDate, entry.Recurrence);

            if (next is not null)
            {
                Db.ComplianceCalendarEntries.Add(new ComplianceCalendarEntry
                {
                    Title = entry.Title,
                    Category = entry.Category,
                    ProjectId = entry.ProjectId,
                    PropertyId = entry.PropertyId,
                    OfficeId = entry.OfficeId,
                    SocietyId = entry.SocietyId,
                    ApprovalRecordId = entry.ApprovalRecordId,
                    LicenceRecordId = entry.LicenceRecordId,
                    ComplianceCertificateId = entry.ComplianceCertificateId,
                    RegulatoryFilingId = entry.RegulatoryFilingId,
                    DueDate = next.Value,
                    Recurrence = entry.Recurrence,
                    OwnerUserId = entry.OwnerUserId,
                    Severity = entry.Severity,
                    AlertDaysBefore = entry.AlertDaysBefore,
                    PenaltyIfMissed = entry.PenaltyIfMissed,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();

        var refreshed = await GetComplianceCalendarAsync(entry.DueDate, entry.DueDate, null);

        return refreshed.First(e => e.Id == entry.Id);
    }

    private static DateOnly? NextOccurrence(DateOnly from, string recurrence) => recurrence switch
    {
        "Monthly" => from.AddMonths(1),
        "Quarterly" => from.AddMonths(3),
        "HalfYearly" => from.AddMonths(6),
        "Yearly" or "Annual" => from.AddYears(1),
        "Weekly" => from.AddDays(7),
        _ => null,
    };

    /// <summary>
    /// Puts a dated obligation on the calendar, or moves the existing one when the date changes.
    /// Matched on the source record rather than on the title, so renaming an approval does not
    /// leave a duplicate deadline behind.
    /// </summary>
    private async Task UpsertCalendarEntryAsync(
        string title, string category, DateOnly dueDate, Guid userId,
        Guid? projectId = null, Guid? propertyId = null, Guid? officeId = null, Guid? societyId = null,
        Guid? approvalRecordId = null, Guid? licenceRecordId = null, Guid? regulatoryFilingId = null,
        Guid? ownerUserId = null, AlertSeverity severity = AlertSeverity.Warning,
        int alertDaysBefore = 30, string? recurrence = null, decimal? penalty = null)
    {
        var existing = await Db.ComplianceCalendarEntries.ForCompany(Tenant)
            .Where(e => !e.IsCompleted)
            .Where(e => (approvalRecordId != null && e.ApprovalRecordId == approvalRecordId)
                     || (licenceRecordId != null && e.LicenceRecordId == licenceRecordId)
                     || (regulatoryFilingId != null && e.RegulatoryFilingId == regulatoryFilingId)
                     || (approvalRecordId == null && licenceRecordId == null && regulatoryFilingId == null
                         && e.Title == title && e.Category == category))
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            existing.Title = title;
            existing.DueDate = dueDate;
            existing.Severity = severity;
            existing.AlertDaysBefore = alertDaysBefore;
            existing.PenaltyIfMissed = penalty;
            existing.AlertSent = false;
            existing.IsOverdue = dueDate < Today;
            existing.StampUpdated(userId);
            return;
        }

        Db.ComplianceCalendarEntries.Add(new ComplianceCalendarEntry
        {
            Title = title,
            Category = category,
            ProjectId = projectId,
            PropertyId = propertyId,
            OfficeId = officeId,
            SocietyId = societyId,
            ApprovalRecordId = approvalRecordId,
            LicenceRecordId = licenceRecordId,
            RegulatoryFilingId = regulatoryFilingId,
            DueDate = dueDate,
            Recurrence = recurrence,
            OwnerUserId = ownerUserId,
            Severity = severity,
            AlertDaysBefore = alertDaysBefore,
            PenaltyIfMissed = penalty,
            IsOverdue = dueDate < Today,
        }.StampNew(Tenant, userId));
    }

    // ═══ Regulatory filings ══════════════════════════════════════════════════

    public async Task<PaginatedResponse<RegulatoryFilingDto>> GetFilingsAsync(ListQueryDto query, string? status)
    {
        var q = Db.RegulatoryFilings.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(status), f => f.Status == status)
            .WhereIf(query.ProjectId.HasValue, f => f.ProjectId == query.ProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                f => f.Reference.Contains(query.Search!) || f.FilingType.Contains(query.Search!))
            .OrderBy(f => f.FiledOn != null)
            .ThenBy(f => f.DueOn);

        return await PageAsync(q, query, async rows =>
        {
            var names = await ProjectNamesAsync(rows.Select(r => r.ProjectId));
            var preparers = await AgentUserNamesAsync(rows.Select(r => r.PreparedByUserId));

            return rows.Select(f => new RegulatoryFilingDto
            {
                Id = f.Id,
                Reference = f.Reference,
                FilingType = f.FilingType,
                ProjectId = f.ProjectId,
                ProjectName = f.ProjectId is null ? null : names.GetValueOrDefault(f.ProjectId.Value),
                Authority = f.Authority,
                PeriodFrom = f.PeriodFrom,
                PeriodTo = f.PeriodTo,
                DueOn = f.DueOn,
                FiledOn = f.FiledOn,
                Status = f.Status,
                Version = f.Version,
                AcknowledgementNumber = f.AcknowledgementNumber,
                PreparedByName = f.PreparedByUserId is null ? null : preparers.GetValueOrDefault(f.PreparedByUserId.Value),
                DocumentUrl = f.DocumentUrl,
                QueryFromAuthority = f.QueryFromAuthority,
                LateFilingPenalty = f.LateFilingPenalty,
                IsOverdue = f.FiledOn is null && f.DueOn < Today,
                DaysToDue = f.DueOn.DayNumber - Today.DayNumber,
            }).ToList();
        });
    }

    // ═══ Quarterly progress report ═══════════════════════════════════════════

    /// <summary>
    /// Builds the quarterly return from the ledgers rather than from anybody's spreadsheet.
    ///
    /// The point of the return is that a regulator can compare what was sold against what was
    /// collected, what went into escrow against what came out, and what was built against what was
    /// promised. Every one of those numbers already exists in this system, so the report reads them
    /// straight through — a figure typed by hand is a figure nobody can reconcile.
    /// </summary>
    public async Task<QuarterlyProgressReportDto> GenerateQprAsync(Guid projectId, int year, int quarter, Guid userId)
    {
        if (quarter is < 1 or > 4)
            throw new InvalidOperationException("A quarter is 1 to 4.");

        var project = await RequireAsync<Project>(projectId, "That project does not exist.");

        var periodFrom = new DateOnly(year, (quarter - 1) * 3 + 1, 1);
        var periodTo = periodFrom.AddMonths(3).AddDays(-1);

        if (periodFrom > Today)
            throw new InvalidOperationException("That quarter has not started yet.");

        var existing = await Db.QuarterlyProgressReports.ForCompany(Tenant)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.Year == year && r.Quarter == quarter);

        if (existing is { IsFiled: true })
            throw new InvalidOperationException(
                $"The return for Q{quarter} {year} has already been filed under acknowledgement "
                + $"{existing.RegulatoryFilingId}. File a revision rather than regenerating it.");

        var report = existing ?? new QuarterlyProgressReport
        {
            Reference = await numbering.NextMasterCodeAsync(Db.QuarterlyProgressReports, "QPR"),
            ProjectId = projectId,
            Year = year,
            Quarter = quarter,
        }.StampNew(Tenant, userId);

        var live = new[]
        {
            BookingStatus.Confirmed, BookingStatus.AgreementSigned, BookingStatus.Defaulting,
            BookingStatus.PossessionOffered, BookingStatus.Possessed, BookingStatus.Completed,
        };

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId && live.Contains(b.Status) && b.BookingDate <= periodTo)
            .Select(b => new { b.Id, b.BookingDate, b.AreaSqFt, b.TotalConsideration, b.NetSalePrice, b.TotalPaid })
            .ToListAsync();

        var receipts = await Db.Receipts.ForCompany(Tenant)
            .Where(r => r.ProjectId == projectId && r.Status == ReceiptStatus.Posted && r.ReceivedOn <= periodTo)
            .Select(r => new { r.ReceivedOn, r.Amount, r.EscrowAmount })
            .ToListAsync();

        var escrowAccount = await Db.ProjectBankAccounts.ForCompany(Tenant)
            .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.Kind == ProjectAccountKind.Escrow);

        var withdrawn = escrowAccount is null
            ? 0m
            : await Db.EscrowLedgerEntries.ForCompany(Tenant)
                .Where(e => e.ProjectBankAccountId == escrowAccount.Id
                         && e.Kind == EscrowMovementKind.Withdrawal
                         && e.EntryDate <= periodTo)
                .SumAsync(e => (decimal?)e.DebitAmount) ?? 0m;

        var budget = await Db.ProjectBudgetLines.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId)
            .Select(b => new { b.CostHead, b.ActualAmount })
            .ToListAsync();

        var totalUnits = await Db.Units.ForCompany(Tenant)
            .CountAsync(u => u.ProjectId == projectId && u.IsSaleable);

        var approvals = await Db.ApprovalRecords.ForCompany(Tenant)
            .Where(a => a.ProjectId == projectId && a.IsMandatory)
            .Select(a => a.State)
            .ToListAsync();

        var progress = (await ProjectProgressAsync([projectId])).GetValueOrDefault(projectId);

        var collectedToDate = RealEstateMapper.Money(receipts.Sum(r => r.Amount));
        var costIncurred = RealEstateMapper.Money(budget.Sum(b => b.ActualAmount));

        report.PeriodFrom = periodFrom;
        report.PeriodTo = periodTo;
        report.TotalUnits = totalUnits;
        report.UnitsBooked = bookings.Count;
        report.UnitsBookedThisQuarter = bookings.Count(b => b.BookingDate >= periodFrom && b.BookingDate <= periodTo);
        report.AreaBookedSqFt = RealEstateMapper.Money(bookings.Sum(b => b.AreaSqFt));
        report.TotalBookingValue = RealEstateMapper.Money(
            bookings.Sum(b => b.TotalConsideration > 0m ? b.TotalConsideration : b.NetSalePrice));
        report.AmountCollected = collectedToDate;
        report.AmountCollectedThisQuarter = RealEstateMapper.Money(
            receipts.Where(r => r.ReceivedOn >= periodFrom).Sum(r => r.Amount));
        report.AmountDepositedToEscrow = RealEstateMapper.Money(receipts.Sum(r => r.EscrowAmount));
        report.AmountWithdrawnFromEscrow = RealEstateMapper.Money(withdrawn);
        report.EscrowBalance = escrowAccount?.Balance ?? 0m;
        report.AmountSpentOnConstruction = RealEstateMapper.Money(
            budget.Where(b => BucketFor(b.CostHead) is 1 or 2).Sum(b => b.ActualAmount));
        report.AmountSpentOnLand = RealEstateMapper.Money(
            budget.Where(b => BucketFor(b.CostHead) == 0).Sum(b => b.ActualAmount));
        report.PhysicalProgressPercent = progress;

        // Financial progress is cost spent against the whole budget, which is deliberately not the
        // same as physical progress. Where the two diverge sharply, that divergence is the story.
        report.FinancialProgressPercent = RealEstateMapper.Percent(costIncurred, project.TotalBudget);

        report.OriginalCompletionDate = project.PlannedCompletionDate;
        report.RevisedCompletionDate = project.ForecastPossessionDate ?? project.PlannedCompletionDate;
        report.ApprovalsObtained = approvals.Count(s => s is ApprovalState.Granted or ApprovalState.GrantedWithConditions);
        report.ApprovalsPending = approvals.Count - report.ApprovalsObtained;

        if (report.RevisedCompletionDate > report.OriginalCompletionDate && string.IsNullOrWhiteSpace(report.DelayReason))
        {
            var slip = report.RevisedCompletionDate!.Value.DayNumber - report.OriginalCompletionDate!.Value.DayNumber;
            report.DelayReason = $"Forecast completion has slipped by {slip} days against the original programme.";
        }

        if (existing is null) Db.QuarterlyProgressReports.Add(report);
        else report.StampUpdated(userId);

        await RebuildQprLinesAsync(report, projectId, periodTo, userId);

        await Db.SaveChangesAsync();

        return (await GetQprAsync(report.Id))!;
    }

    private async Task RebuildQprLinesAsync(
        QuarterlyProgressReport report, Guid projectId, DateOnly periodTo, Guid userId)
    {
        foreach (var line in report.Lines.ToList()) line.StampDeleted(userId);

        var nodes = await Db.ProjectNodes.ForCompany(Tenant)
            .Where(n => n.ProjectId == projectId
                     && (n.Kind == ProjectNodeKind.Block || n.Kind == ProjectNodeKind.Tower))
            .OrderBy(n => n.SortOrder)
            .ToListAsync();

        if (nodes.Count == 0) return;

        var nodeIds = nodes.Select(n => n.Id).ToList();

        var units = await Db.Units.ForCompany(Tenant)
            .Where(u => u.ProjectId == projectId && u.ProjectNodeId != null && nodeIds.Contains(u.ProjectNodeId!.Value))
            .Select(u => new { u.ProjectNodeId, u.Status })
            .ToListAsync();

        var order = 0;

        foreach (var node in nodes)
        {
            var mine = units.Where(u => u.ProjectNodeId == node.Id).ToList();

            report.Lines.Add(new QprLine
            {
                QuarterlyProgressReportId = report.Id,
                ProjectNodeId = node.Id,
                BuildingName = node.Name,
                UnitCount = mine.Count,
                BookedCount = mine.Count(u => u.Status is PropertyStatus.Booked or PropertyStatus.Sold
                                                       or PropertyStatus.Registered or PropertyStatus.Possessed),
                ProgressPercent = node.ProgressPercent,
                CurrentStage = node.Status.ToString(),
                ExpectedCompletion = node.PlannedCompletionDate,
                SortOrder = order++,
            }.StampNew(Tenant, userId));
        }
    }

    public async Task<QuarterlyProgressReportDto?> GetQprAsync(Guid id)
    {
        var report = await Db.QuarterlyProgressReports.ForCompany(Tenant)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report is null) return null;

        var unit = await AreaUnitAsync();
        var currency = await CurrencyAsync();
        var names = await ProjectNamesAsync([report.ProjectId]);
        var engineers = await AgentUserNamesAsync([report.CertifiedByEngineerUserId]);

        return new QuarterlyProgressReportDto
        {
            Id = report.Id,
            Reference = report.Reference,
            ProjectId = report.ProjectId,
            ProjectName = names.GetValueOrDefault(report.ProjectId, "—"),
            Year = report.Year,
            Quarter = report.Quarter,
            PeriodFrom = report.PeriodFrom,
            PeriodTo = report.PeriodTo,
            TotalUnits = report.TotalUnits,
            UnitsBooked = report.UnitsBooked,
            UnitsBookedThisQuarter = report.UnitsBookedThisQuarter,
            AreaBooked = RealEstateMapper.Area(report.AreaBookedSqFt, unit),
            TotalBookingValue = report.TotalBookingValue,
            AmountCollected = report.AmountCollected,
            AmountCollectedThisQuarter = report.AmountCollectedThisQuarter,
            AmountDepositedToEscrow = report.AmountDepositedToEscrow,
            AmountWithdrawnFromEscrow = report.AmountWithdrawnFromEscrow,
            EscrowBalance = report.EscrowBalance,
            AmountSpentOnConstruction = report.AmountSpentOnConstruction,
            AmountSpentOnLand = report.AmountSpentOnLand,
            CurrencyCode = currency,
            PhysicalProgressPercent = report.PhysicalProgressPercent,
            FinancialProgressPercent = report.FinancialProgressPercent,
            OriginalCompletionDate = report.OriginalCompletionDate,
            RevisedCompletionDate = report.RevisedCompletionDate,
            DelayReason = report.DelayReason,
            ApprovalsObtained = report.ApprovalsObtained,
            ApprovalsPending = report.ApprovalsPending,
            CertifiedByEngineerName = report.CertifiedByEngineerUserId is null
                ? null
                : engineers.GetValueOrDefault(report.CertifiedByEngineerUserId.Value),
            ArchitectCertificateUrl = report.ArchitectCertificateUrl,
            CaCertificateUrl = report.CaCertificateUrl,
            IsFiled = report.IsFiled,
            DocumentUrl = report.DocumentUrl,
            Buildings = report.Lines.OrderBy(l => l.SortOrder).Select(l => new QprLineDto
            {
                Id = l.Id,
                ProjectNodeId = l.ProjectNodeId,
                BuildingName = l.BuildingName,
                UnitCount = l.UnitCount,
                BookedCount = l.BookedCount,
                ProgressPercent = l.ProgressPercent,
                CurrentStage = l.CurrentStage,
                ExpectedCompletion = l.ExpectedCompletion,
                SortOrder = l.SortOrder,
            }).ToList(),
        };
    }

    public async Task<QuarterlyProgressReportDto> FileQprAsync(Guid id, string acknowledgementNumber, Guid userId)
    {
        var report = await RequireAsync<QuarterlyProgressReport>(id, "That report does not exist.");

        if (report.IsFiled)
            throw new InvalidOperationException("That report has already been filed.");

        if (string.IsNullOrWhiteSpace(acknowledgementNumber))
            throw new InvalidOperationException("The authority's acknowledgement number is required to close a filing.");

        // The certificates are what make the return a return rather than a claim. Filing without
        // them is the failure mode this whole record exists to prevent.
        if (string.IsNullOrWhiteSpace(report.ArchitectCertificateUrl))
            throw new InvalidOperationException(
                "The architect's certificate of physical progress is missing. Attach it before filing.");

        if (string.IsNullOrWhiteSpace(report.CaCertificateUrl))
            throw new InvalidOperationException(
                "The accountant's certificate on collections and escrow is missing. Attach it before filing.");

        var filing = new RegulatoryFiling
        {
            Reference = await numbering.NextMasterCodeAsync(Db.RegulatoryFilings, "RGF"),
            FilingType = "QuarterlyProgressReport",
            ProjectId = report.ProjectId,
            PeriodFrom = report.PeriodFrom,
            PeriodTo = report.PeriodTo,
            DueOn = report.PeriodTo.AddDays(30),
            FiledOn = Today,
            Status = "Filed",
            AcknowledgementNumber = acknowledgementNumber.Trim(),
            PreparedByUserId = userId,
            DocumentUrl = report.DocumentUrl,
        }.StampNew(Tenant, userId);

        Db.RegulatoryFilings.Add(filing);

        report.RegulatoryFilingId = filing.Id;
        report.IsFiled = true;
        report.StampUpdated(userId);

        // The next quarter goes straight on the calendar, because the only reliable moment to
        // schedule it is the moment the last one closed.
        var nextPeriodEnd = report.PeriodTo.AddMonths(3);

        await UpsertCalendarEntryAsync(
            $"Quarterly return Q{(report.Quarter % 4) + 1} {(report.Quarter == 4 ? report.Year + 1 : report.Year)}",
            "Filing", nextPeriodEnd.AddDays(30), userId,
            projectId: report.ProjectId,
            severity: AlertSeverity.Critical,
            alertDaysBefore: 21,
            recurrence: "Quarterly");

        await Db.SaveChangesAsync();

        return (await GetQprAsync(report.Id))!;
    }

    // ═══ Document templates and generation ═══════════════════════════════════

    public async Task<List<DocumentTemplateDto>> GetTemplatesAsync(string? documentType, Guid? projectId)
    {
        var templates = await Db.DocumentTemplates.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(documentType), t => t.DocumentType == documentType)
            .WhereIf(projectId.HasValue, t => t.ProjectId == projectId || t.ProjectId == null)
            .Include(t => t.Versions)
            .OrderBy(t => t.DocumentType)
            .ThenByDescending(t => t.IsDefault)
            .ToListAsync();

        if (templates.Count == 0) return [];

        var ids = templates.Select(t => t.Id).ToList();
        var names = await ProjectNamesAsync(templates.Select(t => t.ProjectId));

        var usage = await Db.GeneratedDocuments.ForCompany(Tenant)
            .Where(d => d.DocumentTemplateId != null && ids.Contains(d.DocumentTemplateId.Value))
            .GroupBy(d => d.DocumentTemplateId!.Value)
            .Select(g => new { TemplateId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TemplateId, x => x.Count);

        var authors = await AgentUserNamesAsync(templates.SelectMany(t => t.Versions).Select(v => v.CreatedByUserIdRef));

        return templates.Select(t => new DocumentTemplateDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            DocumentType = t.DocumentType,
            ProjectId = t.ProjectId,
            ProjectName = t.ProjectId is null ? null : names.GetValueOrDefault(t.ProjectId.Value),
            LanguageCode = t.LanguageCode,
            IsRightToLeft = t.IsRightToLeft,
            CurrentVersion = t.CurrentVersion,
            IsActive = t.IsActive,
            IsDefault = t.IsDefault,
            LetterheadUrl = t.LetterheadUrl,
            IncludeQrVerification = t.IncludeQrVerification,
            IncludeAmountInWords = t.IncludeAmountInWords,
            PaperSize = t.PaperSize,
            UsageCount = usage.GetValueOrDefault(t.Id),
            Versions = t.Versions.OrderByDescending(v => v.Version).Select(v => new TemplateVersionDto
            {
                Id = v.Id,
                Version = v.Version,
                Body = v.Body,
                HeaderHtml = v.HeaderHtml,
                FooterHtml = v.FooterHtml,
                StyleCss = v.StyleCss,
                MergeFields = MergeFieldsIn(v.Body),
                EffectiveFrom = v.EffectiveFrom,
                EffectiveTo = v.EffectiveTo,
                IsPublished = v.IsPublished,
                ChangeNote = v.ChangeNote,
                CreatedByName = v.CreatedByUserIdRef is null
                    ? null
                    : authors.GetValueOrDefault(v.CreatedByUserIdRef.Value),
            }).ToList(),
        }).ToList();
    }

    private static List<string> MergeFieldsIn(string body)
        => Regex.Matches(body, @"\{\{\s*([A-Za-z0-9_.]+)\s*\}\}")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f)
            .ToList();

    public async Task<DocumentTemplateDto> SaveTemplateAsync(DocumentTemplateDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var template = isNew
            ? new DocumentTemplate { Code = await numbering.NextMasterCodeAsync(Db.DocumentTemplates, "TPL") }
            : await Db.DocumentTemplates.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == dto.Id)
              ?? throw new InvalidOperationException("That template does not exist.");

        if (dto.IsDefault)
        {
            var others = await Db.DocumentTemplates.ForCompany(Tenant)
                .Where(t => t.DocumentType == dto.DocumentType
                         && t.ProjectId == dto.ProjectId
                         && t.LanguageCode == dto.LanguageCode
                         && t.Id != dto.Id
                         && t.IsDefault)
                .ToListAsync();

            // One default per document type, project and language — otherwise generation picks
            // arbitrarily and two customers get different contracts on the same day.
            foreach (var other in others)
            {
                other.IsDefault = false;
                other.StampUpdated(userId);
            }
        }

        template.Name = dto.Name;
        template.DocumentType = dto.DocumentType;
        template.ProjectId = dto.ProjectId;
        template.LanguageCode = dto.LanguageCode;
        template.IsRightToLeft = dto.IsRightToLeft;
        template.IsDefault = dto.IsDefault;
        template.LetterheadUrl = dto.LetterheadUrl;
        template.IncludeQrVerification = dto.IncludeQrVerification;
        template.IncludeAmountInWords = dto.IncludeAmountInWords;
        template.PaperSize = dto.PaperSize;
        template.IsActive = dto.IsActive;

        if (isNew)
        {
            template.StampNew(Tenant, userId);
            Db.DocumentTemplates.Add(template);
        }
        else
        {
            template.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetTemplatesAsync(template.DocumentType, template.ProjectId)).First(t => t.Id == template.Id);
    }

    public async Task<TemplateVersionDto> PublishTemplateVersionAsync(
        Guid templateId, TemplateVersionDto dto, Guid userId)
    {
        var template = await Db.DocumentTemplates.ForCompany(Tenant)
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == templateId)
            ?? throw new InvalidOperationException("That template does not exist.");

        if (string.IsNullOrWhiteSpace(dto.Body))
            throw new InvalidOperationException("A template version needs a body.");

        var nextVersion = template.Versions.Count == 0 ? 1 : template.Versions.Max(v => v.Version) + 1;

        // The previous published version is closed the day before this one starts, so that any
        // document ever generated can be traced to exactly one version of exactly one template.
        var effectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;

        foreach (var previous in template.Versions.Where(v => v.IsPublished && v.EffectiveTo == null))
        {
            previous.EffectiveTo = effectiveFrom.AddDays(-1);
            previous.StampUpdated(userId);
        }

        var version = new TemplateVersion
        {
            DocumentTemplateId = templateId,
            Version = nextVersion,
            Body = dto.Body,
            HeaderHtml = dto.HeaderHtml,
            FooterHtml = dto.FooterHtml,
            StyleCss = dto.StyleCss,
            MergeFieldsJson = System.Text.Json.JsonSerializer.Serialize(MergeFieldsIn(dto.Body)),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedByUserIdRef = userId,
            IsPublished = true,
            ChangeNote = dto.ChangeNote,
        }.StampNew(Tenant, userId);

        Db.TemplateVersions.Add(version);

        template.CurrentVersion = nextVersion;
        template.StampUpdated(userId);

        await Db.SaveChangesAsync();

        var authors = await AgentUserNamesAsync([userId]);

        return new TemplateVersionDto
        {
            Id = version.Id,
            Version = version.Version,
            Body = version.Body,
            HeaderHtml = version.HeaderHtml,
            FooterHtml = version.FooterHtml,
            StyleCss = version.StyleCss,
            MergeFields = MergeFieldsIn(version.Body),
            EffectiveFrom = version.EffectiveFrom,
            EffectiveTo = version.EffectiveTo,
            IsPublished = version.IsPublished,
            ChangeNote = version.ChangeNote,
            CreatedByName = authors.GetValueOrDefault(userId),
        };
    }

    public async Task<List<ClauseLibraryItemDto>> GetClausesAsync(string? category, Guid? projectId)
    {
        var clauses = await Db.ClauseLibraryItems.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(category), c => c.Category == category)
            .WhereIf(projectId.HasValue, c => c.ProjectId == projectId || c.ProjectId == null)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.SortOrder)
            .ToListAsync();

        return clauses.Select(c => new ClauseLibraryItemDto
        {
            Id = c.Id,
            ClauseKey = c.ClauseKey,
            Heading = c.Heading,
            Body = c.Body,
            Category = c.Category,
            LanguageCode = c.LanguageCode,
            Version = c.Version,
            IsMandatory = c.IsMandatory,
            IsNegotiable = c.IsNegotiable,
            ProjectId = c.ProjectId,
            IsActive = c.IsActive,
            SortOrder = c.SortOrder,
        }).ToList();
    }

    public async Task<ClauseLibraryItemDto> SaveClauseAsync(ClauseLibraryItemDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var clause = isNew
            ? new ClauseLibraryItem()
            : await Db.ClauseLibraryItems.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
              ?? throw new InvalidOperationException("That clause does not exist.");

        // Editing the wording of a clause already sitting inside signed agreements would silently
        // change what those agreements appear to say. A new version is created instead.
        if (!isNew && clause.Body != dto.Body)
        {
            clause.IsActive = false;
            clause.StampUpdated(userId);

            var replacement = new ClauseLibraryItem
            {
                ClauseKey = dto.ClauseKey,
                Heading = dto.Heading,
                Body = dto.Body,
                Category = dto.Category,
                LanguageCode = dto.LanguageCode,
                Version = clause.Version + 1,
                IsMandatory = dto.IsMandatory,
                IsNegotiable = dto.IsNegotiable,
                ProjectId = dto.ProjectId,
                SortOrder = dto.SortOrder,
            }.StampNew(Tenant, userId);

            Db.ClauseLibraryItems.Add(replacement);
            await Db.SaveChangesAsync();

            return (await GetClausesAsync(replacement.Category, replacement.ProjectId))
                .First(c => c.Id == replacement.Id);
        }

        clause.ClauseKey = dto.ClauseKey;
        clause.Heading = dto.Heading;
        clause.Body = dto.Body;
        clause.Category = dto.Category;
        clause.LanguageCode = dto.LanguageCode;
        clause.IsMandatory = dto.IsMandatory;
        clause.IsNegotiable = dto.IsNegotiable;
        clause.ProjectId = dto.ProjectId;
        clause.SortOrder = dto.SortOrder;
        clause.IsActive = dto.IsActive;

        if (isNew)
        {
            clause.Version = 1;
            clause.StampNew(Tenant, userId);
            Db.ClauseLibraryItems.Add(clause);
        }
        else
        {
            clause.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetClausesAsync(clause.Category, clause.ProjectId)).First(c => c.Id == clause.Id);
    }

    /// <summary>
    /// Renders a document from a template and the entity it is about.
    ///
    /// The merge fields are resolved from the record rather than typed, and the rendered text is
    /// hashed. That hash, together with the short verification code, is what lets somebody holding
    /// a printed copy years later establish that it is the copy this office issued — which is the
    /// only claim a verification feature can honestly make.
    /// </summary>
    public async Task<GeneratedDocumentDto> GenerateDocumentAsync(DocumentGenerationRequestDto dto, Guid userId)
    {
        if (dto.EntityId == Guid.Empty)
            throw new InvalidOperationException("A document has to be about something. The entity is required.");

        var template = dto.DocumentTemplateId is not null
            ? await Db.DocumentTemplates.ForCompany(Tenant)
                .Include(t => t.Versions)
                .FirstOrDefaultAsync(t => t.Id == dto.DocumentTemplateId)
            : await Db.DocumentTemplates.ForCompany(Tenant)
                .Include(t => t.Versions)
                .Where(t => t.DocumentType == dto.DocumentType && t.IsActive && t.LanguageCode == dto.LanguageCode)
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefaultAsync();

        if (template is null)
            throw new InvalidOperationException(
                $"There is no active {dto.DocumentType} template in {dto.LanguageCode}. Create one first.");

        var version = template.Versions
            .Where(v => v.IsPublished && v.EffectiveFrom <= Today && (v.EffectiveTo == null || v.EffectiveTo >= Today))
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();

        if (version is null)
            throw new InvalidOperationException(
                $"Template “{template.Name}” has no version in force today. Publish one before generating.");

        var merge = await BuildMergeFieldsAsync(dto.DocumentType, dto.EntityId, template);

        foreach (var (key, value) in dto.Overrides) merge[key] = value;

        var required = MergeFieldsIn(version.Body);
        var missing = required.Where(f => !merge.ContainsKey(f)).ToList();

        // A contract that goes out with {{BuyerName}} still printed on it is a document nobody can
        // use and everybody remembers. It is refused, with the list of what could not be resolved.
        if (missing.Count > 0)
            throw new InvalidOperationException(
                "These merge fields could not be resolved from the record: " + string.Join(", ", missing)
                + ". Complete the record, or supply them as overrides.");

        var body = Regex.Replace(version.Body, @"\{\{\s*([A-Za-z0-9_.]+)\s*\}\}",
            m => merge.GetValueOrDefault(m.Groups[1].Value, string.Empty));

        var verificationCode = NewVerificationCode();

        var document = new GeneratedDocument
        {
            DocumentNumber = await numbering.NextDocumentNumberAsync(DateTime.UtcNow),
            DocumentTemplateId = template.Id,
            TemplateVersionId = version.Id,
            DocumentType = dto.DocumentType,
            LanguageCode = dto.LanguageCode,
            GeneratedAt = DateTime.UtcNow,
            GeneratedByUserId = userId,
            Title = $"{template.Name} — {merge.GetValueOrDefault("Reference", dto.EntityId.ToString()[..8])}",
            ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))),
            VerificationCode = template.IncludeQrVerification ? verificationCode : null,
            PageCount = Math.Max(1, body.Length / 3000),
            FileSizeBytes = Encoding.UTF8.GetByteCount(body),
            Description = body,
        };

        AttachDocumentToEntity(document, dto.DocumentType, dto.EntityId);

        document.StampNew(Tenant, userId);
        Db.GeneratedDocuments.Add(document);

        if (dto.SendImmediately && dto.Channels.Count > 0)
        {
            document.IsSent = true;
            document.SentAt = DateTime.UtcNow;
            document.SentVia = dto.Channels[0];

            await QueueNotificationAsync(
                "document.issued",
                document.Title!,
                $"{template.Name} has been issued. Reference {document.DocumentNumber}.",
                $"/realestate/documents/{document.Id}",
                recipientPartyId: document.PartyId,
                entityType: nameof(GeneratedDocument),
                entityId: document.Id);
        }

        await Db.SaveChangesAsync();

        return (await MapDocumentsAsync([document]))[0];
    }

    private static void AttachDocumentToEntity(GeneratedDocument document, string documentType, Guid entityId)
    {
        var type = documentType.ToLowerInvariant();

        if (type.Contains("tenancy") || type.Contains("lease")) document.TenancyId = entityId;
        else if (type.Contains("build") || type.Contains("works")) document.ClientBuildContractId = entityId;
        else if (type.Contains("deal") || type.Contains("memorandum")) document.DealId = entityId;
        else document.BookingId = entityId;
    }

    /// <summary>
    /// A short code a person can read aloud over a telephone. Ambiguous characters are left out —
    /// nobody should have to work out whether that was a zero or the letter O.
    /// </summary>
    private static string NewVerificationCode()
    {
        const string alphabet = "ACDEFGHJKLMNPQRTUVWXY3456789";
        var bytes = RandomNumberGenerator.GetBytes(10);
        var chars = bytes.Select(b => alphabet[b % alphabet.Length]).ToArray();

        return new string(chars[..5]) + "-" + new string(chars[5..]);
    }

    private async Task<Dictionary<string, string>> BuildMergeFieldsAsync(
        string documentType, Guid entityId, DocumentTemplate template)
    {
        var currency = await CurrencyAsync();
        var indian = RealEstateMapper.UsesIndianScale(currency);

        var merge = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Today"] = Today.ToString("dd MMMM yyyy"),
            ["CurrencyCode"] = currency,
            ["TemplateName"] = template.Name,
        };

        var type = documentType.ToLowerInvariant();

        if (type.Contains("tenancy") || type.Contains("lease"))
        {
            var tenancy = await Db.Tenancies.ForCompany(Tenant)
                .Include(t => t.Property)
                .FirstOrDefaultAsync(t => t.Id == entityId);

            if (tenancy is null) return merge;

            var names = await PartyNamesAsync(tenancy.LandlordId is null ? [] : [tenancy.LandlordId.Value]);

            merge["Reference"] = tenancy.Reference;
            merge["Rent"] = tenancy.Rent.ToString("N2");
            merge["RentInWords"] = RealEstateMapper.AmountInWords(tenancy.Rent, currency, indian);
            merge["Deposit"] = tenancy.DepositAmount.ToString("N2");
            merge["DepositInWords"] = RealEstateMapper.AmountInWords(tenancy.DepositAmount, currency, indian);
            merge["StartDate"] = tenancy.StartDate.ToString("dd MMMM yyyy");
            merge["EndDate"] = tenancy.EndDate?.ToString("dd MMMM yyyy") ?? "—";
            merge["TermMonths"] = tenancy.TermMonths?.ToString() ?? "—";
            merge["PropertyAddress"] = tenancy.Property is null ? "—" : RealEstateMapper.OneLineAddress(tenancy.Property);
            merge["LandlordName"] = tenancy.LandlordId is null ? "—" : names.Values.FirstOrDefault() ?? "—";

            return merge;
        }

        if (type.Contains("build") || type.Contains("works"))
        {
            var contract = await Db.ClientBuildContracts.ForCompany(Tenant)
                .FirstOrDefaultAsync(c => c.Id == entityId);

            if (contract is null) return merge;

            var names = await PartyNamesAsync([contract.ClientPartyId]);
            var value = contract.RevisedContractValue > 0m ? contract.RevisedContractValue : contract.ContractValue;

            merge["Reference"] = contract.Reference;
            merge["ClientName"] = names.GetValueOrDefault(contract.ClientPartyId, "—");
            merge["ContractValue"] = value.ToString("N2");
            merge["ContractValueInWords"] = RealEstateMapper.AmountInWords(value, currency, indian);
            merge["CoveredArea"] = contract.CoveredAreaSqFt.ToString("N0");
            merge["RatePerSqFt"] = contract.RatePerSqFt.ToString("N2");
            merge["SiteAddress"] = contract.SiteAddress ?? "—";
            merge["StartDate"] = contract.StartDate?.ToString("dd MMMM yyyy") ?? "—";
            merge["CompletionDate"] = contract.PlannedCompletionDate?.ToString("dd MMMM yyyy") ?? "—";
            merge["DefectsPeriodMonths"] = contract.DefectsPeriodMonths.ToString();

            return merge;
        }

        if (type.Contains("deal") || type.Contains("memorandum"))
        {
            var deal = await Db.Deals.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == entityId);
            if (deal is null) return merge;

            var parties = new List<Guid> { deal.BuyerPartyId };
            if (deal.SellerPartyId is not null) parties.Add(deal.SellerPartyId.Value);

            var names = await PartyNamesAsync(parties);

            var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == deal.PropertyId);

            merge["Reference"] = deal.Reference;
            merge["BuyerName"] = names.GetValueOrDefault(deal.BuyerPartyId, "—");
            merge["SellerName"] = deal.SellerPartyId is null ? "—" : names.GetValueOrDefault(deal.SellerPartyId.Value, "—");
            merge["AgreedPrice"] = deal.AgreedPrice.ToString("N2");
            merge["AgreedPriceInWords"] = RealEstateMapper.AmountInWords(deal.AgreedPrice, currency, indian);
            merge["Deposit"] = deal.DepositAmount.ToString("N2");
            merge["AgreedOn"] = deal.AgreedOn.ToString("dd MMMM yyyy");
            merge["PropertyAddress"] = property is null ? "—" : RealEstateMapper.OneLineAddress(property);

            return merge;
        }

        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == entityId);
        if (booking is null) return merge;

        var applicant = await PartyNamesAsync([booking.PrimaryApplicantPartyId]);
        var projects = await ProjectNamesAsync([booking.ProjectId]);
        var labels = booking.UnitId is null ? [] : await UnitLabelsAsync([booking.UnitId.Value]);

        merge["Reference"] = booking.Reference;
        merge["BuyerName"] = applicant.GetValueOrDefault(booking.PrimaryApplicantPartyId, "—");
        merge["ProjectName"] = projects.GetValueOrDefault(booking.ProjectId, "—");
        merge["UnitNumber"] = booking.UnitId is null ? "—" : labels.GetValueOrDefault(booking.UnitId.Value)?.UnitNumber ?? "—";
        merge["BlockName"] = booking.UnitId is null ? "—" : labels.GetValueOrDefault(booking.UnitId.Value)?.BlockName ?? "—";
        merge["BookingDate"] = booking.BookingDate.ToString("dd MMMM yyyy");
        merge["ListPrice"] = booking.ListPrice.ToString("N2");
        merge["Discount"] = booking.DiscountAmount.ToString("N2");
        merge["NetSalePrice"] = booking.NetSalePrice.ToString("N2");
        merge["TotalConsideration"] = booking.TotalConsideration.ToString("N2");
        merge["TotalConsiderationInWords"] = RealEstateMapper.AmountInWords(booking.TotalConsideration, currency, indian);
        merge["Area"] = booking.AreaSqFt.ToString("N0");
        merge["RatePerSqFt"] = booking.RatePerSqFt.ToString("N2");
        merge["AmountPaid"] = booking.TotalPaid.ToString("N2");
        merge["Outstanding"] = booking.Outstanding.ToString("N2");

        return merge;
    }

    public async Task<PaginatedResponse<GeneratedDocumentDto>> GetDocumentsAsync(
        ListQueryDto query, string? documentType, Guid? entityId)
    {
        var q = Db.GeneratedDocuments.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(documentType), d => d.DocumentType == documentType)
            .WhereIf(entityId.HasValue,
                d => d.BookingId == entityId || d.TenancyId == entityId
                  || d.DealId == entityId || d.ClientBuildContractId == entityId
                  || d.PartyId == entityId || d.PropertyId == entityId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                d => d.DocumentNumber.Contains(query.Search!) || (d.Title ?? "").Contains(query.Search!))
            .OrderByDescending(d => d.GeneratedAt);

        return await PageAsync(q, query, MapDocumentsAsync);
    }

    private async Task<List<GeneratedDocumentDto>> MapDocumentsAsync(List<GeneratedDocument> documents)
    {
        if (documents.Count == 0) return [];

        var authors = await AgentUserNamesAsync(documents.Select(d => d.GeneratedByUserId));

        return documents.Select(d => new GeneratedDocumentDto
        {
            Id = d.Id,
            DocumentNumber = d.DocumentNumber,
            DocumentType = d.DocumentType,
            Title = d.Title,
            Url = d.Url,
            GeneratedAt = d.GeneratedAt,
            GeneratedByName = d.GeneratedByUserId is null ? null : authors.GetValueOrDefault(d.GeneratedByUserId.Value),
            LanguageCode = d.LanguageCode,
            VerificationCode = d.VerificationCode,
            IsSent = d.IsSent,
            IsSigned = d.IsSigned,
            IsSuperseded = d.IsSuperseded,
            PageCount = d.PageCount,
        }).ToList();
    }

    public async Task<GeneratedDocumentDto?> VerifyDocumentAsync(string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(verificationCode)) return null;

        var normalised = verificationCode.Trim().ToUpperInvariant();

        var document = await Db.GeneratedDocuments.ForCompany(Tenant)
            .FirstOrDefaultAsync(d => d.VerificationCode == normalised);

        if (document is null) return null;

        return (await MapDocumentsAsync([document]))[0];
    }

    // ═══ Signatures ══════════════════════════════════════════════════════════

    public async Task<SignatureSessionDto> StartSigningAsync(
        Guid documentId, List<SignaturePartyDto> parties, SignatureMethod method, bool sequential, Guid userId)
    {
        var document = await RequireAsync<GeneratedDocument>(documentId, "That document does not exist.");

        if (document.IsSigned)
            throw new InvalidOperationException("That document has already been signed.");

        if (document.IsSuperseded)
            throw new InvalidOperationException(
                "That document has been superseded. Start signing on the version that replaced it.");

        if (parties.Count == 0)
            throw new InvalidOperationException("A signing session needs at least one signer.");

        // Electronic signing needs somewhere to send the request. A thumb impression does not, which
        // is exactly why both methods exist.
        if (method == SignatureMethod.Electronic)
        {
            var unreachable = parties
                .Where(p => string.IsNullOrWhiteSpace(p.Email) && string.IsNullOrWhiteSpace(p.Phone))
                .Select(p => p.Name)
                .ToList();

            if (unreachable.Count > 0)
                throw new InvalidOperationException(
                    "These signers have neither an email address nor a phone number, so an electronic request "
                    + "cannot reach them: " + string.Join(", ", unreachable) + ".");
        }

        var existing = await Db.SignatureSessions.ForCompany(Tenant)
            .FirstOrDefaultAsync(s => s.GeneratedDocumentId == documentId
                                   && s.State != SignatureState.Cancelled
                                   && s.State != SignatureState.Declined
                                   && s.State != SignatureState.Expired);

        if (existing is not null)
            throw new InvalidOperationException(
                $"Signing session {existing.Reference} is already open on this document. Cancel it before starting another.");

        var settings = await SettingsAsync();

        var session = new SignatureSession
        {
            Reference = await numbering.NextMasterCodeAsync(Db.SignatureSessions, "SIG"),
            GeneratedDocumentId = documentId,
            DocumentType = document.DocumentType,
            EntityId = document.BookingId ?? document.TenancyId ?? document.DealId ?? document.ClientBuildContractId,
            Method = method,
            State = SignatureState.Sent,
            InitiatedAt = DateTime.UtcNow,
            InitiatedByUserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(Math.Max(7, settings.DepositRegistrationDays)),
            IsSequential = sequential,
        }.StampNew(Tenant, userId);

        Db.SignatureSessions.Add(session);

        var order = 0;

        foreach (var party in parties.OrderBy(p => p.SigningOrder))
        {
            var isFirst = order == 0;

            Db.SignatureParties.Add(new SignatureParty
            {
                SignatureSessionId = session.Id,
                PartyId = party.PartyId,
                Name = party.Name,
                Email = party.Email,
                Phone = party.Phone,
                Role = party.Role,
                SigningOrder = order++,

                // In a sequential session only the first signer is invited. The next is invited
                // when the one before them signs, which is what "sequential" has to mean.
                State = !sequential || isFirst ? SignatureState.Sent : SignatureState.Pending,
                SentAt = !sequential || isFirst ? DateTime.UtcNow : null,
            }.StampNew(Tenant, userId));

            if (!sequential || isFirst)
            {
                await QueueNotificationAsync(
                    "signature.requested",
                    $"Signature requested — {document.Title}",
                    $"Please sign {document.DocumentNumber}.",
                    $"/realestate/documents/{document.Id}/sign",
                    recipientPartyId: party.PartyId,
                    entityType: nameof(SignatureSession),
                    entityId: session.Id);
            }
        }

        document.SignatureSessionId = session.Id;
        document.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return (await GetSigningAsync(session.Id))!;
    }

    public async Task<SignatureSessionDto> RecordSignatureAsync(
        Guid sessionId, Guid partyId, string? signatureUrl, string? thumbUrl, string? photoUrl, Guid userId)
    {
        var session = await Db.SignatureSessions.ForCompany(Tenant)
            .Include(s => s.Parties)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("That signing session does not exist.");

        if (session.State is SignatureState.Cancelled or SignatureState.Declined or SignatureState.Expired)
            throw new InvalidOperationException($"That session is {session.State.ToString().ToLowerInvariant()}.");

        if (session.ExpiresAt is not null && session.ExpiresAt < DateTime.UtcNow)
        {
            session.State = SignatureState.Expired;
            session.StampUpdated(userId);
            await Db.SaveChangesAsync();

            throw new InvalidOperationException("That signing session has expired. Start a new one.");
        }

        var signer = session.Parties.FirstOrDefault(p => p.Id == partyId || p.PartyId == partyId)
            ?? throw new InvalidOperationException("That signer is not part of this session.");

        if (signer.State == SignatureState.Signed)
            throw new InvalidOperationException($"{signer.Name} has already signed.");

        if (session.IsSequential)
        {
            var ahead = session.Parties
                .Where(p => p.SigningOrder < signer.SigningOrder && p.State != SignatureState.Signed)
                .Select(p => p.Name)
                .ToList();

            if (ahead.Count > 0)
                throw new InvalidOperationException(
                    "This is a sequential signing. " + string.Join(" and ", ahead) + " must sign first.");
        }

        if (session.Method == SignatureMethod.ThumbImpression && string.IsNullOrWhiteSpace(thumbUrl))
            throw new InvalidOperationException("A thumb impression signing needs the impression captured.");

        if (session.Method != SignatureMethod.ThumbImpression && string.IsNullOrWhiteSpace(signatureUrl))
            throw new InvalidOperationException("The signature image is required.");

        signer.State = SignatureState.Signed;
        signer.SignedAt = DateTime.UtcNow;
        signer.ViewedAt ??= DateTime.UtcNow;
        signer.SignatureImageUrl = signatureUrl;
        signer.ThumbImpressionUrl = thumbUrl;
        signer.PhotoUrl = photoUrl;
        signer.StampUpdated(userId);

        // The next signer in a sequential session is invited only now, so nobody signs a document
        // whose earlier signatures were still outstanding when they saw it.
        if (session.IsSequential)
        {
            var next = session.Parties
                .Where(p => p.State == SignatureState.Pending)
                .OrderBy(p => p.SigningOrder)
                .FirstOrDefault();

            if (next is not null)
            {
                next.State = SignatureState.Sent;
                next.SentAt = DateTime.UtcNow;
                next.StampUpdated(userId);

                await QueueNotificationAsync(
                    "signature.requested",
                    "Your signature is now needed",
                    $"{signer.Name} has signed. {session.Reference} is now with you.",
                    $"/realestate/documents/{session.GeneratedDocumentId}/sign",
                    recipientPartyId: next.PartyId,
                    entityType: nameof(SignatureSession),
                    entityId: session.Id);
            }
        }

        var allSigned = session.Parties.All(p => p.State == SignatureState.Signed);

        if (allSigned)
        {
            session.State = SignatureState.Signed;
            session.CompletedAt = DateTime.UtcNow;

            var document = session.GeneratedDocumentId is null
                ? null
                : await Db.GeneratedDocuments.ForCompany(Tenant)
                    .FirstOrDefaultAsync(d => d.Id == session.GeneratedDocumentId);

            if (document is not null)
            {
                document.IsSigned = true;
                document.StampUpdated(userId);
            }

            await QueueNotificationAsync(
                "signature.completed",
                $"{session.Reference} fully signed",
                "Every party has now signed.",
                $"/realestate/documents/{session.GeneratedDocumentId}",
                entityType: nameof(SignatureSession),
                entityId: session.Id);
        }

        session.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return (await GetSigningAsync(session.Id))!;
    }

    public async Task<SignatureSessionDto?> GetSigningAsync(Guid id)
    {
        var session = await Db.SignatureSessions.ForCompany(Tenant)
            .Include(s => s.Parties)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session is null) return null;

        var initiators = await AgentUserNamesAsync([session.InitiatedByUserId]);

        var title = session.GeneratedDocumentId is null
            ? null
            : await Db.GeneratedDocuments.ForCompany(Tenant)
                .Where(d => d.Id == session.GeneratedDocumentId)
                .Select(d => d.Title)
                .FirstOrDefaultAsync();

        return new SignatureSessionDto
        {
            Id = session.Id,
            Reference = session.Reference,
            GeneratedDocumentId = session.GeneratedDocumentId,
            DocumentType = session.DocumentType,
            DocumentTitle = title,
            Method = session.Method,
            State = session.State,
            InitiatedAt = session.InitiatedAt,
            InitiatedByName = session.InitiatedByUserId is null
                ? null
                : initiators.GetValueOrDefault(session.InitiatedByUserId.Value),
            ExpiresAt = session.ExpiresAt,
            CompletedAt = session.CompletedAt,
            IsSequential = session.IsSequential,
            SignedDocumentUrl = session.SignedDocumentUrl,
            AuditCertificateUrl = session.AuditCertificateUrl,
            DeclineReason = session.DeclineReason,
            SignedCount = session.Parties.Count(p => p.State == SignatureState.Signed),
            TotalSigners = session.Parties.Count,
            Parties = session.Parties.OrderBy(p => p.SigningOrder).Select(p => new SignaturePartyDto
            {
                Id = p.Id,
                PartyId = p.PartyId,
                Name = p.Name,
                Email = p.Email,
                Phone = p.Phone,
                Role = p.Role,
                SigningOrder = p.SigningOrder,
                State = p.State,
                SentAt = p.SentAt,
                ViewedAt = p.ViewedAt,
                SignedAt = p.SignedAt,
                SignatureImageUrl = p.SignatureImageUrl,
                ThumbImpressionUrl = p.ThumbImpressionUrl,
                PhotoUrl = p.PhotoUrl,
                OtpVerified = p.OtpVerified,
                DeclineReason = p.DeclineReason,
            }).ToList(),
        };
    }

    // ═══ The record room ═════════════════════════════════════════════════════

    public async Task<PaginatedResponse<PhysicalFileDto>> GetPhysicalFilesAsync(
        ListQueryDto query, PhysicalFileState? state)
    {
        var q = Db.PhysicalFiles.ForCompany(Tenant)
            .WhereIf(state.HasValue, f => f.State == state)
            .WhereIf(query.ProjectId.HasValue, f => f.ProjectId == query.ProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                f => f.FileNumber.Contains(query.Search!)
                  || (f.BarcodeOrRfid ?? "").Contains(query.Search!)
                  || (f.Rack ?? "").Contains(query.Search!))
            .OrderBy(f => f.State)
            .ThenBy(f => f.FileNumber);

        return await PageAsync(q, query, rows => MapPhysicalFilesAsync(rows, includeMovements: false));
    }

    private async Task<List<PhysicalFileDto>> MapPhysicalFilesAsync(List<PhysicalFile> files, bool includeMovements)
    {
        if (files.Count == 0) return [];

        var ids = files.Select(f => f.Id).ToList();

        var bookingIds = files.Where(f => f.BookingId != null).Select(f => f.BookingId!.Value).Distinct().ToList();

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant)
                .Where(b => bookingIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Reference);

        var names = await PartyNamesAsync(files.Where(f => f.PartyId != null).Select(f => f.PartyId!.Value));
        var projects = await ProjectNamesAsync(files.Select(f => f.ProjectId));
        var labels = await UnitLabelsAsync(files.Where(f => f.UnitId != null).Select(f => f.UnitId!.Value));

        var movements = includeMovements
            ? await Db.PhysicalFileMovements.ForCompany(Tenant)
                .Where(m => ids.Contains(m.PhysicalFileId))
                .OrderByDescending(m => m.OccurredAt)
                .ToListAsync()
            : [];

        var users = await AgentUserNamesAsync(
            files.Select(f => f.IssuedToUserId)
                .Concat(movements.Select(m => m.FromUserId))
                .Concat(movements.Select(m => m.RecordedByUserId)));

        return files.Select(f => new PhysicalFileDto
        {
            Id = f.Id,
            FileNumber = f.FileNumber,
            BookingId = f.BookingId,
            BookingReference = f.BookingId is null ? null : bookings.GetValueOrDefault(f.BookingId.Value),
            PlotFileId = f.PlotFileId,
            UnitId = f.UnitId,
            UnitNumber = f.UnitId is null ? null : labels.GetValueOrDefault(f.UnitId.Value)?.UnitNumber,
            PartyName = f.PartyId is null ? null : names.GetValueOrDefault(f.PartyId.Value),
            ProjectName = f.ProjectId is null ? null : projects.GetValueOrDefault(f.ProjectId.Value),
            State = f.State,
            RoomLocation = f.RoomLocation,
            Rack = f.Rack,
            Cabinet = f.Cabinet,
            Shelf = f.Shelf,
            BarcodeOrRfid = f.BarcodeOrRfid,
            IssuedToName = f.IssuedToName ?? (f.IssuedToUserId is null ? null : users.GetValueOrDefault(f.IssuedToUserId.Value)),
            IssuedOn = f.IssuedOn,
            DueBackOn = f.DueBackOn,
            IsOverdue = f.DueBackOn is not null && f.DueBackOn < Today && f.State != PhysicalFileState.InRecordRoom,
            DocumentCount = f.DocumentCount,
            LastAuditedOn = f.LastAuditedOn,
            IsMissing = f.IsMissing,
            Note = f.Note,
            Movements = movements.Where(m => m.PhysicalFileId == f.Id).Select(m => new PhysicalFileMovementDto
            {
                Id = m.Id,
                Movement = m.Movement,
                OccurredAt = m.OccurredAt,
                FromName = m.FromUserId is null ? null : users.GetValueOrDefault(m.FromUserId.Value),
                ToName = m.ToName,
                Purpose = m.Purpose,
                DueBackOn = m.DueBackOn,
                RecordedByName = m.RecordedByUserId is null ? null : users.GetValueOrDefault(m.RecordedByUserId.Value),
                SignatureUrl = m.SignatureUrl,
                Note = m.Note,
            }).ToList(),
        }).ToList();
    }

    public async Task<PhysicalFileDto> SavePhysicalFileAsync(PhysicalFileDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var file = isNew
            ? new PhysicalFile { FileNumber = await numbering.NextPhysicalFileNumberAsync() }
            : await Db.PhysicalFiles.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == dto.Id)
              ?? throw new InvalidOperationException("That file does not exist.");

        file.BookingId = dto.BookingId;
        file.PlotFileId = dto.PlotFileId;
        file.UnitId = dto.UnitId;
        file.RoomLocation = dto.RoomLocation;
        file.Rack = dto.Rack;
        file.Cabinet = dto.Cabinet;
        file.Shelf = dto.Shelf;
        file.BarcodeOrRfid = dto.BarcodeOrRfid;
        file.DocumentCount = dto.DocumentCount;
        file.LastAuditedOn = dto.LastAuditedOn;
        file.Note = dto.Note;

        if (dto.IsMissing && !file.IsMissing)
        {
            file.IsMissing = true;
            file.State = PhysicalFileState.Missing;
            file.ReportedMissingOn = Today;

            // A missing title file is a serious event, not a checkbox. It is escalated the moment
            // it is recorded rather than surfacing in a monthly report.
            await QueueNotificationAsync(
                "file.missing",
                $"File {file.FileNumber} reported missing",
                "The physical file cannot be located in the record room.",
                $"/realestate/records/files/{file.Id}",
                entityType: nameof(PhysicalFile),
                entityId: file.Id,
                severity: AlertSeverity.Critical);
        }
        else if (!dto.IsMissing && file.IsMissing)
        {
            file.IsMissing = false;
            file.ReportedMissingOn = null;
            file.State = PhysicalFileState.InRecordRoom;
        }

        if (isNew)
        {
            file.StampNew(Tenant, userId);
            Db.PhysicalFiles.Add(file);
        }
        else
        {
            file.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await MapPhysicalFilesAsync([file], includeMovements: true))[0];
    }

    public async Task<PhysicalFileDto> MovePhysicalFileAsync(
        Guid id, string movement, Guid? toUserId, string? toName, string? purpose, DateOnly? dueBack, Guid userId)
    {
        var file = await RequireAsync<PhysicalFile>(id, "That file does not exist.");

        if (file.IsMissing && movement != "Found")
            throw new InvalidOperationException(
                $"File {file.FileNumber} is recorded as missing. Mark it found before moving it.");

        var previousUser = file.IssuedToUserId;

        switch (movement)
        {
            case "Issued":
                if (file.State != PhysicalFileState.InRecordRoom)
                    throw new InvalidOperationException(
                        $"File {file.FileNumber} is already out — it is {Humanise(file.State)}. "
                        + "It has to come back before it can be issued again.");

                if (toUserId is null && string.IsNullOrWhiteSpace(toName))
                    throw new InvalidOperationException("A file cannot be issued to nobody.");

                file.State = PhysicalFileState.IssuedToStaff;
                file.IssuedToUserId = toUserId;
                file.IssuedToName = toName;
                file.IssuedOn = Today;
                file.DueBackOn = dueBack ?? Today.AddDays(7);
                break;

            case "Returned":
                file.State = PhysicalFileState.InRecordRoom;
                file.IssuedToUserId = null;
                file.IssuedToName = null;
                file.IssuedOn = null;
                file.DueBackOn = null;
                file.IsOverdue = false;
                break;

            case "ToLegal":
                file.State = PhysicalFileState.WithLegal;
                file.IssuedToUserId = toUserId;
                file.IssuedToName = toName;
                file.IssuedOn = Today;
                file.DueBackOn = dueBack;
                break;

            case "ToAuditor":
                file.State = PhysicalFileState.WithAuditor;
                file.IssuedToUserId = toUserId;
                file.IssuedToName = toName;
                file.IssuedOn = Today;
                file.DueBackOn = dueBack;
                break;

            case "ReleasedToOwner":
                // Handing the original title documents to the owner is final. Nothing comes back,
                // so the file is closed rather than left looking borrowed.
                file.State = PhysicalFileState.ReleasedToOwner;
                file.IssuedToName = toName;
                file.IssuedOn = Today;
                file.DueBackOn = null;
                break;

            case "Found":
                file.IsMissing = false;
                file.ReportedMissingOn = null;
                file.State = PhysicalFileState.InRecordRoom;
                break;

            case "Archived":
                file.State = PhysicalFileState.Archived;
                break;

            default:
                throw new InvalidOperationException($"“{movement}” is not a movement this record room recognises.");
        }

        Db.PhysicalFileMovements.Add(new PhysicalFileMovement
        {
            PhysicalFileId = file.Id,
            Movement = movement,
            OccurredAt = DateTime.UtcNow,
            FromUserId = previousUser,
            ToUserId = toUserId,
            ToName = toName,
            Purpose = purpose,
            DueBackOn = dueBack,
            RecordedByUserId = userId,
        }.StampNew(Tenant, userId));

        file.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return (await MapPhysicalFilesAsync([file], includeMovements: true))[0];
    }

    private static string Humanise(PhysicalFileState state)
        => Regex.Replace(state.ToString(), "(?<!^)([A-Z])", " $1").ToLowerInvariant();

    // ═══ Litigation ══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<LegalCaseDto>> GetLegalCasesAsync(ListQueryDto query, LegalCaseStatus? status)
    {
        var q = Db.LegalCases.ForCompany(Tenant)
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(query.ProjectId.HasValue, c => c.ProjectId == query.ProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                c => c.Reference.Contains(query.Search!)
                  || (c.CaseNumber ?? "").Contains(query.Search!)
                  || (c.OpposingParty ?? "").Contains(query.Search!))
            .OrderBy(c => c.IsClosed)
            .ThenBy(c => c.NextHearingDate ?? DateOnly.MaxValue);

        return await PageAsync(q, query, rows => MapLegalCasesAsync(rows, includeHearings: false));
    }

    public async Task<LegalCaseDto?> GetLegalCaseAsync(Guid id)
    {
        var legalCase = await Db.LegalCases.ForCompany(Tenant)
            .Include(c => c.Hearings)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (legalCase is null) return null;

        return (await MapLegalCasesAsync([legalCase], includeHearings: true))[0];
    }

    private async Task<List<LegalCaseDto>> MapLegalCasesAsync(List<LegalCase> cases, bool includeHearings)
    {
        if (cases.Count == 0) return [];

        var currency = await CurrencyAsync();
        var projects = await ProjectNamesAsync(cases.Select(c => c.ProjectId));
        var owners = await AgentUserNamesAsync(cases.Select(c => c.OwnerUserId));
        var names = await PartyNamesAsync(cases.Where(c => c.PartyId != null).Select(c => c.PartyId!.Value));
        var labels = await UnitLabelsAsync(cases.Where(c => c.UnitId != null).Select(c => c.UnitId!.Value));

        var propertyIds = cases.Where(c => c.PropertyId != null).Select(c => c.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant).Where(p => propertyIds.Contains(p.Id)).ToListAsync();

        var hearings = includeHearings
            ? cases.SelectMany(c => c.Hearings).ToList()
            : [];

        var attendees = await AgentUserNamesAsync(hearings.Select(h => h.AttendedByUserId));

        return cases.Select(c => new LegalCaseDto
        {
            Id = c.Id,
            Reference = c.Reference,
            CaseNumber = c.CaseNumber,
            CaseType = c.CaseType,
            Status = c.Status,
            ProjectId = c.ProjectId,
            ProjectName = c.ProjectId is null ? null : projects.GetValueOrDefault(c.ProjectId.Value),
            PropertyId = c.PropertyId,
            AddressOneLine = c.PropertyId is null
                ? null
                : properties.Where(p => p.Id == c.PropertyId).Select(RealEstateMapper.OneLineAddress).FirstOrDefault(),
            UnitId = c.UnitId,
            UnitNumber = c.UnitId is null ? null : labels.GetValueOrDefault(c.UnitId.Value)?.UnitNumber,
            BookingId = c.BookingId,
            TenancyId = c.TenancyId,
            SubcontractId = c.SubcontractId,
            PartyId = c.PartyId,
            PartyName = c.PartyId is null ? null : names.GetValueOrDefault(c.PartyId.Value),
            OurRole = c.OurRole,
            OpposingParty = c.OpposingParty,
            Court = c.Court,
            Jurisdiction = c.Jurisdiction,
            FiledOn = c.FiledOn,
            NextHearingDate = c.NextHearingDate,
            DaysToHearing = c.NextHearingDate is null ? null : c.NextHearingDate.Value.DayNumber - Today.DayNumber,
            DecidedOn = c.DecidedOn,
            ClaimAmount = c.ClaimAmount,
            ExposureAmount = c.ExposureAmount,
            LegalCostsIncurred = c.LegalCostsIncurred,
            CurrencyCode = currency,
            AdvocateName = c.AdvocateName,
            AdvocateContact = c.AdvocateContact,
            OwnerName = c.OwnerUserId is null ? null : owners.GetValueOrDefault(c.OwnerUserId.Value),
            BlocksTransaction = c.BlocksTransaction,
            Outcome = c.Outcome,
            Summary = c.Summary,
            IsClosed = c.IsClosed,
            Hearings = c.Hearings.OrderByDescending(h => h.HearingDate).Select(h => new LegalHearingDto
            {
                Id = h.Id,
                HearingDate = h.HearingDate,
                Purpose = h.Purpose,
                Attended = h.Attended,
                AttendedByName = h.AttendedByUserId is null ? null : attendees.GetValueOrDefault(h.AttendedByUserId.Value),
                Outcome = h.Outcome,
                NextDate = h.NextDate,
                NextPurpose = h.NextPurpose,
                OrderSummary = h.OrderSummary,
                OrderDocumentUrl = h.OrderDocumentUrl,
                CostIncurred = h.CostIncurred,
                Note = h.Note,
            }).ToList(),
        }).ToList();
    }

    public async Task<LegalCaseDto> SaveLegalCaseAsync(LegalCaseDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var legalCase = isNew
            ? new LegalCase { Reference = await numbering.NextMasterCodeAsync(Db.LegalCases, "LGL") }
            : await Db.LegalCases.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
              ?? throw new InvalidOperationException("That case does not exist.");

        if (dto.IsClosed && string.IsNullOrWhiteSpace(dto.Outcome))
            throw new InvalidOperationException("A case cannot be closed without recording its outcome.");

        legalCase.CaseNumber = dto.CaseNumber;
        legalCase.CaseType = dto.CaseType;
        legalCase.Status = dto.Status;
        legalCase.ProjectId = dto.ProjectId;
        legalCase.PropertyId = dto.PropertyId;
        legalCase.UnitId = dto.UnitId;
        legalCase.BookingId = dto.BookingId;
        legalCase.TenancyId = dto.TenancyId;
        legalCase.SubcontractId = dto.SubcontractId;
        legalCase.PartyId = dto.PartyId;
        legalCase.OurRole = dto.OurRole;
        legalCase.OpposingParty = dto.OpposingParty;
        legalCase.Court = dto.Court;
        legalCase.Jurisdiction = dto.Jurisdiction;
        legalCase.FiledOn = dto.FiledOn == default ? Today : dto.FiledOn;
        legalCase.NextHearingDate = dto.NextHearingDate;
        legalCase.DecidedOn = dto.DecidedOn;
        legalCase.ClaimAmount = dto.ClaimAmount;
        legalCase.ExposureAmount = dto.ExposureAmount;
        legalCase.LegalCostsIncurred = dto.LegalCostsIncurred;
        legalCase.AdvocateName = dto.AdvocateName;
        legalCase.AdvocateContact = dto.AdvocateContact;
        legalCase.BlocksTransaction = dto.BlocksTransaction;
        legalCase.Outcome = dto.Outcome;
        legalCase.Summary = dto.Summary;
        legalCase.Description = dto.Summary;
        legalCase.IsClosed = dto.IsClosed;

        if (isNew)
        {
            legalCase.OwnerUserId = userId;
            legalCase.StampNew(Tenant, userId);
            Db.LegalCases.Add(legalCase);
        }
        else
        {
            legalCase.StampUpdated(userId);
        }

        // A live case that blocks transactions has to reach the sales floor, not just the legal
        // folder. The unit is flagged so nobody sells what is under dispute.
        if (legalCase.BlocksTransaction && !legalCase.IsClosed)
        {
            if (legalCase.BookingId is not null)
            {
                var booking = await Db.Bookings.ForCompany(Tenant)
                    .FirstOrDefaultAsync(b => b.Id == legalCase.BookingId);

                if (booking is not null && !booking.IsUnderLitigation)
                {
                    booking.IsUnderLitigation = true;
                    booking.StampUpdated(userId);
                }
            }

            if (legalCase.UnitId is not null)
            {
                var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == legalCase.UnitId);

                if (unit is not null && unit.Status == PropertyStatus.Available)
                {
                    unit.Status = PropertyStatus.Litigation;
                    unit.IsSaleable = false;
                    unit.StampUpdated(userId);
                }
            }
        }

        if (legalCase.NextHearingDate is not null && !legalCase.IsClosed)
        {
            await UpsertCalendarEntryAsync(
                $"Hearing — {legalCase.CaseNumber ?? legalCase.Reference}", "Legal",
                legalCase.NextHearingDate.Value, userId,
                projectId: legalCase.ProjectId,
                ownerUserId: legalCase.OwnerUserId,
                severity: AlertSeverity.Critical,
                alertDaysBefore: 3);
        }

        await Db.SaveChangesAsync();

        return (await GetLegalCaseAsync(legalCase.Id))!;
    }

    public async Task<LegalHearingDto> RecordHearingAsync(Guid caseId, LegalHearingDto dto, Guid userId)
    {
        var legalCase = await RequireAsync<LegalCase>(caseId, "That case does not exist.");

        if (legalCase.IsClosed)
            throw new InvalidOperationException("That case is closed. Reopen it before recording a hearing.");

        var hearing = new LegalHearing
        {
            LegalCaseId = caseId,
            HearingDate = dto.HearingDate == default ? Today : dto.HearingDate,
            Purpose = dto.Purpose,
            Attended = dto.Attended,
            AttendedByUserId = dto.Attended ? userId : null,
            Outcome = dto.Outcome,
            NextDate = dto.NextDate,
            NextPurpose = dto.NextPurpose,
            OrderSummary = dto.OrderSummary,
            OrderDocumentUrl = dto.OrderDocumentUrl,
            CostIncurred = dto.CostIncurred,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        Db.LegalHearings.Add(hearing);

        legalCase.NextHearingDate = dto.NextDate;
        legalCase.Status = dto.NextDate is null ? LegalCaseStatus.Reserved : LegalCaseStatus.Hearing;

        if (dto.CostIncurred is > 0m)
            legalCase.LegalCostsIncurred = RealEstateMapper.Money(legalCase.LegalCostsIncurred + dto.CostIncurred.Value);

        legalCase.StampUpdated(userId);

        // A hearing that nobody attended is how cases are lost by default. It is escalated rather
        // than merely recorded.
        if (!dto.Attended)
        {
            await QueueNotificationAsync(
                "legal.hearing.missed",
                $"Hearing on {legalCase.CaseNumber ?? legalCase.Reference} was not attended",
                $"The hearing on {hearing.HearingDate:dd MMM yyyy} went ahead without us.",
                $"/realestate/legal/cases/{legalCase.Id}",
                recipientUserId: legalCase.OwnerUserId,
                entityType: nameof(LegalCase),
                entityId: legalCase.Id,
                severity: AlertSeverity.Critical);
        }

        if (dto.NextDate is not null)
        {
            await UpsertCalendarEntryAsync(
                $"Hearing — {legalCase.CaseNumber ?? legalCase.Reference}", "Legal",
                dto.NextDate.Value, userId,
                projectId: legalCase.ProjectId,
                ownerUserId: legalCase.OwnerUserId,
                severity: AlertSeverity.Critical,
                alertDaysBefore: 3);
        }

        await Db.SaveChangesAsync();

        var attendees = await AgentUserNamesAsync([hearing.AttendedByUserId]);

        return new LegalHearingDto
        {
            Id = hearing.Id,
            HearingDate = hearing.HearingDate,
            Purpose = hearing.Purpose,
            Attended = hearing.Attended,
            AttendedByName = hearing.AttendedByUserId is null
                ? null
                : attendees.GetValueOrDefault(hearing.AttendedByUserId.Value),
            Outcome = hearing.Outcome,
            NextDate = hearing.NextDate,
            NextPurpose = hearing.NextPurpose,
            OrderSummary = hearing.OrderSummary,
            OrderDocumentUrl = hearing.OrderDocumentUrl,
            CostIncurred = hearing.CostIncurred,
            Note = hearing.Note,
        };
    }
}
