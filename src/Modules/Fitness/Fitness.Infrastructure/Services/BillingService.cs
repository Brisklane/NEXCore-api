using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// The money engine.
///
/// A gym's revenue collects itself every night whether or not anyone is watching, so three things
/// matter more than anything else here:
///
/// - **The run is idempotent and resumable.** Four thousand members is four thousand chances to
///   crash halfway, and a rerun that double-charges the first two thousand is worse than a run
///   that never happened. Every schedule row is marked billed as it is consumed, so a restart
///   picks up exactly where it stopped.
/// - **Revenue is deferred, not recognised on collection.** An annual paid in January is a
///   liability in February. Skipping this is why most gym software's revenue disagrees with its
///   own accountant.
/// - **Nothing here processes a card.** Payments are recorded — method, brand, last four, a
///   provider reference — and a real gateway drops in behind <see cref="IPaymentProvider"/>.
/// </summary>
public class BillingService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IPaymentProvider provider) : IBillingService
{
    // ── Schedule ─────────────────────────────────────────────────────────────

    public async Task<List<BillingScheduleDto>> GetScheduleAsync(Guid agreementId)
    {
        var rows = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreementId)
            .OrderBy(s => s.DueOn)
            .ToListAsync();

        return [.. rows.Select(FitnessMapper.ToDto)];
    }

    /// <summary>
    /// Recomputes the unbilled tail of a schedule after the terms changed.
    ///
    /// Only the future is touched — rows already billed are history and are never rewritten, which
    /// is what keeps an invoice from silently disagreeing with the schedule that produced it.
    /// </summary>
    public async Task RebuildScheduleAsync(Guid agreementId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == agreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        if (agreement.Plan is null) return;

        var future = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreementId && !s.IsBilled && s.DueOn > now.Date)
            .ToListAsync();

        var promoLeft = agreement.PromotionalPeriodsRemaining;

        foreach (var row in future.OrderBy(r => r.DueOn))
        {
            // Fees and one-offs are not repriced by a plan change.
            if (row.ChargeKind is not (ChargeKind.MembershipDues or ChargeKind.Instalment)) continue;

            var amount = promoLeft > 0 ? agreement.PromotionalPrice ?? agreement.Price : agreement.Price;
            if (promoLeft > 0) promoLeft--;

            if (row.Amount != amount)
            {
                row.OriginalAmount ??= row.Amount;
                row.Amount = amount;
                row.TaxAmount = Math.Round(amount * agreement.TaxPercent / 100m, 2);
                row.AdjustmentNote = "Repriced after a plan change";
                row.StampUpdated(userId);
            }
        }

        var nextDue = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreementId && !s.IsBilled && !s.IsSkipped)
            .OrderBy(s => s.DueOn)
            .Select(s => (DateTime?)s.DueOn)
            .FirstOrDefaultAsync();

        agreement.NextBillingOn = nextDue;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == agreement.MemberId);
        if (member is not null) member.NextBillingOn = nextDue;

        await db.SaveChangesAsync();
    }

    // ── Runs ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// One billing run.
    ///
    /// A preview computes everything and writes nothing but the run record itself, so a manager
    /// can see exactly what four thousand members are about to be charged before it happens.
    /// A real run then does it for real, one member at a time, committing as it goes.
    /// </summary>
    public async Task<BillingRunDto> StartRunAsync(StartBillingRunDto request, Guid userId)
    {
        var now = DateTime.UtcNow;
        var billingDate = request.BillingDate.Date;

        var run = new BillingRun
        {
            RunNumber = await numbering.NextBillingRunNumberAsync(now),
            BillingDate = billingDate,
            ClubId = request.ClubId,
            PlanId = request.PlanId,
            IsPreview = request.PreviewOnly,
            Status = request.PreviewOnly ? BillingRunStatus.Previewing : BillingRunStatus.Running,
            StartedAt = now,
            RunByUserId = userId,
        }.StampNew(tenant, userId);

        db.BillingRuns.Add(run);
        await db.SaveChangesAsync();

        var due = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => !s.IsBilled && !s.IsSkipped && s.DueOn <= billingDate)
            .WhereIf(request.ClubId is not null, s => s.ClubId == request.ClubId)
            .Include(s => s.Agreement).ThenInclude(a => a!.Member)
            .Include(s => s.Agreement).ThenInclude(a => a!.Plan)
            .OrderBy(s => s.MemberId).ThenBy(s => s.DueOn)
            .ToListAsync();

        if (request.PlanId is not null)
            due = [.. due.Where(s => s.Agreement?.PlanId == request.PlanId)];

        run.TotalScheduled = due.Count;

        // Grouped per member so one member with three due charges gets one invoice, not three.
        foreach (var group in due.GroupBy(s => s.MemberId))
        {
            try
            {
                await BillMemberAsync(run, [.. group], request.PreviewOnly, request.CollectPayments, billingDate, userId);
            }
            catch (Exception ex)
            {
                run.Errors++;
                db.BillingRunLines.Add(new BillingRunLine
                {
                    BillingRunId = run.Id,
                    MemberId = group.Key,
                    Amount = group.Sum(s => s.Amount + s.TaxAmount),
                    Outcome = "Error",
                    Message = ex.Message,
                }.StampNew(tenant, userId));

                await db.SaveChangesAsync();
            }
        }

        run.CompletedAt = DateTime.UtcNow;
        run.Status = run.Errors > 0 ? BillingRunStatus.CompletedWithErrors : BillingRunStatus.Completed;
        if (run.Errors > 0) run.ErrorSummary = $"{run.Errors} member(s) could not be billed. See the run lines.";

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(run);
    }

    public async Task<BillingRunDto?> GetRunAsync(Guid runId)
    {
        var run = await db.BillingRuns.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == runId);
        if (run is null) return null;

        var dto = FitnessMapper.ToDto(run);
        if (run.ClubId is not null)
        {
            dto.ClubName = await db.Clubs.ForTenant(tenant)
                .Where(c => c.Id == run.ClubId).Select(c => c.Name).FirstOrDefaultAsync();
        }

        return dto;
    }

    public async Task<PaginatedResponse<BillingRunLineDto>> GetRunLinesAsync(
        Guid runId, string? outcome, PaginationParams pagination)
    {
        var query = db.BillingRunLines.ForTenant(tenant)
            .Where(l => l.BillingRunId == runId)
            .WhereIf(!string.IsNullOrWhiteSpace(outcome), l => l.Outcome == outcome);

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(l => l.Outcome).ThenBy(l => l.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var memberIds = page.Select(l => l.MemberId).Distinct().ToList();
        var members = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName, m.MemberNumber })
            .ToListAsync();

        var items = page.Select(l =>
        {
            var dto = FitnessMapper.ToDto(l);
            var m = members.FirstOrDefault(x => x.Id == l.MemberId);
            dto.MemberName = m?.Name;
            dto.MemberNumber = m?.MemberNumber;
            return dto;
        }).ToList();

        return PaginatedResponse<BillingRunLineDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<PaginatedResponse<BillingRunDto>> ListRunsAsync(
        Guid? clubId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.BillingRuns.ForTenant(tenant)
            .WhereIf(clubId is not null, r => r.ClubId == clubId)
            .WhereIf(from is not null, r => r.BillingDate >= from)
            .WhereIf(to is not null, r => r.BillingDate <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(r => r.BillingDate).ThenByDescending(r => r.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<BillingRunDto>.Ok(
                   [.. page.Select(FitnessMapper.ToDto)],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    /// <summary>
    /// The nightly run, if the tenant has it switched on.
    ///
    /// Guarded against running twice on the same date: a scheduler that fires again after a
    /// restart must not bill the club's whole membership a second time.
    /// </summary>
    public async Task<BillingRunDto?> RunScheduledBillingAsync()
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        if (settings?.AutoRunBilling != true) return null;

        var today = DateTime.UtcNow.Date;

        var alreadyRan = await db.BillingRuns.ForTenant(tenant)
            .AnyAsync(r => r.BillingDate == today && !r.IsPreview && r.IsAutomatic
                        && r.Status != BillingRunStatus.Failed);

        if (alreadyRan) return null;

        var run = await StartRunAsync(new StartBillingRunDto
        {
            BillingDate = today,
            PreviewOnly = false,
            CollectPayments = true,
        }, Guid.Empty);

        var entity = await db.BillingRuns.ForTenant(tenant).FirstAsync(r => r.Id == run.Id);
        entity.IsAutomatic = true;
        await db.SaveChangesAsync();

        return run;
    }

    // ── Invoices ─────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<InvoiceSummaryDto>> ListInvoicesAsync(
        Guid? clubId, Guid? memberId, InvoiceStatus? status, DateTime? from, DateTime? to,
        bool overdueOnly, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.Invoices.ForTenant(tenant)
            .Include(i => i.Member)
            .Include(i => i.Lines.Where(l => !l.IsDeleted))
            .WhereIf(clubId is not null, i => i.ClubId == clubId)
            .WhereIf(memberId is not null, i => i.MemberId == memberId)
            .WhereIf(status is not null, i => i.Status == status)
            .WhereIf(from is not null, i => i.IssuedOn >= from)
            .WhereIf(to is not null, i => i.IssuedOn <= to)
            .WhereIf(overdueOnly, i => i.BalanceDue > 0 && i.DueOn < now);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(i => i.IssuedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var items = page.Select(i =>
        {
            var dto = FitnessMapper.ToSummary(i, now);
            dto.ClubName = clubNames.GetValueOrDefault(i.ClubId);
            return dto;
        }).ToList();

        return PaginatedResponse<InvoiceSummaryDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<InvoiceDetailDto?> GetInvoiceAsync(Guid invoiceId)
    {
        var now = DateTime.UtcNow;

        var invoice = await db.Invoices.ForTenant(tenant)
            .Include(i => i.Member)
            .Include(i => i.Lines.Where(l => !l.IsDeleted))
            .Include(i => i.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice is null) return null;

        var dto = FitnessMapper.ToDetail(invoice, now);

        dto.ClubName = await db.Clubs.ForTenant(tenant)
            .Where(c => c.Id == invoice.ClubId).Select(c => c.Name).FirstOrDefaultAsync();

        dto.CreditNotes = [.. (await db.CreditNotes.ForTenant(tenant)
            .Where(c => c.InvoiceId == invoiceId)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        if (invoice.AgreementId is not null)
        {
            dto.AgreementNumber = await db.Agreements.ForTenant(tenant)
                .Where(a => a.Id == invoice.AgreementId).Select(a => a.AgreementNumber).FirstOrDefaultAsync();
        }

        return dto;
    }

    public async Task<InvoiceDetailDto> CreateAdHocInvoiceAsync(
        Guid memberId, Guid clubId, List<InvoiceLineDto> lines, Guid userId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new FitnessSettings();

        var invoice = new FitnessInvoice
        {
            InvoiceNumber = await numbering.NextInvoiceNumberAsync(now),
            MemberId = memberId,
            ClubId = clubId,
            Status = InvoiceStatus.Issued,
            IssuedOn = now,
            DueOn = now.Date.AddDays(settings.InvoiceGraceDays),
            CurrencyCode = club.CurrencyCode,
        }.StampNew(tenant, userId);

        var order = 0;
        foreach (var line in lines)
        {
            var lineTotal = line.LineTotal != 0 ? line.LineTotal : line.Quantity * line.UnitPrice - line.DiscountAmount;
            var tax = line.TaxAmount != 0 ? line.TaxAmount : Math.Round(lineTotal * line.TaxPercent / 100m, 2);

            db.InvoiceLines.Add(new FitnessInvoiceLine
            {
                InvoiceId = invoice.Id,
                ChargeKind = line.ChargeKind,
                LineDescription = line.LineDescription,
                PeriodStart = line.PeriodStart,
                PeriodEnd = line.PeriodEnd,
                Quantity = line.Quantity == 0 ? 1 : line.Quantity,
                UnitPrice = line.UnitPrice,
                DiscountAmount = line.DiscountAmount,
                TaxPercent = line.TaxPercent,
                TaxAmount = tax,
                LineTotal = lineTotal,
                ProrationExplanation = line.ProrationExplanation,
                PlanId = line.PlanId,
                DisplayOrder = order++,
            }.StampNew(tenant, userId));

            invoice.Subtotal += lineTotal;
            invoice.DiscountTotal += line.DiscountAmount;
            invoice.TaxTotal += tax;
        }

        invoice.Total = invoice.Subtotal + invoice.TaxTotal;
        invoice.BalanceDue = invoice.Total;

        db.Invoices.Add(invoice);

        PostLedger(member, LedgerEntryKind.Charge, invoice.Total,
            $"Invoice {invoice.InvoiceNumber}", invoice.Id, null, userId);

        await db.SaveChangesAsync();
        return (await GetInvoiceAsync(invoice.Id))!;
    }

    public async Task<InvoiceDetailDto> CancelInvoiceAsync(Guid invoiceId, string reason, Guid userId)
    {
        var invoice = await db.Invoices.ForTenant(tenant)
            .Include(i => i.Member)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.AmountPaid > 0)
            throw new InvalidOperationException(
                "This invoice has been paid against. Raise a credit note or a refund instead of cancelling it.");

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.BalanceDue = 0;
        invoice.Notes = $"{invoice.Notes}\nCancelled: {reason}".Trim();
        invoice.StampUpdated(userId);

        if (invoice.Member is not null)
        {
            PostLedger(invoice.Member, LedgerEntryKind.Adjustment, -invoice.Total,
                $"Invoice {invoice.InvoiceNumber} cancelled — {reason}", invoice.Id, null, userId);
        }

        // Put the schedule rows back so they bill again next time.
        var schedules = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.InvoiceId == invoiceId)
            .ToListAsync();

        foreach (var s in schedules)
        {
            s.IsBilled = false;
            s.InvoiceId = null;
            s.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return (await GetInvoiceAsync(invoiceId))!;
    }

    // ── Payments ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Taking money.
    ///
    /// Applies oldest-first when no specific invoice is named, because that is what both a member
    /// and an auditor expect, and it is what keeps the arrears ageing honest. The idempotency key
    /// is what stops a double-tapped Take Payment button charging twice.
    /// </summary>
    public async Task<TakePaymentResultDto> TakePaymentAsync(TakePaymentDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        if (request.Amount <= 0)
            throw new InvalidOperationException("A payment has to be more than zero.");

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await db.Payments.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey);

            if (existing is not null)
            {
                return new TakePaymentResultDto
                {
                    PaymentId = existing.Id,
                    PaymentNumber = existing.PaymentNumber,
                    AmountTaken = existing.Amount,
                    Status = existing.Status,
                    RemainingBalance = (await db.Members.ForTenant(tenant)
                        .Where(m => m.Id == existing.MemberId).Select(m => m.AccountBalance).FirstOrDefaultAsync()),
                };
            }
        }

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        // A card payment goes through the provider seam; cash and transfers are simply recorded.
        var outcome = request.Method == PaymentMethod.Card && request.PaymentMethodRefId is not null
            ? await provider.CollectAsync(request.PaymentMethodRefId.Value, request.Amount, club.CurrencyCode)
            : new PaymentOutcome(true, request.ProviderReference, request.AuthorisationCode, null, null);

        var payment = new FitnessPayment
        {
            PaymentNumber = await numbering.NextPaymentNumberAsync(now),
            MemberId = request.MemberId,
            InvoiceId = request.InvoiceId,
            ClubId = request.ClubId,
            Method = request.Method,
            Status = outcome.Succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            Amount = request.Amount,
            CurrencyCode = club.CurrencyCode,
            ReceivedOn = now,
            SettledOn = outcome.Succeeded ? now : null,
            ProviderReference = outcome.Reference ?? request.ProviderReference,
            AuthorisationCode = outcome.AuthCode ?? request.AuthorisationCode,
            CardBrand = request.CardBrand,
            CardLastFour = request.CardLastFour,
            FailureReason = outcome.FailureReason,
            FailureMessage = outcome.FailureMessage,
            CashSessionId = request.CashSessionId,
            TakenByStaffId = userId == Guid.Empty ? null : userId,
            IdempotencyKey = request.IdempotencyKey,
            Notes = request.Notes,
        }.StampNew(tenant, userId);

        db.Payments.Add(payment);

        var result = new TakePaymentResultDto
        {
            PaymentId = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            Status = payment.Status,
        };

        if (!outcome.Succeeded)
        {
            await db.SaveChangesAsync();
            result.AmountTaken = 0;
            result.RemainingBalance = member.AccountBalance;
            return result;
        }

        result.AmountTaken = request.Amount;
        result.ChangeDue = request.Method == PaymentMethod.Cash && request.AmountTendered is not null
            ? Math.Max(0, request.AmountTendered.Value - request.Amount)
            : 0;

        // ── Apply it ─────────────────────────────────────────────────────────

        var remaining = request.Amount;

        var targets = request.InvoiceId is not null
            ? await db.Invoices.ForTenant(tenant).Where(i => i.Id == request.InvoiceId).ToListAsync()
            : await db.Invoices.ForTenant(tenant)
                .Where(i => i.MemberId == request.MemberId && i.BalanceDue > 0
                         && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.WrittenOff)
                .OrderBy(i => i.DueOn)
                .ToListAsync();

        foreach (var invoice in targets)
        {
            if (remaining <= 0) break;

            var applied = Math.Min(remaining, invoice.BalanceDue);
            invoice.AmountPaid += applied;
            invoice.BalanceDue -= applied;
            remaining -= applied;

            if (invoice.BalanceDue <= 0)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidOn = now;

                // Paying an invoice closes its collection case; nothing else needs to notice.
                if (invoice.DunningCaseId is not null)
                    await CloseDunningCaseAsync(invoice.DunningCaseId.Value, applied, userId);
            }
            else
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }

            invoice.StampUpdated(userId);
            result.AppliedTo.Add(FitnessMapper.ToSummary(invoice, now));
        }

        // Anything left over becomes credit rather than vanishing.
        if (remaining > 0)
        {
            member.CreditBalance += remaining;
            await UpsertCreditBalanceAsync(member.Id, remaining, club.CurrencyCode, userId);
        }

        PostLedger(member, LedgerEntryKind.Payment, -request.Amount,
            $"Payment {payment.PaymentNumber} ({request.Method})", request.InvoiceId, payment.Id, userId);

        // Was the member locked out over this balance? If so, say so — it is the most useful
        // thing the desk can tell them.
        var wasBlocked = await db.MemberAlerts.ForTenant(tenant)
            .AnyAsync(a => a.MemberId == member.Id
                        && a.Kind == MemberAlertKind.OutstandingBalance && a.BlocksAccess);

        if (request.CashSessionId is not null)
        {
            var session = await db.CashSessions.ForTenant(tenant)
                .FirstOrDefaultAsync(s => s.Id == request.CashSessionId);

            if (session is not null)
            {
                if (request.Method == PaymentMethod.Cash) session.CashSales += request.Amount;
                else if (request.Method == PaymentMethod.Card) session.CardSales += request.Amount;
                else session.OtherSales += request.Amount;

                session.TransactionCount++;
                session.ExpectedCash = session.OpeningFloat + session.CashSales - session.Refunds
                                     + session.PaidIn - session.PaidOut - session.Drops;

                db.CashMovements.Add(new CashMovement
                {
                    CashSessionId = session.Id,
                    Kind = CashMovementKind.Sale,
                    Amount = request.Method == PaymentMethod.Cash ? request.Amount : 0,
                    Reason = $"Payment {payment.PaymentNumber}",
                    Reference = payment.PaymentNumber,
                    PaymentId = payment.Id,
                    StaffId = userId == Guid.Empty ? null : userId,
                }.StampNew(tenant, userId));
            }
        }

        await db.SaveChangesAsync();

        result.RemainingBalance = member.AccountBalance;
        result.AccessRestored = wasBlocked && member.AccountBalance <= club.AccessBalanceThreshold;

        return result;
    }

    public async Task<PaginatedResponse<PaymentDto>> ListPaymentsAsync(
        Guid? clubId, Guid? memberId, PaymentStatus? status, DateTime? from, DateTime? to,
        PaginationParams pagination)
    {
        var query = db.Payments.ForTenant(tenant)
            .Include(p => p.Invoice)
            .WhereIf(clubId is not null, p => p.ClubId == clubId)
            .WhereIf(memberId is not null, p => p.MemberId == memberId)
            .WhereIf(status is not null, p => p.Status == status)
            .WhereIf(from is not null, p => p.ReceivedOn >= from)
            .WhereIf(to is not null, p => p.ReceivedOn <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(p => p.ReceivedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var memberIds = page.Select(p => p.MemberId).Distinct().ToList();
        var names = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        var items = page.Select(p =>
        {
            var dto = FitnessMapper.ToDto(p);
            dto.MemberName = names.GetValueOrDefault(p.MemberId);
            return dto;
        }).ToList();

        return PaginatedResponse<PaymentDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<List<PaymentMethodRefDto>> GetPaymentMethodsAsync(Guid memberId)
    {
        var now = DateTime.UtcNow;

        var methods = await db.PaymentMethods.ForTenant(tenant)
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.IsDefault).ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        return [.. methods.Select(m => FitnessMapper.ToDto(m, now))];
    }

    /// <summary>
    /// Saving a payment method.
    ///
    /// Refuses anything that looks like a card number outright. There is no code path in this
    /// module that accepts a PAN, and the check exists so that a caller which tries — a
    /// well-meaning integration, a mis-mapped field — fails loudly rather than quietly writing
    /// card data into a database that is not built to hold it.
    /// </summary>
    public async Task<PaymentMethodRefDto> SavePaymentMethodAsync(SavePaymentMethodDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        if (LooksLikeCardNumber(request.ProviderToken) || LooksLikeCardNumber(request.CardLastFour))
            throw new InvalidOperationException(
                "This endpoint stores a gateway token, never a card number. Tokenise the card with the " +
                "payment provider first and send the token.");

        if (request.MakeDefault)
        {
            var others = await db.PaymentMethods.ForTenant(tenant)
                .Where(p => p.MemberId == request.MemberId && p.IsDefault)
                .ToListAsync();

            foreach (var other in others) other.IsDefault = false;
        }

        var method = new PaymentMethodRef
        {
            MemberId = request.MemberId,
            Method = request.Method,
            ProviderToken = request.ProviderToken,
            ProviderName = request.ProviderName,
            CardBrand = request.CardBrand,
            CardLastFour = request.CardLastFour,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            BankName = request.BankName,
            AccountLastFour = request.AccountLastFour,
            AccountHolderName = request.AccountHolderName,
            IsDefault = request.MakeDefault,
        }.StampNew(tenant, userId);

        method.IsExpiringSoon = method.ExpiryYear is not null && method.ExpiryMonth is not null
            && new DateTime(method.ExpiryYear.Value, method.ExpiryMonth.Value, 1).AddMonths(1) <= now.AddMonths(2);

        db.PaymentMethods.Add(method);

        if (!string.IsNullOrWhiteSpace(request.MandateReference))
        {
            db.Mandates.Add(new PaymentMandate
            {
                MemberId = request.MemberId,
                PaymentMethodRefId = method.Id,
                MandateReference = request.MandateReference,
                SignedOn = now,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(method, now);
    }

    public async Task DeletePaymentMethodAsync(Guid id, Guid userId)
    {
        var method = await db.PaymentMethods.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Payment method not found.");

        var inUse = await db.Agreements.ForTenant(tenant)
            .AnyAsync(a => a.PaymentMethodRefId == id && a.Status == AgreementStatus.Active);

        if (inUse)
            throw new InvalidOperationException(
                "This is the payment method on a live membership. Add a replacement before removing it.");

        method.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ── Credits, refunds, write-offs ─────────────────────────────────────────

    public async Task<CreditNoteDto> IssueCreditNoteAsync(IssueCreditNoteDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        var invoice = request.InvoiceId is null
            ? null
            : await db.Invoices.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == request.InvoiceId);

        if (invoice is not null && request.Amount > invoice.BalanceDue + invoice.AmountPaid)
            throw new InvalidOperationException("A credit note cannot exceed the invoice it credits.");

        var note = new CreditNote
        {
            CreditNoteNumber = await numbering.NextCreditNoteNumberAsync(now),
            MemberId = request.MemberId,
            InvoiceId = request.InvoiceId,
            ClubId = invoice?.ClubId ?? member.HomeClubId,
            Amount = request.Amount,
            CurrencyCode = invoice?.CurrencyCode ?? "USD",
            IssuedOn = now,
            Reason = request.Reason,
            AppliedToBalance = request.AppliedToBalance,
            ApprovedByUserId = userId,
        }.StampNew(tenant, userId);

        db.CreditNotes.Add(note);

        if (invoice is not null && !request.AppliedToBalance)
        {
            invoice.BalanceDue = Math.Max(0, invoice.BalanceDue - request.Amount);
            if (invoice.BalanceDue == 0) invoice.Status = InvoiceStatus.Paid;
            invoice.StampUpdated(userId);
        }
        else
        {
            member.CreditBalance += request.Amount;
            await UpsertCreditBalanceAsync(member.Id, request.Amount, note.CurrencyCode, userId);
        }

        PostLedger(member, LedgerEntryKind.CreditNote, -request.Amount,
            $"Credit note {note.CreditNoteNumber} — {request.Reason}", request.InvoiceId, null, userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(note);
    }

    public async Task<RefundDto> IssueRefundAsync(IssueRefundDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        var payment = request.PaymentId is null
            ? null
            : await db.Payments.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == request.PaymentId);

        if (payment is not null && request.Amount > payment.Amount - payment.RefundedAmount)
            throw new InvalidOperationException(
                $"Only {payment.Amount - payment.RefundedAmount:0.00} of that payment is still refundable.");

        var outcome = request.ToOriginalMethod && payment?.ProviderReference is not null
            ? await provider.RefundAsync(payment.ProviderReference, request.Amount)
            : new PaymentOutcome(true, null, null, null, null);

        var refund = new Refund
        {
            RefundNumber = await numbering.NextRefundNumberAsync(now),
            MemberId = request.MemberId,
            PaymentId = request.PaymentId,
            InvoiceId = request.InvoiceId,
            ClubId = payment?.ClubId ?? member.HomeClubId,
            Amount = request.Amount,
            CurrencyCode = payment?.CurrencyCode ?? "USD",
            Method = payment?.Method ?? PaymentMethod.Card,
            Status = outcome.Succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            RequestedOn = now,
            ProcessedOn = outcome.Succeeded ? now : null,
            Reason = request.Reason,
            ToOriginalMethod = request.ToOriginalMethod,
            ProviderReference = outcome.Reference,
            ApprovedByUserId = userId,
            CashSessionId = request.CashSessionId,
        }.StampNew(tenant, userId);

        db.Refunds.Add(refund);

        if (outcome.Succeeded)
        {
            if (payment is not null)
            {
                payment.RefundedAmount += request.Amount;
                payment.Status = payment.RefundedAmount >= payment.Amount
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;
                payment.StampUpdated(userId);
            }

            if (request.InvoiceId is not null)
            {
                var invoice = await db.Invoices.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == request.InvoiceId);
                if (invoice is not null)
                {
                    invoice.AmountRefunded += request.Amount;
                    invoice.Status = InvoiceStatus.Refunded;
                    invoice.StampUpdated(userId);
                }
            }

            if (!request.ToOriginalMethod)
            {
                member.CreditBalance += request.Amount;
                await UpsertCreditBalanceAsync(member.Id, request.Amount, refund.CurrencyCode, userId);
            }

            PostLedger(member, LedgerEntryKind.Refund, request.Amount,
                $"Refund {refund.RefundNumber} — {request.Reason}", request.InvoiceId, request.PaymentId, userId);

            if (request.CashSessionId is not null)
            {
                var session = await db.CashSessions.ForTenant(tenant)
                    .FirstOrDefaultAsync(s => s.Id == request.CashSessionId);

                if (session is not null)
                {
                    session.Refunds += request.Amount;
                    session.ExpectedCash -= request.Amount;

                    db.CashMovements.Add(new CashMovement
                    {
                        CashSessionId = session.Id,
                        Kind = CashMovementKind.Refund,
                        Amount = -request.Amount,
                        Reason = request.Reason,
                        Reference = refund.RefundNumber,
                    }.StampNew(tenant, userId));
                }
            }
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(refund);
    }

    public async Task<WriteOffDto> WriteOffAsync(
        Guid memberId, Guid? invoiceId, decimal amount, string reason, Guid userId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");

        var writeOff = new WriteOff
        {
            MemberId = memberId,
            InvoiceId = invoiceId,
            ClubId = member.HomeClubId,
            Amount = amount,
            WrittenOffOn = now,
            Reason = reason,
            ApprovedByUserId = userId,
        }.StampNew(tenant, userId);

        db.WriteOffs.Add(writeOff);

        if (invoiceId is not null)
        {
            var invoice = await db.Invoices.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice is not null)
            {
                invoice.BalanceDue = Math.Max(0, invoice.BalanceDue - amount);
                invoice.Status = invoice.BalanceDue == 0 ? InvoiceStatus.WrittenOff : invoice.Status;
                invoice.StampUpdated(userId);
            }
        }

        PostLedger(member, LedgerEntryKind.WriteOff, -amount,
            $"Written off — {reason}", invoiceId, null, userId);

        await db.SaveChangesAsync();

        return new WriteOffDto
        {
            Id = writeOff.Id,
            MemberId = memberId,
            MemberName = FitnessMapper.FullName(member),
            InvoiceId = invoiceId,
            Amount = amount,
            WrittenOffOn = now,
            Reason = reason,
        };
    }

    // ── Deferred revenue ─────────────────────────────────────────────────────

    /// <summary>
    /// The roll-forward: what was owed at the start, what was added, what was earned, what is left.
    ///
    /// This is the report an accountant asks for, and the reason it exists is that a gym's cash
    /// and its revenue are genuinely different numbers. Collecting a year in January is not a
    /// year of income in January, and a club that reports it that way has a very confusing
    /// February.
    /// </summary>
    public async Task<DeferredRevenueReportDto> GetDeferredRevenueAsync(Guid? clubId, DateTime from, DateTime to)
    {
        var schedules = await db.DeferredRevenue.ForTenant(tenant)
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .Where(s => s.ServiceStart <= to)
            .ToListAsync();

        var entries = await db.DeferredRevenueEntries.ForTenant(tenant)
            .Where(e => schedules.Select(s => s.Id).Contains(e.ScheduleId))
            .ToListAsync();

        var opening = schedules
            .Where(s => s.ServiceStart < from)
            .Sum(s => s.TotalAmount - entries.Where(e => e.ScheduleId == s.Id && e.RecognisedOn < from).Sum(e => e.Amount));

        var additions = schedules.Where(s => s.CreatedAt >= from && s.CreatedAt <= to).Sum(s => s.TotalAmount);
        var recognised = entries.Where(e => e.RecognisedOn >= from && e.RecognisedOn <= to).Sum(e => e.Amount);

        var report = new DeferredRevenueReportDto
        {
            PeriodStart = from,
            PeriodEnd = to,
            OpeningBalance = opening,
            Additions = additions,
            Recognised = recognised,
            ClosingBalance = opening + additions - recognised,
        };

        report.ByCategory = [.. schedules
            .GroupBy(s => s.Basis)
            .Select(g => new DeferredRevenueLineDto
            {
                Label = g.Key switch
                {
                    RevenueRecognitionBasis.StraightLine => "Memberships (earned over time)",
                    RevenueRecognitionBasis.OnConsumption => "Session packs (earned per session)",
                    _ => "Immediate",
                },
                Additions = g.Where(s => s.CreatedAt >= from && s.CreatedAt <= to).Sum(s => s.TotalAmount),
                Recognised = entries.Where(e => g.Select(s => s.Id).Contains(e.ScheduleId)
                                             && e.RecognisedOn >= from && e.RecognisedOn <= to).Sum(e => e.Amount),
                Closing = g.Sum(s => s.RemainingAmount),
                ScheduleCount = g.Count(),
            })];

        report.ByMonth = [.. entries
            .Where(e => e.RecognisedOn >= from && e.RecognisedOn <= to)
            .GroupBy(e => new { e.RecognisedOn.Year, e.RecognisedOn.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new DeferredRevenueLineDto
            {
                Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Recognised = g.Sum(e => e.Amount),
            })];

        return report;
    }

    /// <summary>
    /// Releases the revenue that time has earned. Runs nightly.
    ///
    /// Straight-line schedules earn a day at a time; consumption-based ones are released by the
    /// booking engine as credits are spent, so they are deliberately not touched here.
    /// </summary>
    public async Task<int> RecogniseDueRevenueAsync()
    {
        var today = DateTime.UtcNow.Date;
        var released = 0;

        var open = await db.DeferredRevenue.ForTenant(tenant)
            .Where(s => !s.IsClosed && s.Basis == RevenueRecognitionBasis.StraightLine && s.ServiceStart <= today)
            .ToListAsync();

        foreach (var schedule in open)
        {
            var totalDays = Math.Max(1, (schedule.ServiceEnd - schedule.ServiceStart).Days);
            var elapsedDays = Math.Min(totalDays, Math.Max(0, (today - schedule.ServiceStart).Days));

            var shouldHaveEarned = Math.Round(schedule.TotalAmount * elapsedDays / totalDays, 2);
            var toRecognise = shouldHaveEarned - schedule.RecognisedAmount;

            if (toRecognise <= 0.005m) continue;

            db.DeferredRevenueEntries.Add(new DeferredRevenueEntry
            {
                ScheduleId = schedule.Id,
                RecognisedOn = today,
                Amount = toRecognise,
                Trigger = "Time elapsed",
            }.StampNew(tenant));

            schedule.RecognisedAmount += toRecognise;
            schedule.RemainingAmount = schedule.TotalAmount - schedule.RecognisedAmount;

            if (schedule.RemainingAmount <= 0.005m || today >= schedule.ServiceEnd)
            {
                schedule.IsClosed = true;
                schedule.ClosedOn = today;
                schedule.RemainingAmount = 0;
            }

            released++;
        }

        await db.SaveChangesAsync();
        return released;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Bills one member: their due charges become one invoice, and collection is attempted once.
    ///
    /// The schedule rows are marked billed inside the same save as the invoice, which is what
    /// makes the whole run resumable — a crash leaves either both written or neither.
    /// </summary>
    private async Task BillMemberAsync(
        BillingRun run, List<BillingSchedule> due, bool previewOnly, bool collect,
        DateTime billingDate, Guid userId)
    {
        var now = DateTime.UtcNow;
        var memberId = due[0].MemberId;
        var agreement = due[0].Agreement;
        var member = agreement?.Member
            ?? await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId);

        if (member is null)
        {
            run.Skipped++;
            return;
        }

        // A frozen or cancelled membership does not bill, even if a stale row says otherwise.
        if (member.Status is MemberStatus.Frozen or MemberStatus.Cancelled or MemberStatus.Expired)
        {
            run.Skipped++;
            db.BillingRunLines.Add(new BillingRunLine
            {
                BillingRunId = run.Id,
                MemberId = memberId,
                Amount = due.Sum(d => d.Amount + d.TaxAmount),
                Outcome = "Skipped",
                Message = $"Membership is {member.Status.ToString().ToLower()}",
            }.StampNew(tenant, userId));

            await db.SaveChangesAsync();
            return;
        }

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new FitnessSettings();
        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == due[0].ClubId);
        var total = due.Sum(d => d.Amount + d.TaxAmount);

        if (previewOnly)
        {
            run.InvoicesCreated++;
            run.TotalBilled += total;

            db.BillingRunLines.Add(new BillingRunLine
            {
                BillingRunId = run.Id,
                MemberId = memberId,
                AgreementId = agreement?.Id,
                Amount = total,
                Outcome = "Would bill",
                Message = string.Join("; ", due.Select(d => $"{d.ChargeKind}: {d.Amount:0.00}")),
            }.StampNew(tenant, userId));

            await db.SaveChangesAsync();
            return;
        }

        // ── Invoice ──────────────────────────────────────────────────────────

        var invoice = new FitnessInvoice
        {
            InvoiceNumber = await numbering.NextInvoiceNumberAsync(now),
            MemberId = memberId,
            ClubId = due[0].ClubId,
            AgreementId = agreement?.Id,
            CorporateAccountId = agreement?.CorporateAccountId,
            PayerMemberId = agreement?.PayerMemberId,
            ThirdPartyPayerId = agreement?.ThirdPartyPayerId,
            Status = InvoiceStatus.Issued,
            IssuedOn = now,
            DueOn = billingDate.AddDays(settings.InvoiceGraceDays),
            CurrencyCode = due[0].CurrencyCode,
            BillingRunId = run.Id,
        }.StampNew(tenant, userId);

        var order = 0;
        foreach (var charge in due.OrderBy(d => d.DueOn))
        {
            db.InvoiceLines.Add(new FitnessInvoiceLine
            {
                InvoiceId = invoice.Id,
                ChargeKind = charge.ChargeKind,
                LineDescription = DescribeCharge(charge, agreement?.Plan?.Name),
                PeriodStart = charge.PeriodStart,
                PeriodEnd = charge.PeriodEnd,
                Quantity = 1,
                UnitPrice = charge.Amount,
                TaxAmount = charge.TaxAmount,
                TaxPercent = charge.Amount == 0 ? 0 : Math.Round(charge.TaxAmount / charge.Amount * 100m, 4),
                LineTotal = charge.Amount,
                ProrationExplanation = charge.AdjustmentNote,
                PlanId = agreement?.PlanId,
                DisplayOrder = order++,
            }.StampNew(tenant, userId));

            invoice.Subtotal += charge.Amount;
            invoice.TaxTotal += charge.TaxAmount;

            charge.IsBilled = true;
            charge.InvoiceId = invoice.Id;
            charge.StampUpdated(userId);
        }

        invoice.Total = invoice.Subtotal + invoice.TaxTotal;
        invoice.BalanceDue = invoice.Total;
        db.Invoices.Add(invoice);

        PostLedger(member, LedgerEntryKind.Charge, invoice.Total,
            $"Invoice {invoice.InvoiceNumber}", invoice.Id, null, userId);

        // Deferred revenue for anything paid ahead of the service it buys.
        if (agreement?.Plan is not null && due.Any(d => d.ChargeKind is ChargeKind.MembershipDues or ChargeKind.Instalment))
        {
            var serviceRow = due.First(d => d.ChargeKind is ChargeKind.MembershipDues or ChargeKind.Instalment);

            db.DeferredRevenue.Add(new DeferredRevenueSchedule
            {
                MemberId = memberId,
                AgreementId = agreement.Id,
                InvoiceId = invoice.Id,
                ClubId = invoice.ClubId,
                Basis = agreement.Plan.RecognitionBasis,
                TotalAmount = serviceRow.Amount,
                RemainingAmount = serviceRow.Amount,
                CurrencyCode = invoice.CurrencyCode,
                ServiceStart = serviceRow.PeriodStart,
                ServiceEnd = serviceRow.PeriodEnd,
                RevenueAccountId = agreement.Plan.RevenueAccountId,
                DeferredAccountId = agreement.Plan.DeferredRevenueAccountId,
            }.StampNew(tenant, userId));
        }

        run.InvoicesCreated++;
        run.TotalBilled += invoice.Total;

        if (agreement is not null)
        {
            agreement.PeriodsBilled++;
            agreement.LastBilledOn = billingDate;

            if (agreement.PromotionalPeriodsRemaining > 0) agreement.PromotionalPeriodsRemaining--;

            agreement.NextBillingOn = await db.BillingSchedules.ForTenant(tenant)
                .Where(s => s.AgreementId == agreement.Id && !s.IsBilled && !s.IsSkipped)
                .OrderBy(s => s.DueOn)
                .Select(s => (DateTime?)s.DueOn)
                .FirstOrDefaultAsync();

            member.NextBillingOn = agreement.NextBillingOn;
            agreement.StampUpdated(userId);
        }

        await db.SaveChangesAsync();

        // ── Collection ───────────────────────────────────────────────────────

        if (!collect)
        {
            db.BillingRunLines.Add(new BillingRunLine
            {
                BillingRunId = run.Id,
                MemberId = memberId,
                AgreementId = agreement?.Id,
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Outcome = "Billed",
            }.StampNew(tenant, userId));

            await db.SaveChangesAsync();
            return;
        }

        var method = agreement?.PaymentMethodRefId is not null
            ? await db.PaymentMethods.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == agreement.PaymentMethodRefId)
            : await db.PaymentMethods.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.MemberId == memberId && p.IsDefault && p.IsActive);

        // A member who pays at the desk is not a failure — they simply have no method on file.
        if (method is null)
        {
            db.BillingRunLines.Add(new BillingRunLine
            {
                BillingRunId = run.Id,
                MemberId = memberId,
                AgreementId = agreement?.Id,
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Outcome = "Billed",
                Message = "No payment method on file — pays at the desk",
            }.StampNew(tenant, userId));

            await db.SaveChangesAsync();
            return;
        }

        var result = await TakePaymentAsync(new TakePaymentDto
        {
            MemberId = memberId,
            ClubId = invoice.ClubId,
            InvoiceId = invoice.Id,
            Amount = invoice.Total,
            Method = method.Method,
            PaymentMethodRefId = method.Id,
            CardBrand = method.CardBrand,
            CardLastFour = method.CardLastFour,
            IdempotencyKey = $"run:{run.Id}:inv:{invoice.Id}",
        }, userId);

        if (result.Status == PaymentStatus.Succeeded)
        {
            run.PaymentsCollected++;
            run.TotalCollected += invoice.Total;

            method.ConsecutiveFailures = 0;

            db.BillingRunLines.Add(new BillingRunLine
            {
                BillingRunId = run.Id,
                MemberId = memberId,
                AgreementId = agreement?.Id,
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Outcome = "Collected",
            }.StampNew(tenant, userId));
        }
        else
        {
            run.PaymentsFailed++;
            run.TotalFailed += invoice.Total;

            var payment = await db.Payments.ForTenant(tenant)
                .Where(p => p.Id == result.PaymentId).FirstOrDefaultAsync();

            var reason = payment?.FailureReason ?? PaymentFailureReason.Other;

            method.ConsecutiveFailures++;
            method.LastFailedOn = now;

            invoice.Status = InvoiceStatus.InDunning;

            db.BillingRunLines.Add(new BillingRunLine
            {
                BillingRunId = run.Id,
                MemberId = memberId,
                AgreementId = agreement?.Id,
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Outcome = "Failed",
                FailureReason = reason,
                Message = payment?.FailureMessage,
            }.StampNew(tenant, userId));

            await db.SaveChangesAsync();
            await OpenDunningCaseAsync(invoice, member, reason, club, userId);
        }

        await db.SaveChangesAsync();
    }

    private void PostLedger(
        Member member, LedgerEntryKind kind, decimal amount, string description,
        Guid? invoiceId, Guid? paymentId, Guid userId)
    {
        member.AccountBalance += amount;

        db.Ledger.Add(new MemberLedgerEntry
        {
            MemberId = member.Id,
            ClubId = member.HomeClubId,
            Kind = kind,
            OccurredAt = DateTime.UtcNow,
            Amount = amount,
            BalanceAfter = member.AccountBalance,
            EntryDescription = description,
            InvoiceId = invoiceId,
            PaymentId = paymentId,
        }.StampNew(tenant, userId));

        member.StampUpdated(userId);
    }

    private async Task UpsertCreditBalanceAsync(Guid memberId, decimal delta, string currency, Guid userId)
    {
        var balance = await db.CreditBalances.ForTenant(tenant).FirstOrDefaultAsync(b => b.MemberId == memberId);

        if (balance is null)
        {
            balance = new MemberCreditBalance
            {
                MemberId = memberId,
                CurrencyCode = currency,
            }.StampNew(tenant, userId);

            db.CreditBalances.Add(balance);
        }

        balance.Balance += delta;
        balance.LastMovementOn = DateTime.UtcNow;
    }

    private async Task OpenDunningCaseAsync(
        FitnessInvoice invoice, Member member, PaymentFailureReason reason, FitnessClub? club, Guid userId)
    {
        var now = DateTime.UtcNow;

        var existing = await db.DunningCases.ForTenant(tenant)
            .FirstOrDefaultAsync(c => c.InvoiceId == invoice.Id && c.Status == DunningCaseStatus.Open);

        if (existing is not null) return;

        var policy = await db.DunningPolicies.ForTenant(tenant)
            .Include(p => p.Steps.Where(s => !s.IsDeleted))
            .Where(p => p.IsActive && (p.Id == club!.DefaultDunningPolicyId || p.IsDefault))
            .OrderByDescending(p => p.Id == club!.DefaultDunningPolicyId)
            .FirstOrDefaultAsync();

        if (policy is null) return;

        var firstStep = policy.Steps.OrderBy(s => s.StepNumber).FirstOrDefault();

        var dunningCase = new DunningCase
        {
            CaseNumber = await numbering.NextDunningCaseNumberAsync(now),
            MemberId = member.Id,
            InvoiceId = invoice.Id,
            AgreementId = invoice.AgreementId,
            ClubId = invoice.ClubId,
            DunningPolicyId = policy.Id,
            Status = DunningCaseStatus.Open,
            OpenedOn = now,
            AmountOutstanding = invoice.BalanceDue,
            InitialFailureReason = reason,
            CurrentStep = 0,
            NextStepDueOn = firstStep is null ? null : now.Date.AddDays(firstStep.DelayDays),
        }.StampNew(tenant, userId);

        db.DunningCases.Add(dunningCase);
        invoice.DunningCaseId = dunningCase.Id;

        if (member.Status == MemberStatus.Active) member.Status = MemberStatus.PastDue;
    }

    private async Task CloseDunningCaseAsync(Guid caseId, decimal recovered, Guid userId)
    {
        var dunningCase = await db.DunningCases.ForTenant(tenant)
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.Id == caseId);

        if (dunningCase is null || dunningCase.Status != DunningCaseStatus.Open) return;

        dunningCase.Status = DunningCaseStatus.Recovered;
        dunningCase.ClosedOn = DateTime.UtcNow;
        dunningCase.AmountRecovered += recovered;
        dunningCase.AmountOutstanding = Math.Max(0, dunningCase.AmountOutstanding - recovered);
        dunningCase.StampUpdated(userId);

        db.DunningEvents.Add(new DunningEvent
        {
            DunningCaseId = caseId,
            StepNumber = dunningCase.CurrentStep,
            Action = DunningAction.Retry,
            OccurredAt = DateTime.UtcNow,
            Succeeded = true,
            Detail = "Payment received — case closed",
            AmountCollected = recovered,
        }.StampNew(tenant, userId));

        if (dunningCase.Member?.Status == MemberStatus.PastDue)
            dunningCase.Member.Status = MemberStatus.Active;
    }

    private static string DescribeCharge(BillingSchedule charge, string? planName) => charge.ChargeKind switch
    {
        ChargeKind.MembershipDues or ChargeKind.Instalment =>
            $"{planName ?? "Membership"} — {charge.PeriodStart:d MMM} to {charge.PeriodEnd:d MMM yyyy}",
        ChargeKind.JoiningFee => "Joining and admin fees",
        ChargeKind.ProRata => $"Part period — {charge.PeriodStart:d MMM} to {charge.PeriodEnd:d MMM yyyy}",
        ChargeKind.AnnualMaintenanceFee => $"Annual maintenance fee {charge.PeriodStart:yyyy}",
        ChargeKind.FreezeFee => "Freeze fee",
        ChargeKind.SessionPackage => "Session package",
        _ => charge.ChargeKind.ToString(),
    };

    /// <summary>
    /// A crude Luhn-and-length check, used to refuse anything that looks like a real card number.
    ///
    /// Not security — it is a guardrail. The point is that a mis-wired integration fails on the
    /// first request instead of quietly filling a column with PANs.
    /// </summary>
    private static bool LooksLikeCardNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length is < 13 or > 19) return false;

        var sum = 0;
        var alternate = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alternate) { n *= 2; if (n > 9) n -= 9; }
            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}

