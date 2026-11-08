using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming;

public static class MovieRules
{
    public const int TitleMaxLength = 200;
    public const int SynopsisMaxLength = 2000;
    // From a short film to the longest features a cinema shows
    public const int MaxDurationMinutes = 600;

    public static Error? Validate(string? title, string? synopsis, int? durationMinutes, string? genre, DateOnly? releaseDate, byte[]? poster, bool posterRequired)
    {
        var errors = new Dictionary<string, string[]>();
        Text(errors, "title", title, TitleMaxLength);
        Text(errors, "synopsis", synopsis, SynopsisMaxLength);
        if (genre is null || !MovieGenres.All.Contains(genre.Trim()))
        {
            errors["genre"] = ["The genre is one of the list given by GET /api/programming (genres)."];
        }
        if (durationMinutes is not (>= 1 and <= MaxDurationMinutes))
        {
            errors["durationMinutes"] = [$"The duration is between 1 and {MaxDurationMinutes} minutes."];
        }
        if (releaseDate is null)
        {
            errors["releaseDate"] = ["The release date is expected: 2026-11-30."];
        }
        if (Poster(poster, posterRequired) is { } posterError)
        {
            errors["poster"] = [posterError];
        }

        return errors.Count == 0 ? null : Error.Validation(errors);
    }

    private static void Text(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
    {
        var length = value?.Trim().Length ?? 0;
        if (length == 0 || length > maxLength)
        {
            errors[field] = [$"Between 1 and {maxLength} characters."];
        }
    }

    private static string? Poster(byte[]? poster, bool required)
    {
        if (poster is null)
        {
            return required ? "The poster is required." : null;
        }
        if (poster.Length > PosterRules.MaxBytes)
        {
            return "The poster weighs 2 MB at most.";
        }
        return PosterRules.Detect(poster) is null ? "The poster is a JPEG, PNG or WebP image." : null;
    }
}
