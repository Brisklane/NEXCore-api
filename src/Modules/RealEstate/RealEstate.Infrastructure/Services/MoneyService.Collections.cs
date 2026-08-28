using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The second half of <see cref="MoneyService"/>: demands, receipts and allocation, cheques, the
/// ledger and statement, and the collections desk with its dunning ladder.
/// </summary>
public partial class MoneyService
{
    // ═══ Demands ═════════════════════════════════════════════════════════════

    /// <summary>
    /// The demand run.
    ///
    /// Always previewable, always idempotent: an instalment that already carries a demand is
    /// skipped with a reason rather than demanded twice. A customer who receives the same demand
    /// twice stops trusting every number the developer sends them.
    /// </summary>
    public async Task<DemandBatchDto> RunDemandsAsync(DemandRunRequestDto dto, Guid userId)
    {
        var settings = await SettingsAsync();
        var today = Today;
        var horizon = dto.DueDateTo ?? today.AddDays(settings.DemandLeadDays);
        var from = dto.DueDateFrom ?? DateOnly.MinValue;

        var batch = new DemandBatch
        {
            Reference = await numbering.NextMasterCodeAsync(Db.DemandBatches, "DMB"),
            ProjectId = dto.ProjectId,
            ProjectNodeId = dto.ProjectNodeId,
            ProjectMilestoneId = dto.ProjectMilestoneId,
            RunDate = today,
            DueDateFrom = dto.DueDateFrom,
            DueDateTo = horizon,
            RunType = dto.RunType,
            IsDryRun = dto.IsDryRun,
            StartedAt = DateTime.UtcNow,
            RunByUserId = userId,
        }.StampNew(Tenant, userId);

        // Milestone runs are driven by certification, not by a date: only a *certified* milestone
        // releases its instalments, which is the whole point of the certificate.
        ProjectMilestone? milestone = null;
        if (dto.ProjectMilestoneId is not null)
        {
            milestone = await Db.ProjectMilestones.ForCompany(Tenant)
                .FirstOrDefaultAsync(m => m.Id == dto.ProjectMilestoneId)
                ?? throw new InvalidOperationException("That milestone does not exist.");

            if (milestone.Status != MilestoneStatus.Certified)
                throw new InvalidOperationException(
                    "This milestone has not been certified yet. Certify it first — the certificate is " +
                    "what entitles the demand.");
        }

        var candidates = await (
            from i in Db.Instalments.ForCompany(Tenant)
            join p in Db.PaymentPlans.ForCompany(Tenant) on i.PaymentPlanId equals p.Id
            join b in Db.Bookings.ForCompany(Tenant) on i.BookingId equals b.Id
            where p.IsCurrent
                && i.DemandId == null
                && !i.IsOnHold
                && i.Balance > 0
                && i.Status != InstalmentStatus.Cancelled
                && i.Status != InstalmentStatus.Waived
                && i.Status != InstalmentStatus.Restructured
                && b.Status != BookingStatus.Cancelled
            select new { Instalment = i, Booking = b })
            .WhereIf(dto.ProjectId.HasValue, x => x.Booking.ProjectId == dto.ProjectId)
            .WhereIf(dto.ProjectMilestoneId.HasValue, x => x.Instalment.ProjectMilestoneId == dto.ProjectMilestoneId)
            .WhereIf(!dto.ProjectMilestoneId.HasValue,
                x => x.Instalment.DueDate != null && x.Instalment.DueDate >= from && x.Instalment.DueDate <= horizon)
            .ToListAsync();

        var excluded = dto.ExcludeBookingIds.ToHashSet();
        candidates = candidates.Where(c => !excluded.Contains(c.Booking.Id)).ToList();

        batch.CandidateCount = candidates.Count;
        batch.SkippedCount = excluded.Count;

        var partyIds = candidates.Select(c => c.Booking.PrimaryApplicantPartyId).Distinct().ToList();
        var addresses = await Db.PartyAddresses.ForCompany(Tenant)
            .Where(a => partyIds.Contains(a.PartyId) && a.IsMailingAddress)
            .ToListAsync();

        var preview = new List<Demand>();

        // One demand per booking, carrying every instalment that has come due — a customer with
        // three overdue lines gets one letter, not three.
        foreach (var group in candidates.GroupBy(c => c.Booking.Id))
        {
            var booking = group.First().Booking;
            var instalments = group.Select(g => g.Instalment).OrderBy(i => i.DueDate).ToList();

            var dueDate = milestone is not null
                ? today.AddDays(settings.DemandLeadDays)
                : instalments.Min(i => i.DueDate) ?? today;

            var principal = instalments.Sum(i => i.Amount);
            var tax = instalments.Sum(i => i.TaxAmount);
            var surcharge = instalments.Sum(i => i.SurchargeAccrued - i.SurchargePaid - i.SurchargeWaived);

            // Anything already overdue on this booking that is not in this run's window.
            var arrears = await Db.Instalments.ForCompany(Tenant)
                .Where(i => i.BookingId == booking.Id && i.Balance > 0 && i.DueDate != null && i.DueDate < today)
                .Where(i => !instalments.Select(x => x.Id).Contains(i.Id))
                .SumAsync(i => (decimal?)i.Balance) ?? 0m;

            var address = addresses.FirstOrDefault(a => a.PartyId == booking.PrimaryApplicantPartyId);

            var demand = new Demand
            {
                DemandNumber = dto.IsDryRun ? "(preview)" : await numbering.NextDemandNumberAsync(DateTime.UtcNow),
                BookingId = booking.Id,
                PaymentPlanId = instalments[0].PaymentPlanId,
                DemandBatchId = batch.Id,
                PartyId = booking.PrimaryApplicantPartyId,
                Status = dto.IsDryRun ? DemandStatus.Draft : DemandStatus.Generated,
                IssuedOn = today,
                DueDate = dueDate,
                PrincipalAmount = principal,
                SurchargeAmount = surcharge,
                ArrearsAmount = arrears,
                TaxAmount = tax,
                TotalAmount = RealEstateMapper.Money(principal + tax + surcharge + arrears),
                Balance = RealEstateMapper.Money(principal + tax + surcharge + arrears),
                CurrencyCode = booking.CurrencyCode,
                MailingAddress = address is null ? null : RealEstateMapper.OneLineAddress(address),
                IsReminder = dto.RunType == "Reminder",
            }.StampNew(Tenant, userId);

            foreach (var instalment in instalments)
            {
                demand.Lines.Add(new DemandLine
                {
                    InstalmentId = instalment.Id,
                    Description = instalment.Label,
                    DueDate = instalment.DueDate,
                    Amount = instalment.Amount,
                    TaxAmount = instalment.TaxAmount,
                    Kind = LedgerEntryKind.Demand,
                    SortOrder = instalment.SequenceNumber,
                }.StampNew(Tenant, userId));
            }

            if (surcharge > 0m)
            {
                demand.Lines.Add(new DemandLine
                {
                    Description = "Late payment surcharge",
                    Amount = surcharge,
                    Kind = LedgerEntryKind.Surcharge,
                    SortOrder = 9998,
                }.StampNew(Tenant, userId));
            }

            if (arrears > 0m)
            {
                demand.Lines.Add(new DemandLine
                {
                    Description = "Arrears brought forward",
                    Amount = arrears,
                    Kind = LedgerEntryKind.Demand,
                    SortOrder = 9999,
                }.StampNew(Tenant, userId));
            }

            preview.Add(demand);
            batch.GeneratedCount++;
            batch.TotalAmount += demand.TotalAmount;

            if (dto.IsDryRun) continue;

            Db.Demands.Add(demand);

            foreach (var instalment in instalments)
            {
                instalment.DemandId = demand.Id;
                if (instalment.Status == InstalmentStatus.NotDue)
                    instalment.Status = dueDate <= today ? InstalmentStatus.Due : InstalmentStatus.NotDue;

                // A milestone-linked instalment has no date until its milestone is certified.
                instalment.DueDate ??= dueDate;
                instalment.StampUpdated(userId);
            }

            Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                PartyId = booking.PrimaryApplicantPartyId,
                BookingId = booking.Id,
                EntryDate = today,
                Kind = LedgerEntryKind.Demand,
                Description = $"Demand {demand.DemandNumber}",
                DebitAmount = principal + tax,
                CurrencyCode = booking.CurrencyCode,
                SourceDemandId = demand.Id,
            }.StampNew(Tenant, userId));
        }

        batch.CompletedAt = DateTime.UtcNow;
        batch.IsCompleted = true;

        if (!dto.IsDryRun)
        {
            Db.DemandBatches.Add(batch);

            if (milestone is not null)
            {
                milestone.DemandsRaised = true;
                milestone.StampUpdated(userId);
            }

            await Db.SaveChangesAsync();

            foreach (var bookingId in preview.Select(d => d.BookingId).Distinct())
                await RecalculateBookingTotalsAsync(bookingId, userId);

            await Db.SaveChangesAsync();

            if (dto.SendImmediately)
            {
                foreach (var demand in preview)
                    await SendDemandAsync(demand.Id, dto.Channels, userId);
            }
        }

        var result = MapBatch(batch);
        result.Preview = await MapDemandListAsync(preview.Take(50).ToList());
        return result;
    }

    private static DemandBatchDto MapBatch(DemandBatch b) => new()
    {
        Id = b.Id,
        Reference = b.Reference,
        RunDate = b.RunDate,
        DueDateFrom = b.DueDateFrom,
        DueDateTo = b.DueDateTo,
        RunType = b.RunType,
        IsDryRun = b.IsDryRun,
        CandidateCount = b.CandidateCount,
        GeneratedCount = b.GeneratedCount,
        SkippedCount = b.SkippedCount,
        FailedCount = b.FailedCount,
        SentCount = b.SentCount,
        TotalAmount = b.TotalAmount,
        StartedAt = b.StartedAt,
        CompletedAt = b.CompletedAt,
        ErrorSummary = b.ErrorSummary,
        IsCompleted = b.IsCompleted,
    };

    public async Task<PaginatedResponse<DemandBatchDto>> GetDemandBatchesAsync(ListQueryDto query)
    {
        var q = Db.DemandBatches.ForCompany(Tenant)
            .WhereIf(query.ProjectId.HasValue, b => b.ProjectId == query.ProjectId)
            .OrderByDescending(b => b.StartedAt);

        var projects = await ProjectNamesAsync([query.ProjectId]);

        return await PageAsync(q, query, b =>
        {
            var dto = MapBatch(b);
            dto.ProjectName = b.ProjectId is null ? null : projects.GetValueOrDefault(b.ProjectId.Value);
            return dto;
        });
    }

    public async Task<PaginatedResponse<DemandListItemDto>> GetDemandsAsync(
        ListQueryDto query, Guid? bookingId, DemandStatus? status)
    {
        var q = Db.Demands.ForCompany(Tenant)
            .WhereIf(bookingId.HasValue, d => d.BookingId == bookingId)
            .WhereIf(status.HasValue, d => d.Status == status)
            .WhereIf(query.FromDate.HasValue, d => d.DueDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, d => d.DueDate <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), d => d.DemandNumber.Contains(query.Search!))
            .OrderByDescending(d => d.IssuedOn);

        return await PageAsync(q, query, MapDemandListAsync);
    }

    private async Task<List<DemandListItemDto>> MapDemandListAsync(List<Demand> demands)
    {
        if (demands.Count == 0) return [];

        var bookingIds = demands.Select(d => d.BookingId).Distinct().ToList();
        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => bookingIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Reference, b.PrimaryApplicantPartyId, b.UnitId, b.ProjectId })
            .ToDictionaryAsync(b => b.Id, b => b);

        var names = await PartyNamesAsync(bookings.Values.Select(b => b.PrimaryApplicantPartyId));
        var projects = await ProjectNamesAsync(bookings.Values.Select(b => (Guid?)b.ProjectId));

        var unitIds = bookings.Values.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).ToList();
        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var phones = await Db.Parties.ForCompany(Tenant)
            .Where(p => bookings.Values.Select(b => b.PrimaryApplicantPartyId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PrimaryPhone);

        var today = Today;

        return demands.Select(d =>
        {
            var booking = bookings.GetValueOrDefault(d.BookingId);

            return new DemandListItemDto
            {
                Id = d.Id,
                DemandNumber = d.DemandNumber,
                BookingId = d.BookingId,
                BookingReference = booking?.Reference ?? "—",
                ApplicantName = booking is null ? "—" : names.GetValueOrDefault(booking.PrimaryApplicantPartyId, "—"),
                ApplicantPhone = booking is null ? null : phones.GetValueOrDefault(booking.PrimaryApplicantPartyId),
                UnitNumber = booking?.UnitId is null ? null : units.GetValueOrDefault(booking.UnitId.Value),
                ProjectName = booking is null ? "—" : projects.GetValueOrDefault(booking.ProjectId, "—"),
                Status = d.Status,
                IssuedOn = d.IssuedOn,
                DueDate = d.DueDate,
                DaysOverdue = d.Balance > 0 ? RealEstateMapper.DaysOverdue(d.DueDate, today) : 0,
                PrincipalAmount = d.PrincipalAmount,
                SurchargeAmount = d.SurchargeAmount,
                ArrearsAmount = d.ArrearsAmount,
                TaxAmount = d.TaxAmount,
                TotalAmount = d.TotalAmount,
                PaidAmount = d.PaidAmount,
                Balance = d.Balance,
                CurrencyCode = d.CurrencyCode,
                IsReminder = d.IsReminder,
                ReminderNumber = d.ReminderNumber,
                SentAt = d.SentAt,
                EmailSent = d.EmailSent,
                SmsSent = d.SmsSent,
                WhatsAppSent = d.WhatsAppSent,
                PostSent = d.PostSent,
                DocumentUrl = d.DocumentUrl,
            };
        }).ToList();
    }

    public async Task<DemandDetailDto?> GetDemandAsync(Guid id)
    {
        var demand = await Db.Demands.ForCompany(Tenant).Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id);
        if (demand is null) return null;

        var summary = (await MapDemandListAsync([demand]))[0];

        return new DemandDetailDto
        {
            Id = summary.Id,
            DemandNumber = summary.DemandNumber,
            BookingId = summary.BookingId,
            BookingReference = summary.BookingReference,
            ApplicantName = summary.ApplicantName,
            ApplicantPhone = summary.ApplicantPhone,
            UnitNumber = summary.UnitNumber,
            ProjectName = summary.ProjectName,
            Status = summary.Status,
            IssuedOn = summary.IssuedOn,
            DueDate = summary.DueDate,
            DaysOverdue = summary.DaysOverdue,
            PrincipalAmount = summary.PrincipalAmount,
            SurchargeAmount = summary.SurchargeAmount,
            ArrearsAmount = summary.ArrearsAmount,
            TaxAmount = summary.TaxAmount,
            TotalAmount = summary.TotalAmount,
            PaidAmount = summary.PaidAmount,
            Balance = summary.Balance,
            CurrencyCode = summary.CurrencyCode,
            IsReminder = summary.IsReminder,
            ReminderNumber = summary.ReminderNumber,
            SentAt = summary.SentAt,
            EmailSent = summary.EmailSent,
            SmsSent = summary.SmsSent,
            WhatsAppSent = summary.WhatsAppSent,
            PostSent = summary.PostSent,
            DocumentUrl = summary.DocumentUrl,
            DemandBatchId = demand.DemandBatchId,
            MailingAddress = demand.MailingAddress,
            AcknowledgedAt = demand.AcknowledgedAt,
            Lines = demand.Lines.OrderBy(l => l.SortOrder).Select(l => new DemandLineDto
            {
                Id = l.Id,
                InstalmentId = l.InstalmentId,
                Description = l.Description ?? string.Empty,
                DueDate = l.DueDate,
                Amount = l.Amount,
                TaxAmount = l.TaxAmount,
                Kind = l.Kind,
                SortOrder = l.SortOrder,
            }).ToList(),
        };
    }

    public async Task<DemandDetailDto> SendDemandAsync(Guid id, List<NotificationChannel> channels, Guid userId)
    {
        var demand = await Db.Demands.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new InvalidOperationException("That demand does not exist.");

        if (demand.Status == DemandStatus.Cancelled)
            throw new InvalidOperationException("This demand has been cancelled and cannot be sent.");

        var effective = channels.Count > 0 ? channels : [NotificationChannel.Email, NotificationChannel.WhatsApp];

        foreach (var channel in effective.Distinct())
        {
            switch (channel)
            {
                case NotificationChannel.Email: demand.EmailSent = true; break;
                case NotificationChannel.Sms: demand.SmsSent = true; break;
                case NotificationChannel.WhatsApp: demand.WhatsAppSent = true; break;
                case NotificationChannel.Post: demand.PostSent = true; break;
            }

            await QueueNotificationAsync(
                "demand_issued",
                $"Payment due — {demand.DemandNumber}",
                $"{demand.TotalAmount:N0} {demand.CurrencyCode} due on {demand.DueDate:d MMM yyyy}",
                $"/portal/customer/demands/{demand.Id}",
                recipientPartyId: demand.PartyId,
                entityType: "Demand", entityId: demand.Id,
                severity: AlertSeverity.Info);
        }

        demand.SentAt = DateTime.UtcNow;
        demand.SentVia = effective[0];
        demand.Status = DemandStatus.Sent;
        demand.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetDemandAsync(id))!;
    }

    public async Task CancelDemandAsync(Guid id, Guid reasonCodeId, Guid userId)
    {
        var demand = await Db.Demands.ForCompany(Tenant).Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new InvalidOperationException("That demand does not exist.");

        if (demand.PaidAmount > 0m)
            throw new InvalidOperationException("Money has already been received against this demand, so it cannot be cancelled.");

        demand.Status = DemandStatus.Cancelled;
        demand.CancelReasonCodeId = reasonCodeId;
        demand.StampUpdated(userId);

        // Release the instalments so a corrected demand can be raised.
        var instalmentIds = demand.Lines.Where(l => l.InstalmentId.HasValue).Select(l => l.InstalmentId!.Value).ToList();
        var instalments = await Db.Instalments.ForCompany(Tenant).Where(i => instalmentIds.Contains(i.Id)).ToListAsync();
        foreach (var i in instalments)
        {
            i.DemandId = null;
            i.StampUpdated(userId);
        }

        Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
        {
            PartyId = demand.PartyId,
            BookingId = demand.BookingId,
            EntryDate = Today,
            Kind = LedgerEntryKind.Reversal,
            Description = $"Demand {demand.DemandNumber} cancelled",
            CreditAmount = demand.PrincipalAmount + demand.TaxAmount,
            CurrencyCode = demand.CurrencyCode,
            SourceDemandId = demand.Id,
            IsReversal = true,
        }.StampNew(Tenant, userId));

        await WriteAuditNoteAsync("Demand", demand.Id, "DocumentReissue", reasonCodeId, userId,
            before: demand.DemandNumber, amountImpact: demand.TotalAmount, entityReference: demand.DemandNumber);

        await Db.SaveChangesAsync();
        await RecalculateBookingTotalsAsync(demand.BookingId, userId);
        await Db.SaveChangesAsync();
    }

    // ═══ Receipts & allocation ═══════════════════════════════════════════════

    public async Task<AllocationPreviewDto> PreviewAllocationAsync(ReceiptCreateDto dto)
        => await BuildAllocationAsync(dto, null, Guid.Empty, preview: true);

    /// <summary>
    /// Takes money in.
    ///
    /// The allocation engine runs first and its result is shown to the cashier before anything is
    /// written, because "where did my payment go?" is the second-most-common customer question and
    /// the answer has to be on the receipt.
    /// </summary>
    public async Task<ReceiptDetailDto> CreateReceiptAsync(ReceiptCreateDto dto, Guid userId)
    {
        if (dto.Amount <= 0m) throw new InvalidOperationException("Enter an amount greater than zero.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var settings = await SettingsAsync();
        var currency = dto.CurrencyCode ?? await CurrencyAsync();

        var receipt = new Receipt
        {
            ReceiptNumber = await numbering.NextReceiptNumberAsync(DateTime.UtcNow),
            PartyId = dto.PartyId ?? Guid.Empty,
            BookingId = dto.BookingId,
            TenancyId = dto.TenancyId,
            MaintenanceBillId = dto.MaintenanceBillId,
            ServiceChargeInvoiceId = dto.ServiceChargeInvoiceId,
            DealId = dto.DealId,
            ClientBuildContractId = dto.ClientBuildContractId,
            ProjectId = dto.ProjectId,
            OfficeId = dto.OfficeId,
            Status = ReceiptStatus.Posted,
            ReceivedOn = dto.ReceivedOn == default ? Today : dto.ReceivedOn,
            Amount = dto.Amount,
            CurrencyCode = currency,
            ExchangeRate = dto.ExchangeRate <= 0 ? 1m : dto.ExchangeRate,
            Instrument = dto.Instrument,
            BankName = dto.BankName,
            BranchName = dto.BranchName,
            InstrumentNumber = dto.InstrumentNumber,
            InstrumentDate = dto.InstrumentDate,
            TransactionReference = dto.TransactionReference,
            BankAccountId = dto.BankAccountId,
            CardBrand = dto.CardBrand,
            CardLastFour = dto.CardLastFour,
            AuthorisationCode = dto.AuthorisationCode,
            Narration = dto.Narration,
            ReceivedByUserId = userId,
            PostedAt = DateTime.UtcNow,
            PostedByUserId = userId,
        }.StampNew(Tenant, userId);

        // Resolve the payer from the booking when the cashier did not name one.
        if (receipt.PartyId == Guid.Empty && dto.BookingId is not null)
        {
            var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId);
            if (booking is not null)
            {
                receipt.PartyId = booking.PrimaryApplicantPartyId;
                receipt.ProjectId ??= booking.ProjectId;
            }
        }

        // A cheque is money *promised*, not money received. It sits pending until it clears, and
        // nothing is allocated against it in the meantime.
        ChequeRecord? cheque = null;
        if (dto.Instrument is PaymentInstrument.Cheque or PaymentInstrument.DemandDraft or PaymentInstrument.PayOrder)
        {
            cheque = new ChequeRecord
            {
                PartyId = receipt.PartyId,
                BookingId = dto.BookingId,
                TenancyId = dto.TenancyId,
                ReceiptId = receipt.Id,
                ChequeNumber = dto.InstrumentNumber ?? "—",
                BankName = dto.BankName,
                BranchName = dto.BranchName,
                Amount = dto.Amount,
                CurrencyCode = currency,
                ChequeDate = dto.InstrumentDate ?? receipt.ReceivedOn,
                ReceivedOn = receipt.ReceivedOn,
                IsPostDated = dto.IsPostDated || (dto.InstrumentDate.HasValue && dto.InstrumentDate > Today),
                State = dto.IsPostDated || (dto.InstrumentDate.HasValue && dto.InstrumentDate > Today)
                    ? ChequeState.Pending
                    : ChequeState.Received,
            }.StampNew(Tenant, userId);

            Db.ChequeRecords.Add(cheque);
            receipt.ChequeRecordId = cheque.Id;
        }

        Db.Receipts.Add(receipt);

        var allocatable = cheque is null || cheque.State == ChequeState.Cleared;

        if (allocatable)
        {
            var allocation = await BuildAllocationAsync(dto, receipt, userId, preview: false);
            receipt.AllocatedAmount = allocation.Allocations.Sum(a => a.Amount);
            receipt.UnallocatedAmount = RealEstateMapper.Money(receipt.Amount - receipt.AllocatedAmount);
            receipt.EscrowAmount = allocation.EscrowPortion;
            receipt.FreeAmount = allocation.FreePortion;
            receipt.EscrowSplitApplied = settings.EscrowEnforced;

            if (receipt.UnallocatedAmount > 0m) receipt.Status = ReceiptStatus.OnAccount;
        }
        else
        {
            receipt.UnallocatedAmount = receipt.Amount;
            receipt.Status = ReceiptStatus.OnAccount;
        }

        await Db.SaveChangesAsync();

        if (dto.BookingId is not null)
        {
            await RecalculateBookingTotalsAsync(dto.BookingId.Value, userId);
            await Db.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return (await GetReceiptAsync(receipt.Id))!;
    }

    /// <summary>
    /// The allocation engine.
    ///
    /// Runs the configured order — surcharge first, or oldest first, or principal only — and
    /// records on every line which rule produced it. When the caller supplies manual allocations
    /// those win, and each line is flagged as an override with the reason attached.
    /// </summary>
    private async Task<AllocationPreviewDto> BuildAllocationAsync(
        ReceiptCreateDto dto, Receipt? receipt, Guid userId, bool preview)
    {
        var settings = await SettingsAsync();
        var result = new AllocationPreviewDto { Amount = dto.Amount, Rule = settings.DefaultAllocationOrder };

        if (dto.BookingId is null)
        {
            // Tenancy, maintenance and service-charge receipts allocate against their own charge,
            // which is a simpler path with no surcharge ladder behind it.
            result.Unallocated = dto.Amount;
            return result;
        }

        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == booking.ProjectId);

        var instalments = await Db.Instalments.ForCompany(Tenant)
            .Where(i => i.BookingId == booking.Id)
            .Where(i => i.Status != InstalmentStatus.Cancelled
                     && i.Status != InstalmentStatus.Waived
                     && i.Status != InstalmentStatus.Restructured)
            .OrderBy(i => i.DueDate ?? DateOnly.MaxValue).ThenBy(i => i.SequenceNumber)
            .ToListAsync();

        var remaining = dto.Amount;
        var lines = new List<ReceiptAllocationDto>();
        var order = settings.DefaultAllocationOrder;

        if (dto.ManualAllocations.Count > 0)
        {
            order = AllocationOrder.Manual;

            foreach (var manual in dto.ManualAllocations)
            {
                if (remaining <= 0m) break;

                var instalment = instalments.FirstOrDefault(i => i.Id == manual.InstalmentId);
                if (instalment is null) continue;

                var applied = Math.Min(manual.Amount, remaining);
                lines.Add(Apply(instalment, applied, manual.AppliedTo, true, dto.AllocationOverrideReason));
                remaining -= applied;
            }
        }
        else
        {
            // Surcharge first, where the policy says so — it stops the debt compounding.
            if (order == AllocationOrder.SurchargeFirstThenOldest)
            {
                foreach (var instalment in instalments)
                {
                    if (remaining <= 0m) break;

                    var surchargeDue = instalment.SurchargeAccrued - instalment.SurchargePaid - instalment.SurchargeWaived;
                    if (surchargeDue <= 0m) continue;

                    var applied = Math.Min(surchargeDue, remaining);
                    lines.Add(Apply(instalment, applied, LedgerEntryKind.Surcharge, false, null));
                    remaining -= applied;
                }
            }

            if (order != AllocationOrder.Manual)
            {
                foreach (var instalment in instalments.Where(i => i.Balance > 0))
                {
                    if (remaining <= 0m) break;

                    var applied = Math.Min(instalment.Balance, remaining);
                    lines.Add(Apply(instalment, applied, LedgerEntryKind.Demand, false, null));
                    remaining -= applied;
                }
            }

            // Anything left over clears surcharge that the first pass did not reach.
            if (order == AllocationOrder.OldestFirstThenSurcharge)
            {
                foreach (var instalment in instalments)
                {
                    if (remaining <= 0m) break;

                    var surchargeDue = instalment.SurchargeAccrued - instalment.SurchargePaid - instalment.SurchargeWaived;
                    if (surchargeDue <= 0m) continue;

                    var applied = Math.Min(surchargeDue, remaining);
                    lines.Add(Apply(instalment, applied, LedgerEntryKind.Surcharge, false, null));
                    remaining -= applied;
                }
            }
        }

        result.Rule = order;
        result.Allocations = lines;
        result.Unallocated = RealEstateMapper.Money(remaining);
        result.SurchargeCleared = lines.Where(l => l.AppliedTo == LedgerEntryKind.Surcharge).Sum(l => l.Amount);
        result.PrincipalCleared = lines.Where(l => l.AppliedTo == LedgerEntryKind.Demand).Sum(l => l.Amount);

        // Escrow is split now, on the money that was actually applied, and never reconstructed later.
        var escrowPercent = project?.EscrowPercent ?? settings.DefaultEscrowPercent;
        if (settings.EscrowEnforced && escrowPercent > 0m)
        {
            result.EscrowPortion = RealEstateMapper.Money(dto.Amount * escrowPercent / 100m);
            result.FreePortion = RealEstateMapper.Money(dto.Amount - result.EscrowPortion);
        }
        else
        {
            result.FreePortion = dto.Amount;
        }

        var paidAfter = booking.TotalPaid + result.PrincipalCleared;
        result.NewOutstanding = RealEstateMapper.Money(booking.TotalConsideration - paidAfter);
        result.NewCollectionPercent = RealEstateMapper.Percent(paidAfter, booking.TotalConsideration);
        result.NewNextDueDate = instalments.FirstOrDefault(i => i.Balance > 0 && i.DueDate != null)?.DueDate;

        if (preview) return result;

        // Escrow ledger, so the regulated balance moves with the money rather than after it.
        if (receipt is not null && result.EscrowPortion > 0m && booking.ProjectId != Guid.Empty)
        {
            var escrowAccount = await Db.ProjectBankAccounts.ForCompany(Tenant)
                .FirstOrDefaultAsync(a => a.ProjectId == booking.ProjectId && a.Kind == ProjectAccountKind.Escrow);

            if (escrowAccount is not null)
            {
                escrowAccount.Balance += result.EscrowPortion;
                escrowAccount.TotalCredited += result.EscrowPortion;

                Db.EscrowLedgerEntries.Add(new EscrowLedgerEntry
                {
                    ProjectBankAccountId = escrowAccount.Id,
                    ProjectId = booking.ProjectId,
                    EntryDate = receipt.ReceivedOn,
                    Kind = EscrowMovementKind.CollectionCredit,
                    Description = $"Receipt {receipt.ReceiptNumber} — {booking.Reference}",
                    CreditAmount = result.EscrowPortion,
                    RunningBalance = escrowAccount.Balance,
                    ReceiptId = receipt.Id,
                    BookingId = booking.Id,
                }.StampNew(Tenant, userId));
            }
        }

        if (receipt is not null)
        {
            Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                PartyId = receipt.PartyId,
                BookingId = booking.Id,
                EntryDate = receipt.ReceivedOn,
                Kind = LedgerEntryKind.Receipt,
                Description = $"Receipt {receipt.ReceiptNumber}",
                CreditAmount = dto.Amount,
                CurrencyCode = receipt.CurrencyCode,
                SourceReceiptId = receipt.Id,
            }.StampNew(Tenant, userId));
        }

        return result;

        ReceiptAllocationDto Apply(Instalment instalment, decimal amount, LedgerEntryKind kind, bool isOverride, string? reason)
        {
            if (!preview)
            {
                if (kind == LedgerEntryKind.Surcharge)
                {
                    instalment.SurchargePaid += amount;
                }
                else
                {
                    instalment.PaidAmount += amount;
                    instalment.Balance = RealEstateMapper.Money(instalment.TotalAmount - instalment.PaidAmount - instalment.WaivedAmount);
                    instalment.FirstPaidOn ??= receipt?.ReceivedOn ?? Today;

                    if (instalment.Balance <= 0m)
                    {
                        instalment.Status = InstalmentStatus.Paid;
                        instalment.SettledOn = receipt?.ReceivedOn ?? Today;
                    }
                    else instalment.Status = InstalmentStatus.PartiallyPaid;
                }

                instalment.StampUpdated(userId);

                if (receipt is not null)
                {
                    Db.ReceiptAllocations.Add(new ReceiptAllocation
                    {
                        ReceiptId = receipt.Id,
                        InstalmentId = instalment.Id,
                        DemandId = instalment.DemandId,
                        AppliedTo = kind,
                        Amount = amount,
                        AllocatedOn = receipt.ReceivedOn,
                        SortOrder = instalment.SequenceNumber,
                        AppliedRule = order,
                        IsManualOverride = isOverride,
                        OverrideReason = reason,
                    }.StampNew(Tenant, userId));
                }
            }

            return new ReceiptAllocationDto
            {
                InstalmentId = instalment.Id,
                Description = kind == LedgerEntryKind.Surcharge
                    ? $"Surcharge on {instalment.Label}"
                    : instalment.Label,
                DueDate = instalment.DueDate,
                AppliedTo = kind,
                Amount = amount,
                AllocatedOn = receipt?.ReceivedOn ?? Today,
                AppliedRule = order,
                IsManualOverride = isOverride,
                OverrideReason = reason,
                SortOrder = instalment.SequenceNumber,
            };
        }
    }

    public async Task<PaginatedResponse<ReceiptListItemDto>> GetReceiptsAsync(ReceiptSearchDto query)
    {
        var q = Db.Receipts.ForCompany(Tenant)
            .WhereIf(query.PartyId.HasValue, r => r.PartyId == query.PartyId)
            .WhereIf(query.BookingId.HasValue, r => r.BookingId == query.BookingId)
            .WhereIf(query.TenancyId.HasValue, r => r.TenancyId == query.TenancyId)
            .WhereIf(query.Instrument.HasValue, r => r.Instrument == query.Instrument)
            .WhereIf(query.Status.HasValue, r => r.Status == query.Status)
            .WhereIf(query.UnallocatedOnly == true, r => r.UnallocatedAmount > 0)
            .WhereIf(query.ClientMoneyOnly == true, r => r.IsClientMoney)
            .WhereIf(query.ProjectId.HasValue, r => r.ProjectId == query.ProjectId)
            .WhereIf(query.FromDate.HasValue, r => r.ReceivedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, r => r.ReceivedOn <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                r => r.ReceiptNumber.Contains(query.Search!) ||
                     (r.InstrumentNumber != null && r.InstrumentNumber.Contains(query.Search!)))
            .OrderByDescending(r => r.ReceivedOn).ThenByDescending(r => r.CreatedAt);

        return await PageAsync(q, query, MapReceiptListAsync);
    }

    private async Task<List<ReceiptListItemDto>> MapReceiptListAsync(List<Receipt> receipts)
    {
        if (receipts.Count == 0) return [];

        var names = await PartyNamesAsync(receipts.Select(r => r.PartyId));
        var projects = await ProjectNamesAsync(receipts.Select(r => r.ProjectId));

        var bookingIds = receipts.Where(r => r.BookingId.HasValue).Select(r => r.BookingId!.Value).Distinct().ToList();
        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant).Where(b => bookingIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Reference, b.UnitId })
                .ToDictionaryAsync(b => b.Id, b => b);

        var unitIds = bookings.Values.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).ToList();
        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var chequeIds = receipts.Where(r => r.ChequeRecordId.HasValue).Select(r => r.ChequeRecordId!.Value).ToList();
        var cheques = chequeIds.Count == 0
            ? []
            : await Db.ChequeRecords.ForCompany(Tenant).Where(c => chequeIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.State);

        return receipts.Select(r =>
        {
            var booking = r.BookingId is null ? null : bookings.GetValueOrDefault(r.BookingId.Value);

            return new ReceiptListItemDto
            {
                Id = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                Status = r.Status,
                ReceivedOn = r.ReceivedOn,
                PartyId = r.PartyId,
                PartyName = names.GetValueOrDefault(r.PartyId, "—"),
                BookingId = r.BookingId,
                BookingReference = booking?.Reference,
                TenancyId = r.TenancyId,
                MaintenanceBillId = r.MaintenanceBillId,
                UnitNumber = booking?.UnitId is null ? null : units.GetValueOrDefault(booking.UnitId.Value),
                ProjectName = r.ProjectId is null ? null : projects.GetValueOrDefault(r.ProjectId.Value),
                Amount = r.Amount,
                AllocatedAmount = r.AllocatedAmount,
                UnallocatedAmount = r.UnallocatedAmount,
                CurrencyCode = r.CurrencyCode,
                Instrument = r.Instrument,
                BankName = r.BankName,
                InstrumentNumber = r.InstrumentNumber,
                InstrumentDate = r.InstrumentDate,
                ChequeState = r.ChequeRecordId is null ? null : cheques.GetValueOrDefault(r.ChequeRecordId.Value),
                TransactionReference = r.TransactionReference,
                EscrowAmount = r.EscrowAmount,
                FreeAmount = r.FreeAmount,
                IsClientMoney = r.IsClientMoney,
                IsPrinted = r.IsPrinted,
                Narration = r.Narration,
            };
        }).ToList();
    }

    public async Task<ReceiptDetailDto?> GetReceiptAsync(Guid id)
    {
        var receipt = await Db.Receipts.ForCompany(Tenant)
            .Include(r => r.Allocations)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt is null) return null;

        var summary = (await MapReceiptListAsync([receipt]))[0];

        var instalmentIds = receipt.Allocations.Where(a => a.InstalmentId.HasValue)
            .Select(a => a.InstalmentId!.Value).Distinct().ToList();

        var instalments = instalmentIds.Count == 0
            ? []
            : await Db.Instalments.ForCompany(Tenant).Where(i => instalmentIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => new { i.Label, i.DueDate });

        var cheque = receipt.ChequeRecordId is null
            ? null
            : await Db.ChequeRecords.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == receipt.ChequeRecordId);

        return new ReceiptDetailDto
        {
            Id = summary.Id,
            ReceiptNumber = summary.ReceiptNumber,
            Status = summary.Status,
            ReceivedOn = summary.ReceivedOn,
            PartyId = summary.PartyId,
            PartyName = summary.PartyName,
            BookingId = summary.BookingId,
            BookingReference = summary.BookingReference,
            TenancyId = summary.TenancyId,
            UnitNumber = summary.UnitNumber,
            ProjectName = summary.ProjectName,
            Amount = summary.Amount,
            AllocatedAmount = summary.AllocatedAmount,
            UnallocatedAmount = summary.UnallocatedAmount,
            CurrencyCode = summary.CurrencyCode,
            Instrument = summary.Instrument,
            BankName = summary.BankName,
            BranchName = receipt.BranchName,
            InstrumentNumber = summary.InstrumentNumber,
            InstrumentDate = summary.InstrumentDate,
            ChequeState = summary.ChequeState,
            TransactionReference = summary.TransactionReference,
            EscrowAmount = summary.EscrowAmount,
            FreeAmount = summary.FreeAmount,
            IsClientMoney = summary.IsClientMoney,
            IsPrinted = summary.IsPrinted,
            Narration = summary.Narration,
            CardBrand = receipt.CardBrand,
            CardLastFour = receipt.CardLastFour,
            AuthorisationCode = receipt.AuthorisationCode,
            ExchangeRate = receipt.ExchangeRate,
            PostedAt = receipt.PostedAt,
            ReversedAt = receipt.ReversedAt,
            ReversalReason = receipt.ReversalNote,
            DocumentUrl = receipt.DocumentUrl,
            Cheque = cheque is null ? null : MapCheque(cheque, summary.PartyName, summary.BookingReference),
            Allocations = receipt.Allocations.OrderBy(a => a.SortOrder).Select(a => new ReceiptAllocationDto
            {
                Id = a.Id,
                InstalmentId = a.InstalmentId,
                Description = a.InstalmentId is null
                    ? a.AppliedTo.ToString()
                    : instalments.GetValueOrDefault(a.InstalmentId.Value)?.Label ?? "—",
                DueDate = a.InstalmentId is null ? null : instalments.GetValueOrDefault(a.InstalmentId.Value)?.DueDate,
                AppliedTo = a.AppliedTo,
                Amount = a.Amount,
                AllocatedOn = a.AllocatedOn,
                AppliedRule = a.AppliedRule,
                IsManualOverride = a.IsManualOverride,
                OverrideReason = a.OverrideReason,
                IsReversed = a.IsReversed,
                SortOrder = a.SortOrder,
            }).ToList(),
        };
    }

    public async Task<ReceiptDetailDto> ReverseReceiptAsync(Guid id, Guid reasonCodeId, string? note, Guid userId)
    {
        var receipt = await Db.Receipts.ForCompany(Tenant)
            .Include(r => r.Allocations)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("That receipt does not exist.");

        if (receipt.Status == ReceiptStatus.Reversed)
            throw new InvalidOperationException("This receipt has already been reversed.");

        await UnwindAllocationsAsync(receipt, userId);

        receipt.Status = ReceiptStatus.Reversed;
        receipt.ReversedAt = DateTime.UtcNow;
        receipt.ReversalReasonCodeId = reasonCodeId;
        receipt.ReversalNote = note;
        receipt.StampUpdated(userId);

        Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
        {
            PartyId = receipt.PartyId,
            BookingId = receipt.BookingId,
            EntryDate = Today,
            Kind = LedgerEntryKind.Reversal,
            Description = $"Receipt {receipt.ReceiptNumber} reversed",
            DebitAmount = receipt.Amount,
            CurrencyCode = receipt.CurrencyCode,
            SourceReceiptId = receipt.Id,
            IsReversal = true,
        }.StampNew(Tenant, userId));

        await WriteAuditNoteAsync("Receipt", receipt.Id, "AllocationOverride", reasonCodeId, userId,
            before: $"{receipt.Amount:N0} posted", after: "reversed",
            amountImpact: receipt.Amount, note: note, highRisk: true, entityReference: receipt.ReceiptNumber);

        await Db.SaveChangesAsync();

        if (receipt.BookingId is not null)
        {
            await RecalculateBookingTotalsAsync(receipt.BookingId.Value, userId);
            await Db.SaveChangesAsync();
        }

        return (await GetReceiptAsync(id))!;
    }

    /// <summary>Puts back what an allocation took, line by line, before a reversal or a re-allocation.</summary>
    private async Task UnwindAllocationsAsync(Receipt receipt, Guid userId)
    {
        var live = receipt.Allocations.Where(a => !a.IsReversed).ToList();
        var instalmentIds = live.Where(a => a.InstalmentId.HasValue).Select(a => a.InstalmentId!.Value).ToList();

        var instalments = instalmentIds.Count == 0
            ? []
            : await Db.Instalments.ForCompany(Tenant).Where(i => instalmentIds.Contains(i.Id)).ToListAsync();

        foreach (var allocation in live)
        {
            allocation.IsReversed = true;
            allocation.StampUpdated(userId);

            var instalment = instalments.FirstOrDefault(i => i.Id == allocation.InstalmentId);
            if (instalment is null) continue;

            if (allocation.AppliedTo == LedgerEntryKind.Surcharge)
            {
                instalment.SurchargePaid = Math.Max(0m, instalment.SurchargePaid - allocation.Amount);
            }
            else
            {
                instalment.PaidAmount = Math.Max(0m, instalment.PaidAmount - allocation.Amount);
                instalment.Balance = RealEstateMapper.Money(instalment.TotalAmount - instalment.PaidAmount - instalment.WaivedAmount);
                instalment.SettledOn = null;
                instalment.Status = instalment.PaidAmount > 0m
                    ? InstalmentStatus.PartiallyPaid
                    : instalment.DueDate is not null && instalment.DueDate < Today
                        ? InstalmentStatus.Overdue
                        : InstalmentStatus.Due;
            }

            instalment.StampUpdated(userId);
        }

        receipt.AllocatedAmount = 0m;
        receipt.UnallocatedAmount = receipt.Amount;
    }

    public async Task<ReceiptDetailDto> ReallocateAsync(
        Guid receiptId, List<ManualAllocationDto> allocations, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Re-allocating a receipt needs a reason.");

        var receipt = await Db.Receipts.ForCompany(Tenant)
            .Include(r => r.Allocations)
            .FirstOrDefaultAsync(r => r.Id == receiptId)
            ?? throw new InvalidOperationException("That receipt does not exist.");

        if (receipt.Status == ReceiptStatus.Reversed)
            throw new InvalidOperationException("A reversed receipt cannot be re-allocated.");

        if (allocations.Sum(a => a.Amount) > receipt.Amount)
            throw new InvalidOperationException("The allocations add up to more than the receipt.");

        await UnwindAllocationsAsync(receipt, userId);

        var dto = new ReceiptCreateDto
        {
            BookingId = receipt.BookingId,
            Amount = receipt.Amount,
            ReceivedOn = receipt.ReceivedOn,
            ManualAllocations = allocations,
            AllocationOverrideReason = reason,
        };

        var result = await BuildAllocationAsync(dto, receipt, userId, preview: false);

        receipt.AllocatedAmount = result.Allocations.Sum(a => a.Amount);
        receipt.UnallocatedAmount = RealEstateMapper.Money(receipt.Amount - receipt.AllocatedAmount);
        receipt.Status = receipt.UnallocatedAmount > 0m ? ReceiptStatus.OnAccount : ReceiptStatus.Posted;
        receipt.StampUpdated(userId);

        await Db.SaveChangesAsync();

        if (receipt.BookingId is not null)
        {
            await RecalculateBookingTotalsAsync(receipt.BookingId.Value, userId);
            await Db.SaveChangesAsync();
        }

        return (await GetReceiptAsync(receiptId))!;
    }

    // ═══ Cheques ═════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ChequeRecordDto>> GetChequesAsync(ListQueryDto query, ChequeState? state)
    {
        var q = Db.ChequeRecords.ForCompany(Tenant)
            .WhereIf(state.HasValue, c => c.State == state)
            .WhereIf(query.FromDate.HasValue, c => c.ChequeDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, c => c.ChequeDate <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), c => c.ChequeNumber.Contains(query.Search!))
            .OrderBy(c => c.ChequeDate);

        return await PageAsync(q, query, MapChequeListAsync);
    }

    private async Task<List<ChequeRecordDto>> MapChequeListAsync(List<ChequeRecord> cheques)
    {
        if (cheques.Count == 0) return [];

        var names = await PartyNamesAsync(cheques.Select(c => c.PartyId));
        var bookingIds = cheques.Where(c => c.BookingId.HasValue).Select(c => c.BookingId!.Value).Distinct().ToList();

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant).Where(b => bookingIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Reference);

        return cheques.Select(c => MapCheque(
            c,
            names.GetValueOrDefault(c.PartyId, "—"),
            c.BookingId is null ? null : bookings.GetValueOrDefault(c.BookingId.Value))).ToList();
    }

    private static ChequeRecordDto MapCheque(ChequeRecord c, string partyName, string? bookingReference) => new()
    {
        Id = c.Id,
        PartyId = c.PartyId,
        PartyName = partyName,
        BookingId = c.BookingId,
        BookingReference = bookingReference,
        TenancyId = c.TenancyId,
        ReceiptId = c.ReceiptId,
        ChequeNumber = c.ChequeNumber,
        BankName = c.BankName,
        BranchName = c.BranchName,
        Amount = c.Amount,
        CurrencyCode = c.CurrencyCode,
        ChequeDate = c.ChequeDate,
        ReceivedOn = c.ReceivedOn,
        State = c.State,
        IsPostDated = c.IsPostDated,
        DaysToMaturity = c.IsPostDated ? c.ChequeDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber : null,
        DepositedOn = c.DepositedOn,
        ClearedOn = c.ClearedOn,
        BouncedOn = c.BouncedOn,
        BounceReason = c.BounceReason,
        BounceCharge = c.BounceCharge,
        BounceChargeRecovered = c.BounceChargeRecovered,
        CustomerNotifiedOfBounce = c.CustomerNotifiedOfBounce,
        ReturnedOn = c.ReturnedOn,
        ReplacementChequeId = c.ReplacementChequeId,
        ImageUrl = c.ImageUrl,
        Note = c.Note,
    };

    /// <summary>
    /// Moves a cheque along its lifecycle.
    ///
    /// Clearing allocates the money for the first time; bouncing unwinds whatever was allocated,
    /// charges the bounce fee and tells the customer — all in one step, because a half-done bounce
    /// leaves a booking showing money it never received.
    /// </summary>
    public async Task<ChequeRecordDto> ChangeChequeStateAsync(ChequeStateChangeDto dto, Guid userId)
    {
        var cheque = await Db.ChequeRecords.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.ChequeRecordId)
            ?? throw new InvalidOperationException("That cheque does not exist.");

        var receipt = cheque.ReceiptId is null
            ? null
            : await Db.Receipts.ForCompany(Tenant).Include(r => r.Allocations)
                .FirstOrDefaultAsync(r => r.Id == cheque.ReceiptId);

        switch (dto.NewState)
        {
            case ChequeState.Deposited:
                cheque.DepositedOn = dto.OnDate;
                cheque.DepositBankAccountId = dto.BankAccountId;
                break;

            case ChequeState.Cleared:
                cheque.ClearedOn = dto.OnDate;

                // Cleared money is real money. Allocate it now.
                if (receipt is not null && receipt.AllocatedAmount == 0m)
                {
                    var allocation = await BuildAllocationAsync(new ReceiptCreateDto
                    {
                        BookingId = receipt.BookingId,
                        Amount = receipt.Amount,
                        ReceivedOn = receipt.ReceivedOn,
                    }, receipt, userId, preview: false);

                    receipt.AllocatedAmount = allocation.Allocations.Sum(a => a.Amount);
                    receipt.UnallocatedAmount = RealEstateMapper.Money(receipt.Amount - receipt.AllocatedAmount);
                    receipt.EscrowAmount = allocation.EscrowPortion;
                    receipt.FreeAmount = allocation.FreePortion;
                    receipt.Status = receipt.UnallocatedAmount > 0m ? ReceiptStatus.OnAccount : ReceiptStatus.Posted;
                    receipt.StampUpdated(userId);
                }
                break;

            case ChequeState.Bounced:
                cheque.BouncedOn = dto.OnDate;
                cheque.BounceReason = dto.BounceReason;
                cheque.BounceCharge = dto.BounceCharge ?? 0m;

                if (receipt is not null)
                {
                    await UnwindAllocationsAsync(receipt, userId);
                    receipt.Status = ReceiptStatus.Reversed;
                    receipt.ReversedAt = DateTime.UtcNow;
                    receipt.ReversalNote = $"Cheque {cheque.ChequeNumber} bounced: {dto.BounceReason}";
                    receipt.StampUpdated(userId);

                    Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
                    {
                        PartyId = receipt.PartyId,
                        BookingId = receipt.BookingId,
                        EntryDate = dto.OnDate,
                        Kind = LedgerEntryKind.Reversal,
                        Description = $"Cheque {cheque.ChequeNumber} bounced",
                        DebitAmount = receipt.Amount,
                        CurrencyCode = receipt.CurrencyCode,
                        SourceReceiptId = receipt.Id,
                        IsReversal = true,
                    }.StampNew(Tenant, userId));

                    if (cheque.BounceCharge > 0m)
                    {
                        Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
                        {
                            PartyId = receipt.PartyId,
                            BookingId = receipt.BookingId,
                            EntryDate = dto.OnDate,
                            Kind = LedgerEntryKind.Adjustment,
                            Description = "Cheque return charge",
                            DebitAmount = cheque.BounceCharge,
                            CurrencyCode = receipt.CurrencyCode,
                        }.StampNew(Tenant, userId));
                    }
                }

                if (dto.NotifyCustomer)
                {
                    cheque.CustomerNotifiedOfBounce = true;
                    await QueueNotificationAsync(
                        "cheque_bounced",
                        $"Cheque {cheque.ChequeNumber} was returned",
                        $"{cheque.Amount:N0} {cheque.CurrencyCode}. Reason: {dto.BounceReason}",
                        "/portal/customer",
                        recipientPartyId: cheque.PartyId,
                        entityType: "Cheque", entityId: cheque.Id,
                        severity: AlertSeverity.Critical);
                }
                break;

            case ChequeState.Returned:
                cheque.ReturnedOn = dto.OnDate;
                break;
        }

        cheque.State = dto.NewState;
        cheque.Note = dto.Note ?? cheque.Note;
        cheque.StampUpdated(userId);

        await Db.SaveChangesAsync();

        if (cheque.BookingId is not null)
        {
            await RecalculateBookingTotalsAsync(cheque.BookingId.Value, userId);
            await Db.SaveChangesAsync();
        }

        var names = await PartyNamesAsync([cheque.PartyId]);
        var bookingRef = cheque.BookingId is null
            ? null
            : await Db.Bookings.ForCompany(Tenant).Where(b => b.Id == cheque.BookingId)
                .Select(b => b.Reference).FirstOrDefaultAsync();

        return MapCheque(cheque, names.GetValueOrDefault(cheque.PartyId, "—"), bookingRef);
    }

    public async Task<List<ChequeRecordDto>> GetMaturityCalendarAsync(DateOnly from, DateOnly to)
    {
        var cheques = await Db.ChequeRecords.ForCompany(Tenant)
            .Where(c => c.IsPostDated && c.State == ChequeState.Pending)
            .Where(c => c.ChequeDate >= from && c.ChequeDate <= to)
            .OrderBy(c => c.ChequeDate)
            .ToListAsync();

        return await MapChequeListAsync(cheques);
    }

    // ═══ Ledger & statement ══════════════════════════════════════════════════

    public async Task<PaginatedResponse<CustomerLedgerEntryDto>> GetLedgerAsync(
        Guid? bookingId, Guid? partyId, ListQueryDto query)
    {
        var q = Db.CustomerLedgerEntries.ForCompany(Tenant)
            .WhereIf(bookingId.HasValue, e => e.BookingId == bookingId)
            .WhereIf(partyId.HasValue, e => e.PartyId == partyId)
            .WhereIf(query.FromDate.HasValue, e => e.EntryDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, e => e.EntryDate <= query.ToDate)
            .Where(e => e.IsVisibleToCustomer || true)
            .OrderBy(e => e.EntryDate).ThenBy(e => e.CreatedAt);

        return await PageAsync(q, query, e => new CustomerLedgerEntryDto
        {
            Id = e.Id,
            EntryDate = e.EntryDate,
            Kind = e.Kind,
            Description = e.Description ?? string.Empty,
            DebitAmount = e.DebitAmount,
            CreditAmount = e.CreditAmount,
            RunningBalance = e.RunningBalance,
            CurrencyCode = e.CurrencyCode,
            SourceDemandId = e.SourceDemandId,
            SourceReceiptId = e.SourceReceiptId,
            IsReversal = e.IsReversal,
        });
    }

    /// <summary>The customer-facing statement, with the schedule, the ledger and the ageing on one page.</summary>
    public async Task<StatementOfAccountDto> GetStatementAsync(Guid bookingId, DateOnly? from, DateOnly? to)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == booking.ProjectId);
        var party = await Db.Parties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == booking.PrimaryApplicantPartyId);
        var areaUnit = await AreaUnitAsync(project?.OfficeId);

        var unit = booking.UnitId is null
            ? null
            : await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == booking.UnitId);

        var plan = await GetPlanAsync(bookingId);

        var entries = await Db.CustomerLedgerEntries.ForCompany(Tenant)
            .Where(e => e.BookingId == bookingId)
            .WhereIf(from.HasValue, e => e.EntryDate >= from)
            .WhereIf(to.HasValue, e => e.EntryDate <= to)
            .OrderBy(e => e.EntryDate).ThenBy(e => e.CreatedAt)
            .ToListAsync();

        // The running balance is recomputed for the statement's window rather than trusted from
        // the row, so a statement for a date range always reconciles inside itself.
        var running = 0m;
        var ledger = entries.Select(e =>
        {
            running += e.DebitAmount - e.CreditAmount;
            return new CustomerLedgerEntryDto
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                Kind = e.Kind,
                Description = e.Description ?? string.Empty,
                DebitAmount = e.DebitAmount,
                CreditAmount = e.CreditAmount,
                RunningBalance = RealEstateMapper.Money(running),
                CurrencyCode = e.CurrencyCode,
                SourceDemandId = e.SourceDemandId,
                SourceReceiptId = e.SourceReceiptId,
                IsReversal = e.IsReversal,
            };
        }).ToList();

        var instalments = plan?.Instalments ?? [];
        var today = Today;

        var ageing = new AgeingBucketsDto
        {
            SurchargeOutstanding = instalments.Sum(i => i.SurchargeOutstanding),
        };

        foreach (var i in instalments.Where(i => i.Balance > 0))
        {
            var days = RealEstateMapper.DaysOverdue(i.DueDate, today);
            switch (RealEstateMapper.AgeingBucket(days))
            {
                case "Current": ageing.Current += i.Balance; break;
                case "1-30": ageing.Days1To30 += i.Balance; break;
                case "31-60": ageing.Days31To60 += i.Balance; break;
                case "61-90": ageing.Days61To90 += i.Balance; break;
                default: ageing.Days90Plus += i.Balance; break;
            }
        }

        ageing.Total = RealEstateMapper.Money(
            ageing.Current + ageing.Days1To30 + ageing.Days31To60 + ageing.Days61To90 + ageing.Days90Plus + ageing.SurchargeOutstanding);

        return new StatementOfAccountDto
        {
            BookingId = bookingId,
            BookingReference = booking.Reference,
            ApplicantName = party is null ? "—" : RealEstateMapper.DisplayName(party),
            FatherOrGuardianName = party?.FatherOrGuardianName,
            UnitNumber = unit?.UnitNumber,
            ProjectName = project?.Name ?? "—",
            Area = RealEstateMapper.AreaOrNull(booking.AreaSqFt, areaUnit),
            CurrencyCode = booking.CurrencyCode,
            StatementDate = today,
            PeriodFrom = from,
            PeriodTo = to,
            TotalConsideration = booking.TotalConsideration,
            TotalDemanded = booking.TotalDemanded,
            TotalPaid = booking.TotalPaid,
            SurchargeAccrued = booking.TotalSurcharge,
            SurchargeWaived = booking.TotalWaived,
            Outstanding = booking.Outstanding,
            OverdueAmount = booking.OverdueAmount,
            CollectionPercent = booking.CollectionPercent,
            OutstandingInWords = RealEstateMapper.AmountInWords(
                booking.Outstanding, booking.CurrencyCode, RealEstateMapper.UsesIndianScale(booking.CurrencyCode)),
            NextDueDate = booking.NextDueDate,
            NextDueAmount = booking.NextDueAmount,
            Schedule = instalments,
            Ledger = ledger,
            Ageing = ageing,
        };
    }

    // ═══ The collection desk ═════════════════════════════════════════════════

    /// <summary>
    /// The collector's worklist, sorted by recoverable amount rather than alphabetically, because
    /// an afternoon of calls is finite and the biggest recoverable debt should be the first call.
    /// </summary>
    public async Task<CollectionWorklistDto> GetWorklistAsync(CollectionQueryDto query)
    {
        var today = Today;

        var rows = await (
            from b in Db.Bookings.ForCompany(Tenant)
            where b.OverdueAmount > 0 && b.Status != BookingStatus.Cancelled
            select b)
            .WhereIf(query.ProjectId.HasValue, b => b.ProjectId == query.ProjectId)
            .WhereIf(query.MinDaysOverdue.HasValue, b => b.DaysOverdue >= query.MinDaysOverdue)
            .WhereIf(query.MinAmount.HasValue, b => b.OverdueAmount >= query.MinAmount)
            .OrderByDescending(b => b.OverdueAmount)
            .Take(query.PageSize <= 0 ? 100 : query.PageSize * 4)
            .ToListAsync();

        var bookingIds = rows.Select(b => b.Id).ToList();
        var names = await PartyNamesAsync(rows.Select(b => b.PrimaryApplicantPartyId));
        var projects = await ProjectNamesAsync(rows.Select(b => (Guid?)b.ProjectId));

        var phones = await Db.Parties.ForCompany(Tenant)
            .Where(p => rows.Select(b => b.PrimaryApplicantPartyId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PrimaryPhone);

        var whatsApp = await Db.PartyContacts.ForCompany(Tenant)
            .Where(c => c.IsWhatsApp)
            .Select(c => c.PartyId)
            .Distinct()
            .ToListAsync();

        var unitIds = rows.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).ToList();
        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var cases = await Db.DunningCases.ForCompany(Tenant)
            .Where(c => c.BookingId != null && bookingIds.Contains(c.BookingId!.Value) && !c.IsClosed)
            .ToDictionaryAsync(c => c.BookingId!.Value, c => c);

        var promises = await Db.PromiseToPays.ForCompany(Tenant)
            .Where(p => bookingIds.Contains(p.BookingId) && p.State == PromiseState.Open)
            .ToDictionaryAsync(p => p.BookingId, p => p);

        var bounces = await Db.ChequeRecords.ForCompany(Tenant)
            .Where(c => c.BookingId != null && bookingIds.Contains(c.BookingId!.Value) && c.State == ChequeState.Bounced)
            .GroupBy(c => c.BookingId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var notices = await Db.LegalNotices.ForCompany(Tenant)
            .Where(n => n.BookingId != null && bookingIds.Contains(n.BookingId!.Value))
            .GroupBy(n => n.BookingId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var lastContact = await Db.Activities.ForCompany(Tenant)
            .Where(a => a.BookingId != null && bookingIds.Contains(a.BookingId!.Value))
            .GroupBy(a => a.BookingId!.Value)
            .Select(g => new { Id = g.Key, At = g.Max(x => x.OccurredAt), Outcome = g.OrderByDescending(x => x.OccurredAt).First().Outcome })
            .ToDictionaryAsync(x => x.Id, x => x);

        var items = rows.Select(b =>
        {
            var dunning = cases.GetValueOrDefault(b.Id);
            var promise = promises.GetValueOrDefault(b.Id);
            var contact = lastContact.GetValueOrDefault(b.Id);

            return new CollectionWorklistItemDto
            {
                BookingId = b.Id,
                BookingReference = b.Reference,
                PartyId = b.PrimaryApplicantPartyId,
                CustomerName = names.GetValueOrDefault(b.PrimaryApplicantPartyId, "—"),
                Phone = phones.GetValueOrDefault(b.PrimaryApplicantPartyId),
                HasWhatsApp = whatsApp.Contains(b.PrimaryApplicantPartyId),
                UnitNumber = b.UnitId is null ? null : units.GetValueOrDefault(b.UnitId.Value),
                ProjectName = projects.GetValueOrDefault(b.ProjectId, "—"),
                OverdueAmount = b.OverdueAmount,
                SurchargeAmount = b.TotalSurcharge - b.TotalWaived,
                TotalOutstanding = b.Outstanding,
                CurrencyCode = b.CurrencyCode,
                DaysOverdue = b.DaysOverdue,
                AgeingBucket = RealEstateMapper.AgeingBucket(b.DaysOverdue),
                DunningCaseId = dunning?.Id,
                DunningStep = dunning?.CurrentStep ?? 0,
                NextStepDueAt = dunning?.NextStepDueAt,
                LastContactedAt = contact?.At,
                LastOutcome = contact?.Outcome,
                ActivePromiseId = promise?.Id,
                PromisedDate = promise?.PromisedDate,
                PromisedAmount = promise?.PromisedAmount,
                PromiseBroken = promise is not null && promise.PromisedDate < today,
                ChequeBounceCount = bounces.GetValueOrDefault(b.Id),
                IsUnderLitigation = b.IsUnderLitigation,
                NoticesServed = notices.GetValueOrDefault(b.Id),
            };
        }).ToList();

        if (query.WithPromiseOnly == true) items = items.Where(i => i.ActivePromiseId.HasValue).ToList();
        if (query.BrokenPromiseOnly == true) items = items.Where(i => i.PromiseBroken).ToList();
        if (query.DunningStep.HasValue) items = items.Where(i => i.DunningStep == query.DunningStep).ToList();
        if (!string.IsNullOrWhiteSpace(query.AgeingBucket))
            items = items.Where(i => i.AgeingBucket == query.AgeingBucket).ToList();

        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var collectedToday = await Db.Receipts.ForCompany(Tenant)
            .Where(r => r.ReceivedOn == today && r.Status != ReceiptStatus.Reversed)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        var collectedMonth = await Db.Receipts.ForCompany(Tenant)
            .Where(r => r.ReceivedOn >= monthStart && r.Status != ReceiptStatus.Reversed)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        var demandedMonth = await Db.Demands.ForCompany(Tenant)
            .Where(d => d.IssuedOn >= monthStart && d.Status != DemandStatus.Cancelled)
            .SumAsync(d => (decimal?)d.TotalAmount) ?? 0m;

        var ageing = new AgeingBucketsDto
        {
            Days1To30 = items.Where(i => i.AgeingBucket == "1-30").Sum(i => i.OverdueAmount),
            Days31To60 = items.Where(i => i.AgeingBucket == "31-60").Sum(i => i.OverdueAmount),
            Days61To90 = items.Where(i => i.AgeingBucket == "61-90").Sum(i => i.OverdueAmount),
            Days90Plus = items.Where(i => i.AgeingBucket == "90+").Sum(i => i.OverdueAmount),
            SurchargeOutstanding = items.Sum(i => i.SurchargeAmount),
        };
        ageing.Total = ageing.Days1To30 + ageing.Days31To60 + ageing.Days61To90 + ageing.Days90Plus + ageing.SurchargeOutstanding;

        return new CollectionWorklistDto
        {
            Items = items,
            TotalCount = items.Count,
            TotalOverdue = items.Sum(i => i.OverdueAmount),
            TotalSurcharge = items.Sum(i => i.SurchargeAmount),
            Ageing = ageing,
            PromisesDueToday = items.Count(i => i.PromisedDate == today),
            BrokenPromises = items.Count(i => i.PromiseBroken),
            CollectedToday = collectedToday,
            CollectedThisMonth = collectedMonth,
            DemandedThisMonth = demandedMonth,
            EfficiencyPercent = RealEstateMapper.Percent(collectedMonth, demandedMonth),
        };
    }

    public async Task<PromiseToPayDto> RecordPromiseAsync(PromiseToPayCreateDto dto, Guid userId)
    {
        if (dto.BookingId is null && dto.TenancyId is null)
            throw new InvalidOperationException("A promise has to be against a booking or a tenancy.");

        var partyId = Guid.Empty;
        if (dto.BookingId is not null)
        {
            var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId)
                ?? throw new InvalidOperationException("That booking does not exist.");
            partyId = booking.PrimaryApplicantPartyId;
        }

        var dunningCase = dto.BookingId is null
            ? null
            : await Db.DunningCases.ForCompany(Tenant)
                .FirstOrDefaultAsync(c => c.BookingId == dto.BookingId && !c.IsClosed);

        var promise = new PromiseToPay
        {
            BookingId = dto.BookingId ?? Guid.Empty,
            TenancyId = dto.TenancyId,
            PartyId = partyId,
            DunningCaseId = dunningCase?.Id,
            PromisedAmount = dto.PromisedAmount,
            PromisedDate = dto.PromisedDate,
            MadeAt = DateTime.UtcNow,
            TakenByUserId = userId,
            TakenVia = dto.TakenVia,
            State = PromiseState.Open,
            SuspendsDunning = dto.SuspendsDunning,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        Db.PromiseToPays.Add(promise);

        // A customer who has promised should not also be chased. That is the whole point.
        if (dunningCase is not null && dto.SuspendsDunning)
        {
            dunningCase.ActivePromiseId = promise.Id;
            dunningCase.IsSuspended = true;
            dunningCase.SuspendedUntil = dto.PromisedDate;
            dunningCase.SuspensionReason = "Promise to pay";
            dunningCase.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return (await GetPromisesAsync(null, null)).First(p => p.Id == promise.Id);
    }

    public async Task<List<PromiseToPayDto>> GetPromisesAsync(DateOnly? dueOn, PromiseState? state)
    {
        var promises = await Db.PromiseToPays.ForCompany(Tenant)
            .WhereIf(dueOn.HasValue, p => p.PromisedDate == dueOn)
            .WhereIf(state.HasValue, p => p.State == state)
            .OrderBy(p => p.PromisedDate)
            .ToListAsync();

        var names = await PartyNamesAsync(promises.Select(p => p.PartyId));
        var bookingIds = promises.Select(p => p.BookingId).Where(id => id != Guid.Empty).Distinct().ToList();

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant).Where(b => bookingIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Reference);

        var today = Today;

        return promises.Select(p => new PromiseToPayDto
        {
            Id = p.Id,
            BookingId = p.BookingId,
            BookingReference = bookings.GetValueOrDefault(p.BookingId),
            TenancyId = p.TenancyId,
            PartyId = p.PartyId,
            PartyName = names.GetValueOrDefault(p.PartyId, "—"),
            PromisedAmount = p.PromisedAmount,
            PromisedDate = p.PromisedDate,
            MadeAt = p.MadeAt,
            TakenByName = "—",
            TakenVia = p.TakenVia,
            State = p.State,
            PaidAmount = p.PaidAmount,
            SettledOn = p.SettledOn,
            SuspendsDunning = p.SuspendsDunning,
            IsDueToday = p.PromisedDate == today,
            IsOverdue = p.State == PromiseState.Open && p.PromisedDate < today,
            Note = p.Note,
        }).ToList();
    }

    /// <summary>
    /// Marks promises kept or broken. A broken promise lifts the dunning suspension and escalates,
    /// because the promise was the only reason the ladder paused.
    /// </summary>
    public async Task<int> EvaluatePromisesAsync()
    {
        var today = Today;

        var open = await Db.PromiseToPays.ForCompany(Tenant)
            .Where(p => p.State == PromiseState.Open && p.PromisedDate < today)
            .ToListAsync();

        if (open.Count == 0) return 0;

        var bookingIds = open.Select(p => p.BookingId).Where(id => id != Guid.Empty).Distinct().ToList();

        var receipts = await Db.Receipts.ForCompany(Tenant)
            .Where(r => r.BookingId != null && bookingIds.Contains(r.BookingId!.Value))
            .Where(r => r.Status != ReceiptStatus.Reversed)
            .Select(r => new { r.BookingId, r.ReceivedOn, r.Amount })
            .ToListAsync();

        var cases = await Db.DunningCases.ForCompany(Tenant)
            .Where(c => c.ActivePromiseId != null && open.Select(p => p.Id).Contains(c.ActivePromiseId!.Value))
            .ToListAsync();

        foreach (var promise in open)
        {
            var paid = receipts
                .Where(r => r.BookingId == promise.BookingId && r.ReceivedOn >= DateOnly.FromDateTime(promise.MadeAt))
                .Sum(r => r.Amount);

            promise.PaidAmount = paid;

            if (paid >= promise.PromisedAmount)
            {
                promise.State = PromiseState.Kept;
                promise.SettledOn = today;
            }
            else
            {
                promise.State = PromiseState.Broken;

                var dunningCase = cases.FirstOrDefault(c => c.ActivePromiseId == promise.Id);
                if (dunningCase is not null)
                {
                    dunningCase.IsSuspended = false;
                    dunningCase.SuspendedUntil = null;
                    dunningCase.SuspensionReason = null;
                    dunningCase.ActivePromiseId = null;
                    dunningCase.NextStepDueAt = DateTime.UtcNow;
                }

                await QueueNotificationAsync(
                    "promise_broken",
                    "Promise to pay was not kept",
                    $"{promise.PromisedAmount:N0} was promised for {promise.PromisedDate:d MMM}.",
                    "/realestate/collections",
                    recipientUserId: promise.TakenByUserId,
                    entityType: "PromiseToPay", entityId: promise.Id,
                    severity: AlertSeverity.Warning);
            }
        }

        await Db.SaveChangesAsync();
        return open.Count;
    }
}
