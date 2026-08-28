using Microsoft.EntityFrameworkCore;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The report catalogue and the engine that runs it.
///
/// Every report returns the same shape — columns, rows, totals, and optionally a breakdown and a
/// trend — so the screen that renders one renders all of them, and a new report is a method rather
/// than a new page. The columns carry their own type and formatting, because a number that is a
/// currency in one column and a percentage in the next cannot be guessed at by the client.
///
/// Reports are capped rather than paged. Somebody who genuinely wants eighty thousand rows wants a
/// file, not a screen, and the cap is reported honestly instead of silently truncating.
/// </summary>
public partial class RealEstateReportService
{
    private const int MaxRows = 5000;

    public async Task<List<ReportDefinitionDto>> GetReportCatalogueAsync()
    {
        var settings = await SettingsAsync();

        var all = new List<ReportDefinitionDto>
        {
            // ── Sales and inventory ──────────────────────────────────────────
            Def("sales.register", "Sales register", "Every booking with its value, plan and collection position.",
                "Sales", LineOfBusiness.Development, "receipt", 10, ["FromDate", "ToDate", "ProjectId"], true, true),
            Def("inventory.status", "Inventory status", "What is available, held, booked and sold, block by block.",
                "Sales", LineOfBusiness.Development, "layout-grid", 20, ["ProjectId"], true),
            Def("sales.absorption", "Absorption analysis", "How fast inventory is selling, and what is left at that rate.",
                "Sales", LineOfBusiness.Development, "trending-up", 30, ["ProjectId"], false, true),
            Def("sales.realisation", "Price realisation", "List price against what was actually achieved, and the gap.",
                "Sales", LineOfBusiness.Development, "tag", 40, ["FromDate", "ToDate", "ProjectId"], true),
            Def("sales.cancellations", "Cancellation analysis", "What was cancelled, why, and what was deducted.",
                "Sales", LineOfBusiness.Development, "x-circle", 50, ["FromDate", "ToDate", "ProjectId"], true),
            Def("sales.source", "Booking by source", "Which channels produced bookings, and what they were worth.",
                "Sales", LineOfBusiness.Development, "share-2", 60, ["FromDate", "ToDate", "ProjectId"], true),

            // ── Money ────────────────────────────────────────────────────────
            Def("money.collection", "Collection summary", "Demanded against collected, by period.",
                "Money", LineOfBusiness.Development, "banknote", 100, ["FromDate", "ToDate", "ProjectId"], true, true),
            Def("money.ageing", "Ageing analysis", "Outstanding money bucketed by how long it has been owed.",
                "Money", LineOfBusiness.Development, "hourglass", 110, ["ProjectId"], true),
            Def("money.defaulters", "Defaulter list", "Who is overdue, by how much, and for how long.",
                "Money", LineOfBusiness.Development, "user-x", 120, ["ProjectId"], false),
            Def("money.daybook", "Receipt daybook", "Every receipt taken in a period, by instrument.",
                "Money", null, "book-open", 130, ["FromDate", "ToDate", "OfficeId"], true),
            Def("money.surcharge", "Surcharge register", "Late-payment surcharge accrued, waived and collected.",
                "Money", LineOfBusiness.Development, "percent", 140, ["FromDate", "ToDate", "ProjectId"]),
            Def("money.refunds", "Refund register", "Refunds requested, approved and paid.",
                "Money", null, "corner-down-left", 150, ["FromDate", "ToDate"]),
            Def("money.cheques", "Cheque register", "Post-dated cheques on hand, deposited, cleared and bounced.",
                "Money", null, "file-text", 160, ["FromDate", "ToDate"], true),

            // ── Brokerage ────────────────────────────────────────────────────
            Def("brokerage.funnel", "Lead funnel", "Enquiries by stage, and where they are being lost.",
                "Brokerage", LineOfBusiness.Brokerage, "filter", 200, ["FromDate", "ToDate", "OfficeId"], true),
            Def("brokerage.agents", "Agent performance", "Leads, viewings, deals and fees by negotiator.",
                "Brokerage", LineOfBusiness.Brokerage, "users", 210, ["FromDate", "ToDate", "OfficeId"]),
            Def("brokerage.listings", "Listing performance", "Views, enquiries, viewings and offers per listing.",
                "Brokerage", LineOfBusiness.Brokerage, "home", 220, ["OfficeId"]),
            Def("brokerage.pipeline", "Deal pipeline", "Deals in progress, their fee, and how long they have been stuck.",
                "Brokerage", LineOfBusiness.Brokerage, "git-branch", 230, ["OfficeId"], true),
            Def("brokerage.commission", "Commission statement", "Fees earned, split, deducted and paid.",
                "Brokerage", LineOfBusiness.Brokerage, "wallet", 240, ["FromDate", "ToDate", "AgentId"], true),
            Def("brokerage.fallthrough", "Fall-through analysis", "Deals that collapsed, at what stage, and why.",
                "Brokerage", LineOfBusiness.Brokerage, "unlink", 250, ["FromDate", "ToDate"], true),
            Def("brokerage.partners", "Channel partner performance", "Registrations, visits, bookings and commission by partner.",
                "Brokerage", null, "handshake", 260, ["FromDate", "ToDate"]),

            // ── Estate management ────────────────────────────────────────────
            Def("estate.rentroll", "Rent roll", "Every live tenancy with its rent, term and arrears.",
                "Leasing", LineOfBusiness.EstateManagement, "key", 300, ["OfficeId", "LandlordId"], true),
            Def("estate.arrears", "Rent arrears", "Who is behind, by how much, and for how long.",
                "Leasing", LineOfBusiness.EstateManagement, "alert-circle", 310, ["OfficeId"], true),
            Def("estate.expiries", "Tenancy expiries", "Tenancies ending, so renewals are started in time.",
                "Leasing", LineOfBusiness.EstateManagement, "calendar", 320, ["OfficeId"]),
            Def("estate.voids", "Void analysis", "Empty periods, the rent lost, and how long they ran.",
                "Leasing", LineOfBusiness.EstateManagement, "door-open", 330, ["FromDate", "ToDate"]),
            Def("estate.compliance", "Compliance status", "Safety certificates by property, and what has lapsed.",
                "Leasing", LineOfBusiness.EstateManagement, "shield-check", 340, [], true),
            Def("estate.landlords", "Landlord statements", "Rent collected, fees taken and money paid out per owner.",
                "Leasing", LineOfBusiness.EstateManagement, "user-check", 350, ["FromDate", "ToDate", "LandlordId"]),
            Def("estate.servicecharge", "Service charge recovery", "Budgeted, invoiced and collected service charge.",
                "Leasing", LineOfBusiness.EstateManagement, "receipt-text", 360, ["FromDate", "ToDate"]),

            // ── Society and facility ─────────────────────────────────────────
            Def("society.billing", "Maintenance billing", "Bills raised, collected and outstanding by unit.",
                "Society", null, "building", 400, ["FromDate", "ToDate", "SocietyId"], true),
            Def("society.complaints", "Complaint performance", "Volume, resolution time and breaches by category.",
                "Society", null, "message-square-warning", 410, ["FromDate", "ToDate", "SocietyId"], true),
            Def("facility.workorders", "Work order costs", "What was spent on repairs, by trade and by who bore it.",
                "Facility", null, "hard-hat", 420, ["FromDate", "ToDate"], true),
            Def("facility.meters", "Meter consumption", "Consumption per meter, with common-area and unexplained loss.",
                "Facility", null, "gauge", 430, ["FromDate", "ToDate", "SocietyId"]),
            Def("society.gate", "Gate movement", "Visitors in and out, by kind and by hour.",
                "Society", null, "scan-face", 440, ["FromDate", "ToDate", "SocietyId"], true),

            // ── Construction ─────────────────────────────────────────────────
            Def("construction.costvbudget", "Cost against budget", "Budget, committed, actual and forecast by work package.",
                "Construction", LineOfBusiness.Contracting, "calculator", 500, ["ProjectId"], true),
            Def("construction.ipc", "Certificate register", "Interim payment certificates issued and paid.",
                "Construction", LineOfBusiness.Contracting, "file-check", 510, ["FromDate", "ToDate", "ProjectId"]),
            Def("construction.variations", "Variation register", "Every variation, its value and its time impact.",
                "Construction", LineOfBusiness.Contracting, "git-pull-request", 520, ["ProjectId"], true),
            Def("construction.subcontractors", "Subcontractor liability", "What each subcontractor is owed and what is held.",
                "Construction", LineOfBusiness.Contracting, "users-round", 530, ["ProjectId"]),
            Def("construction.retention", "Retention ledger", "Retention held, released and due for release.",
                "Construction", LineOfBusiness.Contracting, "lock", 540, ["ProjectId"]),
            Def("construction.delays", "Delay register", "Delay events, their cause and the time claimed.",
                "Construction", LineOfBusiness.Contracting, "clock-alert", 550, ["ProjectId"], true),

            // ── Finance ──────────────────────────────────────────────────────
            Def("finance.escrow", "Escrow movement", "What went into escrow, what came out, and against what certification.",
                "Finance", LineOfBusiness.Development, "vault", 600, ["FromDate", "ToDate", "ProjectId"]),
            Def("finance.profitability", "Unit profitability", "Realisation against allocated cost, unit by unit.",
                "Finance", LineOfBusiness.Development, "trending-up", 610, ["ProjectId"], true),
            Def("finance.landowner", "Landowner position", "Entitlement accrued, paid and outstanding per joint venture.",
                "Finance", LineOfBusiness.Development, "handshake", 620, ["ProjectId"]),
            Def("finance.investors", "Investor summary", "Committed, contributed, distributed and the return so far.",
                "Finance", null, "briefcase", 630, ["ProjectId"]),
            Def("finance.withholding", "Withholding register", "Tax deducted at source, deposited and certified.",
                "Finance", null, "landmark", 640, ["FromDate", "ToDate"], true),
            Def("finance.approvals", "Statutory approval status", "Every approval, its state and its expiry.",
                "Finance", LineOfBusiness.Development, "stamp", 650, ["ProjectId"], true),
        };

        // A report for a line of business this company does not run is noise on the menu.
        return all
            .Where(r => r.LineOfBusiness switch
            {
                LineOfBusiness.Brokerage => settings.BrokerageEnabled,
                LineOfBusiness.Development => settings.DevelopmentEnabled,
                LineOfBusiness.Contracting => settings.ContractingEnabled,
                LineOfBusiness.EstateManagement => settings.EstateManagementEnabled,
                _ => true,
            })
            .OrderBy(r => r.SortOrder)
            .ToList();
    }

