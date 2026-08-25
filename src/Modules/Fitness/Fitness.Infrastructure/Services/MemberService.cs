using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// The member lifecycle.
///
/// The two things worth reading here are <see cref="JoinAsync"/> — which does the whole join in
/// one transaction so a half-joined member cannot exist — and <see cref="RefreshAlertsAsync"/>,
/// which materialises everything the door and the desk need to know about a person into rows, so
/// a turnstile decision is one indexed read rather than twelve business rules evaluated live.
/// </summary>
public class MemberService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IAgreementService agreements,
    IBillingService billing,
    IComplianceService compliance) : IMemberService
{
    public async Task<PaginatedResponse<MemberSummaryDto>> ListAsync(
        Guid? clubId, MemberStatus? status, ChurnRiskBand? riskBand, string? search,
        Guid? planId, bool? hasBalance, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.Members.ForTenant(tenant)
            .Include(m => m.HomeClub)
            .WhereIf(clubId is not null, m => m.HomeClubId == clubId)
            .WhereIf(status is not null, m => m.Status == status)
            .WhereIf(riskBand is not null, m => m.RiskBand == riskBand)
            .WhereIf(hasBalance == true, m => m.AccountBalance > 0);

        if (planId is not null)
        {
            var memberIds = db.Agreements.ForTenant(tenant)
                .Where(a => a.PlanId == planId && a.Status == AgreementStatus.Active)
                .Select(a => a.MemberId);
            query = query.Where(m => memberIds.Contains(m.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.FirstName.ToLower().Contains(term) ||
                m.LastName.ToLower().Contains(term) ||
                m.MemberNumber.ToLower().Contains(term) ||
                (m.Phone != null && m.Phone.Contains(term)) ||
                (m.Email != null && m.Email.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var ids = page.Select(m => m.Id).ToList();

        // Two extra queries for the whole page, rather than two per row.
        var planNames = await db.Agreements.ForTenant(tenant)
            .Where(a => ids.Contains(a.MemberId) && a.Status == AgreementStatus.Active)
            .Select(a => new { a.MemberId, PlanName = a.Plan!.Name })
            .ToListAsync();

        var alerts = await db.MemberAlerts.ForTenant(tenant)
            .Where(a => ids.Contains(a.MemberId) && a.AcknowledgedAt == null)
            .GroupBy(a => a.MemberId)
            .Select(g => new { MemberId = g.Key, Count = g.Count(), Blocking = g.Any(a => a.BlocksAccess) })
            .ToListAsync();

        var checkedIn = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.MemberId != null && ids.Contains(c.MemberId.Value) && c.CheckedOutAt == null)
            .Select(c => c.MemberId!.Value)
            .Distinct()
            .ToListAsync();

        var items = page.Select(m =>
        {
            var dto = FitnessMapper.ToSummary(m, now);
            dto.PlanName = planNames.FirstOrDefault(p => p.MemberId == m.Id)?.PlanName;
            var alert = alerts.FirstOrDefault(a => a.MemberId == m.Id);
            dto.AlertCount = alert?.Count ?? 0;
            dto.HasBlockingAlert = alert?.Blocking ?? false;
            dto.IsCheckedIn = checkedIn.Contains(m.Id);
            return dto;
        }).ToList();

        return PaginatedResponse<MemberSummaryDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    /// <summary>
    /// The desk lookup. Ordered so an exact member-number or phone match jumps to the top —
    /// a receptionist who scanned a fob wants that person first, not the alphabetically first
    /// person whose surname contains the same three letters.
    /// </summary>
    public async Task<List<MemberSummaryDto>> SearchAsync(MemberSearchDto request)
    {
        var now = DateTime.UtcNow;
        var term = (request.Query ?? string.Empty).Trim().ToLower();
        if (term.Length < 2) return [];

        var query = db.Members.ForTenant(tenant)
            .Include(m => m.HomeClub)
            .WhereIf(request.ClubId is not null, m => m.HomeClubId == request.ClubId)
            .WhereIf(!request.IncludeInactive, m => m.Status != MemberStatus.Cancelled && m.Status != MemberStatus.Expired)
            .Where(m =>
                m.FirstName.ToLower().StartsWith(term) ||
                m.LastName.ToLower().StartsWith(term) ||
                m.MemberNumber.ToLower().Contains(term) ||
                (m.PreferredName != null && m.PreferredName.ToLower().StartsWith(term)) ||
                (m.Phone != null && m.Phone.Contains(term)) ||
                (m.Email != null && m.Email.ToLower().StartsWith(term)));

        var matches = await query.Take(request.Limit * 3).ToListAsync();

        var ranked = matches
            .OrderByDescending(m => m.MemberNumber.Equals(term, StringComparison.OrdinalIgnoreCase) ? 3 : 0)
            .ThenByDescending(m => m.Phone == term ? 2 : 0)
            .ThenByDescending(m => m.Status == MemberStatus.Active ? 1 : 0)
            .ThenBy(m => m.LastName)
            .Take(request.Limit)
            .ToList();

        return [.. ranked.Select(m => FitnessMapper.ToSummary(m, now))];
    }

    public async Task<MemberDetailDto?> GetAsync(Guid memberId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant)
            .Include(m => m.HomeClub)
            .Include(m => m.Household)
            .Include(m => m.Tags.Where(t => !t.IsDeleted))
            .Include(m => m.EmergencyContacts.Where(c => !c.IsDeleted))
            .Include(m => m.Credentials.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member is null) return null;

        var dto = FitnessMapper.ToDetail(member, now);

        dto.Agreements = [.. (await db.Agreements.ForTenant(tenant)
            .Where(a => a.MemberId == memberId)
            .Include(a => a.Plan)
            .Include(a => a.Freezes.Where(f => !f.IsDeleted))
            .OrderByDescending(a => a.StartsOn)
            .ToListAsync())
            .Select(a => FitnessMapper.ToSummary(a, now))];

        dto.Alerts = [.. (await db.MemberAlerts.ForTenant(tenant)
            .Where(a => a.MemberId == memberId && a.AcknowledgedAt == null
                     && (a.ExpiresOn == null || a.ExpiresOn > now))
            .OrderByDescending(a => a.Severity)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        dto.MedicalFlags = [.. (await db.MedicalFlags.ForTenant(tenant)
            .Where(f => f.MemberId == memberId && f.ResolvedOn == null)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        dto.Credits = [.. (await db.SessionCredits.ForTenant(tenant)
            .Where(c => c.MemberId == memberId && !c.IsExpired && c.Remaining > 0)
            .ToListAsync())
            .Select(c => FitnessMapper.ToCreditSummary(c, now, DescribeCredit(c)))];

        if (member.HouseholdId is not null)
        {
            dto.HouseholdMembers = [.. (await db.HouseholdMembers.ForTenant(tenant)
                .Where(h => h.HouseholdId == member.HouseholdId && h.MemberId != memberId)
                .Include(h => h.Member)
                .ToListAsync())
                .Select(h => FitnessMapper.ToDto(h, now))];
        }

        dto.UpcomingBookings = await GetUpcomingAsync(memberId);

        // Money roll-ups the record leads with.
        var activeAgreement = await db.Agreements.ForTenant(tenant)
            .Where(a => a.MemberId == memberId && a.Status == AgreementStatus.Active)
            .OrderBy(a => a.NextBillingOn)
            .FirstOrDefaultAsync();

        dto.NextBillingAmount = activeAgreement is null
            ? null
            : activeAgreement.PromotionalPeriodsRemaining > 0
                ? activeAgreement.PromotionalPrice ?? activeAgreement.Price
                : activeAgreement.Price;

        dto.LifetimeValue = await db.Payments.ForTenant(tenant)
            .Where(p => p.MemberId == memberId && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount)) ?? 0m;

        var score = await db.ChurnScores.ForTenant(tenant)
            .Include(c => c.Factors.Where(f => !f.IsDeleted))
            .FirstOrDefaultAsync(c => c.MemberId == memberId);

        if (score is not null)
        {
            dto.RiskScore = score.Score;
            dto.RiskReasons = [.. score.Factors.OrderByDescending(f => f.Weight).Select(f => f.Explanation)];
        }

        var openCheckIn = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.MemberId == memberId && c.CheckedOutAt == null)
            .OrderByDescending(c => c.CheckedInAt)
            .FirstOrDefaultAsync();

        dto.IsCheckedIn = openCheckIn is not null;
        dto.CheckedInAt = openCheckIn?.CheckedInAt;

        dto.WaiverCurrent = await IsWaiverCurrentAsync(memberId, now);

        if (member.AssignedCoachId is not null)
        {
            dto.AssignedCoachName = await db.Staff.ForTenant(tenant)
                .Where(s => s.Id == member.AssignedCoachId)
                .Select(s => s.FirstName + " " + s.LastName)
                .FirstOrDefaultAsync();
        }

        if (member.CorporateAccountId is not null)
        {
            dto.CorporateAccountName = await db.CorporateAccounts.ForTenant(tenant)
                .Where(c => c.Id == member.CorporateAccountId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        if (member.LeadSourceId is not null)
        {
            dto.LeadSourceName = await db.LeadSources.ForTenant(tenant)
                .Where(s => s.Id == member.LeadSourceId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
        }

        if (member.ReferredByMemberId is not null)
        {
            dto.ReferredByName = await db.Members.ForTenant(tenant)
                .Where(m => m.Id == member.ReferredByMemberId)
                .Select(m => m.FirstName + " " + m.LastName)
                .FirstOrDefaultAsync();
        }

        var tier = await db.LoyaltyAccounts.ForTenant(tenant)
            .Where(l => l.MemberId == memberId)
            .Select(l => l.Tier!.Name)
            .FirstOrDefaultAsync();
        dto.LoyaltyTierName = tier;

        return dto;
    }

    public async Task<MemberDetailDto> CreateAsync(SaveMemberDto request, Guid userId)
    {
        var settings = await GetSettingsAsync();

        var member = new Member
        {
            MemberNumber = await numbering.NextMemberNumberAsync(settings.MemberNumberPrefix),
            Status = MemberStatus.Lead,
        }.StampNew(tenant, userId);

        ApplyMember(member, request);
        db.Members.Add(member);

        AddEmergencyContacts(member, request.EmergencyContacts, userId);
        AddTags(member.Id, request.Tags, userId);

        await db.SaveChangesAsync();
        await RefreshAlertsAsync(member.Id);

        return (await GetAsync(member.Id))!;
    }

    public async Task<MemberDetailDto> UpdateAsync(Guid memberId, SaveMemberDto request, Guid userId)
    {
        var member = await db.Members.ForTenant(tenant)
            .Include(m => m.EmergencyContacts.Where(c => !c.IsDeleted))
            .Include(m => m.Tags.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");

        if (member.IsAnonymised)
            throw new InvalidOperationException("This member has been erased and cannot be edited.");

        ApplyMember(member, request);
        member.StampUpdated(userId);

        // Replace the emergency-contact set wholesale — it is small, and a partial edit is harder
        // to reason about than a replacement.
        foreach (var existing in member.EmergencyContacts.Where(c => !c.IsDeleted))
            existing.StampDeleted(userId);
        AddEmergencyContacts(member, request.EmergencyContacts, userId);

        var currentTags = member.Tags.Where(t => !t.IsDeleted && !t.IsSystemTag).ToList();
        foreach (var gone in currentTags.Where(t => !request.Tags.Contains(t.Tag)))
            gone.StampDeleted(userId);
        AddTags(member.Id, request.Tags.Where(t => !currentTags.Any(c => c.Tag == t)).ToList(), userId);

        await db.SaveChangesAsync();
        await RefreshAlertsAsync(memberId);

        return (await GetAsync(memberId))!;
    }

    /// <summary>
    /// Person, plan, paperwork and money, in one transaction.
    ///
    /// The order matters: the member exists before the waiver can attach to them, the waiver is
    /// signed before the agreement is active, and the credential is issued last so a fob is never
    /// handed over for a membership that failed to save. If any step throws, nothing is written —
    /// which is the difference between "the join failed, try again" and a member who exists but
    /// cannot get in.
    /// </summary>
    public async Task<JoinResultDto> JoinAsync(JoinMemberDto request, Guid userId)
    {
        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        var plan = await db.Plans.ForTenant(tenant)
            .Include(p => p.Entitlements).ThenInclude(e => e.TimeBands)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId)
            ?? throw new InvalidOperationException("Plan not found.");

        // ── Eligibility, before anything is written ──────────────────────────

        var age = FitnessMapper.AgeOn(request.Member.DateOfBirth, now);

        if (plan.MinimumAge is not null && (age is null || age < plan.MinimumAge))
            throw new InvalidOperationException($"{plan.Name} is only available from age {plan.MinimumAge}.");

        if (plan.MaximumAge is not null && age is not null && age > plan.MaximumAge)
            throw new InvalidOperationException($"{plan.Name} is only available up to age {plan.MaximumAge}.");

        if (age is not null && age < club.MinimumAge && request.GuardianName is null)
            throw new InvalidOperationException(
                $"Members under {club.MinimumAge} need a parent or guardian to sign. Add a guardian to continue.");

        var settings = await GetSettingsAsync();

        // ── Member ───────────────────────────────────────────────────────────

        request.Member.HomeClubId = request.ClubId;

        var member = new Member
        {
            MemberNumber = await numbering.NextMemberNumberAsync(settings.MemberNumberPrefix),
            Status = MemberStatus.Active,
            JoinedOn = request.StartsOn.Date,
            FirstJoinedOn = request.StartsOn.Date,
        }.StampNew(tenant, userId);

        ApplyMember(member, request.Member);
        db.Members.Add(member);
        AddEmergencyContacts(member, request.Member.EmergencyContacts, userId);
        AddTags(member.Id, request.Member.Tags, userId);

        foreach (var consent in request.Consents)
        {
            db.Consents.Add(new MemberConsent
            {
                MemberId = member.Id,
                Channel = consent.Channel,
                Purpose = consent.Purpose,
                Granted = consent.Granted,
                ConsentText = consent.ConsentText,
                CapturedVia = consent.CapturedVia ?? "Join wizard",
                DecidedAt = now,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        // ── Paperwork ────────────────────────────────────────────────────────

        var outstanding = new List<string>();

        var waiverTemplateId = request.WaiverTemplateId ?? plan.WaiverTemplateId;
        if (waiverTemplateId is not null && request.SignatureImageUrl is not null)
        {
            await compliance.SignWaiverAsync(new SignWaiverDto
            {
                WaiverTemplateId = waiverTemplateId.Value,
                ClubId = request.ClubId,
                MemberId = member.Id,
                SignerName = FitnessMapper.FullName(member),
                SignatureImageUrl = request.SignatureImageUrl,
                GuardianName = request.GuardianName,
                GuardianRelationship = request.GuardianRelationship,
                CapturedVia = "Join wizard",
            }, userId);
        }
        else if (club.RequiresWaiver)
        {
            outstanding.Add("Waiver not signed");
        }

        if (request.HealthScreening is not null)
        {
            request.HealthScreening.MemberId = member.Id;
            request.HealthScreening.ClubId = request.ClubId;
            var screening = await compliance.SubmitScreeningAsync(request.HealthScreening, userId);
            if (screening.RequiresClearance)
                outstanding.Add("Medical clearance required before training");
        }
        else if (club.RequiresHealthScreening && plan.RequiresHealthScreening)
        {
            outstanding.Add("Health screening not completed");
        }

        // ── Agreement ────────────────────────────────────────────────────────

        var agreement = await agreements.CreateAsync(new CreateAgreementDto
        {
            MemberId = member.Id,
            PlanId = request.PlanId,
            ClubId = request.ClubId,
            StartsOn = request.StartsOn,
            PriceOverride = request.PriceOverride,
            PriceOverrideReason = request.PriceOverrideReason,
            PromotionRuleId = request.PromotionRuleId,
            PromoCodeText = request.PromoCodeText,
            WaiveJoiningFee = request.WaiveJoiningFee,
            PaymentMethodRefId = request.PaymentMethodRefId,
            SoldByStaffId = request.SoldByStaffId,
            LeadId = request.LeadId,
            TakeFirstPaymentNow = request.TakeFirstPaymentNow,
        }, userId);

        // ── Money ────────────────────────────────────────────────────────────

        decimal amountPaid = 0, changeDue = 0;
        Guid? invoiceId = null;

        var firstInvoice = await db.Invoices.ForTenant(tenant)
            .Where(i => i.AgreementId == agreement.Id)
            .OrderBy(i => i.IssuedOn)
            .FirstOrDefaultAsync();

        if (firstInvoice is not null)
        {
            invoiceId = firstInvoice.Id;

            if (request.TakeFirstPaymentNow && firstInvoice.BalanceDue > 0)
            {
                var payment = await billing.TakePaymentAsync(new TakePaymentDto
                {
                    MemberId = member.Id,
                    ClubId = request.ClubId,
                    InvoiceId = firstInvoice.Id,
                    Amount = firstInvoice.BalanceDue,
                    Method = request.PaymentMethod,
                    PaymentMethodRefId = request.PaymentMethodRefId,
                    AmountTendered = request.AmountTendered,
                    Notes = "First payment, taken at join",
                }, userId);

                amountPaid = payment.AmountTaken;
                changeDue = payment.ChangeDue;
            }
            else if (firstInvoice.BalanceDue > 0)
            {
                outstanding.Add($"First payment of {firstInvoice.BalanceDue:0.00} still due");
            }
        }

        // ── Access ───────────────────────────────────────────────────────────

        Guid? credentialId = null;
        if (request.CredentialType is not null && !string.IsNullOrWhiteSpace(request.CredentialIdentifier))
        {
            var credential = await IssueCredentialAsync(new IssueCredentialDto
            {
                MemberId = member.Id,
                Type = request.CredentialType.Value,
                Identifier = request.CredentialIdentifier!,
            }, userId);
            credentialId = credential.Id;
        }
        else
        {
            outstanding.Add("No entry credential issued yet");
        }

        // ── Wrap up ──────────────────────────────────────────────────────────

        db.MemberStatusHistory.Add(new MemberStatusHistory
        {
            MemberId = member.Id,
            FromStatus = MemberStatus.Lead,
            ToStatus = MemberStatus.Active,
            ChangedAt = now,
            Reason = "Joined",
            ChangedByUserId = userId,
            SourceEntityId = agreement.Id,
            SourceEntityType = nameof(Agreement),
        }.StampNew(tenant, userId));

        db.LoyaltyAccounts.Add(new LoyaltyAccount
        {
            MemberId = member.Id,
            ClubId = request.ClubId,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        await RefreshAlertsAsync(member.Id);
        await tx.CommitAsync();

        return new JoinResultDto
        {
            MemberId = member.Id,
            MemberNumber = member.MemberNumber,
            AgreementId = agreement.Id,
            AgreementNumber = agreement.AgreementNumber,
            InvoiceId = invoiceId,
            AmountDue = firstInvoice?.Total ?? 0,
            AmountPaid = amountPaid,
            ChangeDue = changeDue,
            FirstBillingOn = agreement.NextBillingOn ?? request.StartsOn,
            RecurringAmount = agreement.Price,
            CredentialId = credentialId,
            OutstandingRequirements = outstanding,
        };
    }

    public async Task<MemberDetailDto> ChangeStatusAsync(ChangeMemberStatusDto request, Guid userId)
    {
        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        if (member.Status == request.NewStatus) return (await GetAsync(member.Id))!;

        var from = member.Status;
        member.Status = request.NewStatus;
        member.StampUpdated(userId);

        if (request.NewStatus is MemberStatus.Cancelled or MemberStatus.Expired && member.LeftOn is null)
            member.LeftOn = DateTime.UtcNow.Date;

        // A rejoin keeps the original first-join date so tenure and win-back reporting stay honest.
        if (request.NewStatus == MemberStatus.Active && from is MemberStatus.Cancelled or MemberStatus.Expired)
        {
            member.Status = MemberStatus.WonBack;
            member.JoinedOn = DateTime.UtcNow.Date;
            member.LeftOn = null;
        }

        db.MemberStatusHistory.Add(new MemberStatusHistory
        {
            MemberId = member.Id,
            FromStatus = from,
            ToStatus = member.Status,
            ChangedAt = DateTime.UtcNow,
            Reason = request.Reason,
            ChangedByUserId = userId,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        await RefreshAlertsAsync(member.Id);

        return (await GetAsync(member.Id))!;
    }

    public async Task<MemberDetailDto> SetBanAsync(BanMemberDto request, Guid userId)
    {
        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        if (request.IsBanned && string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("A ban needs a reason — it is the only record of why entry was refused.");

        member.IsBanned = request.IsBanned;
        member.BanReason = request.IsBanned ? request.Reason : null;
        member.BanUntil = request.IsBanned ? request.Until : null;
        member.BannedByUserId = request.IsBanned ? userId : null;
        member.StampUpdated(userId);

        db.MemberNotes.Add(new MemberNote
        {
            MemberId = member.Id,
            Kind = InteractionKind.SystemEvent,
            Body = request.IsBanned
                ? $"Banned: {request.Reason}" + (request.Until is not null ? $" (until {request.Until:d MMM yyyy})" : " (indefinitely)")
                : "Ban lifted",
            IsPinned = request.IsBanned,
            OccurredAt = DateTime.UtcNow,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        await RefreshAlertsAsync(member.Id);
        await compliance.LogAsync(request.IsBanned ? "Banned" : "BanLifted", nameof(Member), member.Id,
            member.Id, request.Reason, false, userId);

        return (await GetAsync(member.Id))!;
    }

    // ── Timeline & attachments ───────────────────────────────────────────────

    public async Task<List<MemberNoteDto>> GetTimelineAsync(Guid memberId, int limit = 100)
    {
        var notes = await db.MemberNotes.ForTenant(tenant)
            .Where(n => n.MemberId == memberId)
            .OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.OccurredAt)
            .Take(limit)
            .ToListAsync();

        return [.. notes.Select(FitnessMapper.ToDto)];
    }

    public async Task<MemberNoteDto> AddNoteAsync(MemberNoteDto request, Guid userId)
    {
        var note = new MemberNote
        {
            MemberId = request.MemberId,
            Kind = request.Kind,
            Body = request.Body,
            OccurredAt = request.OccurredAt == default ? DateTime.UtcNow : request.OccurredAt,
            IsPrivate = request.IsPrivate,
            IsPinned = request.IsPinned,
            StaffId = request.StaffId,
            AuthorName = request.AuthorName,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = request.RelatedEntityType,
        }.StampNew(tenant, userId);

        db.MemberNotes.Add(note);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(note);
    }

    public async Task<List<MemberAlertDto>> GetAlertsAsync(Guid memberId)
    {
        var now = DateTime.UtcNow;
        var alerts = await db.MemberAlerts.ForTenant(tenant)
            .Where(a => a.MemberId == memberId && a.AcknowledgedAt == null
                     && (a.ExpiresOn == null || a.ExpiresOn > now))
            .OrderByDescending(a => a.Severity)
            .ToListAsync();

        return [.. alerts.Select(FitnessMapper.ToDto)];
    }

    /// <summary>
    /// Rebuilds the alert rows for one member.
    ///
    /// Alerts are materialised rather than computed on read because they are read on every single
    /// door scan, and a turnstile has 300 ms. Recomputing "does this person owe money, is their
    /// waiver current, are they frozen, is it their birthday" on every read would not fit, and
    /// caching it in the application would go stale the moment somebody paid at the desk.
    ///
    /// Called after anything that could change the answer: a payment, a status change, a waiver,
    /// a freeze, a ban.
    /// </summary>
    public async Task RefreshAlertsAsync(Guid memberId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId);
        if (member is null) return;

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == member.HomeClubId);
        var threshold = club?.AccessBalanceThreshold ?? 0m;

        var existing = await db.MemberAlerts.ForTenant(tenant)
            .Where(a => a.MemberId == memberId)
            .ToListAsync();

        var wanted = new List<MemberAlert>();

        void Want(MemberAlertKind kind, AlertSeverity severity, string message,
                  bool blocks = false, string? actionLabel = null, string? route = null, DateTime? expires = null)
            => wanted.Add(new MemberAlert
            {
                MemberId = memberId,
                Kind = kind,
                Severity = severity,
                Message = message,
                BlocksAccess = blocks,
                ActionLabel = actionLabel,
                ActionRoute = route,
                ExpiresOn = expires,
            });

        // ── Blocking ─────────────────────────────────────────────────────────

        if (member.IsBanned && (member.BanUntil is null || member.BanUntil > now))
        {
            Want(MemberAlertKind.Banned, AlertSeverity.Blocking,
                member.BanUntil is null
                    ? $"Banned — {member.BanReason}"
                    : $"Banned until {member.BanUntil:d MMM yyyy} — {member.BanReason}",
                blocks: true);
        }

        if (member.AccountBalance > threshold)
        {
            var overdue = await db.Invoices.ForTenant(tenant)
                .AnyAsync(i => i.MemberId == memberId && i.BalanceDue > 0 && i.DueOn < now);

            Want(MemberAlertKind.OutstandingBalance,
                overdue ? AlertSeverity.Blocking : AlertSeverity.Warning,
                $"Balance of {member.AccountBalance:0.00} outstanding",
                blocks: overdue,
                actionLabel: "Take payment",
                route: $"/fitness/members/{memberId}?tab=money");
        }

        if (club?.RequiresWaiver == true && !await IsWaiverCurrentAsync(memberId, now))
        {
            Want(member.WaiverSigned ? MemberAlertKind.WaiverExpired : MemberAlertKind.WaiverMissing,
                AlertSeverity.Blocking,
                member.WaiverSigned ? "Waiver has expired or been superseded" : "Waiver not signed",
                blocks: true,
                actionLabel: "Sign waiver",
                route: $"/fitness/members/{memberId}?tab=documents");
        }

        if (member.MedicalClearance is ClearanceStatus.Required or ClearanceStatus.Submitted or ClearanceStatus.Expired)
        {
            Want(MemberAlertKind.MedicalClearanceRequired, AlertSeverity.Blocking,
                "Medical clearance outstanding",
                blocks: true,
                actionLabel: "View clearance",
                route: $"/fitness/members/{memberId}?tab=health");
        }

        // ── Warnings ─────────────────────────────────────────────────────────

        var expiringCard = await db.PaymentMethods.ForTenant(tenant)
            .Where(p => p.MemberId == memberId && p.IsActive && p.IsDefault
                     && p.ExpiryYear != null && p.ExpiryMonth != null)
            .ToListAsync();

        foreach (var card in expiringCard)
        {
            var expiry = new DateTime(card.ExpiryYear!.Value, card.ExpiryMonth!.Value, 1).AddMonths(1);
            if (expiry <= now.AddMonths(2))
            {
                Want(MemberAlertKind.CardExpiring, AlertSeverity.Warning,
                    expiry <= now
                        ? $"Card ending {card.CardLastFour} has expired"
                        : $"Card ending {card.CardLastFour} expires {card.ExpiryMonth:00}/{card.ExpiryYear % 100:00}",
                    actionLabel: "Update card",
                    route: $"/fitness/members/{memberId}?tab=money");
            }
        }

        var frozen = await db.Freezes.ForTenant(tenant)
            .Where(f => f.MemberId == memberId && !f.IsReleased && f.StartsOn <= now && f.EndsOn >= now)
            .FirstOrDefaultAsync();

        if (frozen is not null)
        {
            Want(MemberAlertKind.Custom, AlertSeverity.Blocking,
                $"Membership frozen until {frozen.EndsOn:d MMM yyyy}",
                blocks: true,
                actionLabel: "End freeze",
                route: $"/fitness/members/{memberId}?tab=membership");
        }

        var strikes = await db.Strikes.ForTenant(tenant)
            .CountAsync(s => s.MemberId == memberId && !s.IsWaived && s.ExpiresOn > now);

        if (strikes >= 2)
        {
            Want(MemberAlertKind.NoShowStreak, AlertSeverity.Warning,
                $"{strikes} late cancellations or no-shows in the last month");
        }

        // ── Information the desk should greet them with ──────────────────────

        if (member.DateOfBirth is not null &&
            member.DateOfBirth.Value.Month == now.Month && member.DateOfBirth.Value.Day == now.Day)
        {
            Want(MemberAlertKind.Birthday, AlertSeverity.Info, "It's their birthday today",
                expires: now.Date.AddDays(1));
        }

        if (member.TotalVisits == 0 && member.Status == MemberStatus.Active)
        {
            Want(MemberAlertKind.FirstVisit, AlertSeverity.Info,
                "First visit since joining — worth a proper welcome");
        }
        else if (member.TotalVisits is 49 or 99 or 249 or 499 or 999)
        {
            Want(MemberAlertKind.VisitMilestone, AlertSeverity.Info,
                $"Next visit is their {member.TotalVisits + 1}th");
        }

        var endingAgreement = await db.Agreements.ForTenant(tenant)
            .Where(a => a.MemberId == memberId && a.Status == AgreementStatus.NoticeGiven)
            .OrderBy(a => a.CancellationEffectiveOn)
            .FirstOrDefaultAsync();

        if (endingAgreement?.CancellationEffectiveOn is not null)
        {
            Want(MemberAlertKind.ContractEnding, AlertSeverity.Warning,
                $"Leaving on {endingAgreement.CancellationEffectiveOn:d MMM yyyy}",
                actionLabel: "Try to save",
                route: $"/fitness/members/{memberId}?tab=membership");
        }

        var expiringCredits = await db.SessionCredits.ForTenant(tenant)
            .Where(c => c.MemberId == memberId && !c.IsExpired && c.Remaining > 0
                     && c.ExpiresOn != null && c.ExpiresOn <= now.AddDays(30))
            .SumAsync(c => (int?)c.Remaining) ?? 0;

        if (expiringCredits > 0)
        {
            Want(MemberAlertKind.CreditsExhausted, AlertSeverity.Info,
                $"{expiringCredits} session credits expire within a month",
                actionLabel: "Book a session",
                route: $"/fitness/appointments?memberId={memberId}");
        }

        // ── Reconcile ────────────────────────────────────────────────────────
        // Matched on kind + message so an unchanged alert keeps its acknowledgement, and a changed
        // one (a different balance) reappears.

        foreach (var stale in existing.Where(e => !wanted.Any(w => w.Kind == e.Kind && w.Message == e.Message)))
            db.MemberAlerts.Remove(stale);

        foreach (var alert in wanted.Where(w => !existing.Any(e => e.Kind == w.Kind && e.Message == w.Message)))
            db.MemberAlerts.Add(alert.StampNew(tenant));

        await db.SaveChangesAsync();
    }

    public async Task<List<VisitHistoryDto>> GetVisitHistoryAsync(Guid memberId, int limit = 50)
    {
        var visits = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.CheckedInAt)
            .Take(limit)
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        return [.. visits.Select(v => FitnessMapper.ToVisitHistory(v, clubNames.GetValueOrDefault(v.ClubId, "")))];
    }

    public async Task<List<MemberLedgerEntryDto>> GetLedgerAsync(Guid memberId, int limit = 100)
    {
        var entries = await db.Ledger.ForTenant(tenant)
            .Where(l => l.MemberId == memberId)
            .OrderByDescending(l => l.OccurredAt)
            .Take(limit)
            .ToListAsync();

        var invoiceNumbers = await db.Invoices.ForTenant(tenant)
            .Where(i => i.MemberId == memberId)
            .Select(i => new { i.Id, i.InvoiceNumber })
            .ToDictionaryAsync(i => i.Id, i => i.InvoiceNumber);

        return [.. entries.Select(e =>
        {
            var dto = FitnessMapper.ToDto(e);
            if (e.InvoiceId is not null) dto.InvoiceNumber = invoiceNumbers.GetValueOrDefault(e.InvoiceId.Value);
            return dto;
        })];
    }

    /// <summary>
    /// Everything in this member's diary — classes, PT and court bookings — merged into one list.
    ///
    /// The member does not think in terms of three tables, and neither should the screen.
    /// </summary>
    public async Task<List<UpcomingBookingDto>> GetUpcomingAsync(Guid memberId)
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(60);
        var upcoming = new List<UpcomingBookingDto>();

        var classes = await db.ClassBookings.ForTenant(tenant)
            .Where(k => k.MemberId == memberId
                     && k.ClassOccurrence!.StartsAt >= now && k.ClassOccurrence.StartsAt <= horizon
                     && (k.Status == BookingStatus.Booked || k.Status == BookingStatus.Waitlisted))
            .Include(k => k.ClassOccurrence).ThenInclude(o => o!.ClassType)
            .Include(k => k.ClassOccurrence).ThenInclude(o => o!.Room)
            .ToListAsync();

        upcoming.AddRange(classes.Select(k => new UpcomingBookingDto
        {
            Id = k.Id,
            BookingType = "Class",
            Title = k.ClassOccurrence?.ClassType?.Name ?? "Class",
            StartsAt = k.ClassOccurrence!.StartsAt,
            EndsAt = k.ClassOccurrence.EndsAt,
            Location = k.ClassOccurrence.Room?.Name,
            SpotLabel = k.SpotLabel,
            Status = k.Status,
            IsWaitlisted = k.Status == BookingStatus.Waitlisted,
            WaitlistPosition = k.WaitlistPosition,
            Route = $"/fitness/classes/{k.ClassOccurrenceId}",
        }));

        var appointments = await db.Appointments.ForTenant(tenant)
            .Where(a => a.MemberId == memberId
                     && a.StartsAt >= now && a.StartsAt <= horizon
                     && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Requested))
            .Include(a => a.Service)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        upcoming.AddRange(appointments.Select(a => new UpcomingBookingDto
        {
            Id = a.Id,
            BookingType = "Appointment",
            Title = a.Service?.Name ?? "Session",
            StartsAt = a.StartsAt,
            EndsAt = a.EndsAt,
            StaffName = staffNames.GetValueOrDefault(a.StaffId),
            Status = BookingStatus.Booked,
            Route = $"/fitness/appointments/{a.Id}",
        }));

        var resources = await db.ResourceBookings.ForTenant(tenant)
            .Where(r => r.MemberId == memberId
                     && r.StartsAt >= now && r.StartsAt <= horizon
                     && r.Status == ResourceBookingStatus.Booked)
            .Include(r => r.BookableResource)
            .ToListAsync();

        upcoming.AddRange(resources.Select(r => new UpcomingBookingDto
        {
            Id = r.Id,
            BookingType = "Resource",
            Title = r.BookableResource?.Name ?? "Booking",
            StartsAt = r.StartsAt,
            EndsAt = r.EndsAt,
            Location = r.BookableResource?.Name,
            Status = BookingStatus.Booked,
            Route = $"/fitness/resources?bookingId={r.Id}",
        }));

        return [.. upcoming.OrderBy(u => u.StartsAt).Take(20)];
    }

    // ── Credentials ──────────────────────────────────────────────────────────

    public async Task<MemberCredentialDto> IssueCredentialAsync(IssueCredentialDto request, Guid userId)
    {
        var identifier = request.Identifier.Trim();

        // One live credential per identifier, checked here as well as by the index so the caller
        // gets a sentence rather than a constraint violation.
        var clash = await db.Credentials.ForTenant(tenant)
            .Where(c => c.Identifier == identifier && c.Status == CredentialStatus.Active)
            .Include(c => c.Member)
            .FirstOrDefaultAsync();

        if (clash is not null && clash.MemberId != request.MemberId)
            throw new InvalidOperationException(
                $"That fob is already assigned to {(clash.Member is null ? "another member" : FitnessMapper.FullName(clash.Member))}. " +
                "Deactivate it there first.");

        if (request.ReplacesCredentialId is not null)
        {
            var old = await db.Credentials.ForTenant(tenant)
                .FirstOrDefaultAsync(c => c.Id == request.ReplacesCredentialId);

            if (old is not null)
            {
                old.Status = CredentialStatus.Replaced;
                old.DeactivatedOn = DateTime.UtcNow;
                old.DeactivationReason = "Replaced";
                old.StampUpdated(userId);
            }
        }

        var credential = new MemberCredential
        {
            MemberId = request.MemberId,
            Type = request.Type,
            Identifier = identifier,
            Status = CredentialStatus.Active,
            IssuedOn = DateTime.UtcNow,
            ExpiresOn = request.ExpiresOn,
            ReplacementFee = request.ReplacementFee,
            ReplacesCredentialId = request.ReplacesCredentialId,
            IssuedByUserId = userId,
        }.StampNew(tenant, userId);

        db.Credentials.Add(credential);

        if (request.ChargeReplacementFee && request.ReplacementFee > 0)
        {
            var member = await db.Members.ForTenant(tenant).FirstAsync(m => m.Id == request.MemberId);
            await billing.CreateAdHocInvoiceAsync(request.MemberId, member.HomeClubId, [
                new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.CardReplacement,
                    LineDescription = "Replacement entry fob",
                    Quantity = 1,
                    UnitPrice = request.ReplacementFee,
                    LineTotal = request.ReplacementFee,
                },
            ], userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(credential);
    }

    public async Task DeactivateCredentialAsync(Guid credentialId, string reason, Guid userId)
    {
        var credential = await db.Credentials.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == credentialId)
            ?? throw new InvalidOperationException("Credential not found.");

        credential.Status = reason.Contains("lost", StringComparison.OrdinalIgnoreCase)
            ? CredentialStatus.Lost
            : CredentialStatus.Deactivated;
        credential.DeactivatedOn = DateTime.UtcNow;
        credential.DeactivationReason = reason;
        credential.StampUpdated(userId);

        await db.SaveChangesAsync();
    }

    // ── Households ───────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a household, or edits the one the request names.
    ///
    /// Membership is reconciled rather than replaced: somebody taken out of the family keeps every
    /// agreement, invoice and visit they ever had, and simply stops being billed with the others.
    /// Taking a person out of a family is not the same as taking them out of the club, and code
    /// that confused the two would eventually delete somebody's teenager.
    /// </summary>
    public async Task<HouseholdDto> SaveHouseholdAsync(SaveHouseholdDto request, Guid userId)
    {
        Household household;

        if (request.Id is { } id)
        {
            household = await db.Households.ForTenant(tenant)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new InvalidOperationException("That household was not found.");

            household.Name = request.Name;
            household.PrimaryMemberId = request.PrimaryMemberId;
            household.AddressLine = request.AddressLine;
            household.City = request.City;
            household.PostCode = request.PostCode;
            household.AnyAdultMayCheckInChildren = request.AnyAdultMayCheckInChildren;
            household.StampUpdated(userId);
        }
        else
        {
            household = new Household
            {
                Name = request.Name,
                PrimaryMemberId = request.PrimaryMemberId,
                AddressLine = request.AddressLine,
                City = request.City,
                PostCode = request.PostCode,
                AnyAdultMayCheckInChildren = request.AnyAdultMayCheckInChildren,
            }.StampNew(tenant, userId);

            db.Households.Add(household);
        }

        var existing = request.Id is null
            ? []
            : await db.HouseholdMembers.ForTenant(tenant)
                .Where(m => m.HouseholdId == household.Id)
                .ToListAsync();

        var wanted = request.Members.Select(m => m.MemberId).ToHashSet();

        // Anybody dropped from the list leaves the household and goes back to being billed alone.
        foreach (var gone in existing.Where(m => !wanted.Contains(m.MemberId)))
        {
            gone.IsDeleted = true;
            gone.StampUpdated(userId);

            var left = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == gone.MemberId);
            if (left is not null)
            {
                left.HouseholdId = null;
                left.StampUpdated(userId);
            }
        }

        foreach (var dto in request.Members)
        {
            var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == dto.MemberId);
            if (member is null) continue;

            member.HouseholdId = household.Id;
            member.StampUpdated(userId);

            // A junior ages out on their eighteenth birthday, which the family-plan review needs
            // to be a diary entry rather than a surprise.
            DateTime? agesOut = dto.Role == HouseholdRole.Child && member.DateOfBirth is not null
                ? member.DateOfBirth.Value.AddYears(18)
                : null;

            var row = existing.FirstOrDefault(m => m.MemberId == dto.MemberId);

            if (row is null)
            {
                db.HouseholdMembers.Add(new HouseholdMember
                {
                    HouseholdId = household.Id,
                    MemberId = dto.MemberId,
                    Role = dto.Role,
                    MayCollectChildren = dto.MayCollectChildren,
                    AgesOutOn = agesOut,
                }.StampNew(tenant, userId));
            }
            else
            {
                row.Role = dto.Role;
                row.MayCollectChildren = dto.MayCollectChildren;
                row.AgesOutOn = agesOut;
                row.IsDeleted = false;
                row.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return (await GetHouseholdAsync(household.Id))!;
    }

    /// <summary>
    /// The households at a club, by family name, searchable.
    ///
    /// Scoped by the home club of the people in it rather than by the household itself, because a
    /// family is a group of people and it is the people who belong to a site.
    /// </summary>
    public async Task<PaginatedResponse<HouseholdDto>> ListHouseholdsAsync(
        Guid? clubId, string? search, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.Households.ForTenant(tenant)
            .Include(h => h.Members.Where(m => !m.IsDeleted)).ThenInclude(m => m.Member)
            .AsQueryable();

        if (clubId is { } club)
        {
            query = query.Where(h => h.Members.Any(m => !m.IsDeleted && m.Member!.HomeClubId == club));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(h => h.Name.ToLower().Contains(term)
                || h.Members.Any(m => !m.IsDeleted
                    && (m.Member!.FirstName + " " + m.Member.LastName).ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderBy(h => h.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var primaryIds = rows.Select(h => h.PrimaryMemberId).ToList();
        var primaryNames = await db.Members.ForTenant(tenant)
            .Where(m => primaryIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName })
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var households = rows.Select(h => new HouseholdDto
        {
            Id = h.Id,
            Name = h.Name,
            PrimaryMemberId = h.PrimaryMemberId,
            PrimaryMemberName = primaryNames.GetValueOrDefault(h.PrimaryMemberId),
            AddressLine = h.AddressLine,
            City = h.City,
            PostCode = h.PostCode,
            AnyAdultMayCheckInChildren = h.AnyAdultMayCheckInChildren,
            CombinedBalance = h.Members.Sum(m => m.Member?.AccountBalance ?? 0),
            Members = [.. h.Members.Select(m => FitnessMapper.ToDto(m, now))],
        }).ToList();

        return PaginatedResponse<HouseholdDto>.Ok(
                   households,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<HouseholdDto?> GetHouseholdAsync(Guid householdId)
    {
        var now = DateTime.UtcNow;

        var household = await db.Households.ForTenant(tenant)
            .Include(h => h.Members.Where(m => !m.IsDeleted)).ThenInclude(m => m.Member)
            .FirstOrDefaultAsync(h => h.Id == householdId);

        if (household is null) return null;

        var primaryName = await db.Members.ForTenant(tenant)
            .Where(m => m.Id == household.PrimaryMemberId)
            .Select(m => m.FirstName + " " + m.LastName)
            .FirstOrDefaultAsync();

        return new HouseholdDto
        {
            Id = household.Id,
            Name = household.Name,
            PrimaryMemberId = household.PrimaryMemberId,
            PrimaryMemberName = primaryName,
            AddressLine = household.AddressLine,
            City = household.City,
            PostCode = household.PostCode,
            AnyAdultMayCheckInChildren = household.AnyAdultMayCheckInChildren,
            CombinedBalance = household.Members.Sum(m => m.Member?.AccountBalance ?? 0),
            Members = [.. household.Members.Select(m => FitnessMapper.ToDto(m, now))],
        };
    }

    // ── Merge ────────────────────────────────────────────────────────────────

    public async Task<MergePreviewDto> PreviewMergeAsync(MergeMembersDto request)
    {
        var keep = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.KeepMemberId)
            ?? throw new InvalidOperationException("The member to keep was not found.");
        var merge = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MergeMemberId)
            ?? throw new InvalidOperationException("The member to merge was not found.");

        var preview = new MergePreviewDto
        {
            KeepMemberId = keep.Id,
            MergeMemberId = merge.Id,
            AgreementsMoved = await db.Agreements.ForTenant(tenant).CountAsync(a => a.MemberId == merge.Id),
            InvoicesMoved = await db.Invoices.ForTenant(tenant).CountAsync(i => i.MemberId == merge.Id),
            VisitsMoved = await db.CheckIns.ForTenant(tenant).CountAsync(c => c.MemberId == merge.Id),
            BookingsMoved = await db.ClassBookings.ForTenant(tenant).CountAsync(b => b.MemberId == merge.Id),
            CombinedBalance = keep.AccountBalance + merge.AccountBalance,
        };

        void Conflict(string field, string? a, string? b)
        {
            if (!string.Equals(a, b, StringComparison.OrdinalIgnoreCase) && (a is not null || b is not null))
                preview.Conflicts.Add(new MergeConflictDto { Field = field, KeepValue = a, MergeValue = b });
        }

        Conflict("First name", keep.FirstName, merge.FirstName);
        Conflict("Last name", keep.LastName, merge.LastName);
        Conflict("Phone", keep.Phone, merge.Phone);
        Conflict("Email", keep.Email, merge.Email);
        Conflict("Date of birth", keep.DateOfBirth?.ToString("d MMM yyyy"), merge.DateOfBirth?.ToString("d MMM yyyy"));
        Conflict("Address", keep.AddressLine, merge.AddressLine);

        if (keep.HomeClubId != merge.HomeClubId)
            preview.Warnings.Add("These two records belong to different clubs.");

        var bothActive = await db.Agreements.ForTenant(tenant)
            .CountAsync(a => (a.MemberId == keep.Id || a.MemberId == merge.Id) && a.Status == AgreementStatus.Active);

        if (bothActive > 1)
            preview.Warnings.Add(
                "Both records have a live membership. After merging, one member will hold two — " +
                "cancel whichever is not wanted.");

        if (merge.AccountBalance != 0)
            preview.Warnings.Add($"The merged record carries a balance of {merge.AccountBalance:0.00}, which moves across.");

        return preview;
    }

    public async Task<MemberDetailDto> MergeAsync(MergeMembersDto request, Guid userId)
    {
        if (request.PreviewOnly)
            throw new InvalidOperationException("This request was marked preview-only.");

        if (request.KeepMemberId == request.MergeMemberId)
            throw new InvalidOperationException("A member cannot be merged into themselves.");

        await using var tx = await db.Database.BeginTransactionAsync();

        var keep = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.KeepMemberId)
            ?? throw new InvalidOperationException("The member to keep was not found.");
        var merge = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MergeMemberId)
            ?? throw new InvalidOperationException("The member to merge was not found.");

        // Field-level choices, where the operator picked the losing record's value.
        foreach (var (field, value) in request.FieldChoices)
        {
            switch (field)
            {
                case "First name": keep.FirstName = value; break;
                case "Last name": keep.LastName = value; break;
                case "Phone": keep.Phone = value; break;
                case "Email": keep.Email = value; break;
                case "Address": keep.AddressLine = value; break;
                case "Date of birth" when DateTime.TryParse(value, out var dob): keep.DateOfBirth = dob; break;
            }
        }

        await ReassignAsync(db.Agreements, a => a.MemberId == merge.Id, a => a.MemberId = keep.Id, userId);
        await ReassignAsync(db.Invoices, i => i.MemberId == merge.Id, i => i.MemberId = keep.Id, userId);
        await ReassignAsync(db.Payments, p => p.MemberId == merge.Id, p => p.MemberId = keep.Id, userId);
        await ReassignAsync(db.CheckIns, c => c.MemberId == merge.Id, c => c.MemberId = keep.Id, userId);
        await ReassignAsync(db.ClassBookings, b => b.MemberId == merge.Id, b => b.MemberId = keep.Id, userId);
        await ReassignAsync(db.Appointments, a => a.MemberId == merge.Id, a => a.MemberId = keep.Id, userId);
        await ReassignAsync(db.MemberNotes, n => n.MemberId == merge.Id, n => n.MemberId = keep.Id, userId);
        await ReassignAsync(db.Ledger, l => l.MemberId == merge.Id, l => l.MemberId = keep.Id, userId);
        await ReassignAsync(db.SessionCredits, c => c.MemberId == merge.Id, c => c.MemberId = keep.Id, userId);
        await ReassignAsync(db.WorkoutResults, r => r.MemberId == merge.Id, r => r.MemberId = keep.Id, userId);
        await ReassignAsync(db.Assessments, a => a.MemberId == merge.Id, a => a.MemberId = keep.Id, userId);
        await ReassignAsync(db.Credentials, c => c.MemberId == merge.Id, c => c.MemberId = keep.Id, userId);

        keep.AccountBalance += merge.AccountBalance;
        keep.CreditBalance += merge.CreditBalance;
        keep.TotalVisits += merge.TotalVisits;
        keep.LoyaltyPoints += merge.LoyaltyPoints;
        if (merge.LastVisitOn > keep.LastVisitOn) keep.LastVisitOn = merge.LastVisitOn;
        if (merge.FirstJoinedOn < keep.FirstJoinedOn) keep.FirstJoinedOn = merge.FirstJoinedOn;
        keep.StampUpdated(userId);

        merge.Status = MemberStatus.Cancelled;
        merge.LeftOn = DateTime.UtcNow.Date;
        merge.StampDeleted(userId);

        db.MemberNotes.Add(new MemberNote
        {
            MemberId = keep.Id,
            Kind = InteractionKind.SystemEvent,
            Body = $"Merged duplicate record {merge.MemberNumber} into this one.",
            OccurredAt = DateTime.UtcNow,
            IsPinned = false,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        await RefreshAlertsAsync(keep.Id);

        await compliance.LogAsync("Merged", nameof(Member), keep.Id, keep.Id,
            $"Merged {merge.MemberNumber} into {keep.MemberNumber}", false, userId);

        await tx.CommitAsync();
        return (await GetAsync(keep.Id))!;
    }

    // ── Data protection ──────────────────────────────────────────────────────

    public async Task<MemberExportDto> ExportAsync(Guid memberId, Guid userId)
    {
        var detail = await GetAsync(memberId)
            ?? throw new InvalidOperationException("Member not found.");

        var now = DateTime.UtcNow;

        var export = new MemberExportDto
        {
            Member = detail,
            Agreements = detail.Agreements,
            Invoices = [.. (await db.Invoices.ForTenant(tenant)
                .Where(i => i.MemberId == memberId)
                .Include(i => i.Lines)
                .OrderByDescending(i => i.IssuedOn)
                .ToListAsync())
                .Select(i => FitnessMapper.ToSummary(i, now))],
            Ledger = await GetLedgerAsync(memberId, int.MaxValue),
            Visits = await GetVisitHistoryAsync(memberId, int.MaxValue),
            Notes = [.. (await db.MemberNotes.ForTenant(tenant)
                .Where(n => n.MemberId == memberId && !n.IsPrivate)
                .OrderByDescending(n => n.OccurredAt)
                .ToListAsync())
                .Select(FitnessMapper.ToDto)],
            Consents = [.. (await db.Consents.ForTenant(tenant)
                .Where(c => c.MemberId == memberId)
                .ToListAsync())
                .Select(FitnessMapper.ToDto)],
            Documents = [.. (await db.MemberDocuments.ForTenant(tenant)
                .Where(d => d.MemberId == memberId)
                .ToListAsync())
                .Select(FitnessMapper.ToDto)],
            Assessments = [.. (await db.Assessments.ForTenant(tenant)
                .Where(a => a.MemberId == memberId)
                .Include(a => a.Values)
                .OrderByDescending(a => a.PerformedOn)
                .ToListAsync())
                .Select(FitnessMapper.ToDto)],
            StatusHistory = [.. (await db.MemberStatusHistory.ForTenant(tenant)
                .Where(h => h.MemberId == memberId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync())
                .Select(FitnessMapper.ToDto)],
        };

        await compliance.LogAsync("Exported", nameof(Member), memberId, memberId,
            "Subject access request export", true, userId);

        return export;
    }

    /// <summary>
    /// Erasure.
    ///
    /// Identity is replaced with a tombstone. What survives is what the law requires a club to
    /// keep — financial transactions and incident reports — with the person's name removed from
    /// them. That distinction is the whole design: "delete everything" and "keep the accounts"
    /// are both wrong, and only the second one is legal.
    /// </summary>
    public async Task AnonymiseAsync(AnonymiseMemberDto request, Guid userId)
    {
        if (!request.ConfirmIrreversible)
            throw new InvalidOperationException("Erasure has to be confirmed — it cannot be undone.");

        var member = await db.Members.ForTenant(tenant)
            .Include(m => m.EmergencyContacts)
            .Include(m => m.Credentials)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        var openBalance = member.AccountBalance;
        if (openBalance > 0)
            throw new InvalidOperationException(
                $"This member owes {openBalance:0.00}. Settle or write off the balance before erasing them.");

        await using var tx = await db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;

        // Tombstone the identity, keep the number so financial records still reconcile.
        member.FirstName = "Erased";
        member.LastName = "Member";
        member.PreferredName = null;
        member.DateOfBirth = null;
        member.NationalId = null;
        member.Occupation = null;
        member.PhotoUrl = null;
        member.Phone = null;
        member.AlternatePhone = null;
        member.Email = null;
        member.AddressLine = null;
        member.City = null;
        member.PostCode = null;
        member.MedicalSummary = null;
        member.IsAnonymised = true;
        member.AnonymisedAt = now;
        member.Status = MemberStatus.Cancelled;
        member.StampUpdated(userId);

        foreach (var contact in member.EmergencyContacts) contact.StampDeleted(userId);

        foreach (var credential in member.Credentials.Where(c => c.Status == CredentialStatus.Active))
        {
            credential.Status = CredentialStatus.Deactivated;
            credential.DeactivatedOn = now;
            credential.DeactivationReason = "Member erased";
        }

        // Special-category data goes entirely — none of it is a record the club must keep.
        await DeleteAllAsync(db.MedicalFlags, f => f.MemberId == member.Id, userId);
        await DeleteAllAsync(db.HealthScreenings, h => h.MemberId == member.Id, userId);
        await DeleteAllAsync(db.ProgressPhotos, p => p.MemberId == member.Id, userId);
        await DeleteAllAsync(db.Assessments, a => a.MemberId == member.Id, userId);
        await DeleteAllAsync(db.MemberNotes, n => n.MemberId == member.Id, userId);
        await DeleteAllAsync(db.MemberDocuments, d => d.MemberId == member.Id, userId);
        await DeleteAllAsync(db.MemberAlerts, a => a.MemberId == member.Id, userId);
        await DeleteAllAsync(db.PaymentMethods, p => p.MemberId == member.Id, userId);

        await db.SaveChangesAsync();

        await compliance.LogAsync("Erased", nameof(Member), member.Id, member.Id,
            $"Right to erasure exercised. Reason: {request.Reason}. Financial and incident records retained.",
            true, userId);

        await tx.CommitAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<FitnessSettings> GetSettingsAsync()
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        if (settings is not null) return settings;

        settings = new FitnessSettings().StampNew(tenant);
        db.Settings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    private async Task<bool> IsWaiverCurrentAsync(Guid memberId, DateTime now)
    {
        return await db.WaiverSignatures.ForTenant(tenant)
            .AnyAsync(s => s.MemberId == memberId
                        && s.Status == SignatureStatus.Signed
                        && (s.ExpiresOn == null || s.ExpiresOn > now));
    }

    private static void ApplyMember(Member m, SaveMemberDto r)
    {
        m.FirstName = r.FirstName;
        m.LastName = r.LastName;
        m.PreferredName = r.PreferredName;
        m.DateOfBirth = r.DateOfBirth;
        m.Gender = r.Gender;
        m.NationalId = r.NationalId;
        m.Occupation = r.Occupation;
        m.PhotoUrl = r.PhotoUrl;
        m.Phone = r.Phone;
        m.AlternatePhone = r.AlternatePhone;
        m.Email = r.Email;
        m.AddressLine = r.AddressLine;
        m.City = r.City;
        m.PostCode = r.PostCode;
        m.CountryCode = r.CountryCode;
        m.PreferredLanguage = r.PreferredLanguage;
        m.PreferredChannel = r.PreferredChannel;
        m.HomeClubId = r.HomeClubId;
        m.HouseholdId = r.HouseholdId;
        m.CorporateAccountId = r.CorporateAccountId;
        m.AssignedCoachId = r.AssignedCoachId;
        m.LeadSourceId = r.LeadSourceId;
        m.ReferredByMemberId = r.ReferredByMemberId;
        m.MedicalSummary = r.MedicalSummary;
        m.PhotoConsent = r.PhotoConsent;
        m.LeaderboardOptIn = r.LeaderboardOptIn;
    }

    private void AddEmergencyContacts(Member member, List<EmergencyContactDto> contacts, Guid userId)
    {
        foreach (var c in contacts.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
        {
            db.EmergencyContacts.Add(new EmergencyContact
            {
                MemberId = member.Id,
                Name = c.Name,
                Relationship = c.Relationship,
                Phone = c.Phone,
                AlternatePhone = c.AlternatePhone,
                Email = c.Email,
                IsPrimary = c.IsPrimary,
            }.StampNew(tenant, userId));
        }
    }

    private void AddTags(Guid memberId, List<string> tags, Guid userId)
    {
        foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
        {
            db.MemberTags.Add(new MemberTag
            {
                MemberId = memberId,
                Tag = tag.Trim(),
            }.StampNew(tenant, userId));
        }
    }

    private static string DescribeCredit(SessionCredit c) => c.Kind switch
    {
        EntitlementKind.PersonalTraining => "PT sessions",
        EntitlementKind.ClassBooking => "Class credits",
        EntitlementKind.GuestPass => "Guest passes",
        EntitlementKind.Creche => "Creche hours",
        EntitlementKind.ResourceBooking => "Court bookings",
        _ => c.Kind.ToString(),
    };

    private async Task ReassignAsync<T>(
        DbSet<T> set,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        Action<T> apply,
        Guid userId) where T : Nexcore.SharedKernel.BaseEntity
    {
        var rows = await set.ForTenant(tenant).Where(predicate).ToListAsync();
        foreach (var row in rows)
        {
            apply(row);
            row.StampUpdated(userId);
        }
    }

    private async Task DeleteAllAsync<T>(
        DbSet<T> set,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        Guid userId) where T : Nexcore.SharedKernel.BaseEntity
    {
        var rows = await set.ForTenant(tenant).Where(predicate).ToListAsync();
        foreach (var row in rows) row.StampDeleted(userId);
    }
}