/// <summary>
/// The seam a real payment gateway drops into.
///
/// Deliberately narrow: authorise, collect, refund, and manage a stored instrument. Everything
/// else about a gateway — three-D secure, webhooks, settlement files — is per-provider and lives
/// in the adapter, not here.
/// </summary>
public interface IPaymentProvider
{
    Task<PaymentOutcome> CollectAsync(Guid paymentMethodRefId, decimal amount, string currency);
    Task<PaymentOutcome> RefundAsync(string providerReference, decimal amount);
}

/// <summary>What a provider says happened. No card data, in either direction.</summary>
public record PaymentOutcome(
    bool Succeeded,
    string? Reference,
    string? AuthCode,
    PaymentFailureReason? FailureReason,
    string? FailureMessage);

/// <summary>
/// The provider used until a real gateway is wired in.
///
/// It succeeds. That is deliberate rather than lazy: the whole billing engine — schedules, runs,
/// deferred revenue, ledger posting, dunning — is exercised end to end from day one, and swapping
/// in a real adapter changes one registration and nothing else. Recording a payment is the
/// product's job; taking one is the gateway's.
/// </summary>
public class ManualPaymentProvider : IPaymentProvider
{
    public Task<PaymentOutcome> CollectAsync(Guid paymentMethodRefId, decimal amount, string currency)
        => Task.FromResult(new PaymentOutcome(true, $"manual:{Guid.NewGuid():N}"[..24], null, null, null));

    public Task<PaymentOutcome> RefundAsync(string providerReference, decimal amount)
        => Task.FromResult(new PaymentOutcome(true, $"manual-refund:{Guid.NewGuid():N}"[..24], null, null, null));
}
