using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Auth;

[Collection(ApiCollection.Name)]
public class AuthenticateUserEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public AuthenticateUserEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_email_and_the_password_are_right_Then_200_with_an_access_token()
    {
        // Arrange
        var email = GrandCinemaApiFactory.NewEmail();
        using var client = _api.CreateVisitorClient();
        await GrandCinemaApiFactory.RegisterCustomerAsync(client, email);

        // Act
        var response = await client.PostAsJsonAsync("/api/tokens", new { email, password = GrandCinemaApiFactory.Password });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("expiresAt").GetDateTime().ShouldBeGreaterThan(DateTime.UtcNow);
        body.GetProperty("user").GetProperty("email").GetString().ShouldBe(email);
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_password_is_wrong_or_the_email_unknown_Then_the_same_401_answer()
    {
        // Arrange
        var email = GrandCinemaApiFactory.NewEmail();
        using var client = _api.CreateVisitorClient();
        await GrandCinemaApiFactory.RegisterCustomerAsync(client, email);

        // Act
        var wrongPassword = await client.PostAsJsonAsync("/api/tokens", new { email, password = "Wrong-password-2026" });
        var unknownEmail = await client.PostAsJsonAsync("/api/tokens", new { email = GrandCinemaApiFactory.NewEmail(), password = GrandCinemaApiFactory.Password });

        // Assert: nothing tells which of the two is wrong
        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var first = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var second = await unknownEmail.Content.ReadFromJsonAsync<JsonElement>();
        first.GetProperty("type").GetString().ShouldBe("/errors/invalid-credentials");
        second.GetProperty("type").GetString().ShouldBe(first.GetProperty("type").GetString());
        second.GetProperty("title").GetString().ShouldBe(first.GetProperty("title").GetString());
    }
}
