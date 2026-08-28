using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Stands a company's Real Estate app up as something that works on first open.
///
/// Two tiers, and the distinction matters.
///
/// **Essential** is the configuration without which the app cannot function at all. You cannot
/// cancel a booking with no reason codes, take a discount with no approval matrix, chase a
/// defaulter with no dunning ladder, or send a demand with no notification rules. Every company
/// gets these, always. An app that greets a new customer with a blank screen and a silent save
/// button is broken, not minimal.
///
/// **Sample** is a demonstration project — a tower with blocks, floors and units, a price list, a
/// payment plan, a milestone schedule. Only companies that asked for sample data get it, because a
/// real developer wants to type in their own scheme, not delete somebody else's.
///
/// Everything is idempotent and gated per tier, so a company that skipped sample data at sign-up
/// can ask for it later, and one that already has an office is never given a second one.
/// </summary>
public class RealEstateInitializationService(
    RealEstateDbContext db,
    ILogger<RealEstateInitializationService> logger)
{
    /// <summary>Called by the CompanyCreated handler. Provisions essentials, and samples if asked.</summary>
    public Task<bool> InitializeForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
        => EnsureProvisionedAsync(companyId, branchId, businessUnitId, userId, includeSampleData);

    /// <summary>
    /// Brings a company's Real Estate app up to a working state, whenever it is called.
    ///
    /// This exists because installing an app is not the same event as creating a company: a
    /// business that has been running NexCore for a year and adds Real Estate today never saw
    /// <c>CompanyCreatedEvent</c>, and would otherwise land on a property list with no office
    /// behind it, where every save silently does nothing.
    /// </summary>
    public async Task<bool> EnsureProvisionedAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
    {
        var tenant = new FixedRealEstateTenant(companyId, branchId, businessUnitId, userId);

        var office = await db.RealEstateOffices
            .FirstOrDefaultAsync(o => o.CompanyId == companyId && !o.IsDeleted);

        var alreadyProvisioned = office is not null;

        var wantsSample = includeSampleData
            && !await db.Projects.AnyAsync(p => p.CompanyId == companyId && !p.IsDeleted);

        if (alreadyProvisioned && !wantsSample) return false;

        if (!alreadyProvisioned)
        {
            await EnsureSettingsAsync(tenant, userId);
            office = await EnsureOfficeAsync(tenant, userId);
            await EnsureReasonCodesAsync(tenant, userId);
            await EnsureApprovalMatrixAsync(tenant, userId);
            await EnsureNotificationRulesAsync(tenant, userId);
            await EnsureSurchargeAndDunningAsync(tenant, userId);
            await EnsureDeductionPolicyAsync(tenant, userId);

            await db.SaveChangesAsync();

            logger.LogInformation("Real Estate essentials provisioned for company {CompanyId}", companyId);
        }

        if (wantsSample)
        {
            await SeedSampleProjectAsync(tenant, office!, userId);
            await db.SaveChangesAsync();

            logger.LogInformation("Real Estate sample project seeded for company {CompanyId}", companyId);
        }

        return true;
    }

    // ═══ Essentials ══════════════════════════════════════════════════════════

    private async Task EnsureSettingsAsync(FixedRealEstateTenant tenant, Guid userId)
    {
        var exists = await db.Settings.AnyAsync(s => s.CompanyId == tenant.CompanyId && !s.IsDeleted);
        if (exists) return;

        // Development is on and the others are off, because that is the shape most companies
        // installing this app have. Everything else is reachable from one settings screen.
        db.Settings.Add(new RealEstateSettings
        {
            DevelopmentEnabled = true,
            BrokerageEnabled = false,
            ContractingEnabled = false,
            EstateManagementEnabled = false,
        }.StampNew(tenant, userId));
    }

    private async Task<RealEstateOffice> EnsureOfficeAsync(FixedRealEstateTenant tenant, Guid userId)
    {
        var office = new RealEstateOffice
        {
            Name = "Head Office",
            Code = "HO",
            OfficeType = OfficeType.HeadOffice,
        }.StampNew(tenant, userId);

        db.RealEstateOffices.Add(office);

        await Task.CompletedTask;
        return office;
    }

    /// <summary>
    /// Reason codes are not decoration. Half the controls in this application refuse to proceed
    /// without one — a discount override, a dues waiver, a cancellation, a receipt reversal — so a
    /// company with an empty list would find those screens simply would not save.
    /// </summary>
    private async Task EnsureReasonCodesAsync(FixedRealEstateTenant tenant, Guid userId)
    {
        if (await db.ReasonCodes.AnyAsync(r => r.CompanyId == tenant.CompanyId && !r.IsDeleted)) return;

        var codes = new (string Context, string Code, string Label, bool RequiresNote)[]
        {
            ("Cancellation", "CUST-WITHDRAW", "Customer withdrew", true),
            ("Cancellation", "DEFAULT", "Payment default", false),
            ("Cancellation", "FINANCE-DECLINED", "Mortgage declined", true),
            ("Cancellation", "RELOCATION", "Customer relocating", false),
            ("Cancellation", "MUTUAL", "Mutual agreement", true),

            ("Discount", "NEGOTIATED", "Negotiated at closing", true),
            ("Discount", "LAUNCH-OFFER", "Launch offer", false),
            ("Discount", "BULK", "Bulk purchase", false),
            ("Discount", "LOYALTY", "Repeat customer", false),

            ("Hold", "AWAITING-FUNDS", "Customer arranging funds", false),
            ("Hold", "AWAITING-FAMILY", "Awaiting family decision", false),
            ("Hold", "DOCUMENT-PENDING", "Documents pending", false),

            ("Waiver", "GOODWILL", "Goodwill", true),
            ("Waiver", "OUR-DELAY", "Delay on our side", true),
            ("Waiver", "HARDSHIP", "Genuine hardship", true),

            ("Reversal", "DATA-ENTRY", "Entered in error", true),
            ("Reversal", "INSTRUMENT-FAILED", "Instrument dishonoured", false),
            ("Reversal", "DUPLICATE", "Duplicate entry", false),

            ("LeadLoss", "PRICE", "Price too high", false),
            ("LeadLoss", "LOCATION", "Location unsuitable", false),
            ("LeadLoss", "COMPETITOR", "Bought elsewhere", true),
            ("LeadLoss", "TIMING", "Not buying yet", false),
            ("LeadLoss", "UNRESPONSIVE", "Could not be reached", false),

            ("ViewingCancel", "CUSTOMER", "Customer cancelled", false),
            ("ViewingCancel", "VENDOR", "Vendor cancelled", false),
            ("ViewingCancel", "WEATHER", "Weather", false),
            ("ViewingCancel", "ACCESS", "No access", true),

            ("Transfer", "FAMILY", "Family transfer", false),
            ("Transfer", "SALE", "Sold on", false),
            ("Transfer", "INHERITANCE", "Inheritance", true),

            ("Void", "REFURBISHMENT", "Refurbishment", false),
            ("Void", "MARKET", "No demand at this rent", false),
            ("Void", "DISPUTE", "Dispute with previous tenant", true),

            ("WorkOrderCancel", "DUPLICATE", "Duplicate request", false),
            ("WorkOrderCancel", "NO-FAULT", "No fault found", true),
            ("WorkOrderCancel", "TENANT-RESOLVED", "Resolved by occupier", false),
        };

        var order = 0;

        foreach (var (context, code, label, requiresNote) in codes)
        {
            db.ReasonCodes.Add(new ReasonCode
            {
                Context = context,
                Code = code,
                Label = label,
                RequiresNote = requiresNote,
                SortOrder = order++,
            }.StampNew(tenant, userId));
        }
    }

    /// <summary>
    /// The approval bands. Deliberately generous at the bottom — an app that stops a sales
    /// executive to approve a two per cent discount teaches everybody to route round it.
    /// </summary>
    private async Task EnsureApprovalMatrixAsync(FixedRealEstateTenant tenant, Guid userId)
    {
        if (await db.ApprovalMatrices.AnyAsync(m => m.CompanyId == tenant.CompanyId && !m.IsDeleted)) return;

        var bands = new (string DocumentType, int Level, decimal Min, decimal? Max, int? Escalate)[]
        {
            ("Discount", 1, 0m, 100_000m, 24),
            ("Discount", 2, 100_000m, null, 24),

            ("Cancellation", 1, 0m, null, 48),
            ("RefundRequest", 1, 0m, 500_000m, 24),
            ("RefundRequest", 2, 500_000m, null, 24),

            ("SurchargeWaiver", 1, 0m, 50_000m, 24),
            ("SurchargeWaiver", 2, 50_000m, null, 24),

            ("EscrowWithdrawal", 1, 0m, null, 12),
            ("Distribution", 1, 0m, null, 24),

            ("TransferDuesOverride", 1, 0m, null, 12),
            ("WriteOff", 1, 0m, null, 48),

            ("InterimPaymentCertificate", 1, 0m, 2_000_000m, 24),
            ("InterimPaymentCertificate", 2, 2_000_000m, null, 24),

            ("VariationOrder", 1, 0m, 500_000m, 24),
            ("VariationOrder", 2, 500_000m, null, 48),

            ("WorkOrder", 1, 0m, 25_000m, 8),
            ("WorkOrder", 2, 25_000m, null, 24),

            ("BroadcastRun", 1, 500m, null, 12),
        };

        foreach (var (documentType, level, min, max, escalate) in bands)
        {
            db.ApprovalMatrices.Add(new ApprovalMatrix
            {
                DocumentType = documentType,
                Level = level,
                MinAmount = min,
                MaxAmount = max,
                EscalateAfterHours = escalate,
                AutoApproveBelowMin = level == 1,
            }.StampNew(tenant, userId));
        }
    }

    private async Task EnsureNotificationRulesAsync(FixedRealEstateTenant tenant, Guid userId)
    {
        if (await db.NotificationRules.AnyAsync(r => r.CompanyId == tenant.CompanyId && !r.IsDeleted)) return;

        var rules = new (string Key, string Name, AlertSeverity Severity, string Channels, int? LeadDays, bool Quiet)[]
        {
            ("demand.issued", "Demand issued", AlertSeverity.Info, "InApp,Email,WhatsApp", null, true),
            ("demand.due", "Instalment falling due", AlertSeverity.Warning, "InApp,Sms,WhatsApp", 7, true),
            ("demand.overdue", "Instalment overdue", AlertSeverity.Warning, "InApp,Sms,WhatsApp", null, true),
            ("receipt.posted", "Receipt issued", AlertSeverity.Info, "InApp,Email,WhatsApp", null, true),
            ("cheque.bounced", "Cheque bounced", AlertSeverity.Critical, "InApp,Sms", null, false),

            ("lead.assigned", "Lead assigned to you", AlertSeverity.Info, "InApp,Push", null, false),
            ("lead.sla", "Lead response overdue", AlertSeverity.Critical, "InApp,Push", null, false),
            ("viewing.reminder", "Viewing tomorrow", AlertSeverity.Info, "InApp,Sms,WhatsApp", 1, true),
            ("sitevisit.reminder", "Site visit tomorrow", AlertSeverity.Info, "InApp,WhatsApp", 1, true),

            ("hold.expiring", "Unit hold expiring", AlertSeverity.Warning, "InApp,Push", null, false),
            ("booking.confirmed", "Booking confirmed", AlertSeverity.Info, "InApp,Email,WhatsApp", null, true),
            ("possession.offered", "Possession offered", AlertSeverity.Info, "InApp,Email,Sms", null, true),

            ("approval.pending", "Approval waiting on you", AlertSeverity.Warning, "InApp,Push", null, false),
            ("approval.escalated", "Approval escalated", AlertSeverity.Warning, "InApp,Email", null, false),
            ("approval.granted", "Statutory approval granted", AlertSeverity.Info, "InApp", null, true),
            ("approval.expiring", "Statutory approval expiring", AlertSeverity.Critical, "InApp,Email", 60, true),

            ("licence.expiring", "Licence renewal due", AlertSeverity.Critical, "InApp,Email", 60, true),
            ("guarantee.expiring", "Bank guarantee expiring", AlertSeverity.Critical, "InApp,Email", 45, true),
            ("compliance.due", "Compliance obligation due", AlertSeverity.Warning, "InApp,Email", 30, true),
            ("certificate.expiring", "Safety certificate expiring", AlertSeverity.Critical, "InApp,Email", 45, true),

            ("escrow.withdrawal.requested", "Escrow withdrawal requested", AlertSeverity.Warning, "InApp,Email", null, false),
            ("escrow.withdrawal.released", "Escrow withdrawal released", AlertSeverity.Info, "InApp", null, true),
            ("jv.accrual.raised", "Landowner share accrued", AlertSeverity.Info, "InApp", null, true),
            ("investor.capital.called", "Capital call issued", AlertSeverity.Warning, "InApp,Email", null, true),

            ("tenancy.expiring", "Tenancy ending", AlertSeverity.Warning, "InApp,Email", 90, true),
            ("complaint.raised", "Complaint logged", AlertSeverity.Info, "InApp,Push", null, false),
            ("complaint.breached", "Complaint past its response time", AlertSeverity.Critical, "InApp,Push", null, false),
            ("workorder.assigned", "Work order assigned", AlertSeverity.Info, "InApp,Push,Sms", null, false),

            ("file.missing", "Physical file missing", AlertSeverity.Critical, "InApp,Email", null, false),
            ("legal.hearing.missed", "Hearing not attended", AlertSeverity.Critical, "InApp,Email", null, false),
            ("document.issued", "Document issued", AlertSeverity.Info, "InApp,Email", null, true),
            ("signature.requested", "Signature requested", AlertSeverity.Info, "InApp,Email,WhatsApp", null, true),
            ("signature.completed", "Document fully signed", AlertSeverity.Info, "InApp,Email", null, true),
            ("portal.invited", "Portal invitation", AlertSeverity.Info, "Email,Sms", null, true),
            ("conversation.assigned", "Conversation assigned", AlertSeverity.Info, "InApp,Push", null, false),
        };

        foreach (var (key, name, severity, channels, leadDays, quiet) in rules)
        {
            db.NotificationRules.Add(new NotificationRule
            {
                RuleKey = key,
                Name = name,
                Severity = severity,
                Channels = channels,
                LeadDays = leadDays,
                RespectQuietHours = quiet,
                IsEnabled = true,
            }.StampNew(tenant, userId));
        }
    }

    private async Task EnsureSurchargeAndDunningAsync(FixedRealEstateTenant tenant, Guid userId)
    {
        if (!await db.SurchargePolicies.AnyAsync(p => p.CompanyId == tenant.CompanyId && !p.IsDeleted))
        {
            // A grace period and a cap, because a surcharge with neither eventually exceeds the
            // instalment it is charged on and nobody ever pays it.
            db.SurchargePolicies.Add(new SurchargePolicy
            {
                Name = "Standard late payment",
                Basis = SurchargeBasis.PerMonthOnOverdue,
                Rate = 1.5m,
                GraceDays = 7,
                CapPercent = 15m,
                IsCompounding = false,
                WaiverRequiresApproval = true,
            }.StampNew(tenant, userId));
        }

        if (await db.DunningPolicies.AnyAsync(p => p.CompanyId == tenant.CompanyId && !p.IsDeleted)) return;

        var policy = new DunningPolicy
        {
            Name = "Standard collections ladder",
            AppliesTo = "Booking",
            IsDefault = true,
        }.StampNew(tenant, userId);

        db.DunningPolicies.Add(policy);

        var steps = new (int Step, string Name, int Days, DunningAction Action, NotificationChannel Channel)[]
        {
            (1, "Friendly reminder", 3, DunningAction.SendReminder, NotificationChannel.WhatsApp),
            (2, "Second reminder", 10, DunningAction.SendReminder, NotificationChannel.Email),
            (3, "Telephone call", 21, DunningAction.CreateCallTask, NotificationChannel.Call),
            (4, "Formal letter", 30, DunningAction.IssueNotice, NotificationChannel.Post),
            (5, "Final notice", 60, DunningAction.IssueFinalNotice, NotificationChannel.Post),
            (6, "Refer to legal", 90, DunningAction.ReferToLegal, NotificationChannel.Post),
        };

        foreach (var (step, name, days, action, channel) in steps)
        {
            db.DunningSteps.Add(new DunningStep
            {
                DunningPolicyId = policy.Id,
                StepNumber = step,
                Name = name,
                DaysAfterDue = days,
                Action = action,
                Channel = channel,
                StopsOnPromise = true,
                RequiresApproval = action is DunningAction.IssueFinalNotice or DunningAction.ReferToLegal,
            }.StampNew(tenant, userId));
        }
    }

    private async Task EnsureDeductionPolicyAsync(FixedRealEstateTenant tenant, Guid userId)
    {
        if (await db.DeductionPolicies.AnyAsync(p => p.CompanyId == tenant.CompanyId && !p.IsDeleted)) return;

        var policy = new DeductionPolicy
        {
            Name = "Standard cancellation deductions",
            Basis = DeductionBasis.SlabByElapsed,
            AdministrativeCharge = 25_000m,
            ForfeitAccruedSurcharge = true,
            ClawBackCommission = true,
            RefundInstalmentCount = 3,
        }.StampNew(tenant, userId);

        db.DeductionPolicies.Add(policy);

        // Sliding, because the further into a plan somebody is, the more of the cost of finding
        // them a replacement has already been spent.
        var slabs = new (decimal FromPercent, decimal ToPercent, decimal Deduct)[]
        {
            (0m, 10m, 100m),
            (10m, 25m, 40m),
            (25m, 50m, 20m),
            (50m, 100m, 10m),
        };

        var order = 0;

        foreach (var (from, to, deduct) in slabs)
        {
            db.DeductionSlabs.Add(new DeductionSlab
            {
                DeductionPolicyId = policy.Id,
                FromPaidPercent = from,
                ToPaidPercent = to,
                DeductionPercent = deduct,
                SortOrder = order++,
            }.StampNew(tenant, userId));
        }
    }

    // ═══ Sample data ═════════════════════════════════════════════════════════

    /// <summary>
    /// One small tower with two blocks, four floors and twenty-four flats, priced and planned. Big
    /// enough that every screen has something on it, small enough to delete in a minute.
    /// </summary>
    private async Task SeedSampleProjectAsync(FixedRealEstateTenant tenant, RealEstateOffice office, Guid userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var project = new Project
        {
            Name = "Riverside Heights",
            Code = "RH",
            Kind = ProjectKind.ApartmentTower,
            Status = ProjectStatus.Launched,
            OfficeId = office.Id,
            City = "Sample City",
            LaunchDate = today.AddMonths(-3),
            BookingOpenDate = today.AddMonths(-3),
            ConstructionStartDate = today.AddMonths(-2),
            PlannedCompletionDate = today.AddMonths(30),
            PromisedPossessionDate = today.AddMonths(33),
            ForecastPossessionDate = today.AddMonths(33),
            TotalLandAreaSqFt = 48_000m,
            SaleableAreaSqFt = 33_600m,
            TotalBudget = 420_000_000m,
            Tagline = "Two-bedroom and three-bedroom apartments overlooking the river",
            HoldHours = 48,
        }.StampNew(tenant, userId);

        db.Projects.Add(project);

        var blockNames = new[] { "Block A", "Block B" };
        var floorCount = 4;
        var perFloor = 3;

        var priceList = new PriceList
        {
            ProjectId = project.Id,
            Name = "Launch pricing",
            EffectiveFrom = today.AddMonths(-3),
            IsPublished = true,
            PublishedAt = DateTime.UtcNow,
        }.StampNew(tenant, userId);

        db.PriceLists.Add(priceList);

        foreach (var (blockName, blockIndex) in blockNames.Select((n, i) => (n, i)))
        {
            var block = new ProjectNode
            {
                ProjectId = project.Id,
                Kind = ProjectNodeKind.Block,
                Name = blockName,
                SortOrder = blockIndex,
                Depth = 0,
                Path = blockName,
                PlannedUnitCount = floorCount * perFloor,
                Status = ProjectStatus.UnderConstruction,
                PlannedCompletionDate = project.PlannedCompletionDate,
                ProgressPercent = 18m,
            }.StampNew(tenant, userId);

            db.ProjectNodes.Add(block);

            for (var floor = 1; floor <= floorCount; floor++)
            {
                for (var position = 1; position <= perFloor; position++)
                {
                    var unitNumber = $"{blockName[^1]}-{floor}0{position}";
                    var isThreeBed = position == perFloor;
                    var area = isThreeBed ? 1_650m : 1_180m;
                    var rate = 8_500m + (floor - 1) * 150m;

                    var property = new Property
                    {
                        Reference = $"PRP-{blockIndex}{floor}{position:D2}",
                        Name = unitNumber,
                        Category = PropertyCategory.Residential,
                        SubType = PropertySubType.Apartment,
                        Status = PropertyStatus.Available,
                        ProjectId = project.Id,
                        ProjectNodeId = block.Id,
                        FloorNumber = floor,
                        UnitNumber = unitNumber,
                        SaleableAreaSqFt = area,
                        CoveredAreaSqFt = area * 0.82m,
                        Bedrooms = isThreeBed ? 3 : 2,
                        Bathrooms = isThreeBed ? 3 : 2,
                        ParkingBays = isThreeBed ? 2 : 1,
                    }.StampNew(tenant, userId);

                    db.Properties.Add(property);

                    db.Units.Add(new Unit
                    {
                        ProjectId = project.Id,
                        ProjectNodeId = block.Id,
                        PropertyId = property.Id,
                        UnitNumber = unitNumber,
                        Status = PropertyStatus.Available,
                        PriceListId = priceList.Id,
                        BaseRatePerSqFt = rate,
                        BasePrice = area * rate,
                        TotalPrice = area * rate,
                        IsSaleable = true,
                        PositionOnFloor = position,
                    }.StampNew(tenant, userId));
                }
            }
        }

        // Milestones with weights that add to a hundred, because the escrow entitlement and the
        // physical progress on every report are computed from exactly these.
        var milestones = new (string Name, string Code, int Months, decimal Weight)[]
        {
            ("Excavation and foundation", "FDN", 2, 12m),
            ("Structure to plinth", "PLN", 5, 10m),
            ("Structure complete", "STR", 14, 28m),
            ("Brickwork and plaster", "BRK", 20, 15m),
            ("Services and finishes", "FIN", 26, 20m),
            ("External works and handover", "EXT", 30, 15m),
        };

        var order = 0;

        foreach (var (name, code, months, weight) in milestones)
        {
            db.ProjectMilestones.Add(new ProjectMilestone
            {
                ProjectId = project.Id,
                Name = name,
                Code = code,
                SortOrder = order++,
                Status = order == 1 ? MilestoneStatus.InProgress : MilestoneStatus.NotStarted,
                PlannedDate = today.AddMonths(months - 2),
                ForecastDate = today.AddMonths(months - 2),
                WeightPercent = weight,
                ProgressPercent = order == 1 ? 45m : 0m,
            }.StampNew(tenant, userId));
        }

        var budgetHeads = new (string Head, decimal Amount)[]
        {
            ("Land acquisition", 140_000_000m),
            ("Construction", 190_000_000m),
            ("Infrastructure and external works", 26_000_000m),
            ("Statutory approvals", 9_000_000m),
            ("Finance cost", 22_000_000m),
            ("Marketing and launch", 14_000_000m),
            ("Sales commission", 12_000_000m),
            ("Overheads", 7_000_000m),
        };

        var budgetOrder = 0;

        foreach (var (head, amount) in budgetHeads)
        {
            db.ProjectBudgetLines.Add(new ProjectBudgetLine
            {
                ProjectId = project.Id,
                CostHead = head,
                BudgetAmount = amount,
                ForecastAmount = amount,
                SortOrder = budgetOrder++,
            }.StampNew(tenant, userId));
        }

        // A construction-linked plan, because that is what a scheme at this stage actually sells,
        // and it is the one that exercises the milestone-to-demand path.
        var template = new PaymentPlanTemplate
        {
            ProjectId = project.Id,
            Name = "Construction-linked plan",
            IsDefault = true,
            EffectiveFrom = today.AddMonths(-3),
        }.StampNew(tenant, userId);

        db.PaymentPlanTemplates.Add(template);

        var lines = new (string Label, InstalmentKind Kind, decimal Percent, int Month)[]
        {
            ("Booking amount", InstalmentKind.BookingAmount, 10m, 0),
            ("On confirmation", InstalmentKind.ConfirmationAmount, 10m, 1),
            ("On foundation", InstalmentKind.MilestoneLinked, 15m, 2),
            ("On plinth", InstalmentKind.MilestoneLinked, 15m, 5),
            ("On structure complete", InstalmentKind.MilestoneLinked, 20m, 14),
            ("On finishes", InstalmentKind.MilestoneLinked, 20m, 26),
            ("On possession", InstalmentKind.PossessionBalance, 10m, 33),
        };

        var lineOrder = 0;

        foreach (var (label, kind, percent, month) in lines)
        {
            db.PaymentPlanTemplateLines.Add(new PaymentPlanTemplateLine
            {
                PaymentPlanTemplateId = template.Id,
                SortOrder = ++lineOrder,
                Label = label,
                Kind = kind,
                Percent = percent,
                Count = 1,
                Frequency = InstalmentFrequency.OneOff,
                StartOffsetMonths = month,
            }.StampNew(tenant, userId));
        }

        project.DefaultPaymentPlanTemplateId = template.Id;
        project.TotalSalesValue = blockNames.Length * floorCount * perFloor * 1_180m * 8_500m;

        await Task.CompletedTask;
    }
}
