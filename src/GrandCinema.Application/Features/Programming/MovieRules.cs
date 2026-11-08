using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming;

public static class MovieRules
{
    // As the columns of the table
    public const int TitleMaxLength = 200;
    public const int GenreMaxLength = 50;
    public const int SynopsisMaxLength = 2000;
    // From a short film to the longest features a cinema shows
    public const int MaxDurationMinutes = 600;

    // The fields of a movie card, the poster when one is sent (required to add a movie, optional to change it); null when all are valid
    public static Error? Validate(string? title, string? synopsis, int? durationMinutes, string? genre, DateOnly? releaseDate, byte[]? poster, bool posterRequired) =>
        throw new NotImplementedException("Step 27c");
}
