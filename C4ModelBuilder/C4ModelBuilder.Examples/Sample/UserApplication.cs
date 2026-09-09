using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class UserApplication : IUserApplication
    {
        private readonly IUserService _userService;

        public UserApplication(IUserService userService) => _userService = userService;

        public Task<User[]> GetUsersAsync(CancellationToken ct) => _userService.GetUsersAsync(ct);
    }
}
