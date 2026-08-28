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
}
