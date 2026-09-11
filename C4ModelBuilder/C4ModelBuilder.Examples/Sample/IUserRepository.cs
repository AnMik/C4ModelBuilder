using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    public interface IUserRepository
    {
        Task<User[]> GetAll(CancellationToken ct);
    }
}
