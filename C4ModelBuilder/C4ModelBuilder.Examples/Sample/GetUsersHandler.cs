using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Examples.Sample
{
    [C4Component]
    public sealed class GetUsersHandler : Rds.Cqrs.Queries.IQueryHandler<GetUsers, User[]>
    {
        private readonly IUserApplication _userApplication;

        public GetUsersHandler(IUserApplication userApplication) => _userApplication = userApplication;

        public Task<User[]> HandleAsync(GetUsers query, CancellationToken ct)
            => _userApplication.GetUsersAsync(ct);
    }
}
