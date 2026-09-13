using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Sample.Target
{
    public interface IUserRepository
    {
        Task<User[]> GetAll(CancellationToken ct);
    }
}
