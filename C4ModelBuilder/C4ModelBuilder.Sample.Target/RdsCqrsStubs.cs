using System.Threading;
using System.Threading.Tasks;

namespace Rds.Cqrs
{
    // Заглушки библиотеки Rds.Cqrs: анализатор распознаёт CQRS-диспетчеризацию
    // по точным именам этих типов.
    public interface IQuery
    {
    }

    public interface ICommand
    {
    }

    public interface IResultingCommand
    {
    }
}

namespace Rds.Cqrs.Queries
{
    public interface IQueryService
    {
        Task Ask(IQuery query, CancellationToken ct = default);
    }

    public interface IQueryHandler<TQuery, TResult>
    {
        Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
    }
}

namespace Rds.Cqrs.Commands
{
    public interface ICommandProcessor
    {
        Task Process(ICommand command, CancellationToken ct = default);
    }

    public interface ICommandHandler<TCommand>
    {
        Task HandleAsync(TCommand command, CancellationToken ct = default);
    }
}
