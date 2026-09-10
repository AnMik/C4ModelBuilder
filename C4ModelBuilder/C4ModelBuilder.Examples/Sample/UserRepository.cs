using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Examples.Sample
{
    [C4Component]
    public sealed class UserRepository : IUserRepository
    {
        private readonly IUserDataSource _userDataSource;

        public UserRepository(IUserDataSource userDataSource) => _userDataSource = userDataSource;

        public Task<User[]> GetAll(CancellationToken ct) => _userDataSource.FetchAll(ct);
    }
}
