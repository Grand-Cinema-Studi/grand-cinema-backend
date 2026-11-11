using GrandCinema.Application.Abstractions.Storage;

namespace GrandCinema.Api.Tests;

public sealed class FakePosterStorage : IPosterStorage
{
    public Task<string> SaveAsync(byte[] content, PosterFormat format, CancellationToken cancellationToken = default) =>
        Task.FromResult($"/posters/{Guid.NewGuid():N}{format.Extension}");
}
