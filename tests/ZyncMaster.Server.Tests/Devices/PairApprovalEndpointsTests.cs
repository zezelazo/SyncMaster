using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ZyncMaster.Server.Tests.Devices;

public class PairApprovalEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PairApprovalEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static WebApplicationFactory<Program> Build(bool seedAccount) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IConnectedAccountStore>();
                services.AddSingleton<IConnectedAccountStore>(_ =>
                {
                    var store = new DataProtectionConnectedAccountStore(DataProtectionProvider.Create("tests"));
                    if (seedAccount)
                        store.SetAsync("default", "rt").GetAwaiter().GetResult();
                    return store;
                });
            });
        });

    private static HttpClient NonRedirectingClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<string> SeedPendingAsync(WebApplicationFactory<Program> factory, string code, string name)
    {
        var devices = factory.Services.GetRequiredService<IDeviceStore>();
        await devices.SavePendingAsync(new PendingPairing
        {
            PairingId = Guid.NewGuid().ToString("N"),
            DeviceName = name,
            Code = code,
            CreatedUtc = DateTimeOffset.UtcNow,
        });
        return code;
    }

    [Fact]
    public async Task No_connected_account_redirects_to_connect_with_returnTo()
    {
        var factory = Build(seedAccount: false);
        var client = NonRedirectingClient(factory);

        var resp = await client.GetAsync("/pair?code=ABC123");

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = resp.Headers.Location!.ToString();
        location.Should().StartWith("/connect?returnTo=");
        Uri.UnescapeDataString(location).Should().Contain("/pair?code=ABC123");
    }

    [Fact]
    public async Task Connected_account_shows_device_name_code_and_approve()
    {
        var factory = Build(seedAccount: true);
        await SeedPendingAsync(factory, "ABC123", "Zeze Laptop");
        var client = NonRedirectingClient(factory);

        var resp = await client.GetAsync("/pair?code=ABC123");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Zeze Laptop");
        html.Should().Contain("ABC123");
        html.Should().Contain("/api/devices/approve");
    }

    [Fact]
    public async Task Unknown_code_shows_invalid_message()
    {
        var factory = Build(seedAccount: true);
        var client = NonRedirectingClient(factory);

        var resp = await client.GetAsync("/pair?code=NOPE99");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("not valid");
    }

    [Fact]
    public async Task Already_approved_code_shows_approved_message()
    {
        var factory = Build(seedAccount: true);
        var devices = factory.Services.GetRequiredService<IDeviceStore>();
        await devices.SavePendingAsync(new PendingPairing
        {
            PairingId = Guid.NewGuid().ToString("N"),
            DeviceName = "Tablet",
            Code = "DONE11",
            Approved = true,
            CreatedUtc = DateTimeOffset.UtcNow,
        });
        var client = NonRedirectingClient(factory);

        var resp = await client.GetAsync("/pair?code=DONE11");

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("already approved");
    }

    [Fact]
    public async Task Missing_code_with_account_shows_no_code_message()
    {
        var factory = Build(seedAccount: true);
        var client = NonRedirectingClient(factory);

        var resp = await client.GetAsync("/pair");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("No pairing code");
    }

    [Fact]
    public async Task Approve_via_existing_endpoint_marks_pending_approved()
    {
        var factory = Build(seedAccount: true);
        await SeedPendingAsync(factory, "GO1234", "Phone");
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/devices/approve", new { code = "GO1234" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var devices = factory.Services.GetRequiredService<IDeviceStore>();
        var pending = await devices.GetPendingByCodeAsync("GO1234");
        pending!.Approved.Should().BeTrue();
    }
}