    private static ReportDefinitionDto Def(
        string key, string title, string description, string category, LineOfBusiness? lob,
        string icon, int sortOrder, List<string> parameters,
        bool supportsGrouping = false, bool supportsTrend = false)
        => new()
        {
            Key = key,
            Title = title,
            Description = description,
            Category = category,
            LineOfBusiness = lob,
            Icon = icon,
            SortOrder = sortOrder,
            Parameters = parameters,
            SupportsGrouping = supportsGrouping,
            SupportsTrend = supportsTrend,
        };

    public async Task<ReportResultDto> RunReportAsync(ReportRequestDto request)
    {
        var catalogue = await GetReportCatalogueAsync();

        var definition = catalogue.FirstOrDefault(r => r.Key == request.ReportKey)
            ?? throw new InvalidOperationException(
                $"There is no report called “{request.ReportKey}”, or it belongs to a line of business this company does not run.");

        var from = request.FromDate ?? Today.AddMonths(-1);
        var to = request.ToDate ?? Today;

        if (to < from) throw new InvalidOperationException("The period ends before it begins.");

        var result = new ReportResultDto
        {
            ReportKey = definition.Key,
            Title = definition.Title,
            Subtitle = definition.Description,
            GeneratedAt = DateTime.UtcNow,
            CurrencyCode = await CurrencyAsync(),
            AppliedFilters = await DescribeFiltersAsync(definition, request, from, to),
        };

        await (definition.Key switch
        {
            "sales.register" => SalesRegisterAsync(result, request, from, to),
            "inventory.status" => InventoryStatusAsync(result, request),
            "sales.absorption" => AbsorptionAsync(result, request, from, to),
            "sales.realisation" => RealisationAsync(result, request, from, to),
            "sales.cancellations" => CancellationsAsync(result, request, from, to),
            "sales.source" => BookingSourceAsync(result, request, from, to),

            "money.collection" => CollectionSummaryAsync(result, request, from, to),
            "money.ageing" => AgeingAsync(result, request),
            "money.defaulters" => DefaultersAsync(result, request),
            "money.daybook" => DaybookAsync(result, request, from, to),
            "money.surcharge" => SurchargeAsync(result, request, from, to),
            "money.refunds" => RefundsAsync(result, from, to),
            "money.cheques" => ChequesAsync(result, from, to),

            "brokerage.funnel" => LeadFunnelAsync(result, request, from, to),
            "brokerage.agents" => AgentPerformanceAsync(result, request, from, to),
            "brokerage.listings" => ListingPerformanceAsync(result, request),
            "brokerage.pipeline" => DealPipelineAsync(result, request),
            "brokerage.commission" => CommissionStatementAsync(result, request, from, to),
            "brokerage.fallthrough" => FallThroughAsync(result, from, to),
            "brokerage.partners" => PartnerPerformanceAsync(result),

            "estate.rentroll" => RentRollAsync(result, request),
            "estate.arrears" => ArrearsAsync(result, request),
            "estate.expiries" => ExpiriesAsync(result, request),
            "estate.voids" => VoidsAsync(result, from, to),
            "estate.compliance" => ComplianceStatusAsync(result),
            "estate.landlords" => LandlordStatementsAsync(result, request, from, to),
            "estate.servicecharge" => ServiceChargeRecoveryAsync(result, from, to),

            "society.billing" => SocietyBillingAsync(result, request, from, to),
            "society.complaints" => ComplaintPerformanceAsync(result, request, from, to),
            "facility.workorders" => WorkOrderCostsAsync(result, from, to),
            "facility.meters" => MeterConsumptionAsync(result, request, from, to),
            "society.gate" => GateMovementAsync(result, request, from, to),

            "construction.costvbudget" => CostAgainstBudgetAsync(result, request),
            "construction.ipc" => CertificateRegisterAsync(result, request, from, to),
            "construction.variations" => VariationRegisterAsync(result, request),
            "construction.subcontractors" => SubcontractorLiabilityAsync(result, request),
            "construction.retention" => RetentionLedgerAsync(result, request),
            "construction.delays" => DelayRegisterAsync(result, request),

            "finance.escrow" => EscrowMovementAsync(result, request, from, to),
            "finance.profitability" => ProfitabilityAsync(result, request),
            "finance.landowner" => LandownerPositionAsync(result, request),
            "finance.investors" => InvestorSummaryAsync(result, request),
            "finance.withholding" => WithholdingRegisterAsync(result, from, to),
            "finance.approvals" => ApprovalStatusAsync(result, request),

            _ => throw new InvalidOperationException($"Report “{definition.Key}” is listed but not implemented."),
        });

        result.RowCount = result.Rows.Count;

        if (result.Rows.Count > MaxRows)
        {
            result.Rows = result.Rows.Take(MaxRows).ToList();
            result.IsTruncated = true;
        }

        if (request.Top is > 0) result.Rows = result.Rows.Take(request.Top.Value).ToList();

        return result;
    }

