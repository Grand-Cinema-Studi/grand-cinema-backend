using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Auth;

[Collection(ApiCollection.Name)]
public class CurrentUserEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public CurrentUserEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_no_token_is_sent_Then_401()
    {
        // Arrange
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_token_is_valid_Then_200_with_the_profile_of_its_owner()
    {
        // Arrange
        var email = GrandCinemaApiFactory.NewEmail();
        using var client = _api.CreateVisitorClient();
        var token = await GrandCinemaApiFactory.RegisterCustomerAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();
        user.GetProperty("email").GetString().ShouldBe(email);
        user.GetProperty("firstName").GetString().ShouldBe("Léa");
        user.GetProperty("role").GetString().ShouldBe("Customer");
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_role_inside_the_token_is_changed_Then_401()
    {
        // Arrange: a customer turns their own token into a manager token and keeps the signature
        using var client = _api.CreateVisitorClient();
        var token = await GrandCinemaApiFactory.RegisterCustomerAsync(client, GrandCinemaApiFactory.NewEmail());
        var parts = token.Split('.');
        var payload = Base64UrlEncoder.Decode(parts[1]);
        payload.ShouldContain("\"role\":\"Customer\"");
        var forgedPayload = Base64UrlEncoder.Encode(payload.Replace("\"role\":\"Customer\"", "\"role\":\"Manager\""));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{parts[0]}.{forgedPayload}.{parts[2]}");

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_account_was_deleted_after_sign_in_Then_404_user_not_found()
    {
        // Arrange: the token is still valid, but the account no longer exists
        var email = GrandCinemaApiFactory.NewEmail();
        using var client = _api.CreateVisitorClient();
        var token = await GrandCinemaApiFactory.RegisterCustomerAsync(client, email);
        await using (var db = _api.CreateDbContext())
        {
            await db.Users.Where(u => u.Email == email).ExecuteDeleteAsync();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/user-not-found");
    }
}
