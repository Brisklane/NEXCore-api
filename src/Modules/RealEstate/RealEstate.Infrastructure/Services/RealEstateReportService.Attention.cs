using Microsoft.EntityFrameworkCore;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The attention list: the handful of things that will cost money if nobody acts today.
///
/// It is computed live rather than read from a table, because a stale warning is worse than none —
/// somebody clears the problem and the banner stays up, and within a fortnight everybody has
/// learned to ignore the banner. Each item carries a count, an amount where money is at stake, and
/// a route straight to the screen that fixes it. Anything that cannot say what to do about it does
/// not belong here.
///
/// Ranking is deliberate: money that is about to be lost outranks money that is merely late, and
/// both outrank anything that is only untidy.
/// </summary>
public partial class RealEstateReportService
{
    public async Task<List<AttentionItemDto>> GetAttentionAsync(Guid? officeId, Guid? projectId)
    {
        var settings = await SettingsAsync();
        var today = Today;
        var now = DateTime.UtcNow;

        var items = new List<AttentionItemDto>();

        // ── Things with a deadline today ─────────────────────────────────────

        var holdsExpiring = await Db.UnitHolds.ForCompany(Tenant)
            .Where(h => h.Status == HoldStatus.Active && h.ExpiresAt <= now.AddHours(4))
            .CountAsync();

        if (holdsExpiring > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "holds.expiring",
                Severity = AlertSeverity.Warning,
                Title = holdsExpiring == 1 ? "A hold expires within four hours" : $"{holdsExpiring} holds expire within four hours",
                Detail = "Each one is a unit somebody is waiting on. Convert it or release it.",
                Icon = "clock",
                Route = "/realestate/inventory/holds",
                Count = holdsExpiring,
                Rank = 20,
            });
        }

        var breaching = await Db.Enquiries.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, e => e.OfficeId == officeId)
            .WhereIf(projectId.HasValue, e => e.ProjectId == projectId)
            .Where(e => e.ClosedAt == null && e.FirstContactedAt == null
                     && e.ResponseDueAt != null && e.ResponseDueAt < now)
            .CountAsync();

        if (breaching > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "leads.sla",
                Severity = AlertSeverity.Critical,
                Title = $"{breaching} lead{(breaching == 1 ? "" : "s")} past the {settings.LeadResponseSlaMinutes}-minute response promise",
                Detail = "A lead answered late converts at a fraction of one answered first.",
                Icon = "phone-missed",
                Route = "/realestate/crm/enquiries?filter=breaching",
                Count = breaching,
                Rank = 10,
            });
        }

        // ── Money at risk ────────────────────────────────────────────────────

        var overdue = await Db.Bookings.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, b => b.ProjectId == projectId)
            .Where(b => b.Status != BookingStatus.Cancelled && b.OverdueAmount > 0m)
            .Select(b => new { b.OverdueAmount, b.DaysOverdue })
            .ToListAsync();

        var severelyOverdue = overdue.Where(b => b.DaysOverdue > 90).ToList();

        if (severelyOverdue.Count > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "collections.severe",
                Severity = AlertSeverity.Critical,
                Title = $"{severelyOverdue.Count} booking{(severelyOverdue.Count == 1 ? " is" : "s are")} more than 90 days overdue",
                Detail = "Past ninety days, recovery rates fall sharply. These need a decision, not another reminder.",
                Icon = "alert-triangle",
                Route = "/realestate/money/dunning",
                Count = severelyOverdue.Count,
                Amount = RealEstateMapper.Money(severelyOverdue.Sum(b => b.OverdueAmount)),
                Rank = 5,
            });
        }
        else if (overdue.Count > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "collections.overdue",
                Severity = AlertSeverity.Warning,
                Title = $"{overdue.Count} booking{(overdue.Count == 1 ? " is" : "s are")} overdue",
                Icon = "banknote",
                Route = "/realestate/money/collections",
                Count = overdue.Count,
                Amount = RealEstateMapper.Money(overdue.Sum(b => b.OverdueAmount)),
                Rank = 30,
            });
        }

        var bouncedCheques = await Db.ChequeRecords.ForCompany(Tenant)
            .Where(c => c.State == ChequeState.Bounced && c.ReplacementChequeId == null)
            .CountAsync();

        if (bouncedCheques > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "cheques.bounced",
                Severity = AlertSeverity.Critical,
                Title = $"{bouncedCheques} cheque{(bouncedCheques == 1 ? "" : "s")} bounced and not represented",
                Detail = "Every day one sits unactioned is a day of statutory notice period lost.",
                Icon = "file-x",
                Route = "/realestate/money/cheques",
                Count = bouncedCheques,
                Rank = 8,
            });
        }

        // ── Statutory dates ──────────────────────────────────────────────────

        var expiringApprovals = await Db.ApprovalRecords.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, a => a.ProjectId == projectId)
            .Where(a => a.IsMandatory && a.ValidUntil != null && a.ValidUntil <= today.AddDays(30))
            .Where(a => a.State == ApprovalState.Granted || a.State == ApprovalState.GrantedWithConditions)
            .CountAsync();

        if (expiringApprovals > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "approvals.expiring",
                Severity = AlertSeverity.Critical,
                Title = $"{expiringApprovals} mandatory approval{(expiringApprovals == 1 ? "" : "s")} expire within a month",
                Detail = "Work carried out under a lapsed approval is unauthorised, whatever the paperwork says afterwards.",
                Icon = "stamp",
                Route = "/realestate/compliance/approvals",
                Count = expiringApprovals,
                Rank = 12,
            });
        }

        var overdueFilings = await Db.RegulatoryFilings.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, f => f.ProjectId == projectId)
            .Where(f => f.FiledOn == null && f.DueOn < today)
            .Select(f => new { f.LateFilingPenalty })
            .ToListAsync();

        if (overdueFilings.Count > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "filings.overdue",
                Severity = AlertSeverity.Critical,
                Title = $"{overdueFilings.Count} regulatory filing{(overdueFilings.Count == 1 ? " is" : "s are")} overdue",
                Icon = "file-warning",
                Route = "/realestate/compliance/filings",
                Count = overdueFilings.Count,
                Amount = overdueFilings.Sum(f => f.LateFilingPenalty ?? 0m) is var penalty && penalty > 0m
                    ? RealEstateMapper.Money(penalty)
                    : null,
                Rank = 7,
            });
        }

        var expiringLicences = await Db.LicenceRecords.ForCompany(Tenant)
            .Where(l => l.IsCurrent && l.IsMandatoryToTrade && l.ExpiresOn <= today.AddDays(30))
            .CountAsync();

        if (expiringLicences > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "licences.expiring",
                Severity = AlertSeverity.Critical,
                Title = expiringLicences == 1
                    ? "A licence required to trade expires within a month"
                    : $"{expiringLicences} licences required to trade expire within a month",
                Detail = "Trading without one is not a paperwork problem; it voids the transactions.",
                Icon = "id-card",
                Route = "/realestate/compliance/licences",
                Count = expiringLicences,
                Rank = 6,
            });
        }

        if (settings.EstateManagementEnabled)
        {
            var expiredCerts = await Db.ComplianceCertificates.ForCompany(Tenant)
                .Where(c => c.IsCurrent && c.ExpiresOn < today)
                .CountAsync();

            if (expiredCerts > 0)
            {
                items.Add(new AttentionItemDto
                {
                    Key = "certificates.expired",
                    Severity = AlertSeverity.Critical,
                    Title = $"{expiredCerts} safety certificate{(expiredCerts == 1 ? " has" : "s have")} expired on let property",
                    Detail = "A tenanted property without a current certificate is an offence and an uninsured risk.",
                    Icon = "shield-alert",
                    Route = "/realestate/leasing/compliance",
                    Count = expiredCerts,
                    Rank = 4,
                });
            }
        }

        // ── Approvals waiting on somebody ────────────────────────────────────

        var pendingApprovals = await Db.ApprovalRequests.ForCompany(Tenant)
            .Where(a => a.Outcome == ApprovalOutcome.Pending)
            .Select(a => new { a.EscalatesAt, a.Amount })
            .ToListAsync();

        var escalated = pendingApprovals.Where(a => a.EscalatesAt is not null && a.EscalatesAt < now).ToList();

        if (escalated.Count > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "approvals.escalated",
                Severity = AlertSeverity.Warning,
                Title = $"{escalated.Count} approval{(escalated.Count == 1 ? " has" : "s have")} passed their escalation time",
                Detail = "Somebody downstream is waiting and does not know why.",
                Icon = "hourglass",
                Route = "/realestate/admin/approvals",
                Count = escalated.Count,
                Amount = RealEstateMapper.Money(escalated.Sum(a => a.Amount)),
                Rank = 25,
            });
        }

        // ── Escrow ───────────────────────────────────────────────────────────

        if (settings.EscrowEnforced)
        {
            var escrowShort = await Db.ProjectBankAccounts.ForCompany(Tenant)
                .WhereIf(projectId.HasValue, a => a.ProjectId == projectId)
                .Where(a => a.Kind == ProjectAccountKind.Escrow && a.IsActive)
                .Where(a => a.TotalWithdrawn > a.WithdrawalEntitlement + 1m)
                .CountAsync();

            if (escrowShort > 0)
            {
                items.Add(new AttentionItemDto
                {
                    Key = "escrow.overdrawn",
                    Severity = AlertSeverity.Critical,
                    Title = escrowShort == 1
                        ? "An escrow account has been drawn beyond its certified entitlement"
                        : $"{escrowShort} escrow accounts have been drawn beyond their certified entitlement",
                    Detail = "This is the first thing a regulator's audit tests. Certify further progress or return the difference.",
                    Icon = "vault",
                    Route = "/realestate/finance/escrow",
                    Count = escrowShort,
                    Rank = 3,
                });
            }

            var auditOverdue = await Db.ProjectBankAccounts.ForCompany(Tenant)
                .Where(a => a.Kind == ProjectAccountKind.Escrow && a.IsActive
                         && a.NextAuditDue != null && a.NextAuditDue < today)
                .CountAsync();

            if (auditOverdue > 0)
            {
                items.Add(new AttentionItemDto
                {
                    Key = "escrow.audit",
                    Severity = AlertSeverity.Warning,
                    Title = $"{auditOverdue} escrow account{(auditOverdue == 1 ? "" : "s")} overdue for audit",
                    Icon = "clipboard-check",
                    Route = "/realestate/finance/escrow",
                    Count = auditOverdue,
                    Rank = 35,
                });
            }
        }

        // ── Client money ─────────────────────────────────────────────────────

        if (settings.ClientMoneySegregated)
        {
            var unreconciled = await Db.ClientMoneyExceptions.ForCompany(Tenant)
                .Where(e => !e.IsResolved)
                .Select(e => new { e.Amount })
                .ToListAsync();

            if (unreconciled.Count > 0)
            {
                items.Add(new AttentionItemDto
                {
                    Key = "clientmoney.exceptions",
                    Severity = AlertSeverity.Critical,
                    Title = $"{unreconciled.Count} unresolved client-money exception{(unreconciled.Count == 1 ? "" : "s")}",
                    Detail = "Client money that will not reconcile is the one finding that closes an agency down.",
                    Icon = "scale",
                    Route = "/realestate/leasing/client-money",
                    Count = unreconciled.Count,
                    Amount = RealEstateMapper.Money(unreconciled.Sum(e => e.Amount ?? 0m)),
                    Rank = 2,
                });
            }
        }

        // ── Delivery ─────────────────────────────────────────────────────────

        var slipping = await Db.Projects.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, p => p.Id == projectId)
            .Where(p => p.PromisedPossessionDate != null && p.ForecastPossessionDate != null
                     && p.ForecastPossessionDate > p.PromisedPossessionDate)
            .Where(p => p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.HandedOver
                                                            && p.Status != ProjectStatus.Closed)
            .CountAsync();

        if (slipping > 0)
        {
            var compensation = settings.DelayCompensationAnnualPercent > 0m
                ? $" Delay compensation accrues at {settings.DelayCompensationAnnualPercent:0.##}% a year on money collected."
                : null;

            items.Add(new AttentionItemDto
            {
                Key = "projects.slipping",
                Severity = AlertSeverity.Warning,
                Title = $"{slipping} project{(slipping == 1 ? " is" : "s are")} forecast to miss the promised possession date",
                Detail = "Every buyer was given that date in writing." + compensation,
                Icon = "calendar-x",
                Route = "/realestate/projects",
                Count = slipping,
                Rank = 15,
            });
        }

        var openSnags = await Db.Snags.ForCompany(Tenant)
            .Where(s => s.Severity == SnagSeverity.Critical && s.Status != SnagStatus.Verified
                                                            && s.Status != SnagStatus.Deferred)
            .CountAsync();

        if (openSnags > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "snags.critical",
                Severity = AlertSeverity.Critical,
                Title = $"{openSnags} critical snag{(openSnags == 1 ? "" : "s")} open on units awaiting handover",
                Detail = "A critical snag blocks possession, so each one is holding a completion.",
                Icon = "wrench",
                Route = "/realestate/handover/snags",
                Count = openSnags,
                Rank = 14,
            });
        }

        // ── Service delivery ─────────────────────────────────────────────────

        var breachedWork = await Db.WorkOrders.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, w => w.ProjectId == projectId)
            .Where(w => w.SlaBreached && w.CompletedAt == null && w.Status != WorkOrderStatus.Cancelled)
            .CountAsync();

        if (breachedWork > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "workorders.sla",
                Severity = AlertSeverity.Warning,
                Title = $"{breachedWork} work order{(breachedWork == 1 ? " is" : "s are")} past their response time",
                Icon = "hard-hat",
                Route = "/realestate/facility/work-orders",
                Count = breachedWork,
                Rank = 40,
            });
        }

        var emergencies = await Db.Complaints.ForCompany(Tenant)
            .Where(c => c.Priority == TicketPriority.Emergency
                     && c.ResolvedAt == null && c.ClosedAt == null)
            .CountAsync();

        if (emergencies > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "complaints.emergency",
                Severity = AlertSeverity.Critical,
                Title = $"{emergencies} emergency complaint{(emergencies == 1 ? " is" : "s are")} still open",
                Detail = "Emergencies are things like no water, no power, or a lift with somebody in it.",
                Icon = "siren",
                Route = "/realestate/society/complaints",
                Count = emergencies,
                Rank = 1,
            });
        }

        // ── Litigation and records ───────────────────────────────────────────

        var hearings = await Db.LegalCases.ForCompany(Tenant)
            .Where(c => !c.IsClosed && c.NextHearingDate != null
                     && c.NextHearingDate >= today && c.NextHearingDate <= today.AddDays(7))
            .CountAsync();

        if (hearings > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "legal.hearings",
                Severity = AlertSeverity.Warning,
                Title = $"{hearings} hearing{(hearings == 1 ? " is" : "s are")} listed in the next seven days",
                Icon = "gavel",
                Route = "/realestate/legal/cases",
                Count = hearings,
                Rank = 18,
            });
        }

        var missingFiles = await Db.PhysicalFiles.ForCompany(Tenant)
            .Where(f => f.IsMissing)
            .CountAsync();

        if (missingFiles > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "files.missing",
                Severity = AlertSeverity.Critical,
                Title = $"{missingFiles} physical file{(missingFiles == 1 ? " is" : "s are")} recorded as missing",
                Detail = "Original title documents. Nothing about the units they belong to can complete without them.",
                Icon = "folder-x",
                Route = "/realestate/records/files",
                Count = missingFiles,
                Rank = 11,
            });
        }

        var overdueFiles = await Db.PhysicalFiles.ForCompany(Tenant)
            .Where(f => !f.IsMissing && f.DueBackOn != null && f.DueBackOn < today
                     && f.State != PhysicalFileState.InRecordRoom
                     && f.State != PhysicalFileState.ReleasedToOwner
                     && f.State != PhysicalFileState.Archived)
            .CountAsync();

        if (overdueFiles > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "files.overdue",
                Severity = AlertSeverity.Info,
                Title = $"{overdueFiles} file{(overdueFiles == 1 ? " is" : "s are")} out past their return date",
                Icon = "folder-clock",
                Route = "/realestate/records/files?filter=overdue",
                Count = overdueFiles,
                Rank = 60,
            });
        }

        // ── Guarantees ───────────────────────────────────────────────────────

        var guarantees = await Db.BankGuarantees.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, g => g.ProjectId == projectId)
            .Where(g => g.Status == "Active" && !g.IsAutoRenewing && g.ExpiresOn <= today.AddDays(30))
            .Select(g => new { g.Amount })
            .ToListAsync();

        if (guarantees.Count > 0)
        {
            items.Add(new AttentionItemDto
            {
                Key = "guarantees.expiring",
                Severity = AlertSeverity.Warning,
                Title = $"{guarantees.Count} bank guarantee{(guarantees.Count == 1 ? "" : "s")} expire within a month",
                Detail = "Once one lapses the cover is gone, and it cannot be reinstated retrospectively.",
                Icon = "shield",
                Route = "/realestate/finance/guarantees",
                Count = guarantees.Count,
                Amount = RealEstateMapper.Money(guarantees.Sum(g => g.Amount)),
                Rank = 22,
            });
        }

        // Dismissals are honoured, so somebody who has decided a warning is not a problem this week
        // is not shown it every morning. They come back when the dismissal lapses.
        var dismissed = await Db.AttentionItems.ForCompany(Tenant)
            .Where(a => a.IsDismissed && (a.DismissedUntil == null || a.DismissedUntil > now))
            .Select(a => a.ItemKey)
            .ToListAsync();

        return items
            .Where(i => !dismissed.Contains(i.Key))
            .OrderBy(i => i.Rank)
            .ThenByDescending(i => i.Amount ?? 0m)
            .ToList();
    }
}
