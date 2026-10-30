namespace GrandCinema.Application.Abstractions.Results;

public static class MovieErrors
{
    public static readonly Error MovieNotFound =
        Error.NotFound("Movie-not-found", "The movie does not exist.");
}
