using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    [C4Component(Description = "User data repository")]
    public sealed class UserRepository : IUserRepository
    {
        private readonly IUserDataSource _userDataSource;

        public UserRepository(IUserDataSource userDataSource) => _userDataSource = userDataSource;

        [C4Component(Description = "Fetch all users from data source")]
        public Task<User[]> GetAll(CancellationToken ct) => _userDataSource.FetchAll(ct);
    }
}
