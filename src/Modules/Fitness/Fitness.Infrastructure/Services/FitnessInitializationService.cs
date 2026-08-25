using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Stands a company's Fitness app up as something that works on first open.
///
/// Two tiers, and the distinction matters:
///
/// **Essential** is the configuration without which the app cannot function at all — you cannot
/// sign a member up with no club, open a door with no access rule, take a payment with no dunning
/// ladder, or let anyone through the barrier with no waiver on file. Every company gets it,
/// always. An app that greets a new customer with a blank screen and a silent save button is
/// broken, not minimal.
///
/// **Sample** is the demonstration club — a real price list, a full week's timetable, trainers
/// with PINs, lockers, commission rules. Only companies that ticked "seed sample data" at
/// registration get it, because a real gym wants to type in their own prices, not delete
/// someone else's.
///
/// Everything is idempotent and gated per tier, so a company that skipped sample data at sign-up
/// can ask for it later, and one that already has a club is never given a second one.
/// </summary>
public class FitnessInitializationService(
    FitnessDbContext db,
    ILogger<FitnessInitializationService> logger)
{
    /// <summary>Called by the CompanyCreated handler. Provisions essentials, and samples if asked.</summary>
    public Task<bool> InitializeForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
        => EnsureProvisionedAsync(companyId, branchId, businessUnitId, userId, includeSampleData);

    /// <summary>
    /// Brings a company's Fitness app up to a working state, whenever it is called.
    ///
    /// This exists because installing an app is not the same event as creating a company: a
    /// business that has been running NexCore for a year and adds Fitness today never saw
    /// <c>CompanyCreatedEvent</c>, and would otherwise land on a member list with no club behind
    /// it — where every save silently does nothing.
    /// </summary>
    public async Task<bool> EnsureProvisionedAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
    {
        var tenant = new FixedFitnessTenant(companyId, branchId, businessUnitId, userId);

        var club = await db.Clubs
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.BranchId == branchId
                                   && c.BusinessUnitId == businessUnitId && !c.IsDeleted);

        var alreadyProvisioned = club is not null;

        // Sample data on a club that already sells memberships would duplicate the price list, so
        // it is only laid down on a club that is still empty.
        var wantsSample = includeSampleData
            && !await db.Plans.AnyAsync(p => p.CompanyId == companyId && !p.IsDeleted);

        // Trading history is gated separately from the price list. A club seeded before this
        // existed has plans but no members, and would otherwise keep a blank dashboard for ever.
        var wantsHistory = includeSampleData
            && !await db.Members.AnyAsync(m => m.CompanyId == companyId && !m.IsDeleted);

        if (alreadyProvisioned && !wantsSample && !wantsHistory) return false;

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            if (!await db.Settings.AnyAsync(s => s.CompanyId == companyId && s.BranchId == branchId
                                              && s.BusinessUnitId == businessUnitId && !s.IsDeleted))
                SeedSettings(tenant);

            club ??= SeedClub(tenant);
            await db.SaveChangesAsync();

            List<ClubArea> areas;
            List<Room> rooms;

            if (!alreadyProvisioned)
            {
                SeedSchedules(tenant, club);
                areas = SeedAreas(tenant, club);
                await db.SaveChangesAsync();

                rooms = SeedRooms(tenant, club, areas);
                await db.SaveChangesAsync();

                SeedSpotMap(tenant, rooms);
                SeedAccess(tenant, club, areas);
                SeedStaffRoles(tenant);
                SeedPolicies(tenant, club);
                SeedWaiver(tenant, club);
                SeedLeadSources(tenant);
                SeedLossReasons(tenant);
                SeedLoyalty(tenant);
                SeedBadges(tenant);
                SeedAssessmentTemplate(tenant);
                SeedFacilityChecks(tenant, club, areas);
                SeedExercises(tenant);
                SeedAppointmentServices(tenant);
            }
            else
            {
                areas = await db.Areas.Where(a => a.ClubId == club.Id && !a.IsDeleted).ToListAsync();
                rooms = await db.Rooms.Where(r => r.ClubId == club.Id && !r.IsDeleted).ToListAsync();
            }

            await db.SaveChangesAsync();

            // ── Sample club ──────────────────────────────────────────────────
            if (wantsSample)
            {
                var plans = SeedPlans(tenant, club);
                await db.SaveChangesAsync();

                SeedPlanDetail(tenant, club, plans);
                SeedPromotion(tenant, club, plans);

                var classTypes = SeedClassTypes(tenant, club);
                await db.SaveChangesAsync();

                var staff = SeedStaff(tenant, club);
                await db.SaveChangesAsync();

                SeedTimetable(tenant, club, classTypes, rooms, staff);
                SeedBookableStaff(tenant, club, staff);
                SeedLockers(tenant, club, areas);
                SeedCommissionRules(tenant, club);
                SeedRetailProducts(tenant, club);
            }

            await db.SaveChangesAsync();

            // A price list with no members behind it leaves the dashboard, reports, front desk and
            // timetable blank, which reads as broken rather than empty. The sample club gets a
            // quarter of real trading so every screen has something true to show.
            if (wantsHistory)
            {
                await new FitnessOperationalSeeder(db).SeedAsync(tenant, club);
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            logger.LogInformation(
                "Fitness module provisioned for company {CompanyId} — club {ClubName}, sample: {Sample}, history: {History}",
                companyId, club.Name, wantsSample, wantsHistory);

            return true;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Failed to provision the Fitness module for company {CompanyId}", companyId);
            throw;
        }
    }

    // ═══ Essential ═══════════════════════════════════════════════════════════

    private FitnessSettings SeedSettings(FixedFitnessTenant tenant)
    {
        var settings = new FitnessSettings
        {
            MemberNumberPrefix = "MEM",
            DefaultNoticePeriodDays = 30,
            DefaultCoolingOffDays = 14,
            MaxFreezeDaysPerYear = 90,
            DefaultBillingAnchor = BillingAnchor.JoinAnniversary,
            DefaultProration = ProrationRule.Daily,
            InvoiceGraceDays = 3,
            AutoRunBilling = true,
            BillingRunTime = new TimeSpan(2, 0, 0),
            AccessCacheSeconds = 300,
            AbsenceRiskDays = 14,
            CriticalAbsenceDays = 30,
            AutoScoreChurn = true,
            RespectQuietHours = true,
            QuietHoursFrom = new TimeSpan(21, 0, 0),
            QuietHoursTo = new TimeSpan(8, 0, 0),
            LeadResponseSlaMinutes = 15,
            DiscountApprovalThresholdPercent = 20,
            RefundApprovalThreshold = 100,
            WriteOffApprovalThreshold = 50,
            RequirePinForOverrides = true,
        }.StampNew(tenant);

        db.Settings.Add(settings);
        return settings;
    }

    private FitnessClub SeedClub(FixedFitnessTenant tenant)
    {
        var club = new FitnessClub
        {
            Name = "Main Club",
            Code = "CLUB-01",
            ClubType = ClubType.Gym,
            CurrencyCode = "USD",
            UnitSystem = UnitSystem.Metric,
            TimeZoneId = TimeZoneInfo.Local.Id,
            SoftCapacity = 120,
            HardCapacity = 150,
            MinimumAge = 16,
            GuardianRequiredBelowAge = 14,
            RequiresWaiver = true,
            RequiresHealthScreening = true,
            AntiPassback = AntiPassbackMode.Soft,
            AntiPassbackMinutes = 60,
            OfflinePolicy = OfflineAccessPolicy.AllowKnownActive,
            AccessBalanceThreshold = 50m,
            AllowsCrossClubVisits = true,
            ReceiptFooter = "Thanks for training with us.",
        }.StampNew(tenant);

        db.Clubs.Add(club);
        return club;
    }

    /// <summary>
    /// Opening hours: staffed 06:00–22:00 on weekdays, shorter at weekends.
    ///
    /// Staffed hours are separate from opening hours because they are different questions — a
    /// 24-hour club is open at 3am and has nobody in it, and the access engine, the class
    /// scheduler and the incident rules all need to know which is which.
    /// </summary>
    private void SeedSchedules(FixedFitnessTenant tenant, FitnessClub club)
    {
        for (var day = 0; day < 7; day++)
        {
            var weekend = day is 0 or 6;

            db.ClubSchedules.Add(new ClubSchedule
            {
                ClubId = club.Id,
                DayOfWeek = day,
                OpensAt = weekend ? new TimeSpan(8, 0, 0) : new TimeSpan(6, 0, 0),
                ClosesAt = weekend ? new TimeSpan(18, 0, 0) : new TimeSpan(22, 0, 0),
                StaffedFrom = weekend ? new TimeSpan(8, 0, 0) : new TimeSpan(6, 30, 0),
                StaffedTo = weekend ? new TimeSpan(17, 0, 0) : new TimeSpan(21, 30, 0),
            }.StampNew(tenant));
        }
    }

    private List<ClubArea> SeedAreas(FixedFitnessTenant tenant, FitnessClub club)
    {
        var areas = new List<ClubArea>
        {
            new() { Name = "Reception", Kind = AreaKind.Reception, DisplayOrder = 1 },
            new() { Name = "Gym Floor", Kind = AreaKind.GymFloor, DisplayOrder = 2, Capacity = 90 },
            new() { Name = "Studio 1", Kind = AreaKind.Studio, DisplayOrder = 3, Capacity = 24 },
            new() { Name = "Studio 2", Kind = AreaKind.Studio, DisplayOrder = 4, Capacity = 16 },
            new() { Name = "Functional Zone", Kind = AreaKind.FunctionalZone, DisplayOrder = 5, Capacity = 20 },
            new() { Name = "Changing Rooms", Kind = AreaKind.ChangingRoom, DisplayOrder = 6 },
        };

        foreach (var area in areas)
        {
            area.ClubId = club.Id;
            area.StampNew(tenant);
            db.Areas.Add(area);
        }

        return areas;
    }

    private List<Room> SeedRooms(FixedFitnessTenant tenant, FitnessClub club, List<ClubArea> areas)
    {
        var studio1 = areas.First(a => a.Name == "Studio 1");
        var studio2 = areas.First(a => a.Name == "Studio 2");
        var functional = areas.First(a => a.Name == "Functional Zone");

        var rooms = new List<Room>
        {
            new()
            {
                Name = "Studio 1", AreaId = studio1.Id, Capacity = 24, DisplayOrder = 1,
                HasSpotMap = true, GridColumns = 6, GridRows = 4,
                EquipmentNote = "24 spin bikes, sound system, projector",
            },
            new() { Name = "Studio 2", AreaId = studio2.Id, Capacity = 16, DisplayOrder = 2 },
            new() { Name = "Rig", AreaId = functional.Id, Capacity = 20, DisplayOrder = 3 },
        };

        foreach (var room in rooms)
        {
            room.ClubId = club.Id;
            room.StampNew(tenant);
            db.Rooms.Add(room);
        }

        return rooms;
    }

    /// <summary>
    /// Bike numbers for the spin studio.
    ///
    /// Laid out left-to-right, front row first, so the number a member is told at the desk matches
    /// the number painted on the floor. Getting this backwards is a small thing that generates a
    /// surprising number of complaints.
    /// </summary>
    private void SeedSpotMap(FixedFitnessTenant tenant, List<Room> rooms)
    {
        var studio = rooms.FirstOrDefault(r => r.HasSpotMap);
        if (studio is null) return;

        var number = 1;
        for (var row = 0; row < studio.GridRows; row++)
        {
            for (var column = 0; column < studio.GridColumns; column++)
            {
                db.RoomSpots.Add(new RoomSpot
                {
                    RoomId = studio.Id,
                    Label = number.ToString(),
                    GridColumn = column,
                    GridRow = row,
                }.StampNew(tenant));

                number++;
            }
        }
    }

    /// <summary>
    /// The default access rule, one controller and the doors on it.
    ///
    /// The rule is deliberately permissive on time (any hour the club is open) and strict on
    /// money and paperwork: a member who owes more than the threshold or has not signed a waiver
    /// does not get through the barrier. Those are the two refusals a gym actually wants.
    /// </summary>
    private void SeedAccess(FixedFitnessTenant tenant, FitnessClub club, List<ClubArea> areas)
    {
        var rule = new AccessRule
        {
            Name = "Standard access",
            ClubId = club.Id,
            BalanceThreshold = 50m,
            RequiresWaiver = true,
            RequiresMedicalClearance = false,
            RespectsOccupancyCap = true,
            AntiPassback = AntiPassbackMode.Soft,
            MinimumAge = 16,
            VisitLimitBasis = EntitlementLimit.Unlimited,
            IsDefault = true,
            Description = "Any hour the club is open, provided the account is in good standing.",
        }.StampNew(tenant);

        db.AccessRules.Add(rule);

        db.AccessRuleWindows.Add(new AccessRuleWindow
        {
            AccessRuleId = rule.Id,
            DaysOfWeekMask = 127,
            StartsAt = new TimeSpan(0, 0, 0),
            EndsAt = new TimeSpan(23, 59, 59),
            Label = "All hours",
        }.StampNew(tenant));

        var controller = new AccessController
        {
            ClubId = club.Id,
            Name = "Front of house",
            Vendor = "Generic",
            OfflinePolicy = OfflineAccessPolicy.AllowKnownActive,
            CacheSeconds = 300,
            HeartbeatTimeoutMinutes = 5,
            Description = "Holds an offline entitlement cache so the barrier keeps working if the network drops.",
        }.StampNew(tenant);

        db.Controllers.Add(controller);

        var reception = areas.FirstOrDefault(a => a.Kind == AreaKind.Reception);
        var studio1 = areas.FirstOrDefault(a => a.Name == "Studio 1");

        db.Doors.Add(new Door
        {
            ClubId = club.Id,
            AreaId = reception?.Id,
            ControllerId = controller.Id,
            Name = "Main turnstile",
            Direction = ReaderDirection.Bidirectional,
            ReaderAddress = "1",
            CountsOccupancy = true,
        }.StampNew(tenant));

        db.Doors.Add(new Door
        {
            ClubId = club.Id,
            AreaId = studio1?.Id,
            ControllerId = controller.Id,
            Name = "Studio 1 door",
            Direction = ReaderDirection.In,
            ReaderAddress = "2",
            CountsOccupancy = false,
            RequiresClassBooking = true,
            ClassBookingWindowMinutes = 15,
            Description = "Opens only for someone booked into the class starting now.",
        }.StampNew(tenant));

        db.Doors.Add(new Door
        {
            ClubId = club.Id,
            ControllerId = controller.Id,
            Name = "Staff entrance",
            Direction = ReaderDirection.Bidirectional,
            ReaderAddress = "3",
            CountsOccupancy = false,
            StaffOnly = true,
        }.StampNew(tenant));

        club.DefaultTaxPercent = 0m;
    }

    /// <summary>
    /// The five roles a club actually runs on, with the limits that stop a well-meaning
    /// receptionist writing off a year's dues.
    /// </summary>
    private void SeedStaffRoles(FixedFitnessTenant tenant)
    {
        var roles = new List<StaffRole>
        {
            new()
            {
                Name = "Club Manager", BaseKind = StaffRoleKind.Manager,
                Permissions = "fitness.*",
                DiscountLimitPercent = 100, RefundLimit = 5000, WriteOffLimit = 1000,
                CanOverridePolicies = true, CanViewMedicalData = true, CanExportMemberData = true,
                IsSystemRole = true,
            },
            new()
            {
                Name = "Duty Manager", BaseKind = StaffRoleKind.DutyManager,
                Permissions = "fitness.members.*,fitness.access.*,fitness.classes.*,fitness.billing.view,fitness.incidents.*",
                DiscountLimitPercent = 30, RefundLimit = 500, WriteOffLimit = 100,
                CanOverridePolicies = true, CanViewMedicalData = true,
                IsSystemRole = true,
            },
            new()
            {
                Name = "Receptionist", BaseKind = StaffRoleKind.Receptionist,
                Permissions = "fitness.frontdesk.*,fitness.members.view,fitness.members.edit,fitness.classes.book,fitness.pos.*",
                DiscountLimitPercent = 10, RefundLimit = 50, WriteOffLimit = 0,
                IsSystemRole = true,
                Description = "Everything the desk needs, and nothing that moves money without approval.",
            },
            new()
            {
                Name = "Sales Consultant", BaseKind = StaffRoleKind.SalesConsultant,
                Permissions = "fitness.leads.*,fitness.members.join,fitness.plans.view,fitness.tours.*",
                DiscountLimitPercent = 15, RefundLimit = 0, WriteOffLimit = 0,
                IsSystemRole = true,
            },
            new()
            {
                Name = "Personal Trainer", BaseKind = StaffRoleKind.PersonalTrainer,
                Permissions = "fitness.appointments.*,fitness.programming.*,fitness.assessments.*,fitness.clients.view",
                CanViewMedicalData = true,
                IsSystemRole = true,
                Description = "Sees medical flags for their own clients, because training someone with a "
                            + "cardiac history blind is the risk this app exists to remove.",
            },
            new()
            {
                Name = "Group Instructor", BaseKind = StaffRoleKind.GroupInstructor,
                Permissions = "fitness.classes.teach,fitness.classes.roster,fitness.programming.view",
                CanViewMedicalData = true,
                IsSystemRole = true,
            },
        };

        foreach (var role in roles)
        {
            role.StampNew(tenant);
            db.StaffRoles.Add(role);
        }
    }

    /// <summary>
    /// Booking, cancellation and dunning defaults.
    ///
    /// The dunning ladder is the one piece of configuration that most affects revenue and the one
    /// nobody wants to build from scratch. This is the industry-standard shape: retry quickly,
    /// tell the member, retry again, add a fee, then involve a human — with access suspended only
    /// after two weeks, because locking someone out over a bounced card on day two costs more in
    /// goodwill than it recovers.
    /// </summary>
    private void SeedPolicies(FixedFitnessTenant tenant, FitnessClub club)
    {
        var booking = new BookingPolicy
        {
            Name = "Standard booking",
            ClubId = club.Id,
            BookingOpensDaysBefore = 14,
            BookingClosesMinutesBefore = 0,
            MaxConcurrentBookings = 4,
            MaxBookingsPerDay = 2,
            WaitlistEnabled = true,
            MaxWaitlistLength = 20,
            HoldCreditOnWaitlist = true,
            WaitlistConfirmMinutes = 30,
            IsDefault = true,
        }.StampNew(tenant);

        db.BookingPolicies.Add(booking);

        var cancellation = new CancellationPolicy
        {
            Name = "Standard cancellation",
            ClubId = club.Id,
            FreeCancelHours = 12,
            LateCancelOutcome = PolicyOutcome.ForfeitCredit,
            NoShowOutcome = PolicyOutcome.Strike,
            NoShowGraceMinutes = 10,
            StrikeThreshold = 3,
            StrikeWindowDays = 30,
            BookingBanDays = 7,
            IsDefault = true,
            Description = "Three no-shows in a month costs a week of booking rights — the seat you did "
                        + "not take was one somebody on the waitlist wanted.",
        }.StampNew(tenant);

        db.CancellationPolicies.Add(cancellation);

        var dunning = new DunningPolicy
        {
            Name = "Standard collections",
            ClubId = club.Id,
            IsDefault = true,
            SuspendAccessAfterDays = 14,
            WriteOffAfterDays = 90,
        }.StampNew(tenant);

        db.DunningPolicies.Add(dunning);

        var steps = new (int Step, int Delay, DunningAction Action, MessageChannel? Channel, decimal Fee, string Note)[]
        {
            (1, 0, DunningAction.SendEmail, MessageChannel.Email, 0,
                "Tell them straight away — most failures are an expired card, not a refusal to pay."),
            (2, 2, DunningAction.Retry, null, 0,
                "Banks often clear on a retry once the member has topped up."),
            (3, 4, DunningAction.SendSms, MessageChannel.Sms, 0, "Email gets ignored; a text rarely does."),
            (4, 7, DunningAction.RequestCardUpdate, MessageChannel.Email, 0,
                "A secure link to re-enter their details — the club never sees the card."),
            (5, 10, DunningAction.AddLateFee, null, 10m, "A fee, only once the member has had ten days."),
            (6, 14, DunningAction.SuspendAccess, null, 0, "Access held until the balance is cleared."),
            (7, 21, DunningAction.CreateCallTask, null, 0,
                "A person picks up the phone. This is the step that recovers the most money."),
            (8, 45, DunningAction.CancelAgreement, null, 0, "Stop billing an account that is not going to pay."),
        };

        foreach (var (step, delay, action, channel, fee, note) in steps)
        {
            db.DunningSteps.Add(new DunningStep
            {
                DunningPolicyId = dunning.Id,
                StepNumber = step,
                DelayDays = delay,
                Action = action,
                Channel = channel,
                FeeAmount = fee,
                Description = note,
            }.StampNew(tenant));
        }

        club.DefaultBookingPolicyId = booking.Id;
        club.DefaultCancellationPolicyId = cancellation.Id;
        club.DefaultDunningPolicyId = dunning.Id;
    }

    /// <summary>
    /// A starting waiver and PAR-Q gate.
    ///
    /// Published unversioned and deliberately generic — this is a working default, not legal
    /// advice, and the text says so. A club's own solicitor replaces the body; the mechanics of
    /// versioning, re-signing and blocking access at the door are what the app provides.
    /// </summary>
    private void SeedWaiver(FixedFitnessTenant tenant, FitnessClub club)
    {
        db.WaiverTemplates.Add(new WaiverTemplate
        {
            Name = "Membership waiver and health declaration",
            Version = 1,
            ClubId = club.Id,
            LanguageCode = "en",
            ActivityScope = "General gym and class use",
            BodyHtml = """
                <h2>Membership waiver and health declaration</h2>
                <p>Please read this before your first visit. If anything is unclear, ask a member of staff.</p>

                <h3>Your health</h3>
                <p>You confirm that you are not aware of any medical reason why you should not take part in
                physical exercise, and that you have answered the health screening questions honestly. If your
                health changes, tell us — we can adjust what you do, but only if we know.</p>

                <h3>Using the facilities</h3>
                <p>You agree to use equipment as instructed and to follow reasonable instructions from staff.
                If you are unsure how something works, ask before you use it.</p>

                <h3>Your belongings</h3>
                <p>Lockers are provided for convenience. Please do not leave valuables in them.</p>

                <h3>Injury</h3>
                <p>Exercise carries a risk of injury. We take reasonable care of the facilities and our staff
                are qualified, but you accept that risk when you train. Nothing here limits our liability for
                death or personal injury caused by our negligence, or for anything else that cannot lawfully
                be excluded.</p>

                <h3>Your information</h3>
                <p>We hold the details you have given us to run your membership. Health information is treated
                as sensitive: access is restricted to staff who need it, and every time it is read we record
                who read it. You can ask for a copy of your data, or ask us to delete it, at any time.</p>

                <p><em>This is a general-purpose template supplied with the software. Have it reviewed by a
                qualified adviser before you rely on it.</em></p>
                """,
            ConsentClausesJson = """
                [
                  {"key":"terms","label":"I have read and accept the membership terms","required":true},
                  {"key":"health","label":"My health declaration is accurate and complete","required":true},
                  {"key":"marketing_email","label":"Send me offers and news by email","required":false},
                  {"key":"marketing_sms","label":"Send me offers and news by text","required":false},
                  {"key":"photos","label":"I am happy for progress photos to be stored on my record","required":false}
                ]
                """,
            RequiresGuardianSignature = true,
            GuardianRequiredBelowAge = 18,
            BlocksAccess = true,
            IsPublished = true,
            RequiresResignOnNewVersion = true,
        }.StampNew(tenant));
    }

    private void SeedLeadSources(FixedFitnessTenant tenant)
    {
        var sources = new (string Name, LeadSourceKind Kind, int Order)[]
        {
            ("Walk-in", LeadSourceKind.WalkIn, 1),
            ("Website enquiry", LeadSourceKind.WebForm, 2),
            ("Phone call", LeadSourceKind.Phone, 3),
            ("Member referral", LeadSourceKind.Referral, 4),
            ("Instagram", LeadSourceKind.SocialMedia, 5),
            ("Facebook", LeadSourceKind.SocialMedia, 6),
            ("Google Ads", LeadSourceKind.PaidAds, 7),
            ("Local event", LeadSourceKind.Event, 8),
            ("Corporate scheme", LeadSourceKind.Corporate, 9),
            ("Win-back campaign", LeadSourceKind.WinBack, 10),
        };

        foreach (var (name, kind, order) in sources)
        {
            db.LeadSources.Add(new LeadSource
            {
                Name = name,
                Kind = kind,
                DisplayOrder = order,
            }.StampNew(tenant));
        }
    }

    private void SeedLossReasons(FixedFitnessTenant tenant)
    {
        var reasons = new (string Name, string Category, bool Note)[]
        {
            ("Price", "Commercial", false),
            ("Joined a competitor", "Commercial", true),
            ("Location or parking", "Practical", false),
            ("Opening hours", "Practical", false),
            ("No childcare", "Practical", false),
            ("Facilities not right", "Product", true),
            ("Timetable does not suit", "Product", false),
            ("Not ready to commit", "Timing", false),
            ("Went quiet", "Timing", false),
            ("Moving away", "Timing", false),
        };

        var order = 1;
        foreach (var (name, category, note) in reasons)
        {
            db.LossReasons.Add(new LossReason
            {
                Name = name,
                Category = category,
                RequiresNote = note,
                DisplayOrder = order++,
            }.StampNew(tenant));
        }
    }

    /// <summary>
    /// Loyalty tiers keyed to turning up, not to spending.
    ///
    /// Points come from attendance and streaks rather than money, because the behaviour a gym
    /// wants to reward is the one that keeps the member: people who come three times a week do
    /// not cancel.
    /// </summary>
    private void SeedLoyalty(FixedFitnessTenant tenant)
    {
        var tiers = new (string Name, int Ordinal, int Points, string Colour, decimal Multiplier, string Benefits)[]
        {
            ("Bronze", 1, 0, "#B08D57", 1.0m, "Standard earning"),
            ("Silver", 2, 500, "#A8A9AD", 1.25m, "Earn 25% faster, one guest pass a month"),
            ("Gold", 3, 1500, "#D4AF37", 1.5m, "Earn 50% faster, two guest passes a month, priority waitlist"),
            ("Platinum", 4, 4000, "#6E7A8A", 2.0m, "Double points, free guest passes, early class booking, one free PT session a quarter"),
        };

        foreach (var (name, ordinal, points, colour, multiplier, benefits) in tiers)
        {
            db.LoyaltyTiers.Add(new LoyaltyTier
            {
                Name = name,
                Ordinal = ordinal,
                PointsRequired = points,
                ColourHex = colour,
                EarnMultiplier = multiplier,
                Benefits = benefits,
                RetentionMonths = 12,
            }.StampNew(tenant));
        }
    }

    private void SeedBadges(FixedFitnessTenant tenant)
    {
        var badges = new (string Name, string Blurb, string Criteria, string Expression, int Points, string Colour)[]
        {
            ("First visit", "You walked through the door. That is the hard bit.", "First recorded check-in", "visits >= 1", 25, "#4CAF50"),
            ("Ten visits", "Ten sessions in the bank.", "Ten lifetime check-ins", "visits >= 10", 50, "#4CAF50"),
            ("Fifty visits", "Fifty sessions. This is a habit now.", "Fifty lifetime check-ins", "visits >= 50", 150, "#2196F3"),
            ("Century", "One hundred visits.", "One hundred lifetime check-ins", "visits >= 100", 300, "#9C27B0"),
            ("Week streak", "Trained every week for a month.", "Four consecutive weeks with a visit", "week_streak >= 4", 100, "#FF9800"),
            ("Early bird", "Twenty sessions before 7am.", "Twenty check-ins before 07:00", "early_visits >= 20", 100, "#FFC107"),
            ("Class regular", "Twenty-five classes attended.", "Twenty-five class attendances", "classes >= 25", 125, "#E91E63"),
            ("Personal best", "You beat your own record.", "Any new personal record", "prs >= 1", 75, "#F44336"),
            ("Full year", "Twelve months a member.", "Twelve months since joining", "tenure_months >= 12", 250, "#00BCD4"),
            ("Referrer", "You brought a friend, and they joined.", "One converted referral", "referrals >= 1", 200, "#8BC34A"),
        };

        var order = 1;
        foreach (var (name, blurb, criteria, expression, points, colour) in badges)
        {
            db.Badges.Add(new Badge
            {
                Name = name,
                Blurb = blurb,
                CriteriaDescription = criteria,
                CriteriaExpression = expression,
                PointsAwarded = points,
                ColourHex = colour,
                DisplayOrder = order++,
                IsAutomatic = true,
            }.StampNew(tenant));
        }
    }

    /// <summary>
    /// The standard body-composition assessment.
    ///
    /// BMI, fat mass and lean mass are marked calculated, so a trainer enters the four numbers
    /// they actually measured and the rest follows. Normal ranges are populated where a broadly
    /// accepted one exists, and left empty where it does not — a made-up "normal" band on a
    /// member's record is worse than no band at all.
    /// </summary>
    private void SeedAssessmentTemplate(FixedFitnessTenant tenant)
    {
        var template = new AssessmentTemplate
        {
            Name = "Body composition and fitness check",
            Purpose = "The baseline taken at induction and repeated every twelve weeks.",
            RecommendedIntervalDays = 90,
            IsSystemTemplate = true,
            DisplayOrder = 1,
        }.StampNew(tenant);

        db.AssessmentTemplates.Add(template);

        var measures = new (string Name, MeasureType Type, MeasureDirection Direction, string Unit,
            string Group, decimal? Low, decimal? High, bool Calculated, bool Required)[]
        {
            ("Weight", MeasureType.Weight, MeasureDirection.Neutral, "kg", "Body composition", null, null, false, true),
            ("Height", MeasureType.Length, MeasureDirection.Neutral, "cm", "Body composition", null, null, false, true),
            ("BMI", MeasureType.Score, MeasureDirection.RangeIsBetter, "", "Body composition", 18.5m, 24.9m, true, false),
            ("Body fat", MeasureType.Percentage, MeasureDirection.LowerIsBetter, "%", "Body composition", null, null, false, false),
            ("Fat mass", MeasureType.Weight, MeasureDirection.LowerIsBetter, "kg", "Body composition", null, null, true, false),
            ("Lean mass", MeasureType.Weight, MeasureDirection.HigherIsBetter, "kg", "Body composition", null, null, true, false),
            ("Waist", MeasureType.Length, MeasureDirection.LowerIsBetter, "cm", "Girths", null, null, false, false),
            ("Hips", MeasureType.Length, MeasureDirection.LowerIsBetter, "cm", "Girths", null, null, false, false),
            ("Waist–hip ratio", MeasureType.Score, MeasureDirection.LowerIsBetter, "", "Girths", null, null, true, false),
            ("Chest", MeasureType.Length, MeasureDirection.Neutral, "cm", "Girths", null, null, false, false),
            ("Resting heart rate", MeasureType.Rate, MeasureDirection.LowerIsBetter, "bpm", "Cardiovascular", 60, 100, false, false),
            ("Blood pressure (systolic)", MeasureType.Pressure, MeasureDirection.RangeIsBetter, "mmHg", "Cardiovascular", 90, 120, false, false),
            ("Blood pressure (diastolic)", MeasureType.Pressure, MeasureDirection.RangeIsBetter, "mmHg", "Cardiovascular", 60, 80, false, false),
            ("Grip strength", MeasureType.Weight, MeasureDirection.HigherIsBetter, "kg", "Strength", null, null, false, false),
            ("Press-ups", MeasureType.Count, MeasureDirection.HigherIsBetter, "reps", "Strength", null, null, false, false),
            ("Plank hold", MeasureType.Duration, MeasureDirection.HigherIsBetter, "s", "Strength", null, null, false, false),
            ("Sit and reach", MeasureType.Length, MeasureDirection.HigherIsBetter, "cm", "Mobility", null, null, false, false),
        };

        var order = 1;
        foreach (var (name, type, direction, unit, group, low, high, calculated, required) in measures)
        {
            db.AssessmentMeasures.Add(new AssessmentMeasure
            {
                AssessmentTemplateId = template.Id,
                Name = name,
                MeasureType = type,
                Direction = direction,
                Unit = unit,
                Grouping = group,
                NormalLow = low,
                NormalHigh = high,
                IsCalculated = calculated,
                IsRequired = required,
                DisplayOrder = order++,
                CalculationNote = calculated ? "Worked out from the measurements above." : null,
            }.StampNew(tenant));
        }
    }

    /// <summary>
    /// Opening, closing and safety checks.
    ///
    /// Critical items are marked as such: a failed fire-exit check raises an incident rather than
    /// sitting in a list, because the whole point of writing it down is that somebody acts on it.
    /// </summary>
    private void SeedFacilityChecks(FixedFitnessTenant tenant, FitnessClub club, List<ClubArea> areas)
    {
        var opening = new FacilityCheck
        {
            ClubId = club.Id,
            Name = "Opening check",
            Kind = FacilityCheckKind.Opening,
            DaysOfWeekMask = 127,
            DueAt = new TimeSpan(6, 0, 0),
            AlertOnMissed = true,
            MissedAfterMinutes = 60,
        }.StampNew(tenant);

        db.FacilityChecks.Add(opening);

        var openingItems = new (string Text, bool Critical)[]
        {
            ("Fire exits clear and unlocked", true),
            ("Emergency lighting working", true),
            ("First aid kit stocked and in date", true),
            ("Defibrillator present, green light showing", true),
            ("Gym floor clear, weights racked", false),
            ("Changing rooms clean, no water on the floor", false),
            ("All cardio machines powered and functional", false),
            ("Studio floors clean and dry", false),
            ("Reception till float counted", false),
        };

        var order = 1;
        foreach (var (text, critical) in openingItems)
        {
            db.FacilityCheckItems.Add(new FacilityCheckItem
            {
                FacilityCheckId = opening.Id,
                ItemDescription = text,
                AnswerKind = ScreeningAnswerKind.YesNo,
                IsCritical = critical,
                DisplayOrder = order++,
            }.StampNew(tenant));
        }

        var closing = new FacilityCheck
        {
            ClubId = club.Id,
            Name = "Closing check",
            Kind = FacilityCheckKind.Closing,
            DaysOfWeekMask = 127,
            DueAt = new TimeSpan(22, 0, 0),
            RequiresSignature = true,
        }.StampNew(tenant);

        db.FacilityChecks.Add(closing);

        var closingItems = new (string Text, bool Critical)[]
        {
            ("Every area swept, nobody left in the building", true),
            ("Changing rooms and toilets empty", true),
            ("All doors and windows secured", true),
            ("Alarm set", true),
            ("Weights racked, equipment wiped down", false),
            ("Lost property logged", false),
            ("Cash counted and banked or locked away", false),
        };

        order = 1;
        foreach (var (text, critical) in closingItems)
        {
            db.FacilityCheckItems.Add(new FacilityCheckItem
            {
                FacilityCheckId = closing.Id,
                ItemDescription = text,
                AnswerKind = ScreeningAnswerKind.YesNo,
                IsCritical = critical,
                DisplayOrder = order++,
            }.StampNew(tenant));
        }

        var gymFloor = areas.FirstOrDefault(a => a.Kind == AreaKind.GymFloor);

        var sweep = new FacilityCheck
        {
            ClubId = club.Id,
            AreaId = gymFloor?.Id,
            Name = "Gym floor sweep",
            Kind = FacilityCheckKind.EquipmentSweep,
            DaysOfWeekMask = 127,
            DueAt = new TimeSpan(12, 0, 0),
            TimesPerDay = 4,
            Description = "Every couple of hours through the day — the check that keeps the floor tidy and "
                        + "catches a broken machine before a member finds it.",
        }.StampNew(tenant);

        db.FacilityChecks.Add(sweep);

        var sweepItems = new[]
        {
            "Weights returned to racks",
            "Benches and mats wiped down",
            "Machines free of faults",
            "Water stations stocked",
            "Bins emptied",
        };

        order = 1;
        foreach (var text in sweepItems)
        {
            db.FacilityCheckItems.Add(new FacilityCheckItem
            {
                FacilityCheckId = sweep.Id,
                ItemDescription = text,
                AnswerKind = ScreeningAnswerKind.YesNo,
                DisplayOrder = order++,
            }.StampNew(tenant));
        }
    }

    /// <summary>
    /// A movement library big enough to write a programme on day one.
    ///
    /// Marked as system exercises so a club can add its own without them being mixed in, and the
    /// ones worth tracking a record against are flagged with the score type that record is
    /// measured in — a back squat PR is a load, a 5k PR is a time, and treating them the same
    /// produces nonsense leaderboards.
    /// </summary>
    private void SeedExercises(FixedFitnessTenant tenant)
    {
        var exercises = new (string Name, ExerciseCategory Category, string Muscles, bool Pr, ScoreType? Score)[]
        {
            ("Back squat", ExerciseCategory.Barbell, "Quads, glutes, core", true, ScoreType.MaxLoad),
            ("Front squat", ExerciseCategory.Barbell, "Quads, core", true, ScoreType.MaxLoad),
            ("Deadlift", ExerciseCategory.Barbell, "Posterior chain", true, ScoreType.MaxLoad),
            ("Romanian deadlift", ExerciseCategory.Barbell, "Hamstrings, glutes", true, ScoreType.MaxLoad),
            ("Bench press", ExerciseCategory.Barbell, "Chest, triceps, shoulders", true, ScoreType.MaxLoad),
            ("Overhead press", ExerciseCategory.Barbell, "Shoulders, triceps", true, ScoreType.MaxLoad),
            ("Barbell row", ExerciseCategory.Barbell, "Back, biceps", true, ScoreType.MaxLoad),
            ("Clean", ExerciseCategory.Olympic, "Full body", true, ScoreType.MaxLoad),
            ("Clean and jerk", ExerciseCategory.Olympic, "Full body", true, ScoreType.MaxLoad),
            ("Snatch", ExerciseCategory.Olympic, "Full body", true, ScoreType.MaxLoad),
            ("Power clean", ExerciseCategory.Olympic, "Full body", true, ScoreType.MaxLoad),
            ("Thruster", ExerciseCategory.Barbell, "Full body", true, ScoreType.MaxLoad),
            ("Dumbbell press", ExerciseCategory.Dumbbell, "Chest, shoulders", false, null),
            ("Dumbbell row", ExerciseCategory.Dumbbell, "Back", false, null),
            ("Goblet squat", ExerciseCategory.Dumbbell, "Quads, glutes", false, null),
            ("Walking lunge", ExerciseCategory.Dumbbell, "Legs, glutes", false, null),
            ("Kettlebell swing", ExerciseCategory.Kettlebell, "Posterior chain", false, null),
            ("Turkish get-up", ExerciseCategory.Kettlebell, "Full body", false, null),
            ("Press-up", ExerciseCategory.Bodyweight, "Chest, triceps", true, ScoreType.Reps),
            ("Pull-up", ExerciseCategory.Gymnastics, "Back, biceps", true, ScoreType.Reps),
            ("Chin-up", ExerciseCategory.Gymnastics, "Back, biceps", true, ScoreType.Reps),
            ("Dip", ExerciseCategory.Gymnastics, "Chest, triceps", true, ScoreType.Reps),
            ("Muscle-up", ExerciseCategory.Gymnastics, "Full body", true, ScoreType.Reps),
            ("Handstand press-up", ExerciseCategory.Gymnastics, "Shoulders", true, ScoreType.Reps),
            ("Toes to bar", ExerciseCategory.Gymnastics, "Core", true, ScoreType.Reps),
            ("Burpee", ExerciseCategory.Bodyweight, "Full body", true, ScoreType.Reps),
            ("Air squat", ExerciseCategory.Bodyweight, "Legs", false, null),
            ("Plank", ExerciseCategory.Bodyweight, "Core", true, ScoreType.TimeUnderLoad),
            ("Sit-up", ExerciseCategory.Bodyweight, "Core", false, null),
            ("Box jump", ExerciseCategory.Plyometric, "Legs", false, null),
            ("Double-under", ExerciseCategory.Plyometric, "Calves, coordination", true, ScoreType.Reps),
            ("Wall ball", ExerciseCategory.Plyometric, "Full body", false, null),
            ("Leg press", ExerciseCategory.Machine, "Quads, glutes", true, ScoreType.MaxLoad),
            ("Lat pulldown", ExerciseCategory.Machine, "Back", false, null),
            ("Chest press machine", ExerciseCategory.Machine, "Chest", false, null),
            ("Leg curl", ExerciseCategory.Machine, "Hamstrings", false, null),
            ("Leg extension", ExerciseCategory.Machine, "Quads", false, null),
            ("Seated row", ExerciseCategory.Cable, "Back", false, null),
            ("Cable fly", ExerciseCategory.Cable, "Chest", false, null),
            ("Face pull", ExerciseCategory.Cable, "Rear delts, upper back", false, null),
            ("Tricep pushdown", ExerciseCategory.Cable, "Triceps", false, null),
            ("Row (2000m)", ExerciseCategory.Cardio, "Full body", true, ScoreType.ForTime),
            ("Row (500m)", ExerciseCategory.Cardio, "Full body", true, ScoreType.ForTime),
            ("Run (5km)", ExerciseCategory.Cardio, "Legs, cardiovascular", true, ScoreType.ForTime),
            ("Run (1 mile)", ExerciseCategory.Cardio, "Legs, cardiovascular", true, ScoreType.ForTime),
            ("Assault bike", ExerciseCategory.Cardio, "Full body", true, ScoreType.Calories),
            ("Ski erg", ExerciseCategory.Cardio, "Full body", true, ScoreType.Calories),
            ("Farmer's carry", ExerciseCategory.Other, "Grip, core", false, null),
            ("Sled push", ExerciseCategory.Other, "Legs, full body", false, null),
            ("Battle ropes", ExerciseCategory.Other, "Shoulders, conditioning", false, null),
            ("Band pull-apart", ExerciseCategory.Band, "Rear delts", false, null),
            ("Hip flexor stretch", ExerciseCategory.Mobility, "Hips", false, null),
            ("Thoracic rotation", ExerciseCategory.Mobility, "Upper back", false, null),
            ("Couch stretch", ExerciseCategory.Mobility, "Quads, hip flexors", false, null),
        };

        foreach (var (name, category, muscles, pr, score) in exercises)
        {
            db.Exercises.Add(new Exercise
            {
                Name = name,
                Category = category,
                MuscleGroups = muscles,
                TracksPersonalRecord = pr,
                PrScoreType = score,
                IsSystemExercise = true,
            }.StampNew(tenant));
        }
    }

    private void SeedAppointmentServices(FixedFitnessTenant tenant)
    {
        var services = new (string Name, AppointmentKind Kind, int Minutes, decimal Price, int Max, string Colour, string Note)[]
        {
            ("Gym induction", AppointmentKind.Induction, 45, 0m, 1, "#4CAF50",
                "Free, and booked automatically on joining — the single biggest predictor of a member still being here in six months."),
            ("Personal training", AppointmentKind.PersonalTraining, 60, 55m, 1, "#2196F3", ""),
            ("Personal training (30 min)", AppointmentKind.PersonalTraining, 30, 32m, 1, "#2196F3", ""),
            ("Semi-private training", AppointmentKind.SemiPrivate, 60, 35m, 2, "#03A9F4", "Price is per person."),
            ("Body composition assessment", AppointmentKind.Assessment, 30, 0m, 1, "#9C27B0", ""),
            ("Programme review", AppointmentKind.Consultation, 30, 0m, 1, "#673AB7", ""),
            ("Nutrition consultation", AppointmentKind.Nutrition, 45, 45m, 1, "#FF9800", ""),
            ("Sports massage", AppointmentKind.Massage, 60, 50m, 1, "#795548", ""),
        };

        var order = 1;
        foreach (var (name, kind, minutes, price, max, colour, note) in services)
        {
            db.Services.Add(new Fitness.Domain.Entities.AppointmentService
            {
                Name = name,
                Kind = kind,
                DurationMinutes = minutes,
                Price = price,
                MaxParticipants = max,
                ColourHex = colour,
                DisplayOrder = order++,
                FreeCancelHours = 24,
                LateCancelOutcome = PolicyOutcome.ForfeitCredit,
                NoShowOutcome = PolicyOutcome.ForfeitCredit,
                Description = string.IsNullOrEmpty(note) ? null : note,
            }.StampNew(tenant));
        }
    }

    // ═══ Sample club ═════════════════════════════════════════════════════════

    /// <summary>
    /// A price list that looks like a real one.
    ///
    /// Off-peak sits below standard, the annual plan is priced at ten months for twelve, and the
    /// session packs get cheaper per session as they get bigger. These are the shapes clubs
    /// actually sell, and they exercise every part of the billing engine — proration, minimum
    /// terms, credits with expiry, one-off passes.
    /// </summary>
    private List<MembershipPlan> SeedPlans(FixedFitnessTenant tenant, FitnessClub club)
    {
        var plans = new List<MembershipPlan>
        {
            new()
            {
                Name = "Off-Peak Monthly", Code = "OFFPEAK", Kind = PlanKind.RecurringMembership,
                Price = 24.99m, BillingPeriod = BillingPeriod.Monthly, JoiningFee = 20m,
                MinimumTermMonths = 0, NoticePeriodDays = 30, DisplayOrder = 1,
                ColourHex = "#8BC34A",
                MarketingBlurb = "Weekdays before 4pm and all weekend. The cheapest way in if your hours are flexible.",
                BookingWindowDays = 7,
            },
            new()
            {
                Name = "Standard Monthly", Code = "STANDARD", Kind = PlanKind.RecurringMembership,
                Price = 39.99m, BillingPeriod = BillingPeriod.Monthly, JoiningFee = 20m,
                MinimumTermMonths = 0, NoticePeriodDays = 30, DisplayOrder = 2,
                ColourHex = "#2196F3",
                MarketingBlurb = "Any time we are open, classes included, no commitment.",
                GuestPassesPerPeriod = 1,
                BookingWindowDays = 14,
            },
            new()
            {
                Name = "Premium Monthly", Code = "PREMIUM", Kind = PlanKind.RecurringMembership,
                Price = 59.99m, BillingPeriod = BillingPeriod.Monthly, JoiningFee = 0m,
                MinimumTermMonths = 0, NoticePeriodDays = 30, DisplayOrder = 3,
                ColourHex = "#9C27B0",
                MarketingBlurb = "Everything in Standard, plus a locker, towels, guest passes and priority booking.",
                GuestPassesPerPeriod = 4,
                BookingWindowDays = 21,
            },
            new()
            {
                Name = "Annual (paid up front)", Code = "ANNUAL", Kind = PlanKind.TermMembership,
                Price = 399.99m, BillingPeriod = BillingPeriod.Annual, JoiningFee = 0m,
                MinimumTermMonths = 12, DurationMonths = 12, AutoRenews = false,
                NoticePeriodDays = 0, DisplayOrder = 4, ColourHex = "#FF9800",
                MarketingBlurb = "Twelve months for the price of ten. Paid once, no monthly collection.",
                RecognitionBasis = RevenueRecognitionBasis.StraightLine,
                GuestPassesPerPeriod = 2,
                BookingWindowDays = 21,
            },
            new()
            {
                Name = "Student Monthly", Code = "STUDENT", Kind = PlanKind.RecurringMembership,
                Price = 27.99m, BillingPeriod = BillingPeriod.Monthly, JoiningFee = 0m,
                MinimumTermMonths = 0, NoticePeriodDays = 30, DisplayOrder = 5,
                ColourHex = "#00BCD4", RequiredProof = EligibilityProof.UploadedDocument,
                MarketingBlurb = "Full access at a student rate. Bring your student card.",
                MaximumAge = 26,
                BookingWindowDays = 14,
            },
            new()
            {
                Name = "Junior (14–17)", Code = "JUNIOR", Kind = PlanKind.RecurringMembership,
                Price = 19.99m, BillingPeriod = BillingPeriod.Monthly, JoiningFee = 0m,
                MinimumTermMonths = 0, NoticePeriodDays = 30, DisplayOrder = 6,
                ColourHex = "#CDDC39", MinimumAge = 14, MaximumAge = 17,
                MarketingBlurb = "Supervised gym access for under-18s. A guardian signs the agreement.",
                BookingWindowDays = 7,
            },
            new()
            {
                Name = "PT Pack — 5 sessions", Code = "PT5", Kind = PlanKind.SessionPack,
                Price = 260m, BillingPeriod = BillingPeriod.OneOff, CreditCount = 5,
                ValidForDays = 90, DisplayOrder = 7, ColourHex = "#3F51B5",
                RecognitionBasis = RevenueRecognitionBasis.OnConsumption,
                MarketingBlurb = "Five hours with a trainer, £52 a session. Valid for three months.",
                CreditsTransferable = false,
            },
            new()
            {
                Name = "PT Pack — 10 sessions", Code = "PT10", Kind = PlanKind.SessionPack,
                Price = 480m, BillingPeriod = BillingPeriod.OneOff, CreditCount = 10,
                ValidForDays = 180, DisplayOrder = 8, ColourHex = "#3F51B5",
                RecognitionBasis = RevenueRecognitionBasis.OnConsumption,
                MarketingBlurb = "Ten hours with a trainer, £48 a session. Valid for six months.",
                CreditsTransferable = false,
            },
            new()
            {
                Name = "Class Pack — 10 classes", Code = "CLASS10", Kind = PlanKind.SessionPack,
                Price = 89m, BillingPeriod = BillingPeriod.OneOff, CreditCount = 10,
                ValidForDays = 90, DisplayOrder = 9, ColourHex = "#E91E63",
                RecognitionBasis = RevenueRecognitionBasis.OnConsumption,
                MarketingBlurb = "Ten classes to use in three months. No membership needed.",
            },
            new()
            {
                Name = "Day Pass", Code = "DAY", Kind = PlanKind.TimePass,
                Price = 12m, BillingPeriod = BillingPeriod.OneOff, ValidForDays = 1,
                DisplayOrder = 10, ColourHex = "#607D8B",
                RecognitionBasis = RevenueRecognitionBasis.Immediate,
                MarketingBlurb = "One day, everything included.",
                SellableAtKiosk = true,
            },
            new()
            {
                Name = "Week Pass", Code = "WEEK", Kind = PlanKind.TimePass,
                Price = 35m, BillingPeriod = BillingPeriod.OneOff, ValidForDays = 7,
                DisplayOrder = 11, ColourHex = "#607D8B",
                RecognitionBasis = RevenueRecognitionBasis.Immediate,
                MarketingBlurb = "Seven days, everything included. Popular with visitors.",
            },
        };

        foreach (var plan in plans)
        {
            plan.CurrencyCode = club.CurrencyCode;
            plan.StampNew(tenant);
            db.Plans.Add(plan);
        }

        return plans;
    }

    /// <summary>Club pricing, entitlements and the off-peak time band.</summary>
    private void SeedPlanDetail(FixedFitnessTenant tenant, FitnessClub club, List<MembershipPlan> plans)
    {
        foreach (var plan in plans)
        {
            db.PlanPrices.Add(new PlanPrice
            {
                PlanId = plan.Id,
                ClubId = club.Id,
                Price = plan.Price,
                JoiningFee = plan.JoiningFee,
                CurrencyCode = club.CurrencyCode,
                IsAvailable = true,
            }.StampNew(tenant));
        }

        var memberships = plans.Where(p => p.Kind is PlanKind.RecurringMembership or PlanKind.TermMembership).ToList();

        foreach (var plan in memberships)
        {
            var access = new PlanEntitlement
            {
                PlanId = plan.Id,
                Kind = EntitlementKind.ClubAccess,
                TargetId = club.Id,
                TargetName = club.Name,
                Limit = EntitlementLimit.Unlimited,
            }.StampNew(tenant);

            db.PlanEntitlements.Add(access);

            // Off-peak is a time band on the access entitlement, not a separate plan family — so a
            // member who upgrades keeps their history and only the band changes.
            if (plan.Code == "OFFPEAK")
            {
                db.AccessTimeBands.Add(new AccessTimeBand
                {
                    EntitlementId = access.Id,
                    Name = "Weekdays before 4pm",
                    DaysOfWeekMask = 0b0111110,
                    StartsAt = new TimeSpan(6, 0, 0),
                    EndsAt = new TimeSpan(16, 0, 0),
                }.StampNew(tenant));

                db.AccessTimeBands.Add(new AccessTimeBand
                {
                    EntitlementId = access.Id,
                    Name = "All weekend",
                    DaysOfWeekMask = 0b1000001,
                    StartsAt = new TimeSpan(0, 0, 0),
                    EndsAt = new TimeSpan(23, 59, 59),
                }.StampNew(tenant));
            }

            db.PlanEntitlements.Add(new PlanEntitlement
            {
                PlanId = plan.Id,
                Kind = EntitlementKind.ClassBooking,
                Limit = plan.Code == "OFFPEAK" ? EntitlementLimit.PerWeek : EntitlementLimit.Unlimited,
                Quantity = plan.Code == "OFFPEAK" ? 3 : 0,
                AllowOverage = plan.Code == "OFFPEAK",
                OverageFee = plan.Code == "OFFPEAK" ? 6m : 0m,
            }.StampNew(tenant));

            if (plan.GuestPassesPerPeriod > 0)
            {
                db.PlanEntitlements.Add(new PlanEntitlement
                {
                    PlanId = plan.Id,
                    Kind = EntitlementKind.GuestPass,
                    Limit = EntitlementLimit.PerMonth,
                    Quantity = plan.GuestPassesPerPeriod,
                }.StampNew(tenant));
            }

            if (plan.Code == "PREMIUM")
            {
                db.PlanEntitlements.Add(new PlanEntitlement
                {
                    PlanId = plan.Id,
                    Kind = EntitlementKind.Locker,
                    Limit = EntitlementLimit.PerAgreement,
                    Quantity = 1,
                }.StampNew(tenant));

                db.PlanEntitlements.Add(new PlanEntitlement
                {
                    PlanId = plan.Id,
                    Kind = EntitlementKind.TowelService,
                    Limit = EntitlementLimit.Unlimited,
                }.StampNew(tenant));
            }
        }

        // Upgrades take effect at once and are prorated; downgrades wait for the next period, so
        // nobody drops a tier mid-month and expects money back.
        var standard = plans.First(p => p.Code == "STANDARD");
        var premium = plans.First(p => p.Code == "PREMIUM");
        var offPeak = plans.First(p => p.Code == "OFFPEAK");

        var paths = new (MembershipPlan From, MembershipPlan To, bool Immediate)[]
        {
            (offPeak, standard, true),
            (standard, premium, true),
            (offPeak, premium, true),
            (premium, standard, false),
            (standard, offPeak, false),
            (premium, offPeak, false),
        };

        foreach (var (from, to, immediate) in paths)
        {
            db.PlanChangePaths.Add(new PlanChangePath
            {
                FromPlanId = from.Id,
                ToPlanId = to.Id,
                EffectiveImmediately = immediate,
                Proration = immediate ? ProrationRule.Daily : ProrationRule.None,
                Description = immediate
                    ? "Upgrade takes effect today, charged for the rest of this period."
                    : "Downgrade takes effect at the start of the next billing period.",
            }.StampNew(tenant));
        }
    }

    private void SeedPromotion(FixedFitnessTenant tenant, FitnessClub club, List<MembershipPlan> plans)
    {
        var rule = new PromotionRule
        {
            Name = "New year — joining fee waived",
            ClubId = club.Id,
            DiscountKind = DiscountKind.WaiveJoiningFee,
            Value = 0,
            NewMembersOnly = true,
            MaxRedemptions = 200,
            DisplayOrder = 1,
            Description = "The offer every club runs in January. Waives the joining fee, leaves the "
                        + "monthly price alone — so the discount does not follow the member for ever.",
        }.StampNew(tenant);

        db.Promotions.Add(rule);

        db.PromoCodes.Add(new PromoCode
        {
            PromotionRuleId = rule.Id,
            CodeText = "NEWYEAR",
            MaxUses = 200,
            OnePerMember = true,
        }.StampNew(tenant));

        var standard = plans.First(p => p.Code == "STANDARD");

        var trial = new PromotionRule
        {
            Name = "Two months half price",
            ClubId = club.Id,
            PlanId = standard.Id,
            DiscountKind = DiscountKind.Percentage,
            Value = 50m,
            PeriodCount = 2,
            NewMembersOnly = true,
            DisplayOrder = 2,
            Description = "Half price for two months, then the standard rate — the promotional price and "
                        + "the number of periods it lasts are both frozen onto the agreement at signing.",
        }.StampNew(tenant);

        db.Promotions.Add(trial);

        db.PromoCodes.Add(new PromoCode
        {
            PromotionRuleId = trial.Id,
            CodeText = "HALF2",
            MaxUses = 100,
            OnePerMember = true,
        }.StampNew(tenant));
    }

    private List<ClassType> SeedClassTypes(FixedFitnessTenant tenant, FitnessClub club)
    {
        var types = new List<ClassType>
        {
            new() { Name = "Spin", DefaultDurationMinutes = 45, DefaultCapacity = 24, Intensity = 4, ColourHex = "#E91E63", DropInPrice = 12m, EquipmentNeeded = "Bike (allocated), towel, water", DisplayOrder = 1, MarketingBlurb = "Indoor cycling to music. Pick a bike, hold a gear, chase the numbers." },
            new() { Name = "Body Pump", DefaultDurationMinutes = 45, DefaultCapacity = 20, Intensity = 3, ColourHex = "#F44336", DropInPrice = 12m, EquipmentNeeded = "Barbell, plates, step, mat", DisplayOrder = 2, MarketingBlurb = "High-rep barbell work through every major muscle group." },
            new() { Name = "HIIT", DefaultDurationMinutes = 30, DefaultCapacity = 16, Intensity = 5, ColourHex = "#FF5722", DropInPrice = 10m, DisplayOrder = 3, MarketingBlurb = "Thirty minutes, short intervals, nowhere to hide." },
            new() { Name = "Yoga", DefaultDurationMinutes = 60, DefaultCapacity = 20, Intensity = 2, ColourHex = "#9C27B0", DropInPrice = 12m, EquipmentNeeded = "Mat (provided), blocks", DisplayOrder = 4, MarketingBlurb = "Vinyasa flow. Breath, movement, and a proper wind-down at the end." },
            new() { Name = "Pilates", DefaultDurationMinutes = 45, DefaultCapacity = 16, Intensity = 2, ColourHex = "#673AB7", DropInPrice = 12m, EquipmentNeeded = "Mat (provided)", DisplayOrder = 5, MarketingBlurb = "Core, control and posture. Small movements, big difference." },
            new() { Name = "Boxercise", DefaultDurationMinutes = 45, DefaultCapacity = 16, Intensity = 4, ColourHex = "#FF9800", DropInPrice = 12m, EquipmentNeeded = "Gloves and pads (provided)", DisplayOrder = 6 },
            new() { Name = "Circuits", DefaultDurationMinutes = 45, DefaultCapacity = 20, Intensity = 4, ColourHex = "#4CAF50", DropInPrice = 10m, DisplayOrder = 7 },
            new() { Name = "Legs, Bums & Tums", DefaultDurationMinutes = 45, DefaultCapacity = 20, Intensity = 3, ColourHex = "#00BCD4", DropInPrice = 10m, DisplayOrder = 8 },
            new() { Name = "Strength Foundations", DefaultDurationMinutes = 60, DefaultCapacity = 12, Intensity = 3, ColourHex = "#3F51B5", DropInPrice = 15m, DisplayOrder = 9, MarketingBlurb = "Coached barbell technique in a small group. Start here if the free weights area feels intimidating." },
            new() { Name = "Aqua Fit", DefaultDurationMinutes = 45, DefaultCapacity = 20, Intensity = 2, ColourHex = "#03A9F4", DropInPrice = 10m, DisplayOrder = 10 },
            new() { Name = "Seniors Mobility", DefaultDurationMinutes = 45, DefaultCapacity = 15, Intensity = 1, ColourHex = "#8BC34A", DropInPrice = 8m, MinimumAge = 60, DisplayOrder = 11, MarketingBlurb = "Gentle, seated and standing work for strength, balance and confidence." },
            new() { Name = "Stretch & Recover", DefaultDurationMinutes = 30, DefaultCapacity = 20, Intensity = 1, ColourHex = "#009688", DropInPrice = 8m, DisplayOrder = 12 },
        };

        foreach (var type in types)
        {
            type.StampNew(tenant);
            db.ClassTypes.Add(type);
        }

        return types;
    }

    private List<FitnessStaff> SeedStaff(FixedFitnessTenant tenant, FitnessClub club)
    {
        // PINs are hashed exactly the same way a real one would be. The demo values are printed in
        // the seed notes, never stored in the clear.
        string Hash(string pin) => BCrypt.Net.BCrypt.HashPassword(pin);

        var staff = new List<FitnessStaff>
        {
            new()
            {
                FirstName = "Sarah", LastName = "Mitchell", DisplayName = "Sarah",
                RoleKind = StaffRoleKind.Manager, PinHash = Hash("1234"),
                CanSell = true, CanTrain = true, CanTeach = true, CanApproveOverrides = true,
                Email = "sarah@example.com",
            },
            new()
            {
                FirstName = "James", LastName = "Okafor", DisplayName = "James",
                RoleKind = StaffRoleKind.PersonalTrainer, PinHash = Hash("2345"),
                CanTrain = true, CanTeach = true, IsBookable = true,
                Email = "james@example.com",
            },
            new()
            {
                FirstName = "Priya", LastName = "Sharma", DisplayName = "Priya",
                RoleKind = StaffRoleKind.PersonalTrainer, PinHash = Hash("3456"),
                CanTrain = true, CanTeach = true, IsBookable = true,
                Email = "priya@example.com",
            },
            new()
            {
                FirstName = "Tom", LastName = "Bergstrom", DisplayName = "Tom",
                RoleKind = StaffRoleKind.GroupInstructor, PinHash = Hash("4567"),
                CanTeach = true, IsContractor = true, IsBookable = true,
                Email = "tom@example.com",
            },
            new()
            {
                FirstName = "Aisha", LastName = "Rahman", DisplayName = "Aisha",
                RoleKind = StaffRoleKind.GroupInstructor, PinHash = Hash("5678"),
                CanTeach = true, IsContractor = true,
                Email = "aisha@example.com",
            },
            new()
            {
                FirstName = "Danny", LastName = "Fitzgerald", DisplayName = "Danny",
                RoleKind = StaffRoleKind.SalesConsultant, PinHash = Hash("6789"),
                CanSell = true,
                Email = "danny@example.com",
            },
            new()
            {
                FirstName = "Chloe", LastName = "Nguyen", DisplayName = "Chloe",
                RoleKind = StaffRoleKind.Receptionist, PinHash = Hash("7890"),
                CanSell = true,
                Email = "chloe@example.com",
            },
        };

        var startedOn = DateTime.UtcNow.AddMonths(-18);

        foreach (var person in staff)
        {
            person.ClubId = club.Id;
            person.StartedOn = startedOn;
            person.StampNew(tenant);
            db.Staff.Add(person);
            startedOn = startedOn.AddMonths(2);
        }

        return staff;
    }

    /// <summary>
    /// A week's timetable that looks like one a club would actually run.
    ///
    /// Peak slots (06:30, 12:15, 17:30, 18:30) are busy; midday is quiet and cheap to staff; the
    /// weekend is late and light. Schedules are published, so the occurrence generator has
    /// something to work with the moment the app opens.
    /// </summary>
    private void SeedTimetable(
        FixedFitnessTenant tenant, FitnessClub club,
        List<ClassType> classTypes, List<Room> rooms, List<FitnessStaff> staff)
    {
        var studio1 = rooms.FirstOrDefault(r => r.Name == "Studio 1");
        var studio2 = rooms.FirstOrDefault(r => r.Name == "Studio 2");
        var rig = rooms.FirstOrDefault(r => r.Name == "Rig");

        var instructors = staff.Where(s => s.CanTeach).ToList();
        ClassType Type(string name) => classTypes.First(t => t.Name == name);

        const int weekdays = 0b0111110;
        const int monWedFri = 0b0101010;
        const int tueThu = 0b0010100;
        const int saturday = 0b1000000;
        const int sunday = 0b0000001;

        var slots = new (string Type, int Mask, int Hour, int Minute, Room? Room, int Instructor)[]
        {
            // Early peak — the people who train before work are the most loyal cohort a gym has.
            ("Spin", monWedFri, 6, 30, studio1, 0),
            ("HIIT", tueThu, 6, 30, studio2, 1),
            ("Circuits", weekdays, 7, 30, rig, 2),

            // Midday — quieter, and where the seniors and shift-workers go.
            ("Seniors Mobility", monWedFri, 10, 30, studio2, 3),
            ("Pilates", tueThu, 10, 30, studio2, 1),
            ("HIIT", weekdays, 12, 15, rig, 2),

            // Evening peak — the busiest hours of the week.
            ("Body Pump", monWedFri, 17, 30, studio1, 0),
            ("Boxercise", tueThu, 17, 30, studio2, 3),
            ("Spin", weekdays, 18, 30, studio1, 0),
            ("Yoga", monWedFri, 19, 30, studio2, 1),
            ("Strength Foundations", tueThu, 19, 30, rig, 2),
            ("Legs, Bums & Tums", tueThu, 18, 30, studio2, 3),

            // Weekend — later starts, longer classes, a proper wind-down.
            ("Spin", saturday, 9, 0, studio1, 0),
            ("Body Pump", saturday, 10, 0, studio1, 2),
            ("Yoga", saturday, 11, 0, studio2, 1),
            ("Circuits", sunday, 10, 0, rig, 3),
            ("Stretch & Recover", sunday, 11, 30, studio2, 1),
        };

        var from = DateTime.UtcNow.Date.AddDays(-90);

        foreach (var (typeName, mask, hour, minute, room, instructorIndex) in slots)
        {
            var type = Type(typeName);
            var instructor = instructors.Count == 0 ? null : instructors[instructorIndex % instructors.Count];

            db.ClassSchedules.Add(new ClassSchedule
            {
                ClubId = club.Id,
                ClassTypeId = type.Id,
                RoomId = room?.Id,
                InstructorStaffId = instructor?.Id,
                DaysOfWeekMask = mask,
                StartsAt = new TimeSpan(hour, minute, 0),
                DurationMinutes = type.DefaultDurationMinutes,
                Capacity = room is null ? type.DefaultCapacity : Math.Min(type.DefaultCapacity, room.Capacity),
                EffectiveFrom = from,
                GenerateAheadDays = 60,
                IsPublished = true,
            }.StampNew(tenant));
        }
    }

    private void SeedBookableStaff(FixedFitnessTenant tenant, FitnessClub club, List<FitnessStaff> staff)
    {
        var trainers = staff.Where(s => s.IsBookable).ToList();

        var profiles = new (string Name, string Specialities, decimal Rate, string Bio)[]
        {
            ("James", "Strength, powerlifting, return from injury", 55m,
                "Fifteen years coaching barbell lifts. Happy working around old injuries — tell him what hurts and he will build around it."),
            ("Priya", "Weight loss, pre and post-natal, nutrition", 55m,
                "Works with people who have not trained in years, or ever. Patient, and very good at making a first session not feel like an ordeal."),
            ("Tom", "Conditioning, endurance, class technique", 50m,
                "Runs the conditioning classes and coaches endurance athletes. Ask him about pacing."),
        };

        foreach (var trainer in trainers)
        {
            var profile = profiles.FirstOrDefault(p => p.Name == trainer.DisplayName);

            var bookable = new BookableStaff
            {
                StaffId = trainer.Id,
                ClubId = club.Id,
                DisplayName = trainer.DisplayName ?? trainer.FirstName,
                Specialities = profile.Specialities,
                Bio = profile.Bio,
                HourlyRate = profile.Rate == 0 ? 50m : profile.Rate,
                BookableOnline = true,
                DefaultBufferMinutes = 15,
                BookingWindowDays = 30,
                IsContractor = trainer.IsContractor,
                MaxClientsPerDay = 8,
                AcceptingNewClients = true,
            }.StampNew(tenant);

            db.BookableStaff.Add(bookable);

            // Weekdays, with a proper break in the middle — a trainer's diary that runs 6am to 8pm
            // with no gap is a trainer who leaves in six months.
            for (var day = 1; day <= 5; day++)
            {
                db.Availability.Add(new StaffAvailability
                {
                    BookableStaffId = bookable.Id,
                    ClubId = club.Id,
                    DayOfWeek = day,
                    StartsAt = new TimeSpan(6, 30, 0),
                    EndsAt = new TimeSpan(20, 0, 0),
                    BreakStartsAt = new TimeSpan(13, 0, 0),
                    BreakEndsAt = new TimeSpan(15, 0, 0),
                }.StampNew(tenant));
            }

            db.Availability.Add(new StaffAvailability
            {
                BookableStaffId = bookable.Id,
                ClubId = club.Id,
                DayOfWeek = 6,
                StartsAt = new TimeSpan(8, 0, 0),
                EndsAt = new TimeSpan(13, 0, 0),
            }.StampNew(tenant));
        }
    }

    private void SeedLockers(FixedFitnessTenant tenant, FitnessClub club, List<ClubArea> areas)
    {
        var changing = areas.FirstOrDefault(a => a.Kind == AreaKind.ChangingRoom);

        var banks = new (string Name, string Location, int Count, LockerSize Size, decimal Monthly)[]
        {
            ("Bank A — Ladies", "Ladies changing room", 40, LockerSize.Medium, 8m),
            ("Bank B — Gents", "Gents changing room", 40, LockerSize.Medium, 8m),
            ("Bank C — Full height", "Corridor by reception", 20, LockerSize.FullHeight, 15m),
        };

        foreach (var (name, location, count, size, monthly) in banks)
        {
            var bank = new LockerBank
            {
                ClubId = club.Id,
                AreaId = changing?.Id,
                Name = name,
                Location = location,
                TotalLockers = count,
                SupportsRental = true,
                SupportsDayUse = true,
            }.StampNew(tenant);

            db.LockerBanks.Add(bank);

            var prefix = name.Contains("Ladies") ? "L" : name.Contains("Gents") ? "G" : "F";

            for (var number = 1; number <= count; number++)
            {
                db.Lockers.Add(new Locker
                {
                    LockerBankId = bank.Id,
                    ClubId = club.Id,
                    Number = $"{prefix}{number:000}",
                    Size = size,
                    Status = LockerStatus.Free,
                    LockType = "Coin return",
                    MonthlyRate = monthly,
                    AnnualRate = monthly * 10,
                    Deposit = 10m,
                }.StampNew(tenant));
            }
        }
    }

    /// <summary>
    /// Commission that pays for the behaviour the club wants.
    ///
    /// Trainers are paid per session **delivered**, not per session sold — which is the difference
    /// between a trainer who sells a ten-pack and disappears, and one who makes sure the member
    /// uses all ten. Sales commission accelerates past target rather than paying flat.
    /// </summary>
    private void SeedCommissionRules(FixedFitnessTenant tenant, FitnessClub club)
    {
        var rules = new List<CommissionRule>
        {
            new()
            {
                Name = "PT session delivered",
                ClubId = club.Id,
                AppliesToRole = StaffRoleKind.PersonalTrainer,
                Basis = CommissionBasis.PercentOfSessionValue,
                Percentage = 60m,
                Priority = 1,
                Description = "Paid when the session is signed off, not when the pack is sold — so the "
                            + "incentive is to get the member to turn up.",
            },
            new()
            {
                Name = "Class taught",
                ClubId = club.Id,
                AppliesToRole = StaffRoleKind.GroupInstructor,
                Basis = CommissionBasis.PerClassTaught,
                RatePerUnit = 28m,
                Priority = 2,
            },
            new()
            {
                Name = "Class head bonus",
                ClubId = club.Id,
                AppliesToRole = StaffRoleKind.GroupInstructor,
                Basis = CommissionBasis.PerClassHead,
                RatePerUnit = 0.75m,
                Threshold = 12,
                Priority = 3,
                Description = "Seventy-five pence a head above twelve. Rewards filling a room, not just "
                            + "showing up to teach an empty one.",
            },
            new()
            {
                Name = "Membership sold",
                ClubId = club.Id,
                AppliesToRole = StaffRoleKind.SalesConsultant,
                Basis = CommissionBasis.PercentOfMembershipSold,
                Percentage = 25m,
                Threshold = 15,
                AcceleratedRate = 40m,
                Priority = 4,
                Description = "A quarter of the first month, rising to 40% once fifteen joiners are on "
                            + "the board for the period.",
            },
            new()
            {
                Name = "Package sold",
                ClubId = club.Id,
                Basis = CommissionBasis.PercentOfPackageSold,
                Percentage = 10m,
                Priority = 5,
            },
            new()
            {
                Name = "Pro shop",
                ClubId = club.Id,
                Basis = CommissionBasis.PercentOfRetailSold,
                Percentage = 5m,
                Priority = 6,
            },
        };

        foreach (var rule in rules)
        {
            rule.EffectiveFrom = DateTime.UtcNow.AddMonths(-12);
            rule.StampNew(tenant);
            db.CommissionRules.Add(rule);
        }
    }

    /// <summary>
    /// A small pro shop.
    ///
    /// Products are plans of kind <see cref="PlanKind.RetailProduct"/> with no Inventory item
    /// attached, so the demo till works without Inventory being installed. A club that runs
    /// Inventory links the item and stock starts depleting.
    /// </summary>
    private void SeedRetailProducts(FixedFitnessTenant tenant, FitnessClub club)
    {
        var products = new (string Name, string Barcode, decimal Price)[]
        {
            ("Bottled water 500ml", "5000000000017", 1.50m),
            ("Sports drink", "5000000000024", 2.50m),
            ("Protein shake (ready to drink)", "5000000000031", 3.50m),
            ("Protein bar", "5000000000048", 2.75m),
            ("Energy gel", "5000000000055", 1.95m),
            ("Shaker bottle", "5000000000062", 6.00m),
            ("Gym towel", "5000000000079", 8.00m),
            ("Resistance band set", "5000000000086", 14.00m),
            ("Lifting straps", "5000000000093", 12.00m),
            ("Padlock", "5000000000109", 5.00m),
            ("Swimming cap", "5000000000116", 4.50m),
            ("Branded t-shirt", "5000000000123", 18.00m),
        };

        var order = 1;
        foreach (var (name, barcode, price) in products)
        {
            db.Plans.Add(new MembershipPlan
            {
                Name = name,
                Code = barcode,
                Kind = PlanKind.RetailProduct,
                Price = price,
                CurrencyCode = club.CurrencyCode,
                BillingPeriod = BillingPeriod.OneOff,
                RecognitionBasis = RevenueRecognitionBasis.Immediate,
                SellableAtDesk = true,
                SellableOnline = false,
                SellableInApp = false,
                DisplayOrder = order++,
            }.StampNew(tenant));
        }
    }
}
