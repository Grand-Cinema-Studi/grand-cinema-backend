namespace GrandCinema.Application.Abstractions.Results;

public static class MovieErrors
{
    public static readonly Error MovieNotFound =
        Error.NotFound("movie-not-found", "The movie does not exist.");
}
