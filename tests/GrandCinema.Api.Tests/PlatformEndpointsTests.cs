using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace GrandCinema.Api.Tests;

[Collection(ApiCollection.Name)]
public class PlatformEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public PlatformEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task When_the_deployment_checks_the_API_Then_health_answers_Healthy_without_a_token()
    {
        // Arrange
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task When_Swagger_is_enabled_Then_the_OpenAPI_document_offers_the_bearer_token_and_no_fixed_server()
    {
        // Arrange
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var bearer = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        bearer.GetProperty("type").GetString().ShouldBe("http");
        bearer.GetProperty("scheme").GetString().ShouldBe("bearer");
        // No server address: Swagger calls the address it is shown on (CloudFront), never the internal server name
        (document.TryGetProperty("servers", out var servers) && servers.GetArrayLength() > 0).ShouldBeFalse();
    }
}
