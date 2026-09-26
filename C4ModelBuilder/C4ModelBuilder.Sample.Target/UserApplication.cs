using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    [C4Component(Description = "User application use-case layer")]
    public sealed class UserApplication : IUserApplication
    {
        private readonly IUserService _userService;

        public UserApplication(IUserService userService) => _userService = userService;

        public Task<User[]> GetUsersAsync(CancellationToken ct) => _userService.GetUsersAsync(ct);
    }
}
