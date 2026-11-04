using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming.Commands;

public sealed class UpdateScreeningHandler : ICommandHandler<UpdateScreeningCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateScreeningHandler(IProgrammingRepository programmingRepository, IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public Task<Result<Guid>> Handle(UpdateScreeningCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}