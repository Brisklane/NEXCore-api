using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Six months of trading for the demonstration club.
///
/// This exists because a gym app with a price list and no members is indistinguishable from a
/// broken one: the dashboard is zeros, the front desk is empty, retention has nothing to score,
/// and reports show blank charts. A prospective customer clicking around cannot tell whether the
/// screens work.
///
/// So the seeded club has a **plausible** history rather than a tidy one. Members joined at
/// different times, some have left, some are in arrears, attendance decays realistically after
/// joining, classes are full in the evening and quiet at midday, and January has a joining spike.
/// The numbers on the dashboard are arithmetic over this data, not constants — which means the
/// demo is also a test.
///
/// Everything derives from a fixed seed, so the same company always gets the same club.
/// </summary>
public class FitnessOperationalSeeder(FitnessDbContext db)
{
    private readonly Random _random = new(20260101);

    private static readonly string[] FirstNames =
    [
        "Oliver", "Amelia", "Harry", "Isla", "Jack", "Ava", "George", "Mia", "Noah", "Grace",
        "Leo", "Sophia", "Arthur", "Freya", "Muhammad", "Aisha", "Oscar", "Poppy", "Theo", "Ivy",
        "Ethan", "Zara", "Daniel", "Chloe", "Samuel", "Ella", "Adam", "Ruby", "Ali", "Maya",
        "Joseph", "Nina", "Lucas", "Hannah", "Ibrahim", "Layla", "Max", "Erin", "Finn", "Sofia",
        "Kai", "Anna", "Reuben", "Priya", "Marcus", "Elena", "Omar", "Ruth", "Tom", "Lucy",
        "Ben", "Katie", "Josh", "Emma", "Nathan", "Rachel", "Dominic", "Sarah", "Callum", "Jess",
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Jones", "Taylor", "Brown", "Williams", "Wilson", "Johnson", "Davies", "Patel", "Robinson",
        "Wright", "Thompson", "Evans", "Walker", "White", "Roberts", "Green", "Hall", "Wood", "Jackson",
        "Clarke", "Khan", "Lewis", "Hughes", "Edwards", "Murphy", "Okafor", "Nguyen", "Rossi", "Kowalski",
        "Andersen", "Silva", "Fernandez", "Yilmaz", "Haddad", "Osei", "Novak", "Dubois", "Weber", "Moreau",
    ];

    private static readonly string[] Goals =
    [
        "Lose weight", "Build strength", "Get fitter for football", "Rehab after a knee injury",
        "Feel better day to day", "Train for a half marathon", "Tone up before a wedding",
        "Manage stress", "Improve mobility", "Get back into a routine",
    ];