    private async Task<Dictionary<string, string>> DescribeFiltersAsync(
        ReportDefinitionDto definition, ReportRequestDto request, DateOnly from, DateOnly to)
    {
        var filters = new Dictionary<string, string>();

        if (definition.Parameters.Contains("FromDate"))
            filters["Period"] = $"{from:dd MMM yyyy} to {to:dd MMM yyyy}";

        if (request.ProjectId is not null)
        {
            var name = await Db.Projects.ForCompany(Tenant)
                .Where(p => p.Id == request.ProjectId).Select(p => p.Name).FirstOrDefaultAsync();

            if (name is not null) filters["Project"] = name;
        }

        if (request.OfficeId is not null)
        {
            var name = await Db.RealEstateOffices.ForCompany(Tenant)
                .Where(o => o.Id == request.OfficeId).Select(o => o.Name).FirstOrDefaultAsync();

            if (name is not null) filters["Office"] = name;
        }

        if (request.SocietyId is not null)
        {
            var name = await Db.Societies.ForCompany(Tenant)
                .Where(s => s.Id == request.SocietyId).Select(s => s.Name).FirstOrDefaultAsync();

            if (name is not null) filters["Society"] = name;
        }

        if (!string.IsNullOrWhiteSpace(request.GroupBy)) filters["Grouped by"] = request.GroupBy;

        return filters;
    }

