using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Auth;

[Collection(ApiCollection.Name)]
public class DeleteCurrentUserEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public DeleteCurrentUserEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_signed_in_user_deletes_their_account_Then_204_and_the_account_no_longer_exists()
    {
        // Arrange
        var email = GrandCinemaApiFactory.NewEmail();
        using var client = _api.CreateVisitorClient();
        var token = await GrandCinemaApiFactory.RegisterCustomerAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync("/api/users/me");

        // Assert: no profile behind the token any more, and no way to sign in again
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync("/api/users/me")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var signIn = await client.PostAsJsonAsync("/api/tokens", new { email, password = GrandCinemaApiFactory.Password });
        signIn.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_account_was_already_deleted_Then_404_user_not_found()
    {
        // Arrange: the token stays valid until it expires, but the account is gone
        using var client = _api.CreateVisitorClient();
        var token = await GrandCinemaApiFactory.RegisterCustomerAsync(client, GrandCinemaApiFactory.NewEmail());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await client.DeleteAsync("/api/users/me");

        // Act
        var response = await client.DeleteAsync("/api/users/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/user-not-found");
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_no_token_is_sent_Then_401()
    {
        // Arrange
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.DeleteAsync("/api/users/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
