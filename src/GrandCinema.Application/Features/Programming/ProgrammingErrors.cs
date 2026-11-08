using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming;

public static class ProgrammingErrors
{
    public static readonly Error ScreeningNotFound =
        Error.NotFound("screening-not-found", "The screening does not exist.");

    public static readonly Error RoomNotFound =
        Error.NotFound("room-not-found", "The room does not exist.");

    // Two screenings never need the same room at the same time, cleaning included.
    public static readonly Error RoomNotAvailable =
        Error.Conflict("room-not-available", "The room is already used at that time, cleaning included.");

    // Same title (whatever the case) and same release date: the movie is already in the catalog
    public static readonly Error MovieAlreadyExists =
        Error.Conflict("movie-already-exists", "This movie is already in the catalog.");

    // A screening that has started is part of the history: it no longer changes.
    public static readonly Error ScreeningStarted =
        Error.Conflict("screening-started", "The screening has already started.");

    // The manager is warned, then sends the same request again with ?confirmed=true.
    public static readonly Error ScreeningHasReservations =
        Error.Conflict("screening-has-reservations", "Seats of this screening are held or sold: confirm to go on.");

}
