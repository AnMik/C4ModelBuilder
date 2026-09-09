using System;
using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public sealed class UserRepository : IUserRepository
    {
        public Task<User[]> GetAll(CancellationToken ct) => Task.FromResult(Array.Empty<User>());
    }
}
