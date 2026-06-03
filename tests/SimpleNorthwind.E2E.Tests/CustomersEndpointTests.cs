using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class CustomersEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task CustomerCrud_FullFlow_Succeeds()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        // Create → 201
        var create = await client.PostAsJsonAsync("/api/customers",
            new { companyName = "CRUD Co.", contactNumber = "02-0000-0000", contactTitle = "CEO" });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await create.ReadJsonAsync()).GetProperty("customerId").GetInt32();

        // Get → 200
        var get = await client.GetAsync($"/api/customers/{id}");
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await get.ReadJsonAsync()).GetProperty("companyName").GetString().ShouldBe("CRUD Co.");

        // Update → 200
        var update = await client.PutAsJsonAsync($"/api/customers/{id}", new
        {
            companyName = "CRUD Co. Renamed",
            contactNumber = "02-1111-1111",
            contactTitle = "CTO",
            isOutContacted = true,
            outContactedDate = (string?)null,
        });
        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await update.ReadJsonAsync();
        updated.GetProperty("companyName").GetString().ShouldBe("CRUD Co. Renamed");
        updated.GetProperty("isOutContacted").GetBoolean().ShouldBeTrue();

        // Delete → 204
        (await client.DeleteAsync($"/api/customers/{id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Get again → 404
        (await client.GetAsync($"/api/customers/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCustomer_NotFound_Returns404()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCustomer_NoChanges_Returns400()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/customers",
            new { companyName = "NoOp Co.", contactNumber = "02-3333-3333", contactTitle = "Owner" });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await create.ReadJsonAsync()).GetProperty("customerId").GetInt32();

        // 送出與現況完全相同的欄位 → 未修改 → 400
        var update = await client.PutAsJsonAsync($"/api/customers/{id}", new
        {
            companyName = "NoOp Co.",
            contactNumber = "02-3333-3333",
            contactTitle = "Owner",
            isOutContacted = false,
            outContactedDate = (string?)null,
        });

        update.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