    public async Task SeedAsync(FixedFitnessTenant tenant, FitnessClub club)
    {
        var now = DateTime.UtcNow;
        var start = now.Date.AddMonths(-6);

        var plans = await db.Plans
            .Where(p => p.CompanyId == tenant.CompanyId && !p.IsDeleted
                     && p.Kind != PlanKind.RetailProduct)
            .ToListAsync();

        if (plans.Count == 0) return;

        var staff = await db.Staff
            .Where(s => s.ClubId == club.Id && !s.IsDeleted).ToListAsync();

        var sources = await db.LeadSources
            .Where(s => s.CompanyId == tenant.CompanyId && !s.IsDeleted).ToListAsync();

        var tiers = await db.LoyaltyTiers
            .Where(t => t.CompanyId == tenant.CompanyId && !t.IsDeleted)
            .OrderBy(t => t.Ordinal).ToListAsync();

        var waiver = await db.WaiverTemplates
            .FirstOrDefaultAsync(w => w.CompanyId == tenant.CompanyId && !w.IsDeleted && w.IsPublished);

        var schedules = await db.ClassSchedules
            .Where(s => s.ClubId == club.Id && !s.IsDeleted && s.IsPublished)
            .Include(s => s.ClassType)
            .ToListAsync();

        var memberships = plans
            .Where(p => p.Kind is PlanKind.RecurringMembership or PlanKind.TermMembership)
            .ToList();

        var salesStaff = staff.Where(s => s.CanSell).ToList();

        // ── Members and their agreements ─────────────────────────────────────

        var members = new List<Member>();
        var agreements = new List<Agreement>();
        var memberNumber = 1;

        for (var week = 0; week < 26; week++)
        {
            var weekStart = start.AddDays(week * 7);

            // Joining is seasonal. January and September are the two peaks every club sees; the
            // dead weeks between Christmas and New Year, and mid-August, are the troughs.
            var joinsThisWeek = weekStart.Month switch
            {
                1 => _random.Next(9, 16),
                9 => _random.Next(7, 12),
                8 or 12 => _random.Next(2, 5),
                _ => _random.Next(4, 9),
            };

            for (var i = 0; i < joinsThisWeek; i++)
            {
                var joinedOn = weekStart.AddDays(_random.Next(7)).AddHours(_random.Next(9, 20));
                if (joinedOn > now) continue;

                var plan = WeightedPlan(memberships);
                var (member, agreement) = BuildMember(
                    tenant, club, plan, joinedOn, memberNumber++, sources, salesStaff, now);

                members.Add(member);
                agreements.Add(agreement);

                db.Members.Add(member);
                db.Agreements.Add(agreement);

                db.MemberStatusHistory.Add(new MemberStatusHistory
                {
                    MemberId = member.Id,
                    FromStatus = MemberStatus.Lead,
                    ToStatus = MemberStatus.Active,
                    ChangedAt = joinedOn,
                    Reason = "Joined",
                    SourceEntityId = agreement.Id,
                    SourceEntityType = nameof(Agreement),
                }.StampNew(tenant));

                db.Credentials.Add(new MemberCredential
                {
                    MemberId = member.Id,
                    Type = CredentialType.RfidFob,
                    Identifier = $"FOB{member.MemberNumber[4..]}",
                    Status = member.Status == MemberStatus.Cancelled
                        ? CredentialStatus.Deactivated
                        : CredentialStatus.Active,
                    IssuedOn = joinedOn,
                }.StampNew(tenant));

                SeedConsents(tenant, member, joinedOn);

                if (waiver is not null) SeedWaiverSignature(tenant, club, waiver, member, joinedOn);

                if (tiers.Count > 0) SeedLoyaltyAccount(tenant, club, member, tiers);
            }
        }

        await db.SaveChangesAsync();

        // ── Billing, invoices and payments ───────────────────────────────────

        var invoiceCounter = 1;
        var paymentCounter = 1;

        foreach (var agreement in agreements)
        {
            var member = members.First(m => m.Id == agreement.MemberId);
            var plan = plans.First(p => p.Id == agreement.PlanId);

            SeedBillingHistory(tenant, club, member, agreement, plan, now, ref invoiceCounter, ref paymentCounter);
        }

        await db.SaveChangesAsync();

        // ── Classes ──────────────────────────────────────────────────────────

        if (schedules.Count > 0)
            SeedClassHistory(tenant, club, schedules, members, staff, now);

        await db.SaveChangesAsync();

        // ── Visits ───────────────────────────────────────────────────────────

        SeedVisits(tenant, club, members, now);
        await db.SaveChangesAsync();

        // ── Families ─────────────────────────────────────────────────────────

        SeedHouseholds(tenant, members, now);
        await db.SaveChangesAsync();

        // ── Pipeline ─────────────────────────────────────────────────────────

        SeedOpenLeads(tenant, club, sources, salesStaff, memberships, now);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// A handful of families, because every club has them and the screens that handle them look
    /// wrong when they are empty.
    ///
    /// Built from members who already share a surname, so the demo data reads like a real club's
    /// rather than like a random pairing of strangers. One family gets a junior close to eighteen,
    /// which is the case the household screen exists to warn about.
    /// </summary>
    private void SeedHouseholds(FixedFitnessTenant tenant, List<Member> members, DateTime now)
    {
        var families = members
            .Where(m => m.Status == MemberStatus.Active)
            .GroupBy(m => m.LastName)
            .Where(g => g.Count() >= 2)
            .Take(6)
            .ToList();

        foreach (var family in families)
        {
            var people = family.Take(_random.Next(2, 5)).ToList();
            var primary = people[0];

            var household = new Household
            {
                Name = $"{family.Key} household",
                PrimaryMemberId = primary.Id,
                AddressLine = $"{_random.Next(1, 120)} Elm Street",
                City = "Springfield",
                PostCode = $"SP{_random.Next(1, 9)} {_random.Next(1, 9)}AB",
                AnyAdultMayCheckInChildren = _random.NextDouble() < 0.8,
            }.StampNew(tenant);

            db.Households.Add(household);

            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i];
                var role = i switch
                {
                    0 => HouseholdRole.Primary,
                    1 => HouseholdRole.Partner,
                    _ => HouseholdRole.Child,
                };

                // A junior in the demo data is genuinely a junior — the plan review this triggers
                // is the point of showing it.
                if (role == HouseholdRole.Child)
                {
                    person.DateOfBirth = now.Date.AddYears(-_random.Next(13, 18)).AddDays(-_random.Next(300));
                }

                person.HouseholdId = household.Id;

                db.HouseholdMembers.Add(new HouseholdMember
                {
                    HouseholdId = household.Id,
                    MemberId = person.Id,
                    Role = role,
                    MayCollectChildren = role != HouseholdRole.Child,
                    AgesOutOn = role == HouseholdRole.Child && person.DateOfBirth is not null
                        ? person.DateOfBirth.Value.AddYears(18)
                        : null,
                }.StampNew(tenant));
            }
        }
    }

    // ═══ Members ═════════════════════════════════════════════════════════════

    private (Member Member, Agreement Agreement) BuildMember(
        FixedFitnessTenant tenant, FitnessClub club, MembershipPlan plan, DateTime joinedOn,
        int number, List<LeadSource> sources, List<FitnessStaff> salesStaff, DateTime now)
    {
        var firstName = FirstNames[_random.Next(FirstNames.Length)];
        var lastName = LastNames[_random.Next(LastNames.Length)];

        var monthsSinceJoining = (now - joinedOn).TotalDays / 30.44;

        // Who has left. Attrition is front-loaded — the first ninety days is where a gym loses
        // people, and a model that spreads it evenly makes retention look better than it is.
        var cancelChance = monthsSinceJoining switch
        {
            < 1 => 0.02,
            < 3 => 0.14,
            < 6 => 0.10,
            _ => 0.06,
        };

        var hasCancelled = _random.NextDouble() < cancelChance;
        var isFrozen = !hasCancelled && _random.NextDouble() < 0.05;
        var isPastDue = !hasCancelled && !isFrozen && _random.NextDouble() < 0.07;

        var status = hasCancelled ? MemberStatus.Cancelled
            : isFrozen ? MemberStatus.Frozen
            : isPastDue ? MemberStatus.PastDue
            : MemberStatus.Active;

        var cancelledOn = hasCancelled
            ? joinedOn.AddDays(_random.Next(30, Math.Max(45, (int)(now - joinedOn).TotalDays)))
            : (DateTime?)null;

        if (cancelledOn > now) cancelledOn = now.AddDays(-_random.Next(1, 20));

        var age = _random.Next(18, 68);
        var member = new Member
        {
            MemberNumber = $"MEM-{number:000000}",
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = now.Date.AddYears(-age).AddDays(-_random.Next(365)),
            Gender = _random.Next(2) == 0 ? Gender.Male : Gender.Female,
            Phone = $"07{_random.Next(100, 999)}{_random.Next(100000, 999999)}",
            Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}{number}@example.com",
            City = "Springfield",
            Status = status,
            HomeClubId = club.Id,
            JoinedOn = joinedOn,
            FirstJoinedOn = joinedOn,
            LeftOn = cancelledOn,
            LeadSourceId = sources.Count == 0 ? null : sources[_random.Next(sources.Count)].Id,
            WaiverSigned = true,
            WaiverSignedOn = joinedOn,
            MedicalClearance = ClearanceStatus.NotRequired,
            PhotoConsent = _random.NextDouble() < 0.6,
            LeaderboardOptIn = _random.NextDouble() < 0.8,
            PreferredChannel = _random.NextDouble() < 0.7 ? MessageChannel.Email : MessageChannel.Sms,
        }.StampNew(tenant);

        // Money owed, for the collections screen to have something real in it.
        if (isPastDue) member.AccountBalance = Math.Round((decimal)(_random.NextDouble() * 90 + 25), 2);

        var price = plan.Price;
        var agreement = new Agreement
        {
            AgreementNumber = $"AGR-{number:000000}",
            MemberId = member.Id,
            PlanId = plan.Id,
            PlanVersion = plan.Version,
            ClubId = club.Id,
            Status = hasCancelled ? AgreementStatus.Cancelled : AgreementStatus.Active,
            StartsOn = joinedOn.Date,
            SignedOn = joinedOn,
            CancelledOn = cancelledOn,
            CancellationEffectiveOn = cancelledOn?.AddDays(plan.NoticePeriodDays),
            CoolingOffEndsOn = joinedOn.Date.AddDays(14),
            Price = price,
            CurrencyCode = club.CurrencyCode,
            BillingPeriod = plan.BillingPeriod,
            BillingAnchor = plan.BillingAnchor,
            NoticePeriodDays = plan.NoticePeriodDays,
            AutoRenews = plan.AutoRenews,
            MinimumTermEndsOn = plan.MinimumTermMonths > 0
                ? joinedOn.Date.AddMonths(plan.MinimumTermMonths)
                : null,
            EndsOn = plan.DurationMonths is not null
                ? joinedOn.Date.AddMonths(plan.DurationMonths.Value)
                : null,
            SoldByStaffId = salesStaff.Count == 0 ? null : salesStaff[_random.Next(salesStaff.Count)].Id,
        }.StampNew(tenant);

        if (hasCancelled)
        {
            agreement.LeaveReason = (LeaveReason)_random.Next(1, 12);
            agreement.LeaveNote = "Recorded at cancellation.";
        }

        // A promotional price on some joiners, so the billing engine's promo handling is exercised
        // and the MRR movement report has real expansion when promotions run out.
        if (!hasCancelled && _random.NextDouble() < 0.18 && plan.BillingPeriod == BillingPeriod.Monthly)
        {
            agreement.PromotionalPrice = Math.Round(price / 2, 2);
            agreement.PromotionalPeriodsRemaining = Math.Max(0, 2 - (int)monthsSinceJoining);
        }

        return (member, agreement);
    }

    private void SeedConsents(FixedFitnessTenant tenant, Member member, DateTime joinedOn)
    {
        var consents = new (MessageChannel Channel, string Purpose, double Chance)[]
        {
            (MessageChannel.Email, "Service messages", 1.0),
            (MessageChannel.Sms, "Service messages", 1.0),
            (MessageChannel.Email, "Marketing", 0.62),
            (MessageChannel.Sms, "Marketing", 0.41),
            (MessageChannel.Push, "Marketing", 0.55),
        };

        foreach (var (channel, purpose, chance) in consents)
        {
            db.Consents.Add(new MemberConsent
            {
                MemberId = member.Id,
                Channel = channel,
                Purpose = purpose,
                Granted = _random.NextDouble() < chance,
                DecidedAt = joinedOn,
                CapturedVia = "Join wizard",
            }.StampNew(tenant));
        }
    }

    private void SeedWaiverSignature(
        FixedFitnessTenant tenant, FitnessClub club, WaiverTemplate waiver, Member member, DateTime joinedOn)
    {
        db.WaiverSignatures.Add(new WaiverSignature
        {
            WaiverTemplateId = waiver.Id,
            TemplateVersion = waiver.Version,
            MemberId = member.Id,
            ClubId = club.Id,
            SignerName = $"{member.FirstName} {member.LastName}",
            SignerEmail = member.Email,
            Status = SignatureStatus.Signed,
            SignedAt = joinedOn,
            CapturedVia = "Desk tablet",
        }.StampNew(tenant));
    }

    private void SeedLoyaltyAccount(
        FixedFitnessTenant tenant, FitnessClub club, Member member, List<LoyaltyTier> tiers)
    {
        var points = _random.Next(0, 3200);
        var tier = tiers.LastOrDefault(t => t.PointsRequired <= points) ?? tiers[0];
        var next = tiers.FirstOrDefault(t => t.PointsRequired > points);

        db.LoyaltyAccounts.Add(new LoyaltyAccount
        {
            MemberId = member.Id,
            ClubId = club.Id,
            PointsBalance = points,
            LifetimePoints = points + _random.Next(0, 400),
            TierId = tier.Id,
            TierAchievedOn = member.JoinedOn,
            PointsToNextTier = next is null ? 0 : next.PointsRequired - points,
        }.StampNew(tenant));

        member.LoyaltyPoints = points;
    }

    // ═══ Money ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Every period the member has been billed for, with the invoice and the payment behind it.
    ///
    /// Roughly one payment in twenty fails, which is close to what a real direct-debit file looks
    /// like — and it gives the collections screen, the dunning ladder and the arrears report
    /// something honest to work on.
    /// </summary>
    private void SeedBillingHistory(
        FixedFitnessTenant tenant, FitnessClub club, Member member, Agreement agreement,
        MembershipPlan plan, DateTime now, ref int invoiceCounter, ref int paymentCounter)
    {
        var periodStart = agreement.StartsOn;
        var periodNumber = 1;
        var stopOn = agreement.CancellationEffectiveOn ?? now;

        // Joining fee, charged once and recognised immediately.
        if (plan.JoiningFee > 0)
        {
            var invoice = BuildInvoice(tenant, club, member, agreement, agreement.StartsOn,
                plan.JoiningFee, ChargeKind.JoiningFee, "Joining fee", ref invoiceCounter);

            SettlePaid(tenant, club, member, invoice, agreement.StartsOn, PaymentMethod.Card, ref paymentCounter);
        }

        while (periodStart < stopOn && periodStart < now)
        {
            var periodEnd = periodStart.AddPeriod(agreement.BillingPeriod).AddDays(-1);
            var price = agreement.PromotionalPrice is not null && periodNumber <= 2
                ? agreement.PromotionalPrice.Value
                : agreement.Price;

            db.BillingSchedules.Add(new BillingSchedule
            {
                AgreementId = agreement.Id,
                MemberId = member.Id,
                ClubId = club.Id,
                DueOn = periodStart,
                PeriodNumber = periodNumber,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ChargeKind = ChargeKind.MembershipDues,
                Amount = price,
                CurrencyCode = club.CurrencyCode,
                IsBilled = true,
            }.StampNew(tenant));

            var invoice = BuildInvoice(tenant, club, member, agreement, periodStart, price,
                ChargeKind.MembershipDues,
                $"{plan.Name} — {periodStart:d MMM} to {periodEnd:d MMM}", ref invoiceCounter,
                periodStart, periodEnd);

            // The last period for a member in arrears stays unpaid; everything else settles, with
            // the occasional failure along the way.
            var isLastPeriod = periodStart.AddPeriod(agreement.BillingPeriod) > now;
            var leaveUnpaid = isLastPeriod && member.Status == MemberStatus.PastDue;

            if (leaveUnpaid)
            {
                RecordFailure(tenant, club, member, invoice, periodStart, ref paymentCounter);
            }
            else
            {
                if (_random.NextDouble() < 0.05)
                    RecordFailure(tenant, club, member, invoice, periodStart, ref paymentCounter);

                var method = plan.BillingPeriod == BillingPeriod.Annual
                    ? PaymentMethod.Card
                    : PaymentMethod.DirectDebit;

                SettlePaid(tenant, club, member, invoice, periodStart.AddDays(_random.Next(0, 3)),
                    method, ref paymentCounter);
            }

            agreement.PeriodsBilled = periodNumber;
            agreement.LastBilledOn = periodStart;
            agreement.NextBillingOn = periodStart.AddPeriod(agreement.BillingPeriod);

            periodStart = periodStart.AddPeriod(agreement.BillingPeriod);
            periodNumber++;

            if (periodNumber > 60) break;
        }

        if (agreement.Status == AgreementStatus.Active)
            member.NextBillingOn = agreement.NextBillingOn;
    }

    private FitnessInvoice BuildInvoice(
        FixedFitnessTenant tenant, FitnessClub club, Member member, Agreement agreement,
        DateTime issuedOn, decimal amount, ChargeKind kind, string description, ref int counter,
        DateTime? periodStart = null, DateTime? periodEnd = null)
    {
        var invoice = new FitnessInvoice
        {
            InvoiceNumber = $"INV-{issuedOn:yyMMdd}-{counter:0000}",
            MemberId = member.Id,
            ClubId = club.Id,
            AgreementId = agreement.Id,
            Status = InvoiceStatus.Issued,
            IssuedOn = issuedOn,
            DueOn = issuedOn,
            Subtotal = amount,
            Total = amount,
            BalanceDue = amount,
            CurrencyCode = club.CurrencyCode,
        }.StampNew(tenant);

        db.Invoices.Add(invoice);

        db.InvoiceLines.Add(new FitnessInvoiceLine
        {
            InvoiceId = invoice.Id,
            ChargeKind = kind,
            LineDescription = description,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Quantity = 1,
            UnitPrice = amount,
            LineTotal = amount,
            PlanId = agreement.PlanId,
        }.StampNew(tenant));

        counter++;
        return invoice;
    }

    private void SettlePaid(
        FixedFitnessTenant tenant, FitnessClub club, Member member, FitnessInvoice invoice,
        DateTime receivedOn, PaymentMethod method, ref int counter)
    {
        if (receivedOn > DateTime.UtcNow) return;

        db.Payments.Add(new FitnessPayment
        {
            PaymentNumber = $"PAY-{receivedOn:yyMMdd}-{counter:0000}",
            MemberId = member.Id,
            InvoiceId = invoice.Id,
            ClubId = club.Id,
            Method = method,
            Status = PaymentStatus.Succeeded,
            Amount = invoice.BalanceDue,
            CurrencyCode = club.CurrencyCode,
            ReceivedOn = receivedOn,
            SettledOn = receivedOn.AddDays(method == PaymentMethod.DirectDebit ? 3 : 0),
            CardBrand = method == PaymentMethod.Card ? "Visa" : null,
            CardLastFour = method == PaymentMethod.Card ? _random.Next(1000, 9999).ToString() : null,
            MandateReference = method == PaymentMethod.DirectDebit ? $"DDM{_random.Next(100000, 999999)}" : null,
            AuthorisationCode = _random.Next(100000, 999999).ToString(),
        }.StampNew(tenant));

        invoice.AmountPaid = invoice.Total;
        invoice.BalanceDue = 0;
        invoice.PaidOn = receivedOn;
        invoice.Status = InvoiceStatus.Paid;

        counter++;
    }

    private void RecordFailure(
        FixedFitnessTenant tenant, FitnessClub club, Member member, FitnessInvoice invoice,
        DateTime attemptedOn, ref int counter)
    {
        // The four reasons that actually come back off a collections file, in roughly the
        // proportions a club sees them.
        var reason = _random.NextDouble() switch
        {
            < 0.45 => PaymentFailureReason.InsufficientFunds,
            < 0.70 => PaymentFailureReason.CardExpired,
            < 0.88 => PaymentFailureReason.MandateCancelled,
            _ => PaymentFailureReason.CardDeclined,
        };

        db.Payments.Add(new FitnessPayment
        {
            PaymentNumber = $"PAY-{attemptedOn:yyMMdd}-{counter:0000}",
            MemberId = member.Id,
            InvoiceId = invoice.Id,
            ClubId = club.Id,
            Method = PaymentMethod.DirectDebit,
            Status = PaymentStatus.Failed,
            Amount = invoice.BalanceDue,
            CurrencyCode = club.CurrencyCode,
            ReceivedOn = attemptedOn,
            FailureReason = reason,
            FailureMessage = Describe(reason),
            AttemptNumber = 1,
        }.StampNew(tenant));

        invoice.Status = InvoiceStatus.Overdue;
        counter++;
    }

    // ═══ Classes ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates the last eight weeks and the next two, then books them.
    ///
    /// Fill follows the time of day rather than being uniform: the 06:30 and 18:30 slots run at
    /// 80–100% and produce a waitlist, midday runs at 30–50%. That shape is what makes the class
    /// performance report say something useful instead of returning a flat average.
    /// </summary>
    private void SeedClassHistory(
        FixedFitnessTenant tenant, FitnessClub club, List<ClassSchedule> schedules,
        List<Member> members, List<FitnessStaff> staff, DateTime now)
    {
        var activeMembers = members
            .Where(m => m.Status is MemberStatus.Active or MemberStatus.PastDue)
            .ToList();

        if (activeMembers.Count == 0) return;

        var from = now.Date.AddDays(-56);
        var to = now.Date.AddDays(14);

        foreach (var schedule in schedules)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                if (!FitnessQueryExtensions.CoversDay(schedule.DaysOfWeekMask, day.DayOfWeek)) continue;

                var startsAt = day.Add(schedule.StartsAt);
                var endsAt = startsAt.AddMinutes(schedule.DurationMinutes);
                var isPast = endsAt < now;

                // Peak hours fill; the middle of the day does not.
                var fillTarget = schedule.StartsAt.Hours switch
                {
                    <= 7 => 0.80 + _random.NextDouble() * 0.25,
                    >= 17 and <= 19 => 0.85 + _random.NextDouble() * 0.25,
                    >= 9 and <= 15 => 0.28 + _random.NextDouble() * 0.30,
                    _ => 0.50 + _random.NextDouble() * 0.35,
                };

                var booked = Math.Min(schedule.Capacity, (int)Math.Round(schedule.Capacity * fillTarget));
                var overflow = fillTarget > 1.0 ? _random.Next(1, 5) : 0;

                var occurrence = new ClassOccurrence
                {
                    ClubId = club.Id,
                    ClassTypeId = schedule.ClassTypeId,
                    ClassScheduleId = schedule.Id,
                    RoomId = schedule.RoomId,
                    InstructorStaffId = schedule.InstructorStaffId,
                    StartsAt = startsAt,
                    EndsAt = endsAt,
                    Capacity = schedule.Capacity,
                    Status = isPast ? ClassOccurrenceStatus.Completed : ClassOccurrenceStatus.Scheduled,
                    BookingOpensAt = startsAt.AddDays(-14),
                    BookingClosesAt = startsAt,
                    BookedCount = booked,
                    WaitlistCount = overflow,
                }.StampNew(tenant);

                // The occasional cancelled class — an instructor off sick, which the substitute and
                // notification paths need to have happened at least once.
                if (isPast && _random.NextDouble() < 0.015)
                {
                    occurrence.Status = ClassOccurrenceStatus.Cancelled;
                    occurrence.CancelledAt = startsAt.AddHours(-3);
                    occurrence.CancellationReason = "Instructor unwell — members notified and credits returned.";
                    occurrence.CancellationNotified = true;
                    occurrence.BookedCount = 0;
                    occurrence.WaitlistCount = 0;
                }

                db.ClassOccurrences.Add(occurrence);

                if (occurrence.Status == ClassOccurrenceStatus.Cancelled) continue;

                var attendees = PickDistinct(activeMembers, booked);
                var attended = 0;
                var noShows = 0;

                foreach (var member in attendees)
                {
                    var status = BookingStatus.Booked;
                    DateTime? checkedInAt = null;

                    if (isPast)
                    {
                        // Around one booking in nine is not honoured — which is why the no-show
                        // policy, the strike ladder and the waitlist promotion all exist.
                        var roll = _random.NextDouble();
                        if (roll < 0.82)
                        {
                            status = BookingStatus.Attended;
                            checkedInAt = startsAt.AddMinutes(-_random.Next(2, 12));
                            attended++;
                        }
                        else if (roll < 0.90)
                        {
                            status = BookingStatus.NoShow;
                            noShows++;
                        }
                        else if (roll < 0.96)
                        {
                            status = BookingStatus.Cancelled;
                        }
                        else
                        {
                            status = BookingStatus.LateCancelled;
                        }
                    }

                    db.ClassBookings.Add(new ClassBooking
                    {
                        ClassOccurrenceId = occurrence.Id,
                        MemberId = member.Id,
                        Status = status,
                        PaymentKind = BookingPaymentKind.Entitlement,
                        Channel = _random.NextDouble() < 0.75 ? BookingChannel.MemberApp : BookingChannel.FrontDesk,
                        BookedAt = startsAt.AddDays(-_random.Next(1, 8)),
                        CheckedInAt = checkedInAt,
                        CancelledAt = status is BookingStatus.Cancelled or BookingStatus.LateCancelled
                            ? startsAt.AddHours(-_random.Next(1, 30))
                            : null,
                        CreditsUsed = 1,
                    }.StampNew(tenant));
                }

                if (isPast)
                {
                    occurrence.AttendedCount = attended;
                    occurrence.NoShowCount = noShows;
                }

                // Waitlisted members, so the promotion path has real rows behind it.
                foreach (var member in PickDistinct(activeMembers.Except(attendees).ToList(), overflow))
                {
                    db.ClassBookings.Add(new ClassBooking
                    {
                        ClassOccurrenceId = occurrence.Id,
                        MemberId = member.Id,
                        Status = isPast ? BookingStatus.Cancelled : BookingStatus.Waitlisted,
                        PaymentKind = BookingPaymentKind.Entitlement,
                        Channel = BookingChannel.MemberApp,
                        BookedAt = startsAt.AddDays(-_random.Next(1, 5)),
                        WaitlistPosition = _random.Next(1, overflow + 1),
                    }.StampNew(tenant));
                }
            }
        }
    }

    // ═══ Visits ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gym-floor visits, with attendance that decays the way real attendance does.
    ///
    /// A member trains hardest in their first month and settles into a lower rate after that —
    /// and a meaningful minority stop entirely without cancelling. Modelling that is what makes
    /// the churn scores, the "zero visits" report and the retention board show something worth
    /// acting on rather than a uniform hum.
    /// </summary>
    private void SeedVisits(FixedFitnessTenant tenant, FitnessClub club, List<Member> members, DateTime now)
    {
        var from = now.Date.AddDays(-90);

        foreach (var member in members)
        {
            if (member.JoinedOn is null) continue;

            var start = member.JoinedOn.Value.Date > from ? member.JoinedOn.Value.Date : from;
            var end = member.LeftOn ?? now;
            if (end > now) end = now;
            if (start >= end) continue;

            // Their long-run rate, plus whether they have quietly gone dormant.
            var baseline = _random.NextDouble() switch
            {
                < 0.18 => 0.3,   // barely comes
                < 0.50 => 1.2,
                < 0.80 => 2.4,
                _ => 3.8,        // most days
            };

            var goneQuiet = member.Status == MemberStatus.Active && _random.NextDouble() < 0.12;
            var quietFrom = goneQuiet ? now.AddDays(-_random.Next(16, 70)) : DateTime.MaxValue;

            var visits = 0;
            DateTime? lastVisit = null;

            for (var day = start; day <= end; day = day.AddDays(1))
            {
                if (day >= quietFrom) break;

                var weeksIn = (day - member.JoinedOn.Value).TotalDays / 7;

                // Honeymoon: about 60% more in the first fortnight, tailing off over three months.
                var multiplier = weeksIn switch
                {
                    < 2 => 1.6,
                    < 6 => 1.25,
                    < 12 => 1.0,
                    _ => 0.85,
                };

                var chance = baseline * multiplier / 7;
                if (_random.NextDouble() > chance) continue;

                // When people actually come: before work, lunchtime, or after work — with the
                // evening peak the biggest by a distance.
                var hour = _random.NextDouble() switch
                {
                    < 0.22 => _random.Next(6, 9),
                    < 0.36 => _random.Next(11, 14),
                    < 0.85 => _random.Next(17, 21),
                    _ => _random.Next(9, 17),
                };

                var checkedInAt = day.AddHours(hour).AddMinutes(_random.Next(60));
                if (checkedInAt > now) continue;

                var duration = _random.Next(35, 95);

                db.CheckIns.Add(new CheckIn
                {
                    ClubId = club.Id,
                    MemberId = member.Id,
                    Kind = VisitKind.Member,
                    CheckedInAt = checkedInAt,
                    CheckedOutAt = checkedInAt.AddMinutes(duration),
                    DurationMinutes = duration,
                    Method = _random.NextDouble() < 0.7 ? CredentialType.RfidFob : CredentialType.MembershipQr,
                }.StampNew(tenant));

                visits++;
                lastVisit = checkedInAt;
            }

            member.TotalVisits = visits;
            member.LastVisitOn = lastVisit;
            member.VisitsThisMonth = visits == 0 ? 0 : _random.Next(0, Math.Min(visits, 14));
            member.VisitFrequencyBaseline = Math.Round((decimal)baseline, 2);
            member.CurrentStreakDays = lastVisit is not null && (now - lastVisit.Value).TotalDays < 2
                ? _random.Next(1, 21)
                : 0;
        }
    }

    // ═══ Pipeline ════════════════════════════════════════════════════════════

    /// <summary>
    /// Live enquiries for the leads board — including a few already past their response SLA,
    /// because a board with nothing overdue on it does not demonstrate the thing the board is for.
    /// </summary>
    private void SeedOpenLeads(
        FixedFitnessTenant tenant, FitnessClub club, List<LeadSource> sources,
        List<FitnessStaff> salesStaff, List<MembershipPlan> plans, DateTime now)
    {
        for (var i = 0; i < 34; i++)
        {
            var firstName = FirstNames[_random.Next(FirstNames.Length)];
            var lastName = LastNames[_random.Next(LastNames.Length)];
            var receivedAt = now.AddHours(-_random.Next(1, 21 * 24));

            var stage = _random.NextDouble() switch
            {
                < 0.26 => LeadStatus.New,
                < 0.48 => LeadStatus.Contacted,
                < 0.66 => LeadStatus.TourBooked,
                < 0.80 => LeadStatus.Toured,
                < 0.90 => LeadStatus.Trialling,
                _ => LeadStatus.Negotiating,
            };

            var contacted = stage != LeadStatus.New;
            var responseMinutes = contacted
                ? _random.NextDouble() < 0.7 ? _random.Next(2, 15) : _random.Next(20, 400)
                : (int?)null;

            var lead = new FitnessLead
            {
                ClubId = club.Id,
                FirstName = firstName,
                LastName = lastName,
                Phone = $"07{_random.Next(100, 999)}{_random.Next(100000, 999999)}",
                Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
                Status = stage,
                LeadSourceId = sources.Count == 0 ? null : sources[_random.Next(sources.Count)].Id,
                Goal = Goals[_random.Next(Goals.Length)],
                ReceivedAt = receivedAt,
                FirstContactedAt = contacted ? receivedAt.AddMinutes(responseMinutes!.Value) : null,
                ResponseMinutes = responseMinutes,
                SlaBreached = responseMinutes > 15 || (!contacted && (now - receivedAt).TotalMinutes > 15),
                AssignedStaffId = salesStaff.Count == 0 ? null : salesStaff[_random.Next(salesStaff.Count)].Id,
                AssignedAt = receivedAt,
                ContactAttempts = contacted ? _random.Next(1, 4) : 0,
                LastActivityAt = contacted ? receivedAt.AddHours(_random.Next(1, 48)) : null,
                NextFollowUpOn = now.Date.AddDays(_random.Next(0, 5)),
                TourBookedFor = stage is LeadStatus.TourBooked
                    ? now.AddDays(_random.Next(1, 6)).Date.AddHours(_random.Next(10, 19))
                    : null,
                TouredOn = stage is LeadStatus.Toured or LeadStatus.Trialling or LeadStatus.Negotiating
                    ? receivedAt.AddDays(_random.Next(1, 5))
                    : null,
                TrialStartedOn = stage == LeadStatus.Trialling ? now.AddDays(-_random.Next(1, 6)) : null,
                TrialEndsOn = stage == LeadStatus.Trialling ? now.AddDays(_random.Next(1, 6)) : null,
            }.StampNew(tenant);

            db.Leads.Add(lead);

            if (contacted)
            {
                db.LeadActivities.Add(new LeadActivity
                {
                    LeadId = lead.Id,
                    Kind = _random.Next(2) == 0 ? LeadActivityKind.Call : LeadActivityKind.Sms,
                    OccurredAt = lead.FirstContactedAt!.Value,
                    StaffId = lead.AssignedStaffId,
                    WasSuccessfulContact = true,
                    Summary = "First contact made.",
                }.StampNew(tenant));
            }
        }
    }

    // ═══ Helpers ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Weighted plan selection.
    ///
    /// Standard is the volume seller, Premium and Off-Peak split most of the rest, and Junior and
    /// Student are small — which is the mix nearly every club reports, and the reason the plan-mix
    /// report is worth having.
    /// </summary>
    private MembershipPlan WeightedPlan(List<MembershipPlan> plans)
    {
        var weights = new Dictionary<string, int>
        {
            ["STANDARD"] = 44,
            ["PREMIUM"] = 18,
            ["OFFPEAK"] = 20,
            ["ANNUAL"] = 8,
            ["STUDENT"] = 7,
            ["JUNIOR"] = 3,
        };

        var pool = plans
            .SelectMany(p => Enumerable.Repeat(p, weights.GetValueOrDefault(p.Code ?? "", 1)))
            .ToList();

        return pool.Count == 0 ? plans[0] : pool[_random.Next(pool.Count)];
    }

    private List<Member> PickDistinct(List<Member> pool, int count)
    {
        if (count <= 0 || pool.Count == 0) return [];

        var wanted = Math.Min(count, pool.Count);
        var picked = new HashSet<int>();

        while (picked.Count < wanted) picked.Add(_random.Next(pool.Count));

        return [.. picked.Select(i => pool[i])];
    }

    private static string Describe(PaymentFailureReason reason) => reason switch
    {
        PaymentFailureReason.InsufficientFunds => "Not enough funds in the account.",
        PaymentFailureReason.CardExpired => "The card on file has expired.",
        PaymentFailureReason.MandateCancelled => "The direct debit mandate was cancelled at the bank.",
        PaymentFailureReason.CardDeclined => "The bank declined the payment.",
        _ => "The payment did not go through.",
    };
}
