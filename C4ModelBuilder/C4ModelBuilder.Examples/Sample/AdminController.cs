using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Examples.Sample
{
    [C4Component(true)]
    public sealed class AdminController
    {
        private readonly IUserApplication _userApplication;
        private readonly ISmsGateway _smsGateway;

        public AdminController(IUserApplication userApplication, ISmsGateway smsGateway)
        {
            _userApplication = userApplication;
            _smsGateway = smsGateway;
        }

        public async Task SendPromoAsync(CancellationToken ct)
        {
            var users = await _userApplication.GetUsersAsync(ct);
            await _smsGateway.SendAsync("promo", ct);

            System.GC.KeepAlive(users);
        }
    }
}
