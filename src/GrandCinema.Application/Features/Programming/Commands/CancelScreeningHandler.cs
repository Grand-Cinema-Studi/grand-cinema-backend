
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming.Commands;

public sealed class CancelScreeningHandler : ICommandHandler<CancelScreeningCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IScreeningReadRepository _screeningReadRepository;
    private readonly TimeProvider _timeProvider;

    public CancelScreeningHandler(IProgrammingRepository programmingRepository, IScreeningReadRepository screeningReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _screeningReadRepository = screeningReadRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(CancelScreeningCommand command, CancellationToken cancellationToken = default)
    {
        var screening = await _programmingRepository.GetScreeningAsync(command.ScreeningId, cancellationToken);
        if (screening is null)
        {
            return ProgrammingErrors.ScreeningNotFound;
        }
        if (screening.StartsAt <= _timeProvider.GetLocalNow().DateTime)
        {
            return ProgrammingErrors.ScreeningStarted;
        }
        if (!command.Confirmed && await _screeningReadRepository.HasTakenSeatsAsync(screening.Id, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken))
        {
            return ProgrammingErrors.ScreeningHasReservations;
        }

        await _programmingRepository.CancelScreeningAsync(screening.Id, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return screening.Id;
    }
}
