namespace GrandCinema.Application.Features.Programming.Commands;

// The fields of CreateMovieCommand; the poster is optional: without it, the poster does not change
public sealed record class UpdateMovieCommand(Guid MovieId, string? Title, string? Synopsis, int? DurationMinutes, string? Genre, DateOnly? ReleaseDate, byte[]? Poster);
