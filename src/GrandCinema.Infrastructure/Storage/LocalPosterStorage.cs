using GrandCinema.Application.Abstractions.Storage;

namespace GrandCinema.Infrastructure.Storage;

// Developer's computer only: the posters are files of a local folder, served by the API (Program.cs)
public sealed class LocalPosterStorage : IPosterStorage
{
    private readonly string _folder;

    public LocalPosterStorage(PostersOptions options)
    {
        _folder = options.LocalFolder;
    }

    public async Task<string> SaveAsync(byte[] content, PosterFormat format, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_folder);
        var name = $"{Guid.NewGuid():N}{format.Extension}";
        await File.WriteAllBytesAsync(Path.Combine(_folder, name), content, cancellationToken);
        return $"/posters/{name}";
    }
}
