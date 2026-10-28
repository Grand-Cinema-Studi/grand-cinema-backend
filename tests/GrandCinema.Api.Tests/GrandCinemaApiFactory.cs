using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GrandCinema.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GrandCinema.Api.Tests;

// The real API (same Program.cs, middlewares and services), started in memory without any port, on a disposable PostgreSQL.
public sealed class GrandCinemaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Password = "Correct-horse-2026";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16").Build();
    private int _visitorCount;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development": no user secrets, no developer exception page; only the settings below and appsettings.json.
        builder.UseEnvironment("Testing");
        // Host settings reach Program.cs as arguments: known when AddInfrastructure reads the connection string.
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "signing-key-of-the-api-tests-only-0123456789");
        // As on the acceptance environment, where the trainers use Swagger.
        builder.UseSetting("Swagger:Enabled", "true");
    }

    public GrandCinemaDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<GrandCinemaDbContext>().UseNpgsql(_database.GetConnectionString()).Options);

    // Each client is a different visitor, as seen behind CloudFront (X-Forwarded-For):
    // the rate limiting (10 attempts per minute and per visitor) never mixes two tests.
    public HttpClient CreateVisitorClient()
    {
        var visitor = Interlocked.Increment(ref _visitorCount);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", new IPAddress([10, 0, (byte)(visitor >> 8), (byte)visitor]).ToString());
        return client;
    }

    public static string NewEmail() => $"{Guid.NewGuid()}@example.com";

    // Creates a customer account through the API, as the front does, and returns its access token.
    public static async Task<string> RegisterCustomerAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/users", new { email, password = Password, firstName = "Léa", lastName = "Martin", acceptPrivacyPolicy = true });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    // WebApplicationFactory already has a DisposeAsync returning ValueTask: the one of xUnit is implemented explicitly.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }
}
