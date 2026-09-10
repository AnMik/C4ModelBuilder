using System.Threading;
using System.Threading.Tasks;
using C4ModelBuilder.Models.Attributes;

namespace C4ModelBuilder.Examples.Sample
{
    // Интерфейс внешнего сервиса без имплементации в солюшене (аналог вызова внешней библиотеки).
    [C4Component]
    public interface ISmsGateway
    {
        Task SendAsync(string message, CancellationToken ct);
    }
}
