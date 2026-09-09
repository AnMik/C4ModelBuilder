using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly IUserDataSource _userDataSource;

        public UserRepository(IUserDataSource userDataSource) => _userDataSource = userDataSource;

        public Task<User[]> GetAll(CancellationToken ct) => _userDataSource.FetchAll(ct);
    }
}
