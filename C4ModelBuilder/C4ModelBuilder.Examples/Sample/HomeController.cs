using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class HomeController
    {
        private readonly Rds.Cqrs.Queries.IQueryService _queryService;
        private readonly IUserApplication _userApplication;
        private readonly ISmsGateway _smsGateway;

        public HomeController(
            Rds.Cqrs.Queries.IQueryService queryService,
            IUserApplication userApplication,
            ISmsGateway smsGateway)
        {
            _queryService = queryService;
            _userApplication = userApplication;
            _smsGateway = smsGateway;
        }

        [C4Component]
        public async Task GetUsersAsync(CancellationToken ct)
        {
            var users = await _userApplication.GetUsersAsync(ct);
            await _smsGateway.SendAsync("welcome", ct);
            await _queryService.Ask(new GetUsers(), ct);

            System.GC.KeepAlive(users);
        }
    }
}
