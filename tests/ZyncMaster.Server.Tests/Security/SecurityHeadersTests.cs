using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ZyncMaster.Server.Tests.Security;

// L3: every response carries the baseline security headers (clickjacking, MIME sniffing,
// referrer leakage). Applied at the top of the pipeline so static panel/UI assets get them
// too. HSTS / HttpsRedirection are intentionally NOT asserted here: the test host runs as
// Development over plain http, where they are gated off by design.
public class SecurityHeadersTests : IClassFixture<ServerTestFactory>
{
    private readonly ServerTestFactory _factory;

    public SecurityHeadersTests(ServerTestFactory factory) => _factory = factory;

    private static string? Header(System.Net.Http.HttpResponseMessage resp, string name) =>
        resp.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    [Fact]
    public async Task Static_panel_response_carries_security_headers()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        Header(resp, "X-Frame-Options").Should().Be("DENY");
        Header(resp, "X-Content-Type-Options").Should().Be("nosniff");
        Header(resp, "Referrer-Policy").Should().Be("no-referrer");
    }

    [Fact]
    public async Task Api_response_carries_security_headers()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/health");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        Header(resp, "X-Frame-Options").Should().Be("DENY");
        Header(resp, "X-Content-Type-Options").Should().Be("nosniff");
        Header(resp, "Referrer-Policy").Should().Be("no-referrer");
    }
}
