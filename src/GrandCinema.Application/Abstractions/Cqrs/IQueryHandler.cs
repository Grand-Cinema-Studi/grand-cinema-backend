namespace GrandCinema.Application.Abstractions.Cqrs;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
}