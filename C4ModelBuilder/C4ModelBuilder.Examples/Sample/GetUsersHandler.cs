using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class GetUsersHandler : Rds.Cqrs.Queries.IQueryHandler<GetUsers, User[]>
    {
        private readonly IUserService _userService;

        public GetUsersHandler(IUserService userService) => _userService = userService;

        public Task<User[]> HandleAsync(GetUsers query, CancellationToken ct)
            => _userService.GetUsersAsync(ct);
    }
}
