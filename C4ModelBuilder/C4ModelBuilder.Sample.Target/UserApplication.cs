using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    [C4Component("User application use-case layer", technology: "C# / .NET 8")]
    public sealed class UserApplication : IUserApplication
    {
        private readonly IUserService _userService;

        public UserApplication(IUserService userService) => _userService = userService;

        public Task<User[]> GetUsersAsync(CancellationToken ct) => _userService.GetUsersAsync(ct);
    }
}
