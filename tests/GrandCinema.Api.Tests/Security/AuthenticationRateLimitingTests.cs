using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace GrandCinema.Api.Tests.Security;

[Collection(ApiCollection.Name)]
public class AuthenticationRateLimitingTests
{
    private readonly GrandCinemaApiFactory _api;

    public AuthenticationRateLimitingTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("US", "2.1")]
    [Trait("Category", "Integration")]
    public async Task When_a_visitor_makes_more_than_10_attempts_in_a_minute_Then_they_get_429_and_other_visitors_do_not()
    {
        // Arrange: ten wrong attempts from the same visitor
        using var attacker = _api.CreateVisitorClient();
        using var otherVisitor = _api.CreateVisitorClient();
        var attempt = new { email = GrandCinemaApiFactory.NewEmail(), password = "Wrong-password-2026" };
        for (var i = 0; i < 10; i++)
        {
            var allowed = await attacker.PostAsJsonAsync("/api/tokens", attempt);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Act
        var eleventh = await attacker.PostAsJsonAsync("/api/tokens", attempt);
        var fromAnotherVisitor = await otherVisitor.PostAsJsonAsync("/api/tokens", attempt);

        // Assert: counted per visitor (X-Forwarded-For, as behind CloudFront), not for all the visitors together
        eleventh.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        fromAnotherVisitor.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
