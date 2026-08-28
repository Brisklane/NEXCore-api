using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Project bank accounts, the escrow control, development loans, guarantees and customer mortgages.
///
/// Escrow exists because buyers' money is not the developer's money until the developer has built
/// something. The rule that follows from that is simple and absolute: what may be withdrawn is a
/// function of certified physical progress, not of what the bank balance happens to be. So the
/// entitlement is recomputed from the certified percentage each time, an over-draw is refused
/// rather than flagged, and every withdrawal carries the certificates that justified it — because
/// in two years' time, an auditor will ask exactly that question about exactly that transfer.
/// </summary>
public partial class FinanceService
{
    public async Task<List<ProjectBankAccountDto>> GetProjectAccountsAsync(Guid projectId)
    {
        var accounts = await Db.ProjectBankAccounts.ForCompany(Tenant)
            .Where(a => a.ProjectId == projectId)
            .OrderBy(a => a.Kind)
            .ThenBy(a => a.AccountTitle)
            .ToListAsync();

        return await MapAccountsAsync(accounts);
    }

    private async Task<List<ProjectBankAccountDto>> MapAccountsAsync(List<ProjectBankAccount> accounts)
    {
        if (accounts.Count == 0) return [];

        var projectNames = await ProjectNamesAsync(accounts.Select(a => (Guid?)a.ProjectId));
        var progress = await ProjectProgressAsync(accounts.Select(a => a.ProjectId).Distinct().ToList());

        return accounts.Select(a => new ProjectBankAccountDto
        {
            Id = a.Id,
            ProjectId = a.ProjectId,
            ProjectName = projectNames.GetValueOrDefault(a.ProjectId, "—"),
            Kind = a.Kind,
            AccountTitle = a.AccountTitle,
            AccountNumber = MaskAccount(a.AccountNumber),
            BankName = a.BankName,
            BranchName = a.BranchName,
            IbanOrSwift = a.IbanOrSwift,
            CurrencyCode = a.CurrencyCode,
            Balance = a.Balance,
            TotalCredited = a.TotalCredited,
            TotalWithdrawn = a.TotalWithdrawn,
            DepositPercent = a.DepositPercent,
            WithdrawalEntitlement = a.WithdrawalEntitlement,
            WithdrawnAgainstEntitlement = a.WithdrawnAgainstEntitlement,
            AvailableForWithdrawal = a.AvailableForWithdrawal,
            PhysicalProgressPercent = progress.GetValueOrDefault(a.ProjectId),
            RegulatorReference = a.RegulatorReference,
            RequiresEngineerCertificate = a.RequiresEngineerCertificate,
            RequiresArchitectCertificate = a.RequiresArchitectCertificate,
            RequiresAuditorCertificate = a.RequiresAuditorCertificate,
            LastAuditedOn = a.LastAuditedOn,
            NextAuditDue = a.NextAuditDue,
            AuditOverdue = a.NextAuditDue is not null && a.NextAuditDue < Today,
            IsActive = a.IsActive,
        }).ToList();
    }

    /// <summary>
    /// Everything but the last four digits. A bank account number on a shared screen is a fraud
    /// invitation, and nobody reading a balance needs the whole thing.
    /// </summary>
    private static string MaskAccount(string accountNumber)
        => string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length <= 4
            ? accountNumber
            : new string('•', accountNumber.Length - 4) + accountNumber[^4..];

    /// <summary>
    /// Certified physical progress per project, weighted by each milestone's own weight.
    ///
    /// Only certified milestones count. "Reached" is the site engineer's opinion; "certified" is
    /// somebody putting their licence behind it, and only the second one may release money.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> ProjectProgressAsync(List<Guid> projectIds)
    {
        if (projectIds.Count == 0) return [];

        var milestones = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => projectIds.Contains(m.ProjectId))
            .Select(m => new { m.ProjectId, m.WeightPercent, m.ProgressPercent, m.Status })
            .ToListAsync();

        var result = new Dictionary<Guid, decimal>();

        foreach (var projectId in projectIds)
        {
            var mine = milestones.Where(m => m.ProjectId == projectId).ToList();

            if (mine.Count == 0) { result[projectId] = 0m; continue; }

            var totalWeight = mine.Sum(m => m.WeightPercent);

            if (totalWeight <= 0m)
            {
                // Nobody weighted the milestones, so each counts equally. Still only the certified
                // ones, on the same reasoning.
                var certified = mine.Count(m => m.Status == MilestoneStatus.Certified);
                result[projectId] = RealEstateMapper.Percent(certified, mine.Count);
                continue;
            }

            var earned = mine.Sum(m => m.Status == MilestoneStatus.Certified
                ? m.WeightPercent
                : m.WeightPercent * Math.Min(100m, m.ProgressPercent) / 100m * 0m);

            result[projectId] = RealEstateMapper.Money(earned / totalWeight * 100m);
        }

