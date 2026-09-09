using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public interface IUserDataSource
    {
        Task<User[]> FetchAll(CancellationToken ct);
    }
}
