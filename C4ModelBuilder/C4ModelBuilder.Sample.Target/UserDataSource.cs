using System;
using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Sample.Target
{
    [C4Component(Description = "User data source (DB)")]
    public sealed class UserDataSource : IUserDataSource
    {
        public Task<User[]> FetchAll(CancellationToken ct) => Task.FromResult(Array.Empty<User>());
    }
}
