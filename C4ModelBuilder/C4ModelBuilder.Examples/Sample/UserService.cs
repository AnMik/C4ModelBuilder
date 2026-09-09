using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository) => _userRepository = userRepository;

        public Task<User[]> GetUsersAsync(CancellationToken ct) => _userRepository.GetAll(ct);
    }
}
