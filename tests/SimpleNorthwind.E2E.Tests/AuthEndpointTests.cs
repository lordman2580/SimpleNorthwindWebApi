using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class AuthEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { employeeId = AuthHelper.SeedEmployeeId, password = AuthHelper.SeedPassword });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await response.ReadJsonAsync();
        root.GetProperty("token").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("expiresAt").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { employeeId = AuthHelper.SeedEmployeeId, password = "wrong-password" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
