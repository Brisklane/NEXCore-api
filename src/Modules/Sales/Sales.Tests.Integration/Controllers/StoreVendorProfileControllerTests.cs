namespace Sales.Tests.Integration.Controllers;

[Collection(SalesTestCollection.Name)]
public class StoreVendorProfileControllerTests : IAsyncLifetime
{
    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;
    private readonly List<Guid> _createdStoreIds = [];

    public StoreVendorProfileControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // StoreVendorProfile.StoreId is Cascade — deleting the store removes the profile
        foreach (var id in _createdStoreIds)
            await _builder.DeletePosStoreAsync(id);
        _client.Dispose();
    }

    private static string VendorProfileUrl(Guid storeId) =>
        $"api/sales/PosStore/{storeId}/vendor-profile";

    // ── GET — profile does not exist ──────────────────────────────────────────

    [Fact]
    public async Task Get_NoProfileExists_Returns404()
    {
        var store = await _builder.CreatePosStoreAsync("Profile Not Found Store");
        _createdStoreIds.Add(store.Id);

        var response = await _client.GetAsync(VendorProfileUrl(store.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT — create (first upsert) ── Minimal ────────────────────────────────

    [Fact]
    public async Task Upsert_WithMinimalPayload_Creates201AndProfile()
    {
        var store = await _builder.CreatePosStoreAsync("Vendor Profile Minimal Store");
        _createdStoreIds.Add(store.Id);

        var payload = new UpsertVendorProfileDto
        {
            OwnerName = "Fatima Khan",
        };

        var response = await _client.PutAsJsonAsync(VendorProfileUrl(store.Id), payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreVendorProfileDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.OwnerName.Should().Be("Fatima Khan");
        body.Data.StoreId.Should().Be(store.Id);
        body.Data.OnboardingStatus.Should().Be(VendorOnboardingStatus.Draft);
    }

    // ── PUT — create (first upsert) ── Maximal ────────────────────────────────

    [Fact]
    public async Task Upsert_WithMaximalPayload_Creates201AndAllFieldsPersisted()
    {
        var store = await _builder.CreatePosStoreAsync("Vendor Profile Maximal Store");
        _createdStoreIds.Add(store.Id);

        var payload = new UpsertVendorProfileDto
        {
            OwnerName = "Ahmed Raza",
            OwnerCnic = "42201-1234567-9",
            OwnerPhone = "0300-1234567",
            OwnerEmail = "ahmed.raza@homechef.pk",
            CnicFrontDocUrl = "https://docs.example.com/cnic-front.jpg",
            CnicBackDocUrl = "https://docs.example.com/cnic-back.jpg",
            BusinessName = "Ahmed's Kitchen",
            BusinessRegistrationNumber = "REG-2026-0042",
            FoodLicenseNumber = "FOOD-LIC-12345",
            FoodLicenseExpiry = new DateOnly(2027, 12, 31),
            FoodLicenseDocUrl = "https://docs.example.com/food-license.pdf",
            BusinessDescription = "Authentic home-cooked Pakistani meals delivered daily",
            BankName = "Meezan Bank",
            BankBranch = "Gulshan Branch",
            AccountTitle = "Ahmed Raza",
            AccountNumber = "0123456789",
            IbanNumber = "PK36MEZN0001234567890001",
            FacebookUrl = "https://facebook.com/ahmedkitchen",
            InstagramUrl = "https://instagram.com/ahmedkitchen",
            TiktokUrl = "https://tiktok.com/@ahmedkitchen",
            WhatsappNumber = "0300-1234567",
            StoreFrontPhotoUrl = "https://cdn.example.com/storefronts/ahmed.jpg",
            KitchenPhotos = ["https://cdn.example.com/kitchens/k1.jpg", "https://cdn.example.com/kitchens/k2.jpg"],
        };

        var response = await _client.PutAsJsonAsync(VendorProfileUrl(store.Id), payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreVendorProfileDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.OwnerName.Should().Be("Ahmed Raza");
        body.Data.OwnerCnic.Should().Be(payload.OwnerCnic);
        body.Data.OwnerEmail.Should().Be(payload.OwnerEmail);
        body.Data.BusinessName.Should().Be("Ahmed's Kitchen");
        body.Data.BusinessRegistrationNumber.Should().Be(payload.BusinessRegistrationNumber);
        body.Data.FoodLicenseNumber.Should().Be(payload.FoodLicenseNumber);
        body.Data.FoodLicenseExpiry.Should().Be(payload.FoodLicenseExpiry);
        body.Data.BankName.Should().Be("Meezan Bank");
        body.Data.AccountNumber.Should().Be(payload.AccountNumber);
        body.Data.IbanNumber.Should().Be(payload.IbanNumber);
        body.Data.FacebookUrl.Should().Be(payload.FacebookUrl);
        body.Data.KitchenPhotos.Should().HaveCount(2);
    }

    // ── GET — profile exists after upsert ─────────────────────────────────────

    [Fact]
    public async Task Get_AfterUpsert_ReturnsProfile()
    {
        var store = await _builder.CreatePosStoreAsync("Get Profile Store");
        _createdStoreIds.Add(store.Id);

        var payload = new UpsertVendorProfileDto { OwnerName = "Sara Malik" };
        await _client.PutAsJsonAsync(VendorProfileUrl(store.Id), payload);

        var response = await _client.GetAsync(VendorProfileUrl(store.Id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreVendorProfileDto>>();
        body!.Data!.OwnerName.Should().Be("Sara Malik");
        body.Data.StoreId.Should().Be(store.Id);
    }

    // ── PUT — second upsert updates existing ──────────────────────────────────

    [Fact]
    public async Task Upsert_SecondCall_Updates200AndProfile()
    {
        var store = await _builder.CreatePosStoreAsync("Update Profile Store");
        _createdStoreIds.Add(store.Id);

        await _client.PutAsJsonAsync(VendorProfileUrl(store.Id), new UpsertVendorProfileDto { OwnerName = "First Name" });

        var updatePayload = new UpsertVendorProfileDto
        {
            OwnerName = "Updated Name",
            OwnerPhone = "0321-9999999",
        };

        var response = await _client.PutAsJsonAsync(VendorProfileUrl(store.Id), updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreVendorProfileDto>>();
        body!.Data!.OwnerName.Should().Be("Updated Name");
        body.Data.OwnerPhone.Should().Be("0321-9999999");
    }

    // ── POST submit ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_WithRequiredFields_TransitionsToSubmitted()
    {
        var store = await _builder.CreatePosStoreAsync("Submit Profile Store");
        _createdStoreIds.Add(store.Id);

        var payload = new UpsertVendorProfileDto
        {
            OwnerName = "Usman Ali",
            OwnerCnic = "35202-9876543-1",
            AccountNumber = "9876543210",
        };
        await _client.PutAsJsonAsync(VendorProfileUrl(store.Id), payload);

        var submitResponse = await _client.PostAsJsonAsync($"{VendorProfileUrl(store.Id)}/submit", new { });

        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await submitResponse.Content.ReadFromJsonAsync<ApiResponse<StoreVendorProfileDto>>();
        body!.Data!.OnboardingStatus.Should().Be(VendorOnboardingStatus.Submitted);
        body.Data.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Submit_WithoutCnic_Returns400()
    {
        var store = await _builder.CreatePosStoreAsync("Submit No CNIC Store");
        _createdStoreIds.Add(store.Id);

        // Create profile without CNIC
        await _client.PutAsJsonAsync(VendorProfileUrl(store.Id),
            new UpsertVendorProfileDto { OwnerName = "No CNIC User", AccountNumber = "1234567890" });

        var submitResponse = await _client.PostAsJsonAsync($"{VendorProfileUrl(store.Id)}/submit", new { });

        submitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_WithoutBankDetails_Returns400()
    {
        var store = await _builder.CreatePosStoreAsync("Submit No Bank Store");
        _createdStoreIds.Add(store.Id);

        // Create profile without bank account or IBAN
        await _client.PutAsJsonAsync(VendorProfileUrl(store.Id),
            new UpsertVendorProfileDto { OwnerName = "No Bank User", OwnerCnic = "35202-1111111-1" });

        var submitResponse = await _client.PostAsJsonAsync($"{VendorProfileUrl(store.Id)}/submit", new { });

        submitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/sales/vendor-applications/pending ────────────────────────────

    [Fact]
    public async Task GetPending_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("api/sales/vendor-applications/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<StoreVendorProfileDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPending_IncludesSubmittedProfile()
    {
        var store = await _builder.CreatePosStoreAsync("Pending Review Store");
        _createdStoreIds.Add(store.Id);

        await _client.PutAsJsonAsync(VendorProfileUrl(store.Id), new UpsertVendorProfileDto
        {
            OwnerName = "Pending Seller",
            OwnerCnic = "42201-5555555-5",
            IbanNumber = "PK36MEZN0001234567890099",
        });
        await _client.PostAsJsonAsync($"{VendorProfileUrl(store.Id)}/submit", new { });

        var response = await _client.GetAsync("api/sales/vendor-applications/pending");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<StoreVendorProfileDto>>>();

        body!.Data.Should().Contain(p => p.StoreId == store.Id);
    }

    // ── POST review ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Review_Approve_TransitionsToApproved()
    {
        var store = await _builder.CreatePosStoreAsync("Approve Review Store");
        _createdStoreIds.Add(store.Id);

        await _client.PutAsJsonAsync(VendorProfileUrl(store.Id), new UpsertVendorProfileDto
        {
            OwnerName = "Approved Seller",
            OwnerCnic = "42201-7777777-7",
            AccountNumber = "7777777777",
        });
        await _client.PostAsJsonAsync($"{VendorProfileUrl(store.Id)}/submit", new { });

        var reviewPayload = new ReviewVendorApplicationDto
        {
            Decision = VendorOnboardingStatus.Approved,
            ReviewNotes = "All documents verified",
        };

        var response = await _client.PostAsJsonAsync($"{VendorProfileUrl(store.Id)}/review", reviewPayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreVendorProfileDto>>();
        body!.Data!.OnboardingStatus.Should().Be(VendorOnboardingStatus.Approved);
        body.Data.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Review_Reject_RequiresRejectionReason()
    {
        var response = await _client.PostAsJsonAsync(
            $"{VendorProfileUrl(_fixture.SharedStore.Id)}/review",
            new ReviewVendorApplicationDto { Decision = VendorOnboardingStatus.Rejected });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Unauthenticated access ────────────────────────────────────────────────

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var anonClient = _fixture.CreateClient();
        var response = await anonClient.GetAsync(VendorProfileUrl(_fixture.SharedStore.Id));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anonClient.Dispose();
    }
}
