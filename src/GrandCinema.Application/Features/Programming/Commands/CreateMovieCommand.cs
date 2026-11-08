namespace GrandCinema.Application.Features.Programming.Commands;

// The fields as typed by the manager, checked by the handler; the poster as read by the endpoint, at most one byte beyond the limit
public sealed record class CreateMovieCommand(string? Title, string? Synopsis, int? DurationMinutes, string? Genre, DateOnly? ReleaseDate, byte[]? Poster);
