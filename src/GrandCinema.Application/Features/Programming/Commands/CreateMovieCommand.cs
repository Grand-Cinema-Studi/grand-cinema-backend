namespace GrandCinema.Application.Features.Programming.Commands;

public sealed record class CreateMovieCommand(string? Title, string? Synopsis, int? DurationMinutes, string? Genre, DateOnly? ReleaseDate, byte[]? Poster);
