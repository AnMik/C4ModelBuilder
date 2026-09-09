using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class GetUsersHandler : Rds.Cqrs.Queries.IQueryHandler<GetUsers, User[]>
    {
        private readonly IUserApplication _userApplication;

        public GetUsersHandler(IUserApplication userApplication) => _userApplication = userApplication;

        public Task<User[]> HandleAsync(GetUsers query, CancellationToken ct)
            => _userApplication.GetUsersAsync(ct);
    }
}
