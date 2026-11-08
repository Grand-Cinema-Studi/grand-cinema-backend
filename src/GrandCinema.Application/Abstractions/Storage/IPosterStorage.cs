namespace GrandCinema.Application.Abstractions.Storage;

public interface IPosterStorage
{
    Task<string> SaveAsync(byte[] content, PosterFormat format, CancellationToken cancellationToken = default);
}
