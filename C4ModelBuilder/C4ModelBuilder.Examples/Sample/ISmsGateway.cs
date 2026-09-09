using System.Threading;
using System.Threading.Tasks;

namespace C4ModelBuilder.Examples.Sample
{
    // Интерфейс внешнего сервиса без имплементации в солюшене (аналог вызова внешней библиотеки).
    public interface ISmsGateway
    {
        Task SendAsync(string message, CancellationToken ct);
    }
}
