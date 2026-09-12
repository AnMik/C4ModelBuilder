using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Sample.Target
{
    public interface IUserApplication
    {
        Task<User[]> GetUsersAsync(CancellationToken ct);
    }
}
