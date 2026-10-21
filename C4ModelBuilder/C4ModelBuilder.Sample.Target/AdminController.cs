using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    [C4Component("Admin page: send promo campaigns", isRoot: true)]
    public sealed class AdminController
    {
        private readonly IUserApplication _userApplication;
        private readonly ISmsGateway _smsGateway;

        public AdminController(IUserApplication userApplication, ISmsGateway smsGateway)
        {
            _userApplication = userApplication;
            _smsGateway = smsGateway;
        }

        [C4Component("Send promo campaign via SMS", isRoot: true)]
        public async Task SendPromoAsync(CancellationToken ct)
        {
            var users = await _userApplication.GetUsersAsync(ct);
            await SendOneAsync("promo", ct);

            GC.KeepAlive(users);
        }

        [C4Component("Send one SMS message", technology: "Twilio SDK")]
        private async Task SendOneAsync(string message, CancellationToken ct)
        {
            await _smsGateway.SendAsync(message, ct);
        }
    }
}
