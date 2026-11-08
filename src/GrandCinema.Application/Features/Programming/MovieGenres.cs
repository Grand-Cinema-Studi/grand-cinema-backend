namespace GrandCinema.Application.Features.Programming;

// The genres a movie belongs to, as the public filters them (US 2.1): one list, so that "Drame" and "drame" never make two genres
public static class MovieGenres
{
    public static readonly IReadOnlyList<string> All = [];
}
