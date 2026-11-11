using GrandCinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GrandCinema.Infrastructure.Tests;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public GrandCinemaDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<GrandCinemaDbContext>().UseNpgsql(_container.GetConnectionString()).Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
