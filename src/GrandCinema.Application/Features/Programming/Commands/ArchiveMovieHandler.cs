using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;

namespace GrandCinema.Application.Features.Programming.Commands;

public sealed class ArchiveMovieHandler : ICommandHandler<ArchiveMovieCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public ArchiveMovieHandler(IProgrammingRepository programmingRepository, IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        _programmingRepository = programmingRepository;
        _movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public Task<Result<Guid>> Handle(ArchiveMovieCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 28d");
}
