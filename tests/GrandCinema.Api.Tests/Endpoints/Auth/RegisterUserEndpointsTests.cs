using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Auth;

[Collection(ApiCollection.Name)]
public class RegisterUserEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public RegisterUserEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_visitor_signs_up_with_valid_data_Then_201_with_an_access_token_for_a_customer_account()
    {
        // Arrange
        var email = GrandCinemaApiFactory.NewEmail();
        using var client = _api.CreateVisitorClient();

        // Act: the address is typed with capitals and spaces
        var response = await client.PostAsJsonAsync("/api/users", new { email = $" {email.ToUpperInvariant()} ", password = GrandCinemaApiFactory.Password, firstName = "Léa", lastName = "Martin" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.ShouldBe("/api/users/me");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        var user = body.GetProperty("user");
        user.GetProperty("email").GetString().ShouldBe(email);
        user.GetProperty("role").GetString().ShouldBe("Customer");
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_fields_are_invalid_Then_400_with_a_problem_details_listing_the_invalid_fields()
    {
        // Arrange
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/users", new { email = "not-an-email", password = "too-short", firstName = "Léa", lastName = "Martin" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/validation");
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).ShouldBe(new[] { "email", "password" }, ignoreOrder: true);
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_email_already_has_an_account_Then_409_even_written_with_capitals()
    {
        // Arrange
        var email = GrandCinemaApiFactory.NewEmail();
        using var client = _api.CreateVisitorClient();
        await GrandCinemaApiFactory.RegisterCustomerAsync(client, email);

        // Act
        var response = await client.PostAsJsonAsync("/api/users", new { email = email.ToUpperInvariant(), password = GrandCinemaApiFactory.Password, firstName = "Léo", lastName = "Durand" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/email-already-used");
    }
}
