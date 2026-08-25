using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Waivers, health screening, clearances, incidents and the checks a club has to be able to prove
/// it did.
///
/// The chain that matters runs: a health question answered "yes" raises a clearance requirement, a
/// clearance requirement blocks participation, and the block is enforced at the door and the
/// booking engine rather than noted in a file. A screening form that only gets filed is a form
/// nobody should have made anyone fill in.
///
/// Incidents are treated as legal records — timestamped, attributed, never quietly edited, and
/// exportable — because that is what an insurer asks for and what a club regrets not having.
/// </summary>
public class ComplianceService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IHttpContextAccessor httpContext) : IComplianceService
{
    // ── Waivers ──────────────────────────────────────────────────────────────

    public async Task<List<WaiverTemplateDto>> GetWaiverTemplatesAsync(Guid? clubId, bool publishedOnly)
    {
        var now = DateTime.UtcNow;

        var templates = await db.WaiverTemplates.ForTenant(tenant)
            .WhereIf(clubId is not null, t => t.ClubId == clubId || t.ClubId == null)
            .WhereIf(publishedOnly, t => t.IsPublished
                                      && t.EffectiveFrom <= now
                                      && (t.EffectiveTo == null || t.EffectiveTo >= now))
            .OrderBy(t => t.Name).ThenByDescending(t => t.Version)
            .ToListAsync();

        var ids = templates.Select(t => t.Id).ToList();

        var signed = await db.WaiverSignatures.ForTenant(tenant)
            .Where(s => ids.Contains(s.WaiverTemplateId) && s.Status == SignatureStatus.Signed)
            .GroupBy(s => s.WaiverTemplateId)
            .Select(g => new { TemplateId = g.Key, Count = g.Count() })
            .ToListAsync();

        var activeMembers = await db.Members.ForTenant(tenant)
            .CountAsync(m => m.Status == MemberStatus.Active && (clubId == null || m.HomeClubId == clubId));

        return [.. templates.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t);
            dto.SignedCount = signed.FirstOrDefault(s => s.TemplateId == t.Id)?.Count ?? 0;
            dto.OutstandingCount = t.IsPublished ? Math.Max(0, activeMembers - dto.SignedCount) : 0;
            return dto;
        })];
    }

    /// <summary>
    /// Saving a waiver.
    ///
    /// Editing a published template creates a **new version** rather than changing the words
    /// people already signed. A waiver is only worth anything if the club can show the exact
    /// wording agreed to on the day, and editing in place destroys every signature before it.
    /// </summary>
    public async Task<WaiverTemplateDto> SaveWaiverTemplateAsync(Guid? id, WaiverTemplateDto request, Guid userId)
    {
        WaiverTemplate template;

        if (id is null)
        {
            template = new WaiverTemplate { Version = 1 }.StampNew(tenant, userId);
            db.WaiverTemplates.Add(template);
        }
        else
        {
            var existing = await db.WaiverTemplates.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Waiver not found.");

            if (existing.IsPublished && existing.BodyHtml != request.BodyHtml)
            {
                // A new version, and the old one stops being the current text.
                existing.EffectiveTo = DateTime.UtcNow;
                existing.StampUpdated(userId);

                template = new WaiverTemplate { Version = existing.Version + 1 }.StampNew(tenant, userId);
                db.WaiverTemplates.Add(template);

                if (existing.RequiresResignOnNewVersion)
                    await SupersedeSignaturesAsync(existing.Id, userId);
            }
            else
            {
                template = existing;
                template.StampUpdated(userId);
            }
        }

        template.Name = request.Name;
        template.ClubId = request.ClubId;
        template.CountryCode = request.CountryCode;
        template.LanguageCode = request.LanguageCode;
        template.ClassTypeId = request.ClassTypeId;
        template.ActivityScope = request.ActivityScope;
        template.BodyHtml = request.BodyHtml;
        template.ConsentClausesJson = request.ConsentClausesJson;
        template.RequiresGuardianSignature = request.RequiresGuardianSignature;
        template.GuardianRequiredBelowAge = request.GuardianRequiredBelowAge;
        template.ValidForDays = request.ValidForDays;
        template.BlocksAccess = request.BlocksAccess;
        template.EffectiveFrom = request.EffectiveFrom == default ? DateTime.UtcNow : request.EffectiveFrom;
        template.RequiresResignOnNewVersion = request.RequiresResignOnNewVersion;
        template.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(template);
    }

    public async Task<WaiverTemplateDto> PublishWaiverAsync(Guid id, Guid userId)
    {
        var template = await db.WaiverTemplates.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new InvalidOperationException("Waiver not found.");

        if (string.IsNullOrWhiteSpace(template.BodyHtml))
            throw new InvalidOperationException("A waiver with no text cannot be published.");

        // Only one published version of a named waiver at a time.
        var others = await db.WaiverTemplates.ForTenant(tenant)
            .Where(t => t.Name == template.Name && t.Id != id && t.IsPublished
                     && t.ClubId == template.ClubId)
            .ToListAsync();

        foreach (var other in others)
        {
            other.IsPublished = false;
            other.EffectiveTo = DateTime.UtcNow;
            other.StampUpdated(userId);
        }

        template.IsPublished = true;
        template.EffectiveFrom = DateTime.UtcNow;
        template.StampUpdated(userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(template);
    }

    public async Task<WaiverSignatureDto> SignWaiverAsync(SignWaiverDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var template = await db.WaiverTemplates.ForTenant(tenant)
            .FirstOrDefaultAsync(t => t.Id == request.WaiverTemplateId)
            ?? throw new InvalidOperationException("Waiver not found.");

        // A minor's waiver is signed by their guardian, and refusing here is the whole point of
        // recording the age in the first place.
        var dateOfBirth = request.SignerDateOfBirth;
        if (request.MemberId is not null)
        {
            dateOfBirth ??= await db.Members.ForTenant(tenant)
                .Where(m => m.Id == request.MemberId).Select(m => m.DateOfBirth).FirstOrDefaultAsync();
        }

        var age = FitnessMapper.AgeOn(dateOfBirth, now);
        var needsGuardian = template.RequiresGuardianSignature
                            || (template.GuardianRequiredBelowAge is not null && age is not null
                                && age < template.GuardianRequiredBelowAge);

        if (needsGuardian && string.IsNullOrWhiteSpace(request.GuardianName))
            throw new InvalidOperationException(
                $"This waiver needs a parent or guardian signature for anyone under {template.GuardianRequiredBelowAge ?? 18}.");

        // Superseded rather than duplicated, so "their current waiver" is always one row.
        if (request.MemberId is not null)
        {
            var previous = await db.WaiverSignatures.ForTenant(tenant)
                .Where(s => s.MemberId == request.MemberId
                         && s.WaiverTemplateId == template.Id
                         && s.Status == SignatureStatus.Signed)
                .ToListAsync();

            foreach (var old in previous)
            {
                old.Status = SignatureStatus.Superseded;
                old.StampUpdated(userId);
            }
        }

        var signature = new WaiverSignature
        {
            WaiverTemplateId = template.Id,
            TemplateVersion = template.Version,
            MemberId = request.MemberId,
            SignerName = request.SignerName,
            SignerEmail = request.SignerEmail,
            SignerPhone = request.SignerPhone,
            SignerDateOfBirth = dateOfBirth,
            GuestVisitId = request.GuestVisitId,
            DayPassId = request.DayPassId,
            ClubId = request.ClubId,
            Status = SignatureStatus.Signed,
            SignedAt = now,
            ExpiresOn = template.ValidForDays > 0 ? now.AddDays(template.ValidForDays) : null,
            GuardianName = request.GuardianName,
            GuardianRelationship = request.GuardianRelationship,
            GuardianSignatureUrl = request.GuardianSignatureUrl,
            SignatureImageUrl = request.SignatureImageUrl,
            ConsentAnswersJson = request.ConsentAnswersJson,
            CapturedVia = request.CapturedVia ?? "Front desk",
            IpAddress = ClientIp(),
            UserAgent = UserAgent(),
        }.StampNew(tenant, userId);

        db.WaiverSignatures.Add(signature);

        if (request.MemberId is not null)
        {
            var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId);
            if (member is not null)
            {
                member.WaiverSigned = true;
                member.WaiverSignedOn = now;
                member.WaiverTemplateVersionId = template.Id;
                member.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();

        var saved = await db.WaiverSignatures.ForTenant(tenant)
            .Include(s => s.WaiverTemplate)
            .Include(s => s.Member)
            .FirstAsync(s => s.Id == signature.Id);

        return FitnessMapper.ToDto(saved, now);
    }

    public async Task<List<WaiverSignatureDto>> GetSignaturesAsync(
        Guid? memberId, Guid? clubId, SignatureStatus? status)
    {
        var now = DateTime.UtcNow;

        var signatures = await db.WaiverSignatures.ForTenant(tenant)
            .WhereIf(memberId is not null, s => s.MemberId == memberId)
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .WhereIf(status is not null, s => s.Status == status)
            .Include(s => s.WaiverTemplate)
            .Include(s => s.Member)
            .OrderByDescending(s => s.SignedAt)
            .Take(500)
            .ToListAsync();

        return [.. signatures.Select(s => FitnessMapper.ToDto(s, now))];
    }

    /// <summary>Members with no current waiver — who cannot be let in tomorrow.</summary>
    public async Task<PaginatedResponse<MemberSummaryDto>> GetOutstandingWaiversAsync(
        Guid? clubId, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var current = db.WaiverSignatures.ForTenant(tenant)
            .Where(s => s.Status == SignatureStatus.Signed && (s.ExpiresOn == null || s.ExpiresOn > now)
                     && s.MemberId != null)
            .Select(s => s.MemberId!.Value);

        var query = db.Members.ForTenant(tenant)
            .Include(m => m.HomeClub)
            .Where(m => m.Status == MemberStatus.Active && !current.Contains(m.Id))
            .WhereIf(clubId is not null, m => m.HomeClubId == clubId);

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<MemberSummaryDto>.Ok(
                   [.. page.Select(m => FitnessMapper.ToSummary(m, now))],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    // ── Health screening ─────────────────────────────────────────────────────

    /// <summary>
    /// The blank screening form.
    ///
    /// PAR-Q+ shaped, with the gating questions marked. It lives server-side rather than in the
    /// app so the questions cannot drift between the join wizard, the kiosk and the member app —
    /// three copies of a health questionnaire is three different clinical positions.
    /// </summary>
    public Task<HealthScreeningFormDto> GetScreeningFormAsync(Guid? clubId)
    {
        var form = new HealthScreeningFormDto
        {
            TemplateName = "PAR-Q+",
            TemplateVersion = 1,
            Introduction =
                "A few questions before you start training. They take a minute, and they help us keep you safe. " +
                "If you answer yes to any of them, we will simply ask for a note from your doctor before you begin.",
            GatingMessage =
                "Thanks — because of one or more of your answers, we need medical clearance before you start. " +
                "Reception will explain exactly what is needed.",
            Questions =
            [
                Q(1, "Has your doctor ever said that you have a heart condition, or that you should only do physical activity recommended by a doctor?", true),
                Q(2, "Do you feel pain in your chest when you do physical activity?", true),
                Q(3, "In the past month, have you had chest pain when you were not doing physical activity?", true),
                Q(4, "Do you lose your balance because of dizziness, or have you ever lost consciousness?", true),
                Q(5, "Do you have a bone or joint problem that could be made worse by a change in your physical activity?", true),
                Q(6, "Is your doctor currently prescribing drugs for a blood pressure or heart condition?", true),
                Q(7, "Do you know of any other reason why you should not do physical activity?", true),
                Q(8, "Are you pregnant, or have you given birth in the last six months?", false),
                Q(9, "Do you have any injuries or conditions we should know about?", false),
                Q(10, "Are you taking any medication we should be aware of?", false),
            ],
        };

        return Task.FromResult(form);

        static HealthScreeningAnswerDto Q(int number, string text, bool gating) => new()
        {
            QuestionNumber = number,
            QuestionText = text,
            AnswerKind = ScreeningAnswerKind.YesNo,
            IsGatingQuestion = gating,
        };
    }

    /// <summary>
    /// Recording a completed screening.
    ///
    /// A "yes" on any gating question raises a real clearance requirement and flips the member's
    /// status, which is what the door and the booking engine read. The risk summary is written for
    /// the person who will be standing next to them, not for a file.
    /// </summary>
    public async Task<HealthScreeningDto> SubmitScreeningAsync(HealthScreeningDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        var gatingYes = request.Answers.Any(a => a.IsGatingQuestion && a.BooleanAnswer == true);

        var screening = new HealthScreening
        {
            MemberId = request.MemberId,
            ClubId = request.ClubId,
            TemplateName = request.TemplateName,
            TemplateVersion = request.TemplateVersion,
            CompletedAt = now,
            ExpiresOn = now.AddYears(1),
            RequiresClearance = gatingYes,
            ClearanceStatus = gatingYes ? ClearanceStatus.Required : ClearanceStatus.NotRequired,
            CapturedVia = request.CapturedVia ?? "Join wizard",
        }.StampNew(tenant, userId);

        db.HealthScreenings.Add(screening);

        foreach (var answerDto in request.Answers)
        {
            db.ScreeningAnswers.Add(new HealthScreeningAnswer
            {
                HealthScreeningId = screening.Id,
                QuestionNumber = answerDto.QuestionNumber,
                QuestionText = answerDto.QuestionText,
                AnswerKind = answerDto.AnswerKind,
                BooleanAnswer = answerDto.BooleanAnswer,
                TextAnswer = answerDto.TextAnswer,
                NumericAnswer = answerDto.NumericAnswer,
                DateAnswer = answerDto.DateAnswer,
                IsGatingQuestion = answerDto.IsGatingQuestion,
                FollowUpAnswer = answerDto.FollowUpAnswer,
            }.StampNew(tenant, userId));
        }

        // A short line the desk and the instructor actually read.
        var flagged = request.Answers
            .Where(a => a.BooleanAnswer == true || !string.IsNullOrWhiteSpace(a.TextAnswer))
            .Select(a => a.FollowUpAnswer ?? a.TextAnswer ?? ShortForm(a.QuestionNumber))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        screening.RiskSummary = flagged.Count == 0
            ? "Nothing flagged."
            : string.Join("; ", flagged.Take(4));

        if (gatingYes)
        {
            db.Clearances.Add(new MedicalClearance
            {
                MemberId = request.MemberId,
                ClubId = request.ClubId,
                HealthScreeningId = screening.Id,
                Status = ClearanceStatus.Required,
                RequestedOn = now,
                BlocksParticipation = true,
            }.StampNew(tenant, userId));

            member.MedicalClearance = ClearanceStatus.Required;
        }
        else
        {
            member.MedicalClearance = ClearanceStatus.NotRequired;
        }

        // Free-text answers become medical flags so an instructor sees them on the roster.
        foreach (var answer in request.Answers.Where(a =>
                     !a.IsGatingQuestion && !string.IsNullOrWhiteSpace(a.TextAnswer)))
        {
            db.MedicalFlags.Add(new MedicalFlag
            {
                MemberId = request.MemberId,
                Category = answer.QuestionNumber == 10 ? "Medication" : "Condition",
                Detail = answer.TextAnswer!,
                Severity = AlertSeverity.Info,
                VisibleToInstructors = true,
            }.StampNew(tenant, userId));
        }

        member.MedicalSummary = screening.RiskSummary;
        member.StampUpdated(userId);

        await db.SaveChangesAsync();
        await LogAsync("Created", nameof(HealthScreening), screening.Id, request.MemberId,
            "Health screening completed", true, userId);

        return (await GetScreeningAsync(request.MemberId, userId))!;
    }

    public async Task<HealthScreeningDto?> GetScreeningAsync(Guid memberId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var screening = await db.HealthScreenings.ForTenant(tenant)
            .Include(s => s.Member)
            .Include(s => s.Answers.Where(a => !a.IsDeleted))
            .Where(s => s.MemberId == memberId)
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefaultAsync();

        if (screening is null) return null;

        await LogAsync("Viewed", nameof(HealthScreening), screening.Id, memberId,
            "Health screening answers", true, userId);

        var dto = FitnessMapper.ToDto(screening, now);

        if (screening.ReviewedByStaffId is not null)
        {
            dto.ReviewedByName = await db.Staff.ForTenant(tenant)
                .Where(s => s.Id == screening.ReviewedByStaffId)
                .Select(s => s.FirstName + " " + s.LastName)
                .FirstOrDefaultAsync();
        }

        return dto;
    }

    // ── Clearances ───────────────────────────────────────────────────────────

    public async Task<MedicalClearanceDto> SubmitClearanceAsync(SubmitClearanceDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var clearance = request.ClearanceId is not null
            ? await db.Clearances.ForTenant(tenant).Include(c => c.Member)
                .FirstOrDefaultAsync(c => c.Id == request.ClearanceId)
            : await db.Clearances.ForTenant(tenant).Include(c => c.Member)
                .Where(c => c.MemberId == request.MemberId && c.Status != ClearanceStatus.Approved)
                .OrderByDescending(c => c.RequestedOn)
                .FirstOrDefaultAsync();

        if (clearance is null)
        {
            var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
                ?? throw new InvalidOperationException("Member not found.");

            clearance = new MedicalClearance
            {
                MemberId = request.MemberId,
                ClubId = member.HomeClubId,
                RequestedOn = now,
            }.StampNew(tenant, userId);

            db.Clearances.Add(clearance);
        }
        else
        {
            clearance.StampUpdated(userId);
        }

        clearance.Status = ClearanceStatus.Submitted;
        clearance.SubmittedOn = now;
        clearance.PractitionerName = request.PractitionerName;
        clearance.PractitionerRegistration = request.PractitionerRegistration;
        clearance.PracticeName = request.PracticeName;
        clearance.Restrictions = request.Restrictions;
        clearance.DocumentId = request.DocumentId;
        clearance.ExpiresOn = request.ExpiresOn;

        var target = clearance.Member
            ?? await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == clearance.MemberId);

        if (target is not null)
        {
            target.MedicalClearance = ClearanceStatus.Submitted;
            target.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        await LogAsync("Updated", nameof(MedicalClearance), clearance.Id, clearance.MemberId,
            "Medical clearance submitted for review", true, userId);

        return FitnessMapper.ToDto(clearance, now);
    }

    public async Task<MedicalClearanceDto> ReviewClearanceAsync(ReviewClearanceDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var clearance = await db.Clearances.ForTenant(tenant)
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.Id == request.ClearanceId)
            ?? throw new InvalidOperationException("Clearance not found.");

        clearance.Status = request.Approve ? ClearanceStatus.Approved : ClearanceStatus.Rejected;
        clearance.ApprovedOn = request.Approve ? now : null;
        clearance.ApprovedByStaffId = userId == Guid.Empty ? null : userId;
        clearance.RejectionReason = request.Approve ? null : request.Note;
        clearance.Restrictions = request.Restrictions ?? clearance.Restrictions;
        clearance.ExpiresOn = request.ExpiresOn ?? clearance.ExpiresOn;
        clearance.StampUpdated(userId);

        var member = clearance.Member;
        if (member is not null)
        {
            member.MedicalClearance = clearance.Status;
            member.StampUpdated(userId);

            // Restrictions go onto the roster, where a coach will actually see them.
            if (request.Approve && !string.IsNullOrWhiteSpace(clearance.Restrictions))
            {
                db.MedicalFlags.Add(new MedicalFlag
                {
                    MemberId = member.Id,
                    Category = "Medical restriction",
                    Detail = clearance.Restrictions,
                    Severity = AlertSeverity.Warning,
                    VisibleToInstructors = true,
                    ReviewOn = clearance.ExpiresOn,
                }.StampNew(tenant, userId));
            }
        }

        await db.SaveChangesAsync();
        await LogAsync(request.Approve ? "Approved" : "Rejected", nameof(MedicalClearance),
            clearance.Id, clearance.MemberId, request.Note, true, userId);

        return FitnessMapper.ToDto(clearance, now);
    }

    public async Task<List<MedicalClearanceDto>> GetClearancesAsync(Guid? clubId, ClearanceStatus? status)
    {
        var now = DateTime.UtcNow;

        var clearances = await db.Clearances.ForTenant(tenant)
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .WhereIf(status is not null, c => c.Status == status)
            .Include(c => c.Member)
            .OrderBy(c => c.Status).ThenByDescending(c => c.RequestedOn)
            .Take(500)
            .ToListAsync();

        return [.. clearances.Select(c => FitnessMapper.ToDto(c, now))];
    }

    // ── Incidents ────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<IncidentDto>> GetIncidentsAsync(
        Guid? clubId, IncidentKind? kind, IncidentStatus? status, IncidentSeverity? severity,
        DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.Incidents.ForTenant(tenant)
            .Include(i => i.Actions.Where(a => !a.IsDeleted))
            .WhereIf(clubId is not null, i => i.ClubId == clubId)
            .WhereIf(kind is not null, i => i.Kind == kind)
            .WhereIf(status is not null, i => i.Status == status)
            .WhereIf(severity is not null, i => i.Severity == severity)
            .WhereIf(from is not null, i => i.OccurredAt >= from)
            .WhereIf(to is not null, i => i.OccurredAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(i => i.OccurredAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var items = await DecorateIncidentsAsync(page, now);

        return PaginatedResponse<IncidentDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<IncidentDto?> GetIncidentAsync(Guid incidentId)
    {
        var now = DateTime.UtcNow;

        var incident = await db.Incidents.ForTenant(tenant)
            .Include(i => i.Actions.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(i => i.Id == incidentId);

        if (incident is null) return null;

        var decorated = await DecorateIncidentsAsync([incident], now);
        return decorated[0];
    }

    /// <summary>
    /// Recording an incident.
    ///
    /// Serious incidents automatically raise the follow-up actions a club would otherwise
    /// discover it needed at the insurance claim: a witness statement, an equipment check, a
    /// review date. The reporting delay is stored as its own number, because a long one is itself
    /// a finding.
    /// </summary>
    public async Task<IncidentDto> SaveIncidentAsync(Guid? id, SaveIncidentDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        Incident incident;
        var isNew = id is null;

        if (isNew)
        {
            incident = new Incident
            {
                IncidentNumber = await numbering.NextIncidentNumberAsync(now),
                ClubId = request.ClubId,
                ReportedAt = now,
                ReportedByStaffId = userId == Guid.Empty ? null : userId,
            }.StampNew(tenant, userId);

            db.Incidents.Add(incident);
        }
        else
        {
            incident = await db.Incidents.ForTenant(tenant)
                .Include(i => i.Actions.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new InvalidOperationException("Incident not found.");

            if (incident.Status == IncidentStatus.Closed)
                throw new InvalidOperationException(
                    "A closed incident cannot be edited. Add an action or reopen it if something new has come to light.");

            incident.StampUpdated(userId);
        }

        incident.AreaId = request.AreaId;
        incident.EquipmentAssetId = request.EquipmentAssetId;
        incident.Kind = request.Kind;
        incident.Severity = request.Severity;
        incident.OccurredAt = request.OccurredAt;
        incident.MemberId = request.MemberId;
        incident.InvolvedPersonName = request.InvolvedPersonName;
        incident.InvolvedPersonPhone = request.InvolvedPersonPhone;
        incident.Summary = request.Summary;
        incident.Detail = request.Detail;
        incident.WitnessNames = request.WitnessNames;
        incident.WitnessStatements = request.WitnessStatements;
        incident.FirstAidGiven = request.FirstAidGiven;
        incident.FirstAiderName = request.FirstAiderName;
        incident.AedUsed = request.AedUsed;
        incident.AmbulanceCalled = request.AmbulanceCalled;
        incident.HospitalAttended = request.HospitalAttended;
        incident.ImmediateAction = request.ImmediateAction;
        incident.PhotoUrls = request.PhotoUrls.Count == 0 ? null : string.Join(',', request.PhotoUrls);
        incident.OwnerStaffId = request.OwnerStaffId ?? incident.OwnerStaffId;
        incident.IsReportable = request.IsReportable
                                || request.Severity == IncidentSeverity.Reportable
                                || request.AmbulanceCalled
                                || request.HospitalAttended;
        incident.InsurerNotified = request.InsurerNotified;
        incident.EstimatedCost = request.EstimatedCost;

        incident.ReviewDueOn = request.ReviewDueOn
            ?? incident.ReviewDueOn
            ?? (request.Severity >= IncidentSeverity.Serious ? now.Date.AddDays(3) : now.Date.AddDays(14));

        if (isNew && request.Severity >= IncidentSeverity.Serious)
        {
            AddAction(incident, "Take a written statement from everyone involved and any witnesses", now.AddDays(1), userId);
            AddAction(incident, "Check the equipment and the area, and record what was found", now.AddDays(1), userId);

            if (incident.IsReportable)
                AddAction(incident, "Confirm whether this is reportable to the regulator, and report it if so", now.AddDays(2), userId);

            if (request.EquipmentAssetId is not null)
                AddAction(incident, "Inspect the equipment before it goes back into service", now.AddDays(1), userId);
        }

        // An equipment incident takes the machine off the floor immediately.
        if (request.TakeEquipmentOutOfService && request.EquipmentAssetId is not null)
        {
            var asset = await db.Equipment.ForTenant(tenant)
                .FirstOrDefaultAsync(e => e.Id == request.EquipmentAssetId);

            if (asset is not null)
            {
                asset.Status = AssetStatus.OutOfOrder;
                asset.OutOfServiceNote = $"Incident {incident.IncidentNumber} — {request.Summary}";
                asset.OutOfServiceSince = now;
                asset.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        await LogAsync(isNew ? "Created" : "Updated", nameof(Incident), incident.Id, request.MemberId,
            $"{request.Kind}: {request.Summary}", false, userId);

        return (await GetIncidentAsync(incident.Id))!;
    }

    public async Task<IncidentActionDto> AddIncidentActionAsync(Guid incidentId, IncidentActionDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var incident = await db.Incidents.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == incidentId)
            ?? throw new InvalidOperationException("Incident not found.");

        var action = new IncidentAction
        {
            IncidentId = incidentId,
            Action = request.Action,
            AssignedStaffId = request.AssignedStaffId,
            RaisedOn = now,
            DueOn = request.DueOn,
            CompletedOn = request.CompletedOn,
            CompletionNote = request.CompletionNote,
        }.StampNew(tenant, userId);

        db.IncidentActions.Add(action);

        if (incident.Status == IncidentStatus.Open) incident.Status = IncidentStatus.ActionRequired;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(action, now);
    }

    public async Task<IncidentDto> CloseIncidentAsync(
        Guid incidentId, string rootCause, string preventiveAction, Guid userId)
    {
        var now = DateTime.UtcNow;

        var incident = await db.Incidents.ForTenant(tenant)
            .Include(i => i.Actions.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(i => i.Id == incidentId)
            ?? throw new InvalidOperationException("Incident not found.");

        var openActions = incident.Actions.Count(a => !a.IsDeleted && a.CompletedOn is null);
        if (openActions > 0)
            throw new InvalidOperationException(
                $"{openActions} action(s) are still outstanding. Complete them before closing the incident.");

        if (string.IsNullOrWhiteSpace(rootCause))
            throw new InvalidOperationException("Closing an incident needs a root cause — that is what makes it useful later.");

        incident.Status = IncidentStatus.Closed;
        incident.ClosedOn = now;
        incident.RootCause = rootCause;
        incident.PreventiveAction = preventiveAction;
        incident.StampUpdated(userId);

        await db.SaveChangesAsync();
        await LogAsync("Closed", nameof(Incident), incidentId, incident.MemberId, rootCause, false, userId);

        return (await GetIncidentAsync(incidentId))!;
    }

    // ── Complaints ───────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<ComplaintDto>> GetComplaintsAsync(
        Guid? clubId, ComplaintStatus? status, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.Complaints.ForTenant(tenant)
            .Include(c => c.Member)
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .WhereIf(status is not null, c => c.Status == status);

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(c => c.Status).ThenBy(c => c.Priority).ThenByDescending(c => c.RaisedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var items = page.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c, now);
            dto.ClubName = clubNames.GetValueOrDefault(c.ClubId);
            if (c.OwnerStaffId is not null) dto.OwnerName = staffNames.GetValueOrDefault(c.OwnerStaffId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<ComplaintDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<ComplaintDto> SaveComplaintAsync(Guid? id, SaveComplaintDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        Complaint complaint;
        if (id is null)
        {
            complaint = new Complaint
            {
                ComplaintNumber = await numbering.NextComplaintNumberAsync(now),
                ClubId = request.ClubId,
                RaisedOn = now,
            }.StampNew(tenant, userId);

            db.Complaints.Add(complaint);
        }
        else
        {
            complaint = await db.Complaints.ForTenant(tenant)
                .Include(c => c.Member)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Complaint not found.");
            complaint.StampUpdated(userId);
        }

        complaint.MemberId = request.MemberId;
        complaint.ComplainantName = request.ComplainantName;
        complaint.ComplainantContact = request.ComplainantContact;
        complaint.Category = request.Category;
        complaint.Summary = request.Summary;
        complaint.Detail = request.Detail;
        complaint.Priority = request.Priority;
        complaint.OwnerStaffId = request.OwnerStaffId;
        complaint.Channel = request.Channel;

        // A default target rather than none — an unresolved complaint with no clock is a complaint
        // that ages quietly.
        complaint.TargetResolutionOn = request.TargetResolutionOn
            ?? complaint.TargetResolutionOn
            ?? now.Date.AddDays(request.Priority == 1 ? 2 : 5);

        if (complaint.Status == ComplaintStatus.Open && request.OwnerStaffId is not null)
        {
            complaint.Status = ComplaintStatus.Acknowledged;
            complaint.AcknowledgedOn = now;
        }

        // A complaint from a member is a retention event, so it lands on their timeline too.
        if (id is null && request.MemberId is not null)
        {
            db.MemberNotes.Add(new MemberNote
            {
                MemberId = request.MemberId.Value,
                Kind = InteractionKind.Complaint,
                Body = $"{request.Category}: {request.Summary}",
                OccurredAt = now,
                IsPinned = true,
                RelatedEntityId = complaint.Id,
                RelatedEntityType = nameof(Complaint),
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(complaint, now);
    }

    public async Task<ComplaintDto> ResolveComplaintAsync(ResolveComplaintDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var complaint = await db.Complaints.ForTenant(tenant)
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.Id == request.ComplaintId)
            ?? throw new InvalidOperationException("Complaint not found.");

        complaint.Status = ComplaintStatus.Resolved;
        complaint.ResolvedOn = now;
        complaint.Resolution = request.Resolution;
        complaint.CompensationValue = request.CompensationValue;
        complaint.CompensationNote = request.CompensationNote;
        complaint.ComplainantSatisfied = request.ComplainantSatisfied;
        complaint.StampUpdated(userId);

        if (complaint.MemberId is not null)
        {
            db.MemberNotes.Add(new MemberNote
            {
                MemberId = complaint.MemberId.Value,
                Kind = InteractionKind.SystemEvent,
                Body = $"Complaint resolved — {request.Resolution}",
                OccurredAt = now,
                StaffId = userId == Guid.Empty ? null : userId,
            }.StampNew(tenant, userId));

            // Goodwill goes onto the account rather than being a promise in a note.
            if (request.IssueCredit && request.CompensationValue is > 0 && complaint.Member is not null)
            {
                complaint.Member.CreditBalance += request.CompensationValue.Value;
                complaint.Member.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(complaint, now);
    }

    // ── Lost property ────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<LostPropertyItemDto>> GetLostPropertyAsync(
        Guid? clubId, LostPropertyStatus? status, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.LostProperty.ForTenant(tenant)
            .WhereIf(clubId is not null, l => l.ClubId == clubId)
            .WhereIf(status is not null, l => l.Status == status);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(l => l.FoundOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<LostPropertyItemDto>.Ok(
                   [.. page.Select(l => FitnessMapper.ToDto(l, now))],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<LostPropertyItemDto> SaveLostPropertyAsync(Guid? id, SaveLostPropertyDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        LostPropertyItem item;
        if (id is null)
        {
            item = new LostPropertyItem { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.LostProperty.Add(item);
        }
        else
        {
            item = await db.LostProperty.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new InvalidOperationException("Item not found.");
            item.StampUpdated(userId);
        }

        item.ItemDescription = request.ItemDescription;
        item.Category = request.Category;
        item.PhotoUrl = request.PhotoUrl;
        item.FoundOn = request.FoundOn ?? now;
        item.FoundLocation = request.FoundLocation;
        item.FoundByStaffId = userId == Guid.Empty ? null : userId;
        item.StorageLocation = request.StorageLocation;
        item.DisposeAfter = item.FoundOn.AddDays(request.HoldDays);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(item, now);
    }

    public async Task<LostPropertyItemDto> ClaimLostPropertyAsync(ClaimLostPropertyDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var item = await db.LostProperty.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == request.ItemId)
            ?? throw new InvalidOperationException("Item not found.");

        if (item.Status != LostPropertyStatus.Held)
            throw new InvalidOperationException($"That item has already been {item.Status.ToString().ToLower()}.");

        item.Status = LostPropertyStatus.Claimed;
        item.ClaimedByMemberId = request.ClaimedByMemberId;
        item.ClaimedByName = request.ClaimedByName;
        item.ClaimedOn = now;
        item.ReleasedByStaffId = userId == Guid.Empty ? null : userId;
        item.StampUpdated(userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(item, now);
    }

    // ── Facility checks ──────────────────────────────────────────────────────

    public async Task<List<FacilityCheckDto>> GetChecksAsync(Guid clubId, bool dueTodayOnly)
    {
        var now = DateTime.UtcNow;

        var checks = await db.FacilityChecks.ForTenant(tenant)
            .Where(c => c.ClubId == clubId && c.IsActive)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .OrderBy(c => c.DueAt)
            .ToListAsync();

        var dtos = checks.Select(c => FitnessMapper.ToDto(c, now)).ToList();

        if (dueTodayOnly) dtos = [.. dtos.Where(d => d.DueToday)];

        var areaNames = await db.Areas.ForTenant(tenant)
            .Where(a => a.ClubId == clubId)
            .Select(a => new { a.Id, a.Name })
            .ToDictionaryAsync(a => a.Id, a => a.Name);

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        foreach (var dto in dtos)
        {
            if (dto.AreaId is not null) dto.AreaName = areaNames.GetValueOrDefault(dto.AreaId.Value);

            foreach (var item in dto.Items.Where(i => i.LastCompletedAt is not null))
            {
                var entity = checks.SelectMany(c => c.Items).FirstOrDefault(i => i.Id == item.Id);
                if (entity?.LastCompletedByStaffId is not null)
                    item.LastCompletedByName = staffNames.GetValueOrDefault(entity.LastCompletedByStaffId.Value);
            }
        }

        return dtos;
    }

    public async Task<FacilityCheckDto> SaveCheckAsync(Guid? id, FacilityCheckDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        FacilityCheck check;
        if (id is null)
        {
            check = new FacilityCheck { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.FacilityChecks.Add(check);
        }
        else
        {
            check = await db.FacilityChecks.ForTenant(tenant)
                .Include(c => c.Items.Where(i => !i.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Check not found.");
            check.StampUpdated(userId);
        }

        check.AreaId = request.AreaId;
        check.Name = request.Name;
        check.Kind = request.Kind;
        check.DaysOfWeekMask = request.DaysOfWeekMask;
        check.DueAt = request.DueAt;
        check.TimesPerDay = Math.Max(1, request.TimesPerDay);
        check.DefaultAssigneeRoleId = request.DefaultAssigneeRoleId;
        check.AlertOnMissed = request.AlertOnMissed;
        check.MissedAfterMinutes = request.MissedAfterMinutes;
        check.RequiresSignature = request.RequiresSignature;
        check.IsActive = request.IsActive;

        var existing = check.Items.Where(i => !i.IsDeleted).ToList();
        var keptIds = request.Items.Where(i => i.Id != Guid.Empty).Select(i => i.Id).ToHashSet();

        foreach (var gone in existing.Where(i => !keptIds.Contains(i.Id))) gone.StampDeleted(userId);

        var order = 0;
        foreach (var itemDto in request.Items.OrderBy(i => i.DisplayOrder))
        {
            var item = existing.FirstOrDefault(i => i.Id == itemDto.Id);
            if (item is null)
            {
                item = new FacilityCheckItem { FacilityCheckId = check.Id }.StampNew(tenant, userId);
                db.FacilityCheckItems.Add(item);
            }
            else
            {
                item.StampUpdated(userId);
            }

            item.ItemDescription = itemDto.ItemDescription;
            item.DisplayOrder = order++;
            item.AnswerKind = itemDto.AnswerKind;
            item.Unit = itemDto.Unit;
            item.AcceptableLow = itemDto.AcceptableLow;
            item.AcceptableHigh = itemDto.AcceptableHigh;
            item.IsCritical = itemDto.IsCritical;
            item.RequiresPhoto = itemDto.RequiresPhoto;
        }

        await db.SaveChangesAsync();

        var saved = await db.FacilityChecks.ForTenant(tenant)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .FirstAsync(c => c.Id == check.Id);

        return FitnessMapper.ToDto(saved, now);
    }

    /// <summary>
    /// Submitting a completed check.
    ///
    /// A reading outside its acceptable band — a pool pH, a fridge temperature — raises a
    /// corrective action automatically. That is the entire reason to hold these in software
    /// rather than on a clipboard: an out-of-range number on paper is a number somebody has to
    /// notice.
    /// </summary>
    public async Task<FacilityCheckDto> SubmitCheckAsync(SubmitFacilityCheckDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var check = await db.FacilityChecks.ForTenant(tenant)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == request.FacilityCheckId)
            ?? throw new InvalidOperationException("Check not found.");

        var failures = new List<string>();

        foreach (var answer in request.Items)
        {
            var item = check.Items.FirstOrDefault(i => i.Id == answer.ItemId);
            if (item is null) continue;

            var outOfRange = answer.Value is not null
                             && ((item.AcceptableLow is not null && answer.Value < item.AcceptableLow)
                              || (item.AcceptableHigh is not null && answer.Value > item.AcceptableHigh));

            var passed = answer.Passed ?? !outOfRange;

            item.LastCompletedAt = now;
            item.LastCompletedByStaffId = userId == Guid.Empty ? null : userId;
            item.LastPassed = passed;
            item.LastValue = answer.Value;
            item.LastNote = answer.Note;
            item.StampUpdated(userId);

            if (!passed || outOfRange)
            {
                failures.Add(outOfRange
                    ? $"{item.ItemDescription}: {answer.Value}{item.Unit} (acceptable {item.AcceptableLow}–{item.AcceptableHigh}{item.Unit})"
                    : $"{item.ItemDescription}: {answer.Note ?? "failed"}");
            }
        }

        // Anything failed raises an incident so it cannot simply be logged and forgotten.
        if (failures.Count > 0)
        {
            var critical = request.Items.Any(a =>
                check.Items.FirstOrDefault(i => i.Id == a.ItemId)?.IsCritical == true && a.Passed == false);

            await SaveIncidentAsync(null, new SaveIncidentDto
            {
                ClubId = request.ClubId,
                AreaId = check.AreaId,
                Kind = check.Kind == FacilityCheckKind.PoolChemistry ? IncidentKind.Other : IncidentKind.NearMiss,
                Severity = critical ? IncidentSeverity.Serious : IncidentSeverity.Minor,
                OccurredAt = now,
                Summary = $"{check.Name} — {failures.Count} item(s) failed",
                Detail = string.Join("\n", failures),
                ImmediateAction = request.Note,
            }, userId);
        }

        await db.SaveChangesAsync();

        var saved = await db.FacilityChecks.ForTenant(tenant)
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .FirstAsync(c => c.Id == check.Id);

        return FitnessMapper.ToDto(saved, now);
    }

    public async Task<ShiftHandoverDto> SaveHandoverAsync(ShiftHandoverDto request, Guid userId)
    {
        var handover = new ShiftHandover
        {
            ClubId = request.ClubId,
            ShiftEndedAt = request.ShiftEndedAt == default ? DateTime.UtcNow : request.ShiftEndedAt,
            FromStaffId = request.FromStaffId ?? (userId == Guid.Empty ? null : userId),
            ToStaffId = request.ToStaffId,
            Notes = request.Notes,
            OutstandingItems = request.OutstandingItems,
            HasUrgentItems = request.HasUrgentItems,
        }.StampNew(tenant, userId);

        db.Handovers.Add(handover);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(handover);
    }

    public async Task<List<ShiftHandoverDto>> GetHandoversAsync(Guid clubId, int limit)
    {
        var handovers = await db.Handovers.ForTenant(tenant)
            .Where(h => h.ClubId == clubId)
            .OrderByDescending(h => h.ShiftEndedAt)
            .Take(limit)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. handovers.Select(h =>
        {
            var dto = FitnessMapper.ToDto(h);
            if (h.FromStaffId is not null) dto.FromStaffName = staffNames.GetValueOrDefault(h.FromStaffId.Value);
            if (h.ToStaffId is not null) dto.ToStaffName = staffNames.GetValueOrDefault(h.ToStaffId.Value);
            if (h.AcknowledgedByStaffId is not null)
                dto.AcknowledgedByName = staffNames.GetValueOrDefault(h.AcknowledgedByStaffId.Value);
            return dto;
        })];
    }

    // ── Audit ────────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<AuditEntryDto>> GetAuditAsync(
        Guid? clubId, Guid? memberId, Guid? actorUserId, string? entityType,
        bool sensitiveOnly, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Audit.ForTenant(tenant)
            .WhereIf(clubId is not null, a => a.ClubId == clubId)
            .WhereIf(memberId is not null, a => a.MemberId == memberId)
            .WhereIf(actorUserId is not null, a => a.ActorUserId == actorUserId)
            .WhereIf(!string.IsNullOrWhiteSpace(entityType), a => a.EntityType == entityType)
            .WhereIf(sensitiveOnly, a => a.IsSensitiveAccess)
            .WhereIf(from is not null, a => a.OccurredAt >= from)
            .WhereIf(to is not null, a => a.OccurredAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var memberIds = page.Where(a => a.MemberId is not null).Select(a => a.MemberId!.Value).Distinct().ToList();
        var names = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        var items = page.Select(a =>
        {
            var dto = FitnessMapper.ToDto(a);
            if (a.MemberId is not null) dto.MemberName = names.GetValueOrDefault(a.MemberId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<AuditEntryDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    /// <summary>
    /// Writes an audit row.
    ///
    /// Called from anywhere that touches money, entitlements or health data. Reads of
    /// special-category data are logged as well as writes — that is the standard this kind of
    /// data is held to, and the reason the log is a table rather than a log line.
    /// </summary>
    public async Task LogAsync(
        string action, string entityType, Guid? entityId, Guid? memberId,
        string? summary, bool isSensitive, Guid userId)
    {
        db.Audit.Add(new AuditEntry
        {
            OccurredAt = DateTime.UtcNow,
            ActorUserId = userId == Guid.Empty ? null : userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            MemberId = memberId,
            ChangeSummary = summary,
            IsSensitiveAccess = isSensitive,
            IpAddress = ClientIp(),
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<List<IncidentDto>> DecorateIncidentsAsync(List<Incident> incidents, DateTime now)
    {
        var memberIds = incidents.Where(i => i.MemberId is not null).Select(i => i.MemberId!.Value).Distinct().ToList();
        var names = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var areaNames = await db.Areas.ForTenant(tenant)
            .Select(a => new { a.Id, a.Name }).ToDictionaryAsync(a => a.Id, a => a.Name);

        var assetNames = await db.Equipment.ForTenant(tenant)
            .Select(e => new { e.Id, e.Name }).ToDictionaryAsync(e => e.Id, e => e.Name);

        return [.. incidents.Select(i =>
        {
            var dto = FitnessMapper.ToDto(i, now);
            dto.ClubName = clubNames.GetValueOrDefault(i.ClubId);
            if (i.MemberId is not null) dto.MemberName = names.GetValueOrDefault(i.MemberId.Value);
            if (i.AreaId is not null) dto.AreaName = areaNames.GetValueOrDefault(i.AreaId.Value);
            if (i.EquipmentAssetId is not null) dto.EquipmentName = assetNames.GetValueOrDefault(i.EquipmentAssetId.Value);
            if (i.ReportedByStaffId is not null) dto.ReportedByName = staffNames.GetValueOrDefault(i.ReportedByStaffId.Value);
            if (i.OwnerStaffId is not null) dto.OwnerName = staffNames.GetValueOrDefault(i.OwnerStaffId.Value);

            foreach (var action in dto.Actions.Where(a => a.AssignedStaffId is not null))
                action.AssignedStaffName = staffNames.GetValueOrDefault(action.AssignedStaffId!.Value);

            return dto;
        })];
    }

    private void AddAction(Incident incident, string action, DateTime dueOn, Guid userId)
    {
        db.IncidentActions.Add(new IncidentAction
        {
            IncidentId = incident.Id,
            Action = action,
            AssignedStaffId = incident.OwnerStaffId,
            RaisedOn = DateTime.UtcNow,
            DueOn = dueOn,
        }.StampNew(tenant, userId));
    }

    private async Task SupersedeSignaturesAsync(Guid oldTemplateId, Guid userId)
    {
        var signatures = await db.WaiverSignatures.ForTenant(tenant)
            .Where(s => s.WaiverTemplateId == oldTemplateId && s.Status == SignatureStatus.Signed)
            .ToListAsync();

        foreach (var signature in signatures)
        {
            signature.Status = SignatureStatus.Superseded;
            signature.StampUpdated(userId);

            if (signature.MemberId is not null)
            {
                var member = await db.Members.ForTenant(tenant)
                    .FirstOrDefaultAsync(m => m.Id == signature.MemberId);

                if (member is not null) member.WaiverSigned = false;
            }
        }
    }

    private static string ShortForm(int questionNumber) => questionNumber switch
    {
        1 => "Heart condition",
        2 or 3 => "Chest pain",
        4 => "Dizziness or blackouts",
        5 => "Bone or joint problem",
        6 => "Blood pressure or heart medication",
        7 => "Other medical reason",
        8 => "Pregnancy or recent birth",
        _ => "See screening",
    };

    private string? ClientIp() => httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent()
    {
        var value = httpContext.HttpContext?.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Length > 500 ? value[..500] : value;
    }
}
