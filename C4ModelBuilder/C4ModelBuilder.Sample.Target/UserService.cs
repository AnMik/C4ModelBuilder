using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    [C4Component(Description = "User business logic")]
    public sealed class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository) => _userRepository = userRepository;

        public Task<User[]> GetUsersAsync(CancellationToken ct) => _userRepository.GetAll(ct);
    }
}
