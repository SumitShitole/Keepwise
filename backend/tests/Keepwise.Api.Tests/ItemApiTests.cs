using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Keepwise.Application.Items;
using Keepwise.Application.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Keepwise.Api.Tests;

public sealed class KeepwiseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:Keepwise",
            "Host=127.0.0.1;Port=5432;Database=keepwise_test;Username=keepwise;Password=keepwise_dev");
        builder.UseSetting("Auth:AllowDevLogin", "true");
    }

    public HttpClient CreateAuthenticatedClient(string email = "sumit@keepwise.app")
    {
        var client = CreateClient();
        var response = client.PostAsJsonAsync("/v1/auth/dev-login", new DevLoginRequest(email, "Sumit")).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var body = response.Content.ReadFromJsonAsync<AuthResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Missing auth response");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return client;
    }
}

public class ItemApiTests : IClassFixture<KeepwiseApiFactory>
{
    private readonly KeepwiseApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public ItemApiTests(KeepwiseApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_is_anonymous()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cannot_list_items_without_token()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/v1/items");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_item_with_warranty_and_read_dashboard()
    {
        var client = _factory.CreateAuthenticatedClient($"washer-{Guid.NewGuid():N}@keepwise.app");
        var create = await client.PostAsJsonAsync("/v1/items", new CreateItemRequest(
            "Samsung Washing Machine",
            null,
            null,
            "Samsung",
            "WW90",
            null,
            new DateOnly(2027, 3, 15),
            42000m,
            "INR",
            "Croma",
            null,
            null,
            new CreateCoverageRequest(
                Keepwise.Domain.CoverageKind.Warranty,
                null, null, null, new DateOnly(2027, 3, 15), 2, Keepwise.Domain.DurationUnit.Years,
                null, null, null, null, null, null)));

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await create.Content.ReadFromJsonAsync<ItemDetailDto>(Json);
        item.Should().NotBeNull();
        item!.Coverages.Should().ContainSingle();
        item.Coverages[0].EndDate.Should().Be(new DateOnly(2029, 3, 15));

        var list = await client.GetAsync("/v1/items?search=washing");
        list.EnsureSuccessStatusCode();

        var dashboard = await client.GetAsync("/v1/dashboard");
        dashboard.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Users_cannot_see_each_others_items()
    {
        var a = _factory.CreateAuthenticatedClient($"a-{Guid.NewGuid():N}@keepwise.app");
        var created = await a.PostAsJsonAsync("/v1/items", new CreateItemRequest(
            "Private Fridge", null, null, null, null, null, null, null, null, null, null, null, null));
        created.EnsureSuccessStatusCode();
        var item = await created.Content.ReadFromJsonAsync<ItemDetailDto>(Json);

        var b = _factory.CreateAuthenticatedClient($"b-{Guid.NewGuid():N}@keepwise.app");
        var response = await b.GetAsync($"/v1/items/{item!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ingest_text_creates_candidate_and_confirm_creates_item()
    {
        var client = _factory.CreateAuthenticatedClient($"ingest-{Guid.NewGuid():N}@keepwise.app");
        var ingest = await client.PostAsJsonAsync("/v1/ingestion/text", new
        {
            text = "Amazon.in order confirmation\nOrder confirmed: Samsung 253L Refrigerator\nOrder 403-1234567-1234567\nPurchased: 14 Aug 2026\nAmount ₹42,999\nWarranty for 1 year\nReturn window 7 days",
            sourceType = 2
        });
        ingest.StatusCode.Should().Be(HttpStatusCode.Accepted);
        using var doc = JsonDocument.Parse(await ingest.Content.ReadAsStringAsync());
        var candidateId = doc.RootElement.GetProperty("candidateId").GetGuid();

        var confirm = await client.PostAsJsonAsync($"/v1/purchase-candidates/{candidateId}/confirm", new { });
        confirm.EnsureSuccessStatusCode();

        var items = await client.GetAsync("/v1/items?search=Refrigerator");
        items.EnsureSuccessStatusCode();
        var body = await items.Content.ReadAsStringAsync();
        body.Should().Contain("Samsung");

        var second = await client.PostAsJsonAsync("/v1/ingestion/text", new
        {
            text = "Amazon.in order confirmation DUPLICATE CHECK\nOrder confirmed: Samsung 253L Refrigerator\nOrder 403-1234567-1234567\nPurchased: 14 Aug 2026\nAmount ₹42,999",
            sourceType = 2
        });
        using var dup = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        dup.RootElement.GetProperty("status").GetInt32().Should().Be((int)Keepwise.Domain.CandidateStatus.Duplicate);

        var itemId = (await confirm.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("itemId").GetGuid();
        var detail = await client.GetAsync($"/v1/items/{itemId}");
        detail.EnsureSuccessStatusCode();
        using var itemDoc = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        var kinds = itemDoc.RootElement.GetProperty("coverages").EnumerateArray()
            .Select(c => c.GetProperty("kind").GetInt32())
            .ToList();
        kinds.Should().Contain((int)Keepwise.Domain.CoverageKind.Warranty);
        kinds.Should().Contain((int)Keepwise.Domain.CoverageKind.ReturnWindow);
    }

    [Fact]
    public async Task Candidates_are_isolated_and_can_be_edited_or_ignored()
    {
        var owner = _factory.CreateAuthenticatedClient($"owner-{Guid.NewGuid():N}@keepwise.app");
        var ingest = await owner.PostAsJsonAsync("/v1/ingestion/text", new
        {
            text = "Croma invoice\nProduct: Sony Bravia\nPurchased: 01 Aug 2026\nAmount ₹55,000",
            sourceType = 2
        });
        ingest.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await ingest.Content.ReadAsStringAsync());
        var candidateId = doc.RootElement.GetProperty("candidateId").GetGuid();

        var stranger = _factory.CreateAuthenticatedClient($"stranger-{Guid.NewGuid():N}@keepwise.app");
        var hidden = await stranger.GetAsync($"/v1/purchase-candidates/{candidateId}");
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var edited = await owner.PutAsJsonAsync($"/v1/purchase-candidates/{candidateId}", new
        {
            isPurchase = true,
            productName = "Sony Bravia 55",
            vendor = "Croma",
            purchaseDate = "2026-08-01",
            amount = 55000,
            currency = "INR",
            warrantyProvenance = 2
        });
        edited.EnsureSuccessStatusCode();
        (await edited.Content.ReadAsStringAsync()).Should().Contain("Sony Bravia 55");

        var ignore = await owner.PostAsJsonAsync($"/v1/purchase-candidates/{candidateId}/ignore", new { });
        ignore.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await owner.GetAsync("/v1/purchase-candidates?status=3");
        list.EnsureSuccessStatusCode();
        (await list.Content.ReadAsStringAsync()).Should().Contain(candidateId.ToString());
    }
}
