using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Examples.Sample
{
    [C4Component(IsRoot = true, Description = "Admin page: send promo campaigns")]
    public sealed class AdminController
    {
        private readonly IUserApplication _userApplication;
        private readonly ISmsGateway _smsGateway;

        public AdminController(IUserApplication userApplication, ISmsGateway smsGateway)
        {
            _userApplication = userApplication;
            _smsGateway = smsGateway;
        }

        [C4Component(Description = "Send promo campaign via SMS")]
        public async Task SendPromoAsync(CancellationToken ct)
        {
            var users = await _userApplication.GetUsersAsync(ct);
            await _smsGateway.SendAsync("promo", ct);

            System.GC.KeepAlive(users);
        }
    }
}
