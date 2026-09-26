using C4ModelBuilder.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    [C4Component(Description = "User data source (DB)")]
    public sealed class UserDataSource : IUserDataSource
    {
        public Task<User[]> FetchAll(CancellationToken ct) => Task.FromResult(Array.Empty<User>());
    }
}
