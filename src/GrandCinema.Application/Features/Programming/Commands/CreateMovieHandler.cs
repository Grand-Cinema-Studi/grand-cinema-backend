using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Abstractions.Results;
using GrandCinema.Application.Abstractions.Storage;

namespace GrandCinema.Application.Features.Programming.Commands;

public class CreateMovieHandler : ICommandHandler<CreateMovieCommand, Result<Guid>>
{
    private readonly IProgrammingRepository _programmingRepository;
    private readonly IPosterStorage _posterStorage;

    public CreateMovieHandler(IProgrammingRepository programmingRepository, IPosterStorage posterStorage)
    {
        _programmingRepository = programmingRepository;
        _posterStorage = posterStorage;
    }

    public Task<Result<Guid>> Handle(CreateMovieCommand command, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Step 27c");
}
