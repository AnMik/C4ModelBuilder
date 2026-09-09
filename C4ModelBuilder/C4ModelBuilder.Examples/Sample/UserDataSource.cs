using System;
using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class UserDataSource : IUserDataSource
    {
        public Task<User[]> FetchAll(CancellationToken ct) => Task.FromResult(Array.Empty<User>());
    }
}
