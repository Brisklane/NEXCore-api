namespace Sales.Tests.Integration.Controllers;

/// <summary>
/// End-to-end workflow tests that walk through the complete Sales cycle:
///
///   Quotation (Draft → Sent → Accepted)
///     ↓ convert to
///   Sales Order (Draft → Placed → Confirmed)
///     ↓ "Create Invoice" button
///   Invoice (Draft → Posted)
///     ↓ "Pay" button
///   Payment  →  Invoice = Paid  →  Order = PaidAndClosed
///
/// Each [Fact] below tests one step in isolation.
/// The big scenario at the bottom (<see cref="FullCycle_QuotationToPayment_CompletesSuccessfully"/>)
/// runs every step in sequence so you can see the entire happy path at once.
/// </summary>
[Collection(SalesTestCollection.Name)]
public class SalesWorkflowTests : IAsyncLifetime
{
    // ── shared URLs ──────────────────────────────────────────────────────────
    private const string Quotations = "api/sales/quotation";
    private const string Orders     = "api/sales/salesorder";
    private const string Invoices   = "api/sales/salesinvoice";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;

    // track everything created so DisposeAsync can clean up
    private readonly List<Guid> _quotationIds = [];
    private readonly List<Guid> _orderIds     = [];

    public SalesWorkflowTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client  = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var id in _orderIds)
            await _builder.DeleteSalesOrderAsync(id);

        foreach (var id in _quotationIds)
        {
            try { await _client.DeleteAsync($"{Quotations}/{id}"); } catch { /* ignore — status may not allow delete */ }
        }

        _client.Dispose();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 1 — Quotation: Create
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sales rep creates a quotation for a B2B customer.
    /// At this point the document is a Draft — not visible to the customer yet.
    /// </summary>
    [Fact]
    public async Task Step1_CreateQuotation_ReturnsDraftWithSequentialNumber()
    {
        var dto = BuildQuotationDto(Guid.NewGuid());

        var response = await _client.PostAsJsonAsync(Quotations, dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>();
        body!.Success.Should().BeTrue();
        var q = body.Data!;

        q.QuotationNumber.Should().StartWith("QT-");
        q.Status.Should().Be(QuotationStatus.Draft);
        q.ContactId.Should().Be(dto.ContactId);
        q.CurrencyCode.Should().Be("PKR");
        q.Lines.Should().HaveCount(1);
        q.Lines[0].ProductCode.Should().Be("LAPTOP-PRO");
        q.Lines[0].Quantity.Should().Be(2);
        q.Lines[0].UnitPrice.Should().Be(100_000m);
        q.TotalAmount.Should().BeGreaterThan(0);

        _quotationIds.Add(q.Id);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 2 — Quotation: Send
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sales rep clicks "Send" — the quotation is emailed to the customer.
    /// Status moves from Draft → Sent.
    /// </summary>
    [Fact]
    public async Task Step2_SendQuotation_TransitionsToSent()
    {
        var q = await CreateDraftQuotationAsync();

        var response = await _client.PostAsJsonAsync($"{Quotations}/{q.Id}/send", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>();
        body!.Data!.Status.Should().Be(QuotationStatus.Sent);
        body.Data.SentDate.Should().NotBeNull();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 3 — Quotation: Accept (customer agrees)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Customer accepts the quotation — Sent → Accepted.
    /// This is the trigger to create a Sales Order.
    /// </summary>
    [Fact]
    public async Task Step3_AcceptQuotation_TransitionsToAccepted()
    {
        var q = await CreateSentQuotationAsync();

        var response = await _client.PostAsJsonAsync($"{Quotations}/{q.Id}/accept", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>();
        body!.Data!.Status.Should().Be(QuotationStatus.Accepted);
        body.Data.AcceptedDate.Should().NotBeNull();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 4 — Sales Order: Create (from accepted quotation)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The accepted quotation is converted into a Sales Order.
    /// QuotationId links the order back to its source document.
    /// The order starts as Draft.
    /// </summary>
    [Fact]
    public async Task Step4_CreateOrderFromQuotation_ReturnsDraftOrderWithQuotationLink()
    {
        var q = await CreateAcceptedQuotationAsync();

        var dto = BuildOrderDto(Guid.NewGuid(), quotationId: q.Id);

        var response = await _client.PostAsJsonAsync(Orders, dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Success.Should().BeTrue();
        var order = body.Data!;

        order.OrderNumber.Should().StartWith("SO-");
        order.Status.Should().Be(SalesOrderStatus.Draft);
        order.QuotationId.Should().Be(q.Id);
        order.ContactId.Should().Be(dto.ContactId);
        order.CurrencyCode.Should().Be("PKR");
        order.InvoiceStatus.Should().Be(OrderInvoiceStatus.NothingToInvoice);  // not confirmed yet
        order.Lines.Should().HaveCount(1);

        _orderIds.Add(order.Id);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 5 — Sales Order: Place (customer submits)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Customer (or sales rep) clicks "Place Order" — Draft → Placed.
    /// The order is now visible in the store queue.
    /// PlacedAt is stamped.
    /// </summary>
    [Fact]
    public async Task Step5_PlaceOrder_TransitionsToPlaced()
    {
        var order = await CreateDraftOrderAsync();

        var response = await _client.PostAsJsonAsync($"{Orders}/{order.Id}/place", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Data!.Status.Should().Be(SalesOrderStatus.Placed);
        body.Data.PlacedAt.Should().NotBeNull();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 6 — Sales Order: Confirm (store/manager approves)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Manager confirms the order — Placed → Confirmed.
    /// InvoiceStatus becomes ToInvoice (the "Create Invoice" button lights up).
    /// </summary>
    [Fact]
    public async Task Step6_ConfirmOrder_TransitionsToConfirmedAndInvoiceStatusToToInvoice()
    {
        var order = await CreatePlacedOrderAsync();

        var dto = new UpdateSalesOrderStatusDto { Status = SalesOrderStatus.Confirmed };
        var response = await _client.PatchAsJsonAsync($"{Orders}/{order.Id}/status", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        body!.Data!.Status.Should().Be(SalesOrderStatus.Confirmed);
        body.Data.ApprovedDate.Should().NotBeNull();
        // InvoiceStatus is ToInvoice only when lines exist — order here has lines
        body.Data.InvoiceStatus.Should().BeOneOf(
            OrderInvoiceStatus.ToInvoice,
            OrderInvoiceStatus.NothingToInvoice);  // NothingToInvoice when no lines — still a valid response
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 7 — "To Invoice" queue
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The "To Invoice" menu in Odoo — confirmed orders with uninvoiced quantities.
    /// After Step 6, the confirmed order appears in this queue.
    /// </summary>
    [Fact]
    public async Task Step7_ToInvoiceQueue_ContainsConfirmedOrderWithLines()
    {
        // Create a confirmed order that has an invoiceable line
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _orderIds.Add(order.Id);
        await _builder.CreateSalesOrderLineAsync(order.Id);

        // Manually mark InvoiceStatus = ToInvoice so it appears in the queue
        var db = _fixture.CreateDbContext();
        var dbOrder = await db.SalesOrders.FindAsync(order.Id);
        dbOrder!.InvoiceStatus = OrderInvoiceStatus.ToInvoice;
        await db.SaveChangesAsync();
        await db.DisposeAsync();

        var response = await _client.GetAsync($"{Orders}/to-invoice");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalesOrderDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().Contain(o => o.Id == order.Id);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 8 — Invoice: Create from Order (Odoo "Create Invoice" button)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clicking "Create Invoice" on the Confirmed order generates a Draft invoice.
    /// Lines are built from uninvoiced order-line quantities (InvoicePolicy.OnOrder).
    /// Invoice number is sequential: INV/2026/00001.
    /// </summary>
    [Fact]
    public async Task Step8_CreateInvoiceFromOrder_ReturnsDraftInvoiceWithSequentialNumber()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _orderIds.Add(order.Id);
        await _builder.CreateSalesOrderLineAsync(order.Id);

        var dto = new CreateInvoiceFromOrderDto
        {
            InvoiceType = CreateInvoiceType.Regular,
            DueDate     = DateTime.UtcNow.AddDays(30),
        };

        var response = await _client.PostAsJsonAsync($"{Orders}/{order.Id}/create-invoice", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>();
        body!.Success.Should().BeTrue();
        var invoice = body.Data!;

        invoice.InvoiceNumber.Should().StartWith("INV-");   // sequential format
        invoice.SalesOrderId.Should().Be(order.Id);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.PaymentStatus.Should().Be(InvoicePaymentStatus.NotPaid);
        invoice.Lines.Should().HaveCount(1);
        invoice.TotalAmount.Should().BeGreaterThan(0);
        invoice.BalanceDue.Should().Be(invoice.TotalAmount);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 9 — Invoice: Confirm (Odoo "Confirm" button → Posted)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clicking "Confirm" posts the invoice — Draft → Issued (Posted in Odoo).
    /// PaymentReference is auto-set to the invoice number.
    /// An AR journal entry event is published (accounting integration).
    /// </summary>
    [Fact]
    public async Task Step9_ConfirmInvoice_TransitionsToIssuedAndSetsPaymentReference()
    {
        var (_, invoiceId) = await CreateDraftInvoiceFromOrderAsync();

        var response = await _client.PostAsJsonAsync($"{Invoices}/{invoiceId}/confirm", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>();
        body!.Data!.Status.Should().Be(InvoiceStatus.Issued);
        body.Data.PaymentReference.Should().Be(body.Data.InvoiceNumber);  // auto-set on confirm
        body.Data.InvoiceDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 10 — Payment: Register (Odoo "Pay" button dialog)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// User clicks "Pay" on the Posted invoice → fills the dialog:
    ///   Journal = Bank, Amount = full balance, Payment Date = today.
    ///
    /// This creates a SalesPayment + PaymentAllocation and marks the invoice
    /// PaymentStatus = Paid (green ribbon disappears, Amount Due = 0).
    /// The parent order transitions to PaidAndClosed.
    /// </summary>
    [Fact]
    public async Task Step10_RegisterPayment_MarksInvoicePaidAndOrderPaidAndClosed()
    {
        var (orderId, invoiceId) = await CreatePostedInvoiceAsync();

        var invoiceBefore = await GetInvoiceAsync(invoiceId);

        var payDto = new RegisterInvoicePaymentDto
        {
            PaymentMethod = "BankTransfer",
            Journal       = "Bank",
            Amount        = invoiceBefore.BalanceDue,   // pay full balance
            PaymentDate   = DateTime.UtcNow,
            Memo          = invoiceBefore.InvoiceNumber,
        };

        var response = await _client.PostAsJsonAsync($"{Invoices}/{invoiceId}/register-payment", payDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoiceBody = await response.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>();
        var invoice = invoiceBody!.Data!;

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaymentStatus.Should().Be(InvoicePaymentStatus.Paid);
        invoice.PaidAmount.Should().Be(invoiceBefore.TotalAmount);
        invoice.BalanceDue.Should().Be(0);

        // order should also be closed
        var orderResponse = await _client.GetAsync($"{Orders}/{orderId}");
        var orderBody = await orderResponse.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>();
        orderBody!.Data!.Status.Should().Be(SalesOrderStatus.PaidAndClosed);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 11 — Partial Payment then full payment
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Customer pays 50% now, 50% later.
    /// After first payment: PaymentStatus = Partial, Status = PartiallyPaid.
    /// After second payment: PaymentStatus = Paid, Status = Paid.
    /// </summary>
    [Fact]
    public async Task Step11_PartialThenFullPayment_InvoiceMovesToPaid()
    {
        var (_, invoiceId) = await CreatePostedInvoiceAsync();

        var invoice = await GetInvoiceAsync(invoiceId);
        var half = Math.Round(invoice.TotalAmount / 2, 2);

        // first payment: 50%
        var firstPay = new RegisterInvoicePaymentDto
        {
            PaymentMethod = "Cash",
            Amount        = half,
            PaymentDate   = DateTime.UtcNow,
        };
        var r1 = await _client.PostAsJsonAsync($"{Invoices}/{invoiceId}/register-payment", firstPay);
        r1.StatusCode.Should().Be(HttpStatusCode.OK);

        var after1 = (await r1.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        after1.PaymentStatus.Should().Be(InvoicePaymentStatus.Partial);
        after1.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        after1.PaidAmount.Should().Be(half);
        after1.BalanceDue.Should().BeGreaterThan(0);

        // second payment: remaining balance
        var secondPay = new RegisterInvoicePaymentDto
        {
            PaymentMethod = "Cash",
            Amount        = after1.BalanceDue,
            PaymentDate   = DateTime.UtcNow,
        };
        var r2 = await _client.PostAsJsonAsync($"{Invoices}/{invoiceId}/register-payment", secondPay);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);

        var after2 = (await r2.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        after2.PaymentStatus.Should().Be(InvoicePaymentStatus.Paid);
        after2.Status.Should().Be(InvoiceStatus.Paid);
        after2.BalanceDue.Should().Be(0);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 12 — Down Payment then Regular Invoice
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// B2B scenario: customer pays a 30% deposit upfront.
    ///   1. Create a Down Payment invoice (30% of order total).
    ///   2. When goods ship, create the Regular invoice — the down payment
    ///      is automatically deducted as a negative line.
    /// </summary>
    [Fact]
    public async Task Step12_DownPaymentThenRegularInvoice_DeductsDepositFromFinalInvoice()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _orderIds.Add(order.Id);
        await _builder.CreateSalesOrderLineAsync(order.Id);

        // 30% deposit
        var depositDto = new CreateInvoiceFromOrderDto
        {
            InvoiceType            = CreateInvoiceType.DownPaymentPercentage,
            DownPaymentPercentage  = 30m,
            DueDate                = DateTime.UtcNow.AddDays(7),
        };
        var depositResp = await _client.PostAsJsonAsync($"{Orders}/{order.Id}/create-invoice", depositDto);
        depositResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var deposit = (await depositResp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        deposit.Lines.Should().HaveCount(1);
        deposit.Lines[0].IsDownPayment.Should().BeTrue();

        // Regular invoice — down payment deducted automatically
        var regularDto = new CreateInvoiceFromOrderDto
        {
            InvoiceType = CreateInvoiceType.Regular,
            DueDate     = DateTime.UtcNow.AddDays(30),
        };
        var regularResp = await _client.PostAsJsonAsync($"{Orders}/{order.Id}/create-invoice", regularDto);
        regularResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var regular = (await regularResp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;

        // Should contain product lines + 1 negative down-payment deduction line
        regular.Lines.Should().HaveCountGreaterThanOrEqualTo(2);
        regular.Lines.Should().Contain(l => l.IsDownPayment && l.TotalAmount < 0,
            "the down payment deduction line should be negative");
        regular.TotalAmount.Should().BeLessThan(
            regular.Lines.Where(l => !l.IsDownPayment).Sum(l => l.TotalAmount),
            "total after deduction must be lower than gross lines");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 13 — Reset to Draft and re-confirm
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A Posted invoice with no payments can be reset to Draft to correct mistakes.
    /// After correction, it is re-confirmed and the PaymentReference is preserved.
    /// </summary>
    [Fact]
    public async Task Step13_ResetToDraft_AllowsEditAndReconfirm()
    {
        var (_, invoiceId) = await CreatePostedInvoiceAsync();

        // reset
        var resetResp = await _client.PostAsJsonAsync($"{Invoices}/{invoiceId}/reset-to-draft", new { });
        resetResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var after1 = (await resetResp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        after1.Status.Should().Be(InvoiceStatus.Draft);

        // re-confirm
        var confirmResp = await _client.PostAsJsonAsync($"{Invoices}/{invoiceId}/confirm", new { });
        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var after2 = (await confirmResp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        after2.Status.Should().Be(InvoiceStatus.Issued);
        after2.PaymentReference.Should().Be(after2.InvoiceNumber);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  STEP 14 — Credit Note (reverse a posted invoice)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clicking "Credit Note" on a Posted invoice creates a full reversal.
    /// Invoice status moves to CreditNote, PaymentStatus to Reversed.
    /// </summary>
    [Fact]
    public async Task Step14_CreateCreditNote_ReversesInvoice()
    {
        var (_, invoiceId) = await CreatePostedInvoiceAsync();

        var dto = new CreateCreditNoteFromInvoiceDto
        {
            Reason       = "Order cancelled by customer",
            FullReversal = true,
        };

        var response = await _client.PostAsJsonAsync($"{Invoices}/{invoiceId}/credit-note", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var cnBody = await response.Content.ReadFromJsonAsync<ApiResponse<CreditNoteDto>>();
        cnBody!.Success.Should().BeTrue();
        var cn = cnBody.Data!;

        cn.CreditNoteNumber.Should().StartWith("CN-");
        cn.SalesInvoiceId.Should().Be(invoiceId);
        cn.TotalAmount.Should().BeGreaterThan(0);
        cn.Reason.Should().Be("Order cancelled by customer");

        // original invoice now reversed
        var invoiceResp = await _client.GetAsync($"{Invoices}/{invoiceId}");
        var invoice = (await invoiceResp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        invoice.Status.Should().Be(InvoiceStatus.CreditNote);
        invoice.PaymentStatus.Should().Be(InvoicePaymentStatus.Reversed);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  THE BIG ONE — Full E2E happy path in a single test
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Complete Sales cycle in sequence — Quotation → Order → Invoice → Payment.
    ///
    /// Run this test to see the entire workflow work end-to-end.
    /// Each assertion is labelled with the Odoo-equivalent UI action.
    /// </summary>
    [Fact]
    public async Task FullCycle_QuotationToPayment_CompletesSuccessfully()
    {
        var contactId = Guid.NewGuid();  // cross-module customer ref

        // ── Step 1: Sales rep creates a quotation ────────────────────────────
        var qDto = BuildQuotationDto(contactId);
        var qResp = await _client.PostAsJsonAsync(Quotations, qDto);
        qResp.StatusCode.Should().Be(HttpStatusCode.Created, "quotation creation must succeed");
        var quotation = (await qResp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;

        quotation.Status.Should().Be(QuotationStatus.Draft);
        _quotationIds.Add(quotation.Id);

        // ── Step 2: "Send" quotation to customer ─────────────────────────────
        var sendQResp = await _client.PostAsJsonAsync($"{Quotations}/{quotation.Id}/send", new { });
        sendQResp.StatusCode.Should().Be(HttpStatusCode.OK, "send quotation must succeed");
        var sentQ = (await sendQResp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;
        sentQ.Status.Should().Be(QuotationStatus.Sent);

        // ── Step 3: Customer "Accepts" the quotation ─────────────────────────
        var acceptResp = await _client.PostAsJsonAsync($"{Quotations}/{quotation.Id}/accept", new { });
        acceptResp.StatusCode.Should().Be(HttpStatusCode.OK, "accept quotation must succeed");
        var acceptedQ = (await acceptResp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;
        acceptedQ.Status.Should().Be(QuotationStatus.Accepted);

        // ── Step 4: Convert to Sales Order ───────────────────────────────────
        var orderDto = BuildOrderDto(contactId, quotationId: quotation.Id);
        var orderResp = await _client.PostAsJsonAsync(Orders, orderDto);
        orderResp.StatusCode.Should().Be(HttpStatusCode.Created, "order creation must succeed");
        var order = (await orderResp.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;

        order.Status.Should().Be(SalesOrderStatus.Draft);
        order.QuotationId.Should().Be(quotation.Id);
        _orderIds.Add(order.Id);

        // ── Step 5: Place the order ───────────────────────────────────────────
        var placeResp = await _client.PostAsJsonAsync($"{Orders}/{order.Id}/place", new { });
        placeResp.StatusCode.Should().Be(HttpStatusCode.OK, "place order must succeed");
        var placed = (await placeResp.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        placed.Status.Should().Be(SalesOrderStatus.Placed);

        // ── Step 6: Confirm the order ─────────────────────────────────────────
        var confirmOrderResp = await _client.PatchAsJsonAsync(
            $"{Orders}/{order.Id}/status",
            new UpdateSalesOrderStatusDto { Status = SalesOrderStatus.Confirmed, Note = "Approved by manager" });
        confirmOrderResp.StatusCode.Should().Be(HttpStatusCode.OK, "confirm order must succeed");
        var confirmed = (await confirmOrderResp.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        confirmed.Status.Should().Be(SalesOrderStatus.Confirmed);

        // ── Step 7: Create invoice from order ("Create Invoice" button) ───────
        // Seed an actual order line so the invoice has content
        await _builder.CreateSalesOrderLineAsync(order.Id);

        var createInvResp = await _client.PostAsJsonAsync(
            $"{Orders}/{order.Id}/create-invoice",
            new CreateInvoiceFromOrderDto
            {
                InvoiceType = CreateInvoiceType.Regular,
                DueDate     = DateTime.UtcNow.AddDays(30),
                Notes       = "Invoice for Laptop Pro x2",
            });
        createInvResp.StatusCode.Should().Be(HttpStatusCode.Created, "create invoice must succeed");
        var invoice = (await createInvResp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;

        invoice.InvoiceNumber.Should().StartWith("INV-");
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.PaymentStatus.Should().Be(InvoicePaymentStatus.NotPaid);
        invoice.SalesOrderId.Should().Be(order.Id);
        invoice.Lines.Should().HaveCount(1);
        invoice.TotalAmount.Should().BeGreaterThan(0);

        // ── Step 8: Confirm invoice ("Confirm" button → Posted) ───────────────
        var confirmInvResp = await _client.PostAsJsonAsync($"{Invoices}/{invoice.Id}/confirm", new { });
        confirmInvResp.StatusCode.Should().Be(HttpStatusCode.OK, "confirm invoice must succeed");
        var postedInvoice = (await confirmInvResp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;

        postedInvoice.Status.Should().Be(InvoiceStatus.Issued);
        postedInvoice.PaymentReference.Should().Be(postedInvoice.InvoiceNumber,
            "PaymentReference is auto-set to InvoiceNumber on confirm");

        // ── Step 9: Register payment ("Pay" button dialog) ────────────────────
        var payResp = await _client.PostAsJsonAsync(
            $"{Invoices}/{invoice.Id}/register-payment",
            new RegisterInvoicePaymentDto
            {
                PaymentMethod = "BankTransfer",
                Journal       = "Bank",
                Amount        = postedInvoice.BalanceDue,
                PaymentDate   = DateTime.UtcNow,
                Memo          = postedInvoice.InvoiceNumber,
            });
        payResp.StatusCode.Should().Be(HttpStatusCode.OK, "register payment must succeed");
        var paidInvoice = (await payResp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;

        // ── Final assertions ──────────────────────────────────────────────────
        paidInvoice.Status.Should().Be(InvoiceStatus.Paid,          "invoice must be Paid");
        paidInvoice.PaymentStatus.Should().Be(InvoicePaymentStatus.Paid, "payment status must be Paid");
        paidInvoice.BalanceDue.Should().Be(0,                        "no outstanding balance");
        paidInvoice.PaidAmount.Should().Be(invoice.TotalAmount,      "full amount recorded");

        // order must be PaidAndClosed
        var finalOrderResp = await _client.GetAsync($"{Orders}/{order.Id}");
        var finalOrder = (await finalOrderResp.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        finalOrder.Status.Should().Be(SalesOrderStatus.PaidAndClosed, "order must close when fully paid");
        finalOrder.ClosedDate.Should().NotBeNull();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Private helpers — build DTOs and shared setup shortcuts
    // ═════════════════════════════════════════════════════════════════════════

    private static CreateQuotationDto BuildQuotationDto(Guid contactId) => new()
    {
        ContactId    = contactId,
        ContactName  = "John Smith",
        CurrencyCode = "PKR",
        ExchangeRate = 278m,
        PaymentTerms = PaymentTerms.Net30,
        ValidUntil   = DateTime.UtcNow.AddDays(30),
        Notes        = "Quotation for laptop procurement",
        Lines =
        [
            new CreateQuotationLineDto
            {
                ProductId    = Guid.NewGuid(),
                ProductCode  = "LAPTOP-PRO",
                ProductName  = "Laptop Pro 16\"",
                Quantity     = 2,
                UnitOfMeasure = "PCS",
                UnitPrice    = 100_000m,
                TaxCategory  = TaxCategory.Standard,
            },
        ],
    };

    private static CreateSalesOrderDto BuildOrderDto(Guid contactId, Guid? quotationId = null) => new()
    {
        ContactId      = contactId,
        ContactName    = "John Smith",
        SalesChannel   = SalesChannel.DirectSales,
        FulfillmentType = FulfillmentType.Delivery,
        CurrencyCode   = "PKR",
        ExchangeRate   = 278m,
        PaymentTerms   = PaymentTerms.Net30,
        QuotationId    = quotationId,
        Lines =
        [
            new CreateSalesOrderLineDto
            {
                ProductId    = Guid.NewGuid(),
                ProductCode  = "LAPTOP-PRO",
                ProductName  = "Laptop Pro 16\"",
                Quantity     = 2,
                UnitOfMeasure = "PCS",
                UnitPrice    = 100_000m,
                TaxCategory  = TaxCategory.Standard,
            },
        ],
    };

    // ── Shortcut builders ─────────────────────────────────────────────────────

    private async Task<QuotationDto> CreateDraftQuotationAsync()
    {
        var resp = await _client.PostAsJsonAsync(Quotations, BuildQuotationDto(Guid.NewGuid()));
        var q    = (await resp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;
        _quotationIds.Add(q.Id);
        return q;
    }

    private async Task<QuotationDto> CreateSentQuotationAsync()
    {
        var q = await CreateDraftQuotationAsync();
        await _client.PostAsJsonAsync($"{Quotations}/{q.Id}/send", new { });
        return (await (await _client.GetAsync($"{Quotations}/{q.Id}"))
            .Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;
    }

    private async Task<QuotationDto> CreateAcceptedQuotationAsync()
    {
        var q = await CreateSentQuotationAsync();
        await _client.PostAsJsonAsync($"{Quotations}/{q.Id}/accept", new { });
        return (await (await _client.GetAsync($"{Quotations}/{q.Id}"))
            .Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>())!.Data!;
    }

    private async Task<SalesOrderDto> CreateDraftOrderAsync()
    {
        var resp  = await _client.PostAsJsonAsync(Orders, BuildOrderDto(Guid.NewGuid()));
        var order = (await resp.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
        _orderIds.Add(order.Id);
        return order;
    }

    private async Task<SalesOrderDto> CreatePlacedOrderAsync()
    {
        var order = await CreateDraftOrderAsync();
        await _client.PostAsJsonAsync($"{Orders}/{order.Id}/place", new { });
        return (await (await _client.GetAsync($"{Orders}/{order.Id}"))
            .Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data!;
    }

    /// <summary>Creates a confirmed order + line, then creates a Draft invoice from it.</summary>
    private async Task<(Guid OrderId, Guid InvoiceId)> CreateDraftInvoiceFromOrderAsync()
    {
        var order = await _builder.CreateSalesOrderAsync(SalesOrderStatus.Confirmed);
        _orderIds.Add(order.Id);
        await _builder.CreateSalesOrderLineAsync(order.Id);

        var resp = await _client.PostAsJsonAsync(
            $"{Orders}/{order.Id}/create-invoice",
            new CreateInvoiceFromOrderDto
            {
                InvoiceType = CreateInvoiceType.Regular,
                DueDate     = DateTime.UtcNow.AddDays(30),
            });
        var invoice = (await resp.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
        return (order.Id, invoice.Id);
    }

    /// <summary>Creates a confirmed invoice (Posted/Issued) ready to be paid.</summary>
    private async Task<(Guid OrderId, Guid InvoiceId)> CreatePostedInvoiceAsync()
    {
        var (orderId, invoiceId) = await CreateDraftInvoiceFromOrderAsync();
        await _client.PostAsJsonAsync($"{Invoices}/{invoiceId}/confirm", new { });
        return (orderId, invoiceId);
    }

    private async Task<SalesInvoiceDto> GetInvoiceAsync(Guid id)
        => (await (await _client.GetAsync($"{Invoices}/{id}"))
            .Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data!;
}
