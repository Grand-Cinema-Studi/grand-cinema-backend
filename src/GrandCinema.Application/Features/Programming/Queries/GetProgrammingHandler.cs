using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Programming.DTOs;
using GrandCinema.Application.Features.Programming.DTOs.Mappings;

namespace GrandCinema.Application.Features.Programming.Queries;

public sealed class GetProgrammingHandler : IQueryHandler<GetProgrammingQuery, ProgrammingDto>
{

    private readonly IProgrammingRepository _programmingRepository;
    private readonly IScreeningReadRepository _screeningReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetProgrammingHandler(IProgrammingRepository programmingRepository, IScreeningReadRepository screeningReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _screeningReadRepository = screeningReadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<ProgrammingDto> Handle(GetProgrammingQuery query, CancellationToken cancellationToken = default)
    {
        var date = query.Date ?? DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        // Every screening of the day, started ones included: the manager sees the whole day
        var screenings = await _screeningReadRepository.GetScreeningsAsync(date, cancellationToken);
        var takenSeats = await _screeningReadRepository.CountTakenSeatsAsync(screenings.Select(s => s.Id).ToList(), _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        var movies = await _programmingRepository.GetMoviesAsync(cancellationToken);
        var rooms = await _programmingRepository.GetRoomsAsync(cancellationToken);

        return new ProgrammingDto
        {
            Date = date,
            Screenings = screenings
                .OrderBy(s => s.StartsAt)
                .ThenBy(s => s.Room.Name)
                .Select(s => s.ToProgrammedScreeningDto(takenSeats.GetValueOrDefault(s.Id)))
                .ToList(),
            Movies = movies.Select(m => m.ToProgrammingMovieDto()).ToList(),
            Rooms = rooms.Select(r => r.ToProgrammingRoomDto()).ToList(),
            Genres = []
        };
    }
}
