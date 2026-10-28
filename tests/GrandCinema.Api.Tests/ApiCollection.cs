namespace GrandCinema.Api.Tests;

// All the API tests share one API and one PostgreSQL container, started once:
// the test classes of this collection run one after the other, and each test creates its own data.
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<GrandCinemaApiFactory>
{
    public const string Name = "API";
}
