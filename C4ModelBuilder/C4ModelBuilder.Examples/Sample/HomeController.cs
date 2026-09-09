using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class HomeController
    {
        private readonly Rds.Cqrs.Queries.IQueryService _queryService;
        private readonly IUserService _userService;

        public HomeController(
            Rds.Cqrs.Queries.IQueryService queryService,
            IUserService userService)
        {
            _queryService = queryService;
            _userService = userService;
        }

        [C4Component]
        public async Task GetUsersAsync(CancellationToken ct)
        {
            var users = await _userService.GetUsersAsync(ct);
            await _queryService.Ask(new GetUsers(), ct);

            System.GC.KeepAlive(users);
        }
    }
}