        return result;
    }

    public async Task<ProjectBankAccountDto> SaveProjectAccountAsync(ProjectBankAccountDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var account = isNew
            ? new ProjectBankAccount()
            : await Db.ProjectBankAccounts.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
              ?? throw new InvalidOperationException("That account does not exist.");

        if (string.IsNullOrWhiteSpace(dto.AccountNumber) || dto.AccountNumber.Contains('•'))
        {
            // The masked number came back from the screen unchanged. Keep what is on file rather
            // than writing bullet characters into the account number.
            if (isNew) throw new InvalidOperationException("The account number is required.");
        }
        else
        {
            account.AccountNumber = dto.AccountNumber.Trim();
        }

        if (dto.Kind == ProjectAccountKind.Escrow)
        {
            var existingEscrow = await Db.ProjectBankAccounts.ForCompany(Tenant)
                .AnyAsync(a => a.ProjectId == dto.ProjectId
                            && a.Kind == ProjectAccountKind.Escrow
                            && a.IsActive
                            && a.Id != dto.Id);

            // One escrow account per project. Two would mean the deposit split has to choose, and
            // the regulator's reconciliation would never tie.
            if (existingEscrow)
                throw new InvalidOperationException(
                    "This project already has an active escrow account. Close it before opening another.");

            if (dto.DepositPercent is <= 0m or > 100m)
                throw new InvalidOperationException(
                    "An escrow account needs the percentage of every collection that must be deposited into it.");
        }

        account.ProjectId = dto.ProjectId;
        account.Kind = dto.Kind;
        account.AccountTitle = dto.AccountTitle;
        account.BankName = dto.BankName;
        account.BranchName = dto.BranchName;
        account.IbanOrSwift = dto.IbanOrSwift;
        account.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? await CurrencyAsync() : dto.CurrencyCode;
        account.DepositPercent = dto.DepositPercent;
        account.RegulatorReference = dto.RegulatorReference;
        account.RequiresEngineerCertificate = dto.RequiresEngineerCertificate;
        account.RequiresArchitectCertificate = dto.RequiresArchitectCertificate;
        account.RequiresAuditorCertificate = dto.RequiresAuditorCertificate;
        account.LastAuditedOn = dto.LastAuditedOn;
        account.NextAuditDue = dto.NextAuditDue;
        account.IsActive = dto.IsActive;

        if (isNew)
        {
            account.StampNew(Tenant, userId);
            Db.ProjectBankAccounts.Add(account);
        }
        else
        {
            account.StampUpdated(userId);
        }

        // The project keeps a pointer to each of its two accounts so that a receipt can be split
        // without a lookup on every transaction.
        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.ProjectId);

        if (project is not null)
        {
            if (account.Kind == ProjectAccountKind.Escrow)
            {
                project.EscrowAccountId = account.Id;
                project.EscrowPercent = account.DepositPercent;
            }
            else if (account.Kind == ProjectAccountKind.Free)
            {
                project.FreeAccountId = account.Id;
            }

            project.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await MapAccountsAsync([account]))[0];
    }

    public async Task<PaginatedResponse<EscrowLedgerEntryDto>> GetEscrowLedgerAsync(Guid accountId, ListQueryDto query)
    {
        var q = Db.EscrowLedgerEntries.ForCompany(Tenant)
            .Where(e => e.ProjectBankAccountId == accountId)
            .WhereIf(query.FromDate.HasValue, e => e.EntryDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, e => e.EntryDate <= query.ToDate)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt);

        return await PageAsync(q, query, MapEscrowLedgerAsync);
    }

    private async Task<List<EscrowLedgerEntryDto>> MapEscrowLedgerAsync(List<EscrowLedgerEntry> entries)
    {
        if (entries.Count == 0) return [];

        var receiptIds = entries.Where(e => e.ReceiptId != null).Select(e => e.ReceiptId!.Value).Distinct().ToList();
        var bookingIds = entries.Where(e => e.BookingId != null).Select(e => e.BookingId!.Value).Distinct().ToList();

        var receipts = receiptIds.Count == 0
            ? []
            : await Db.Receipts.ForCompany(Tenant)
                .Where(r => receiptIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.ReceiptNumber);

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant)
                .Where(b => bookingIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Reference, b.PrimaryApplicantPartyId })
                .ToListAsync();

        var names = await PartyNamesAsync(bookings.Select(b => b.PrimaryApplicantPartyId));

        return entries.Select(e =>
        {
            var booking = e.BookingId is null ? null : bookings.FirstOrDefault(b => b.Id == e.BookingId);

            return new EscrowLedgerEntryDto
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                Kind = e.Kind,
                Description = e.Description ?? DescribeEscrowEntry(e.Kind),
                CreditAmount = e.CreditAmount,
                DebitAmount = e.DebitAmount,
                RunningBalance = e.RunningBalance,
                ReceiptNumber = e.ReceiptId is null ? null : receipts.GetValueOrDefault(e.ReceiptId.Value),
                BookingReference = booking?.Reference,
                CustomerName = booking is null ? null : names.GetValueOrDefault(booking.PrimaryApplicantPartyId),
                IsReconciled = e.IsReconciled,
                BankReference = e.BankReference,
            };
        }).ToList();
    }

    private static string DescribeEscrowEntry(EscrowMovementKind kind) => kind switch
    {
        EscrowMovementKind.CollectionCredit => "Customer collection deposited",
        EscrowMovementKind.Withdrawal => "Withdrawal against certified progress",
        EscrowMovementKind.InterestCredit => "Bank interest credited",
        EscrowMovementKind.BankCharge => "Bank charges",
        EscrowMovementKind.TransferToFree => "Transfer to free account",
        _ => "Adjustment",
    };

    // ═══ Withdrawal entitlement ══════════════════════════════════════════════

    /// <summary>
    /// Recomputes what may be taken out of escrow, from certified progress.
    ///
    /// Entitlement = certified progress × total collections deposited. Everything already withdrawn
    /// comes off that, and what is left is capped by the actual bank balance — because an
    /// entitlement is a permission, not a source of funds.
    /// </summary>
    public async Task<ProjectBankAccountDto> RecomputeEntitlementAsync(Guid accountId, Guid userId)
    {
        var account = await RequireAsync<ProjectBankAccount>(accountId, "That account does not exist.");

        if (account.Kind != ProjectAccountKind.Escrow)
            throw new InvalidOperationException("Only an escrow account carries a withdrawal entitlement.");

        await RecomputeEntitlementCoreAsync(account, userId);
        await Db.SaveChangesAsync();

        return (await MapAccountsAsync([account]))[0];
    }

    private async Task<decimal> RecomputeEntitlementCoreAsync(ProjectBankAccount account, Guid userId)
    {
        var progress = (await ProjectProgressAsync([account.ProjectId])).GetValueOrDefault(account.ProjectId);

        var entitlement = RealEstateMapper.Money(account.TotalCredited * progress / 100m);

        account.WithdrawalEntitlement = entitlement;
        account.WithdrawnAgainstEntitlement = account.TotalWithdrawn;
        account.AvailableForWithdrawal = RealEstateMapper.Money(
            Math.Max(0m, Math.Min(entitlement - account.TotalWithdrawn, account.Balance)));

        account.StampUpdated(userId);

        return progress;
    }

    public async Task<EscrowWithdrawalDto> RequestWithdrawalAsync(EscrowWithdrawalRequestDto dto, Guid userId)
    {
        var account = await RequireAsync<ProjectBankAccount>(
            dto.ProjectBankAccountId, "That account does not exist.");

        if (account.Kind != ProjectAccountKind.Escrow)
            throw new InvalidOperationException("Only an escrow account needs a withdrawal request.");

        if (dto.RequestedAmount <= 0m)
            throw new InvalidOperationException("A withdrawal has to be for more than nothing.");

        var progress = await RecomputeEntitlementCoreAsync(account, userId);

        var missing = MissingCertificates(account, dto.Certificates);

        var withdrawal = new EscrowWithdrawal
        {
            Reference = await numbering.NextEscrowWithdrawalNumberAsync(DateTime.UtcNow),
            ProjectBankAccountId = account.Id,
            ProjectId = account.ProjectId,
            RequestedOn = Today,
            RequestedAmount = RealEstateMapper.Money(dto.RequestedAmount),
            Purpose = dto.Purpose,
            ProgressPercentAtRequest = progress,
            EntitlementAtRequest = account.WithdrawalEntitlement,
            AlreadyWithdrawn = account.TotalWithdrawn,
            AvailableAtRequest = account.AvailableForWithdrawal,
            ExceededEntitlement = dto.RequestedAmount > account.AvailableForWithdrawal + 0.01m,
            RequestedByUserId = userId,
            Status = "Requested",
        };

        // A dry run answers "could I?" without leaving a request behind. The screen uses it to show
        // the arithmetic while somebody is still typing an amount.
        if (dto.DryRun)
        {
            var preview = await MapWithdrawalAsync([withdrawal], [], includeCertificates: false);
            preview[0].MissingCertificates = missing;
            preview[0].Certificates = dto.Certificates;
            return preview[0];
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "This withdrawal cannot be requested without " + string.Join(" and ", missing).ToLowerInvariant()
                + ". They are what the regulator relies on to release the money.");

        if (withdrawal.ExceededEntitlement)
            throw new InvalidOperationException(
                $"That is more than the certified entitlement allows. Progress is certified at {progress:0.##}%, "
                + $"which entitles {account.WithdrawalEntitlement:N0} against {account.TotalCredited:N0} deposited. "
                + $"{account.TotalWithdrawn:N0} has already been withdrawn, leaving {account.AvailableForWithdrawal:N0} "
                + "available. Certify further progress first.");

        withdrawal.StampNew(Tenant, userId);
        Db.EscrowWithdrawals.Add(withdrawal);

        foreach (var cert in dto.Certificates)
        {
            Db.WithdrawalCertificates.Add(new WithdrawalCertificate
            {
                EscrowWithdrawalId = withdrawal.Id,
                CertifierType = cert.CertifierType,
                CertifierName = cert.CertifierName,
                RegistrationNumber = cert.RegistrationNumber,
                CertifiedOn = cert.CertifiedOn == default ? Today : cert.CertifiedOn,
                CertifiedProgressPercent = cert.CertifiedProgressPercent,
                CertifiedCostIncurred = cert.CertifiedCostIncurred,
                DocumentUrl = cert.DocumentUrl,
                IsVerified = cert.IsVerified,
            }.StampNew(Tenant, userId));
        }

        var approval = await RaiseApprovalAsync(
            nameof(EscrowWithdrawal), withdrawal.Id, withdrawal.Reference, withdrawal.RequestedAmount,
            $"Escrow withdrawal of {withdrawal.RequestedAmount:N0} at {progress:0.##}% certified progress",
            userId, projectId: account.ProjectId);

        withdrawal.ApprovalRequestId = approval?.Id;

        if (approval is null)
        {
            // Nothing in the approval matrix covers this amount, so it stands approved as requested.
            withdrawal.Status = "Approved";
            withdrawal.ApprovedAmount = withdrawal.RequestedAmount;
        }
        else
        {
            withdrawal.Status = "PendingApproval";
        }

        await QueueNotificationAsync(
            "escrow.withdrawal.requested",
            $"Escrow withdrawal {withdrawal.Reference}",
            $"{withdrawal.RequestedAmount:N0} requested against {progress:0.##}% certified progress.",
            $"/realestate/finance/escrow/{account.Id}",
            entityType: nameof(EscrowWithdrawal),
            entityId: withdrawal.Id,
            severity: AlertSeverity.Warning);

        await Db.SaveChangesAsync();

        return (await GetWithdrawalAsync(withdrawal.Id))!;
    }

    private static List<string> MissingCertificates(ProjectBankAccount account, List<WithdrawalCertificateDto> supplied)
    {
        var missing = new List<string>();

        bool Has(string type) => supplied.Any(c =>
            string.Equals(c.CertifierType, type, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(c.CertifierName));

        if (account.RequiresEngineerCertificate && !Has("Engineer")) missing.Add("An engineer's certificate");
        if (account.RequiresArchitectCertificate && !Has("Architect")) missing.Add("An architect's certificate");
        if (account.RequiresAuditorCertificate && !Has("Auditor")) missing.Add("An auditor's certificate");

        return missing;
    }

    private async Task<EscrowWithdrawalDto?> GetWithdrawalAsync(Guid id)
    {
        var withdrawal = await Db.EscrowWithdrawals.ForCompany(Tenant)
            .Include(w => w.Certificates)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (withdrawal is null) return null;

        var mapped = await MapWithdrawalAsync([withdrawal], [], includeCertificates: true);
        return mapped[0];
    }

    public async Task<PaginatedResponse<EscrowWithdrawalDto>> GetWithdrawalsAsync(ListQueryDto query, Guid? projectId)
    {
        var q = Db.EscrowWithdrawals.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, w => w.ProjectId == projectId)
            .WhereIf(query.FromDate.HasValue, w => w.RequestedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, w => w.RequestedOn <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), w => w.Reference.Contains(query.Search!))
            .OrderByDescending(w => w.RequestedOn)
            .ThenByDescending(w => w.CreatedAt);

        return await PageAsync(q, query, rows => MapWithdrawalAsync(rows, [], includeCertificates: false));
    }

    private async Task<List<EscrowWithdrawalDto>> MapWithdrawalAsync(
        List<EscrowWithdrawal> withdrawals, List<WithdrawalCertificate> preloaded, bool includeCertificates)
    {
        if (withdrawals.Count == 0) return [];

        var currency = await CurrencyAsync();
        var projectNames = await ProjectNamesAsync(withdrawals.Select(w => (Guid?)w.ProjectId));
        var requesters = await AgentUserNamesAsync(withdrawals.Select(w => w.RequestedByUserId));

        var certificates = preloaded;

        if (includeCertificates && certificates.Count == 0)
        {
            var ids = withdrawals.Where(w => w.Id != Guid.Empty).Select(w => w.Id).ToList();

            certificates = ids.Count == 0
                ? []
                : await Db.WithdrawalCertificates.ForCompany(Tenant)
                    .Where(c => ids.Contains(c.EscrowWithdrawalId))
                    .ToListAsync();
        }

        return withdrawals.Select(w => new EscrowWithdrawalDto
        {
            Id = w.Id,
            Reference = w.Reference,
            ProjectBankAccountId = w.ProjectBankAccountId,
            ProjectId = w.ProjectId,
            ProjectName = projectNames.GetValueOrDefault(w.ProjectId, "—"),
            RequestedOn = w.RequestedOn,
            RequestedAmount = w.RequestedAmount,
            ApprovedAmount = w.ApprovedAmount,
            Purpose = w.Purpose,
            CurrencyCode = currency,
            ProgressPercentAtRequest = w.ProgressPercentAtRequest,
            EntitlementAtRequest = w.EntitlementAtRequest,
            AlreadyWithdrawn = w.AlreadyWithdrawn,
            AvailableAtRequest = w.AvailableAtRequest,
            Status = w.Status,
            ExceededEntitlement = w.ExceededEntitlement,
            RequestedByName = w.RequestedByUserId is null ? null : requesters.GetValueOrDefault(w.RequestedByUserId.Value),
            WithdrawnOn = w.WithdrawnOn,
            BankReference = w.BankReference,
            Certificates = certificates
                .Where(c => c.EscrowWithdrawalId == w.Id)
                .OrderBy(c => c.CertifierType)
                .Select(c => new WithdrawalCertificateDto
                {
                    Id = c.Id,
                    CertifierType = c.CertifierType,
                    CertifierName = c.CertifierName,
                    RegistrationNumber = c.RegistrationNumber,
                    CertifiedOn = c.CertifiedOn,
                    CertifiedProgressPercent = c.CertifiedProgressPercent,
                    CertifiedCostIncurred = c.CertifiedCostIncurred,
                    DocumentUrl = c.DocumentUrl,
                    IsVerified = c.IsVerified,
                }).ToList(),
        }).ToList();
    }

    public async Task<EscrowWithdrawalDto> DecideWithdrawalAsync(
        Guid id, ApprovalOutcome outcome, decimal? approvedAmount, string? comment, Guid userId)
    {
        var withdrawal = await RequireAsync<EscrowWithdrawal>(id, "That withdrawal request does not exist.");

        if (withdrawal.Status is "Withdrawn" or "Rejected")
            throw new InvalidOperationException($"That request is already {withdrawal.Status.ToLowerInvariant()}.");

        var account = await RequireAsync<ProjectBankAccount>(
            withdrawal.ProjectBankAccountId, "That account does not exist.");

        if (outcome is ApprovalOutcome.Rejected or ApprovalOutcome.Withdrawn)
        {
            withdrawal.Status = "Rejected";
            withdrawal.Description = comment;
            withdrawal.StampUpdated(userId);
            await Db.SaveChangesAsync();

            return (await GetWithdrawalAsync(withdrawal.Id))!;
        }

        if (outcome is not (ApprovalOutcome.Approved or ApprovalOutcome.AutoApproved))
            throw new InvalidOperationException("Only an approval or a rejection closes a withdrawal request.");

        var amount = RealEstateMapper.Money(approvedAmount ?? withdrawal.RequestedAmount);

        if (amount <= 0m)
            throw new InvalidOperationException("An approved withdrawal has to be for more than nothing.");

        if (amount > withdrawal.RequestedAmount + 0.01m)
            throw new InvalidOperationException(
                $"Approving {amount:N0} against a request for {withdrawal.RequestedAmount:N0} is not an approval — "
                + "raise a fresh request for the larger figure.");

        // The entitlement is checked again at the moment of release, not only at the moment of
        // request. Between the two, another withdrawal may have gone out.
        await RecomputeEntitlementCoreAsync(account, userId);

        if (amount > account.AvailableForWithdrawal + 0.01m)
            throw new InvalidOperationException(
                $"Only {account.AvailableForWithdrawal:N0} is available against certified progress now — "
                + "the entitlement has moved since this was requested.");

        withdrawal.ApprovedAmount = amount;
        withdrawal.Status = "Withdrawn";
        withdrawal.WithdrawnOn = Today;
        withdrawal.Description = comment;
        withdrawal.StampUpdated(userId);

        await AppendEscrowEntryAsync(
            account, EscrowMovementKind.Withdrawal, credit: 0m, debit: amount, userId: userId,
            withdrawalId: withdrawal.Id,
            description: $"Withdrawal {withdrawal.Reference}"
                + (withdrawal.Purpose is null ? null : $" — {withdrawal.Purpose}"));

        await RecomputeEntitlementCoreAsync(account, userId);

        await QueueNotificationAsync(
            "escrow.withdrawal.released",
            $"Escrow withdrawal {withdrawal.Reference} released",
            $"{amount:N0} released. {account.AvailableForWithdrawal:N0} remains available against certified progress.",
            $"/realestate/finance/escrow/{account.Id}",
            entityType: nameof(EscrowWithdrawal),
            entityId: withdrawal.Id);

        await Db.SaveChangesAsync();

        return (await GetWithdrawalAsync(withdrawal.Id))!;
    }

    /// <summary>
    /// Appends one movement to an escrow ledger and carries the balance. Used by the withdrawal
    /// path here, and by the collections path when a receipt is split between escrow and free.
    /// </summary>
    private async Task<EscrowLedgerEntry> AppendEscrowEntryAsync(
        ProjectBankAccount account, EscrowMovementKind kind, decimal credit, decimal debit, Guid userId,
        Guid? receiptId = null, Guid? bookingId = null, Guid? withdrawalId = null,
        string? description = null, string? bankReference = null, DateOnly? entryDate = null)
    {
        var previous = await Db.EscrowLedgerEntries.ForCompany(Tenant)
            .Where(e => e.ProjectBankAccountId == account.Id)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => (decimal?)e.RunningBalance)
            .FirstOrDefaultAsync();

        var running = RealEstateMapper.Money((previous ?? 0m) + credit - debit);

        var entry = new EscrowLedgerEntry
        {
            ProjectBankAccountId = account.Id,
            ProjectId = account.ProjectId,
            EntryDate = entryDate ?? Today,
            Kind = kind,
            CreditAmount = RealEstateMapper.Money(credit),
            DebitAmount = RealEstateMapper.Money(debit),
            RunningBalance = running,
            ReceiptId = receiptId,
            BookingId = bookingId,
            EscrowWithdrawalId = withdrawalId,
            BankReference = bankReference,
            Description = description,
        }.StampNew(Tenant, userId);

        Db.EscrowLedgerEntries.Add(entry);

        account.Balance = running;
        account.TotalCredited = RealEstateMapper.Money(account.TotalCredited + credit);
        account.TotalWithdrawn = RealEstateMapper.Money(account.TotalWithdrawn + debit);
        account.StampUpdated(userId);

        return entry;
    }

    // ═══ Development loans ═══════════════════════════════════════════════════

    public async Task<PaginatedResponse<ProjectLoanDto>> GetLoansAsync(ListQueryDto query, Guid? projectId)
    {
        var q = Db.ProjectLoans.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, l => l.ProjectId == projectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                l => l.Reference.Contains(query.Search!) || (l.LenderName ?? "").Contains(query.Search!))
            .OrderByDescending(l => l.OutstandingAmount);

        return await PageAsync(q, query, rows => MapLoansAsync(rows, includeSchedule: false));
    }

    private async Task<List<ProjectLoanDto>> MapLoansAsync(List<ProjectLoan> loans, bool includeSchedule)
    {
        if (loans.Count == 0) return [];

        var ids = loans.Select(l => l.Id).ToList();
        var projectNames = await ProjectNamesAsync(loans.Select(l => (Guid?)l.ProjectId));

        var drawdowns = await Db.LoanDrawdowns.ForCompany(Tenant)
            .Where(d => ids.Contains(d.ProjectLoanId))
            .OrderBy(d => d.RequestedOn)
            .ToListAsync();

        var repayments = includeSchedule
            ? await Db.LoanRepayments.ForCompany(Tenant)
                .Where(r => ids.Contains(r.ProjectLoanId))
                .OrderBy(r => r.InstalmentNumber)
                .ToListAsync()
            : [];

        return loans.Select(l => new ProjectLoanDto
        {
            Id = l.Id,
            Reference = l.Reference,
            ProjectId = l.ProjectId,
            ProjectName = projectNames.GetValueOrDefault(l.ProjectId, "—"),
            LenderName = l.LenderName,
            Status = l.Status,
            SanctionedAmount = l.SanctionedAmount,
            DrawnAmount = l.DrawnAmount,
            OutstandingAmount = l.OutstandingAmount,
            RepaidAmount = l.RepaidAmount,
            UndrawnAmount = RealEstateMapper.Money(Math.Max(0m, l.SanctionedAmount - l.DrawnAmount)),
            CurrencyCode = l.CurrencyCode,
            InterestRate = l.InterestRate,
            RateBasis = l.RateBasis,
            AccruedInterest = l.AccruedInterest,
            PaidInterest = l.PaidInterest,
            SanctionedOn = l.SanctionedOn,
            FirstDrawdownOn = l.FirstDrawdownOn,
            MoratoriumEndsOn = l.MoratoriumEndsOn,
            MaturityDate = l.MaturityDate,
            TenureMonths = l.TenureMonths,
            SecurityDescription = l.SecurityDescription,
            UnitsMortgaged = l.UnitsMortgaged,
            MortgagedUnitCount = l.MortgagedUnitCount,
            Covenants = l.Covenants,
            NextCovenantTestOn = l.NextCovenantTestOn,
            DocumentUrl = l.DocumentUrl,
            Drawdowns = drawdowns.Where(d => d.ProjectLoanId == l.Id).Select(MapDrawdown).ToList(),
            Repayments = repayments.Where(r => r.ProjectLoanId == l.Id).Select(r => new LoanRepaymentDto
            {
                Id = r.Id,
                InstalmentNumber = r.InstalmentNumber,
                DueDate = r.DueDate,
                PrincipalAmount = r.PrincipalAmount,
                InterestAmount = r.InterestAmount,
                TotalAmount = r.TotalAmount,
                PaidAmount = r.PaidAmount,
                PaidOn = r.PaidOn,
                OutstandingAfter = r.OutstandingAfter,
                IsPaid = r.IsPaid,
                IsOverdue = !r.IsPaid && r.DueDate < Today,
            }).ToList(),
        }).ToList();
    }

    private static LoanDrawdownDto MapDrawdown(LoanDrawdown d) => new()
    {
        Id = d.Id,
        Reference = d.Reference,
        RequestedOn = d.RequestedOn,
        ReceivedOn = d.ReceivedOn,
        Amount = d.Amount,
        ProgressPercentAtDrawdown = d.ProgressPercentAtDrawdown,
        MilestoneCertificateId = d.MilestoneCertificateId,
        Purpose = d.Purpose,
        ProcessingFee = d.ProcessingFee,
        IsReceived = d.IsReceived,
    };

    public async Task<ProjectLoanDto> SaveLoanAsync(ProjectLoanDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var loan = isNew
            ? new ProjectLoan { Reference = await numbering.NextMasterCodeAsync(Db.ProjectLoans, "LOAN") }
            : await Db.ProjectLoans.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == dto.Id)
              ?? throw new InvalidOperationException("That loan does not exist.");

        if (!isNew && dto.SanctionedAmount < loan.DrawnAmount)
            throw new InvalidOperationException(
                $"{loan.DrawnAmount:N0} has already been drawn. The sanction cannot be reduced below that.");

        loan.ProjectId = dto.ProjectId;
        loan.LenderName = dto.LenderName;
        loan.Status = dto.Status;
        loan.SanctionedAmount = dto.SanctionedAmount;
        loan.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? await CurrencyAsync() : dto.CurrencyCode;
        loan.InterestRate = dto.InterestRate;
        loan.RateBasis = dto.RateBasis;
        loan.SanctionedOn = dto.SanctionedOn;
        loan.MoratoriumEndsOn = dto.MoratoriumEndsOn;
        loan.MaturityDate = dto.MaturityDate;
        loan.TenureMonths = dto.TenureMonths;
        loan.SecurityDescription = dto.SecurityDescription;
        loan.UnitsMortgaged = dto.UnitsMortgaged;
        loan.MortgagedUnitCount = dto.MortgagedUnitCount;
        loan.Covenants = dto.Covenants;
        loan.NextCovenantTestOn = dto.NextCovenantTestOn;
        loan.DocumentUrl = dto.DocumentUrl;
        loan.Description = dto.SecurityDescription;

        if (isNew)
        {
            loan.StampNew(Tenant, userId);
            Db.ProjectLoans.Add(loan);
        }
        else
        {
            loan.StampUpdated(userId);
        }

        if (loan.NextCovenantTestOn is not null)
        {
            await UpsertCalendarEntryAsync(
                $"Covenant test — {loan.Reference}", "Loan", loan.NextCovenantTestOn.Value, userId,
                projectId: loan.ProjectId, severity: AlertSeverity.Warning);
        }

        await Db.SaveChangesAsync();

        return (await MapLoansAsync([loan], includeSchedule: true))[0];
    }

    public async Task<LoanDrawdownDto> RecordDrawdownAsync(Guid loanId, LoanDrawdownDto dto, Guid userId)
    {
        var loan = await RequireAsync<ProjectLoan>(loanId, "That loan does not exist.");

        if (dto.Amount <= 0m)
            throw new InvalidOperationException("A drawdown has to be for more than nothing.");

        if (loan.Status is LoanStatus.Closed or LoanStatus.Defaulted)
            throw new InvalidOperationException($"That loan is {loan.Status.ToString().ToLowerInvariant()}.");

        var undrawn = loan.SanctionedAmount - loan.DrawnAmount;

        if (dto.Amount > undrawn + 0.01m)
            throw new InvalidOperationException(
                $"Only {Math.Max(0m, undrawn):N0} remains undrawn against a sanction of {loan.SanctionedAmount:N0}.");

        var progress = (await ProjectProgressAsync([loan.ProjectId])).GetValueOrDefault(loan.ProjectId);

        var drawdown = new LoanDrawdown
        {
            ProjectLoanId = loan.Id,
            Reference = await numbering.NextMasterCodeAsync(Db.LoanDrawdowns, "DRW"),
            RequestedOn = dto.RequestedOn == default ? Today : dto.RequestedOn,
            ReceivedOn = dto.ReceivedOn,
            Amount = RealEstateMapper.Money(dto.Amount),
            ProgressPercentAtDrawdown = dto.ProgressPercentAtDrawdown > 0m ? dto.ProgressPercentAtDrawdown : progress,
            MilestoneCertificateId = dto.MilestoneCertificateId,
            Purpose = dto.Purpose,
            ProcessingFee = dto.ProcessingFee,
            IsReceived = dto.IsReceived,
            Description = dto.Purpose,
        }.StampNew(Tenant, userId);

        Db.LoanDrawdowns.Add(drawdown);

        // Only money actually in the bank moves the loan balances. A drawdown requested but not yet
        // received is a request, and treating it as debt overstates gearing.
        if (drawdown.IsReceived)
        {
            loan.DrawnAmount = RealEstateMapper.Money(loan.DrawnAmount + drawdown.Amount);
            loan.OutstandingAmount = RealEstateMapper.Money(loan.DrawnAmount - loan.RepaidAmount);
            loan.FirstDrawdownOn ??= drawdown.ReceivedOn ?? drawdown.RequestedOn;

            loan.Status = loan.DrawnAmount >= loan.SanctionedAmount - 0.01m
                ? LoanStatus.FullyDrawn
                : LoanStatus.PartiallyDrawn;
        }

        loan.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return MapDrawdown(drawdown);
    }

    // ═══ Bank guarantees ═════════════════════════════════════════════════════

    public async Task<List<BankGuaranteeDto>> GetGuaranteesAsync(Guid? projectId, bool expiringOnly)
    {
        var guarantees = await Db.BankGuarantees.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, g => g.ProjectId == projectId)
            .WhereIf(expiringOnly, g => g.Status == "Active")
            .OrderBy(g => g.ExpiresOn)
            .ToListAsync();

        var mapped = await MapGuaranteesAsync(guarantees);

        return expiringOnly
            ? mapped.Where(g => g.IsExpiringSoon || g.DaysToExpiry < 0).ToList()
            : mapped;
    }

    private async Task<List<BankGuaranteeDto>> MapGuaranteesAsync(List<BankGuarantee> guarantees)
    {
        if (guarantees.Count == 0) return [];

        var currency = await CurrencyAsync();
        var projectNames = await ProjectNamesAsync(guarantees.Select(g => g.ProjectId));
        var partyNames = await PartyNamesAsync(
            guarantees.Where(g => g.PartyId != null).Select(g => g.PartyId!.Value));

        return guarantees.Select(g =>
        {
            var days = g.ExpiresOn.DayNumber - Today.DayNumber;

            return new BankGuaranteeDto
            {
                Id = g.Id,
                GuaranteeNumber = g.GuaranteeNumber,
                Kind = g.Kind,
                Direction = g.Direction,
                ProjectId = g.ProjectId,
                ProjectName = g.ProjectId is null ? null : projectNames.GetValueOrDefault(g.ProjectId.Value),
                SubcontractId = g.SubcontractId,
                CounterpartyName = g.PartyId is null ? null : partyNames.GetValueOrDefault(g.PartyId.Value),
                IssuingBank = g.IssuingBank,
                Amount = g.Amount,
                CurrencyCode = string.IsNullOrWhiteSpace(g.CurrencyCode) ? currency : g.CurrencyCode,
                IssuedOn = g.IssuedOn,
                ExpiresOn = g.ExpiresOn,
                ClaimPeriodEndsOn = g.ClaimPeriodEndsOn,
                DaysToExpiry = days,
                IsExpiringSoon = g.Status == "Active" && days <= g.AlertDaysBefore,
                Commission = g.Commission,
                MarginHeld = g.MarginHeld,
                Status = g.Status,
                IsAutoRenewing = g.IsAutoRenewing,
                ReleasedOn = g.ReleasedOn,
                DocumentUrl = g.DocumentUrl,
            };
        }).ToList();
    }

    public async Task<BankGuaranteeDto> SaveGuaranteeAsync(BankGuaranteeDto dto, Guid userId)
    {
        if (dto.ExpiresOn <= dto.IssuedOn)
            throw new InvalidOperationException("A guarantee cannot expire on or before the day it was issued.");

        var isNew = dto.Id == Guid.Empty;

        var guarantee = isNew
            ? new BankGuarantee()
            : await Db.BankGuarantees.ForCompany(Tenant).FirstOrDefaultAsync(g => g.Id == dto.Id)
              ?? throw new InvalidOperationException("That guarantee does not exist.");

        guarantee.GuaranteeNumber = dto.GuaranteeNumber;
        guarantee.Kind = dto.Kind;
        guarantee.Direction = dto.Direction;
        guarantee.ProjectId = dto.ProjectId;
        guarantee.SubcontractId = dto.SubcontractId;
        guarantee.IssuingBank = dto.IssuingBank;
        guarantee.Amount = dto.Amount;
        guarantee.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? await CurrencyAsync() : dto.CurrencyCode;
        guarantee.IssuedOn = dto.IssuedOn;
        guarantee.ExpiresOn = dto.ExpiresOn;
        guarantee.ClaimPeriodEndsOn = dto.ClaimPeriodEndsOn;
        guarantee.Commission = dto.Commission;
        guarantee.MarginHeld = dto.MarginHeld;
        guarantee.Status = dto.Status;
        guarantee.IsAutoRenewing = dto.IsAutoRenewing;
        guarantee.ReleasedOn = dto.ReleasedOn;
        guarantee.DocumentUrl = dto.DocumentUrl;

        // Changing the expiry resets the alert, otherwise a renewed guarantee stays silent through
        // its second expiry because the flag was already set on its first.
        if (!isNew) guarantee.ExpiryAlertSent = false;

        if (isNew)
        {
            guarantee.StampNew(Tenant, userId);
            Db.BankGuarantees.Add(guarantee);
        }
        else
        {
            guarantee.StampUpdated(userId);
        }

        // A guarantee that lapses unnoticed is an uncovered exposure, so it goes on the compliance
        // calendar as well as carrying its own alert.
        await UpsertCalendarEntryAsync(
            $"Guarantee {guarantee.GuaranteeNumber} expires", "Guarantee",
            guarantee.ExpiresOn, userId,
            projectId: guarantee.ProjectId,
            severity: AlertSeverity.Critical,
            alertDaysBefore: guarantee.AlertDaysBefore);

        await Db.SaveChangesAsync();

        return (await MapGuaranteesAsync([guarantee]))[0];
    }

    // ═══ Customer mortgages ══════════════════════════════════════════════════

    public async Task<PaginatedResponse<CustomerMortgageDto>> GetMortgagesAsync(ListQueryDto query, string? status)
    {
        var q = Db.CustomerMortgages.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(status), m => m.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                m => m.Reference.Contains(query.Search!) || (m.LenderName ?? "").Contains(query.Search!))
            .OrderByDescending(m => m.AppliedOn);

        return await PageAsync(q, query, rows => MapMortgagesAsync(rows, includeDisbursements: false));
    }

    private async Task<List<CustomerMortgageDto>> MapMortgagesAsync(
        List<CustomerMortgage> mortgages, bool includeDisbursements)
    {
        if (mortgages.Count == 0) return [];

        var currency = await CurrencyAsync();
        var settings = await SettingsAsync();
        var ids = mortgages.Select(m => m.Id).ToList();

        var names = await PartyNamesAsync(mortgages.Select(m => m.PartyId));

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => mortgages.Select(m => m.BookingId).Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Reference);

        var disbursements = await Db.MortgageDisbursements.ForCompany(Tenant)
            .Where(d => ids.Contains(d.CustomerMortgageId))
            .OrderBy(d => d.SequenceNumber)
            .ToListAsync();

        var milestoneNames = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => disbursements.Where(d => d.ProjectMilestoneId != null)
                                     .Select(d => d.ProjectMilestoneId!.Value)
                                     .Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        return mortgages.Select(m =>
        {
            var mine = disbursements.Where(d => d.CustomerMortgageId == m.Id).ToList();

            return new CustomerMortgageDto
            {
                Id = m.Id,
                Reference = m.Reference,
                BookingId = m.BookingId,
                BookingReference = bookings.GetValueOrDefault(m.BookingId, "—"),
                PartyId = m.PartyId,
                PartyName = names.GetValueOrDefault(m.PartyId, "—"),
                LenderName = m.LenderName,
                Status = m.Status,
                AppliedAmount = m.AppliedAmount,
                SanctionedAmount = m.SanctionedAmount,
                DisbursedAmount = m.DisbursedAmount,
                PendingDisbursement = RealEstateMapper.Money(Math.Max(0m, m.SanctionedAmount - m.DisbursedAmount)),
                InterestRate = m.InterestRate,
                TenureYears = m.TenureYears,
                CurrencyCode = currency,
                AppliedOn = m.AppliedOn,
                SanctionedOn = m.SanctionedOn,
                SanctionValidUntil = m.SanctionValidUntil,
                SanctionExpiringSoon = m.SanctionValidUntil is not null
                    && m.DisbursedAmount < m.SanctionedAmount
                    && m.SanctionValidUntil.Value.DayNumber - Today.DayNumber <= 30,
                TripartiteAgreementSigned = m.TripartiteAgreementSigned,
                NocToMortgageId = m.NocToMortgageId,
                RejectionReason = m.RejectionReason,
                DocumentUrl = m.DocumentUrl,
                Disbursements = includeDisbursements
                    ? mine.Select(d => new MortgageDisbursementDto
                    {
                        Id = d.Id,
                        SequenceNumber = d.SequenceNumber,
                        ExpectedOn = d.ExpectedOn,
                        ReceivedOn = d.ReceivedOn,
                        Amount = d.Amount,
                        ProjectMilestoneId = d.ProjectMilestoneId,
                        MilestoneName = d.ProjectMilestoneId is null
                            ? null
                            : milestoneNames.GetValueOrDefault(d.ProjectMilestoneId.Value),
                        DemandId = d.DemandId,
                        BankReference = d.BankReference,
                        IsReceived = d.IsReceived,
                        IsOverdue = !d.IsReceived && d.ExpectedOn is not null
                            && d.ExpectedOn.Value.DayNumber + settings.DepositRegistrationDays < Today.DayNumber,
                        DelayReason = d.DelayReason,
                    }).ToList()
                    : [],
            };
        }).ToList();
    }

    public async Task<CustomerMortgageDto> SaveMortgageAsync(CustomerMortgageDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var mortgage = isNew
            ? new CustomerMortgage { Reference = await numbering.NextMasterCodeAsync(Db.CustomerMortgages, "MTG") }
            : await Db.CustomerMortgages.ForCompany(Tenant).FirstOrDefaultAsync(m => m.Id == dto.Id)
              ?? throw new InvalidOperationException("That mortgage does not exist.");

        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        // The bank cannot sanction more than the flat is being sold for. Where it appears to, one of
        // the two numbers is wrong and the discrepancy needs settling before anybody disburses.
        if (dto.SanctionedAmount > booking.TotalConsideration + 0.01m && booking.TotalConsideration > 0m)
            throw new InvalidOperationException(
                $"The sanction of {dto.SanctionedAmount:N0} is more than the total consideration of "
                + $"{booking.TotalConsideration:N0} on booking {booking.Reference}.");

        if (!isNew && dto.SanctionedAmount < mortgage.DisbursedAmount)
            throw new InvalidOperationException(
                $"{mortgage.DisbursedAmount:N0} has already been disbursed. The sanction cannot be set below that.");

        mortgage.BookingId = dto.BookingId;
        mortgage.PartyId = dto.PartyId == Guid.Empty ? booking.PrimaryApplicantPartyId : dto.PartyId;
        mortgage.LenderName = dto.LenderName;
        mortgage.Status = dto.Status;
        mortgage.AppliedAmount = dto.AppliedAmount;
        mortgage.SanctionedAmount = dto.SanctionedAmount;
        mortgage.InterestRate = dto.InterestRate;
        mortgage.TenureYears = dto.TenureYears;
        mortgage.AppliedOn = dto.AppliedOn;
        mortgage.SanctionedOn = dto.SanctionedOn;
        mortgage.SanctionValidUntil = dto.SanctionValidUntil;
        mortgage.TripartiteAgreementSigned = dto.TripartiteAgreementSigned;
        mortgage.NocToMortgageId = dto.NocToMortgageId;
        mortgage.RejectionReason = dto.RejectionReason;
        mortgage.DocumentUrl = dto.DocumentUrl;

        if (dto.TripartiteAgreementSigned && mortgage.TripartiteSignedOn is null)
            mortgage.TripartiteSignedOn = Today;

        if (isNew)
        {
            mortgage.StampNew(Tenant, userId);
            Db.CustomerMortgages.Add(mortgage);
        }
        else
        {
            mortgage.StampUpdated(userId);
        }

        // The booking carries the pointer, because collections needs to know a mortgage is coming
        // before it starts chasing the customer for an instalment the bank is going to pay.
        if (booking.CustomerMortgageId != mortgage.Id)
        {
            booking.CustomerMortgageId = mortgage.Id;
            booking.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await MapMortgagesAsync([mortgage], includeDisbursements: true))[0];
    }

    public async Task<MortgageDisbursementDto> RecordDisbursementAsync(
        Guid mortgageId, MortgageDisbursementDto dto, Guid userId)
    {
        var mortgage = await RequireAsync<CustomerMortgage>(mortgageId, "That mortgage does not exist.");

        if (dto.Amount <= 0m)
            throw new InvalidOperationException("A disbursement has to be for more than nothing.");

        var pending = mortgage.SanctionedAmount - mortgage.DisbursedAmount;

        if (dto.IsReceived && dto.Amount > pending + 0.01m)
            throw new InvalidOperationException(
                $"Only {Math.Max(0m, pending):N0} remains to be disbursed against a sanction of "
                + $"{mortgage.SanctionedAmount:N0}.");

        // The tripartite agreement is what makes the bank's money the developer's money. Without
        // it, a disbursement is a payment nobody has agreed the terms of.
        if (dto.IsReceived && !mortgage.TripartiteAgreementSigned)
            throw new InvalidOperationException(
                "The tripartite agreement between the buyer, the bank and the developer has not been signed. "
                + "Record it before booking a disbursement against this mortgage.");

        var nextSequence = await Db.MortgageDisbursements.ForCompany(Tenant)
            .Where(d => d.CustomerMortgageId == mortgageId)
            .Select(d => (int?)d.SequenceNumber)
            .MaxAsync() ?? 0;

        var disbursement = new MortgageDisbursement
        {
            CustomerMortgageId = mortgageId,
            BookingId = mortgage.BookingId,
            SequenceNumber = dto.SequenceNumber > 0 ? dto.SequenceNumber : nextSequence + 1,
            ExpectedOn = dto.ExpectedOn,
            ReceivedOn = dto.ReceivedOn,
            Amount = RealEstateMapper.Money(dto.Amount),
            ProjectMilestoneId = dto.ProjectMilestoneId,
            DemandId = dto.DemandId,
            BankReference = dto.BankReference,
            IsReceived = dto.IsReceived,
            DelayReason = dto.DelayReason,
        }.StampNew(Tenant, userId);

        Db.MortgageDisbursements.Add(disbursement);

        if (disbursement.IsReceived)
        {
            mortgage.DisbursedAmount = RealEstateMapper.Money(mortgage.DisbursedAmount + disbursement.Amount);
            mortgage.ReceivedOnStatus();
            mortgage.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        var milestoneName = disbursement.ProjectMilestoneId is null
            ? null
            : await Db.ProjectMilestones.ForCompany(Tenant)
                .Where(m => m.Id == disbursement.ProjectMilestoneId)
                .Select(m => m.Name)
                .FirstOrDefaultAsync();

        return new MortgageDisbursementDto
        {
            Id = disbursement.Id,
            SequenceNumber = disbursement.SequenceNumber,
            ExpectedOn = disbursement.ExpectedOn,
            ReceivedOn = disbursement.ReceivedOn,
            Amount = disbursement.Amount,
            ProjectMilestoneId = disbursement.ProjectMilestoneId,
            MilestoneName = milestoneName,
            DemandId = disbursement.DemandId,
            BankReference = disbursement.BankReference,
            IsReceived = disbursement.IsReceived,
            DelayReason = disbursement.DelayReason,
        };
    }
}

internal static class MortgageExtensions
{
    /// <summary>
    /// Moves the mortgage's status on once money has actually arrived, so that the pipeline board
    /// reflects cash rather than paperwork.
    /// </summary>
    public static void ReceivedOnStatus(this CustomerMortgage mortgage)
    {
        mortgage.Status = mortgage.DisbursedAmount >= mortgage.SanctionedAmount - 0.01m
            ? "FullyDisbursed"
            : "PartiallyDisbursed";
    }
}