    // ═══ Column helpers ══════════════════════════════════════════════════════

    private static ReportColumnDto Text(string key, string label, int width = 160)
        => new() { Key = key, Label = label, Type = "text", Width = width };

    private static ReportColumnDto Money(string key, string label, bool totalled = true, int width = 130)
        => new() { Key = key, Label = label, Type = "currency", Align = "right", IsTotalled = totalled, Width = width };

    private static ReportColumnDto Number(string key, string label, bool totalled = true, int width = 100)
        => new() { Key = key, Label = label, Type = "number", Align = "right", IsTotalled = totalled, Width = width };

    private static ReportColumnDto Pct(string key, string label, int width = 110)
        => new() { Key = key, Label = label, Type = "percent", Align = "right", Width = width };

    private static ReportColumnDto Date(string key, string label, int width = 120)
        => new() { Key = key, Label = label, Type = "date", Width = width };

    private static ReportColumnDto Tag(string key, string label, int width = 130)
        => new() { Key = key, Label = label, Type = "tag", Width = width };

    /// <summary>
    /// Totals every column the definition marked as totalled, from the rows actually returned.
    /// Totalling on the server rather than the client means the figure on screen is the figure in
    /// the exported file, which is the only way anybody can reconcile the two.
    /// </summary>
    private static void Total(ReportResultDto result)
    {
        foreach (var column in result.Columns.Where(c => c.IsTotalled))
        {
            decimal sum = 0m;

            foreach (var row in result.Rows)
            {
                if (row.TryGetValue(column.Key, out var value) && value is not null
                    && decimal.TryParse(value.ToString(), out var parsed))
                {
                    sum += parsed;
                }
            }

            result.Totals[column.Key] = RealEstateMapper.Money(sum);
        }

        result.Totals["__rowCount"] = result.Rows.Count;
    }
}
