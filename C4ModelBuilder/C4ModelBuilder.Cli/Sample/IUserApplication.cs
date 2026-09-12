using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public interface IUserApplication
    {
        Task<User[]> GetUsersAsync(CancellationToken ct);
    }
}
